using OxyPlot;
using OxyPlot.Wpf;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;

namespace Panoptes.View.Charts
{
    /// <summary>
    /// Provides a plot manipulator for tracker functionality.
    /// </summary>
    internal sealed class MultiTrackerManipulator : MouseManipulator
    {
        /// <summary>
        /// The current series.
        /// </summary>
        private OxyPlot.Series.Series currentSeries;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrackerManipulator" /> class.
        /// </summary>
        /// <param name="plotView">The plot view.</param>
        public MultiTrackerManipulator(IPlotView plotView)
            : base(plotView)
        {
            Snap = true;
            PointsOnly = false;
            LockToInitialSeries = true;
            FiresDistance = 20.0;
            CheckDistanceBetweenPoints = false;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show tracker on points only (not interpolating).
        /// </summary>
        public bool PointsOnly { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to snap to the nearest point.
        /// </summary>
        public bool Snap { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to lock the tracker to the initial series.
        /// </summary>
        /// <value><c>true</c> if the tracker should be locked; otherwise, <c>false</c>.</value>
        public bool LockToInitialSeries { get; set; }

        /// <summary>
        /// Gets or sets the distance from the series at which the tracker fires.
        /// </summary>
        public double FiresDistance { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to check distance when showing tracker between data points.
        /// </summary>
        /// <remarks>This parameter is ignored if <see cref="PointsOnly"/> is equal to <c>False</c>.</remarks>
        public bool CheckDistanceBetweenPoints { get; set; }

        /// <summary>
        /// Occurs when a manipulation is complete.
        /// </summary>
        /// <param name="e">The <see cref="OxyPlot.OxyMouseEventArgs" /> instance containing the event data.</param>
        public override void Completed(OxyMouseEventArgs e)
        {
            base.Completed(e);
            e.Handled = true;

            currentSeries = null;
            PlotView.HideTracker();
            HideExtraTrackers();
            if (PlotView.ActualModel != null)
            {
                PlotView.ActualModel.RaiseTrackerChanged(null);
            }
        }

        /// <summary>
        /// Occurs when the input device changes position during a manipulation.
        /// </summary>
        /// <param name="e">The <see cref="OxyMouseEventArgs" /> instance containing the event data.</param>
        public override void Delta(OxyMouseEventArgs e)
        {
            base.Delta(e);
            e.Handled = true;

            if (currentSeries == null || !LockToInitialSeries)
            {
                // get the nearest
                currentSeries = PlotView.ActualModel?.GetSeriesFromPoint(e.Position, FiresDistance);
            }

            if (currentSeries == null)
            {
                if (!LockToInitialSeries)
                {
                    PlotView.HideTracker();
                    HideExtraTrackers();
                }

                return;
            }

            var actualModel = PlotView.ActualModel;
            if (actualModel == null)
            {
                return;
            }

            if (!actualModel.PlotArea.Contains(e.Position.X, e.Position.Y))
            {
                return;
            }

            var result = GetNearestHit(currentSeries, e.Position, Snap, PointsOnly, FiresDistance, CheckDistanceBetweenPoints);
            if (result != null)
            {
                result.PlotModel = PlotView.ActualModel;
                PlotView.ShowTracker(result);
                ShowExtraTrackers(result);
                PlotView.ActualModel.RaiseTrackerChanged(result);
            }
        }

        private Dictionary<string, ContentControl> _extraTrackers = new Dictionary<string, ContentControl>();

        private void HideExtraTrackers()
        {
            var view = (PlotView)PlotView;
            var border = VisualTreeHelper.GetChild(view, 0);
            if (VisualTreeHelper.GetChild(border, 0) is Grid grid && VisualTreeHelper.GetChild(grid, 1) is Canvas canvas)
            {
                foreach (var tracker in _extraTrackers)
                {
                    canvas.Children.Remove(tracker.Value);
                }
            }
            _extraTrackers.Clear();
        }

        private void ShowExtraTrackers(TrackerHitResult result)
        {
            var view = (PlotView)PlotView;
            var border = VisualTreeHelper.GetChild(view, 0);
            if (VisualTreeHelper.GetChild(border, 0) is Grid grid && VisualTreeHelper.GetChild(grid, 1) is Canvas canvas)
            {
                foreach (var def in view.TrackerDefinitions)
                {
                    ContentControl currentExtraTracker = null;
                    if (_extraTrackers.ContainsKey(def.TrackerKey))
                    {
                        currentExtraTracker = _extraTrackers[def.TrackerKey];
                    }
                    else
                    {
                        _extraTrackers.Add(def.TrackerKey, null);
                    }

                    var extraTracker = new ContentControl
                    {
                        Template = def.TrackerTemplate,
                        DataContext = result,
                    };

                    if (!ReferenceEquals(extraTracker, currentExtraTracker))
                    {
                        canvas.Children.Remove(currentExtraTracker);
                        canvas.Children.Add(extraTracker);
                        _extraTrackers[def.TrackerKey] = extraTracker;
                    }
                }
            }
        }

        /// <summary>
        /// Occurs when an input device begins a manipulation on the plot.
        /// </summary>
        /// <param name="e">The <see cref="OxyPlot.OxyMouseEventArgs" /> instance containing the event data.</param>
        public override void Started(OxyMouseEventArgs e)
        {
            base.Started(e);
            currentSeries = PlotView.ActualModel?.GetSeriesFromPoint(e.Position, FiresDistance);
            Delta(e);
        }

        #region TrackerHelper
        /// <summary>
        /// Gets the nearest tracker hit.
        /// </summary>
        /// <param name="series">The series.</param>
        /// <param name="point">The point.</param>
        /// <param name="snap">Snap to points.</param>
        /// <param name="pointsOnly">Check points only (no interpolation).</param>
        /// <param name="firesDistance">The distance from the series at which the tracker fires</param>
        /// <param name="checkDistanceBetweenPoints">The value indicating whether to check distance
        /// when showing tracker between data points.</param>
        /// <remarks>
        /// <paramref name="checkDistanceBetweenPoints" /> is ignored if <paramref name="pointsOnly"/> is equal to <c>False</c>.
        /// </remarks>
        /// <returns>A tracker hit result.</returns>
        public static TrackerHitResult GetNearestHit(
            OxyPlot.Series.Series series,
            ScreenPoint point,
            bool snap,
            bool pointsOnly,
            double firesDistance,
            bool checkDistanceBetweenPoints)
        {
            if (series == null)
            {
                return null;
            }

            // Check data points only
            if (snap || pointsOnly)
            {
                var result = series.GetNearestPoint(point, false);
                if (ShouldTrackerOpen(result, point, firesDistance))
                {
                    return result;
                }
            }

            // Check between data points (if possible)
            if (!pointsOnly)
            {
                var result = series.GetNearestPoint(point, true);
                if (!checkDistanceBetweenPoints || ShouldTrackerOpen(result, point, firesDistance))
                {
                    return result;
                }
            }

            return null;
        }

        private static bool ShouldTrackerOpen(TrackerHitResult result, ScreenPoint point, double firesDistance) =>
            result?.Position.DistanceTo(point) < firesDistance;
        #endregion
    }
}
