using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Text.RegularExpressions;

public partial class UserDefinedFunctions
{
    public static String SerializeIndex(IEnumerable index)
    {
        return String.Empty;
    }

    public static IEnumerable MapJsonIndex(String docname, String json, IEnumerable filters)
    {
        /*parse the Document*/
        var documentInfo = ToJsonTable(json).Cast<JsonRow>().Where(d => !String.Equals(d.ItemType, "array"));

        /*map the elements to their index pattern.*/
        var step1 = (from f in filters.Cast<JsonMapSchema>()
                     from di in documentInfo
                     where Regex.IsMatch(di.Url, f.Selector) && String.Equals(f.Reference, docname, sc)
                     select new IndexedRow
                     {
                         _type = docname,
                         Url = di.Url,
                         ItemKey = di.ItemKey,
                         ItemValue = di.ItemValue,
                         ItemType = di.ItemType,
                         Label = di.ItemKey,
                         Selector = f.Selector,
                         IncludedKey = di.IncludedKey
                     }).Cast<IndexedRow>();

        List<IndexedRow> map = new List<IndexedRow>();
        /*bind the values to a stable collection*/
        foreach (IndexedRow m in step1)
        {
            IndexedRow s = m;
            if (String.Equals(m.ItemType, "object", sc) && rxKey.IsMatch(m.ItemValue))
            {
                s.ItemValue = rxKey.Match(m.ItemValue).Groups["ObjectID"].Value;
                s.ItemType = "objectid";
            }
            map.Add(s);
        }
        return map;
    }

    public static IEnumerable MergeJsonIndex(IEnumerable mapped, IEnumerable rindex)
    {
        /*bind the results to a stable collection*/
        List<IndexedRow> map = new List<IndexedRow>();
        map = mapped.Cast<IndexedRow>().ToList();

        /*update each JsonMergeSchema with the source value from the results */
        var source = (from ix in rindex.Cast<JsonMergeSchema>()
                      from mp in map.Cast<IndexedRow>()
                      where Regex.IsMatch(mp.Url, ix.SourceSelector) && String.Equals(ix.SourceReference, mp._type, sc)
                      select new JsonMergeSchema
                      {
                          SourceUrl = mp.Url,
                          SourceReference = ix.SourceReference,
                          SourceSelector = ix.SourceSelector,
                          SourceKey = MergeProperty(mp, ix.SourceKey),
                          SourceValue = MergeProperty(mp, ix.SourceValue),
                          TargetUrl = ix.TargetUrl,
                          TargetReference = ix.TargetReference,
                          TargetSelector = ix.TargetSelector,
                          TargetKey = ix.TargetKey,
                          TargetValue = ix.TargetValue
                      }).Cast<JsonMergeSchema>().ToList();

        /*update the collection*/
        List<IndexedRow> merged = new List<IndexedRow>();
        foreach (JsonMergeSchema s in source)
        {
            foreach (IndexedRow r in mapped)
            {
                if (Regex.IsMatch(r.Url, s.TargetSelector)
                        && String.Equals(r._type, s.TargetReference, sc)
                        && String.Equals(s.SourceKey, MergeProperty(r, s.TargetKey), sc))
                {
                    merged.Add((IndexedRow)UpdateProperty(r, s.TargetValue, s.SourceValue));
                }
            }
        }

        /*Reach back into the map and purge it of disqualifed items*/
        foreach (var mg in merged)
        {
            //map.RemoveAll(m => String.Equals(m.NodeKey, mg.NodeKey, sc) && String.Equals(m.Selector, mg.Selector, sc));
            map.RemoveAll(m => String.Equals(m.Selector, mg.Selector, sc) | String.Equals(m._id, mg._id, sc));
        }
        map = merged.Count() > 0 ? map.Union(merged).ToList() : map;
        return map;
    }

