using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RebellionCodeChallenge.Constants;
using RebellionCodeChallenge.Entities;

namespace RebellionCodeChallenge.Services {
   public class Parser {
      private const string SPACE = " ";
      private const string SLASH = "/";
      private const string DASH = "-";
      private const string UNDERSCORE = "_";

      /// <summary>
      /// Will parse the products, then the listings and than will
      /// write the results file
      /// </summary>
      /// <param name="_productFileName"></param>
      /// <param name="_listingsFileName"></param>
      /// <param name="_resultsFileName"></param>
      /// <returns></returns>
      public static EStatusCode Parse(string _productFileName, string _listingsFileName, string _resultsFileName) {
         var startTime = DateTime.Now;
         // need to parse the product file and create a dictionary
         List<Node> productTree;
         var statusCode = ParseProducts(_productFileName, out productTree);
         var productsParsedTime = DateTime.Now;

         if(statusCode!= EStatusCode.Success) {
            // parsing of product was not successful so let the caller of 
            // this method know the result
            return statusCode;
         }

         // Parse the listings
         Dictionary<string, Result> results;
         statusCode = ParseListings(productTree, _listingsFileName, out results);
         var listingsParsedTime = DateTime.Now;

         if(statusCode != EStatusCode.Success) {
            // Parsing of the listing was not successful so 
            // return the statusCode
            return statusCode;
         }

         // Create the results file
         statusCode = WriteResults(results, _resultsFileName);
         var resultsWrittenTime = DateTime.Now;

         Console.WriteLine(string.Format("Time to parse products:{0} \r\nTime to parse listings:{1} \r\nTime to write results:{2}", productsParsedTime.Subtract(startTime).TotalSeconds, listingsParsedTime.Subtract(productsParsedTime).TotalSeconds, resultsWrittenTime.Subtract(listingsParsedTime).TotalSeconds));

         return statusCode;
      }

