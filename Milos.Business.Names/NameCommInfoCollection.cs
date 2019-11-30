using Milos.BusinessObjects;

namespace Milos.Business.Names
{
    /// <summary>
    /// Summary description for NameAddressCollection.
    /// </summary>
    public class NameCommInfoCollection : EntityXlinkSubItemCollection
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="parentEntity">Parent Entity</param>
        public NameCommInfoCollection(IBusinessEntity parentEntity) : base(parentEntity) { }

        /// <summary>
        /// The indexer must be overridden to return the appropriate type
        /// </summary>
        public new INameCommInfoEntity this[int index] => (INameCommInfoEntity) GetItemByIndex(index);

        /// <summary>
        /// Configures the object to operate seamlessly, without much additional code required
        /// </summary>
        protected override void Configure()
        {
            PrimaryKeyField = "pk_commassignment";
            ForeignKeyField = "fk_name";
            ParentTableName = "names";
            ParentTablePrimaryKeyField = "pk_name";
            TargetForeignKeyField = "fk_comminfo";
            TargetPrimaryKeyField = "pk_comminfo";
            AutoAddTarget = true;
        }

        /// <summary>
        /// his is needed for the collection to generate and serve up new item instances
        /// </summary>
        /// <returns>Item Object</returns>
        public override IEntitySubItemCollectionItem GetItemObject() => new NameCommInfoEntity(this);

