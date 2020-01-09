using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;
using Milos.BusinessObjects;

namespace Milos.Business.DocumentManagement
{
    /// <summary>Business Entity for FileAttachments.</summary>
    public class FileAttachmentsBusinessEntity : BusinessEntity
    {
        private static readonly List<FileSystemWatcher> Watchers = new List<FileSystemWatcher>();

        private static readonly Dictionary<string, WatcherInfo> WatchedFiles = new Dictionary<string, WatcherInfo>();
        private static Timer _fileWatcherTimer;

        /// <summary>
        ///     Constructor - No Parameters
        ///     The constructor is private.  Use the Static Methods for object creation
        /// </summary>
        public FileAttachmentsBusinessEntity()
        {
        }

        /// <summary>
        ///     Constructor - Pk Parameter
        ///     The constructor is private.  Use the Static Methods for object creation
        /// </summary>
        /// <param name="id">Primary Key</param>
        public FileAttachmentsBusinessEntity(Guid id) : base(id)
        {
        }

        /// <summary>
        ///     Property for Assignments Collection
        /// </summary>
        public AssignmentsCollection Assignments { get; private set; }

        /// <summary>
        ///     Attaches the specified file
        /// </summary>
        /// <param name="fileName">File to attach</param>
        /// <returns>True if successful</returns>
        public virtual bool AttachFile(string fileName) => AttachFile(fileName, FileAttachmentIndex.FirstAttachment);

        ///// <summary>
        /////     Attaches the specified file from a web upload
        ///// </summary>
        ///// <param name="uploadFile">Http posted file</param>
        ///// <returns>True if successful</returns>
        //public virtual bool AttachFile(HttpPostedFile uploadFile)
        //{
        //    return AttachFile(uploadFile, FileAttachmentIndex.FirstAttachment);
        //}

        ///// <summary>
        /////     Attaches the specified file from a web upload
        ///// </summary>
        ///// <param name="uploadFile">Http posted file</param>
        ///// <param name="attachmentNumber">Attachment number/index</param>
        ///// <returns>True if successful</returns>
        //public virtual bool AttachFile(HttpPostedFile uploadFile, FileAttachmentIndex attachmentNumber)
        //{
        //    if (uploadFile.ContentLength < 1) throw new FileNotFoundException("Specified upload file does not exist (posted content has a length of 0 bytes).", uploadFile.FileName);

        //    var fileBytes = new byte[uploadFile.ContentLength];
        //    uploadFile.InputStream.Read(fileBytes, 0, uploadFile.ContentLength);

        //    // We can now assign the values to the current object
        //    switch (attachmentNumber)
        //    {
        //        case FileAttachmentIndex.FirstAttachment:
        //            Attachment = fileBytes;
        //            AttachDate = DateTime.Now.ToUniversalTime();
        //            FileDate = DateTime.Now.ToUniversalTime();
        //            FileName = JustFileName(uploadFile.FileName);
        //            Type = JustExtension(uploadFile.FileName);
        //            if (Title.Length == 0) Title = FileName;
        //            break;
        //        case FileAttachmentIndex.SecondAttachment:
        //            Attachment2 = fileBytes;
        //            FileDate2 = DateTime.Now.ToUniversalTime();
        //            FileName2 = JustFileName(uploadFile.FileName);
        //            Type2 = JustExtension(uploadFile.FileName);
        //            break;
        //        case FileAttachmentIndex.ThirdAttachment:
        //            Attachment3 = fileBytes;
        //            FileDate3 = DateTime.Now.ToUniversalTime();
        //            FileName3 = JustFileName(uploadFile.FileName);
        //            Type3 = JustExtension(uploadFile.FileName);
        //            break;
        //    }

        //    return true;
        //}

        ///// <summary>
        /////     Returns the file name of a full path
        ///// </summary>
        ///// <param name="fullName">Fully qualified file name</param>
        ///// <returns>File name</returns>
        //private static string JustFileName(string fullName)
        //{
        //    var occ = StringHelper.Occurs('\\', fullName);
        //    if (occ > 0) fullName = fullName.Substring(StringHelper.At("\\", fullName, occ));
        //    return fullName;
        //}

        ///// <summary>
        /////     Returns the extension of a full path/file
        ///// </summary>
        ///// <param name="fullName">Fully qualified file name</param>
        ///// <returns>File extension</returns>
        //private static string JustExtension(string fullName)
        //{
        //    var occ = StringHelper.Occurs('.', fullName);
        //    if (occ > 0) fullName = fullName.Substring(StringHelper.At(".", fullName, occ));
        //    return fullName;
        //}

