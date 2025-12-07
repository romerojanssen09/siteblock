using System.Collections.ObjectModel;
using System.ComponentModel;

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

        public void AddBlockedDomain(string domain)
        {
            var lowerDomain = domain.ToLowerInvariant();
            if (!_blockedDomains.Contains(lowerDomain))
            {
                _blockedDomains.Add(lowerDomain);
            }
        }

        public void RemoveBlockedDomain(string domain)
        {
            _blockedDomains.Remove(domain.ToLowerInvariant());
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

        public void ClearAll()
        {
            _blockedDomains.Clear();
            _blockedIps.Clear();
        }
    }
}
