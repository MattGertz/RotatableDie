# RotatableDie

A repository containing dice-related projects — a 3D/4D die viewer built with
WPF, and a solitaire Yacht dice game built with .NET MAUI.

## Background

This repository started as an experiment in AI-assisted development. The original
project, the 3D & 4D Die Viewer, was created entirely through collaboration with
GitHub Copilot, with the human developer providing only instructions and guidance.
It served as a way to improve at writing effective Copilot prompts and to explore
the boundaries of what Copilot could accomplish with 3D mathematics, geometry,
and rendering techniques.

The Yacht dice game grew out of that work. Having built all the dice geometry and
rendering infrastructure, it was a natural next step to put those dice to use in
an actual game. Matt's Yacht is a solitaire Yacht (Yahtzee-style) dice game
featuring 3D rendered dice with custom physics, built as a cross-platform .NET
MAUI app targeting both Windows and Android.

Both projects were developed with me writing the spec and the architecture, and with 
GitHub Copilot (Claude Sonnet 3.0 for the die viewer, Claude Open 4.6 for Matt's Yacht) doing
nearly all of the coding.

## Matt's Yacht (`YachtDiceMaui/`)

A solitaire Yacht dice game with 3D rendered dice, custom physics, and sound
effects. Built with .NET 9 MAUI, SkiaSharp, and Plugin.Maui.Audio.

### Features

- **3D Dice**: Real-time 3D rendered dice using SkiaSharp with perspective
  projection, back-face culling, and depth sorting
- **Custom Physics**: Gravity, floor/wall bounce, die-die collision with purely
  lateral separation (no stacking), and settle detection
- **Two Game Modes**: Normal (single scorecard column) and Triple (three columns)
- **Sound Effects**: Dice bounce sounds during rolls, toggle/score/applause audio
- **Dice Customization**: Configurable die color, pip color, number vs. pip
  display, and translucency — all accessible from the Options page
- **High Scores**: Persistent high score tracking for both game modes
- **YACHT! Celebration**: Animated gold text celebration when you roll five of a kind
- **Splash Screen**: Two-layer splash (native + in-app) for a smooth launch experience
- **Landscape Lock**: Optimized for landscape orientation on all devices
- **Cross-Platform**: Runs on Windows 10+ and Android 5.0+

### How to Play

1. Choose Normal or Triple mode from the menu
2. Click **Roll!** to throw the dice (up to 3 rolls per turn)
3. Tap a die to hold/unhold it between rolls
4. Click a score category on the scorecard to record your score
5. Fill all categories to complete the game

### Building

**Windows (debug):**
```
dotnet build -f net9.0-windows10.0.19041.0
dotnet run -f net9.0-windows10.0.19041.0
```

**Android (deploy to emulator/device):**
```
dotnet build -t:Run -f net9.0-android
```

**Windows installer (release):**
```
dotnet publish -f net9.0-windows10.0.19041.0 -c Release
& "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe" Setup.iss
```
The installer is output to `Setup/MattsYachtSetup.exe`.

**Android APK (release):**
```
dotnet publish -f net9.0-android -c Release
```
The signed APK is in `bin/Release/net9.0-android/publish/`.

### Technical Details

- .NET 9 MAUI with SkiaSharp.Views.Maui.Controls for 3D rendering
- Plugin.Maui.Audio for cross-platform sound playback
- Custom physics engine (`SimpleDicePhysics`) with gravity, restitution, friction,
  and angular velocity
- Quaternion-based die rotation with axis-aligned snapping on settle
- Platform-specific code via partial class `PlatformHelpers` (no `#if` blocks in
  shared code)
- Preferences-based persistence for settings and high scores

## 3D & 4D Die Viewer (`RotatableDie.csproj`)

A .NET 9 WPF application for visualizing and interacting with 3D and 4D
polyhedral dice.

### Features

- **Multiple Die Types**: d4, d6, d8, d10, d12, d20, plus 4D polytopes
  (pentachoron, hexadecachoron, tesseract, octaplex)
- **Interactive Rotation**: Left-drag for X/Y rotation, right-drag for Z-axis
  spin, middle-drag and mouse wheel for 4D rotations (XW, YW, ZW planes)
- **Wireframe Mode**: Toggle between solid and wireframe rendering
- **Customizable Colors**: Choose from a wide variety of die colors
- **Realistic Rendering**: Proper face numbering, orientation indicators for 6/9,
  and surface texture effects
- **Random Rotation**: Automatic tumbling with configurable direction changes

### 4D Dice

The 4D dice project the fourth spatial dimension into 3D using transparency-based
depth cues. Each cell in a 4D polytope uses a unique numbering system (Arabic,
Roman, Greek, symbols, etc.) to help track the complex structure as it rotates.
The tesseract (8-cell hypercube) is the showcase piece, with 8 cubic cells each
rendered with a different notation.

### How to Use

1. Select a die type from the dropdown
2. Choose a color
3. Toggle wireframe mode as desired
4. Drag to rotate; use middle-click and scroll wheel for 4D controls

### Requirements

- Windows
- .NET 9 runtime

## An Experiment in AI-Assisted Development

Both projects were created entirely through collaboration with GitHub Copilot,
with the human developer providing only instructions and guidance. Even this
documentation was written by GitHub Copilot based on human instructions. The
projects demonstrated that Copilot can handle complex mathematical domains
(quaternion rotations, polyhedral geometry, 4D projection, custom physics) when
given clear, iterative guidance — though specialized geometries like the d10
(pentagonal trapezohedron) required explicit teaching about the underlying math.

## License

Copyright 2024, 2025, 2026 Matthew W. Gertz

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to use,
copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the
Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR
A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN
ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
- Graphics card with DirectX 10 or later support

## Future Possibilities

- Additional higher-dimensional polytopes (5D and beyond)
- Dice rolling physics simulation
- Multiple dice visualization
- Export/import customized dice
- Texture customization options
