using System;
using System.Globalization;
using System.Windows.Data;
using Aronium.Pos.Common.Extensions;

namespace Aronium.Pos.MultiUom
{
    internal sealed class MoneyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "";
            decimal amount = System.Convert.ToDecimal(value, CultureInfo.InvariantCulture);
            string format = StringFormatExtension.MoneyFormat;
            if (string.IsNullOrEmpty(format)) format = "N0";
            return amount.ToString(format, CultureInfo.InvariantCulture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string text = (value == null ? "" : value.ToString()).Trim();
            decimal amount;
            if (decimal.TryParse(text, NumberStyles.Number | NumberStyles.Currency, CultureInfo.InvariantCulture, out amount))
                return amount;
            if (decimal.TryParse(text, NumberStyles.Number | NumberStyles.Currency, CultureInfo.CurrentCulture, out amount))
                return amount;
            string normalized = text.Replace(" ", "").Replace("$", "").Replace("Rp", "");
            if (decimal.TryParse(normalized, NumberStyles.Number | NumberStyles.Currency, CultureInfo.InvariantCulture, out amount))
                return amount;
            throw new FormatException("Format harga tidak valid.");
        }
    }
}
