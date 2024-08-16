﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
#if !NETSTANDARD2_0
using System.Windows.Data;
#endif

namespace ChoETL
{
    [ChoTypeConverter(typeof(ChoCurrency))]
#if !NETSTANDARD2_0
    public class ChoCurrencyConverter : IValueConverter
#else
    public class ChoCurrencyConverter : IChoValueConverter
#endif
    {
        private NumberStyles? GetConvertTypeFormat(object parameter)
        {
            ChoTypeConverterFormatSpec ts = parameter.GetValueAt<ChoTypeConverterFormatSpec>(0);
            if (ts != null)
                return ts.CurrencyNumberStyle;

            return parameter.GetValueAt(0, ChoTypeConverterFormatSpec.Instance.CurrencyNumberStyle);
        }
        private string GetConvertBackTypeFormat(object parameter)
        {
            ChoTypeConverterFormatSpec ts = parameter.GetValueAt<ChoTypeConverterFormatSpec>(0);
            if (ts != null)
                return ts.CurrencyFormat;

            return parameter.GetValueAt(1, ChoTypeConverterFormatSpec.Instance.CurrencyFormat);
        }
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string)
            {
                string text = value as string;
                if (text.IsNullOrWhiteSpace())
                    text = "0";

                NumberStyles? format = GetConvertTypeFormat(parameter); //.GetValueAt<NumberStyles?>(0, ChoTypeConverterFormatSpec.Instance.CurrencyNumberStyle);
                return format == null ? Double.Parse(text, culture) : Double.Parse(text, format.Value, culture);
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double && targetType == typeof(string))
            {
                double convValue = (double)value;
                string format = GetConvertBackTypeFormat(parameter); //.GetValueAt<string>(1, ChoTypeConverterFormatSpec.Instance.CurrencyFormat);
                if (format.IsNullOrWhiteSpace())
                    format = "C";

                return convValue.ToString(format, culture); // String.Format(culture, "{0:" + format + "}", value);
            }
            else if (value is ChoCurrency curr && targetType == typeof(string))
            {
                decimal convValue = curr.Amount;
                string format = parameter.GetValueAt<string>(1, ChoTypeConverterFormatSpec.Instance.CurrencyFormat);
                if (format.IsNullOrWhiteSpace())
                    format = "C";

                return convValue.ToString(format, culture); // String.Format(culture, "{0:" + format + "}", value);

            }
            else if (value == DBNull.Value)
                return null;

            return value;
        }
    }

    [ChoTypeConverter(typeof(XElement))]
#if !NETSTANDARD2_0
    public class ChoXElementConverter : IValueConverter
#else
    public class ChoXElementConverter : IChoValueConverter
#endif
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is XElement)
            {
                return ((XElement)value).NilAwareValue();
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }
}
