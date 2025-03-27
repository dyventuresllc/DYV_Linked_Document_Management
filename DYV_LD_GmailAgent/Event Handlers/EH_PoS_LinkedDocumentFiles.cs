using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using DYV_Linked_Document_Management.Handlers;
using DYV_Linked_Document_Management.Utilities;
using kCura.EventHandler;
using Relativity.API;
using Relativity.Services.Objects;
using Field = kCura.EventHandler.Field;

namespace DYV_Linked_Document_Management.Event_Handlers
{
    public class EH_PoS_LinkedDocumentFiles : PostSaveEventHandler
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

                var requiredFields = new List<(Guid Guid, int FieldTypeID)>
                {
                    (helper.LdfFileIdentifer,0),
                    (helper.LdfStatus, 4),                    
                };

                foreach(var field in requiredFields)
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

            var existingStatus = this.ActiveArtifact.Fields[helper.LdfStatus.ToString()].Value.Value?.ToString() ?? "";
            statusHtml.Insert(0, existingStatus);
            statusHtml.Append("<div style='font-family:Consolas,monospace;font-size:0.9em;'>");

            try
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
                    string formattedTimestamp = DateTime.UtcNow.ToString("MM/dd/yyyy HH:mm:ss") + " UTC";
                    statusHtml.Append($"<div style='margin:0;padding:0 0 0 10px;'><span style='color:#555;font-weight:bold;'>[{formattedTimestamp}]</span> File Identifier has been updated to reflect File GUID (name): <span style='color:#0066cc;'>{newfileName}</span></div>");

                    IServicesMgr servicesMgr = Helper.GetServicesManager();
                    using (var objectManager = servicesMgr.CreateProxy<IObjectManager>(ExecutionIdentity.System))
                    {
                        ObjectHandler.UpdateFieldValueAsync(objectManager,
                            Helper.GetActiveCaseID(),
                            ActiveArtifact.ArtifactID,
                            helper.LdfStatus,
                            statusHtml.ToString() + "</div>",
                            helper.Logger).GetAwaiter().GetResult();

                        ObjectHandler.UpdateFieldValueAsync(objectManager,
                            Helper.GetActiveCaseID(),
                            ActiveArtifact.ArtifactID,
                            helper.LdfFileIdentifer,
                            newfileName,
                            helper.Logger).GetAwaiter().GetResult();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in EH_PoS_LinkedDocumentFiles");
                retVal.Success = false;
                retVal.Message = $"Error: {ex.Message}";
                return retVal;
            }          
            return retVal;
        }       
    }
}