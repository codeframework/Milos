using System;
using System.Data;
using System.Globalization;
using Milos.BusinessObjects;

namespace Milos.Business.DocumentManagement
{
    /// <summary>
    /// Business Object for FileAttachments.
    /// </summary>
    public class FileAttachmentsBusinessObject : BusinessObject
    {
        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <returns></returns>
        public static FileAttachmentsBusinessObject NewInstance() => new FileAttachmentsBusinessObject();

        /// <summary>
        /// Configure the Business Object
        /// </summary>
        protected override void Configure()
        {
            DataConfigurationPrefix = "database";

            MasterEntity = "FileAttachments";
            PrimaryKeyField = "PK_FileAttachments";

            // Required Field Checking for FileAttachments
            BusinessRules.AddRequiredField("cTitle", "FileAttachments", "File attachment title must be specified.");
            BusinessRules.AddRequiredField("cFileName", "FileAttachments", "File name must be specified");
            BusinessRules.AddRequiredField("cType", "FileAttachments", "File type (extension) must be specified");
            BusinessRules.AddRequiredField("dFileDate", "FileAttachments", "File date must be specified");
            BusinessRules.AddRequiredField("dAttachDate", "FileAttachments", "File attachment date must be specified");
            BusinessRules.AddRequiredField("bAttachment", "FileAttachments", "File attachment must be present");
        }

        /// <summary>
        /// Override LoadSecondaryTables
        /// </summary>
        /// <param name="parentPk"></param>
        /// <param name="existingDataSet"></param>
        protected override void LoadSecondaryTables(Guid parentPk, DataSet existingDataSet) => QueryMultipleRecordsByKey("FileAttachmentAssignments", "*", "fk_fileattachments", parentPk, existingDataSet);

        /// <summary>
        /// Override SaveSecondaryTables
        /// </summary>
        /// <param name="parentPk"></param>
        /// <param name="existingDataSet"></param>
        protected override bool SaveSecondaryTables(Guid parentPk, DataSet existingDataSet) => SaveTable(existingDataSet.Tables["FileAttachmentAssignments"], "pk_fileattachmentassignments");

        /// <summary>
        /// Override AddNewSecondaryTables
        /// </summary>
        /// <param name="parentPk"></param>
        /// <param name="existingDataSet"></param>
        protected override void AddNewSecondaryTables(Guid parentPk, DataSet existingDataSet) => NewSecondaryEntity("FileAttachmentAssignments", existingDataSet);

        /// <summary>
        /// Override PopulateNewRecord
        /// </summary>
        /// <param name="newRow"></param>
        /// <param name="tableName"></param>
        protected override void PopulateNewRecord(DataRow newRow, string tableName)
        {
            base.PopulateNewRecord(newRow, tableName);
            if (string.Compare(tableName, "fileattachments", true, CultureInfo.InvariantCulture) == 0) newRow["dAttachDate"] = DateTime.Now.ToUniversalTime();
        }

        /// <summary>
        /// Retrieves the attachment id (pk) for an attachment linked to 
        /// a certain object id.
        /// </summary>
        /// <param name="linkedObjectId">Linked object id</param>
        /// <returns>Attachment ID (or Guid.Empty if not found)</returns>
        /// <remarks>If more than one attachment is linked to an object, only the first attachment ID will be returned.</remarks>
        public virtual Guid GetAttachmentIdByLinkedObjectId(Guid linkedObjectId)
        {
            using (var command = NewDbCommand())
            {
                command.CommandText = "SELECT FileAttachments.PK_FileAttachments FROM FileAttachmentAssignments INNER JOIN " +
                                      "  FileAttachments ON FileAttachmentAssignments.fk_fileattachments = FileAttachments.PK_FileAttachments " +
                                      "WHERE (FileAttachmentAssignments.FK_LinkedToObject = @Object) ";
                AddDbCommandParameter(command, "@Object", linkedObjectId);

                try
                {
                    return (Guid) ExecuteScalar(command);
                }
                catch
                {
                    return Guid.Empty;
                }
            }
        }

