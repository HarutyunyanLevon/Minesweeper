# Editing the UI

Every screen is a prefab you edit in Unity (double-click it, edit in Prefab Mode, save, press Play). No code change is needed.

| Prefab | What it is | Canvas |
|---|---|---|
| `Resources/UI/Hud/HudWindow` | In-shift HUD: day/money/quota plates, battery, key hints, hotbar, toast line, dusk overlay | Hud (1920 x 1080) |
| `Resources/UI/Hud/WorkBar` | Progress bar above the deminer while defusing or collecting | Hud |
| `Resources/UI/Windows/RulebookWindow` | Book of Regulations (briefing, and the B key) | Windows (1024 x 660) |
| `Resources/UI/Windows/DebriefWindow` | Page 1 of the evening: the state report (rows are written on the form's lines) | Windows |
| `Resources/UI/Windows/ExpensesWindow` | Page 2: the family page with the locket, the note and the notebook with checkable expenses | Windows |

Each canvas scales with the screen (Expand), so design at its reference size.

## Tips
- Templates stay visible in the prefab so you can style them: `RowTemplate` (report), `ExpenseTemplate` (expenses). The game clones them per line and hides the original.
- Overlapping layouts in `RulebookWindow` (`PosterGroup`, `PlainTitle`/`BodyPlain`, `BareGroup`) are shown or hidden by code per page; toggle them on in the Inspector to edit them.
- `HudView` / `HotbarSlot` / `WorkBarView` / `DebriefWindow` / `ExpensesWindow` / `RulebookWindow` expose their texts, colours and format strings (`Day {0} ({1})`, `Kits {0}/{1}`, ...) in the Inspector.
- Text with `OsFont` uses a handwriting font from Windows (`Font Names` list).
- Sprites live in `Resources/UI` (kit) and `Resources/Debrief`, `Resources/Rulebook`. Nine-slice borders come from `Resources/UI/ui_kit.json`.

## Rebuilding from scratch
`Minefield > UI > Build Prefabs` recreates every prefab and OVERWRITES your edits. Default positions and sizes are the constants at the top of `Assets/Editor/UiWindowBuilder.cs`. Prefer editing the prefabs.

## Housekeeping
- `Minefield > Tools > Audit Unused Assets` writes `audit.txt` (project root): assets under Art and Resources that no scene, prefab or script mentions. Rulebook posters are loaded by name from `basic_rules.json`, so they show up there although they are used.
- `Minefield > Tools > Delete Files Listed In delete_list.txt` deletes the `Assets/...` paths listed (one per line).
- Removed UI kit extras (wide buttons, speech panel, heart, tags, ...) are still in the SpriteCook UI kit and can be fetched again.
- The dev readout (`DevHud`, DEV ON) is the only IMGUI left; it is a debug tool.
