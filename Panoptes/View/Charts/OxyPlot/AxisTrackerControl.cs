using OxyPlot;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using VerticalAlignment = System.Windows.VerticalAlignment;

namespace Panoptes.View.Charts
{
    /// <summary>
    /// The tracker control.
    /// </summary>
    public sealed class AxisTrackerControl : ContentControl
    {
        /// <summary>
        /// Identifies the <see cref="HorizontalLineVisibility"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HorizontalLineVisibilityProperty =
            DependencyProperty.Register(
                nameof(HorizontalLineVisibility),
                typeof(Visibility),
                typeof(AxisTrackerControl),
                new PropertyMetadata(Visibility.Visible));

        /// <summary>
        /// Identifies the <see cref="VerticalLineVisibility"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty VerticalLineVisibilityProperty =
            DependencyProperty.Register(
                nameof(VerticalLineVisibility),
                typeof(Visibility),
                typeof(AxisTrackerControl),
                new PropertyMetadata(Visibility.Visible));

        /// <summary>
        /// Identifies the <see cref="LineStroke"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LineStrokeProperty = DependencyProperty.Register(
            nameof(LineStroke), typeof(Brush), typeof(AxisTrackerControl), new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="LineExtents"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LineExtentsProperty = DependencyProperty.Register(
            nameof(LineExtents), typeof(OxyRect), typeof(AxisTrackerControl), new PropertyMetadata(new OxyRect()));

        /// <summary>
        /// Identifies the <see cref="LineDashArray"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LineDashArrayProperty = DependencyProperty.Register(
            nameof(LineDashArray), typeof(DoubleCollection), typeof(AxisTrackerControl), new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="BorderEdgeMode"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BorderEdgeModeProperty = DependencyProperty.Register(
            nameof(BorderEdgeMode), typeof(EdgeMode), typeof(AxisTrackerControl));

        /// <summary>
        /// Identifies the <see cref="ShowPointer"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowPointerProperty = DependencyProperty.Register(
            nameof(ShowPointer), typeof(bool), typeof(AxisTrackerControl), new PropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
            nameof(CornerRadius), typeof(double), typeof(AxisTrackerControl), new PropertyMetadata(0.0));

