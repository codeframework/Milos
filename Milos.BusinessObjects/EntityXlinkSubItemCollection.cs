using System;
using System.Data;
using CODE.Framework.Fundamentals.Utilities;
using Milos.Data;

namespace Milos.BusinessObjects
{
    /// <summary>
    ///     Summary description for EntityXlinkSubItemCollection.
    /// </summary>
    public class EntityXlinkSubItemCollection : EntitySubItemCollection, IEntityXlinkSubItemCollection
    {
        /// <summary>
        ///     Internal field used as a temporary buffer.
        ///     This field is not to be used for anything other than what it already is used for.
        ///     In other words: Do not access this field at any point in time in new methods, as
        ///     the field content may vary and you may see unexpected results!
        /// </summary>
        private Guid guidCurrentTargetFk;

        /// <summary>
        ///     Internal field used as a temporary buffer.
        ///     This field is not to be used for anything other than what it already is used for.
        ///     In other words: Do not access this field at any point in time in new methods, as
        ///     the field content may vary and you may see unexpected results!
        /// </summary>
        private int integerCurrentTargetFk;

        /// <summary>
        ///     Internal field used as a temporary buffer.
        ///     This field is not to be used for anything other than what it already is used for.
        ///     In other words: Do not access this field at any point in time in new methods, as
        ///     the field content may vary and you may see unexpected results!
        /// </summary>
        private string stringCurrentTargetFk;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="parentEntity">Parent entity</param>
        public EntityXlinkSubItemCollection(IBusinessEntity parentEntity) : base(parentEntity) { }

        /// <summary>
        ///     Internal reference to a cross-link table
        /// </summary>
        public DataTable InternalTargetDataTable { get; private set; }

        /// <summary>
        ///     This is the foreign key field that links to the target table.
        ///     Example: If this collection links names to categories, this field
        ///     is the name of the field that links to the primary key of the category.
        /// </summary>
        public string TargetForeignKeyField { get; set; } = string.Empty;

        /// <summary>
        ///     This is the primary key field used by the target table.
        ///     Example: If this collection links names to categories, this field
        ///     is the name of the primary key field of the category table.
        /// </summary>
        public string TargetPrimaryKeyField { get; set; } = string.Empty;

        /// <summary>
        ///     This is the field that identifies the table field used for text comparison
        ///     in the target table. For instance, if the user specifies to add a new record
        ///     that links to the "Enterprise Customer" category, then this setting will
        ///     identify the field that would hold that text value in the target table.
        /// </summary>
        public string TargetTextField { get; set; } = string.Empty;

        /// <summary>
        ///     Defines whether or not a new record gets added to the target table automatically
        /// </summary>
        public bool AutoAddTarget { get; set; }

        /// <summary>
        ///     This method adds a new record to the x-link DataSet and links to the specified foreign key record
        /// </summary>
        /// <param name="targetItemId">Target item ID (such as the primary of a group when linking to a group record)</param>
        /// <returns>New item</returns>
        public virtual IEntitySubItemCollectionItem Add(int targetItemId)
        {
            // We verify whether or not the target record exists
            if (TargetPrimaryKeyField.Length == 0) return null;

            // We are ready to find the record in the DataSet
            if (!InternalTargetDataTable.Columns.Contains(TargetPrimaryKeyField))
                throw new TargetItemNotFoundException("Field '" + TargetPrimaryKeyField + "' not found in target table.");

            var found = false;
            foreach (DataRow row in InternalTargetDataTable.Rows)
                if ((int) row[TargetPrimaryKeyField] == targetItemId)
                {
                    // This is the one!

                    // This looks completely wrong to me (Markus). Why look up the primary key field of another table in this row? We should just use the ID we have, not that we verified that it exists...
                    // I removed this line, since the guid gets set a few lines below anyway.
                    // guidCurrentTargetFk = (Guid)oRow[strPrimaryKeyField];
                    found = true;
                    // All done. We have what we want.
                    break;
                }

            if (found)
            {
                // We store the FK Guid, so we can use it on the AddNewRowInformation() method.
                integerCurrentTargetFk = targetItemId;
                return AddNewRow();
            }

            throw new TargetItemNotFoundException();
        }

