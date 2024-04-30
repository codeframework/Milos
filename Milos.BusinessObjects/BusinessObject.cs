using System.Threading.Tasks;
using Milos.Data;

// TODO: We should check all the methods in this object to see which overloads can be handled by optional parameters

namespace Milos.BusinessObjects;

/// <summary>This class is an abstract business object class. It's purpose is to be sub-classed into individual business objects.</summary>
public abstract class BusinessObject : IBusinessObject
{
    /// <summary>Secondary list of primary key types that may be used for secondary tables.</summary>
    private readonly Hashtable primaryKeyTypes = new Hashtable();

    /// <summary>\The following fields are for internal use only, and are not exposed through public properties.</summary>
    private IDataService dataService;

    /// <summary>Stores the deletion business rules.</summary>
    private BusinessRuleCollection deletionBusinessRules;

    /// <summary>Stores a reference to a data context (internal use only).</summary>
    private SharedDataContext internalDataContext;

    /// <summary>Internal reference to the business rules collection.</summary>
    private BusinessRuleCollection rules;

    /// <summary>For internal use only.</summary>
    private bool startedTransaction;

    /// <summary>Constructor</summary>
    protected BusinessObject()
    {
        // We automatically branch out the the configuration method
        // The idea here is that giving the user/developer this method
        // to override should be a bit safer then overriding the constructor,
        // which may have a whole set of options that go along with it.
        Configure();
    }

    /// <summary>Can be used to retrieve the last error message that occurred.</summary>
    public virtual string LastErrorMessage { get; set; }

    /// <summary>Default fields returned by GetList().</summary>
    public virtual string DefaultFields { get; set; } = "*";

    /// <summary>Default order returned by GetList()</summary>
    public virtual string DefaultOrder { get; set; } = string.Empty;

    /// <summary>Configuration prefix used to instantiate the data service for this particular business object.</summary>
    public virtual string DataConfigurationPrefix { get; set; } = "database";

    /// <summary>Application Role used by this business object.</summary>
    public virtual string AppRole { get; set; } = string.Empty;

    /// <summary>Application Role Password used by this business object.</summary>
    /// <remarks>This should not be set in code! Instead, it should be loaded from a secure configuration setting! Hard-coding passwords into source code is a bad idea!</remarks>
    public virtual string AppRolePassword { get; set; } = string.Empty;

    /// <summary>Defines whether or not a business object should allow to save data that is flagged with critical violations.</summary>
    /// <remarks>This should only be changed to 'true' in very exceptional cases!</remarks>
    protected virtual bool AllowSaveWithViolations { get; set; }

    /// <summary>Defines whether or not a business object should allow to save data that is flagged with warnings.</summary>
    protected virtual bool AllowSaveWithWarnings { get; set; } = true;

    /// <summary>
    /// This (read-only) property provides a reference to the current data service class.
    /// Note that this method should be called as sparse as possible. If you need to access
    /// the DataService more than once, consider assigning the retrieved object to a local
    /// variable for performance reasons.
    /// </summary>
    protected internal virtual IDataService DataService
    {
        get
        {
            IDataService service;

            // If a shared data context is in use, we use it rather than
            // the current object to find things such as data services.
            // (Note: Data context objects clearly are the exception)
            if (internalDataContext == null)
                // The first time around, we need to create an appropriate DataService
            {
                if (dataService == null)
                    try
                    {
                        dataService = DataServiceFactory.GetDataService(DataConfigurationPrefix);
                    }
                    catch (DataServiceInstantiationException ex)
                    {
                        throw new NoDataServiceAvailableException("No data service available\r\nCheck inner exception for details.", ex);
                    }

                service = dataService;
            }
            else
            {
                // A data context is in use, so we use its data service
                service = internalDataContext.DataService;
            }

            // If no service exists, we raise an error
            if (service == null) throw new NoDataServiceAvailableException();

            return service;
        }
    }

    /// <summary>Returns a reference to a data context object that encapsulates all data access semantics without exposing them.</summary>
    public virtual SharedDataContext SharedDataContext
    {
        get
        {
            if (internalDataContext == null)
            {
                var context = new SharedDataContext(DataService);
                internalDataContext = context;
            }

            return internalDataContext;
        }
    }

    /// <summary>Contains the name of the broken rules table that is embedded in the DataSet when broken rules are encountered.</summary>
    /// <remarks>This is mostly intended for internal use, although some classes may want to override this name, in scenarios where the table name could cause a conflict.</remarks>
    protected internal virtual string BrokenRulesTableName { get; set; } = "__BrokenRules";

    /// <summary>Business Rules collection.</summary>
    protected virtual BusinessRuleCollection BusinessRules => rules ?? (rules = new BusinessRuleCollection(this));

    /// <summary>This property indicates whether a save operation for secondary tables is triggered as a delete operation or an update/insert operation.</summary>
    protected virtual DataSaveMode SaveMode { get; set; } = DataSaveMode.AllChanges;

    /// <summary>Gets the deletion business rules.</summary>
    /// <value>The deletion business rules.</value>
    public BusinessRuleCollection DeletionBusinessRules => deletionBusinessRules ?? (deletionBusinessRules = new BusinessRuleCollection(this));

    /// <summary>Gets or sets a value indicating whether or not a business object should be allowed to delete a record that violates a rule.</summary>
    /// <value>
    ///     <c>true</c> if [allow delete with violations]; otherwise, <c>false</c>.
    /// </value>
    public bool AllowDeleteWithViolations { get; set; }

    internal DeletionVerificationLevel DeletionVerificationLevel { get; private set; }

    /// <summary>
    /// This is the main database entity this business object relates to.
    /// For instance, a names business object may connect to a "Names" table.
    /// There may be additional entities/tables (such as addresses), but
    /// this identifies the main one.
    /// </summary>
    public virtual string MasterEntity { get; set; } = string.Empty;

    /// <summary>Property that exposes the field name of the primary key field</summary>
    public virtual string PrimaryKeyField { get; set; } = string.Empty;

    /// <summary>Returns the type of the primary key field.</summary>
    public virtual KeyType PrimaryKeyType { get; set; } = KeyType.Guid;

    /// <summary>Defines whether only changed fields (default) or the complete record will be written to the server when updates are required.</summary>
    public virtual DataRowUpdateMode UpdateMode { get; set; } = DataRowUpdateMode.ChangedFieldsOnly;

    /// <summary>Defines how the data base back end is to be updated (sql commands, stored procedures,...)</summary>
    public virtual DataRowProcessMethod UpdateMethod { get; set; } = DataRowProcessMethod.Default;

    /// <summary>Defines how the data base back end is to be updated (sql commands, stored procedures,...)</summary>
    public virtual DataRowProcessMethod DeleteMethod { get; set; } = DataRowProcessMethod.Default;

    /// <summary>Defines the method used to query records on the database back end (for automated queries)</summary>
    public virtual DataRowProcessMethod QueryMethod { get; set; } = DataRowProcessMethod.Default;

    /// <summary>This method verifies a DataSet. It is designed to be overridden in subclasses.</summary>
    /// <param name="masterDataSet">DataSet</param>
    /// <returns>True or False depending on whether or not the DataSet is valid.</returns>
    public virtual bool Verify(DataSet masterDataSet)
    {
        // Since we start over, we want to start with a clear slate, 
        // so we remove all current violations.
        ClearBrokenRules(masterDataSet);
        // We apply default rules
        BusinessRules.ApplyRules(masterDataSet);
        // We check whether we appear to have broken rules
        return !DataSetHasViolations(masterDataSet);
    }

    /// <summary>This method verifies and saves the passed DataSet.</summary>
    /// <remarks>This method is to be overridden in subclasses for anything but the simplest save operations.</remarks>
    /// <param name="masterDataSet">DataSet</param>
    /// <returns>True or False</returns>
    public virtual bool Save(DataSet masterDataSet)
    {
        if (masterDataSet == null) throw new NullReferenceException("Parameter 'masterDataSet' cannot be null.");

        // Before we do anything else, we need to make sure that
        // the DataSet contains valid data.
        if (!Verify(masterDataSet))
            if (!AllowSaveWithViolations)
                if (DataSetHasViolations(masterDataSet, RuleViolationType.Violation))
                {
                    // Critical problems are in fact present, so we will not save
                    // the data in this business object, which does not allow
                    // to save data with critical errors (violations)
                    return false;
                }
                else
                {
                    // We did not find critical errors in the data.
                    // Now we need to check whether it is OK to save
                    // with warnings.
                    if (!AllowSaveWithWarnings) return false;
                }

        // We check for app roles
        if (!string.IsNullOrEmpty(AppRole)) DataService.ApplyAppRole(AppRole, AppRolePassword);

        // We are good to go. We wrap all saving operations into a transaction
        var previousTransactionStatus = DataService.TransactionStatus;
        if (!BeginTransaction())
            if (previousTransactionStatus != TransactionStatus.TransactionInProgress)
                throw new TransactionException("Cannot start transaction");

        // We use this variable to determine whether save operations are executes successfully
        var saved = false;

        // Before we save the master entity, we process all deletions that may have occurred
        // in secondary entities
        SaveMode = DataSaveMode.DeletesOnly;
        var primaryKeyType = GetPrimaryKeyType();

        if (masterDataSet.Tables[MasterEntity].Rows.Count > 0 && masterDataSet.Tables[MasterEntity].Rows[0].RowState != DataRowState.Deleted)
            switch (primaryKeyType)
            {
                case KeyType.Guid:
                    saved = SaveSecondaryTables((Guid) masterDataSet.Tables[MasterEntity].Rows[0][PrimaryKeyField], masterDataSet);
                    break;
                case KeyType.Integer:
                    saved = SaveSecondaryTables((int) masterDataSet.Tables[MasterEntity].Rows[0][PrimaryKeyField], masterDataSet);
                    break;
                case KeyType.IntegerAutoIncrement:
                    saved = SaveSecondaryTables((int) masterDataSet.Tables[MasterEntity].Rows[0][PrimaryKeyField], masterDataSet);
                    break;
                case KeyType.String:
                    saved = SaveSecondaryTables(masterDataSet.Tables[MasterEntity].Rows[0][PrimaryKeyField].ToString(), masterDataSet);
                    break;
            }
        else
            switch (primaryKeyType)
            {
                case KeyType.Guid:
                    saved = SaveSecondaryTables(Guid.Empty, masterDataSet);
                    break;
                case KeyType.Integer:
                    saved = SaveSecondaryTables(0, masterDataSet);
                    break;
                case KeyType.IntegerAutoIncrement:
                    saved = SaveSecondaryTables(0, masterDataSet);
                    break;
                case KeyType.String:
                    saved = SaveSecondaryTables(string.Empty, masterDataSet);
                    break;
            }

        SaveMode = DataSaveMode.AllChanges;
        if (!saved)
        {
            // We reset the app role if we set a role
            if (!string.IsNullOrEmpty(AppRole)) DataService.RevertAppRole();

            // The operation failed. We need to roll back our update attempt!
            if (!AbortTransaction()) throw new TransactionException("Unable to roll back transaction during failed save.");
            return false;
        }

        // We save the master entity
        saved = SaveMasterEntity(masterDataSet.Tables[MasterEntity]);

        // If we save the master entity and no errors occured, we proceed to saving changes
        // and additions in the secondary tables. Otherwise, we revert.
        if (saved != true)
        {
            // We reset the app role if we set a role
            if (!string.IsNullOrEmpty(AppRole)) DataService.RevertAppRole();

            // There already was a problem. We do not even have to go further...
            if (!AbortTransaction()) throw new TransactionException("Cannot roll back transaction");
            return false;
        }

        // We also provide an opportunity to save secondary tables, without having to override this method
        // This time, we process inserts and updates, but no deletes.
        SaveMode = DataSaveMode.AllChangesExceptDeletes;
        primaryKeyType = GetPrimaryKeyType();
        if (masterDataSet.Tables[MasterEntity].Rows.Count > 0 && masterDataSet.Tables[MasterEntity].Rows[0].RowState != DataRowState.Deleted)
            switch (primaryKeyType)
            {
                case KeyType.Guid:
                    saved = SaveSecondaryTables((Guid) masterDataSet.Tables[MasterEntity].Rows[0][PrimaryKeyField], masterDataSet);
                    break;
                case KeyType.Integer:
                    saved = SaveSecondaryTables((int) masterDataSet.Tables[MasterEntity].Rows[0][PrimaryKeyField], masterDataSet);
                    break;
                case KeyType.IntegerAutoIncrement:
                    saved = SaveSecondaryTables((int) masterDataSet.Tables[MasterEntity].Rows[0][PrimaryKeyField], masterDataSet);
                    break;
                case KeyType.String:
                    saved = SaveSecondaryTables(masterDataSet.Tables[MasterEntity].Rows[0][PrimaryKeyField].ToString(), masterDataSet);
                    break;
            }
        else
            switch (primaryKeyType)
            {
                case KeyType.Guid:
                    saved = SaveSecondaryTables(Guid.Empty, masterDataSet);
                    break;
                case KeyType.Integer:
                    saved = SaveSecondaryTables(0, masterDataSet);
                    break;
                case KeyType.IntegerAutoIncrement:
                    saved = SaveSecondaryTables(0, masterDataSet);
                    break;
                case KeyType.String:
                    saved = SaveSecondaryTables(string.Empty, masterDataSet);
                    break;
            }

        SaveMode = DataSaveMode.AllChanges;
        if (!saved)
        {
            // We reset the app role if we set a role
            if (!string.IsNullOrEmpty(AppRole)) DataService.RevertAppRole();

            // The operation failed. We need to roll back our update attempt!
            if (!AbortTransaction()) throw new TransactionException("Cannot roll back transaction");
        }
        else
        {
            // We reset the app role if we set a role
            if (!string.IsNullOrEmpty(AppRole)) DataService.RevertAppRole();

            // Great! We saved the record without problems
            if (!CommitTransaction()) throw new TransactionException("Failed to commit transaction");
        }

        return saved;
    }

