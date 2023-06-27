using Helpers;
using Prism.DryIoc;

namespace MyShinySample
{
	public static class MauiProgram
	{
		public static MauiApp CreateMauiApp() => MauiApp
			 .CreateBuilder()
			 .UseMauiApp<App>()
			 .UseMauiCommunityToolkit()
			 .UseShinyFramework(
				  new DryIocContainerExtension(),
				  prism => prism.OnAppStart("NavigationPage/MainPage")
			 )
			 .ConfigureFonts(fonts =>
			 {
				 fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				 fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			 })
			 .RegisterInfrastructure()
			 .RegisterAppServices()
			 .RegisterViews()
			 .Build();


		static MauiAppBuilder RegisterAppServices(this MauiAppBuilder builder)
		{
			// register your own services here!
			var s = builder.Services;

			s.AddSingleton<TimerHelper>();
			return builder;
		}


		static MauiAppBuilder RegisterInfrastructure(this MauiAppBuilder builder)
		{

			builder.Configuration.AddJsonPlatformBundle();
#if DEBUG
        builder.Logging.SetMinimumLevel(LogLevel.Trace);
        builder.Logging.AddDebug();
#endif
			var s = builder.Services;
			s.AddBluetoothLE<MyBleClient.MyBleDelegate>();
			s.AddDataAnnotationValidation();
			s.AddGlobalCommandExceptionHandler(new(
#if DEBUG
            ErrorAlertType.FullError
#else
				 ErrorAlertType.NoLocalize
#endif
			));
			return builder;
		}


		static MauiAppBuilder RegisterViews(this MauiAppBuilder builder)
		{
			var s = builder.Services;


			s.RegisterForNavigation<MainPage, MainViewModel>();
			s.RegisterForNavigation<DataPage, DataViewModel>();
			return builder;
		}
	}
}