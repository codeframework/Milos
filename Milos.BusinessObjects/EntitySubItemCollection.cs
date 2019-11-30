using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using CODE.Framework.Fundamentals.Utilities;
using Milos.Data;

namespace Milos.BusinessObjects
{
    /// <summary>This class is used as a base class for a number of different item collections that may reside in a BusinessEntity.</summary>
    public abstract class EntitySubItemCollection : IEntitySubItemCollection, IFilterable, ISortable
    {
        private BrokenRulesCollection brokenRules;
        private string filter = string.Empty;
        private string filterMaster = string.Empty;
        private string internalDataTableName;
        private ListChangedEventHandler listChangedEvent;
        private IBusinessEntity parentEntity;
        private string sortBy = string.Empty;
        private string sortByMaster = string.Empty;

        /// <summary>Initializes a new instance of the <see cref="EntitySubItemCollection" /> class.</summary>
        public EntitySubItemCollection() { }

        /// <summary>Constructor</summary>
        /// <param name="parentEntity">The parent entity.</param>
        protected EntitySubItemCollection(IBusinessEntity parentEntity)
        {
            SetParentEntity(parentEntity);
            Configure();
        }

        /// <summary>For internal use only</summary>
        /// <remarks>Indicates whether the raw data table is used for this collection, or whether a different approach (data view) is needed.</remarks>
        protected bool IsRawTable => SpecialView == null;

        /// <summary>Data load state of the entity</summary>
        [NotReportSerializable]
        [NotClonable]
        public EntityLoadState LoadState { get; private set; } = EntityLoadState.Loading;

        /// <summary>Broken business rules collection (specific to this collection).</summary>
        [NotReportSerializable]
        [NotClonable]
        public BrokenRulesCollection BrokenRules => brokenRules ?? (brokenRules = new BrokenRulesCollection(this));

        /// <summary>Primary key type used by this entity.</summary>
        protected internal virtual KeyType PrimaryKeyType => ParentEntity.AssociatedBusinessObject is BusinessObject biz2 ? biz2.GetPrimaryKeyType(InternalDataTable.TableName) : ParentEntity.PrimaryKeyType;

        /// <summary>Number of items in the collection/.</summary>
        public virtual int Count => GetItemCount(false);

        /// <summary>Primary key field for current entity (such as the key in the line items table if this is part of an order entity).</summary>
        protected internal string PrimaryKeyField { get; set; } = string.Empty;

        /// <summary>Name of the field that links us to the parent table.</summary>
        protected string ForeignKeyField { get; set; } = string.Empty;

        /// <summary>Name of the parent table (such as orders if this is a line item entity).</summary>
        protected string ParentTableName { get; set; } = string.Empty;

        /// <summary>Primary key field used by the parent table (such as "order_pk").</summary>
        protected string ParentTablePrimaryKeyField { get; set; } = string.Empty;

        /// <summary>Internal data table.</summary>
        public DataTable InternalDataTable { get; private set; }

        /// <summary>Internal data view that may be used instead of a raw data table.</summary>
        protected DataRow[] SpecialView { get; set; }

        /// <summary>Sort expression</summary>
        /// <example>FirstName, LastName DESC, Company</example>
        /// <remarks>
        /// The sort expression is a comma-separated list of fields that make up the sort order.
        /// For descending sorting, add DESC after the field name (separated by a space).
        /// The field names can be the names of the property on the object, or the names
        /// of the fields as provided by the database.
        /// Note: To use property names, maps must be provided.
        /// </remarks>
        public string SortBy
        {
            get => sortBy;
            set
            {
                sortBy = MassageSortExpression(value);
                UpdateSpecialDataView();
            }
        }

        /// <summary>Master sort expression.</summary>
        /// <remarks>
        /// Sortable objects are first sorted by the master expression,
        /// and then by the sort-by expression
        /// The sort expression is a comma-separated list of fields that make up the sort order.
        /// For descending sorting, add DESC after the field name (separated by a space).
        /// The field names can be the names of the property on the object, or the names
        /// of the fields as provided by the database.
        /// Note: To use property names, maps must be provided.
        /// </remarks>
        /// <example>Company</example>
        public string SortByMaster
        {
            get => sortByMaster;
            set
            {
                sortByMaster = MassageSortExpression(value);
                UpdateSpecialDataView();
            }
        }

        /// <summary>Complete sort expression.</summary>
        /// <remarks>This is a combination of the master sort expression and the sort-by expression.</remarks>
        /// <example>Company, FirstName, LastName</example>
        [NotReportSerializable]
        [NotClonable]
        public string CompleteSortExpression
        {
            get
            {
                var retVal = string.Empty;
                if (SortByMaster.Length > 0)
                    retVal = SortByMaster;
                if (SortBy.Length > 0)
                {
                    if (retVal.Length > 0)
                        retVal += ", ";
                    retVal += SortBy;
                }

                return retVal;
            }
        }

