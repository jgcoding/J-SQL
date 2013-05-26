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
    /// Container for rows of JSON documents stored in a document store
    /// </summary>
    /// <param name="row">A row object returned by a CLR operation</param>
    /// <param name="_id">The document unique id</param>
    /// <param name="document">The JSON document</param>
    /// <param name="_type">The document name or type</param>
    /// <param name="nodekey">The primary key for each node within the documents returned</param>
    private static void DocumentRows(Object row, out String _id, out String document, out String _type/*, out String nodekey*/)
    {
        IncludedRow col = (IncludedRow)row;
        _id = (String)(col._id);
        document = (String)(col.document);
        _type = (String)(col._type);
        //nodekey = (String)(col.nodeKey);
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