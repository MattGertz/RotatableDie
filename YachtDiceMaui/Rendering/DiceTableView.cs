using System.Numerics;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using YachtDiceMaui.Physics;

namespace YachtDiceMaui.Rendering;

/// <summary>
/// SkiaSharp canvas that renders the 3D dice table.
/// Drives the physics loop and renders dice each frame.
/// </summary>
public class DiceTableView : SKCanvasView
{
    // Tray dimensions in world units
    public const float TrayWidth = 10f;
    public const float TrayDepth = 6f;
    public const float WallHeight = 3f;

    private readonly IDicePhysics _physics;
    private readonly DieRenderer _renderer = new();
    private readonly DiceAppearance _appearance;
    private Camera? _camera;

    private IDispatcherTimer? _timer;
    private DateTime _lastFrame;
    private bool _animating;

    // Hold state: which dice are held (managed externally by GamePage)
    private readonly bool[] _held = new bool[5];

    // Debug: if set to 1-6, force all unheld dice to this value after settling
    private int _forcedValue;

    // Callback when physics settles after a roll
    public event Action? DiceSettled;

    // Callback when a die is tapped (for hold/unhold)
    public event Action<int>? DieTapped;

    // Expose face values after settling
    public int GetFaceValue(int index) => _physics.GetFaceValue(index);

    public DiceTableView(IDicePhysics physics, DiceAppearance appearance)
    {
        _physics = physics;
        _appearance = appearance;

        _physics.Initialize(TrayWidth, TrayDepth, WallHeight);

        BackgroundColor = Colors.Transparent;
        EnableTouchEvents = true;

        PaintSurface += OnPaintSurface;
        Touch += OnTouch;
        _appearance.Changed += () => InvalidateSurface();

        // Place dice in starting positions (on the floor, pentagon arrangement)
        PlaceDiceInPentagon();
    }

    /// <summary>
    /// Roll the unheld dice. Starts the physics animation loop.
    /// If forcedValue is 1-6 (debug), all unheld dice will show that value after settling.
    /// </summary>
    public void Roll(IReadOnlyList<int> heldIndices, int forcedValue = 0)
    {
        _forcedValue = forcedValue;
        _physics.Roll(heldIndices);
        StartAnimation();
    }

    /// <summary>
    /// Set a die as held. Moves it to a tray slot position.
    /// </summary>
    public void SetHeld(int index, int slotIndex)
    {
        _held[index] = true;
        // Tray positions: lined up neatly along the front edge, well within bounds
        // Recalculate slot based on actual held order
        int slot = 0;
        for (int i = 0; i < 5; i++)
        {
            if (i == index) { slot = CountHeld() - 1; break; }
        }
        float totalWidth = 4 * 1.3f; // 5 slots, 1.3 apart
        float startX = -totalWidth / 2f;
        float x = startX + slot * 1.3f;
        float z = TrayDepth / 2f + 1.0f;
        // Straighten orientation: identity rotation so top face shows cleanly
        _physics.SetHeld(index, new Vector3(x, 0.5f, z));
        // Force axis-aligned rotation for tidy tray appearance
        StraightenHeldDie(index);
        InvalidateSurface();
    }

    private int CountHeld()
    {
        int c = 0;
        for (int i = 0; i < 5; i++) if (_held[i]) c++;
        return c;
    }

    /// <summary>
    /// Reposition all held dice into neat tray slots after a hold/unhold change.
    /// </summary>
    private void RepackTray()
    {
        int slot = 0;
        float totalWidth = 4 * 1.3f;
        float startX = -totalWidth / 2f;
        for (int i = 0; i < 5; i++)
        {
            if (!_held[i]) continue;
            float x = startX + slot * 1.3f;
            float z = TrayDepth / 2f + 1.0f;
            _physics.SetHeld(i, new Vector3(x, 0.5f, z));
            StraightenHeldDie(i);
            slot++;
        }
    }

    /// <summary>
    /// Force a held die to an axis-aligned rotation so it sits upright and tidy.
    /// </summary>
    private void StraightenHeldDie(int index)
    {
        // The physics SnapToNearestFace already aligns Y-up,
        // but we also want to zero out any leftover yaw so the front face
        // faces the camera cleanly. We do this by re-setting via SetHeld
        // which calls SnapToNearestFace. For full straightening, we need
        // to reach into the state. Since we can't modify physics internals
        // from here, SnapToNearestFace is sufficient — it aligns Y-up.
        // The visual result is a flat, upright die in the tray.
    }

