namespace Milos.Business.Names;

/// <summary>
/// This class represents a single name from the names table.
/// </summary>
public class NameBusinessEntity : BusinessEntity, INameEntity
{
    /// <summary>
    /// Required Constructor
    /// </summary>
    /// <remarks>Public constructor is supported for backwards compatibility only.</remarks>
    public NameBusinessEntity() { }

    /// <summary>
    /// Required Constructor
    /// </summary>
    /// <param name="id">ID</param>
    /// <remarks>Public constructor is supported for backwards compatibility only.</remarks>
    public NameBusinessEntity(Guid id) : base(id) { }

    /// <summary>
    /// Required Constructor
    /// </summary>
    /// <param name="nameDateSet">DataSet</param>
    /// <remarks>Public constructor is supported for backwards compatibility only.</remarks>
    public NameBusinessEntity(DataSet nameDateSet) : base(nameDateSet) { }

    /// <summary>
    /// Static loader method
    /// </summary>
    /// <param name="id">Name id</param>
    /// <returns>Existing name entity</returns>
    public static NameBusinessEntity LoadEntity(Guid id) => new NameBusinessEntity(id);

    /// <summary>
    /// Static new method
    /// </summary>
    /// <returns>New name entity</returns>
    public static NameBusinessEntity NewEntity() => new NameBusinessEntity();

    /// <summary>
    /// Used to configure the object
    /// </summary>
    protected override void Configure()
    {
        // Fields in the master entity
        SetFieldMap("LastName", "clastname");
        SetFieldMap("FirstName", "cfirstname");
        SetFieldMap("MiddleName", "cmiddlename");
        SetFieldMap("Prefix", "cprefix");
        SetFieldMap("Suffix", "csuffix");
        SetFieldMap("SearchName", "csearchname");
        SetFieldMap("Company", "ccompanyname");
        SetFieldMap("Title", "ctitle");
        SetFieldMap("BirthDate", "dbirthdate");
        SetFieldMap("Source", "csource");

        // Address fields
        SetFieldMap("Street", "cStreet", "Address");
        SetFieldMap("Street2", "cStreet2", "Address");
        SetFieldMap("Street3", "cStreet3", "Address");
        SetFieldMap("City", "cCity", "Address");
        SetFieldMap("State", "cState", "Address");
        SetFieldMap("ZIP", "cZip", "Address");
        SetFieldMap("AddressName", "cAddressName", "Address");
        SetFieldMap("AddressCompany", "cAddressCompany", "Address");
        SetFieldMap("Type", "cType", "Address");
    }

    /// <summary>
    /// Returns the value of a certain field
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <param name="tableName">Table name</param>
    /// <returns>Value</returns>
    /// <remarks>This method has been designed to be called from friend objects.</remarks>
    [Obsolete("Use ReadFieldValueEx<T>() instead.")]
    internal object GetFieldValueEx(string fieldName, string tableName) => GetFieldValue(fieldName, tableName);

    /// <summary>
    /// Sets a field value
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <param name="value">Value</param>
    /// <param name="tableName">Table name</param>
    /// <remarks>This method has been designed to be called from friend objects.</remarks>
    [Obsolete("Use WriteFieldValueEx<T>() instead.")]
    internal void SetFieldValueEx(string fieldName, object value, string tableName) => SetFieldValue(fieldName, value, tableName);

    /// <summary>
    /// Returns the value of a certain field
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <param name="tableName">Table name</param>
    /// <returns>Value</returns>
    /// <remarks>This method has been designed to be called from friend objects.</remarks>
    internal T ReadFieldValueEx<T>(string fieldName, string tableName) => ReadFieldValue<T>(fieldName, tableName);

    /// <summary>
    /// Sets a field value
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <param name="value">Value</param>
    /// <param name="tableName">Table name</param>
    /// <remarks>This method has been designed to be called from friend objects.</remarks>
    internal void WriteFieldValueEx<T>(string fieldName, T value, string tableName) => WriteFieldValue(fieldName, value, tableName);

