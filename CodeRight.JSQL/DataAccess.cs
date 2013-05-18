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
    /// <summary>
    /// Helper method used to retrieve a JSON document from the document store and return it in the form of a strongly-typed structure
    /// </summary>
    /// <param name="endpoint">The document store's endpoint location</param>
    /// <param name="_id">The document ID of the document to be retrieved from the document store</param>
    /// <returns>An IncludedRow structure</returns>
    public static IncludedRow ClrRetrieveDocument(String endpoint, String _id)
    {
        IncludedRow irow = new IncludedRow();

        StringBuilder sql = new StringBuilder();
        sql.AppendFormat("select [_id], [document], [_type], [internalId] FROM [{0}].[dbo].[fRetrieveDocument]('{1}')d ", endpoint, _id);
        //using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["imgRemoteMaster"].ConnectionString))
        using (SqlConnection cn = new SqlConnection("context connection = true"))
        {
            cn.Open();
            SqlCommand command = new SqlCommand(sql.ToString(), cn);
            SqlDataReader dr = command.ExecuteReader();
            while (dr.Read())
            {
                irow._id = dr[0].ToString();
                irow.document = dr[1].ToString();
                irow._type = dr[2].ToString();
            }
        }
        return irow;
    }

    /// <summary>
    /// Returns a collection of documents selected and any documents referenced within each selected document. (known as inclusions)
    /// </summary>
    /// <param name="endpoint">The location of the document store</param>
    /// <param name="_id">The id of the document to be selected</param>
    /// <param name="include">a list of documents referenced within documents selected</param>
    /// <returns>IEnumerable</returns>
    public static IEnumerable ClrJsonIncludes(String endpoint, Guid _id, ArrayList include)
    {
        var document = ClrRetrieveDocument(endpoint, _id.ToString());
        ArrayList documents = new ArrayList();
        documents.AddRange(ProcessInclusions(endpoint, document, include).Cast<IncludedRow>().Where(w => !String.Equals(w.nodeKey, "include", sc) && !String.Equals(w.document, "exclude", sc)).ToList());
        documents.Add(document);
        return documents;
    }

    /// <summary>
    /// Container for rows of JSON documents stored in a document store
    /// </summary>
    /// <param name="row">A row object returned by a CLR operation</param>
    /// <param name="_id">The document unique id</param>
    /// <param name="document">The JSON document</param>
    /// <param name="_type">The document name or type</param>
    /// <param name="nodekey">The primary key for each node within the documents returned</param>
    private static void DocumentRows(Object row, out String _id, out String document, out String _type, out String nodekey)
    {
        IncludedRow col = (IncludedRow)row;
        _id = (String)(col._id);
        document = (String)(col.document);
        _type = (String)(col._type);
        nodekey = (String)(col.nodeKey);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="endpoint">The endpoint for the document store and the index store</param>
    /// <param name="_id">The document's unique id</param>
    /// <param name="index"></param>
    /// <returns></returns>
    [SqlFunction(DataAccess = DataAccessKind.Read, SystemDataAccess = SystemDataAccessKind.Read, FillRowMethodName = "JsonIndexRows",
    TableDefinition = "DocumentName nvarchar(100), NodeKey nvarchar(36), Url nvarchar(500), ItemKey nvarchar(100), ItemValue nvarchar(max), ItemType nvarchar(25), Label nvarchar(100),  Selector nvarchar(500), IsVisible bit")]
    public static IEnumerable IndexJson(String endpoint, String _id, String index)
    {
        /*retrieve the index schema*/
        String ixschema = IndexSchemaFetch(endpoint, index, true).Cast<IndexRow>().FirstOrDefault(ix => !String.IsNullOrEmpty(ix.IndexSchema)).IndexSchema;

        if (String.IsNullOrEmpty(ixschema))
            ixschema = index;

        JsonIndexSchema s = new JsonIndexSchema();
        s = ParseJsonIndex(ixschema);

        /*retrieve the document and all inclusions*/

        var documents = ClrJsonIncludes(endpoint, new Guid(_id), s.Include).Cast<IncludedRow>().Where(d => !String.Equals(d.document, "exclude", sc)).Distinct();

        /*map element selectors within each document in the collection*/
        ArrayList indexed = new ArrayList();
        foreach (var irow in documents)
        {
            var mapped = MapJsonIndex(irow._type, irow.document, s.MapIndex).Cast<IndexedRow>();
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

    /// <summary>
    /// extracts references to external documents embedded within a parent document, and retrieves the document to which the reference
    /// refers, replacing the reference with the contents of the document referenced
    /// </summary>
    /// <param name="endpoint">Location of the document store</param>
    /// <param name="document">The JSON document to be processed</param>
    /// <param name="documents">The list of documents referenced in the parent an all children to the parent</param>
    /// <returns></returns>
    public static ArrayList ProcessInclusions(String endpoint, IncludedRow document, ArrayList documents)
    {
        Boolean inclusions = documents.Cast<IncludedRow>().Any(c => String.Equals(c.nodeKey, "include", sc));
        Boolean exclusions = documents.Cast<IncludedRow>().Any(c => String.Equals(c.document, "exclude", sc));
        String root = documents.Cast<IncludedRow>().SingleOrDefault(r => String.IsNullOrEmpty(r.nodeKey))._type;
        if (!string.IsNullOrEmpty(document.document))
        {
            /*collect the references within the document*/
            var matches = SelectIncluded(document.document).Cast<IncludedRow>().Distinct();

            if (matches.Count() > 0)
            {
                foreach (IncludedRow row in matches)
                {
                    /*verify the document hasn't already been processed and isn't a reference to a load carrier*/
                    if (!documents.Cast<IncludedRow>().Any(c => String.Equals(c._id, row._id, sc)))
                    {
                        /*retrieve any inclusions found within the document*/
                        var incdoc = ClrRetrieveDocument(endpoint, row._id);
                        /*assign the IncludedKey to the output*/
                        incdoc.nodeKey = row.nodeKey;
                        /*if inclusions have been defined verify the result document qualifies for inclusion in the result set*/
                        if ((inclusions && !documents.Cast<IncludedRow>().Any(c => String.Equals(c._type, incdoc._type, sc) && String.Equals(c.nodeKey, "include", sc))) |
                            /*if exclusion have been defined verify the result document isn't to be excluded*/
                            (exclusions && documents.Cast<IncludedRow>().Any(c => String.Equals(c._type, incdoc._type, sc) && String.Equals(c.document, "exclude", sc))))
                        {
                            incdoc.document = "exclude";
                            documents.Add(incdoc);
                            continue;
                        }

                        /*map the Included Key to the object to be retrieved*/
                        incdoc.nodeKey = row.nodeKey;
                        /*if no document was returned*/
                        if (String.IsNullOrEmpty(incdoc.document))
                            incdoc.document = String.Format("Document Not Found.");
                        /*verify the document isn't already in the collection before adding it*/
                        if (!documents.Cast<IncludedRow>().Any(c => c._id.Equals(incdoc._id)))
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

    /// <summary>
    /// Returns a collection of index schema as an array of JSON
    /// </summary>
    /// <param name="endpoint">Location of the index store</param>
    /// <param name="index">The name of the index to be retrieved</param>
    /// <param name="useDefault">returns the default index schema if no custom schema is found</param>
    /// <returns>IEnumerable</returns>
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

    /// <summary>
    /// The CLR row structure for a JSON index row
    /// </summary>
    /// <param name="row">The row object returned from a CLR operation</param>
    /// <param name="_type">The document name or type for which the index applies</param>
    /// <param name="_id">The node or document id</param>
    /// <param name="Url">The hierarchical link to the element</param>
    /// <param name="ItemKey">The element name in a key-value pairing</param>
    /// <param name="ItemValue">The value of the element in a key-value pairing</param>
    /// <param name="ItemType">The type of element in the pair</param>
    /// <param name="Label">An optional, alternate/alias for the resultant element</param>
    /// <param name="Selector">The regular expression constructed for selecting the element</param>
    /// <param name="IsVisible">true if the element selected is to be included in the output. false if it is needed internally only</param>
    private static void JsonIndexRows(Object row, out String _type, out String _id, out String Url, out String ItemKey, out String ItemValue, out String ItemType,
    out String Label, out String Selector, out Boolean IsVisible)
    {
        IndexedRow col = (IndexedRow)row;
        _type = (String)(col._type);
        _id = (String)(col._id);
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