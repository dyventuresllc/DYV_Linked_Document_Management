using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Relativity.API;
using Relativity.Services.Objects;
using Relativity.Services.Objects.DataContracts;

namespace DYV_Linked_Document_Management.Handlers
{
    public class ObjectHandler
    {
        private IAPILog Logger { get; set; }
        internal IServicesMgr ServicesMgr { get; set; }


        public ObjectHandler(IServicesMgr servicesMgr, IAPILog logger)
        {
            Logger = logger;
            ServicesMgr = servicesMgr;
        }
        public static async Task<UpdateResult> UpdateFieldValueAsync(
            IObjectManager objectManager,
            int workspaceArtifactId,
            int objectArtifactId,
            Guid fieldGuid,
            object fieldValue,
            IAPILog logger)
        {
            try
            {
                var UpdateRequest = new UpdateRequest
                {
                    Object = new RelativityObjectRef
                    {
                        ArtifactID = objectArtifactId
                    },
                    FieldValues = new List<FieldRefValuePair>
                    {
                        new FieldRefValuePair
                        {
                            Field = new FieldRef
                            {
                                Guid = fieldGuid
                            },
                            Value = fieldValue
                        }
                    }
                };
                return await objectManager.UpdateAsync(workspaceArtifactId, UpdateRequest);
            }
            catch (Exception ex)
            {
                string errorMessage = ex.InnerException != null ?
                    String.Concat(ex.InnerException.Message, "---", ex.StackTrace) :
                    String.Concat(ex.Message, "---", ex.StackTrace);
                logger.ForContext<ObjectHandler>().LogError($"{errorMessage}");
                return null;
            }
        }
    }
}
