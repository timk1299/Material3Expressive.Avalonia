# Changelog

## 0.3.1 – 2026-10-29

- Each text field has its own context menu (`M3TextBoxContextFlyout`, Cut · Copy · Paste) instead of
  Avalonia's `DefaultTextBoxContextFlyout`, one menu for all fields. A field keeps its selection while it loses
  the focus with its context menu open; with the shared menu open for another field, a field selected before
  (e.g. by a long press on a touch screen) kept its selection highlighted.

## 0.3.0 – 2026-10-29

- Scrollable tabs (`Classes="Scrollable"`) scroll: the tab strip lies in a `ScrollViewer` (scrolling off
  without the class, so other tabs keep their layout). Tabs that do not fit scroll sideways without a scroll
  bar, by swipe or mouse wheel (`HorizontalWheel.IsEnabled` on a `ScrollViewer` turns the wheel sideways).
- Elevation level 0 is transparent: buttons without a container (e.g. a transparent `Button` of an app) no
  longer show a hairline ring. Replaces the fix for `Icon` buttons only; transitions from level 0 fade the
  shadow in.
- Buttons without a size class have the padding of `Small` (16dp), the documented default; `XSmall` has 12dp.
  Large FABs (`LargeFab`, `ExtendedFab Large`) have 36dp icons.
- Icons in tab content keep the size of their component: the 24dp tab icon size applies to the tab's own
  icon only (it made the icons of XSmall/Small buttons, chips and FABs on a tab page 24dp).
- Elevated toggle buttons keep their shadow: level 2 on hover, level 1 while pressed (was level 0).
- `FloatingToolbar` is horizontal by default, with the FAB beside it, and `Orientation="Vertical"` stacks its
  items (the property took StackPanel's vertical default, so neither worked).
- Keyboard: Tab reaches the content of a dialog before its buttons; the month of a calendar is one Tab stop
  (the selected day, else today), the arrow keys move between the days; the modal date picker opens with the
  focus on the selected day, and picker dialogs in input mode keep the focus in their field. Escape closes the
  calendar of a `DateField` and, like Cancel, returns the focus to the field. Escape in a search bar leaves
  the search like the back button.
- Menus highlight the item under the pointer or the keyboard focus, no longer the item Avalonia selects when
  a menu opens under the mouse. Menu separators (`Header="-"`) are a thin line with 8dp above and below
  instead of a 44dp high bar.
- An icon with a label in a button (a horizontal `StackPanel` with a `TextBlock`, also in a `SplitButton`)
  centers the label.
- Removed the unused `M3Converters.PercentToSweepAngle` and `M3Converters.CornerRadiusToDouble` and the
  resource `M3ChevronRightGeometry` (use `M3ChevronRightIconGeometry`).

## 0.2.0 – 2026-10-27

- Drop-down lists and menus open aligned with their field or item again. Avalonia places a popup's content
  without its margin, so the offsets that compensated the shadow margin moved the list of a `ComboBox`
  and the menu of a menu bar 8dp to the left and a submenu 8dp into its parent menu and up. Now: lists 2dp
  below the field, menus 4dp below the item, the first item of a submenu level with its parent item.
- Search bar (`Classes="Search"`) after the M3 Expressive spec: the bar keeps 12dp to each side at rest and
  widens with a spring when tapped (margins 24dp → 12dp at the content edge of a 12dp container). It brings
  its own search icon, which turns into a back button while active (clears the search and leaves it), and a
  clear button that shows while active or while there is text. Pill on Surface container high, 56dp.
- Icon buttons (`Icon`) no longer show a hairline ring: their zero-size elevation shadows are transparent.
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
