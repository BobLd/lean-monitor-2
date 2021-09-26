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
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Markup;

//https://docs.microsoft.com/en-us/windows/communitytoolkit/mvvm/ioc

namespace Panoptes
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Services = ConfigureServices();
            InitializeComponent();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            var vCulture = CultureInfo.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = vCulture;
            Thread.CurrentThread.CurrentUICulture = vCulture;
            CultureInfo.DefaultThreadCurrentCulture = vCulture;
            CultureInfo.DefaultThreadCurrentUICulture = vCulture;
            FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

            base.OnStartup(e);
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
            services.AddSingleton<IResultSerializer, ResultSerializer>();
            services.AddSingleton<IResultMutator, BenchmarkResultMutator>();
            services.AddSingleton<IStatisticsFormatter, StatisticsFormatter>();

            // Sessions
            services.AddSingleton<ISessionService, SessionServiceWPF>(); //SessionService>();

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
            services.AddTransient<ProfitLossPanelViewModel>();
            services.AddTransient<LogPanelViewModel>();
            services.AddTransient<StatusViewModel>();
            services.AddTransient<OxyPlotSelectionViewModel>();

            //services.AddTransient<ToolPaneViewModel>(); // abstract

            return services.BuildServiceProvider();
        }
    }
}
