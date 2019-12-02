using System;
using System.Data;
using System.Globalization;

namespace Milos.BusinessObjects
{
    /// <summary>Basic interface for a x-link collection entity item.</summary>
    public interface IEntitySubItemCollectionXLinkItem : IEntitySubItemCollectionItem
    {
        /// <summary>Returns the name of the linked item.</summary>
        /// <remarks>The field value displayed here depends on the Target Text Field configuration of the collections.</remarks>
        string Text { get; }

        /// <summary>This overload memorizes the current target row, as well as the current actual row (default behavior).</summary>
        /// <param name="currentRow">Current X-Link table row</param>
        /// <param name="currentTargetRow">Current row in the target table</param>
        /// <param name="textFieldName">Name of the field in the target table that is the default text field for the table</param>
        void SetCurrentRow(DataRow currentRow, DataRow currentTargetRow, string textFieldName);

        /// <summary>Returns whether or not that field's value is currently null/nothing.</summary>
        /// <param name="fieldName">Field name as it appears in the data set</param>
        /// <param name="mode">Target or link table?</param>
        /// <returns>True or false</returns>
        bool IsFieldNull(string fieldName, XLinkItemAccessMode mode);

        /// <summary>
        /// Remove method that indicates whether or not a remove operation should only remove the link or also the record that is cross-linked
        /// </summary>
        /// <param name="mode">Remove mode</param>
        /// <returns></returns>
        bool Remove(XLinkItemRemoveMode mode);

        /// <summary>
        /// Defines the default removal mode if no specific remove parameter is specified
        /// </summary>
        XLinkItemRemoveMode DefaultRemoveMode { get; }

        /// <summary>
        /// Indicates whether the target record can be removed.
        /// </summary>
        /// <returns></returns>
        bool CanRemoveTargetRecord();
    }

    /// <summary>Item object used as members of x-link collections.</summary>
    public class EntitySubItemCollectionXLinkItem : EntitySubItemCollectionItem, IEntitySubItemCollectionXLinkItem
    {
        /// <summary>Internal reference to the target row.</summary>
        private DataRow currentTargetRow;

        /// <summary>Initializes a new instance of the EntitySubItemCollectionXLinkItem class.</summary>
        public EntitySubItemCollectionXLinkItem()
        {
        }

        /// <summary>Constructor</summary>
        /// <param name="parentCollection">Reference to the collection hosting this item</param>
        public EntitySubItemCollectionXLinkItem(IEntitySubItemCollection parentCollection) : base(parentCollection)
        {
        }

        /// <summary>Returns the name of the linked item.</summary>
        /// <remarks>The field value displayed here depends on the Target Text Field configuration of the collections.</remarks>
        public virtual string Text => TextFieldName.Length == 0 ? string.Empty : (string) GetFieldValue(TextFieldName, XLinkItemAccessMode.TargetTable);

        /// <summary>Name of the field accessed whenever the text property is queries.</summary>
        protected virtual string TextFieldName { get; set; } = string.Empty;

        /// <summary>Data row which is represented by the current items target.</summary>
        protected virtual DataRow CurrentTargetRow => currentTargetRow;

        /// <summary>Foreign key field that links to the target table.</summary>
        /// <remarks>Update this field if you want to link to a different record in the target table.</remarks>
        public virtual int TargetFKInteger
        {
            get
            {
                var collection = (EntityXlinkSubItemCollection) ParentCollection;
                return (int) GetFieldValue(collection.TargetForeignKeyField, XLinkItemAccessMode.CurrentTable);
            }
            set
            {
                if (ResetTargetRow(value))
                {
                    var collection = (EntityXlinkSubItemCollection) ParentCollection;
                    SetFieldValue(collection.TargetForeignKeyField, value, XLinkItemAccessMode.CurrentTable);
                }
                else
                {
                    // There is no such target!!!
                    throw new TargetItemNotFoundException("No target item: '" + value.ToString(CultureInfo.InvariantCulture) + "'");
                }
            }
        }

