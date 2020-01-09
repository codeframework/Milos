using System;
using System.Data;
using System.Diagnostics;
using CODE.Framework.Fundamentals.Utilities;
using Milos.Data;

namespace Milos.BusinessObjects
{
    /// <summary>This class contains various helper methods used by the business entity class</summary>
    public static class BusinessEntityHelper
    {
        /// <summary>Returns the value of a field in the specified table of the specified DataSet</summary>
        /// <typeparam name="TField">The expected returned type for the field.</typeparam>
        /// <param name="entity">The business entity the field belongs to.</param>
        /// <param name="dataSet">The data set the field lives in.</param>
        /// <param name="tableName">Name of the table containing the field.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="rowIndex">Index of the row of interest within the DataSet.</param>
        /// <param name="ignoreNulls">Should null values be ignored and returned as null (true) or should they be turned into default values (false)?</param>
        /// <returns>Field value (or type default in exception scenarios)</returns>
        public static TField GetFieldValue<TField>(BusinessEntity entity, DataSet dataSet, string tableName, string fieldName, int rowIndex, bool ignoreNulls)
        {
            if (entity == null) throw new NullReferenceException("Parameter 'entity' cannot be null or empty.");
            if (dataSet == null) throw new NullReferenceException("Parameter 'dataSet' cannot be null or empty.");
            if (string.IsNullOrEmpty(tableName)) throw new NullReferenceException("Parameter 'tableName' cannot be null or empty.");
            if (string.IsNullOrEmpty(fieldName)) throw new NullReferenceException("Parameter 'fieldName' cannot be null or empty.");

            var internalTableName = entity.GetInternalTableName(tableName);

            return GetFieldValue<TField>(entity, dataSet, tableName, fieldName, dataSet.Tables[internalTableName].Rows[rowIndex], ignoreNulls);
        }

        /// <summary>Returns the value of a field in the specified table of the specified DataSet</summary>
        /// <typeparam name="TField">The expected returned type for the field.</typeparam>
        /// <param name="entity">The business entity the field belongs to.</param>
        /// <param name="dataSet">The data set the field lives in.</param>
        /// <param name="tableName">Name of the table containing the field.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="dataRow">The data row.</param>
        /// <param name="ignoreNulls">Should null values be ignored and returned as null (true) or should they be turned into default values (false)?</param>
        /// <returns>Field value (or type default in exception scenarios)</returns>
        public static TField GetFieldValue<TField>(BusinessEntity entity, DataSet dataSet, string tableName, string fieldName, DataRow dataRow, bool ignoreNulls)
        {
            if (entity == null) throw new NullReferenceException("Parameter 'entity' cannot be null or empty.");
            if (dataSet == null) throw new NullReferenceException("Parameter 'dataSet' cannot be null or empty.");
            if (string.IsNullOrEmpty(tableName)) throw new NullReferenceException("Parameter 'tableName' cannot be null or empty.");
            if (string.IsNullOrEmpty(fieldName)) throw new NullReferenceException("Parameter 'fieldName' cannot be null or empty.");
            if (dataRow == null) throw new NullReferenceException("Parameter 'dataRow' cannot be null or empty.");

            // We get the mapped names
            // If there's no mapping for the field, we get an empty string.
            // If that's the case, we must keep going with the fieldName that's 
            // been passed, otherwise CheckColumn looks for empty string, which 
            // ends up throwing an exception saying that "column '' can't be found" or something...
            var internalFieldName = entity.GetInternalFieldName(fieldName, tableName);
            if (!string.IsNullOrEmpty(internalFieldName))
                fieldName = internalFieldName;

            tableName = entity.GetInternalTableName(tableName);

            // We make sure the column exists, and then immediately return its value
            if (!CheckColumn(dataSet, fieldName, tableName))
                throw new FieldDoesntExistException("Field doesn't exist.") {Source = fieldName + "." + tableName};

            // We check for null values...
            if (!ignoreNulls)
                if (dataRow[fieldName] == DBNull.Value)
                    return (TField) GenerateDefaultValueForColumn(dataSet.Tables[tableName].Columns[fieldName]);

            try
            {
                // No null values found. We return the actual value
                // However, we do check to see if this is a DateTime field, because if so, we do some transformations.
                if (dataSet.Tables[tableName].Columns[fieldName].DataType == typeof(DateTime))
                {
                    //DateTime datValue = (DateTime)dsInternal.Tables[tableName].Rows[0][fieldName];
                    var currentBusinessObject = entity.AssociatedBusinessObject;
                    if (currentBusinessObject is BusinessObject currentBusinessObject2)
                    {
                        var currentService = currentBusinessObject2.DataService;
                        var currentDate = (DateTime) dataRow[fieldName];
                        if (currentDate == currentService.DateMinValue)
                            // This field contains the minimum date value of the current database.
                            // We convert it to .NET's minimum date value
                            return (TField) (object) DateTime.MinValue;
                        if (currentDate == currentService.DateMaxValue)
                            // This field contains the maximum date value of the current database.
                            // We convert it to .NET's maximum date value
                            return (TField) (object) DateTime.MaxValue;
                        return (TField) dataRow[fieldName];
                    }

                    return (TField) dataRow[fieldName];
                }

                return (TField) dataRow[fieldName];
            }
            catch
            {
                return default;
            }
        }

