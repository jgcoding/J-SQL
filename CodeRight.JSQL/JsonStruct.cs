using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

public partial class UserDefinedFunctions
{
    public static readonly StringComparison sc = StringComparison.InvariantCultureIgnoreCase;
    public class SelectionCriteria
    {
        public String Endpoint { get; set; }
        public String Index { get; set; }
        public Int32 Ordinal { get; set; }
        public String ViewName { get; set; }
        public String PropertyName { get; set; }
        public String Operator { get; set; }
        public String PropertyValue { get; set; }
        public String BlockType { get; set; }
    }
    public class JsonRow
    {
        public String DocumentName { get; set; }
        public String IncludedKey { get; set; }
        public int ElementID { get; set; }
        public int ParentID { get; set; }
        public int ObjectID { get; set; }
        public String Command { get; set; }
        public String Url { get; set; }
        public String KeyColumn { get; set; }
        public String NodeKey { get; set; }
        public String Node { get; set; }
        public String ItemKey { get; set; }
        public String ItemValue { get; set; }
        public String ItemType { get; set; }
        public String Value_U { get; set; }
        public String Selector { get; set; }
        public String Filter { get; set; }
        public String Operand { get; set; }
        public String FilterValue { get; set; }
        public String Label { get; set; }
        public Boolean IsVisible { get; set; }
        public Boolean IsDirty { get; set; }
        public JsonRow()
        {
            this.ElementID = 1;
            this.ParentID = 0;
            this.ObjectID = 1;
            this.Command = "select";
            this.Url = "/";
            this.Node = "root";
            this.KeyColumn = "NA";
            this.NodeKey = Guid.Empty.ToString();
            this.ItemType = "object";
            this.IsVisible = true;
            this.IsDirty = false;
        }
    }

    public struct IndexRow
    {
        public Guid IndexSchemaID;
        public String IndexName;
        public String DocumentName;
        public String IndexSchema;
        public Boolean IsDefault;
    }
    public struct UrlAncestry
    {
        public Int32 Generation;
        public String Node;
        public String NodeKey;
    }
    public class IndexedRow
    {
        public String DocumentName { get; set; }
        public String IncludedKey { get; set; }
        public String NodeKey { get; set; }
        public String Url { get; set; }
        public String ItemKey { get; set; }
        public String ItemValue { get; set; }
        public String ItemType { get; set; }
        public String Label { get; set; }
        public String Selector { get; set; }
        public String Filter { get; set; }
        public String operand { get; set; }
        public String FilterValue { get; set; }
        public Boolean IsVisible { get; set; }
        public IndexedRow()
        {
            this.IsVisible = true;
        }
    }

    public struct IncludedRow
    {
        public String IncludedKey;
        public String DocumentID;
        public String Document;
        public String DocumentName;
        public int PublicDocumentID;
    }

    public class JsonMapSchema
    {
        public String Url { get; set; }
        public String Reference { get; set; }
        public String Selector { get; set; }
    }

    public class JsonMergeSchema
    {
        public String SourceUrl { get; set; }
        public String SourceReference { get; set; }
        public String SourceSelector { get; set; }
        public String SourceKey { get; set; }
        public String SourceValue { get; set; }
        public String TargetUrl { get; set; }
        public String TargetReference { get; set; }
        public String TargetSelector { get; set; }
        public String TargetKey { get; set; }
        public String TargetValue { get; set; }
        public JsonMergeSchema()
        {

        }
    }

    public class JsonReduceSchema
    {
        public String Url { get; set; }
        public String Reference { get; set; }
        public String Selector { get; set; }
        public String Filter { get; set; }
        public String Operand { get; set; }
        public String FilterValue { get; set; }
        public Boolean IsVisible { get; set; }
        public String Label { get; set; }
        public JsonReduceSchema()
        {
            this.IsVisible = true;
        }
    }

    public class JsonIndexSchema
    {
        public String DocumentName { get; set; }
        public String IndexName { get; set; }
        public ArrayList MapIndex { get; set; }
        public ArrayList MergeIndex { get; set; }
        public ArrayList ReduceIndex { get; set; }
        public ArrayList Include { get; set; }
        public JsonIndexSchema()
        {
            this.MapIndex = new ArrayList();
            this.MergeIndex = new ArrayList();
            this.ReduceIndex = new ArrayList();
            this.Include = new ArrayList();
        }
    }
}
