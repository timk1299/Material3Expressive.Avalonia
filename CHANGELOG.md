# Changelog

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
