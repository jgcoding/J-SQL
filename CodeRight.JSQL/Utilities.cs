using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Globalization;
using Microsoft.SqlServer.Server;

public partial class UserDefinedFunctions
{
    public static String JsonEscape(String unescaped)
    {
        unescaped = Regex.Replace(unescaped, "[\\\\]+", "\\");
        unescaped = Regex.Replace(unescaped, "[\"“”]+", "\\\"");
        unescaped = Regex.Replace(unescaped, "[\\\\]+\"", "\\\"");
        unescaped = Regex.Replace(unescaped, "[\\\\]+/", "/");
        unescaped = Regex.Replace(unescaped, "[’`']+", "''");
        unescaped = Regex.Replace(unescaped, "[…]+", "...");
        unescaped = Regex.Replace(unescaped, ",", ", ");
        unescaped = Regex.Replace(unescaped, "\\s, ", ", ");
        unescaped = Regex.Replace(unescaped, "\\s{2,}", " ");
        unescaped = Regex.Replace(unescaped, "\\.{2,}", ".");
        unescaped = Regex.Replace(unescaped, ",{2,}", ",");
        unescaped = Regex.Replace(unescaped, "\t", "\\t");
        unescaped = Regex.Replace(unescaped, "\n", "\\n");
        unescaped = Regex.Replace(unescaped, "\r", "\\r");
        return unescaped;
    }
    //construct and manage sql server error messages
    public static String SqlErrorMessage(SqlException sqlex)
    {
        StringBuilder sqlerr = new StringBuilder();
        sqlerr.Append("{");
        sqlerr.AppendFormat("\"ErrorNumber\":{0},", sqlex.Number);
        sqlerr.AppendFormat("\"SeverityLevel\":\"{0}\",", sqlex.Class);
        sqlerr.AppendFormat("\"State\":{0},", sqlex.State);
        sqlerr.AppendFormat("\"Procedure\":\"{0}\",", sqlex.Procedure);
        sqlerr.AppendFormat("\"LineNumber\":{0},", sqlex.LineNumber);
        sqlerr.AppendFormat("\"HRESULT\":\"{0}\"", sqlex.ErrorCode);
        sqlerr.Append("}");
        return sqlerr.ToString();
    }

    /// <summary>
    /// Stringifies the column to JSON
    /// </summary>
    /// <param name="key">The column name</param>
    /// <param name="value">The column value</param>
    /// <param name="dt">The column data type</param>
    /// <returns>String</returns>
    [SqlFunction()]
    public static String StringifyColumn(String key, String value, String dt)
    {
        String json = String.Empty;
        switch (dt.ToLower())
        {
            case "datetime":
            case "datetime2":
                json = value.ToString().Trim();
                if (json.Length < 1)
                    json = String.Empty;
                else
                {
                    CultureInfo ci = CultureInfo.InvariantCulture;
                    DateTime rdate = DateTime.Parse(json);
                    //json = string.Format("\"{0}\":\"{1}\"", key, rdate.ToString("MM/dd/yyyy HH:mm:ss.FFF", ci.DateTimeFormat));

                    // format to JavaScript DateTime formatting
                    json = String.Format("\"{0}\":\"/new Date({1})/\"", key, ToUnixTime(rdate));
                }
                break;
            case "uniqueidentifier":
            case "string":
            case "varchar":
            case "nvarchar":
            case "char":
            case "nchar":
            case "date":
            case "smalldatetime":
            case "text":
            case "ntext":
                json = string.Format("\"{0}\":\"{1}\"", key, JsonEscape(value));
                break;
            case "int":
            case "smallint":
            case "tinyint":
            case "decimal":
            case "money":
            case "float":
            case "numeric":
            case "number":
            case "real":
            case "smallmoney":
            case "bigint":
                json = string.Format("\"{0}\":{1}", key, value);
                break;
            case "boolean":
            case "bool":
            case "bit":
                json = string.Format("\"{0}\":{1}", key, String.IsNullOrEmpty(value) | value.Equals(true) ? "true" : "false");
                break;
            case "array":
                json = string.Format("\"{0}\":[{1}]", key, value);
                break;
            case "object":
                json = string.Format("\"{0}\":{{1}}", key, value);
                break;
            case null:
            case "":
                json = String.Empty;
                break;
            default:
                break;
        }
        return json;
    }

