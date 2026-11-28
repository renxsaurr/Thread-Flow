using System.Globalization;

namespace ThreadFlow.WinForms.ApiClient;

public static class TenantCurrencyFormatter
{
    private static string _currencyCode = "PHP";
    private static CultureInfo _culture = CultureInfo.GetCultureInfo("en-PH");

    public static string CurrencyCode => _currencyCode;
    public static CultureInfo Culture => _culture;

    public static void SetCurrencyCode(string? currencyCode)
    {
        var code = currencyCode?.Trim().ToUpperInvariant();
        (_currencyCode, _culture) = code switch
        {
            "USD" => ("USD", CultureInfo.GetCultureInfo("en-US")),
            _ => ("PHP", CultureInfo.GetCultureInfo("en-PH"))
        };
    }

    public static string Format(decimal amount) => amount.ToString("C2", _culture);
    public static string FormatNumber(decimal amount) => amount.ToString("N2", _culture);
    public static string FieldLabel(string label) => $"{label} ({_currencyCode})";

    public static void ConfigureCurrencyColumn(DataGridView grid, string propertyName)
    {
        if (!grid.Columns.Contains(propertyName))
            return;

        var style = grid.Columns[propertyName]!.DefaultCellStyle;
        style.Format = "C2";
        style.FormatProvider = _culture;
    }
}
