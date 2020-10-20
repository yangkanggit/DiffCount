using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace DiffCountByBeyond.Services
{
    //[ValueConversion(typeof(string), typeof(string))]
    public class MyConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string item = value.ToString();
            if (item== DiffCountByBeyond.Services.Items.代码差异行和New新增行总数.ToString())
            {
                return new SolidColorBrush(Colors.Red);
                
            }
            return new SolidColorBrush(Colors.Black);
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