        /// <summary>Foreign key field that links to the target table.</summary>
        /// <remarks>Update this field if you want to link to a different record in the target table.</remarks>
        public virtual string TargetFKString
        {
            get
            {
                var collection = (EntityXlinkSubItemCollection) ParentCollection;
                return (string) GetFieldValue(collection.TargetForeignKeyField, XLinkItemAccessMode.CurrentTable);
            }
            set
            {
                if (value == null) throw new NullReferenceException("Parameter 'value' cannot be null.");

                if (ResetTargetRow(value))
                {
                    var collection = (EntityXlinkSubItemCollection) ParentCollection;
                    SetFieldValue(collection.TargetForeignKeyField, value, XLinkItemAccessMode.CurrentTable);
                }
                else
                {
                    // There is no such target!!!
                    throw new TargetItemNotFoundException("No target item: '" + value + "'");
                }
            }
        }

        /// <summary>Foreign key field that links to the target table.</summary>
        /// <remarks>Update this field if you want to link to a different record in the target table.</remarks>
        public virtual Guid TargetFK
        {
            get
            {
                var collection = (EntityXlinkSubItemCollection) ParentCollection;
                return (Guid) GetFieldValue(collection.TargetForeignKeyField, XLinkItemAccessMode.CurrentTable);
            }
            set
            {
                if (ResetTargetRow(value))
                {
                    var collection = (EntityXlinkSubItemCollection) ParentCollection;
                    SetFieldValue(collection.TargetForeignKeyField, value, XLinkItemAccessMode.CurrentTable);
                }
                else
                {
                    // There is no such target!!!
                    throw new TargetItemNotFoundException("No target item: '" + value + "'");
                }
            }
        }

        /// <summary>Sets the value of a field in the database. This overload allows to specify whether the value is set in the master table, or the target table.</summary>
        /// <param name="fieldName">Field name</param>
        /// <param name="value">New value</param>
        /// <param name="mode">Specifies whether we want to access the current (link) table ("Current") or the target table.</param>
        /// <param name="forceUpdate">
        ///     Should the value be set, even if it is the same as before (and therefore set the dirty flag
        ///     despite that there were no changes)?
        /// </param>
        /// <returns>True or False</returns>
        protected virtual bool SetFieldValue(string fieldName, object value, XLinkItemAccessMode mode, bool forceUpdate = false)
        {
            if (mode == XLinkItemAccessMode.CurrentTable) return SetFieldValue(fieldName, value, forceUpdate);

            BusinessEntityHelper.CheckColumn(CurrentTargetRow.Table, fieldName);
            if (!(ParentEntity is BusinessEntity entity)) throw new NullReferenceException("Parent entity is not a business entity.");
            return BusinessEntityHelper.SetFieldValue(entity, fieldName, value, CurrentTargetRow.Table.TableName, CurrentRow.Table.DataSet, CurrentTargetRow, forceUpdate);
        }

        /// <summary>Sets the value of a field in the database. This overload allows to specify whether the value is set in the master table, or the target table.</summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Field name</param>
        /// <param name="value">New value</param>
        /// <param name="mode">Specifies whether we want to access the current (link) table ("Current") or the target table.</param>
        /// <param name="forceUpdate">
        ///     Should the value be set, even if it is the same as before (and therefore set the dirty flag
        ///     despite that there were no changes)?
        /// </param>
        /// <returns>True or False</returns>
        protected virtual bool WriteFieldValue<TField>(string fieldName, TField value, XLinkItemAccessMode mode, bool forceUpdate = false)
        {
            if (mode == XLinkItemAccessMode.CurrentTable) return SetFieldValue(fieldName, value, forceUpdate);

            BusinessEntityHelper.CheckColumn(CurrentTargetRow.Table, fieldName);
            if (!(ParentEntity is BusinessEntity entity)) throw new NullReferenceException("Parent entity is not a business entity.");
            return BusinessEntityHelper.SetFieldValue(entity, fieldName, value, CurrentTargetRow.Table.TableName, CurrentRow.Table.DataSet, CurrentTargetRow, forceUpdate);
        }

