using System;
using System.Collections.Generic;
using System.Linq;
using Memorandum.Desktop.Models;
using Memorandum.Desktop.Themes;

namespace Memorandum.Desktop.Services;

/// <summary>
/// Строит узлы и рёбра графа: вершины — задания (заметки), папки и теги. Задания в графе не повторяются (уникальность по названию+папка).
/// Эластичное расстояние: все вершины на сетке с шагом GridStep — расстояние между любыми соседними вершинами одинаковое.
/// </summary>
public sealed class NotesGraphDataProvider : IGraphDataProvider
{
    private readonly Func<IReadOnlyList<NoteCardItem>> _getNotes;
    private static readonly string NoteColor = "#2a2a3e";
    private static readonly string FolderColor = "#374151";
    private static readonly string TagColor = "#4b5563";

    public NotesGraphDataProvider(Func<IReadOnlyList<NoteCardItem>> getNotes)
    {
        _getNotes = getNotes ?? (() => Array.Empty<NoteCardItem>());
    }

    public IReadOnlyList<GraphNode> GetNodes()
    {
        var notes = _getNotes();
        var folderSegmentsSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var tagSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenNoteKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var uniqueNotes = new List<NoteCardItem>();

        foreach (var n in notes)
        {
            if (!string.IsNullOrWhiteSpace(n.FolderName))
            {
                var folderPath = n.FolderName.Trim();
                var segments = folderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                foreach (var segment in segments)
                {
                    folderSegmentsSet.Add(segment.Trim());
                }
            }
            foreach (var t in n.TagLabels)
                if (!string.IsNullOrWhiteSpace(t))
                    tagSet.Add(t.Trim());
            var key = (n.Title ?? "").Trim() + "|" + (n.FolderName ?? "").Trim();
            if (seenNoteKeys.Add(key))
                uniqueNotes.Add(n);
        }

        var folderSegments = folderSegmentsSet.OrderBy(f => f).ToList();
        var tags = tagSet.OrderBy(t => t).ToList();
        var nodes = new List<GraphNode>();
        var step = GraphPaintOptions.GridStep;
        var baseX = step * 2;
        var yFolders = step * 2;
        var yNotes = yFolders + step;

        var notesByFolder = new Dictionary<string, List<NoteCardItem>>(StringComparer.OrdinalIgnoreCase);
        foreach (var n in uniqueNotes)
        {
            var fn = string.IsNullOrWhiteSpace(n.FolderName) ? "" : n.FolderName.Trim();
            if (!notesByFolder.TryGetValue(fn, out var list))
            {
                list = new List<NoteCardItem>();
                notesByFolder[fn] = list;
            }
            list.Add(n);
        }

        var maxNotesInColumn = 0;
        foreach (var folderName in notesByFolder.Keys)
        {
            if (notesByFolder.TryGetValue(folderName, out var list) && list.Count > maxNotesInColumn)
                maxNotesInColumn = list.Count;
        }
        if (maxNotesInColumn == 0)
            maxNotesInColumn = 1;
        var yTags = yNotes + maxNotesInColumn * step;

        for (var i = 0; i < folderSegments.Count; i++)
        {
            nodes.Add(new GraphNode
            {
                Id = "f_" + folderSegments[i],
                Type = GraphNodeType.Folder,
                Label = folderSegments[i],
                X = baseX + i * step,
                Y = yFolders,
                Color = FolderColor
            });
        }

        var noteIndex = 0;
        foreach (var kvp in notesByFolder.OrderBy(k => k.Key))
        {
            var folderPath = kvp.Key;
            var folderNotes = kvp.Value;
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                var folderX = folderSegments.Count > 0 ? baseX + folderSegments.Count * step : baseX;
                for (var j = 0; j < folderNotes.Count; j++)
                {
                    var note = folderNotes[j];
                    nodes.Add(new GraphNode
                    {
                        Id = "n_" + noteIndex,
                        Type = GraphNodeType.Note,
                        Label = note.Title?.Length > 20 ? note.Title[..17] + "..." : (note.Title ?? ""),
                        X = folderX,
                        Y = yNotes + j * step,
                        Color = NoteColor
                    });
                    noteIndex++;
                }
                continue;
            }

            var segments = folderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
                continue;

            var lastSegment = segments[segments.Length - 1].Trim();
            var lastSegmentIdx = folderSegments.IndexOf(lastSegment);
            if (lastSegmentIdx < 0)
                continue;

            var noteColX = baseX + lastSegmentIdx * step;
            for (var j = 0; j < folderNotes.Count; j++)
            {
                var note = folderNotes[j];
                nodes.Add(new GraphNode
                {
                    Id = "n_" + noteIndex,
                    Type = GraphNodeType.Note,
                    Label = note.Title?.Length > 20 ? note.Title[..17] + "..." : (note.Title ?? ""),
                    X = noteColX,
                    Y = yNotes + j * step,
                    Color = NoteColor
                });
                noteIndex++;
            }
        }

