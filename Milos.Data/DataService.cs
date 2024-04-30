using System.Globalization;
using System.Threading.Tasks;

namespace Milos.Data;

/// <summary>
///     Business objects use this class to connect to a data service.
///     This is an abstract class that is not meant to be used directly.
///     Instead, the DataServiceFactory class will instantiate a subclass
///     of this class, depending on the actual configuration of the factory.
///     Individual subclasses could connect to different data sources in different
///     ways, such as a direct connection to SQL Server, or a connection
///     to a web service, and the like.
/// </summary>
public abstract class DataService : IDataService
{
    /// <summary>
    ///     Internal configuration prefix. Based on this, different nodes
    ///     will be used in the configuration file (such as database:username)
    /// </summary>
    protected string DataConfigurationPrefix { get; set; } = "database";

    /// <summary>
    ///     Unique identifier of this instance of the data service
    ///     This can be used to compare different data service instances
    ///     of the same type.
    /// </summary>
    public Guid InstanceGuid { get; } = Guid.NewGuid();

    /// <summary>
    ///     Minimum date value supported by this data source
    /// </summary>
    public virtual DateTime DateMinValue => DateTime.MinValue;

    /// <summary>
    ///     Maximum date value supported by this data source
    /// </summary>
    public virtual DateTime DateMaxValue => DateTime.MaxValue;

    /// <summary>
    ///     Connection status (online, offline,...)
    /// </summary>
    public virtual DataServiceConnectionStatus ConnectionStatus => DataServiceConnectionStatus.Unknown;

    /// <summary>
    ///     Returns an identifier string that allows the developer to compare two different
    ///     instances of a data service or two completely different services to see
    ///     whether they connect to the same database in the same way.
    /// </summary>
    public virtual string ServiceInstanceIdentifier => "milos.data.dataservice::non-functional (abstract) instance - " + Environment.TickCount.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    ///     This method sets the prefix used in this configuration
    /// </summary>
    /// <param name="prefix">Prefix, such as "database" or "northwind"</param>
    public virtual void SetConfigurationPrefix(string prefix) => DataConfigurationPrefix = prefix;

    /// <summary>
    ///     Abstract implementation
    /// </summary>
    /// <returns>True or False</returns>
    public abstract bool IsValid();

    /// <summary>
    ///     This property provides additional information why the status may be invalid.
    /// </summary>
    public virtual string InvalidStatus => "Unknown Invalid Status";

    /// <summary>
    ///     Abstract implementation
    /// </summary>
    /// <param name="command">Command object</param>
    /// <param name="entityName">Entity Name</param>
    /// <param name="existingDataSet"></param>
    /// <returns>DataSet</returns>
    public abstract DataSet ExecuteQuery(IDbCommand command, string entityName = "", DataSet existingDataSet = null);

    public abstract Task<DataSet> ExecuteQueryAsync(IDbCommand command, string entityName = "", DataSet existingDataSet = null);

    /// <summary>
    ///     Abstract implementation
    /// </summary>
    /// <param name="command">Command object</param>
    /// <returns>Number of affected records</returns>
    public abstract int ExecuteNonQuery(IDbCommand command);

    /// <summary>
    ///     This method executes a query and returns the number of affected rows.
    /// </summary>
    /// <param name="command">Command string (such as an SQL Insert command)</param>
    /// <param name="expectedRecordCount">Number of records we expect to be effected by this command.</param>
    /// <returns>True, if number of affected records is the same as the expected record count.</returns>
    public virtual bool ExecuteNonQuery(IDbCommand command, int expectedRecordCount) => ExecuteNonQuery(command) == expectedRecordCount;

    public abstract Task<int> ExecuteNonQueryAsync(IDbCommand command);

    /// <summary>
    ///     Abstract implementation
    /// </summary>
    /// <param name="role">Role name</param>
    /// <param name="password">App role password</param>
    /// <returns>True or False</returns>
    public abstract bool ApplyAppRole(string role, string password);

    /// <summary>
    ///     Reverts to a previous app role. This is typically done after an
    ///     app role has been applied using ApplyAppRole.
    ///     RevertAppRole simply reverts back to a previously set app role.
    ///     This is done in a FILO stack fashion.
    /// </summary>
    /// <returns>True or False</returns>
    public abstract bool RevertAppRole();

    /// <summary>
    ///     Abstract implementation
    /// </summary>
    /// <returns>True or False</returns>
    public abstract bool BeginTransaction();