        /// <summary>
        /// Returns a list of all file attachments attached to the specified object id.
        /// </summary>
        /// <param name="linkedObjectId">Object attachments are linked to.</param>
        /// <param name="category">Category</param>
        /// <returns>Attachment ID (or Guid.Empty if not found)</returns>
        /// <remarks>If more than one attachment is linked to an object, only the first attachment ID will be returned.</remarks>
        public virtual Guid GetAttachmentByObjectLinkedToAndCategory(Guid linkedObjectId, string category)
        {
            using (var command = NewDbCommand())
            {
                command.CommandText = "SELECT TOP 1 FA.PK_FileAttachments FROM FileAttachments FA JOIN FileAttachmentAssignments FAA ON FA.PK_FileAttachments = FAA.FK_FileAttachments " +
                                      "WHERE FAA.FK_LinkedToObject = @ObjectID AND FA.cCategory = @Category ";
                AddDbCommandParameter(command, "@ObjectID", linkedObjectId);
                AddDbCommandParameter(command, "@Category", category.Trim());
                try
                {
                    return (Guid) ExecuteScalar(command);
                }
                catch
                {
                    return Guid.Empty;
                }
            }
        }

        /// <summary>
        /// Returns a list of all file attachments attached to the specified object id.
        /// </summary>
        /// <param name="linkedObjectId">Object attachments are linked to.</param>
        /// <returns>List of objects attached to a certain entity/object.</returns>
        public virtual DataSet GetAttachmentsByObjectLinkedTo(Guid linkedObjectId)
        {
            using (var command = NewDbCommand())
            {
                command.CommandText =
                    "SELECT DISTINCT FA.PK_FileAttachments, FA.cTitle, FA.cFileName, FA.cType, SUBSTRING(FA.cDescription, 1, 50) AS cDescription," + "FA.dFileDate, FA.dAttachDate, FA.cCategory, FA.cSubCategory, NULL AS bPreviewThumbnail, FA.iSize " +
                    "FROM FileAttachments FA " +
                    "JOIN FileAttachmentAssignments FAA ON FA.PK_FileAttachments = FAA.fk_FileAttachments " +
                    "WHERE FAA.FK_LinkedToObject = @ObjectID ";
                AddDbCommandParameter(command, "@ObjectID", linkedObjectId);
                DataSet attachments;
                // We may get duplicate PKs
                try
                {
                    attachments = ExecuteQuery(command, "Attachments");
                }
                catch (ConstraintException)
                {
                    attachments = null;
                }

                return attachments;
            }
        }

        /// <summary>
        /// Returns a list of all file attachments attached to the specified object id.
        /// </summary>
        /// <param name="linkedObjectId">Object attachments are linked to.</param>
        /// <param name="category">Category</param>
        /// <returns>List of objects attached to a certain entity/object.</returns>
        public virtual DataSet GetAttachmentsByObjectLinkedToAndCategory(Guid linkedObjectId, string category)
        {
            using (var command = NewDbCommand())
            {
                command.CommandText =
                    "SELECT FA.PK_FileAttachments, FA.cTitle, FA.cFileName, FA.cType, FA.cDescription, FA.dFileDate, " +
                    "FA.dAttachDate, FA.cCategory, FA.cSubCategory, FA.bPreviewThumbnail, FA.iSize " +
                    "FROM FileAttachments FA " +
                    "JOIN FileAttachmentAssignments FAA ON FA.PK_FileAttachments = FAA.fk_FileAttachments " +
                    "WHERE FAA.FK_LinkedToObject = @ObjectID " +
                    "AND FA.cCategory LIKE @Category ";
                AddDbCommandParameter(command, "@ObjectID", linkedObjectId);
                AddDbCommandParameter(command, "@Category", category.Trim() + "%");
                return ExecuteQuery(command, "Attachments");
            }
        }

        /// <summary>
        /// Returns a list of all file attachments attached to the specified object id.
        /// </summary>
        /// <param name="linkedObjectId">Object attachments are linked to.</param>
        /// <param name="category">Category</param>
        /// <param name="subCategory">Sub-Category</param>
        /// <returns>List of objects attached to a certain entity/object.</returns>
        public virtual DataSet GetAttachmentsByObjectLinkedToAndCategory(Guid linkedObjectId, string category, string subCategory)
        {
            using (var command = NewDbCommand())
            {
                command.CommandText =
                    "SELECT FA.PK_FileAttachments, FA.cTitle, FA.cFileName, FA.cType, FA.cDescription, FA.dFileDate, " +
                    "FA.dAttachDate, FA.cCategory, FA.cSubCategory, FA.bPreviewThumbnail, FA.iSize " +
                    "FROM FileAttachments FA " +
                    "JOIN FileAttachmentAssignments FAA ON FA.PK_FileAttachments = FAA.fk_FileAttachments " +
                    "WHERE FAA.FK_LinkedToObject = @ObjectID " +
                    "AND FA.cCategory LIKE @Category " +
                    "AND FA.cSubCategory LIKE @SubCategory ";
                AddDbCommandParameter(command, "@ObjectID", linkedObjectId);
                AddDbCommandParameter(command, "@Category", category.Trim() + "%");
                AddDbCommandParameter(command, "@SubCategory", subCategory.Trim() + "%");
                return ExecuteQuery(command, "Attachments");
            }
        }

