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
    [SqlFunction(FillRowMethodName = "IncludedRows",
    TableDefinition = "IncludedKey nvarchar(100), DocumentID uniqueidentifier")]
    public static IEnumerable SelectIncluded(String input)
    {
        ArrayList rows = new ArrayList();
        ParseInclusions(input, rows);
        return rows;
    }

    private static void IncludedRows(Object row, out String IncludedKey, out Guid DocumentID)
    {
        IncludedRow col = (IncludedRow)row;
        IncludedKey = col.IncludedKey.ToString();
        DocumentID = new Guid(col.DocumentID);

    }

    private static void ParseInclusions(String input, ArrayList rows)
    {
        foreach (Match m in rxIncludedKey.Matches(input))
        {
            IncludedRow column = new IncludedRow
            {
                IncludedKey = m.Groups["IncludedKey"].Value,
                DocumentID = m.Groups["DocumentID"].Value
            };
            rows.Add(column);
        }
    }
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
    private static String BuildSearchCriteria(String json)
    {
        /*parse the search criteria*/
        var criteria = RxJsonParse(json).Cast<JsonRow>().Where(w => !String.IsNullOrEmpty(w.ItemValue)).ToList();

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

    private static void MatchedViews(Object obj, out String DocumentID, out String DocumentView)
    {
        Object[] column = (Object[])obj;
        DocumentID = (String)column[0];
        DocumentView = (String)column[1];
    }

    [SqlFunction(DataAccess = DataAccessKind.Read, SystemDataAccess = SystemDataAccessKind.Read, FillRowMethodName = "MatchedViews",
        TableDefinition = "DocumentID nvarchar(36), DocumentView nvarchar(max)")]
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
    
    private static void ItemUpdateView(Object obj, out String ItemKey, out String Value_O, out String Value_U)
    {
        Object[] column = (Object[])obj;
        ItemKey = (String)column[0];
        Value_O = String.IsNullOrEmpty((String)column[1]) ? "null" : (String)column[1];
        Value_U = String.IsNullOrEmpty((String)column[2]) ? "null" : (String)column[2];
    }

    [SqlFunction()]
    public static Boolean rxPivot(String json, String key, String value)
    {

        return Regex.IsMatch(json, value);
    }

    [SqlFunction()]
    public static Boolean rxContains(String json, String value)
    {
        return Regex.IsMatch(json, value);
    }

    [SqlFunction()]
    public static String SelectKey(String json)
    {
        return rxKey.Match(json).Groups["ObjectID"].Value;
    }

    private static void EntityUpdateView(Object obj, out String EntityType, out String EntityName, out String Action)
    {
        Object[] column = (Object[])obj;
        EntityType = (String)column[0];
        EntityName = (String)column[1];
        Action = (String)column[2];
    }

    [SqlFunction(FillRowMethodName = "MergedUrls",
        TableDefinition = "Url nvarchar(500), Selector nvarchar(500)")]
    public static IEnumerable MergeUrl(String sourceUrl, String sourceKey, String targetUrl)
    {
        ArrayList rows = new ArrayList();
        Object[] column = new Object[2];
        column[0] = String.Format("{0}:{1}{2}", sourceUrl.LastIndexOf("/").Equals(sourceUrl.Length - 1) ? sourceUrl.Remove(sourceUrl.Length - 1, 1) : sourceUrl, sourceKey, targetUrl);
        column[1] = TemplateJsonUrl(column[0].ToString());
        rows.Add(column);
        return rows;
    }

    private static void MergedUrls(Object obj, out String Url, out String Selector)
    {
        Object[] column = (Object[])obj;
        Url = (String)column[0];
        Selector = (String)column[1];
    }

    [SqlFunction(FillRowMethodName = "ParsedUrlRows",
    TableDefinition = "Generation int, NodeKey nvarchar(36), Node nvarchar(100)")]

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
    private static void ParsedUrlRows(Object row, out Int32 Generation, out String NodeKey, out String Node)
    {
        UrlAncestry col = (UrlAncestry)row;
        Generation = (Int32)(col.Generation);
        NodeKey = (String)(col.NodeKey);
        Node = (String)(col.Node);
    }

    [SqlFunction(FillRowMethodName = "ParsedRows",
    TableDefinition = "ParentID int, ObjectID int, Url nvarchar(500), NodeKey nvarchar(50), Node nvarchar(100), ItemKey nvarchar(100), ItemValue nvarchar(max), ItemType nvarchar(25), Selector nvarchar(500)")]
    public static IEnumerable RxJsonParse(String json)
    {
        /*initialize the collection with the root containing the entire json object*/
        JsonRow root = new JsonRow { ParentID = 1, ObjectID = 1, NodeKey = Guid.Empty.ToString(), Node = "root", ItemValue = json };

        var rows = ParseJson(root, 1);
        root = new JsonRow { ParentID = 0, ObjectID = 1, NodeKey = Guid.Empty.ToString(), Node = "root", ItemValue = String.Empty, ItemType = "object" };
        rows.Add(root);
        return rows;
    }
    private static void ParsedRows(Object row, out Int32 ParentID, out Int32 ObjectID,
    out String Url, out String NodeKey, out String Node, out String ItemKey, out String ItemValue, out String ItemType, out String Selector)
    {
        JsonRow col = (JsonRow)row;
        ParentID = (Int32)(col.ParentID);
        ObjectID = (Int32)(col.ObjectID);
        Url = (String)(col.Url);
        NodeKey = (String)(col.NodeKey);
        Node = (String)(col.Node);
        ItemKey = (String)(col.ItemKey);
        ItemValue = (String)(col.ItemValue);
        ItemType = (String)(col.ItemType);
        Selector = (String)(col.Selector);
    }

    private static List<JsonRow> ParseJson(JsonRow eroot, Int32 newID)
    {
        // list of rows
        List<JsonRow> rows = new List<JsonRow>();

        // list of nested rows within the rows
        List<JsonRow> irows = new List<JsonRow>();

        if (eroot.ItemValue.StartsWith("{"))
        {
            // this is an object
            eroot.ItemValue = eroot.ItemValue.Substring(1, eroot.ItemValue.Length - 2);
        }
        else if (eroot.ItemValue.StartsWith("["))
        {
            // this is an array
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
                NodeKey = eroot.NodeKey,
                Node = eroot.Node,
                ItemKey = m.Groups["ItemKey"].Value,
                ItemValue = m.Groups["ItemValue"].Value
            };
            
            if (row.ItemValue.StartsWith("\"") && !row.ItemValue.StartsWith("\"@"))
            {
                /*first, verify the value isn't an empty quoted string*/
                if (row.ItemValue.Equals("\"\""))
                    row.ItemValue = String.Empty;
                else
                    /*remove quotes from the value*/
                    row.ItemValue = row.ItemValue.Substring(1, row.ItemValue.Length - 2);

                /*determine if the element is keyed, updating the object key if it is*/
                if (rxKey.IsMatch(m.Value))
                {
                    eroot.NodeKey = rxKey.Match(m.Value).Groups["ObjectID"].Value;
                }
                row.ItemType = "string";
            }
            else if (row.ItemValue.StartsWith("[") | row.ItemValue.StartsWith("\"@"))
            {
                /*increment the newID*/
                newID++;
                row.ObjectID = newID;
                row.ItemType = "array";
            }
            else if (row.ItemValue.StartsWith("{"))
            {
                /*increment the newID*/
                newID++;
                row.ObjectID = newID;
                row.ItemType = "object";
            }
            /*boolean*/
            else if (String.Equals(row.ItemValue, "true", sc) | String.Equals(row.ItemValue, "false", sc))
                row.ItemType = "bool";
            /*floats*/
            else if (Regex.IsMatch(row.ItemValue, "^-{0,1}\\d*\\.[\\d]+$") && !String.Equals(row.ItemType, "string", sc))
                row.ItemType = "float";
            /*int*/
            else if (Regex.IsMatch(row.ItemValue, "^-{0,1}(?:[1-9]+[0-9]*|[0]{1})$") && !String.Equals(row.ItemType, "string", sc))
                row.ItemType = "int";
            /*nulls*/
            else if (row.ItemValue == null)
                row.ItemType = "null";

            /*add the parsed element to the output collection*/
            rows.Add(row);
        }

        /*update each element in the object with object's key and url*/
        foreach (JsonRow r in rows)
        {
            /*update the key*/
            r.NodeKey = eroot.NodeKey;

            /*update the url*/
            if (String.IsNullOrEmpty(eroot.Url) | eroot.Url.Equals("/"))
                r.Url = String.Format("/{0}", r.ItemKey);
            else
                r.Url = r.NodeKey.CompareString().Equals(Guid.Empty.ToString()) ? String.Format("{0}/{1}", eroot.Url, r.ItemKey) : String.Format("{0}:{1}/{2}", eroot.Url, r.NodeKey, r.ItemKey);

            switch (r.ItemType.CompareString())
            {
                case "array":
                    List<JsonRow> iobj = new List<JsonRow>();
                    if (r.ItemValue.StartsWith("[{") && rxParseArrayOfObjects.IsMatch(r.ItemValue))
                    {
                        foreach (Match o in rxParseArrayOfObjects.Matches(r.ItemValue))
                        {
                            newID++;/*increment the objectID*/
                            /*add the nested parent array object*/
                            JsonRow aroot = new JsonRow();
                            aroot.ParentID = r.ObjectID;
                            aroot.ObjectID = newID;
                            aroot.NodeKey = r.NodeKey;
                            aroot.Url = r.Url;
                            aroot.Node = r.ItemKey;
                            aroot.ItemKey = r.ItemKey;
                            aroot.ItemValue = o.Value;
                            aroot.ItemType = "object";

                            /*add the nested parent array object*/
                            iobj.Add(aroot);
                        }
                        irows.AddRange(iobj);
                        foreach (JsonRow aorow in iobj)
                        {
                            newID++;/*increment the objectID*/
                            JsonRow aoroot = new JsonRow();
                            aoroot.ParentID = aorow.ObjectID;
                            aoroot.ObjectID = newID;
                            aoroot.NodeKey = Guid.Empty.ToString();
                            aoroot.Url = aorow.Url;
                            aoroot.Node = aorow.ItemKey;
                            aoroot.ItemKey = aorow.ItemKey;
                            aoroot.ItemValue = String.IsNullOrEmpty(aorow.ItemValue) ? "object" : aorow.ItemValue;

                            /*add the nested elements within the nested parent array object*/
                            irows.AddRange(ParseJson(aoroot, newID).Cast<JsonRow>());

                            /*retrieve the last ObjectID from the inner collection*/
                            newID = NewID(rows, irows);
                        }
                    }
                    else if (r.ItemValue.StartsWith("[") && !r.ItemValue.StartsWith("[{"))
                    {
                        /*verify this isn't an empty array*/
                        if (!r.ItemValue.Equals("[]"))
                        {
                            /*remove the outer array braces*/
                            //String simpleArray = r.ItemValue.Substring(1, r.ItemValue.Length - 2);
                            /*initialize the array element counter*/
                            Int32 ai = 0;
                            List<String> items = new List<String>();
                            /*determine whether a simple string or a numberic array*/
                            String itype = String.Empty;
                            if (rxSimpleStringArray.IsMatch(r.ItemValue))
                            {
                                foreach (Match s in rxSimpleStringArray.Matches(r.ItemValue))
                                    items.Add(s.Groups["ArrayItem"].Value);
                                itype = "string";
                            }
                            else if (rxSimpleNumericArray.IsMatch(r.ItemValue))
                            {
                                foreach (Match s in rxSimpleNumericArray.Matches(r.ItemValue))
                                    items.Add(s.Groups["ArrayItem"].Value);
                                itype = "float";
                            }
                            foreach (String item in items)
                            {
                                //newID++;/*increment the objectID*/
                                ai++;/*increment the array item index*/
                                /*add the nested parent array object*/
                                JsonRow sa = new JsonRow();
                                sa.ParentID = r.ObjectID;
                                sa.ObjectID = 0;
                                sa.NodeKey = r.NodeKey;
                                sa.Url = String.Concat(r.Url, "/", ai);
                                sa.Node = r.ItemKey;
                                sa.ItemKey = null;
                                sa.ItemValue = item;
                                sa.ItemType = itype;

                                /*add the nested parent array object*/
                                iobj.Add(sa);
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
                    oroot.NodeKey = r.NodeKey;
                    oroot.Url = r.Url;
                    oroot.Node = r.ItemKey;
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
        newID = NewID(rows, irows);

        return rows;
    }

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
                        template.AppendFormat("/{0}:{1}", cap.Value, "{0}");
                    /*If this is a captured ItemKey from the Url.*/
                    if (String.Equals(s, "itemkey", sc))
                        template.AppendFormat("/{0}", cap.Value);
                }
            }
        }
        return template.ToString();
    }

}
