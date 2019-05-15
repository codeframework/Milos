using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Threading.Tasks;

namespace Milos.Data
{
    /// <summary>
    ///     This interface represents the most basic incarnation of an EPS Data Service.
    ///     It can be implemented at a low level, without having to subclass any of the
    ///     abstract or solid data service classes in our framework.
    /// </summary>
    public interface IDataService : IDisposable
    {
        /// <summary>
        ///     This property provides additional information why the status may be invalid.
        /// </summary>
        string InvalidStatus { get; }

        /// <summary>
        ///     Indicates the status of the connection (such as "LAN", "Internet", or "Offline").
        /// </summary>
        DataServiceConnectionStatus ConnectionStatus { get; }

        /// <summary>
        ///     Returns an identifier string that allows the developer to compare two different
        ///     instances of a data service or two completely different services to see
        ///     whether they connect to the same database in the same way.
        /// </summary>
        string ServiceInstanceIdentifier { get; }

        /// <summary>
        ///     Minimum date value supported by this data source
        /// </summary>
        DateTime DateMinValue { get; }

        /// <summary>
        ///     Maximum date value supported by this data source
        /// </summary>
        DateTime DateMaxValue { get; }

        /// <summary>
        ///     Sets the prefix (namespace) used in the configuration file.
        /// </summary>
        /// <param name="prefix">Prefix (such as "database" or "Northwind")</param>
        void SetConfigurationPrefix(string prefix);

        /// <summary>
        ///     This method checks if the data service can connect to a life data source.
        /// </summary>
        /// <returns>True if the connection is available.</returns>
        bool IsValid();

        /// <summary>
        ///     This method executes a query and returns a DataSet containing the results.
        /// </summary>
        /// <param name="command">SQL Command Object</param>
        /// <param name="entityName">Name of the newly added entity (table) in the DataSet</param>
        /// <param name="existingDataSet">Existing data set the result set is to be added to.</param>
        /// <returns>Resulting DataSet</returns>
        DataSet ExecuteQuery(IDbCommand command, string entityName, DataSet existingDataSet = null);

        /// <summary>
        ///     This method executes a query and fills the new information into the DataSet that has been passed as a parameter..
        /// </summary>
        /// <param name="command">SQL Command Object</param>
        /// <param name="entityName">Name of the newly added entity (table) in the DataSet</param>
        /// <param name="existingDataSet">Existing data set the result set is to be added to.</param>
        /// <returns>Nothing</returns>
        Task<DataSet> ExecuteQueryAsync(IDbCommand command, string entityName, DataSet existingDataSet);

        /// <summary>
        ///     This method executes a query and returns the number of affected rows.
        /// </summary>
        /// <param name="command">Command string (such as an SQL Insert command)</param>
        /// <returns>Number of affected rows.</returns>
        int ExecuteNonQuery(IDbCommand command);

        /// <summary>
        ///     This method executes a query and returns the number of affected rows.
        /// </summary>
        /// <param name="command">Command string (such as an SQL Insert command)</param>
        /// <param name="expectedRecordCount">Number of records we expect to be effected by this command.</param>
        /// <returns>True, if number of affected records is the same as the expected record count.</returns>
        bool ExecuteNonQuery(IDbCommand command, int expectedRecordCount);

        /// <summary>
        ///     This method executes a query asynchronously.
        /// </summary>
        /// <param name="command">Command string (such as an SQL Insert command)</param>
        Task<int> ExecuteNonQueryAsync(IDbCommand command);

        /// <summary>This method executes a query and returns a single value.</summary>
        /// <param name="command">Command object (such as an SQL Select command)</param>
        /// <returns>Single Value</returns>
        object ExecuteScalar(IDbCommand command);

        /// <summary>This method executes a query and returns a single value.</summary>
        /// <param name="command">Command object (such as an SQL Select command)</param>
        /// <returns>Single Value</returns>
        T ExecuteScalar<T>(IDbCommand command);

        /// <summary>
        ///     This method executes a query that returns a single value asynchronously (use the ScalarValueRetrieved event to get
        ///     the value).
        /// </summary>
        /// <param name="command">Command object (such as an SQL Select command)</param>
        /// <returns>Nothing</returns>
        Task<object> ExecuteScalarAsync(IDbCommand command);