        /// <summary>
        /// Links a new object to an attachment that already exists in the database.
        /// </summary>
        /// <param name="attachmentId">Attachment Id</param>
        /// <param name="newLinkedObjectId">Object that is to be linked to the attachment</param>
        /// <returns>Success (true or false)</returns>
        public virtual bool LinkAdditionalObject(Guid attachmentId, Guid newLinkedObjectId)
        {
            using (var command = NewDbCommand("insert into FileAttachmentAssignments (fk_fileattachments, FK_LinkedToObject) values (@AttachmentId, @ObjectId)"))
            {
                AddDbCommandParameter(command, "@AttachmentId", attachmentId);
                AddDbCommandParameter(command, "@ObjectId", newLinkedObjectId);
                if (ExecuteNonQuery(command) == 1) return true;
                return false;
            }
        }

        /// <summary>
        /// Unassigns an attachment from the specified object ID.
        /// If the object (specified by the id) was the last/only object
        /// assigned to this attachment, the attachment will be removed.
        /// </summary>
        /// <param name="attachmentId">The attachment id.</param>
        /// <param name="linkedObjectId">The linked object id.</param>
        public virtual void UnassignAttachment(Guid attachmentId, Guid linkedObjectId)
        {
            // The attachment exists. We now check how many objects we have linked to the attachment
            using (var command = NewDbCommand("SELECT * FROM FileAttachmentAssignments WHERE fk_fileattachments = @AttachmentId"))
            {
                AddDbCommandParameter(command, "@AttachmentId", attachmentId);

                using (var dsLinks = ExecuteQuery(command, "Links"))
                {
                    if (dsLinks.Tables[0].Rows.Count == 1)
                    {
                        // The current object is the only object linked, so we can completely remove the attachment and all links
                        BeginTransaction();
                        using (var cmd2 = NewDbCommand("DELETE FROM FileAttachmentAssignments WHERE fk_fileattachments = @AttachId"))
                        {
                            AddDbCommandParameter(cmd2, "@AttachId", attachmentId);
                            if (ExecuteNonQuery(cmd2) > 0)
                            {
                                var cmd3 = NewDbCommand("DELETE FROM FileAttachments WHERE PK_FileAttachments = @AttachId");
                                AddDbCommandParameter(cmd3, "@AttachId", attachmentId);
                                if (ExecuteNonQuery(cmd3) > 0)
                                    CommitTransaction();
                                else
                                    AbortTransaction();
                            }
                            else
                                AbortTransaction();
                        }
                    }
                    else
                    {
                        if (dsLinks.Tables[0].Rows.Count > 0)
                        {
                            // We only delete the current link to the object
                            using (var cmd2 = NewDbCommand("DELETE FROM FileAttachmentAssignments WHERE FK_LinkedToObject = @ObjectID"))
                            {
                                AddDbCommandParameter(cmd2, "@ObjectID", linkedObjectId);
                                ExecuteNonQuery(cmd2);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Unassigns an attachment from the specified object ID.
        /// If the object (specified by the id) was the last/only object
        /// assigned to this attachment, the attachment will be removed.
        /// </summary>
        /// <param name="linkedObjectId">Linked object id</param>
        public virtual void UnassignAttachment(Guid linkedObjectId)
        {
            var attachmentId = GetAttachmentIdByLinkedObjectId(linkedObjectId);
            if (attachmentId != Guid.Empty) UnassignAttachment(attachmentId, linkedObjectId);
        }

        /// <summary>
        /// Determine if a link exists between the specified attachment and object.
        /// </summary>
        /// <param name="attachmentId">Attachment Id</param>
        /// <param name="linkedObjectId">Object that is to be linked to the attachment</param>
        /// <returns>Success (true or false)</returns>
        public virtual bool IsObjectLinkedToAttachment(Guid attachmentId, Guid linkedObjectId)
        {
            using (var cmd = NewDbCommand())
            {
                cmd.CommandText =
                    "SELECT PK_FileAttachmentAssignments FROM FileAttachmentAssignments " +
                    " WHERE FK_FileAttachments = @AttachmentId AND FK_LinkedToObject = @linkedObjectId ";
                AddDbCommandParameter(cmd, "@attachmentId", attachmentId);
                AddDbCommandParameter(cmd, "@linkedObjectId", linkedObjectId);
                using (var dsAttachments = ExecuteQuery(cmd, "Attachments"))
                    return dsAttachments.Tables[0].Rows.Count == 1;
            }
        }

        /// <summary>
        /// Returns quick information about a fiele attachment. This includes all information
        /// from the first attachment associated with a certain ID, but NOT the actual binary information.
        /// </summary>
        /// <param name="attachmentId">The attachment id.</param>
        /// <returns>DataSet with a single table and a single record</returns>
        /// <remarks>
        /// Use this method when you quickly need to retrieve information about an attachment but not the attachment itself.
        /// This method is much faster than loading the entire entity.
        /// </remarks>
        public DataSet GetAttachmentQuickInfoById(Guid attachmentId)
        {
            using (var command = NewDbCommand("SELECT PK_FileAttachments, cTitle, cFileName, cType, cDescription, dFileDate, dAttachDate, cCategory, cSubCategory FROM FileAttachments WHERE PK_FileAttachments = @ID"))
            {
                AddDbCommandParameter(command, "@ID", attachmentId);
                return ExecuteQuery(command, "Attachment");
            }
        }

        /// <summary>
        /// Logs access to a file (such as a download)
        /// </summary>
        /// <param name="attachmentId">The attachment id.</param>
        /// <param name="accessDateTime">The access date time.</param>
        /// <returns>True if logged successful</returns>
        public bool LogAttachmentAccess(Guid attachmentId, DateTime accessDateTime)
        {
            using (var command = NewDbCommand("INSERT INTO FileAttachmentsLog (fk_FileAttachments, dAccessDateTime) VALUES (@ID, @AccessDateTime)"))
            {
                AddDbCommandParameter(command, "@ID", attachmentId);
                AddDbCommandParameter(command, "@AccessDateTime", accessDateTime);
                return ExecuteNonQuery(command) == 1;
            }
        }

        /// <summary>
        /// Logs access to a file (such as a download) at the current date/time
        /// </summary>
        /// <param name="attachmentId">The attachment id.</param>
        /// <returns>True if logged successful</returns>
        public bool LogAttachmentAccess(Guid attachmentId) => LogAttachmentAccess(attachmentId, DateTime.Now);

        /// <summary>
        /// Returns the count for the number of times a certain attachment has been accessed (such as downloaded)
        /// </summary>
        /// <param name="attachmentId">The attachment id.</param>
        /// <returns>Number of times accessed</returns>
        /// <remarks>
        /// For the access count to work, counts have to be logged using the LogAttachmentAccess() methods.
        /// </remarks>
        public int GetAccessCount(Guid attachmentId)
        {
            using (var command = NewDbCommand("SELECT Count(*) as Count FROM FileAttachmentsLog WHERE fk_FileAttachments = @Id"))
            {
                AddDbCommandParameter(command, "@Id", attachmentId);
                return (int) ExecuteScalar(command);
            }
        }

        /// <summary>
        /// Returns the complete attachment log for a specific attachment
        /// </summary>
        /// <param name="attachmentId">The attachment id.</param>
        /// <returns></returns>
        public DataSet GetAccessLogByAttachment(Guid attachmentId)
        {
            using (var command = NewDbCommand("SELECT * FROM FileAttachmentsLog WHERE fk_FileAttachments = @Id"))
            {
                AddDbCommandParameter(command, "@Id", attachmentId);
                return ExecuteQuery(command, "Counts");
            }
        }
    }

    // SJF 04/08/06 - Work in progress - this rule needs to be added to the AttachmentAssigment collection
    /// <summary>
    /// We need to prevent linking of the same file to the same object 
    /// </summary>
    public class DuplicateAttachmentRule : BusinessRule
    {
        public DuplicateAttachmentRule() : base("products") { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="currentRow"></param>
        /// <param name="rowIndex"></param>
        public override void VerifyRow(DataRow currentRow, int rowIndex)
        {
            var fileAttachment = FileAttachmentsBusinessObject.NewInstance();
            var attachmentId = (Guid) currentRow["FK_FileAttachments"];
            var linkedObjectId = (Guid) currentRow["FK_LinkedToObject"];
            if (fileAttachment.IsObjectLinkedToAttachment(attachmentId, linkedObjectId))
                LogBusinessRuleViolation(currentRow, rowIndex, "DuplicateAttachment", "This file is already attached to this object.");
        }
    }
}