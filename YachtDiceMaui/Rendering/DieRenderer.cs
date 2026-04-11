using System.Numerics;
using SkiaSharp;
using YachtDiceMaui.Physics;

namespace YachtDiceMaui.Rendering;

/// <summary>
/// Renders a 3D cube die onto a SkiaSharp canvas using perspective projection.
/// Consumes DieState (position + quaternion rotation) from the physics engine.
/// </summary>
public class DieRenderer
{
    // Cube vertices (unit cube centered at origin, half-size = 0.5)
    private static readonly Vector3[] CubeVerts =
    {
        new(-0.5f, -0.5f, -0.5f), // 0: left-bottom-front
        new( 0.5f, -0.5f, -0.5f), // 1: right-bottom-front
        new( 0.5f,  0.5f, -0.5f), // 2: right-top-front
        new(-0.5f,  0.5f, -0.5f), // 3: left-top-front
        new(-0.5f, -0.5f,  0.5f), // 4: left-bottom-back
        new( 0.5f, -0.5f,  0.5f), // 5: right-bottom-back
        new( 0.5f,  0.5f,  0.5f), // 6: right-top-back
        new(-0.5f,  0.5f,  0.5f), // 7: left-top-back
    };

    // Each face: 4 vertex indices (CCW when viewed from outside), face normal, face value
    private static readonly (int[] Verts, Vector3 Normal, int Value)[] Faces =
    {
        (new[]{0, 1, 2, 3}, new Vector3( 0,  0, -1), 1), // Front  (-Z) = 1
        (new[]{1, 5, 6, 2}, new Vector3( 1,  0,  0), 2), // Right  (+X) = 2
        (new[]{0, 4, 5, 1}, new Vector3( 0, -1,  0), 3), // Bottom (-Y) = 3
        (new[]{3, 2, 6, 7}, new Vector3( 0,  1,  0), 4), // Top    (+Y) = 4
        (new[]{4, 0, 3, 7}, new Vector3(-1,  0,  0), 5), // Left   (-X) = 5
        (new[]{5, 4, 7, 6}, new Vector3( 0,  0,  1), 6), // Back   (+Z) = 6
    };

    // Pip patterns for each face value (positions in 0..1 UV space)
    // Layout: standard Western die pip positions
    private static readonly Dictionary<int, (float U, float V)[]> PipPatterns = new()
    {
        [1] = [(0.5f, 0.5f)],
        [2] = [(0.3f, 0.3f), (0.7f, 0.7f)],
        [3] = [(0.3f, 0.3f), (0.5f, 0.5f), (0.7f, 0.7f)],
        [4] = [(0.3f, 0.3f), (0.7f, 0.3f), (0.3f, 0.7f), (0.7f, 0.7f)],
        [5] = [(0.3f, 0.3f), (0.7f, 0.3f), (0.5f, 0.5f), (0.3f, 0.7f), (0.7f, 0.7f)],
        [6] = [(0.3f, 0.25f), (0.7f, 0.25f), (0.3f, 0.5f), (0.7f, 0.5f), (0.3f, 0.75f), (0.7f, 0.75f)],
    };

    // Blotch cache removed — spotty texture feature removed