        /// <summary>Use this method to assign any value to the specified table in the provided data set</summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <param name="fieldName">Name of the field that is to be assigned</param>
        /// <param name="value">Value that is to be assigned</param>
        /// <param name="tableName">Name of the table the field is in</param>
        /// <param name="dataSet">The data set.</param>
        /// <param name="rowIndex">Index of the row.</param>
        /// <param name="forceSet">Should the value be set, even if it is the same as before? (Causes the object to be dirty, possibly without changes)</param>
        /// <returns>True if update succeeded</returns>
        public static bool SetFieldValue<TField>(BusinessEntity entity, string fieldName, TField value, string tableName, DataSet dataSet, int rowIndex, bool forceSet)
        {
            if (entity == null) throw new NullReferenceException("Parameter 'entity' cannot be null or empty.");
            if (string.IsNullOrEmpty(fieldName)) throw new NullReferenceException("Parameter 'fieldName' cannot be null or empty.");
            if (dataSet == null) throw new NullReferenceException("Parameter 'dataSet' cannot be null or empty.");

            return SetFieldValue(entity, fieldName, value, tableName, dataSet, dataSet.Tables[tableName].Rows[rowIndex], forceSet);
        }

        /// <summary>Use this method to assign any value to the specified table in the provided data set</summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <param name="fieldName">Name of the field that is to be assigned</param>
        /// <param name="value">Value that is to be assigned</param>
        /// <param name="tableName">Name of the table the field is in</param>
        /// <param name="dataSet">The data set.</param>
        /// <param name="dataRow">The data row.</param>
        /// <param name="forceSet">Should the value be set, even if it is the same as before? (Causes the object to be dirty, possibly without changes)</param>
        /// <returns>True if update succeeded</returns>
        public static bool SetFieldValue<TField>(BusinessEntity entity, string fieldName, TField value, string tableName, DataSet dataSet, DataRow dataRow, bool forceSet)
        {
            if (entity == null) throw new NullReferenceException("Parameter 'entity' cannot be null or empty.");
            if (string.IsNullOrEmpty(fieldName)) throw new NullReferenceException("Parameter 'fieldName' cannot be null or empty.");
            if (dataSet == null) throw new NullReferenceException("Parameter 'dataSet' cannot be null or empty.");
            if (dataRow == null) throw new NullReferenceException("Parameter 'dataRow");

            // We get the mapped names
            // If there's no mapping for the field, we get an empty string.
            // If that's the case, we must keep going with the fieldName 
            // that's been passed, otherwise CheckColumn looks for 
            // empty string, which ends up throwing an exception saying 
            // that "column '' can't be found" or something...
            var internalFieldName = entity.GetInternalFieldName(fieldName, tableName);
            if (!string.IsNullOrEmpty(internalFieldName))
                fieldName = internalFieldName;

            tableName = entity.GetInternalTableName(tableName);

            // We make sure the column exists, and then try to assign the new value
            if (!CheckColumn<TField>(dataSet, fieldName, tableName))
                throw new FieldDoesntExistException("Field doesn't exist.") {Source = fieldName + "." + tableName};

            // We perform special translations on date time values, since we always want to have
            // valid min/max values, no matter what the min/max values of the current database are.
            // In other words: Min/max values are "magic", no matter what their respective values
            // are in the database.
            if (value is DateTime)
            {
                var datValue = (DateTime) (object) value;
                if (datValue == DateTime.MinValue)
                {
                    var currentBusinessObject = entity.AssociatedBusinessObject;
                    if (currentBusinessObject is BusinessObject currentBusinessObject2)
                        // This only works if this is a default implementation of the EPS 
                        // business object class (or a subclass thereof).
                        value = (TField) (object) currentBusinessObject2.DataService.DateMinValue;
                }
                else if (datValue == DateTime.MaxValue)
                {
                    var currentBusinessObject = entity.AssociatedBusinessObject;
                    if (currentBusinessObject is BusinessObject currentBusinessObject2)
                        // This only works if this is a default implementation of the EPS 
                        // business object class (or a subclass thereof).
                        value = (TField) (object) currentBusinessObject2.DataService.DateMaxValue;
                }
            }

            // We may have to double-check the value we are trying to set
            if (entity.InvalidFieldUpdateMode != InvalidFieldBehavior.IgnoreInvalidValues)
            {
                var invalid = IsValueInvalid(dataSet.Tables[tableName], fieldName, value, entity);

                if (invalid && entity.InvalidFieldUpdateMode == InvalidFieldBehavior.RejectInvalidValues)
                {
                    Debug.Assert(false, $"Invalid Value for Field {fieldName} of Table {tableName} rejected.");
                    return false;
                }
                if (invalid)
                    // We attempt to fix the value
                    value = GetValidValue(dataSet.Tables[tableName], fieldName, value, entity);
            }

            // We are ready to proceed with our updates
            if (forceSet || ObjectHelper.ValuesDiffer(dataRow[fieldName], value))
            {
                dataRow[fieldName] = value;
                entity.DataUpdated(fieldName, tableName);
            }

            return true;
        }

