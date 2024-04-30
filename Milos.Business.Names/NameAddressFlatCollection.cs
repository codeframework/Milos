using System.Collections;
using System.ComponentModel;
using CODE.Framework.Fundamentals.Configuration;

namespace Milos.Business.Names;

/// <summary>
/// Summary description for NameAddressCollection.
/// </summary>
public class NameAddressFlatCollection : INameAddressCollection
{
    /// <summary>
    /// Constructor
    /// </summary>
    public NameAddressFlatCollection() { }

    /// <summary>
    /// Defines the internal default country setting.
    /// If the setting is empty, the default country is read from a config file.
    /// If there is no setting in a config file either, then we assume "US".
    /// Note: This is the country code.
    /// </summary>
    private static string _defaultCountry = string.Empty;

    /// <summary>
    /// Defines the default country for new addresses.
    /// The default country can be changed by either overriding the sDefaultCountry field in a subclass,
    /// or by setting the "DefaultCountry" setting in the application settings (app.config).
    /// </summary>
    protected static string DefaultCountry
    {
        get
        {
            if (string.IsNullOrEmpty(_defaultCountry))
            {
                // No setting yet. We need to load if from the config file
                _defaultCountry = ConfigurationSettings.Settings.IsSettingSupported("DefaultCountry") ? ConfigurationSettings.Settings["DefaultCountry"] : string.Empty;
                if (string.IsNullOrEmpty(_defaultCountry))
                    // We still haven't found the default country. All we can do now is go with a basic assumption
                    _defaultCountry = "US";
            }

            return _defaultCountry;
        }
    }

    /// <summary>
    /// Returns the enumerator for this object
    /// </summary>
    /// <returns>Enumerator</returns>
    public IEnumerator GetEnumerator() => new FlatAddressEnumerator(this);

