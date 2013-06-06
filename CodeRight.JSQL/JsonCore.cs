using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

using Microsoft.SqlServer.Server;
using System.Data.SqlClient;
using System.Data;
using System.Text.RegularExpressions;

public partial class UserDefinedFunctions
{
    /// <summary>
    /// Returns a collection of documents referenced within a selection of JSON documents
    /// </summary>
    /// <param name="input">the document id of the document referenced</param>
    /// <returns>IEnumerable</returns>
    //[SqlFunction(FillRowMethodName = "IncludedRows",
    //TableDefinition = "IncludedKey nvarchar(100), DocumentID uniqueidentifier")]
    public static IEnumerable SelectIncluded(String input)
    {
        ArrayList rows = new ArrayList();
        ParseInclusions(input, rows);
        return rows;
    }

    /// <summary>
    /// A strongly typed row object containing a document referenced within a parent document
    /// </summary>
    /// <param name="row">The row object returned within the result set of the CLR function</param>
    /// <param name="nodeKey">The id of the node selected</param>
    /// <param name="_id">The document id of the document referenced</param>
    private static void IncludedRows(Object row, out String nodeKey, out Guid _id)
    {
        IncludedRow col = (IncludedRow)row;
        nodeKey = col.nodeKey.ToString();
        _id = new Guid(col._id);

    }

    /// <summary>
    /// helper method for parsing and extracting the document id of documents referenced in a parent document
    /// </summary>
    /// <param name="input">The document id of the reference embedded within a parent document</param>
    /// <param name="rows">The accumulated rows of documents retrieved using the references embedded in a parent document</param>
    private static void ParseInclusions(String input, ArrayList rows)
    {
        foreach (Match m in rxIncludedKey.Matches(input))
        {
            IncludedRow column = new IncludedRow
            {
                nodeKey = m.Groups["IncludedKey"].Value,
                _id = m.Groups["DocumentID"].Value
            };
            rows.Add(column);
        }
    }

    /// <summary>
    /// Compares a url template provided with the template of each element in the selection to determine if they match. 
    /// Serves as means for selecting elements matching certain criteria as in a WHERE clause
    /// </summary>
    /// <param name="url">The node address of an element within a document</param>
    /// <returns>IEnumerable - Returns a list of elements matching the url template. </returns>
    public static IEnumerable CompareJsonUrl(String url)
    {
        ArrayList rows = new ArrayList();
        StringBuilder template = new StringBuilder();

        string[] groups = rxUrl.GetGroupNames();
        foreach (Match m in rxUrl.Matches(url))
        {
            /*step through each object extracted from the url and reassemble it, replacing the UUID's with placeholders*/
            foreach (string s in groups)
            {
                Group g = m.Groups[s];
                CaptureCollection cc = g.Captures;
                foreach (Capture cap in cc)
                {
                    /*If this is a captured Node from the Url.*/
                    if (String.Equals(s, "node", sc))
                        template.AppendFormat("/{0}:{1}", cap.Value, "{0}");
                    /*If this is a captured ItemKey from the Url.*/
                    if (String.Equals(s, "itemkey", sc))
                        template.AppendFormat("/{0}", cap.Value);
                }
            }
        }
        return rows;
    }

