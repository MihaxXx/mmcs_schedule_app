using AndroidX.Core.View;
using Microsoft.Maui.Handlers;

namespace mmcs_schedule_app.Droid;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        MauiAppBuilder builder = MauiApp.CreateBuilder();

        builder
            .UseSharedMauiApp();

        // Modal pages live in their own window, which would otherwise draw dark status bar icons
        PageHandler.Mapper.AppendToMapping("LightIconsStatusBar", (handler, _) =>
        {
            handler.PlatformView.ViewAttachedToWindow += (sender, _) =>
            {
                if (sender is Android.Views.View view)
                {
                    ViewCompat.GetWindowInsetsController(view)?.AppearanceLightStatusBars = false;
                }
            };
        });

        return builder.Build();
    }
}