    /// <summary>
    ///     Abstract implementation
    /// </summary>
    /// <returns>True or False</returns>
    public abstract bool CommitTransaction();

    /// <summary>
    ///     Abstract implementation
    /// </summary>
    /// <returns>True or False</returns>
    public abstract bool AbortTransaction();

    /// <summary>
    ///     Indicates the current transaction status.
    /// </summary>
    public virtual TransactionStatus TransactionStatus => TransactionStatus.Unknown;

    /// <summary>
    ///     Abstract implementation
    /// </summary>
    /// <param name="command">Data Command</param>
    /// <returns>Value object</returns>
    public abstract object ExecuteScalar(IDbCommand command);

    public abstract Task<object> ExecuteScalarAsync(IDbCommand command);

    public abstract T ExecuteScalar<T>(IDbCommand command);

    public abstract Task<T> ExecuteScalarAsync<T>(IDbCommand command);

    /// <summary>
    ///     Executes a stored procedure and adds the result to an existing DataSet
    /// </summary>
    /// <param name="command">Sql Command object</param>
    /// <param name="entityName">Name of the resulting entity in the DataSet</param>
    /// <param name="existingDataSet">Existing data set the data is to be added to</param>
    /// <returns>DataSet</returns>
    public abstract DataSet ExecuteStoredProcedureQuery(IDbCommand command, string entityName = "", DataSet existingDataSet = null);

    public abstract Task<DataSet> ExecuteStoredProcedureQueryAsync(IDbCommand command, string entityName = "", DataSet existingDataSet = null);

    /// <summary>
    ///     Executes a stored procedure
    /// </summary>
    /// <param name="command">Data Command object</param>
    /// <returns>True or False</returns>
    public abstract bool ExecuteStoredProcedure(IDbCommand command);

    public abstract Task<bool> ExecuteStoredProcedureAsync(IDbCommand command);

    /// <summary>Returns a database command object.</summary>
    /// <param name="commandText">Command text to be set on the new command object</param>
    /// <remarks>
    /// In an SQL Server service, this would be SqlCommand, in an OleDb service, this would be OleDbCommand, and so forth.
    /// This method is identical to NewCommand()
    /// </remarks>
    /// <returns>IDbCommand object instance</returns>
    public abstract IDbCommand NewCommandObject(string commandText = null);

    /// <summary>Returns a database command object.</summary>
    /// <param name="commandText">Command text to be set on the new command object</param>
    /// <remarks>
    /// In an SQL Server service, this would be SqlCommand, in an OleDb service, this would be OleDbCommand, and so forth.
    /// This method is identical to NewCommandObject()
    /// </remarks>
    /// <returns>IDbCommand object instance</returns>
    public virtual IDbCommand NewCommand(string commandText = null) => NewCommandObject(commandText);

    /// <summary>
    ///     Returns an instance of a parameter object that can be added
    ///     to an IDbCommand.Parameters collection.
    /// </summary>
    /// <param name="parameterName">Name of the new parameter</param>
    /// <param name="parameterValue">Value of the new parameter</param>
    /// <returns>Parameter object</returns>
    public abstract IDbDataParameter NewCommandObjectParameter(string parameterName, object parameterValue);

    /// <summary>
    ///     Generically returns the value of the specified parameter in a
    ///     generic IDbCommand object or any other object that implements that interface
    /// </summary>
    /// <param name="parameterName">Parameter Name</param>
    /// <param name="command">Command Object</param>
    /// <returns>Parameter value</returns>
    public abstract object GetCommandParameterValue(string parameterName, IDbCommand command);

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
    /// <returns>Update command that can sub-sequentially be executed against the database using the same data service.</returns>
    public abstract IDbCommand BuildUpdateCommand(DataRow changedRow, KeyType primaryKeyType, string primaryKeyFieldName, string tableName, DataRowUpdateMode updateMode = DataRowUpdateMode.ChangedFieldsOnly, DataRowProcessMethod updateMethod = DataRowProcessMethod.Default, IList<string> fieldNames = null, IDictionary<string, string> fieldMaps = null);

