using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using CODE.Framework.Fundamentals.Utilities;
using Milos.Data;

namespace Milos.BusinessObjects
{
    /// <summary>
    ///     A BusinessEntity is an individual instance of a data entity served up by
    ///     BusinessObjects. For instance, a name BusinessObject may generate a PersonEntity.
    ///     Note that all business objects can operate without the use of an entity object.
    ///     However, Entity objects make using business objects much more straightforward.
    /// </summary>
    [Serializable]
    public abstract class BusinessEntity : IBusinessEntity, IDataBindingRefresher, INotifyPropertyChanged, IDirty
    {
        /// <summary>
        ///     Delegate used for all event handlers that can be canceled
        /// </summary>
        public delegate void CancelableEventHandler(object sender, CancelableEventArgs e);

        /// <summary>
        ///     Delegate used for the DataSourceChangedWithDetails event
        /// </summary>
        public delegate void DataSourceChangedEventHandler(object sender, DataSourceChangedEventArgs e);

        /// <summary>
        ///     Internal string dictionary of field maps
        /// </summary>
        private readonly Dictionary<string, string> fieldMaps = new Dictionary<string, string>();

        /// <summary>
        ///     Internal string dictionary of table maps
        /// </summary>
        private readonly Dictionary<string, string> tableMaps = new Dictionary<string, string>();

        /// <summary>
        ///     Internal reference to the broken rules collection
        /// </summary>
        [NonSerialized] private BrokenRulesCollection brokenRulesCollection;

        /// <summary>
        ///     Internal reference to a business object
        /// </summary>
        private IBusinessObject internalBusinessObject;

        /// <summary>
        ///     If this property is set to true, the entity will show as
        ///     NOT dirty.
        /// </summary>
        /// <remarks>This will be reset to false, every time the SetFieldValue() method is called.</remarks>
        private bool isDirtyOverride;

        /// <summary>
        ///     Constructor intended to be used when a new entity is meant to be created
        /// </summary>
        protected BusinessEntity() => ConfigureObject(EntityLaunchMode.New, null);

        /// <summary>
        ///     Constructor used for custom entity launch modes
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <param name="parameters">The parameters.</param>
        protected BusinessEntity(EntityLaunchMode mode, object parameters) => ConfigureObject(mode, parameters);

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="internalDataSet">Internal data used by this entity (DataSet)</param>
        protected BusinessEntity(DataSet internalDataSet) => ConfigureObject(EntityLaunchMode.PassData, internalDataSet);

        /// <summary>
        ///     This constructor is used to load existing data based on an ID
        /// </summary>
        /// <param name="entityId">Entity ID (PK)</param>
        protected BusinessEntity(Guid entityId) => ConfigureObject(EntityLaunchMode.Load, entityId);

        /// <summary>
        ///     This constructor is used to load existing data based on an ID
        /// </summary>
        /// <param name="entityId">Entity ID (PK)</param>
        protected BusinessEntity(int entityId) => ConfigureObject(EntityLaunchMode.Load, entityId);

        /// <summary>
        ///     This constructor is used to load existing data based on an ID
        /// </summary>
        /// <param name="entityId">Entity ID (PK)</param>
        protected BusinessEntity(string entityId) => ConfigureObject(EntityLaunchMode.Load, entityId);

        /// <summary>
        ///     Initializes a new instance of the <see cref="BusinessEntity" /> class.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <param name="context">The context.</param>
        protected BusinessEntity(SerializationInfo info, StreamingContext context)
        {
            // Since all the data in this entity is stored in the internal DataSet,
            // we can simply pass serialization calls through to a DataSet
            DataSet internalDataSet = null;
            var dataSetType = typeof(DataSet);
            var constructors = dataSetType.GetConstructors(BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance);
            foreach (var constructor in constructors)
            {
                var parameters = constructor.GetParameters();
                if (parameters.Length != 2) continue;
                if (parameters[0].ParameterType == typeof(SerializationInfo) && parameters[1].ParameterType == typeof(StreamingContext))
                    // This is the right one!
                    internalDataSet = (DataSet) constructor.Invoke(new object[] {info, context});
            }

            if (internalDataSet != null)
                ConfigureObject(EntityLaunchMode.PassData, internalDataSet);
            else
                throw new SerializationException();
        }

        /// <summary>
        ///     Reference to the DataSet that holds the data utilized by this BusinessEntity
        /// </summary>
        protected virtual DataSet InternalDataSet { get; set; }

        /// <summary>
        ///     Name of the master entity in the internal DataSet
        /// </summary>
        protected virtual string MasterEntity { get; set; }

        /// <summary>
        ///     Name of the PK column of the master entity in the internal DataSet
        /// </summary>
        protected virtual string PrimaryKeyField { get; set; }

        /// <summary>
        ///     Contains the name of the broken rules table that is embedded in the dataset when broken rules are encountered.
        ///     Note: This is mostly intended for internal use, although some classes may want to override this name,
        ///     in scenarios where the table name could cause a conflict.
        /// </summary>
        protected internal virtual string BrokenRulesTableName { get; set; } = "__BrokenRules";

        /// <summary>
        ///     Broken business rules collection
        /// </summary>
        [NotReportSerializable]
        [NotClonable]
        [XmlIgnore]
        public BrokenRulesCollection BrokenRules => brokenRulesCollection ?? (brokenRulesCollection = new BrokenRulesCollection(this));

