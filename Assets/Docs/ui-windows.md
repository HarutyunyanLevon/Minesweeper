# Editing the briefing and debrief windows

Both windows are ordinary Unity prefabs in `Assets/Resources/UI/Windows/`:

- `RulebookWindow.prefab` - the Book of Regulations (daily briefing, and the B key in the field).
- `DebriefWindow.prefab` - the end-of-day report (page 1) and the family page with expenses (page 2).

## How to edit
1. Double-click a prefab (Prefab Mode). Move, resize, recolour, swap sprites or fonts like any uGUI object.
2. Overlapping layouts are switched off in the prefab: in DebriefWindow turn `FamilyPage` on to edit it (and `ReportPage` off); in RulebookWindow each page has `PosterGroup`, `BodyPlain`/`PlainTitle` and `BareGroup`, which are shown or hidden by code at runtime.
3. `RowTemplate` and `ExpenseTemplate` are the row looks; the game clones them and hides the originals.
4. Text with the `OsFont` component uses a handwriting font from Windows (see its `Font Names` list).
5. Save the prefab and press Play. No code change needed.

## Rebuilding from scratch
Menu `Minefield > UI > Build Windows` recreates both prefabs and overwrites your edits. All default positions are constants at the top of `Assets/Editor/UiWindowBuilder.cs`.

## Art
- Debrief: `Resources/Debrief/report_form.png`, `family_table.png`, `locket_open.png`, `notebook.png` (Soviet spiral notebook).
- Book: `Resources/Rulebook/book_spread.png`, `poster_ribbon.png`, posters in `Resources/Rulebook/Posters/`.