    /// <summary>
    ///     Creates a delete command object for the defined table and primary key.
    /// </summary>
    /// <param name="tableName">Name of the table the record is to be deleted from .</param>
    /// <param name="primaryKeyFieldName">Primary key field name within the table</param>
    /// <param name="primaryKeyValue">Primary key value for the record that is to be deleted</param>
    /// <param name="updateMethod">Method used to update the database (commands, stored procedures,...)</param>
    /// <returns>IDbCommand object that can sub-sequentially be executed against a database</returns>
    public abstract IDbCommand BuildDeleteCommand(string tableName, string primaryKeyFieldName, object primaryKeyValue, DataRowProcessMethod updateMethod = DataRowProcessMethod.Default);

    /// <summary>
    ///     Builds a command object that queries an empty record containing all fields of the specified table.
    /// </summary>
    /// <param name="tableName">Table Name</param>
    /// <param name="fieldList">List of fields to be included in the query</param>
    /// <param name="selectMethod">Select method (such as stored procedure or select commands)</param>
    /// <returns>IDbCommand object</returns>
    public abstract IDbCommand BuildEmptyRecordQueryCommand(string tableName, string fieldList = "*", DataRowProcessMethod selectMethod = DataRowProcessMethod.Default);

    /// <summary>
    ///     Builds a command object that queries all records (with specified fields) from the specified table.
    ///     Defining an order is possible as well.
    /// </summary>
    /// <param name="tableName">Name of the table to query from</param>
    /// <param name="fieldList">Fields to query (or * for all fields)</param>
    /// <param name="orderBy">Order (or empty string if no special order is desired)</param>
    /// <param name="selectMethod">Select method (such as stored procedure or select commands)</param>
    /// <returns>IDbCommand object</returns>
    public abstract IDbCommand BuildAllRecordsQueryCommand(string tableName, string fieldList = "*", string orderBy = "", DataRowProcessMethod selectMethod = DataRowProcessMethod.Default);

    /// <summary>
    /// Queries all records from the specified table
    /// </summary>
    /// <param name="tableName">Name of the table to query from</param>
    /// <param name="fieldList">Fields to query (or * for all fields)</param>
    /// <param name="orderBy">Order (or empty string if no special order is desired)</param>
    /// <param name="selectMethod">Select method (such as stored procedure or select commands)</param>
    /// <returns>DataSet with the requested data</returns>
    public virtual DataSet GetAllRecords(string tableName, string fieldList = "*", string orderBy = "", DataRowProcessMethod selectMethod = DataRowProcessMethod.Default)
    {
        using (var command = BuildAllRecordsQueryCommand(tableName, fieldList, orderBy, selectMethod))
            return ExecuteQuery(command, tableName);
    }

    /// <summary>
    /// Queries all records from the specified table
    /// </summary>
    /// <param name="tableName">Name of the table to query from</param>
    /// <param name="fieldList">Fields to query (or * for all fields)</param>
    /// <param name="orderBy">Order (or empty string if no special order is desired)</param>
    /// <param name="selectMethod">Select method (such as stored procedure or select commands)</param>
    /// <returns>DataSet with the requested data</returns>
    public virtual async Task<DataSet> GetAllRecordsAsync(string tableName, string fieldList = "*", string orderBy = "", DataRowProcessMethod selectMethod = DataRowProcessMethod.Default)
    {
        using (var command = BuildAllRecordsQueryCommand(tableName, fieldList, orderBy, selectMethod))
            return await ExecuteQueryAsync(command, tableName);
    }

    /// <summary>
    ///     Returns a single record (with a specified list of fields) by primary key.
    /// </summary>
    /// <param name="tableName">Name of the table to query.</param>
    /// <param name="fieldList">List of fields to return.</param>
    /// <param name="primaryKeyFieldName">Name of the primary key field.</param>
    /// <param name="primaryKeyValue">Primary key (value)</param>
    /// <param name="selectMethod">Select method (such as stored procedure or select commands)</param>
    /// <returns>IDbCommand object</returns>
    /// <remarks>
    ///     The name of the method is a little misleading. It can actually return more than one query *if* the key
    ///     field used for the query is non-unique (in other words: not a primary key field).
    /// </remarks>
    /// <example>
    ///     IDbCommand command = service.BuildSingleRecordQueryCommand("Customers", "*", "CustomerKey", key,
    ///     DataRowProcessMethod.Default);
    ///     DataSet data = service.ExecuteQuery(command);
    /// </example>
    public abstract IDbCommand BuildSingleRecordQueryCommand(string tableName, string fieldList = "*", string primaryKeyFieldName = "Id", object primaryKeyValue = null, DataRowProcessMethod selectMethod = DataRowProcessMethod.Default);

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
    public virtual DataSet GetSingleRecord(string tableName, string fieldList = "*", string primaryKeyFieldName = "Id", object primaryKeyValue = null, DataRowProcessMethod selectMethod = DataRowProcessMethod.Default)
    {
        using (var command = BuildSingleRecordQueryCommand(tableName, fieldList, primaryKeyFieldName, primaryKeyValue, selectMethod))
            return ExecuteQuery(command, tableName);
    }

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
    public virtual async Task<DataSet> GetSingleRecordAsync(string tableName, string fieldList = "*", string primaryKeyFieldName = "Id", object primaryKeyValue = null, DataRowProcessMethod selectMethod = DataRowProcessMethod.Default)
    {
        using (var command = BuildSingleRecordQueryCommand(tableName, fieldList, primaryKeyFieldName, primaryKeyValue, selectMethod))
            return await ExecuteQueryAsync(command, tableName);
    }

