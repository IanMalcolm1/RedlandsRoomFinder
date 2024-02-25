using CommunityToolkit.Maui;
using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.Maui;
using Microsoft.Extensions.Logging;

namespace RedlandsRoomFinder
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseArcGISRuntime(config =>
                config.UseApiKey("AAPK18ce5b51a60a49f99077444fa795b959WbLnkf4zGh5vJXsfyxLF24RP7c_jWXviQgTIraGpU8xlvoWlffbHWp0BI5DbEykz"))
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
