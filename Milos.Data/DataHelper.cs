using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

namespace Milos.Data
{
    /// <summary>
    ///     This static class provides methods that may be useful for data related tasks
    /// </summary>
    public static class DataHelper
    {
        /// <summary>
        ///     This method builds delete commands based on individual Sql statements
        /// </summary>
        /// <param name="tableName">Name of the table that contains the record that is to be deleted.</param>
        /// <param name="primaryKeyFieldName">Primary key field name</param>
        /// <param name="primaryKeyValue">Primary key value</param>
        /// <param name="dataService">Instance of a concrete data service</param>
        /// <returns>IDbCommand object</returns>
        public static IDbCommand BuildDeleteCommand(string tableName, string primaryKeyFieldName, object primaryKeyValue, IDataService dataService)
        {
            var deleteCommand = dataService.NewCommandObject();
            deleteCommand.CommandText = "DELETE FROM " + tableName + " WHERE " + primaryKeyFieldName + " = @PK";
            deleteCommand.Parameters.Add(dataService.NewCommandObjectParameter("@PK", primaryKeyValue));
            return deleteCommand;
        }

        /// <summary>
        ///     This method takes an empty command object as well as a data row, and builds
        ///     an SQL compliant update command, depending on the row state and update mode.
        /// </summary>
        /// <param name="changedRow">Updated data row</param>
        /// <param name="primaryKeyType">Primary key type (guid, integer,...)</param>
        /// <param name="primaryKeyField">Name of the primary key field</param>
        /// <param name="dataService">Instance of a concrete data service</param>
        /// <param name="updateMode">Should all fields be sent to the server, or only the changed ones?</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="fieldNames">Names of the fields to be included in the update (all others will be ignored)</param>
        /// <param name="fieldMaps">
        ///     List of key value pairs that can be used to map field names. For instance, if a field in the
        ///     table is called MyId but in the database it is called ID, then one can add a key 'MyId' with a value of 'ID'
        /// </param>
        /// <returns>
        ///     Fully configured command object that is ready to be executed.
        /// </returns>
        public static IDbCommand BuildSqlUpdateCommand(DataRow changedRow, KeyType primaryKeyType, string primaryKeyField, IDataService dataService, DataRowUpdateMode updateMode, string tableName, IList<string> fieldNames = null, IDictionary<string, string> fieldMaps = null)
        {
            if (fieldMaps == null) fieldMaps = new Dictionary<string, string>();
            if (fieldNames == null) fieldNames = new List<string>();

            // We create a new command object
            var emptyCommand = dataService.NewCommandObject();

            // We will use a string builder to generate the command since it is much faster than a simple string
            var commandBuilder = new StringBuilder();
            string fieldName;
            var fieldCount = 0;

            // We need to make sure which one of the included fields in the table is the PK field,
            // which may be different in name from the name specified in the parameter, since
            // the field may be mapped.
            var primaryKeyFieldInMemory = primaryKeyField;
            foreach (var key in fieldMaps.Keys)
            {
                var value = fieldMaps[key];
                if (value == primaryKeyFieldInMemory)
                {
                    primaryKeyFieldInMemory = key;
                    break;
                }
            }

            // First of all, we need to find out whether this is a new, updated, or deleted row
            switch (changedRow.RowState)
            {
                case DataRowState.Added:
                    // We generate an insert command
                    var newParametersBuilder = new StringBuilder();
                    var newFieldsBuilder = new StringBuilder();
                    var usedFieldsCounter = -1;
                    for (var fieldCounter = 0; fieldCounter < changedRow.ItemArray.Length; fieldCounter++)
                    {
                        // We basically include all fields. The only field we may leave out is the primary
                        // key field of identity integer key business objects.
                        fieldName = changedRow.Table.Columns[fieldCounter].ColumnName;
                        if (fieldNames.Count == 0 || ContainsString(fieldNames, fieldName, true))
                            if (primaryKeyType != KeyType.IntegerAutoIncrement || fieldName == primaryKeyFieldInMemory)
                                if (!changedRow.Table.Columns[fieldName].AutoIncrement)
                                    if (changedRow[fieldName] != DBNull.Value)
                                    {
                                        usedFieldsCounter++;
                                        if (usedFieldsCounter > 0)
                                        {
                                            newFieldsBuilder.Append(", ");
                                            newParametersBuilder.Append(", ");
                                        }

                                        newFieldsBuilder.Append(fieldName);
                                        var fieldNameInDatabase = fieldName;
                                        if (fieldMaps.ContainsKey(fieldNameInDatabase)) fieldNameInDatabase = fieldMaps[fieldNameInDatabase];
                                        newParametersBuilder.Append("@p" + fieldNameInDatabase);
                                        emptyCommand.Parameters.Add(dataService.NewCommandObjectParameter("@p" + fieldNameInDatabase, changedRow[fieldName]));
                                    }
                    }

                    if (usedFieldsCounter > -1)
                        emptyCommand.CommandText = "INSERT INTO " + tableName + "( " + newFieldsBuilder + ") VALUES (" + newParametersBuilder + ")";
                    else
                    {
                        emptyCommand.Dispose();
                        return null;
                    }

                    // If this is an incremental integer primary key BO, then the current key will not be right,
                    // since it is only a temporary key. Therefore, we need to make sure we replace it with the 
                    // new (real) key the database generates for us. We do this by checking whether the operation
                    // succeeded, and if so, we return the new key. Otherwise, we return -1. All of this is
                    // embedded in a single command that we pass to SQL Server.
                    if (primaryKeyType == KeyType.IntegerAutoIncrement) emptyCommand.CommandText += "; if @@ROWCOUNT < 1 select -1 AS [SCOPE_IDENTITY] else SELECT convert(int,SCOPE_IDENTITY()) AS [SCOPE_IDENTITY]";
                    break;
                case DataRowState.Deleted:
                    // We generate a delete command
                    emptyCommand.CommandText = "DELETE FROM " + tableName + " WHERE " + primaryKeyField + " = @pPK";
                    emptyCommand.Parameters.Add(dataService.NewCommandObjectParameter("@pPK", changedRow[primaryKeyField, DataRowVersion.Original]));
                    break;
                case DataRowState.Modified:
                    // We generate an update command
                    commandBuilder.Append("UPDATE " + tableName + " SET ");
                    // Sometimes, a row is flagged as modified, but then does not have changed fields.
                    var bModified = false;
                    for (var fieldCounter = 0; fieldCounter < changedRow.ItemArray.Length; fieldCounter++)
                    {
                        fieldName = changedRow.Table.Columns[fieldCounter].ColumnName;
                        if (fieldNames.Count == 0 || ContainsString(fieldNames, fieldName, true))
                        {
                            var fieldNameInDatabase = fieldName;
                            if (fieldMaps.ContainsKey(fieldNameInDatabase)) fieldNameInDatabase = fieldMaps[fieldNameInDatabase];
                            // We do NOT want to update the PK field
                            if (fieldName != primaryKeyFieldInMemory)
                                if (!changedRow.Table.Columns[fieldName].AutoIncrement)
                                {
                                    var fieldsDiffer = ValuesDiffer(changedRow[fieldCounter, DataRowVersion.Current], changedRow[fieldCounter, DataRowVersion.Original]);

                                    if (updateMode == DataRowUpdateMode.AllFields || fieldsDiffer)
                                    {
                                        bModified = true;
                                        fieldCount++;
                                        if (fieldCount > 1) commandBuilder.Append(", ");
                                        commandBuilder.Append(fieldNameInDatabase);
                                        commandBuilder.Append(" = @p" + fieldNameInDatabase);
                                        emptyCommand.Parameters.Add(dataService.NewCommandObjectParameter("@p" + fieldNameInDatabase, changedRow[fieldName]));
                                    }
                                }
                        }
                    }

                    emptyCommand.Parameters.Add(dataService.NewCommandObjectParameter("@pPK", changedRow[primaryKeyFieldInMemory]));
                    commandBuilder.Append(" WHERE " + primaryKeyField + " = @pPK");
                    emptyCommand.CommandText = commandBuilder.ToString();
                    // If there were no updates, we do not have a valid command object, and need to get rid of it.
                    if (bModified == false)
                    {
                        emptyCommand.Dispose();
                        emptyCommand = null;
                    }

                    break;
                default:
                    // Nothing to do here...
                    emptyCommand.Dispose();
                    emptyCommand = null;
                    break;
            }

            return emptyCommand;
        }