        /// <summary>Assigns a default table to the collection.</summary>
        /// <param name="table">DataTable that represents the encapsulated data for this collection.</param>
        public void SetTable(DataTable table)
        {
            InternalDataTable = table;
            internalDataTableName = table.TableName;
            LoadState = EntityLoadState.LoadComplete;
        }

        /// <summary>This method sets the parent entity of this collection.</summary>
        /// <param name="parentBusinessEntity">Parent entity (usually Me/this)</param>
        public void SetParentEntity(IBusinessEntity parentBusinessEntity) => parentEntity = parentBusinessEntity;

        /// <summary>Adds a new record to the internal DataSet.</summary>
        /// <returns>New item</returns>
        public virtual IEntitySubItemCollectionItem Add() => AddNewRow();

        /// <summary>Removed an item from the collection.</summary>
        /// <param name="index">Numeric index of the item that is to be removed</param>
        public virtual void Remove(int index) => this[index].Remove();

        /// <summary>Reference to the parent entity object.</summary>
        public IBusinessEntity ParentEntity
        {
            [DebuggerStepThrough]
            get
            {
                if (parentEntity == null)
                {
                    // This is a real problem!
                    // TODO: EPS.QA.Asserts.Alert(Properties.Resources.NoParentEntity);
                }

                return parentEntity;
            }
        }

        /// <summary>Indexer reference to an item in the collection.</summary>
        public virtual IEntitySubItemCollectionItem this[int index] => GetItemByIndex(index);

        /// <summary>Indexer reference to an item in the collection.</summary>
        public virtual IEntitySubItemCollectionItem this[Guid key] => GetItemByKey(key);

        /// <summary>
        ///     This method instantiated the appropriate item collection object
        ///     It can be overwritten in subclasses
        /// </summary>
        /// <returns>Collection item object</returns>
        public virtual IEntitySubItemCollectionItem GetItemObject() => null;

        /// <summary>
        ///     Implementation of IEnumerable, in particular GetEnumerator()
        /// </summary>
        /// <returns>Entity Item Enumerator</returns>
        public IEnumerator GetEnumerator()
        {
            for (var count = 0; count < Count; count++)
                yield return this[count];
        }

        /// <summary>
        ///     Adds the <see cref="T:System.ComponentModel.PropertyDescriptor" /> to the indexes used for searching.
        /// </summary>
        /// <param name="property">
        ///     The <see cref="T:System.ComponentModel.PropertyDescriptor" /> to add to the indexes used for
        ///     searching.
        /// </param>
        void IBindingList.AddIndex(PropertyDescriptor property)
        {
            // Not supported
        }

        /// <summary>
        ///     Adds a new item to the list.
        /// </summary>
        /// <returns>The item added to the list.</returns>
        /// <exception cref="T:System.NotSupportedException">
        ///     <see cref="P:System.ComponentModel.IBindingList.AllowNew" /> is false.
        /// </exception>
        object IBindingList.AddNew() => Add();

        /// <summary>
        ///     Gets whether you can update items in the list.
        /// </summary>
        /// <value></value>
        /// <returns>true if you can update the items in the list; otherwise, false.</returns>
        bool IBindingList.AllowEdit => true;

        /// <summary>
        ///     Gets whether you can add items to the list using <see cref="M:System.ComponentModel.IBindingList.AddNew" />.
        /// </summary>
        /// <value></value>
        /// <returns>
        ///     true if you can add items to the list using <see cref="M:System.ComponentModel.IBindingList.AddNew" />;
        ///     otherwise, false.
        /// </returns>
        bool IBindingList.AllowNew => true;

        /// <summary>
        ///     Gets whether you can remove items from the list, using
        ///     <see cref="M:System.Collections.IList.Remove(System.Object)" /> or
        ///     <see cref="M:System.Collections.IList.RemoveAt(System.Int32)" />.
        /// </summary>
        /// <value></value>
        /// <returns>true if you can remove items from the list; otherwise, false.</returns>
        bool IBindingList.AllowRemove => true;

        /// <summary>
        ///     Sorts the list based on a <see cref="T:System.ComponentModel.PropertyDescriptor" /> and a
        ///     <see cref="T:System.ComponentModel.ListSortDirection" />.
        /// </summary>
        /// <param name="property">The <see cref="T:System.ComponentModel.PropertyDescriptor" /> to sort by.</param>
        /// <param name="direction">One of the <see cref="T:System.ComponentModel.ListSortDirection" /> values.</param>
        /// <exception cref="T:System.NotSupportedException">
        ///     <see cref="P:System.ComponentModel.IBindingList.SupportsSorting" /> is false.
        /// </exception>
        void IBindingList.ApplySort(PropertyDescriptor property, ListSortDirection direction)
        {
            if (!(ParentEntity is BusinessEntity entity)) return;
            var propertyName = property.Name;
            var fieldName = entity.GetInternalFieldName(propertyName, InternalDataTable.TableName);
            if (string.IsNullOrEmpty(fieldName))
                fieldName = propertyName;
            if (InternalDataTable.Columns.Contains(fieldName))
            {
                var sortExpression = fieldName;
                if (direction == ListSortDirection.Descending)
                    sortExpression += " DESC";
                SortBy = sortExpression;
            }
            else
                ((IBindingList) this).RemoveSort();
        }

