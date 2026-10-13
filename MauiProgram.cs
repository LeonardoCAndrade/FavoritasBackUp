using Microsoft.Extensions.Logging;

using FavoritasBackUp.Services;
using Microsoft.Maui.Storage;

namespace FavoritasBackUp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddSingleton<ISecureStorage>(SecureStorage.Default);
            builder.Services.AddSingleton<CloudConfigurationService>();
            builder.Services.AddTransient<ConfigPage>();
            builder.Services.AddTransient<SyncPage>();
            builder.Services.AddTransient<AppShell>();

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
