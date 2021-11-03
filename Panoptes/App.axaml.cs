using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Messaging;
using Panoptes.Model;
using Panoptes.Model.Sessions;
using Panoptes.Model.Statistics;
using Panoptes.ViewModels;
using Panoptes.ViewModels.Charts;
using Panoptes.ViewModels.NewSession;
using Panoptes.ViewModels.Panels;
using System;

namespace Panoptes
{
    public class App : Application
    {
        public App()
        {
            Services = ConfigureServices();
        }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }

        /// <summary>
        /// Gets the current <see cref="App"/> instance in use.
        /// </summary>
        public new static App Current => (App)Application.Current;

        /// <summary>
        /// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
        /// </summary>
        public IServiceProvider Services { get; }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // Results
            services.AddSingleton<IResultConverter, ResultConverter>();
            services.AddSingleton<IResultSerializer, AdvancedResultSerializer>();
            services.AddSingleton<IResultMutator, BenchmarkResultMutator>();
            services.AddSingleton<IStatisticsFormatter, StatisticsFormatter>();

            // Sessions
            services.AddSingleton<ISessionService, SessionService>(); //SessionService>();

            // Api
            //For<IApiClient>().Singleton().Use<ApiClient>();

            services.AddSingleton<IMessenger, WeakReferenceMessenger>();

            services.AddSingleton<INewSessionViewModel, NewStreamSessionViewModel>();
            services.AddSingleton<INewSessionViewModel, NewMongoSessionViewModel>();
            services.AddSingleton<INewSessionViewModel, NewFileSessionViewModel>();

            // Viewmodels
            services.AddTransient<NewSessionWindowViewModel>();
            //services.AddTransient<AboutWindowViewModel>();
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<StatisticsPanelViewModel>();
            services.AddTransient<RuntimeStatisticsPanelViewModel>();
            services.AddTransient<TradesPanelViewModel>();
            services.AddTransient<HoldingsPanelViewModel>();
            services.AddTransient<ProfitLossPanelViewModel>();
            services.AddTransient<LogPanelViewModel>();
            services.AddTransient<StatusViewModel>();
            services.AddTransient<OxyPlotSelectionViewModel>();

            //services.AddTransient<ToolPaneViewModel>(); // abstract

            return services.BuildServiceProvider();
        }
    }
}
