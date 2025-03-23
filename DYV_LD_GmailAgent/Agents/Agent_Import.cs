using System;
using System.Threading.Tasks;
using DYV_Linked_Document_Management.Handlers;
using DYV_Linked_Document_Management.Logging;
using DYV_Linked_Document_Management.Managers;
using kCura.Agent;
using Relativity.API;

namespace DYV_Linked_Document_Management.Agents
{
    [kCura.Agent.CustomAttributes.Name("DYV Linked Document Mgmt - Import")]
    [System.Runtime.InteropServices.Guid("84F60C42-C163-485B-8009-99BB31C58956")]
    public class DYVLDM_ImportAgent : AgentBase
    {
        private IAPILog _relativityLogger;
        private ILDLogger _ldLogger;
        private IDBContext _eddsDbContext;
        private QueueHandler _queueHandler;
        private JobManager _jobManager;

        public override string Name => "DYV Linked Document Mgmt - Import";

        public override void Execute()
        {
            Initialize();

            try
            {
                _ldLogger.LogInformation($"Import Agent started with Agent ID: {this.AgentID}");

                // Get the next available import job
                var importJob = _queueHandler.GetNextImportJob();
                if (importJob == null)
                {                    
                    return;
                }

                // Mark the job as in progress using the AgentID
                _queueHandler.MarkImportJobAsInProgress(importJob.ImportQueueId, this.AgentID.ToString());

                // Improved log message with more details about the job
                _ldLogger.LogInformation($"Found import job: {importJob.ImportQueueId} with identifier: {importJob.ImportIdentifier}, for workspace: {importJob.ImportWorkspaceArtifactId}, file type: {importJob.FileType}");
                _ldLogger.LogInformation($"Marked import job: {importJob.ImportQueueId} as in progress with agent ID: {this.AgentID}");

                try
                {
                    // Process the job using the job manager
                    Task.Run(async () => await _jobManager.ProcessImportJob(importJob)).Wait();
                }
                catch (Exception ex)
                {
                    // The JobManager already handles error logging and updating the job status,
                    // but we'll log here at the agent level as well
                    _ldLogger.LogError(ex, $"Error processing import job {importJob.ImportQueueId} with identifier: {importJob.ImportIdentifier}");
                    _relativityLogger.LogError(ex, $"Error processing import job {importJob.ImportQueueId} with identifier: {importJob.ImportIdentifier}");
                    _relativityLogger.LogError($"Stack Trace: {ex.StackTrace}");
                }
            }
            catch (Exception ex)
            {
                _ldLogger.LogError(ex, "Unhandled exception in DYVLDM - Import Agent");
                _relativityLogger.LogError(ex, "Unhandled exception in DYVLDM - Import Agent");
                _relativityLogger.LogError($"Stack Trace: {ex.StackTrace}");
            }
        }

        private void Initialize()
        {
            _relativityLogger = Helper.GetLoggerFactory().GetLogger().ForContext<DYVLDM_ImportAgent>();
            _eddsDbContext = Helper.GetDBContext(-1);
            _ldLogger = LoggerFactory.CreateLogger<DYVLDM_ImportAgent>(_eddsDbContext, Helper, _relativityLogger);

            try
            {
                _queueHandler = new QueueHandler(_eddsDbContext, _ldLogger);
                _jobManager = new JobManager(Helper, _ldLogger, _queueHandler);

                _ldLogger.LogInformation("DYVLDM Import Agent initialized successfully");
            }
            catch (Exception ex)
            {
                _ldLogger.LogError(ex, "Error initializing DYVLDM Import Agent dependencies");
                _relativityLogger.LogError(ex, "Error initializing DYVLDM Import Agent dependencies");
                _relativityLogger.LogError($"Stack Trace: {ex.StackTrace}");
                throw; // Re-throw to prevent agent from running with uninitialized dependencies
            }
        }
    }
}