    /// <summary>
    /// A single item representing the address entity
    /// </summary>
    protected NameAddressFlatEntity SingleItem { get; set; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="entity">Parent entity</param>
    /// <param name="table">Table containing the single address</param>
    public NameAddressFlatCollection(IBusinessEntity entity, DataTable table)
    {
        addressTable = table ?? throw new ArgumentNullException("table");
        ParentEntity = entity ?? throw new ArgumentNullException("entity");
        SingleItem = new NameAddressFlatEntity(this, addressTable.Rows[0]);
    }

    /// <summary>
    /// Sets the internal table
    /// </summary>
    /// <param name="table">Address table</param>
    public void SetTable(DataTable table) => addressTable = table;

    /// <summary>
    /// Sets the internal table
    /// </summary>
    /// <param name="table">Not supported</param>
    /// <param name="xlinkTargetTable">Not supported</param>
    public void SetTable(DataTable table, DataTable xlinkTargetTable) => throw new NotSupportedException("Not Supported.");

    /// <summary>
    /// Parent entity
    /// </summary>
    public IBusinessEntity ParentEntity { get; private set; }

    /// <summary>
    /// Sets the parent entity
    /// </summary>
    /// <param name="parentEntity">Parent entity</param>
    public void SetParentEntity(IBusinessEntity parentEntity) => ParentEntity = parentEntity;

    /// <summary>
    /// Removes an item 
    /// </summary>
    /// <param name="index">index</param>
    public void Remove(int index) => throw new NotSupportedException("Removing addresses not supported");

    /// <summary>
    /// For internal use only
    /// </summary>
    private DataTable addressTable;

    /// <summary>
    /// The indexer must be overridden to return the appropriate type
    /// </summary>
    INameAddressEntity INameAddressCollection.this[int index] => (INameAddressEntity) GetItemByIndex(0);

    /// <summary>
    /// The indexer must be overridden to return the appropriate type
    /// </summary>
    public IEntitySubItemCollectionItem this[int index] => (INameAddressEntity) GetItemByIndex(0);

    /// <summary>
    /// Returns the single item inside this collection
    /// </summary>
    /// <param name="index">Index (ignored)</param>
    /// <returns>Address item</returns>
    public IEntitySubItemCollectionItem GetItemByIndex(int index) => SingleItem;

    /// <summary>
    /// This is needed for the collection to generate and serve up new item instances
    /// </summary>
    /// <returns>Item object</returns>
    public IEntitySubItemCollectionItem GetItemObject() => new NameAddressFlatEntity(this, addressTable.Rows[0]);

    /// <summary>
    /// Adds a new address of a certain type
    /// </summary>
    /// <param name="type">Address type</param>
    public INameAddressEntity Add(AddressType type) => throw new NotSupportedException("Adding addresses not supported");

    /// <summary>
    /// Adds a new address of a certain type
    /// </summary>
    /// <param name="type">Address type (string)</param>
    INameAddressEntity INameAddressCollection.Add(string type) => throw new NotSupportedException("Adding addresses not supported");

    /// <summary>
    /// Adds a new address of a certain type
    /// </summary>
    /// <returns>Address type</returns>
    public IEntitySubItemCollectionItem Add() => throw new NotSupportedException("Adding addresses not supported");

    /// <summary>
    /// This method adds a new record to the x-link DataSet and links to the specified foreign key record
    /// </summary>
    /// <param name="targetItemId">Target item ID (such as the primary of a group when linking to a group record)</param>
    /// <returns>New item</returns>
    public IEntitySubItemCollectionItem Add(Guid targetItemId) => throw new NotSupportedException("Adding addresses not supported");

    /// <summary>
    /// This method adds a new record to the x-link DataSet and links to the specified foreign key record
    /// </summary>
    /// <param name="targetItemId">Target item ID (such as the primary of a group when linking to a group record)</param>
    /// <returns>New item</returns>
    public IEntitySubItemCollectionItem Add(int targetItemId) => throw new NotSupportedException("Adding addresses not supported");

    /// <summary>
    /// This method adds a new record to the x-link DataSet and links to the record 
    /// identified by it's descriptive text.
    /// The field used for this operation is defined in the strTargetTextField field.
    /// </summary>
    /// <param name="targetItemText">text used by the target table. For instance, if you want to link to a "People" category, "People" would be the text passed along here.</param>
    /// <returns>New item</returns>
    public IEntitySubItemCollectionItem Add(string targetItemText) => throw new NotSupportedException("Adding addresses not supported");

    /// <summary>
    /// Count (always 0)
    /// </summary>
    public int Count => 1;

    /// <summary>
    /// Adds the <see cref="T:System.ComponentModel.PropertyDescriptor"></see> to the indexes used for searching.
    /// </summary>
    /// <param name="property">The <see cref="T:System.ComponentModel.PropertyDescriptor"></see> to add to the indexes used for searching.</param>
    public void AddIndex(PropertyDescriptor property) => throw new Exception("The method or operation is not implemented.");

    /// <summary>
    /// Adds a new item to the list.
    /// </summary>
    /// <returns>The item added to the list.</returns>
    /// <exception cref="T:System.NotSupportedException"><see cref="P:System.ComponentModel.IBindingList.AllowNew"></see> is false. </exception>
    public object AddNew() => throw new Exception("The method or operation is not implemented.");

    /// <summary>
    /// Gets whether you can update items in the list.
    /// </summary>
    /// <value></value>
    /// <returns>true if you can update the items in the list; otherwise, false.</returns>
    public bool AllowEdit => throw new Exception("The method or operation is not implemented.");

    /// <summary>
    /// Gets whether you can add items to the list using <see cref="M:System.ComponentModel.IBindingList.AddNew"></see>.
    /// </summary>
    /// <value></value>
    /// <returns>true if you can add items to the list using <see cref="M:System.ComponentModel.IBindingList.AddNew"></see>; otherwise, false.</returns>
    public bool AllowNew => throw new Exception("The method or operation is not implemented.");

    /// <summary>
    /// Gets whether you can remove items from the list, using <see cref="M:System.Collections.IList.Remove(System.Object)"></see> or <see cref="M:System.Collections.IList.RemoveAt(System.Int32)"></see>.
    /// </summary>
    /// <value></value>
    /// <returns>true if you can remove items from the list; otherwise, false.</returns>
    public bool AllowRemove => throw new Exception("The method or operation is not implemented.");

    /// <summary>
    /// Sorts the list based on a <see cref="T:System.ComponentModel.PropertyDescriptor"></see> and a <see cref="T:System.ComponentModel.ListSortDirection"></see>.
    /// </summary>
    /// <param name="property">The <see cref="T:System.ComponentModel.PropertyDescriptor"></see> to sort by.</param>
    /// <param name="direction">One of the <see cref="T:System.ComponentModel.ListSortDirection"></see> values.</param>
    /// <exception cref="T:System.NotSupportedException"><see cref="P:System.ComponentModel.IBindingList.SupportsSorting"></see> is false. </exception>
    public void ApplySort(PropertyDescriptor property, ListSortDirection direction) => throw new Exception("The method or operation is not implemented.");

    /// <summary>
    /// Returns the index of the currentRow that has the given <see cref="T:System.ComponentModel.PropertyDescriptor"></see>.
    /// </summary>
    /// <param name="property">The <see cref="T:System.ComponentModel.PropertyDescriptor"></see> to search on.</param>
    /// <param name="key">The value of the property parameter to search for.</param>
    /// <returns>
    /// The index of the currentRow that has the given <see cref="T:System.ComponentModel.PropertyDescriptor"></see>.
    /// </returns>
    /// <exception cref="T:System.NotSupportedException"><see cref="P:System.ComponentModel.IBindingList.SupportsSearching"></see> is false. </exception>
    public int Find(PropertyDescriptor property, object key)
    {
        ListChanged?.Invoke(ListChanged, new ListChangedEventArgs(ListChangedType.Reset, 0, 0));
        throw new Exception("The method or operation is not implemented.");
    }

    /// <summary>
    /// Gets whether the items in the list are sorted.
    /// </summary>
    /// <value></value>
    /// <returns>true if <see cref="M:System.ComponentModel.IBindingList.ApplySort(System.ComponentModel.PropertyDescriptor,System.ComponentModel.ListSortDirection)"></see> has been called and <see cref="M:System.ComponentModel.IBindingList.RemoveSort"></see> has not been called; otherwise, false.</returns>
    /// <exception cref="T:System.NotSupportedException"><see cref="P:System.ComponentModel.IBindingList.SupportsSorting"></see> is false. </exception>
    public bool IsSorted => throw new Exception("The method or operation is not implemented.");

    /// <summary>
    /// Occurs when the list changes or an item in the list changes.
    /// </summary>
    public event ListChangedEventHandler ListChanged;

    /// <summary>
    /// Removes the <see cref="T:System.ComponentModel.PropertyDescriptor"></see> from the indexes used for searching.
    /// </summary>
    /// <param name="property">The <see cref="T:System.ComponentModel.PropertyDescriptor"></see> to remove from the indexes used for searching.</param>
    public void RemoveIndex(PropertyDescriptor property) => throw new Exception("The method or operation is not implemented.");

    /// <summary>
    /// Removes any sort applied using <see cref="M:System.ComponentModel.IBindingList.ApplySort(System.ComponentModel.PropertyDescriptor,System.ComponentModel.ListSortDirection)"></see>.
    /// </summary>
    /// <exception cref="T:System.NotSupportedException"><see cref="P:System.ComponentModel.IBindingList.SupportsSorting"></see> is false. </exception>
    public void RemoveSort() => throw new Exception("The method or operation is not implemented.");

    /// <summary>
    /// Gets the direction of the sort.
    /// </summary>
    /// <value></value>
    /// <returns>One of the <see cref="T:System.ComponentModel.ListSortDirection"></see> values.</returns>
    /// <exception cref="T:System.NotSupportedException"><see cref="P:System.ComponentModel.IBindingList.SupportsSorting"></see> is false. </exception>
    public ListSortDirection SortDirection => throw new Exception("The method or operation is not implemented.");

    /// <summary>
    /// Gets the <see cref="T:System.ComponentModel.PropertyDescriptor"></see> that is being used for sorting.
    /// </summary>
    /// <value></value>
    /// <returns>The <see cref="T:System.ComponentModel.PropertyDescriptor"></see> that is being used for sorting.</returns>
    /// <exception cref="T:System.NotSupportedException"><see cref="P:System.ComponentModel.IBindingList.SupportsSorting"></see> is false. </exception>
    public PropertyDescriptor SortProperty => throw new Exception("The method or operation is not implemented.");

    /// <summary>
    /// Gets whether a <see cref="E:System.ComponentModel.IBindingList.ListChanged"></see> event is raised when the list changes or an item in the list changes.
    /// </summary>
    /// <value></value>
    /// <returns>true if a <see cref="E:System.ComponentModel.IBindingList.ListChanged"></see> event is raised when the list changes or when an item changes; otherwise, false.</returns>
    public bool SupportsChangeNotification => throw new Exception("The method or operation is not implemented.");

    /// <summary>
    /// Gets whether the list supports searching using the <see cref="M:System.ComponentModel.IBindingList.Find(System.ComponentModel.PropertyDescriptor,System.Object)"></see> method.
    /// </summary>
    /// <value></value>
    /// <returns>true if the list supports searching using the <see cref="M:System.ComponentModel.IBindingList.Find(System.ComponentModel.PropertyDescriptor,System.Object)"></see> method; otherwise, false.</returns>
    public bool SupportsSearching => throw new Exception("The method or operation is not implemented.");

    /// <summary>
    /// Gets whether the list supports sorting.
    /// </summary>
    /// <value></value>
    /// <returns>true if the list supports sorting; otherwise, false.</returns>
    public bool SupportsSorting => throw new Exception("The method or operation is not implemented.");

    /// <summary>
    /// Adds an item to the <see cref="T:System.Collections.IList"></see>.
    /// </summary>
    /// <param name="value">The <see cref="T:System.Object"></see> to add to the <see cref="T:System.Collections.IList"></see>.</param>
    /// <returns>
    /// The position into which the new element was inserted.
    /// </returns>
    /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.IList"></see> is read-only.-or- The <see cref="T:System.Collections.IList"></see> has a fixed size. </exception>
    public int Add(object value) => throw new Exception("The method or operation is not implemented.");

    /// <summary>
    /// Removes all items from the <see cref="T:System.Collections.IList"></see>.
    /// </summary>
    /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.IList"></see> is read-only. </exception>
    public void Clear() => throw new Exception("The method or operation is not implemented.");

    /// <summary>
    /// Determines whether the <see cref="T:System.Collections.IList"></see> contains a specific value.
    /// </summary>
    /// <param name="value">The <see cref="T:System.Object"></see> to locate in the <see cref="T:System.Collections.IList"></see>.</param>
    /// <returns>
    /// true if the <see cref="T:System.Object"></see> is found in the <see cref="T:System.Collections.IList"></see>; otherwise, false.
    /// </returns>
    public bool Contains(object value) => throw new Exception("The method or operation is not implemented.");

    /// <summary>
    /// Determines the index of a specific item in the <see cref="T:System.Collections.IList"></see>.
    /// </summary>
    /// <param name="value">The <see cref="T:System.Object"></see> to locate in the <see cref="T:System.Collections.IList"></see>.</param>
    /// <returns>
    /// The index of value if found in the list; otherwise, -1.
    /// </returns>
    public int IndexOf(object value) => throw new Exception("The method or operation is not implemented.");

    /// <summary>
    /// Inserts an item to the <see cref="T:System.Collections.IList"></see> at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index at which value should be inserted.</param>
    /// <param name="value">The <see cref="T:System.Object"></see> to insert into the <see cref="T:System.Collections.IList"></see>.</param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">index is not a valid index in the <see cref="T:System.Collections.IList"></see>. </exception>
    /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.IList"></see> is read-only.-or- The <see cref="T:System.Collections.IList"></see> has a fixed size. </exception>
    /// <exception cref="T:System.NullReferenceException">value is null reference in the <see cref="T:System.Collections.IList"></see>.</exception>
    public void Insert(int index, object value) => throw new Exception("The method or operation is not implemented.");

    /// <summary>
    /// Gets a value indicating whether the <see cref="T:System.Collections.IList"></see> has a fixed size.
    /// </summary>
    /// <value></value>
    /// <returns>true if the <see cref="T:System.Collections.IList"></see> has a fixed size; otherwise, false.</returns>
    public bool IsFixedSize => throw new Exception("The method or operation is not implemented.");

    /// <summary>
    /// Gets a value indicating whether the <see cref="T:System.Collections.IList"></see> is read-only.
    /// </summary>
    /// <value></value>
    /// <returns>true if the <see cref="T:System.Collections.IList"></see> is read-only; otherwise, false.</returns>
    public bool IsReadOnly => throw new Exception("The method or operation is not implemented.");

    /// <summary>
    /// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.IList"></see>.
    /// </summary>
    /// <param name="value">The <see cref="T:System.Object"></see> to remove from the <see cref="T:System.Collections.IList"></see>.</param>
    /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.IList"></see> is read-only.-or- The <see cref="T:System.Collections.IList"></see> has a fixed size. </exception>
    public void Remove(object value) => throw new Exception("The method or operation is not implemented.");

    /// <summary>
    /// Removes the <see cref="T:System.Collections.IList"></see> item at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the item to remove.</param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">index is not a valid index in the <see cref="T:System.Collections.IList"></see>. </exception>
    /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.IList"></see> is read-only.-or- The <see cref="T:System.Collections.IList"></see> has a fixed size. </exception>
    public void RemoveAt(int index) => throw new Exception("The method or operation is not implemented.");

    /// <summary>
    /// Indexer
    /// </summary>
    /// <value></value>
    object IList.this[int index]
    {
        get => throw new Exception("The method or operation is not implemented.");
        set => throw new Exception("The method or operation is not implemented.");
    }

    /// <summary>
    /// Copies the elements of the <see cref="T:System.Collections.ICollection"></see> to an <see cref="T:System.Array"></see>, starting at a particular <see cref="T:System.Array"></see> index.
    /// </summary>
    /// <param name="array">The one-dimensional <see cref="T:System.Array"></see> that is the destination of the elements copied from <see cref="T:System.Collections.ICollection"></see>. The <see cref="T:System.Array"></see> must have zero-based indexing.</param>
    /// <param name="index">The zero-based index in array at which copying begins.</param>
    /// <exception cref="T:System.ArgumentNullException">array is null. </exception>
    /// <exception cref="T:System.ArgumentOutOfRangeException">index is less than zero. </exception>
    /// <exception cref="T:System.ArgumentException">array is multidimensional.-or- index is equal to or greater than the length of array.-or- The number of elements in the source <see cref="T:System.Collections.ICollection"></see> is greater than the available space from index to the end of the destination array. </exception>
    /// <exception cref="T:System.InvalidCastException">The type of the source <see cref="T:System.Collections.ICollection"></see> cannot be cast automatically to the type of the destination array. </exception>
    public void CopyTo(Array array, int index) => throw new Exception("The method or operation is not implemented.");

    /// <summary>
    /// Gets a value indicating whether access to the <see cref="T:System.Collections.ICollection"></see> is synchronized (thread safe).
    /// </summary>
    /// <value></value>
    /// <returns>true if access to the <see cref="T:System.Collections.ICollection"></see> is synchronized (thread safe); otherwise, false.</returns>
    public bool IsSynchronized => throw new Exception("The method or operation is not implemented.");

    /// <summary>
    /// Gets an object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection"></see>.
    /// </summary>
    /// <value></value>
    /// <returns>An object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection"></see>.</returns>
    public object SyncRoot => throw new Exception("The method or operation is not implemented.");
}

/// <summary>
/// Enumerator for the flat address collection
/// </summary>
/// <remarks>
/// Constructor
/// </remarks>
/// <param name="collection">Enumerated collection</param>
public class FlatAddressEnumerator(NameAddressFlatCollection collection) : IEnumerator
{

