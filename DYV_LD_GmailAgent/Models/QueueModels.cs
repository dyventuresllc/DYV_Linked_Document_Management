using System;

namespace DYV_Linked_Document_Management.Models
{
    /// <summary>
    /// Represents an item in the LinkedDocument import queue
    /// </summary>
    public class ImportQueueItem
    {
        /// <summary>
        /// Unique identifier for the import queue item
        /// </summary>
        public int ImportQueueId { get; set; }

        /// <summary>
        /// Identifier for the import (usually a GUID)
        /// </summary>
        public string ImportIdentifier { get; set; }

        /// <summary>
        /// Path to the file to be imported
        /// </summary>
        public string ImportFilePath { get; set; }

        /// <summary>
        /// Artifact ID of the LinkedDocument Files object
        /// </summary>
        public int LDFObjectArtifactId { get; set; }

        /// <summary>
        /// Artifact ID of the workspace where the import will occur
        /// </summary>
        public int ImportWorkspaceArtifactId { get; set; }

        /// <summary>
        /// Artifact ID of the object type to import into
        /// </summary>
        public int ImportObjectTypArtifactId { get; set; }

        /// <summary>
        /// Type of file being imported (e.g., "G - Email Metadata (.csv)")
        /// </summary>
        public string FileType { get; set; }

        /// <summary>
        /// ID of the custodian associated with this import
        /// </summary>
        public int CustodianId { get; set; }

        /// <summary>
        /// When the job was submitted to the queue
        /// </summary>
        public DateTime SubmittedDateTime { get; set; }

        /// <summary>
        /// ID of the agent that processed the import
        /// </summary>
        public int? ImportAgentId { get; set; }

        /// <summary>
        /// When an agent started processing this job
        /// </summary>
        public DateTime? ImportStartedDateTime { get; set; }

        /// <summary>
        /// When the import was completed
        /// </summary>
        public DateTime? ImportCompletedDateTime { get; set; }

        /// <summary>
        /// Import job ID for the Relativity import
        /// </summary>
        public Guid? Import_ImportId { get; set; }

        /// <summary>
        /// Source ID for the Relativity import
        /// </summary>
        public Guid? Import_SourceId { get; set; }

        /// <summary>
        /// ID of the agent that processed the overlay
        /// </summary>
        public int? OverlayAgentId { get; set; }

        /// <summary>
        /// Import job ID for the Relativity overlay
        /// </summary>
        public Guid? Overlay_ImportId { get; set; }

        /// <summary>
        /// Source ID for the Relativity overlay
        /// </summary>
        public Guid? Overlay_SourceId { get; set; }

        /// <summary>
        /// When an agent started processing the overlay
        /// </summary>
        public DateTime? OverlayStartedDateTime { get; set; }

        /// <summary>
        /// When the overlay was completed
        /// </summary>
        public DateTime? OverlayCompletedDateTime { get; set; }

        /// <summary>
        /// Returns a string representation of this import queue item
        /// </summary>
        public override string ToString()
        {
            string importStatus = "Pending";
            if (ImportCompletedDateTime.HasValue)
                importStatus = "Completed";
            else if (ImportStartedDateTime.HasValue)
                importStatus = "In Progress";

            string overlayStatus = "Not Started";
            if (OverlayCompletedDateTime.HasValue)
                overlayStatus = "Completed";
            else if (OverlayStartedDateTime.HasValue)
                overlayStatus = "In Progress";

            return $"ImportQueueId: {ImportQueueId}, FileType: {FileType}, Import Status: {importStatus}, Overlay Status: {overlayStatus}";
        }
    }

    /// <summary>
    /// Represents an item in the LinkedDocument overlay queue
    /// </summary>
    public class OverlayQueueItem
    {
        /// <summary>
        /// Unique identifier for the overlay queue item
        /// </summary>
        public int OverlayQueueId { get; set; }

        /// <summary>
        /// Identifier for the overlay (usually a GUID)
        /// </summary>
        public string OverlayIdentifier { get; set; }

        /// <summary>
        /// Path to the file to be used for overlay
        /// </summary>
        public string OverlayFilePath { get; set; }

        /// <summary>
        /// Artifact ID of the LinkedDocument Files object
        /// </summary>
        public int LDFObjectArtifactId { get; set; }

        /// <summary>
        /// Artifact ID of the workspace where the overlay will occur
        /// </summary>
        public int OverlayWorkspaceArtifactId { get; set; }

        /// <summary>
        /// Artifact ID of the object type to overlay
        /// </summary>
        public int OverlayObjectTypArtifactId { get; set; }

        /// <summary>
        /// Type of file being used for overlay (e.g., "G - Email Metadata (.csv)")
        /// </summary>
        public string FileType { get; set; }

        /// <summary>
        /// When the job was submitted to the queue
        /// </summary>
        public DateTime? SubmittedDateTime { get; set; }

        /// <summary>
        /// When an agent started processing this job
        /// </summary>
        public DateTime? OverlayStartedDateTime { get; set; }

        /// <summary>
        /// When the overlay was completed
        /// </summary>
        public DateTime? OverlayCompletedDateTime { get; set; }

        /// <summary>
        /// Whether the overlay was successful
        /// </summary>
        public bool? OverlaySuccessful { get; set; }

        /// <summary>
        /// Error message if the overlay failed
        /// </summary>
        public string OverlayErrorMessage { get; set; }

        /// <summary>
        /// ID of the agent that processed this job
        /// </summary>
        public string OverlayAgentId { get; set; }

        /// <summary>
        /// Returns a string representation of this overlay queue item
        /// </summary>
        public override string ToString()
        {
            return $"OverlayQueueId: {OverlayQueueId}, FileType: {FileType}, Status: {(OverlayCompletedDateTime.HasValue ? (OverlaySuccessful.GetValueOrDefault() ? "Completed" : "Failed") : (OverlayStartedDateTime.HasValue ? "In Progress" : "Pending"))}";
        }
    }
}