        /// <summary>Checks whether a given column exists in the given table. If the column does not exists and should be created on the fly, call the overload that takes the column's value instead.</summary>
        /// <param name="dataSet">The DataSet.</param>
        /// <param name="fieldName">Field name to check for.</param>
        /// <param name="tableName">Name of the table the field is in</param>
        /// <returns>True or false to indicate whether or not the column exists on the table.</returns>
        public static bool CheckColumn(DataSet dataSet, string fieldName, string tableName)
        {
            if (dataSet == null) throw new NullReferenceException("Parameter 'dataSet' cannot be null or empty.");
            if (string.IsNullOrEmpty(fieldName)) throw new NullReferenceException("Parameter 'fieldName' cannot be null or empty.");
            if (string.IsNullOrEmpty(tableName)) throw new NullReferenceException("Parameter 'tableName' cannot be null or empty.");

            var table = dataSet.Tables[tableName];
            return table.Columns.Contains(fieldName);
        }

        /// <summary>Checks whether a given column exists in the given table. If the column does not exists and should be created on the fly, call the overload that takes the column's value instead.</summary>
        /// <param name="table">The data table.</param>
        /// <param name="fieldName">Field name to check for.</param>
        /// <remarks>Field and table names used here must be INTERNAL names, not mapped names.</remarks>
        /// <returns>True or false to indicate whether or not the column exists on the table.</returns>
        public static bool CheckColumn(DataTable table, string fieldName)
        {
            if (table == null) throw new NullReferenceException("Parameter 'table' cannot be null or empty.");
            if (string.IsNullOrEmpty(fieldName)) throw new NullReferenceException("Parameter 'fieldName' cannot be null or empty.");

            if (!table.Columns.Contains(fieldName))
            {
                table.Columns.Add(fieldName);
                foreach (DataRow row in table.Rows)
                    row[fieldName] = string.Empty;
            }

            return true;
        }

        /// <summary>This method can be used to make sure the default table in the internal RecordSet has all the required fields. If the field (column) doesn't exist, it will be added.</summary>
        /// <param name="dataSet">The DataSet.</param>
        /// <param name="fieldName">Field name to check for.</param>
        /// <param name="tableName">Name of the table the field is in</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static bool CheckColumn(DataSet dataSet, string fieldName, string tableName, object value)
        {
            if (dataSet == null) throw new NullReferenceException("Parameter 'dataSet' cannot be null or empty.");
            if (string.IsNullOrEmpty(fieldName)) throw new NullReferenceException("Parameter 'fieldName' cannot be null or empty.");
            if (string.IsNullOrEmpty(tableName)) throw new NullReferenceException("Parameter 'tableName' cannot be null or empty.");

            var table = dataSet.Tables[tableName];
            if (!table.Columns.Contains(fieldName))
                // Apparently, that column doesn't exist. Let's add it...
                table.Columns.Add(fieldName, value.GetType());
            return true;
        }

        /// <summary>This method can be used to make sure the default table in the internal recordset has all the required fields. If the field (column) doesn't exist, it will be added.</summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="dataSet">The DataSet.</param>
        /// <param name="fieldName">Field name to check for.</param>
        /// <param name="tableName">Name of the table the field is in</param>
        /// <returns></returns>
        public static bool CheckColumn<TField>(DataSet dataSet, string fieldName, string tableName)
        {
            if (dataSet == null) throw new NullReferenceException("Parameter 'dataSet' cannot be null or empty.");
            if (string.IsNullOrEmpty(fieldName)) throw new NullReferenceException("Parameter 'fieldName' cannot be null or empty.");
            if (string.IsNullOrEmpty(tableName)) throw new NullReferenceException("Parameter 'tableName' cannot be null or empty.");

            var table = dataSet.Tables[tableName];
            if (!table.Columns.Contains(fieldName))
                // Apparently, that column doesn't exist. Let's add it...
                table.Columns.Add(fieldName, typeof(TField));
            return true;
        }