    public static String FormatColumnInput(String colName, String sdt)
    {
        String result = string.Empty;
        switch (sdt.ToLower())
        {
            case "uniqueidentifier":
            case "string":
            case "varchar":
            case "nvarchar":
            case "char":
            case "nchar":
            case "date":
            case "datetime":
            case "datetime2":
            case "smalldatetime":
            case "text":
            case "ntext":
                result = string.Format("'{0}'", colName);
                break;
            case "int":
            case "smallint":
            case "tinyint":
            case "decimal":
            case "money":
            case "float":
            case "numeric":
            case "number":
            case "real":
            case "smallmoney":
            case "bigint":
                result = string.Format("{0}", colName);
                break;
            case "boolean":
            case "bool":
            case "bit":
                result = string.Format("'{0}'", String.IsNullOrEmpty(colName) | colName.Equals(true) ? "true" : "false");
                break;
            default:
                break;
        }
        return result;
    }
    public static String FormatColumnForJson(String colName, String sdt, String alias, Boolean nullable)
    {
        String result = String.Empty;
        alias = String.IsNullOrEmpty(alias) ? String.Format("[{0}]", colName) : String.Format("[{0}].[{1}]", alias, colName);
        switch (sdt.ToLower())
        {
            case "ntext":
            case "text":
                result = nullable.Equals(true) ?
                    result = String.Format("ISNULL('\"{0}\":\"'+CONVERT(NVarchar(max),{1})+'\",','\"{0}\":null,')+", colName, alias) :
                    result = String.Format("ISNULL('\"{0}\":\"'+CONVERT(NVarchar(max),{1})+'\",','')+", colName, alias);
                break;
            case "uniqueidentifier":
                result = nullable.Equals(true) ?
                    String.Format("ISNULL('\"{0}\":\"'+CONVERT(VarChar(36),{1})+'\",','\"{0}\":null,')+", colName, alias) :
                    String.Format("ISNULL('\"{0}\":\"'+CONVERT(VarChar(36),{1})+'\",','')+", colName, alias);
                break;
            case "string":
            case "char":
            case "nchar":
            case "nvarchar":
            case "varchar":
                result = nullable.Equals(true) ?
                    String.Format("ISNULL('\"{0}\":\"'+{1}+'\",','\"{0}\":null,')+", colName, alias) :
                    String.Format("ISNULL('\"{0}\":\"'+{1}+'\",','')+", colName, alias);
                break;
            case "date":
            case "datetime":
            case "smalldatetime":
                result = nullable.Equals(true) ?
                    result = String.Format("ISNULL('\"{0}\":\"'+CONVERT(Varchar(25),{1}, 121)+'\",','\"{0}\":null,')+", colName, alias) :
                    result = String.Format("ISNULL('\"{0}\":\"'+CONVERT(Varchar(25),{1}, 121)+'\",','')+", colName, alias);
                break;
            case "bigint":
                result = String.Format("'\"{0}\":\'+CONVERT(Varchar(30),ISNULL({1},0))+','+", colName, alias);
                break;
            case "numeric":
            case "number":
            case "decimal":
            case "float":
            case "int":
            case "money":
            case "real":
            case "smallmoney":
                result = String.Format("'\"{0}\":\'+CONVERT(Varchar(18),ISNULL({1},0))+','+", colName, alias);
                break;
            case "smallint":
                result = String.Format("'\"{0}\":\'+CONVERT(Varchar(7),ISNULL({1},0))+','+", colName, alias);
                break;
            case "tinyint":
                result = String.Format("'\"{0}\":\'+CONVERT(Varchar(5),ISNULL({1},0))+','+", colName, alias);
                break;
            case "boolean":
            case "bool":
            case "bit":
                result = string.Format("'\"{0}\":\'+CASE WHEN {1} = ISNULL({1},0) THEN 'false' ELSE 'true' END+','+", colName, alias);
                break;
            default: result = String.Empty;
                break;
        }
        return result;
    }

