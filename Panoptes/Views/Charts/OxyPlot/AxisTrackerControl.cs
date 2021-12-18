using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.VisualTree;
using OxyPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using HorizontalAlignment = Avalonia.Layout.HorizontalAlignment;
using VerticalAlignment = Avalonia.Layout.VerticalAlignment;

namespace Panoptes.Views.Charts
{
    /// <summary>
    /// The tracker control.
    /// </summary>
    internal class AxisTrackerControl : ContentControl
    {
        /// <summary>
        /// Identifies the <see cref="HorizontalLineVisibility"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> HorizontalLineVisibilityProperty = AvaloniaProperty.Register<AxisTrackerControl, bool>(nameof(HorizontalLineVisibility), true);

        /// <summary>
        /// Identifies the <see cref="VerticalLineVisibility"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> VerticalLineVisibilityProperty = AvaloniaProperty.Register<AxisTrackerControl, bool>(nameof(VerticalLineVisibility), true);

        /// <summary>
        /// Identifies the <see cref="LineStroke"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<IBrush> LineStrokeProperty = AvaloniaProperty.Register<AxisTrackerControl, IBrush>(nameof(LineStroke));

        /// <summary>
        /// Identifies the <see cref="LineExtents"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<OxyRect> LineExtentsProperty = AvaloniaProperty.Register<AxisTrackerControl, OxyRect>(nameof(LineExtents), new OxyRect());

        /// <summary>
        /// Identifies the <see cref="LineDashArray"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<List<double>> LineDashArrayProperty = AvaloniaProperty.Register<AxisTrackerControl, List<double>>(nameof(LineDashArray));

        /// <summary>
        /// Identifies the <see cref="ShowPointer"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> ShowPointerProperty = AvaloniaProperty.Register<AxisTrackerControl, bool>(nameof(ShowPointer), true);

        /// <summary>
        /// Identifies the <see cref="Distance"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> DistanceProperty = AvaloniaProperty.Register<AxisTrackerControl, double>(nameof(Distance), 7.0);

        /// <summary>
        /// Identifies the <see cref="Position"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<ScreenPoint> PositionProperty = AvaloniaProperty.Register<AxisTrackerControl, ScreenPoint>(nameof(Position), new ScreenPoint());

        /// <summary>
        /// Identifies the <see cref="IsVertical"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> IsVerticalProperty = AvaloniaProperty.Register<AxisTrackerControl, bool>(nameof(IsVertical), true);

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
        private const string PartContentContainer = "PART_ContentContainer";

        /// <summary>
        /// The horizontal line part string.
        /// </summary>
        private const string PartHorizontalLine = "PART_HorizontalLine";

        /// <summary>
        /// The vertical line part string.
        /// </summary>
        private const string PartVerticalLine = "PART_VerticalLine";

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
        private Panel contentContainer;

        /// <summary>
        /// The vertical line.
        /// </summary>
        private Line verticalLine;

        /// <summary>
        /// Initializes static members of the <see cref = "AxisTrackerControl" /> class.
        /// </summary>
        static AxisTrackerControl()
        {
            ClipToBoundsProperty.OverrideDefaultValue<AxisTrackerControl>(false);
            PositionProperty.Changed.AddClassHandler<AxisTrackerControl>(PositionChanged);
        }