    /// <summary>Saves a business entity to the back end.</summary>
    /// <param name="entity">Business entity that is to be saved</param>
    /// <returns>True or False</returns>
    /// <remarks>Retrieves the internal DataSet from the business entity and saves it using another overload of the Save() method.</remarks>
    public virtual bool Save(IBusinessEntity entity)
    {
        if (entity == null) throw new NullReferenceException("Parameter 'entity' cannot be null");
        return Save(entity.GetInternalData());
    }

    /// <summary>Saves a batch of business entities to the back end</summary>
    /// <param name="entities">Business entity array that is to be saved</param>
    /// <returns>True or False</returns>
    /// <remarks>
    /// Before the save operation, all the entities are verified. If any of them fails to verify, no save is attempted.
    /// Note that all entities are verified, no matter whether others had already exposed problems.
    /// All entities will be saved within a transaction. If any of the entities fail to save, the transaction is aborted.
    /// </remarks>
    public virtual bool Save(IEnumerable<IBusinessEntity> entities)
    {
        var masterDataSets = new List<DataSet>();
        foreach (var entity in entities)
            masterDataSets.Add(entity.GetInternalData());
        return Save(masterDataSets);
    }

    /// <summary>Saves a batch of DataSets to the back end.</summary>
    /// <param name="masterDataSets">DataSet array that is to be saved</param>
    /// <returns>True or False</returns>
    /// <remarks>
    /// Before the save operation, all the DataSets are verified. If any of them fails to verify, no save is attempted.
    /// Note that all DataSets are verified, no matter whether others had already exposed problems.
    /// All DataSets will be saved within a transaction. If any of the DataSets fail to save, the transaction is aborted.
    /// </remarks>
    public virtual bool Save(IEnumerable<DataSet> masterDataSets)
    {
        // Before we do anything else, we need to make sure that all the DataSets contain valid data.
        var dataVerifiedOk = true;
        var dataSetsToVerify = masterDataSets as DataSet[] ?? masterDataSets.ToArray();
        foreach (var verifyDataSet in dataSetsToVerify)
            if (!Verify(verifyDataSet))
                dataVerifiedOk = false;

        if (!dataVerifiedOk)
            if (!AllowSaveWithViolations)
            {
                // There are violations. The question is: How bad are they?
                // This object feels that critical violations should not be
                // allowed (which is the recommended default), so if we have 
                // critical errors, we must stop the process.
                var foundViolations = false;
                foreach (var violationDataSet in dataSetsToVerify)
                    if (DataSetHasViolations(violationDataSet, RuleViolationType.Violation))
                        foundViolations = true;
                if (foundViolations) return false;

                // We did not find critical errors in the data.
                // Now we need to check whether it is OK to save
                // with warnings.
                if (!AllowSaveWithWarnings) return false;
            }

        // We check for app roles
        if (!string.IsNullOrEmpty(AppRole)) DataService.ApplyAppRole(AppRole, AppRolePassword);

        // We are good to go. We wrap all saving operations into a transaction
        if (!BeginTransaction()) throw new TransactionException("Cannot start transaction.");
        foreach (var masterDataSet in dataSetsToVerify)
        {
            // We use this variable to determine whether save operations are executes successfully
            var saved = false;

            // Before we save the master entity, we process all deletions that may have occured
            // in secondary entities
            SaveMode = DataSaveMode.DeletesOnly;
            var pkType = GetPrimaryKeyType();
            if (masterDataSet.Tables[MasterEntity].Rows.Count > 0 && masterDataSet.Tables[MasterEntity].Rows[0].RowState != DataRowState.Deleted)
                switch (pkType)
                {
                    case KeyType.Guid:
                        saved = SaveSecondaryTables((Guid) masterDataSet.Tables[MasterEntity].Rows[0][PrimaryKeyField], masterDataSet);
                        break;
                    case KeyType.Integer:
                        saved = SaveSecondaryTables((int) masterDataSet.Tables[MasterEntity].Rows[0][PrimaryKeyField], masterDataSet);
                        break;
                    case KeyType.IntegerAutoIncrement:
                        saved = SaveSecondaryTables((int) masterDataSet.Tables[MasterEntity].Rows[0][PrimaryKeyField], masterDataSet);
                        break;
                    case KeyType.String:
                        saved = SaveSecondaryTables(masterDataSet.Tables[MasterEntity].Rows[0][PrimaryKeyField].ToString(), masterDataSet);
                        break;
                }
            else
                switch (pkType)
                {
                    case KeyType.Guid:
                        saved = SaveSecondaryTables(Guid.Empty, masterDataSet);
                        break;
                    case KeyType.Integer:
                        saved = SaveSecondaryTables(0, masterDataSet);
                        break;
                    case KeyType.IntegerAutoIncrement:
                        saved = SaveSecondaryTables(0, masterDataSet);
                        break;
                    case KeyType.String:
                        saved = SaveSecondaryTables(string.Empty, masterDataSet);
                        break;
                }

            SaveMode = DataSaveMode.AllChanges;
            if (!saved)
            {
                // We reset the app role if we set a role
                if (!string.IsNullOrEmpty(AppRole)) DataService.RevertAppRole();

                // The operation failed. We need to roll back our update attempt!
                if (!AbortTransaction()) throw new TransactionException("Cannot roll back transaction.");
                return false;
            }

            // We save the master entity
            saved = SaveMasterEntity(masterDataSet.Tables[MasterEntity]);

            // If we save the master entity and no errors occured, we proceed to saving changes
            // and additions in the secondary tables. Otherwise, we revert.
            if (saved != true)
            {
                // We reset the app role if we set a role
                if (!string.IsNullOrEmpty(AppRole)) DataService.RevertAppRole();

                // There already was a problem. We do not even have to go further...
                if (!AbortTransaction()) throw new TransactionException("Cannot roll back transaction.");
                return false;
            }

            // We also provide an opportunity to save secondary tables, without having to override this method
            // This time, we process inserts and updates, but no deletes.
            SaveMode = DataSaveMode.AllChangesExceptDeletes;
            pkType = GetPrimaryKeyType();
            if (masterDataSet.Tables[MasterEntity].Rows.Count > 0 && masterDataSet.Tables[MasterEntity].Rows[0].RowState != DataRowState.Deleted)
                switch (pkType)
                {
                    case KeyType.Guid:
                        saved = SaveSecondaryTables((Guid) masterDataSet.Tables[MasterEntity].Rows[0][PrimaryKeyField], masterDataSet);
                        break;
                    case KeyType.Integer:
                        saved = SaveSecondaryTables((int) masterDataSet.Tables[MasterEntity].Rows[0][PrimaryKeyField], masterDataSet);
                        break;
                    case KeyType.IntegerAutoIncrement:
                        saved = SaveSecondaryTables((int) masterDataSet.Tables[MasterEntity].Rows[0][PrimaryKeyField], masterDataSet);
                        break;
                    case KeyType.String:
                        saved = SaveSecondaryTables(masterDataSet.Tables[MasterEntity].Rows[0][PrimaryKeyField].ToString(), masterDataSet);
                        break;
                }
            else
                switch (pkType)
                {
                    case KeyType.Guid:
                        saved = SaveSecondaryTables(Guid.Empty, masterDataSet);
                        break;
                    case KeyType.Integer:
                        saved = SaveSecondaryTables(0, masterDataSet);
                        break;
                    case KeyType.IntegerAutoIncrement:
                        saved = SaveSecondaryTables(0, masterDataSet);
                        break;
                    case KeyType.String:
                        saved = SaveSecondaryTables(string.Empty, masterDataSet);
                        break;
                }

            SaveMode = DataSaveMode.AllChanges;
            if (!saved)
            {
                // We reset the app role if we set a role
                if (!string.IsNullOrEmpty(AppRole)) DataService.RevertAppRole();

                // The operation failed. We need to roll back our update attempt!
                if (!AbortTransaction()) throw new TransactionException("Cannot roll back transaction.");
            }
        }

        // We reset the app role if we set a role
        if (!string.IsNullOrEmpty(AppRole)) DataService.RevertAppRole();

        // Great! We saved the record without problems
        if (!CommitTransaction()) throw new TransactionException("Failed to commit transaction.");

        return true;
    }

    /// <summary>Deletes a single item from the master entity based on its primary key.</summary>
    /// <param name="entityKey">Item primary key</param>
    /// <returns>True or False</returns>
    public virtual bool Delete(string entityKey)
    {
        var pkType = GetPrimaryKeyType();
        switch (pkType)
        {
            case KeyType.Guid:
                return Delete(new Guid(entityKey));
            case KeyType.Integer:
                return Delete(int.Parse(entityKey, NumberFormatInfo.InvariantInfo));
            case KeyType.IntegerAutoIncrement:
                return Delete(int.Parse(entityKey, NumberFormatInfo.InvariantInfo));
            case KeyType.String:
                if (BeginTransaction())
                    if (DeleteSecondaryTables(entityKey))
                    {
                        var deleteCommand = DataService.BuildDeleteCommand(MasterEntity, PrimaryKeyField, entityKey, DeleteMethod);
                        if (ExecuteNonQuery(deleteCommand) > 0)
                        {
                            CommitTransaction();
                            return true;
                        }
                    }
                    else
                    {
                        AbortTransaction();
                        return false;
                    }
                else
                    throw new TransactionException("Cannot start transaction.");

                break;
            default:
                throw new UnsupportedKeyTypeException(GetPrimaryKeyType());
        }

        return false;
    }

    /// <summary>Deletes a single item from the master entity based on its primary key.</summary>
    /// <param name="entityKey">Item primary key</param>
    /// <returns>True or False</returns>
    public virtual bool Delete(Guid entityKey)
    {
        // We check if the current business object uses Guid types
        if (GetPrimaryKeyType() != KeyType.Guid) throw new UnsupportedKeyTypeException(KeyType.Guid);

        // We are ready to go.
        if (BeginTransaction())
            if (DeleteSecondaryTables(entityKey))
            {
                var deleteCommand = DataService.BuildDeleteCommand(MasterEntity, PrimaryKeyField, entityKey, DeleteMethod);
                if (ExecuteNonQuery(deleteCommand) > 0)
                {
                    CommitTransaction();
                    return true;
                }
            }
            else
            {
                AbortTransaction();
                return false;
            }
        else
            throw new TransactionException("Cannot start transaction.");

        return false;
    }

    /// <summary>Deletes a single item from the master entity based on its primary key.</summary>
    /// <param name="entityKey">Item primary key</param>
    /// <returns>True or False</returns>
    public virtual bool Delete(int entityKey)
    {
        // We check if the current business object uses Guid types
        var pkType = GetPrimaryKeyType();
        if (pkType != KeyType.Integer && pkType != KeyType.IntegerAutoIncrement) throw new UnsupportedKeyTypeException(KeyType.Integer);

        // We are ready to go.
        if (BeginTransaction())
            if (DeleteSecondaryTables(entityKey))
            {
                var deleteCommand = DataService.BuildDeleteCommand(MasterEntity, PrimaryKeyField, entityKey, DeleteMethod);
                if (ExecuteNonQuery(deleteCommand) > 0)
                {
                    CommitTransaction();
                    return true;
                }
            }
            else
            {
                AbortTransaction();
                return false;
            }
        else
            throw new TransactionException("Cannot start transaction.");

        return false;
    }

    /// <summary>This method retrieves all records and all fields for the main entity.</summary>
    /// <returns>DataSet</returns>
    public virtual DataSet GetList()
    {
        using (var selectCommand = DataService.BuildAllRecordsQueryCommand(MasterEntity, DefaultFields, DefaultOrder, QueryMethod))
            return ExecuteQuery(selectCommand, MasterEntity);
    }

    /// <summary>This method can be used to load a single record DataSet based on an entity primary key.</summary>
    /// <param name="entityKey">Entity primary key</param>
    /// <returns>DataSet</returns>
    public virtual DataSet LoadEntity(string entityKey)
    {
        switch (PrimaryKeyType)
        {
            case KeyType.Guid:
                return LoadEntity(new Guid(entityKey));
            case KeyType.Integer:
                return LoadEntity(int.Parse(entityKey, NumberFormatInfo.InvariantInfo));
            case KeyType.IntegerAutoIncrement:
                return LoadEntity(int.Parse(entityKey, NumberFormatInfo.InvariantInfo));
            case KeyType.String:
                // We are ready to go.
                var comLoad = DataService.BuildSingleRecordQueryCommand(MasterEntity, "*", PrimaryKeyField, entityKey, QueryMethod);
                var dsInternal = ExecuteQuery(comLoad, MasterEntity);
                LoadSecondaryTables(entityKey, dsInternal);
                return dsInternal;
            default:
                throw new UnsupportedKeyTypeException(GetPrimaryKeyType());
        }
    }

