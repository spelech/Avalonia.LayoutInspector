# Avalonia.LayoutInspector

Automated layout auditing, boundary overflow detection, sibling collision checking, touch target ergonomics validation, and responsive viewport inspection for Avalonia UI.

[![NuGet](https://img.shields.io/nuget/v/AvaloniaLayoutInspector.svg)](https://www.nuget.org/packages/AvaloniaLayoutInspector)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

## Overview

`Avalonia.LayoutInspector` provides fast, automated, headless-compatible layout verification for Avalonia applications. It inspects visual trees to find visual defects, overlapping siblings, clipped text, and accessibility hitbox issues before release.

## Key Capabilities

- **Boundary Overflow Detection**: Detects child elements that clip or spill outside container bounds.
- **Sibling Collision Auditing**: Detects overlapping visual siblings across common containers (`Canvas`, `Grid`, `StackPanel`).
- **Touch Target Ergonomics**: Validates interactive hitboxes against accessibility guidelines ($\ge 24\times24$ or $44\times44$ pixels).
- **Text Clipping Detection**: Detects truncated text blocks and missing ellipsis indicators.
- **Responsive Viewport Sweeping**: Tests views across desktop, tablet, and mobile breakpoints in headless test suites.
- **In-App Diagnostic Overlay**: Renders real-time color-coded diagnostic outlines and HUD badges directly over running views.
- **Multi-Target Support**: Targets .NET 8.0, .NET 9.0, and .NET 10.0.

## Installation

Install the package from NuGet:

```bash
dotnet add package AvaloniaLayoutInspector
```

## Quick Start

### Headless Unit Testing

Use headless assertions in xUnit test suites:

```csharp
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LayoutInspector.Assertions;
using Avalonia.LayoutInspector.Responsive;

namespace MyApp.Tests;

public class ViewLayoutTests
{
    [AvaloniaFact]
    public void MainView_ShouldHaveNoLayoutViolations()
    {
        var view = new MainUserControl();
        var window = new Window { Content = view, Width = 800, Height = 600 };
        window.Show();

        window.ShouldHaveNoLayoutViolations();
        window.Close();
    }

    [AvaloniaFact]
    public void MainView_ShouldFitStandardBreakpoints()
    {
        var window = new Window { Content = new MainUserControl() };
        window.ShouldFitResponsiveBreakpoints(StandardBreakpoints.AllStandard);
        window.Close();
    }
}
```

### In-App Diagnostic Overlay

Attach the visual inspector overlay in debug or development builds:

```csharp
#if DEBUG
Avalonia.LayoutInspector.Overlay.LayoutInspectorOverlay.Attach(mainWindow);
#endif
```

## Rules Reference

| Rule ID | Name | Severity | Description |
|---|---|---|---|
| `LAYOUT001_OVERFLOW` | Boundary Overflow | Error | Element extends outside its parent container bounds. |
| `LAYOUT002_COLLISION` | Sibling Collision | Warning | Sibling elements un-intentionally collide or overlap. |
| `LAYOUT003_ERGONOMICS` | Target Ergonomics | Warning | Interactive control has touch/click hitbox below minimum size. |
| `LAYOUT004_CLIPPING` | Text Clipping | Warning | Text element has trimmed or clipped content. |

## License

Licensed under the [MIT License](LICENSE).
