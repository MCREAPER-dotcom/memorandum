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
        var folderSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var tagSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenNoteKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var uniqueNotes = new List<NoteCardItem>();

        foreach (var n in notes)
        {
            if (!string.IsNullOrWhiteSpace(n.FolderName))
                folderSet.Add(n.FolderName.Trim());
            foreach (var t in n.TagLabels)
                if (!string.IsNullOrWhiteSpace(t))
                    tagSet.Add(t.Trim());
            var key = (n.Title ?? "").Trim() + "|" + (n.FolderName ?? "").Trim();
            if (seenNoteKeys.Add(key))
                uniqueNotes.Add(n);
        }

        var folders = folderSet.OrderBy(f => f).ToList();
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
        foreach (var folderName in folders)
        {
            if (notesByFolder.TryGetValue(folderName, out var list) && list.Count > maxNotesInColumn)
                maxNotesInColumn = list.Count;
        }
        if (notesByFolder.TryGetValue("", out var noFolder) && noFolder.Count > maxNotesInColumn)
            maxNotesInColumn = noFolder.Count;
        if (maxNotesInColumn == 0)
            maxNotesInColumn = 1;
        var yTags = yNotes + maxNotesInColumn * step;

        for (var i = 0; i < folders.Count; i++)
        {
            nodes.Add(new GraphNode
            {
                Id = "f_" + folders[i],
                Type = GraphNodeType.Folder,
                Label = folders[i],
                X = baseX + i * step,
                Y = yFolders,
                Color = FolderColor
            });
        }

        var noteIndex = 0;
        foreach (var folderName in folders)
        {
            if (!notesByFolder.TryGetValue(folderName, out var folderNotes))
                continue;
            var folderIdx = folders.IndexOf(folderName);
            var folderX = baseX + folderIdx * step;
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
        }
        if (notesByFolder.TryGetValue("", out var noFolderNotes))
        {
            var folderX = folders.Count > 0 ? baseX + folders.Count * step : baseX;
            for (var j = 0; j < noFolderNotes.Count; j++)
            {
                var note = noFolderNotes[j];
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
        var folders = uniqueNotes
            .Select(n => string.IsNullOrWhiteSpace(n.FolderName) ? "" : n.FolderName!.Trim())
            .Distinct()
            .OrderBy(f => f)
            .Where(f => !string.IsNullOrEmpty(f))
            .ToList();
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
        foreach (var folderName in folders)
        {
            if (!notesByFolder.TryGetValue(folderName, out var folderNotes))
                continue;
            foreach (var note in folderNotes)
                noteKeys.Add((note.Title ?? "").Trim() + "|" + (note.FolderName ?? "").Trim());
        }
        if (notesByFolder.TryGetValue("", out var noFolderNotes))
        {
            foreach (var note in noFolderNotes)
                noteKeys.Add((note.Title ?? "").Trim() + "|");
        }
        var keyToNoteId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < noteKeys.Count; i++)
            keyToNoteId[noteKeys[i]] = "n_" + i;

        var edges = new List<GraphEdge>();
        var addedEdges = new HashSet<(string From, string To)>();
        foreach (var note in notes)
        {
            var key = (note.Title ?? "").Trim() + "|" + (note.FolderName ?? "").Trim();
            if (!keyToNoteId.TryGetValue(key, out var noteId)) continue;
            if (!string.IsNullOrWhiteSpace(note.FolderName))
            {
                var folderId = "f_" + note.FolderName.Trim();
                if (nodes.Any(n => n.Id == folderId) && addedEdges.Add((noteId, folderId)))
                    edges.Add(new GraphEdge { From = noteId, To = folderId, Type = GraphEdgeType.InFolder });
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