        /// <summary>
        ///     Sets internally used data tables. This is usually done on or immediately after instantiation.
        /// </summary>
        /// <param name="table">Cross-Link Table between the main entity and the related table.</param>
        /// <param name="xlinkTargetTable">Table that holds the actual data.</param>
        public virtual void SetTable(DataTable table, DataTable xlinkTargetTable)
        {
            SetTable(table);
            InternalTargetDataTable = xlinkTargetTable;
        }

        /// <summary>
        ///     This method adds a new record to the x-link DataSet and links to the record
        ///     identified by it's descriptive text.
        ///     The field used for this operation is defined in the strTargetTextField field.
        /// </summary>
        /// <param name="targetItemText">
        ///     text used by the target table. For instance, if you want to link to a "People" category,
        ///     "People" would be the text passed along here.
        /// </param>
        /// <returns>New item</returns>
        public virtual IEntitySubItemCollectionItem Add(string targetItemText)
        {
            // TODO: THis method currently does not really support string key types.

            // Before we can perform this operation, we need to identify the target record we
            // are trying to link to. This can only be done if the object is configured right.
            if (TargetPrimaryKeyField.Length == 0)
                // Trouble! This needs to be provided!!!
                return null;

            if (TargetTextField.Length == 0 && AutoAddTarget == false)
                // Trouble! This needs to be provided!!!
                return null;

            // We are ready to find the record in the DataSet
            var found = false;

            // If we automatically add targets, add the new one, otherwise, we search for the record we need to link to
            if (AutoAddTarget)
            {
                CheckColumn(TargetPrimaryKeyField, InternalTargetDataTable);
                var rowTarget = InternalTargetDataTable.NewRow();
                switch (ParentEntity.PrimaryKeyType)
                {
                    case KeyType.Guid:
                        rowTarget[TargetPrimaryKeyField] = Guid.NewGuid();
                        break;
                    case KeyType.Integer:
                        rowTarget[PrimaryKeyField] = ParentEntity.AssociatedBusinessObject.GetNewIntegerKey(rowTarget.Table.TableName, rowTarget.Table.DataSet);
                        break;
                    case KeyType.IntegerAutoIncrement:
                        rowTarget[PrimaryKeyField] = ParentEntity.AssociatedBusinessObject.GetNewIntegerKey(rowTarget.Table.TableName, rowTarget.Table.DataSet);
                        break;
                    case KeyType.String:
                        rowTarget[PrimaryKeyField] = ParentEntity.AssociatedBusinessObject.GetNewStringKey(rowTarget.Table.TableName, rowTarget.Table.DataSet);
                        break;
                }

                var businessObject = ParentEntity.AssociatedBusinessObject;
                if (businessObject is BusinessObject businessObject2)
                    businessObject2.CallPopulateNewRecord(rowTarget, rowTarget.Table.TableName, rowTarget.Table.DataSet);

                InternalTargetDataTable.Rows.Add(rowTarget);
                guidCurrentTargetFk = (Guid) rowTarget[TargetPrimaryKeyField];
                found = true;
            }
            else
            {
                // We locate the record we need
                foreach (DataRow row in InternalTargetDataTable.Rows)
                    if (((string) row[TargetTextField]).Trim() == targetItemText)
                    {
                        // This is the one!
                        switch (ParentEntity.PrimaryKeyType)
                        {
                            case KeyType.Guid:
                                guidCurrentTargetFk = (Guid) row[TargetPrimaryKeyField];
                                break;
                            case KeyType.Integer:
                                integerCurrentTargetFk = (int) row[TargetPrimaryKeyField];
                                break;
                            case KeyType.IntegerAutoIncrement:
                                integerCurrentTargetFk = (int) row[TargetPrimaryKeyField];
                                break;
                            case KeyType.String:
                                stringCurrentTargetFk = (string) row[TargetPrimaryKeyField];
                                break;
                        }

                        found = true;
                        // All done. We have what we want.
                        break;
                    }
            }

            // Only if we found a record, will we add the new row.
            if (found) return AddNewRow();

            throw new TargetItemNotFoundException();
        }

        /// <summary>
        ///     Retrieves an item from the collection by its index
        ///     and adds the appropriate data to the new object.
        /// </summary>
        /// <param name="index">Numeric index</param>
        /// <returns>Item</returns>
        public override IEntitySubItemCollectionItem GetItemByIndex(int index) => GetItemByIndex(index, false);

