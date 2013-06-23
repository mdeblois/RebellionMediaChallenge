using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RebellionCodeChallenge.Entities {
   public class Node {
      public string Value { get; set; }
      public string CompareValue { get; set; }
      public List<Node> Children { get; set; }
      public Product Product { get; set; }
   }
}
