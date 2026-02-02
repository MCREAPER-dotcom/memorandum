using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Memorandum.Desktop.Controls;
using Memorandum.Desktop.Models;
using Memorandum.Desktop.Services;

namespace Memorandum.Desktop.Views;

public partial class GraphView : UserControl
{
    private readonly IGraphDataProvider _dataProvider;
    private IReadOnlyList<GraphNode> _nodes = new List<GraphNode>();
    private IReadOnlyList<GraphEdge> _edges = new List<GraphEdge>();

    public GraphView(IGraphDataProvider? dataProvider = null)
    {
        _dataProvider = dataProvider ?? new MockGraphDataProvider();
        InitializeComponent();
        RefreshData();
    }

    public void RefreshData()
    {
        _nodes = _dataProvider.GetNodes();
        _edges = _dataProvider.GetEdges();
        GraphCanvas.Nodes = _nodes;
        GraphCanvas.Edges = _edges;
    }

    public System.Action? OnBack { get; set; }
    public System.Action<string, string>? OnOpenNoteRequested { get; set; }

    private void OnBackClick(object? sender, RoutedEventArgs e) => OnBack?.Invoke();

    private void OnZoomOutClick(object? sender, RoutedEventArgs e)
    {
        GraphCanvas.Zoom = System.Math.Max(0.3, GraphCanvas.Zoom / 1.2);
        UpdateZoomText();
    }

    private void OnZoomInClick(object? sender, RoutedEventArgs e)
    {
        GraphCanvas.Zoom = System.Math.Min(3, GraphCanvas.Zoom * 1.2);
        UpdateZoomText();
    }

    private void OnResetClick(object? sender, RoutedEventArgs e)
    {
        GraphCanvas.Zoom = 1;
        GraphCanvas.Offset = new Point(0, 0);
        UpdateZoomText();
    }

    private void OnGraphZoomChanged() => UpdateZoomText();
    private void OnGraphOffsetChanged() { }

    private void OnGraphSelectedNodeChanged(GraphNode? node)
    {
        if (node == null)
        {
            SidePanel.IsVisible = false;
            return;
        }

        SidePanel.IsVisible = true;
        NodeTypeText.Text = GraphNode.GetTypeDisplayName(node.Type);
        NodeLabelText.Text = node.Label;
        NodeColorText.Text = node.Color;
        NodeColorPreview.Background = new SolidColorBrush(Color.Parse(node.Color));

        ConnectionsStack.Children.Clear();
        foreach (var edge in _edges)
        {
            if (edge.From != node.Id && edge.To != node.Id) continue;
            var otherId = edge.From == node.Id ? edge.To : edge.From;
            var other = FindNode(otherId);
            if (other == null) continue;
            var prefix = edge.Type == GraphEdgeType.InFolder ? "[в папке] " : "[тег] ";
            var btn = new Button { Content = prefix + other.Label };
            btn.Classes.Add("GraphLinkButton");
            btn.Click += (_, _) =>
            {
                GraphCanvas.SelectedNode = other;
                OnGraphSelectedNodeChanged(other);
            };
            ConnectionsStack.Children.Add(btn);
        }

        OpenNoteButton.IsVisible = node.Type == GraphNodeType.Note;
    }

    private void OnCloseSidePanelClick(object? sender, RoutedEventArgs e)
    {
        GraphCanvas.SelectedNode = null;
        SidePanel.IsVisible = false;
    }

    private void OnOpenNoteClick(object? sender, RoutedEventArgs e)
    {
        var node = GraphCanvas.SelectedNode;
        if (node != null && node.Type == GraphNodeType.Note)
            OnOpenNoteRequested?.Invoke(node.Id, node.Label);
        else if (node != null)
            OnOpenNoteRequested?.Invoke(node.Label, "");
    }

    private void UpdateZoomText() => ZoomText.Text = $"{System.Math.Round(GraphCanvas.Zoom * 100)}%";

    private GraphNode? FindNode(string id)
    {
        foreach (var n in _nodes)
            if (n.Id == id) return n;
        return null;
    }
}