    /// <summary>
    /// Helper method used to dynamically construct either an OUTER APPLY or CROSS APPLY block. 
    /// </summary>
    /// <param name="query">Contains the partially constructed T-SQL query string to which an APPLY block may be added.</param>
    /// <param name="p">Contains the select criteria</param>
    /// <param name="pcount">Serves as an iterative terminator</param>
    /// <returns>String containing the T_SQL query and APPLY block</returns>
    private static StringBuilder ApplyBlock(StringBuilder query, SelectionCriteria p, Int32 pcount)
    {
        if (pcount.Equals(1))
        {
            p.BlockType = "CROSS";
            query.AppendFormat("\t{0} APPLY(", String.IsNullOrEmpty(p.BlockType) ? "CROSS" : p.BlockType);
            query.AppendLine();
            query.AppendFormat("\t\tselect [_id] from [{0}].[dbo].[{1}]", p.Endpoint, String.IsNullOrEmpty(p.Index) ? "JsonIndex" : p.Index);
            query.AppendFormat("\t\t\twhere [ViewName] = '{0}'", p.ViewName);
            query.AppendLine();
            query.AppendFormat("\t\t\t\tand [Label] = '{0}'", p.PropertyName);
            query.AppendLine();
            query.AppendFormat("\t\t\t\tand [ItemValue] {0} {1}", p.Operator, p.PropertyValue);
            query.AppendLine();
            query.AppendFormat("\t\t\t\tand [_id] = [ix{0}].[_id]", pcount.Equals(1) ? String.Empty : (pcount - 1).ToString());
            query.AppendLine();
            query.AppendFormat("\t)[ix{0}]", pcount);
            query.AppendLine();
        }
        return query;
    }

    /// <summary>
    /// Converts a JSON string containing selection criteria for a query and builds out the T-SQL statement 
    /// necessary to achieve the results sought.
    /// </summary>
    /// <param name="json">The selection criteria for the query statement</param>
    /// <returns>String</returns>
    private static String BuildSearchCriteria(String json)
    {
        /*parse the search criteria*/
        var criteria = ToJsonTable(json).Cast<JsonRow>().Where(w => !String.IsNullOrEmpty(w.ItemValue)).ToList();

        /*select the view name*/
        String viewName = criteria.First(v => String.Equals(v.ItemKey, "EntityType")).ItemValue;
        /*select the block type, if provided*/
        JsonRow btypeRow = criteria.FirstOrDefault(v => String.Equals(v.ItemKey, "BlockType"));
        /*apply the default if empty*/
        String btype = btypeRow == null ? "and" : btypeRow.ItemValue;
        /*convert the operator to the block type*/
        String blockType = String.IsNullOrEmpty(btype) ? "cross" : (String.Equals(btype, "or", sc) ? "outer" : "cross");

        String endpoint = criteria.First(v => String.Equals(v.ItemKey, "Endpoint")).ItemValue;
        if (String.IsNullOrEmpty(endpoint))
        {
            return "ERROR!: A valid database name must be provided in the criteria.";
        }
        StringBuilder query = new StringBuilder();
        /*initialize the select statement*/
        query.AppendFormat("select [ix].[DocumentID], [ix].[IndexDocument] from [{0}].[dbo].[DocumentIndex] [ix]", endpoint);
        query.AppendLine();

        /*select the properties to which the criteria is to be applied*/
        var prop = criteria.Where(w => !String.Equals(w.Node, "root", sc)).Select(s => s.Node).Distinct().ToList();

        /*initialize the criteria block counter*/
        Int32 pcount = 0;

        /*loop through each property, building apply blocks for each operator to be applied*/
        foreach (var c in prop)
        {
            SelectionCriteria wh = new SelectionCriteria();
            wh.ViewName = viewName;
            wh.PropertyName = c;
            wh.BlockType = blockType;

            /*build in clause, if applicable*/
            var inc = criteria.Where(w => String.Equals(w.Node, c, sc) && String.IsNullOrEmpty(w.ItemKey)).Select(s => s.ItemValue).ToList();
            if (inc.Count > 0)
            {
                wh.Operator = "in";
                //wh.BlockType = "cross";
                wh.BlockType = blockType;
                foreach (var v in inc)
                {
                    wh.PropertyValue = String.Concat(wh.PropertyValue, String.Format("'{0}',", v));
                }
                wh.PropertyValue = wh.PropertyValue.Remove(wh.PropertyValue.Length - 1, 1);//remove the trailing comma
                wh.PropertyValue = wh.PropertyValue.Insert(0, "(");
                wh.PropertyValue = String.Concat(wh.PropertyValue, ")");
                pcount++;
                query = ApplyBlock(query, wh, pcount);
            }
            /*build LIKE clause, if applicable*/
            var like = criteria.Where(w => String.Equals(w.Node, c, sc) && String.Equals(w.ItemKey, "like", sc)).ToList();
            if (like.Count > 0)
            {
                wh.Operator = like.First(w => String.Equals(w.ItemKey, "like", sc)).ItemKey;
                wh.PropertyValue = String.Format("'{0}'", like.First(w => String.Equals(w.ItemKey, "like", sc)).ItemValue);
                wh.BlockType = blockType;
                pcount++;
                query = ApplyBlock(query, wh, pcount);
            }
            /*build BETWEEN clause, if applicable*/
            List<JsonRow> between = criteria.Where(w => String.Equals(w.Node, c, sc) && (String.Equals(w.ItemKey, "between", sc) | String.Equals(w.ItemKey, "and", sc))).ToList();
            if (between.Count > 0)
            {
                wh.Operator = "between";
                wh.Index = "DateIndex";
                wh.BlockType = blockType;
                wh.PropertyValue = String.Format("'{0}' and '{1}'"
                    , between.First(b => String.Equals(b.ItemKey, "between", sc)).ItemValue
                    , between.First(b => String.Equals(b.ItemKey, "and", sc)).ItemValue);
                pcount++;
                query = ApplyBlock(query, wh, pcount);
                wh.Index = "JsonIndex";
            }
        }
        /*close out the search query*/
        query.AppendFormat("\twhere [ix].[IndexName] = '{0}'", viewName);
        return query.ToString();
    }