        for (var i = 0; i < tags.Count; i++)
        {
            nodes.Add(new GraphNode
            {
                Id = "t_" + tags[i],
                Type = GraphNodeType.Tag,
                Label = tags[i],
                X = baseX + i * step,
                Y = yTags,
                Color = TagColor
            });
        }

        return nodes;
    }

    public IReadOnlyList<GraphEdge> GetEdges()
    {
        var notes = _getNotes();
        var nodes = GetNodes();
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var uniqueNotes = new List<NoteCardItem>();
        foreach (var n in notes)
        {
            var key = (n.Title ?? "").Trim() + "|" + (n.FolderName ?? "").Trim();
            if (seenKeys.Add(key))
                uniqueNotes.Add(n);
        }
        var notesByFolder = new Dictionary<string, List<NoteCardItem>>(StringComparer.OrdinalIgnoreCase);
        foreach (var n in uniqueNotes)
        {
            var fn = string.IsNullOrWhiteSpace(n.FolderName) ? "" : n.FolderName.Trim();
            if (!notesByFolder.TryGetValue(fn, out var list))
            {
                list = new List<NoteCardItem>();
                notesByFolder[fn] = list;
            }
            list.Add(n);
        }
        var noteKeys = new List<string>();
        foreach (var kvp in notesByFolder.OrderBy(k => k.Key))
        {
            foreach (var note in kvp.Value)
                noteKeys.Add((note.Title ?? "").Trim() + "|" + (note.FolderName ?? "").Trim());
        }
        var keyToNoteId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < noteKeys.Count; i++)
            keyToNoteId[noteKeys[i]] = "n_" + i;

        var edges = new List<GraphEdge>();
        var addedEdges = new HashSet<(string From, string To)>();

        foreach (var kvp in notesByFolder.OrderBy(k => k.Key))
        {
            var folderPath = kvp.Key;
            if (string.IsNullOrWhiteSpace(folderPath))
                continue;

            var segments = folderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 2)
                continue;

            for (var i = 0; i < segments.Length - 1; i++)
            {
                var parentSegment = segments[i].Trim();
                var childSegment = segments[i + 1].Trim();
                var parentId = "f_" + parentSegment;
                var childId = "f_" + childSegment;

                if (nodes.Any(n => n.Id == parentId) && nodes.Any(n => n.Id == childId) && addedEdges.Add((parentId, childId)))
                {
                    edges.Add(new GraphEdge { From = parentId, To = childId, Type = GraphEdgeType.InFolder });
                }
            }
        }

        foreach (var note in notes)
        {
            var key = (note.Title ?? "").Trim() + "|" + (note.FolderName ?? "").Trim();
            if (!keyToNoteId.TryGetValue(key, out var noteId)) continue;

            if (!string.IsNullOrWhiteSpace(note.FolderName))
            {
                var folderPath = note.FolderName.Trim();
                var segments = folderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length > 0)
                {
                    var lastSegment = segments[segments.Length - 1].Trim();
                    var folderId = "f_" + lastSegment;
                    if (nodes.Any(n => n.Id == folderId) && addedEdges.Add((noteId, folderId)))
                        edges.Add(new GraphEdge { From = noteId, To = folderId, Type = GraphEdgeType.InFolder });
                }
            }

            foreach (var tag in note.TagLabels)
            {
                if (string.IsNullOrWhiteSpace(tag)) continue;
                var tagId = "t_" + tag.Trim();
                if (nodes.Any(n => n.Id == tagId) && addedEdges.Add((noteId, tagId)))
                    edges.Add(new GraphEdge { From = noteId, To = tagId, Type = GraphEdgeType.HasTag });
            }
        }
        return edges;
    }
}
