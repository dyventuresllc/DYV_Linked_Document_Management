using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using DYV_Linked_Document_Management.Utilities;
using kCura.EventHandler;
using Relativity.API;
using Field = kCura.EventHandler.Field;

namespace DYV_Linked_Document_Management.Event_Handlers
{
    public class EH_PS_LinkedDocumentFiles : PreSaveEventHandler
    {
        private IAPILog logger;
        public override FieldCollection RequiredFields
        {
            get
            {
                var retVal = new FieldCollection();
                if (logger == null)
                {
                    logger = Helper.GetLoggerFactory().GetLogger();
                }
                var helper = new DYVLDHelper(this.Helper, Helper.GetLoggerFactory().GetLogger());
                retVal.Add(new Field(0,          // artifactID
                    "Visible",                   // name
                    "Visible",                   // columnName
                    0,                           // fieldTypeID
                    0,                           // codeTypeID
                    0,                           // fieldCategoryID
                    false,                       // isReflected
                    false,                       // isInLayout
                    null,                        // value
                    new List<Guid> { helper.LdfFileIdentifer }  // guids
                    ));
                retVal.Add(new Field(0,         // artifactID
                   "Visible",                   // name
                   "Visible",                   // columnName
                   4,                           // fieldTypeID
                   0,                           // codeTypeID
                   0,                           // fieldCategoryID
                   false,                       // isReflected
                   false,                       // isInLayout
                   null,                        // value
                   new List<Guid> { helper.LdfStatus }  // guids
                   ));
                retVal.Add(new Field(0,         // artifactID
                   "Visible",                   // name
                   "Visible",                   // columnName
                   1,                           // fieldTypeID
                   0,                           // codeTypeID
                   0,                           // fieldCategoryID
                   false,                       // isReflected
                   false,                       // isInLayout
                   null,                        // value
                   new List<Guid> { helper.LdfObjectId }  // guids
                   ));
                retVal.Add(new Field(0,         // artifactID
                   "Visible",                   // name
                   "Visible",                   // columnName
                   0,                           // fieldTypeID
                   0,                           // codeTypeID
                   0,                           // fieldCategoryID
                   false,                       // isReflected
                   false,                       // isInLayout
                   null,                        // value
                   new List<Guid> { helper.LdfFilesTableId }  // guids
                   ));
                retVal.Add(new Field(0,         // artifactID
                    "Visible",                  // name
                    "Visible",                  // columnName
                    0,                          // fieldTypeID
                    0,                          // codeTypeID
                    0,                          // fieldCategoryID
                    false,                      // isReflected
                    false,                      // isInLayout
                    null,                       // value
                    new List<Guid> { helper.LdfObjectID_GM_Metadata }  // guids
                    ));
                return retVal;
            }
        }
        public override Response Execute()
        {
            if (logger == null)
            {
                logger = Helper.GetLoggerFactory().GetLogger();
            }
            var retVal = new Response
            {
                Success = true,
                Message = string.Empty
            };
            var helper = new DYVLDHelper(this.Helper, Helper.GetLoggerFactory().GetLogger());
            var statusHtml = new StringBuilder();
            statusHtml.Append("<div style='font-family:Consolas,monospace;font-size:0.9em;'>");
            statusHtml.Append("<div style='font-weight:bold;margin:0;padding:0;'>Settings confirmation...</div>");
            try
            {

                //Validate Workspace ArtifactId
                if (!this.ActiveArtifact.Fields[helper.LdfTargetWorkspaceId.ToString()].Value.IsNull)
                {
                    int targetWorkspaceId = (int)this.ActiveArtifact.Fields[helper.LdfTargetWorkspaceId.ToString()].Value.Value;
                    ValidateTargetWorkspace(statusHtml, ref retVal, targetWorkspaceId);
                    ValidateCustodian(statusHtml, ref retVal, targetWorkspaceId, helper, DateTime.UtcNow);
                }
                var dbContext = Helper.GetDBContext(Helper.GetActiveCaseID());
                ValidateLdfObjectTypeId(statusHtml, ref retVal, dbContext, helper, DateTime.UtcNow);
                ValidateLdfGMMetadataObjectTypeID(statusHtml, ref retVal, dbContext, helper, DateTime.UtcNow);
                var fileFieldId = ValidateFileId(statusHtml, ref retVal, dbContext, helper, DateTime.UtcNow);

                bool isNewRecord = this.ActiveArtifact.IsNew;
                switch (isNewRecord)
                {
                    case true:

                        ExtractFileNameFromNewRecord(statusHtml, ref retVal);              
                        break;
                    case false:
                        var fileName = ExtractFileNameFromExistingRecord(statusHtml, ref retVal, dbContext, helper, DateTime.UtcNow, fileFieldId, this.ActiveArtifact.ArtifactID);
                        this.ActiveArtifact.Fields[helper.LdfFileIdentifer.ToString()].Value.Value = fileName;
                        break;
                }
                             
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in EH_PS_LinkedDocumentFiles");
                retVal.Success = false;
                retVal.Message = $"Error: {ex.Message}";
                return retVal;
            }
            
            statusHtml.Append("</div>");
            this.ActiveArtifact.Fields[helper.LdfStatus.ToString()].Value.Value = statusHtml.ToString();
            return retVal;
        }
        private void ValidateCustodian(StringBuilder statusHtml, ref Response retVal, int targetWorkspaceId, DYVLDHelper helper, DateTime timestamp)
        {
            try
            {
                var targetDbContext = Helper.GetDBContext(targetWorkspaceId);
                // Get custodian information directly from the required field
                var custodianField = this.ActiveArtifact.Fields[helper.LdfCustodianId.ToString()];
                int custodianArtifactId = Convert.ToInt32(custodianField.Value.Value);
                // Query the entity in the target workspace
                string custodianSql = $"SELECT ArtifactId, FullName FROM eddsdbo.Entity WHERE ArtifactId = {custodianArtifactId}";
                var custodianResult = targetDbContext.ExecuteSqlStatementAsDataTable(custodianSql);
                if (custodianResult != null && custodianResult.Rows.Count > 0)
                {
                    var artifactId = custodianResult.Rows[0]["ArtifactId"].ToString();
                    var fullName = custodianResult.Rows[0]["FullName"].ToString();
                    string formattedTimestamp = timestamp.ToString("MM/dd/yyyy HH:mm:ss") + " UTC";
                    statusHtml.Append($"<div style='margin:0;padding:0 0 0 10px;'><span style='color:#555;font-weight:bold;'>[{formattedTimestamp}]</span> Custodian for processing: Id[<span style='color:#0066cc;'>{artifactId}]</span> - Name[<span style='color:#0066cc;'>{fullName}</span>]</div>");
                }
                else
                {
                    logger.LogError($"Custodian with ID {custodianArtifactId} not found in target workspace.");
                    retVal.Success = false;
                    retVal.Message = $"Specified custodian (ID: {custodianArtifactId}) not found in target workspace.";
                    return;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error validating custodian: {ex.Message}");
                retVal.Success = false;
                retVal.Message = $"Error validating custodian: {ex.Message}";
                return;
            }
        }
        private void ValidateTargetWorkspace(StringBuilder statusHtml, ref Response retVal, int targetWorkspaceId)
        {
            try
            {
                // Check if target workspace exists
                var eddsDbContext = Helper.GetDBContext(-1);
                string workspaceNameSql = $"SELECT [Name] FROM eddsdbo.ExtendedCase WHERE ArtifactID = {targetWorkspaceId}";
                DataTable workspaceResult = eddsDbContext.ExecuteSqlStatementAsDataTable(workspaceNameSql);
                if (workspaceResult == null || workspaceResult.Rows.Count == 0)
                {
                    retVal.Success = false;
                    retVal.Message = $"Target Workspace with ID {targetWorkspaceId} was not found.";
                    return;
                }
                string workspaceName = workspaceResult.Rows[0]["Name"].ToString();
                string formattedTimestamp = DateTime.UtcNow.ToString("MM/dd/yyyy HH:mm:ss") + " UTC";
                statusHtml.Append($"<div style='margin:0;padding:0 0 0 10px;'><span style='color:#555;font-weight:bold;'>[{formattedTimestamp}]</span> Target Workspace: <span style='color:#0066cc;'>{workspaceName}</span> (ID: {targetWorkspaceId})</div>");
            }
            catch (Exception ex)
            {
                logger.LogError($"Error Validating Target Workspace: {ex.Message}");
                retVal.Success = false;
                retVal.Message = $"Error Validating Target Workspace: {ex.Message}";
                return;
            }
        }
        private void ValidateLdfObjectTypeId(StringBuilder statusHtml, ref Response retVal, IDBContext dbContext, DYVLDHelper helper, DateTime timestamp)
        {
            try
            {
                string objectTypeSql = "SELECT DescriptorArtifactTypeID FROM eddsdbo.ObjectType WHERE [Name] like 'Linked Document Files'";
                DataTable objectTypeResult = dbContext.ExecuteSqlStatementAsDataTable(objectTypeSql);
                if (objectTypeResult != null && objectTypeResult.Rows.Count > 0)
                {
                    int objectTypeID = Convert.ToInt32(objectTypeResult.Rows[0]["DescriptorArtifactTypeID"]);
                    this.ActiveArtifact.Fields[helper.LdfObjectId.ToString()].Value.Value = objectTypeID;
                    string formattedTimestamp = timestamp.ToString("MM/dd/yyyy HH:mm:ss") + " UTC";
                    statusHtml.Append($"<div style='margin:0;padding:0 0 0 10px;'><span style='color:#555;font-weight:bold;'>[{formattedTimestamp}]</span> Linked Document Files ObjectType ID: {objectTypeID}</div>");
                    return;
                }
            }
            catch (Exception ex)
            {
                NullValues(helper);
                logger.LogError($"Error Validating Linked Document File Object ID - {ex.Message}");
                retVal.Success = false;
                retVal.Message = $"Error Validating Linked Document File Object ID";
            }
        }
        private void ValidateLdfGMMetadataObjectTypeID(StringBuilder statusHtml, ref Response retVal, IDBContext dbContext, DYVLDHelper helper, DateTime timestamp)
        {
            try
            {
                //Get the Gmail CSV Metadata ObjectTypeId
                string objectTypeSql = "SELECT DescriptorArtifactTypeID FROM eddsdbo.ObjectType WHERE [Name] like 'Gmail (CSV) Metadata'";
                DataTable objectTypeResult = dbContext.ExecuteSqlStatementAsDataTable(objectTypeSql);
                if (objectTypeResult != null && objectTypeResult.Rows.Count > 0)
                {
                    int objectTypeID = Convert.ToInt32(objectTypeResult.Rows[0]["DescriptorArtifactTypeID"]);
                    this.ActiveArtifact.Fields[helper.LdfObjectID_GM_Metadata.ToString()].Value.Value = objectTypeID;
                    string formattedTimestamp = timestamp.ToString("MM/dd/yyyy HH:mm:ss") + " UTC";
                    statusHtml.Append($"<div style='margin:0;padding:0 0 0 10px;'><span style='color:#555;font-weight:bold;'>[{formattedTimestamp}]</span> Linked Document GM Metadata CSV ObjectType ID: {objectTypeID}</div>");
                    return;
                }
            }
            catch (Exception ex)
            {
                NullValues(helper);
                logger.LogError($"Error Validating GMail Metadata CSV ObjectTypeId - {ex.Message}");
                retVal.Success = false;
                retVal.Message = "Error Validating GMail Metadata CSV ObjectType ID";
            }
        }
        private int ValidateFileId(StringBuilder statusHtml, ref Response retVal, IDBContext dbContext, DYVLDHelper helper, DateTime timestamp)
        {
            try
            {
                string fileFieldSql = @"SELECT f.ArtifactID
                            FROM eddsdbo.Field f
                            JOIN eddsdbo.ObjectType ot
                            ON ot.DescriptorArtifactTypeID = f.FieldArtifactTypeID
                            WHERE f.DisplayName = 'File'
                            AND ot.[Name] like 'Linked Document Files'";
                DataTable fileFieldResult = dbContext.ExecuteSqlStatementAsDataTable(fileFieldSql);
                if (fileFieldResult != null && fileFieldResult.Rows.Count > 0)
                {
                    int fileFieldID = Convert.ToInt32(fileFieldResult.Rows[0]["ArtifactID"]);
                    this.ActiveArtifact.Fields[helper.LdfFilesTableId.ToString()].Value.Value = fileFieldID;
                    string formattedTimestamp = timestamp.ToString("MM/dd/yyyy HH:mm:ss") + " UTC";
                    statusHtml.Append($"<div style='margin:0;padding:0 0 0 10px;'><span style='color:#555;font-weight:bold;'>[{formattedTimestamp}]</span> Linked Document Files Field ID: {fileFieldID}</div>");
                    return fileFieldID;
                }
                else
                {
                    retVal.Success = false;
                    retVal.Message = "No File field found for Linked Document Files object type";
                    return 0;
                }
            }
            catch (Exception ex)
            {
                helper.Logger.LogError($"Error Validating File Table ArtifactId - {ex.Message}");
                retVal.Success = false;
                retVal.Message = "Error Validating File Table ArtifactId";
                return 0;
            }
        }
        private string ExtractFileNameFromNewRecord(StringBuilder statusHtml, ref Response retVal)
        {
            string newfileName = null;
            var fileField = ActiveArtifact.Fields.Cast<Field>().FirstOrDefault(field => field.FieldTypeID == 9);

            if (fileField != null && !fileField.Value.IsNull)
            {
                var fileFieldValue = (FileFieldValue)fileField.Value;
                if (fileFieldValue.FileValue != null)
                {
                    newfileName = System.IO.Path.GetFileName(fileFieldValue.FileValue.FilePath);
                }
            }

            // If we found a filename, update fields
            if (!string.IsNullOrEmpty(newfileName))
            {
                // Set the File Name field (Type 0)
                var fileNameField = ActiveArtifact.Fields.Cast<Field>()
                    .FirstOrDefault(field => field.Name == "File Identifier");

                if (fileNameField != null)
                {
                    fileNameField.Value.Value = newfileName;
                    string formattedTimestamp = DateTime.UtcNow.ToString("MM/dd/yyyy HH:mm:ss") + " UTC";
                    statusHtml.Append($"<div style='margin:0;padding:0 0 0 10px;'><span style='color:#555;font-weight:bold;'>[{formattedTimestamp}]</span> File Identifier has been updated to reflect File GUID (name): <span style='color:#0066cc;'>{newfileName}</span></div>");
                    return newfileName;
                }
                else
                {
                    retVal.Success = false;
                    retVal.Message = "Unspecified Error could not save record.";
                    return null;
                }
            }

            return null;
        }
        private string ExtractFileNameFromExistingRecord(StringBuilder statusHtml, ref Response retVal, IDBContext dbContext, DYVLDHelper helper, DateTime timestamp, int fileFieldId, int objectArtifactId)
        {
            try
            {
                string fileTableName = $"File{fileFieldId}";
                string filePathSql = $"SELECT Location FROM eddsdbo.[{fileTableName}] WHERE ObjectArtifactID = {objectArtifactId}";
                object result = dbContext.ExecuteSqlStatementAsScalar(filePathSql);

                if (result == null || result == DBNull.Value)
                {
                    retVal.Success = false;
                    retVal.Message = "File location not found in database";
                    return string.Empty;
                }
                string filePath = result.ToString();
                int startIndex = filePath.IndexOf($"\\{fileTableName}\\");
                if (startIndex >= 0)
                {
                    string fileName = filePath.Substring(startIndex + fileTableName.Length + 2);
                    string formattedTimestamp = timestamp.ToString("MM/dd/yyyy HH:mm:ss") + " UTC";
                    statusHtml.Append($"<div style='margin:0;padding:0 0 0 10px;'><span style='color:#555;font-weight:bold;'>[{formattedTimestamp}]</span> File Name extracted from filepath: <span style='color:#0066cc;'>{fileName}</span></div>");
                    return fileName;
                }
                else
                {
                    retVal.Success = false;
                    retVal.Message = "Error extracting filename from filepath";
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                NullValues(helper);
                helper.Logger.LogError($"Error extracting fileName from filepath - {ex.Message}");
                retVal.Success = false;
                retVal.Message = "Error extracting filename from filepath";
                return string.Empty;
            }
        }        
        private void NullValues(DYVLDHelper helper)
        {
            this.ActiveArtifact.Fields[helper.LdfObjectId.ToString()].Value.Value = null;
            this.ActiveArtifact.Fields[helper.LdfObjectID_GM_Metadata.ToString()].Value.Value = null;
            this.ActiveArtifact.Fields[helper.LdfFilesTableId.ToString()].Value.Value = null;
            this.ActiveArtifact.Fields[helper.LdfFileIdentifer.ToString()].Value.Value = null;
        }
    }
}