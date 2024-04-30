using Milos.Data;

namespace Milos.BusinessObjects;

/// <summary>
/// Class designed to be sub-classed for sub-item collection items
/// </summary>
public class EntitySubItemCollectionItem : IEntitySubItemCollectionItem
{
    /// <summary>
    /// Internal reference to the primary key field used by the records represented by this collection item
    /// </summary>
    /// <summary>
    /// Internal reference to the broken rules collection
    /// </summary>
    private BrokenRulesCollection brokenRulesCollection;

    /// <summary>
    /// Initializes a new instance of the <see cref="EntitySubItemCollectionItem"/> class.
    /// </summary>
    public EntitySubItemCollectionItem() { }

    /// <summary>
    /// Constructor
    /// </summary>
    public EntitySubItemCollectionItem(IEntitySubItemCollection parentCollection)
    {
        SetParentCollection(parentCollection);
        if (parentCollection is EntitySubItemCollection collection)
            PrimaryKeyField = collection.PrimaryKeyField;
    }

    /// <summary>
    /// Reference to the parent collection object
    /// </summary>
    [NotReportSerializable]
    [NotClonable]
    public IEntitySubItemCollection ParentCollection { get; private set; }

    /// <summary>
    /// Broken business rules collection (specific to this item)
    /// </summary>
    [NotReportSerializable]
    [NotClonable]
    public BrokenRulesCollection BrokenRules => brokenRulesCollection ?? (brokenRulesCollection = new BrokenRulesCollection(this));

    /// <summary>
    /// Reference to the parent entity object
    /// </summary>
    [NotReportSerializable]
    [NotClonable]
    public IBusinessEntity ParentEntity => ParentCollection.ParentEntity;

    /// <summary>
    /// Data row the item represents
    /// </summary>
    protected virtual DataRow CurrentRow { get; private set; }

    /// <summary>
    /// For internal use only
    /// </summary>
    internal string TableName => CurrentRow.Table.TableName;

    /// <summary>
    /// For internal use only
    /// </summary>
    internal int IndexInCollection
    {
        get
        {
            var currentIndex = -1;
            var found = false;
            switch (ParentEntity.PrimaryKeyType)
            {
                case KeyType.Guid:
                    foreach (DataRow oRow in CurrentRow.Table.Rows)
                    {
                        currentIndex++;
                        if ((Guid) oRow[PrimaryKeyField] == PK)
                        {
                            found = true;
                            break;
                        }
                    }

                    break;
                case KeyType.IntegerAutoIncrement:
                case KeyType.Integer:
                    foreach (DataRow oRow in CurrentRow.Table.Rows)
                    {
                        currentIndex++;
                        if ((int) oRow[PrimaryKeyField] == PKInteger)
                        {
                            found = true;
                            break;
                        }
                    }

                    break;
                case KeyType.String:
                    foreach (DataRow oRow in CurrentRow.Table.Rows)
                    {
                        currentIndex++;
                        if ((string) oRow[PrimaryKeyField] == PKString)
                        {
                            found = true;
                            break;
                        }
                    }

                    break;
            }

            if (!found)
                return -1;
            return currentIndex;
        }
    }

    /// <summary>
    /// Primary key of the current entity (Guid)
    /// </summary>
    [NotReportSerializable]
    [NotClonable]
    public virtual Guid PK
    {
        get
        {
            if (ParentCollection is EntitySubItemCollection collection)
            {
                if (collection.PrimaryKeyType != KeyType.Guid) return Guid.Empty;
            }
            else
            {
                if (ParentCollection.ParentEntity.PrimaryKeyType != KeyType.Guid) return Guid.Empty;
            }

            return (Guid) GetFieldValue(PrimaryKeyField);
        }
    }

    /// <summary>
    /// State (new, updated, deleted,...) of the current item.
    /// </summary>
    [NotReportSerializable]
    [NotClonable]
    public DataRowState ItemState => CurrentRow.RowState;

