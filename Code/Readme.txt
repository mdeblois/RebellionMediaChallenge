This is the readme file for Rebellion Code Challenge


Data Structures:
Products consists of a simple tree with the following levels:
   Level 0 -> Manufacturers
   Level 1 -> Products
   Level 2 -> Family (only if there are more than one for that particular model)

The nodes have the following properties:
   Value|string -> string for the value of the node
   CompareValue|string -> cleaned version of Value
   Children|List<Node> -> list of children nodes
   Product|Product -> only set on leaf nodes, null otherwise

Product, Listing and Results are simple classes.


Process:
1. Parse Products and create tree
2. Go through listings one line at a time
   2.1 clean listing title and manufacturer values
       2.1.1 get one long string for title
       2.1.2 get list of substrings for title to help model matching
   2.2 check if manufacturer exists
   2.3 if yes, go through models of manufacturer
       2.3.1 go through title substring and match based on continuation of model's compareValue and 1 or many (in sequence) sub string from title
       2.3.2 if model match, verify that no other models previous to this one has matched as well
   2.4 store product and listing in results list
3. Write results to file


Execution:
1. To execute with default file locations (Data\):
    RebellionCodeChallenge.exe
2. To execute with different file locations:
    RebellionCodeChallenge.exe [products] [listings] [results|optional]
