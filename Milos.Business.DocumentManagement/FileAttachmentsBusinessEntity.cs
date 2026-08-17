using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Milos.Business.DocumentManagement;

/// <summary>Business Entity for FileAttachments.</summary>
public class FileAttachmentsBusinessEntity : BusinessEntity
{
    private static readonly List<FileSystemWatcher> Watchers = [];

    private static readonly Dictionary<string, WatcherInfo> WatchedFiles = [];
    private static Timer _fileWatcherTimer;

    public FileAttachmentsBusinessEntity(bool configureImmediately = true) : base(configureImmediately) { }
    public FileAttachmentsBusinessEntity(Guid id, bool configureImmediately = true) : base(id, configureImmediately) { }

    /// <summary>Assignments Collection</summary>
    public AssignmentsCollection Assignments { get; private set; }

    /// <summary>
    /// Attaches the specified file
    /// </summary>
    /// <param name="fileName">File to attach</param>
    /// <returns>True if successful</returns>
    public virtual bool AttachFile(string fileName) => AttachFile(fileName, FileAttachmentIndex.FirstAttachment);

    /// <summary>
    /// Attaches the specified file
    /// </summary>
    /// <param name="fileName">File to attach</param>
    /// <param name="attachmentNumber">Attachment number/index</param>
    /// <returns>True if successful</returns>
    public virtual bool AttachFile(string fileName, FileAttachmentIndex attachmentNumber = FileAttachmentIndex.FirstAttachment)
    {
        if (!File.Exists(fileName)) throw new FileNotFoundException("Specified file does not exist and can therefore not be attached.", fileName);

        try
        {
            using var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            var fileData = new byte[fileStream.Length];
            fileStream.Read(fileData, 0, (int)fileStream.Length);
            fileStream.Close();
            return AttachFile(fileData, fileName, attachmentNumber);
        }
        catch (IOException)
        {
            return false;
        }
    }

    /// <summary>
    /// Attaches the specified file
    /// </summary>
    /// <param name="fileName">File to attach</param>
    /// <param name="attachmentNumber">Attachment number/index</param>
    /// <returns>True if successful</returns>
    public async virtual Task<bool> AttachFileAsync(string fileName, FileAttachmentIndex attachmentNumber = FileAttachmentIndex.FirstAttachment)
    {
        if (!File.Exists(fileName)) throw new FileNotFoundException("Specified file does not exist and can therefore not be attached.", fileName);

        try
        {
            using var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            var fileData = new byte[fileStream.Length];
            await fileStream.ReadAsync(fileData, 0, (int)fileStream.Length);
            fileStream.Close();
            return AttachFile(fileData, fileName, attachmentNumber);
        }
        catch (IOException)
        {
            return false;
        }
    }

    /// <summary>
    /// Attaches the specified bytes as the file.
    /// </summary>
    /// <param name="fileData">The file data (bytes).</param>
    /// <param name="fileName">Name of the file.</param>
    /// <returns></returns>
    public bool AttachFile(byte[] fileData, string fileName) => 
        AttachFile(fileData, fileName, FileAttachmentIndex.FirstAttachment);

    /// <summary>
    /// Attaches the specified bytes as the file.
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
    /// Opens the primary (first) attachment for editing.
    /// </summary>
    public virtual void EditAttachment() => EditAttachment(FileAttachmentIndex.FirstAttachment, true);

    /// <summary>
    /// Opens the specified attachment for editing
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
            watcher.Changed += async (s, e) => await OnEditedFileChanged(s, e);
            watcher.EnableRaisingEvents = true;
            Watchers.Add(watcher);
            WatchedFiles.Add(tempFileName, new WatcherInfo(PK, index, this, tempFileName));