    /// <summary>
    /// Primary key of the current entity (int)
    /// </summary>
    [NotReportSerializable]
    [NotClonable]
    public virtual int PKInteger
    {
        get
        {
            if (ParentCollection is EntitySubItemCollection collection)
            {
                if (collection.PrimaryKeyType != KeyType.Integer && collection.PrimaryKeyType != KeyType.IntegerAutoIncrement)
                    return -1;
            }
            else
            {
                if (ParentCollection.ParentEntity.PrimaryKeyType != KeyType.Integer && ParentCollection.ParentEntity.PrimaryKeyType != KeyType.IntegerAutoIncrement)
                    return -1;
            }

            return (int) GetFieldValue(PrimaryKeyField);
        }
    }

    /// <summary>
    /// Primary key of the current entity (string)
    /// </summary>
    [NotReportSerializable]
    [NotClonable]
    public virtual string PKString
    {
        get
        {
            if (ParentCollection is EntitySubItemCollection collection)
            {
                if (collection.PrimaryKeyType != KeyType.String)
                    return string.Empty;
            }
            else
            {
                if (ParentCollection.ParentEntity.PrimaryKeyType != KeyType.String)
                    return string.Empty;
            }

            return GetFieldValue(PrimaryKeyField).ToString();
        }
    }

    /// <summary>
    /// Primary key of the current entity (string)
    /// </summary>
    [NotClonable]
    public virtual string Id
    {
        get
        {
            if (ParentCollection is EntitySubItemCollection collection)
                switch (collection.PrimaryKeyType)
                {
                    case KeyType.Guid:
                        return ReadFieldValue<Guid>(PrimaryKeyField).ToString();
                    case KeyType.Integer:
                    case KeyType.IntegerAutoIncrement:
                        return ReadFieldValue<int>(PrimaryKeyField).ToString();
                    case KeyType.String:
                        return ReadFieldValue<string>(PrimaryKeyField);
                }
            else
                switch (ParentCollection.ParentEntity.PrimaryKeyType)
                {
                    case KeyType.Guid:
                        return ReadFieldValue<Guid>(PrimaryKeyField).ToString();
                    case KeyType.Integer:
                    case KeyType.IntegerAutoIncrement:
                        return ReadFieldValue<int>(PrimaryKeyField).ToString();
                    case KeyType.String:
                        return ReadFieldValue<string>(PrimaryKeyField);
                }

            return string.Empty;
        }
    }

    /// <summary>
    /// Field name of the primary key field
    /// </summary>
    [NotReportSerializable]
    [NotClonable]
    public string PrimaryKeyField { set; get; } = string.Empty;

    /// <summary>
    /// Returns whether or not that field's value is currently null/nothing
    /// </summary>
    /// <param name="fieldName">Field name as it appears in the data set</param>
    /// <returns>True or false</returns>
    public virtual bool IsFieldNull(string fieldName)
    {
        if (ParentEntity is BusinessEntity entity)
            fieldName = entity.GetInternalFieldName(fieldName, TableName);
        return CurrentRow[fieldName] == DBNull.Value;
    }

    /// <summary>
    /// Method used to assign the internal current row field
    /// </summary>
    /// <param name="currentRow">DataRow</param>
    public virtual void SetCurrentRow(DataRow currentRow) => CurrentRow = currentRow;

    /// <summary>
    /// Removes the current item from the collection
    /// </summary>
    public virtual void Remove()
    {
        CurrentRow.Delete();

        // We raise an update event
        if (ParentEntity is BusinessEntity entity)
            entity.DataUpdated(string.Empty, CurrentRow.Table.TableName);
        if (ParentCollection is EntitySubItemCollection collection)
            collection.DataUpdated(string.Empty, CurrentRow);
    }

    /// <summary>
    /// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
    /// </summary>
    /// <returns>
    /// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
    /// </returns>
    public override string ToString() => Id;

    /// <summary>
    /// Sets the value of a field in the database
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <param name="value">New value</param>
    /// <param name="forceUpdate">Should the value be set, even if it is the same as before (and therefore set the dirty flag despite that there were no changes)?</param>
    /// <returns>True or False</returns>
    protected virtual bool SetFieldValue(string fieldName, object value, bool forceUpdate = false)
    {
        CheckColumn(fieldName);
        return SetFieldValue(fieldName, value, forceUpdate, CurrentRow);
    }

