using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using OxyPlot;
using Panoptes.Model.Messages;
using Panoptes.Model.Sessions;
using Panoptes.ViewModels;
using Panoptes.ViewModels.Charts;
using Panoptes.ViewModels.Panels;

namespace Panoptes.Avalonia.ViewModels
{
    public sealed class MainWindowViewModel : ObservableRecipient
    {
        private readonly ISessionService _sessionService;
        //private readonly ILayoutManager _layoutManager;
        private readonly IMessenger _messenger;

        private SessionState _sessionState = SessionState.Unsubscribed;

        public RelayCommand ExitCommand { get; }
        public RelayCommand OpenSessionCommand { get; }
        public RelayCommand CloseCommand { get; }
        public RelayCommand ExportCommand { get; }

        public RelayCommand ConnectCommand { get; }
        public RelayCommand DisconnectCommand { get; }

        /*
        public RelayCommand<DockingManager> SaveLayoutCommand { get; }
        public RelayCommand<DockingManager> ResetLayoutCommand { get; }
        public RelayCommand<DockingManager> RestoreLayoutCommand { get; }
        */

        public LogPanelViewModel LogPane { get; }
        public StatisticsPanelViewModel StatisticsPane { get; }
        public RuntimeStatisticsPanelViewModel RuntimeStatisticsPane { get; }
        public TradesPanelViewModel TradesPane { get; }
        public ProfitLossPanelViewModel ProfitLossPane { get; }

        public OxyPlotSelectionViewModel OxyPlotSelectionPane { get; }

        public bool IsSessionActive => _sessionService.IsSessionActive;

        public SessionState SessionState
        {
            get { return _sessionState; }
            set
            {
                _sessionState = value;
                OnPropertyChanged();
            }
        }

        public StatusViewModel StatusViewModel { get; }

        public MainWindowViewModel(ISessionService resultService, IMessenger messenger, StatusViewModel statusViewModel,
            LogPanelViewModel logPanelViewModel, StatisticsPanelViewModel statisticsPanelViewModel,
            RuntimeStatisticsPanelViewModel runtimeStatisticsPanelViewModel, ProfitLossPanelViewModel profitLossPanelViewModel,
            TradesPanelViewModel tradesPanelViewModel, OxyPlotSelectionViewModel oxyPlotSelectionViewModel)
        {
            StatusViewModel = statusViewModel;
            _sessionService = resultService;
            //_layoutManager = layoutManager;
            _messenger = messenger;

            LogPane = logPanelViewModel;
            StatisticsPane = statisticsPanelViewModel;
            RuntimeStatisticsPane = runtimeStatisticsPanelViewModel;
            ProfitLossPane = profitLossPanelViewModel;
            TradesPane = tradesPanelViewModel;
            OxyPlotSelectionPane = oxyPlotSelectionViewModel;

            ExitCommand = new RelayCommand(() =>
            {
                if (App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.MainWindow.Close();
                }
            });

            CloseCommand = new RelayCommand(() => _sessionService.ShutdownSession(), () => IsSessionActive);
            OpenSessionCommand = new RelayCommand(() => _messenger.Send(new ShowNewSessionWindowMessage()));
            ExportCommand = new RelayCommand(Export, () => IsSessionActive);
            ConnectCommand = new RelayCommand(() => _sessionService.IsSessionSubscribed = true, () => _sessionState != SessionState.Subscribed && _sessionService.CanSubscribe);
            DisconnectCommand = new RelayCommand(() => _sessionService.IsSessionSubscribed = false, () => _sessionState != SessionState.Unsubscribed);

            /*
            SaveLayoutCommand = new RelayCommand<DockingManager>(manager => _layoutManager.SaveLayout(manager));
            RestoreLayoutCommand = new RelayCommand<DockingManager>(manager => _layoutManager.LoadLayout(manager));
            ResetLayoutCommand = new RelayCommand<DockingManager>(manager => _layoutManager.ResetLayout(manager));
            */

            _messenger.Register<MainWindowViewModel, SessionOpenedMessage>(this, (r, _) => r.InvalidateCommands());

            _messenger.Register<MainWindowViewModel, SessionClosedMessage>(this, (r, _) =>
            {
                r.SessionState = SessionState.Unsubscribed;
                //r.Documents.Clear();
                r.InvalidateCommands();
            });

            _messenger.Register<MainWindowViewModel, SessionStateChangedMessage>(this, (r, m) =>
            {
                r.SessionState = m.State;
                r.InvalidateCommands();
            });

            _messenger.Register<MainWindowViewModel, SessionUpdateMessage>(this, (r, m) =>
            {
                /*
                try
                {
                    lock (_documents)
                    {
                        r.ParseResult(m.ResultContext.Result);
                    }
                }
                catch (Exception e)
                {
                    Trace.WriteLine(e);
                }
                */
            });

            _messenger.Register<MainWindowViewModel, GridRequestMessage>(this, (r, m) =>
            {
                /*
                var chartTableViewModel = new GridPanelViewModel
                {
                    Key = m.Key
                };

                // Calcualte the index for this tab
                var index = Documents.IndexOf(Documents.First(c => c.Key == m.Key));
                r.Documents.Insert(index, chartTableViewModel);

                chartTableViewModel.IsSelected = true;

                // Get the latest data for this tab and inject it
                var chart = _sessionService.LastResult.Charts[m.Key];
                chartTableViewModel.ParseChart(chart);
                */
            });
        }

        public void Initialize()
        {
            _sessionService.Initialize();
        }

        public void HandleDroppedFileName(string fileName)
        {
            /*
            _sessionService.OpenFile(new FileSessionParameters
            {
                FileName = fileName,
                Watch = true
            });
            */
        }

        internal void ShutdownSession()
        {
            _sessionService.ShutdownSession();
        }

        private PlotModel _plotModel;
        public PlotModel PlotModel
        {
            get
            {
                return _plotModel;
            }
            set
            {
                _plotModel = value;
                OnPropertyChanged();
            }
        }

        private void Export()
        {
            /*
            var exportDialog = new SaveFileDialog
            {
                FileName = DateTime.Now.ToString("yyyyMMddHHmm") + "_export",
                DefaultExt = ".json",
                Filter = "Json documents (.json)|*.json"
            };

            var dialogResult = exportDialog.ShowDialog();
            if (dialogResult != true) return;

            var serializer = new ResultSerializer(new ResultConverter());
            var serialized = serializer.Serialize(_sessionService.LastResult);
            File.WriteAllText(exportDialog.FileName, serialized);
            */
        }

        private void InvalidateCommands()
        {
            OnPropertyChanged(nameof(IsSessionActive));
            CloseCommand.NotifyCanExecuteChanged();
            ExportCommand.NotifyCanExecuteChanged();
            ConnectCommand.NotifyCanExecuteChanged();
            DisconnectCommand.NotifyCanExecuteChanged();
        }
    }
}
