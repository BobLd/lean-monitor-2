using OxyPlot;

namespace Panoptes.View.Charts
{
    internal sealed class PanoptesPlotController : PlotController
    {
        private readonly OxyModifierKeys _zoomOxyModifierKeys = OxyModifierKeys.Control;

        public PanoptesPlotController() : base()
        {
            this.BindMouseDown(OxyMouseButton.Left, PanZoomAt);
            this.BindMouseEnter(new DelegatePlotCommand<OxyMouseEventArgs>((view, controller, args) =>
                controller.AddHoverManipulator(view, new MultiTrackerManipulator(view)
                {
                    LockToInitialSeries = false,
                    Snap = true,
                    PointsOnly = false,
                }, args)));

            this.BindMouseDown(OxyMouseButton.Left, _zoomOxyModifierKeys, PlotCommands.ZoomRectangle);
            this.BindMouseDown(OxyMouseButton.Left, OxyModifierKeys.None, 2, PlotCommands.ResetAt);

            this.UnbindMouseDown(OxyMouseButton.Middle);
            this.UnbindMouseDown(OxyMouseButton.Right);
            this.UnbindKeyDown(OxyKey.C, OxyModifierKeys.Control | OxyModifierKeys.Alt);
            this.UnbindKeyDown(OxyKey.R, OxyModifierKeys.Control | OxyModifierKeys.Alt);
            this.UnbindKeyDown(OxyKey.Up);
            this.UnbindKeyDown(OxyKey.Down);
            this.UnbindKeyDown(OxyKey.Left);
            this.UnbindKeyDown(OxyKey.Right);

            this.UnbindKeyDown(OxyKey.Up, OxyModifierKeys.Control);
            this.UnbindKeyDown(OxyKey.Down, OxyModifierKeys.Control);
            this.UnbindKeyDown(OxyKey.Left, OxyModifierKeys.Control);
            this.UnbindKeyDown(OxyKey.Right, OxyModifierKeys.Control);
            this.UnbindMouseWheel();
        }

        private static readonly IViewCommand<OxyMouseDownEventArgs> PanZoomAt = new DelegatePlotCommand<OxyMouseDownEventArgs>((view, controller, args)
            => controller.AddMouseManipulator(view, new PanZoomManipulator(view), args));
    }

    public class PanZoomManipulator : MouseManipulator
    {
        public PanZoomManipulator(IPlotView plotView) : base(plotView)
        { }

        private ScreenPoint PreviousPosition { get; set; }
        private DataPoint PreviousPositionShortTerm { get; set; }
        private bool IsPanEnabled { get; set; }

        public override void Completed(OxyMouseEventArgs e)
        {
            base.Completed(e);

            if (!IsPanEnabled)
            {
                return;
            }

            View.SetCursorType(CursorType.Default);
            e.Handled = true;
        }

        public override void Delta(OxyMouseEventArgs e)
        {
            base.Delta(e);
            if (PreviousPosition.Equals(e.Position))
            {
                e.Handled = true;
                return;
            }

            if (!IsPanEnabled)
            {
                e.Handled = true;
                return;
            }

            DataPoint current = InverseTransform(e.Position.X, e.Position.Y);
            double inScale = 1.03;
            double outScale = 0.97;

            if (XAxis != null && YAxis != null)
            {
                // this is pan
                XAxis.Pan(PreviousPosition, e.Position);
                YAxis.Pan(PreviousPosition, e.Position);
            }
            else
            {
                double scale;
                // this is zoom
                if (YAxis?.IsZoomEnabled == true)
                {
                    if (PreviousPositionShortTerm.Y - current.Y > 0)
                    {
                        scale = outScale;
                    }
                    else if (PreviousPositionShortTerm.Y - current.Y < 0)
                    {
                        scale = inScale;
                    }
                    else
                    {
                        scale = 1;
                    }

                    PreviousPositionShortTerm = InverseTransform(e.Position.X, e.Position.Y);
                    YAxis.ZoomAt(scale, current.Y);
                }

                if (XAxis?.IsZoomEnabled == true)
                {
                    if (PreviousPositionShortTerm.X - current.X > 0)
                    {
                        scale = inScale;
                    }
                    else if (PreviousPositionShortTerm.X - current.X < 0)
                    {
                        scale = outScale;
                    }
                    else
                    {
                        scale = 1;
                    }

                    PreviousPositionShortTerm = InverseTransform(e.Position.X, e.Position.Y);
                    XAxis.ZoomAt(scale, current.X);
                }
            }
            PlotView.InvalidatePlot(false);
            PreviousPosition = e.Position;
            e.Handled = true;
        }

        public override void Started(OxyMouseEventArgs e)
        {
            base.Started(e);
            PreviousPosition = e.Position;

            IsPanEnabled = (XAxis?.IsPanEnabled == true) || (YAxis?.IsPanEnabled == true);

            if (IsPanEnabled)
            {
                if (XAxis != null && YAxis != null)
                {
                    View.SetCursorType(CursorType.Pan);
                }
                else if (XAxis == null && YAxis != null)
                {
                    View.SetCursorType(CursorType.ZoomVertical);
                }
                else if (XAxis != null && YAxis == null)
                {
                    View.SetCursorType(CursorType.ZoomHorizontal);
                }
                e.Handled = true;
            }
        }
    }
}