        /// <summary>
        ///     This method is not supported here, as we need to know what to link to...
        /// </summary>
        /// <returns>New item</returns>
        public override IEntitySubItemCollectionItem Add() => AutoAddTarget ? Add(string.Empty) : null;

        /// <summary>
        ///     This method adds a new record to the x-link DataSet and links to the specified foreign key record
        /// </summary>
        /// <param name="targetItemId">Target item ID (such as the primary of a group when linking to a group record)</param>
        /// <returns>New item</returns>
        public virtual IEntitySubItemCollectionItem Add(Guid targetItemId)
        {
            // We verify whether or not the target record exists
            if (TargetPrimaryKeyField.Length == 0) return null;

            // We are ready to find the record in the DataSet
            if (!InternalTargetDataTable.Columns.Contains(TargetPrimaryKeyField)) throw new TargetItemNotFoundException("Field '" + TargetPrimaryKeyField + "' Not found in target table");

            var found = false;
            foreach (DataRow row in InternalTargetDataTable.Rows)
                if ((Guid) row[TargetPrimaryKeyField] == targetItemId)
                {
                    // This is the one!

                    // This looks completely wrong to me (Markus). Why look up the primary key field of another table in this row? We should just use the ID we have, not that we verified that it exists...
                    // I removed this line, since the guid gets set a few lines below anyway.
                    // guidCurrentTargetFk = (Guid)oRow[strPrimaryKeyField];
                    found = true;
                    // All done. We have what we want.
                    break;
                }

            if (found)
            {
                // We store the FK Guid, so we can use it on the AddNewRowInformation() method.
                guidCurrentTargetFk = targetItemId;
                return AddNewRow();
            }

            throw new TargetItemNotFoundException();
        }

        /// <summary>
        ///     Determines whether or not the collection contains
        ///     the specified category (a link to the category)
        /// </summary>
        /// <param name="category">Category (plain text)</param>
        /// <returns>True of linked, false otherwise.</returns>
        public virtual bool Contains(string category)
        {
            var found = false;
            foreach (EntitySubItemCollectionXLinkItem item in this)
                if (item.Text == category)
                {
                    found = true;
                    break;
                }

            return found;
        }

        /// <summary>
        ///     Determines whether or not the collection contains
        ///     the specified category (a link to the category)
        /// </summary>
        /// <param name="category">Category (plain text)</param>
        /// <param name="ignoreCase">Should the search be done case insensitive?</param>
        /// <returns>True of linked, false otherwise.</returns>
        public virtual bool Contains(string category, bool ignoreCase)
        {
            if (!ignoreCase) return Contains(category);

            var found = false;
            foreach (EntitySubItemCollectionXLinkItem item in this)
                if (StringHelper.Compare(item.Text, category))
                {
                    found = true;
                    break;
                }

            return found;
        }

        /// <summary>
        ///     This method can be used to make sure the default table in the internal DataSet has all the required fields.
        ///     If the field (column) doesn't exist, it will be added.
        /// </summary>
        /// <param name="fieldName">Field name to check for.</param>
        /// <param name="tableToCheck">Table that is supposed to have this column.</param>
        /// <returns>true or false</returns>
        protected virtual bool CheckColumn(string fieldName, DataTable tableToCheck) => BusinessEntityHelper.CheckColumn(tableToCheck, fieldName);

        /// <summary>
        ///     We use this method to add the foreign key of the target table, to complete the link
        /// </summary>
        /// <param name="newRow">New row that is being added to the internal table.</param>
        protected override void AddNewRowInformation(DataRow newRow)
        {
            // Just in case...
            base.AddNewRowInformation(newRow);
            // We make sure we have all the information we need for this operation...
            if (TargetForeignKeyField.Length == 0) return;

            // We take the foreign key of the target table, which we have stored in our 
            // internal buffer, and assign it to the new row. The row already has a PK,
            // and it already is linked to the parent record. So this completes the link.
            // After we perform this action, the default behavior will take over,
            // and the new record will be added to the internal table, to complete
            // this operation.
            switch (ParentEntity.PrimaryKeyType)
            {
                case KeyType.Guid:
                    newRow[TargetForeignKeyField] = guidCurrentTargetFk;
                    break;
                case KeyType.Integer:
                    newRow[TargetForeignKeyField] = integerCurrentTargetFk;
                    break;
                case KeyType.IntegerAutoIncrement:
                    newRow[TargetForeignKeyField] = integerCurrentTargetFk;
                    break;
                case KeyType.String:
                    newRow[TargetForeignKeyField] = stringCurrentTargetFk;
                    break;
            }
        }

