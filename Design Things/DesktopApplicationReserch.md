# Desktop Application Design Research

## 1. UI Frameworks for Avalonia

### FluentAvalonia
- **GitHub**: amwx/FluentAvalonia (1.5k stars)
- WinUI 3 / Windows 11 Fluent Design port for Avalonia
- Controls: NavigationView, TabView, InfoBar, NumberBox, RatingControl, etc.
- Supports acrylic/mica blur backgrounds natively on Windows
- Most "native" feel for Windows users
- Requires Avalonia 11.2+, actively maintained
- NuGet: `FluentAvaloniaUI`

### Material.Avalonia
- **GitHub**: AvaloniaCommunity/Material.Avalonia (1.1k stars)
- Material Design 3 implementation for Avalonia
- Full theming system (light/dark/custom palettes), ripple effects, elevation
- Extra controls: Snackbars, side sheets, floating buttons, cards, dialogs
- Supports runtime theme switching
- NuGet: `Material.Avalonia`

### Avalonia Built-in Themes
- `FluentTheme` — Windows 11-ish, default, lightweight
- `SimpleTheme` — Minimal, good for custom styling
- Both ship with Avalonia, no extra packages

## 2. Color Schemes & Theming

### Catppuccin (what Sabeltann currently uses)
- **Base**: `#1e1e2e` (mantle), `#181825` (crust), `#313244` (surface0)
- **Text**: `#cdd6f4`
- **Subtext**: `#a6adc8`, `#6c7086`
- **Accent (Mauve)**: `#cba6f7`
- **Green**: `#a6e3a1`, **Red**: `#f38ba8`, **Yellow**: `#f9e2af`, **Blue**: `#89b4fa`
- Pros: Eye-friendly dark theme, good contrast, consistent palette
- Cons: Not native Windows, can feel "themed" rather than integrated

### Material Design 3 (Material You)
- Dynamic color from wallpaper (Android), but on desktop you pick a palette
- Color roles: Primary, Secondary, Tertiary, Error, Surface
- Surface variants: Surface, SurfaceVariant, SurfaceContainer, etc.
- Uses tonal palettes (lighter/darker variants of each color)
- Good for: apps that want a modern, consistent cross-platform look

### Windows 11 Fluent Design Colors
- Accent color user-configurable (system setting)
- Mica: transparent/acrylic background that samples desktop wallpaper
- Acrylic: glass-like blur effect
- Uses `SolidColorBrush` with opacity rather than fixed hex values
- Good for: apps that should feel native to Windows 11

### General Color Principles
- Dark UI needs slightly desaturated backgrounds (pure black `#000` causes eye strain)
- Text contrast: body text on dark bg = min 4.5:1 ratio
- Accent colors should cover ~10% of surface area max
- Use color sparingly — reserve for interactive elements and highlights

## 3. Spacing & Layout

### The 4px / 8px Grid
- Base unit: 4px. All spacing in multiples of 4.
- Common values: 4, 8, 12, 16, 20, 24, 32, 40, 48
- Padding inside containers: 16-24px
- Spacing between related elements: 8px
- Spacing between sections: 24-32px

### Layout Best Practices
- Sidebar: 240-320px wide, use splitter for resizability
- Toolbar height: 40-48px
- Status bar height: 24-32px
- Channel/item height: 40-52px (comfortable for mouse)
- Button min touch target: 32px (48px for accessibility)
- Corner radius: 4px (subtle), 8px (modern), 16px (card)

## 4. Typography

- **System font stack**: `Segoe UI Variable` (Win11), `Segoe UI` (Win10), `San Francisco` (macOS)
- Body text: 12-14px
- Small/text: 11px
- Headings: 16-24px
- Use font weight for hierarchy (Regular 400, SemiBold 600, Bold 700)
- Letter-spacing for uppercase labels (1-2px)
- Avoid mixing too many font sizes; 3-4 sizes max

## 5. Animations & Transitions

### Duration Guidelines
- Micro-interactions (hover, focus): 100-150ms
- UI state changes (expand, collapse): 200-300ms
- Page transitions: 300-500ms

### Common Patterns
- Button hover: scale 1.02-1.05 or background tint
- List items: slide 2-4px on hover + background change
- Fade in/out: opacity transitions for overlays
- Smooth scroll: animation duration 200ms

### Implementation in Avalonia
- Use `Transitions` property on controls:
  - `TransformTransition` for `RenderTransform` (position, scale)
  - `DoubleTransition` for `Opacity`, `BorderThickness`
  - `ColorTransition` for `Background`, `Foreground`
- XAML syntax: `<Transitions><TransformTransition Property="RenderTransform" Duration="0:0:0.15"/></Transitions>`

## 6. shadcn/ui and Desktop Equivalents

### What shadcn/ui Is
- Not a component library — it's a collection of copy-pasteable React components
- Built on Radix UI primitives, styled with Tailwind CSS
- Philosophy: clean, minimal, well-spaced, accessible
- Features: consistent color tokens, proper focus rings, smooth transitions

### Closest Desktop Equivalents
- **FluentAvalonia** — most similar in polish level, focused on Windows
- **Material.Avalonia** — most similar in design system rigor
- **Custom Catppuccin styles** — what Sabeltann does — closest in "curated design system" feel

### shadcn Design Principles Applicable to Avalonia
- Every interactive element needs a focus ring / visible state
- Consistent border radius everywhere (use a token, not raw values)
- Proper disabled states (reduced opacity, no hover effects)
- Color as communication (red for destructive, green for success)
- Padding consistency (use spacing scale, not arbitrary values)

## 7. Windows 11 Design Guidelines Summary

- **Mica**: Preferred background material for title bar + main surface
- **Acrylic**: For flyouts, menus, tooltips (temporary surfaces)
- **Rounded corners**: All windows and containers, 4-8px
- **Snap layouts**: Support maximize hover overlay
- **Dark/Light mode**: Support both, follow system setting
- **Typography**: Segoe UI Variable at all sizes
- **Iconography**: Segoe Fluent Icons or custom SVG

## 8. Recommendations for Sabeltann

### Quick wins (what was implemented)
- Consistent 4px spacing grid
- Hover animations on channel cards
- Better visual hierarchy in sidebar
- Darker glass effect on overlays
- Rounded borders (6-8px)

### Next steps if desired
- **FluentAvalonia** for native Win11 feel (acrylic backgrounds, NavigationView)
- Channel logos/colored initials instead of "TV" placeholder
- Smooth category select transition
- Keyboard navigation visual feedback
- Volume slider styling
- Custom title bar (remove system chrome, add acrylic)
