using SQLite;

namespace siteblock.Storage
{
    public class BlockedSiteDatabase
    {
        private readonly SQLiteAsyncConnection _db;

        public BlockedSiteDatabase(string dbPath)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[Database] Initializing database at: {dbPath}");
                _db = new SQLiteAsyncConnection(dbPath);
                _db.CreateTableAsync<BlockedSite>().Wait();
                System.Diagnostics.Debug.WriteLine($"[Database] Database initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Database] Initialization error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        // ✅ CREATE - Add blocked site
        public async Task<int> AddBlockedSiteAsync(BlockedSite site)
        {
            try
            {
                if (site == null)
                    throw new ArgumentNullException(nameof(site));

                site.Domain = site.Domain.ToLowerInvariant();
                site.AddedDate = DateTime.Now;
                
                System.Diagnostics.Debug.WriteLine($"[Database] Adding blocked site: {site.Domain}");
                
                var existing = await GetBlockedSiteAsync(site.Domain);
                if (existing != null)
                {
                    existing.IsActive = true;
                    existing.Category = site.Category;
                    existing.Notes = site.Notes;
                    var result = await _db.UpdateAsync(existing);
                    System.Diagnostics.Debug.WriteLine($"[Database] Updated existing site: {site.Domain}");
                    return result;
                }
                
                var insertResult = await _db.InsertAsync(site);
                System.Diagnostics.Debug.WriteLine($"[Database] Inserted new site: {site.Domain}, Result: {insertResult}");
                return insertResult;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Database] Error adding site: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        // ✅ READ - Get all blocked sites (active only)
        public async Task<List<BlockedSite>> GetAllBlockedSitesAsync()
        {
            return await _db.Table<BlockedSite>()
                .Where(s => s.IsActive)
                .OrderByDescending(s => s.AddedDate)
                .ToListAsync();
        }

        // ✅ READ - Get all sites including inactive
        public async Task<List<BlockedSite>> GetAllSitesIncludingInactiveAsync()
        {
            return await _db.Table<BlockedSite>()
                .OrderByDescending(s => s.AddedDate)
                .ToListAsync();
        }

        // ✅ READ - Get single blocked site by domain
        public async Task<BlockedSite?> GetBlockedSiteAsync(string domain)
        {
            var lowerDomain = domain.ToLowerInvariant();
            return await _db.Table<BlockedSite>()
                .Where(s => s.Domain == lowerDomain)
                .FirstOrDefaultAsync();
        }

        // ✅ READ - Get active domains only (for blocking)
        public async Task<List<string>> GetActiveDomainsAsync()
        {
            var sites = await _db.Table<BlockedSite>()
                .Where(s => s.IsActive)
                .ToListAsync();
            return sites.Select(s => s.Domain).ToList();
        }

        // ✅ READ - Get count of active sites
        public async Task<int> GetActiveCountAsync()
        {
            return await _db.Table<BlockedSite>()
                .Where(s => s.IsActive)
                .CountAsync();
        }

        // ✅ READ - Get sites by category
        public async Task<List<BlockedSite>> GetSitesByCategoryAsync(string category)
        {
            return await _db.Table<BlockedSite>()
                .Where(s => s.IsActive && s.Category == category)
                .OrderByDescending(s => s.AddedDate)
                .ToListAsync();
        }

        // ✅ UPDATE - Remove (deactivate) blocked site
        public async Task<int> RemoveBlockedSiteAsync(string domain)
        {
            var site = await GetBlockedSiteAsync(domain);
            if (site != null)
            {
                site.IsActive = false;
                System.Diagnostics.Debug.WriteLine($"[Database] Deactivated site: {domain}");
                return await _db.UpdateAsync(site);
            }
            return 0;
        }

        // ✅ DELETE - Permanently delete a site
        public async Task<int> DeleteBlockedSiteAsync(int id)
        {
            System.Diagnostics.Debug.WriteLine($"[Database] Permanently deleting site with ID: {id}");
            return await _db.DeleteAsync<BlockedSite>(id);
        }

        // ✅ DELETE - Permanently delete by domain
        public async Task<int> DeleteBlockedSiteByDomainAsync(string domain)
        {
            var site = await GetBlockedSiteAsync(domain);
            if (site != null)
            {
                System.Diagnostics.Debug.WriteLine($"[Database] Permanently deleting site: {domain}");
                return await _db.DeleteAsync(site);
            }
            return 0;
        }

        // ✅ UPDATE - Clear all (deactivate all sites)
        public async Task<int> ClearAllBlockedSitesAsync()
        {
            var sites = await _db.Table<BlockedSite>().ToListAsync();
            foreach (var site in sites)
            {
                site.IsActive = false;
            }
            System.Diagnostics.Debug.WriteLine($"[Database] Deactivated all {sites.Count} sites");
            return await _db.UpdateAllAsync(sites);
        }

        // ✅ DELETE - Permanently delete all sites
        public async Task<int> DeleteAllSitesAsync()
        {
            System.Diagnostics.Debug.WriteLine($"[Database] Permanently deleting all sites");
            return await _db.DeleteAllAsync<BlockedSite>();
        }

        // ✅ UTILITY - Check if domain is blocked
        public async Task<bool> IsDomainBlockedAsync(string domain)
        {
            var site = await GetBlockedSiteAsync(domain);
            return site != null && site.IsActive;
        }
    }
}
