using System.Drawing;

namespace ThreadFlow.WinForms.Theme;

/// <summary>
/// Vertical metrics shared by the app's text fields. A borderless single-line TextBox draws its
/// text at the very top of its own box, so a box sized from the control's default height leaves
/// only a pixel under the glyphs; any DPI rounding then cuts the tails off g, j, p, q and y.
/// Every field frame places its input with these numbers instead, so all fields keep the same
/// room under the line.
/// </summary>
internal static class InputMetrics
{
    // Room kept under the font's line, enough for a descender at any DPI rounding.
    private const int TailRoom = 10;

    // Space kept between the input's box and the rounded frame border.
    private const int BorderGap = 2;

    public static int LineHeight(Font font, int dpi) =>
        (int)Math.Ceiling(font.GetHeight(dpi));

    /// <summary>
    /// The bounds of a single-line text box inside a field frame: the text line is centred in the
    /// frame (the glyphs start at the top of the box) and the box is taller than the line, so the
    /// tails of g, j, p, q and y are never clipped.
    /// </summary>
    public static Rectangle SingleLineBox(
        int frameWidth,
        int frameHeight,
        Font font,
        int dpi,
        int paddingLeft,
        int paddingRight)
    {
        var lineHeight = LineHeight(font, dpi);
        var top = Math.Max(0, (frameHeight - lineHeight) / 2);
        var available = Math.Max(1, frameHeight - top - BorderGap);
        var height = Math.Min(lineHeight + TailRoom, Math.Max(lineHeight, available));

        return new Rectangle(
            paddingLeft,
            top,
            Math.Max(10, frameWidth - paddingLeft - paddingRight),
            height);
    }
}