    /// <summary>
    /// Sets the value of a field in the database
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <param name="value">New value</param>
    /// <param name="forceUpdate">Should the value be set, even if it is the same as before (and therefore set the dirty flag despite that there were no changes)?</param>
    /// <returns>True or False</returns>
    protected virtual bool WriteFieldValue<TField>(string fieldName, TField value, bool forceUpdate = false)
    {
        CheckColumn(fieldName);
        return WriteFieldValue(fieldName, value, forceUpdate, CurrentRow);
    }

    /// <summary>
    /// Sets the value of a field in the database
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <param name="value">New value</param>
    /// <param name="forceUpdate">Should the value be set, even if it is the same as before (and therefore set the dirty flag despite that there were no changes)?</param>
    /// <param name="currentRow">Row that has the field that needs to be updated</param>
    /// <returns>True or False</returns>
    protected virtual bool SetFieldValue(string fieldName, object value, bool forceUpdate, DataRow currentRow)
    {
        if (!(ParentEntity is BusinessEntity entity)) throw new NullReferenceException("Parent entity is not a business entity.");
        var retVal = BusinessEntityHelper.SetFieldValue(entity, fieldName, value, TableName, currentRow.Table.DataSet, currentRow, forceUpdate);

        entity.DataUpdated(fieldName, currentRow.Table.TableName);
        if (ParentCollection is EntitySubItemCollection collection)
            collection.DataUpdated(fieldName, currentRow);
        return retVal;
    }

    /// <summary>
    /// Sets the value of a field in the database
    /// </summary>
    /// <typeparam name="TField">The type of the field.</typeparam>
    /// <param name="fieldName">Field name</param>
    /// <param name="value">New value</param>
    /// <param name="forceUpdate">Should the value be set, even if it is the same as before (and therefore set the dirty flag despite that there were no changes)?</param>
    /// <param name="currentRow">Row that has the field that needs to be updated</param>
    /// <returns>True or False</returns>
    protected virtual bool WriteFieldValue<TField>(string fieldName, TField value, bool forceUpdate, DataRow currentRow)
    {
        if (!(ParentEntity is BusinessEntity entity)) throw new NullReferenceException("Parent entity is not a business entity.");
        var retVal = BusinessEntityHelper.SetFieldValue(entity, fieldName, value, TableName, currentRow.Table.DataSet, currentRow, forceUpdate);

        entity.DataUpdated(fieldName, currentRow.Table.TableName);
        if (ParentCollection is EntitySubItemCollection collection)
            collection.DataUpdated(fieldName, currentRow);
        return retVal;
    }

    /// <summary>
    /// Sets the value of a field in the database in the table specified.
    /// The record of the table is identified by the search expression
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <param name="value">New value</param>
    /// <param name="forceUpdate">Should the value be set, even if it is the same as before (and therefore set the dirty flag despite that there were no changes)?</param>
    /// <param name="tableName">Name of the table that holds the row that needs to be updated</param>
    /// <param name="searchExpression">Search expression used to identify the record that needs to be updated</param>
    /// <returns>True or False</returns>
    /// <example>SetFieldValue("MyField","xxx value",true,"MySecondaryTable","id = 'x'");</example>
    protected virtual bool SetFieldValue(string fieldName, object value, bool forceUpdate, string tableName, string searchExpression)
    {
        if (ParentEntity is not BusinessEntity entity) throw new NullReferenceException("Parent entity is not a business entity.");

        // TODO: We should probably do something with the search expression, so that can work too with maps
        fieldName = entity.GetInternalFieldName(fieldName, tableName);
        tableName = entity.GetInternalTableName(tableName);

        // Before we can do anything else, we have to retrieve the appropriate table
        if (!CurrentRow.Table.DataSet.Tables.Contains(tableName))
            // The specified table name does not exist. We need to throw an error!
            throw new ArgumentException("Table '@tableName' not in DataSet", "tableName");
        var secondaryTable = CurrentRow.Table.DataSet.Tables[tableName];

        // Now that we have the table, we can try to find the desired row
        var matchingRows = secondaryTable.Select(searchExpression);
        // We expect to find exactly one row. If fewer or more rows get returned, we throw an error
        if (matchingRows.Length != 1)
        {
            if (matchingRows.Length > 1)
                // The record was not uniquely identified
                throw new ArgumentException("Unable to find unique record: " + matchingRows.Length.ToString(NumberFormatInfo.InvariantInfo) + " records returned by search expression '@searchExpression'", "searchExpression");
            throw new RowNotInTableException("Search record not found by expression '@searchExpression'");
        }

        // We did find the row. We can now make sure it has the field we desire
        CheckColumn(fieldName, secondaryTable);

        var retVal = BusinessEntityHelper.SetFieldValue(entity, fieldName, value, tableName, matchingRows[0].Table.DataSet, matchingRows[0], forceUpdate);

        entity.DataUpdated(fieldName, CurrentRow.Table.TableName);

        if (ParentCollection is EntitySubItemCollection collection)
            collection.DataUpdated(fieldName, matchingRows[0]);
        return retVal;
    }