        /// <summary>
        ///     This method executes a query that returns a single value asynchronously (use the ScalarValueRetrieved event to get
        ///     the value).
        /// </summary>
        /// <param name="command">Command object (such as an SQL Select command)</param>
        /// <returns>Nothing</returns>
        Task<T> ExecuteScalarAsync<T>(IDbCommand command);

        /// <summary>
        ///     Executes a stored procedure and adds the result to an existing DataSet
        /// </summary>
        /// <param name="command">Sql Command object</param>
        /// <param name="entityName">Name of the resulting entity in the DataSet</param>
        /// <param name="existingDataSet">Existing data set the data is to be added to</param>
        /// <returns>DataSet</returns>
        DataSet ExecuteStoredProcedureQuery(IDbCommand command, string entityName, DataSet existingDataSet = null);

        /// <summary>
        ///     Executes a stored procedure and adds the result to an existing DataSet
        /// </summary>
        /// <param name="command">Sql Command object</param>
        /// <param name="entityName">Name of the resulting entity in the DataSet</param>
        /// <param name="existingDataSet">Existing data set the data is to be added to</param>
        /// <returns>DataSet</returns>
        Task<DataSet> ExecuteStoredProcedureQueryAsync(IDbCommand command, string entityName, DataSet existingDataSet = null);

        /// <summary>
        ///     Executes a stored procedure
        /// </summary>
        /// <param name="command">Sql Command object</param>
        /// <returns>True or False</returns>
        bool ExecuteStoredProcedure(IDbCommand command);

        /// <summary>
        ///     Executes a stored procedure
        /// </summary>
        /// <param name="command">Sql Command object</param>
        /// <returns>True or False</returns>
        Task<bool> ExecuteStoredProcedureAsync(IDbCommand command);

        /// <summary>Returns a database command object.</summary>
        /// <param name="commandText">Command text to be set on the new command object</param>
        /// <remarks>
        /// In an SQL Server service, this would be SqlCommand, in an OleDb service, this would be OleDbCommand, and so forth.
        /// This method is identical to NewCommand()
        /// </remarks>
        /// <returns>IDbCommand object instance</returns>
        IDbCommand NewCommandObject(string commandText = null);

        /// <summary>Returns a database command object.</summary>
        /// <param name="commandText">Command text to be set on the new command object</param>
        /// <remarks>
        /// In an SQL Server service, this would be SqlCommand, in an OleDb service, this would be OleDbCommand, and so forth.
        /// This method is identical to NewCommandObject()
        /// </remarks>
        /// <returns>IDbCommand object instance</returns>
        IDbCommand NewCommand(string commandText = null);

        /// <summary>
        ///     Returns an instance of a parameter object that can be added
        ///     to an IDbCommand.Parameters collection.
        /// </summary>
        /// <param name="parameterName">Name of the new parameter</param>
        /// <param name="parameterValue">Value of the new parameter</param>
        /// <returns>Parameter object</returns>
        IDbDataParameter NewCommandObjectParameter(string parameterName, object parameterValue);

        /// <summary>
        ///     Generically returns the value of the specified parameter in a
        ///     generic IDbCommand object or any other object that implements that interface
        /// </summary>
        /// <param name="parameterName">Parameter Name</param>
        /// <param name="command">Command Object</param>
        /// <returns>Parameter value</returns>
        object GetCommandParameterValue(string parameterName, IDbCommand command);

        /// <summary>
        ///     Creates an update command object for the row passed along.
        /// </summary>
        /// <param name="changedRow">Changed ADO.NET data row</param>
        /// <param name="primaryKeyType">Primary key type</param>
        /// <param name="primaryKeyFieldName">Name of the primary key field</param>
        /// <param name="updateMode">Optimistic or pessimistic update mode.</param>
        /// <param name="updateMethod">Method used to update the database (commands, stored procedures,...)</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="fieldNames">Names of the fields to be included in the update (all others will be ignored)</param>
        /// <param name="fieldMaps">
        ///     List of key value pairs that can be used to map field names. For instance, if a field in the
        ///     table is called MyId but in the database it is called ID, then one can add a key 'MyId' with a value of 'ID'
        /// </param>
        /// <returns>
        ///     Update command that can sub-sequentially be executed against the database using the same data service.
        /// </returns>
        IDbCommand BuildUpdateCommand(DataRow changedRow, KeyType primaryKeyType, string primaryKeyFieldName, string tableName, DataRowUpdateMode updateMode = DataRowUpdateMode.ChangedFieldsOnly, DataRowProcessMethod updateMethod = DataRowProcessMethod.Default, IList<string> fieldNames = null, IDictionary<string, string> fieldMaps = null);