    /// <summary>
    /// Return result row object from a document view selection function
    /// </summary>
    /// <param name="obj">The row returned from the function</param>
    /// <param name="_id">The document id of the view returned</param>
    /// <param name="view">The view document returned</param>
    private static void MatchedViews(Object obj, out String _id, out String view)
    {
        Object[] column = (Object[])obj;
        _id = (String)column[0];
        view = (String)column[1];
    }

    //[SqlFunction(DataAccess = DataAccessKind.Read, SystemDataAccess = SystemDataAccessKind.Read, FillRowMethodName = "MatchedViews",
    //    TableDefinition = "[_id] nvarchar(36), [view] nvarchar(max)")]
    public static IEnumerable CriteriaSearch(String json)
    {
        ArrayList rows = new ArrayList();

        using (SqlConnection cn = new SqlConnection("context connection = true"))
        {
            cn.Open();
            SqlCommand command = new SqlCommand(BuildSearchCriteria(json), cn);
            SqlDataReader dr = command.ExecuteReader();
            while (dr.Read())
            {
                Object[] column = new Object[2];
                column[0] = dr[0].ToString();
                column[1] = dr[1].ToString();
                rows.Add(column);
            }
        }
        return rows;
    }
    
    /// <summary>
    /// returns a row containing the original value and the updated value resulting from an update of a document
    /// </summary>
    /// <param name="obj">The result row</param>
    /// <param name="ItemKey">The item key included in the update</param>
    /// <param name="Value_O">The original item value related to an item key included in an update</param>
    /// <param name="Value_U">The new value related to the item key</param>
    private static void ItemUpdateView(Object obj, out String ItemKey, out String Value_O, out String Value_U)
    {
        Object[] column = (Object[])obj;
        ItemKey = (String)column[0];
        Value_O = String.IsNullOrEmpty((String)column[1]) ? "null" : (String)column[1];
        Value_U = String.IsNullOrEmpty((String)column[2]) ? "null" : (String)column[2];
    }

    //[SqlFunction()]
    public static Boolean rxPivot(String json, String key, String value)
    {
        return Regex.IsMatch(json, value);
    }

    /// <summary>
    /// returns true for each element matching the criteria supplied.
    /// </summary>
    /// <param name="json">The JSON on which the match is applied</param>
    /// <param name="value">The value used in the comparison</param>
    /// <returns></returns>
    [SqlFunction()]
    public static Boolean rxContains(String json, String value)
    {
        return Regex.IsMatch(json, value);
    }

    /// <summary>
    /// extracts and selects the node key embedded in a collection of objects
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    //[SqlFunction()]
    public static String SelectKey(String json)
    {
        return rxKey.Match(json).Groups["nodeKey"].Value;
    }