    /// <summary>
    /// For internal use only
    /// </summary>
    private readonly NameAddressFlatCollection parentCollection = collection;

    /// <summary>
    /// Internal item pointer
    /// </summary>
    private int position = -1;

    /// <summary>
    /// Moves the internal pointer to the next item
    /// </summary>
    /// <returns>True if we haven't reached the end of the collection.</returns>
    public bool MoveNext()
    {
        if (position != -1) return false;
        position++;
        return true;

    }

    /// <summary>
    /// Reset to first item
    /// </summary>
    public void Reset() => position = -1;

    /// <summary>
    /// Returns the current item in the collection
    /// </summary>
    object IEnumerator.Current => parentCollection.GetItemByIndex(0);

    /// <summary>
    /// Returns the current item in the collection in a strongly typed manner
    /// </summary>
    public NameAddressFlatEntity Current => (NameAddressFlatEntity) parentCollection[0];
}

/// <summary>
/// Sub-items for different addresses
/// </summary>
/// <remarks>
/// Constructor
/// </remarks>
/// <param name="collection">Parent collection</param>
/// <param name="row">Row the item is based on</param>
public class NameAddressFlatEntity(NameAddressFlatCollection collection, DataRow row) : INameAddressEntity
{

    /// <summary>
    /// Sets the current data currentRow
    /// </summary>
    /// <param name="row">Row</param>
    public void SetCurrentRow(DataRow row) => currentRow = row;

