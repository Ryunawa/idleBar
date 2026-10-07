using System.Collections.Generic;
using Godot;

namespace IdleBar.Ui;

public sealed record ContractRowContent(
    string GoodId,
    string Title,
    IReadOnlyList<string> Facts,
    string Note,
    Color NoteColor,
    double? Progress);
