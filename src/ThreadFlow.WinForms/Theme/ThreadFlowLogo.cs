using System.Drawing.Drawing2D;
using ThreadFlow.WinForms.Theme.Colors;

namespace ThreadFlow.WinForms.Theme;

/// <summary>
/// The ThreadFlow logo: a rounded brand-blue tile with a white clothes hanger.
/// Use this control on every screen that shows the logo so it always looks identical.
/// Placeholder until a real logo image is available; swapping it here updates every screen.
/// </summary>
public sealed class ThreadFlowLogo : Control
{
    private static readonly Pallets Palette = new();

    public ThreadFlowLogo(int size = 56)
    {
        Size = new Size(size, size);
        TabStop = false;

        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;

        // Paint the corners in whatever color sits behind the logo, so it works on any background.
        g.Clear(Parent?.BackColor ?? BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var tileColor = Palette.Primary.Color;
        var hangerColor = Palette.BackgroundSecondary.Color;

        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        using (var tile = RoundedRectangle(bounds, Math.Max(8, Width / 4)))
        using (var fill = new SolidBrush(tileColor))
        {
            g.FillPath(fill, tile);
        }

        // The hanger is drawn on a 24 x 24 grid, then scaled to fit the tile.
        // Shape: a small hook on top, a short neck, and a wide shallow triangle for the body.
        var scale = Math.Min(Width, Height) * 0.62f / 24f;
        var iconSize = 24f * scale;
        g.TranslateTransform((Width - iconSize) / 2f, (Height - iconSize) / 2f - 0.25f * scale);
        g.ScaleTransform(scale, scale);

        using var pen = new Pen(hangerColor, 2f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };

        // Hook: top half of a small circle, curling down into the neck.
        using (var hook = new GraphicsPath())
        {
            hook.AddArc(10f, 5f, 4f, 4f, 0f, -180f);
            hook.AddBezier(
                new PointF(10f, 7f),
                new PointF(10f, 8.1f),
                new PointF(10.9f, 9f),
                new PointF(12f, 9f));
            g.DrawPath(pen, hook);
        }

        // Neck.
        g.DrawLine(pen, 12f, 9f, 12f, 11f);

        // Body: wide, shallow triangle with a flat bottom bar.
        g.DrawPolygon(pen,
        [
            new PointF(12f, 11f),
            new PointF(2.8f, 17.6f),
            new PointF(3.6f, 19f),
            new PointF(20.4f, 19f),
            new PointF(21.2f, 17.6f)
        ]);

        base.OnPaint(e);
    }

    private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}