    /// <summary>
    /// Returns true if the field is null.
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <returns>True or false</returns>
    public bool IsFieldNull(string fieldName) => currentRow[fieldName] == DBNull.Value;

    /// <summary>
    /// For internal use only
    /// </summary>
    private DataRow currentRow = row;

    /// <summary>
    /// For internal use only
    /// </summary>
    private readonly NameAddressFlatCollection parentCollection = collection;

    /// <summary>
    /// ID (primary key)
    /// </summary>
    public string Id => parentCollection.ParentEntity.Id;

    /// <summary>
    /// Primary Key
    /// </summary>
    public Guid PK => parentCollection.ParentEntity.PK;

    /// <summary>
    /// Primary Key
    /// </summary>
    public string PKString => parentCollection.ParentEntity.PKString;

    /// <summary>
    /// Primary Key
    /// </summary>
    public int PKInteger => parentCollection.ParentEntity.PKInteger;

    /// <summary>
    /// Returns a field value
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <returns>Value</returns>
    [Obsolete("Use ReadFieldValue<T>() instead.")]
    public object GetFieldValue(string fieldName) => ((NameEntity) parentCollection.ParentEntity).GetFieldValueEx(fieldName, currentRow.Table.TableName);

    /// <summary>
    /// Returns a field value
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <returns>Value</returns>
    public T ReadFieldValue<T>(string fieldName)
    {
        var parentCollectionParentEntity = parentCollection.ParentEntity as NameBusinessEntity;
        return parentCollectionParentEntity == null ? default : parentCollectionParentEntity.ReadFieldValueEx<T>(fieldName, currentRow.Table.TableName);
    }