        /// <summary>
        ///     Creates a delete command object for the defined table and primary key.
        /// </summary>
        /// <param name="tableName">Name of the table the record is to be deleted from .</param>
        /// <param name="primaryKeyFieldName">Primary key field name within the table</param>
        /// <param name="primaryKeyValue">Primary key value for the record that is to be deleted</param>
        /// <param name="updateMethod">Method used to update the database (commands, stored procedures,...)</param>
        /// <returns>IDbCommand object that can sub-sequentially be executed against a database</returns>
        IDbCommand BuildDeleteCommand(string tableName, string primaryKeyFieldName, object primaryKeyValue, DataRowProcessMethod updateMethod = DataRowProcessMethod.Default);

        /// <summary>
        ///     Builds a command object that queries an empty record containing all fields of the specified table.
        /// </summary>
        /// <param name="tableName">Table Name</param>
        /// <param name="fieldList">List of fields to be included in the query</param>
        /// <param name="selectMethod">Select method (such as stored procedure or select commands)</param>
        /// <returns>IDbCommand object</returns>
        IDbCommand BuildEmptyRecordQueryCommand(string tableName, string fieldList = "*", DataRowProcessMethod selectMethod = DataRowProcessMethod.Default);

        /// <summary>
        ///     Builds a command object that queries all records (with specified fields) from the specified table.
        ///     Defining an order is possible as well.
        /// </summary>
        /// <param name="tableName">Name of the table to query from</param>
        /// <param name="fieldList">Fields to query (or * for all fields)</param>
        /// <param name="orderBy">Order (or empty string if no special order is desired)</param>
        /// <param name="selectMethod">Select method (such as stored procedure or select commands)</param>
        /// <returns>IDbCommand object</returns>
        IDbCommand BuildAllRecordsQueryCommand(string tableName, string fieldList = "*", string orderBy = "", DataRowProcessMethod selectMethod = DataRowProcessMethod.Default);

        /// <summary>
        /// Queries all records from the specified table
        /// </summary>
        /// <param name="tableName">Name of the table to query from</param>
        /// <param name="fieldList">Fields to query (or * for all fields)</param>
        /// <param name="orderBy">Order (or empty string if no special order is desired)</param>
        /// <param name="selectMethod">Select method (such as stored procedure or select commands)</param>
        /// <returns>DataSet with the requested data</returns>
        DataSet GetAllRecords(string tableName, string fieldList = "*", string orderBy = "", DataRowProcessMethod selectMethod = DataRowProcessMethod.Default);

        /// <summary>
        /// Queries all records from the specified table async
        /// </summary>
        /// <param name="tableName">Name of the table to query from</param>
        /// <param name="fieldList">Fields to query (or * for all fields)</param>
        /// <param name="orderBy">Order (or empty string if no special order is desired)</param>
        /// <param name="selectMethod">Select method (such as stored procedure or select commands)</param>
        /// <returns>DataSet with the requested data</returns>
        Task<DataSet> GetAllRecordsAsync(string tableName, string fieldList = "*", string orderBy = "", DataRowProcessMethod selectMethod = DataRowProcessMethod.Default);

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
        /// <returns>IDbCommand object representing the query</returns>
        /// <example>
        ///     var command = service.BuildSingleRecordQueryCommand("Customers", "*", "CustomerKey", key,
        ///     DataRowProcessMethod.Default);
        ///     DataSet data = service.ExecuteQuery(command);
        /// </example>
        IDbCommand BuildSingleRecordQueryCommand(string tableName, string fieldList = "*", string primaryKeyFieldName = "Id", object primaryKeyValue = null, DataRowProcessMethod selectMethod = DataRowProcessMethod.Default);

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
        ///     using (var data= service.GetSingleRecord("Customers", "*", "CustomerKey", key))
        ///     {
        ///         // Do something with the data
        ///     }
        /// </example>
        DataSet GetSingleRecord(string tableName, string fieldList = "*", string primaryKeyFieldName = "Id", object primaryKeyValue = null, DataRowProcessMethod selectMethod = DataRowProcessMethod.Default);

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
        ///     using (var data = await service.GetSingleRecordAsync("Customers", "*", "CustomerKey", key))
        ///     {
        ///         // Do something with the data
        ///     }
        /// </example>
        Task<DataSet> GetSingleRecordAsync(string tableName, string fieldList = "*", string primaryKeyFieldName = "Id", object primaryKeyValue = null, DataRowProcessMethod selectMethod = DataRowProcessMethod.Default);