    /// <summary>This method can be used to load a single record DataSet based on an entity primary key.</summary>
    /// <param name="entityKey">Entity primary key</param>
    /// <returns>DataSet</returns>
    public virtual DataSet LoadEntity(Guid entityKey)
    {
        // We check if the current business object uses Guid types
        if (GetPrimaryKeyType() != KeyType.Guid) throw new UnsupportedKeyTypeException(KeyType.Guid);

        // We are ready to go.
        var loadCommand = DataService.BuildSingleRecordQueryCommand(MasterEntity, "*", PrimaryKeyField, entityKey, QueryMethod);
        var dsInternal = ExecuteQuery(loadCommand, MasterEntity);
        LoadSecondaryTables(entityKey, dsInternal);
        return dsInternal;
    }

    /// <summary>This method can be used to load a single record DataSet based on an entity primary key.</summary>
    /// <param name="entityKey">Entity primary key</param>
    /// <returns>DataSet</returns>
    public virtual DataSet LoadEntity(int entityKey)
    {
        // We check if the current business object uses Guid types
        var pkType = GetPrimaryKeyType();
        if (pkType != KeyType.Integer && pkType != KeyType.IntegerAutoIncrement) throw new UnsupportedKeyTypeException(KeyType.Guid);

        // We are ready to go.
        var loadCommand = DataService.BuildSingleRecordQueryCommand(MasterEntity, "*", PrimaryKeyField, entityKey, QueryMethod);
        var dsInternal = ExecuteQuery(loadCommand, MasterEntity);
        LoadSecondaryTables(entityKey, dsInternal);
        return dsInternal;
    }

    /// <summary>Creates a new master entity record.</summary>
    /// <returns>DataSet with new master entity record.</returns>
    public virtual DataSet AddNew()
    {
        var internalDataSet = NewMasterEntity();
        switch (GetPrimaryKeyType())
        {
            case KeyType.Guid:
                AddNewSecondaryTables((Guid) internalDataSet.Tables[MasterEntity].Rows[0][PrimaryKeyField], internalDataSet);
                break;
            case KeyType.Integer:
                AddNewSecondaryTables((int) internalDataSet.Tables[MasterEntity].Rows[0][PrimaryKeyField], internalDataSet);
                break;
            case KeyType.IntegerAutoIncrement:
                AddNewSecondaryTables((int) internalDataSet.Tables[MasterEntity].Rows[0][PrimaryKeyField], internalDataSet);
                break;
            case KeyType.String:
                AddNewSecondaryTables((string) internalDataSet.Tables[MasterEntity].Rows[0][PrimaryKeyField], internalDataSet);
                break;
        }

        return internalDataSet;
    }

    /// <summary>This method can be used to log business rule violations. This method is usually called from within the Verify() method.</summary>
    /// <param name="currentDataSet">DataSet that contains the data that violated a rule.</param>
    /// <param name="tableName">Table that contains the violation.</param>
    /// <param name="fieldName">Field name that contains the violation.</param>
    /// <param name="rowIndex">Row (record) that contains the violation</param>
    /// <param name="violationType">Type (severity) of the violation.</param>
    /// <param name="ruleClass">Name of the rule that was broken (usually based on the name of the business rule class).</param>
    /// <param name="message">Plain text message</param>
    public virtual void LogBusinessRuleViolation(DataSet currentDataSet, string tableName, string fieldName, int rowIndex, RuleViolationType violationType, string message, string ruleClass)
    {
        if (currentDataSet == null) throw new NullReferenceException("Parameter 'currentDataSet' cannot be null.");

        // We need to make sure we have a place to store rule violations
        if (!currentDataSet.Tables.Contains(BrokenRulesTableName)) CreateBrokenRulesCollection(currentDataSet);

        // We can now log the rule violation in our table
        var rowViolation = currentDataSet.Tables[BrokenRulesTableName].NewRow();
        rowViolation["TableName"] = tableName;
        rowViolation["FieldName"] = fieldName;
        rowViolation["RowIndex"] = rowIndex;
        rowViolation["ViolationType"] = (int) violationType;
        rowViolation["Message"] = message;
        rowViolation["RuleClass"] = ruleClass;
        currentDataSet.Tables[BrokenRulesTableName].Rows.Add(rowViolation);
    }

    /// <summary>This method creates a new key for integer-key rows.</summary>
    /// <param name="entityName">Name of the entity the key is generated for</param>
    /// <param name="dataSetWithNewRecord">DataSet containing the new record the PK is for</param>
    /// <returns>Integer key</returns>
    public virtual int GetNewIntegerKey(string entityName, DataSet dataSetWithNewRecord)
    {
        if (dataSetWithNewRecord == null) throw new NullReferenceException("Parameter 'dataSetWithNewRecord' cannot be null.");
        if (GetPrimaryKeyType(entityName) == KeyType.IntegerAutoIncrement) return dataSetWithNewRecord.Tables[entityName].Rows.Count * -1;
        return 0;
    }

    /// <summary>
    /// This method creates a new key for string-key rows.
    /// By default, this method creates a GUID and turns it into a string.
    /// If different behavior is desired, this method can be overridden in subclasses.
    /// </summary>
    /// <param name="entityName">Name of the entity the key is generated for</param>
    /// <param name="dataSetWithNewRecords">DataSet containing the new record the PK is for</param>
    /// <returns>String key</returns>
    public virtual string GetNewStringKey(string entityName, DataSet dataSetWithNewRecords) => Guid.NewGuid().ToString();

    /// <summary>Implementation of IDisposable, in particular the Dispose() method.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>This method is called when the object initializes. Use this method to set configuration options.</summary>
    protected abstract void Configure();

    /// <summary>This method verifies a DataSet. It is designed to be overridden in subclasses.</summary>
    /// <param name="masterDataSet">DataSet</param>
    /// <param name="ruleType">Type of business rule the verification is to be limited to</param>
    /// <returns>True or False depending on whether or not the DataSet is valid.</returns>
    public virtual bool Verify(DataSet masterDataSet, Type ruleType)
    {
        // Since we start over, we want to start with a clear slate, so we remove all current violations.
        ClearBrokenRules(masterDataSet);
        // We apply default rules
        BusinessRules.ApplyRules(masterDataSet, ruleType);
        // We check whether we appear to have broken rules
        return !DataSetHasViolations(masterDataSet);
    }

    /// <summary>This method can be overridden in subclasses to load additional/secondary tables when the main entity loads.</summary>
    /// <param name="parentKey">PK of the parent record</param>
    /// <param name="existingDataSet">Existing DataSet the new records are to be added to</param>
    protected virtual void LoadSecondaryTables(Guid parentKey, DataSet existingDataSet) { }

    /// <summary>This method can be overridden in subclasses to load additional/secondary tables when the main entity loads.</summary>
    /// <param name="parentKey">PK of the parent record</param>
    /// <param name="existingDataSet">Existing DataSet the new records are to be added to</param>
    protected virtual void LoadSecondaryTables(int parentKey, DataSet existingDataSet) { }

    /// <summary>This method can be overridden in subclasses to load additional/secondary tables when the main entity loads.</summary>
    /// <param name="parentKey">PK of the parent record</param>
    /// <param name="existingDataSet">Existing DataSet the new records are to be added to</param>
    protected virtual void LoadSecondaryTables(string parentKey, DataSet existingDataSet) { }
    
    /// <summary>This method can be overridden in subclasses to load additional/secondary tables when the main entity loads.</summary>
    /// <param name="tableName">Name of the table that is to be loaded</param>
    /// <param name="parentKey">PK of the parent record</param>
    /// <param name="existingDataSet">Existing DataSet the new records are to be added to</param>
    public virtual void LoadSecondaryTablesOnDemand(string tableName, Guid parentKey, DataSet existingDataSet) { }

    /// <summary>This method can be overridden in subclasses to load additional/secondary tables when the main entity loads.</summary>
    /// <param name="tableName">Name of the table that is to be loaded</param>
    /// <param name="parentKey">PK of the parent record</param>
    /// <param name="existingDataSet">Existing DataSet the new records are to be added to</param>
    public virtual void LoadSecondaryTablesOnDemand(string tableName, int parentKey, DataSet existingDataSet) { }

    /// <summary>This method can be overridden in subclasses to load additional/secondary tables when the main entity loads.</summary>
    /// <param name="tableName">Name of the table that is to be loaded</param>
    /// <param name="parentKey">PK of the parent record</param>
    /// <param name="existingDataSet">Existing DataSet the new records are to be added to</param>
    public virtual void LoadSecondaryTablesOnDemand(string tableName, string parentKey, DataSet existingDataSet) { }

    /// <summary>This method can be overridden in subclasses to load additional/secondary tables async when the main entity loads.</summary>
    /// <param name="tableName">Name of the table that is to be loaded</param>
    /// <param name="parentKey">PK of the parent record</param>
    /// <param name="existingDataSet">Existing DataSet the new records are to be added to</param>
    /// <param name="callback">Callback object</param>
    public virtual void LoadSecondaryTablesAsync(string tableName, Guid parentKey, DataSet existingDataSet, AsyncCallback callback) { }

    /// <summary>This method can be overridden in subclasses to load additional/secondary tables async when the main entity loads.</summary>
    /// <param name="tableName">Name of the table that is to be loaded</param>
    /// <param name="parentKey">PK of the parent record</param>
    /// <param name="existingDataSet">Existing DataSet the new records are to be added to</param>
    /// <param name="callback">Callback object</param>
    public virtual void LoadSecondaryTablesAsync(string tableName, int parentKey, DataSet existingDataSet, AsyncCallback callback) { }

    /// <summary>This method can be overridden in subclasses to load additional/secondary tables async when the main entity loads.</summary>
    /// <param name="tableName">Name of the table that is to be loaded</param>
    /// <param name="parentKey">PK of the parent record</param>
    /// <param name="existingDataSet">Existing DataSet the new records are to be added to</param>
    /// <param name="callback">Callback object</param>
    public virtual void LoadSecondaryTablesAsync(string tableName, string parentKey, DataSet existingDataSet, AsyncCallback callback) { }

    /// <summary>This method can be overridden in subclasses to save additional/secondary tables when the main entity saves.</summary>
    /// <param name="parentKey">PK of the parent record</param>
    /// <param name="existingDataSet">Existing DataSet the new records are to be added to</param>
    /// <returns>True or False depending on the success of the operation</returns>
    protected virtual bool SaveSecondaryTables(Guid parentKey, DataSet existingDataSet) => true;

    /// <summary>This method can be overridden in subclasses to save additional/secondary tables when the main entity saves.</summary>
    /// <param name="parentKey">PK of the parent record</param>
    /// <param name="existingDataSet">Existing DataSet the new records are to be added to</param>
    /// <returns>True or False depending on the success of the operation</returns>
    protected virtual bool SaveSecondaryTables(int parentKey, DataSet existingDataSet) => true;

    /// <summary>This method can be overridden in subclasses to save additional/secondary tables when the main entity saves.</summary>
    /// <param name="parentKey">PK of the parent record</param>
    /// <param name="existingDataSet">Existing DataSet the new records are to be added to</param>
    /// <returns>True or False depending on the success of the operation</returns>
    protected virtual bool SaveSecondaryTables(string parentKey, DataSet existingDataSet) => true;

    /// <summary>This method can be overridden in subclasses to add new additional/secondary tables when the main entity adds a new row.</summary>
    /// <param name="parentKey">PK of the parent record</param>
    /// <param name="existingDataSet">Existing DataSet the new records are to be added to</param>
    protected virtual void AddNewSecondaryTables(Guid parentKey, DataSet existingDataSet) { }

    /// <summary>This method can be overridden in subclasses to add new additional/secondary tables when the main entity adds a new row.</summary>
    /// <param name="parentKey">PK of the parent record</param>
    /// <param name="existingDataSet">Existing DataSet the new records are to be added to</param>
    protected virtual void AddNewSecondaryTables(int parentKey, DataSet existingDataSet) { }

    /// <summary>This method can be overridden in subclasses to add new additional/secondary tables when the main entity adds a new row.</summary>
    /// <param name="parentKey">PK of the parent record</param>
    /// <param name="existingDataSet">Existing DataSet the new records are to be added to</param>
    protected virtual void AddNewSecondaryTables(string parentKey, DataSet existingDataSet) { }

    /// <summary>This method can be overridden in subclasses to delete additional/secondary tables when the main entity deletes a row.</summary>
    /// <param name="parentKey">PK of the parent record</param>
    /// <returns>True or False depending on the success of the operation</returns>
    protected virtual bool DeleteSecondaryTables(Guid parentKey) => true;

    /// <summary>This method can be overridden in subclasses to delete additional/secondary tables when the main entity deletes a row.</summary>
    /// <param name="parentKey">PK of the parent record</param>
    /// <returns>True or False depending on the success of the operation</returns>
    protected virtual bool DeleteSecondaryTables(int parentKey) => true;

    /// <summary>This method can be overridden in subclasses to delete additional/secondary tables when the main entity deletes a row.</summary>
    /// <param name="parentKey">PK of the parent record</param>
    /// <returns>True or False depending on the success of the operation</returns>
    protected virtual bool DeleteSecondaryTables(string parentKey) => true;

    /// <summary>Sets the primary key type for the master entity as well as the default for secondary tables primary key type.</summary>
    /// <param name="keyType">Primary key type</param>
    protected virtual void SetPrimaryKeyType(KeyType keyType) => PrimaryKeyType = keyType;

    /// <summary>Sets the primary key type for the master entity as well as the default for secondary tables primary key type.</summary>
    /// <param name="tableName">Name of the table the primary key </param>
    /// <param name="keyType">Primary key type</param>
    protected virtual void SetPrimaryKeyType(string tableName, KeyType keyType)
    {
        tableName = tableName.ToLower(CultureInfo.InvariantCulture);
        if (primaryKeyTypes.Contains(tableName))
            primaryKeyTypes[tableName] = keyType;
        else
            primaryKeyTypes.Add(tableName, keyType);
    }

