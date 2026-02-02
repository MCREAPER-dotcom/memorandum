using System;
using System.Collections.Generic;
using System.Linq;
using Memorandum.Desktop.Models;

namespace Memorandum.Desktop.Services;

/// <summary>
/// Строит узлы и рёбра графа из списка заметок: папки, уникальные заметки (1 заметка = 1 вершина), теги.
/// Привязка к папкам: раскладка папки — заметки — теги, увеличенные отступы для читаемости линий.
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
        var stepX = 240;
        var yFolders = 140;
        var yNotes = 360;
        var yTags = 580;

        for (var i = 0; i < folders.Count; i++)
        {
            nodes.Add(new GraphNode
            {
                Id = "f_" + folders[i],
                Type = GraphNodeType.Folder,
                Label = folders[i],
                X = 220 + i * stepX,
                Y = yFolders,
                Color = FolderColor
            });
        }

        for (var i = 0; i < uniqueNotes.Count; i++)
        {
            var note = uniqueNotes[i];
            var folderName = string.IsNullOrWhiteSpace(note.FolderName) ? "" : note.FolderName.Trim();
            var folderIndex = string.IsNullOrEmpty(folderName) ? 0 : Math.Max(0, folders.IndexOf(folderName));
            var baseX = 220 + folderIndex * stepX;
            var row = i % Math.Max(1, folders.Count);
            var col = i / Math.Max(1, folders.Count);
            var noteX = baseX + col * 56 - row * 8;
            var noteY = yNotes + row * 70;

            nodes.Add(new GraphNode
            {
                Id = "n_" + i,
                Type = GraphNodeType.Note,
                Label = note.Title?.Length > 20 ? note.Title[..17] + "..." : (note.Title ?? ""),
                X = noteX,
                Y = noteY,
                Color = NoteColor
            });
        }

        for (var i = 0; i < tags.Count; i++)
        {
            nodes.Add(new GraphNode
            {
                Id = "t_" + tags[i],
                Type = GraphNodeType.Tag,
                Label = tags[i],
                X = 220 + i * stepX,
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
        var noteKeys = new List<string>();
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var n in notes)
        {
            var key = (n.Title ?? "").Trim() + "|" + (n.FolderName ?? "").Trim();
            if (seenKeys.Add(key))
                noteKeys.Add(key);
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
