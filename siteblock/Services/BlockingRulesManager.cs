using System.Collections.ObjectModel;
using System.ComponentModel;
using siteblock.Storage;

namespace siteblock.Services
{
    public class BlockingRulesManager : INotifyPropertyChanged
    {
        private static BlockingRulesManager? _instance;
        public static BlockingRulesManager Instance => _instance ??= new BlockingRulesManager();

        private readonly ObservableCollection<string> _blockedDomains = new();
        private readonly ObservableCollection<string> _blockedIps = new();
        private int _blockedCount;
        private int _allowedCount;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<string> BlockedDomains => _blockedDomains;
        public ObservableCollection<string> BlockedIps => _blockedIps;

        public int BlockedCount
        {
            get => _blockedCount;
            private set
            {
                _blockedCount = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BlockedCount)));
            }
        }

        public int AllowedCount
        {
            get => _allowedCount;
            private set
            {
                _allowedCount = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AllowedCount)));
            }
        }

        private BlockingRulesManager() { }

        public void Initialize(BlockedSiteDatabase database)
        {
            _ = LoadBlockedSitesFromDatabaseAsync();
        }

        private async Task LoadBlockedSitesFromDatabaseAsync()
        {
            try
            {
                if (!DatabaseService.IsInitialized)
                {
                    System.Diagnostics.Debug.WriteLine("[BlockingRulesManager] Database not initialized yet");
                    return;
                }

                var db = DatabaseService.GetDatabase();
                var domains = await db.GetActiveDomainsAsync();
                
                foreach (var domain in domains)
                {
                    if (!_blockedDomains.Contains(domain))
                    {
                        _blockedDomains.Add(domain);
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"[BlockingRulesManager] Loaded {domains.Count} blocked sites from database");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BlockingRulesManager] Error loading blocked sites: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        public async Task AddBlockedDomainAsync(string domain, string? category = null, string? notes = null)
        {
            try
            {
                var lowerDomain = domain.ToLowerInvariant();
                System.Diagnostics.Debug.WriteLine($"[BlockingRulesManager] Adding domain: {lowerDomain}");
                
                if (!_blockedDomains.Contains(lowerDomain))
                {
                    _blockedDomains.Add(lowerDomain);
                    System.Diagnostics.Debug.WriteLine($"[BlockingRulesManager] Domain added to collection");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[BlockingRulesManager] Domain already exists in collection: {lowerDomain}");
                }
                
                // Always save to database (will update if exists)
                var db = DatabaseService.GetDatabase();
                await db.AddBlockedSiteAsync(new BlockedSite
                {
                    Domain = lowerDomain,
                    Category = category,
                    Notes = notes
                });
                
                System.Diagnostics.Debug.WriteLine($"[BlockingRulesManager] Domain saved to database");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BlockingRulesManager] Error adding domain: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public void AddBlockedDomain(string domain)
        {
            Task.Run(async () => await AddBlockedDomainAsync(domain));
        }

        public async Task RemoveBlockedDomainAsync(string domain)
        {
            var lowerDomain = domain.ToLowerInvariant();
            _blockedDomains.Remove(lowerDomain);
            
            // Remove from database
            var db = DatabaseService.GetDatabase();
            await db.RemoveBlockedSiteAsync(lowerDomain);
            System.Diagnostics.Debug.WriteLine($"[BlockingRulesManager] Domain removed: {lowerDomain}");
        }

        public void RemoveBlockedDomain(string domain)
        {
            Task.Run(async () => await RemoveBlockedDomainAsync(domain));
        }

        public void AddBlockedIp(string ip)
        {
            if (!_blockedIps.Contains(ip))
            {
                _blockedIps.Add(ip);
            }
        }

        public void RemoveBlockedIp(string ip)
        {
            _blockedIps.Remove(ip);
        }

        public bool IsBlocked(string ip)
        {
            return _blockedIps.Contains(ip);
        }

        public bool IsDomainBlocked(string domain)
        {
            var lowerDomain = domain.ToLowerInvariant();
            var isBlocked = _blockedDomains.Any(blockedDomain =>
                lowerDomain == blockedDomain || lowerDomain.EndsWith($".{blockedDomain}"));

            if (isBlocked)
                BlockedCount++;
            else
                AllowedCount++;

            return isBlocked;
        }

        public void ResetStats()
        {
            BlockedCount = 0;
            AllowedCount = 0;
        }

        public async Task ClearAllAsync()
        {
            _blockedDomains.Clear();
            _blockedIps.Clear();
            
            // Clear database
            var db = DatabaseService.GetDatabase();
            await db.ClearAllBlockedSitesAsync();
            System.Diagnostics.Debug.WriteLine($"[BlockingRulesManager] All sites cleared");
        }

        // ✅ Get all cached blocked sites
        public async Task<List<BlockedSite>> GetAllCachedSitesAsync()
        {
            var db = DatabaseService.GetDatabase();
            return await db.GetAllBlockedSitesAsync();
        }

        // ✅ Get count of cached sites
        public async Task<int> GetCachedSitesCountAsync()
        {
            var db = DatabaseService.GetDatabase();
            return await db.GetActiveCountAsync();
        }

        public void ClearAll()
        {
            Task.Run(async () => await ClearAllAsync());
        }
    }
}
