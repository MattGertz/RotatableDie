namespace YachtDiceMaui.Views;

/// <summary>
/// Draws a single die face with pips. Used as a GraphicsView drawable.
/// </summary>
public class DieDrawable : IDrawable
{
    public int Value { get; set; } = 1;
    public Color FaceColor { get; set; } = Colors.White;
    public Color PipColor { get; set; } = Colors.DarkRed;
    public Color BorderColor { get; set; } = Colors.DarkRed;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        float size = Math.Min(dirtyRect.Width, dirtyRect.Height);
        float x = (dirtyRect.Width - size) / 2;
        float y = (dirtyRect.Height - size) / 2;
        float corner = size * 0.12f;
        float pipR = size * 0.07f;

        // Die face
        canvas.FillColor = FaceColor;
        canvas.FillRoundedRectangle(x, y, size, size, corner);

        // Border
        canvas.StrokeColor = BorderColor;
        canvas.StrokeSize = 2;
        canvas.DrawRoundedRectangle(x, y, size, size, corner);

        // Pip positions (relative to die face)
        float cx = x + size / 2;
        float cy = y + size / 2;
        float off = size * 0.28f;

        var pips = GetPipPositions(cx, cy, off);
        canvas.FillColor = PipColor;
        foreach (var (px, py) in pips)
            canvas.FillCircle(px, py, pipR);
    }

    private List<(float X, float Y)> GetPipPositions(float cx, float cy, float off)
    {
        return Value switch
        {
            1 => [(cx, cy)],
            2 => [(cx - off, cy - off), (cx + off, cy + off)],
            3 => [(cx - off, cy - off), (cx, cy), (cx + off, cy + off)],
            4 => [(cx - off, cy - off), (cx + off, cy - off),
                   (cx - off, cy + off), (cx + off, cy + off)],
            5 => [(cx - off, cy - off), (cx + off, cy - off),
                   (cx, cy),
                   (cx - off, cy + off), (cx + off, cy + off)],
            6 => [(cx - off, cy - off), (cx + off, cy - off),
                   (cx - off, cy), (cx + off, cy),
                   (cx - off, cy + off), (cx + off, cy + off)],
            _ => [(cx, cy)]
        };
    }
}