    /// <summary>
    /// Sets the field value
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <param name="value">Field value</param>
    public void WriteFieldValue<T>(string fieldName, T value) => (parentCollection.ParentEntity as NameBusinessEntity)?.WriteFieldValueEx(fieldName, value, currentRow.Table.TableName);

    /// <summary>
    /// Sets the field value
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <param name="value">Field value</param>
    [Obsolete("Use WriteFieldValue<T>() instead.")]
    public void SetFieldValue(string fieldName, object value) => ((NameEntity) parentCollection.ParentEntity).SetFieldValueEx(fieldName, value, currentRow.Table.TableName);

    /// <summary>
    /// Primary Key
    /// </summary>
    public string PrimaryKeyField
    {
        get => parentCollection.ParentEntity.GetType().GetProperty("PrimaryKeyField")?.GetValue(parentCollection.ParentEntity, null).ToString();
        set { } // Poor-man's read only property
    }

    /// <summary>
    /// Item state (changed, unchanged,...)
    /// </summary>
    public DataRowState ItemState => currentRow.RowState;

    /// <summary>
    /// Removes the current item
    /// </summary>
    public void Remove() => throw new NotSupportedException("Not Supported.");

    /// <summary>
    /// This method returns a well formatted address string, based on the country
    /// </summary>
    /// <param name="includeName">Specifies whether potential address name information shall be included in the output string</param>
    /// <returns>Well formatted address string</returns>
    public string GetFormattedAddress(bool includeName = false)
    {
        // We need the country object to perform this operation
        using var entCountry = NewCountryEntity();
        // Now, we can concatenate the appropriate string
        var address = string.Empty;
        if (includeName)
        {
            if (!string.IsNullOrEmpty(AddressCompany)) address += AddressCompany.Trim() + "\r\n";
            if (!string.IsNullOrEmpty(AddressName)) address += AddressName.Trim() + "\r\n";
        }

        address += Street.Trim() + "\r\n";
        if (!string.IsNullOrEmpty(Street2)) address += Street2.Trim() + "\r\n";
        if (!string.IsNullOrEmpty(Street3)) address += Street3.Trim() + "\r\n";

        switch (entCountry.AddressFormat)
        {
            case AddressFormat.CityStateZip:
                address += City.Trim() + ", " + State.Trim() + " " + Zip.Trim() + "\r\n";
                break;
            case AddressFormat.CityPostalCode:
                address += City.Trim() + ", " + Zip.Trim() + "\r\n";
                break;
            case AddressFormat.PostalCodeCity:
                address += Zip.Trim() + " " + City.Trim() + "\r\n";
                break;
            case AddressFormat.PostalCodeCityState:
                address += Zip.Trim() + " " + City.Trim() + ", " + State.Trim() + "\r\n";
                break;
        }

        address += entCountry.Name.Trim();

        return address;
    }