    /// <summary>Returns the configured primary key type for the specified table.</summary>
    /// <param name="tableName">Table name</param>
    /// <returns>Key type</returns>
    public virtual KeyType GetPrimaryKeyType(string tableName)
    {
        if (string.IsNullOrEmpty(tableName)) throw new NullReferenceException("Parameter 'tableName' cannot be null.");

        tableName = tableName.ToLower(CultureInfo.InvariantCulture);
        if (primaryKeyTypes.Contains(tableName)) return (KeyType) primaryKeyTypes[tableName];
        return PrimaryKeyType;
    }

    /// <summary>Returns the configured primary key type for the specified table.</summary>
    /// <returns>Key type</returns>
    public virtual KeyType GetPrimaryKeyType() => GetPrimaryKeyType(MasterEntity);

    /// <summary>Shares a data context with another business object.</summary>
    /// <param name="primaryDataBusinessObject">The business object whose data context is to be applied to this business object</param>
    /// <returns>True or false</returns>
    public virtual bool ShareDataContext(BusinessObject primaryDataBusinessObject) => ShareDataContext(primaryDataBusinessObject, ContextSharingRestriction.ExactMatchOnly);

    /// <summary>Shares a data context with another business entity.</summary>
    /// <param name="primaryDataBusinessEntity">The business entity whose data context is to be applied to this business object</param>
    /// <returns>True or false</returns>
    public virtual bool ShareDataContext(IBusinessEntity primaryDataBusinessEntity)
    {
        if (primaryDataBusinessEntity == null) throw new NullReferenceException("Parameter 'primaryDataBusinessEntity' cannot be null.");

        if (primaryDataBusinessEntity.AssociatedBusinessObject is BusinessObject businessObject)
            return ShareDataContext(businessObject, ContextSharingRestriction.ExactMatchOnly);

        LastErrorMessage = "Cannot share data context business object.";
        return false;
    }

    /// <summary>Shares a data context with another business object.</summary>
    /// <param name="primaryDataBusinessObject">The business object whose data context is to be applied to this business object</param>
    /// <param name="restriction">Specifies restrictions under which data contexts can be shared.</param>
    /// <returns>True or false</returns>
    public virtual bool ShareDataContext(BusinessObject primaryDataBusinessObject, ContextSharingRestriction restriction)
    {
        if (primaryDataBusinessObject == null) throw new NullReferenceException("Parameter 'primaryDataBusinessObject' cannot be null.");

        // Before we share the data context, we have to check 
        // whether it is even possible to share contexts with 
        // the other object
        var canShare = false;
        switch (restriction)
        {
            case ContextSharingRestriction.None:
                canShare = true;
                break;
            case ContextSharingRestriction.DatabaseMatch:
                if (DataConfigurationPrefix == primaryDataBusinessObject.DataConfigurationPrefix)
                    canShare = true;
                else
                    LastErrorMessage = "Data contexts can only be shared if they have the same data configuration prefix. (Restriction = DatabaseMatch)";

                break;
            case ContextSharingRestriction.ExactMatchOnly:
                if (DataService.ServiceInstanceIdentifier == primaryDataBusinessObject.DataService.ServiceInstanceIdentifier)
                    canShare = true;
                else
                    LastErrorMessage = "Data contexts can only be shared if they access the same database in the same way. (Restriction = ExactMatch)";

                break;
        }

        // If sharing is possible, we now set the other object's context on this object
        if (canShare) internalDataContext = primaryDataBusinessObject.SharedDataContext;
        return canShare;
    }

    /// <summary>Sets the shared data context.</summary>
    /// <param name="dataContext">Shared data context</param>
    /// <returns>True of false depending on success</returns>
    public virtual bool ShareDataContext(SharedDataContext dataContext)
    {
        internalDataContext = dataContext;
        return true;
    }

    /// <summary>Sets the current business object to use its own data context, rather than a context set by ShareDataContext()</summary>
    /// <returns>True or false</returns>
    public virtual bool UnshareDataContext()
    {
        internalDataContext = null;
        return true;
    }

    /// <summary>Clears all broken rule information out of the current DataSet</summary>
    /// <param name="currentDataSet">The DataSet that contains the data that has no broken rules.</param>
    protected virtual void ClearBrokenRules(DataSet currentDataSet)
    {
        if (currentDataSet.Tables.Contains(BrokenRulesTableName)) currentDataSet.Tables[BrokenRulesTableName].Clear();
    }

    /// <summary>Checks whether a certain DataSet has business rules violations.</summary>
    /// <param name="currentDataSet">DataSet that may contain violations</param>
    /// <returns>True if violations are present</returns>
    public virtual bool DataSetHasViolations(DataSet currentDataSet)
    {
        if (currentDataSet == null) throw new NullReferenceException("Parameter 'currentDataSet' cannot be null.");
        if (currentDataSet.Tables.Contains(BrokenRulesTableName))
            if (currentDataSet.Tables[BrokenRulesTableName].Rows.Count > 0)
                return true;
        return false;
    }

    /// <summary>Checks whether a certain DataSet has business rules violations of a certain type.</summary>
    /// <param name="currentDataSet">DataSet that may contain violations</param>
    /// <param name="violationType">Defines the type of violation we are looking for.</param>
    /// <returns>True if violations are present</returns>
    public virtual bool DataSetHasViolations(DataSet currentDataSet, RuleViolationType violationType)
    {
        if (currentDataSet == null) throw new NullReferenceException("Parameter 'currentDataSet' cannot be null.");

        if (currentDataSet.Tables.Contains(BrokenRulesTableName))
        {
            if (currentDataSet.Tables[BrokenRulesTableName].Rows.Count <= 0) return false;
            // There are violations, so we need to check for the type we are interested in.
            var rows = currentDataSet.Tables[BrokenRulesTableName].Select("ViolationType = " + StringHelper.ToString((int) violationType));
            if (rows.Length > 0) return true;
        }

        return false;
    }

    /// <summary>This method makes sure we have a way to store broken rule information in the DataSet.</summary>
    /// <remarks>This method is intended for internal use only!</remarks>
    /// <param name="currentDataSet">DataSet that may contain data that breaks rules.</param>
    internal virtual void CreateBrokenRulesCollection(DataSet currentDataSet)
    {
        var containedTable = true;
        if (!currentDataSet.Tables.Contains(BrokenRulesTableName))
        {
            // This DataSet does not contains a broken rules table.
            // We need that table, since we want to use it to store broken
            // rules information. For this reason, we create that table now.
            currentDataSet.Tables.Add(BrokenRulesTableName);
            containedTable = false;
        }

        // We also need to add all the fields we need
        if (!currentDataSet.Tables[BrokenRulesTableName].Columns.Contains("TableName")) currentDataSet.Tables[BrokenRulesTableName].Columns.Add("TableName", typeof(string));
        if (!currentDataSet.Tables[BrokenRulesTableName].Columns.Contains("FieldName")) currentDataSet.Tables[BrokenRulesTableName].Columns.Add("FieldName", typeof(string));
        if (!currentDataSet.Tables[BrokenRulesTableName].Columns.Contains("RowIndex")) currentDataSet.Tables[BrokenRulesTableName].Columns.Add("RowIndex", typeof(int));
        if (!currentDataSet.Tables[BrokenRulesTableName].Columns.Contains("ViolationType")) currentDataSet.Tables[BrokenRulesTableName].Columns.Add("ViolationType", typeof(int));
        if (!currentDataSet.Tables[BrokenRulesTableName].Columns.Contains("RuleClass")) currentDataSet.Tables[BrokenRulesTableName].Columns.Add("RuleClass", typeof(string));
        if (!currentDataSet.Tables[BrokenRulesTableName].Columns.Contains("Message")) currentDataSet.Tables[BrokenRulesTableName].Columns.Add("Message", typeof(string));

        if (!containedTable) CreateBrokenRulesCollectionCustomizations(currentDataSet);
    }

    /// <summary>
    /// This method is called whenever a new broken rules collection is created.
    /// This method is designed to be overridden in subclasses. It can be used
    /// to customize the broken rule creation behavior, since the CreateBrokenRulesCollection()
    /// method is not virtual (and should not be virtual due to its 'internal' status).
    /// </summary>
    /// <param name="currentDataSet">DataSets that holds the broken rules collection data</param>
    protected virtual void CreateBrokenRulesCollectionCustomizations(DataSet currentDataSet) { }

    /// <summary>
    /// Fires an SQL Command to the back end data server and returns a DataSet.
    /// Note that this method is designed to be called from other methods only (not from the outside).
    /// If you need to access this method from outside this object, add another method that uses this method.
    /// Note: The name of the newly created entity will default to the master entity name.
    /// </summary>
    /// <param name="command">IDbCommand object</param>
    /// <returns>Result DataSet</returns>
    protected virtual DataSet ExecuteQuery(IDbCommand command)
    {
        // We use the current DataService and execute the specified command.
        DataSet dsResult;
        try
        {
            var service = DataService;

            // We check for app roles
            if (!string.IsNullOrEmpty(AppRole)) service.ApplyAppRole(AppRole, AppRolePassword);

            dsResult = service.ExecuteQuery(command, MasterEntity);

            // We reset the app role if we set a role
            if (!string.IsNullOrEmpty(AppRole)) service.RevertAppRole();
        }
        catch (Exception ex)
        {
            LastErrorMessage = ex.Message;
            throw;
        }

        return dsResult;
    }

    /// <summary>
    /// Fires an SQL Command to the back end data server and returns a DataSet.
    /// Note that this method is designed to be called from other methods only (not from the outside).
    /// If you need to access this method from outside this object, add another method that uses this method.
    /// </summary>
    /// <param name="command">IDbCommand object</param>
    /// <param name="entityName">Table name of the generated entity (for instance, if you SELECT * From Names, the entity name might be "names").</param>
    /// <returns>Result DataSet</returns>
    protected virtual DataSet ExecuteQuery(IDbCommand command, string entityName)
    {
        // We use the current DataService and execute the specified command.
        DataSet dsResult;
        try
        {
            var service = DataService;
            // We check for app roles
            if (!string.IsNullOrEmpty(AppRole)) service.ApplyAppRole(AppRole, AppRolePassword);

            dsResult = service.ExecuteQuery(command, entityName);

            // We reset the app role if we set a role
            if (!string.IsNullOrEmpty(AppRole)) service.RevertAppRole();
        }
        catch (Exception ex)
        {
            LastErrorMessage = ex.Message;
            throw;
        }

        return dsResult;
    }

    /// <summary>
    /// Fires an SQL Command to the back end data server and returns a DataSet.
    /// Note that this method is designed to be called from other methods only (not from the outside).
    /// If you need to access this method from outside this object, add another method that uses this method.
    /// </summary>
    /// <param name="command">SqlCommand object</param>
    /// <param name="entityName">Table name of the generated entity (for instance, if you SELECT * From Names, the entity name might be "names").</param>
    /// <param name="existingDataSet">Use this parameter if the result is to be added to an existing DataSet</param>
    /// <returns>DataSet</returns>
    protected virtual DataSet ExecuteQuery(IDbCommand command, string entityName, DataSet existingDataSet)
    {
        // We use the current DataService and execute the specified command.
        try
        {
            var service = DataService;

            // We check for app roles
            if (!string.IsNullOrEmpty(AppRole)) service.ApplyAppRole(AppRole, AppRolePassword);

            existingDataSet = service.ExecuteQuery(command, entityName, existingDataSet);

            // We reset the app role if we set a role
            if (!string.IsNullOrEmpty(AppRole)) service.RevertAppRole();
        }
        catch (Exception ex)
        {
            LastErrorMessage = ex.Message;
            throw;
        }

        return existingDataSet;
    }

    /// <summary>
    /// Fires an SQL Command to the back end data server and returns a DataSet.
    /// Note that this method is designed to be called from other methods only (not from the outside).
    /// If you need to access this method from outside this object, add another method that uses this method.
    /// </summary>
    /// <param name="command">SqlCommand object</param>
    /// <param name="entityName">Table name of the generated entity (for instance, if you SELECT * From Names, the entity name might be "names").</param>
    /// <param name="existingDataSet">Use this parameter if the result is to be added to an existing DataSet</param>
    protected virtual async Task ExecuteQueryAsync(IDbCommand command, string entityName, DataSet existingDataSet)
    {
        // We use the current DataService and execute the specified command.
        var service = DataService;

        // We check for app roles
        if (!string.IsNullOrEmpty(AppRole)) service.ApplyAppRole(AppRole, AppRolePassword);

        await service.ExecuteQueryAsync(command, entityName, existingDataSet);

        // We reset the app role if we set a role
        if (!string.IsNullOrEmpty(AppRole)) service.RevertAppRole();
    }

    /// <summary>This method executes an SQL Command and returns the number of affected records.</summary>
    /// <param name="command">IDbCommand Object</param>
    /// <returns>Number of affected records.</returns>
    protected virtual int ExecuteNonQuery(IDbCommand command)
    {
        try
        {
            var service = DataService;

            // We check for app roles
            if (!string.IsNullOrEmpty(AppRole)) service.ApplyAppRole(AppRole, AppRolePassword);

            var affectedRecords = service.ExecuteNonQuery(command);

            // We reset the app role if we set a role
            if (!string.IsNullOrEmpty(AppRole)) service.RevertAppRole();

            return affectedRecords;
        }
        catch (Exception ex)
        {
            LastErrorMessage = ex.Message;
            throw;
        }
    }