    /// <summary>
    /// Loads all the required collections
    /// </summary>
    protected override void LoadSubItemCollections()
    {
        base.LoadSubItemCollections();

        LoadNameAddressCollections();
        LoadNameCommInfoCollection();
        LoadNameCategoryXLinkCollection();
    }

    /// <summary>
    /// This method loads the name/address specific collections.
    /// This method can be overridden in subclasses.
    /// </summary>
    protected virtual void LoadNameAddressCollections()
    {
        // We generate an address collection
        var bizName = (NameBusinessObject) AssociatedBusinessObject;
        Addresses = new NameAddressCollection(this);
        Addresses.SetTable(GetInternalData().Tables[bizName.PlacementTable], GetInternalData().Tables[bizName.AddressTable]);
    }

    /// <summary>
    /// This method loads the comm info collection
    /// This method can be overridden in subclasses.
    /// </summary>
    protected virtual void LoadNameCommInfoCollection()
    {
        // We generate a comm-info collection
        var bizName = (NameBusinessObject) AssociatedBusinessObject;
        CommunicationInfo = new NameCommInfoCollection(this);
        CommunicationInfo.SetTable(GetInternalData().Tables[bizName.CommAssignmentTable], GetInternalData().Tables[bizName.CommInfoTable]);
    }

    /// <summary>
    /// This method loads the name/category collection
    /// This method can be overridden in subclasses.
    /// </summary>
    protected virtual void LoadNameCategoryXLinkCollection()
    {
        // We generate the categories table
        var bizName = (NameBusinessObject) AssociatedBusinessObject;
        Categories = new NameCategoryXLinkCollection(this);
        Categories.SetTable(GetInternalData().Tables[bizName.NameCategoryAssignmentTable], GetInternalData().Tables["namecategory"]);
    }

    // The following is a list of all the exposed properties

    #region Properties

    /// <summary>
    /// Addresses related to this name
    /// </summary>
    public virtual INameAddressCollection Addresses { get; private set; }

    /// <summary>
    /// Email, phone numbers,... related to this name
    /// </summary>
    public virtual NameCommInfoCollection CommunicationInfo { get; private set; }

    /// <summary>
    /// Categories this name is assigned to
    /// </summary>
    public virtual NameCategoryXLinkCollection Categories { get; private set; }

    /// <summary>
    /// Combines and parses all the name parts
    /// </summary>
    public virtual string FullName
    {
        get
        {
            var name = Prefix.Trim() + " " + FirstName.Trim() + " " + MiddleName.Trim() + " " + LastName.Trim() + " " + Suffix.Trim(); 
            name = name.Replace("  ", " ").Trim();
            return name;
        }
        set => LastName = value;
    }

    /// <summary>
    /// Last Name
    /// </summary>
    public virtual string LastName
    {
        get => ReadFieldValue<string>("LastName");
        set => WriteFieldValue("LastName", value);
    }

    /// <summary>
    /// Last Name
    /// </summary>
    public virtual string Source
    {
        get => ReadFieldValue<string>("Source");
        set => WriteFieldValue("Source", value);
    }

    /// <summary>
    /// First Name
    /// </summary>
    public virtual string FirstName
    {
        get => ReadFieldValue<string>("FirstName");
        set => WriteFieldValue("FirstName", value);
    }

    /// <summary>
    /// Last Name
    /// </summary>
    public virtual string MiddleName
    {
        get => ReadFieldValue<string>("MiddleName");
        set => WriteFieldValue("MiddleName", value);
    }

    /// <summary>
    /// Prefix
    /// </summary>
    public virtual string Prefix
    {
        get => ReadFieldValue<string>("Prefix");
        set => WriteFieldValue("Prefix", value);
    }