    /// <summary>
    /// This method returns a well formatted address string (HTML), based on the country
    /// </summary>
    /// <param name="includeName">Specifies whether potential address name information shall be included in the output string</param>
    /// <returns>Well formatted address string</returns>
    public string GetHtmlFormattedAddress(bool includeName = false)
    {
        // We need the country object to perform this operation
        using var entCountry = NewCountryEntity();
        // Now, we can concatenate the appropriate string
        var address = string.Empty;
        if (includeName)
        {
            if (!string.IsNullOrEmpty(AddressCompany)) address += "<b>" + AddressCompany.Trim() + "</b><br>";
            if (!string.IsNullOrEmpty(AddressName)) address += "<b>" + AddressName.Trim() + "</b><br>";
        }

        address += Street.Trim() + "<br>";
        if (!string.IsNullOrEmpty(Street2)) address += Street2.Trim() + "<br>";
        if (!string.IsNullOrEmpty(Street3)) address += Street3.Trim() + "<br>";

        switch (entCountry.AddressFormat)
        {
            case AddressFormat.CityStateZip:
                address += City.Trim() + ", " + State.Trim() + " " + Zip.Trim() + "<br>";
                break;
            case AddressFormat.CityPostalCode:
                address += City.Trim() + ", " + Zip.Trim() + "<br>";
                break;
            case AddressFormat.PostalCodeCity:
                address += Zip.Trim() + " " + City.Trim() + "<br>";
                break;
            case AddressFormat.PostalCodeCityState:
                address += Zip.Trim() + " " + City.Trim() + ", " + State.Trim() + "<br>";
                break;
        }

        address += entCountry.Name.Trim();

        return address;
    }

