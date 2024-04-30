using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Data.SqlTypes;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using CODE.Framework.Fundamentals.Configuration;

namespace Milos.Data.SqlServer;

/// <summary>
/// This class can be used to connect to SQL Server and retrieve data
/// or run commands against SQL.
/// </summary>
public class SqlDataService : DataService
{
    /// <summary>
    /// For internal use only (app role filo stack)
    /// </summary>
    private readonly List<AppRoleStackItem> appRoleStack = [];

    /// <summary>
    /// Defines which data access modes are allowed in the current scenario.
    /// Example: If the allowed mode is "StoredProcedures" and someone tries
    /// to execute an individual SELECT command, the data service will not
    /// execute the call.
    /// </summary>
    private AllowedDataAccessMethod allowedDataAccessMethod = AllowedDataAccessMethod.Undefined;

    /// <summary>
    /// Should schema information be automatically retrieved from the database?
    /// </summary>
    private bool autoRetrieveDatabaseSchema = true;

    /// <summary>
    /// For internal use only
    /// </summary>
    private bool autoRetrieveDatabaseSchemaRead;

    /// <summary>
    /// Application Role name that is to be set on the connection 
    /// if no other settings are specified.
    /// </summary>
    private string defaultAppRole;

    /// <summary>
    /// Password for the default application role
    /// </summary>
    private string defaultAppRolePassword;

    /// <summary>
    /// Internal object reference to the current connection.
    /// </summary>
    private SqlConnection directConnection;

    /// <summary>
    /// For internal use only
    /// </summary>
    private string invalidStatus = string.Empty;

    /// <summary>
    /// Stores the minimum timeout for a command
    /// </summary>
    private int minimumCommandTimeout = -1;

    /// <summary>
    /// For internal use only
    /// </summary>
    private SqlServerParameterType parameterType = SqlServerParameterType.Unknown;

    /// <summary>
    /// Default transaction isolation level
    /// </summary>
    private string transactionIsolationLevel = string.Empty;

    /// <summary>
    /// This service is a direct connection to SQL Server over a LAN
    /// </summary>
    public override DataServiceConnectionStatus ConnectionStatus => DataServiceConnectionStatus.OnlineLAN;

    /// <summary>
    /// Indicates the current transaction status.
    /// </summary>
    public override TransactionStatus TransactionStatus => InTransaction ? TransactionStatus.TransactionInProgress : TransactionStatus.Unknown;

    /// <summary>
    /// Minimum date value supported by this data source
    /// </summary>
    public override DateTime DateMinValue => SqlDateTime.MinValue.Value;

    /// <summary>
    /// Maximum date value supported by this data source
    /// </summary>
    public override DateTime DateMaxValue => SqlDateTime.MaxValue.Value;

    // The following are some properties for internal use
    /// <summary>
    /// This property returns a valid and open connection to the current data source
    /// </summary>
    protected SqlConnection Connection
    {
        get
        {
            // We make sure we have a connection
            if (directConnection == null)
            {
                if (string.IsNullOrEmpty(ConnectionString))
                {
                    if (TrustedConnection)
                        ConnectionString = $"initial catalog={Catalog};server={Server};Integrated Security=SSPI";
                    else
                        ConnectionString = $"user id={UserName};password={Password};initial catalog={Catalog};server={Server}";

                    if (!string.IsNullOrEmpty(CurrentAppRole))
                        // If we have an app role in place, we can not use connection pooling
                        ConnectionString += ";pooling=false";
                }

                directConnection = new SqlConnection(ConnectionString);
                directConnection.Open();
                RegisterCurrentAppRoleOnServer(directConnection); // May be needed if an app role is set
            }

            // We make sure the connection is open
            if (directConnection.State == ConnectionState.Closed)
            {
                // If an app role is applied, we need to make sure pooling is off.
                if (!string.IsNullOrEmpty(CurrentAppRole))
                {
                    // An app role is assigned
                    if (ConnectionString != null && ConnectionString.ToLowerInvariant().IndexOf("pooling=false", StringComparison.Ordinal) <= 0)
                        ConnectionString += ";pooling=false";
                }
                else
                {
                    // No app role exists. We can use pooling.
                    if (ConnectionString != null && ConnectionString.IndexOf(";pooling=false", StringComparison.Ordinal) > 0)
                        ConnectionString = ConnectionString.Replace(";pooling=false", string.Empty);
                }

                // OK, ready to go.
                directConnection.ConnectionString = ConnectionString; // We re-assign this in case something in this service changed the string (such as an assigned app role)
                directConnection.Open();
                RegisterCurrentAppRoleOnServer(directConnection); // May be needed if an app role is set
            }

            return directConnection;
        }
        set => directConnection = value;
    }

    /// <summary>
    /// Last error that occured.
    /// Note: Under normal circumstances, this property should not be written to.
    /// </summary>
    public string LastError { get; set; }

    /// <summary>
    /// Defines which data access modes are allowed in the current scenario.
    /// Example: If the allowed mode is "StoredProcedures" and someone tries
    /// to execute an individual SELECT command, the data service will not
    /// execute the call.
    /// </summary>
    protected AllowedDataAccessMethod AllowedDataMethod
    {
        get
        {
            if (allowedDataAccessMethod == AllowedDataAccessMethod.Undefined)
            {
                // We load the setting from the configuration file (if it is there)
                var configMethod = string.Empty;
                if (ConfigurationSettings.Settings.IsSettingSupported(DataConfigurationPrefix + ":AllowedDataMethod"))
                {
                    configMethod = ConfigurationSettings.Settings[DataConfigurationPrefix + ":AllowedDataMethod"];
                    configMethod = configMethod.ToLowerInvariant().Trim().Replace(" ", string.Empty);
                }

                if (string.IsNullOrEmpty(configMethod))
                    // Nothing set. We allow for everything!
                    allowedDataAccessMethod = AllowedDataAccessMethod.All;
                else
                    allowedDataAccessMethod = configMethod switch
                    {
                        "storedprocedures" => AllowedDataAccessMethod.StoredProceduresOnly,
                        "individualcommands" => AllowedDataAccessMethod.IndividualCommandsOnly,
                        _ => AllowedDataAccessMethod.All,
                    };
            }

            return allowedDataAccessMethod;
        }
    }

    /// <summary>User name used to connect to SQL Server (generally configured in app.config)</summary>
    protected string UserName { get; set; }

    /// <summary>Password used to connect to SQL Server (generally configured in app.config)</summary>
    protected string Password { get; set; }

    /// <summary>SQL Server server name (generally configured in app.config)</summary>
    protected string Server { get; set; }

    /// <summary>SQL Server database (generally configured in app.config)</summary>
    protected string Catalog { get; set; }

    /// <summary>Does this service use a trusted connection?</summary>
    protected bool TrustedConnection { get; set; }

    /// <summary>
    /// Current AppRole that needs to be set on all connections
    /// </summary>
    protected string CurrentAppRole { get; private set; }

    /// <summary>
    /// Password associated with the current app role
    /// </summary>
    protected string CurrentAppRolePassword { get; private set; }

    /// <summary>
    /// Default AppRole that needs to be set on all connections,
    /// when no specific app role is specified
    /// </summary>
    protected string DefaultAppRole
    {
        get
        {
            if (defaultAppRole == null)
            {
                defaultAppRole = string.Empty;
                if (ConfigurationSettings.Settings.IsSettingSupported(DataConfigurationPrefix + ":AppRole"))
                    defaultAppRole = ConfigurationSettings.Settings[DataConfigurationPrefix + ":AppRole"];
            }

            return defaultAppRole;
        }
    }

    /// <summary>
    /// Password associated with the default app role
    /// </summary>
    protected string DefaultAppRolePassword
    {
        get
        {
            if (defaultAppRolePassword == null)
            {
                defaultAppRolePassword = string.Empty;
                if (ConfigurationSettings.Settings.IsSettingSupported(DataConfigurationPrefix + ":AppRolePassword"))
                    defaultAppRolePassword = ConfigurationSettings.Settings[DataConfigurationPrefix + ":AppRolePassword"];
            }

            return defaultAppRolePassword;
        }
    }

    /// <summary>
    /// Defines whether schema information is automatically retrieved from the database.
    /// Note: Schema retrieval slows down data access!
    /// </summary>
    protected bool AutoRetrieveDatabaseSchema
    {
        get
        {
            if (!autoRetrieveDatabaseSchemaRead)
            {
                if (ConfigurationSettings.Settings.IsSettingSupported(DataConfigurationPrefix + ":AutoLoadSchemaInformation"))
                {
                    var autoLoadSchemaInformation = ConfigurationSettings.Settings[DataConfigurationPrefix + ":AutoLoadSchemaInformation"].ToLowerInvariant();
                    autoRetrieveDatabaseSchema = autoLoadSchemaInformation == "true";
                }

                autoRetrieveDatabaseSchemaRead = true;
            }

            return autoRetrieveDatabaseSchema;
        }
    }