        /// <summary>
        /// Gets or sets HorizontalLineVisibility.
        /// </summary>
        public bool HorizontalLineVisibility
        {
            get
            {
                return GetValue(HorizontalLineVisibilityProperty);
            }

            set
            {
                SetValue(HorizontalLineVisibilityProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets VerticalLineVisibility.
        /// </summary>
        public bool VerticalLineVisibility
        {
            get
            {
                return GetValue(VerticalLineVisibilityProperty);
            }

            set
            {
                SetValue(VerticalLineVisibilityProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets LineStroke.
        /// </summary>
        public IBrush LineStroke
        {
            get
            {
                return GetValue(LineStrokeProperty);
            }

            set
            {
                SetValue(LineStrokeProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets LineExtents.
        /// </summary>
        public OxyRect LineExtents
        {
            get
            {
                return GetValue(LineExtentsProperty);
            }

            set
            {
                SetValue(LineExtentsProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets LineDashArray.
        /// </summary>
        public List<double> LineDashArray
        {
            get
            {
                return GetValue(LineDashArrayProperty);
            }

            set
            {
                SetValue(LineDashArrayProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show a 'pointer' on the border.
        /// </summary>
        public bool ShowPointer
        {
            get
            {
                return GetValue(ShowPointerProperty);
            }

            set
            {
                SetValue(ShowPointerProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the distance of the content container from the trackers Position.
        /// </summary>
        public double Distance
        {
            get
            {
                return GetValue(DistanceProperty);
            }

            set
            {
                SetValue(DistanceProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets Position of the tracker.
        /// </summary>
        public ScreenPoint Position
        {
            get
            {
                return GetValue(PositionProperty);
            }

            set
            {
                SetValue(PositionProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the tracker is vertical axis.
        /// </summary>
        public bool IsVertical
        {
            get
            {
                return GetValue(IsVerticalProperty);
            }

            set
            {
                SetValue(IsVerticalProperty, value);
            }
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            path = e.NameScope.Get<Path>(PartPath);
            content = e.NameScope.Get<ContentPresenter>(PartContent);
            contentContainer = e.NameScope.Get<Panel>(PartContentContainer);
            horizontalLine = e.NameScope.Find<Line>(PartHorizontalLine);
            verticalLine = e.NameScope.Find<Line>(PartVerticalLine);

            UpdatePositionAndBorder();
        }

        /// <summary>
        /// Called when the position is changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="AvaloniaPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void PositionChanged(AvaloniaObject sender, AvaloniaPropertyChangedEventArgs e)
        {
            ((AxisTrackerControl)sender).OnPositionChanged(e);
        }

        /// <summary>
        /// Called when the position is changed.
        /// </summary>
        /// <param name="dependencyPropertyChangedEventArgs">The dependency property changed event args.</param>
#pragma warning disable RCS1163, IDE0060 // Unused parameter.
        private void OnPositionChanged(AvaloniaPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
#pragma warning restore RCS1163, IDE0060 // Unused parameter.
        {
            UpdatePositionAndBorder();
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

            Control parent = this;
            while (parent is not Canvas && parent != null)
            {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                parent = parent.GetVisualParent() as Control;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
            }

            if (parent == null)
            {
                return;
            }

            // throw new InvalidOperationException("The AxisTrackerControl must have a Canvas parent.");
            var canvasWidth = parent.Bounds.Width;
            var canvasHeight = parent.Bounds.Height;

            content.Measure(new Size(canvasWidth, canvasHeight));
            content.Arrange(new Rect(0, 0, content.DesiredSize.Width, content.DesiredSize.Height));

            var contentWidth = content.DesiredSize.Width;
            var contentHeight = content.DesiredSize.Height;

            var dx = ha == HorizontalAlignment.Center ? -0.5 : ha == HorizontalAlignment.Left ? 0 : -1;
            var dy = va == VerticalAlignment.Center ? -0.5 : va == VerticalAlignment.Top ? 0 : -1;

            path.Data = ShowPointer ? CreatePointerBorderGeometry(ha, va, contentWidth, contentHeight, out var margin)
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
                    horizontalLine.StartPoint = horizontalLine.StartPoint.WithX(LineExtents.Left);
                    horizontalLine.EndPoint = horizontalLine.EndPoint.WithX(LineExtents.Right);
                }
                else
                {
                    horizontalLine.StartPoint = horizontalLine.StartPoint.WithX(0);
                    horizontalLine.EndPoint = horizontalLine.EndPoint.WithX(canvasWidth);
                }

                horizontalLine.StartPoint = horizontalLine.StartPoint.WithY(pos.Y);
                horizontalLine.EndPoint = horizontalLine.EndPoint.WithY(pos.Y);
            }

            if (verticalLine != null)
            {
                if (LineExtents.Height > 0)
                {
                    verticalLine.StartPoint = verticalLine.StartPoint.WithY(LineExtents.Top);
                    verticalLine.EndPoint = verticalLine.EndPoint.WithY(LineExtents.Bottom + margin.Top);
                }
                else
                {
                    verticalLine.StartPoint = verticalLine.StartPoint.WithY(0);
                    verticalLine.EndPoint = verticalLine.EndPoint.WithY(canvasHeight);
                }

                verticalLine.StartPoint = verticalLine.StartPoint.WithX(pos.X);
                verticalLine.EndPoint = verticalLine.EndPoint.WithX(pos.X);
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
            var m = Distance;
            var rect = new Rect(ha == HorizontalAlignment.Left ? m : 0, va == VerticalAlignment.Top ? m : 0, width, height);

            double left = 0;
            double top = 0;
            double right = 0;
            double bottom = 0;

            if (ha == HorizontalAlignment.Left)
            {
                left = m;
            }
            else
            {
                right = m;
            }

            if (va == VerticalAlignment.Top)
            {
                top = m;
            }
            else
            {
                bottom = m;
            }

            margin = new Thickness(left, top, right, bottom);

            return new RectangleGeometry(rect);
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
            var m = Distance;
            margin = new Thickness();

            if (ha == HorizontalAlignment.Center && va == VerticalAlignment.Bottom)
            {
                const double x0 = 0;
                var x1 = width;
                var x2 = (x0 + x1) / 2;
                const double y0 = 0;
                var y1 = height;
                margin = new Thickness(0, 0, 0, m);
                points = new[]
                    {
                        new Point(x0, y0), new Point(x1, y0), new Point(x1, y1), new Point(x2 + (m / 2), y1),
                        new Point(x2, y1 + m), new Point(x2 - (m / 2), y1), new Point(x0, y1)
                    };
            }
            else if (ha == HorizontalAlignment.Center && va == VerticalAlignment.Top)
            {
                const double x0 = 0;
                var x1 = width;
                var x2 = (x0 + x1) / 2;
                var y0 = m;
                var y1 = m + height;
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
                var x0 = m;
                var x1 = m + width;
                const double y0 = 0;
                var y1 = height;
                var y2 = (y0 + y1) / 2;
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
                const double x0 = 0;
                var x1 = width;
                const double y0 = 0;
                var y1 = height;
                var y2 = (y0 + y1) / 2;
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
                throw new ArgumentException("AxisTrackerControl.CreatePointerBorderGeometry: Not possible");
            }

            if (points == null)
            {
                return null;
            }

            var pc = new List<Point>(points.Length);
            foreach (var p in points)
            {
                pc.Add(p);
            }
            var segments = new PathSegments();
            segments.AddRange(pc.Select(p => new LineSegment { Point = p }));
            var pf = new PathFigure { StartPoint = points[0], Segments = segments, IsClosed = true };
            return new PathGeometry { Figures = new PathFigures { pf } };
        }
    }
}
