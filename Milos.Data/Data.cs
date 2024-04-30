using System.Threading.Tasks;

namespace Milos.Data;

public static class Data
{
    public static string DataConfigurationPrefix { get; set; } = "database";

    private static readonly Dictionary<string, IDataService> KnownServices = [];

    public static IDataService DataService
    {
        get
        {
            if (KnownServices.ContainsKey(DataConfigurationPrefix) && !KnownServices[DataConfigurationPrefix].IsValid())
            {
                KnownServices[DataConfigurationPrefix]?.Dispose();
                KnownServices.Remove(DataConfigurationPrefix);
            }

            if (!KnownServices.ContainsKey(DataConfigurationPrefix))
            {
                var newService = DataServiceFactory.GetDataService(DataConfigurationPrefix);
                if (newService.IsValid())
                    KnownServices.Add(DataConfigurationPrefix, newService);
            }

            if (KnownServices.ContainsKey(DataConfigurationPrefix))
                return KnownServices[DataConfigurationPrefix];

            return null;
        }
    }

    /// <summary>
    /// Queries all records from the specified table
    /// </summary>
    /// <param name="tableName">Name of the table to query from</param>
    /// <param name="fieldList">Fields to query (or * for all fields)</param>
    /// <param name="orderBy">Order (or empty string if no special order is desired)</param>
    /// <param name="selectMethod">Select method (such as stored procedure or select commands)</param>
    /// <returns>DataSet with the requested data</returns>
    public static DataSet GetAllRecords(string tableName, string fieldList = "*", string orderBy = "", DataRowProcessMethod selectMethod = DataRowProcessMethod.Default) => DataService?.GetAllRecords(tableName, fieldList, orderBy, selectMethod);

    /// <summary>
    /// Queries all records from the specified table async
    /// </summary>
    /// <param name="tableName">Name of the table to query from</param>
    /// <param name="fieldList">Fields to query (or * for all fields)</param>
    /// <param name="orderBy">Order (or empty string if no special order is desired)</param>
    /// <param name="selectMethod">Select method (such as stored procedure or select commands)</param>
    /// <returns>DataSet with the requested data</returns>
    public static async Task<DataSet> GetAllRecordsAsync(string tableName, string fieldList = "*", string orderBy = "", DataRowProcessMethod selectMethod = DataRowProcessMethod.Default) => await DataService.GetAllRecordsAsync(tableName, fieldList, orderBy, selectMethod);

    /// <summary>
    ///     Returns a single record or multiple records (with a specified list of fields) by primary key.
    /// </summary>
    /// <remarks>
    ///     The name of the method is a little misleading. It can actually return more than one query *if* the key
    ///     field used for the query is non-unique (in other words: not a primary key field).
    /// </remarks>
    /// <param name="tableName">Name of the table to query.</param>
    /// <param name="fieldList">List of fields to return.</param>
    /// <param name="primaryKeyFieldName">Name of the primary key field.</param>
    /// <param name="primaryKeyValue">Primary key (value)</param>
    /// <param name="selectMethod">Select method (such as stored procedure or select commands)</param>
    /// <returns>DataSet with the returned data</returns>
    /// <example>
    ///     using (var data = Data.GetSingleRecord("Customers", "*", "CustomerKey", key))
    ///     {
    ///         // Do something with the data
    ///     }
    /// </example>
    public static DataSet GetSingleRecord(string tableName, string fieldList = "*", string primaryKeyFieldName = "Id", object primaryKeyValue = null, DataRowProcessMethod selectMethod = DataRowProcessMethod.Default) => DataService.GetSingleRecord(tableName, fieldList, primaryKeyFieldName, primaryKeyValue, selectMethod);