    /// <summary>Runs a scalar command and returns the value.</summary>
    /// <param name="command">IDbCommand object</param>
    /// <returns>Value object</returns>
    protected virtual object ExecuteScalar(IDbCommand command)
    {
        try
        {
            var service = DataService;

            // We check for app roles
            if (!string.IsNullOrEmpty(AppRole)) service.ApplyAppRole(AppRole, AppRolePassword);

            var result = service.ExecuteScalar(command);

            // We reset the app role if we set a role
            if (!string.IsNullOrEmpty(AppRole)) service.RevertAppRole();

            return result;
        }
        catch (Exception ex)
        {
            LastErrorMessage = ex.Message;
            throw;
        }
    }

    /// <summary>
    /// Executes a stored procedure using the current data service and returns the result as a DataSet
    /// The table in the DataSet will be named after the master entity in this BO.
    /// Note: In and out parameters are passed as part of the parameters collection.
    /// </summary>
    /// <param name="command">IDbCommand object</param>
    /// <returns>DataSet</returns>
    public virtual DataSet ExecuteStoredProcedureQuery(IDbCommand command)
    {
        try
        {
            var service = DataService;

            // We check for app roles
            if (!string.IsNullOrEmpty(AppRole)) service.ApplyAppRole(AppRole, AppRolePassword);

            var dsResult = service.ExecuteStoredProcedureQuery(command, MasterEntity);

            // We reset the app role if we set a role
            if (!string.IsNullOrEmpty(AppRole)) service.RevertAppRole();

            return dsResult;
        }
        catch (Exception ex)
        {
            LastErrorMessage = ex.Message;
            throw;
        }
    }

    /// <summary>Executes a stored procedure using the current data service and returns the result as a DataSet.</summary>
    /// <remarks>In and out parameters are passed as part of the parameters collection.</remarks>
    /// <param name="command">IDbCommand object</param>
    /// <param name="entityName">Name of the resulting entity in the DataSet</param>
    /// <returns>DataSet</returns>
    public virtual DataSet ExecuteStoredProcedureQuery(IDbCommand command, string entityName)
    {
        try
        {
            var service = DataService;

            // We check for app roles
            if (!string.IsNullOrEmpty(AppRole)) service.ApplyAppRole(AppRole, AppRolePassword);

            var dsResult = service.ExecuteStoredProcedureQuery(command, entityName);

            // We reset the app role if we set a role
            if (!string.IsNullOrEmpty(AppRole)) service.RevertAppRole();

            return dsResult;
        }
        catch (Exception ex)
        {
            LastErrorMessage = ex.Message;
            throw;
        }
    }

    /// <summary>Executes a stored procedure using the current data service and adds the result to an existing DataSet.</summary>
    /// <param name="command">IDbCommand object</param>
    /// <param name="entityName">Name of the resulting entity in the DataSet</param>
    /// <param name="existingDataSet">Existing DataSet the data is to be added to</param>
    /// <returns>DataSet</returns>
    public virtual DataSet ExecuteStoredProcedureQuery(IDbCommand command, string entityName, DataSet existingDataSet)
    {
        try
        {
            var service = DataService;

            // We check for app roles
            if (!string.IsNullOrEmpty(AppRole)) service.ApplyAppRole(AppRole, AppRolePassword);

            var dsResult = service.ExecuteStoredProcedureQuery(command, entityName, existingDataSet);

            // We reset the app role if we set a role
            if (!string.IsNullOrEmpty(AppRole)) service.RevertAppRole();

            return dsResult;
        }
        catch (Exception ex)
        {
            LastErrorMessage = ex.Message;
            throw;
        }
    }

    /// <summary>Executes a stored procedure</summary>
    /// <param name="command">IDbCommand object</param>
    /// <returns>True or False</returns>
    public virtual bool ExecuteStoredProcedure(IDbCommand command)
    {
        try
        {
            var service = DataService;

            // We check for app roles
            if (!string.IsNullOrEmpty(AppRole)) service.ApplyAppRole(AppRole, AppRolePassword);

            var result = service.ExecuteStoredProcedure(command);

            // We reset the app role if we set a role
            if (!string.IsNullOrEmpty(AppRole)) service.RevertAppRole();

            return result;
        }
        catch (Exception ex)
        {
            LastErrorMessage = ex.Message;
            throw;
        }
    }

    /// <summary>
    /// This helper method generates an SqlCommand object that contains the appropriate command
    /// to update the database with the changes reflected by the passed DataRow.
    /// This could be an insert, update, or delete command.
    /// </summary>
    /// <param name="changedRow">DataRow object that represents/contains the changed data.</param>
    /// <param name="primaryKeyField">Name of the primary key field contained in the row.</param>
    /// <param name="updateMode">Optimistic or pessimistic update mode.</param>
    /// <param name="updateMethod">Method used to update the database (commands, stored procedures,...)</param>
    /// <returns>Update command</returns>
    protected virtual IDbCommand BuildSqlUpdateCommand(DataRow changedRow, string primaryKeyField, DataRowUpdateMode updateMode = DataRowUpdateMode.ChangedFieldsOnly, DataRowProcessMethod updateMethod = DataRowProcessMethod.Default)
    {
        if (changedRow == null) throw new NullReferenceException("Parameter 'changedRow' cannot be null");
        return DataService.BuildUpdateCommand(changedRow, GetPrimaryKeyType(changedRow.Table.TableName), primaryKeyField, changedRow.Table.TableName, updateMode, updateMethod);
    }

    /// <summary>
    /// This helper method generates an SqlCommand object that contains the appropriate command
    /// to update the database with the changes reflected by the passed DataRow.
    /// This could be an insert, update, or delete command.
    /// </summary>
    /// <param name="changedRow">DataRow object that represents/contains the changed data.</param>
    /// <param name="primaryKeyField">Name of the primary key field contained in the row.</param>
    /// <param name="updateMode">Optimistic or pessimistic update mode.</param>
    /// <param name="updateMethod">Method used to update the database (commands, stored procedures,...)</param>
    /// <param name="tableName">Name of the table.</param>
    /// <param name="fieldNames">Names of the fields to be included in the update (all others will be ignored)</param>
    /// <param name="fieldMaps">List of key value pairs that can be used to map field names. For instance, if a field in the table is called MyId but in the database it is called ID, then one can add a key 'MyId' with a value of 'ID'</param>
    /// <returns>Update command</returns>
    protected virtual IDbCommand BuildSqlUpdateCommand(DataRow changedRow, string primaryKeyField, DataRowUpdateMode updateMode, DataRowProcessMethod updateMethod, string tableName, string[] fieldNames, Dictionary<string, string> fieldMaps)
    {
        if (changedRow == null) throw new NullReferenceException("Parameter 'changedRow' cannot be null");
        return DataService.BuildUpdateCommand(changedRow, GetPrimaryKeyType(changedRow.Table.TableName), primaryKeyField, tableName, updateMode, updateMethod, fieldNames, fieldMaps);
    }

    /// <summary>This method attempts to update an entire DataTable, assuming the table name is the same as the table name in the database.</summary>
    /// <param name="updatedTable">DataTable that contains the updated data</param>
    /// <param name="primaryKeyField">Primary key field used for this table.</param>
    /// <returns></returns>
    protected virtual bool SaveTable(DataTable updatedTable, string primaryKeyField)
    {
        if (updatedTable == null) return true;

        var retVal = true;

        for (var rowCounter = 0; rowCounter < updatedTable.Rows.Count; rowCounter++)
            if (updatedTable.Rows[rowCounter].RowState != DataRowState.Unchanged && updatedTable.Rows[rowCounter].RowState != DataRowState.Detached)
                if (SaveMode == DataSaveMode.AllChanges || SaveMode == DataSaveMode.DeletesOnly && updatedTable.Rows[rowCounter].RowState == DataRowState.Deleted || SaveMode == DataSaveMode.AllChangesExceptDeletes && updatedTable.Rows[rowCounter].RowState != DataRowState.Deleted)
                {
                    var updateCommand = BuildSqlUpdateCommand(updatedTable.Rows[rowCounter], primaryKeyField, UpdateMode, UpdateMethod);
                    if (updateCommand != null)
                        if (updatedTable.Columns[primaryKeyField].DataType == typeof(int) && GetPrimaryKeyType(updatedTable.TableName) == KeyType.IntegerAutoIncrement && updatedTable.Rows[rowCounter].RowState == DataRowState.Added)
                        {
                            // This is a new row for an integer (identity) primary key record. We need to issue a special 
                            // update command, and replace the current values.
                            var newKey = (int) ExecuteScalar(updateCommand);
                            if (newKey == -1)
                            {
                                // The insert failed.
                                retVal = false;
                            }
                            else
                            {
                                // Success. We need to make sure we update all the client-side data,
                                // since the key we had before was only temporary. We start out by
                                // replacing the current row key with the new one that was generated
                                // by the database and returned to us.
                                var intOldKey = (int) updatedTable.Rows[rowCounter][PrimaryKeyField];
                                // Identity fields are most likely read-only. In order to set the key
                                // we retrieved from the database, we need to temporarily allow updates.
                                var wasReadOnly = updatedTable.Columns[PrimaryKeyField].ReadOnly;
                                if (wasReadOnly) updatedTable.Columns[PrimaryKeyField].ReadOnly = false;
                                updatedTable.Rows[rowCounter][PrimaryKeyField] = newKey;
                                // If it was read-only before, then we now need to set that back,
                                // so we do not end up changing the environment/data.
                                if (wasReadOnly) updatedTable.Columns[PrimaryKeyField].ReadOnly = false;
                                // Now, we also need to make sure that all the child tables have a
                                // chance to update their foreign keys. We can not do this automatically,
                                // since we have no knowledge of other tables that may be related. 
                                // However, we will give the programmer a chance to do 
                                PrimaryKeyValueChanged(updatedTable.DataSet, updatedTable.TableName, intOldKey, newKey);
                                retVal = true;
                            }
                        }
                        else
                        {
                            var rowsAffected = ExecuteNonQuery(updateCommand);
                            if (rowsAffected < 1) retVal = false;
                        }
                    else
                        retVal = true;
                }

        // If the operation was successful, and we are either in "save all" or "save all except deletes"
        // mode, we accept changes to clear the "dirty" state of the table. 
        // Note: We do not clear the dirty state if this is a delete-only run, because in that case,
        // we always expect a second pass in "all except deletes" mode and we do not want to accept
        // changes before that, as that would flag all records as "not updated", which would effectively
        // disable the second save pass.
        if (SaveMode != DataSaveMode.DeletesOnly && retVal) updatedTable.AcceptChanges();

        return retVal;
    }

    /// <summary>This method attempts to update an the specified fields in the provided DataTable. The data table can map to a different internal table name in the data source</summary>
    /// <param name="updatedTable">DataTable that contains the updated data</param>
    /// <param name="primaryKeyField">Primary key field used for this table.</param>
    /// <param name="tableName">Name of the table.</param>
    /// <param name="fieldNames">Names of the fields to be included in the update (all others will be ignored)</param>
    /// <param name="fieldMaps">List of key value pairs that can be used to map field names. For instance, if a field in the table is called MyId but in the database it is called ID, then one can add a key 'MyId' with a value of 'ID'</param>
    /// <param name="acceptTableChanges">If set to <c>true</c> the table row will be flagged as not having changes after a successful save.</param>
    /// <returns></returns>
    protected virtual bool SaveTable(DataTable updatedTable, string primaryKeyField, string tableName, string[] fieldNames, Dictionary<string, string> fieldMaps, bool acceptTableChanges)
    {
        if (updatedTable == null) return true;

        var returnValue = true;

        for (var rowCounter = 0; rowCounter < updatedTable.Rows.Count; rowCounter++)
            if (updatedTable.Rows[rowCounter].RowState != DataRowState.Unchanged && updatedTable.Rows[rowCounter].RowState != DataRowState.Detached)
                if (SaveMode == DataSaveMode.AllChanges || SaveMode == DataSaveMode.DeletesOnly && updatedTable.Rows[rowCounter].RowState == DataRowState.Deleted || SaveMode == DataSaveMode.AllChangesExceptDeletes && updatedTable.Rows[rowCounter].RowState != DataRowState.Deleted)
                {
                    var updateCommand = BuildSqlUpdateCommand(updatedTable.Rows[rowCounter], primaryKeyField, UpdateMode, UpdateMethod, tableName, fieldNames, fieldMaps);
                    if (updateCommand != null)
                    {
                        // If this is a business object that uses auto-increment integer keys, and we have new records,
                        // we have to replace the current temporary key with a new key and propagate that change down 
                        // through child tables.
                        var primaryKeyFieldInMemory = primaryKeyField;
                        foreach (var key in fieldMaps.Keys)
                        {
                            var value = fieldMaps[key];
                            if (StringHelper.Compare(value, primaryKeyFieldInMemory))
                            {
                                primaryKeyFieldInMemory = key;
                                break;
                            }
                        }

                        if (updatedTable.Columns[primaryKeyFieldInMemory].DataType == typeof(int) && GetPrimaryKeyType(tableName) == KeyType.IntegerAutoIncrement && updatedTable.Rows[rowCounter].RowState == DataRowState.Added)
                        {
                            // This is a new row for an integer (identity) primary key record. We need to issue a special 
                            // update command, and replace the current values.
                            var newKey = (int) ExecuteScalar(updateCommand);
                            if (newKey == -1)
                                // The insert failed.
                            {
                                returnValue = false;
                            }
                            else
                            {
                                // Success. We need to make sure we update all the client-side data,
                                // since the key we had before was only temporary. We start out by
                                // replacing the current row key with the new one that was generated
                                // by the database and returned to us.
                                var oldKey = (int) updatedTable.Rows[rowCounter][primaryKeyFieldInMemory];
                                // Identity fields are most likely read-only. In order to set the key
                                // we retrieved from the database, we need to temporarily allow updates.
                                var wasReadOnly = updatedTable.Columns[primaryKeyFieldInMemory].ReadOnly;
                                if (wasReadOnly) updatedTable.Columns[primaryKeyFieldInMemory].ReadOnly = false;
                                updatedTable.Rows[rowCounter][primaryKeyFieldInMemory] = newKey;
                                // If it was read-only before, then we now need to set that back,
                                // so we do not end up changing the environment/data.
                                if (wasReadOnly) updatedTable.Columns[primaryKeyFieldInMemory].ReadOnly = false;
                                // Now, we also need to make sure that all the child tables have a
                                // chance to update their foreign keys. We can not do this automatically,
                                // since we have no knowledge of other tables that may be related. 
                                // However, we will give the programmer a chance to do 
                                PrimaryKeyValueChanged(updatedTable.DataSet, tableName, oldKey, newKey);
                                returnValue = true;
                            }
                        }
                        else
                        {
                            var rowsAffected = ExecuteNonQuery(updateCommand);
                            if (rowsAffected < 1) returnValue = false;
                        }
                    }
                    else
                        // Nothing to update here...
                    {
                        returnValue = true;
                    }
                }

        // If the operation was successful, and we are either in "save all" or "save all except deletes"
        // mode, we accept changes to clear the "dirty" state of the table. 
        // Note: We do not clear the dirty state if this is a delete-only run, because in that case,
        // we always expect a second pass in "all except deletes" mode and we do not want to accept
        // changes before that, as that would flag all records as "not updated", which would effectively
        // disable the second save pass.
        if (SaveMode != DataSaveMode.DeletesOnly && returnValue)
            if (acceptTableChanges)
                updatedTable.AcceptChanges();

        return returnValue;
    }