        /// <summary>
        ///     Returns the value of the specified field in the database
        /// </summary>
        /// <param name="fieldName">Field name</param>
        /// <param name="mode">Accessing a field in the parent (link) or target (child/foreign) table?</param>
        /// <param name="ignoreNulls">
        ///     Should nulls be ignored and returned as such (true) or should the be turned into default
        ///     values (false)?
        /// </param>
        /// <returns>Value object</returns>
        protected virtual object GetFieldValue(string fieldName, XLinkItemAccessMode mode, bool ignoreNulls = false)
        {
            if (mode == XLinkItemAccessMode.CurrentTable) return GetFieldValue(fieldName);

            var entity = ParentEntity as BusinessEntity;
            if (entity == null) throw new NullReferenceException("Parent entity is not a business entity.");
            return BusinessEntityHelper.GetFieldValue<object>(entity, CurrentTargetRow.Table.DataSet, CurrentTargetRow.Table.TableName, fieldName, CurrentTargetRow, ignoreNulls);
        }

        /// <summary>
        ///     Returns the value of the specified field in the database
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Field name</param>
        /// <param name="mode">Accessing a field in the parent (link) or target (child/foreign) table?</param>
        /// <param name="ignoreNulls">
        ///     Should nulls be ignored and returned as such (true) or should the be turned into default
        ///     values (false)?
        /// </param>
        /// <returns>Value object</returns>
        protected virtual TField ReadFieldValue<TField>(string fieldName, XLinkItemAccessMode mode, bool ignoreNulls = false)
        {
            if (mode == XLinkItemAccessMode.CurrentTable) return ReadFieldValue<TField>(fieldName);

            if (!(ParentEntity is BusinessEntity entity)) throw new NullReferenceException("Parent entity is not a business entity.");
            return BusinessEntityHelper.GetFieldValue<TField>(entity, CurrentTargetRow.Table.DataSet, CurrentTargetRow.Table.TableName, fieldName, CurrentTargetRow, ignoreNulls);
        }

        /// <summary>
        ///     Returns whether or not that field's value is currently null/nothing
        /// </summary>
        /// <param name="fieldName">Field name as it appears in the data set</param>
        /// <param name="mode">Link table or target table?</param>
        /// <returns>True or false</returns>
        public virtual bool IsFieldNull(string fieldName, XLinkItemAccessMode mode)
        {
            if (mode == XLinkItemAccessMode.CurrentTable) return IsFieldNull(fieldName);

            if (ParentEntity is BusinessEntity entity)
                fieldName = entity.GetInternalFieldName(fieldName, currentTargetRow.Table.TableName);
            return currentTargetRow[fieldName] == DBNull.Value;
        }

        /// <summary>
        ///     Method used to check whether the current data row has a certain field
        ///     This overload allows passing a table object.
        /// </summary>
        /// <param name="fieldName">Field Name</param>
        /// <param name="tableRow">DataRow the field is a member of</param>
        /// <returns>True (if the column existed or has been added successfully) or False</returns>
        protected virtual bool CheckColumn(string fieldName, DataRow tableRow) => BusinessEntityHelper.CheckColumn(tableRow.Table, fieldName);

        /// <summary>
        ///     Finds the new target row within the DataSet
        /// </summary>
        /// <param name="foreignKey">Key the cross-link links to</param>
        /// <returns>True if the row was found. False otherwise.</returns>
        protected virtual bool ResetTargetRow(string foreignKey)
        {
            if (foreignKey == null)
                throw new NullReferenceException("Parameter 'foreignKey' cannot be null.");

            // We find the new target row based on primary key field information we have in the collection
            var collection = (EntityXlinkSubItemCollection) ParentCollection;
            var targetTable = CurrentTargetRow.Table;
            var targetRows = targetTable.Select(collection.TargetPrimaryKeyField + " = '" + foreignKey + "'");
            if (targetRows.Length < 1)
                // The row we tried to link to does not exists!
                return false;
            currentTargetRow = targetRows[0];
            return true;
        }