    /// <summary>
    ///     Returns a single record or multiple records (with a specified list of fields) by primary key (async).
    /// </summary>
    /// <remarks>
    ///     The name of the method is a little misleading. It can actually return more than one query *if* the key
    ///     field used for the query is non-unique (in other words: not a primary key field).
    /// </remarks>
    /// <param name="tableName">Name of the table to query.</param>
    /// <param name="fieldList">List of fields to return.</param>
    /// <param name="primaryKeyFieldName">Name of the primary key field.</param>
    /// <param name="primaryKeyValue">Primary key (value)</param>
    /// <param name="selectMethod">Select method (such as stored procedure or select commands)</param>
    /// <returns>DataSet with the returned data</returns>
    /// <example>
    ///     using (var data = await Data.GetSingleRecordAsync("Customers", "*", "CustomerKey", key))
    ///     {
    ///         // Do something with the data
    ///     }
    /// </example>
    public static async Task<DataSet> GetSingleRecordAsync(string tableName, string fieldList = "*", string primaryKeyFieldName = "Id", object primaryKeyValue = null, DataRowProcessMethod selectMethod = DataRowProcessMethod.Default) => await DataService.GetSingleRecordAsync(tableName, fieldList, primaryKeyFieldName, primaryKeyValue, selectMethod);

    /// <summary>Runs the specified query</summary>
    /// <param name="tableName">Name of the table.</param>
    /// <param name="fieldList">The list of fields returned by the query (ignored for stored procedure execution)</param>
    /// <param name="fieldNames">The list of fields by which to filter</param>
    /// <param name="filterParameters">Parameters used for filtering. The parameters need to match the list of filter fields (name and types)</param>
    /// <param name="selectMethod">Process method for the select method</param>
    /// <returns>DataSet with the returned data</returns>
    /// <example>
    /// var fieldNames = new string[] { "FirstName", "LastName", "IsActive" };
    /// var parameters = new object[] { "Chris", "Pronger", true };
    /// using (var data = Data.Query("Customers", "*", fieldNames, parameters);
    /// {
    ///     // Do something with the data
    /// }
    /// </example>
    public static DataSet Query(string tableName, string fieldList, IList<string> fieldNames, IList<object> filterParameters, DataRowProcessMethod selectMethod = DataRowProcessMethod.Default) => DataService.Query(tableName, fieldList, fieldNames, filterParameters, selectMethod);

    /// <param name="tableName">Name of the table.</param>
    /// <param name="fieldList">The list of fields returned by the query (ignored for stored procedure execution)</param>
    /// <param name="fieldNames">The list of fields by which to filter</param>
    /// <param name="filterParameters">Parameters used for filtering. The parameters need to match the list of filter fields (name and types)</param>
    /// <param name="selectMethod">Process method for the select method</param>
    /// <returns>DataSet with the returned data</returns>
    /// <example>
    /// var fieldNames = new string[] { "FirstName", "LastName", "IsActive" };
    /// var parameters = new object[] { "Chris", "Pronger", true };
    /// using (var data = await Data.QueryAsync("Customers", "*", fieldNames, parameters);
    /// {
    ///     // Do something with the data
    /// }
    /// </example>
    public static async Task<DataSet> QueryAsync(string tableName, string fieldList, IList<string> fieldNames, IList<object> filterParameters, DataRowProcessMethod selectMethod = DataRowProcessMethod.Default) => await DataService.QueryAsync(tableName, fieldList, fieldNames, filterParameters, selectMethod);

    /// <summary>Runs the specified query</summary>
    /// <param name="command">Command to execute</param>
    /// <param name="entityName">Name of the table in the resulting DataSet</param>
    /// <returns>DataSet with the returned data</returns>
    /// <example>
    /// using (var command = NewCommand("SELECT * FROM Customers"))
    /// using (var data = Data.Query(command);
    /// {
    ///     // Do something with the data
    /// }
    ///
    ///  or:
    /// 
    /// using (var data = Data.Query(NewCommand("SELECT * FROM Customers"));
    /// {
    ///     // Do something with the data
    /// }
    /// </example>
    public static DataSet Query(IDbCommand command, string entityName = "") => DataService.Query(command, entityName);