        /// <summary>
        ///     Builds a command that returns a set of records based on the provided field names and filter parameters.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="fieldList">The list of fields returned by the query (ignored for stored procedure execution)</param>
        /// <param name="fieldNames">The list of fields by which to filter</param>
        /// <param name="filterParameters">Parameters used for filtering. The parameters need to match the list of filter fields (name and types)</param>
        /// <param name="selectMethod">Process method for the select method</param>
        /// <returns>IDbCommand object representing the query</returns>
        /// <example>
        /// var fieldNames = new string[] { "FirstName", "LastName", "IsActive" };
        /// var parameters = new object[] { "Chris", "Pronger", true };
        /// using (var command = service.BuildQueryCommand("Customers", "*", fieldNames, parameters))
        /// using (var data = service.ExecuteQuery(command))
        /// {
        ///     // Do something with the data
        /// }
        /// </example>
        IDbCommand BuildQueryCommand(string tableName, string fieldList, IList<string> fieldNames, IList<object> filterParameters, DataRowProcessMethod selectMethod = DataRowProcessMethod.Default);

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
        /// using (var data = service.Query("Customers", "*", fieldNames, parameters);
        /// {
        ///     // Do something with the data
        /// }
        /// </example>
        DataSet Query(string tableName, string fieldList, IList<string> fieldNames, IList<object> filterParameters, DataRowProcessMethod selectMethod = DataRowProcessMethod.Default);

        /// <param name="tableName">Name of the table.</param>
        /// <param name="fieldList">The list of fields returned by the query (ignored for stored procedure execution)</param>
        /// <param name="fieldNames">The list of fields by which to filter</param>
        /// <param name="filterParameters">Parameters used for filtering. The parameters need to match the list of filter fields (name and types)</param>
        /// <param name="selectMethod">Process method for the select method</param>
        /// <returns>DataSet with the returned data</returns>
        /// <example>
        /// var fieldNames = new string[] { "FirstName", "LastName", "IsActive" };
        /// var parameters = new object[] { "Chris", "Pronger", true };
        /// using (var data = await service.QueryAsync("Customers", "*", fieldNames, parameters);
        /// {
        ///     // Do something with the data
        /// }
        /// </example>
        Task<DataSet> QueryAsync(string tableName, string fieldList, IList<string> fieldNames, IList<object> filterParameters, DataRowProcessMethod selectMethod = DataRowProcessMethod.Default);

        /// <summary>Runs the specified query</summary>
        /// <param name="command">Command to execute</param>
        /// <param name="entityName">Name of the table in the resulting DataSet</param>
        /// <returns>DataSet with the returned data</returns>
        /// <example>
        /// using (var command = NewCommand("SELECT * FROM Customers"))
        /// using (var data = service.Query(command);
        /// {
        ///     // Do something with the data
        /// }
        ///
        ///  or:
        /// 
        /// using (var data = service.Query(NewCommand("SELECT * FROM Customers"));
        /// {
        ///     // Do something with the data
        /// }
        /// </example>
        DataSet Query(IDbCommand command, string entityName = "");

        /// <summary>Runs the specified query async</summary>
        /// <param name="command">Command to execute</param>
        /// <param name="entityName">Name of the table in the resulting DataSet</param>
        /// <returns>DataSet with the returned data</returns>
        /// <example>
        /// using (var command = NewCommand("SELECT * FROM Customers"))
        /// using (var data = await service.QueryAsync(command);
        /// {
        ///     // Do something with the data
        /// }
        ///
        ///  or:
        /// 
        /// using (var data = await service.QueryAsync(NewCommand("SELECT * FROM Customers"));
        /// {
        ///     // Do something with the data
        /// }
        /// </example>
        Task<DataSet> QueryAsync(IDbCommand command, string entityName = "");