        /// <summary>
        ///     Defines how the object treats attempts to set invalid values in fields,
        ///     such as trying to assign a string that is too long.
        /// </summary>
        protected internal virtual InvalidFieldBehavior InvalidFieldUpdateMode { get; set; } = InvalidFieldBehavior.FixInvalidValues;

        /// <summary>
        ///     Data load state of the entity
        /// </summary>
        [NotReportSerializable]
        [NotClonable]
        public EntityLoadState LoadState { get; private set; } = EntityLoadState.Loading;

        /// <summary>
        ///     Primary key of the current entity (Guid)
        /// </summary>
        [NotReportSerializable]
        [NotClonable]
        public Guid PK
        {
            get
            {
                if (PrimaryKeyType != KeyType.Guid) return Guid.Empty;
                return (Guid) InternalDataSet.Tables[GetInternalTableName(MasterEntity)].Rows[0][GetInternalFieldName(PrimaryKeyField, MasterEntity)];
            }
        }

        /// <summary>
        ///     Primary Key Type used by this entity
        /// </summary>
        [NotReportSerializable]
        [NotClonable]
        public KeyType PrimaryKeyType { get; set; } = KeyType.Guid;

        /// <summary>
        ///     Primary key of the current entity (int)
        /// </summary>
        [NotReportSerializable]
        [NotClonable]
        public int PKInteger
        {
            get
            {
                if (PrimaryKeyType != KeyType.Integer && PrimaryKeyType != KeyType.IntegerAutoIncrement) return -1;
                return (int) InternalDataSet.Tables[GetInternalTableName(MasterEntity)].Rows[0][GetInternalFieldName(PrimaryKeyField, MasterEntity)];
            }
        }

        /// <summary>
        ///     Primary key of the current entity (string)
        /// </summary>
        [NotReportSerializable]
        [NotClonable]
        public string PKString => PrimaryKeyType != KeyType.String ? string.Empty : InternalDataSet.Tables[GetInternalTableName(MasterEntity)].Rows[0][GetInternalFieldName(PrimaryKeyField, MasterEntity)].ToString();

        /// <summary>
        ///     Primary key of the current entity (string)
        /// </summary>
        [NotClonable]
        public string Id => InternalDataSet.Tables[GetInternalTableName(MasterEntity)].Rows[0][GetInternalFieldName(PrimaryKeyField, MasterEntity)].ToString();

        /// <summary>
        ///     Internal reference to the business object associated with this entity
        /// </summary>
        /// <remarks>This property uses the GetBusinessObject() method to create an instance of the business object if needed.</remarks>
        [NotReportSerializable]
        [NotClonable]
        public IBusinessObject AssociatedBusinessObject => internalBusinessObject ?? (internalBusinessObject = GetBusinessObject());

        /// <summary>
        ///     Indicates whether the objects data contains any chances, such as
        ///     modifications, deletes, or additions.
        /// </summary>
        [NotReportSerializable]
        [NotClonable]
        public bool IsDirty => !isDirtyOverride && InternalDataSet.HasChanges();

        /// <summary>
        ///     Sets the entity to appear not dirty, even if there are changes in the data.
        /// </summary>
        /// <remarks>
        ///     This does NOT accept changes. It only provides a manual override for IsDirty.
        ///     This can sometimes be useful for new entities and also for entities that should
        ///     not appear dirty until further changes are made (such as when the user just
        ///     has been asked whether they want to save changes).
        /// </remarks>
        public void IgnoreIsDirty() => isDirtyOverride = true;

        /// <summary>
        ///     State of the current entity (unchanged, added, deleted, modified,...)
        /// </summary>
        [NotReportSerializable]
        [NotClonable]
        public DataRowState EntityState => InternalDataSet.Tables[GetInternalTableName(MasterEntity)].Rows[0].RowState;

        /// <summary>
        ///     Returns the data set used internally.
        /// </summary>
        /// <returns>DataSet</returns>
        /// <remarks>
        ///     This type of business enitity uses a DataSet internally to keep track
        ///     of the state of the entity. Using this method, it is possible to gain access
        ///     to that DataSet. Note however, that not all business entity types use
        ///     DataSets. If you use the internal data set as an architectural cornerstone
        ///     of your application, it will not be possible to switch to other business
        ///     entity types. Avoid using the internal DataSet if possible.
        /// </remarks>
        public DataSet GetInternalData() => InternalDataSet;

        /// <summary>
        ///     Returns whether or not that field's value is currently null/nothing
        /// </summary>
        /// <param name="fieldName">Field name as it appears in the data set</param>
        /// <returns>True or false</returns>
        public bool IsFieldNull(string fieldName) => InternalDataSet.Tables[GetInternalTableName(MasterEntity)].Rows[0][GetInternalFieldName(fieldName, MasterEntity)] == DBNull.Value;

        /// <summary>
        ///     Returns all the internal data as an XML string.
        /// </summary>
        /// <returns>Xml String</returns>
        public string GetRawData() => InternalDataSet.GetXml();

        /// <summary>
        ///     Saves the current data
        /// </summary>
        /// <returns>True or false, depending on whether or not the operation is successful.</returns>
        public virtual bool Save()
        {
            // If the entity is considered deleted, then we raise an exception,
            // since it is not possible anymore to save the entity to the database
            if (LoadState == EntityLoadState.Deleted) ThrowDeletedEntityException("Cannot save deleted entities.", this);

            if (BeforeSave != null)
            {
                var args = new CancelableEventArgs();
                BeforeSave(this, args);
                if (args.Cancel) return false;
            }

            var saved = Save(AssociatedBusinessObject);
            if (saved)
                Saved?.Invoke(this, new EventArgs());
            return saved;
        }