    /// <summary>
    /// Draw a single die on the canvas.
    /// </summary>
    /// <param name="canvas">SkiaSharp canvas to draw on</param>
    /// <param name="state">Position and rotation from physics</param>
    /// <param name="dieIndex">Index of this die (0-4) for blotch seeding</param>
    /// <param name="appearance">Color/texture settings</param>
    /// <param name="camera">Camera for projection</param>
    /// <param name="isHeld">Whether this die is held (draw highlight border)</param>
    public void DrawDie(SKCanvas canvas, DieState state, int dieIndex,
        DiceAppearance appearance, Camera camera, bool isHeld)
    {
        // Transform vertices to world space
        var worldVerts = new SKPoint[8];
        var worldZ = new float[8];
        for (int i = 0; i < 8; i++)
        {
            var v = Vector3.Transform(CubeVerts[i], state.Rotation) + state.Position;
            var (sx, sy) = camera.Project(v);
            worldVerts[i] = new SKPoint(sx, sy);
            worldZ[i] = camera.GetDepth(v);
        }

        // Sort faces back-to-front (painter's algorithm)
        var faceOrder = new List<int> { 0, 1, 2, 3, 4, 5 };
        faceOrder.Sort((a, b) =>
        {
            float za = 0, zb = 0;
            for (int i = 0; i < 4; i++)
            {
                za += worldZ[Faces[a].Verts[i]];
                zb += worldZ[Faces[b].Verts[i]];
            }
            return zb.CompareTo(za); // far first
        });

        var lightDir = Vector3.Normalize(new Vector3(0.3f, 1f, -0.5f));

        foreach (int fi in faceOrder)
        {
            var face = Faces[fi];
            var worldNormal = Vector3.Normalize(Vector3.Transform(face.Normal, state.Rotation));

            // Back-face culling: skip faces pointing away from or edge-on to camera
            var toCam = Vector3.Normalize(camera.Position - state.Position);
            if (Vector3.Dot(worldNormal, toCam) < 0.08f)
                continue;

            // Build face polygon path
            var path = new SKPath();
            path.MoveTo(worldVerts[face.Verts[0]]);
            for (int i = 1; i < 4; i++)
                path.LineTo(worldVerts[face.Verts[i]]);
            path.Close();

            // Lighting: diffuse + ambient
            float diffuse = MathF.Max(0, Vector3.Dot(worldNormal, lightDir));
            float brightness = 0.45f + 0.55f * diffuse;

            var baseColor = appearance.DieColor;
            byte faceAlpha = appearance.Translucent ? (byte)190 : (byte)255;
            var litColor = new SKColor(
                (byte)MathF.Min(255, baseColor.Red * brightness),
                (byte)MathF.Min(255, baseColor.Green * brightness),
                (byte)MathF.Min(255, baseColor.Blue * brightness),
                faceAlpha);

            // Fill face
            using var facePaint = new SKPaint
            {
                Color = litColor,
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
            };
            canvas.DrawPath(path, facePaint);

            // Edge outline
            using var edgePaint = new SKPaint
            {
                Color = new SKColor(0, 0, 0, (byte)(appearance.Translucent ? 40 : 60)),
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.2f,
            };
            canvas.DrawPath(path, edgePaint);

            // Draw pips or numbers on this face
            if (appearance.UseNumbers)
                DrawNumber(canvas, face, worldVerts, appearance.PipColor, brightness, faceAlpha);
            else
                DrawPips(canvas, face, worldVerts, appearance.PipColor, brightness, faceAlpha);
        }

        // Held indicator: glow border around the die
        if (isHeld)
        {
            DrawHeldIndicator(canvas, worldVerts);
        }
    }

    private const int PipSegments = 12;
    private const float PipUVRadius = 0.065f; // radius in UV space

    private void DrawPips(SKCanvas canvas, (int[] Verts, Vector3 Normal, int Value) face,
        SKPoint[] worldVerts, SKColor pipColor, float brightness, byte alpha)
    {
        if (!PipPatterns.TryGetValue(face.Value, out var pips))
            return;

        var litPip = new SKColor(
            (byte)MathF.Min(255, pipColor.Red * (0.6f + 0.4f * brightness)),
            (byte)MathF.Min(255, pipColor.Green * (0.6f + 0.4f * brightness)),
            (byte)MathF.Min(255, pipColor.Blue * (0.6f + 0.4f * brightness)),
            alpha);

        using var pipPaint = new SKPaint
        {
            Color = litPip,
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
        };

        foreach (var (pu, pv) in pips)
        {
            var path = new SKPath();
            for (int s = 0; s < PipSegments; s++)
            {
                float angle = s * MathF.Tau / PipSegments;
                float su = pu + MathF.Cos(angle) * PipUVRadius;
                float sv = pv + MathF.Sin(angle) * PipUVRadius;
                var pt = InterpolateQuad(worldVerts, face.Verts, su, sv);
                if (s == 0) path.MoveTo(pt);
                else path.LineTo(pt);
            }
            path.Close();
            canvas.DrawPath(path, pipPaint);
        }
    }

