namespace Milos.BusinessObjects;

/// <summary>
///     This method encapsulates a collection of business rules
/// </summary>
/// <remarks>Constructor</remarks>
/// <param name="parentBusinessObject">Business object this collection lives in.</param>
public class BusinessRuleCollection(IBusinessObject parentBusinessObject) : List<IBusinessRule>
{

    /// <summary>Internal reference to the business object this collection lives in.</summary>
    protected IBusinessObject BusinessObject { get; } = parentBusinessObject;

    /// <summary>
    ///     Adds a new rule to the rules collection
    /// </summary>
    /// <param name="newRule">Rule that's to be added</param>
    public new void Add(IBusinessRule newRule)
    {
        if (newRule == null) throw new NullReferenceException("Parameter 'newRule' cannot be null.");

        newRule.SetBusinessObject(BusinessObject);
        base.Add(newRule);
    }

    /// <summary>Flags a field as a required field.</summary>
    /// <param name="fieldName">Name of the field that can not be blank.</param>
    /// <param name="tableName">Table name</param>
    public void AddRequiredField(string fieldName, string tableName)
    {
        if (tableName == null) throw new NullReferenceException("Parameter 'tableName' cannot be null.");
        if (fieldName == null) throw new NullReferenceException("Parameter 'fieldName' cannot be null.");


        // We check whether we have an empty field rule for this object instantiated. 
        // If so, we can simply add the new field to its field list.
        for (var counter = 0; counter < Count; counter++)
            if (this[counter] is EmptyFieldBusinessRule)
                if (this[counter].TableName.ToLower(CultureInfo.InvariantCulture) == tableName.ToLower(CultureInfo.InvariantCulture))
                {
                    // We can use this one.
                    var emptyRule = (EmptyFieldBusinessRule) this[counter];
                    emptyRule.AddFieldToList(fieldName);
                    return;
                }

        // Apparently, no useful rule object exists so far, so we have to create a new one.
        Add(new EmptyFieldBusinessRule(fieldName, tableName, "Field cannot be empty."));
    }

    /// <summary>Flags a field as a required field.</summary>
    /// <param name="fieldName">Name of the field that can not be blank.</param>
    /// <param name="tableName">Table name</param>
    /// <param name="message">Error message</param>
    public void AddRequiredField(string fieldName, string tableName, string message)
    {
        if (fieldName == null) throw new NullReferenceException("Parameter 'fieldName' cannot be null.");
        if (tableName == null) throw new NullReferenceException("Parameter 'tableName' cannot be null.");
        if (message == null) throw new NullReferenceException("Parameter 'message' cannot be null.");


        // We check whether we have an empty field rule for this object instantiated. 
        // If so, we can simply add the new field to its field list.
        for (var counter = 0; counter < Count; counter++)
            if (this[counter] is EmptyFieldBusinessRule)
                if (this[counter].TableName.ToLower(CultureInfo.InvariantCulture) == tableName.ToLower(CultureInfo.InvariantCulture))
                {
                    // We can use this one.
                    var emptyRule = (EmptyFieldBusinessRule) this[counter];
                    emptyRule.AddFieldToList(fieldName, message);
                    return;
                }

        // Apparently, no useful rule object exists so far, so we have to create a new one.
        Add(new EmptyFieldBusinessRule(fieldName, tableName, message));
    }

    /// <summary>Applies all rules within the collection to the provided DataSet</summary>
    /// <param name="currentDataSet">DataSet containing potential rule violating data</param>
    public void ApplyRules(DataSet currentDataSet)
    {
        if (currentDataSet == null) throw new NullReferenceException("Parameter 'currentDataSet' cannot be null.");

        // Before we do anything else, we check whether there are business
        // rules we do not have a table for
        foreach (var preRule in this)
        {
            var found = false;
            // We iterate over the collection of tables to make sure we
            // find the table, even if the table name is cased differently
            foreach (DataTable preTable in currentDataSet.Tables)
                if (StringHelper.Compare(preRule.TableName, preTable.TableName))
                {
                    found = true;
                    break;
                }

            if (!found)
                // Looks like we do not have that table in the DataSet
                BusinessObject.LogBusinessRuleViolation(currentDataSet, preRule.TableName, string.Empty, -1, RuleViolationType.Warning, "The table (" + preRule.TableName + ") for the specified rule is NotFiniteNumberException available.", string.Empty);
        }

        // We iterate over all the tables
        var maxTables = currentDataSet.Tables.Count;
        for (var tableCounter = 0; tableCounter < maxTables; tableCounter++)
            // Then, we iterate over all our rules and see if they need to be applied to that particular table
        for (var iCounter = 0; iCounter < Count; iCounter++)
            if (this[iCounter].TableName.ToLower(CultureInfo.InvariantCulture) == currentDataSet.Tables[tableCounter].TableName.ToLower(CultureInfo.InvariantCulture))
                for (var rowCounter = 0; rowCounter < currentDataSet.Tables[tableCounter].Rows.Count; rowCounter++)
                    if (currentDataSet.Tables[tableCounter].Rows[rowCounter].RowState != DataRowState.Deleted)
                        this[iCounter].VerifyRow(currentDataSet.Tables[tableCounter].Rows[rowCounter], rowCounter);

        // We make sure this table does not flag the
        // entire DataSet as dirty!
        if (BusinessObject is BusinessObject currentBusinessObject)
            // This only works if the current business object is not just an IBusinessObject,
            // but it in fact has to be of the EPS BusinessObject default implementation, since
            // business rule checking is specific to that implementation. Note that this should
            // always be the case. Otherwise, this method would have not been called at all.
            if (currentDataSet.Tables.Contains(currentBusinessObject.BrokenRulesTableName))
                currentDataSet.Tables[currentBusinessObject.BrokenRulesTableName].AcceptChanges();
    }

