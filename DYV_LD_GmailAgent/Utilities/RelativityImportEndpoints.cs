using System;

namespace DYV_Linked_Document_Management.Utilities
{
    /// <summary>
    /// Helper class containing Relativity Import API endpoints
    /// </summary>
    public static class RelativityImportEndpoints
    {
        // Import Job section
        public static string GetImportJobCreateUri(int workspaceId, Guid importId) =>
            $"Relativity.REST/api/import-service/v1/workspaces/{workspaceId}/import-jobs/{importId}";
        public static string GetImportJobBeginUri(int workspaceId, Guid importId) =>
            $"Relativity.REST/api/import-service/v1/workspaces/{workspaceId}/import-jobs/{importId}/begin";
        public static string GetImportJobEndUri(int workspaceId, Guid importId) =>
            $"Relativity.REST/api/import-service/v1/workspaces/{workspaceId}/import-jobs/{importId}/end";
        // RDO Configuration section
        public static string GetRdoConfigurationUri(int workspaceId, Guid importId) =>
            $"Relativity.REST/api/import-service/v1/workspaces/{workspaceId}/import-jobs/{importId}/rdos-configurations";
        // Data Source section
        public static string GetImportSourceUri(int workspaceId, Guid importId, Guid sourceId) =>
            $"Relativity.REST/api/import-service/v1/workspaces/{workspaceId}/import-jobs/{importId}/sources/{sourceId}";
        public static string GetImportSourceDetailsUri(int workspaceId, Guid importId, Guid sourceId) =>
            $"Relativity.REST/api/import-service/v1/workspaces/{workspaceId}/import-jobs/{importId}/sources/{sourceId}/details";
        public static string GetImportSourceProgressUri(int workspaceId, Guid importId, Guid sourceId) =>
            $"Relativity.REST/api/import-service/v1/workspaces/{workspaceId}/import-jobs/{importId}/sources/{sourceId}/progress";
    }
}