        /// <summary>
        ///     Saves the current data.
        /// </summary>
        /// <param name="businessObject">BusinessObject used to save the current information.</param>
        /// <returns></returns>
        public virtual bool Save(IBusinessObject businessObject)
        {
            // If the entity is considered deleted, then we raise an exception,
            // since it is not possible anymore to save the entity to the database
            if (LoadState == EntityLoadState.Deleted) ThrowDeletedEntityException("Cannot save deleted entities.", this);

            if (businessObject == null) throw new NullReferenceException("Parameter 'businessObject' cannot be null.");

            if (BeforeSave != null)
            {
                var args = new CancelableEventArgs();
                BeforeSave(this, args);
                if (args.Cancel) return false;
            }

            var saved = businessObject.Save(GetInternalData());
            if (saved)
                Saved?.Invoke(this, new EventArgs());
            return saved;
        }

        /// <summary>
        ///     Verifies the current data and returns true or false depending on the result
        /// </summary>
        /// <returns>True or false</returns>
        bool IVerifyable.Verify()
        {
            Verify();
            return BrokenRules.Count == 0;
        }

        /// <summary>
        ///     Removes (deletes) the current entity
        /// </summary>
        /// <returns>True or False</returns>
        /// <example>
        ///     CustomerEntity customer = CustomerEntity.LoadEntity(key);
        ///     customer.Remove();
        ///     // The customer is now removed from the database. The in-memory data
        ///     // is therefore invalid and cannot be used anymore.
        ///     // The following would now raise an exception:
        ///     Console.WriteLine(customer.LastName); // BusinessEntityDeletedException!
        /// </example>
        public virtual bool Remove()
        {
            // If the entity is already considered deleted, then we raise an exception,
            // since it is not possible to delete an entity that has already been deleted
            if (LoadState == EntityLoadState.Deleted) ThrowDeletedEntityException("Cannot delete entities that have already been deleted.", this);

            if (BeforeRemove != null)
            {
                var args = new CancelableEventArgs();
                BeforeRemove(this, args);
                if (args.Cancel) return false;
            }

            var retVal = false;
            switch (PrimaryKeyType)
            {
                case KeyType.Guid:
                    retVal = AssociatedBusinessObject.Delete(PK);
                    break;
                case KeyType.Integer:
                case KeyType.IntegerAutoIncrement:
                    retVal = AssociatedBusinessObject.Delete(PKInteger);
                    break;
                case KeyType.String:
                    retVal = AssociatedBusinessObject.Delete(PKString);
                    break;
            }

            if (retVal) LoadState = EntityLoadState.Deleted;

            if (retVal) Removed?.Invoke(this, new EventArgs());
            return retVal;
        }

        /// <summary>
        ///     Performs a delete operation by calling Remove() internally
        /// </summary>
        /// <returns>True or false</returns>
        /// <remarks>
        ///     Note: This method is not overridable. Override the Remove() method instead, which is called by this method.
        /// </remarks>
        public bool Delete() => Remove();

        /// <summary>
        ///     This method generates the appropriate business object for the current entity.
        ///     It serves as a factory.
        ///     This method has to be overridden in subclasses if used by the entity.
        /// </summary>
        /// <returns>BusinessObject</returns>
        public abstract IBusinessObject GetBusinessObject();

        /// <summary>
        ///     Implementation of IDisposable, in particular the Dispose() method.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Event that indicates that data within the object has been updated
        /// </summary>
        /// <remarks>
        ///     This is a useful generic event that fires every time any of the
        ///     data in the business entity is updated. This can be useful for
        ///     things such as data binding.
        /// </remarks>
        public event EventHandler DataSourceChanged;

        /// <summary>
        ///     This event fires whenever a property on this control changes
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        ///     This event fires every time an entity is saved
        /// </summary>
        public event EventHandler Saved;

        /// <summary>
        ///     This event fires before the entity is saved
        /// </summary>
        public event CancelableEventHandler BeforeSave;

        /// <summary>
        ///     This event fires after the entity is verified
        /// </summary>
        public event EventHandler Verified;

        /// <summary>
        ///     This event fires before the entity is verified
        /// </summary>
        public event CancelableEventHandler BeforeVerify;

        /// <summary>
        ///     This event fires after the entity is verified
        /// </summary>
        public event EventHandler Removed;

        /// <summary>
        ///     This event fires before the entity is verified
        /// </summary>
        public event CancelableEventHandler BeforeRemove;

        /// <summary>
        ///     Event that indicates that data within the object has been updated
        /// </summary>
        /// <remarks>
        ///     This event also provides specific information about the field and
        ///     table names that updated.
        ///     This is a useful generic event that fires every time any of the
        ///     data in the business entity is updated. This can be useful for
        ///     things such as data binding.
        /// </remarks>
        public event DataSourceChangedEventHandler DataSourceChangedWithDetails;

