using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Godot;

namespace IdleBar.Ui;

public sealed class Journal
{
    private const string Path = "user://journal.txt";
    private const int MaxEntries = 200;
    private static readonly TimeSpan RepeatWindow = TimeSpan.FromSeconds(10);

    private readonly List<JournalEntry> _entries = [];
    private readonly string _file = ProjectSettings.GlobalizePath(Path);

    public Journal()
    {
        try
        {
            if (File.Exists(_file))
            {
                _entries.AddRange(File.ReadAllLines(_file).Select(Parse).OfType<JournalEntry>().TakeLast(MaxEntries));
            }
        }
        catch (IOException)
        {
        }
    }

    public event Action? Changed;

    public IReadOnlyList<JournalEntry> Entries => _entries;

    public void Add(string text)
    {
        DateTimeOffset now = DateTimeOffset.Now;
        if (_entries.Count > 0 && _entries[^1].Text == text && now - _entries[^1].At < RepeatWindow)
        {
            return;
        }

        _entries.Add(new JournalEntry(now, text));
        if (_entries.Count > MaxEntries)
        {
            _entries.RemoveRange(0, _entries.Count - MaxEntries);
        }

        try
        {
            File.WriteAllLines(_file, _entries.Select(Format));
        }
        catch (IOException)
        {
        }

        Changed?.Invoke();
    }

    private static string Format(JournalEntry entry) =>
        $"{entry.At.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)}\t{entry.Text.ReplaceLineEndings(" ")}";

    private static JournalEntry? Parse(string line)
    {
        string[] parts = line.Split('\t', 2);
        return parts.Length == 2 && long.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out long seconds)
            ? new JournalEntry(DateTimeOffset.FromUnixTimeSeconds(seconds).ToLocalTime(), parts[1])
            : null;
    }
}
