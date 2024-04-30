namespace Milos.BusinessObjects;

/// <summary>
/// This interface defines the most fundamental IEntitySubItemCollection interface
/// </summary>
public interface IEntitySubItemCollection : IBindingList
{
    /// <summary>
    /// Reference to the parent entity object
    /// </summary>
    IBusinessEntity ParentEntity { get; }

    /// <summary>
    /// Indexer reference to an item in the collection
    /// </summary>
    new IEntitySubItemCollectionItem this[int index] { get; }

    /// <summary>
    /// Retrieves an item from the collection by its index
    /// </summary>
    /// <param name="index">Numeric index</param>
    /// <returns>Item</returns>
    IEntitySubItemCollectionItem GetItemByIndex(int index);

    /// <summary>
    /// This method instantiated the appropriate item collection object
    /// It can be overwritten in subclasses
    /// </summary>
    /// <returns>Collection item object</returns>
    IEntitySubItemCollectionItem GetItemObject();

    /// <summary>
    /// Assigns a default table to the collection.
    /// </summary>
    /// <param name="table">DataTable that represents the encapsulated data for this collection.</param>
    void SetTable(DataTable table);

    /// <summary>
    /// This method sets the parent entity of this collection
    /// </summary>
    /// <param name="parentBusinessEntity">Parent entity (usually Me/this)</param>
    void SetParentEntity(IBusinessEntity parentBusinessEntity);

    /// <summary>
    /// Adds a new record to the internal DataSet.
    /// </summary>
    IEntitySubItemCollectionItem Add();

    /// <summary>
    /// Removed an item from the collection
    /// </summary>
    /// <param name="index">Numeric index of the item that is to be removed</param>
    void Remove(int index);
}