        /// <summary>
        ///     This method is called internally by the object's constructors.
        ///     This method calls out to other methods to load its data or create new data.
        /// </summary>
        /// <param name="mode">Launch mode enum</param>
        /// <param name="parameter1">Pass-through parameter</param>
        protected virtual void ConfigureObject(EntityLaunchMode mode, object parameter1)
        {
            var associatedBusinessObject = AssociatedBusinessObject;
            // We always need to configure these settings
            MasterEntity = associatedBusinessObject.MasterEntity;
            PrimaryKeyField = associatedBusinessObject.PrimaryKeyField;
            PrimaryKeyType = associatedBusinessObject is BusinessObject concreteBusinessObject ? concreteBusinessObject.GetPrimaryKeyType() : associatedBusinessObject.PrimaryKeyType;
            Configure();

            var loadedFromXmlSerialization = false;

            switch (mode)
            {
                case EntityLaunchMode.New:
                    try
                    {
                        NewEntity(associatedBusinessObject);
                    }
                    catch
                    {
                        // This could potentially be a serialization problem
                        if (LaunchedFormXmlDeserialization())
                            loadedFromXmlSerialization = true;
                        else
                            throw;
                    }

                    break;
                case EntityLaunchMode.Load:
                    switch (PrimaryKeyType)
                    {
                        case KeyType.Guid:
                            LoadEntity((Guid) parameter1, associatedBusinessObject);
                            break;
                        case KeyType.Integer:
                            LoadEntity((int) parameter1, associatedBusinessObject);
                            break;
                        case KeyType.IntegerAutoIncrement:
                            LoadEntity((int) parameter1, associatedBusinessObject);
                            break;
                        case KeyType.String:
                            LoadEntity((string) parameter1, associatedBusinessObject);
                            break;
                    }

                    break;
                case EntityLaunchMode.PassData:
                    // We simply store the provided dataset for later use.
                    InternalDataSet = (DataSet) parameter1;
                    break;
                case EntityLaunchMode.Custom:
                    LoadCustom(parameter1, associatedBusinessObject);
                    break;
            }

            // We also load additional collections (potentially)
            if (!loadedFromXmlSerialization) LoadSubItemCollections();

            //// We also trigger background loading of data (if needed)
            //if (mode != EntityLaunchMode.New) InitiateBackgroundLoading();

            // This entity is considered "loaded"
            LoadState = EntityLoadState.LoadComplete;
        }

        /// <summary>
        ///     Returns true if the entity is created by the XML deserialization mechanism.
        /// </summary>
        /// <returns></returns>
        [Obsolete]
        private bool LaunchedFormXmlDeserialization() => false; // Was never implemented and is really obsolete

        /// <summary>
        ///     This method is called when the object initializes.
        ///     Use this method to set configuration options.
        /// </summary>
        protected virtual void Configure()
        {
        }

        /// <summary>
        ///     This method can be overridden in subclasses to implement custom load behavior
        ///     For instance, if the entity is not loaded by GUID, this method could be used.
        ///     Typically, this method would assign a dataset to dsInternal.
        /// </summary>
        /// <param name="parameter1">Pass through parameter from constructor</param>
        /// <param name="businessObject">Business Object</param>
        protected virtual void LoadCustom(object parameter1, IBusinessObject businessObject)
        {
        }

        /// <summary>
        ///     This method can be overridden in subclasses to load additional member objects
        /// </summary>
        protected virtual void LoadSubItemCollections()
        {
        }

        protected virtual void LoadEntity(Guid entityId, IBusinessObject businessObject)
        {
            if (PrimaryKeyType != KeyType.Guid) throw new UnsupportedKeyTypeException(KeyType.Guid);
            InternalDataSet = businessObject.LoadEntity(entityId);
        }

        /// <summary>
        ///     This method is used to load existing data into the current entity.
        ///     This method is designed to be overridden in subclasses if needed.
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="businessObject">Business Object</param>
        protected virtual void LoadEntity(int entityId, IBusinessObject businessObject)
        {
            if (PrimaryKeyType != KeyType.Integer && PrimaryKeyType != KeyType.IntegerAutoIncrement) throw new UnsupportedKeyTypeException(KeyType.Integer);
            InternalDataSet = businessObject.LoadEntity(entityId);
        }

        /// <summary>
        ///     This method is used to load existing data into the current entity.
        ///     This method is designed to be overridden in subclasses if needed.
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="businessObject">Business Object</param>
        protected virtual void LoadEntity(string entityId, IBusinessObject businessObject)
        {
            switch (PrimaryKeyType) {
                case KeyType.Integer:
                    InternalDataSet = businessObject.LoadEntity(new Guid(entityId));
                    break;
                case KeyType.String:
                    InternalDataSet = businessObject.LoadEntity(entityId);
                    break;
                default:
                    throw new UnsupportedKeyTypeException(PrimaryKeyType);
            }
        }

        /// <summary>
        ///     This method is used to add a new record to the internal data source.
        ///     This method can be overridden if adding a new entity is not according
        ///     to the default behavior
        /// </summary>
        /// <param name="businessObject">Business object</param>
        protected virtual void NewEntity(IBusinessObject businessObject) => InternalDataSet = businessObject.AddNew();

        /// <summary>
        ///     Verifies the current data
        /// </summary>
        public virtual void Verify()
        {
            if (BeforeVerify != null)
            {
                var args = new CancelableEventArgs();
                BeforeVerify(this, args);
                if (args.Cancel) return;
            }

            var currentBusinessObject = AssociatedBusinessObject;
            currentBusinessObject.Verify(InternalDataSet);
            Verified?.Invoke(this, new EventArgs());
        }