    private void DrawNumber(SKCanvas canvas, (int[] Verts, Vector3 Normal, int Value) face,
        SKPoint[] worldVerts, SKColor pipColor, float brightness, byte alpha)
    {
        float faceSize = QuadSize(worldVerts, face.Verts);
        if (faceSize < 8f) return;

        var litPip = new SKColor(
            (byte)MathF.Min(255, pipColor.Red * (0.6f + 0.4f * brightness)),
            (byte)MathF.Min(255, pipColor.Green * (0.6f + 0.4f * brightness)),
            (byte)MathF.Min(255, pipColor.Blue * (0.6f + 0.4f * brightness)),
            alpha);

        // Get text outline as a path
        using var font = new SKFont(
            SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold,
                SKFontStyleWidth.Normal, SKFontStyleSlant.Upright),
            100f);
        using var textPath = font.GetTextPath(face.Value.ToString(), new SKPoint(0, 0));
        if (textPath == null || textPath.PointCount == 0) return;

        var bounds = textPath.TightBounds;
        if (bounds.Width < 1f || bounds.Height < 1f) return;

        // Map text path points from glyph coords → UV (0.15..0.85) → face screen coords
        // via InterpolateQuad, just like pips
        float uvMargin = 0.18f;
        float uvRange = 1f - 2f * uvMargin;

        using var projPath = new SKPath();
        using var iter = textPath.CreateRawIterator();
        var pts = new SKPoint[4];
        SKPathVerb verb;
        while ((verb = iter.Next(pts)) != SKPathVerb.Done)
        {
            switch (verb)
            {
                case SKPathVerb.Move:
                    projPath.MoveTo(GlyphToFace(pts[0]));
                    break;
                case SKPathVerb.Line:
                    projPath.LineTo(GlyphToFace(pts[1]));
                    break;
                case SKPathVerb.Quad:
                    // Approximate quad bezier with line segments
                    for (int s = 1; s <= 4; s++)
                    {
                        float t = s / 4f;
                        float it = 1f - t;
                        float px = it * it * pts[0].X + 2f * it * t * pts[1].X + t * t * pts[2].X;
                        float py = it * it * pts[0].Y + 2f * it * t * pts[1].Y + t * t * pts[2].Y;
                        projPath.LineTo(GlyphToFace(new SKPoint(px, py)));
                    }
                    break;
                case SKPathVerb.Cubic:
                    for (int s = 1; s <= 6; s++)
                    {
                        float t = s / 6f;
                        float it = 1f - t;
                        float px = it * it * it * pts[0].X + 3f * it * it * t * pts[1].X
                                 + 3f * it * t * t * pts[2].X + t * t * t * pts[3].X;
                        float py = it * it * it * pts[0].Y + 3f * it * it * t * pts[1].Y
                                 + 3f * it * t * t * pts[2].Y + t * t * t * pts[3].Y;
                        projPath.LineTo(GlyphToFace(new SKPoint(px, py)));
                    }
                    break;
                case SKPathVerb.Close:
                    projPath.Close();
                    break;
            }
        }

        using var paint = new SKPaint
        {
            Color = litPip,
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
        };
        canvas.DrawPath(projPath, paint);