        /// <summary>
        /// Adds communication information
        /// </summary>
        /// <param name="type">Type</param>
        /// <param name="value">Value</param>
        /// <returns>Added entity</returns>
        public INameCommInfoEntity Add(CommInfoType type, string value)
        {
            var entCommInfo = (INameCommInfoEntity) Add();
            entCommInfo.Type = type;
            entCommInfo.Value = value;
            return entCommInfo;
        }
    }

    /// <summary>
    /// This address type will be stored in the database (in the crosslink table)
    /// </summary>
    /// <remarks>
    /// CL on 09/20/2005: Added Blog and BlogRss, as per Markus' request.
    /// </remarks>
    public enum CommInfoType
    {
        /// <summary>
        /// Phone
        /// </summary>
        Phone = 0,

        /// <summary>
        /// Fax
        /// </summary>
        Fax = 1,

        /// <summary>
        /// Email
        /// </summary>
        Email = 2,

        /// <summary>
        /// Web site
        /// </summary>
        WebSite = 3,

        /// <summary>
        /// Home Phone
        /// </summary>
        HomePhone = 4,

        /// <summary>
        /// Home Fax
        /// </summary>
        HomeFax = 5,

        /// <summary>
        /// Mobile Phone
        /// </summary>
        MobilePhone = 6,

        /// <summary>
        /// Pager
        /// </summary>
        Pager = 7,

        /// <summary>
        /// Text Messaging
        /// </summary>
        TextMessaging = 8,

        /// <summary>
        /// Blog.
        /// </summary>
        Blog = 9,

        /// <summary>
        /// Blog RSS.
        /// </summary>
        BlogRss = 10,

        /// <summary>
        /// Twitter handle (such as Milos for twitter.com/Milos
        /// </summary>
        TwitterHandle = 11,

        /// <summary>
        /// Facebook name (such as Milos for facebook.com/Milos
        /// </summary>
        FacebookName = 12,

        /// <summary>
        /// Numeric Facebook ID
        /// </summary>
        FacebookId = 13,

        /// <summary>
        /// LinkedIn handle (such as Milos for linkedin.com/in/Milos
        /// </summary>
        LinkedInHandle = 14,

        /// <summary>
        /// Windows Live ID (usually an email address)
        /// </summary>
        WindowsLiveId = 15,

        /// <summary>
        /// Xing Handle (such as Milos for xing.com/profile/Milos)
        /// </summary>
        XingHandle = 16,

        /// <summary>
        /// Flicker ID
        /// </summary>
        FlickrId = 17,

        /// <summary>
        /// YouTube handle (such as Milos for youtube.com/user/Milos)
        /// </summary>
        YouTubeHandle = 18
    }

    /// <summary>
    /// Basic interface for comm info entities
    /// </summary>
    public interface INameCommInfoEntity
    {
        /// <summary>
        /// Comm info type (email, phone,...)
        /// </summary>
        CommInfoType Type { get; set; }

        /// <summary>
        /// Phone number (only available if this is a phone number)
        /// </summary>
        string Phone { get; set; }

        /// <summary>
        /// Fax number (only available if this is a fax number)
        /// </summary>
        string Fax { get; set; }

        /// <summary>
        /// Email address (only available if this is an email address)
        /// </summary>
        string Email { get; set; }

        /// <summary>
        /// URL (only available of this is a web address)
        /// </summary>
        string WebSite { get; set; }

        /// <summary>
        /// Value information (phone number, email address,...)
        /// </summary>
        string Value { get; set; }
    }

    /// <summary>
    /// Sub-Items for different communication information
    /// </summary>
    public class NameCommInfoEntity : EntitySubItemCollectionXLinkItem, INameCommInfoEntity
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="parentCollection">Parent collection</param>
        public NameCommInfoEntity(IEntitySubItemCollection parentCollection) : base(parentCollection) { }

        /// <summary>
        /// Comm info type (email, phone,...)
        /// </summary>
        public CommInfoType Type
        {
            get
            {
                try
                {
                    return ReadFieldValue<CommInfoType>("iCommType", XLinkItemAccessMode.TargetTable);
                }
                catch
                {
                    // Not sure what else to do by default
                    return CommInfoType.Email;
                }
            }
            set => WriteFieldValue("iCommType", value, XLinkItemAccessMode.TargetTable);
        }

        /// <summary>
        /// Phone number (only available if this is a phone number)
        /// </summary>
        public string Phone
        {
            get
            {
                if (Type == CommInfoType.Phone)
                    return ReadFieldValue<string>("cvalue", XLinkItemAccessMode.TargetTable);
                return "";
            }
            set
            {
                WriteFieldValue("cvalue", value, XLinkItemAccessMode.TargetTable);
                Type = CommInfoType.Phone;
            }
        }

        /// <summary>
        /// Fax number (only available if this is a fax number)
        /// </summary>
        public string Fax
        {
            get
            {
                if (Type == CommInfoType.Fax)
                    return ReadFieldValue<string>("cvalue", XLinkItemAccessMode.TargetTable);
                return "";
            }
            set
            {
                WriteFieldValue("cvalue", value, XLinkItemAccessMode.TargetTable);
                Type = CommInfoType.Fax;
            }
        }

        /// <summary>
        /// Email address (only available if this is an email address)
        /// </summary>
        public string Email
        {
            get => Type == CommInfoType.Email ? ReadFieldValue<string>("cvalue", XLinkItemAccessMode.TargetTable) : string.Empty;
            set
            {
                WriteFieldValue("cvalue", value, XLinkItemAccessMode.TargetTable);
                Type = CommInfoType.Email;
            }
        }

        /// <summary>
        /// URL (only available of this is a web address)
        /// </summary>
        public string WebSite
        {
            get => Type == CommInfoType.WebSite ? ReadFieldValue<string>("cvalue", XLinkItemAccessMode.TargetTable) : "";
            set
            {
                WriteFieldValue("cvalue", value, XLinkItemAccessMode.TargetTable);
                Type = CommInfoType.WebSite;
            }
        }

        /// <summary>
        /// Value information (phone number, email address,...)
        /// </summary>
        public string Value
        {
            get => ReadFieldValue<string>("cvalue", XLinkItemAccessMode.TargetTable);
            set => WriteFieldValue("cvalue", value, XLinkItemAccessMode.TargetTable);
        }
    }
}