    /// <summary>Runs the specified query async</summary>
    /// <param name="command">Command to execute</param>
    /// <param name="entityName">Name of the table in the resulting DataSet</param>
    /// <returns>DataSet with the returned data</returns>
    /// <example>
    /// using (var command = NewCommand("SELECT * FROM Customers"))
    /// using (var data = await Data.QueryAsync(command);
    /// {
    ///     // Do something with the data
    /// }
    ///
    ///  or:
    /// 
    /// using (var data = await Data.QueryAsync(NewCommand("SELECT * FROM Customers"));
    /// {
    ///     // Do something with the data
    /// }
    /// </example>
    public static async Task<DataSet> QueryAsync(IDbCommand command, string entityName = "") => await DataService.QueryAsync(command, entityName);

    /// <summary>Runs the specified query</summary>
    /// <param name="command">Command to execute</param>
    /// <param name="parameters">Parameters used by the command</param>
    /// <param name="entityName">Name of the table in the resulting DataSet</param>
    /// <returns>DataSet with the returned data</returns>
    /// <example>
    /// using (var data = Data.Query("SELECT * FROM Customers");
    /// {
    ///     // Do something with the data
    /// }
    /// 
    /// // or:
    /// 
    /// var parameters = new Dictionary&lt;string, object&gt;();
    /// parameters.Add("@FirstName", "Markus");
    /// parameters.Add("@LastName", "Egger%");
    /// using (var data = Data.Query("SELECT * FROM Customers WHERE l_lane LIKE @LastName AND f_name LIKE @FirstName", parameters));
    /// {
    ///     // Do something with the data
    /// }
    /// 
    /// // or:
    /// 
    /// var parameters = new Dictionary&lt;string, object&gt; {{"@FirstName", "Markus"}, {"@LastName", "Egger%"}};
    /// using (var data = Data.Query("SELECT * FROM Customers WHERE l_lane LIKE @LastName AND f_name LIKE @FirstName", parameters));
    /// {
    ///     // Do something with the data
    /// }
    /// 
    /// // or:
    /// 
    /// using (var data = Data.Query("SELECT * FROM Customers WHERE l_lane LIKE @LastName AND f_name LIKE @FirstName", new Dictionary&lt;string, object&gt; {{"@FirstName", "Markus"}, {"@LastName", "Egger%"}}));
    /// {
    ///     // Do something with the data
    /// }
    /// </example>
    public static DataSet Query(string command, Dictionary<string, object> parameters = null, string entityName = "") => DataService.Query(command, parameters, entityName);

    /// <summary>Runs the specified query async</summary>
    /// <param name="command">Command to execute</param>
    /// <param name="parameters">Parameters used by the command</param>
    /// <param name="entityName">Name of the table in the resulting DataSet</param>
    /// <returns>DataSet with the returned data</returns>
    /// <example>
    /// using (var data = await Data.QueryAsync("SELECT * FROM Customers");
    /// {
    ///     // Do something with the data
    /// }
    /// 
    /// // or:
    /// 
    /// var parameters = new Dictionary&lt;string, object&gt;();
    /// parameters.Add("@FirstName", "Markus");
    /// parameters.Add("@LastName", "Egger%");
    /// using (var data = await service.QueryAsync("SELECT * FROM Customers WHERE l_lane LIKE @LastName AND f_name LIKE @FirstName", parameters));
    /// {
    ///     // Do something with the data
    /// }
    /// 
    /// // or:
    /// 
    /// var parameters = new Dictionary&lt;string, object&gt; {{"@FirstName", "Markus"}, {"@LastName", "Egger%"}};
    /// using (var data = await Data.QueryAsync("SELECT * FROM Customers WHERE l_lane LIKE @LastName AND f_name LIKE @FirstName", parameters));
    /// {
    ///     // Do something with the data
    /// }
    /// 
    /// // or:
    /// 
    /// using (var data = await Data.QueryAsync("SELECT * FROM Customers WHERE l_lane LIKE @LastName AND f_name LIKE @FirstName", new Dictionary&lt;string, object&gt; {{"@FirstName", "Markus"}, {"@LastName", "Egger%"}}));
    /// {
    ///     // Do something with the data
    /// }
    /// </example>
    public static async Task<DataSet> QueryAsync(string command, Dictionary<string, object> parameters = null, string entityName = "") => await DataService.QueryAsync(command, parameters, entityName);
}