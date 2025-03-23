using Relativity.API;

namespace DYV_Linked_Document_Management.Logging
{
    public static class LoggerFactory
    {
        public static ILDLogger CreateLogger<T>(
            IDBContext eddsDbContext,
            IHelper helper,
            IAPILog logger,
            string applicationName = "DYV Linked Document Management")
        {
            return new LDLogger(
                eddsDbContext,
                helper,
                logger,
                applicationName,
                typeof(T).Name
            );
        }
    }
}
