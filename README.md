# Material3Expressive.Avalonia

A [Material 3 Expressive](https://m3.material.io) theme for [Avalonia UI](https://avaloniaui.net) 12. It is built on [Material3.Avalonia](https://github.com/timk1299/avalonia_material_3) (a `Styles`-based theme object, class-based variants, assist attached properties) and adds the Expressive update of the design system:

- **Spring motion.** Every theme animation runs on the M3 spring tokens (`md.sys.motion.spring.*`). The theme has the Expressive and Standard motion schemes, and you can use the same springs in your own transitions and code.
- **Shape.** The expanded corner scale (`LargeIncreased`, `ExtraLargeIncreased`, `ExtraExtraLarge`), all 35 shapes of the Material shape library, and shape morphing.
- **Date and time pickers.** Modal, docked and range date pickers, and the clock-dial time picker with keyboard input.
- **Expressive components.** Button sizes XS–XL with round and square shapes that morph on press, toggle buttons, standard and connected button groups, split buttons, the FAB menu, new FAB sizes, the loading indicator, wavy progress indicators, the new slider, floating and docked toolbars, flexible app bars, an expandable navigation rail, and Expressive lists and menus.
- **Emphasized typography.** Every type-scale style has an emphasized variant.
- **Everything from Material 3.** Dynamic color from a seed (HCT, ported from `material-color-utilities`), all 49 color roles, state layers, ripples, focus rings, light and dark, and a recolored Fluent fallback for controls without an M3 template.

Token values come from Google's Material 3 token set as published in Jetpack Compose Material3 (`androidx.compose.material3.tokens`). Shape definitions are ported from `MaterialShapes` and `androidx.graphics.shapes`.

## Getting started

Reference the project (`src/Material3Expressive.Avalonia`), or the NuGet package attached to a [release](https://github.com/timk1299/Material3Expressive.Avalonia/releases) (put the `.nupkg` into a local package source). Then add the theme to `App.axaml`:

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:m3="https://github.com/timk1299/material3-expressive-avalonia"
             RequestedThemeVariant="Default">
  <Application.Styles>
    <m3:Material3ExpressiveTheme SeedColor="#6750A4" Variant="TonalSpot" MotionScheme="Expressive" />
  </Application.Styles>
</Application>
```

Do **not** add `<FluentTheme />` as well. The theme already includes it as the fallback layer. Set `UseFluentFallback="False"` to drop it.

The theme also sets two app-wide behaviors:

- **No clipped shadows.** Avalonia 12 clips every templated control to its bounds. The theme turns that off for all templated controls (`Resources/Styles.axaml`), so the elevation shadow of a hovered button at the edge of a `UserControl`, `ItemsControl` or tab is not cut off. Scrolled content is still clipped by the `ScrollContentPresenter`. `TabControl` and `NavigationRail` clip their content only when a `PageTransition` is set.
- **No flicker when controls appear.** Bindings and styles of a new or re-shown view settle in several steps, and the template transitions would fade each step. The transitions of a control and its template parts start only once it is attached and settled (`Motion/SettledTransitions.cs`). Hover, press and later state changes animate as usual.

Run the showcase:

```sh
dotnet run --project samples/Material3Expressive.Avalonia.Demo
```

## Motion

Expressive motion uses springs instead of fixed durations and easing curves. There are six tokens per scheme. **Spatial** springs move and resize things and may overshoot. **Effects** springs animate color and opacity and never overshoot.

| Token | Expressive (damping, stiffness) | Standard |
|---|---|---|
| `FastSpatial` | 0.6, 800 | 0.9, 1400 |
| `DefaultSpatial` | 0.8, 380 | 0.9, 700 |
| `SlowSpatial` | 0.8, 200 | 0.9, 300 |
| `FastEffects` / `DefaultEffects` / `SlowEffects` | 1, 3800 / 1600 / 800 | same |

`Material3ExpressiveTheme.MotionScheme` selects the scheme. It applies to animations that start after the change, so set it in XAML.

Use the springs in your own transitions with the two markup extensions (`xmlns:m3="https://github.com/timk1299/material3-expressive-avalonia"`). The duration is the spring's settle time:

```xml
<Border.Transitions>
  <Transitions>
    <CornerRadiusTransition Property="CornerRadius"
                            Duration="{m3:SpringDuration FastSpatial}"
                            Easing="{m3:Spring FastSpatial}" />
  </Transitions>
</Border.Transitions>
```

In code, `SpringAnimator` is a physics-based value. It keeps its velocity when the target changes mid-flight, so interrupted motion stays continuous. `FrameTicker` drives it from the window's animation frames:

```csharp
var x = new SpringAnimator(SpringToken.DefaultSpatial);
var ticker = new FrameTicker(control, dt => { var running = x.Step(dt); Apply(x.Value); return running; });
x.AnimateTo(1);
ticker.Start();
```

`M3Motion.Get(token)` returns the `SpringSpec` (damping ratio and stiffness) of the active scheme. Set `M3Motion.ReduceMotion = true` to make all theme animations jump to their end state.

## Shapes

`MaterialShape` lists the 35 shapes of the Expressive shape library: `Circle`, `Square`, `Slanted`, `Arch`, `Fan`, `Arrow`, `SemiCircle`, `Oval`, `Pill`, `Triangle`, `Diamond`, `ClamShell`, `Pentagon`, `Gem`, `Sunny`, `VerySunny`, `Cookie4Sided`, `Cookie6Sided`, `Cookie7Sided`, `Cookie9Sided`, `Cookie12Sided`, `Ghostish`, `Clover4Leaf`, `Clover8Leaf`, `Burst`, `SoftBurst`, `Boom`, `SoftBoom`, `Flower`, `Puffy`, `PuffyDiamond`, `PixelCircle`, `PixelTriangle`, `Bun` and `Heart`.

`m3:ShapeBox` fills and clips its child to a shape. Changing `Shape` morphs the outline with a spring:

```xml
<m3:ShapeBox Shape="Cookie9Sided" Width="96" Height="96" Background="{DynamicResource M3PrimaryContainerBrush}">
  <Image Source="avatar.png" Stretch="UniformToFill" />
</m3:ShapeBox>
```

For your own drawing, `MaterialShapes.CreateGeometry(shape, bounds)` returns the exact outline and `ShapeMorph.CreateGeometry(from, to, progress, bounds, rotation)` returns an in-between shape. `RoundedPolygon` builds new shapes from vertices with per-vertex `CornerRounding`.

The corner scale is exposed as `CornerRadius` resources: `M3ShapeNone`, `M3ShapeExtraSmall` (4), `M3ShapeSmall` (8), `M3ShapeMedium` (12), `M3ShapeLarge` (16), `M3ShapeLargeIncreased` (20), `M3ShapeExtraLarge` (28), `M3ShapeExtraLargeIncreased` (32), `M3ShapeExtraExtraLarge` (48), `M3ShapeFull`, plus the `*Top`, `*Start` and `*End` variants.

## Color

| Property | Description |
|---|---|
| `SeedColor` | Key color that both schemes are generated from (default `#6750A4`). |
| `Variant` | `TonalSpot` (default, Android's dynamic color), `Baseline` (seed `#6750A4` reproduces the M3 baseline purple), `Neutral`, `Vibrant`, `Expressive`, `Monochrome`. |
| `LightScheme` / `DarkScheme` | Explicit `Material3ColorScheme` objects, e.g. exported from Material Theme Builder. Each overrides the generated scheme for its variant. |

Every role is exposed as a `Color` and a brush resource:

```xml
<Border Background="{DynamicResource M3SurfaceContainerHighBrush}">
  <TextBlock Foreground="{DynamicResource M3OnSurfaceBrush}" />
</Border>
```

Resource keys are `M3{Role}Color` and `M3{Role}Brush`, where `Role` is any `Material3ColorScheme` property: `Primary`, `OnPrimary`, `PrimaryContainer`, …, `SurfaceDim`, `SurfaceContainerLowest` … `SurfaceContainerHighest`, `Outline`, `OutlineVariant`, `InverseSurface`, the `*Fixed` roles, and so on. Pre-blended helpers are also provided: `M3DisabledContentBrush` (on-surface 38%), `M3DisabledContainerBrush` (on-surface 12%) and `M3ScrimOverlayBrush`.

Changing colors at runtime:

```csharp
var theme = Material3ExpressiveTheme.Find()!;
theme.SeedColor = Color.Parse("#0061A4");
theme.Variant = SchemeVariant.Vibrant;

// Or build the schemes yourself:
var light = Material3ColorScheme.FromSeed(seed, isDark: false);
light.Error = Color.Parse("#B3261E");   // override any single role
theme.LightScheme = light;
```

`Hct` and `TonalPalette` are public if you need extra tones (`TonalPalette.FromColor(c).Tone(95)`).

## Typography

`TextBlock` classes follow the M3 type scale: `DisplayLarge|Medium|Small`, `HeadlineLarge|Medium|Small`, `TitleLarge|Medium|Small`, `BodyLarge|Medium|Small`, `LabelLarge|Medium|Small`.

Each style has an **emphasized** variant. It uses the same metrics with a heavier weight (Medium, or Bold for titles medium/small and labels). Write it as `Classes="TitleMediumEmphasized"` or `Classes="TitleMedium Emphasized"`.

Color helper classes: `Primary`, `Secondary`, `Tertiary`, `Error`, `OnSurface`, `OnSurfaceVariant`.

```xml
<TextBlock Classes="HeadlineSmallEmphasized" Text="Settings" />
<TextBlock Classes="BodyMedium OnSurfaceVariant" Text="Supporting text" />
```

## Components

Variants, sizes and shapes are set with `Classes`.

### Buttons

| Control | Classes |
|---|---|
| `Button` | Color: Filled (default), `Tonal`, `Elevated`, `Outlined`, `Text`. Size: `XSmall` (32), `Small` (40, default), `Medium` (56), `Large` (96), `XLarge` (136). Shape: round (default) or `Square`. |
| `ToggleButton` | The same colors, sizes and shapes as `Button`. Checking it changes the color and morphs round to square (a `Square` one becomes round). |
| Icon buttons | `Icon`, `FilledIcon`, `TonalIcon`, `OutlinedIcon` on `Button` or `ToggleButton`, combined with a size, `Narrow` or `Wide`, and `Square`. |
| FABs | `Fab` (56), `MediumFab` (80), `LargeFab` (96), `SmallFab` (40, deprecated), `ExtendedFab` (+ `Medium`, `Large`). Color: primary container (default), `Secondary`, `Tertiary`, `Surface`, and `Filled` for the strong color. Add `Lowered` for less elevation. |
| Chips | `AssistChip`, `SuggestionChip`, `InputChip` (Button) and `FilterChip` (ToggleButton). Add `Elevated` for the elevated versions. |
| `SplitButton` | Avalonia's split button: a leading action and a trailing menu button. Colors `Tonal`, `Elevated`, `Outlined`; sizes `XSmall` to `XLarge`. The inner corners grow on hover, and while the flyout is open the menu button becomes a pill and its chevron turns. |

Pressing a button morphs its corners to a tighter radius on the fast spatial spring. A quick tap still shows the morph, because the theme keeps the `:pressing` pseudo-class for at least 110 ms.

```xml
<Button Classes="Medium Square">
  <StackPanel Orientation="Horizontal" Spacing="8">
    <PathIcon Data="{StaticResource SendIcon}" />
    <TextBlock Text="Send" />
  </StackPanel>
</Button>

<ToggleButton Classes="FilledIcon Large Wide"><PathIcon Data="{StaticResource PlayIcon}" /></ToggleButton>

<SplitButton Classes="Tonal" Content="Save">
  <SplitButton.Flyout>
    <MenuFlyout><MenuItem Header="Save as…" /></MenuFlyout>
  </SplitButton.Flyout>
</SplitButton>
```

### Button groups

`m3:ButtonGroup` lays out buttons in a row. When a button is pressed it widens by `ExpandedRatio` (15%) and its neighbors make room, driven by a spring.

- **Standard** (default): buttons sit 18, 12 or 8 px apart depending on their size, and each keeps its own shape.
- **Connected** (`IsConnected="True"`): buttons sit 2 px apart and share one shape. Outer corners are fully round and inner corners are 8 px. Inner corners tighten to 4 px while pressed, and a checked button rounds out fully. This replaces the segmented button; the old `Segmented` toggle-button classes still work.
- `SelectionMode`: `None` (independent toggles), `Single` or `SingleRequired`, with a two-way `SelectedIndex`. `FillWidth="True"` shares the available width equally.

```xml
<m3:ButtonGroup IsConnected="True" SelectionMode="SingleRequired" SelectedIndex="{Binding Range}">
  <ToggleButton Content="Day" />
  <ToggleButton Content="Week" />
  <ToggleButton Content="Month" />
</m3:ButtonGroup>
```

### FAB menu

`m3:FabMenu` is a toggle FAB that opens a stack of `m3:FabMenuItem`s above itself. When it opens, the FAB morphs into a round close button and the items spring in one after another. Classes set the color (`Secondary`, `Tertiary`) and the size of the closed FAB (`Medium`, `Large`). Clicking an item or pressing Escape closes the menu.

```xml
<m3:FabMenu Classes="Medium" HorizontalAlignment="Right" VerticalAlignment="Bottom" Margin="16">
  <m3:FabMenu.Icon><PathIcon Data="{StaticResource EditIcon}" /></m3:FabMenu.Icon>
  <m3:FabMenuItem Content="Reply" Click="OnReply">
    <m3:FabMenuItem.Icon><PathIcon Data="{StaticResource ReplyIcon}" /></m3:FabMenuItem.Icon>
  </m3:FabMenuItem>
</m3:FabMenu>
```

### Feedback

| Control | Notes |
|---|---|
| `m3:LoadingIndicator` | Morphs through the shape library while rotating, with Compose's shape sequence and timing. `Contained` class for the primary-container version. `Progress` (0–1) for the determinate version. `Shapes` sets a custom shape sequence. |
| `ProgressBar` | The secondary-container track sits 4 px away from the active indicator, with a stop dot at its end. Add `Wavy` for the wave, `Thick` for an 8 px stroke, and `Circular` for the circular indicator. Determinate changes are smoothed with a spring, and the wave flattens near 0% and 100%. |
| `m3:Dialog`, `m3:Snackbar`, `m3:Badge`, `ToolTip` (`Rich`) | Same as Material 3. Dialogs and snackbars now enter with a spring. |

### Inputs and selection

| Control | Notes |
|---|---|
| `Slider` | Expressive slider: a thick track (primary and secondary container), a 4 px bar handle with 6 px gaps that narrows while dragged, and a stop dot. Sizes: `XSmall` (default, 16 px track), `Small`, `Medium`, `Large`, `XLarge` (96 px), horizontal or vertical. `ValueLabel` shows the value; `TickPlacement` + `TickFrequency` draw stop indicators. |
| `TextBox`, `ComboBox` | Filled (default) or `Outlined` M3 text fields with `TextFieldAssist.Label` and `SupportingText`. `Search` for a search bar. |
| `CheckBox`, `RadioButton`, `ToggleSwitch` | M3 selection controls; the switch handle moves on a spring. |

### Date and time pickers

Avalonia already uses the names `DatePicker`, `TimePicker` and `Calendar` for its Fluent spinner controls (those still render in the recolored Fluent style). The Material 3 pickers are therefore separate controls:

| Control | Notes |
|---|---|
| `DatePickerDialog.ShowAsync` / `ShowRangeAsync` | The modal date picker and date range picker: a header showing the selected date or range, the calendar, and a toggle to keyboard input. Returns the chosen date, `(Start, End)`, or `null` when dismissed. Options: `minDate`, `maxDate`, a `dateFilter` (e.g. weekdays only), the start mode, a title and button texts. |
| `m3:DateField` | The docked date picker: a filled (or `Outlined`) text field that accepts typed dates and has a calendar button that opens the calendar below it. Two-way `SelectedDate`, `MinDate`, `MaxDate`, `DateFormat`, `Label`. |
| `m3:CalendarView` | The calendar on its own: month header (click it for the year grid), arrows, weekday row and 40 px day cells. Single or range selection (`SelectionMode`), today outline, `MinDate`/`MaxDate`/`DateFilter`, `FirstDayOfWeek`, and keyboard navigation (arrows, Page Up/Down, Home/End, Enter). Months slide in on a spring. |
| `m3:DatePickerPanel` | The content of the modal picker (header, calendar or input fields) for embedding in your own layout. |
| `TimePickerDialog.ShowAsync` | The modal time picker. The hour and minute selectors sit above a 256 px clock dial, with an AM/PM selector on 12-hour clocks; in 24-hour mode the hours 12–23 are on an inner ring. After you pick the hour it switches to minutes. Tapping a minute snaps to 5 minutes, dragging selects any minute, and the hand springs to the value. Options: `is24Hour` (default from the culture), keyboard `displayMode`, and `orientation: Horizontal` for the landscape layout. Returns a `TimeSpan` or `null`. |
| `m3:TimeField` | A text field for a time ("14:30", "2:30 PM", "1430" …) with a clock button that opens the time picker. |
| `m3:TimePickerPanel`, `m3:ClockDial`, `m3:PeriodSelector` | The time picker's content and parts for custom layouts. |

```csharp
DateTime? day = await DatePickerDialog.ShowAsync(this, DateTime.Today, minDate: DateTime.Today);
var trip = await DatePickerDialog.ShowRangeAsync(this);              // (DateTime Start, DateTime End)?
TimeSpan? alarm = await TimePickerDialog.ShowAsync(this, new TimeSpan(7, 30, 0));
```

```xml
<m3:DateField Label="Date of birth" SelectedDate="{Binding Birthday}" />
<m3:TimeField Classes="Outlined" Label="Alarm" SelectedTime="{Binding Alarm}" />
```

Dates are typed in the culture's short date pattern with two-digit day and month and a four-digit year (`DateInputFormat.Default`, e.g. `mm/dd/yyyy`), and weekday letters, month names and AM/PM come from the current culture. The built-in texts ("Select date", "Cancel", …) are English; pass `title`, `confirmText` and `dismissText` to localize the dialogs.

### Navigation and containers

| Control | Notes |
|---|---|
| `m3:NavigationRail` | Expressive rail, built on `TabControl` (destinations are `TabItem`s with `Icon` and `Header`). Collapsed it is 96 px wide with labels under the icons. Expanded (`IsExpanded`, or the built-in menu button) it is `ExpandedWidth` wide (240) with labels beside the icons in 56 px pills. The width animates with a spring. `Header` holds content such as a FAB. |
| `TabControl` | Primary tabs (default), `Secondary`, `Scrollable`, `NavigationRail` (classic rail), `NavigationBar` (64 px Expressive bar; add `Horizontal` for side-by-side icon and label). |
| `m3:FloatingToolbar` | A 64 px pill with elevation 3, standard or `Vibrant` (primary container), horizontal or `Orientation="Vertical"`, and an optional `FloatingActionButton` beside it. |
| `m3:DockedToolbar` | A full-width 64 px bar with its actions spread evenly (`Vibrant` available). Replaces the bottom app bar. |
| `m3:TopAppBar` | Small (default), `CenterAligned`, `Medium` and `Large` (the flexible app bars: HeadlineMedium / DisplaySmall titles), and an optional `Subtitle`. `IsScrolled` or `Scrolled` switches to the scrolled color. |
| `ListBox` | M3 list; `Expressive` for a segmented Expressive list (rounded items 2 px apart whose corners grow on hover, press and selection); `NavigationDrawer` for drawer destinations. |
| `Menu`, `ContextMenu`, `MenuFlyout` | Expressive menus: 16 px container corners, 44 px items, and checked items shown as selected (tertiary container). `FlyoutPresenterClasses="Vibrant"` on a `MenuFlyout`, or `Classes="Vibrant"` on a `ContextMenu`, gives the vibrant (tertiary) menu. |
| `GridSplitter` | A transparent grab area with a 2 px divider line (outline variant); `Handle` shows the Material 3 drag handle (4 × 48 pill) instead. Hover and dragging color it primary. Set `ResizeDirection="Rows"` for horizontal splitters. |
| `m3:Card`, `m3:BottomAppBar`, `Separator`, `SplitView`, `Window` | Same as Material 3. |

Text fields use `TextFieldAssist`:

```xml
<TextBox Classes="Outlined"
         m3:TextFieldAssist.Label="Email"
         m3:TextFieldAssist.SupportingText="We'll never share it"
         PlaceholderText="name@example.com" />
```

Dialogs and snackbars need no host control. They appear in the window's overlay layer:

```csharp
bool discard = await Dialog.ShowMessageAsync(this, "Discard draft?", "This cannot be undone.", "Discard", "Cancel");
Snackbar.Show(this, "Conversation archived", "Undo", () => Restore());
```

A single-line `TextBox` (`AcceptsReturn="False"`) does not scroll vertically, so a vertical swipe over it scrolls the list behind it.

Scroll bars that should stay visible: set `AllowAutoHide="False"` on the `ScrollViewer`, or `ScrollViewer.AllowAutoHide="False"` on a `ListBox` or `TextBox`. The content then gets its own space beside the bars. Setting it on the `ScrollBar` instead keeps the bars visible but leaves them over the content.

Icons: `PathIcon` stretches its geometry's bounds. For 24 px Material icon paths, add `M0 0h.01M24 24h-.01` to the path so the icon keeps its built-in padding (the demo does this), or use an icon package such as Material.Icons.Avalonia. Icon sizes inside buttons follow the button size (20, 24, 32 or 40 px).

## Project layout

```
src/Material3Expressive.Avalonia/
  ColorSystem/      HCT, CAM16, tonal palettes, scheme generation
  Motion/           Spring tokens and physics, M3SpringEasing, {m3:Spring}/{m3:SpringDuration}, SpringAnimator
  Shapes/           RoundedPolygon (androidx.graphics.shapes port), MaterialShapes, ShapeMorph
  Controls/         ButtonGroup, FabMenu, LoadingIndicator, progress presenters, ShapeBox, toolbars,
                    NavigationRail, CalendarView, date and time pickers, Ripple, Card, TopAppBar,
                    Dialog, Snackbar, Badge, …
  Resources/        Design tokens and global styles (type scale, icon sizing)
  Themes/           ControlThemes per component (Expressive.axaml: new Expressive controls, Pickers.axaml: date/time pickers)
  Material3ExpressiveTheme.*   The theme entry point
samples/Material3Expressive.Avalonia.Demo/   Showcase app
tests/Material3Expressive.Avalonia.Tests/    Color, motion and shape tests, headless interaction and rendering tests
```

`dotnet test` covers several areas. It checks generated colors against the official M3 baseline tokens, the spring tokens and physics, and the shape library. It exercises the Expressive interactions headlessly: press morph, button-group expansion, connected-group selection, split button, FAB menu and navigation rail. It also renders every showcase page in light and dark. Set `M3_SCREENSHOT_DIR` to keep the PNGs.

## Limitations

- Shape morphing interpolates outlines in polar coordinates around the center, not with androidx's feature matching. It is exact for the star-shaped outlines of the shape library, but rotation-heavy morphs can look slightly different from Compose.
- Shapes are normalized to their exact outline, while androidx uses control-point bounds. They fill their box about 1–2% more than in Compose.
- `Vibrant` and `Expressive` color variants use the hue-rotation tables from material-color-utilities with the standard tone mapping. They are close to MCU output but not identical, and the 2025 dynamic-color spec is not implemented.
- Not implemented yet: the search app bar and search view, carousel, bottom and side sheets, the modal expanded navigation rail, range and centered sliders, inset slider icons, icons inside switch handles, and text-field prefix, suffix and character counter.
- In an outlined text field with a leading icon, the floating label stays aligned with the text instead of moving over the icon.
- Roboto is bundled; the Expressive brand typeface (Google Sans Flex) is not.
- `MotionScheme` changes apply to animations created after the change.
- Writing your own templates: the theme turns off clipping for templated controls, but plain panels and borders inside a template still clip when they set `ClipToBounds="True"`. Keep shadows, focus rings and state layers inside an element that does not clip.

## Credits

Roboto is bundled under the Apache License 2.0 (`Assets/Fonts/Roboto-LICENSE.txt`). The color algorithms are ported from Google's [material-color-utilities](https://github.com/material-foundation/material-color-utilities) (Apache 2.0). The shape library and polygon rounding are ported from AndroidX `compose.material3.MaterialShapes` and `graphics.shapes` (Apache 2.0, The Android Open Source Project), and component and motion token values come from the Material 3 tokens published in AndroidX Compose Material3. Icon paths are from Google's Material Icons (Apache 2.0). See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