        /// <summary>
        ///     This method takes an empty command object as well as a data row, and builds
        ///     an SQL compliant update command, depending on the row state and update mode.
        ///     The command assumes that a certain Stored Procedure is available to perform
        ///     the desired update.
        /// </summary>
        /// <param name="changedRow">Updated data row</param>
        /// <param name="primaryKeyField">Name of the primary key field</param>
        /// <param name="primaryKeyType">Primary key type (guid, integer,...)</param>
        /// <param name="dataService">Instance of a concrete data service</param>
        /// <param name="updateMode">Should all fields be sent to the server, or only the changed ones?</param>
        /// <param name="storedProcedurePrefix">Prefix used for the stored procedure</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="fieldNames">Names of the fields to be included in the update (all others will be ignored)</param>
        /// <param name="fieldMaps">
        ///     List of key value pairs that can be used to map field names. For instance, if a field in the
        ///     table is called MyId but in the database it is called ID, then one can add a key 'MyId' with a value of 'ID'
        /// </param>
        /// <returns>Fully configured command object that is ready to be executed.</returns>
        /// <remarks>
        ///     Stored Procedure update commands rely on a stored procedure of a certain name to be present. The name of the stored
        ///     procedure follows the following pattern:
        ///     [Prefix]upd[TableName]
        ///     So in a scenario where the default prefix is used, and the table name is Customer, the following Stored Procedure
        ///     would be required:
        ///     milos_updCustomer
        ///     The SP needs to accept one parameter for each field (named the same as the fields - the cName field requires a
        ///     corresponding @cName parameter) as well as an additional
        ///     parameter called @__cChangedFields, which contains a comma-separated list of all changed fields (with a trailing
        ///     comma, and no spaces).
        ///     The Stored Procedure also needs to know (without any outside parameters) what the name of the primary key field is
        ///     that is used to identify the updated row. This value is simply passed to this SP as one of the parameters.
        /// </remarks>
        public static IDbCommand BuildStoredProcedureUpdateCommand(DataRow changedRow, KeyType primaryKeyType, string primaryKeyField, IDataService dataService, DataRowUpdateMode updateMode, string storedProcedurePrefix, string tableName, IList<string> fieldNames, IDictionary<string, string> fieldMaps)
        {
            if (fieldNames == null) fieldNames = new List<string>();
            if (fieldMaps == null) fieldMaps = new Dictionary<string, string>();

            // We create a new command object
            var emptyCommand = dataService.NewCommandObject();
            emptyCommand.CommandType = CommandType.StoredProcedure;

            // We will use a string builder to generate the command since it is much faster than a simple string
            var commandBuilder = new StringBuilder();
            string fieldName;

            // We need to make sure which one of the included fields in the table is the PK field,
            // which may be different in name from the name specified in the parameter, since
            // the field may be mapped.
            var primaryKeyFieldInMemory = primaryKeyField;
            foreach (var key in fieldMaps.Keys)
            {
                var value = fieldMaps[key];
                if (value == primaryKeyFieldInMemory)
                {
                    primaryKeyFieldInMemory = key;
                    break;
                }
            }

            // First of all, we need to find out whether this is a new, updated, or deleted row
            switch (changedRow.RowState)
            {
                case DataRowState.Added:
                    // We generate an insert command
                    var newParametersBuilder = new StringBuilder();
                    var newFieldsBuilder = new StringBuilder();
                    var fieldsUsed = -1;
                    for (var fieldCounter = 0; fieldCounter < changedRow.ItemArray.Length; fieldCounter++)
                    {
                        // We basically include all fields. The only field we may leave out is the primary
                        // key field of identity integer key business objects (which would not be needed,
                        // since the identity value will be created automatically on the server).
                        fieldName = changedRow.Table.Columns[fieldCounter].ColumnName;
                        if (fieldNames.Count == 0 || ContainsString(fieldNames, fieldName, true))
                        {
                            var fieldNameInDatabase = fieldName;
                            if (fieldMaps.ContainsKey(fieldNameInDatabase)) fieldNameInDatabase = fieldMaps[fieldNameInDatabase];
                            if (primaryKeyType != KeyType.IntegerAutoIncrement || fieldName != primaryKeyField)
                                if (changedRow[fieldName] != DBNull.Value)
                                {
                                    fieldsUsed++;
                                    if (fieldsUsed > 0)
                                    {
                                        newFieldsBuilder.Append(", ");
                                        newParametersBuilder.Append(", ");
                                    }

                                    newFieldsBuilder.Append(fieldNameInDatabase);
                                    newParametersBuilder.Append("@" + fieldNameInDatabase);
                                    emptyCommand.Parameters.Add(dataService.NewCommandObjectParameter("@" + fieldNameInDatabase, changedRow[fieldName]));
                                }
                        }
                    }

                    if (fieldsUsed > 0)
                        emptyCommand.CommandText = storedProcedurePrefix + "upd" + tableName;
                    else
                    {
                        emptyCommand.Dispose();
                        return null;
                    }

                    // Note: Since we are inserting, we do not need to send the field list as a parameter.
                    //       The Stored Procedure knows that for inserts, all fields have to be updated.

                    // NOTE FOR COMPARISON WITH SINGLE-COMMAND MODE OBJECTS:
                    // If this is an incremental integer primary key BO, then the current key will not be right,
                    // since it is only a temporary key. Therefore, we need to make sure we replace it with the 
                    // new (real) key the database generates for us. We do this by checking whether the operation
                    // succeeded, and if so, we return the new key. Otherwise, we return -1. While in single-command
                    // mode this is all embedded into the command we send, things are a bit different in a 
                    // Stored Procedure environment where the Stored Procedure needs to take on that responsibility.
                    break;
                case DataRowState.Deleted:
                    emptyCommand.CommandText = storedProcedurePrefix + "del" + tableName;
                    emptyCommand.Parameters.Add(dataService.NewCommandObjectParameter("@" + primaryKeyField, changedRow[primaryKeyField, DataRowVersion.Original]));
                    break;
                case DataRowState.Modified:
                    // We generate an update command
                    commandBuilder.Append(storedProcedurePrefix + "upd" + tableName);
                    // Sometimes, a row is flagged as modified, but then does not have changed fields.
                    var isModified = false;
                    // List of fields that are to be updated
                    var updatedFieldList = string.Empty;
                    for (var fieldCounter = 0; fieldCounter < changedRow.ItemArray.Length; fieldCounter++)
                    {
                        fieldName = changedRow.Table.Columns[fieldCounter].ColumnName;
                        if (fieldNames.Count != 0 && !ContainsString(fieldNames, fieldName, true)) continue;
                        var fieldNameInDatabase = fieldName;
                        if (fieldMaps.ContainsKey(fieldNameInDatabase)) fieldNameInDatabase = fieldMaps[fieldNameInDatabase];
                        // We do NOT want to update the PK field
                        if (fieldName == primaryKeyField) continue;
                        var fieldsDiffer = ValuesDiffer(changedRow[fieldCounter, DataRowVersion.Current], changedRow[fieldCounter, DataRowVersion.Original]);
                        if (updateMode != DataRowUpdateMode.AllFields && !fieldsDiffer) continue;
                        isModified = true;
                        updatedFieldList += fieldNameInDatabase + ",";
                        emptyCommand.Parameters.Add(dataService.NewCommandObjectParameter("@" + fieldNameInDatabase, changedRow[fieldName]));
                    }

                    // This still needs to be passed, since it is used to identify the primary key field
                    emptyCommand.Parameters.Add(dataService.NewCommandObjectParameter("@" + primaryKeyField, changedRow[primaryKeyFieldInMemory]));
                    // We need to specify a list of fields that have been updated
                    emptyCommand.Parameters.Add(dataService.NewCommandObjectParameter("@__cChangedFields", updatedFieldList));
                    // We are ready to build the command object
                    emptyCommand.CommandText = commandBuilder.ToString();
                    // If there were no updates, we do not have a valid command object, and need to get rid of it.
                    if (isModified == false)
                    {
                        emptyCommand.Dispose();
                        emptyCommand = null;
                    }

                    break;
                default:
                    // Nothing to do here...
                    emptyCommand.Dispose();
                    emptyCommand = null;
                    break;
            }

            return emptyCommand;
        }

