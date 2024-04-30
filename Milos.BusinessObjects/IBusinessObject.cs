using Milos.Data;

namespace Milos.BusinessObjects;

/// <summary>
///     This interface defines the very basic interface supported by all business objects
/// </summary>
public interface IBusinessObject : IDisposable
{
    /// <summary>Name of the master entity this business object is responsible for.</summary>
    string MasterEntity { get; set; }

    /// <summary>Name of the primary key field used by the master entity in this business object.</summary>
    string PrimaryKeyField { get; set; }

    /// <summary>Primary key type this object operates on.</summary>
    KeyType PrimaryKeyType { get; set; }

    /// <summary>Defines whether only changed fields (default) or the complete record will be written to the server when updates are required.</summary>
    DataRowUpdateMode UpdateMode { get; set; }

    /// <summary>Defines how the data base back end is to be updated (sql commands, stored procedures,...).</summary>
    DataRowProcessMethod UpdateMethod { get; set; }

    /// <summary>Defines the method used to delete records on the database back end.</summary>
    DataRowProcessMethod DeleteMethod { get; set; }

    /// <summary>Defines the method used to query records on the database back end (for automated queries).</summary>
    DataRowProcessMethod QueryMethod { get; set; }

    /// <summary>This method is used to verify whether the current DataSet is valid.</summary>
    /// <param name="masterDataSet">DataSet that contains the information that needs to be verified.</param>
    /// <returns>True or False depending on whether or not the verified DataSet is valid.</returns>
    bool Verify(DataSet masterDataSet);

    /// <summary>This method deletes a single row in the master entity based on its primary key.</summary>
    /// <param name="entityKey">Entity PK</param>
    /// <returns>True or False</returns>
    bool Delete(string entityKey);

    /// <summary>This method deletes a single row in the master entity based on its primary key.</summary>
    /// <param name="entityKey">Entity PK</param>
    /// <returns>True or False</returns>
    bool Delete(Guid entityKey);

    /// <summary>This method deletes a single row in the master entity based on its primary key.</summary>
    /// <param name="entityKey">Entity PK</param>
    /// <returns>True of False</returns>
    bool Delete(int entityKey);

    /// <summary>Saves a DataSet to the back end.</summary>
    /// <param name="masterDataSet">DataSet that is to be saved</param>
    /// <returns>True or False</returns>
    bool Save(DataSet masterDataSet);

    /// <summary>Saves a business entity to the back end.</summary>
    /// <param name="entity">Business entity that is to be saved</param>
    /// <returns>True or False</returns>
    bool Save(IBusinessEntity entity);

    /// <summary>Saves a batch of business entities to the back end.</summary>
    /// <param name="entities">Business entity array that is to be saved</param>
    /// <returns>True or False</returns>
    bool Save(IEnumerable<IBusinessEntity> entities);

    /// <summary>Saves a batch of DataSets to the back end.</summary>
    /// <param name="masterDataSets">DataSet array that is to be saved</param>
    /// <returns>True or False</returns>
    bool Save(IEnumerable<DataSet> masterDataSets);

    /// <summary>Retrieves a list of main entity data.</summary>
    /// <returns>DataSet</returns>
    DataSet GetList();

    /// <summary>Loads a single record of the main entity into a DataSet and returns it.</summary>
    /// <param name="entityKey">Entity PK</param>
    /// <returns></returns>
    DataSet LoadEntity(string entityKey);

    /// <summary>
    ///     Loads a single record of the main entity into a DataSet and returns it.
    /// </summary>
    /// <param name="entityKey">Entity PK</param>
    /// <returns>DataSet</returns>
    DataSet LoadEntity(Guid entityKey);

    /// <summary>Loads a single record of the main entity into a DataSet and returns it.</summary>
    /// <param name="entityKey">Entity PK</param>
    /// <returns>DataSet</returns>
    DataSet LoadEntity(int entityKey);

    /// <summary>Creates a new instance of the current master entity.</summary>
    /// <returns>DataSet</returns>
    DataSet AddNew();

    /// <summary>This method creates a new key for integer-key rows.</summary>
    /// <param name="entityName">Name of the entity the key is generated for</param>
    /// <param name="dataSetWithNewRecord">DataSet containing the new record the PK is for</param>
    /// <returns>Integer key</returns>
    int GetNewIntegerKey(string entityName, DataSet dataSetWithNewRecord);

    /// <summary>
    /// This method creates a new key for string-key rows.
    /// By default, this method creates a GUID and turns it into a string.
    /// If different behavior is desired, this method can be overridden in subclasses.
    /// </summary>
    /// <param name="entityName">Name of the entity the key is generated for</param>
    /// <param name="dataSetWithNewRecords">DataSet containing the new record the PK is for</param>
    /// <returns>String key</returns>
    string GetNewStringKey(string entityName, DataSet dataSetWithNewRecords);

    /// <summary>This method can be used to log business rule violations.</summary>
    /// <param name="currentDataSet">DataSet that contains the data that violated a rule.</param>
    /// <param name="tableName">Table that contains the violation.</param>
    /// <param name="fieldName">Field name that contains the violation.</param>
    /// <param name="rowIndex">Row (record) that contains the violation</param>
    /// <param name="violationType">Type (severity) of the violation.</param>
    /// <param name="message">Plain text message</param>
    /// <param name="ruleClass">Identifier for the rule. This is usually the name of the class of the rule object that detected the rule violation.</param>
    void LogBusinessRuleViolation(DataSet currentDataSet, string tableName, string fieldName, int rowIndex, RuleViolationType violationType, string message, string ruleClass);
}