    /// <summary>Saves the primary entity table to the database.</summary>
    /// <param name="updatedTable">Primary entity data table.</param>
    /// <returns>True or false depending on the success of the save operation</returns>
    protected virtual bool SaveMasterEntity(DataTable updatedTable) => SaveTable(updatedTable, PrimaryKeyField);

    /// <summary>This method generates a new DataSet and adds a new row to the master table.</summary>
    /// <remarks>This only works if the DataSet contains a 1:1 mapping to the back end table.</remarks>
    /// <returns>DataSet containing the new row</returns>
    protected virtual DataSet NewMasterEntity()
    {
        using (var command = DataService.BuildEmptyRecordQueryCommand(MasterEntity, "*", QueryMethod))
        {
            var masterDataSet = ExecuteQuery(command, MasterEntity);
            return NewMasterEntity(masterDataSet);
        }
    }

    /// <summary>This method adds a new data row to the main entity contained in the passed DataSet</summary>
    /// <param name="currentDataSet">DataSet</param>
    /// <returns>DataSet with the new row added (to the master entity table)</returns>
    protected virtual DataSet NewMasterEntity(DataSet currentDataSet)
    {
        if (currentDataSet == null) throw new NullReferenceException("Parameter 'currentDataSet' cannot be null.");

        var rowName = currentDataSet.Tables[MasterEntity].NewRow();
        switch (GetPrimaryKeyType())
        {
            case KeyType.Guid:
                rowName[PrimaryKeyField] = Guid.NewGuid();
                break;
            case KeyType.Integer:
                rowName[PrimaryKeyField] = GetNewIntegerKey(MasterEntity, currentDataSet);
                break;
            case KeyType.IntegerAutoIncrement:
                rowName[PrimaryKeyField] = GetNewIntegerKey(MasterEntity, currentDataSet);
                break;
            case KeyType.String:
                rowName[PrimaryKeyField] = GetNewStringKey(MasterEntity, currentDataSet);
                break;
        }

        CallPopulateNewRecord(rowName, rowName.Table.TableName, currentDataSet);
        currentDataSet.Tables[MasterEntity].Rows.Add(rowName);
        return currentDataSet;
    }

    /// <summary>
    /// This method is called whenever the primary key value changes, as it may be the
    /// case in scenario where auto-increment integer keys are generated by the database
    /// and override previously created temporary keys on the client.
    /// This method is designed to be overridden in subclasses.
    /// </summary>
    /// <param name="updatedDataSet">DataSet that contains the updated table as well as potential other tables that need to be updated.</param>
    /// <param name="tableName">Table in which the key has changed.</param>
    /// <param name="oldKey">Original (temporary) key value</param>
    /// <param name="newKey">New (final) key value</param>
    public virtual void PrimaryKeyValueChanged(DataSet updatedDataSet, string tableName, int oldKey, int newKey) { }

    /// <summary>Adds a blank secondary table to an existing DataSet.</summary>
    /// <remarks>This does NOT add a new record to the table. Use the overload with the two additional string parameters to add a new record.</remarks>
    /// <param name="tableName">Table Name</param>
    /// <param name="existingDataSet">Existing DataSet</param>
    /// <returns>DataSet</returns>
    protected virtual DataSet NewSecondaryEntity(string tableName, DataSet existingDataSet)
    {
        using var command = DataService.BuildEmptyRecordQueryCommand(tableName, "*", QueryMethod);
        ExecuteQuery(command, tableName, existingDataSet);
        return existingDataSet;
    }

    /// <summary>Adds a blank secondary table to an existing DataSet.</summary>
    /// <remarks>This version of this method DOES add a blank record and immediately links the record to the master entity.</remarks>
    /// <param name="tableName">Table Name</param>
    /// <param name="existingDataSet">Existing DataSet</param>
    /// <param name="primaryKeyField">Primary key field of the secondary table (pass blank if no new PK is to be generated)</param>
    /// <param name="foreignKeyField">Foreign key field that links to the master entity (pass blank if no link is to be created)</param>
    /// <returns>DataSet</returns>
    protected virtual DataSet NewSecondaryEntity(string tableName, DataSet existingDataSet, string primaryKeyField, string foreignKeyField)
    {
        // We only add a new table if it isn't there at all
        // This allows for this method to be called multiple times on the same table to create multiple rows.
        if (existingDataSet.Tables[tableName] == null) NewSecondaryEntity(tableName, existingDataSet);
        var rowGen = existingDataSet.Tables[tableName].NewRow();
        // We check whether we need to generate a new PK
        if (primaryKeyField.Length > 0)
        {
            var pkType = GetPrimaryKeyType(tableName);
            switch (pkType)
            {
                case KeyType.Guid:
                    rowGen[primaryKeyField] = Guid.NewGuid();
                    break;
                case KeyType.Integer:
                    rowGen[primaryKeyField] = GetNewIntegerKey(tableName, existingDataSet);
                    break;
                case KeyType.IntegerAutoIncrement:
                    rowGen[primaryKeyField] = GetNewIntegerKey(tableName, existingDataSet);
                    break;
                case KeyType.String:
                    rowGen[primaryKeyField] = GetNewStringKey(tableName, existingDataSet);
                    break;
            }
        }

        // We check whether we have a foreign key we can fill in
        if (foreignKeyField.Length > 0) rowGen[foreignKeyField] = existingDataSet.Tables[MasterEntity].Rows[0][PrimaryKeyField];
        CallPopulateNewRecord(rowGen, rowGen.Table.TableName, existingDataSet);
        existingDataSet.Tables[tableName].Rows.Add(rowGen);
        return existingDataSet;
    }

    /// <summary>This method gets called whenever a new record is added to a table. This method is designed to be overridden in subclasses, allowing the developer to add new values to a new data row.</summary>
    /// <param name="newRow">Newly created row</param>
    /// <param name="tableName">Name of the table this row belongs to</param>
    protected virtual void PopulateNewRecord(DataRow newRow, string tableName) { }

    /// <summary>This method gets called whenever a new record is added to a table. This method is designed to be overridden in subclasses, allowing the developer to add new values to a new data row.</summary>
    /// <param name="newRow">Newly created row</param>
    /// <param name="tableName">Name of the table this row belongs to</param>
    /// <param name="existingDataSet">The DataSet the row will be added to</param>
    protected virtual void PopulateNewRecord(DataRow newRow, string tableName, DataSet existingDataSet) { }

    /// <summary>Calls the populate new record. (For internal use only...)</summary>
    /// <param name="newRow">Newly created row</param>
    /// <param name="tableName">Name of the table this row belongs to</param>
    /// <param name="existingDataSet">The DataSet the row will be added to</param>
    protected internal virtual void CallPopulateNewRecord(DataRow newRow, string tableName, DataSet existingDataSet)
    {
        PopulateNewRecord(newRow, tableName);
        PopulateNewRecord(newRow, tableName, existingDataSet);
    }

    /// <summary>Creates a new IDbCommand object using the current data service.</summary>
    /// <returns>Generic IDbCommand object.</returns>
    protected virtual IDbCommand NewDbCommand() => DataService.NewCommandObject();

    /// <summary>Creates a new IDbCommand object using the current data service.</summary>
    /// <param name="commandText">New command text</param>
    /// <returns>Generic IDbCommand object.</returns>
    protected virtual IDbCommand NewDbCommand(string commandText)
    {
        var command = DataService.NewCommandObject();
        command.CommandText = commandText;
        return command;
    }

    /// <summary>Adds a new parameter to a DbCommand object.</summary>
    /// <param name="command">Command the parameter is to be added to.</param>
    /// <param name="parameterName">Name of the parameter to add.</param>
    /// <param name="parameterValue">Parameter value.</param>
    /// <returns>Reference to the added parameter.</returns>
    protected virtual IDbDataParameter AddDbCommandParameter(IDbCommand command, string parameterName, object parameterValue)
    {
        if (command == null) throw new NullReferenceException("Parameter 'command' cannot be null.");
        if (parameterValue == null) throw new NullReferenceException("Parameter 'parameterValue' cannot be null.");

        var newParameter = DataService.NewCommandObjectParameter(parameterName, parameterValue);
        command.Parameters.Add(newParameter);
        return newParameter;
    }

    /// <summary>Creates a command object to query a single record based on a key/value pair.</summary>
    /// <param name="tableName">Table to query record from.</param>
    /// <param name="fieldList">Fields to be included in the return set.</param>
    /// <param name="keyFieldName">Field name used as the key (often primary or foreign key field)</param>
    /// <param name="keyValue">Key value</param>
    /// <returns>IDbCommand object</returns>
    protected virtual IDbCommand GetSingleRecordCommand(string tableName, string fieldList, string keyFieldName, object keyValue) => DataService.BuildSingleRecordQueryCommand(tableName, fieldList, keyFieldName, keyValue, QueryMethod);

    /// <summary>Creates a command object to query a multiple records based on a key/value pair.</summary>
    /// <param name="tableName">Table to query record from.</param>
    /// <param name="fieldList">Fields to be included in the return set.</param>
    /// <param name="keyFieldName">Field name used as the key (often primary or foreign key field)</param>
    /// <param name="keyValue">Key value</param>
    /// <returns>IDbCommand object</returns>
    protected IDbCommand GetMultipleRecordsByKeyCommand(string tableName, string fieldList, string keyFieldName, object keyValue) => GetSingleRecordCommand(tableName, fieldList, keyFieldName, keyValue);

    /// <summary>Builds a command that returns a set of records based on the provided field names and filter parameters.</summary>
    /// <param name="tableName">Name of the table.</param>
    /// <param name="fieldList">The list of fields returned by the query (ignored for stored procedure execution)</param>
    /// <param name="fieldNames">The list of fields by which to filter</param>
    /// <param name="filterParameters">Parameters used for filtering. The parameters need to match the list of filter fields (name and types)</param>
    /// <param name="selectMethod">Process method for the select method</param>
    /// <returns>IDbCommand object representing the query</returns>
    /// <example>
    /// string[] fieldNames = new string[] { "FirstName", "LastName", "IsActive" };
    /// object[] parameters = new object[] { "Chris", "Pronger", true };
    /// IDbCommand command = GetQueryCommand("Customers", "*", fieldNames, parameters,
    /// DataRowProcessMethod.IndividualCommands);
    /// DataSet data = ExecuteQuery(command, "Customers");
    /// </example>
    /// <remarks>
    /// All provided parameters are added using "and" logical operators.
    /// The fields are used as exact matches. Therefore passing "Pron" as a filter parameter will NOT include
    /// "Pronger". However, it is possible to pass "Pron%", in which case "Pronger" is included, assuming the
    /// database back end understands the % character.
    /// </remarks>
    protected virtual IDbCommand GetQueryCommand(string tableName, string fieldList, string[] fieldNames, object[] filterParameters, DataRowProcessMethod selectMethod) => DataService.BuildQueryCommand(tableName, fieldList, fieldNames, filterParameters, selectMethod);