      /// <summary>
      /// This method will
      /// </summary>
      /// <param name="_productFileName"></param>
      /// <param name="_productTree"></param>
      /// <returns></returns>
      private static EStatusCode ParseProducts(string _productFileName, out List<Node> _productTree) {
         _productTree = new List<Node>();

         // get each line which is a json object representing a product
         var products = File.ReadAllLines(_productFileName);

         // go through each product, create the object and 
         // set the dictionary
         foreach (var jsonProductString in products) {
            var product = new Product();
            var jsonProduct = JObject.Parse(jsonProductString);
            product.ProductName = jsonProduct[JsonConstants.PRODUCT_NAME].ToString();
            product.Manufacturer = jsonProduct[JsonConstants.MANUFACTURER].ToString();
            if (jsonProduct[JsonConstants.FAMILY] != null) {
               product.Family = jsonProduct[JsonConstants.FAMILY].ToString();
            }
            product.Model = jsonProduct[JsonConstants.MODEL].ToString();
            product.AnnouncedDate = DateTime.Parse(jsonProduct[JsonConstants.ANNOUNCED_DATE].ToString());


            // check if manufacturer already exists
            var compareValue = CleanString(product.Manufacturer);
            Node manufacturerNode = null;
            if(_productTree.Count(_manufacturer => _manufacturer.CompareValue == compareValue) == 0) {
               manufacturerNode = new Node {CompareValue = compareValue, Value = product.Manufacturer, Children = new List<Node>()};
               _productTree.Add(manufacturerNode);
            }else {
               foreach (var node in _productTree.Where(_node => _node.CompareValue == compareValue)) {
                  manufacturerNode = node;
                  break;
               }
            }

            // check if model already exists
            var modelAlreadyExists = false;
            var modelCompareValue = CleanString(product.Model);
            foreach (var modelNode in manufacturerNode.Children) {
               if (modelNode.CompareValue != modelCompareValue) {
                  continue;
               }

               modelAlreadyExists = true;

               // only add children if either the current or
               // new product has a family
               if(string.IsNullOrEmpty(modelNode.Product.Family) && string.IsNullOrEmpty(product.Family)) {
                  break;
               }

               // model exists, so check if there's already children
               string familyCompareValue = string.Empty;
               if(modelNode.Children == null) {
                  // there are no children so create children list of nodes
                  // that will be used for family
                  if (!string.IsNullOrEmpty(modelNode.Product.Family)) {
                     familyCompareValue = CleanString(modelNode.Product.Family);
                  }
                  modelNode.Children = new List<Node> {new Node {Children = null, Product = modelNode.Product, Value = modelNode.Product.Family, CompareValue = familyCompareValue}};
               }

               // make sure to only add if the family does not already exist
               familyCompareValue = CleanString(product.Family);
               if (modelNode.Children.Count(_family => _family.CompareValue == familyCompareValue) == 0) {
                  // now add new model to children node
                  modelNode.Children.Add(new Node {Children = null, Product = product, Value = product.Family, CompareValue = familyCompareValue});
               }

               // can break as there will only be one model
               break;
            }

            if (!modelAlreadyExists) {
               // model does not exist, so create node and add it to manufacturer
               // dictionary
               manufacturerNode.Children.Add(new Node { Children = null, Product = product, Value = product.Model, CompareValue = modelCompareValue});
            }
         }

         return EStatusCode.Success;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="_productTree"></param>
      /// <param name="_listingsFileName"></param>
      /// <param name="_results"></param>
      /// <returns></returns>
      private static EStatusCode ParseListings(IEnumerable<Node> _productTree, string _listingsFileName, out  Dictionary<string, Result> _results) {
         _results = new Dictionary<string, Result>();

         // open stream for listings
         var listingsStream = new StreamReader(_listingsFileName);
         while (!listingsStream.EndOfStream) {
            var jsonListingString = listingsStream.ReadLine();

            // make sure line has an object
            if (jsonListingString == null || jsonListingString.Length <= 1) {
               continue;
            }

            // parse line into object
            var listing = new Listing();
            var jsonListing = JObject.Parse(jsonListingString);
            listing.Title = jsonListing[JsonConstants.TITLE].ToString();
            listing.Manufacturer = jsonListing[JsonConstants.MANUFACTURER].ToString();
            listing.Currency = jsonListing[JsonConstants.CURRENCY].ToString();
            listing.Price = jsonListing[JsonConstants.PRICE].ToString();

            // search listing within product tree
            // set some strings
            var tmpTitle = CleanString(listing.Title);
            var titleSubStrings = ParseTitle(listing.Title);
            var tmpManufacturer = CleanString(listing.Manufacturer);

            // first get list of manufacturers,
            var manufacturers = _productTree.Where(_manufacturer => tmpManufacturer.Contains(_manufacturer.CompareValue)).ToList();

            // if there are no manufacturer found, check for manufacturer in title
            if (manufacturers.Count == 0) {
               //manufacturers.AddRange(_productTree.Where(_manufacturer => tmpTitle.Contains(_manufacturer.CompareValue)));

               //if (manufacturers.Count == 0) {
                  // if there are still no manufacturer, log it
                  Logger.Instance().Log(string.Format("There are no manufacturer for this listing:{0}", jsonListingString));
                  continue;
               //}
            }

            // search model
            // go through all models of all manufacturers and check if the model is contained within the title.
            Node matchedNode = null;
            foreach (var modelNode in manufacturers.SelectMany(_manufacturerNode => _manufacturerNode.Children)) {
               // go through each substring of the title and compare to the model's compare value
               var modelMatched = DoesModelMatch(titleSubStrings, modelNode);

               if (!modelMatched) {
                  continue;
               }

               Node compareNode = null;
               // first check if there are children
               if (modelNode.Children != null) {
                  // there are multiple families for this model, 
                  // we need to search the listing title for the family
                  foreach (var family in modelNode.Children) {
                     var cleanedFamily = CleanString(family.Value);
                     if (compareNode == null && tmpTitle.Contains(cleanedFamily)) {
                        // family is found so set the product
                        compareNode = new Node {CompareValue = modelNode.CompareValue, Value = modelNode.Value, Product = family.Product};
                     } else if (compareNode != null && tmpTitle.Contains(cleanedFamily)) {
                        // there are multiple families in the titles which means multiple
                        // products so need to go to next listing
                        break;
                     }
                  }

                  if (compareNode == null) {
                     // no family found in title so go to next model
                     continue;
                  }
               } else {
                  // there are no children so set the compare node 
                  // to be a copy of the model node
                  compareNode = new Node {CompareValue = modelNode.CompareValue, Value = modelNode.Value, Product = modelNode.Product};
               }

               // Now check to see
               // if this model is a subset or superset of previously matched
               // model.
               if (matchedNode != null) {
                  var compareNodeIndex = tmpTitle.IndexOf(compareNode.CompareValue);
                  var matchedNodeIndex = tmpTitle.IndexOf(matchedNode.CompareValue);
                  if ( compareNodeIndex !=  matchedNodeIndex) {
                     // the indices are different, so this listing is for 
                     // multiple products and therefore the listing should be skipped
                     break;
                  } else {
                     // if the indices are the same, pick the one which is 
                     // longest
                     if (matchedNode.CompareValue.Length < compareNode.CompareValue.Length) {
                        matchedNode = compareNode;
                     }
                  }
               } else {
                  // this is the first matched model so no comparison needed
                  matchedNode = compareNode;
               }
            }

            
            if (matchedNode == null) {
               // still didn't find anything so log it
               Logger.Instance().Log(string.Format("There were no models found for this listing: {0}", jsonListingString));
               continue;
            } 
            
            // Add listing to the result
            var matchedProduct = matchedNode.Product;
            if (!_results.ContainsKey(matchedProduct.ProductName)) {
               _results.Add(matchedProduct.ProductName, new Result { ProductName = matchedProduct.ProductName, Listings = new List<Listing>() });
            }

            _results[matchedProduct.ProductName].Listings.Add(listing);
         }

         listingsStream.Close();
         return EStatusCode.Success;
      }

      private static bool DoesModelMatch(IEnumerable<string> _titleSubStrings, Node _modelNode) {
         var modelMatched = false;
         var lastIndex = -1;
         var comparedLength = 0;
         var comparedCharacters = new List<int>();

         foreach (var titleSubStr in _titleSubStrings) {
            // if the sub string in title is greater than model, then it's not this one, 
            // so just move on.
            if (titleSubStr.Length > _modelNode.CompareValue.Length) {
               lastIndex = -1;
               comparedLength = 0;
               comparedCharacters.Clear();
            } else if (titleSubStr.Length < _modelNode.CompareValue.Length) {
               // title sub string is less, so check if the model value
               // contains this sub string and record the index.
               var ssIndex = _modelNode.CompareValue.IndexOf(titleSubStr, comparedLength);
               if (ssIndex < 0) {
                  // it wasn't found so reset values and go to next sub string
                  lastIndex = -1;
                  comparedLength = 0;
                  comparedCharacters.Clear();
                  continue;
               }

               if (ssIndex > -1 && lastIndex == -1) {
                  // this is the first found string, so just store it as the last
                  lastIndex = ssIndex;
                  comparedLength += titleSubStr.Length;
                  comparedCharacters.AddRange(titleSubStr.Select((_t, _i) => _i + ssIndex));
               } else if (ssIndex > -1) {
                  // we're already 'active' so check to make sure index does not overlap
                  // with previous index and found string
                  var isOverlapped = false;
                  for (var i = 0; i < titleSubStr.Length; i++) {
                     if (comparedCharacters.Contains(i + ssIndex)) {
                        isOverlapped = true;
                     }
                  }
                  if (isOverlapped) {
                     // some characters are overlapped so clear previous info
                     // and set to this new sting
                     lastIndex = ssIndex;
                     comparedLength = titleSubStr.Length;
                     comparedCharacters.Clear();
                     comparedCharacters.AddRange(titleSubStr.Select((_t, _i) => _i + ssIndex));
                  } else {
                     // there's no overlap so check if length would equal
                     // model value
                     comparedLength += titleSubStr.Length;
                     if (_modelNode.CompareValue.Length == comparedLength) {
                        modelMatched = true;
                        break;
                     }
                     // still not enough so add index of compared characters
                     for (var i = ssIndex; i < titleSubStr.Length; i++) {
                        comparedCharacters.Add(i);
                     }
                  }
               }
            } else {
               if (titleSubStr == _modelNode.CompareValue) {
                  // it is equal so set matched to true and 
                  // go to next model
                  modelMatched = true;
                  break;
               }
               // it's not equal, so reset values and go to next sub string
               lastIndex = -1;
               comparedLength = 0;
               comparedCharacters.Clear();
               continue;
            }
         }

         return modelMatched;
      }

      /// <summary>
      /// Go through giving results and write the json to a file
      /// </summary>
      /// <param name="_results">results</param>
      /// <param name="_resultsFileName">filename to save the results</param>
      /// <returns></returns>
      private static EStatusCode WriteResults(Dictionary<string, Result> _results, string _resultsFileName) {
         // open file
         var resultsStream = new StreamWriter(_resultsFileName);
         var resultJsonWriter = new JsonTextWriter(resultsStream) {Formatting = Formatting.None};

         // go through results
         foreach (var result in _results.Values) {
            var jsonArray = new JArray();
            foreach (var listing in result.Listings) {
               var jsonListing = new JObject
                                    {
                                       {JsonConstants.TITLE, listing.Title}, 
                                       {JsonConstants.MANUFACTURER, listing.Manufacturer}, 
                                       {JsonConstants.CURRENCY, listing.Currency}, 
                                       {JsonConstants.PRICE, listing.Price}
                                    };
               jsonArray.Add(jsonListing);
            }
            var jsonObject = new JObject
                                {
                                   { JsonConstants.PRODUCT_NAME, result.ProductName },
                                   { JsonConstants.LISTINGS, jsonArray}
                                };

            // write the product to file
            jsonObject.WriteTo(resultJsonWriter);
            resultsStream.WriteLine();
         }

         resultsStream.Close();
         return EStatusCode.Success;
      }

      /// <summary>
      /// Helper method to clean the strings (remove spaces, dashes, underscores and set to lower case)
      /// </summary>
      /// <param name="_stringToClean"></param>
      /// <returns></returns>
      private static string CleanString(string _stringToClean) {
         return _stringToClean.Replace(SLASH, string.Empty).Replace(DASH, string.Empty).Replace(UNDERSCORE, string.Empty).Replace(SPACE, string.Empty).ToLowerInvariant();
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="_title"></param>
      /// <returns></returns>
      private static List<string> ParseTitle(string _title) {
         var stringArray = _title.ToLowerInvariant().Split(new char[] {' ', ',', '-', '/', '_'}, StringSplitOptions.RemoveEmptyEntries);
         return stringArray.ToList();
      }
   }
}