    /// <summary>
    /// Defines the prefix for all default stored procedure operations.
    /// For instance, instead of calling a SP "getCustomerAllRecords",
    /// the stored procedure (which is generated automatically), can 
    /// be called "milos_getCustomerAllRecords" to differentiate between
    /// automatically generated stored procedures, and manually created ones.
    /// </summary>
    protected string DefaultStoredProcedurePrefix { get; set; } = "milos_";

    /// <summary>
    /// Could be used as an alternative to the individual settings provided by the individual fields. 
    /// If a connection string is provided, it will overwrite the individual settings.
    /// </summary>
    protected string ConnectionString { get; set; }

    /// <summary>
    /// Reference to a potentially active transaction.
    /// (For internal use only)
    /// </summary>
    protected SqlTransaction CurrentTransaction { get; set; }

    /// <summary>
    /// Returns whether or not the service is currently in a transaction
    /// </summary>
    protected bool InTransaction => CurrentTransaction != null;

    /// <summary>
    /// Minimum command timeout
    /// </summary>
    public int MinimumCommandTimeout
    {
        get
        {
            if (minimumCommandTimeout == -1)
            {
                if (ConfigurationSettings.Settings.IsSettingSupported("MinimumCommandTimeout"))
                    try
                    {
                        minimumCommandTimeout = int.Parse(ConfigurationSettings.Settings["MinimumCommandTimeout"], CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        minimumCommandTimeout = 0;
                    }
                else
                    minimumCommandTimeout = 0;
            }

            return minimumCommandTimeout;
        }
        set => minimumCommandTimeout = value;
    }

    /// <summary>
    /// Returns an identifier string that allows the developer to compare two different
    /// instances of a data service or two completely different services to see
    /// whether they connect to the same database in the same way.
    /// </summary>
    /// <value></value>
    public override string ServiceInstanceIdentifier => $"SqlDataService - Server: {Server} - Catalog: {Catalog}";

    /// <summary>
    /// This property provides additional information why the status may be invalid.
    /// </summary>
    public override string InvalidStatus => invalidStatus;

    /// <summary>
    /// Do NOT use this method, unless you are absolutely sure you understand what it does!!!
    /// </summary>
    /// <param name="retrieveSchema">For internal use only</param>
    public void ManualAutoRetrieveSchemaOverride(bool retrieveSchema)
    {
        autoRetrieveDatabaseSchemaRead = true;
        autoRetrieveDatabaseSchema = retrieveSchema;
    }

    /// <summary>
    /// This method sets the prefix used in this configuration
    /// </summary>
    /// <param name="prefix">Prefix, such as "database" or "northwind"</param>
    public override void SetConfigurationPrefix(string prefix)
    {
        base.SetConfigurationPrefix(prefix);
        UpdateInternalConfiguration();
    }

    /// <summary>
    /// Refreshes internal configuration settings
    /// </summary>
    protected void UpdateInternalConfiguration()
    {
        // We check if we have a trusted connection
        if (ConfigurationSettings.Settings.IsSettingSupported(DataConfigurationPrefix + ":TrustedConnection"))
        {
            var trustedConnection = ConfigurationSettings.Settings[DataConfigurationPrefix + ":TrustedConnection"].ToLowerInvariant();
            TrustedConnection = trustedConnection == "yes" || trustedConnection == "true";
        }
        else
        {
            TrustedConnection = false;
        }

        // If we did NOT have a trusted connection, then we have to find user settings or a connection string
        if (!TrustedConnection)
        {
            if (ConfigurationSettings.Settings.IsSettingSupported(DataConfigurationPrefix + ":UserName"))
                UserName = ConfigurationSettings.Settings[DataConfigurationPrefix + ":UserName"];
            if (ConfigurationSettings.Settings.IsSettingSupported(DataConfigurationPrefix + ":Password"))
                Password = ConfigurationSettings.Settings[DataConfigurationPrefix + ":Password"];
            if (ConfigurationSettings.Settings.IsSettingSupported(DataConfigurationPrefix + ":ConnectionString"))
                ConnectionString = ConfigurationSettings.Settings[DataConfigurationPrefix + ":ConnectionString"];
        }

        // We also find some server settings
        if (ConfigurationSettings.Settings.IsSettingSupported(DataConfigurationPrefix + ":Server"))
            Server = ConfigurationSettings.Settings[DataConfigurationPrefix + ":Server"];
        if (ConfigurationSettings.Settings.IsSettingSupported(DataConfigurationPrefix + ":Catalog"))
            Catalog = ConfigurationSettings.Settings[DataConfigurationPrefix + ":Catalog"];

        // We look for stored proc specific settings
        if (ConfigurationSettings.Settings.IsSettingSupported(DataConfigurationPrefix + ":StoredProcedurePrefix"))
        {
            var storedProcedurePrefix = ConfigurationSettings.Settings[DataConfigurationPrefix + ":StoredProcedurePrefix"];
            if (!string.IsNullOrEmpty(storedProcedurePrefix))
                DefaultStoredProcedurePrefix = storedProcedurePrefix;
        }

        // We make sure we have the required configuration options
        if (!string.IsNullOrEmpty(ConnectionString))
        {
            if (!TrustedConnection)
            {
                if (string.IsNullOrEmpty(UserName))
                    throw new MissingDataConfigurationException(DataConfigurationPrefix + ":UserName");
                if (string.IsNullOrEmpty(Password))
                    throw new MissingDataConfigurationException(DataConfigurationPrefix + ":Password");
            }

            if (string.IsNullOrEmpty(Server))
                throw new MissingDataConfigurationException(DataConfigurationPrefix + ":Server");
            if (string.IsNullOrEmpty(Catalog))
                throw new MissingDataConfigurationException(DataConfigurationPrefix + ":Catalog");
        }
    }

    /// <summary>
    /// This method is used to set an application role
    /// </summary>
    /// <param name="role">Role Name</param>
    /// <param name="password">Password</param>
    /// <returns>True or False depending on whether the method proceeds</returns>
    /// <remarks>If this method is called with an empty string as the app role, the system's configured default app role will apply. If no global app role is configured, the system will un-apply the current role if needed.</remarks>
    public override bool ApplyAppRole(string role, string password)
    {
        // If the app role is empty, we go to the default role
        if (role.Length == 0)
        {
            role = DefaultAppRole;
            password = DefaultAppRolePassword;
        }

        // If the app role is different from a previously set role,
        // we need to terminate the old connection
        if (CurrentAppRole != role && CurrentAppRolePassword != password)
            // We check if there is an open connection.
            // Note: If we are in a transaction, app roles can not be changed! 
            //       The actual change will happen when the transaction is complete.
            //       This happens automatically, since the connection will be closed,
            //       whenever a transaction is completed.
            if (directConnection != null && directConnection.State == ConnectionState.Open && !InTransaction)
                // We close the connection. The next time the connection is used,
                // it will be re-opened automatically, and the new app role
                // will be applied.
                directConnection.Close();
        // We memorize the role for future use
        CurrentAppRole = role;
        CurrentAppRolePassword = password;

        // We also add this to the stack of app roles (but only,
        // if this is a valid setting and other settings already exist,
        // otherwise, app roles are not really executed. In those cases,
        // we are in a role-less environment (which is a likely scenario)
        if (appRoleStack.Count > 0 || role.Length > 0)
            appRoleStack.Add(new AppRoleStackItem {Role = role, Password = password});

        return true;
    }

    /// <summary>
    /// Reverts to a previous app role. This is typically done after an 
    /// app role has been applied using ApplyAppRole.
    /// RevertAppRole simply reverts back to a previously set app role.
    /// This is done in a FILO stack fashion.
    /// </summary>
    /// <returns>True or False</returns>
    public override bool RevertAppRole()
    {
        string role;
        string password;

        // We check whether we can revert anything
        if (appRoleStack.Count > 1)
        {
            // We have more than one item, so we can revert to the previous one
            // Before we do so, we remove the most recent (current) one, since
            // we do not need it anymore, and want the previous one to be the most current one.
            appRoleStack.RemoveAt(appRoleStack.Count - 1);
            // We now get the latest role name and pw
            var stackItem = appRoleStack[appRoleStack.Count - 1];
            role = stackItem.Role;
            password = stackItem.Password;
        }
        else
        {
            // There really isn't any previous history to revert to, so we revert to a default state.

            // Note: It is unlikely that this method would be called with 0 items on the stack,
            //       since the DataServiceFactory generally causes a default to be set (generally
            //       string.Empty unless a configuration file specified a different setting).

            // If there is a single item in the stack, we just get rid of it
            if (appRoleStack.Count == 1)
                appRoleStack.RemoveAt(0);

            // The role and password need to be defaulted at this point
            role = DefaultAppRole;
            password = DefaultAppRolePassword;
        }

        // Note: From this point on, this method is similar to ApplyAppRole(),
        //       except that nothing is added to the stack since we are in 
        //       revert mode.

        // If the app role is different from a previously set role,
        // we need to terminate the old connection
        if (CurrentAppRole != role && CurrentAppRolePassword != password)
            // We check if there is an open connection
            // Note: If we are in a transaction, app roles can not be changed! 
            //       The actual change will happen when the transaction is complete.
            //       This happens automatically, since the connection will be closed,
            //       whenever a transaction is completed.
            if (directConnection != null && directConnection.State == ConnectionState.Open && !InTransaction)
                // We close the connection. The next time the connection is used, it will be re-opened automatically, and the new app role will be applied.
                directConnection.Close();
        // We memorize the role for future use
        CurrentAppRole = role;
        CurrentAppRolePassword = password;

        return true;
    }

    /// <summary>
    /// Assigns the current app role on SQL Server
    /// </summary>
    /// <param name="currentConnection">The connection the app role is to be set on</param>
    /// <returns>True or False</returns>
    protected virtual bool RegisterCurrentAppRoleOnServer(SqlConnection currentConnection)
    {
        if (!string.IsNullOrEmpty(CurrentAppRole))
        {
            // We have a role setting, so we apply it
            var applyRoleCommand = new SqlCommand("sp_setapprole") {CommandType = CommandType.StoredProcedure};
            applyRoleCommand.Parameters.AddWithValue("@rolename", CurrentAppRole);
            applyRoleCommand.Parameters.AddWithValue("@password", CurrentAppRolePassword);

            // We are now ready to execute the SP
            // Note: We can NOT use this.ExecuteScalar() for this, since this could
            //       cause a cyclic call to the Connection property and ultimately
            //       to this very method. Also, we want to be able to execute this SP
            //       even if ExecuteScalar() was configured to not allow SPs.
            applyRoleCommand.Connection = currentConnection;
            try
            {
                applyRoleCommand.ExecuteScalar();
                applyRoleCommand.Connection = null;
                applyRoleCommand.Dispose();
                return true;
            }
            catch
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Aborts a transaction and reverts all changes in the database
    /// </summary>
    /// <returns>True of False depending on the success of the operation</returns>
    public override bool AbortTransaction()
    {
        if (!InTransaction)
        {
            // No transaction is active. There is nothing to abort!
            LastError = "There is no active transaction that could be aborted.";
            return false;
        }

        // We are ready to abort the transaction
        var returnValue = false;
        try
        {
            CurrentTransaction.Rollback();
            returnValue = true;
        }
        catch (InvalidOperationException oEx)
        {
            // Failed to abort the transaction
            LastError = oEx.Message;
        }

        // We do not need this transaction anymore
        CurrentTransaction.Dispose();
        CurrentTransaction = null;

        // We can now also close the connection to the database
        CloseConnection();

        return returnValue;
    }

    /// <summary>
    /// Begins a transaction using default SQL transaction logic.
    /// </summary>
    /// <returns>True of False depending on the success of the operation</returns>
    public override bool BeginTransaction()
    {
        // There can only be a single transaction at a time on this service.
        // We check whether a transaction exists, and if so, the method returns false.
        if (InTransaction)
        {
            // We are already in a transaction
            LastError = "This data service only supports one transaction at a time.";
            return false;
        }

        // To begin a transaction, we first need to make sure we have a
        // live connection that we can use to initiate that transaction.
        // We also need to make sure that that connection will not get 
        // closed until the transaction is completed.
        //SqlConnection currentConnection = this.Connection;
        SqlTransaction currentTransaction;
        try
        {
            if (transactionIsolationLevel.Length == 0) transactionIsolationLevel = ConfigurationSettings.Settings.IsSettingSupported(DataConfigurationPrefix + ":TransactionIsolationLevel") ? ConfigurationSettings.Settings[DataConfigurationPrefix + ":TransactionIsolationLevel"].ToLowerInvariant() : "default";

            switch (transactionIsolationLevel)
            {
                case "chaos":
                    currentTransaction = Connection.BeginTransaction(IsolationLevel.Chaos);
                    break;
                case "readcommitted":
                    currentTransaction = Connection.BeginTransaction(IsolationLevel.ReadCommitted);
                    break;
                case "readuncommitted":
                    currentTransaction = Connection.BeginTransaction(IsolationLevel.ReadUncommitted);
                    break;
                case "repeatableread":
                    currentTransaction = Connection.BeginTransaction(IsolationLevel.RepeatableRead);
                    break;
                case "serializable":
                    currentTransaction = Connection.BeginTransaction(IsolationLevel.Serializable);
                    break;
                case "unspecified":
                    currentTransaction = Connection.BeginTransaction(IsolationLevel.Unspecified);
                    break;
                default:
                    currentTransaction = Connection.BeginTransaction();
                    break;
            }
        }
        catch (InvalidOperationException oEx)
        {
            // Failed to initialize the transaction
            LastError = oEx.Message;
            return false;
        }

        // We store the transaction for later use.
        CurrentTransaction = currentTransaction;

        return true;
    }

    /// <summary>
    /// Commits a transaction (commits changes to the database)
    /// </summary>
    /// <returns>True of False depending on the success of the operation</returns>
    public override bool CommitTransaction()
    {
        if (!InTransaction)
        {
            // No transaction is active. There is nothing to commit!
            LastError = "There is no active transaction that could be committed.";
            return false;
        }

        // We are ready to commit the transaction
        var returnValue = false;
        try
        {
            CurrentTransaction.Commit();
            returnValue = true;
        }
        catch (InvalidOperationException oEx)
        {
            // Failed to commit the transaction
            LastError = oEx.Message;
        }

        // We do not need this transaction anymore
        CurrentTransaction.Dispose();
        CurrentTransaction = null;

        // We can now also close the connection to the database
        CloseConnection();

        return returnValue;
    }

    /// <summary>
    /// We access the current SQL Server connection and check whether it's status is "open".
    /// We do this through the Connection property, which should take care of connecting to the 
    /// database, even if the connection hasn't been established before.
    /// </summary>
    /// <returns>True or False</returns>
    public override bool IsValid()
    {
        try
        {
            invalidStatus = string.Empty;
            var connection = Connection;
            if (connection.State != ConnectionState.Open)
            {
                invalidStatus = "The connection to SQL Server failed for an unknown reason. Make sure you specified the right server and database name, as well as the appropriate logon credentials.";
                return false;
            }
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            invalidStatus = ex.Message;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Executes an Sql Command and returns the number of affected rows.
    /// </summary>
    /// <param name="command">Sql Command object</param>
    public override async Task<int> ExecuteNonQueryAsync(IDbCommand command)
    {
        if (command is not SqlCommand sqlCommand) throw new UnsupportedCommandObjectException("SqlCommand expected.");

        // We check whether the execution method of this command object conforms to allowable settings
        if (AllowedDataMethod != AllowedDataAccessMethod.All)
        {
            // There are special settings! We need to verify things are OK!
            if (command.CommandType == CommandType.StoredProcedure && AllowedDataMethod != AllowedDataAccessMethod.StoredProceduresOnly)
                // The command type is a stored proc, but stored procedures are not allowed!
                throw new UnsupportedProcessMethodException("Stored Procedures are not a valid data access method based on the current system configuration!");
            if (command.CommandType == CommandType.Text && AllowedDataMethod != AllowedDataAccessMethod.IndividualCommandsOnly)
                // The command type is a text command, but individual text commands are not allowed!
                throw new UnsupportedProcessMethodException("Individual text commands are not a valid data access method based on the current system configuration!");
        }

        // This is an SqlCommand object. We are ready to go.
        PrepareCommandObject(sqlCommand);

        var affectedRows = await sqlCommand.ExecuteNonQueryAsync();

        // Some cleanup work to make sure we do not have any dangling references
        CleanCommandObject(sqlCommand);
        CloseConnection();

        // We raise an event
        RaiseOnNonQueryCompleteEvent(sqlCommand, affectedRows);

        return affectedRows;
    }

    /// <summary>
    /// Executes an Sql Command and returns the number of affected rows.
    /// </summary>
    /// <param name="command">Sql Command object</param>
    public override int ExecuteNonQuery(IDbCommand command)
    {
        if (command is not SqlCommand sqlCommand) throw new UnsupportedCommandObjectException("SqlCommand expected.");

        // We check whether the execution method of this command object conforms to allowable settings
        if (AllowedDataMethod != AllowedDataAccessMethod.All)
        {
            // There are special settings! We need to verify things are OK!
            if (command.CommandType == CommandType.StoredProcedure && AllowedDataMethod != AllowedDataAccessMethod.StoredProceduresOnly)
                // The command type is a stored proc, but stored procedures are not allowed!
                throw new UnsupportedProcessMethodException("Stored Procedures are not a valid data access method based on the current system configuration!");
            if (command.CommandType == CommandType.Text && AllowedDataMethod != AllowedDataAccessMethod.IndividualCommandsOnly)
                // The command type is a text command, but individual text commands are not allowed!
                throw new UnsupportedProcessMethodException("Individual text commands are not a valid data access method based on the current system configuration!");
        }

        // This is an SqlCommand object. We are ready to go.
        PrepareCommandObject(sqlCommand);

        var affectedRows = sqlCommand.ExecuteNonQuery();

        // Some cleanup work to make sure we do not have any dangling references
        CleanCommandObject(sqlCommand);
        CloseConnection();

        // We raise an event
        RaiseOnNonQueryCompleteEvent(sqlCommand, affectedRows);

        return affectedRows;
    }

    /// <summary>
    /// Executes a query and returns a DataSet
    /// </summary>
    /// <param name="command">Sql Command object</param>
    /// <param name="entityName">Entity Name (name of the table added to the DataSet)</param>
    /// <param name="existingDataSet">Existing data set to add the data to</param>
    /// <returns>DataSet</returns>
    /// <exception>Throws ArgumentNullException if null existingDataSet is passed.</exception>
    public override async Task<DataSet> ExecuteQueryAsync(IDbCommand command, string entityName = "", DataSet existingDataSet = null)
    {
        // We grab the command object and verify that it is an SQLCommand
        if (command is not SqlCommand sqlCommand) throw new UnsupportedCommandObjectException("SqlCommand expected.");

        existingDataSet ??= new DataSet {Locale = CultureInfo.InvariantCulture};

        // We check whether the execution method of this command object conforms to allowable settings
        if (AllowedDataMethod != AllowedDataAccessMethod.All)
        {
            // There are special settings! We need to verify things are OK!
            if (command.CommandType == CommandType.StoredProcedure && AllowedDataMethod != AllowedDataAccessMethod.StoredProceduresOnly)
                // The command type is a stored proc, but stored procedures are not allowed!
                throw new UnsupportedProcessMethodException("Stored Procedures are not a valid data access method based on the current system configuration!");
            if (command.CommandType == CommandType.Text && AllowedDataMethod != AllowedDataAccessMethod.IndividualCommandsOnly)
                // The command type is a text command, but individual text commands are not allowed!
                throw new UnsupportedProcessMethodException("Individual text commands are not a valid data access method based on the current system configuration!");
        }

        // This is an SqlCommand object. We are ready to go.
        PrepareCommandObject(sqlCommand);

        // Since there is no intrinsic way of filling a DataSet async, we do it manually
        return await Task.Run(() =>
                              {
                                  using (var sqlDataAdapter = new SqlDataAdapter(sqlCommand))
                                  {
                                      if (AutoRetrieveDatabaseSchema) sqlDataAdapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;
                                      if (!string.IsNullOrEmpty(entityName))
                                          sqlDataAdapter.Fill(existingDataSet, entityName);
                                      else
                                          sqlDataAdapter.Fill(existingDataSet);
                                  }

                                  // We fire an event
                                  RaiseOnQueryCompleteEvent(existingDataSet, sqlCommand, entityName);

                                  // We do not want to keep the connection attached to the command, since we do not know how long the command will stay around...
                                  CleanCommandObject(sqlCommand);
                                  CloseConnection();
                                  return existingDataSet;
                              });
    }

    /// <summary>
    /// Executes a query and returns a DataSet
    /// </summary>
    /// <param name="command">Sql Command object</param>
    /// <param name="entityName">Entity Name (name of the table added to the DataSet)</param>
    /// <param name="existingDataSet">Existing data set to add the data to</param>
    /// <returns>DataSet</returns>
    /// <exception>Throws ArgumentNullException if null existingDataSet is passed.</exception>
    public override DataSet ExecuteQuery(IDbCommand command, string entityName = "", DataSet existingDataSet = null)
    {
        // We grab the command object and verify that it is an SQLCommand
        if (!(command is SqlCommand sqlCommand)) throw new UnsupportedCommandObjectException("SqlCommand expected.");

        if (existingDataSet == null)
            existingDataSet = new DataSet {Locale = CultureInfo.InvariantCulture};

        // We check whether the execution method of this command object conforms to allowable settings
        if (AllowedDataMethod != AllowedDataAccessMethod.All)
        {
            // There are special settings! We need to verify things are OK!
            if (command.CommandType == CommandType.StoredProcedure && AllowedDataMethod != AllowedDataAccessMethod.StoredProceduresOnly)
                // The command type is a stored proc, but stored procedures are not allowed!
                throw new UnsupportedProcessMethodException("Stored Procedures are not a valid data access method based on the current system configuration!");
            if (command.CommandType == CommandType.Text && AllowedDataMethod != AllowedDataAccessMethod.IndividualCommandsOnly)
                // The command type is a text command, but individual text commands are not allowed!
                throw new UnsupportedProcessMethodException("Individual text commands are not a valid data access method based on the current system configuration!");
        }

        // This is an SqlCommand object. We are ready to go.
        PrepareCommandObject(sqlCommand);

        using (var sqlDataAdapter = new SqlDataAdapter(sqlCommand))
        {
            if (AutoRetrieveDatabaseSchema) sqlDataAdapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;
            if (!string.IsNullOrEmpty(entityName))
                sqlDataAdapter.Fill(existingDataSet, entityName);
            else
                sqlDataAdapter.Fill(existingDataSet);
        }

        // We fire an event
        RaiseOnQueryCompleteEvent(existingDataSet, sqlCommand, entityName);

        // We do not want to keep the connection attached to the command, since we do not know how long the command will stay around...
        CleanCommandObject(sqlCommand);
        CloseConnection();
        return existingDataSet;
    }

    public override async Task<object> ExecuteScalarAsync(IDbCommand command)
    {
        // We grab the command object and verify that it is an SQLCommand
        if (command is not SqlCommand sqlCommand) throw new UnsupportedCommandObjectException("SqlCommand expected.");

        // We check whether the execution method of this command object conforms to allowable settings
        if (AllowedDataMethod != AllowedDataAccessMethod.All)
        {
            // There are special settings! We need to verify things are OK!
            if (command.CommandType == CommandType.StoredProcedure && AllowedDataMethod != AllowedDataAccessMethod.StoredProceduresOnly)
                // The command type is a stored proc, but stored procedures are not allowed!
                throw new UnsupportedProcessMethodException("Stored Procedures are not a valid data access method based on the current system configuration!");
            if (command.CommandType == CommandType.Text && AllowedDataMethod != AllowedDataAccessMethod.IndividualCommandsOnly)
                // The command type is a text command, but individual text commands are not allowed!
                throw new UnsupportedProcessMethodException("Individual text commands are not a valid data access method based on the current system configuration!");
        }

        // This is an SqlCommand object. We are ready to go.
        PrepareCommandObject(sqlCommand);

        try
        {
            var result = await sqlCommand.ExecuteScalarAsync();

            CleanCommandObject(sqlCommand);
            CloseConnection();

            // We raise an event
            RaiseOnScalarQueryCompleteEvent(command, result);

            return result;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return null;
        }
    }

    public override object ExecuteScalar(IDbCommand command)
    {
        // We grab the command object and verify that it is an SQLCommand
        if (command is not SqlCommand sqlCommand) throw new UnsupportedCommandObjectException("SqlCommand expected.");

        // We check whether the execution method of this command object conforms to allowable settings
        if (AllowedDataMethod != AllowedDataAccessMethod.All)
        {
            // There are special settings! We need to verify things are OK!
            if (command.CommandType == CommandType.StoredProcedure && AllowedDataMethod != AllowedDataAccessMethod.StoredProceduresOnly)
                // The command type is a stored proc, but stored procedures are not allowed!
                throw new UnsupportedProcessMethodException("Stored Procedures are not a valid data access method based on the current system configuration!");
            if (command.CommandType == CommandType.Text && AllowedDataMethod != AllowedDataAccessMethod.IndividualCommandsOnly)
                // The command type is a text command, but individual text commands are not allowed!
                throw new UnsupportedProcessMethodException("Individual text commands are not a valid data access method based on the current system configuration!");
        }

        // This is an SqlCommand object. We are ready to go.
        PrepareCommandObject(sqlCommand);

        try
        {
            var result = sqlCommand.ExecuteScalar();

            CleanCommandObject(sqlCommand);
            CloseConnection();

            // We raise an event
            RaiseOnScalarQueryCompleteEvent(command, result);

            return result;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return null;
        }
    }

    public override async Task<T> ExecuteScalarAsync<T>(IDbCommand command)
    {
        // We grab the command object and verify that it is an SQLCommand
        if (command is not SqlCommand sqlCommand) throw new UnsupportedCommandObjectException("SqlCommand expected.");

        // We check whether the execution method of this command object conforms to allowable settings
        if (AllowedDataMethod != AllowedDataAccessMethod.All)
        {
            // There are special settings! We need to verify things are OK!
            if (command.CommandType == CommandType.StoredProcedure && AllowedDataMethod != AllowedDataAccessMethod.StoredProceduresOnly)
                // The command type is a stored proc, but stored procedures are not allowed!
                throw new UnsupportedProcessMethodException("Stored Procedures are not a valid data access method based on the current system configuration!");
            if (command.CommandType == CommandType.Text && AllowedDataMethod != AllowedDataAccessMethod.IndividualCommandsOnly)
                // The command type is a text command, but individual text commands are not allowed!
                throw new UnsupportedProcessMethodException("Individual text commands are not a valid data access method based on the current system configuration!");
        }

        // This is an SqlCommand object. We are ready to go.
        PrepareCommandObject(sqlCommand);

        try
        {
            var result = await sqlCommand.ExecuteScalarAsync();

            CleanCommandObject(sqlCommand);
            CloseConnection();

            // We raise an event
            RaiseOnScalarQueryCompleteEvent(command, result);

            return (T) result;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return default;
        }
    }

    public override T ExecuteScalar<T>(IDbCommand command)
    {
        // We grab the command object and verify that it is an SQLCommand
        if (command is not SqlCommand sqlCommand) throw new UnsupportedCommandObjectException("SqlCommand expected.");

        // We check whether the execution method of this command object conforms to allowable settings
        if (AllowedDataMethod != AllowedDataAccessMethod.All)
        {
            // There are special settings! We need to verify things are OK!
            if (command.CommandType == CommandType.StoredProcedure && AllowedDataMethod != AllowedDataAccessMethod.StoredProceduresOnly)
                // The command type is a stored proc, but stored procedures are not allowed!
                throw new UnsupportedProcessMethodException("Stored Procedures are not a valid data access method based on the current system configuration!");
            if (command.CommandType == CommandType.Text && AllowedDataMethod != AllowedDataAccessMethod.IndividualCommandsOnly)
                // The command type is a text command, but individual text commands are not allowed!
                throw new UnsupportedProcessMethodException("Individual text commands are not a valid data access method based on the current system configuration!");
        }

        // This is an SqlCommand object. We are ready to go.
        PrepareCommandObject(sqlCommand);

        try
        {
            var result = sqlCommand.ExecuteScalar();

            CleanCommandObject(sqlCommand);
            CloseConnection();

            // We raise an event
            RaiseOnScalarQueryCompleteEvent(command, result);

            return (T) result;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return default;
        }
    }

    public override async Task<DataSet> ExecuteStoredProcedureQueryAsync(IDbCommand command, string entityName = "", DataSet existingDataSet = null)
    {
        // We grab the command object and verify that it is an SQLCommand
        if (command is not SqlCommand sqlCommand) throw new UnsupportedCommandObjectException("SqlCommand expected.");

        existingDataSet ??= new DataSet {Locale = CultureInfo.InvariantCulture};

        // We check whether the execution method of this command object conforms to allowable settings
        if (AllowedDataMethod != AllowedDataAccessMethod.All)
        {
            // There are special settings! We need to verify things are OK!
            if (command.CommandType == CommandType.StoredProcedure && AllowedDataMethod != AllowedDataAccessMethod.StoredProceduresOnly)
                // The command type is a stored proc, but stored procedures are not allowed!
                throw new UnsupportedProcessMethodException("Stored Procedures are not a valid data access method based on the current system configuration!");
            if (command.CommandType == CommandType.Text && AllowedDataMethod != AllowedDataAccessMethod.IndividualCommandsOnly)
                // The command type is a text command, but individual text commands are not allowed!
                throw new UnsupportedProcessMethodException("Individual text commands are not a valid data access method based on the current system configuration!");
        }

        // This is an SqlCommand object. We are ready to go.
        PrepareCommandObject(sqlCommand);

        // We make sure the command type is appropriate for stored procedures
        if (sqlCommand.CommandType != CommandType.StoredProcedure)
            sqlCommand.CommandType = CommandType.StoredProcedure;

        // Since there is no intrinsic way of filling a DataSet async, we spin off a new task and do it manually
        return await Task.Run(() =>
                              {
                                  using (var sqlDataAdapter = new SqlDataAdapter(sqlCommand))
                                  {
                                      // We use the connection on the data service and fill the DataSet
                                      if (AutoRetrieveDatabaseSchema) sqlDataAdapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;
                                      if (!string.IsNullOrEmpty(entityName))
                                          sqlDataAdapter.Fill(existingDataSet, entityName);
                                      else
                                          sqlDataAdapter.Fill(existingDataSet);
                                  }

                                  // We fire an event
                                  RaiseOnQueryCompleteEvent(existingDataSet, sqlCommand, entityName);

                                  // We do not want to keep the connection attached to the command, since we do not know how long the command will stay around...
                                  CleanCommandObject(sqlCommand);
                                  CloseConnection();
                                  return existingDataSet;
                              });
    }

    public override DataSet ExecuteStoredProcedureQuery(IDbCommand command, string entityName = "", DataSet existingDataSet = null)
    {
        // We grab the command object and verify that it is an SQLCommand
        if (command is not SqlCommand sqlCommand) throw new UnsupportedCommandObjectException("SqlCommand expected.");

        existingDataSet ??= new DataSet {Locale = CultureInfo.InvariantCulture};

        // We check whether the execution method of this command object conforms to allowable settings
        if (AllowedDataMethod != AllowedDataAccessMethod.All)
        {
            // There are special settings! We need to verify things are OK!
            if (command.CommandType == CommandType.StoredProcedure && AllowedDataMethod != AllowedDataAccessMethod.StoredProceduresOnly)
                // The command type is a stored proc, but stored procedures are not allowed!
                throw new UnsupportedProcessMethodException("Stored Procedures are not a valid data access method based on the current system configuration!");
            if (command.CommandType == CommandType.Text && AllowedDataMethod != AllowedDataAccessMethod.IndividualCommandsOnly)
                // The command type is a text command, but individual text commands are not allowed!
                throw new UnsupportedProcessMethodException("Individual text commands are not a valid data access method based on the current system configuration!");
        }

        // This is an SqlCommand object. We are ready to go.
        PrepareCommandObject(sqlCommand);

        // We make sure the command type is appropriate for stored procedures
        if (sqlCommand.CommandType != CommandType.StoredProcedure)
            sqlCommand.CommandType = CommandType.StoredProcedure;

        using (var sqlDataAdapter = new SqlDataAdapter(sqlCommand))
        {
            // We use the connection on the data service and fill the DataSet
            if (AutoRetrieveDatabaseSchema) sqlDataAdapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;
            if (!string.IsNullOrEmpty(entityName))
                sqlDataAdapter.Fill(existingDataSet, entityName);
            else
                sqlDataAdapter.Fill(existingDataSet);
        }

        // We fire an event
        RaiseOnQueryCompleteEvent(existingDataSet, sqlCommand, entityName);

        // We do not want to keep the connection attached to the command, since we do not know how long the command will stay around...
        CleanCommandObject(sqlCommand);
        CloseConnection();
        return existingDataSet;
    }

    /// <summary>
    /// Executes a stored procedure
    /// </summary>
    /// <param name="command">Sql Command object</param>
    /// <returns>True or False</returns>
    public override async Task<bool> ExecuteStoredProcedureAsync(IDbCommand command)
    {
        // We grab the command object and verify that it is an SQLCommand
        if (command is not SqlCommand sqlCommand) throw new UnsupportedCommandObjectException("SqlCommand expected.");

        // We check whether the execution method of this command object conforms to allowable settings
        if (AllowedDataMethod != AllowedDataAccessMethod.All)
        {
            // There are special settings! We need to verify things are OK!
            if (command.CommandType == CommandType.StoredProcedure && AllowedDataMethod != AllowedDataAccessMethod.StoredProceduresOnly)
                // The command type is a stored proc, but stored procedures are not allowed!
                throw new UnsupportedProcessMethodException("Stored Procedures are not a valid data access method based on the current system configuration!");
            if (command.CommandType == CommandType.Text && AllowedDataMethod != AllowedDataAccessMethod.IndividualCommandsOnly)
                // The command type is a text command, but individual text commands are not allowed!
                throw new UnsupportedProcessMethodException("Individual text commands are not a valid data access method based on the current system configuration!");
        }

        // This is an SqlCommand object. We are ready to go.
        PrepareCommandObject(sqlCommand);

        // We make sure the command type is appropriate for stored procedures
        if (sqlCommand.CommandType != CommandType.StoredProcedure)
            sqlCommand.CommandType = CommandType.StoredProcedure;

        var affectedRows = await sqlCommand.ExecuteNonQueryAsync();

        // We make sure we leave no dangling references
        CleanCommandObject(sqlCommand);
        CloseConnection();

        // We raise an event
        RaiseOnNonQueryCompleteEvent(command, affectedRows);

        return affectedRows > 0;
    }

    /// <summary>
    /// Executes a stored procedure
    /// </summary>
    /// <param name="command">Sql Command object</param>
    /// <returns>True or False</returns>
    public override bool ExecuteStoredProcedure(IDbCommand command)
    {
        // We grab the command object and verify that it is an SQLCommand
        if (!(command is SqlCommand sqlCommand)) throw new UnsupportedCommandObjectException("SqlCommand expected.");

        // We check whether the execution method of this command object conforms to allowable settings
        if (AllowedDataMethod != AllowedDataAccessMethod.All)
        {
            // There are special settings! We need to verify things are OK!
            if (command.CommandType == CommandType.StoredProcedure && AllowedDataMethod != AllowedDataAccessMethod.StoredProceduresOnly)
                // The command type is a stored proc, but stored procedures are not allowed!
                throw new UnsupportedProcessMethodException("Stored Procedures are not a valid data access method based on the current system configuration!");
            if (command.CommandType == CommandType.Text && AllowedDataMethod != AllowedDataAccessMethod.IndividualCommandsOnly)
                // The command type is a text command, but individual text commands are not allowed!
                throw new UnsupportedProcessMethodException("Individual text commands are not a valid data access method based on the current system configuration!");
        }

        // This is an SqlCommand object. We are ready to go.
        PrepareCommandObject(sqlCommand);

        // We make sure the command type is appropriate for stored procedures
        if (sqlCommand.CommandType != CommandType.StoredProcedure)
            sqlCommand.CommandType = CommandType.StoredProcedure;

        var affectedRows = sqlCommand.ExecuteNonQuery();

        // We make sure we leave no dangling references
        CleanCommandObject(sqlCommand);
        CloseConnection();

        // We raise an event
        RaiseOnNonQueryCompleteEvent(command, affectedRows);

        return affectedRows > 0;
    }

    /// <summary>Returns a new SqlCommand object.</summary>
    /// <param name="commandText">Command text to be set on the new command object</param>
    /// <returns>SqlCommand object instance</returns>
    public override IDbCommand NewCommandObject(string commandText = null)
    {
        var command = new SqlCommand();
        if (!string.IsNullOrEmpty(commandText)) command.CommandText = commandText;
        return command;
    }

    /// <summary>
    /// Returns an instance of a parameter object that can be added
    /// to an IDbCommand.Parameters collection.
    /// </summary>
    /// <param name="parameterName">Name of the new parameter</param>
    /// <param name="parameterValue">Value of the new parameter</param>
    /// <returns>Parameter object</returns>
    /// <exception>Throws ArgumentNullException if null parameterName is passed, or ArgumentException if empty parameterName is passed.</exception>
    public override IDbDataParameter NewCommandObjectParameter(string parameterName, object parameterValue)
    {
        if (string.IsNullOrEmpty(parameterName)) throw new ArgumentNullException("parameterName");

        var sqlParameter = new SqlParameter(parameterName, parameterValue);

        // We check whether we know what parameter type is to be used
        if (parameterType == SqlServerParameterType.Unknown)
        {
            // We do not yet know the parameter type, so we find it in the configuration
            if (ConfigurationSettings.Settings.IsSettingSupported("SqlServerParameterType"))
            {
                var setting = ConfigurationSettings.Settings["SqlServerParameterType"].ToLowerInvariant().Trim();
                if (setting == "unicode")
                    parameterType = SqlServerParameterType.Unicode;
                else if (setting == "notunicode")
                    parameterType = SqlServerParameterType.NotUnicode;
                else
                    parameterType = SqlServerParameterType.Undefined;
            }
            else
            {
                parameterType = SqlServerParameterType.Undefined;
            }
        }

        // We make sure the parameter types are right
        if (parameterType != SqlServerParameterType.Undefined)
        {
            if (parameterType == SqlServerParameterType.Unicode)
                switch (sqlParameter.SqlDbType)
                {
                    case SqlDbType.Char:
                        sqlParameter.SqlDbType = SqlDbType.NChar;
                        break;
                    case SqlDbType.VarChar:
                        sqlParameter.SqlDbType = SqlDbType.NVarChar;
                        break;
                    case SqlDbType.Text:
                        sqlParameter.SqlDbType = SqlDbType.NText;
                        break;
                }

            if (parameterType == SqlServerParameterType.NotUnicode)
                switch (sqlParameter.SqlDbType)
                {
                    case SqlDbType.NChar:
                        sqlParameter.SqlDbType = SqlDbType.Char;
                        break;
                    case SqlDbType.NVarChar:
                        sqlParameter.SqlDbType = SqlDbType.VarChar;
                        break;
                    case SqlDbType.NText:
                        sqlParameter.SqlDbType = SqlDbType.Text;
                        break;
                }
        }

        return sqlParameter;
    }

    /// <summary>
    /// Generically returns the value of the specified parameter in a SqlCommand object.
    /// </summary>
    /// <param name="parameterName">Parameter Name</param>
    /// <param name="command">Command Object</param>
    /// <returns>Parameter value</returns>
    /// <exception>Throws ArgumentNullException if null parameterName is passed, or ArgumentException if empty parameterName is passed.</exception>
    public override object GetCommandParameterValue(string parameterName, IDbCommand command)
    {
        if (string.IsNullOrEmpty(parameterName)) return null;

        // We grab the command object and verify that it is an SQLCommand
        if (command is not SqlCommand sqlCommand) throw new UnsupportedCommandObjectException("SqlCommand expected.");

        // This is an SqlCommand object. We are ready to go.

        // We simply try to access the appropriate parameter.
        // If it doesn't exist, we will simply let the error bubble up.

        // CL on 11/6/2006: Before, this method was returning the actual Parameter object, instead of return its value. Fixed that.
        try
        {
            return sqlCommand.Parameters[parameterName].Value;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Creates an update command object for the row passed along.
    /// </summary>
    /// <param name="changedRow">Changed ADO.NET data row</param>
    /// <param name="primaryKeyType">Primary key type</param>
    /// <param name="primaryKeyFieldName">Name of the primary key field</param>
    /// <param name="updateMode">Optimistic or pessimistic update mode.</param>
    /// <param name="updateMethod">Method used to update the database (commands, stored procedures,...)</param>
    /// <param name="tableName">Name of the table.</param>
    /// <param name="fieldNames">Names of the fields to be included in the update (all others will be ignored)</param>
    /// <param name="fieldMaps">List of key value pairs that can be used to map field names. For instance, if a field in the table is called MyId but in the database it is called ID, then one can add a key 'MyId' with a value of 'ID'</param>
    /// <returns>Update command that can sub-sequentially be executed against the database using the same data service.</returns>
    /// <exception>
    /// Throws ArgumentNullException if null changedRow is passed.
    /// Throws ArgumentNullException if null primaryKeyFieldName is passed.
    /// Throws ArgumentException if empty primaryKeyFieldName is passed.
    /// </exception>
    public override IDbCommand BuildUpdateCommand(DataRow changedRow, KeyType primaryKeyType, string primaryKeyFieldName, string tableName, DataRowUpdateMode updateMode = DataRowUpdateMode.ChangedFieldsOnly, DataRowProcessMethod updateMethod = DataRowProcessMethod.Default, IList<string> fieldNames = null, IDictionary<string, string> fieldMaps = null)
    {
        if (changedRow == null) throw new ArgumentNullException("changedRow");
        if (string.IsNullOrEmpty(primaryKeyFieldName)) throw new ArgumentNullException("primaryKeyFieldName");

        return updateMethod switch
        {
            DataRowProcessMethod.Default or DataRowProcessMethod.IndividualCommands => DataHelper.BuildSqlUpdateCommand(changedRow, primaryKeyType, primaryKeyFieldName, this, updateMode, tableName, fieldNames, fieldMaps),
            DataRowProcessMethod.StoredProcedures => DataHelper.BuildStoredProcedureUpdateCommand(changedRow, primaryKeyType, primaryKeyFieldName, this, updateMode, DefaultStoredProcedurePrefix, tableName, fieldNames, fieldMaps),
            _ => throw new UnsupportedProcessMethodException("SQL Server databases can not be updated through the chosen update method: " + updateMethod),
        };
    }

    /// <summary>
    /// Creates a delete command object for the defined table and primary key.
    /// </summary>
    /// <param name="tableName">Name of the table the record is to be deleted from .</param>
    /// <param name="primaryKeyFieldName">Primary key field name within the table</param>
    /// <param name="primaryKeyValue">Primary key value for the record that is to be deleted</param>
    /// <param name="updateMethod">Method used to update the database (commands, stored procedures,...)</param>
    /// <returns>IDbCommand object that can sub-sequentially be executed against a database</returns>
    /// <remarks>Whenever this method is called in Stored Procedure mode, there needs to be a Stored Procedure on the server that follows the following naming convention:
    /// 
    ///   [Prefix]del[TableName]
    ///   
    /// So in a scenario where the default prefix is used and the table name is Customer, the SP needs to have the following name:
    /// 
    ///   milos_delCustomer
    ///   
    /// The SP needs to accept a single parameter, which defines the primary key value of the row that is to be deleted. The name of the parameter needs to be the same as the primary key field. (If the PK field is named pk_customer, the parameter name is @pk_customer).</remarks>
    /// <exception>
    /// Throws ArgumentNullException if null tableName is passed.
    /// Throws ArgumentNullException if null primaryKeyFieldName is passed.
    /// Throws ArgumentException if empty primaryKeyFieldName is passed.
    /// </exception>
    public override IDbCommand BuildDeleteCommand(string tableName, string primaryKeyFieldName, object primaryKeyValue, DataRowProcessMethod updateMethod = DataRowProcessMethod.Default)
    {
        if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException("tableName");
        if (string.IsNullOrEmpty(primaryKeyFieldName)) throw new ArgumentNullException("primaryKeyFieldName");

        switch (updateMethod)
        {
            case DataRowProcessMethod.Default:
            case DataRowProcessMethod.IndividualCommands:
                return DataHelper.BuildDeleteCommand(tableName, primaryKeyFieldName, primaryKeyValue, this);
            case DataRowProcessMethod.StoredProcedures:
                var comDelete = NewCommandObject();
                comDelete.CommandType = CommandType.StoredProcedure;
                comDelete.CommandText = DefaultStoredProcedurePrefix + "del" + tableName;
                comDelete.Parameters.Add(NewCommandObjectParameter("@" + primaryKeyFieldName, primaryKeyValue));
                return comDelete;
            default:
                throw new UnsupportedProcessMethodException("SQL Server databases can not be deleted through the chosen update method: " + updateMethod);
        }
    }

    /// <summary>
    /// Builds a command object that queries an empty record containing all fields of the specified table.
    /// </summary>
    /// <param name="tableName">Table Name</param>
    /// <param name="fieldList">List of fields to be included in the query. If selectMethod is StoredProcedure, this parameter is ignored.</param>
    /// <param name="selectMethod">Select method (such as stored procedure or select commands)</param>
    /// <returns>IDbCommand object</returns>
    /// <remarks>Whenever this method is called in Stored Procedure mode, there needs to be a Stored Procedure on the server that follows the following naming convention:
    /// 
    ///   [Prefix]new[TableName]
    ///   
    /// So in a scenario where the default prefix is used and the table name is Customer, the SP needs to have the following name:
    /// 
    ///   milos_newCustomer
    ///   
    /// The SP accepts no parameters.</remarks>
    /// <exception>
    /// Throws ArgumentNullException if null tableName is passed.
    /// Throws ArgumentException if empty tableName is passed.
    /// </exception>
    public override IDbCommand BuildEmptyRecordQueryCommand(string tableName, string fieldList, DataRowProcessMethod selectMethod = DataRowProcessMethod.Default)
    {
        if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException("tableName");

        switch (selectMethod)
        {
            case DataRowProcessMethod.Default:
            case DataRowProcessMethod.IndividualCommands:
                var comSelect = NewCommandObject();
                comSelect.CommandText = "SET FMTONLY ON;SELECT " + fieldList + " FROM " + tableName + ";SET FMTONLY OFF";
                return comSelect;
            case DataRowProcessMethod.StoredProcedures:
                var comNew = NewCommandObject();
                comNew.CommandType = CommandType.StoredProcedure;
                comNew.CommandText = DefaultStoredProcedurePrefix + "new" + tableName;
                return comNew;
            default:
                throw new UnsupportedProcessMethodException("SQL Server databases can not be accessed through the chosen update method: " + selectMethod);
        }
    }

    /// <summary>
    /// Builds a command object that queries all records (with specified fields) from the specified table.
    /// Defining an order is possible as well.
    /// </summary>
    /// <param name="tableName">Name of the table to query from</param>
    /// <param name="fieldList">Fields to query (or * for all fields) - Note that this setting only applies if the system is NOT in Stored Procedure mode!</param>
    /// <param name="orderBy">Order (or empty string if no special order is desired) - Note that this setting only applies if the system is NOT in Stored Procedure mode!</param>
    /// <param name="selectMethod">Select method (such as stored procedure or select commands)</param>
    /// <returns>IDbCommand object</returns>
    /// <remarks>Whenever this data service runs in Stored Procedure mode, it will automatically look for a Stored Procedure that matches the following naming convention:
    /// 
    ///   [prefix]get[Tablename]AllRecords
    /// 
    /// For instance, if the prefix is the default prefix ("milos_"), and the table name is Customer, then the resulting stored procedure the system would use is:
    /// 
    ///   milos_getCustomerAlLRecords
    /// 
    /// Note that the list of fields as well as the sort order is defined by the stored procedure, and not by the fields passed to this method.
    /// </remarks>
    /// <exception>
    /// Throws ArgumentNullException if null tableName is passed.
    /// Throws ArgumentException if empty tableName is passed.
    /// </exception>
    public override IDbCommand BuildAllRecordsQueryCommand(string tableName, string fieldList, string orderBy = "", DataRowProcessMethod selectMethod = DataRowProcessMethod.Default)
    {
        if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException("tableName");

        switch (selectMethod)
        {
            case DataRowProcessMethod.Default:
            case DataRowProcessMethod.IndividualCommands:
                var selectCommand = NewCommandObject();
                selectCommand.CommandText = "SELECT " + fieldList + " FROM " + tableName;
                if (!string.IsNullOrEmpty(orderBy))
                    selectCommand.CommandText += " ORDER BY " + orderBy;
                return selectCommand;
            case DataRowProcessMethod.StoredProcedures:
                var storedProcedureSelectCommand = NewCommandObject();
                storedProcedureSelectCommand.CommandType = CommandType.StoredProcedure;
                storedProcedureSelectCommand.CommandText = DefaultStoredProcedurePrefix + "get" + tableName + "AllRecords";
                return storedProcedureSelectCommand;
            default:
                throw new UnsupportedProcessMethodException("SQL Server databases can not be accessed through the chosen update method: " + selectMethod);
        }
    }

    /// <summary>
    /// Returns a single record (with a specified list of fields) by primary key.
    /// </summary>
    /// <param name="tableName">Name of the table to query.</param>
    /// <param name="fieldList">List of fields to return. If selectMethod is StoredProcedure, this parameter is ignored.</param>
    /// <param name="primaryKeyFieldName">Name of the primary key field.</param>
    /// <param name="primaryKeyValue">Primary key (value)</param>
    /// <param name="selectMethod">Select method (such as stored procedure or select commands)</param>
    /// <returns>IDbCommand object</returns>
    /// <exception>
    /// Throws ArgumentNullException for parameters that cannot be null
    /// Throws ArgumentException for parameters that cannot be empty
    /// Throws UnsupportedProcessMethodException for data access methods other than default, stored procedures, or individual commands.
    /// </exception>
    /// <example>
    /// IDbCommand command = service.BuildSingleRecordQueryCommand("Customers", "*", "CustomerKey", key, DataRowProcessMethod.Default);
    /// DataSet data = service.ExecuteQuery(command);
    /// </example>
    public override IDbCommand BuildSingleRecordQueryCommand(string tableName, string fieldList, string primaryKeyFieldName, object primaryKeyValue, DataRowProcessMethod selectMethod = DataRowProcessMethod.Default)
    {
        if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException("tableName");
        if (string.IsNullOrEmpty(fieldList)) throw new ArgumentNullException("fieldList");
        if (selectMethod != DataRowProcessMethod.StoredProcedures && fieldList.Length == 0) throw new ArgumentException("Parameter cannot be empty.", "fieldList");
        if (string.IsNullOrEmpty(primaryKeyFieldName)) throw new ArgumentNullException("primaryKeyFieldName");

        switch (selectMethod)
        {
            case DataRowProcessMethod.Default:
            case DataRowProcessMethod.IndividualCommands:
                var loadCommand = NewCommandObject();
                loadCommand.CommandText = "SELECT " + fieldList + " FROM " + tableName + " WHERE " + primaryKeyFieldName + " = @PK";
                loadCommand.Parameters.Add(NewCommandObjectParameter("@PK", primaryKeyValue));
                return loadCommand;
            case DataRowProcessMethod.StoredProcedures:
                var storedProcLoadCommand = NewCommandObject();
                storedProcLoadCommand.CommandType = CommandType.StoredProcedure;
                storedProcLoadCommand.CommandText = DefaultStoredProcedurePrefix + "get" + tableName + "By" + primaryKeyFieldName;
                storedProcLoadCommand.Parameters.Add(NewCommandObjectParameter("@" + primaryKeyFieldName, primaryKeyValue));
                return storedProcLoadCommand;
            default:
                throw new UnsupportedProcessMethodException("SQL Server databases can not be accessed through the chosen update method: " + selectMethod);
        }
    }

    /// <summary>
    /// Builds a command that returns a set of records based on the provided field names and filter paremters.
    /// </summary>
    /// <param name="tableName">Name of the table.</param>
    /// <param name="fieldList">The list of fields returned by the query (ignored for stored procedure execution)</param>
    /// <param name="fieldNames">The list of fields by which to filter</param>
    /// <param name="filterParameters">Parameters used for filtering. The parameters need to match the list of filter fields (name and types)</param>
    /// <param name="selectMethod">Process method for the select method</param>
    /// <returns>IDbCommand object representing the query</returns>
    /// <exception>
    /// Throws ArgumentNullException for parameters that cannot be null
    /// Throws ArgumentException for parameters that cannot be empty
    /// Throws UnsupportedProcessMethodException for data access methods other than default, stored procedures, or individual commands.
    /// </exception>
    /// <example>
    /// string[] fieldNames = new List() { "FirstName", "LastName", "IsActive" };
    /// object[] parameters = new List() { "Chris", "Pronger", true };
    /// IDbCommand command = service.BuildQueryCommand("Customers", "*", fieldNames, parameters, DataRowProcessMethod.Default);
    /// DataSet data = service.ExecuteQuery(command);
    /// </example>
    /// <remarks>
    /// All provided parameters are added using "and" logical operators.
    /// The fields are used as exact matches. Therefore passing "Pron" as a filter parameter will NOT include
    /// "Pronger". However, it is possible to pass "Pron%", in which case "Pronger" is included, assuming the
    /// database back end understands the % character.
    /// </remarks>
    public override IDbCommand BuildQueryCommand(string tableName, string fieldList, IList<string> fieldNames = null, IList<object> filterParameters = null, DataRowProcessMethod selectMethod = DataRowProcessMethod.Default)
    {
        if (string.IsNullOrEmpty(tableName)) throw new ArgumentException("Parameter cannot be empty.", "tableName");
        if (string.IsNullOrEmpty(fieldList)) throw new ArgumentException("Parameter cannot be empty.", "fieldList");

        fieldNames ??= [];
        foreach (var field in fieldNames)
            if (string.IsNullOrEmpty(field))
                throw new ArgumentException("Field name cannot be empty.", string.Empty);

        if (filterParameters == null) filterParameters = [];

        if (filterParameters.Count != fieldNames.Count) throw new ArgumentException("Field list and filter parameter list must have the same length.");

        switch (selectMethod)
        {
            case DataRowProcessMethod.Default:
            case DataRowProcessMethod.IndividualCommands:
                var loadCommand = NewCommandObject();
                loadCommand.CommandText = $"SELECT {fieldList} FROM {tableName} WHERE "; // +primaryKeyFieldName + " = @PK";
                var whereClause = new StringBuilder();
                for (var counter = 0; counter < fieldNames.Count; counter++)
                {
                    var fieldName = fieldNames[counter];
                    var parameter = filterParameters[counter];
                    if (counter > 0)
                        whereClause.Append(" AND ");
                    whereClause.Append(fieldName + " = @P" + fieldName);
                    loadCommand.Parameters.Add(NewCommandObjectParameter("@P" + fieldName, parameter));
                }

                loadCommand.CommandText += whereClause.ToString();
                return loadCommand;

            case DataRowProcessMethod.StoredProcedures:
                var storedProcLoadCommand = NewCommandObject();
                storedProcLoadCommand.CommandType = CommandType.StoredProcedure;
                storedProcLoadCommand.CommandText = $"{DefaultStoredProcedurePrefix}get{tableName}By";
                var parameterAdded = false;
                for (var counter = 0; counter < fieldNames.Count; counter++)
                {
                    var fieldName = fieldNames[counter];
                    var parameter = filterParameters[counter];
                    if (parameterAdded)
                        storedProcLoadCommand.CommandText += "And";
                    else
                        parameterAdded = true;
                    storedProcLoadCommand.CommandText += fieldName;
                    storedProcLoadCommand.Parameters.Add(NewCommandObjectParameter("@" + fieldName, parameter));
                }

                return storedProcLoadCommand;

            default:
                throw new UnsupportedProcessMethodException("SQL Server databases can not be accessed through the chosen update method: " + selectMethod);
        }
    }

    /// <summary>
    /// This method decorates a command object for use.
    /// It assigns things such as a connection and possibly
    /// a transaction.
    /// </summary>
    /// <param name="command">Sql Command object</param>
    protected virtual void PrepareCommandObject(SqlCommand command)
    {
        // First, we get a connection (this will open the connection if needed)
        command.Connection = Connection;

        // Then we check whether we are currently in a transaction, and if so, use that transaction on the current connection.
        if (InTransaction)
            command.Transaction = CurrentTransaction;

        // We also check the timeout
        if (MinimumCommandTimeout > 0 && command.CommandTimeout < MinimumCommandTimeout)
            command.CommandTimeout = MinimumCommandTimeout;
    }

    /// <summary>
    /// Detaches a command object from plumbing mechanisms
    /// such as connections or transactions.
    /// </summary>
    /// <param name="command">Sql Command object</param>
    protected virtual void CleanCommandObject(SqlCommand command)
    {
        command.Connection = null;
        command.Transaction = null;
    }

    /// <summary>
    /// This method closes the current connection (if appropriate)
    /// </summary>
    protected virtual void CloseConnection()
    {
        // We can only close the connection if we are not in a transaction
        if (InTransaction)
            // We ignore this call. Once the transaction ends, the connection will be closed automatically.
            return;

        // We are ready to close the connection
        try
        {
            Connection.Close();
        }
        catch
        {
            // Nothing we can really do here!
        }
    }

    /// <summary>
    /// Used to clean up the open connections.
    /// </summary>
    /// <param name="disposing">True if called from Dispose()</param>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        // Make sure we are not in a transaction
        if (InTransaction)
            AbortTransaction();

        // We make sure we close the connections on shutdown
        if (directConnection != null && directConnection.State != ConnectionState.Closed)
            try
            {
                directConnection.Close();
                directConnection.Dispose();
            }
            catch
            {
                // Nothing we can do, really.
            }
    }

    /// <summary>
    /// For internal use only. (This class is used in the app role stack)
    /// </summary>
    private struct AppRoleStackItem
    {
        /// <summary>
        /// Role name
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        /// Role Password
        /// </summary>
        public string Password { get; set; }
    }
}

/// <summary>
/// Parameter type sent to SQL Server
/// </summary>
public enum SqlServerParameterType
{
    /// <summary>
    /// The type is not yet known (used internally only)
    /// </summary>
    Unknown,

    /// <summary>
    /// No parameter interference
    /// </summary>
    Undefined,

    /// <summary>
    /// Unicode parameters (such as NVarChar)
    /// </summary>
    Unicode,

    /// <summary>
    /// Not Unicode (such as VarChar)
    /// </summary>
    NotUnicode
}