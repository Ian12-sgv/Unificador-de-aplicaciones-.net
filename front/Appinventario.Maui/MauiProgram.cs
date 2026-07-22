using Appinventario.Controls;
using Appinventario.Platforms.Android.Handlers;
using Microsoft.Extensions.Logging;

namespace Appinventario;

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
            })
            .ConfigureMauiHandlers(handlers =>
            {
#if ANDROID
                handlers.AddHandler(typeof(SoftkeyboardDisabledEntry), typeof(SoftkeyboardDisabledEntryHandler));
                handlers.AddHandler(typeof(RoundedEntry), typeof(RoundedEntryHandler));
                handlers.AddHandler(typeof(RoundedEntry2), typeof(RoundedEntry2Handler));
#endif
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}