        /// <summary>
        ///     Verifies the current data
        /// </summary>
        /// <param name="ruleType">Type of rule the verification is to be limited to</param>
        public virtual void Verify(Type ruleType)
        {
            if (BeforeVerify != null)
            {
                var args = new CancelableEventArgs();
                BeforeVerify(this, args);
                if (args.Cancel) return;
            }

            var currentBusinessObject = AssociatedBusinessObject;
            if (currentBusinessObject is BusinessObject currentBusinessObject2)
                currentBusinessObject2.Verify(InternalDataSet, ruleType);
            else
                currentBusinessObject.Verify(InternalDataSet);
            Verified?.Invoke(this, new EventArgs());
        }

        /// <summary>
        ///     Raises the deleted entity exception.
        /// </summary>
        /// <param name="message">The message the exception is populated with</param>
        /// <param name="source">The entity that caused the exception</param>
        protected static void ThrowDeletedEntityException(string message, BusinessEntity source) => throw new DeletedEntityException(message) {Source = source.GetType().Name};

        /// <summary>
        ///     Finalize
        /// </summary>
        ~BusinessEntity() => Dispose(false);

        /// <summary>
        ///     Dispose method designed to be overridden in subclasses
        /// </summary>
        /// <param name="disposing">True is called from Dispose()</param>
        protected virtual void Dispose(bool disposing)
        {
            if (InternalDataSet != null)
            {
                InternalDataSet.Dispose();
                InternalDataSet = null;
            }

            if (internalBusinessObject != null)
            {
                internalBusinessObject.Dispose();
                internalBusinessObject = null;
            }
        }

        /// <summary>
        ///     Creates a copy of the current business entity
        /// </summary>
        /// <returns>New instance of a business entity with identical data.</returns>
        /// <remarks>
        ///     Note that the returned object is NOT an exact copy of the original entity.
        ///     Instead, it will be a new object with identical values. Many internals however,
        ///     such as primary keys, will be new and independent from the current object.
        /// </remarks>
        public IBusinessEntity Clone()
        {
            // We have to create a new instance of the same object as the current one
            var currentEntityType = GetType();
            // We check whether this type has a NewEntity static method
            var newEntityStaticMethod = currentEntityType.GetMethod("NewEntity", BindingFlags.Public | BindingFlags.Static);
            var newEntity = newEntityStaticMethod != null ? (IBusinessEntity) newEntityStaticMethod.Invoke(null, null) : (IBusinessEntity) Activator.CreateInstance(currentEntityType);

            // We clone the actual values
            CloneProperties(this, newEntity);

            return newEntity;
        }

        /// <summary>
        ///     Explores an individual object and clones all clonable properties
        /// </summary>
        /// <param name="originalObject">Original object</param>
        /// <param name="newObject">New object</param>
        private void CloneProperties(object originalObject, object newObject)
        {
            // We need to figure out what properties we have on the current object
            var properties = originalObject.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            // We iterate over all the objects and find the read/write properties that 
            // should be included in the cloning operation
            foreach (var property in properties)
            {
                // If the property is a sub-item collection, we need to handle it special
                if (property.PropertyType.IsSubclassOf(typeof(EntitySubItemCollection))) CloneCollection(originalObject, newObject, property);
                // If this is a sub-item collection and one of the target key fields, then we need to set the appropriate values but only for the selected key type.
                if (newObject != null && newObject is EntitySubItemCollectionXLinkItem newItem && (property.Name == "TargetFK" || property.Name == "TargetFKString" || property.Name == "TargetFKInteger"))
                {
                    switch (property.Name)
                    {
                        case "TargetFK":
                            if (newItem.ParentEntity.PrimaryKeyType == KeyType.Guid)
                            {
                                // We are supposed to set the guid, so we can go ahead and set 
                                var customAttributes = property.GetCustomAttributes(typeof(NotClonableAttribute), true);
                                if (customAttributes.Length == 0) property.SetValue(newObject, property.GetValue(originalObject, null), null);
                            }

                            break;
                        case "TargetFKString":
                            if (newItem.ParentEntity.PrimaryKeyType == KeyType.String)
                            {
                                // We are supposed to set the guid, so we can go ahead and set 
                                var customAttributes = property.GetCustomAttributes(typeof(NotClonableAttribute), true);
                                if (customAttributes.Length == 0) property.SetValue(newObject, property.GetValue(originalObject, null), null);
                            }

                            break;
                        case "TargetFKInteger":
                            if (newItem.ParentEntity.PrimaryKeyType == KeyType.Integer || newItem.ParentEntity.PrimaryKeyType == KeyType.IntegerAutoIncrement)
                            {
                                // We are supposed to set the guid, so we can go ahead and set 
                                var customAttributes = property.GetCustomAttributes(typeof(NotClonableAttribute), true);
                                if (customAttributes.Length == 0) property.SetValue(newObject, property.GetValue(originalObject, null), null);
                            }

                            break;
                    }
                }
                else if (property.CanRead && property.CanWrite)
                {
                    // We check for the special NotClonable attribute 
                    var customAttributes = property.GetCustomAttributes(typeof(NotClonableAttribute), true);
                    if (customAttributes.Length == 0) property.SetValue(newObject, property.GetValue(originalObject, null), null);
                }
            }
        }