        /// <summary>Runs the specified query</summary>
        /// <param name="command">Command to execute</param>
        /// <param name="parameters">Parameters used by the command</param>
        /// <param name="entityName">Name of the table in the resulting DataSet</param>
        /// <returns>DataSet with the returned data</returns>
        /// <example>
        /// using (var data = service.Query("SELECT * FROM Customers");
        /// {
        ///     // Do something with the data
        /// }
        /// 
        /// // or:
        /// 
        /// var parameters = new Dictionary&lt;string, object&gt;();
        /// parameters.Add("@FirstName", "Markus");
        /// parameters.Add("@LastName", "Egger%");
        /// using (var data = service.Query("SELECT * FROM Customers WHERE l_lane LIKE @LastName AND f_name LIKE @FirstName", parameters));
        /// {
        ///     // Do something with the data
        /// }
        /// 
        /// // or:
        /// 
        /// var parameters = new Dictionary&lt;string, object&gt; {{"@FirstName", "Markus"}, {"@LastName", "Egger%"}};
        /// using (var data = service.Query("SELECT * FROM Customers WHERE l_lane LIKE @LastName AND f_name LIKE @FirstName", parameters));
        /// {
        ///     // Do something with the data
        /// }
        /// 
        /// // or:
        /// 
        /// using (var data = service.Query("SELECT * FROM Customers WHERE l_lane LIKE @LastName AND f_name LIKE @FirstName", new Dictionary&lt;string, object&gt; {{"@FirstName", "Markus"}, {"@LastName", "Egger%"}}));
        /// {
        ///     // Do something with the data
        /// }
        /// </example>
        DataSet Query(string command, Dictionary<string, object> parameters = null, string entityName = "");

        /// <summary>Runs the specified query async</summary>
        /// <param name="command">Command to execute</param>
        /// <param name="parameters">Parameters used by the command</param>
        /// <param name="entityName">Name of the table in the resulting DataSet</param>
        /// <returns>DataSet with the returned data</returns>
        /// <example>
        /// using (var data = await service.QueryAsync("SELECT * FROM Customers");
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
        /// using (var data = await service.QueryAsync("SELECT * FROM Customers WHERE l_lane LIKE @LastName AND f_name LIKE @FirstName", parameters));
        /// {
        ///     // Do something with the data
        /// }
        /// 
        /// // or:
        /// 
        /// using (var data = await service.QueryAsync("SELECT * FROM Customers WHERE l_lane LIKE @LastName AND f_name LIKE @FirstName", new Dictionary&lt;string, object&gt; {{"@FirstName", "Markus"}, {"@LastName", "Egger%"}}));
        /// {
        ///     // Do something with the data
        /// }
        /// </example>
        Task<DataSet> QueryAsync(string command, Dictionary<string, object> parameters = null, string entityName = "");

        /// <summary>This event fires whenever a query has been completed</summary>
        event EventHandler<QueryEventArgs> QueryComplete;

        /// <summary>This event fires whenever a scalar query has been completed</summary>
        event EventHandler<ScalarEventArgs> ScalarQueryComplete;

        /// <summary>This event fires whenever a non-query database command has been completed</summary>
        event EventHandler<NonQueryEventArgs> NonQueryComplete;

        /// <summary>Applies an application security role. This can be used in case the user by default has no rights.</summary>
        /// <param name="role">SQL Server Role</param>
        /// <param name="password">App role password</param>
        /// <returns>True if role was applied successfully.</returns>
        bool ApplyAppRole(string role, string password);

        /// <summary>
        ///     Reverts to a previous app role. This is typically done after an
        ///     app role has been applied using ApplyAppRole.
        ///     RevertAppRole simply reverts back to a previously set app role.
        ///     This is done in a FILO stack fashion.
        /// </summary>
        /// <returns>True or False</returns>
        bool RevertAppRole();

        /// <summary>
        ///     Begins a server-side transaction.
        /// </summary>
        /// <returns>True if transaction was opened successfully.</returns>
        bool BeginTransaction();

        /// <summary>
        ///     Ends a server-side transaction and commits changes.
        /// </summary>
        /// <returns>True if changes were applied successfully.</returns>
        bool CommitTransaction();

