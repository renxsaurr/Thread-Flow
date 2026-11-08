using ThreadFlow.Desktop.Theme.Colors;

namespace ThreadFlow.Desktop.Theme;

/// <summary>
/// Central WinForms theme built from the project's existing color tokens.
/// Use this on each form so future module screens share the same palette.
/// </summary>
public static class ThreadFlowTheme
{
    private static readonly Pallets Palette = new();
    private static readonly Font InterfaceFont = new("Segoe UI", 9F);

    public static void Apply(Control root)
    {
        ArgumentNullException.ThrowIfNull(root);

        root.BackColor = Palette.BackgroundPrimary.Color;
        root.ForeColor = Palette.TextPrimary.Color;
        root.Font = InterfaceFont;

        ApplyToChildren(root.Controls);
    }

    private static void ApplyToChildren(Control.ControlCollection controls)
    {
        foreach (Control control in controls)
        {
            switch (control)
            {
                case ButtonBase button:
                    button.BackColor = Palette.Primary.Color;
                    button.ForeColor = Palette.BackgroundSecondary.Color;
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = Palette.Primary.Color;
                    break;

                case TextBoxBase textBox:
                    textBox.BackColor = Palette.BackgroundSecondary.Color;
                    textBox.ForeColor = Palette.TextPrimary.Color;
                    break;

                case ComboBox comboBox:
                    comboBox.BackColor = Palette.BackgroundSecondary.Color;
                    comboBox.ForeColor = Palette.TextPrimary.Color;
                    break;

                case DataGridView grid:
                    grid.BackgroundColor = Palette.BackgroundSecondary.Color;
                    grid.GridColor = Palette.BorderSecondary.Color;
                    grid.EnableHeadersVisualStyles = false;
                    grid.ColumnHeadersDefaultCellStyle.BackColor = Palette.Primary.Color;
                    grid.ColumnHeadersDefaultCellStyle.ForeColor = Palette.BackgroundSecondary.Color;
                    grid.DefaultCellStyle.BackColor = Palette.BackgroundSecondary.Color;
                    grid.DefaultCellStyle.ForeColor = Palette.TextPrimary.Color;
                    grid.AlternatingRowsDefaultCellStyle.BackColor = Palette.BackgroundPrimary.Color;
                    break;

                case Panel or TabPage or GroupBox:
                    control.BackColor = Palette.BackgroundSecondary.Color;
                    control.ForeColor = Palette.TextPrimary.Color;
                    break;

                default:
                    control.ForeColor = Palette.TextPrimary.Color;
                    break;
            }

            control.Font = InterfaceFont;

            if (control.HasChildren)
            {
                ApplyToChildren(control.Controls);
            }
        }
    }
}