    /// <summary>
    /// Address primary key
    /// </summary>
    public Guid AddressId => ReadFieldValue<Guid>("pk_address");

    /// <summary>
    /// Street (Address 1)
    /// </summary>
    public virtual string Street
    {
        get => ReadFieldValue<string>("Street");
        set => WriteFieldValue("Street", value);
    }

    /// <summary>
    /// Street 2 (Address 2)
    /// </summary>
    public virtual string Street2
    {
        get => ReadFieldValue<string>("Street2");
        set => WriteFieldValue("Street2", value);
    }

    /// <summary>
    /// Street 3 (Address 3)
    /// </summary>
    public virtual string Street3
    {
        get => ReadFieldValue<string>("Street3");
        set => WriteFieldValue("Street3", value);
    }

    /// <summary>
    /// City
    /// </summary>
    public virtual string City
    {
        get => ReadFieldValue<string>("City");
        set => WriteFieldValue("City", value);
    }

    /// <summary>
    /// State (where applicable)
    /// Usually a state code, although for non-US countries, this may be a full name.
    /// </summary>
    public virtual string State
    {
        get => ReadFieldValue<string>("State");
        set => WriteFieldValue("State", value);
    }

    /// <summary>
    /// ZIP or Postal Code (where applicable)
    /// </summary>
    public virtual string Zip
    {
        get => ReadFieldValue<string>("Zip");
        set => WriteFieldValue("Zip", value);
    }