        /// <summary>
        ///     Attaches the specified file
        /// </summary>
        /// <param name="fileName">File to attach</param>
        /// <param name="attachmentNumber">Attachment number/index</param>
        /// <returns>True if successful</returns>
        public virtual bool AttachFile(string fileName, FileAttachmentIndex attachmentNumber)
        {
            if (!File.Exists(fileName)) throw new FileNotFoundException("Specified file does not exist and can therefore not be attached.", fileName);

            try
            {
                using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    var fileData = new byte[fileStream.Length];
                    fileStream.Read(fileData, 0, (int) fileStream.Length);
                    fileStream.Close();
                    return AttachFile(fileData, fileName, attachmentNumber);
                }
            }
            catch (IOException)
            {
                return false;
            }
        }

        /// <summary>
        ///     Attaches the specified bytes as the file.
        /// </summary>
        /// <param name="fileData">The file data (bytes).</param>
        /// <param name="fileName">Name of the file.</param>
        /// <returns></returns>
        public bool AttachFile(byte[] fileData, string fileName) => AttachFile(fileData, fileName, FileAttachmentIndex.FirstAttachment);

        /// <summary>
        ///     Attaches the specified bytes as the file.
        /// </summary>
        /// <param name="fileData">The file data (bytes).</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="attachmentNumber">The attachment number.</param>
        /// <returns></returns>
        public bool AttachFile(byte[] fileData, string fileName, FileAttachmentIndex attachmentNumber)
        {
            var fileInfo = new FileInfo(fileName);

            // We can now assign the values to the current object
            switch (attachmentNumber)
            {
                case FileAttachmentIndex.FirstAttachment:
                    Attachment = fileData;
                    AttachDate = DateTime.Now.ToUniversalTime();
                    FileDate = File.GetCreationTime(fileName);
                    FileName = fileInfo.Name;
                    Type = fileInfo.Extension;
                    if (Title.Length == 0) Title = fileInfo.Name;
                    break;
                case FileAttachmentIndex.SecondAttachment:
                    Attachment2 = fileData;
                    FileDate2 = File.GetCreationTime(fileName);
                    FileName2 = fileInfo.Name;
                    Type2 = fileInfo.Extension;
                    break;
                case FileAttachmentIndex.ThirdAttachment:
                    Attachment3 = fileData;
                    FileDate3 = File.GetCreationTime(fileName);
                    FileName3 = fileInfo.Name;
                    Type3 = fileInfo.Extension;
                    break;
            }

            return true;
        }

        /// <summary>
        ///     Opens the primary (first) attachment for editing.
        /// </summary>
        public virtual void EditAttachment() => EditAttachment(FileAttachmentIndex.FirstAttachment, true);

        /// <summary>
        ///     Opens the specified attachment for editing
        /// </summary>
        /// <param name="index">Attachment index</param>
        /// <param name="autoSaveChanges">Should the object monitor changes on the edited file and save them back to the database?</param>
        public virtual void EditAttachment(FileAttachmentIndex index, bool autoSaveChanges)
        {
            var fName = Environment.TickCount.ToString(CultureInfo.InvariantCulture);
            // We use the temp internet path, which is convenient since the OS takes care of deleting files
            var tempPath = Environment.GetFolderPath(Environment.SpecialFolder.InternetCache) + @"\Milos";
            if (!Directory.Exists(tempPath)) Directory.CreateDirectory(tempPath);
            var tempFileName = tempPath + @"\" + fName + "." + Type;
            SaveAttachmentAs(index, tempFileName);
            if (autoSaveChanges)
            {
                // We use a file watcher to monitor changes
                var watcher = new FileSystemWatcher(tempPath, fName + "." + Type) { IncludeSubdirectories = false, NotifyFilter = NotifyFilters.Attributes | NotifyFilters.CreationTime | NotifyFilters.LastWrite };
                watcher.Changed += OnEditedFileChanged;
                watcher.EnableRaisingEvents = true;
                Watchers.Add(watcher);
                WatchedFiles.Add(tempFileName, new WatcherInfo(PK, index, this, tempFileName));

                // We also use a timer to find out when the file has been closed 
                // and we can do a final save
                if (_fileWatcherTimer == null) _fileWatcherTimer = new Timer(FileWatcherTimerTick, null, 10000, 10000);
            }

            Process.Start(tempFileName);
        }