    /// <summary>Builds a command that returns a set of records based on the provided field names and filter parameters.</summary>
    /// <param name="tableName">Name of the table.</param>
    /// <param name="fieldList">The list of fields returned by the query (ignored for stored procedure execution)</param>
    /// <param name="fieldNames">The list of fields by which to filter</param>
    /// <param name="filterParameters">Parameters used for filtering. The parameters need to match the list of filter fields (name and types).</param>
    /// <returns>IDbCommand object representing the query</returns>
    /// <example>
    /// string[] fieldNames = new string[] { "FirstName", "LastName", "IsActive" };
    /// object[] parameters = new object[] { "Chris", "Pronger", true };
    /// IDbCommand command = GetQueryCommand("Customers", "*", fieldNames, parameters);
    /// DataSet data = ExecuteQuery(command, "Customers");
    /// </example>
    /// <remarks>
    /// This overload uses the default setting of the system to determine whether the query is executed as
    /// individual commands or through stored procedures.
    /// All provided parameters are added using "and" logical operators.
    /// The fields are used as exact matches. Therefore passing "Pron" as a filter parameter will NOT include
    /// "Pronger". However, it is possible to pass "Pron%", in which case "Pronger" is included, assuming the
    /// database back end understands the % character.
    /// </remarks>
    protected virtual IDbCommand GetQueryCommand(string tableName, string fieldList, string[] fieldNames, object[] filterParameters) => GetQueryCommand(tableName, fieldList, fieldNames, filterParameters, DataRowProcessMethod.Default);

    /// <summary>Creates a command object that returns all records from a table.</summary>
    /// <param name="tableName">Table to query from</param>
    /// <returns>IDbCommand object</returns>
    protected virtual IDbCommand GetAllRecordsCommand(string tableName) => DataService.BuildAllRecordsQueryCommand(tableName, "*", "", QueryMethod);

    /// <summary>Creates a command object that returns all records from a table (for a specified list of fields).</summary>
    /// <param name="tableName">Table to query from</param>
    /// <param name="fieldList">Fields to be included in the return set</param>
    /// <returns>IDbCommand object</returns>
    protected virtual IDbCommand GetAllRecordsCommand(string tableName, string fieldList) => DataService.BuildAllRecordsQueryCommand(tableName, fieldList, "", QueryMethod);

    /// <summary>Creates a command object that returns all records from a table (in a certain order and for a specified list of fields).</summary>
    /// <param name="tableName">Table to query from</param>
    /// <param name="fieldList">Fields to be included in the return set</param>
    /// <param name="orderBy">Sort order</param>
    /// <returns>IDbCommand object</returns>
    protected virtual IDbCommand GetAllRecordsCommand(string tableName, string fieldList, string orderBy) => DataService.BuildAllRecordsQueryCommand(tableName, fieldList, orderBy, QueryMethod);

    /// <summary>Creates a command that deletes records by key</summary>
    /// <param name="tableName">Name of the table.</param>
    /// <param name="primaryKeyFieldName">Name of the primary key field.</param>
    /// <param name="primaryKeyValue">The primary key value.</param>
    /// <returns>Command</returns>
    /// <remarks>
    /// Only use this method to delete records by their primary key.
    /// Do NOT use this method and pass a field name as the key field name that is not the primary key,
    /// as that will lead to naming conflicts for stored procedures.
    /// </remarks>
    protected virtual IDbCommand GetDeleteRecordsByKeyCommand(string tableName, string primaryKeyFieldName, object primaryKeyValue) => DataService.BuildDeleteCommand(tableName, primaryKeyFieldName, primaryKeyValue, DeleteMethod);

    /// <summary>Deletes records by key value.</summary>
    /// <param name="tableName">Name of the table.</param>
    /// <param name="primaryKeyFieldName">Name of the primary key field.</param>
    /// <param name="primaryKeyValue">The primary key value.</param>
    /// <returns>Number of deleted records</returns>
    /// <remarks>
    /// Only use this method to delete records by their primary key.
    /// Do NOT use this method and pass a field name as the key field name that is not the primary key,
    /// as that will lead to naming conflicts for stored procedures.
    /// </remarks>
    protected virtual int DeleteRecordsByKey(string tableName, string primaryKeyFieldName, object primaryKeyValue) => ExecuteNonQuery(GetDeleteRecordsByKeyCommand(tableName, primaryKeyFieldName, primaryKeyValue));

    /// <summary>Queries a single record based on a key/value pair.</summary>
    /// <param name="tableName">Table to query record from.</param>
    /// <param name="fieldList">Fields to be included in the return set.</param>
    /// <param name="keyFieldName">Field name used as the key (often primary or foreign key field)</param>
    /// <param name="keyValue">Key value</param>
    /// <returns>DataSet</returns>
    /// <remarks>The queried result is stored in a new DataSet in a table of the same names as the provided table name.</remarks>
    protected virtual DataSet QuerySingleRecord(string tableName, string fieldList, string keyFieldName, object keyValue) => ExecuteQuery(DataService.BuildSingleRecordQueryCommand(tableName, fieldList, keyFieldName, keyValue, QueryMethod), tableName);

    /// <summary>Queries a single record based on a key/value pair.</summary>
    /// <param name="tableName">Table to query record from.</param>
    /// <param name="fieldList">Fields to be included in the return set.</param>
    /// <param name="keyFieldName">Field name used as the key (often primary or foreign key field)</param>
    /// <param name="keyValue">Key value</param>
    /// <param name="existingDataSet">Existing DataSet the returned value is to be queried into.</param>
    /// <returns>DataSet</returns>
    /// <remarks>The queried result is stored in the existing (passed) DataSet in a table of the same names as the provided table name.</remarks>
    protected virtual DataSet QuerySingleRecord(string tableName, string fieldList, string keyFieldName, object keyValue, DataSet existingDataSet) => ExecuteQuery(DataService.BuildSingleRecordQueryCommand(tableName, fieldList, keyFieldName, keyValue, QueryMethod), tableName, existingDataSet);

    /// <summary>Queries multiple records based on a key/value pair.</summary>
    /// <param name="tableName">Table to query record from.</param>
    /// <param name="fieldList">Fields to be included in the return set.</param>
    /// <param name="keyFieldName">Field name used as the key (often primary or foreign key field)</param>
    /// <param name="keyValue">Key value</param>
    /// <returns>DataSet</returns>
    /// <remarks>The query result is stored in a new DataSet in a table of the same name as the table name passed as a parameter.</remarks>
    protected virtual DataSet QueryMultipleRecordsByKey(string tableName, string fieldList, string keyFieldName, object keyValue) => QuerySingleRecord(tableName, fieldList, keyFieldName, keyValue);

    /// <summary>Queries multiple records based on a key/value pair.</summary>
    /// <param name="tableName">Table to query record from.</param>
    /// <param name="fieldList">Fields to be included in the return set.</param>
    /// <param name="keyFieldName">Field name used as the key (often primary or foreign key field)</param>
    /// <param name="keyValue">Key value</param>
    /// <param name="existingDataSet">Existing DataSet the returns set is to be filled into</param>
    /// <returns>DataSet</returns>
    /// <remarks>The query result is stored in the existing (passed) DataSet in a table of the same name as the table name passed as a parameter.</remarks>
    protected virtual DataSet QueryMultipleRecordsByKey(string tableName, string fieldList, string keyFieldName, object keyValue, DataSet existingDataSet) => QuerySingleRecord(tableName, fieldList, keyFieldName, keyValue, existingDataSet);

    /// <summary>Queries the specified table name.</summary>
    /// <param name="tableName">Name of the table.</param>
    /// <param name="fieldList">The list of fields returned by the query (ignored for stored procedure execution)</param>
    /// <param name="fieldNames">The list of fields by which to filter</param>
    /// <param name="filterParameters">Parameters used for filtering. The parameters need to match the list of filter fields (name and types).</param>
    /// <param name="selectMethod">Process method for the select method</param>
    /// <returns>IDbCommand object representing the query</returns>
    /// <example>
    /// string[] fieldNames = new string[] { "FirstName", "LastName", "IsActive" };
    /// object[] parameters = new object[] { "Chris", "Pronger", true };
    /// DataSet data = Query("Customers", "*", fieldNames, parameters, DataRowProcessMethod.IndividualCommands);
    /// </example>
    /// <remarks>
    /// If possible, use the overload without the selectMethod parameter to achieve better database independence.
    /// All provided parameters are added using "and" logical operators.
    /// The fields are used as exact matches. Therefore passing "Pron" as a filter parameter will NOT include
    /// "Pronger". However, it is possible to pass "Pron%", in which case "Pronger" is included, assuming the
    /// database back end understands the % character.
    /// </remarks>
    protected virtual DataSet Query(string tableName, string fieldList, string[] fieldNames, object[] filterParameters, DataRowProcessMethod selectMethod) => ExecuteQuery(GetQueryCommand(tableName, fieldList, fieldNames, filterParameters, selectMethod));

    /// <summary>Queries the specified table name.</summary>
    /// <param name="tableName">Name of the table.</param>
    /// <param name="fieldList">The list of fields returned by the query (ignored for stored procedure execution)</param>
    /// <param name="fieldNames">The list of fields by which to filter</param>
    /// <param name="filterParameters">Parameters used for filtering. The parameters need to match the list of filter fields (name and types).</param>
    /// <returns>IDbCommand object representing the query</returns>
    /// <example>
    /// string[] fieldNames = new string[] { "FirstName", "LastName", "IsActive" };
    /// object[] parameters = new object[] { "Chris", "Pronger", true };
    /// DataSet data = Query("Customers", "*", fieldNames, parameters, DataRowProcessMethod.IndividualCommands);
    /// </example>
    /// <remarks>
    /// This method uses the default query method (individual commands vs. stored procedures)
    /// All provided parameters are added using "and" logical operators.
    /// The fields are used as exact matches. Therefore passing "Pron" as a filter parameter will NOT include
    /// "Pronger". However, it is possible to pass "Pron%", in which case "Pronger" is included, assuming the
    /// database back end understands the % character.
    /// </remarks>
    protected virtual DataSet Query(string tableName, string fieldList, string[] fieldNames, object[] filterParameters) => ExecuteQuery(GetQueryCommand(tableName, fieldList, fieldNames, filterParameters));

    /// <summary>Queries the specified table name.</summary>
    /// <param name="tableName">Name of the table.</param>
    /// <param name="fieldList">The list of fields returned by the query (ignored for stored procedure execution)</param>
    /// <param name="fieldNames">The list of fields by which to filter</param>
    /// <param name="filterParameters">Parameters used for filtering. The parameters need to match the list of filter fields (name and types).</param>
    /// <param name="selectMethod">Process method for the select method</param>
    /// <param name="entityName">Name of the entity (table in the resulting DataSet).</param>
    /// <returns>IDbCommand object representing the query</returns>
    /// <example>
    /// string[] fieldNames = new string[] { "FirstName", "LastName", "IsActive" };
    /// object[] parameters = new object[] { "Chris", "Pronger", true };
    /// DataSet data = Query("Customers", "*", fieldNames, parameters, DataRowProcessMethod.IndividualCommands);
    /// </example>
    /// <remarks>
    /// If possible, use the overload without the selectMethod parameter to achieve better database independence.
    /// All provided parameters are added using "and" logical operators.
    /// The fields are used as exact matches. Therefore passing "Pron" as a filter parameter will NOT include
    /// "Pronger". However, it is possible to pass "Pron%", in which case "Pronger" is included, assuming the
    /// database back end understands the % character.
    /// </remarks>
    protected virtual DataSet Query(string tableName, string fieldList, string[] fieldNames, object[] filterParameters, DataRowProcessMethod selectMethod, string entityName) => ExecuteQuery(GetQueryCommand(tableName, fieldList, fieldNames, filterParameters, selectMethod), entityName);

    /// <summary>Queries the specified table name.</summary>
    /// <param name="tableName">Name of the table.</param>
    /// <param name="fieldList">The list of fields returned by the query (ignored for stored procedure execution)</param>
    /// <param name="fieldNames">The list of fields by which to filter</param>
    /// <param name="filterParameters">Parameters used for filtering. The parameters need to match the list of filter fields (name and types).</param>
    /// <param name="entityName">Name of the entity (table in the resulting DataSet).</param>
    /// <returns>IDbCommand object representing the query</returns>
    /// <example>
    /// string[] fieldNames = new string[] { "FirstName", "LastName", "IsActive" };
    /// object[] parameters = new object[] { "Chris", "Pronger", true };
    /// DataSet data = Query("Customers", "*", fieldNames, parameters, DataRowProcessMethod.IndividualCommands);
    /// </example>
    /// <remarks>
    /// This method uses the default query method (individual commands vs. stored procedures)
    /// All provided parameters are added using "and" logical operators.
    /// The fields are used as exact matches. Therefore passing "Pron" as a filter parameter will NOT include
    /// "Pronger". However, it is possible to pass "Pron%", in which case "Pronger" is included, assuming the
    /// database back end understands the % character.
    /// </remarks>
    protected virtual DataSet Query(string tableName, string fieldList, string[] fieldNames, object[] filterParameters, string entityName) => ExecuteQuery(GetQueryCommand(tableName, fieldList, fieldNames, filterParameters), entityName);

    /// <summary>Queries the specified table name.</summary>
    /// <param name="tableName">Name of the table.</param>
    /// <param name="fieldList">The list of fields returned by the query (ignored for stored procedure execution)</param>
    /// <param name="fieldNames">The list of fields by which to filter</param>
    /// <param name="filterParameters">Parameters used for filtering. The parameters need to match the list of filter fields (name and types).</param>
    /// <param name="selectMethod">Process method for the select method</param>
    /// <param name="entityName">Name of the entity (table in the resulting DataSet).</param>
    /// <param name="existingDataSet">DataSet the result is to be loaded into.</param>
    /// <returns>IDbCommand object representing the query</returns>
    /// <example>
    /// string[] fieldNames = new string[] { "FirstName", "LastName", "IsActive" };
    /// object[] parameters = new object[] { "Chris", "Pronger", true };
    /// DataSet data = Query("Customers", "*", fieldNames, parameters, DataRowProcessMethod.IndividualCommands);
    /// </example>
    /// <remarks>
    /// If possible, use the overload without the selectMethod parameter to achieve better database independence.
    /// All provided parameters are added using "and" logical operators.
    /// The fields are used as exact matches. Therefore passing "Pron" as a filter parameter will NOT include
    /// "Pronger". However, it is possible to pass "Pron%", in which case "Pronger" is included, assuming the
    /// database back end understands the % character.
    /// </remarks>
    protected virtual DataSet Query(string tableName, string fieldList, string[] fieldNames, object[] filterParameters, DataRowProcessMethod selectMethod, string entityName, DataSet existingDataSet) => ExecuteQuery(GetQueryCommand(tableName, fieldList, fieldNames, filterParameters, selectMethod), entityName, existingDataSet);