    /// <summary>
    /// Sets the value of a field in the database in the table specified.
    /// The record of the table is identified by the search expression
    /// </summary>
    /// <typeparam name="TField">The type of the field.</typeparam>
    /// <param name="fieldName">Field name</param>
    /// <param name="value">New value</param>
    /// <param name="forceUpdate">Should the value be set, even if it is the same as before (and therefore set the dirty flag despite that there were no changes)?</param>
    /// <param name="tableName">Name of the table that holds the row that needs to be updated</param>
    /// <param name="searchExpression">Search expression used to identify the record that needs to be updated</param>
    /// <returns>True or False</returns>
    /// <example>SetFieldValue("MyField","xxx value",true,"MySecondaryTable","id = 'x'");</example>
    protected virtual bool WriteFieldValue<TField>(string fieldName, TField value, bool forceUpdate, string tableName, string searchExpression)
    {
        if (ParentEntity is not BusinessEntity entity) throw new NullReferenceException("Parent entity is not a business entity.");

        fieldName = entity.GetInternalFieldName(fieldName, tableName);
        tableName = entity.GetInternalTableName(tableName);

        // Before we can do anything else, we have to retrieve the appropriate table
        if (!CurrentRow.Table.DataSet.Tables.Contains(tableName))
            // The specified table name does not exist. We need to throw an error!
            throw new ArgumentException("Table '@tableName' not in DataSet.", "tableName");
        var secondaryTable = CurrentRow.Table.DataSet.Tables[tableName];

        // Now that we have the table, we can try to find the desired row
        var matchingRows = secondaryTable.Select(searchExpression);
        // We expect to find exactly one row. If fewer or more rows get returned, we throw an error
        if (matchingRows.Length != 1)
        {
            if (matchingRows.Length > 1)
                // The record was not uniquely identified
                throw new ArgumentException("Cannot find unique record: " + matchingRows.Length.ToString(NumberFormatInfo.InvariantInfo) + " records returned by search expression '@searchExpression'", "searchExpression");
            throw new RowNotInTableException("Search record not found by expression '@searchExpression'");
        }

        // We did find the row. We can now make sure it has the field we desire
        CheckColumn(fieldName, secondaryTable);

        var retVal = BusinessEntityHelper.SetFieldValue(entity, fieldName, value, tableName, matchingRows[0].Table.DataSet, matchingRows[0], forceUpdate);

        entity.DataUpdated(fieldName, CurrentRow.Table.TableName);
        if (ParentCollection is EntitySubItemCollection collection)
            collection.DataUpdated(fieldName, matchingRows[0]);
        return retVal;
    }

    /// <summary>
    /// Sets the value of a field in the database in the table specified.
    /// The record of the table is identified by the search expression
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <param name="value">New value</param>
    /// <param name="tableName">Name of the table that holds the row that needs to be updated</param>
    /// <param name="searchExpression">Search expression used to identify the record that needs to be updated</param>
    /// <returns>True or False</returns>
    /// <example>SetFieldValue("MyField","xxx value","MySecondaryTable","id = 'x'");</example>
    protected virtual bool SetFieldValue(string fieldName, object value, string tableName, string searchExpression) => SetFieldValue(fieldName, value, false, tableName, searchExpression);