        /// <summary>
        ///     Returns the index of the row that has the given <see cref="T:System.ComponentModel.PropertyDescriptor" />.
        /// </summary>
        /// <param name="property">The <see cref="T:System.ComponentModel.PropertyDescriptor" /> to search on.</param>
        /// <param name="key">The value of the <paramref name="property" /> parameter to search for.</param>
        /// <returns>
        ///     The index of the row that has the given <see cref="T:System.ComponentModel.PropertyDescriptor" />.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">
        ///     <see cref="P:System.ComponentModel.IBindingList.SupportsSearching" /> is false.
        /// </exception>
        int IBindingList.Find(PropertyDescriptor property, object key) => -1;

        /// <summary>
        ///     Gets whether the items in the list are sorted.
        /// </summary>
        /// <value></value>
        /// <returns>
        ///     true if
        ///     <see
        ///         cref="M:System.ComponentModel.IBindingList.ApplySort(System.ComponentModel.PropertyDescriptor,System.ComponentModel.ListSortDirection)" />
        ///     has been called and <see cref="M:System.ComponentModel.IBindingList.RemoveSort" /> has not been called; otherwise,
        ///     false.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">
        ///     <see cref="P:System.ComponentModel.IBindingList.SupportsSorting" /> is false.
        /// </exception>
        bool IBindingList.IsSorted => !string.IsNullOrEmpty(sortBy);

        /// <summary>
        ///     Occurs when the list changes or an item in the list changes.
        /// </summary>
        event ListChangedEventHandler IBindingList.ListChanged
        {
            add
            {
                lock (this) listChangedEvent += value;
            }
            remove
            {
                lock (this) listChangedEvent -= value;
            }
        }

        /// <summary>
        ///     Removes the <see cref="T:System.ComponentModel.PropertyDescriptor" /> from the indexes used for searching.
        /// </summary>
        /// <param name="property">
        ///     The <see cref="T:System.ComponentModel.PropertyDescriptor" /> to remove from the indexes used
        ///     for searching.
        /// </param>
        void IBindingList.RemoveIndex(PropertyDescriptor property)
        {
            // Not supported
        }

        /// <summary>
        ///     Removes any sort applied using
        ///     <see
        ///         cref="M:System.ComponentModel.IBindingList.ApplySort(System.ComponentModel.PropertyDescriptor,System.ComponentModel.ListSortDirection)" />
        ///     .
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">
        ///     <see cref="P:System.ComponentModel.IBindingList.SupportsSorting" /> is false.
        /// </exception>
        void IBindingList.RemoveSort() => SortBy = string.Empty;

        /// <summary>
        ///     Gets the direction of the sort.
        /// </summary>
        /// <value></value>
        /// <returns>One of the <see cref="T:System.ComponentModel.ListSortDirection" /> values.</returns>
        /// <exception cref="T:System.NotSupportedException">
        ///     <see cref="P:System.ComponentModel.IBindingList.SupportsSorting" /> is false.
        /// </exception>
        ListSortDirection IBindingList.SortDirection
        {
            get
            {
                if (string.IsNullOrEmpty(sortBy))
                    // We are not sorted, so it doesn't really matter
                    return ListSortDirection.Ascending;

                var sortExpressions = sortBy.Split(',');
                if (sortExpressions.Length > 0)
                {
                    var firstExpression = sortExpressions[0].Trim().ToLower(CultureInfo.InvariantCulture);
                    return firstExpression.EndsWith(" desc") ? ListSortDirection.Descending : ListSortDirection.Ascending;
                }

                return ListSortDirection.Ascending;
            }
        }

        /// <summary>
        ///     Gets the <see cref="T:System.ComponentModel.PropertyDescriptor" /> that is being used for sorting.
        /// </summary>
        /// <value></value>
        /// <returns>The <see cref="T:System.ComponentModel.PropertyDescriptor" /> that is being used for sorting.</returns>
        /// <exception cref="T:System.NotSupportedException">
        ///     <see cref="P:System.ComponentModel.IBindingList.SupportsSorting" /> is false.
        /// </exception>
        PropertyDescriptor IBindingList.SortProperty
        {
            get
            {
                if (string.IsNullOrEmpty(SortBy)) return null;
                var sortProperties = SortBy.Split(',');
                if (sortProperties.Length > 0)
                {
                    var expressionParts = sortProperties[0].Split(' ');
                    return new EntityPropertyDescriptor(expressionParts[0].Trim(), this);
                }

                return null;
            }
        }