        /// <summary>
        ///     Clones an individual collection within a business entity
        /// </summary>
        /// <param name="originalEntity">Original (clone origin) entity</param>
        /// <param name="newEntity">New (clone result) entity</param>
        /// <param name="collectionProperty">Information about the property that represents the collection</param>
        private void CloneCollection(object originalEntity, object newEntity, PropertyInfo collectionProperty)
        {
            var originalCollection = (IEntitySubItemCollection) collectionProperty.GetValue(originalEntity, null);
            var newCollection = (IEntitySubItemCollection) collectionProperty.GetValue(newEntity, null);
            var itemCount = 0;
            foreach (IEntitySubItemCollectionItem originalItem in originalCollection)
            {
                itemCount++;

                // We first create a new collection items on the new entity
                IEntitySubItemCollectionItem newItem;
                if (newCollection.Count < itemCount)
                    if (newCollection is EntityXlinkSubItemCollection newCollection2 && originalItem is EntitySubItemCollectionXLinkItem originalItem2)
                        switch (newCollection.ParentEntity.PrimaryKeyType)
                        {
                            case KeyType.Guid:
                                newItem = newCollection2.Add(originalItem2.TargetFK);
                                break;
                            case KeyType.String:
                                newItem = newCollection2.Add(originalItem2.TargetFKString);
                                break;
                            default:
                                newItem = newCollection2.Add(originalItem2.TargetFKInteger);
                                break;
                        }
                    else
                        newItem = newCollection.Add();
                else
                    // For some reason, an item with that index already exists, so we reuse it.
                    // The reason for this must be that the entity creates collection items by default.
                    newItem = newCollection[itemCount - 1];

                // We now iterate over all properties on that item and clone them
                CloneProperties(originalItem, newItem);
            }
        }

        /// <summary>
        ///     Saves all the specified business entities within an atomic operation
        ///     (within the same transaction)
        /// </summary>
        /// <param name="entities">Collection of entities that is to be saved.</param>
        /// <param name="verifyBeforeSave">Defines whether a verify should be run on the business entities before they are saved.</param>
        /// <returns>True if saved successfully</returns>
        public static bool AtomicSave(IList<BusinessEntity> entities, bool verifyBeforeSave = true)
        {
            var entityCount = entities.Count;
            var entities2 = new BusinessEntity[entityCount];
            foreach (var entity in entities)
                entities2[entityCount] = entity;

            return AtomicSave(entities2, verifyBeforeSave);
        }

        /// <summary>
        ///     Saves all the specified business entities within an atomic operation
        ///     (within the same transaction)
        /// </summary>
        /// <param name="entities">Array of entities that is to be saved.</param>
        /// <param name="verifyBeforeSave">Defines whether a verify should be run on the business entities before they are saved.</param>
        /// <returns>True if saved successfully</returns>
        public static bool AtomicSave(BusinessEntity[] entities, bool verifyBeforeSave = true)
        {
            foreach (var entity in entities)
                if (entity.LoadState == EntityLoadState.Deleted)
                    ThrowDeletedEntityException("Deleted entity found in atomic save.", entity);

            // We only attempt to save if all entities can be verified properly
            var verified = true;
            if (verifyBeforeSave)
                foreach (var ent in entities)
                {
                    ent.Verify();
                    if (ent.BrokenRules.Count > 0) verified = false;
                }

            if (!verified) return false;

            // We now proceed saving the entities
            if (entities.Length > 1)
            {
                // There are multiple entities, so we have to span transactions around them
                // (this is what this method is for, after all)

                // First, we need to make sure all these entities can share a data context
                // To do so, we share every entity's business object's data context with
                // the very first entity
                if (!(entities[0].AssociatedBusinessObject is BusinessObject)) throw new IncompatibleBusinessObjectException();
                var firstBusinessObject = (BusinessObject) entities[0].AssociatedBusinessObject;
                for (var entityCounter = 1; entityCounter < entities.Length; entityCounter++)
                {
                    if (!(entities[entityCounter].AssociatedBusinessObject is BusinessObject)) throw new IncompatibleBusinessObjectException();
                    var currentBusinessObject = (BusinessObject) entities[entityCounter].AssociatedBusinessObject;
                    if (!currentBusinessObject.ShareDataContext(firstBusinessObject)) throw new ContextSharingRestrictionViolatedException();
                }

                // Now that all the entities share a data context, we can start a transaction
                firstBusinessObject.SharedDataContext.BeginTransaction();
                try
                {
                    foreach (var ent in entities)
                        if (!ent.Save())
                        {
                            firstBusinessObject.SharedDataContext.AbortTransaction();
                            return false;
                        }

                    firstBusinessObject.SharedDataContext.CommitTransaction();
                }
                catch //(Exception oEx)
                {
                    firstBusinessObject.SharedDataContext.AbortTransaction();
                    throw new AtomicSaveFailedException();
                }
            }
            else if (entities.Length == 1)
                // There is only one entity, which is odd, but OK
                return entities[0].Save();
            else
                // No entities were passed. We consider that to be OK
                return true;

            return true;
        }