    /// <summary>
    ///     Builds a command that returns a set of records based on the provided field names and filter parameters.
    /// </summary>
    /// <param name="tableName">Name of the table.</param>
    /// <param name="fieldList">The list of fields returned by the query (ignored for stored procedure execution)</param>
    /// <param name="fieldNames">The list of fields by which to filter</param>
    /// <param name="filterParameters">
    ///     Parameters used for filtering. The parameters need to match the list of filter fields
    ///     (name and types)
    /// </param>
    /// <param name="selectMethod">Process method for the select method</param>
    /// <returns>IDbCommand object representing the query</returns>
    /// <example>
    ///     string[] fieldNames = new string[] { "FirstName", "LastName", "IsActive" };
    ///     object[] parameters = new object[] { "Chris", "Pronger", true };
    ///     IDbCommand command = service.BuildQueryCommand("Customers", "*", fieldNames, parameters,
    ///     DataRowProcessMethod.Default);
    ///     DataSet data = service.ExecuteQuery(command);
    /// </example>
    public abstract IDbCommand BuildQueryCommand(string tableName, string fieldList, IList<string> fieldNames = null, IList<object> filterParameters = null, DataRowProcessMethod selectMethod = DataRowProcessMethod.Default);

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
    public virtual DataSet Query(string tableName, string fieldList, IList<string> fieldNames, IList<object> filterParameters, DataRowProcessMethod selectMethod = DataRowProcessMethod.Default)
    {
        using (var command = BuildQueryCommand(tableName, fieldList, fieldNames, filterParameters, selectMethod))
            return ExecuteQuery(command);
    }

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
    public virtual async Task<DataSet> QueryAsync(string tableName, string fieldList, IList<string> fieldNames, IList<object> filterParameters, DataRowProcessMethod selectMethod = DataRowProcessMethod.Default)
    {
        using (var command = BuildQueryCommand(tableName, fieldList, fieldNames, filterParameters, selectMethod))
            return await ExecuteQueryAsync(command);
    }

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
    public virtual DataSet Query(IDbCommand command, string entityName = "") => ExecuteQuery(command, entityName);

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
    public virtual async Task<DataSet> QueryAsync(IDbCommand command, string entityName = "") => await ExecuteQueryAsync(command, entityName);

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
    public virtual DataSet Query(string command, Dictionary<string, object> parameters = null, string entityName = "")
    {
        using (var cmd = NewCommand())
        {
            cmd.CommandText = command;

            if (parameters != null)
                foreach (var key in parameters.Keys)
                {
                    var parameter = NewCommandObjectParameter(key, parameters[key]);
                    cmd.Parameters.Add(parameter);
                }

            return ExecuteQuery(cmd, entityName);
        }
    }

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
    public virtual async Task<DataSet> QueryAsync(string command, Dictionary<string, object> parameters = null, string entityName = "")
    {
        using (var cmd = NewCommand())
        {
            cmd.CommandText = command;

            if (parameters != null)
                foreach (var key in parameters.Keys)
                {
                    var parameter = NewCommandObjectParameter(key, parameters[key]);
                    cmd.Parameters.Add(parameter);
                }

            return await ExecuteQueryAsync(cmd, entityName);
        }
    }

    /// <summary>This event fires whenever a query has been completed</summary>
    public event EventHandler<QueryEventArgs> QueryComplete;