            // We also use a timer to find out when the file has been closed 
            // and we can do a final save
            _fileWatcherTimer ??= new Timer(FileWatcherTimerTick, null, 10000, 10000);
        }

        Process.Start(tempFileName);
    }

    /// <summary>
    /// Reacts to timer events
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
    /// Reacts to changes in the file system
    /// </summary>
    /// <param name="source"></param>
    /// <param name="e"></param>
    private async static Task OnEditedFileChanged(object source, FileSystemEventArgs e)
    {
        if (WatchedFiles.ContainsKey(e.FullPath))
            if (e.ChangeType == WatcherChangeTypes.Changed)
            {
                // All of this only makes sense if we can get read-access to the file
                bool readAccess;
                try
                {
                    using var fileStream = new FileStream(e.FullPath, FileMode.Open, FileAccess.Read);
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
                    if (await attachmentEntity.AttachFileAsync(e.FullPath, WatchedFiles[e.FullPath].AttachmentIndex))
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
    /// Saves the primary (first) attachment as the specified file name.
    /// </summary>
    /// <param name="fileName">Name the file is to be saved as</param>
    public virtual void SaveAttachmentAs(string fileName) => 
        SaveAttachmentAs(FileAttachmentIndex.FirstAttachment, fileName);

    /// <summary>
    /// Saves the specified attachment as the specified file name.
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
                    using var fileStream = new FileStream(fileName, FileMode.CreateNew);
                    fileStream.Write(Attachment, 0, Attachment.Length);
                    fileStream.Close();
                }
                else
                    throw new AttachmentNotFoundException("The current entity (" + Title + ") does not have a primary attachment.");

                break;
            case FileAttachmentIndex.SecondAttachment:
                if (Attachment2.Length > 0)
                {
                    using var fileStream = new FileStream(fileName, FileMode.CreateNew);
                    fileStream.Write(Attachment2, 0, Attachment.Length);
                    fileStream.Close();
                }
                else
                    throw new AttachmentNotFoundException("The current entity (" + Title + ") does not have a secondary attachment.");

                break;
            case FileAttachmentIndex.ThirdAttachment:
                if (Attachment3.Length > 0)
                {
                    using var fileStream = new FileStream(fileName, FileMode.CreateNew);
                    fileStream.Write(Attachment3, 0, Attachment.Length);
                    fileStream.Close();
                }
                else
                    throw new AttachmentNotFoundException("The current entity (" + Title + ") does not have a third attachment.");

                break;
        }
    }

    /// <summary>
    /// Saves the specified attachment as the specified file name.
    /// </summary>
    /// <param name="index">Attachment index</param>
    /// <param name="fileName">Name the file is to be saved as</param>
    public async virtual Task SaveAttachmentAsAsync(FileAttachmentIndex index, string fileName)
    {
        switch (index)
        {
            case FileAttachmentIndex.FirstAttachment:
                if (Attachment.Length > 0)
                {
                    using var fileStream = new FileStream(fileName, FileMode.CreateNew);
                    await fileStream.WriteAsync(Attachment, 0, Attachment.Length);
                    fileStream.Close();
                }
                else
                    throw new AttachmentNotFoundException($"The current entity ({Title}) does not have a primary attachment.");

                break;
            case FileAttachmentIndex.SecondAttachment:
                if (Attachment2.Length > 0)
                {
                    using var fileStream = new FileStream(fileName, FileMode.CreateNew);
                    await fileStream.WriteAsync(Attachment2, 0, Attachment2.Length);
                    fileStream.Close();
                }
                else
                    throw new AttachmentNotFoundException($"The current entity ({Title}) does not have a secondary attachment.");

                break;
            case FileAttachmentIndex.ThirdAttachment:
                if (Attachment3.Length > 0)
                {
                    using var fileStream = new FileStream(fileName, FileMode.CreateNew);
                    await fileStream.WriteAsync(Attachment3, 0, Attachment3.Length);
                    fileStream.Close();
                }
                else
                    throw new AttachmentNotFoundException($"The current entity ({Title}) does not have a third attachment.");

                break;
        }
    }

    /// <summary>
    /// Creates a new field attachment entity/record
    /// </summary>
    public static FileAttachmentsBusinessEntity NewEntity() => new();

    public async static Task<FileAttachmentsBusinessEntity> NewEntityAsync()
    {
        var entity = new FileAttachmentsBusinessEntity(false);
        await entity.ConfigureForNew();
        return entity;
    }

    /// <summary>
    /// NewEntity
    /// Used to create a new entity instance and immediately attach a file to a certain object/entity.
    /// </summary>
    /// <param name="fileName">Name of the file to be attached</param>
    /// <param name="attachToObjectId">Object/Entity id the file is to be attached to</param>
    public static FileAttachmentsBusinessEntity NewEntity(string fileName, Guid attachToObjectId)
    {
        using var attachmentEntity = new FileAttachmentsBusinessEntity();
        attachmentEntity.AttachFile(fileName);
        attachmentEntity.Assignments.Add();
        attachmentEntity.Assignments[0].LinkedObjectFk = attachToObjectId;
        return attachmentEntity;
    }

    /// <summary>
    /// NewEntity
    /// Used to create a new entity instance and immediately attach a file to a certain object/entity.
    /// </summary>
    /// <param name="fileName">Name of the file to be attached</param>
    /// <param name="attachToObjectId">Object/Entity id the file is to be attached to</param>
    public static async Task<FileAttachmentsBusinessEntity> NewEntityAsync(string fileName, Guid attachToObjectId)
    {
        using var attachmentEntity = new FileAttachmentsBusinessEntity(false);
        await attachmentEntity.ConfigureForNew();
        attachmentEntity.AttachFile(fileName);
        attachmentEntity.Assignments.Add();
        attachmentEntity.Assignments[0].LinkedObjectFk = attachToObjectId;
        return attachmentEntity;
    }

    /// <summary>
    /// Load Entity
    /// Used to create a new entity instance using the passed id.
    /// </summary>
    /// <param name="id">Primary Key</param>
    public static FileAttachmentsBusinessEntity LoadEntity(Guid id) => new(id);

    /// <summary>
    /// Load Entity
    /// Used to create a new entity instance using the passed id.
    /// </summary>
    /// <param name="id">Primary Key</param>
    public static async Task<FileAttachmentsBusinessEntity> LoadEntityAsync(Guid id)
    {
        var entity = new FileAttachmentsBusinessEntity(id, false);
        await entity.ConfigureForLoad(id);
        return entity;
    }

    public override IBusinessObject GetBusinessObject() => FileAttachmentsBusinessObject.NewInstance();

    /// <summary>
    /// Override LoadSubItemCollections
    /// </summary>
    protected override void LoadSubItemCollections()
    {
        Assignments = new AssignmentsCollection(this);
        Assignments.SetTable(GetInternalData().Tables["FileAttachmentAssignments"]);
    }

    /// <summary>
    /// File Title
    /// </summary>
    public string Title
    {
        get => Get<string>("cTitle").Trim();
        set => Set(value, "cTitle");
    }

    /// <summary>
    /// File Name
    /// </summary>
    public string FileName
    {
        get => Get<string>("cFileName").Trim();
        set => Set(value, "cFileName");
    }

    /// <summary>
    /// Second attachment's original file name
    /// </summary>
    public string FileName2
    {
        get => Get<string>("cFileName2").Trim();
        set => Set(value, "cFileName2");
    }

    /// <summary>
    /// Third attachment's original file name
    /// </summary>
    public string FileName3
    {
        get => Get<string>("cFileName3").Trim();
        set => Set(value, "cFileName3");
    }

    /// <summary>
    /// First attachment type
    /// </summary>
    public string Type
    {
        get => Get<string>("cType").Trim();
        set
        {
            if (value.StartsWith(".")) value = value.Substring(1);
            Set(value.Trim(), "cType");
        }
    }

    /// <summary>
    /// Second attachment type
    /// </summary>
    public string Type2
    {
        get => Get<string>("cType2").Trim();
        set => Set(value, "cType2");
    }

    /// <summary>
    /// Third attachment type
    /// </summary>
    public string Type3
    {
        get => Get<string>("cType3").Trim();
        set => Set(value, "cType3");
    }

    /// <summary>
    /// First attachment size
    /// </summary>
    public int AttachmentSize
    {
        get => Get<int>("iSize");
        set => Set(value, "iSize");
    }

    /// <summary>
    /// Second attachment size
    /// </summary>
    public int AttachmentSize2
    {
        get => Get<int>("iSize2");
        set => Set(value, "iSize2");
    }

    /// <summary>
    /// Third attachment size
    /// </summary>
    public int AttachmentSize3
    {
        get => Get<int>("iSize3");
        set => Set(value, "iSize3");
    }

    /// <summary>
    /// Attachment Description
    /// </summary>
    public string Description
    {
        get => Get<string>("cDescription").Trim();
        set => Set(value, "cDescription");
    }

    /// <summary>
    /// File date of the first attachment
    /// </summary>
    public DateTime FileDate
    {
        get => Get<DateTime>("dFileDate");
        set => Set(value, "dFileDate");
    }

    /// <summary>
    /// File date of the second attachment
    /// </summary>
    public DateTime FileDate2
    {
        get => Get<DateTime>("dFileDate2");
        set => Set(value, "dFileDate2");
    }

    /// <summary>
    /// File date of the third attachment
    /// </summary>
    public DateTime FileDate3
    {
        get => Get<DateTime>("dFileDate3");
        set => Set(value, "dFileDate3");
    }

    /// <summary>
    /// Attachment Date
    /// </summary>
    public DateTime AttachDate
    {
        get => Get<DateTime>("dAttachDate");
        set => Set(value, "dAttachDate");
    }

    /// <summary>
    /// First file attachment
    /// </summary>
    public byte[] Attachment
    {
        get => Get<byte[]>("bAttachment");
        set
        {
            Set(value, "bAttachment");
            AttachmentSize = value.Length;
        }
    }

    /// <summary>
    /// Second file attachment
    /// </summary>
    public byte[] Attachment2
    {
        get => Get<byte[]>("bAttachment2");
        set
        {
            Set(value, "bAttachment2");
            AttachmentSize2 = value.Length;
        }
    }

    /// <summary>
    /// Third file attachment
    /// </summary>
    public byte[] Attachment3
    {
        get => Get<byte[]>("bAttachment3");
        set
        {
            Set(value, "bAttachment3");
            AttachmentSize3 = value.Length;
        }
    }

    /// <summary>
    /// Attachment Category
    /// </summary>
    public string Category
    {
        get => Get<string>("cCategory").Trim();
        set => Set(value, "cCategory");
    }

    /// <summary>
    /// Attachment SubCategory
    /// </summary>
    public string SubCategory
    {
        get => Get<string>("cSubCategory").Trim();
        set => Set(value, "cSubCategory");
    }

    /// <summary>
    /// Attachment Preview Thumbnail
    /// </summary>
    public byte[] PreviewThumbnail
    {
        get => Get<byte[]>("bPreviewThumbnail");
        set => Set(value, "bPreviewThumbnail");
    }
}

