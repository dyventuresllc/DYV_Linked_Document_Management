﻿using DYV_Linked_Document_Management.Handlers;
using DYV_Linked_Document_Management.Utilities;
using kCura.EventHandler;
using Relativity.API;
using Relativity.Services.Objects;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using Relativity.HostingBridge.V1.AgentStatusManager;
using System.Threading.Tasks;
using kCura.EventHandler.CustomAttributes;

namespace DYV_Linked_Document_Management.Event_Handlers
{
    [Description("Console event handler for linked document files operation")]
    [System.Runtime.InteropServices.Guid("9CEF33B4-372A-4C37-8A38-60105E08735E")]
    public class EH_Cnsl_LinkedDocumentFiles : ConsoleEventHandler
    {
        private IAPILog logger;

        public override FieldCollection RequiredFields
        {
            get
            {
                FieldCollection retVal = new FieldCollection();
                var helper = new DYVLDHelper(this.Helper, Helper.GetLoggerFactory().GetLogger());

                // Define required fields with their specific field types
                var requiredFields = new List<(Guid Guid, int FieldTypeID)>
                {
                    (helper.LdfFileIdentifer, 0),   
                    (helper.LdfStatus, 4),          
                    (helper.LdfObjectID_GM_Metadata, 0),  
                    (helper.LdfFilesTableId, 0)     
                };

                foreach (var field in requiredFields)
                {
                    retVal.Add(new Field(0,          // artifactID
                        "Visible",                   // name
                        "Visible",                   // columnName
                        field.FieldTypeID,           // Use the specific field type
                        0,                           // codeTypeID
                        0,                           // fieldCategoryID
                        false,                       // isReflected
                        false,                       // isInLayout
                        null,                        // value
                        new List<Guid> { field.Guid }  // guids
                    ));
                }

                return retVal;
            }
        }