    /// <summary>Applies all rules within the collection to the provided DataSet.</summary>
    /// <param name="currentDataSet">DataSet containing potential rule violating data</param>
    /// <param name="ruleType">Type of business rule the verification is to be limited to</param>
    public void ApplyRules(DataSet currentDataSet, Type ruleType)
    {
        if (currentDataSet == null) throw new NullReferenceException("Parameter 'currentDataSet' cannot be null.");

        // Before we do anything else, we check whether there are business rules we do not have a table for
        foreach (var preRule in this)
            if (preRule.GetType() == ruleType)
            {
                var found = false;
                // We iterate over the collection of tables to make sure we
                // find the table, even if the table name is cased differently
                foreach (DataTable preTable in currentDataSet.Tables)
                    if (StringHelper.Compare(preRule.TableName, preTable.TableName))
                    {
                        found = true;
                        break;
                    }

                if (!found)
                    // Looks like we do not have that table in the DataSet
                    BusinessObject.LogBusinessRuleViolation(currentDataSet, preRule.TableName, string.Empty, -1, RuleViolationType.Warning, "Table for specified rule not available (" + preRule.TableName + ")", string.Empty);
            }

        // We iterate over all the tables
        var maxTables = currentDataSet.Tables.Count;
        for (var tableCounter = 0; tableCounter < maxTables; tableCounter++)
            // Then, we iterate over all our rules and see if they need to be applied to that particular table
        for (var iCounter = 0; iCounter < Count; iCounter++)
            if (this[iCounter].TableName.ToLower(CultureInfo.InvariantCulture) == currentDataSet.Tables[tableCounter].TableName.ToLower(CultureInfo.InvariantCulture))
                for (var iRowCounter = 0; iRowCounter < currentDataSet.Tables[tableCounter].Rows.Count; iRowCounter++)
                    if (currentDataSet.Tables[tableCounter].Rows[iRowCounter].RowState != DataRowState.Deleted)
                        if (this[iCounter].GetType() == ruleType)
                            this[iCounter].VerifyRow(currentDataSet.Tables[tableCounter].Rows[iRowCounter], iRowCounter);

        // We make sure this table does not flag the
        // entire DataSet as dirty!
        if (BusinessObject is BusinessObject bizCurrent)
            // This only works if the current business object is not just an IBusinessObject,
            // but it in fact has to be of the EPS BusinessObject default implementation, since
            // business rule checking is specific to that implementation. Note that this should
            // always be the case. Otherwise, this method would have not been called at all.
            if (currentDataSet.Tables.Contains(bizCurrent.BrokenRulesTableName))
                currentDataSet.Tables[bizCurrent.BrokenRulesTableName].AcceptChanges();
    }
}

/// <summary>This interface defines the methods and properties that need to be implemented by every business rule.</summary>
public interface IBusinessRule
{
    /// <summary>Table name property. At least a 'get' needs to be supported.</summary>
    string TableName { get; }

    /// <summary>Defines whether this rule needs to be applied to deleted rows.</summary>
    bool CheckDeletedRows { get; }

    /// <summary>Verifies a single row of data.</summary>
    /// <param name="currentRow">Data row that is to be verified</param>
    /// <param name="rowIndex">Index of the row that is to be identified (within its data table)</param>
    void VerifyRow(DataRow currentRow, int rowIndex);

    /// <summary>Sets the business object this object belongs to.</summary>
    /// <param name="currentBusinessObject">Business Object</param>
    void SetBusinessObject(IBusinessObject currentBusinessObject);
}