    /// <summary>Queries the specified table name.</summary>
    /// <param name="tableName">Name of the table.</param>
    /// <param name="fieldList">The list of fields returned by the query (ignored for stored procedure execution)</param>
    /// <param name="fieldNames">The list of fields by which to filter</param>
    /// <param name="filterParameters">Parameters used for filtering. The parameters need to match the list of filter fields (name and types).</param>
    /// <param name="entityName">Name of the entity (table in the resulting DataSet).</param>
    /// <param name="existingDataSet">DataSet the result is to be loaded into.</param>
    /// <returns>IDbCommand object representing the query</returns>
    /// <example>
    /// string[] fieldNames = new string[] { "FirstName", "LastName", "IsActive" };
    /// object[] parameters = new object[] { "Chris", "Pronger", true };
    /// DataSet data = Query("Customers", "*", fieldNames, parameters, DataRowProcessMethod.IndividualCommands);
    /// </example>
    /// <remarks>
    /// This method uses the default query method (individual commands vs. stored procedures)
    /// All provided parameters are added using "and" logical operators.
    /// The fields are used as exact matches. Therefore passing "Pron" as a filter parameter will NOT include
    /// "Pronger". However, it is possible to pass "Pron%", in which case "Pronger" is included, assuming the
    /// database back end understands the % character.
    /// </remarks>
    protected virtual DataSet Query(string tableName, string fieldList, string[] fieldNames, object[] filterParameters, string entityName, DataSet existingDataSet) => ExecuteQuery(GetQueryCommand(tableName, fieldList, fieldNames, filterParameters), entityName, existingDataSet);

    /// <summary>Queries all records from a certain table.</summary>
    /// <param name="tableName">Table to query from</param>
    /// <returns>DataSet</returns>
    /// <remarks>The result is stored in a new DataSet in a table of the same name as the passed table name.</remarks>
    protected virtual DataSet QueryAllRecords(string tableName) => ExecuteQuery(DataService.BuildAllRecordsQueryCommand(tableName, "*", string.Empty, QueryMethod), tableName);

    /// <summary>Queries all records from a certain table (for a specified list of fields).</summary>
    /// <param name="tableName">Table to query from</param>
    /// <param name="fieldList">Fields to be included in the return set</param>
    /// <returns>DataSet</returns>
    /// <remarks>The result is stored in a new DataSet in a table of the same name as the passed table name.</remarks>
    protected virtual DataSet QueryAllRecords(string tableName, string fieldList) => ExecuteQuery(DataService.BuildAllRecordsQueryCommand(tableName, fieldList, string.Empty, QueryMethod), tableName);

    /// <summary>Queries all records from a certain table (in a certain order and for a specified list of fields).</summary>
    /// <param name="tableName">Table to query from</param>
    /// <param name="fieldList">Fields to be included in the return set</param>
    /// <param name="orderBy">Sort order</param>
    /// <returns>DataSet</returns>
    /// <remarks>The result is stored in a new DataSet in a table of the same name as the passed table name.</remarks>
    protected virtual DataSet QueryAllRecords(string tableName, string fieldList, string orderBy) => ExecuteQuery(DataService.BuildAllRecordsQueryCommand(tableName, fieldList, orderBy, QueryMethod), tableName);

    /// <summary>Queries all records from a certain table.</summary>
    /// <param name="tableName">Table to query from</param>
    /// <param name="existingDataSet">Existing DataSet</param>
    /// <returns>DataSet</returns>
    /// <remarks>The result is stored in the existing (passed) DataSet in a table of the same name as the passed table name.</remarks>
    protected virtual DataSet QueryAllRecords(string tableName, DataSet existingDataSet) => ExecuteQuery(DataService.BuildAllRecordsQueryCommand(tableName, "*", string.Empty, QueryMethod), tableName, existingDataSet);

    /// <summary>Queries all records from a certain table (for a specified list of fields).</summary>
    /// <param name="tableName">Table to query from</param>
    /// <param name="fieldList">Fields to be included in the return set</param>
    /// <param name="existingDataSet">Existing DataSet</param>
    /// <returns>DataSet</returns>
    /// <remarks>The result is stored in the existing (passed) DataSet in a table of the same name as the passed table name.</remarks>
    protected virtual DataSet QueryAllRecords(string tableName, string fieldList, DataSet existingDataSet) => ExecuteQuery(DataService.BuildAllRecordsQueryCommand(tableName, fieldList, string.Empty, QueryMethod), tableName, existingDataSet);

    /// <summary>Queries all records from a certain table (in a certain order and for a specified list of fields).</summary>
    /// <param name="tableName">Table to query from</param>
    /// <param name="fieldList">Fields to be included in the return set</param>
    /// <param name="orderBy">Sort order</param>
    /// <param name="existingDataSet">Existing DataSet</param>
    /// <returns>DataSet</returns>
    /// <remarks>The result is stored in the existing (passed) DataSet in a table of the same name as the passed table name.</remarks>
    protected virtual DataSet QueryAllRecords(string tableName, string fieldList, string orderBy, DataSet existingDataSet) => ExecuteQuery(DataService.BuildAllRecordsQueryCommand(tableName, fieldList, orderBy, QueryMethod), tableName, existingDataSet);

    /// <summary>Initiates a new transaction.</summary>
    /// <returns>True or false depending on the success of the operation</returns>
    protected virtual bool BeginTransaction()
    {
        startedTransaction = DataService.BeginTransaction();
        return startedTransaction;
    }

    /// <summary>Aborts the current transaction (rollback).</summary>
    /// <returns>True or false depending on the success of the operation</returns>
    protected virtual bool AbortTransaction()
    {
        if (internalDataContext != null)
            if (startedTransaction)
                return DataService.AbortTransaction();
            else
                throw new AbortedSharedTransactionException();
        return DataService.AbortTransaction();
    }

    /// <summary>Commits the current transaction.</summary>
    /// <returns>True or false depending on the success of the operation</returns>
    protected virtual bool CommitTransaction()
    {
        if (internalDataContext != null)
        {
            // We have a shared data context and thus need to verify whether
            // this was our own transaction or not
            if (startedTransaction) return DataService.CommitTransaction();
            // If it was not our transaction, we can not commit it, 
            // instead, we simply give no indication that there is any further concern.
            // Which should cause an external system to commit the transaction.
            return true;
        }

        // We have a private context, so we have full control over the connection
        return DataService.CommitTransaction();
    }

    /// <summary>
    /// This method allows to set the data access method (such as query vs. stored procedures)
    /// on a business object wide basis. This method is a shortcut that allows developers
    /// to set the data access method for queries, updates, and deletes with one call,
    /// rather than setting these values individually.
    /// </summary>
    /// <param name="accessMethod">Data access method, such as 'StoredProcedure'.</param>
    protected virtual void SetDataAccessMethod(DataRowProcessMethod accessMethod)
    {
        UpdateMethod = accessMethod;
        QueryMethod = accessMethod;
        DeleteMethod = accessMethod;
    }

    /// <summary>Dispose designed to be overridden in subclasses.</summary>
    /// <param name="disposing">True is called from Dispose()</param>
    protected virtual void Dispose(bool disposing) => dataService?.Dispose(); // We give the data service the opportunity to finalize

    /// <summary>Destructor</summary>
    ~BusinessObject() => Dispose(false);

    /// <summary>Deletes rows after passing verification. If verification fails on any of the rows, the whole delete is canceled, and the DataSet gets a BrokenRules table listing the violations.</summary>
    /// <param name="masterDataSet">The master DataSet.</param>
    /// <returns></returns>
    public bool DeleteWithVerification(DataSet masterDataSet)
    {
        // Allow the delete if there are no records to verify.
        var deleteSucceededForAll = masterDataSet.Tables[0].Rows.Count == 0;

        var deletionResults = VerifyForDeletion(masterDataSet);
        var canDelete = !deletionResults.HasViolations;

        if (canDelete || AllowDeleteWithViolations)
        {
            // TODO: Why did we disable the transaction here?
            //BeginTransaction();

            foreach (DataRow row in masterDataSet.Tables[0].Rows)
            {
                // Call into Milos for the delete.
                deleteSucceededForAll = Delete((Guid) row[PrimaryKeyField]);

                if (!deleteSucceededForAll) break;
            }

            //if (deleteSucceededForAll)
            //{
            //    CommitTransaction();
            //}
        }

        return deleteSucceededForAll;
    }

    /// <summary>Verifies for deletion.</summary>
    /// <param name="masterDataSet">The master DataSet containing the data to be verified.</param>
    /// <returns>Deletion results</returns>
    public DeletionResults VerifyForDeletion(DataSet masterDataSet)
    {
        if (masterDataSet == null) throw new NullReferenceException("Parameter 'masterDataSet' cannot be null.");

        ClearBrokenRules(masterDataSet);
        ApplyDeletionRules(masterDataSet);
        var hasViolations = DataSetHasViolations(masterDataSet);

        return new DeletionResults(hasViolations, null);
    }

    /// <summary>Verifies for deletion.</summary>
    /// <param name="masterDataSet">The master DataSet containing the data to be verified.</param>
    /// <param name="level">The level.</param>
    /// <param name="manager">Deletion dependency manager</param>
    /// <returns></returns>
    public virtual DeletionResults VerifyForDeletion(DataSet masterDataSet, DeletionVerificationLevel level, DeletionDependencyManager manager)
    {
        DeletionVerificationLevel = level;
        var deletionResults = VerifyForDeletion(masterDataSet);
        ProcessDependencies(manager.Dependencies, masterDataSet);
        return deletionResults;
    }

    /// <summary>Verifies for deletion.</summary>
    /// <param name="masterDataSet">The master DataSet containing the data to be verified.</param>
    /// <param name="level">The level.</param>
    /// <returns></returns>
    public virtual DeletionResults VerifyForDeletion(DataSet masterDataSet, DeletionVerificationLevel level)
    {
        DeletionVerificationLevel = level;
        var deletionResults = VerifyForDeletion(masterDataSet);
        return deletionResults;
    }

    private void ProcessDependencies(IEnumerable<IDeletionDependency> dependencies, DataSet currentDataSet)
    {
        foreach (var dependency in dependencies)
        {
            dependency.CurrentDependentRowsTable = currentDataSet.Tables["__BrokenRules" + dependency.TableName];
            ProcessDependencies(dependency.Dependencies, currentDataSet);
        }
    }

    /// <summary>Applies the deletion rules.</summary>
    /// <param name="currentDataSet">The current DataSet.</param>
    private void ApplyDeletionRules(DataSet currentDataSet)
    {
        foreach (var preRule in DeletionBusinessRules)
            if (preRule is DeletionBusinessRule)
            {
                var found = false;
                foreach (DataTable preTable in currentDataSet.Tables)
                    if (StringHelper.Compare(preRule.TableName, preTable.TableName))
                    {
                        found = true;
                        break;
                    }

                if (!found)
                    LogBusinessRuleViolation(currentDataSet, preRule.TableName, string.Empty, -1, RuleViolationType.Warning, "Table for specified rule not available (" + preRule.TableName + ")", string.Empty);
            }

        var iMaxTables = currentDataSet.Tables.Count;
        for (var tableCounter = 0; tableCounter < iMaxTables; tableCounter++)
            foreach (var businessRule in DeletionBusinessRules)
                if (businessRule is DeletionBusinessRule && businessRule.TableName.ToLower(CultureInfo.InvariantCulture) == currentDataSet.Tables[tableCounter].TableName.ToLower(CultureInfo.InvariantCulture))
                    for (var iRowCounter = 0; iRowCounter < currentDataSet.Tables[tableCounter].Rows.Count; iRowCounter++)
                        if (currentDataSet.Tables[tableCounter].Rows[iRowCounter].RowState != DataRowState.Deleted)
                            businessRule.VerifyRow(currentDataSet.Tables[tableCounter].Rows[iRowCounter], iRowCounter);
                        else if (businessRule.CheckDeletedRows)
                            businessRule.VerifyRow(currentDataSet.Tables[tableCounter].Rows[iRowCounter], iRowCounter);

        if (currentDataSet.Tables.Contains(BrokenRulesTableName)) currentDataSet.Tables[BrokenRulesTableName].AcceptChanges();
    }
}

public enum DeletionVerificationLevel
{
    Simple,
    Partial,
    Full
}

public class DeletionResults
{
    /// <summary>Initializes a new instance of the DeletionResults class.</summary>
    /// <param name="hasViolations"></param>
    /// <param name="manager"></param>
    public DeletionResults(bool hasViolations, DeletionDependencyManager manager)
    {
        HasViolations = hasViolations;
        Manager = manager;
    }

    public bool HasViolations { get; }

    public DeletionDependencyManager Manager { get; }
}