        SKPoint GlyphToFace(SKPoint glyph)
        {
            float u = uvMargin + (glyph.X - bounds.Left) / bounds.Width * uvRange;
            float v = uvMargin + (glyph.Y - bounds.Top) / bounds.Height * uvRange;
            return InterpolateQuad(worldVerts, face.Verts, u, v);
        }
    }

    private static void DrawHeldIndicator(SKCanvas canvas, SKPoint[] worldVerts)
    {
        // Draw a golden glow around the die's silhouette
        // Use all 8 projected vertices to find bounding box
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;
        foreach (var v in worldVerts)
        {
            minX = MathF.Min(minX, v.X);
            minY = MathF.Min(minY, v.Y);
            maxX = MathF.Max(maxX, v.X);
            maxY = MathF.Max(maxY, v.Y);
        }

        float pad = 4f;
        using var holdPaint = new SKPaint
        {
            Color = new SKColor(0xFF, 0xD7, 0x00, 180),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 3f,
        };
        canvas.DrawRoundRect(minX - pad, minY - pad,
            maxX - minX + pad * 2, maxY - minY + pad * 2, 4, 4, holdPaint);
    }

    /// <summary>
    /// Bilinear interpolation across a projected quad.
    /// UV (0,0) = Verts[0], (1,0) = Verts[1], (1,1) = Verts[2], (0,1) = Verts[3]
    /// </summary>
    private static SKPoint InterpolateQuad(SKPoint[] allVerts, int[] faceVerts, float u, float v)
    {
        var p0 = allVerts[faceVerts[0]];
        var p1 = allVerts[faceVerts[1]];
        var p2 = allVerts[faceVerts[2]];
        var p3 = allVerts[faceVerts[3]];

        var top = Lerp(p0, p1, u);
        var bot = Lerp(p3, p2, u);
        return Lerp(top, bot, v);
    }

    private static SKPoint Lerp(SKPoint a, SKPoint b, float t)
        => new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);

    /// <summary>
    /// Approximate size of the projected quad (average of diagonals).
    /// </summary>
    private static float QuadSize(SKPoint[] allVerts, int[] faceVerts)
    {
        var d1 = Distance(allVerts[faceVerts[0]], allVerts[faceVerts[2]]);
        var d2 = Distance(allVerts[faceVerts[1]], allVerts[faceVerts[3]]);
        return (d1 + d2) / 2f;
    }

    private static float Distance(SKPoint a, SKPoint b)
    {
        float dx = a.X - b.X, dy = a.Y - b.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Get the screen-space bounding box of a die's 8 projected vertices.
    /// </summary>
    public static (float MinX, float MinY, float MaxX, float MaxY) GetScreenBounds(
        DieState state, Camera camera)
    {
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;
        for (int i = 0; i < 8; i++)
        {
            var v = Vector3.Transform(CubeVerts[i], state.Rotation) + state.Position;
            var (sx, sy) = camera.Project(v);
            if (sx < minX) minX = sx;
            if (sy < minY) minY = sy;
            if (sx > maxX) maxX = sx;
            if (sy > maxY) maxY = sy;
        }
        return (minX, minY, maxX, maxY);
    }
}

/// <summary>
/// Simple perspective camera for top-down angled view of the dice tray.
/// </summary>
public class Camera
{
    public Vector3 Position { get; set; }
    public Vector3 Target { get; set; }
    public Vector3 Up { get; set; } = Vector3.UnitY;
    public float FieldOfView { get; set; } = 45f; // degrees
    public float ViewportWidth { get; set; }
    public float ViewportHeight { get; set; }

    private Matrix4x4 _view;
    private Matrix4x4 _proj;
    private bool _dirty = true;

    public void SetViewport(float width, float height)
    {
        ViewportWidth = width;
        ViewportHeight = height;
        _dirty = true;
    }

    private void UpdateMatrices()
    {
        if (!_dirty) return;
        _view = Matrix4x4.CreateLookAt(Position, Target, Up);
        float aspect = ViewportWidth / Math.Max(1, ViewportHeight);
        _proj = Matrix4x4.CreatePerspectiveFieldOfView(
            FieldOfView * MathF.PI / 180f, aspect, 0.1f, 100f);
        _dirty = false;
    }

    /// <summary>
    /// Project a 3D world point to 2D screen coordinates.
    /// </summary>
    public (float X, float Y) Project(Vector3 worldPoint)
    {
        UpdateMatrices();
        var vp = Vector4.Transform(new Vector4(worldPoint, 1f), _view * _proj);
        if (MathF.Abs(vp.W) < 0.0001f) vp.W = 0.0001f;

        float ndcX = vp.X / vp.W;
        float ndcY = vp.Y / vp.W;

        float sx = (ndcX * 0.5f + 0.5f) * ViewportWidth;
        float sy = (1f - (ndcY * 0.5f + 0.5f)) * ViewportHeight; // flip Y

        return (sx, sy);
    }

    /// <summary>
    /// Get the depth of a point in camera space (for sorting).
    /// </summary>
    public float GetDepth(Vector3 worldPoint)
    {
        UpdateMatrices();
        var vp = Vector4.Transform(new Vector4(worldPoint, 1f), _view);
        return vp.Z;
    }

    public void MarkDirty() => _dirty = true;

    /// <summary>
    /// Create a default camera for the dice tray: angled from above/front.
    /// trayWidth/trayDepth in world units.
    /// </summary>
    public static Camera CreateTrayCamera(float trayWidth, float trayDepth)
    {
        // Position further back with narrower FOV for less perspective distortion
        float dist = MathF.Max(trayWidth, trayDepth) * 1.9f;
        return new Camera
        {
            Position = new Vector3(0, dist * 0.88f, dist * 0.50f),
            Target = new Vector3(0, 0, 0.5f),
            Up = Vector3.UnitY,
            FieldOfView = 24f,
        };
    }
}
