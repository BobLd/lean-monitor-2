using Panoptes.ViewModels;
using Panoptes.ViewModels.Charts;
using Panoptes.ViewModels.NewSession;
using Panoptes.ViewModels.Panels;

namespace Panoptes
{
    internal sealed class ViewModelLocator
    {
        public static NewSessionWindowViewModel NewSessionWindow => GetViewModel<NewSessionWindowViewModel>();
        //public static AboutWindowViewModel AboutWindow => GetViewModel<AboutWindowViewModel>();

        public static MainWindowViewModel MainWindow => GetViewModel<MainWindowViewModel>();

        public static StatisticsPanelViewModel StatisticsPanel => GetViewModel<StatisticsPanelViewModel>();
        public static RuntimeStatisticsPanelViewModel RuntimeStatisticsPanel => GetViewModel<RuntimeStatisticsPanelViewModel>();
        public static TradesPanelViewModel TradesPanel => GetViewModel<TradesPanelViewModel>();
        public static HoldingsPanelViewModel HoldingsPanel => GetViewModel<HoldingsPanelViewModel>();
        public static ProfitLossPanelViewModel ProfitLossPanel => GetViewModel<ProfitLossPanelViewModel>();
        public static LogPanelViewModel LogPanel => GetViewModel<LogPanelViewModel>();

        public static OxyPlotSelectionViewModel OxyPlotSelectionPanel => GetViewModel<OxyPlotSelectionViewModel>();
        private static T GetToolViewModel<T>() where T : ToolPaneViewModel
        {
            //return _container.GetInstance<T>();
            return (T)App.Current.Services.GetService(typeof(T));
        }

        private static T GetViewModel<T>()
        {
            // Get all viewmodels as unique instances
            // return _container.GetInstance<T>();
            return (T)App.Current.Services.GetService(typeof(T));
        }
    }
}