/// <summary>Business Entity for File Attachment Assignments.</summary>
/// <param name="parentCollection">Parent Collection</param>
public class AssignmentsBusinessItem(IEntitySubItemCollection parentCollection) : EntitySubItemCollectionItem(parentCollection)
{

    /// <summary>
    /// Field Attachment ID
    /// </summary>
    public Guid FileAttachmentsFk
    {
        get => Get<Guid>("fk_fileattachments");
        set => Set(value, "fk_fileattachments");
    }

    /// <summary>
    /// ID of the object the attachment is linked to
    /// </summary>
    public Guid LinkedObjectFk
    {
        get => Get<Guid>("fk_linkedtoobject");
        set => Set(value, "fk_linkedtoobject");
    }

    /// <summary>
    /// Attachment Description
    /// </summary>
    public string Description
    {
        get => Get<string>("cDescription").Trim();
        set => Set(value, "cDescription");
    }

    /// <summary>
    /// Attachment Label
    /// </summary>
    public string AttachmentLabel
    {
        get => Get<string>("cAttachmentLabel").Trim();
        set => Set(value, "cAttachmentLabel");
    }

    /// <summary>
    /// Attachment Comment
    /// </summary>
    public string Comment
    {
        get => Get<string>("cComment").Trim();
        set => Set(value, "cComment");
    }
}