        public override kCura.EventHandler.Console GetConsole(PageEvent pageEvent)
        {
            logger = Helper.GetLoggerFactory().GetLogger();

            // Create console with a button
            kCura.EventHandler.Console returnConsole = new kCura.EventHandler.Console()
            {
                Items = new List<IConsoleItem>()
            };

            // Add Submit button - no validation needed since pre-save handles it
            returnConsole.Items.Add(new ConsoleButton()
            {
                Name = "SubmitImportJob",
                DisplayText = "Submit File for Import",
                Enabled = true,
                RaisesPostBack = true
            });

            returnConsole.Items.Add(new ConsoleSeparator());

            // Add refresh button with direct JavaScript
            ConsoleButton refreshButton = new ConsoleButton()
            {
                Name = "RefreshPage",
                DisplayText = "Refresh Page",
                ToolTip = "Refresh the current page",
                Enabled = true,
                RaisesPostBack = false,
                OnClickEvent = "window.location.reload();",
                CssClass = "refresh-button"
            };

            returnConsole.AddScriptBlock("refreshButtonStyle", @"
            <style>
                .refresh-button {
                    background-color: #2980b9;
                    color: white;
                    border: 1px solid #2573a7;
                }
                .refresh-button:hover {
                    background-color: #3498db;
                }
            </style>");

            returnConsole.Items.Add(refreshButton);
            return returnConsole;
        }

        public override async void OnButtonClick(ConsoleButton consoleButton)
        {
            var helper = new DYVLDHelper(this.Helper, Helper.GetLoggerFactory().GetLogger());
            var statusHtml = new StringBuilder();

            var existingStatus = this.ActiveArtifact.Fields[helper.LdfStatus.ToString()].Value.Value?.ToString() ?? "";
            statusHtml.Insert(0, existingStatus);
            statusHtml.Append("<div style='font-family:Consolas,monospace;font-size:0.9em;'>");
            try
            {
                switch (consoleButton.Name)
                {
                    case "SubmitImportJob":
                        bool isSuccess = ValidateAndPrepareImport(helper, out string sourceId, out int workspaceId,
                            out string fileTypeValue, out string submittedFilePath,
                            out int? importObjectTypeId, out int custodianId);

                        var timestamp = DateTime.UtcNow.ToString("MM/dd/yyyy HH:mm:ss") + " UTC";

                        if (isSuccess)
                        {
                            InsertIntoImportQueue(
                                workspaceId,
                                submittedFilePath,
                                fileTypeValue,
                                ActiveArtifact.ArtifactID,
                                sourceId,
                                importObjectTypeId,
                                custodianId
                            );

                            statusHtml.Append($"<div style='margin:0;padding:0 0 0 10px;'><span style='color:#555;font-weight:bold;'>[{timestamp}]</span> <span style='color:green;font-weight:bold;'>File has been submitted for import <b>successfully</b>.</span></div>");
                        }
                        else
                        {
                            statusHtml.Append($"<div style='margin:0;padding:0 0 0 10px;'><span style='color:#555;font-weight:bold;'>[{timestamp}]</span> <span style='color:red;font-weight:bold;'>File has not been submitted. Errors occurred, please resolve and resubmit.</span></div>");
                        }

                        IServicesMgr servicesMgr = Helper.GetServicesManager();
                        using (var objectManager = servicesMgr.CreateProxy<IObjectManager>(ExecutionIdentity.System))
                        {
                            await ObjectHandler.UpdateFieldValueAsync(objectManager,
                                Helper.GetActiveCaseID(),
                                ActiveArtifact.ArtifactID,
                                helper.LdfStatus,
                                statusHtml.ToString() + "</div>",
                                helper.Logger);
                        }

                        await StartImportAgentAsync();
                        break;
                }
            }
            catch (Exception ex)
            {
                helper.Logger.LogError($"Error submitting file for import: {ex.Message}");
                var timestamp = DateTime.UtcNow + " UTC";
                statusHtml.Append($"<div style='margin:0;padding:0 0 0 10px;'><span style='color:#555;font-weight:bold;'>[{timestamp}]</span> <span style='color:red;font-weight:bold;'>File has not been submitted. Errors occurred, please resolve and resubmit.</span></div>");
                this.ActiveArtifact.Fields[helper.LdfStatus.ToString()].Value.Value = statusHtml.ToString() + "</div>";
            }
        }

        private bool ValidateAndPrepareImport(DYVLDHelper helper, out string sourceId, out int workspaceId,
            out string fileTypeValue, out string submittedFilePath,
            out int? importObjectTypeId, out int custodianId)
        {
            sourceId = null;
            workspaceId = Helper.GetActiveCaseID();
            fileTypeValue = string.Empty;
            submittedFilePath = null;
            importObjectTypeId = null;
            custodianId = 0;
            bool isSuccess = true;

            // Get custodian
            custodianId = (int)this.ActiveArtifact.Fields[helper.LdfCustodianId.ToString()].Value.Value;

            // Get source ID
            var fileIdField = this.ActiveArtifact.Fields[helper.LdfFileIdentifer.ToString()];
            if (fileIdField != null && !fileIdField.Value.IsNull)
            {
                sourceId = fileIdField.Value.Value.ToString();
            }
            else
            {
                isSuccess = false;
            }

            // Get file type
            var fileTypeField = this.ActiveArtifact.Fields[helper.LdfFileType.ToString()];
            if (fileTypeField != null && !fileTypeField.Value.IsNull)
            {
                try
                {
                    ChoiceFieldValue cfv = (ChoiceFieldValue)fileTypeField.Value;
                    foreach (Choice c in cfv.Choices)
                    {
                        fileTypeValue = c.Name;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error processing choice field: {ex.Message}");
                    isSuccess = false;
                }
            }
            else
            {
                isSuccess = false;
            }

            // Get file path
            submittedFilePath = GetFilePath(helper);
            if (string.IsNullOrEmpty(submittedFilePath))
            {
                isSuccess = false;
            }

            // Determine import object type
            string normalizedFileType = fileTypeValue?.Replace(" ", "").ToLowerInvariant();
            switch (normalizedFileType)
            {
                case "g-emailmetadata(.csv)":
                    importObjectTypeId = (int)this.ActiveArtifact.Fields[helper.LdfObjectID_GM_Metadata.ToString()].Value.Value;
                    if (!importObjectTypeId.HasValue)
                    {
                        isSuccess = false;
                    }
                    break;
            }

            return isSuccess;
        }        

        private string GetFilePath(DYVLDHelper helper)
        {
            try
            {
                var workspaceDbContext = Helper.GetDBContext(Helper.GetActiveCaseID());

                string fileSql = $"SELECT Location FROM eddsdbo.File{this.ActiveArtifact.Fields[helper.LdfFilesTableId.ToString()].Value.Value} WHERE ObjectArtifactID = {this.ActiveArtifact.ArtifactID}";
                DataTable fileResult = workspaceDbContext.ExecuteSqlStatementAsDataTable(fileSql);
                return fileResult.Rows[0]["Location"].ToString();
            }
            catch (Exception ex)
            {
                logger.LogError($"Error obtaining File location for inmport job: {ex.Message}");
                return null;
            }
        }

        private void InsertIntoImportQueue(int workspaceId, string filePath, string fileType,
                                           int objectArtifactId, string sourceId, int? importObjectTypeId, int custodianId)
        {
            var eddsDbContext = Helper.GetDBContext(-1);
            try
            {
                // Build SQL parameters
                var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@ImportIdentifier", new Guid(sourceId)), // Using the File Identifier from the current record
                    new SqlParameter("@ImportWorkspaceArtifactId", workspaceId),
                    new SqlParameter("@ImportFilePath", filePath), // Full path from database
                    new SqlParameter("@LDFObjectArtifactId", objectArtifactId), // This is the current record's ArtifactID (this.ActiveArtifact.ArtifactID)
                    new SqlParameter("@FileType", fileType),
                    new SqlParameter("@CustodianId", custodianId)
                };
                // Add optional ImportObjectTypeId if available
                if (importObjectTypeId.HasValue)
                {
                    parameters.Add(new SqlParameter("@ImportObjectTypeArtifactId", importObjectTypeId.Value));
                }
                else
                {
                    parameters.Add(new SqlParameter("@ImportObjectTypeArtifactId", DBNull.Value));
                }
                // SQL for inserting into import queue
                string sql = @"
                    INSERT INTO [QE].[LinkedDocumentImportQueue]
                    (
                        [ImportIdentifier],
                        [ImportWorkspaceArtifactId],
                        [ImportFilePath],
                        [LDFObjectArtifactId],
                        [ImportObjectTypArtifactId],
                        [FileType],
                        [SubmittedDateTime],
                        [CustodianId]
                    )
                    VALUES
                    (
                        @ImportIdentifier,
                        @ImportWorkspaceArtifactId,
                        @ImportFilePath,
                        @LDFObjectArtifactId,
                        @ImportObjectTypeArtifactId,
                        @FileType,                        
                        GETUTCDATE(),
                        @custodianId
                    )";
                // Execute the insert
                eddsDbContext.ExecuteNonQuerySQLStatement(sql, parameters.ToArray());
                string fileName = System.IO.Path.GetFileName(filePath);
                logger.LogInformation($"Successfully inserted file {fileName} into import queue for workspace {workspaceId}");
            }
            catch (Exception ex)
            {
                logger.LogError($"Error inserting into import queue: {ex.Message}");
                throw;
            }
        }

        private async Task StartImportAgentAsync()
        {
            try
            {
                //Get the agent GUID from the database
                var eddsDbContext = Helper.GetDBContext(-1);
                string agentGuidSql = @"SELECT AgentTypeGuid FROM eddsdbo.ExtendedAgent WHERE AgentTypeName like '%DYV Linked Document Mgmt - Import%'";
                DataTable result = eddsDbContext.ExecuteSqlStatementAsDataTable(agentGuidSql);
                if (result == null || result.Rows.Count == 0)
                {
                    logger.LogError("Linked Document Import Agent not found");
                    return;
                }
                Guid agentGuid = new Guid(result.Rows[0]["AgentTypeGuid"].ToString());
                //Start the agent using the agent Manager API
                IServicesMgr servicesMgr = Helper.GetServicesManager();
                using (var agentManager = servicesMgr.CreateProxy<IAgentStatusManagerService>(ExecutionIdentity.System))
                {
                    await agentManager.StartAgentAsync(agentGuid);
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error starting import agent: {ex.Message}");
            }
        }
    }
}