        /// <summary>
        ///     Returns the internal (mapped) field name and table name
        ///     that is used under the hood to store the actual data in the data set.
        /// </summary>
        /// <param name="exposedFieldName">External field name</param>
        /// <param name="exposedTableName">Table the field belongs to (external name)</param>
        /// <returns>Mapped field name</returns>
        public virtual string GetInternalFieldName(string exposedFieldName, string exposedTableName)
        {
            exposedTableName = GetInternalTableName(exposedTableName);
            var key = exposedTableName + ":" + exposedFieldName;
            if (fieldMaps.ContainsKey(key))
                exposedFieldName = fieldMaps[key];
            else if (!InternalDataSet.Tables[exposedTableName].Columns.Contains(exposedFieldName)) exposedFieldName = string.Empty;

            return exposedFieldName;
        }

        /// <summary>
        ///     Returns the internal (mapped) table name
        /// </summary>
        /// <param name="exposedTableName">External table name</param>
        /// <returns>Internal table name (as it appears in the database)</returns>
        public virtual string GetInternalTableName(string exposedTableName) => tableMaps.ContainsKey(exposedTableName) ? tableMaps[exposedTableName] : exposedTableName;

        /// <summary>
        ///     Maps an externally visible table name to an internal name
        /// </summary>
        /// <param name="exposedTableName">Table name used by the entity</param>
        /// <param name="internalTableName">Table name as it appears in the database</param>
        protected virtual void SetTableMap(string exposedTableName, string internalTableName)
        {
            if (tableMaps.ContainsKey(exposedTableName))
                tableMaps[exposedTableName] = internalTableName;
            else
                tableMaps.Add(exposedTableName, internalTableName);
        }

        /// <summary>
        ///     Maps an externally visible table name to an internal name
        /// </summary>
        /// <param name="exposedFieldName">Field name used by the entity</param>
        /// <param name="internalFieldName">Field name as it appears in the database</param>
        /// <param name="exposedTableName">Table name used externally (by the entity)</param>
        protected virtual void SetFieldMap(string exposedFieldName, string internalFieldName, string exposedTableName)
        {
            var key = GetInternalTableName(exposedTableName) + ":" + exposedFieldName;
            if (fieldMaps.ContainsKey(key))
                fieldMaps[key] = internalFieldName;
            else
                fieldMaps.Add(key, internalFieldName);
        }

        /// <summary>
        ///     Maps an externally visible table name to an internal name
        /// </summary>
        /// <param name="exposedFieldName">Field name used by the entity</param>
        /// <param name="internalFieldName">Field name as it appears in the database</param>
        protected virtual void SetFieldMap(string exposedFieldName, string internalFieldName)
        {
            var exposedTableName = MasterEntity;
            var key = GetInternalTableName(exposedTableName) + ":" + exposedFieldName;
            if (fieldMaps.ContainsKey(key))
                fieldMaps[key] = internalFieldName;
            else
                fieldMaps.Add(key, internalFieldName);
        }

        /// <summary>
        ///     Use this method to retrieve a value from the internal data store
        /// </summary>
        /// <param name="fieldName">Name of the field that is to be returned</param>
        /// <param name="tableName">Name of the table the field is in</param>
        /// <param name="ignoreNulls">
        ///     Should null values be ignored and returned as null (true) or should they be turned into
        ///     default values (false)?
        /// </param>
        /// <returns>Value object</returns>
        [Obsolete("Use ReadFieldValue<T>() instead.")]
        protected virtual object GetFieldValue(string fieldName, string tableName = null, bool ignoreNulls = false)
        {
            if (tableName == null) tableName = MasterEntity;
            return BusinessEntityHelper.GetFieldValue<object>(this, InternalDataSet, tableName, fieldName, 0, ignoreNulls);
        }

        /// <summary>
        ///     Use this method to retrieve a value from the internal data store
        /// </summary>
        /// <typeparam name="TField">The expected return type for the field.</typeparam>
        /// <param name="fieldName">Name of the field that is to be returned</param>
        /// <param name="tableName">Name of the table the field is in</param>
        /// <param name="ignoreNulls">
        ///     Should null values be ignored and returned as null (true) or should they be turned into
        ///     default values (false)?
        /// </param>
        /// <returns>Value</returns>
        protected virtual TField ReadFieldValue<TField>(string fieldName, string tableName = null, bool ignoreNulls = false)
        {
            if (tableName == null) tableName = MasterEntity;
            return BusinessEntityHelper.GetFieldValue<TField>(this, InternalDataSet, tableName, fieldName, 0, ignoreNulls);
        }

        /// <summary>
        ///     Use this method to assign any value to the internal data source
        /// </summary>
        /// <param name="fieldName">Name of the field that is to be assigned</param>
        /// <param name="value">Value that is to be assigned</param>
        /// <param name="tableName">Name of the table the field is in</param>
        /// <param name="forceSet">
        ///     Should the value be set, even if it is the same as before? (Causes the object to be dirty,
        ///     possibly without changes)
        /// </param>
        /// <returns>True if update succeeded</returns>
        [Obsolete("Use WriteFieldValue<T>() instead.")]
        protected virtual bool SetFieldValue(string fieldName, object value, string tableName = null, bool forceSet = false)
        {
            if (tableName == null) tableName = MasterEntity;
            return BusinessEntityHelper.SetFieldValue(this, fieldName, value, tableName, InternalDataSet, 0, forceSet);
        }

