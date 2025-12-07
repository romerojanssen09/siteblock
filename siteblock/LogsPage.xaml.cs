using siteblock.Services;

namespace siteblock
{
    public partial class LogsPage : ContentPage
    {
        public LogsPage()
        {
            InitializeComponent();
            LogsList.ItemsSource = LogManager.Instance.Logs;
        }

        private void OnClearLogsClicked(object sender, EventArgs e)
        {
            LogManager.Instance.Clear();
        }
    }
}
