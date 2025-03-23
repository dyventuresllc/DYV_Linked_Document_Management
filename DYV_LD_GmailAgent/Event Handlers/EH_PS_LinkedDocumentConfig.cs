using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using DYV_Linked_Document_Management.Utilities;
using kCura.EventHandler;
namespace DYV_Linked_Document_Management.Event_Handlers
{
    public class EH_PS_LinkedDocumentConfig : PreSaveEventHandler
    {
        public override FieldCollection RequiredFields
        {
            get
            {
                var retVal = new FieldCollection();
                var helper = new DYVLDHelper(this.Helper, Helper.GetLoggerFactory().GetLogger());
                retVal.Add(new Field(0,          // artifactID
                    "Visible",                   // name
                    "Visible",                   // columnName
                    3,                           // fieldTypeID
                    0,                           // codeTypeID
                    0,                           // fieldCategoryID
                    false,                       // isReflected
                    false,                       // isInLayout
                    null,                        // value
                    new List<Guid> { helper.LdcSetupValidated }  // guids
                    ));
                
                retVal.Add(new Field(0,          // artifactID
                    "Visible",                   // name
                    "Visible",                   // columnName
                    1,                           // fieldTypeID
                    0,                           // codeTypeID
                    0,                           // fieldCategoryID
                    false,                       // isReflected
                    false,                       // isInLayout
                    null,                        // value
                    new List<Guid> { helper.LdcLdfObjectId }  // guids
                    ));

                retVal.Add(new Field(0,          // artifactID
                    "Visible",                   // name
                    "Visible",                   // columnName
                    1,                           // fieldTypeID
                    0,                           // codeTypeID
                    0,                           // fieldCategoryID
                    false,                       // isReflected
                    false,                       // isInLayout
                    null,                        // value
                    new List<Guid> { helper.LdcLdfFilesTableId }  // guids
                    ));

                retVal.Add(new Field(0,          // artifactID
                    "Visible",                   // name
                    "Visible",                   // columnName
                    1,                           // fieldTypeID
                    0,                           // codeTypeID
                    0,                           // fieldCategoryID
                    false,                       // isReflected
                    false,                       // isInLayout
                    null,                        // value
                    new List<Guid> { helper.LdcObjectID_G_Metadata }  // guids
                    ));

                retVal.Add(new Field(0,          // artifactID
                    "Visible",                   // name
                    "Visible",                   // columnName
                    4,                           // fieldTypeID
                    0,                           // codeTypeID
                    0,                           // fieldCategoryID
                    false,                       // isReflected
                    false,                       // isInLayout
                    null,                        // value
                    new List<Guid> { helper.LdcStatus }  // guids
                    ));
                return retVal;
            }
        }
        public override Response Execute()
        {
            var retVal = new Response
            {
                Success = true,
                Message = string.Empty
            };
            var helper = new DYVLDHelper(this.Helper, Helper.GetLoggerFactory().GetLogger());                        
            var statusHtml = new StringBuilder();            
            statusHtml.Append("<div style='font-family: monospace; font-size: 11px; padding-left: 20px;'>");
            bool error = false;
            try
            {
                // Get target workspace ID from the form
                int targetWorkspaceId = 0;
                targetWorkspaceId = Convert.ToInt32(this.ActiveArtifact.Fields[helper.LdcTargetWorkspaceId.ToString()].Value.Value);
            
                // Check if target workspace exists
                var eddsDbContext = Helper.GetDBContext(-1);
                string workspaceNameSql = $"SELECT [Name] FROM eddsdbo.ExtendedCase WHERE ArtifactID = {targetWorkspaceId}";
                DataTable workspaceResult = eddsDbContext.ExecuteSqlStatementAsDataTable(workspaceNameSql);
                if (workspaceResult == null || workspaceResult.Rows.Count == 0)
                {
                    retVal.Success = false;
                    retVal.Message = $"Target Workspace with ID {targetWorkspaceId} was not found.";
                    return retVal;
                }
                    string workspaceName = workspaceResult.Rows[0]["Name"].ToString();
                    statusHtml.Append($"<span style='color: green;'>✓ Target Workspace: </span>{workspaceName} (ID: {targetWorkspaceId})<br/>");
                   
                    // Get database context for target workspace
                    var dbContext = Helper.GetDBContext(targetWorkspaceId);
                    string objectTypeSql = string.Empty;
                    DataTable objectTypeResult;

                // Query 1: Get the Linked Document Files ObjectTypeId
                objectTypeSql = "SELECT DescriptorArtifactTypeID FROM eddsdbo.ObjectType WHERE [Name] like 'Linked Document Files'";
                objectTypeResult = dbContext.ExecuteSqlStatementAsDataTable(objectTypeSql);
                if (objectTypeResult != null && objectTypeResult.Rows.Count > 0)
                {
                    int objectTypeID = Convert.ToInt32(objectTypeResult.Rows[0]["DescriptorArtifactTypeID"]);
                    this.ActiveArtifact.Fields[helper.LdcLdfObjectId.ToString()].Value.Value = objectTypeID;                   
                    statusHtml.Append($"<span style='color: green;'>✓ Linked Document Files Object ID: </span>{objectTypeID}<br/>");
                }
                else
                {                                        
                    statusHtml.Append("<div style='font-family: monospace; font-size: 11px;'>");
                    statusHtml.Append("<span style='color: red;'>✗ Error: No Linked Document Files object type found in the target workspace.</span><br/>");                    
                    this.ActiveArtifact.Fields[helper.LdcSetupValidated.ToString()].Value.Value = false;
                    this.ActiveArtifact.Fields[helper.LdcLdfObjectId.ToString()].Value.Value = null;
                    error = true;
                }

                // Query 2: Get the Gmail CSV Metadata ObjectTypeId
                objectTypeSql = "SELECT DescriptorArtifactTypeID FROM eddsdbo.ObjectType WHERE [Name] like 'Gmail (CSV) Metadata'";
                objectTypeResult = dbContext.ExecuteSqlStatementAsDataTable(objectTypeSql);
                if (objectTypeResult != null && objectTypeResult.Rows.Count > 0)
                {
                    int objectTypeID = Convert.ToInt32(objectTypeResult.Rows[0]["DescriptorArtifactTypeID"]);
                    this.ActiveArtifact.Fields[helper.LdcObjectID_G_Metadata.ToString()].Value.Value = objectTypeID;
                    statusHtml.Append($"<span style='color: green;'>✓ Gmail (CSV) Metadata Object ID: </span>{objectTypeID}<br/>");
                }
                else
                {
                    statusHtml.Append("<div style='font-family: monospace; font-size: 11px;'>");
                    statusHtml.Append("<span style='color: red;'>✗ Error: No Gmail (CSV) Metadata object type found in the target workspace.</span><br/>");
                    this.ActiveArtifact.Fields[helper.LdcSetupValidated.ToString()].Value.Value = false;
                    this.ActiveArtifact.Fields[helper.LdcLdfObjectId.ToString()].Value.Value = null;
                    error = true;
                }

                // Query 3: Get the File Field ID
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
                    this.ActiveArtifact.Fields[helper.LdcLdfFilesTableId.ToString()].Value.Value = fileFieldID;
                    statusHtml.Append($"<span style='color: green;'>✓ Linked Document Files Field ID: </span>{fileFieldID}<br/>");
                }
                else
                {                    
                    statusHtml.Append("<span style='color: red;'>✗ Error: No File field found for Linked Document Files object type.</span><br/>");                    
                    this.ActiveArtifact.Fields[helper.LdcSetupValidated.ToString()].Value.Value = false;
                    this.ActiveArtifact.Fields[helper.LdcLdfFilesTableId.ToString()].Value.Value = null;                    
                    error = true;
                }
            }
            catch (Exception ex)
            {
                this.ActiveArtifact.Fields[helper.LdcLdfFilesTableId.ToString()].Value.Value = null;
                this.ActiveArtifact.Fields[helper.LdcLdfObjectId.ToString()].Value.Value = null;

                statusHtml.Append($"<span style='color: red;'>✗  Error: {ex.Message}</span>");
                this.ActiveArtifact.Fields[helper.LdcStatus.ToString()].Value.Value = statusHtml.ToString();
                return retVal;
            }

            if (error)
            {
                statusHtml.Append("<span style='color: red;'>✗ Configuration not complete!</span>");
                this.ActiveArtifact.Fields[helper.LdcSetupValidated.ToString()].Value.Value = false;
            }
            else
            {
                statusHtml.Append("<span style='color: green; font-weight: bold;'>✓ Configuration completed successfully!</span>");
                this.ActiveArtifact.Fields[helper.LdcSetupValidated.ToString()].Value.Value = true;
            }
                        
            statusHtml.Append("</div>");
            this.ActiveArtifact.Fields[helper.LdcStatus.ToString()].Value.Value = statusHtml.ToString();            
            return retVal;
        }
    }
}