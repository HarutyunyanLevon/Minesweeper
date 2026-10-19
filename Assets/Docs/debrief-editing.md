# Editing the debrief windows

Everything lives in `Assets/Resources/Debrief/`:

| File | What it is |
|---|---|
| `layout.json` | All positions, sizes, colours, texts, fonts of both pages |
| `report_form.png` | Page 1 background (formal report). 1024x576 |
| `family_table.png` | Page 2 background (lace table). 1024x576 |
| `locket_open.png` | The open locket (transparent PNG) |
| `notepad.png` | The worn notepad (transparent PNG, 260x205 drawn at 2x) |

The canvas is 1024x576 and is scaled to fit the window. Coordinates are canvas pixels, origin top-left.
Colours: `#RRGGBB` or `#RRGGBBAA`.

## Workflow
1. Edit `layout.json`, save.
2. (In the editor the file is read straight from disk, no import needed.)
3. Play, turn DEV on (F10), press **F9** to end the shift, **Enter** for page 2.
4. While a debrief is open, press **F5** to reload `layout.json`. No need to restart.

## layout.json
- `report.lineYs`: y of every ruled line of the form. Row N is written on line N.
- `report.rowX/rowW/rowH/rowSize/rowLift`: row text box. `rowLift` nudges text down (+) or up (-) from the line.
- `report.color / badColor`: normal and bad (fine, unmet quota) row colours.
- `report.title`, `report.button`: heading and the "Next" stamp.
- `family.locket`: art path, x, y, width.
- `family.note`: the handwritten note. `left/top/w/h` are fractions of the locket image, `tilt` degrees,
  `text` (use `\n` for a line break), `fonts` = OS font names tried in order, `shadow`/`shadowOffset`.
- `family.pad`, `padArt`: the notepad rectangle and its art. Leave `padArt` empty for a plain `padColor` box.
- `family.header`, `inHand`: pad heading and money line (y is relative to the pad).
- `family.listY / lineH / priceW / boxSize / boxGap / rowSize`: the checklist. The ruled lines in `notepad.png`
  are 27px apart (54 on canvas), so keep `lineH` at 54 to stay on them.
- `family.ink / faint / tick / boxFill`: text, unaffordable text, checkmark and checkbox fill colours.
- `family.next`: the "Next day" button (`border` 0 = no frame).

## Optional expenses
`Assets/Resources/Family/expenses.json`: `id`, `label`, `cost` (0 with kind `medicine` uses the difficulty's
medicine cost), `kind`, `placeholder`. Add or remove entries freely; lines past the pad height need `pad.h` and the art enlarged.

## Replacing art
Overwrite a PNG with the same name and size ratio. Page backgrounds are stretched to the full canvas.