    /// <summary>
    /// returns a collection of urls merged as a result of two documents being combined or merged as one
    /// </summary>
    /// <param name="sourceUrl">The url of the element in the source document</param>
    /// <param name="sourceKey">The item key of the element in the source document</param>
    /// <param name="targetUrl">The url of the target element</param>
    /// <returns></returns>
    //[SqlFunction(FillRowMethodName = "MergedUrls",
    //    TableDefinition = "Url nvarchar(500), Selector nvarchar(500)")]
    public static IEnumerable MergeUrl(String sourceUrl, String sourceKey, String targetUrl)
    {
        ArrayList rows = new ArrayList();
        Object[] column = new Object[2];
        column[0] = String.Format("{0}:{1}{2}", sourceUrl.LastIndexOf("/").Equals(sourceUrl.Length - 1) ? sourceUrl.Remove(sourceUrl.Length - 1, 1) : sourceUrl, sourceKey, targetUrl);
        column[1] = TemplateJsonUrl(column[0].ToString());
        rows.Add(column);
        return rows;
    }

    /// <summary>
    /// return object form the MergeUrl function
    /// </summary>
    /// <param name="obj">result row form the MergeUrl function</param>
    /// <param name="Url">The newly merged url</param>
    /// <param name="Selector">The selector used to locate matches</param>
    private static void MergedUrls(Object obj, out String Url, out String Selector)
    {
        Object[] column = (Object[])obj;
        Url = (String)column[0];
        Selector = (String)column[1];
    }

    /// <summary>
    /// returns a collection of url row objects
    /// </summary>
    /// <param name="url">The url to be parsed or split into tabular format</param>
    /// <returns>IEnumerable</returns>
    //[SqlFunction(FillRowMethodName = "ParsedUrlRows",
    //TableDefinition = "Generation int, NodeKey nvarchar(36), Node nvarchar(100)")]
    public static IEnumerable ParseUrl(String url)
    {
        ArrayList rows = new ArrayList();
        string[] groups = rxUrlAncestry.GetGroupNames();
        Int32 gen = 0;
        foreach (Match m in rxUrlAncestry.Matches(url))
        {
            /*step through each object extracted from the url and reassemble it, replacing the UUID's with placeholders*/
            foreach (string s in groups)
            {
                Group g = m.Groups[s];
                CaptureCollection cc = g.Captures;
                foreach (Capture cap in cc)
                {
                    if (String.Equals(s, "Object", sc))
                    {
                        UrlAncestry row = new UrlAncestry();
                        if (rxKeyInUrl.IsMatch(cap.Value))
                        {
                            gen++;
                            row.Generation = gen;
                            row.NodeKey = rxKeyInUrl.Match(cap.Value).Groups["NodeKey"].Value;
                            row.Node = rxKeyInUrl.Match(cap.Value).Groups["Node"].Value;
                        }
                        rows.Add(row);
                    }
                }
            }
        }
        return rows;
    }

    /// <summary>
    /// The resulting row from a ParseUrl function call
    /// </summary>
    /// <param name="row">The url row</param>
    /// <param name="Generation">The depth of the node from the root in which an element was located</param>
    /// <param name="NodeKey">The node element's key id</param>
    /// <param name="Node">The node element name or type</param>
    private static void ParsedUrlRows(Object row, out Int32 Generation, out String NodeKey, out String Node)
    {
        UrlAncestry col = (UrlAncestry)row;
        Generation = (Int32)(col.Generation);
        NodeKey = (String)(col.NodeKey);
        Node = (String)(col.Node);
    }

