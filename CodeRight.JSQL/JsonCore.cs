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
    /// returns a collection of url row objects
    /// </summary>
    /// <param name="url">The url to be parsed or split into tabular format</param>
    /// <returns>IEnumerable</returns>
    [SqlFunction(FillRowMethodName = "ParsedUrlRows",
    TableDefinition = "Generation int, Node nvarchar(200)")]
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
    /// <param name="Node">The node element name or type</param>
    private static void ParsedUrlRows(Object row, out Int32 Generation, out String Node)
    {
        UrlAncestry col = (UrlAncestry)row;
        Generation = (Int32)(col.Generation);
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
                if (String.IsNullOrEmpty(r.ItemKey))
                {
                    r.Node = String.Format("{0}[{1}]", eroot.Node, r.ObjectID);   
                }
                else
                {
                    r.Node = String.Format("{0}.{1}", eroot.Node, r.ItemKey);
                }
                
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