        /// <summary>
        ///     Gets whether a <see cref="E:System.ComponentModel.IBindingList.ListChanged" /> event is raised when the list
        ///     changes or an item in the list changes.
        /// </summary>
        /// <value></value>
        /// <returns>
        ///     true if a <see cref="E:System.ComponentModel.IBindingList.ListChanged" /> event is raised when the list
        ///     changes or when an item changes; otherwise, false.
        /// </returns>
        bool IBindingList.SupportsChangeNotification => true;

        /// <summary>
        ///     Gets whether the list supports searching using the
        ///     <see cref="M:System.ComponentModel.IBindingList.Find(System.ComponentModel.PropertyDescriptor,System.Object)" />
        ///     method.
        /// </summary>
        /// <value></value>
        /// <returns>
        ///     true if the list supports searching using the
        ///     <see cref="M:System.ComponentModel.IBindingList.Find(System.ComponentModel.PropertyDescriptor,System.Object)" />
        ///     method; otherwise, false.
        /// </returns>
        bool IBindingList.SupportsSearching => false;

        /// <summary>
        ///     Gets whether the list supports sorting.
        /// </summary>
        /// <value></value>
        /// <returns>true if the list supports sorting; otherwise, false.</returns>
        bool IBindingList.SupportsSorting => true;

        /// <summary>
        ///     Adds an item to the <see cref="T:System.Collections.IList" />.
        /// </summary>
        /// <param name="value">The <see cref="T:System.Object" /> to add to the <see cref="T:System.Collections.IList" />.</param>
        /// <returns>
        ///     The position into which the new element was inserted.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">
        ///     The <see cref="T:System.Collections.IList" /> is read-only.-or- The
        ///     <see cref="T:System.Collections.IList" /> has a fixed size.
        /// </exception>
        int IList.Add(object value)
        {
            throw new OperationNotSupportedByEntityException();
        }

        /// <summary>
        ///     Removes all items from the <see cref="T:System.Collections.IList" />.
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.IList" /> is read-only. </exception>
        void IList.Clear()
        {
            while (Count > 0)
                Remove(0);
        }

        /// <summary>
        ///     Determines whether the <see cref="T:System.Collections.IList" /> contains a specific value.
        /// </summary>
        /// <param name="value">The <see cref="T:System.Object" /> to locate in the <see cref="T:System.Collections.IList" />.</param>
        /// <returns>
        ///     true if the <see cref="T:System.Object" /> is found in the <see cref="T:System.Collections.IList" />; otherwise,
        ///     false.
        /// </returns>
        bool IList.Contains(object value)
        {
            if (!(value is IEntitySubItemCollectionItem item))
                throw new InvalidObjectTypeInEntityException("Parameter is not a sub item collection item.");
            foreach (IEntitySubItemCollectionItem existingItem in this)
                switch (PrimaryKeyType)
                {
                    case KeyType.Guid:
                        if (existingItem.PK == item.PK)
                            return true;
                        break;
                    case KeyType.Integer:
                    case KeyType.IntegerAutoIncrement:
                        if (existingItem.PKInteger == item.PKInteger)
                            return true;
                        break;
                    case KeyType.String:
                        if (StringHelper.Compare(existingItem.PKString, item.PKString, false))
                            return true;
                        break;
                }

            return false;
        }

        /// <summary>
        ///     Determines the index of a specific item in the <see cref="T:System.Collections.IList" />.
        /// </summary>
        /// <param name="value">The <see cref="T:System.Object" /> to locate in the <see cref="T:System.Collections.IList" />.</param>
        /// <returns>
        ///     The index of <paramref name="value" /> if found in the list; otherwise, -1.
        /// </returns>
        int IList.IndexOf(object value)
        {
            var index = -1;
            var item = value as IEntitySubItemCollectionItem;
            if (item == null)
                throw new InvalidObjectTypeInEntityException("Parameter is not a sub item collection item.");
            foreach (IEntitySubItemCollectionItem existingItem in this)
            {
                index++;
                switch (PrimaryKeyType)
                {
                    case KeyType.Guid:
                        if (existingItem.PK == item.PK)
                            return index;
                        break;
                    case KeyType.Integer:
                    case KeyType.IntegerAutoIncrement:
                        if (existingItem.PKInteger == item.PKInteger)
                            return index;
                        break;
                    case KeyType.String:
                        if (StringHelper.Compare(existingItem.PKString, item.PKString, false))
                            return index;
                        break;
                }
            }

            return -1;
        }

        /// <summary>
        ///     Inserts an item to the <see cref="T:System.Collections.IList" /> at the specified index.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value">The <see cref="T:System.Object" /> to insert into the <see cref="T:System.Collections.IList" />.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///     <paramref name="index" /> is not a valid index in the <see cref="T:System.Collections.IList" />.
        /// </exception>
        /// <exception cref="T:System.NotSupportedException">
        ///     The <see cref="T:System.Collections.IList" /> is read-only.-or- The
        ///     <see cref="T:System.Collections.IList" /> has a fixed size.
        /// </exception>
        /// <exception cref="T:System.NullReferenceException">
        ///     <paramref name="value" /> is null reference in the <see cref="T:System.Collections.IList" />.
        /// </exception>
        void IList.Insert(int index, object value)
        {
            throw new OperationNotSupportedByEntityException();
        }