    /// <summary>
    /// Sets the value of a field in the database in the table specified.
    /// The record of the table is identified by the search expression
    /// </summary>
    /// <typeparam name="TField">The type of the field.</typeparam>
    /// <param name="fieldName">Field name</param>
    /// <param name="value">New value</param>
    /// <param name="tableName">Name of the table that holds the row that needs to be updated</param>
    /// <param name="searchExpression">Search expression used to identify the record that needs to be updated</param>
    /// <returns>True or False</returns>
    /// <example>SetFieldValue("MyField","xxx value","MySecondaryTable","id = 'x'");</example>
    protected virtual bool WriteFieldValue<TField>(string fieldName, TField value, string tableName, string searchExpression) => WriteFieldValue(fieldName, value, false, tableName, searchExpression);

    /// <summary>
    /// Returns the value of the specified field in the database
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <param name="ignoreNulls">Should nulls be ignored and returned as such (true) or should the be turned into default values (false)?</param>
    /// <returns>Value object</returns>
    protected virtual object GetFieldValue(string fieldName, bool ignoreNulls = false)
    {
        if (ParentEntity is not BusinessEntity entity) throw new NullReferenceException("Parent entity is not a business entity.");
        return BusinessEntityHelper.GetFieldValue<object>(entity, CurrentRow.Table.DataSet, TableName, fieldName, CurrentRow, ignoreNulls);
    }

    /// <summary>
    /// Returns the value of the specified field in the database
    /// </summary>
    /// <typeparam name="TField">The type of the field.</typeparam>
    /// <param name="fieldName">Field name</param>
    /// <param name="ignoreNulls">Should nulls be ignored and returned as such (true) or should the be turned into default values (false)?</param>
    /// <returns>Value object</returns>
    protected virtual TField ReadFieldValue<TField>(string fieldName, bool ignoreNulls = false)
    {
        if (ParentEntity is not BusinessEntity entity) throw new NullReferenceException("Parent entity is not a business entity.");
        return BusinessEntityHelper.GetFieldValue<TField>(entity, CurrentRow.Table.DataSet, TableName, fieldName, CurrentRow, ignoreNulls);
    }

    /// <summary>
    /// Returns the value of the specified field in the database
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <param name="ignoreNulls">Should nulls be ignored and returned as such (true) or should the be turned into default values (false)?</param>
    /// <param name="currentRow">Current data row, which contains the value we are interested in</param>
    /// <returns>Value object</returns>
    protected virtual object GetFieldValue(string fieldName, bool ignoreNulls, DataRow currentRow)
    {
        if (ParentEntity is not BusinessEntity entity) throw new NullReferenceException("Parent entity is not a business entity.");
        return BusinessEntityHelper.GetFieldValue<object>(entity, currentRow.Table.DataSet, TableName, fieldName, currentRow, ignoreNulls);
    }

    /// <summary>
    /// Returns the value of the specified field in the database
    /// </summary>
    /// <typeparam name="TField">The type of the field.</typeparam>
    /// <param name="fieldName">Field name</param>
    /// <param name="ignoreNulls">Should nulls be ignored and returned as such (true) or should the be turned into default values (false)?</param>
    /// <param name="currentRow">Current data row, which contains the value we are interested in</param>
    /// <returns>Value object</returns>
    protected virtual TField ReadFieldValue<TField>(string fieldName, bool ignoreNulls, DataRow currentRow)
    {
        if (ParentEntity is not BusinessEntity entity) throw new NullReferenceException("Parent entity is not a business entity.");
        return BusinessEntityHelper.GetFieldValue<TField>(entity, currentRow.Table.DataSet, TableName, fieldName, currentRow, ignoreNulls);
    }

