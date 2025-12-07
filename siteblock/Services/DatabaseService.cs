using siteblock.Storage;

namespace siteblock.Services
{
    /// <summary>
    /// Service to access the database from anywhere in the app
    /// </summary>
    public static class DatabaseService
    {
        private static BlockedSiteDatabase? _database;

        public static void Initialize(BlockedSiteDatabase database)
        {
            _database = database;
            System.Diagnostics.Debug.WriteLine("[DatabaseService] Database initialized");
        }

        public static BlockedSiteDatabase GetDatabase()
        {
            if (_database == null)
            {
                throw new InvalidOperationException("Database not initialized. Call Initialize first.");
            }
            return _database;
        }

        public static bool IsInitialized => _database != null;
    }
}
