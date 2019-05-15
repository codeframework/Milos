using System;
using System.Data;

namespace Milos.BusinessObjects.Generic
{
    /// <summary>
    ///     This class is used as a base class for a number of different item collections
    ///     that may reside in a BusinessEntity
    /// </summary>
    /// <typeparam name="TItem">The type of the item.</typeparam>
    public class GenericEntitySubItemCollection<TItem> : EntitySubItemCollection, IGenericEntitySubItemCollection<TItem> where TItem : IEntitySubItemCollectionItem
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="parentEntity">Parent Entity</param>
        /// <param name="primaryKeyField">The primary key field name. (Example: pk_LineItems)</param>
        /// <param name="foreignKeyField">The foreign key field name. (Example: fk_Invoice)</param>
        /// <param name="parentTableName">Name of the parent table. (Example: Invoice)</param>
        /// <param name="parentTablePrimaryKeyField">The parent table primary key field name. (Example: pk_Invoice)</param>
        /// <param name="table">The table represented by the collection. (Example: LineItems table)</param>
        /// <remarks>
        ///     These types of collections are used to create 1:n collections, such as
        ///     the line items of an invoice
        /// </remarks>
        public GenericEntitySubItemCollection(IBusinessEntity parentEntity, string primaryKeyField, string foreignKeyField, string parentTableName, string parentTablePrimaryKeyField, DataTable table) : base(parentEntity)
        {
            PrimaryKeyField = primaryKeyField;
            ForeignKeyField = foreignKeyField;
            ParentTableName = parentTableName;
            ParentTablePrimaryKeyField = parentTablePrimaryKeyField;
            SetTable(table);
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="parentEntity">Parent Entity</param>
        /// <param name="primaryKeyField">The primary key field name. (Example: pk_LineItems)</param>
        /// <param name="foreignKeyField">The foreign key field name. (Example: fk_Invoice)</param>
        /// <param name="parentTableName">Name of the parent table. (Example: Invoice)</param>
        /// <param name="parentTablePrimaryKeyField">The parent table primary key field name. (Example: pk_Invoice)</param>
        /// <remarks>
        ///     These types of collections are used to create 1:n collections, such as
        ///     the line items of an invoice
        /// </remarks>
        public GenericEntitySubItemCollection(IBusinessEntity parentEntity, string primaryKeyField, string foreignKeyField, string parentTableName, string parentTablePrimaryKeyField) : base(parentEntity)
        {
            PrimaryKeyField = primaryKeyField;
            ForeignKeyField = foreignKeyField;
            ParentTableName = parentTableName;
            ParentTablePrimaryKeyField = parentTablePrimaryKeyField;
        }

        /// <summary>
        ///     Adds a new record to the internal DataSet.
        /// </summary>
        /// <returns>New item</returns>
        public new virtual TItem Add() => (TItem) base.AddNewRow();

        /// <summary>
        ///     Indexer reference to an item in the collection
        /// </summary>
        public new virtual TItem this[int index] => (TItem) base.GetItemByIndex(index);

        /// <summary>
        ///     Retrieves an item from the collection by its index
        /// </summary>
        /// <param name="index">Numeric index</param>
        /// <returns>Item</returns>
        public new virtual TItem GetItemByIndex(int index) => (TItem) base.GetItemByIndex(index, false);

        /// <summary>
        ///     This method instantiated the appropriate item collection object
        ///     It can be overwritten in subclasses
        /// </summary>
        /// <returns>Collection item object</returns>
        public override IEntitySubItemCollectionItem GetItemObject() => (TItem) Activator.CreateInstance(typeof(TItem), this);

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

        /// <summary>
        ///     Method used to configure this object.
        /// </summary>
        protected override void Configure()
        {
            // Nothing to do here, since we use the constructor to configure the object.
            // But if they wanted, people can still override this method in subclasses.
        }
    }
}