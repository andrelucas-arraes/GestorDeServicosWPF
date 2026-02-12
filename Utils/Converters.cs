using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace GestaoAulas.Utils
{
    /// <summary>
    /// Converte status para cor de fundo.
    /// </summary>
    public class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status switch
                {
                    "Pago" => new SolidColorBrush(Color.FromRgb(34, 197, 94)),      // Verde
                    "Pendente" => new SolidColorBrush(Color.FromRgb(245, 158, 11)), // Amarelo
                    _ => new SolidColorBrush(Color.FromRgb(110, 118, 129))          // Cinza
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converte valor booleano para Visibility.
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }

    /// <summary>
    /// Converte valor nulo para Visibility.
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool invert = parameter?.ToString() == "invert";
            bool isNull = value == null;
            
            if (invert)
            {
                return isNull ? Visibility.Visible : Visibility.Collapsed;
            }
            return isNull ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converte valor decimal para string formatada em reais.
    /// </summary>
    public class CurrencyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal decValue)
            {
                return decValue.ToString("C2", new CultureInfo("pt-BR"));
            }
            if (value is double dblValue)
            {
                return dblValue.ToString("C2", new CultureInfo("pt-BR"));
            }
            return "R$ 0,00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string strValue)
            {
                strValue = strValue.Replace("R$", "").Replace(" ", "").Trim();
                if (decimal.TryParse(strValue, NumberStyles.Currency, new CultureInfo("pt-BR"), out decimal result))
                {
                    return result;
                }
            }
            return 0m;
        }
    }

    /// <summary>
    /// Converte duração em horas para formato H:MM.
    /// </summary>
    public class HoursToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double hours)
            {
                int h = (int)hours;
                int m = (int)((hours - h) * 60);
                return $"{h}:{m:D2}";
            }
            return "0:00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string strValue && strValue.Contains(':'))
            {
                var parts = strValue.Split(':');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out int hours) &&
                    int.TryParse(parts[1], out int minutes))
                {
                    return hours + (minutes / 60.0);
                }
            }
            return 0.0;
        }
    }

    /// <summary>
    /// Converte data para formato brasileiro.
    /// </summary>
    public class DateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime date)
            {
                return date.ToString("dd/MM/yyyy");
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string strValue)
            {
                if (DateTime.TryParseExact(strValue, "dd/MM/yyyy", 
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
                {
                    return result;
                }
            }
            return DateTime.Today;
        }
    }

    /// <summary>
    /// Extrai o dia do mês de uma data.
    /// </summary>
    public class DateToDayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime date)
            {
                return date.Day.ToString("D2");
            }
            return "--";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converte data para dia da semana completo (ex: QUINTA).
    /// </summary>
    public class DateToFullWeekDayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime date)
            {
                // "dddd" retorna o nome completo (segunda-feira).
                // "pt-BR"
                var culturePt = new CultureInfo("pt-BR");
                string dia = date.ToString("dddd", culturePt).ToUpper();
                
                // Remove "-FEIRA" se desejar apenas o nome principal como no exemplo "QUINTA"
                if (dia.Contains("-FEIRA"))
                {
                    dia = dia.Replace("-FEIRA", "");
                }
                
                return dia;
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