        /// <summary>
        ///     Retrieves an item from the collection by its index
        ///     and adds the appropriate data to the new object.
        /// </summary>
        /// <param name="index">Numeric index</param>
        /// <param name="absoluteTableIndex">If true, the index is based on the data table, not the (potentially) sorted view</param>
        /// <returns>Item</returns>
        public override IEntitySubItemCollectionItem GetItemByIndex(int index, bool absoluteTableIndex)
        {
            // Note that this collection really only contains one member object (at a time).
            // This object is configured with an index, to know what row in the
            // DataTable to access. That object is then returned, and appears to be
            // a new object to the outside.
            var item = (EntitySubItemCollectionXLinkItem) GetItemObject();
            // We need to find the index of the item in the target table and pass that along as well
            // First, we need to find the row that corresponds to this index. We need to verify that
            // the records we are looking at are actual good records and not deleted.
            var rowIndex = 0;
            var undeletedRecords = 0;
            var recordFound = false;

            if (IsRawTable || absoluteTableIndex)
                foreach (DataRow row in InternalDataTable.Rows)
                {
                    rowIndex++;
                    if (row.RowState != DataRowState.Deleted && row.RowState != DataRowState.Detached) undeletedRecords++;

                    if (undeletedRecords == index + 1)
                    {
                        // We found the record we are after
                        recordFound = true;
                        break;
                    }
                }
            else
                // We can simply go through the array of rows in this filtered or sorted collection
                foreach (var row in SpecialView)
                {
                    rowIndex++;
                    if (row.RowState != DataRowState.Deleted && row.RowState != DataRowState.Detached) undeletedRecords++;

                    if (undeletedRecords == index + 1)
                    {
                        // We found the record we are after
                        recordFound = true;
                        break;
                    }
                }

            if (!recordFound)
                // We were not able to identify the record requested
                throw new IndexOutOfBoundsException();

            if (TargetForeignKeyField.Length > 0)
            {
                var foreignKey = InternalDataTable.Rows[rowIndex - 1][TargetForeignKeyField];
                var found = false;
                // Now that we have the target FK, we can proceed and search for that as well.
                DataRow row = null;
                for (var counter = 0; counter < InternalTargetDataTable.Rows.Count; counter++)
                {
                    switch (ParentEntity.PrimaryKeyType)
                    {
                        case KeyType.Guid:
                            if ((Guid) InternalTargetDataTable.Rows[counter][TargetPrimaryKeyField] == (Guid) foreignKey)
                            {
                                // We found the item!
                                row = InternalTargetDataTable.Rows[counter];
                                found = true;
                            }

                            break;
                        case KeyType.Integer:
                            if ((int) InternalTargetDataTable.Rows[counter][TargetPrimaryKeyField] == (int) foreignKey)
                            {
                                // We found the item!
                                row = InternalTargetDataTable.Rows[counter];
                                found = true;
                            }

                            break;
                        case KeyType.IntegerAutoIncrement:
                            if ((int) InternalTargetDataTable.Rows[counter][TargetPrimaryKeyField] == (int) foreignKey)
                            {
                                // We found the item!
                                row = InternalTargetDataTable.Rows[counter];
                                found = true;
                            }

                            break;
                        case KeyType.String:
                            if ((string) InternalTargetDataTable.Rows[counter][TargetPrimaryKeyField] == (string) foreignKey)
                            {
                                // We found the item!
                                row = InternalTargetDataTable.Rows[counter];
                                found = true;
                            }

                            break;
                    }

                    if (found) break;
                }

                if (found != true) throw new TargetItemNotFoundException();

                item.SetCurrentRow(InternalDataTable.Rows[rowIndex - 1], row, TargetTextField);
            }
            else
                // We may choose to ignore the target table. This would be the case if it is handled manually in a sub-class
                item.SetCurrentRow(InternalDataTable.Rows[rowIndex - 1]);

            // We keep a reference to the primary key field in the new item, so we can get that value if we have to
            item.PrimaryKeyField = TargetForeignKeyField;
            return item;
        }

        /// <summary>
        ///     Object configuration
        ///     We have to implement this here, as it is marked as abstract in the parent class.
        /// </summary>
        protected override void Configure() { }
    }
}