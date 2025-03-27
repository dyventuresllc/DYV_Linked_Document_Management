using Relativity.API;
using System;

namespace DYV_Linked_Document_Management.Utilities
{
    internal class DYVLDHelper
    {
        private readonly IHelper _helper;
        private readonly IAPILog _logger;

        public IHelper Helper => _helper;
        public IAPILog Logger => _logger;
        
        public Guid LdfFileIdentifer { get; } = new Guid("92351D44-C356-4638-879D-1E49708CECB8");
        public Guid LdfFileType { get; } = new Guid("9BDD069B-031D-484F-95B1-94AD622D369C");
        public Guid LdfCustodianId { get; } = new Guid("467AC300-9560-46F8-B82D-12DB32FA4C80");
        public Guid LdfObjectID_GM_Metadata { get; } = new Guid("A8B19396-396F-426D-9C32-F44072B04601");
        public Guid LdfTargetWorkspaceId { get; } = new Guid("55077480-BE7D-4D00-A86F-DCD0C5DC30E9");
        public Guid LdfObjectId { get; } = new Guid("F294DF03-C5C2-4836-9B1C-5425F5314C52");
        public Guid LdfFilesTableId { get; } = new Guid("48DBACF7-084B-4686-A34A-8E09D84C321E");
        public Guid LdfStatus { get; } = new Guid("C0802896-2C41-4DA3-A976-F06E35E801D8");        

        public DYVLDHelper(IHelper helper, IAPILog logger)
        {
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));
            _logger = logger ?? throw new ArgumentNullException(nameof(_logger));
        }
    }
}