    /// <summary>
    /// Parses JSON into a tabular format.
    /// </summary>
    /// <param name="json">The JSON to be parsed into a tabular format</param>
    /// <returns>IEnumerable of JSON rows</returns>
    [SqlFunction(FillRowMethodName = "ParsedRows",
    TableDefinition = "ParentID int, ObjectID int, Node nvarchar(500), ItemKey nvarchar(100), ItemValue nvarchar(max), ItemType nvarchar(25)")]
    public static IEnumerable ToJsonTable(String json)
    {        
        /*initialize the collection with the root containing the entire json object*/
        JsonRow root = new JsonRow { ParentID = 1, ObjectID = 1, ItemValue = json };

        var rows = ParseJson(root, 1);
        root = new JsonRow { ParentID = 0, ObjectID = 1, ItemValue = String.Empty, ItemType = "object"};
        rows.Add(root);
        return rows;
    }

    /// <summary>
    /// The strongly typed row result object from a ToJsonTable call
    /// </summary>
    /// <param name="row">The JsonRow containing the parsed values of a JSON element or node</param>
    /// <param name="ParentID">The temporary id number of the parsed elements parent</param>
    /// <param name="ObjectID">The temporary id of the node</param>
    /// <param name="Node">The name of the object in which an element resides. Includes the object itself</param>
    /// <param name="ItemKey">The item key in a key-value pairing</param>
    /// <param name="ItemValue">The item value in a key-value pairing</param>
    /// <param name="ItemType">The item type in a key-value pairing</param>
    /// Serves the same purpose as a WHERE clause template. </param>
    private static void ParsedRows(Object row, out Int32 ParentID, out Int32 ObjectID,
    out String Node, out String ItemKey, out String ItemValue, out String ItemType)
    {
        JsonRow col = (JsonRow)row;
        ParentID = (Int32)(col.ParentID);
        ObjectID = (Int32)(col.ObjectID);
        Node = (String)(col.Node);
        ItemKey = (String)(col.ItemKey);        
        ItemValue = (String)(col.ItemValue);
        ItemType = (String)(col.ItemType);
        switch (ItemType)
        {
            case"array":
                ItemValue = String.Concat("{", "@JArray", ObjectID, "}");
                break;
            case "object":
                ItemValue = String.Concat("{", "@JObject", ObjectID, "}");
                break;
            case "null":
                ItemValue = "null";
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Returns a list of strongly-typed row objects
    /// </summary>
    /// <param name="eroot">A strongly-type row </param>
    /// <param name="newID">The id of the parent node</param>
    /// <returns></returns>
    private static List<JsonRow> ParseJson(JsonRow eroot, Int32 newID)
    {
        // list of rows
        List<JsonRow> rows = new List<JsonRow>();

        // list of nested rows within the row
        List<JsonRow> irows = new List<JsonRow>();

        if (eroot.ItemValue.StartsWith("{"))
        {
            // the element is an object
            eroot.ItemValue = eroot.ItemValue.Substring(1, eroot.ItemValue.Length - 2);
        }
        else if (eroot.ItemValue.StartsWith("["))
        {
            // the element is an array
            eroot.ItemValue = eroot.ItemValue.Substring(1, eroot.ItemValue.Length - 2);
        }
        else
        {
            return rows.ToList();
        }

        foreach (Match m in rxJsonAll.Matches(eroot.ItemValue))
        {
            JsonRow row = new JsonRow
            {
                ParentID = eroot.ParentID,
                ObjectID = 0,
                ItemKey = m.Groups["ItemKey"].Value,
                ItemValue = m.Groups["ItemValue"].Value
            };
            
            if (row.ItemValue.StartsWith("\"") && !row.ItemValue.StartsWith("\"@"))
            {
                /*first, verify the value isn't an empty quoted string*/
                if (row.ItemValue.Equals("\"\""))
                {
                    row.ItemValue = String.Empty;
                }
                else
                {
                    /*remove quotes from the value*/
                    row.ItemValue = row.ItemValue.Substring(1, row.ItemValue.Length - 2);
                }
                row.ItemType = "string";
            }
            /*array*/
            else if (row.ItemValue.StartsWith("[") | row.ItemValue.StartsWith("\"@"))
            {
                /*increment the newID*/
                newID++;
                row.ObjectID = newID;
                row.ItemType = "array";
            }
            /*object*/
            else if (row.ItemValue.StartsWith("{"))
            {
                /*increment the newID*/
                newID++;
                row.ObjectID = newID;
                row.ItemType = "object";
            }
            /*boolean*/
            else if (String.Equals(row.ItemValue, "true", sc) | String.Equals(row.ItemValue, "false", sc))
            {
                row.ItemType = "bool";
            }
            /*floats*/
            else if (Regex.IsMatch(row.ItemValue, "^-{0,1}\\d*\\.[\\d]+$") && !String.Equals(row.ItemType, "string", sc))
            {
                row.ItemType = "float";
            }
            /*int*/
            else if (Regex.IsMatch(row.ItemValue, "^-{0,1}(?:[1-9]+[0-9]*|[0]{1})$") && !String.Equals(row.ItemType, "string", sc))
            {
                row.ItemType = "int";
            }
            /*nulls*/
            else if (String.IsNullOrEmpty(row.ItemValue))
            {
                row.ItemValue = null;
                row.ItemType = "null";
            }
            
            /*add the parsed element to the output collection*/
            rows.Add(row);
        }

        /* double back and handle the array and/or the object rows as the ancestry and hierarchy is established*/
        foreach (JsonRow r in rows)
        {            
            /*update the url*/
            if (r.ItemType == "object" || r.ItemType == "array")
            {
                r.Node = String.Format("{0}.{1}", eroot.Node, r.ItemKey);
            }
            else
            {
                r.Node = String.Format("{0}", eroot.Node);
            }

            // double back and handle the array and/or the object rows
            switch (r.ItemType.CompareString())
            {
                case "array":
                    List<JsonRow> iobj = new List<JsonRow>();
                    if (r.ItemValue.StartsWith("[{") && rxParseArrayOfObjects.IsMatch(r.ItemValue))
                    {
                        Int32 oIndex = 0;
                        foreach (Match o in rxParseArrayOfObjects.Matches(r.ItemValue))
                        {
                            newID++;/*increment the objectID*/
                            /*add the nested parent array object*/
                            JsonRow aroot = new JsonRow();
                            aroot.ParentID = r.ObjectID;
                            aroot.ObjectID = newID;
                            aroot.Node = String.Format("{0}[{1}]", r.Node, oIndex);
                            //aroot.ItemKey = r.ItemKey;
                            aroot.ItemKey = String.Empty;
                            aroot.ItemValue = o.Value;
                            aroot.ItemType = "object";

                            /*add the nested parent array object*/
                            iobj.Add(aroot);
                            oIndex++;
                        }
                        irows.AddRange(iobj);
                        foreach (JsonRow aorow in iobj)
                        {
                            newID++;/*increment the objectID*/
                            JsonRow aoroot = new JsonRow();
                            aoroot.ParentID = aorow.ObjectID;
                            aoroot.ObjectID = newID;
                            aoroot.Node = aorow.Node;
                            //aoroot.ItemKey = aorow.ItemKey;
                            aoroot.ItemKey = String.Empty;
                            aoroot.ItemValue = String.IsNullOrEmpty(aorow.ItemValue) ? "object" : aorow.ItemValue;

                            /*add the nested elements within the nested parent array object*/
                            irows.AddRange(ParseJson(aoroot, newID).Cast<JsonRow>());

                            /*retrieve the last ObjectID from the inner collection*/
                            newID = NewID(rows, irows);
                        }
                    }
                    /*process simple arrays*/
                    else if (r.ItemValue.StartsWith("[") && !r.ItemValue.StartsWith("[{"))
                    {
                        /*verify this isn't an empty array*/
                        if (!r.ItemValue.Equals("[]"))
                        {
                            /*initialize the array element counter*/
                            Int32 ai = 0;
                            List<String> items = new List<String>();
                            /*determine whether a simple string or a numberic array*/
                            String itype = String.Empty;
                            if (rxSimpleStringArray.IsMatch(r.ItemValue))
                            {
                                foreach (Match s in rxSimpleStringArray.Matches(r.ItemValue))
                                {
                                    items.Add(s.Groups["ArrayItem"].Value);
                                }
                                itype = "string";
                            }
                            else if (rxSimpleNumericArray.IsMatch(r.ItemValue))
                            {
                                foreach (Match s in rxSimpleNumericArray.Matches(r.ItemValue))
                                {
                                    items.Add(s.Groups["ArrayItem"].Value);
                                }
                                itype = "float";
                            }
                            foreach (String item in items)
                            {
                                /*add the nested parent array object*/
                                JsonRow sa = new JsonRow();
                                sa.ParentID = r.ObjectID;
                                sa.ObjectID = 0;
                                sa.Node = String.Format("{0}[{1}]", r.Node, ai);
                                sa.ItemKey = String.Empty;
                                sa.ItemValue = item;
                                sa.ItemType = itype;
                                /*add the nested parent array object*/
                                iobj.Add(sa);
                                ai++;/*increment the array item index*/
                            }
                            irows.AddRange(iobj);
                        }
                    }
                    break;
                case "object":
                    newID++;
                    /*initialize the nested elements root values*/
                    JsonRow oroot = new JsonRow();
                    oroot.ParentID = r.ObjectID;
                    oroot.ObjectID = newID;
                    oroot.Node = r.Node;
                    oroot.ItemKey = r.ItemKey;
                    oroot.ItemValue = r.ItemValue;

                    /*add the nested elements to the outer collection*/
                    irows.AddRange(ParseJson(oroot, newID).Cast<JsonRow>());

                    /*retrieve the last ObjectID from the inner collection*/
                    newID = NewID(rows, irows);
                    break;
                default:
                    break;
            }
        }
        rows.AddRange(irows);
        /*retrieve the last ObjectID from the inner collection*/
        newID = NewID(rows, irows);

        return rows;
    }

    /// <summary>
    /// returns the last ObjectID from the inner collection
    /// </summary>
    /// <param name="orows">The collection of outer rows</param>
    /// <param name="irows">The collection of inner rows</param>
    /// <returns>Int32</returns>
    public static Int32 NewID(List<JsonRow> orows, List<JsonRow> irows)
    {
        Int32 outerID = orows.Count > 0 ? orows.Cast<JsonRow>().Max(oid => oid.ObjectID) : 0;
        Int32 nestedID = irows.Count > 0 ? irows.Cast<JsonRow>().Max(oid => oid.ObjectID) : 0;
        return nestedID > outerID ? nestedID : outerID;
    }
        
    /// <summary>
    /// Each Group item represents a named capture group defined in the pattern. 
    /// This function can be used to parse and re-assemble each url into a template used in indexing by replacing each 
    /// Within each group the actual values captured by the pattern are collected.
    /// "Url" is the complete address of an Item in a parsed collection of JSON elements.
    /// "Node" represents each object or array within the hierarchy of the URL.
    /// "NodeKey" represents UUID for each object or array providing ownership and identifiable differences within a collection of the same.
    /// "ItemKey" is the key in the key/value pair of any Json element.
    /// </summary>
    /// <param name="url">JsonUrl for a parsed Json element</param>
    //[SqlFunction()]
    public static String TemplateJsonUrl(String url)
    {
        StringBuilder template = new StringBuilder();
        string[] groups = rxUrl.GetGroupNames();
        foreach (Match m in rxUrl.Matches(url))
        {
            /*step through each object extracted from the url and reassemble it, replacing the UUID's with placeholders*/
            foreach (string s in groups)
            {
                Group g = m.Groups[s];
                CaptureCollection cc = g.Captures;
                foreach (Capture cap in cc)
                {
                    /*If this is a captured Node from the Url.*/
                    if (String.Equals(s, "node", sc))
                    {
                        template.AppendFormat(".{0}[{1}]", cap.Value, "{0}");
                    }
                    /*If this is a captured ItemKey from the Url.*/
                    if (String.Equals(s, "itemkey", sc))
                    {
                        template.AppendFormat(".{0}", cap.Value);
                    }
                }
            }
        }
        return template.ToString();
    }
}