    /// <summary>
    /// Returns the value from a field in a row of the specified table.
    /// The row is identified by the primary key field name and value that is passed along.
    /// The table is identified by the provided table name.
    /// This method can be used to retrieve information from 1:1 related secondary tables
    /// in scenarios where child items have related table that extend the child row's schema.
    /// In most scenarios, this overload is NOT needed. Use the simpler overloads instead!
    /// </summary>
    /// <param name="fieldName">Name of the field that contains the value.</param>
    /// <param name="tableName">Name of the table that contains the field</param>
    /// <param name="searchExpression">Search (filter) expression used to identify the record in the secondary table</param>
    /// <returns>Value object</returns>
    /// <example>GetFieldValue("CustomerStatus", "ExtendedCustomerInformationTable", "cust_id = 'x'");</example>
    /// <remarks>May throw ArgumentException and RowNotInTableException.</remarks>
    protected virtual object GetFieldValue(string fieldName, string tableName, string searchExpression) => GetFieldValue(fieldName, false, tableName, searchExpression);

    /// <summary>
    /// Returns the value from a field in a row of the specified table.
    /// The row is identified by the primary key field name and value that is passed along.
    /// The table is identified by the provided table name.
    /// This method can be used to retrieve information from 1:1 related secondary tables
    /// in scenarios where child items have related table that extend the child row's schema.
    /// In most scenarios, this overload is NOT needed. Use the simpler overloads instead!
    /// </summary>
    /// <typeparam name="TField">The type of the field.</typeparam>
    /// <param name="fieldName">Name of the field that contains the value.</param>
    /// <param name="tableName">Name of the table that contains the field</param>
    /// <param name="searchExpression">Search (filter) expression used to identify the record in the secondary table</param>
    /// <returns>Value object</returns>
    /// <example>GetFieldValue("CustomerStatus", "ExtendedCustomerInformationTable", "cust_id = 'x'");</example>
    /// <remarks>May throw ArgumentException and RowNotInTableException.</remarks>
    protected virtual TField GetFieldValue<TField>(string fieldName, string tableName, string searchExpression) => ReadFieldValue<TField>(fieldName, false, tableName, searchExpression);

    /// <summary>
    /// Returns the value from a field in a row of the specified table.
    /// The row is identified by the primary key field name and value that is passed along.
    /// The table is identified by the provided table name.
    /// This method can be used to retrieve information from 1:1 related secondary tables
    /// in scenarios where child items have related table that extend the child row's schema.
    /// In most scenarios, this overload is NOT needed. Use the simpler overloads instead!
    /// </summary>
    /// <param name="fieldName">Name of the field that contains the value.</param>
    /// <param name="ignoreNulls">Should nulls be returned (true) or should default values be provided for nulls (false)?</param>
    /// <param name="tableName">Name of the table that contains the field</param>
    /// <param name="searchExpression">Search (filter) expression used to identify the record in the secondary table</param>
    /// <returns>Value object</returns>
    /// <example>GetFieldValue("CustomerStatus", true, "ExtendedCustomerInformationTable", "cust_id = 'x'");</example>
    /// <remarks>May throw ArgumentException and RowNotInTableException.</remarks>
    protected virtual object GetFieldValue(string fieldName, bool ignoreNulls, string tableName, string searchExpression)
    {
        if (ParentEntity is not BusinessEntity entity) throw new NullReferenceException("Parent entity is not a business entity.");

        fieldName = ((BusinessEntity) ParentEntity).GetInternalFieldName(fieldName, tableName);

        var secondaryTable = CurrentRow.Table.DataSet.Tables[tableName];

        // Now that we have the table, we can try to find the desired row
        var foundRows = secondaryTable.Select(searchExpression);
        // We expect to find exactly one row. If fewer or more rows get returned, we throw an error
        if (foundRows.Length != 1)
        {
            if (foundRows.Length > 1)
                // The record was not uniquely identified
                throw new ArgumentException("Cannot find unique record: " + foundRows.Length.ToString(NumberFormatInfo.InvariantInfo) + " records returned by search expression '@searchExpression'", "searchExpression");
            throw new RowNotInTableException("Search record not found by expression '@searchExpression'");
        }

        // We did find the row. We can now make sure it has the field we desire
        CheckColumn(fieldName, secondaryTable);

        // Finally, we are ready to retrieve the desired field
        return BusinessEntityHelper.GetFieldValue<object>(entity, foundRows[0].Table.DataSet, tableName, fieldName, foundRows[0], ignoreNulls);
    }

