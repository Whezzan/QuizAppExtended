using System.Globalization;
using Lab3_QuizApp.Models;
using System.Windows.Data;

namespace Lab3_QuizApp.Converters
{
    internal class IntToDifficultyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value)
            {
                case Difficulty.Easy:
                    value = 0;
                    break;
                case Difficulty.Medium:
                    value = 1;
                    break;
                case Difficulty.Hard:
                    value = 2;
                    break;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value)
            {
                case 0:
                    value = Difficulty.Easy;
                    break;
                case 1:
                    value = Difficulty.Medium;
                    break;
                case 2:
                    value = Difficulty.Hard;
                    break;
            }
            return value;
        }
    }
}