    /// <summary>
    /// converts a SQL DateTime value to a unix Int64 value
    /// </summary>
    /// <param name="dt">DateTime value to be converted to Unix format</param>
    /// <returns>Double</returns>
    [SqlFunction()]
    public static Double ToUnixTime(DateTime dt)
    {
        return (Double)(dt - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds;
    }

    [SqlFunction()]
    public static String ToJavaScriptDate(DateTime dt)
    {
        Double ticks = ToUnixTime(dt);
        String result = String.Empty;
        result = String.Format("/Date({0})/", ticks);

        return result;
    }

    /// <summary>
    /// converts unix time to standard datetime
    /// </summary>
    /// <param name="udt">unix date time value</param>
    /// <returns>standard datetime</returns>
    [SqlFunction]
    public static DateTime FromUnixTime(Double udt)
    {
        // Unix timestamp is seconds past epoch
        DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        return dt.AddMilliseconds(udt).ToUniversalTime();
    }

    /// <summary>
    /// converts JavaScript unix Date to .NET datetime
    /// </summary>
    /// <param name="jsunixdate">JavaScript unix Date</param>
    /// <returns>standard datetime</returns>
    [SqlFunction]
    public static DateTime FromJsUnixTime(String jsunixdate)
    {
        //extract the milliseconds from the JavaScript Unix DateTime
        var ms = jsUnixDate.IsMatch(jsunixdate) ? Double.Parse(jsUnixDate.Match(jsunixdate).Groups["value"].Value) : 0;
        
        // Unix timestamp is seconds past epoch
        DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, 0);

        return dt.AddMilliseconds(ms).ToUniversalTime();
    }

    public static String StringifySqlColumn(String colName, String sdt)
    {
        String result = string.Empty;
        switch (sdt.ToLower())
        {
            case "uniqueidentifier":
                result = string.Format("'+COALESCE('\"'+CONVERT(NVarchar(36),ISNULL({0},null))+'\"','null')+'", colName);
                break;
            case "string":
            case "varchar":
            case "nvarchar":
            case "char":
            case "nchar":
                result = string.Format("'+COALESCE('\"'+ISNULL({0},null)+'\"','null')+'", JsonEscape(colName));
                break;
            case "date":
            case "datetime":
            case "datetime2":
            case "smalldatetime":
                result = string.Format("'+COALESCE('\"'+CONVERT(NVarchar(25),ISNULL({0},null), 121)+'\"','null')+'", colName);
                break;
            case "smallint":
                result = string.Format("'+CONVERT(NVarchar(7),ISNULL({0},0))+'", colName);
                break;
            case "tinyint":
                result = string.Format("'+CONVERT(NVarchar(3),ISNULL({0},0))+'", colName);
                break;
            case "boolean":
            case "bool":
            case "bit":
                result = string.Format("'+CASE WHEN {0} IS null THEN 'null' WHEN {0} = 0 THEN 'false' ELSE 'true' END+'", colName);
                break;
            case "int":
            case "decimal":
            case "money":
            case "float":
            case "numeric":
            case "number":
            case "real":
            case "smallmoney":
                result = string.Format("'+CONVERT(NVarchar(15),ISNULL({0},0))+'", colName);
                break;
            case "bigint":
                result = string.Format("'+CONVERT(NVarchar(36),ISNULL({0},0))+'", colName);
                break;
            case "text":
            case "ntext":
                result = string.Format("'+COALESCE('\"'+CONVERT(NVarchar(max),ISNULL({0},null))+'\"','null')+'", JsonEscape(colName));
                break;
            default:
                break;
        }
        return result;
    }
}