        /// <summary>
        ///     Gets a value indicating whether the <see cref="T:System.Collections.IList" /> has a fixed size.
        /// </summary>
        /// <value></value>
        /// <returns>true if the <see cref="T:System.Collections.IList" /> has a fixed size; otherwise, false.</returns>
        bool IList.IsFixedSize => false;

        /// <summary>
        ///     Gets a value indicating whether the <see cref="T:System.Collections.IList" /> is read-only.
        /// </summary>
        /// <value></value>
        /// <returns>true if the <see cref="T:System.Collections.IList" /> is read-only; otherwise, false.</returns>
        bool IList.IsReadOnly => false;

        /// <summary>
        ///     Removes the first occurrence of a specific object from the <see cref="T:System.Collections.IList" />.
        /// </summary>
        /// <param name="value">The <see cref="T:System.Object" /> to remove from the <see cref="T:System.Collections.IList" />.</param>
        /// <exception cref="T:System.NotSupportedException">
        ///     The <see cref="T:System.Collections.IList" /> is read-only.-or- The
        ///     <see cref="T:System.Collections.IList" /> has a fixed size.
        /// </exception>
        void IList.Remove(object value)
        {
            if (!(value is IEntitySubItemCollectionItem)) 
                throw new InvalidObjectTypeInEntityException("Parameter is not an entity sub item collection item.");

            if (this is IList list)
            {
                var itemIndex = list.IndexOf(value);
                if (itemIndex > -1)
                    Remove(itemIndex);
            }
        }

        /// <summary>
        ///     Removes the <see cref="T:System.Collections.IList" /> item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///     <paramref name="index" /> is not a valid index in the <see cref="T:System.Collections.IList" />.
        /// </exception>
        /// <exception cref="T:System.NotSupportedException">
        ///     The <see cref="T:System.Collections.IList" /> is read-only.-or- The
        ///     <see cref="T:System.Collections.IList" /> has a fixed size.
        /// </exception>
        void IList.RemoveAt(int index) => Remove(index);

        /// <summary>
        ///     Indexer reference to an item in the collection
        /// </summary>
        /// <value></value>
        object IList.this[int index]
        {
            get => this[index];
            set => throw new OperationNotSupportedByEntityException();
        }

        /// <summary>
        ///     Copies the elements of the <see cref="T:System.Collections.ICollection" /> to an <see cref="T:System.Array" />,
        ///     starting at a particular <see cref="T:System.Array" /> index.
        /// </summary>
        /// <param name="array">
        ///     The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied
        ///     from <see cref="T:System.Collections.ICollection" />. The <see cref="T:System.Array" /> must have zero-based
        ///     indexing.
        /// </param>
        /// <param name="index">The zero-based index in <paramref name="array" /> at which copying begins.</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///     <paramref name="array" /> is null.
        /// </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///     <paramref name="index" /> is less than zero.
        /// </exception>
        /// <exception cref="T:System.ArgumentException">
        ///     <paramref name="array" /> is multidimensional.-or- <paramref name="index" /> is equal to or greater than the length
        ///     of <paramref name="array" />.-or- The number of elements in the source
        ///     <see cref="T:System.Collections.ICollection" /> is greater than the available space from <paramref name="index" />
        ///     to the end of the destination <paramref name="array" />.
        /// </exception>
        /// <exception cref="T:System.ArgumentException">
        ///     The type of the source <see cref="T:System.Collections.ICollection" />
        ///     cannot be cast automatically to the type of the destination <paramref name="array" />.
        /// </exception>
        void ICollection.CopyTo(Array array, int index)
        {
            var count = Count;
            var items = new IEntitySubItemCollectionItem[count];
            for (var counter = 0; counter < count; counter++)
                items[counter] = this[counter];
            Array.Copy(items, 0, array, index, count);
        }

        /// <summary>
        ///     Number of items in the collection
        /// </summary>
        /// <value></value>
        int ICollection.Count => Count;

        /// <summary>
        ///     Gets a value indicating whether access to the <see cref="T:System.Collections.ICollection" /> is synchronized
        ///     (thread safe).
        /// </summary>
        /// <value></value>
        /// <returns>
        ///     true if access to the <see cref="T:System.Collections.ICollection" /> is synchronized (thread safe);
        ///     otherwise, false.
        /// </returns>
        bool ICollection.IsSynchronized => InternalDataTable.Rows.IsSynchronized;

        /// <summary>
        ///     Gets an object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection" />.
        /// </summary>
        /// <value></value>
        /// <returns>An object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection" />.</returns>
        object ICollection.SyncRoot => InternalDataTable.Rows.SyncRoot;

