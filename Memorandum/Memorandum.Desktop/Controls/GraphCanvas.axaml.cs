using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Memorandum.Desktop.Models;
using Memorandum.Desktop.Themes;

namespace Memorandum.Desktop.Controls;

public partial class GraphCanvas : UserControl
{
    public static readonly StyledProperty<IReadOnlyList<GraphNode>?> NodesProperty =
        AvaloniaProperty.Register<GraphCanvas, IReadOnlyList<GraphNode>?>(nameof(Nodes));

    public static readonly StyledProperty<IReadOnlyList<GraphEdge>?> EdgesProperty =
        AvaloniaProperty.Register<GraphCanvas, IReadOnlyList<GraphEdge>?>(nameof(Edges));

    public static readonly StyledProperty<double> ZoomProperty =
        AvaloniaProperty.Register<GraphCanvas, double>(nameof(Zoom), 1.0);

    public static readonly StyledProperty<Point> OffsetProperty =
        AvaloniaProperty.Register<GraphCanvas, Point>(nameof(Offset), new Point(0, 0));

    public static readonly StyledProperty<GraphNode?> SelectedNodeProperty =
        AvaloniaProperty.Register<GraphCanvas, GraphNode?>(nameof(SelectedNode));

    private bool _isDragging;
    private GraphNode? _draggedNode;
    private double _nodeDragOffsetX;
    private double _nodeDragOffsetY;

    static GraphCanvas()
    {
        NodesProperty.Changed.AddClassHandler<GraphCanvas>((c, _) => c.InvalidateVisual());
        EdgesProperty.Changed.AddClassHandler<GraphCanvas>((c, _) => c.InvalidateVisual());
        ZoomProperty.Changed.AddClassHandler<GraphCanvas>((c, _) => c.InvalidateVisual());
        OffsetProperty.Changed.AddClassHandler<GraphCanvas>((c, _) => c.InvalidateVisual());
        SelectedNodeProperty.Changed.AddClassHandler<GraphCanvas>((c, _) => c.InvalidateVisual());
    }
    private Point _dragStart;

    public IReadOnlyList<GraphNode>? Nodes
    {
        get => GetValue(NodesProperty);
        set => SetValue(NodesProperty, value);
    }

    public IReadOnlyList<GraphEdge>? Edges
    {
        get => GetValue(EdgesProperty);
        set => SetValue(EdgesProperty, value);
    }

