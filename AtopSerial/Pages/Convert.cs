using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace AtopSerial.Pages
{
    class Convert
    {
    }

    /// <summary>
    /// bool正向显示隐藏
    /// </summary>
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class boolVisibeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(bool)value)
                return Visibility.Collapsed;
            else
                return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    /// <summary>
    /// bool反向显示隐藏
    /// </summary>
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class boolNotVisibeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
                return Visibility.Collapsed;
            else
                return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    /// <summary>
    /// Graph和文本框图标的切换
    /// </summary>
    [ValueConversion(typeof(bool), typeof(string))]
    public class graphSwIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
                return "\ue694";
            else
                return "\ue766";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    /// <summary>
    /// 数据监控区选择全部图标
    /// </summary>
    [ValueConversion(typeof(bool), typeof(string))]
    public class dataMonitorAllSelectIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
                return "ToggleOn";
            else
                return "ToggleOff";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    /// <summary>
    /// 数据监控区仅显示选择图标
    /// </summary>
    [ValueConversion(typeof(bool), typeof(string))]
    public class dataMonitorSelectShowIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
                return "Eye";
            else
                return "LowVision";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    /// <summary>
    /// bool为true时显示连接，否则显示断开
    /// </summary>
    [ValueConversion(typeof(bool), typeof(string))]
    public class boolConnected : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? "Disconnect" : "Connect";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    /// <summary>
    /// bool为true时显示连接，否则显示断开
    /// </summary>
    [ValueConversion(typeof(bool), typeof(bool))]
    public class boolNot : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }


    /// <summary>
    /// bool为true时显示连接，否则显示断开
    /// </summary>
    [ValueConversion(typeof(int), typeof(bool?))]
    public class showHexFormat : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                1 => false,
                2 => true,
                _ => null,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                false => 1,
                true => 2,
                _ => 0,
            };
        }
    }
}
