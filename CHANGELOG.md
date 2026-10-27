# Changelog

## Unreleased

- `TextFieldVariant` on the theme: `Outlined` makes every `TextBox` and `ComboBox` without a class of its own
  outlined. Fields in control templates, fields with their own `Theme`, search bars and fields with the class
  `Filled` stay as they are.
- Slider: the outer ends of the track no longer move by a pixel while the value changes. Layout
  rounding rounded the origin and size of the track segments separately; the new `SliderTrack`
  snaps every segment edge to the track ends or the handle.

## 0.1.0 – 2026-10-18

First release, for Avalonia 12.1.

- Material 3 theme with dynamic color from a seed (HCT, all color roles, light and dark), state
  layers, ripples, focus rings and a recolored Fluent fallback for controls without an M3 template.
- Expressive update: spring motion (Expressive and Standard schemes), expanded corner scale, the
  35 shapes of the Material shape library with shape morphing, emphasized typography.
- Components: buttons XS–XL with press morph, toggle buttons, standard and connected button groups,
  split buttons, FAB and FAB menu, loading indicator, wavy progress indicators, slider, floating and
  docked toolbars, flexible top app bars, expandable navigation rail, Expressive lists and menus,
  cards, dialogs, snackbars, badges, text fields with labels and supporting text.
- Date and time pickers: modal, docked and range date pickers, clock-dial time picker with keyboard
  input.
- Elevation shadows and the hover transition match material-web.
- Shadows and focus rings are not clipped by containers (Avalonia 12 clips templated controls);
  tab content is clipped only for a page transition.
- Controls appear in their final state: template transitions start once a control has settled.
- Single-line text boxes leave vertical swipes to the list behind them.
- GridSplitter as divider line or Material 3 drag handle (`Classes="Handle"`).