    public double Zoom
    {
        get => GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    public Point Offset
    {
        get => GetValue(OffsetProperty);
        set => SetValue(OffsetProperty, value);
    }

    public GraphNode? SelectedNode
    {
        get => GetValue(SelectedNodeProperty);
        set => SetValue(SelectedNodeProperty, value);
    }

    public event System.Action? ZoomChanged;
    public event System.Action? OffsetChanged;
    public event System.Action<GraphNode?>? SelectedNodeChanged;

    public GraphCanvas()
    {
        InitializeComponent();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var pt = e.GetPosition(this);
        var (graphX, graphY) = ScreenToGraph(pt.X, pt.Y);

        var node = HitTestNode(graphX, graphY);
        if (node != null)
        {
            _draggedNode = node;
            _nodeDragOffsetX = graphX - node.X;
            _nodeDragOffsetY = graphY - node.Y;
            SelectedNode = node;
            SelectedNodeChanged?.Invoke(node);
            e.Pointer.Capture(this);
        }
        else
        {
            _isDragging = true;
            _dragStart = new Point(pt.X - Offset.X, pt.Y - Offset.Y);
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var pt = e.GetPosition(this);
        var (graphX, graphY) = ScreenToGraph(pt.X, pt.Y);

        if (_draggedNode != null)
        {
            _draggedNode.X = graphX - _nodeDragOffsetX;
            _draggedNode.Y = graphY - _nodeDragOffsetY;
            InvalidateVisual();
        }
        else if (_isDragging)
        {
            Offset = new Point(pt.X - _dragStart.X, pt.Y - _dragStart.Y);
            OffsetChanged?.Invoke();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_draggedNode != null)
        {
            e.Pointer.Capture(null);
            _draggedNode = null;
        }
        _isDragging = false;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _draggedNode = null;
        _isDragging = false;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        var delta = e.Delta.Y > 0 ? 1.1 : 0.9;
        var newZoom = System.Math.Clamp(Zoom * delta, 0.3, 3.0);
        Zoom = newZoom;
        ZoomChanged?.Invoke();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var nodes = Nodes;
        var edges = Edges;
        if (nodes == null || edges == null) return;

        using (context.PushPreTransform(Matrix.CreateTranslation(Offset.X, Offset.Y)))
        using (context.PushPreTransform(Matrix.CreateScale(Zoom, Zoom)))
        {
            var edgeBrush = new SolidColorBrush(Color.Parse(GraphPaintOptions.EdgeColor));
            var edgePen = new Pen(edgeBrush, 2);
            var edgeDashed = new Pen(edgeBrush, 2, new DashStyle(new double[] { 5, 5 }, 0));
            var outlinePen = new Pen(new SolidColorBrush(Color.Parse(GraphPaintOptions.OutlineColor)), 2);
            var selectedPen = new Pen(new SolidColorBrush(Color.Parse(GraphPaintOptions.SelectedColor)), 3);
            var textBrush = new SolidColorBrush(Color.Parse(GraphPaintOptions.TextColor));

            foreach (var edge in edges)
            {
                var fromNode = FindNode(nodes, edge.From);
                var toNode = FindNode(nodes, edge.To);
                if (fromNode == null || toNode == null) continue;

                var pen = edge.Type == GraphEdgeType.HasTag ? edgeDashed : edgePen;
                context.DrawLine(pen, new Point(fromNode.X, fromNode.Y), new Point(toNode.X, toNode.Y));
            }

            foreach (var node in nodes)
            {
                var radius = node.Radius;
                var brush = new SolidColorBrush(Color.Parse(node.Color));
                var pen = node == SelectedNode ? selectedPen : outlinePen;
                context.DrawEllipse(brush, pen, new Point(node.X, node.Y), radius, radius);

                var icon = node.Type switch
                {
                    GraphNodeType.Note => "N",
                    GraphNodeType.Folder => "F",
                    GraphNodeType.Tag => "T",
                    _ => "?"
                };
                var formatted = new FormattedText(
                    icon,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    Typeface.Default,
                    20,
                    textBrush);
                context.DrawText(formatted, new Point(node.X - formatted.Width / 2, node.Y - formatted.Height / 2));

                var label = node.Label.Length > 15 ? node.Label.Substring(0, 12) + "..." : node.Label;
                var labelFormatted = new FormattedText(
                    label,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    Typeface.Default,
                    14,
                    textBrush);
                var labelX = node.X - labelFormatted.Width / 2;
                var labelY = node.Y + radius + 4;
                context.FillRectangle(new SolidColorBrush(Color.Parse(GraphPaintOptions.LabelBackgroundColor)), new Rect(labelX - 4, labelY, labelFormatted.Width + 8, 20));
                context.DrawText(labelFormatted, new Point(labelX, labelY + 2));
            }
        }
    }

    private (double x, double y) ScreenToGraph(double screenX, double screenY)
    {
        return ((screenX - Offset.X) / Zoom, (screenY - Offset.Y) / Zoom);
    }

    private static GraphNode? FindNode(IReadOnlyList<GraphNode> nodes, string id)
    {
        foreach (var n in nodes)
            if (n.Id == id) return n;
        return null;
    }

    private GraphNode? HitTestNode(double graphX, double graphY)
    {
        var nodes = Nodes;
        if (nodes == null) return null;
        foreach (var node in nodes)
        {
            var dx = graphX - node.X;
            var dy = graphY - node.Y;
            if (dx * dx + dy * dy <= node.Radius * node.Radius)
                return node;
        }
        return null;
    }
}
