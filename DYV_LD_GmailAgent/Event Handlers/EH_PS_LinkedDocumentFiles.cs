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
                   )); ;
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
                // Check if this is a new record
                bool isNewRecord = this.ActiveArtifact.IsNew;

                switch (isNewRecord)
                {
                    case true:
                        string fileName = null;

                        var fileField = ActiveArtifact.Fields.Cast<Field>().Where(field => field.FieldTypeID == 9).FirstOrDefault();                        

                        if (fileField != null && !fileField.Value.IsNull)
                        {
                            var fileFieldValue = (FileFieldValue)fileField.Value;                            
                            if (fileFieldValue.FileValue != null)
                            {                                
                                // Get just the filename part from the path
                                fileName = System.IO.Path.GetFileName(fileFieldValue.FileValue.FilePath);                                     
                            }
                        }

                        // If we found a filename, update fields
                        if (!string.IsNullOrEmpty(fileName))
                        {
                            // Set the File Name field (Type 0)
                            var fileNameField = ActiveArtifact.Fields.Cast<Field>()
                                .Where(field => field.Name == "File Identifier")
                                .FirstOrDefault();

                            if (fileNameField != null)
                            {
                                fileNameField.Value.Value = fileName;
                                statusHtml.Append($"File Identifier has been updated to reflect File GUID (name): {fileName}<br/>");
                            }
                            else
                            {
                                retVal.Success = false;
                                retVal.Message = "Unspecified Error could not save record.";
                                return retVal;
                            }
                        }
                        break;

                    case false:
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in EH_PS_LinkedDocumentFiles");
                statusHtml.Append($"<span style='color: red;'>✗ Error: {ex.Message}</span><br/>");
            }

            // Update the status field
            statusHtml.Append("</div>");
            this.ActiveArtifact.Fields[helper.LdfStatus.ToString()].Value.Value = statusHtml.ToString();
            return retVal;
        }

    }
}