        /// <summary>
        ///     Use this method to assign any value to the internal data source
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field that is to be assigned</param>
        /// <param name="value">Value that is to be assigned</param>
        /// <param name="tableName">Name of the table the field is in</param>
        /// <param name="forceSet">
        ///     Should the value be set, even if it is the same as before? (Causes the object to be dirty,
        ///     possibly without changes)
        /// </param>
        /// <returns>True if update succeeded</returns>
        protected virtual bool WriteFieldValue<TField>(string fieldName, TField value, string tableName = null, bool forceSet = false)
        {
            if (tableName == null) tableName = MasterEntity;
            return BusinessEntityHelper.SetFieldValue(this, fieldName, value, tableName, InternalDataSet, 0, forceSet);
        }

        /// <summary>
        ///     This method can be used to make sure the default table in the internal recordset has all the required fields.
        ///     If the field (column) doesn't exist, it will be added.
        /// </summary>
        /// <param name="fieldName">Field name to check for.</param>
        /// <param name="tableName">Name of the table the field is in</param>
        /// <param name="value">
        ///     The value that will go on the column. This will determine what type the column must be
        ///     in case it has to be created on-the-fly.
        /// </param>
        /// <returns></returns>
        protected virtual bool CheckColumn(string fieldName, string tableName = null, object value = null)
        {
            if (tableName == null) tableName = MasterEntity;
            return BusinessEntityHelper.CheckColumn(InternalDataSet, fieldName, tableName, value);
        }

        /// <summary>
        ///     Checks whether a certain column exists in a table of a data set
        /// </summary>
        /// <param name="fieldName">Field Name</param>
        /// <param name="table">Table object</param>
        /// <remarks>Field and table names used here must be INTERNAL names, not mapped names.</remarks>
        /// <returns>True (if field exist or has been added) or False </returns>
        protected virtual bool CheckColumn(string fieldName, DataTable table) => BusinessEntityHelper.CheckColumn(table, fieldName);

        /// <summary>
        ///     Checks whether a certain table has the specified minimum number of rows.
        ///     If the table doesn't have the specified minimum number of rows (and
        ///     autoAddRows is passed as true), the rows are automatically created.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="primaryKeyField">The primary key field.</param>
        /// <param name="minimumRowCount">The minimum row count.</param>
        /// <param name="autoAddRows">If set to <c>true</c>, adds missing rows automatically.</param>
        /// <returns>
        ///     True if the table has the appropriate number of rows (or the appropriate number of rows has been added)
        /// </returns>
        protected virtual bool CheckRows(string tableName, string primaryKeyField, int minimumRowCount, bool autoAddRows) => BusinessEntityHelper.CheckRows(tableName, primaryKeyField, minimumRowCount, autoAddRows, InternalDataSet, this);

        /// <summary>
        ///     Checks the number of rows in the specified table.
        ///     Removes all data if the table has any.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <returns>True if successful</returns>
        public virtual bool ClearRows(string tableName) => BusinessEntityHelper.ClearRows(InternalDataSet, tableName);

        /// <summary>
        ///     Checks whether a certain table has at least 1 data row.
        ///     If the table doesn't have the specified minimum number of rows (and
        ///     autoAddRows is passed as true), a new row is automatically added.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="primaryKeyField">The primary key field.</param>
        /// <param name="autoAddRows">If set to <c>true</c>, a missing row is added automatically.</param>
        /// <returns>
        ///     True if the table has at least 1 row (or the rows has been added successfully)
        /// </returns>
        protected virtual bool CheckRows(string tableName, string primaryKeyField, bool autoAddRows = true) => CheckRows(tableName, primaryKeyField, 1, autoAddRows);

        /// <summary>
        ///     Checks whether the value is valid based on the definition of the field in the provided data table.
        /// </summary>
        /// <param name="table">Data table</param>
        /// <param name="fieldName">Field name within the data table</param>
        /// <param name="value">Value</param>
        /// <remarks>Field and table names used here must be INTERNAL names, not mapped names.</remarks>
        /// <returns>True if invalid, false otherwise</returns>
        protected internal virtual bool IsValueInvalid(DataTable table, string fieldName, object value) => BusinessEntityHelper.IsValueInvalid(table, fieldName, value, this);

        /// <summary>
        ///     Fixes the value to be valid based on the current field type
        /// </summary>
        /// <param name="table">Data table</param>
        /// <param name="fieldName">Field name within the data table</param>
        /// <param name="value">Value</param>
        /// <remarks>Field and table names used here must be INTERNAL names, not mapped names.</remarks>
        /// <returns>Valid value</returns>
        protected internal virtual object GetValidValue(DataTable table, string fieldName, object value) => BusinessEntityHelper.GetValidValue(table, fieldName, value, this);

        /// <summary>
        ///     Checks whether two field values are the same or not
        /// </summary>
        /// <param name="value1">First value</param>
        /// <param name="value2">Second value</param>
        /// <returns>True of they are different, false if they are the same</returns>
        protected internal virtual bool ValuesDiffer(object value1, object value2) => ObjectHelper.ValuesDiffer(value1, value2);

        /// <summary>
        ///     Resets the last update tick
        /// </summary>
        /// <param name="fieldName">Updated field name</param>
        /// <param name="tableName">Updated table name</param>
        protected internal virtual void DataUpdated(string fieldName, string tableName)
        {
            isDirtyOverride = false; // The override can only exist until more changes are made.

            // We fire a generic DataSourceChanged event
            DataSourceChanged?.Invoke(this, new EventArgs());

            // We fire a more specific event that also includes the field and table name in the event args
            DataSourceChangedWithDetails?.Invoke(this, new DataSourceChangedEventArgs(fieldName, tableName));

            // We also support the PropertyChanged event
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }
    }
}