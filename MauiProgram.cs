using Microsoft.Extensions.Logging;
using Rag.Views;


using Rag.Services;
using Rag.ViewModels;


namespace Rag
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

            // 1. Registramos el servicio (Singleton: una sola instancia para toda la app)
            builder.Services.AddSingleton<AnythingLlmService>();

            // 2. Registramos el ViewModel (Transient: se crea uno nuevo cada vez que se pide)
            builder.Services.AddTransient<MainViewModel>();

            // 3. Registramos la Vista
            builder.Services.AddTransient<MainPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