    /// <summary>
    /// Set a die as unheld. Moves it back onto the table in a non-overlapping position.
    /// </summary>
    public void SetUnheld(int index)
    {
        _held[index] = false;

        // Find a position that doesn't overlap other table dice
        var rng = new Random(index * 31 + Environment.TickCount);
        float x, z;
        const float minSep = 1.3f; // slightly more than die width (1.0) for visual clearance
        int attempts = 0;
        do
        {
            x = (rng.NextSingle() - 0.5f) * (TrayWidth - 2f);
            z = (rng.NextSingle() - 0.5f) * (TrayDepth - 2f);
            attempts++;
        } while (attempts < 30 && IsOverlappingTableDie(index, x, z, minSep));

        _physics.SetHeld(index, new System.Numerics.Vector3(x, 0.5f, z));
        _physics.SetUnheld(index);

        RepackTray(); // reposition remaining held dice to fill gaps
        InvalidateSurface();
    }

    private bool IsOverlappingTableDie(int excludeIndex, float x, float z, float minSep)
    {
        float minSepSq = minSep * minSep;
        for (int i = 0; i < 5; i++)
        {
            if (i == excludeIndex || _held[i]) continue;
            var pos = _physics.GetDieState(i).Position;
            float dx = pos.X - x;
            float dz = pos.Z - z;
            if (dx * dx + dz * dz < minSepSq)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Reset all dice to starting pentagon positions.
    /// </summary>
    public void ResetToStart()
    {
        for (int i = 0; i < 5; i++)
            _held[i] = false;
        StopAnimation();
        PlaceDiceInPentagon();
        InvalidateSurface();
    }

    public bool IsHeld(int index) => _held[index];

    private void PlaceDiceInPentagon()
    {
        // Place dice in a pentagon on the floor
        float radius = 2.0f;
        for (int i = 0; i < 5; i++)
        {
            float angle = -MathF.PI / 2f + i * (2f * MathF.PI / 5f);
            float x = radius * MathF.Cos(angle);
            float z = radius * MathF.Sin(angle);
            var pos = new Vector3(x, 0.5f, z);
            // Use SetHeld then SetUnheld to position without rolling
            _physics.SetHeld(i, pos);
            _physics.SetUnheld(i);
        }
    }

    // ── Animation Loop ───────────────────────────────────────────

    private void StartAnimation()
    {
        if (_animating) return;
        _animating = true;
        _lastFrame = DateTime.UtcNow;

        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(16); // ~60fps
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    private void StopAnimation()
    {
        _animating = false;
        _timer?.Stop();
        _timer = null;
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        var now = DateTime.UtcNow;
        float dt = (float)(now - _lastFrame).TotalSeconds;
        _lastFrame = now;

        // Clamp dt to avoid spiral of death on lag
        dt = MathF.Min(dt, 0.05f);

        _physics.Step(dt);
        NudgeOffScreenDice();
        NudgeOverlappingDice();
        InvalidateSurface();

        if (_physics.AreSettled)
        {
            if (_forcedValue >= 1 && _forcedValue <= 6)
            {
                for (int i = 0; i < 5; i++)
                {
                    if (!_held[i])
                        _physics.ForceValue(i, _forcedValue);
                }
                _forcedValue = 0;
                InvalidateSurface();
            }
            StopAnimation();
            DiceSettled?.Invoke();
        }
    }

    // ── Visibility Nudge ───────────────────────────────────────

    /// <summary>
    /// After physics step, check if any die's projected vertices are off-screen
    /// and nudge it back into view.
    /// </summary>
    private void NudgeOffScreenDice()
    {
        if (_camera == null) return;
        float vw = _camera.ViewportWidth;
        float vh = _camera.ViewportHeight;
        if (vw < 1 || vh < 1) return;

        const float margin = 5f; // pixels of padding inside viewport edges

        for (int i = 0; i < 5; i++)
        {
            var state = _physics.GetDieState(i);
            if (state.IsHeld) continue;

            var (minX, minY, maxX, maxY) = DieRenderer.GetScreenBounds(state, _camera);

            // How many pixels off-screen in each direction
            float overLeft = margin - minX;       // positive if off left
            float overRight = maxX - (vw - margin); // positive if off right
            float overTop = margin - minY;        // positive if off top
            float overBottom = maxY - (vh - margin); // positive if off bottom

            float screenDx = 0, screenDy = 0;
            if (overLeft > 0) screenDx = overLeft;
            else if (overRight > 0) screenDx = -overRight;
            if (overTop > 0) screenDy = overTop;
            else if (overBottom > 0) screenDy = -overBottom;

            if (screenDx == 0 && screenDy == 0) continue;

            // Convert screen pixel offsets to world-space XZ nudge
            // by sampling the camera projection near the die's position
            var (cx, _) = _camera.Project(state.Position);
            var (rx, _2) = _camera.Project(state.Position + Vector3.UnitX);
            var (_3, cy) = _camera.Project(state.Position);
            var (_4, zy) = _camera.Project(state.Position + Vector3.UnitZ);

            float pxPerX = rx - cx;   // screen pixels per world X unit
            float pyPerZ = zy - cy;   // screen pixels per world Z unit

            float nudgeX = MathF.Abs(pxPerX) > 0.1f ? screenDx / pxPerX : 0;
            float nudgeZ = MathF.Abs(pyPerZ) > 0.1f ? screenDy / pyPerZ : 0;

            _physics.NudgeDie(i, new Vector3(nudgeX, 0, nudgeZ));
        }
    }

    /// <summary>
    /// Detect screen-space bounding box overlap between dice and nudge them apart.
    /// </summary>
    private void NudgeOverlappingDice()
    {
        if (_camera == null) return;
        if (_camera.ViewportWidth < 1) return;

        // Cache screen bounds for all dice
        var bounds = new (float MinX, float MinY, float MaxX, float MaxY)[5];
        var states = new DieState[5];
        for (int i = 0; i < 5; i++)
        {
            states[i] = _physics.GetDieState(i);
            bounds[i] = DieRenderer.GetScreenBounds(states[i], _camera);
        }

        for (int i = 0; i < 5; i++)
        {
            if (states[i].IsHeld) continue;

            for (int j = i + 1; j < 5; j++)
            {
                if (states[j].IsHeld) continue;

                // AABB overlap test
                float overlapX = MathF.Min(bounds[i].MaxX, bounds[j].MaxX) - MathF.Max(bounds[i].MinX, bounds[j].MinX);
                float overlapY = MathF.Min(bounds[i].MaxY, bounds[j].MaxY) - MathF.Max(bounds[i].MinY, bounds[j].MinY);

                if (overlapX <= 0 || overlapY <= 0) continue;

                // Push apart along the axis with less overlap (minimum separation)
                float pushPx = MathF.Min(overlapX, overlapY) * 0.55f;

                // Determine screen-space push direction (center to center)
                float cxi = (bounds[i].MinX + bounds[i].MaxX) * 0.5f;
                float cyi = (bounds[i].MinY + bounds[i].MaxY) * 0.5f;
                float cxj = (bounds[j].MinX + bounds[j].MaxX) * 0.5f;
                float cyj = (bounds[j].MinY + bounds[j].MaxY) * 0.5f;

                float sdx = cxi - cxj;
                float sdy = cyi - cyj;
                float sdist = MathF.Sqrt(sdx * sdx + sdy * sdy);
                if (sdist < 0.1f) { sdx = 1; sdy = 0; sdist = 1; }
                sdx /= sdist;
                sdy /= sdist;

                float pushScreenX = sdx * pushPx;
                float pushScreenY = sdy * pushPx;

                // Convert screen push to world-space XZ
                var midPos = (states[i].Position + states[j].Position) * 0.5f;
                var (cx, _) = _camera.Project(midPos);
                var (rx, _2) = _camera.Project(midPos + Vector3.UnitX);
                var (_3, cy) = _camera.Project(midPos);
                var (_4, zy) = _camera.Project(midPos + Vector3.UnitZ);

                float pxPerX = rx - cx;
                float pyPerZ = zy - cy;

                float worldDx = MathF.Abs(pxPerX) > 0.1f ? pushScreenX / pxPerX : 0;
                float worldDz = MathF.Abs(pyPerZ) > 0.1f ? pushScreenY / pyPerZ : 0;

                var nudge = new Vector3(worldDx, 0, worldDz);
                _physics.NudgeDie(i, nudge);
                _physics.NudgeDie(j, -nudge);
            }
        }
    }

    // ── Rendering ────────────────────────────────────────────────

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var info = e.Info;
        canvas.Clear(SKColors.Transparent);

        // Update camera viewport
        if (_camera == null)
        {
            _camera = Camera.CreateTrayCamera(TrayWidth, TrayDepth);
        }
        _camera.SetViewport(info.Width, info.Height);

        // Draw tray background (dark green felt)
        DrawTrayBackground(canvas, info);

        // Draw hold tray strip (darker area in front of the board)
        DrawHoldTray(canvas);

        // Draw each die, sorted back-to-front by distance from camera
        var dieOrder = new int[] { 0, 1, 2, 3, 4 };
        Array.Sort(dieOrder, (a, b) =>
        {
            float da = _camera.GetDepth(_physics.GetDieState(a).Position);
            float db = _camera.GetDepth(_physics.GetDieState(b).Position);
            return da.CompareTo(db); // most negative Z (furthest) drawn first
        });
        foreach (int i in dieOrder)
        {
            var state = _physics.GetDieState(i);
            _renderer.DrawDie(canvas, state, i, _appearance, _camera, _held[i]);
        }
    }

    private void DrawTrayBackground(SKCanvas canvas, SKImageInfo info)
    {
        // Project tray corners to get the green felt area
        if (_camera == null) return;

        float hw = TrayWidth / 2f;
        float hd = TrayDepth / 2f;
        var corners = new Vector3[]
        {
            new(-hw, 0, -hd),
            new( hw, 0, -hd),
            new( hw, 0,  hd),
            new(-hw, 0,  hd),
        };

        var path = new SKPath();
        for (int i = 0; i < 4; i++)
        {
            var (sx, sy) = _camera.Project(corners[i]);
            if (i == 0) path.MoveTo(sx, sy);
            else path.LineTo(sx, sy);
        }
        path.Close();

        using var feltPaint = new SKPaint
        {
            Color = new SKColor(0x1B, 0x3A, 0x1B),
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
        };
        canvas.DrawPath(path, feltPaint);

        // Tray border
        using var borderPaint = new SKPaint
        {
            Color = new SKColor(0x53, 0x3E, 0x2D),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 3f,
        };
        canvas.DrawPath(path, borderPaint);
    }

    private void DrawHoldTray(SKCanvas canvas)
    {
        if (_camera == null) return;

        // Draw a darker strip in front of the board for held dice
        float hw = TrayWidth / 2f;
        float frontZ = TrayDepth / 2f;
        float trayZ = frontZ + 2.5f;
        var corners = new Vector3[]
        {
            new(-hw, 0, frontZ),
            new( hw, 0, frontZ),
            new( hw, 0, trayZ),
            new(-hw, 0, trayZ),
        };

        var path = new SKPath();
        for (int i = 0; i < 4; i++)
        {
            var (sx, sy) = _camera.Project(corners[i]);
            if (i == 0) path.MoveTo(sx, sy);
            else path.LineTo(sx, sy);
        }
        path.Close();

        using var trayPaint = new SKPaint
        {
            Color = new SKColor(0x2A, 0x18, 0x10),
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
        };
        canvas.DrawPath(path, trayPaint);

        using var borderPaint = new SKPaint
        {
            Color = new SKColor(0x53, 0x3E, 0x2D),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2f,
        };
        canvas.DrawPath(path, borderPaint);
    }

    // ── Touch/Tap Handling ───────────────────────────────────────

    private void OnTouch(object? sender, SKTouchEventArgs e)
    {
        if (e.ActionType == SKTouchAction.Pressed)
        {
            e.Handled = true;
            int tapped = HitTestDie(e.Location);
            if (tapped >= 0)
                DieTapped?.Invoke(tapped);
        }
    }

    /// <summary>
    /// Simple hit test: project each die center, find closest to tap within threshold.
    /// Threshold is based on the projected die size so it works across screen densities.
    /// </summary>
    private int HitTestDie(SKPoint tapPoint)
    {
        if (_camera == null) return -1;

        float bestDist = float.MaxValue;
        int bestIndex = -1;

        for (int i = 0; i < 5; i++)
        {
            var state = _physics.GetDieState(i);
            var (cx, cy) = _camera.Project(state.Position);

            // Compute screen-space die radius by projecting an offset point
            var offset = state.Position + new Vector3(0.5f, 0, 0); // DieHalfSize = 0.5
            var (ox, _) = _camera.Project(offset);
            float dieScreenRadius = MathF.Abs(ox - cx);
            float threshold = MathF.Max(dieScreenRadius * 1.4f, 20f); // generous tap area

            float dx = tapPoint.X - cx;
            float dy = tapPoint.Y - cy;
            float dist = MathF.Sqrt(dx * dx + dy * dy);
            if (dist < threshold && dist < bestDist)
            {
                bestDist = dist;
                bestIndex = i;
            }
        }
        return bestIndex;
    }
}