        /// <summary>
        ///     Finds the new target row within the DataSet
        /// </summary>
        /// <param name="foreignKey">Key the cross-link links to</param>
        /// <returns>True if the row was found. False otherwise.</returns>
        protected virtual bool ResetTargetRow(int foreignKey)
        {
            // We find the new target row based on primary key field information we have in the collection
            var collection = (EntityXlinkSubItemCollection) ParentCollection;
            var targetTable = CurrentTargetRow.Table;
            var targetRows = targetTable.Select(collection.TargetPrimaryKeyField + " = " + foreignKey.ToString(CultureInfo.InvariantCulture));
            if (targetRows.Length < 1)
                // The row we tried to link to does not exists!
                return false;
            currentTargetRow = targetRows[0];
            return true;
        }

        /// <summary>
        ///     Finds the new target row within the DataSet
        /// </summary>
        /// <param name="foreignKey">Key the cross-link links to</param>
        /// <returns>True if the row was found. False otherwise.</returns>
        protected virtual bool ResetTargetRow(Guid foreignKey)
        {
            // We find the new target row based on primary key field information we have in the collection
            var collection = (EntityXlinkSubItemCollection) ParentCollection;
            var targetTable = CurrentTargetRow.Table;
            var targetRows = targetTable.Select(collection.TargetPrimaryKeyField + " = '" + foreignKey + "'");
            if (targetRows.Length < 1)
                // The row we tried to link to does not exists!
                return false;
            currentTargetRow = targetRows[0];
            return true;
        }

        /// <summary>
        ///     This overload memorizes the current target row, as well as the current actual row (default behavior)
        /// </summary>
        /// <param name="currentRow">Current X-Link table row</param>
        /// <param name="targetRow">Current row in the target table</param>
        /// <param name="fieldName">Name of the field in the target table that is the default text field for the table</param>
        public virtual void SetCurrentRow(DataRow currentRow, DataRow targetRow, string fieldName)
        {
            base.SetCurrentRow(currentRow);
            currentTargetRow = targetRow;
            TextFieldName = fieldName;
        }

        /// <summary>
        ///     We provide a more meaningful "ToString" value.
        /// </summary>
        /// <returns>Text</returns>
        public override string ToString() => Text;

        public override void Remove() => Remove(DefaultRemoveMode);

        public virtual bool Remove(XLinkItemRemoveMode mode)
        {
            if (mode == XLinkItemRemoveMode.LinkAndTargetRecord)
            {
                // We first need to check if the current table x-linked to this entity can actually be deleted.
                // It may not be possible to do so, if other items are x-linked to it.
                if (!CanRemoveTargetRecord()) return false; 

                // Apparently it is OK to delete the linked record, so we remove it.
                CurrentTargetRow.Delete();
            }

            // Now we remove the assignment record, which removes the x-link
            CurrentRow.Delete();

            // We raise an update event
            if (ParentEntity is BusinessEntity entity)
                entity.DataUpdated(string.Empty, CurrentRow.Table.TableName);
            if (ParentCollection is EntitySubItemCollection collection)
                collection.DataUpdated(string.Empty, CurrentRow);

            return true;
        }

        public virtual XLinkItemRemoveMode DefaultRemoveMode { get; set; } = XLinkItemRemoveMode.LinkRecordOnly;

        public virtual bool CanRemoveTargetRecord() => true;
    }

    public enum XLinkItemRemoveMode
    {
        LinkRecordOnly,
        LinkAndTargetRecord
    }
}