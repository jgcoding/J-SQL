using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Data.SqlClient;
using System.Data;

using Microsoft.SqlServer.Server;

public partial class UserDefinedFunctions
{   
    public static IncludedRow ClrRetrieveDocument(String endpoint, String docid)
    {
        IncludedRow irow = new IncludedRow();

        StringBuilder sql = new StringBuilder();
        sql.AppendFormat("select [DocumentID], [Document], [Node], [PublicDocumentID] FROM [{0}].[dbo].[fRetrieveDocument]('{1}')d ", endpoint, docid);
        //using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["imgRemoteMaster"].ConnectionString))
        using (SqlConnection cn = new SqlConnection("context connection = true"))
        {
            cn.Open();
            SqlCommand command = new SqlCommand(sql.ToString(), cn);
            SqlDataReader dr = command.ExecuteReader();
            while (dr.Read())
            {
                irow.DocumentID = dr[0].ToString();
                irow.Document = dr[1].ToString();
                irow.DocumentName = dr[2].ToString();
            }
        }
        return irow;
    }

    public static IEnumerable ClrJsonIncludes(String endpoint, Guid docID, ArrayList include)
    {
        var document = ClrRetrieveDocument(endpoint, docID.ToString());
        ArrayList documents = new ArrayList();
        documents.AddRange(ProcessInclusions(endpoint, document, include).Cast<IncludedRow>().Where(w => !String.Equals(w.IncludedKey, "include", sc) && !String.Equals(w.Document, "exclude", sc)).ToList());
        documents.Add(document);
        return documents;
    }
    private static void DocumentRows(Object row, out String DocumentID, out String Document, out String DocumentName, out String IncludedKey)
    {
        IncludedRow col = (IncludedRow)row;
        DocumentID = (String)(col.DocumentID);
        Document = (String)(col.Document);
        DocumentName = (String)(col.DocumentName);
        IncludedKey = (String)(col.IncludedKey);
    }

    [SqlFunction(DataAccess = DataAccessKind.Read, SystemDataAccess = SystemDataAccessKind.Read, FillRowMethodName = "JsonIndexRows",
    TableDefinition = "DocumentName nvarchar(100), NodeKey nvarchar(36), Url nvarchar(500), ItemKey nvarchar(100), ItemValue nvarchar(max), ItemType nvarchar(25), Label nvarchar(100),  Selector nvarchar(500), IsVisible bit")]
    public static IEnumerable IndexJson(String endpoint, String docID, String index)
    {
        /*retrieve the index schema*/
        String ixschema = IndexSchemaFetch(endpoint, index, true).Cast<IndexRow>().FirstOrDefault(ix => !String.IsNullOrEmpty(ix.IndexSchema)).IndexSchema;

        if (String.IsNullOrEmpty(ixschema))
            ixschema = index;

        JsonIndexSchema s = new JsonIndexSchema();
        s = ParseJsonIndex(ixschema);

        /*retrieve the document and all inclusions*/

        var documents = ClrJsonIncludes(endpoint, new Guid(docID), s.Include).Cast<IncludedRow>().Where(d => !String.Equals(d.Document, "exclude", sc)).Distinct();

        /*map element selectors within each document in the collection*/
        ArrayList indexed = new ArrayList();
        foreach (var irow in documents)
        {
            var mapped = MapJsonIndex(irow.DocumentName, irow.Document, s.MapIndex).Cast<IndexedRow>();
            indexed.AddRange(mapped.ToList());
        }
        /*merge elements*/
        //var merged = MergeJsonIndex(indexed, s.MergeIndex).Cast<IndexedRow>();

        /*reduce each document with the collection of filters and format the result set*/
        ////var reduced = ReduceJsonResult(merged, s.ReduceIndex).Cast<IndexedRow>();
        //var reduced = ReduceJsonResult(merged, s.ReduceIndex).Cast<IndexedRow>().Distinct();

        /*serialize results*/
        //return reduced;
        return indexed.Cast<IndexedRow>().ToList().Distinct();
    }

