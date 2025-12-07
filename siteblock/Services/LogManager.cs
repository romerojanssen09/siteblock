using System.Collections.ObjectModel;

namespace siteblock.Services
{
    public class LogManager
    {
        private static LogManager? _instance;
        public static LogManager Instance => _instance ??= new LogManager();

        private readonly ObservableCollection<string> _logs = new();
        private const int MaxLogs = 500;

        public ObservableCollection<string> Logs => _logs;

        private LogManager() { }

        public void AddLog(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var logEntry = $"[{timestamp}] {message}";

            MainThread.BeginInvokeOnMainThread(() =>
            {
                _logs.Insert(0, logEntry);
                if (_logs.Count > MaxLogs)
                {
                    _logs.RemoveAt(_logs.Count - 1);
                }
            });
        }

        public void Clear()
        {
            MainThread.BeginInvokeOnMainThread(() => _logs.Clear());
        }
    }
}