        /// <summary>
        /// Identifies the <see cref="Distance"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DistanceProperty = DependencyProperty.Register(
            nameof(Distance), typeof(double), typeof(AxisTrackerControl), new PropertyMetadata(7.0));

        /// <summary>
        /// Identifies the <see cref="Position"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PositionProperty = DependencyProperty.Register(
            nameof(Position),
            typeof(ScreenPoint),
            typeof(AxisTrackerControl),
            new PropertyMetadata(new ScreenPoint(), PositionChanged));

        /// <summary>
        /// Identifies the <see cref="IsVertical"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsVerticalProperty =
            DependencyProperty.Register(nameof(IsVertical), typeof(bool), typeof(AxisTrackerControl), new PropertyMetadata(true));

        /// <summary>
        /// The path part string.
        /// </summary>
        private const string PartPath = "PART_Path";

        /// <summary>
        /// The content part string.
        /// </summary>
        private const string PartContent = "PART_Content";

        /// <summary>
        /// The content container part string.
        /// </summary>
        private const string PartContentcontainer = "PART_ContentContainer";

        /// <summary>
        /// The horizontal line part string.
        /// </summary>
        private const string PartHorizontalline = "PART_HorizontalLine";

        /// <summary>
        /// The vertical line part string.
        /// </summary>
        private const string PartVerticalline = "PART_VerticalLine";

        /// <summary>
        /// The content.
        /// </summary>
        private ContentPresenter content;

        /// <summary>
        /// The horizontal line.
        /// </summary>
        private Line horizontalLine;

        /// <summary>
        /// The path.
        /// </summary>
        private Path path;

        /// <summary>
        /// The content container.
        /// </summary>
        private Grid contentContainer;

        /// <summary>
        /// The vertical line.
        /// </summary>
        private Line verticalLine;

        /// <summary>
        /// Initializes static members of the <see cref = "TrackerControl" /> class.
        /// </summary>
        static AxisTrackerControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AxisTrackerControl),
                new FrameworkPropertyMetadata(typeof(AxisTrackerControl)));
        }

        /// <summary>
        /// Gets or sets BorderEdgeMode.
        /// </summary>
        public EdgeMode BorderEdgeMode
        {
            get => (EdgeMode)this.GetValue(BorderEdgeModeProperty);
            set => this.SetValue(BorderEdgeModeProperty, value);
        }

        /// <summary>
        /// Gets or sets HorizontalLineVisibility.
        /// </summary>
        public Visibility HorizontalLineVisibility
        {
            get => (Visibility)this.GetValue(HorizontalLineVisibilityProperty);
            set => this.SetValue(HorizontalLineVisibilityProperty, value);
        }

        /// <summary>
        /// Gets or sets VerticalLineVisibility.
        /// </summary>
        public Visibility VerticalLineVisibility
        {
            get => (Visibility)this.GetValue(VerticalLineVisibilityProperty);
            set => this.SetValue(VerticalLineVisibilityProperty, value);
        }

        /// <summary>
        /// Gets or sets LineStroke.
        /// </summary>
        public Brush LineStroke
        {
            get => (Brush)this.GetValue(LineStrokeProperty);
            set => this.SetValue(LineStrokeProperty, value);
        }

        /// <summary>
        /// Gets or sets LineExtents.
        /// </summary>
        public OxyRect LineExtents
        {
            get => (OxyRect)this.GetValue(LineExtentsProperty);
            set => this.SetValue(LineExtentsProperty, value);
        }

        /// <summary>
        /// Gets or sets LineDashArray.
        /// </summary>
        public DoubleCollection LineDashArray
        {
            get => (DoubleCollection)this.GetValue(LineDashArrayProperty);
            set => this.SetValue(LineDashArrayProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show a 'pointer' on the border.
        /// </summary>
        public bool ShowPointer
        {
            get => (bool)this.GetValue(ShowPointerProperty);
            set => this.SetValue(ShowPointerProperty, value);
        }

        /// <summary>
        /// Gets or sets the corner radius (only used when ShowPoint=<c>false</c>).
        /// </summary>
        public double CornerRadius
        {
            get => (double)this.GetValue(CornerRadiusProperty);
            set => this.SetValue(CornerRadiusProperty, value);
        }

        /// <summary>
        /// Gets or sets the distance of the content container from the trackers Position.
        /// </summary>
        public double Distance
        {
            get => (double)this.GetValue(DistanceProperty);
            set => this.SetValue(DistanceProperty, value);
        }

        /// <summary>
        /// Gets or sets Position of the tracker.
        /// </summary>
        public ScreenPoint Position
        {
            get => (ScreenPoint)this.GetValue(PositionProperty);
            set => this.SetValue(PositionProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the tracker can center its content box horizontally.
        /// </summary>
        public bool IsVertical
        {
            get => (bool)this.GetValue(IsVerticalProperty);
            set => this.SetValue(IsVerticalProperty, value);
        }

        /// <summary>
        /// When overridden in a derived class, is invoked whenever application code or internal processes call <see cref="M:System.Windows.FrameworkElement.ApplyTemplate" />.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this.path = this.GetTemplateChild(PartPath) as Path;
            this.content = this.GetTemplateChild(PartContent) as ContentPresenter;
            this.contentContainer = this.GetTemplateChild(PartContentcontainer) as Grid;
            this.horizontalLine = this.GetTemplateChild(PartHorizontalline) as Line;
            this.verticalLine = this.GetTemplateChild(PartVerticalline) as Line;

            if (this.contentContainer == null)
            {
                throw new InvalidOperationException($"The TrackerControl template must contain a content container with name +'{PartContentcontainer}'");
            }

            if (this.path == null)
            {
                throw new InvalidOperationException($"The TrackerControl template must contain a Path with name +'{PartPath}'");
            }

            if (this.content == null)
            {
                throw new InvalidOperationException($"The TrackerControl template must contain a ContentPresenter with name +'{PartContent}'");
            }

            this.UpdatePositionAndBorder();
        }

        /// <summary>
        /// Called when the position is changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs" /> instance containing the event data.</param>
        private static void PositionChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((AxisTrackerControl)sender).OnPositionChanged(e);
        }

        /// <summary>
        /// Called when the position is changed.
        /// </summary>
        /// <param name="dependencyPropertyChangedEventArgs">The dependency property changed event args.</param>
        // ReSharper disable once UnusedParameter.Local
        private void OnPositionChanged(DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            this.UpdatePositionAndBorder();
        }

        /// <summary>
        /// Update the position and border of the tracker.
        /// </summary>
        private void UpdatePositionAndBorder()
        {
            if (contentContainer == null)
            {
                return;
            }

            var ha = HorizontalAlignment.Center;
            var va = VerticalAlignment.Center;
            if (IsVertical)
            {
                Canvas.SetLeft(contentContainer, LineExtents.Right);
                Canvas.SetTop(contentContainer, Position.Y);
                ha = HorizontalAlignment.Left; // Would need to check vertical axis position
            }
            else
            {
                Canvas.SetLeft(contentContainer, Position.X);
                Canvas.SetTop(contentContainer, LineExtents.Bottom);
                va = VerticalAlignment.Top; // Would need to check horizontal axis position
            }

            FrameworkElement parent = this;
            while (parent is not Canvas and not null)
            {
                parent = VisualTreeHelper.GetParent(parent) as FrameworkElement;
            }

            if (parent == null)
            {
                return;
            }

            // throw new InvalidOperationException("The TrackerControl must have a Canvas parent.");
            double canvasWidth = parent.ActualWidth;
            double canvasHeight = parent.ActualHeight;

            content.Measure(new Size(canvasWidth, canvasHeight));
            content.Arrange(new Rect(0, 0, content.DesiredSize.Width, content.DesiredSize.Height));

            double contentWidth = content.DesiredSize.Width;
            double contentHeight = content.DesiredSize.Height;

            double dx = ha == HorizontalAlignment.Center ? -0.5 : ha == HorizontalAlignment.Left ? 0 : -1;
            double dy = va == VerticalAlignment.Center ? -0.5 : va == VerticalAlignment.Top ? 0 : -1;

            path.Data = ShowPointer ? CreatePointerBorderGeometry(ha, va, contentWidth, contentHeight, out Thickness margin)
                                    : CreateBorderGeometry(ha, va, contentWidth, contentHeight, out margin);

            content.Margin = margin;

            contentContainer.Measure(new Size(canvasWidth, canvasHeight));
            var contentSize = contentContainer.DesiredSize;

            contentContainer.RenderTransform = new TranslateTransform
            {
                X = dx * contentSize.Width,
                Y = dy * contentSize.Height
            };

            var pos = Position;

            if (horizontalLine != null)
            {
                if (LineExtents.Width > 0)
                {
                    horizontalLine.X1 = LineExtents.Left;
                    horizontalLine.X2 = LineExtents.Right;
                }
                else
                {
                    horizontalLine.X1 = 0;
                    horizontalLine.X2 = canvasWidth;
                }

                horizontalLine.Y1 = pos.Y;
                horizontalLine.Y2 = pos.Y;
            }

            if (verticalLine != null)
            {
                if (LineExtents.Height > 0)
                {
                    verticalLine.Y1 = LineExtents.Top;
                    verticalLine.Y2 = LineExtents.Bottom + margin.Top;
                }
                else
                {
                    verticalLine.Y1 = 0;
                    verticalLine.Y2 = canvasHeight;
                }

                verticalLine.X1 = pos.X;
                verticalLine.X2 = pos.X;
            }
        }

        /// <summary>
        /// Create the border geometry.
        /// </summary>
        /// <param name="ha">The horizontal alignment.</param>
        /// <param name="va">The vertical alignment.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="margin">The margin.</param>
        /// <returns>The border geometry.</returns>
        private Geometry CreateBorderGeometry(HorizontalAlignment ha, VerticalAlignment va, double width, double height, out Thickness margin)
        {
            double m = Distance;
            var rect = new Rect(
                ha == HorizontalAlignment.Left ? m : 0,
                va == VerticalAlignment.Top ? m : 0,
                width,
                height);
            margin = new Thickness(
                ha == HorizontalAlignment.Left ? m : 0,
                va == VerticalAlignment.Top ? m : 0,
                ha == HorizontalAlignment.Right ? m : 0,
                va == VerticalAlignment.Bottom ? m : 0);
            return new RectangleGeometry { Rect = rect, RadiusX = CornerRadius, RadiusY = CornerRadius };
        }

        /// <summary>
        /// Create a border geometry with a 'pointer'.
        /// </summary>
        /// <param name="ha">The horizontal alignment.</param>
        /// <param name="va">The vertical alignment.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="margin">The margin.</param>
        /// <returns>The border geometry.</returns>
        private Geometry CreatePointerBorderGeometry(HorizontalAlignment ha, VerticalAlignment va, double width, double height, out Thickness margin)
        {
            Point[] points = null;
            double m = this.Distance;
            margin = new Thickness();

            if (ha == HorizontalAlignment.Center && va == VerticalAlignment.Bottom)
            {
                double x0 = 0;
                double x1 = width;
                double x2 = (x0 + x1) / 2;
                double y0 = 0;
                double y1 = height;
                margin = new Thickness(0, 0, 0, m);
                points = new[]
                {
                    new Point(x0, y0),
                    new Point(x1, y0),
                    new Point(x1, y1),
                    new Point(x2 + (m / 2), y1),
                    new Point(x2, y1 + m),
                    new Point(x2 - (m / 2), y1),
                    new Point(x0, y1)
                };
            }
            else if (ha == HorizontalAlignment.Center && va == VerticalAlignment.Top)
            {
                double x0 = 0;
                double x1 = width;
                double x2 = (x0 + x1) / 2;
                double y0 = m;
                double y1 = m + height;
                margin = new Thickness(0, m, 0, 0);
                points = new[]
                {
                    new Point(x0, y0),
                    new Point(x2 - (m / 2), y0),
                    new Point(x2, 0),
                    new Point(x2 + (m / 2), y0),
                    new Point(x1, y0),
                    new Point(x1, y1),
                    new Point(x0, y1)
                };
            }
            else if (ha == HorizontalAlignment.Left && va == VerticalAlignment.Center)
            {
                double x0 = m;
                double x1 = m + width;
                double y0 = 0;
                double y1 = height;
                double y2 = (y0 + y1) / 2;
                margin = new Thickness(m, 0, 0, 0);
                points = new[]
                {
                    new Point(0, y2),
                    new Point(x0, y0),
                    new Point(x1, y0),
                    new Point(x1, y1),
                    new Point(x0, y1),
                };
            }
            else if (ha == HorizontalAlignment.Right && va == VerticalAlignment.Center)
            {
                double x0 = 0;
                double x1 = width;
                double y0 = 0;
                double y1 = height;
                double y2 = (y0 + y1) / 2;
                margin = new Thickness(0, 0, m, 0);
                points = new[]
                {
                    new Point(x1 + m, y2),
                    new Point(x1, y1),
                    new Point(x0, y1),
                    new Point(x0, y0),
                    new Point(x1, y0),
                };
            }
            else
            {
                throw new ArgumentException("Not possible");
            }

            if (points == null)
            {
                return null;
            }

            var pc = new PointCollection(points.Length);
            foreach (var p in points)
            {
                pc.Add(p);
            }

            var segments = new PathSegmentCollection { new PolyLineSegment { Points = pc } };
            var pf = new PathFigure { StartPoint = points[0], Segments = segments, IsClosed = true };
            return new PathGeometry { Figures = new PathFigureCollection { pf } };
        }
    }
}