    /// <summary>
    /// Foreign key for assigned country
    /// </summary>
    public virtual Guid CountryID
    {
        get => ReadFieldValue<Guid>("fk_country");
        set => WriteFieldValue("fk_country", value);
    }

    /// <summary>
    /// Address name (optional)
    /// This can be used whenever this particular address uses a different name.
    /// </summary>
    public virtual string AddressName
    {
        get => ReadFieldValue<string>("AddressName");
        set => WriteFieldValue("AddressName", value);
    }

    /// <summary>
    /// Address company name (optional)
    /// This can be used whenever this particular address uses a different company name.
    /// </summary>
    public virtual string AddressCompany
    {
        get => ReadFieldValue<string>("AddressCompany");
        set => WriteFieldValue("AddressCompany", value);
    }

    /// <summary>
    /// Instantiates and returns a new country entity
    /// based on the country assigned to this entity.
    /// </summary>
    public virtual CountryBusinessEntity NewCountryEntity() => new CountryBusinessEntity(CountryID);

    /// <summary>
    /// Gets or sets the address type (strongly typed)
    /// Note that the values available through this setting are limited to the values
    /// available in the AddressType enum. Unknown underlying values are exposed as "Other".
    /// Note: Use the regular "Type" property to get access to all string values.
    /// </summary>
    public virtual AddressType TypeStrong
    {
        get
        {
            try
            {
                return (AddressType) Enum.Parse(typeof(AddressType), ReadFieldValue<string>("Type"), true);
            }
            catch
            {
                return AddressType.Other;
            }
        }
        set => WriteFieldValue("Type", value.ToString());
    }

    /// <summary>
    /// Gets or sets the address type.
    /// Note that this property allows direct access to the underlying string value.
    /// The strings to not have to match the AddressType enum. Strings that do not 
    /// match that enum will be exposed as "Other" through the "TypeStrong" property.
    /// </summary>
    public virtual string Type
    {
        get => ReadFieldValue<string>("Type");
        set => WriteFieldValue("Type", value);
    }
}