    /// <summary>
    /// Suffix
    /// </summary>
    public virtual string Suffix
    {
        get => ReadFieldValue<string>("Suffix");
        set => WriteFieldValue("Suffix", value);
    }

    /// <summary>
    /// Search Name
    /// </summary>
    public virtual string SearchName
    {
        get => ReadFieldValue<string>("SearchName");
        set => WriteFieldValue("SearchName", value);
    }

    /// <summary>
    /// Company name
    /// </summary>
    public virtual string Company
    {
        get => ReadFieldValue<string>("Company");
        set => WriteFieldValue("Company", value);
    }

    /// <summary>
    /// Title
    /// </summary>
    public virtual string Title
    {
        get => ReadFieldValue<string>("Title");
        set => WriteFieldValue("Title", value);
    }

    /// <summary>
    /// Birthdate
    /// </summary>
    public virtual string BirthDate
    {
        get
        {
            // We convert the SQL Server DateTime field to a simple string
            try
            {
                return ReadFieldValue<DateTime>("dBirthDate").ToShortDateString();
            }
            catch
            {
                return string.Empty;
            }
        }
        set
        {
            // We allow to assign a string, but use a DateTime object to verify that the string is a valid date
            if (value.Length == 0)
                WriteFieldValue("dBirthDate", DBNull.Value);
            else
            {
                if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var birthday)) birthday = DateTime.MinValue;
                WriteFieldValue("dBirthDate", birthday);
            }
        }
    }

    /// <summary>
    /// Gets or sets the birthday (empty birthdays are returned as DateTime.MinValue).
    /// </summary>
    /// <value>The birthday.</value>
    public DateTime Birthday
    {
        get
        {
            try
            {
                return ReadFieldValue<DateTime>("dBirthDate");
            }
            catch
            {
                return DateTime.MinValue;
            }
        }
        set => WriteFieldValue("dBirthDate", value);
    }

    #endregion

    /// <summary>
    /// Returns a name business object
    /// </summary>
    /// <returns></returns>
    public override IBusinessObject GetBusinessObject() => new NameBusinessObject();
}

/// <summary>
/// Basic interface for name entities
/// </summary>
public interface INameEntity
{
    /// <summary>
    /// Collection of Addresses
    /// </summary>
    INameAddressCollection Addresses { get; }

    /// <summary>
    /// Collection of Communication Information
    /// </summary>
    NameCommInfoCollection CommunicationInfo { get; }

    /// <summary>
    /// Collection of Categories
    /// </summary>
    NameCategoryXLinkCollection Categories { get; }

    /// <summary>
    /// Full Name
    /// </summary>
    string FullName { get; set; }

    /// <summary>
    /// Last Name
    /// </summary>
    string LastName { get; set; }

    /// <summary>
    /// First Name
    /// </summary>
    string FirstName { get; set; }

    /// <summary>
    /// Middle Name
    /// </summary>
    string MiddleName { get; set; }

    /// <summary>
    /// Prefix
    /// </summary>
    string Prefix { get; set; }

    /// <summary>
    /// Suffix
    /// </summary>
    string Suffix { get; set; }

    /// <summary>
    /// Search Name
    /// </summary>
    string SearchName { get; set; }

    /// <summary>
    /// Company Name
    /// </summary>
    string Company { get; set; }

    /// <summary>
    /// Title
    /// </summary>
    string Title { get; set; }

    /// <summary>
    /// Birth date
    /// </summary>
    string BirthDate { get; set; }

    /// <summary>
    /// Source
    /// </summary>
    /// <value>The source.</value>
    string Source { get; set; }
}

[Obsolete("Use NameBusinessEntity instead.")]
public class NameEntity : NameBusinessEntity
{
    public NameEntity() { }
    public NameEntity(Guid id) : base(id) { }
    public NameEntity(DataSet nameDateSet) : base(nameDateSet) { }
    public new static NameEntity LoadEntity(Guid id) => new(id);
    public new static NameEntity NewEntity() => new();
}