    /// <summary>This event fires whenever a scalar query has been completed</summary>
    public event EventHandler<ScalarEventArgs> ScalarQueryComplete;

    /// <summary>This event fires whenever a non-query database command has been completed</summary>
    public event EventHandler<NonQueryEventArgs> NonQueryComplete;

    /// <summary>
    ///     Implementation of IDisposable, in particular the Dispose() method
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public virtual async Task<bool> ExecuteNonQueryAsync(IDbCommand command, int expectedRecordCount) => await ExecuteNonQueryAsync(command) == expectedRecordCount;

    /// <summary>
    ///     This method raises the QueryComplete event
    /// </summary>
    /// <param name="resultDataSet">DataSet returned by the query</param>
    /// <param name="executedCommand">This command lead to the provided result data set</param>
    /// <param name="entityName">Name of the last queried entity</param>
    protected virtual void RaiseOnQueryCompleteEvent(DataSet resultDataSet, IDbCommand executedCommand, string entityName) => QueryComplete?.Invoke(this, new QueryEventArgs(resultDataSet, executedCommand, entityName));

    /// <summary>
    ///     This method raises the ScalarQueryComplete event
    /// </summary>
    /// <param name="executedCommand">This command lead to the provided result data set</param>
    /// <param name="result">Scalar query result</param>
    protected virtual void RaiseOnScalarQueryCompleteEvent(IDbCommand executedCommand, object result) => ScalarQueryComplete?.Invoke(this, new ScalarEventArgs(executedCommand, result));

    /// <summary>
    ///     This method raises the NonQueryComplete event
    /// </summary>
    /// <param name="executedCommand">This command lead to the provided result data set</param>
    /// <param name="affectedRecords">Number of records affected by the non-query command</param>
    protected virtual void RaiseOnNonQueryCompleteEvent(IDbCommand executedCommand, int affectedRecords) => NonQueryComplete?.Invoke(this, new NonQueryEventArgs(executedCommand, affectedRecords));

    /// <summary>
    ///     Protected dispose method that can be overridden in subclasses
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing) { }

    public DataSet GetList(string tableName, string fieldList = "*", string sortOrder = "", DataRowProcessMethod selectMethod = DataRowProcessMethod.Default)
    {
        using (var selectCommand = BuildAllRecordsQueryCommand(tableName, fieldList, sortOrder, selectMethod))
            return ExecuteQuery(selectCommand, tableName);
    }

    public async Task<DataSet> GetListAsync(string tableName, string fieldList = "*", string sortOrder = "", DataRowProcessMethod selectMethod = DataRowProcessMethod.Default)
    {
        using (var selectCommand = BuildAllRecordsQueryCommand(tableName, fieldList, sortOrder, selectMethod))
            return await ExecuteQueryAsync(selectCommand, tableName);
    }

    #region IStoredProcedureFacadeService Members

    ///// <summary>
    /////     Internal reference for a stored procedure facade.
    ///// </summary>
    //private IStoredProcedureFacade _facade;

    ///// <summary>
    /////     Reference to a stored procedure facade
    ///// </summary>
    //public virtual IStoredProcedureFacade StoredProcedureFacade
    //{
    //    get
    //    {
    //        if (_facade != null) return _facade;
    //        // No facade has been loaded. We check whether there is a setting that tells us what to load
    //        var configurationSetting = DataConfigurationPrefix + ":" + "StoredProcedureFacade";
    //        var facade = string.Empty;
    //        if (ConfigurationSettings.Settings.IsSettingSupported(configurationSetting)) facade = ConfigurationSettings.Settings[configurationSetting];
    //        if (facade.Length <= 0) return _facade;
    //        // There is a setting, so we may be able to load a facade
    //        try
    //        {
    //            var parts = facade.Split('/');
    //            var assembly = parts[0];
    //            var type = parts[1];
    //            try
    //            {
    //                _facade = (IStoredProcedureFacade) ObjectHelper.CreateObject(type, assembly);
    //                _facade.DataService = this;
    //            }
    //            catch (Exception ex)
    //            {
    //                throw new FacadeInstantiationException(Resources.UnableToInstantiateSpecificFacade + facade, ex);
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            throw new FacadeInstantiationException(Resources.IncorrectFacadeSetting + facade, ex);
    //        }

    //        return _facade;
    //    }
    //    set => _facade = value;
    //}

    #endregion
}