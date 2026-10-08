using System;
using System.Globalization;
using System.Linq;
using Godot;

namespace IdleBar.Ui;

public partial class JournalPanel : VBoxContainer
{
    private static readonly CultureInfo French = new("fr-FR");

    private VBoxContainer _rows = null!;

    public override void _Ready()
    {
        Name = "Journal";
        _rows = new VBoxContainer();
        _rows.AddThemeConstantOverride("separation", 4);
        AddChild(WindowRows.Scroll(_rows));
    }

    public void Refresh(Journal journal)
    {
        WindowRows.Clear(_rows);
        if (journal.Entries.Count == 0)
        {
            _rows.AddChild(WindowRows.Muted("Les annonces de la barre s'inscrivent ici : saluts, visites, objectifs, habitués… pour ne rien manquer quand tu ne regardes pas l'écran."));
            return;
        }

        DateTime? day = null;
        foreach (JournalEntry entry in journal.Entries.Reverse())
        {
            if (entry.At.Date != day)
            {
                day = entry.At.Date;
                _rows.AddChild(WindowRows.Heading(DayName(entry.At.Date)));
            }

            HBoxContainer row = new();
            row.AddThemeConstantOverride("separation", 10);
            Label time = WindowRows.Muted(entry.At.ToString("HH:mm", French));
            time.AutowrapMode = TextServer.AutowrapMode.Off;
            time.SizeFlagsVertical = SizeFlags.ShrinkBegin;
            row.AddChild(time);
            row.AddChild(new Label
            {
                Text = entry.Text,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            });
            _rows.AddChild(row);
        }
    }

    private static string DayName(DateTime date)
    {
        DateTime today = DateTime.Today;
        return date == today ? "Aujourd'hui"
            : date == today.AddDays(-1) ? "Hier"
            : date.ToString("dddd d MMMM", French);
    }
}