        /// <summary>
        ///     Reacts to timer events
        /// </summary>
        /// <param name="status">Status (unused)</param>
        private static void FileWatcherTimerTick(object status)
        {
            // We basically suspend the timer
            _fileWatcherTimer.Change(10000000, 10000);

            var iCount = WatchedFiles.Count;
            var iCounter2 = -1;
            var keysArray = new string[WatchedFiles.Keys.Count];
            WatchedFiles.Keys.CopyTo(keysArray, 0);
            for (var iCounter = 0; iCounter < iCount; iCounter++)
            {
                iCounter2++;
                var oInfo = WatchedFiles[keysArray[iCounter2]];
                var fileName = oInfo.WatchedFileName;

                // We are only interested in this item in case it has changes that need to be saved
                if (oInfo.HasUnsavedChanges && File.Exists(fileName))
                {
                    // First, we check whether we can get read/write access to the file,
                    // so we know whether some other app still has it open
                    bool bIsAvailable;
                    try
                    {
                        var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite);
                        fileStream.Close();
                        bIsAvailable = true;
                    }
                    catch
                    {
                        bIsAvailable = false;
                    }

                    if (bIsAvailable)
                        if (oInfo.AttachmentEntity.AttachFile(fileName, oInfo.AttachmentIndex))
                            oInfo.AttachmentEntity.Save();
                }

                // Perhaps we can now also perform some cleanup
                if (!File.Exists(fileName) || oInfo.LaunchTime < DateTime.Now.AddDays(-1))
                {
                    // The file has either been deleted, or it is more than a day old. We won't keep monitoring it.

                    // First, we remove the watcher
                    Watchers[iCounter].Dispose();
                    Watchers.RemoveAt(iCounter);
                    // We now remove the entry in the watcher info table
                    WatchedFiles.Remove(fileName);
                    // We reduce the counters by one, so we do not throw off the loop
                    iCounter--;
                    iCount--;
                }

                if (Watchers.Count < 1)
                {
                    // There are no more watchers left. We can kill the timer as well
                    _fileWatcherTimer.Dispose();
                    _fileWatcherTimer = null;
                    return;
                }
            }