/// <summary>
/// Collection for Assignments.
/// </summary>
/// <remarks>
/// Constructor
/// </remarks>
/// <param name="parentEntity"></param>
public class AssignmentsCollection(IBusinessEntity parentEntity) : EntitySubItemCollection(parentEntity)
{
    public new AssignmentsBusinessItem this[int index] => (AssignmentsBusinessItem)GetItemByIndex(index);
    public override IEntitySubItemCollectionItem GetItemObject() => new AssignmentsBusinessItem(this);

    protected override void Configure()
    {
        PrimaryKeyField = "pk_fileattachmentassignments";
        ForeignKeyField = "fk_fileattachments";
        ParentTableName = "FileAttachments";
        ParentTablePrimaryKeyField = "pk_fileattachments";

        // TODO - cannot have duplicate FK_FileAttachments + FK_LinkedToObject
    }

    /// <summary>
    /// Adds a new assignment business item
    /// </summary>
    /// <returns>Assignment business item</returns>
    public new AssignmentsBusinessItem Add() => (AssignmentsBusinessItem)base.Add();
}

/// <summary>
/// Identifies the attachment index/number
/// </summary>
/// <remarks>
/// The ability to specify the attachment index is important
/// because a Milos attachment can really have multiple
/// attachments (such as a document that is attached both in
/// Word and PDF format).
/// </remarks>
public enum FileAttachmentIndex
{
    FirstAttachment,
    SecondAttachment,
    ThirdAttachment
}

/// <summary>
/// Exception that is thrown when someone tries to access an attachment that does not exist.
/// </summary>
[Serializable]
public class AttachmentNotFoundException : Exception
{
    public AttachmentNotFoundException() : base("Attachment not found.") { }
    public AttachmentNotFoundException(string message) : base(message) { }
    public AttachmentNotFoundException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>Constructor</summary>
    /// <param name="serializationInfo">Serialization info</param>
    /// <param name="context">Streaming context</param>
    protected AttachmentNotFoundException(SerializationInfo serializationInfo, StreamingContext context) : base(serializationInfo, context) { }
}

/// <summary>
/// For internal use only
/// </summary>
/// <param name="id">Attachment ID (PK)</param>
/// <param name="index">Index</param>
/// <param name="entity">Entity</param>
/// <param name="watchedFileName">File name</param>
public class WatcherInfo(Guid id, FileAttachmentIndex index, FileAttachmentsBusinessEntity entity, string watchedFileName)
{
    public Guid AttachmentId { get; set; } = id;
    public FileAttachmentIndex AttachmentIndex { get; set; } = index;
    public FileAttachmentsBusinessEntity AttachmentEntity { get; set; } = entity;
    public string WatchedFileName { get; set; } = watchedFileName;
    public DateTime LaunchTime { get; set; } = DateTime.Now;
    public bool HasUnsavedChanges { get; set; }
    public void SetSavedState(bool hasUnsavedData) => HasUnsavedChanges = hasUnsavedData;
}