    public static ArrayList ProcessInclusions(String endpoint, IncludedRow document, ArrayList documents)
    {
        Boolean inclusions = documents.Cast<IncludedRow>().Any(c => String.Equals(c.IncludedKey, "include", sc));
        Boolean exclusions = documents.Cast<IncludedRow>().Any(c => String.Equals(c.Document, "exclude", sc));
        String root = documents.Cast<IncludedRow>().SingleOrDefault(r => String.IsNullOrEmpty(r.IncludedKey)).DocumentName;
        if (!string.IsNullOrEmpty(document.Document))
        {
            /*collect the references within the document*/
            var matches = SelectIncluded(document.Document).Cast<IncludedRow>().Distinct();

            if (matches.Count() > 0)
            {
                foreach (IncludedRow row in matches)
                {
                    /*verify the document hasn't already been processed and isn't a reference to a load carrier*/
                    if (!documents.Cast<IncludedRow>().Any(c => String.Equals(c.DocumentID, row.DocumentID, sc)))
                    {
                        /*retrieve any inclusions found within the document*/
                        var incdoc = ClrRetrieveDocument(endpoint, row.DocumentID);
                        /*assign the IncludedKey to the output*/
                        incdoc.IncludedKey = row.IncludedKey;
                        /*if inclusions have been defined verify the result document qualifies for inclusion in the result set*/
                        if ((inclusions && !documents.Cast<IncludedRow>().Any(c => String.Equals(c.DocumentName, incdoc.DocumentName, sc) && String.Equals(c.IncludedKey, "include", sc))) |
                            /*if exclusion have been defined verify the result document isn't to be excluded*/
                            (exclusions && documents.Cast<IncludedRow>().Any(c => String.Equals(c.DocumentName, incdoc.DocumentName, sc) && String.Equals(c.Document, "exclude", sc))))
                        {
                            incdoc.Document = "exclude";
                            documents.Add(incdoc);
                            continue;
                        }

                        /*map the Included Key to the object to be retrieved*/
                        incdoc.IncludedKey = row.IncludedKey;
                        /*if no document was returned*/
                        if (String.IsNullOrEmpty(incdoc.Document))
                            incdoc.Document = String.Format("Document Not Found.");
                        /*verify the document isn't already in the collection before adding it*/
                        if (!documents.Cast<IncludedRow>().Any(c => c.DocumentID.Equals(incdoc.DocumentID)))
                            documents.Add(incdoc);
                        /*set the included document as the current document for the recursion*/
                        document = incdoc;
                        /*process inclusions residing in the included document*/
                        documents = ProcessInclusions(endpoint, document, documents);
                    }
                }
            }/*all the inclusions have been processed. return the results to the caller.*/
            else return documents;
        }
        /*no more matches exist. return the results to the caller.*/
        return documents;
    }

    public static IEnumerable IndexSchemaFetch(String endpoint, String index, Boolean useDefault)
    {
        ArrayList row = new ArrayList();
        StringBuilder sql = new StringBuilder();
        sql.AppendFormat("select [IndexID], [IndexName], [DocumentName], [IndexSchema], [IsDefault] FROM [{0}].[dbo].[IndexRegistry] ", endpoint);
        sql.AppendLine();
        sql.AppendFormat("where [IndexName] = '{0}' ", index);
        if (useDefault)
            sql.Append("and [IsDefault] = 'true' ");

        //using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["imgRemoteMaster"].ConnectionString))
        using (SqlConnection cn = new SqlConnection("context connection = true"))
        {
            cn.Open();
            SqlCommand command = new SqlCommand(sql.ToString(), cn);
            SqlDataReader dr = command.ExecuteReader(CommandBehavior.SingleResult | CommandBehavior.CloseConnection);
            while (dr.Read())
            {
                IndexRow irow = new IndexRow();
                irow.IndexSchemaID = dr.GetGuid(0);
                irow.IndexName = dr.GetString(1);
                irow.DocumentName = dr.GetString(2);
                irow.IndexSchema = dr.GetString(3);
                irow.IsDefault = dr.GetBoolean(4);
                row.Add(irow);
            }
        }
        return row;
    }

    private static void JsonIndexRows(Object row, out String DocumentName, out String NodeKey, out String Url, out String ItemKey, out String ItemValue, out String ItemType,
    out String Label, out String Selector, out Boolean IsVisible)
    {
        IndexedRow col = (IndexedRow)row;
        DocumentName = (String)(col.DocumentName);
        NodeKey = (String)(col.NodeKey);
        Url = (String)(col.Url);
        ItemKey = (String)(col.ItemKey);
        ItemValue = (String)(col.ItemValue);
        ItemType = (String)(col.ItemType);
        Label = (String)(col.Label);
        Selector = (String)(col.Selector);
        IsVisible = (Boolean)(col.IsVisible);
    }

    //public static SqlConnection CreateSqlConnection(String cn)
    //{
    //    if (String.IsNullOrEmpty(cn))
    //    {
    //        cn = "imgRemoteMaster";
    //    }

    //    Connector connect = new Connector();
    //    connect.connString = ConfigurationManager.ConnectionStrings[cn].ToString();
    //    SqlConnection connection = Connections.Orcaworks(connect);
    //    return connection;
    //}
}