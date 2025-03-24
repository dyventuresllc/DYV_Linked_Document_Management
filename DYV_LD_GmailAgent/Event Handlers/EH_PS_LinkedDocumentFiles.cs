using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using DYV_Linked_Document_Management.Utilities;
using kCura.EventHandler;
using Relativity.API;

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
                retVal.Add(new Field(0,                         // artifactID
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
            statusHtml.Append("<div style='font-family: monospace; font-size: 11px; padding-left: 20px;'>");

            try
            {
                // Only process new records
                if (this.ActiveArtifact.IsNew)
                {
                    // Get the file name
                    string fileName = GetFileName();
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        // Update the File Identifier field
                        var fileNameField = ActiveArtifact.Fields.Cast<Field>()
                            .Where(field => field.Name == "File Identifier")
                            .FirstOrDefault();

                        if (fileNameField != null)
                        {
                            fileNameField.Value.Value = fileName;
                            statusHtml.Append($"<span style='color: green;'>✓ File Identifier has been updated to reflect File GUID (name): {fileName}</span><br/>");
                        }
                        else
                        {
                            retVal.Success = false;
                            retVal.Message = "File Identifier field not found";
                            // Return immediately with error message, no need to update status
                            return retVal;
                        }
                    }

                    // Validate Custodian
                    ValidateCustodian(statusHtml, ref retVal);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in EH_PS_LinkedDocumentFiles");
                retVal.Success = false;
                retVal.Message = $"Error: {ex.Message}";
                return retVal;
            }

            // Only update the status field if successful
            statusHtml.Append("</div>");
            this.ActiveArtifact.Fields[helper.LdfStatus.ToString()].Value.Value = statusHtml.ToString();

            return retVal;
        }

        private string GetFileName()
        {
            var fileField = ActiveArtifact.Fields.Cast<Field>()
                .Where(field => field.FieldTypeID == 9)
                .FirstOrDefault();

            if (fileField != null && !fileField.Value.IsNull)
            {
                var fileFieldValue = (FileFieldValue)fileField.Value;
                if (fileFieldValue.FileValue != null)
                {
                    return System.IO.Path.GetFileName(fileFieldValue.FileValue.FilePath);
                }
            }

            return null;
        }

        private void ValidateCustodian(StringBuilder statusHtml, ref Response retVal)
        {
            try
            {
                var dbContext = Helper.GetDBContext(Helper.GetActiveCaseID());

                // Get target workspace ID
                var targetWorkspaceIdSql = "SELECT TargetWorkspaceArtifactId FROM eddsdbo.LinkedDocumentConfiguration";
                var targetWorkspaceId = (int)dbContext.ExecuteSqlStatementAsScalar(targetWorkspaceIdSql);

                // Switch to target workspace context
                var targetDbContext = Helper.GetDBContext(targetWorkspaceId);

                // Get custodian information directly from the required field
                var helper = new DYVLDHelper(this.Helper, Helper.GetLoggerFactory().GetLogger());
                var custodianField = this.ActiveArtifact.Fields[helper.LdfCustodianId.ToString()];
                int custodianArtifactId = Convert.ToInt32(custodianField.Value.Value);

                // Query the entity in the target workspace
                string custodianSql = $"SELECT ArtifactId, FullName FROM eddsdbo.Entity WHERE ArtifactId = {custodianArtifactId}";
                var custodianResult = targetDbContext.ExecuteSqlStatementAsDataTable(custodianSql);

                if (custodianResult != null && custodianResult.Rows.Count > 0)
                {
                    var artifactId = custodianResult.Rows[0]["ArtifactId"].ToString();
                    var fullName = custodianResult.Rows[0]["FullName"].ToString();

                    statusHtml.Append($"<span style='color: green;'>✓ Custodian for processing: Id[{artifactId}] - Name[{fullName}]</span><br/>");
                }
                else
                {
                    // Log more helpful information
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
                // Return immediately with error message, no need to update status
                return;
            }
        }
    }
}