        /// <summary>
        ///     Filter expression
        /// </summary>
        /// <remarks>
        ///     Filterable objects are always filtered by their master expression
        ///     AND the individual filter expression.
        ///     Filter expressions are NOT mapped. Therefore, they are based on the
        ///     field names used internally by the entity, and not by the publicly exposed
        ///     property names. If a property name needs to be used for filtering purposes,
        ///     one can retrieve the internal field name through the GetInternalFieldName()
        ///     method of the parent entity.
        /// </remarks>
        /// <example>iStatus = 1</example>
        public string FilterMaster
        {
            get => filterMaster;
            set
            {
                filterMaster = MassageFilterExpression(value);
                UpdateSpecialDataView();
            }
        }

        /// <summary>
        ///     Complete filter expression, including the master filter
        ///     and the individual filter
        /// </summary>
        /// <example>(Status = 1) AND (FirstName = 'John')</example>
        [NotReportSerializable]
        [NotClonable]
        public string CompleteFilterExpression
        {
            get
            {
                var retVal = string.Empty;
                if (FilterMaster.Length > 0)
                    retVal += "(" + FilterMaster + ")";
                if (Filter.Length > 0)
                {
                    if (retVal.Length > 0)
                        retVal += " AND ";
                    retVal += "(" + Filter + ")";
                }

                return retVal;
            }
        }

        /// <summary>
        ///     Filter expression
        /// </summary>
        /// <example>cFirstName = 'John'</example>
        /// <remarks>
        ///     Filter expressions are NOT mapped. Therefore, they are based on the
        ///     field names used internally by the entity, and not by the publicly exposed
        ///     property names. If a property name needs to be used for filtering purposes,
        ///     one can retrieve the internal field name through the GetInternalFieldName()
        ///     method of the parent entity.
        /// </remarks>
        public string Filter
        {
            get => filter;
            set
            {
                filter = MassageFilterExpression(value);
                UpdateSpecialDataView();
            }
        }

        /// <summary>
        ///     Clears out all filter expressions, except the master filter.
        /// </summary>
        public void ClearFilter() => Filter = string.Empty;

        /// <summary>Resets the internal table based on the data set of the parent collection.</summary>
        public void ResetTable()
        {
            InternalDataTable = null;
            InternalDataTable = ParentEntity.GetInternalData().Tables[internalDataTableName];
            LoadState = EntityLoadState.LoadComplete;
            if (!IsRawTable)
                UpdateSpecialDataView();
        }

        /// <summary>Method used to configure this object.</summary>
        protected abstract void Configure();

        /// <summary>Removes an item based on its (primary) key.</summary>
        /// <param name="key">Key of the item that's to be removed.</param>
        /// <returns>True if removed</returns>
        public virtual bool RemoveByKey(Guid key)
        {
            var retVal = false;
            if (ParentEntity.PrimaryKeyType == KeyType.Guid)
            {
                foreach (IEntitySubItemCollectionItem entity in this)
                    if (entity.PK == key)
                    {
                        entity.Remove();
                        retVal = true;
                        break;
                    }
            }
            else
                throw new UnsupportedKeyTypeException("Key type not supported.");

            return retVal;
        }

        /// <summary>Removes an item based on its (primary) key.</summary>
        /// <param name="key">Key of the item that's to be removed.</param>
        /// <returns>True if removed</returns>
        public virtual bool RemoveByKey(int key)
        {
            var retVal = false;
            if (ParentEntity.PrimaryKeyType == KeyType.Integer || ParentEntity.PrimaryKeyType == KeyType.IntegerAutoIncrement)
            {
                foreach (IEntitySubItemCollectionItem entity in this)
                    if (entity.PKInteger == key)
                    {
                        entity.Remove();
                        retVal = true;
                        break;
                    }
            }
            else
                throw new UnsupportedKeyTypeException("Key type not supported.");

            return retVal;
        }

        /// <summary>Removes an item based on its (primary) key.</summary>
        /// <param name="key">Key of the item that's to be removed.</param>
        /// <returns>True if removed</returns>
        public virtual bool RemoveByKey(string key)
        {
            var retVal = false;
            if (ParentEntity.PrimaryKeyType == KeyType.String)
            {
                foreach (IEntitySubItemCollectionItem entity in this)
                    if (entity.PKString == key)
                    {
                        entity.Remove();
                        retVal = true;
                        break;
                    }
            }
            else
                throw new UnsupportedKeyTypeException("Key type not supported.");

            return retVal;
        }

        /// <summary>Returns the current item count.</summary>
        /// <param name="ignoreSorting">
        ///     If set to <c>true</c> the sorting sequence (if present) is ignored and the raw table is
        ///     used instead..
        /// </param>
        /// <returns></returns>
        protected virtual int GetItemCount(bool ignoreSorting)
        {
            var rowCount = 0;
            if (ignoreSorting || IsRawTable)
                // This really is the number of items in the DataTable which we are abstracting away here...
                // Note: The collection of rows may contain deleted items, so we can not just use the current number
                foreach (DataRow row in InternalDataTable.Rows)
                    if (row.RowState != DataRowState.Deleted && row.RowState != DataRowState.Detached)
                        rowCount++;
                    else
                        // This is a filtered or sorted collection. We use the special view (array or rows)
                        // as the sort order.
                        foreach (var row2 in SpecialView)
                            if (row2.RowState != DataRowState.Deleted && row2.RowState != DataRowState.Detached)
                                rowCount++;

            return rowCount;
        }