        /// <summary>
        ///     This method takes an IDbCommand object and serializes it to XML.
        ///     Note that this is a custom serialization.
        ///     We only respect the actual command and the parameters.
        /// </summary>
        /// <param name="command">IDbCommand object</param>
        /// <param name="entityName">Query result table name (if applicable... pass "" otherwise)</param>
        /// <returns>Xml serialized string</returns>
        public static string SerializeIDbCommandToXml(IDbCommand command, string entityName = "")
        {
            // We need to look at the command object, and serialize it into a specialized XML string so we can send it to the server.
            var stream = new MemoryStream();
            var writer = new XmlTextWriter(stream, Encoding.UTF8);

            writer.WriteStartElement("dbCommand");
            writer.WriteElementString("commandText", command.CommandText);
            writer.WriteElementString("commandType", command.CommandType.ToString());
            writer.WriteElementString("entityName", entityName);

            writer.WriteStartElement("parameters");

            foreach (IDataParameter para in command.Parameters)
            {
                writer.WriteStartElement("parameter");
                writer.WriteAttributeString("dbType", para.DbType.ToString());
                writer.WriteAttributeString("name", para.ParameterName);
                switch (para.DbType)
                {
                    case DbType.Binary:
                        var buffer = (byte[]) para.Value;
                        writer.WriteAttributeString("value", Convert.ToBase64String(buffer, 0, buffer.Length));
                        break;
                    default:
                        writer.WriteAttributeString("value", para.Value.ToString());
                        break;
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement(); // <parameters>
            writer.WriteEndElement(); // <sqlCommand>

            // This is all we really need. We now memorize this XML segment for later use
            writer.Flush();

            var resultString = StreamToString(stream);
            while (!resultString.StartsWith("<")) resultString = resultString.Substring(1);

            return resultString;
        }

        /// <summary>
        /// Turns a stream into a string
        /// </summary>
        /// <param name="streamToConvert">Input stream</param>
        /// <returns>Output String</returns>
        private static string StreamToString(Stream streamToConvert)
        {
            if (streamToConvert == null) return string.Empty;

            var retVal = string.Empty;
            var stream = streamToConvert;

            stream.Position = 0;
            if (stream.CanRead && stream.CanSeek)
            {
                var length = (int) stream.Length;
                var buffer = new byte[length];
                stream.Read(buffer, 0, length);
                retVal = Encoding.UTF8.GetString(buffer);
            }

            return retVal;
        }

        /// <summary>
        /// Returns true if the array contains the string we are looking for
        /// </summary>
        /// <param name="hostArray">The host array.</param>
        /// <param name="searchText">The search string.</param>
        /// <param name="ignoreCase">if set to <c>true</c> [ignore case].</param>
        /// <returns>True or false</returns>
        /// <example>
        /// string[] testArray = new string[] { "One", "Two", "Three" };
        /// bool result1 = StringHelper.ArrayContainsString(testArray, "one", true); // returns true
        /// bool result2 = StringHelper.ArrayContainsString(testArray, "one"); // returns false
        /// bool result3 = StringHelper.ArrayContainsString(testArray, "One"); // returns true
        /// bool result4 = StringHelper.ArrayContainsString(testArray, "Four"); // returns false
        /// </example>
        private static bool ContainsString(IEnumerable<string> hostArray, string searchText, bool ignoreCase)
        {
            var found = false;
            foreach (var item in hostArray)
                if (CompareStrings(item, searchText, ignoreCase))
                {
                    found = true;
                    break;
                }

            return found;
        }

        /// <summary>
        /// Returns true if the two strings match.
        /// </summary>
        /// <param name="firstString">First string</param>
        /// <param name="secondString">Second string</param>
        /// <param name="ignoreCase"></param>
        /// <returns>True or False</returns>
        /// <remarks>
        /// The strings are trimmed and compared in a case-insensitive, culture neutral fashion.
        /// </remarks>
        private static bool CompareStrings(string firstString, string secondString, bool ignoreCase = true) => string.Compare(firstString.Trim(), secondString.Trim(), ignoreCase, CultureInfo.InvariantCulture) == 0;

        /// <summary>
        /// Compares the values of two objects and returns true if the values are different
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>True if values DIFFER</returns>
        /// <example>
        /// object o1 = "Hello";
        /// object o2 = "World";
        /// object o3 = 25;
        /// ObjectHelper.ValuesDiffer(o1, o2); // returns true;
        /// ObjectHelper.ValuesDiffer(o1, o3); // returns true;
        /// </example>
        /// <remarks>
        /// This method has been created to be easily able to compare objects of unknown types.
        /// In particular, this is useful when comparing two fields in a DataSet.
        /// This method can even handle byte arrays.
        /// </remarks>
        public static bool ValuesDiffer(object value1, object value2)
        {
            if (value1 == null) return false;
            if (value2 == null) return false;

            var fieldsDiffer = false;

            if (value1 is IComparable comparableValue1 && value2 is IComparable comparableValue2) return !comparableValue1.Equals(comparableValue2);

            // Apparently, the values are not comparable. A likely scenario for this is that the
            // data is byte arrays, which do not implement IComparable, but they can still be compared.
            if (value1 is byte[] array1 && value2 is byte[] array2)
            {
                if (array1.Length != array2.Length)
                    return true; // Certainly not the same

                for (var arrayCounter = 0; arrayCounter < array1.Length; arrayCounter++)
                    if (array1[arrayCounter] != array2[arrayCounter])
                        // The first time this hits, we found a difference and are thus
                        // sure they can not be the same
                        return true;
                // If we made it this far, they have to be the same.
                return false;
            }

            // This seems to be a little more complex
            try
            {
                // First of all, we check for nulls on one end
                var v1IsNull = value1 is DBNull;
                var v2IsNull = value2 is DBNull;
                if (v1IsNull && !v2IsNull || !v1IsNull && v2IsNull)
                    // Note: I chose to return from here since setting
                    //       the return variable and waiting all the way
                    //       for the end of the method would have increased
                    //       the complexity of the method to a point where
                    //       I did not consider it helpful anymore.
                    return true;
            }
            catch (InvalidCastException)
            {
                // This is odd. For some reason, we were not able to cast the value.
                // We inform the developer about the problem.
                fieldsDiffer = true;
            }

            return fieldsDiffer;
        }
    }
}