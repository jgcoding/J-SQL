using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

public partial class UserDefinedFunctions
{
    public static readonly StringComparison sc = StringComparison.InvariantCultureIgnoreCase;

    public class JsonRow
    {        
        public int ParentID { get; set; }
        public int ObjectID { get; set; }
        public String Node { get; set; }
        public String ItemKey { get; set; }
        public String ItemValue { get; set; }
        public String ItemType { get; set; }
        public JsonRow()
        {
            this.ParentID = 0;
            this.ObjectID = 1;
            this.Node = "root";
            this.ItemKey = String.Empty;
            this.ItemType = "object";
        }
    }
}
