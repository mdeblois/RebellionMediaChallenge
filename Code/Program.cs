
using System;
using RebellionCodeChallenge.Services;

namespace RebellionCodeChallenge {
   class Program {
      static void Main(string[] args) {
         var productFileName = "Data/products.txt";
         if(args.Length == 1) {
            productFileName = args[0];
         }
         var listingFileName = "Data/listings.txt";
         if(args.Length == 2) {
            listingFileName = args[1];
         }

         // get the arguments, if there are two arguments, use
         // the default for the output
         var resultsFileName = "result.txt";
         if(args.Length > 2) {
            resultsFileName = args[2];
         }

         var statusCode = Parser.Parse(productFileName, listingFileName, resultsFileName);
         Console.Write((int) statusCode);
      }
   }
}