    public static IEnumerable ReduceJsonResult(IEnumerable results, IEnumerable rindex)
    {
        /*bind the results to a stable collection*/
        List<IndexedRow> map = new List<IndexedRow>();
        map = results.Cast<IndexedRow>().ToList();

        /*select all keys matching the filter selector.*/
        List<IndexedRow> matched = (from ix in rindex.Cast<JsonReduceSchema>()
                                    from mp in map.Cast<IndexedRow>()
                                    where Regex.IsMatch(mp.Url, ix.Selector) && String.Equals(ix.Reference, mp._type, sc)
                                    select new IndexedRow
                                    {
                                        _type = mp._type,
                                        _id = mp._id,
                                        Url = mp.Url,
                                        ItemKey = mp.ItemKey,                                        
                                        ItemValue = mp.ItemValue,
                                        ItemType = mp.ItemType,
                                        Label = String.IsNullOrEmpty(ix.Label) ? mp.Label : ix.Label,
                                        Selector = ix.Selector,
                                        Filter = ix.Filter,
                                        operand = ix.Operand,
                                        FilterValue = ix.FilterValue,
                                        IsVisible = ix.IsVisible
                                    }).Cast<IndexedRow>().ToList();

        /*remove unprocessed matches from the map*/
        foreach (IndexedRow r in matched)
        {
            map.RemoveAll(v => String.Equals(v.ItemValue, r.ItemValue, sc) && String.Equals(v.Url, r.Url, sc));
        }
        /*add the processed elements back to the map*/
        map = map.Union(matched).ToList();

        /*select elements with ItemValues matching the FilterValue.*/
        List<IndexedRow> filtered = (from f in map
                                     where String.Equals(f.ItemValue, f.FilterValue, sc)
                                     select f).ToList();

        /*Reach back into the map and retrieve each matched elements siblings to complete the return object*/
        List<IndexedRow> arrayItems = new List<IndexedRow>();
        foreach (var r in filtered)
        {
            var aitems = map.Where(kv => String.Equals(kv._id, r._id, sc)).ToList();
            if (arrayItems.Count() > 0)
                arrayItems.Union(aitems);
            else arrayItems = aitems;
        }

        /*Reach back into the map and purge it of disqualifed items*/
        foreach (var c in arrayItems)
        {
            map.RemoveAll(m => String.Equals(m.Selector, c.Selector, sc));
        }
        map = arrayItems.Count() > 0 ? map.Union(arrayItems).ToList() : map;
        /*remove non-visible elements */
        map.RemoveAll(v => v.IsVisible.Equals(false));

        /*merge the non-conditioned matched elements with the qualified conditioned elements*/
        var reduced = map;
        /*return the serialized value to the caller*/
        return reduced;
    }

