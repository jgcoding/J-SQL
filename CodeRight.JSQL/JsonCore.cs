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
    /// The strongly typed row result object from a ToJsonTable call
    /// </summary>
    /// <param name="row">The JsonRow containing the parsed values of a JSON element or node</param>
    /// <param name="ParentID">The temporary id number of the parsed elements parent</param>
    /// <param name="ObjectID">The temporary id of the node</param>
    /// <param name="Node">The name of the object in which an element resides. Includes the object itself</param>
    /// <param name="itemKey">The item key in a key-value pairing</param>
    /// <param name="itemValue">The item value in a key-value pairing</param>
    /// <param name="itemType">The item type in a key-value pairing</param>
    /// Serves the same purpose as a WHERE clause template. </param>
    private static void ParsedRows(Object row, out Int32 ParentID, out Int32 ObjectID,
    out String Node, out String itemKey, out String itemValue, out String itemType)
    {
        JsonRow col = (JsonRow)row;
        ParentID = (Int32)(col.ParentID);
        ObjectID = (Int32)(col.ObjectID);
        Node = (String)(col.Node);
        itemKey = (String)(col.itemKey);
        itemValue = (String)(col.itemValue);
        itemType = (String)(col.itemType);
    }

    /// <summary>
    /// Parses JSON into a tabular format.
    /// </summary>
    /// <param name="json">The JSON to be parsed into a tabular format</param>
    /// <returns>IEnumerable of JSON rows</returns>
    [SqlFunction(FillRowMethodName = "ParsedRows",
    TableDefinition = "ParentID int, ObjectID int, Node nvarchar(500), itemKey nvarchar(100), itemValue nvarchar(max), itemType nvarchar(25)")]
    public static IEnumerable ToJsonTable(String json)
    {
        /*clear white spaces, tab, returns, and line-feeds from the beginning of the string.
          Initialize the collection with the root containing the entire json object. */
        var root = new JsonRow { 
            ParentID = 1
            , ObjectID = 1
            , itemKey = String.Empty
            , itemValue = json.TrimStart(' ', '\t', '\r', '\n')
            , itemType = json.StartsWith("[") ? "array" : "object" 
        };

        //start parsing the string into a collection
        var rows = ParseJson(root, root.ObjectID);

        //reset the root parentID to zero. TODO: make this un-necessary
        root.ParentID = 0;

        rows.Add(root);
        return rows;
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

        var evalue = eroot.itemValue;
        if (evalue.StartsWith("{"))
        {
            // the element is an object
            evalue = evalue.Substring(1, evalue.Length - 2);
        }
        else if (evalue.StartsWith("["))
        {
            // the element is an array
            evalue = evalue.Substring(1, evalue.Length - 2);
        }
        else
        {
            return rows.ToList();
        }

        foreach (Match m in rxJsonAll.Matches(evalue))
        {
            JsonRow row = new JsonRow
            {
                ParentID = eroot.ParentID,
                ObjectID = 0,
                itemKey = m.Groups["itemKey"].Value,
                itemValue = m.Groups["itemValue"].Value
            };
            
            if (row.itemValue.StartsWith("\"") && !row.itemValue.StartsWith("\"@"))
            {
                /*first, verify the value isn't an empty quoted string*/
                if (row.itemValue.Equals("\"\""))
                {
                    row.itemValue = String.Empty;
                }
                else
                {
                    /*remove quotes from the value*/
                    row.itemValue = row.itemValue.Substring(1, row.itemValue.Length - 2);
                }
                row.itemType = "string";
            }
            /*array*/
            else if (row.itemValue.StartsWith("[") | row.itemValue.StartsWith("\"@"))
            {
                /*increment the newID*/
                newID++;
                row.ObjectID = newID;
                row.itemType = "array";
            }
            /*object*/
            else if (row.itemValue.StartsWith("{"))
            {
                /*increment the newID*/
                newID++;
                row.ObjectID = newID;
                row.itemType = "object";
            }
            /*boolean*/
            else if (String.Equals(row.itemValue, "true", sc) | String.Equals(row.itemValue, "false", sc))
            {
                row.itemType = "bool";
            }
            /*floats*/
            else if (Regex.IsMatch(row.itemValue, "^-{0,1}\\d*\\.[\\d]+$") && !String.Equals(row.itemType, "string", sc))
            {
                row.itemType = "float";
            }
            /*int*/
            else if (Regex.IsMatch(row.itemValue, "^-{0,1}(?:[1-9]+[0-9]*|[0]{1})$") && !String.Equals(row.itemType, "string", sc))
            {
                row.itemType = "int";
            }
            /*nulls*/
            else if (String.IsNullOrEmpty(row.itemValue))
            {
                row.itemValue = "null";
                row.itemType = "null";
            }
            
            /*add the parsed element to the output collection*/
            rows.Add(row);
        }

        /* double back and handle the array and/or the object rows as the ancestry and hierarchy is established*/
        foreach (JsonRow r in rows)
        {            
            /*update the url*/
            if (r.itemType == "object" || r.itemType == "array")
            {
                if (String.IsNullOrEmpty(r.itemKey))
                {
                    r.Node = String.Format("{0}[{1}]", eroot.Node, r.ObjectID);   
                }
                else
                {
                    r.Node = String.Format("{0}.{1}", eroot.Node, r.itemKey);
                }
                
            }
            else
            {
                r.Node = String.Format("{0}", eroot.Node);
            }

            // double back and handle the array and/or the object rows
            switch (r.itemType.CompareString())
            {
                case "array":
                    List<JsonRow> iobj = new List<JsonRow>();
                    if (r.itemValue.StartsWith("[") && rxParseArrayOfObjects.IsMatch(r.itemValue))
                    {
                        Int32 oIndex = 0;
                        foreach (Match o in rxParseArrayOfObjects.Matches(r.itemValue))
                        {
                            newID++;/*increment the objectID*/
                            /*add the nested parent array object*/
                            JsonRow aroot = new JsonRow
                            {
                                ParentID = r.ObjectID,
                                ObjectID = newID,
                                Node = String.Format("{0}[{1}]", r.Node, oIndex),
                                itemKey = String.Empty,
                                itemValue = o.Value,
                                itemType = "object"
                            };
                            /*add the nested parent array object*/
                            iobj.Add(aroot);
                            oIndex++;
                        }
                        irows.AddRange(iobj);
                        foreach (JsonRow aorow in iobj)
                        {
                            newID++;/*increment the objectID*/
                            JsonRow aoroot = new JsonRow
                            {
                                ParentID = aorow.ObjectID,
                                ObjectID = newID,
                                Node = aorow.Node,
                                itemKey = String.Empty,
                                itemValue = String.IsNullOrEmpty(aorow.itemValue) ? "object" : aorow.itemValue
                            };
                            /*add the nested elements within the nested parent array object*/
                            irows.AddRange(ParseJson(aoroot, newID).Cast<JsonRow>());

                            /*retrieve the last ObjectID from the inner collection*/
                            newID = NewID(rows, irows);
                        }
                    }
                    /*process simple arrays*/
                    else if (r.itemValue.StartsWith("[") && !rxParseArrayOfObjects.IsMatch(r.itemValue))
                    {
                        /*verify this isn't an empty array*/
                        if (!r.itemValue.Equals("[]"))
                        {
                            /*initialize the array element counter*/
                            Int32 ai = 0;
                            List<String> items = new List<String>();
                            /*determine whether a simple string or a numberic array*/
                            String itype = String.Empty;
                            if (rxSimpleStringArray.IsMatch(r.itemValue))
                            {
                                foreach (Match s in rxSimpleStringArray.Matches(r.itemValue))
                                {
                                    items.Add(s.Groups["ArrayItem"].Value);
                                }
                                itype = "string";
                            }
                            else if (rxSimpleNumericArray.IsMatch(r.itemValue))
                            {
                                foreach (Match s in rxSimpleNumericArray.Matches(r.itemValue))
                                {
                                    items.Add(s.Groups["ArrayItem"].Value);
                                }
                                itype = "float";
                            }
                            foreach (String item in items)
                            {
                                /*add the nested parent array object*/
                                JsonRow sa = new JsonRow
                                {
                                    ParentID = r.ObjectID,
                                    ObjectID = 0,
                                    Node = String.Format("{0}[{1}]", r.Node, ai),
                                    itemKey = String.Empty,
                                    itemValue = item,
                                    itemType = itype
                                };
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
                    JsonRow oroot = new JsonRow
                    {
                        ParentID = r.ObjectID,
                        ObjectID = newID,
                        Node = r.Node,
                        itemKey = r.itemKey,
                        itemValue = r.itemValue
                    };
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
    /// An experimental, work-in-progress effort to return a list of strongly-typed row objects more efficiently than
    /// the original ParseJson
    /// </summary>
    /// <param name="eroot">A strongly-type row </param>
    /// <param name="newID">The id of the parent node</param>
    /// <returns></returns>
    private static List<JsonRow> ParseJson_experimental(JsonRow eroot, Int32 newID)
    {
        // list of rows
        List<JsonRow> rows = new List<JsonRow>();

        // list of nested rows within the row
        List<JsonRow> irows = new List<JsonRow>();

        var evalue = eroot.itemValue;
        if (evalue.StartsWith("{"))
        {
            // the element is an object
            evalue = evalue.Substring(1, evalue.Length - 2);
        }
        else if (evalue.StartsWith("["))
        {
            // the element is an array
            evalue = evalue.Substring(1, evalue.Length - 2);
        }
        else
        {
            return rows.ToList();
        }

        foreach (Match m in rxJsonAll.Matches(evalue))
        {
            JsonRow row = new JsonRow
            {
                ParentID = eroot.ParentID,
                ObjectID = 0,
                itemKey = m.Groups["itemKey"].Value,
                itemValue = m.Groups["itemValue"].Value
            };

            if (row.itemValue.StartsWith("\"") && !row.itemValue.StartsWith("\"@"))
            {
                /*first, verify the value isn't an empty quoted string*/
                if (row.itemValue.Equals("\"\""))
                {
                    row.itemValue = String.Empty;
                }
                else
                {
                    /*remove quotes from the value*/
                    row.itemValue = row.itemValue.Substring(1, row.itemValue.Length - 2);
                }
                row.itemType = "string";
            }
            /*array*/
            else if (row.itemValue.StartsWith("[") | row.itemValue.StartsWith("\"@"))
            {
                /*increment the newID*/
                newID++;
                row.ObjectID = newID;
                row.itemType = "array";
            }
            /*object*/
            else if (row.itemValue.StartsWith("{"))
            {
                /*increment the newID*/
                newID++;
                row.ObjectID = newID;
                row.itemType = "object";
            }
            /*boolean*/
            else if (String.Equals(row.itemValue, "true", sc) | String.Equals(row.itemValue, "false", sc))
            {
                row.itemType = "bool";
            }
            /*floats*/
            else if (Regex.IsMatch(row.itemValue, "^-{0,1}\\d*\\.[\\d]+$") && !String.Equals(row.itemType, "string", sc))
            {
                row.itemType = "float";
            }
            /*int*/
            else if (Regex.IsMatch(row.itemValue, "^-{0,1}(?:[1-9]+[0-9]*|[0]{1})$") && !String.Equals(row.itemType, "string", sc))
            {
                row.itemType = "int";
            }
            /*nulls*/
            else if (String.IsNullOrEmpty(row.itemValue))
            {
                row.itemValue = "null";
                row.itemType = "null";
            }

            /*add the parsed element to the output collection*/
            rows.Add(row);
        }

        /* double back and handle the array and/or the object rows as the ancestry and hierarchy is established*/
        foreach (JsonRow r in rows)
        {
            /*update the url*/
            if (r.itemType == "object" || r.itemType == "array")
            {
                if (String.IsNullOrEmpty(r.itemKey))
                {
                    r.Node = String.Format("{0}[{1}]", eroot.Node, r.ObjectID);
                }
                else
                {
                    r.Node = String.Format("{0}.{1}", eroot.Node, r.itemKey);
                }

            }
            else
            {
                r.Node = String.Format("{0}", eroot.Node);
            }

            // double back and handle the array and/or the object rows
            switch (r.itemType.CompareString())
            {
                case "array":
                    List<JsonRow> iobj = new List<JsonRow>();
                    if (r.itemValue.StartsWith("[") && rxParseArrayOfObjects.IsMatch(r.itemValue))
                    {
                        Int32 oIndex = 0;
                        foreach (Match o in rxParseArrayOfObjects.Matches(r.itemValue))
                        {
                            newID++;/*increment the objectID*/
                            /*add the nested parent array object*/
                            JsonRow aroot = new JsonRow
                            {
                                ParentID = r.ObjectID,
                                ObjectID = newID,
                                Node = String.Format("{0}[{1}]", r.Node, oIndex),
                                itemKey = String.Empty,
                                itemValue = o.Value,
                                itemType = "object"
                            };
                            /*add the nested parent array object*/
                            iobj.Add(aroot);
                            oIndex++;
                        }
                        irows.AddRange(iobj);
                        foreach (JsonRow aorow in iobj)
                        {
                            newID++;/*increment the objectID*/
                            JsonRow aoroot = new JsonRow
                            {
                                ParentID = aorow.ObjectID,
                                ObjectID = newID,
                                Node = aorow.Node,
                                itemKey = String.Empty,
                                itemValue = String.IsNullOrEmpty(aorow.itemValue) ? "object" : aorow.itemValue
                            };
                            /*add the nested elements within the nested parent array object*/
                            irows.AddRange(ParseJson(aoroot, newID).Cast<JsonRow>());

                            /*retrieve the last ObjectID from the inner collection*/
                            newID = NewID(rows, irows);
                        }
                    }
                    /*process simple arrays*/
                    else if (r.itemValue.StartsWith("[") && !rxParseArrayOfObjects.IsMatch(r.itemValue))
                    {
                        /*verify this isn't an empty array*/
                        if (!r.itemValue.Equals("[]"))
                        {
                            /*initialize the array element counter*/
                            Int32 ai = 0;
                            List<String> items = new List<String>();
                            /*determine whether a simple string or a numberic array*/
                            String itype = String.Empty;
                            if (rxSimpleStringArray.IsMatch(r.itemValue))
                            {
                                foreach (Match s in rxSimpleStringArray.Matches(r.itemValue))
                                {
                                    items.Add(s.Groups["ArrayItem"].Value);
                                }
                                itype = "string";
                            }
                            else if (rxSimpleNumericArray.IsMatch(r.itemValue))
                            {
                                foreach (Match s in rxSimpleNumericArray.Matches(r.itemValue))
                                {
                                    items.Add(s.Groups["ArrayItem"].Value);
                                }
                                itype = "float";
                            }
                            foreach (String item in items)
                            {
                                /*add the nested parent array object*/
                                JsonRow sa = new JsonRow
                                {
                                    ParentID = r.ObjectID,
                                    ObjectID = 0,
                                    Node = String.Format("{0}[{1}]", r.Node, ai),
                                    itemKey = String.Empty,
                                    itemValue = item,
                                    itemType = itype
                                };
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
                    JsonRow oroot = new JsonRow
                    {
                        ParentID = r.ObjectID,
                        ObjectID = newID,
                        Node = r.Node,
                        itemKey = r.itemKey,
                        itemValue = r.itemValue
                    };
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
    /// "itemKey" is the key in the key/value pair of any Json element.
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
                    /*If this is a captured itemKey from the Url.*/
                    if (String.Equals(s, "itemKey", sc))
                    {
                        template.AppendFormat(".{0}", cap.Value);
                    }
                }
            }
        }
        return template.ToString();
    }
}