        /// <summary>Inspects the provided column and returns an appropriate default value for the column, cast as an object type.</summary>
        /// <param name="column">The column used to determine the appropriate default value.</param>
        /// <returns>Default value for the column.</returns>
        public static object GenerateDefaultValueForColumn(DataColumn column)
        {
            if (column == null) throw new NullReferenceException("Parameter 'column' cannot be null or empty.");

            if (column.DataType == typeof(bool)) return false;

            if (column.DataType == typeof(byte[]))
                return Array.Empty<byte>();
            if (column.DataType == typeof(sbyte[]))
                return Array.Empty<sbyte>();
            if (column.DataType == typeof(byte))
                return byte.MinValue;
            if (column.DataType == typeof(sbyte))
                return sbyte.MinValue;
            if (column.DataType == typeof(char))
                return ' ';
            if (column.DataType == typeof(string))
                return string.Empty;
            if (column.DataType == typeof(DateTime))
                return DateTime.MinValue;
            if (column.DataType == typeof(decimal))
                return 0m;
            if (column.DataType == typeof(double))
                return 0d;
            if (column.DataType == typeof(short))
                return (short)0;
            if (column.DataType == typeof(int))
                return 0;
            if (column.DataType == typeof(long))
                return (long)0;
            if (column.DataType == typeof(ushort))
                return (ushort)0;
            if (column.DataType == typeof(uint))
                return (uint)0;
            if (column.DataType == typeof(ulong))
                return (ulong)0;
            if (column.DataType == typeof(float))
                return 0f;
            if (column.DataType == typeof(TimeSpan))
                return TimeSpan.MinValue;
            if (column.DataType == typeof(Guid))
                return Guid.Empty;
            return 0;
        }

        /// <summary>Checks whether the value is valid based on the definition of the field in the provided data table.</summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="table">Data table</param>
        /// <param name="fieldName">Field name within the data table</param>
        /// <param name="value">Value</param>
        /// <param name="entity">The entity.</param>
        /// <returns>True if invalid, false otherwise</returns>
        /// <remarks>Field and table names used here must be INTERNAL names, not mapped names.</remarks>
        public static bool IsValueInvalid<TField>(DataTable table, string fieldName, TField value, BusinessEntity entity)
        {
            if (table == null) throw new NullReferenceException("Parameter 'table' cannot be null or empty.");
            if (string.IsNullOrEmpty(fieldName)) throw new NullReferenceException("Parameter 'fieldName' cannot be null or empty.");
            if (entity == null) throw new NullReferenceException("Parameter 'entity' cannot be null or empty.");

            if (table.Columns[fieldName].DataType == typeof(string))
            {
                var valueString = value.ToString();
                var maxLength = table.Columns[fieldName].MaxLength;
                if (maxLength != -1 && valueString.Length > maxLength)
                    return true;
            }
            else if (table.Columns[fieldName].DataType == Type.GetType("System.DateTime"))
            {
                var associatedBusinessObject = entity.AssociatedBusinessObject;
                if (associatedBusinessObject is BusinessObject currentBusinessObject)
                {
                    // Date checking can only work if the business object is a subclass
                    // of the EPS Business Object class
                    var datValue = (DateTime) (object) value;
                    if (datValue < currentBusinessObject.DataService.DateMinValue)
                        return true;
                    if (datValue > currentBusinessObject.DataService.DateMaxValue)
                        return true;
                }
            }

            return false;
        }