        /// <summary>Retrieves an item from the collection by its index.</summary>
        /// <param name="key">Guid Key</param>
        /// <returns>Item</returns>
        public virtual IEntitySubItemCollectionItem GetItemByKey(Guid key)
        {
            // Note that this collection really only contains one member object (at a time).
            // This object is configured with an index, to know what row in the
            // DataTable to access. That object is then returned, and appears to be
            // a new object to the outside.
            var item = GetItemObject();
            item.PrimaryKeyField = PrimaryKeyField;
            // We need to find the row index of the item, considering that there might be deleted
            // records that need to be skipped.
            if (IsRawTable)
                foreach (DataRow row in InternalDataTable.Rows)
                {
                    if (row.RowState != DataRowState.Deleted && row.RowState != DataRowState.Detached) continue;
                    if (row[PrimaryKeyField].ToGuidSafe() != key) continue;
                    // We found the record we are after
                    item.SetCurrentRow(row);
                    return item;
                }
            else
                // We can simply go through the array of rows in this filtered or sorted collection
                foreach (var row in SpecialView)
                {
                    if (row.RowState != DataRowState.Deleted && row.RowState != DataRowState.Detached) continue;
                    if (row[PrimaryKeyField].ToGuidSafe() != key) continue;
                    // We found the record we are after
                    item.SetCurrentRow(row);
                    return item;
                }

            throw new IndexOutOfBoundsException();
        }

        public virtual IEntitySubItemCollectionItem GetItemByIndex(int index) => GetItemByIndex(index, false); // This method implementation is required by the interface

        /// <summary>Retrieves an item from the collection by its index.</summary>
        /// <param name="index">Numeric index</param>
        /// <param name="absoluteTableIndex">If true, the index is based on the data table, not the (potentially) sorted view</param>
        /// <returns>Item</returns>
        public virtual IEntitySubItemCollectionItem GetItemByIndex(int index, bool absoluteTableIndex)
        {
            // Note that this collection really only contains one member object (at a time).
            // This object is configured with an index, to know what row in the
            // DataTable to access. That object is then returned, and appears to be
            // a new object to the outside.
            var item = GetItemObject();
            item.PrimaryKeyField = PrimaryKeyField;
            // We need to find the row index of the item, considering that there might be deleted
            // records that need to be skipped.
            var rowIndex = 0;
            var undeletedRecords = 0;
            var recordFound = false;
            if (IsRawTable || absoluteTableIndex)
                foreach (DataRow row in InternalDataTable.Rows)
                {
                    rowIndex++;
                    if (row.RowState != DataRowState.Deleted && row.RowState != DataRowState.Detached)
                        undeletedRecords++;
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
                    if (row.RowState != DataRowState.Deleted && row.RowState != DataRowState.Detached)
                        undeletedRecords++;
                    if (undeletedRecords == index + 1)
                    {
                        // We found the record we are after
                        recordFound = true;
                        break;
                    }
                }

            if (!recordFound) throw new IndexOutOfBoundsException();
            if (IsRawTable || absoluteTableIndex)
                item.SetCurrentRow(InternalDataTable.Rows[rowIndex - 1]);
            else
                item.SetCurrentRow(SpecialView[rowIndex - 1]);
            return item;
        }

        /// <summary>
        ///     This method can be used to make sure the default table in the internal recordset has all the required fields.
        ///     If the field (column) doesn't exist, it will be added.
        /// </summary>
        /// <param name="fieldName">Field name to check for.</param>
        /// <returns>true or false</returns>
        protected bool CheckColumn(string fieldName) => BusinessEntityHelper.CheckColumn(InternalDataTable, fieldName);

