using System;
using System.Collections.Generic;
using System.Data;
using Milos.Core.Utilities;

namespace Milos.BusinessObjects
{
    /// <summary>
    ///     This class provides an abstract implementation of a business rule.
    /// </summary>
    public abstract class BusinessRule : IBusinessRule
    {
        /// <summary>
        ///     Type of the rule violation
        /// </summary>
        private readonly RuleViolationType violationType;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="tableName">Name of the table the rule is to be applied to.</param>
        /// <param name="ruleType">Severity of the rule violation</param>
        protected BusinessRule(string tableName, RuleViolationType ruleType = RuleViolationType.Violation)
        {
            TableName = tableName;
            violationType = ruleType;
        }

        /// <summary>
        ///     Holds a reference to a business object
        /// </summary>
        protected IBusinessObject BusinessObject { get; private set; }

        /// <summary>
        ///     Holds the name of the table this rule is to be applied to
        /// </summary>
        public string TableName { get; }

        /// <summary>
        ///     Defines whether this rule is applied to deleted rows
        /// </summary>
        public bool CheckDeletedRows { get; set; }

        /// <summary>
        ///     Sets the business object this object belongs to.
        /// </summary>
        /// <param name="currentBusinessObject">Business Object</param>
        public virtual void SetBusinessObject(IBusinessObject currentBusinessObject)
        {
            BusinessObject = currentBusinessObject;
        }

        /// <summary>
        ///     Verifies a single row of data.
        /// </summary>
        /// <param name="currentRow">Data row that is to be verified</param>
        /// <param name="rowIndex">Index of the row that is to be identified (within its data table)</param>
        public abstract void VerifyRow(DataRow currentRow, int rowIndex);

        /// <summary>
        ///     This method can be used to log business rule violations.
        ///     This method is usually called from within the Verify() method.
        /// </summary>
        /// <param name="currentDataRow">DataRow that contains the data that violated a rule.</param>
        /// <param name="rowIndex">Index if the row within the current data set/ data table</param>
        /// <param name="fieldName">Field name that contains the violation.</param>
        /// <param name="message">Plain text message</param>
        protected virtual void LogBusinessRuleViolation(DataRow currentDataRow, int rowIndex, string fieldName, string message)
        {
            BusinessObject.LogBusinessRuleViolation(currentDataRow.Table.DataSet, currentDataRow.Table.TableName, fieldName, rowIndex, violationType, message, GetType().FullName);
        }
    }

    /// <summary>
    ///     This rule verifies that a certain field (or fields) are not empty.
    /// </summary>
    public class EmptyFieldBusinessRule : BusinessRule
    {
        /// <summary>
        ///     Default message
        /// </summary>
        private readonly string defaultMessage = string.Empty;

        /// <summary>
        ///     Internal list of messages
        /// </summary>
        private readonly List<string> messages = new List<string>();

        /// <summary>
        ///     For internal use only
        /// </summary>
        private string fields;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="fieldList">Field list (multiple fields can be separated by commas)</param>
        /// <param name="tableName">Table this object is responsible for.</param>
        /// <param name="message">Default message for broken rule</param>
        public EmptyFieldBusinessRule(string fieldList, string tableName, string message) : base(tableName)
        {
            fields = fieldList;
            defaultMessage = message;
            var fieldCounter = StringHelper.Occurs(fieldList, ",") + 1;
            for (var counter = 0; counter < fieldCounter; counter++)
                messages.Add(message);
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="fieldList">Field list (multiple fields can be separated by commas)</param>
        /// <param name="tableName">Table this object is responsible for.</param>
        /// <param name="violationType">Severity of the violation</param>
        public EmptyFieldBusinessRule(string fieldList, string tableName, RuleViolationType violationType) : base(tableName, violationType)
        {
            fields = fieldList;
            messages.Add(defaultMessage);
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="fieldList">Field list (multiple fields can be separated by commas)</param>
        /// <param name="tableName">Table this object is responsible for.</param>
        /// <param name="message">Default message for broken rule</param>
        /// <param name="violationType">Type of the violation.</param>
        public EmptyFieldBusinessRule(string fieldList, string tableName, string message, RuleViolationType violationType) : base(tableName, violationType)
        {
            fields = fieldList;
            defaultMessage = message;
            var fieldCounter = StringHelper.Occurs(fieldList, ",") + 1;
            for (var counter = 0; counter < fieldCounter; counter++)
                messages.Add(message);
        }

        /// <summary>
        ///     Verifies that the current row contains all the required fields
        /// </summary>
        /// <param name="currentRow">Current data row</param>
        /// <param name="rowIndex">Index of the row that is to be identified (within its data table)</param>
        public override void VerifyRow(DataRow currentRow, int rowIndex)
        {
            if (currentRow == null) throw new NullReferenceException("Parameter 'currentRow' cannot be null.");

            var fields = this.fields.Split(',');
            // We iterate over all the fields in the field list,
            // and make sure we have data for them.
            var fieldCounter = 0;
            foreach (var requiredField in fields)
            {
                if (currentRow.RowState != DataRowState.Deleted)
                    if (string.IsNullOrEmpty(currentRow[requiredField].ToString()))
                        LogBusinessRuleViolation(currentRow, rowIndex, requiredField, messages[fieldCounter]);
                    else if (currentRow[requiredField] == DBNull.Value)
                        LogBusinessRuleViolation(currentRow, rowIndex, requiredField, messages[fieldCounter]);
                    else if (currentRow[requiredField] is Guid)
                        if ((Guid) currentRow[requiredField] == Guid.Empty)
                            LogBusinessRuleViolation(currentRow, rowIndex, requiredField, messages[fieldCounter]);
                fieldCounter++;
            }
        }

        /// <summary>
        ///     Adds another field to the list of fields that can not be empty for this table.
        /// </summary>
        /// <param name="fieldName">Field name</param>
        public void AddFieldToList(string fieldName)
        {
            if (fields.Length > 0) fields += ",";
            fields += fieldName;
            messages.Add(defaultMessage);
        }

        /// <summary>
        ///     Adds another field to the list of fields that can not be empty for this table.
        /// </summary>
        /// <param name="fieldName">Field name</param>
        /// <param name="message">Message</param>
        public void AddFieldToList(string fieldName, string message)
        {
            if (fields.Length > 0) fields += ",";
            fields += fieldName;
            messages.Add(message);
        }
    }

    /// <summary>
    ///     This class provides an abstract implementation of a deletion business rule.
    /// </summary>
    public abstract class DeletionBusinessRule : BusinessRule
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DeletionBusinessRule" /> class.
        /// </summary>
        /// <param name="tableName">Name of the table the rule is to be applied to.</param>
        /// <param name="ruleType">Severity of the rule violation</param>
        protected DeletionBusinessRule(string tableName, RuleViolationType ruleType) : base(tableName, ruleType) { }

        /// <summary>
        ///     Verifies a single row of data.
        /// </summary>
        /// <param name="currentRow">Data row that is to be verified</param>
        /// <param name="rowIndex">Index of the row that is to be identified (within its data table)</param>
        public abstract override void VerifyRow(DataRow currentRow, int rowIndex);
    }
}