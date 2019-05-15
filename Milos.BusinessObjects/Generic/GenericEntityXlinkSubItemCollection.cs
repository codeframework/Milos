using System;
using System.Data;

namespace Milos.BusinessObjects.Generic
{
    /// <summary>
    ///     Summary description for EntityXlinkSubItemCollection.
    /// </summary>
    /// <typeparam name="TItem">The type of the item.</typeparam>
    public class GenericEntityXlinkSubItemCollection<TItem> : EntityXlinkSubItemCollection, IGenericEntityXlinkSubItemCollection<TItem> where TItem : IEntitySubItemCollectionXLinkItem, new()
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="parentEntity">Parent entity</param>
        /// <param name="primaryKeyField">The primary key field name. (Example: pk_CategoryAssignment)</param>
        /// <param name="foreignKeyField">The foreign key field name. (Example: fk_Names)</param>
        /// <param name="parentTableName">Name of the parent table name. (Example: Names)</param>
        /// <param name="parentTablePrimaryKeyField">The parent table primary key field name. (Example: pk_Names)</param>
        /// <param name="targetForeignKeyField">The target foreign key field. (Example: fk_Category)</param>
        /// <param name="targetPrimaryKeyField">The target primary key field. (Example: pk_Category)</param>
        /// <param name="targetTextField">The target text field name. (Example: CategoryName)</param>
        /// <param name="autoAddTarget">
        ///     Specifies whether target records are automatically added if they do not yet exist (such as
        ///     adding a new category on the fly).
        /// </param>
        /// <param name="xlinkTable">The xlink table. (Example: CategoryAssignment)</param>
        /// <param name="targetTable">The target table. (Example: Categories)</param>
        /// <remarks>
        ///     This type of collection can be used to create a cross-link relationship between two
        ///     tables by means of an intermediary table.
        ///     For instance, names could be linked to categories by means of a category assignment table.
        /// </remarks>
        public GenericEntityXlinkSubItemCollection(IBusinessEntity parentEntity, string primaryKeyField, string foreignKeyField, string parentTableName, string parentTablePrimaryKeyField, string targetForeignKeyField, string targetPrimaryKeyField, string targetTextField, bool autoAddTarget, DataTable xlinkTable, DataTable targetTable) : base(parentEntity)
        {
            PrimaryKeyField = primaryKeyField;
            ForeignKeyField = foreignKeyField;
            ParentTableName = parentTableName;
            ParentTablePrimaryKeyField = parentTablePrimaryKeyField;
            TargetTextField = targetTextField;
            TargetForeignKeyField = targetForeignKeyField;
            TargetPrimaryKeyField = targetPrimaryKeyField;
            AutoAddTarget = autoAddTarget;
            SetTable(xlinkTable, targetTable);
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="parentEntity">Parent entity</param>
        /// <param name="primaryKeyField">The primary key field name. (Example: pk_CategoryAssignment)</param>
        /// <param name="foreignKeyField">The foreign key field name. (Example: fk_Names)</param>
        /// <param name="parentTableName">Name of the parent table name. (Example: Names)</param>
        /// <param name="parentTablePrimaryKeyField">The parent table primary key field name. (Example: pk_Names)</param>
        /// <param name="targetForeignKeyField">The target foreign key field. (Example: fk_Category)</param>
        /// <param name="targetPrimaryKeyField">The target primary key field. (Example: pk_Category)</param>
        /// <param name="targetTextField">The target text field name. (Example: CategoryName)</param>
        /// <param name="xlinkTable">The xlink table. (Example: CategoryAssignment)</param>
        /// <param name="targetTable">The target table. (Example: Categories)</param>
        /// <remarks>
        ///     This type of collection can be used to create a cross-link relationship between two
        ///     tables by means of an intermediary table.
        ///     For instance, names could be linked to categories by means of a category assignment table.
        /// </remarks>
        public GenericEntityXlinkSubItemCollection(IBusinessEntity parentEntity, string primaryKeyField, string foreignKeyField, string parentTableName, string parentTablePrimaryKeyField, string targetForeignKeyField, string targetPrimaryKeyField, string targetTextField, DataTable xlinkTable, DataTable targetTable) : base(parentEntity)
        {
            PrimaryKeyField = primaryKeyField;
            ForeignKeyField = foreignKeyField;
            ParentTableName = parentTableName;
            ParentTablePrimaryKeyField = parentTablePrimaryKeyField;
            TargetTextField = targetTextField;
            TargetForeignKeyField = targetForeignKeyField;
            TargetPrimaryKeyField = targetPrimaryKeyField;
            AutoAddTarget = false;
            SetTable(xlinkTable, targetTable);
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="parentEntity">Parent entity</param>
        /// <param name="primaryKeyField">The primary key field name. (Example: pk_CategoryAssignment)</param>
        /// <param name="foreignKeyField">The foreign key field name. (Example: fk_Names)</param>
        /// <param name="parentTableName">Name of the parent table name. (Example: Names)</param>
        /// <param name="parentTablePrimaryKeyField">The parent table primary key field name. (Example: pk_Names)</param>
        /// <param name="targetForeignKeyField">The target foreign key field. (Example: fk_Category)</param>
        /// <param name="targetPrimaryKeyField">The target primary key field. (Example: pk_Category)</param>
        /// <param name="targetTextField">The target text field name. (Example: CategoryName)</param>
        /// <param name="autoAddTarget">
        ///     Specifies whether target records are automatically added if they do not yet exist (such as
        ///     adding a new category on the fly).
        /// </param>
        /// <remarks>
        ///     This type of collection can be used to create a cross-link relationship between two
        ///     tables by means of an intermediary table.
        ///     For instance, names could be linked to categories by means of a category assignment table.
        /// </remarks>
        public GenericEntityXlinkSubItemCollection(IBusinessEntity parentEntity, string primaryKeyField, string foreignKeyField, string parentTableName, string parentTablePrimaryKeyField, string targetForeignKeyField, string targetPrimaryKeyField, string targetTextField, bool autoAddTarget)
            : base(parentEntity)
        {
            PrimaryKeyField = primaryKeyField;
            ForeignKeyField = foreignKeyField;
            ParentTableName = parentTableName;
            ParentTablePrimaryKeyField = parentTablePrimaryKeyField;
            TargetTextField = targetTextField;
            TargetForeignKeyField = targetForeignKeyField;
            TargetPrimaryKeyField = targetPrimaryKeyField;
            AutoAddTarget = autoAddTarget;
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="parentEntity">Parent entity</param>
        /// <param name="primaryKeyField">The primary key field name. (Example: pk_CategoryAssignment)</param>
        /// <param name="foreignKeyField">The foreign key field name. (Example: fk_Names)</param>
        /// <param name="parentTableName">Name of the parent table name. (Example: Names)</param>
        /// <param name="parentTablePrimaryKeyField">The parent table primary key field name. (Example: pk_Names)</param>
        /// <param name="targetForeignKeyField">The target foreign key field. (Example: fk_Category)</param>
        /// <param name="targetPrimaryKeyField">The target primary key field. (Example: pk_Category)</param>
        /// <param name="targetTextField">The target text field name. (Example: CategoryName)</param>
        /// <remarks>
        ///     This type of collection can be used to create a cross-link relationship between two
        ///     tables by means of an intermediary table.
        ///     For instance, names could be linked to categories by means of a category assignment table.
        /// </remarks>
        public GenericEntityXlinkSubItemCollection(IBusinessEntity parentEntity, string primaryKeyField, string foreignKeyField, string parentTableName, string parentTablePrimaryKeyField, string targetForeignKeyField, string targetPrimaryKeyField, string targetTextField)
            : base(parentEntity)
        {
            PrimaryKeyField = primaryKeyField;
            ForeignKeyField = foreignKeyField;
            ParentTableName = parentTableName;
            ParentTablePrimaryKeyField = parentTablePrimaryKeyField;
            TargetTextField = targetTextField;
            TargetForeignKeyField = targetForeignKeyField;
            TargetPrimaryKeyField = targetPrimaryKeyField;
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="parentEntity">Parent entity</param>
        /// <param name="primaryKeyField">The primary key field name. (Example: pk_CategoryAssignment)</param>
        /// <param name="foreignKeyField">The foreign key field name. (Example: fk_Names)</param>
        /// <param name="parentTableName">Name of the parent table name. (Example: Names)</param>
        /// <param name="parentTablePrimaryKeyField">The parent table primary key field name. (Example: pk_Names)</param>
        /// <param name="targetForeignKeyField">The target foreign key field. (Example: fk_Category)</param>
        /// <param name="targetPrimaryKeyField">The target primary key field. (Example: pk_Category)</param>
        /// <remarks>
        ///     This type of collection can be used to create a cross-link relationship between two
        ///     tables by means of an intermediary table.
        ///     For instance, names could be linked to categories by means of a category assignment table.
        /// </remarks>
        public GenericEntityXlinkSubItemCollection(IBusinessEntity parentEntity, string primaryKeyField, string foreignKeyField, string parentTableName, string parentTablePrimaryKeyField, string targetForeignKeyField, string targetPrimaryKeyField)
            : base(parentEntity)
        {
            PrimaryKeyField = primaryKeyField;
            ForeignKeyField = foreignKeyField;
            ParentTableName = parentTableName;
            ParentTablePrimaryKeyField = parentTablePrimaryKeyField;
            TargetForeignKeyField = targetForeignKeyField;
            TargetPrimaryKeyField = targetPrimaryKeyField;
        }

        /// <summary>
        ///     This method is not supported here, as we need to know what to link to...
        /// </summary>
        /// <returns>New item</returns>
        public new virtual TItem Add() => (TItem) base.Add();

        /// <summary>
        ///     This method adds a new record to the x-link DataSet and links to the specified foreign key record
        /// </summary>
        /// <param name="targetItemId">Target item ID (such as the primary of a group when linking to a group record)</param>
        /// <returns>New item</returns>
        public new virtual TItem Add(Guid targetItemId) => (TItem) base.Add(targetItemId);

        /// <summary>
        ///     This method adds a new record to the x-link DataSet and links to the specified foreign key record
        /// </summary>
        /// <param name="targetItemId">Target item ID (such as the primary of a group when linking to a group record)</param>
        /// <returns>New item</returns>
        public new virtual TItem Add(int targetItemId) => (TItem) base.Add(targetItemId);

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
        public new virtual TItem Add(string targetItemText) => (TItem) base.Add(targetItemText);

        /// <summary>
        ///     Retrieves an item from the collection by its index
        ///     and adds the appropriate data to the new object.
        /// </summary>
        /// <param name="index">Numeric index</param>
        /// <returns>Item</returns>
        public new virtual TItem GetItemByIndex(int index) => (TItem) base.GetItemByIndex(index);

        /// <summary>
        ///     Indexer reference to an item in the collection
        /// </summary>
        public new virtual TItem this[int index] => (TItem) base.GetItemByIndex(index);

        /// <summary>
        ///     This method instantiated the appropriate item collection object
        ///     It can be overwritten in subclasses
        /// </summary>
        /// <returns>Collection item object</returns>
        public override IEntitySubItemCollectionItem GetItemObject()
        {
            //TODO: CL on 3/17/2008 - The SetParentCollection method isn't part of the interface,
            // so we must cast it to the concrete type. This should be improved.

            var item = new TItem();
            if (item is EntitySubItemCollectionItem item2) item2.SetParentCollection(this);
            return item;
        }

        /// <summary>
        ///     Object configuration
        ///     We have to implement this here, as it is marked as abstract in the parent class.
        /// </summary>
        protected override void Configure()
        {
            // Nothing to do here, since we use the constructor to configure the object.
            // But if they wanted, people can still override this method in subclasses.
        }

        /// <summary>
        ///     Retrieves an item from the collection by its index
        /// </summary>
        /// <param name="index">Numeric index</param>
        /// <param name="absoluteTableIndex">If true, the index is based on the data table, not the (potentially) sorted view</param>
        /// <returns>Item</returns>
        public new virtual TItem GetItemByIndex(int index, bool absoluteTableIndex) => (TItem) base.GetItemByIndex(index, absoluteTableIndex);

        /// <summary>
        ///     Adds a new row to the internal data table, and returns a
        ///     sub-item entity that links to it.
        /// </summary>
        /// <returns>Entity Sub Item Collection Item</returns>
        protected new virtual TItem AddNewRow() => (TItem) base.AddNewRow();
    }
}