        /// <summary>Fixes the value to be valid based on the current field type</summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="table">Data table</param>
        /// <param name="fieldName">Field name within the data table</param>
        /// <param name="value">Value</param>
        /// <param name="entity">The entity.</param>
        /// <returns>Valid value</returns>
        /// <remarks>Field and table names used here must be INTERNAL names, not mapped names.</remarks>
        public static TField GetValidValue<TField>(DataTable table, string fieldName, TField value, BusinessEntity entity)
        {
            if (table == null) throw new NullReferenceException("Parameter 'table' cannot be null or empty.");
            if (string.IsNullOrEmpty(fieldName)) throw new NullReferenceException("Parameter 'fieldName' cannot be null or empty.");
            if (entity == null) throw new NullReferenceException("Parameter 'entity' cannot be null or empty.");

            if (table.Columns[fieldName].DataType == typeof(string))
            {
                var valueString = value.ToString();
                var maxLength = table.Columns[fieldName].MaxLength;
                if (maxLength > 0 && valueString.Length > maxLength)
                    return (TField) (object) valueString.Substring(0, maxLength);
            }
            else if (table.Columns[fieldName].DataType == typeof(DateTime))
            {
                var associatedBusinessObject = entity.AssociatedBusinessObject;
                if (associatedBusinessObject is BusinessObject currentBusinessObject)
                {
                    // Date checking can only work if the business object is a subclass of the EPS Business Object class
                    var datValue = (DateTime) (object) value;
                    if (datValue < currentBusinessObject.DataService.DateMinValue)
                        return (TField) (object) currentBusinessObject.DataService.DateMinValue;
                    if (datValue > currentBusinessObject.DataService.DateMaxValue)
                        return (TField) (object) currentBusinessObject.DataService.DateMaxValue;
                }
            }

            return value;
        }

        /// <summary>Clears the rows from the specified table.</summary>
        /// <param name="dataSet">The data set.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <returns>Success indicator</returns>
        public static bool ClearRows(DataSet dataSet, string tableName)
        {
            if (dataSet == null) throw new NullReferenceException("Parameter 'dataSet' cannot be null or empty.");
            if (string.IsNullOrEmpty(tableName)) throw new NullReferenceException("Parameter 'tableName' cannot be null or empty.");

            if (dataSet.Tables.Contains(tableName))
            {
                if (dataSet.Tables[tableName].Rows.Count > 0)
                    dataSet.Tables[tableName].Rows.Clear();
                return true;
            }

            return false;
        }

        /// <summary>Checks whether a certain table has the specified minimum number of rows. If the table doesn't have the specified minimum number of rows (and autoAddRows is passed as true), the rows are automatically created.</summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="primaryKeyField">The primary key field.</param>
        /// <param name="minimumRowCount">The minimum row count.</param>
        /// <param name="autoAddRows">If set to <c>true</c>, adds missing rows automatically.</param>
        /// <param name="dataSet">The data set.</param>
        /// <param name="entity">The entity.</param>
        /// <returns>True if the table has the appropriate number of rows (or the appropriate number of rows has been added)</returns>
        public static bool CheckRows(string tableName, string primaryKeyField, int minimumRowCount, bool autoAddRows, DataSet dataSet, BusinessEntity entity)
        {
            if (dataSet == null) throw new NullReferenceException("Parameter 'dataSet' cannot be null or empty.");
            if (entity == null) throw new NullReferenceException("Parameter 'entity' cannot be null or empty.");
            if (string.IsNullOrEmpty(tableName)) throw new NullReferenceException("Parameter 'tableName' cannot be null or empty.");
            if (string.IsNullOrEmpty(primaryKeyField)) throw new NullReferenceException("Parameter 'primaryKeyField' cannot be null or empty.");

            if (!dataSet.Tables.Contains(tableName)) return false;
            if (dataSet.Tables[tableName].Rows.Count >= minimumRowCount) return true;

            if (!autoAddRows) return false;

            // We need to know the actual name of the pk field
            var primaryKeyFieldName = entity.GetInternalFieldName(primaryKeyField, tableName);

            // There aren't enough rows, so we add them...
            while (dataSet.Tables[tableName].Rows.Count < minimumRowCount)
            {
                // We must add more records
                var newRow = dataSet.Tables[tableName].NewRow();
                switch (entity.PrimaryKeyType)
                {
                    case KeyType.Guid:
                        newRow[primaryKeyFieldName] = Guid.NewGuid();
                        break;
                    case KeyType.Integer:
                        newRow[primaryKeyFieldName] = entity.AssociatedBusinessObject.GetNewIntegerKey(tableName, dataSet);
                        break;
                    case KeyType.IntegerAutoIncrement:
                        newRow[primaryKeyFieldName] = entity.AssociatedBusinessObject.GetNewIntegerKey(tableName, dataSet);
                        break;
                    case KeyType.String:
                        newRow[primaryKeyFieldName] = entity.AssociatedBusinessObject.GetNewStringKey(tableName, dataSet);
                        break;
                }

                if (entity.AssociatedBusinessObject is BusinessObject associatedBusinessObject)
                    associatedBusinessObject.CallPopulateNewRecord(newRow, tableName, dataSet);
                dataSet.Tables[tableName].Rows.Add(newRow);
            }

            return true;
        }
    }
}