        /// <summary>
        ///     Ends a server-side transaction and rolls back changes.
        /// </summary>
        /// <returns>True if aborted successfully.</returns>
        bool AbortTransaction();

        /// <summary>
        ///     Indicates the current transaction status.
        /// </summary>
        TransactionStatus TransactionStatus { get; }

        /// <summary>
        /// Returns all records and all fields for the specified table
        /// </summary>
        /// <param name="tableName">Table to return the data for.</param>
        /// <param name="fieldList">Comma-separated list of fields to return, or * for all fields (default).</param>
        /// <param name="sortOrder">Sort order expression (typically a field name)</param>
        /// <param name="selectMethod">Defines whether individual commands or a stored procedure is used to access the data</param>
        /// <returns>DataSet with the queried data</returns>
        /// <remarks>Since this returns all data, from a table, this method should only be used for small tables.</remarks>
        /// <example>
        /// var service = DataServiceFactory.GetService();
        /// var ds1 = service.GetList("Categories");
        /// var ds2 = service.GetList("Categories", "Id, Name");
        /// var ds3 = service.GetList("Categories", "Id, Name, DateCreated", "DateCreated DESC");
        /// </example>
        DataSet GetList(string tableName, string fieldList = "*", string sortOrder = "", DataRowProcessMethod selectMethod = DataRowProcessMethod.Default);

        /// <summary>
        /// Returns all records and all fields for the specified table async
        /// </summary>
        /// <param name="tableName">Table to return the data for.</param>
        /// <param name="fieldList">Comma-separated list of fields to return, or * for all fields (default).</param>
        /// <param name="sortOrder">Sort order expression (typically a field name)</param>
        /// <param name="selectMethod">Defines whether individual commands or a stored procedure is used to access the data</param>
        /// <returns>DataSet with the queried data</returns>
        /// <remarks>Since this returns all data, from a table, this method should only be used for small tables.</remarks>
        /// <example>
        /// var service = DataServiceFactory.GetService();
        /// var ds1 = service.GetList("Categories");
        /// var ds2 = service.GetList("Categories", "Id, Name");
        /// var ds3 = service.GetList("Categories", "Id, Name, DateCreated", "DateCreated DESC");
        /// </example>
        Task<DataSet> GetListAsync(string tableName, string fieldList = "*", string sortOrder = "", DataRowProcessMethod selectMethod = DataRowProcessMethod.Default);

    }

    /// <summary>
    ///     This enum defines the status of each data service. This information
    ///     can be used to provide connection information to the user.
    /// </summary>
    public enum DataServiceConnectionStatus
    {
        /// <summary>Status unknown</summary>
        Unknown,

        /// <summary>An online connection on a local area network</summary>
        OnlineLAN,

        /// <summary>An online connection over the internet</summary>
        OnlineInternet,

        /// <summary>No connection (possibly an offline cache)</summary>
        Offline
    }

    /// <summary>
    ///     Defines what records to update on the database back end
    /// </summary>
    public enum DataRowUpdateMode
    {
        /// <summary>Send only changed field values to the server ("optimistic")</summary>
        ChangedFieldsOnly,

        /// <summary>Send all fields to the server regardless of their changed state ("pessimistic")</summary>
        AllFields
    }

    /// <summary>
    ///     Defines how update commands are generated for a certain data source
    /// </summary>
    public enum DataRowProcessMethod
    {
        /// <summary>Whatever the default is. This is probably the most common setting. Note: This may also be influenced by other configuration settings.</summary>
        Default,

        /// <summary>Created individual commands, such as UPDATE for SQL Server.</summary>
        IndividualCommands,

        /// <summary>Uses stored procedure for updates (or the database's equivalent thereof)</summary>
        StoredProcedures,

        /// <summary>Other (reserved)</summary>
        Other,

        /// <summary>Other (reserved)</summary>
        Other2,

        /// <summary>Other (reserved)</summary>
        Other3
    }

    /// <summary>
    ///     What is the status of the current transaction?
    /// </summary>
    public enum TransactionStatus
    {
        /// <summary>Transaction status is completely unknown</summary>
        Unknown,

        /// <summary>No transaction is currently active.</summary>
        NoTransaction,

        /// <summary>A transaction is currently active</summary>
        TransactionInProgress,