        /// <summary>
        ///     Adds a new row to the internal data table, and returns a
        ///     sub-item entity that links to it.
        /// </summary>
        /// <returns>Entity Sub Item Collection Item</returns>
        protected virtual IEntitySubItemCollectionItem AddNewRow()
        {
            // We add a completely new row
            var rowTab = InternalDataTable.NewRow();
            switch (PrimaryKeyType)
            {
                case KeyType.Guid:
                    rowTab[PrimaryKeyField] = Guid.NewGuid();
                    break;
                case KeyType.Integer:
                    rowTab[PrimaryKeyField] = ParentEntity.AssociatedBusinessObject.GetNewIntegerKey(InternalDataTable.TableName, InternalDataTable.DataSet);
                    break;
                case KeyType.IntegerAutoIncrement:
                    rowTab[PrimaryKeyField] = ParentEntity.AssociatedBusinessObject.GetNewIntegerKey(InternalDataTable.TableName, InternalDataTable.DataSet);
                    break;
                case KeyType.String:
                    rowTab[PrimaryKeyField] = ParentEntity.AssociatedBusinessObject.GetNewStringKey(InternalDataTable.TableName, InternalDataTable.DataSet);
                    break;
            }

            rowTab[ForeignKeyField] = InternalDataTable.DataSet.Tables[ParentTableName].Rows[0][ParentTablePrimaryKeyField];

            // We allow the developer to add new data to rows
            var associatedBusinessObject = ParentEntity.AssociatedBusinessObject;
            var currentBusinessObject = associatedBusinessObject as BusinessObject;
            if (currentBusinessObject != null)
                currentBusinessObject.CallPopulateNewRecord(rowTab, rowTab.Table.TableName, rowTab.Table.DataSet);
            AddNewRowInformation(rowTab);

            // Done. The record can be added
            InternalDataTable.Rows.Add(rowTab);
            var newItem = GetItemByIndex(GetItemCount(true) - 1, true);

            // We may have to trigger a refresh of the special view in case sorting is applied
            if (!string.IsNullOrEmpty(SortBy))
                SortBy = SortBy; // Triggers a refresh of the view

            // We raise an update event
            if (ParentEntity is BusinessEntity)
            {
                var entity = (BusinessEntity) ParentEntity;
                entity.DataUpdated(string.Empty, InternalDataTable.TableName);
            }

            return newItem;
        }

        /// <summary>
        ///     This method is provided to be overridden in subclasses.
        ///     It can be used to add new information to a newly added record
        ///     of the collection table. For instance, if the developer wanted
        ///     to set a timestamp field in the table every time a new record
        ///     gets added and before the record is available as a collection item,
        ///     this method could be overridden, and the timestamp field could simply
        ///     be set like so: NewRow["timeStamp"] = DateTime.Now;
        /// </summary>
        /// <param name="newRow">New Data Row</param>
        protected virtual void AddNewRowInformation(DataRow newRow) { }

        /// <summary>
        ///     This method is used internally to make sure the appropriate data view
        ///     or data table is used.
        /// </summary>
        protected virtual void UpdateSpecialDataView()
        {
            if (CompleteFilterExpression.Length > 0 || CompleteSortExpression.Length > 0)
                // We have to use a view
                SpecialView = InternalDataTable.Select(CompleteFilterExpression, CompleteSortExpression);
            else
                SpecialView = null;
        }

        /// <summary>
        ///     Called whenever data in a sub-item is updated
        /// </summary>
        /// <param name="fieldName">Changed field name</param>
        /// <param name="updatedRow">Reference to the internal row that has been updated</param>
        internal virtual void DataUpdated(string fieldName, DataRow updatedRow)
        {
            if (!IsRawTable)
                UpdateSpecialDataView();

            if (this is IBindingList list)
            {
                // We need to use reflection to raise this event, because we can not add
                // a raise-method to the explicit interface implementation,
                // and we cannot just call the event on this type since we have
                // to cast it to its interface
                var eventInfo = typeof(IBindingList).GetEvent("ListChanged", BindingFlags.Public | BindingFlags.Instance);
                if (eventInfo != null)
                {
                    var args = new ListChangedEventArgs(ListChangedType.Reset, -1);
                    object[] arguments = {this, args};
                    try
                    {
                        var raiseMethod = eventInfo.GetRaiseMethod();
                        if (raiseMethod != null)
                            raiseMethod.Invoke(list, arguments);
                    }
                    catch { } // Probably no subscribers
                }
            }
        }

        /// <summary>
        ///     This method is used to change the filter expression automatically to correct syntax.
        /// </summary>
        /// <param name="filterExpression">Original filter expression</param>
        /// <returns>New filter expression</returns>
        protected virtual string MassageFilterExpression(string filterExpression) => filterExpression;

        /// <summary>
        ///     Changes the sort expression, so things like property names
        ///     are replaced by actual field names.
        /// </summary>
        /// <param name="sortExpression">Original sort expression</param>
        /// <returns>New sort expression</returns>
        protected virtual string MassageSortExpression(string sortExpression)
        {
            if (ParentEntity is BusinessEntity entity)
            {
                // We look at each comma-separated part of the expression
                var expressionParts = sortExpression.Split(',');
                var newSortExpression = string.Empty;
                foreach (var part in expressionParts)
                {
                    // We trim out other aspects, such as strings caused by things like DESC
                    var pieces = part.Trim().Split(' ');
                    var originalPiece = pieces[0];
                    var newPiece = entity.GetInternalFieldName(originalPiece, InternalDataTable.TableName);
                    var currentPart = newPiece;
                    for (var pieceCounter = 1; pieceCounter < pieces.Length; pieceCounter++)
                        currentPart += " " + pieces[pieceCounter];
                    if (newSortExpression.Length > 0)
                        newSortExpression += ", ";
                    newSortExpression += currentPart;
                }

                sortExpression = newSortExpression;
            }

            return sortExpression;
        }
    }
}