    /// <summary>
    /// Returns the value from a field in a row of the specified table.
    /// The row is identified by the primary key field name and value that is passed along.
    /// The table is identified by the provided table name.
    /// This method can be used to retrieve information from 1:1 related secondary tables
    /// in scenarios where child items have related table that extend the child row's schema.
    /// In most scenarios, this overload is NOT needed. Use the simpler overloads instead!
    /// </summary>
    /// <typeparam name="TField">The type of the field.</typeparam>
    /// <param name="fieldName">Name of the field that contains the value.</param>
    /// <param name="ignoreNulls">Should nulls be returned (true) or should default values be provided for nulls (false)?</param>
    /// <param name="tableName">Name of the table that contains the field</param>
    /// <param name="searchExpression">Search (filter) expression used to identify the record in the secondary table</param>
    /// <returns>Value object</returns>
    /// <example>GetFieldValue("CustomerStatus", true, "ExtendedCustomerInformationTable", "cust_id = 'x'");</example>
    /// <remarks>May throw ArgumentException and RowNotInTableException.</remarks>
    protected virtual TField ReadFieldValue<TField>(string fieldName, bool ignoreNulls, string tableName, string searchExpression)
    {
        // TODO: We may have to do something with the search expression, which should also support maps

        if (ParentEntity is not BusinessEntity entity) throw new NullReferenceException("Parent entity is not a business entity.");

        fieldName = ((BusinessEntity) ParentEntity).GetInternalFieldName(fieldName, tableName);

        var secondaryTable = CurrentRow.Table.DataSet.Tables[tableName];

        // Now that we have the table, we can try to find the desired row
        var foundRows = secondaryTable.Select(searchExpression);
        // We expect to find exactly one row. If fewer or more rows get returned, we throw an error
        if (foundRows.Length != 1)
        {
            if (foundRows.Length > 1)
                // The record was not uniquely identified
                throw new ArgumentException("Unable to find unique record: " + foundRows.Length.ToString(NumberFormatInfo.InvariantInfo) + " records returned by search expression '@searchExpression'", "searchExpression");
            throw new RowNotInTableException("Search record not found by expression '@searchExpression'");
        }

        // We did find the row. We can now make sure it has the field we desire
        CheckColumn(fieldName, secondaryTable);

        // Finally, we are ready to retrieve the desired field
        return BusinessEntityHelper.GetFieldValue<TField>(entity, foundRows[0].Table.DataSet, tableName, fieldName, foundRows[0], ignoreNulls);
    }

    /// <summary>
    /// Method used to check whether the current data row has a certain field
    /// </summary>
    /// <param name="fieldName">Field Name</param>
    /// <returns>True (if the column existed or has been added successfully) or False</returns>
    protected virtual bool CheckColumn(string fieldName) => CheckColumn(fieldName, CurrentRow.Table);

    /// <summary>
    /// This method can be used to make sure the default table in the internal DataSet has all the required fields.
    /// If the field (column) doesn't exist, it will be added.
    /// </summary>
    /// <param name="fieldName">Field name to check for.</param>
    /// <param name="tableToCheck">Table that is supposed to have this column.</param>
    /// <returns>true or false</returns>
    protected virtual bool CheckColumn(string fieldName, DataTable tableToCheck) => BusinessEntityHelper.CheckColumn(tableToCheck, fieldName);

    /// <summary>
    /// Checks whether two field values are the same or not
    /// </summary>
    /// <param name="value1">First value</param>
    /// <param name="value2">Second value</param>
    /// <returns>True of they are different, false if they are the same</returns>
    protected virtual bool ValuesDiffer(object value1, object value2) => ObjectHelper.ValuesDiffer(value1, value2);

    /// <summary>
    /// Sets the parent collection for this item.
    /// </summary>
    /// <param name="parentCollection">The parent collection.</param>
    public void SetParentCollection(IEntitySubItemCollection parentCollection) => ParentCollection = parentCollection;
}