        /// <summary>The last transaction has been aborted</summary>
        TransactionAborted,

        /// <summary>The last transaction has been committed</summary>
        TransactionCommitted
    }

    /// <summary>
    ///     Query event arguments
    /// </summary>
    public class QueryEventArgs : EventArgs
    {
        /// <summary>Constructor</summary>
        /// <param name="resultDataSet">DataSet resulting from the query</param>
        /// <param name="executedCommand">Command executed to arrive at the specified result set</param>
        /// <param name="entityName">Name of the last queried entity</param>
        public QueryEventArgs(DataSet resultDataSet, IDbCommand executedCommand, string entityName)
        {
            ResultDataSet = resultDataSet;
            ExecutedCommand = executedCommand;
            EntityName = entityName;
        }

        /// <summary>
        ///     DataSet returned from the query
        /// </summary>
        public DataSet ResultDataSet { get; }

        /// <summary>DataTable within the current DataSet that contains the result of the most recent query.</summary>
        public DataTable ResultDataTable => ResultDataSet?.Tables[EntityName];

        /// <summary>Executed command</summary>
        public IDbCommand ExecutedCommand { get; }

        /// <summary>Provides the name of the last queried entity name</summary>
        public string EntityName { get; }

        /// <summary>
        ///     Returns an XML serialized version of the executed command object
        /// </summary>
        /// <returns>Xml</returns>
        public string GetSerializedCommandXml() => DataHelper.SerializeIDbCommandToXml(ExecutedCommand);
    }

    /// <summary>
    ///     Scalar query event arguments
    /// </summary>
    public class ScalarEventArgs : EventArgs
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="executedCommand">Command executed to arrive at the specified result set</param>
        /// <param name="result">Result of the scalar query</param>
        public ScalarEventArgs(IDbCommand executedCommand, object result)
        {
            ExecutedCommand = executedCommand;
            Result = result;
        }

        /// <summary>
        ///     Executed command
        /// </summary>
        public IDbCommand ExecutedCommand { get; }

        /// <summary>
        ///     Returned result for the scalar query
        /// </summary>
        public object Result { get; }

        /// <summary>
        ///     Returns an XML serialized version of the executed command object
        /// </summary>
        /// <returns>Xml</returns>
        public string GetSerializedCommandXml() => DataHelper.SerializeIDbCommandToXml(ExecutedCommand);
    }

    /// <summary>
    ///     Non-query event arguments
    /// </summary>
    public class NonQueryEventArgs : EventArgs
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="executedCommand">Command executed to arrive at the specified result set</param>
        /// <param name="affectedRecords">Number of records affected by the completed non-query</param>
        public NonQueryEventArgs(IDbCommand executedCommand, int affectedRecords)
        {
            ExecutedCommand = executedCommand;
            AffectedRecords = affectedRecords;
        }

        /// <summary>
        ///     Executed command
        /// </summary>
        public IDbCommand ExecutedCommand { get; }

        /// <summary>
        ///     Number of affected records
        /// </summary>
        public int AffectedRecords { get; }

        /// <summary>
        ///     Returns an XML serialized version of the executed command object
        /// </summary>
        /// <returns>Xml</returns>
        public string GetSerializedCommandXml() => DataHelper.SerializeIDbCommandToXml(ExecutedCommand);
    }

    /// <summary>Defines supported primary key types</summary>
    public enum KeyType
    {
        /// <summary>Unique identifier</summary>
        Guid,

        /// <summary>Integer identifier</summary>
        Integer,

        /// <summary>Auto-increment integer identifier</summary>
        IntegerAutoIncrement,

        /// <summary>String identifier</summary>
        String
    }

    /// <summary>
    /// Defines which data access methods are allowed in the current scenario.
    /// Example: If the allowed method is "StoredProcedures" and someone tries
    /// to execute an individual SELECT command, the data service will not
    /// execute the call.
    /// </summary>
    public enum AllowedDataAccessMethod
    {
        /// <summary>Undefined/Unknown</summary>
        Undefined,

        /// <summary>All modes are acceptable (default)</summary>
        All,

        /// <summary>Stored Procedures only</summary>
        StoredProceduresOnly,

        /// <summary>Individual database commands only</summary>
        IndividualCommandsOnly
    }
}