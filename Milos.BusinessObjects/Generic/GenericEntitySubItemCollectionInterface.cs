using System;
using System.ComponentModel;
using System.Data;

namespace Milos.BusinessObjects.Generic
{
    /// <summary>
    ///     This interface defines the most fundamental IEntitySubItemCollection interface
    /// </summary>
    /// <typeparam name="TItem">The type of the item stored in the collection.</typeparam>
    public interface IGenericEntitySubItemCollection<out TItem> : IBindingList where TItem : IEntitySubItemCollectionItem
    {
        /// <summary>
        ///     Reference to the parent entity object
        /// </summary>
        IBusinessEntity ParentEntity { get; }

        /// <summary>
        ///     Indexer reference to an item in the collection
        /// </summary>
        new TItem this[int index] { get; }

        /// <summary>
        ///     Assigns a default table to the collection.
        /// </summary>
        /// <param name="table">DataTable that represents the encapsulated data for this collection.</param>
        void SetTable(DataTable table);

        /// <summary>
        ///     This method sets the parent entity of this collection
        /// </summary>
        /// <param name="parentEntity">Parent entity (usually Me/this)</param>
        void SetParentEntity(IBusinessEntity parentEntity);

        /// <summary>
        ///     Adds a new record to the internal DataSet.
        /// </summary>
        TItem Add();

        /// <summary>
        ///     Removed an item from the collection
        /// </summary>
        /// <param name="index">Numeric index of the item that is to be removed</param>
        void Remove(int index);

        /// <summary>
        ///     Retrieves an item from the collection by its index
        /// </summary>
        /// <param name="index">Numeric index</param>
        /// <returns>Item</returns>
        TItem GetItemByIndex(int index);

        /// <summary>
        ///     This method instantiated the appropriate item collection object
        ///     It can be overwritten in subclasses
        /// </summary>
        /// <returns>Collection item object</returns>
        IEntitySubItemCollectionItem GetItemObject();
    }

    /// <summary>
    ///     Basic entity sub item collection interface
    /// </summary>
    /// <typeparam name="TItem">The type of the x-link item stored in the collection.</typeparam>
    public interface IGenericEntityXlinkSubItemCollection<out TItem> : IGenericEntitySubItemCollection<TItem> where TItem : IEntitySubItemCollectionXLinkItem
    {
        /// <summary>
        ///     Sets internally used data tables. This is usually done on or immediately after instantiation.
        /// </summary>
        /// <param name="table">Cross-Link Table between the main entity and the related table.</param>
        /// <param name="xlinkTargetTable">Table that holds the actual data.</param>
        void SetTable(DataTable table, DataTable xlinkTargetTable);

        /// <summary>
        ///     This method adds a new record to the x-link DataSet and links to the specified foreign key record
        /// </summary>
        /// <param name="targetItemId">Target item ID (such as the primary of a group when linking to a group record)</param>
        /// <returns>New item</returns>
        TItem Add(Guid targetItemId);

        /// <summary>
        ///     This method adds a new record to the x-link DataSet and links to the specified foreign key record
        /// </summary>
        /// <param name="targetItemId">Target item ID (such as the primary of a group when linking to a group record)</param>
        /// <returns>New item</returns>
        TItem Add(int targetItemId);

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
        TItem Add(string targetItemText);
    }
}