            // We re-enable the timer
            _fileWatcherTimer.Change(10000, 10000);
        }

        /// <summary>
        ///     Reacts to changes in the file system
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private static void OnEditedFileChanged(object source, FileSystemEventArgs e)
        {
            if (WatchedFiles.ContainsKey(e.FullPath))
                if (e.ChangeType == WatcherChangeTypes.Changed)
                {
                    // All of this only makes sense if we can get read-access to the file
                    bool readAccess;
                    try
                    {
                        var fileStream = new FileStream(e.FullPath, FileMode.Open, FileAccess.Read);
                        fileStream.Close();
                        readAccess = true;
                    }
                    catch
                    {
                        readAccess = false;
                    }

                    if (readAccess)
                    {
                        //Guid attachmentId = ((WatcherInfo)FileAttachmentsBusinessEntity.watchedFiles[e.FullPath]).AttachmentId;
                        var attachmentEntity = WatchedFiles[e.FullPath].AttachmentEntity;
                        if (attachmentEntity.AttachFile(e.FullPath, WatchedFiles[e.FullPath].AttachmentIndex))
                        {
                            attachmentEntity.Save();
                            // Everything is up to date now
                            WatchedFiles[e.FullPath].SetSavedState(false);
                        }
                        else
                            // There are changes, but we can not save them. So we mark the file to be saved later.
                            WatchedFiles[e.FullPath].SetSavedState(true);
                    }
                    else
                        // There are changes, but we can not save them.  So we mark the file to be saved later.
                        WatchedFiles[e.FullPath].SetSavedState(true);
                }
        }


        /// <summary>
        ///     Saves the primary (first) attachment as the specified file name.
        /// </summary>
        /// <param name="fileName">Name the file is to be saved as</param>
        public virtual void SaveAttachmentAs(string fileName) => SaveAttachmentAs(FileAttachmentIndex.FirstAttachment, fileName);

        /// <summary>
        ///     Saves the specified attachment as the specified file name.
        /// </summary>
        /// <param name="index">Attachment index</param>
        /// <param name="fileName">Name the file is to be saved as</param>
        public virtual void SaveAttachmentAs(FileAttachmentIndex index, string fileName)
        {
            switch (index)
            {
                case FileAttachmentIndex.FirstAttachment:
                    if (Attachment.Length > 0)
                    {
                        var fileStream = new FileStream(fileName, FileMode.CreateNew);
                        fileStream.Write(Attachment, 0, Attachment.Length);
                        fileStream.Close();
                    }
                    else
                        throw new AttachmentNotFoundException("The current entity (" + Title + ") does not have a primary attachment.");

                    break;
                case FileAttachmentIndex.SecondAttachment:
                    if (Attachment2.Length > 0)
                    {
                        var fileStream = new FileStream(fileName, FileMode.CreateNew);
                        fileStream.Write(Attachment2, 0, Attachment.Length);
                        fileStream.Close();
                    }
                    else
                        throw new AttachmentNotFoundException("The current entity (" + Title + ") does not have a secondary attachment.");

                    break;
                case FileAttachmentIndex.ThirdAttachment:
                    if (Attachment3.Length > 0)
                    {
                        var fileStream = new FileStream(fileName, FileMode.CreateNew);
                        fileStream.Write(Attachment3, 0, Attachment.Length);
                        fileStream.Close();
                    }
                    else
                        throw new AttachmentNotFoundException("The current entity (" + Title + ") does not have a third attachment.");

                    break;
            }
        }

        /// <summary>
        ///     NewEntity
        ///     Used to create a new entity instance.
        /// </summary>
        public static FileAttachmentsBusinessEntity NewEntity() => new FileAttachmentsBusinessEntity();

        /// <summary>
        ///     NewEntity
        ///     Used to create a new entity instance and immediately attach a file to a certain object/entity.
        /// </summary>
        /// <param name="fileName">Name of the file to be attached</param>
        /// <param name="attachToObjectId">Object/Entity id the file is to be attached to</param>
        public static FileAttachmentsBusinessEntity NewEntity(string fileName, Guid attachToObjectId)
        {
            using (var attachmentEntity = new FileAttachmentsBusinessEntity())
            {
                attachmentEntity.AttachFile(fileName);
                attachmentEntity.Assignments.Add();
                attachmentEntity.Assignments[0].LinkedObjectFk = attachToObjectId;
                return attachmentEntity;
            }
        }

        /// <summary>
        ///     Load Entity
        ///     Used to create a new entity instance using the passed id.
        /// </summary>
        /// <param name="id">Primary Key</param>
        public static FileAttachmentsBusinessEntity LoadEntity(Guid id) => new FileAttachmentsBusinessEntity(id);

        /// <summary>
        ///     Override the GetBusinessObject method
        /// </summary>
        public override IBusinessObject GetBusinessObject() => FileAttachmentsBusinessObject.NewInstance();

        /// <summary>
        ///     Override LoadSubItemCollections
        /// </summary>
        protected override void LoadSubItemCollections()
        {
            Assignments = new AssignmentsCollection(this);
            Assignments.SetTable(GetInternalData().Tables["FileAttachmentAssignments"]);
        }

        /// <summary>
        ///     Property for cTitle
        /// </summary>
        public string Title
        {
            get => ReadFieldValue<string>("cTitle").Trim();
            set => WriteFieldValue("cTitle", value);
        }

        /// <summary>
        ///     First attachment's original file name
        /// </summary>
        public string FileName
        {
            get => ReadFieldValue<string>("cFileName").Trim();
            set => WriteFieldValue("cFileName", value);
        }

        /// <summary>
        ///     Second attachment's original file name
        /// </summary>
        public string FileName2
        {
            get => ReadFieldValue<string>("cFileName2").Trim();
            set => WriteFieldValue("cFileName2", value);
        }

        /// <summary>
        ///     Third attachment's original file name
        /// </summary>
        public string FileName3
        {
            get => ReadFieldValue<string>("cFileName3").Trim();
            set => WriteFieldValue("cFileName3", value);
        }

        /// <summary>
        ///     First attachment type
        /// </summary>
        public string Type
        {
            get => ReadFieldValue<string>("cType").Trim();
            set
            {
                if (value.StartsWith(".")) value = value.Substring(1);
                WriteFieldValue("cType", value.Trim());
            }
        }

        /// <summary>
        ///     Second attachment type
        /// </summary>
        public string Type2
        {
            get => ReadFieldValue<string>("cType2").Trim();
            set => WriteFieldValue("cType2", value);
        }

        /// <summary>
        ///     Third attachment type
        /// </summary>
        public string Type3
        {
            get => ReadFieldValue<string>("cType3").Trim();
            set => WriteFieldValue("cType3", value);
        }

        /// <summary>
        ///     First attachment size
        /// </summary>
        public int AttachmentSize
        {
            get => ReadFieldValue<int>("iSize");
            set => WriteFieldValue("iSize", value);
        }

        /// <summary>
        ///     Second attachment size
        /// </summary>
        public int AttachmentSize2
        {
            get => ReadFieldValue<int>("iSize2");
            set => WriteFieldValue("iSize2", value);
        }

        /// <summary>
        ///     Third attachment size
        /// </summary>
        public int AttachmentSize3
        {
            get => ReadFieldValue<int>("iSize3");
            set => WriteFieldValue("iSize3", value);
        }

        /// <summary>
        ///     Property for cDescription
        /// </summary>
        public string Description
        {
            get => ReadFieldValue<string>("cDescription").ToString().Trim();
            set => WriteFieldValue("cDescription", value);
        }

        /// <summary>
        ///     File date of the first attachment
        /// </summary>
        public DateTime FileDate
        {
            get => ReadFieldValue<DateTime>("dFileDate");
            set => WriteFieldValue("dFileDate", value);
        }

        /// <summary>
        ///     File date of the second attachment
        /// </summary>
        public DateTime FileDate2
        {
            get => ReadFieldValue<DateTime>("dFileDate2");
            set => WriteFieldValue("dFileDate2", value);
        }

        /// <summary>
        ///     File date of the third attachment
        /// </summary>
        public DateTime FileDate3
        {
            get => ReadFieldValue<DateTime>("dFileDate3");
            set => WriteFieldValue("dFileDate3", value);
        }

        /// <summary>
        ///     Property for dAttachDate
        /// </summary>
        public DateTime AttachDate
        {
            get => ReadFieldValue<DateTime>("dAttachDate");
            set => WriteFieldValue("dAttachDate", value);
        }

        /// <summary>
        ///     First file attachment
        /// </summary>
        public byte[] Attachment
        {
            get => ReadFieldValue<byte[]>("bAttachment");
            set
            {
                WriteFieldValue("bAttachment", value);
                AttachmentSize = value.Length;
            }
        }

        /// <summary>
        ///     Second file attachment
        /// </summary>
        public byte[] Attachment2
        {
            get => ReadFieldValue<byte[]>("bAttachment2");
            set
            {
                WriteFieldValue("bAttachment2", value);
                AttachmentSize2 = value.Length;
            }
        }

        /// <summary>
        ///     Third file attachment
        /// </summary>
        public byte[] Attachment3
        {
            get => ReadFieldValue<byte[]>("bAttachment3");
            set
            {
                WriteFieldValue("bAttachment3", value);
                AttachmentSize3 = value.Length;
            }
        }

        /// <summary>
        ///     Property for cCategory
        /// </summary>
        public string Category
        {
            get => ReadFieldValue<string>("cCategory").Trim();
            set => WriteFieldValue("cCategory", value);
        }

        /// <summary>
        ///     Property for cSubCategory
        /// </summary>
        public string SubCategory
        {
            get => ReadFieldValue<string>("cSubCategory").Trim();
            set => WriteFieldValue("cSubCategory", value);
        }

        /// <summary>
        ///     Property for bPreviewThumbnail
        /// </summary>
        public byte[] PreviewThumbnail
        {
            get => ReadFieldValue<byte[]>("bPreviewThumbnail");
            set => WriteFieldValue("bPreviewThumbnail", value);
        }
    }

    /// <summary>
    ///     Business Entity for Assignments.
    /// </summary>
    public class AssignmentsBusinessItem : EntitySubItemCollectionItem
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="parentCollection"></param>
        public AssignmentsBusinessItem(IEntitySubItemCollection parentCollection) : base(parentCollection)
        {
        }

        /// <summary>
        ///     Property for fk_fileattachments
        /// </summary>
        public Guid FileAttachmentsFk
        {
            get => ReadFieldValue<Guid>("fk_fileattachments");
            set => WriteFieldValue("fk_fileattachments", value);
        }

        /// <summary>
        ///     Property for fk_linkedtoobject
        /// </summary>
        public Guid LinkedObjectFk
        {
            get => ReadFieldValue<Guid>("fk_linkedtoobject");
            set => WriteFieldValue("fk_linkedtoobject", value);
        }

        /// <summary>
        ///     Property for cDescription
        /// </summary>
        public string Description
        {
            get => ReadFieldValue<string>("cDescription").Trim();
            set => WriteFieldValue("cDescription", value);
        }

        /// <summary>
        ///     Property for cAttachmentLabel
        /// </summary>
        public string AttachmentLabel
        {
            get => ReadFieldValue<string>("cAttachmentLabel").Trim();
            set => WriteFieldValue("cAttachmentLabel", value);
        }

        /// <summary>
        ///     Property for cComment
        /// </summary>
        public string Comment
        {
            get => ReadFieldValue<string>("cComment").Trim();
            set => WriteFieldValue("cComment", value);
        }
    }

    /// <summary>
    ///     Collection for Assignments.
    /// </summary>
    public class AssignmentsCollection : EntitySubItemCollection
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="parentEntity"></param>
        public AssignmentsCollection(IBusinessEntity parentEntity) : base(parentEntity)
        {
        }

        /// <summary>
        ///     Override the indexer
        /// </summary>
        public new AssignmentsBusinessItem this[int index] => (AssignmentsBusinessItem) GetItemByIndex(index);

        /// <summary>
        ///     Override the GetItemObject method
        /// </summary>
        public override IEntitySubItemCollectionItem GetItemObject() => new AssignmentsBusinessItem(this);

        /// <summary>
        ///     Override Configure
        /// </summary>
        protected override void Configure()
        {
            PrimaryKeyField = "pk_fileattachmentassignments";
            ForeignKeyField = "fk_fileattachments";
            ParentTableName = "FileAttachments";
            ParentTablePrimaryKeyField = "pk_fileattachments";

            // TODO - cannot have duplicate FK_FileAttachments + FK_LinkedToObject
        }

        /// <summary>
        ///     Adds a new assignment business item
        /// </summary>
        /// <returns>Assignment business item</returns>
        public new AssignmentsBusinessItem Add() => (AssignmentsBusinessItem) base.Add();
    }

    /// <summary>
    ///     Identifies the attachment index/number
    /// </summary>
    /// <remarks>
    ///     The ability to specify the attachment index is important
    ///     because a Milos attachment can really have multiple
    ///     attachments (such as a document that is attached both in
    ///     Word and PDF format).
    /// </remarks>
    public enum FileAttachmentIndex
    {
        /// <summary>
        ///     First attachment
        /// </summary>
        FirstAttachment,

        /// <summary>
        ///     Second attachment
        /// </summary>
        SecondAttachment,

        /// <summary>
        ///     Third attachment
        /// </summary>
        ThirdAttachment
    }

    /// <summary>
    ///     Exception that is thrown when someone tries to access an attachment that does not exist.
    /// </summary>
    [Serializable]
    public class AttachmentNotFoundException : Exception
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        public AttachmentNotFoundException() : base("Attachment not found.")
        {
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="message">Error message</param>
        public AttachmentNotFoundException(string message) : base(message)
        {
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="innerException">Inner exception</param>
        public AttachmentNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="serializationInfo">Serialization info</param>
        /// <param name="context">Streaming context</param>
        protected AttachmentNotFoundException(SerializationInfo serializationInfo, StreamingContext context) : base(serializationInfo, context)
        {
        }
    }

    /// <summary>
    ///     For internal use only
    /// </summary>
    public class WatcherInfo
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="id">Attachment ID (PK)</param>
        /// <param name="index">Index</param>
        /// <param name="entity">Entity</param>
        /// <param name="watchedFileName">File name</param>
        public WatcherInfo(Guid id, FileAttachmentIndex index, FileAttachmentsBusinessEntity entity, string watchedFileName)
        {
            AttachmentId = id;
            AttachmentIndex = index;
            AttachmentEntity = entity;
            WatchedFileName = watchedFileName;
            LaunchTime = DateTime.Now;
        }

        /// <summary>
        ///     Attachment ID
        /// </summary>
        public Guid AttachmentId { get; set; }

        /// <summary>
        ///     Attachment Index
        /// </summary>
        public FileAttachmentIndex AttachmentIndex { get; set; }

        /// <summary>
        ///     Attachment Entity
        /// </summary>
        public FileAttachmentsBusinessEntity AttachmentEntity { get; set; }

        /// <summary>
        ///     Watched file name
        /// </summary>
        public string WatchedFileName { get; set; }

        /// <summary>
        ///     When was this file watcher launched?
        /// </summary>
        public DateTime LaunchTime { get; set; }

        /// <summary>
        ///     Are there unsaved changes?
        /// </summary>
        public bool HasUnsavedChanges { get; set; }

        /// <summary>
        ///     Sets the saved state
        /// </summary>
        /// <param name="hasUnsavedData">True or false</param>
        public void SetSavedState(bool hasUnsavedData) => HasUnsavedChanges = hasUnsavedData;
    }
}