    public static JsonIndexSchema ParseJsonIndex(String index)
    {
        JsonIndexSchema s = new JsonIndexSchema();
        String ixval = String.Empty;
        ArrayList include = new ArrayList();
        foreach (Match m in rxJsonIndexDocument.Matches(index))
        {
            switch (m.Groups["IndexKey"].Value)
            {
                case "DocumentName":
                    s.DocumentName = m.Groups["IndexValue"].Value;
                    break;
                case "IndexName":
                    s.IndexName = m.Groups["IndexValue"].Value;
                    break;
                case "MapIndex":
                    ixval = m.Groups["IndexValue"].Value;
                    if (!String.IsNullOrEmpty(ixval))
                    {
                        foreach (Match jmp in rxJsonMapIndex.Matches(ixval))
                        {
                            JsonMapSchema mp = new JsonMapSchema();
                            mp.Url = jmp.Groups["Url"].Value;
                            mp.Reference = jmp.Groups["Reference"].Value;
                            mp.Selector = jmp.Groups["Selector"].Value.Replace("*", "\\*").WithIdentity();
                            s.MapIndex.Add(mp);
                        }
                        /*collect the references required for mapping*/
                        foreach (JsonMapSchema ms in s.MapIndex)
                        {
                            IncludedRow irow = new IncludedRow();
                            irow.nodeKey = "Include";
                            irow._id = Guid.Empty.ToString();
                            irow._type = ms.Reference;
                            include.Add(irow);
                        }
                    }
                    break;
                case "MergeIndex":
                    ixval = m.Groups["IndexValue"].Value;
                    if (!String.IsNullOrEmpty(ixval))
                    {
                        foreach (Match jmi in rxJsonMergeIndex.Matches(ixval))
                        {
                            JsonMergeSchema ms = new JsonMergeSchema();
                            ms.SourceUrl = jmi.Groups["SourceUrl"].Value;
                            ms.SourceReference = jmi.Groups["SourceReference"].Value;
                            ms.SourceSelector = jmi.Groups["SourceSelector"].Value.Replace("*", "\\*").WithIdentity();
                            ms.SourceKey = jmi.Groups["SourceKey"].Value;
                            ms.SourceValue = jmi.Groups["SourceValue"].Value;
                            ms.TargetUrl = jmi.Groups["TargetUrl"].Value;
                            ms.TargetReference = jmi.Groups["TargetReference"].Value;
                            ms.TargetSelector = jmi.Groups["TargetSelector"].Value.Replace("*", "\\*").WithIdentity();
                            ms.TargetKey = jmi.Groups["TargetKey"].Value;
                            ms.TargetValue = jmi.Groups["TargetValue"].Value;
                            s.MergeIndex.Add(ms);
                        }
                        /*collect the references required for merging*/
                        foreach (JsonMergeSchema mgs in s.MergeIndex)
                        {
                            IncludedRow irow = new IncludedRow();
                            irow.nodeKey = "Include";
                            irow._id = Guid.Empty.ToString();
                            irow._type = mgs.SourceReference;
                            include.Add(irow);
                        }
                    }
                    break;
                case "ReduceIndex":
                    ixval = m.Groups["IndexValue"].Value;
                    if (!String.IsNullOrEmpty(ixval))
                    {
                        foreach (Match jri in rxJsonReduceIndex.Matches(ixval))
                        {
                            JsonReduceSchema rs = new JsonReduceSchema();
                            rs.Url = jri.Groups["Url"].Value;
                            rs.Reference = jri.Groups["Reference"].Value;
                            rs.Selector = jri.Groups["Selector"].Value.Replace("*", "\\*").WithIdentity();
                            rs.Filter = jri.Groups["Filter"].Value;
                            rs.Operand = jri.Groups["Operand"].Value;
                            rs.FilterValue = jri.Groups["FilterValue"].Value;
                            rs.IsVisible = String.IsNullOrEmpty(jri.Groups["IsVisible"].Value) ? true : Boolean.Parse(jri.Groups["IsVisible"].Value);
                            rs.Label = jri.Groups["Label"].Value;
                            s.ReduceIndex.Add(rs);
                        }
                        /*collect the references required for merging*/
                        foreach (JsonReduceSchema rs in s.ReduceIndex)
                        {
                            IncludedRow irow = new IncludedRow();
                            irow.nodeKey = "Include";
                            irow._id = Guid.Empty.ToString();
                            irow._type = rs.Reference;
                            include.Add(irow);
                        }
                    }
                    break;
                default:
                    break;
            }
        }
        s.Include.AddRange(include.Cast<IncludedRow>().Distinct().ToList());
        return s;
    }
    public static String MergeProperty(Object obj, String pname)
    {
        String content;
        if (obj.Equals(null) | String.IsNullOrEmpty(pname))
            return String.Empty;
        try
        {
            PropertyInfo pinfo = obj.GetType().GetProperty(pname);

            Object value = pinfo.GetValue(obj, null);
            content = value as string;
        }
        catch (NullReferenceException)
        {
            return "Property Not Found.";
        }
        catch (Exception)
        {
            return String.Empty;
        }
        return content;
    }

    public static Object UpdateProperty(Object obj, String pname, String pvalue)
    {
        if (obj.Equals(null) | String.IsNullOrEmpty(pname))
            return obj;
        try
        {
            PropertyInfo pinfo = obj.GetType().GetProperty(pname);

            // To Set to Property Name
            pinfo.SetValue(obj, pvalue, null);
        }
        catch (Exception)
        {
            return obj;
        }
        return obj;
    }
}
