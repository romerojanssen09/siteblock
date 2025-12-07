using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
using siteblock.Storage;

namespace siteblock
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseLocalNotification()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Register SQLite database as singleton
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "blockedsites.db");
            System.Diagnostics.Debug.WriteLine($"[MauiProgram] Database path: {dbPath}");
            
            var database = new BlockedSiteDatabase(dbPath);
            builder.Services.AddSingleton(database);
            
            // Initialize services with database
            siteblock.Services.DatabaseService.Initialize(database);
            siteblock.Services.BlockingRulesManager.Instance.Initialize(database);

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
