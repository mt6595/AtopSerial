using System;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using HandyControl.Data;
using HandyControl.Themes;
using HandyControl.Tools;

namespace AtopSerial.Tools
{
    internal static class ThemeManager
    {
        private const string ThemePrefix = "pack://application:,,,/HandyControl;component/Themes/";
        private const string SkinDictionaryKey = "AtopSerial.HandyControlSkin";
        private const string ThemeDictionaryKey = "AtopSerial.HandyControlTheme";

        public const string DefaultTheme = "Default";
        public const string DarkTheme = "Dark";
        public const string VioletTheme = "Violet";

        public static void Apply(string themeName)
        {
            var app = Application.Current;
            if (app == null)
                return;

            var dictionaries = app.Resources.MergedDictionaries;

            // 找出旧的 HandyControl 皮肤/主题字典（带标记的替换实例 + App.xaml 里的初始实例）
            var oldDictionaries = dictionaries
                .Where(x => x.Contains(SkinDictionaryKey) || x.Contains(ThemeDictionaryKey)
                    || (x.Source != null
                        && x.Source.OriginalString.IndexOf("/Themes/", StringComparison.OrdinalIgnoreCase) >= 0
                        && (x.Source.OriginalString.IndexOf("Skin", StringComparison.OrdinalIgnoreCase) >= 0
                            || x.Source.OriginalString.EndsWith("Theme.xaml", StringComparison.OrdinalIgnoreCase))))
                .ToList();
            int insertIndex = oldDictionaries.Count > 0
                ? dictionaries.IndexOf(oldDictionaries[0])
                : dictionaries.Count;
            foreach (var old in oldDictionaries)
                dictionaries.Remove(old);
            if (insertIndex > dictionaries.Count)
                insertIndex = dictionaries.Count;

            // 官方换肤流程：清空共享字典缓存，Skin 和 Theme 一起重建。
            // Theme.xaml 里的画刷是 o:Freeze 冻结的，只换 Skin 字典不会重取颜色。
            SharedResourceDictionary.SharedDictionaries.Clear();

            var skin = ResourceHelper.GetSkin(GetSkinType(themeName));
            skin[SkinDictionaryKey] = true;
            var theme = new ResourceDictionary
            {
                Source = new Uri(ThemePrefix + "Theme.xaml", UriKind.Absolute)
            };
            theme[ThemeDictionaryKey] = true;

            dictionaries.Insert(insertIndex, skin);
            dictionaries.Insert(insertIndex + 1, theme);

            // 强制所有已打开窗口重新应用模板，已实例化的控件才能换肤
            foreach (Window window in app.Windows)
            {
                if (window.IsLoaded)
                    window.OnApplyTemplate();
            }

            ApplyWindowChromeTheme(themeName);
            ApplyPlotTheme();
        }

        /// <summary>
        /// 联动 ScottPlot 曲线主题：深色(Dark)用 Black，浅色/紫色用 Default。
        /// 由 PlotPage.ApplyLinkedTheme 实际执行；曲线页未创建时跳过，其构造时会自行同步。
        /// 同时联动日志显示区（uartDataText）字体配色。
        /// </summary>
        private static void ApplyPlotTheme()
        {
            try
            {
                foreach (Window window in Application.Current.Windows)
                {
                    // 注意：MainWindow 在 AtopSerial 根命名空间，不在 View 下
                    if (window is global::AtopSerial.MainWindow mainWindow)
                    {
                        mainWindow.PlotThemeSync();
                        mainWindow.DataShowThemeSync();
                    }
                }
            }
            catch
            {
                // 曲线页/日志页尚未创建或正在关闭时忽略
            }
        }

        public static string Normalize(string themeName)
        {
            if (string.Equals(themeName, DarkTheme, StringComparison.OrdinalIgnoreCase))
                return DarkTheme;
            if (string.Equals(themeName, VioletTheme, StringComparison.OrdinalIgnoreCase))
                return VioletTheme;
            return DefaultTheme;
        }

        private static SkinType GetSkinType(string themeName)
        {
            switch (Normalize(themeName))
            {
                case DarkTheme:
                    return SkinType.Dark;
                case VioletTheme:
                    return SkinType.Violet;
                default:
                    return SkinType.Default;
            }
        }

        private static void ApplyWindowChromeTheme(string themeName)
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (!window.IsLoaded)
                    continue;
                SetTitleBarTheme(window, Normalize(themeName));
            }
        }

        public static void ApplyWindowChromeTheme(Window window, string themeName)
        {
            SetTitleBarTheme(window, Normalize(themeName));
        }

        private static void SetTitleBarTheme(Window window, string themeName)
        {
            var source = PresentationSource.FromVisual(window) as HwndSource;
            if (source == null)
                return;

            try
            {
                bool dark = Normalize(themeName) == DarkTheme;
                int darkAttribute = 20; // DWMWA_USE_IMMERSIVE_DARK_MODE
                int darkValue = dark ? 1 : 0;
                DwmSetWindowAttribute(source.Handle, darkAttribute, ref darkValue, sizeof(int));

                int captionAttribute = 35; // DWMWA_CAPTION_COLOR
                int captionColor = ColorToDwmColor(GetCaptionColor(themeName));
                DwmSetWindowAttribute(source.Handle, captionAttribute, ref captionColor, sizeof(int));

                int textAttribute = 36; // DWMWA_TEXT_COLOR
                int textColor = ColorToDwmColor(dark ? Colors.White : Colors.Black);
                DwmSetWindowAttribute(source.Handle, textAttribute, ref textColor, sizeof(int));

                // 强制重绘非客户区，否则已显示的窗口要等焦点切换后才刷新标题栏颜色
                SetWindowPos(source.Handle, IntPtr.Zero, 0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED);

                // Win10 上 DWM 属性变更后标题栏不会主动重绘：
                // 1) 立即强制重画一次；2) 再延迟一次（等合成器登记新属性后补画）
                ForceNcRedraw(source.Handle);
                source.Dispatcher.BeginInvoke(new Action(() => ForceNcRedraw(source.Handle)),
                    System.Windows.Threading.DispatcherPriority.Background);
            }
            catch
            {
                // 旧版 Windows 不支持标题栏颜色属性时保留系统标题栏。
            }
        }

        private static void ForceNcRedraw(IntPtr hwnd)
        {
            try
            {
                // WM_NCACTIVATE 相反态 -> 真实态切换：
                // 激活窗口：0 -> 1；非激活窗口：1 -> 0。
                // 必须先发与当前相反的状态，状态真正翻转一次，DefWindowProc 才会按最新
                // DWM 属性完整重画标题栏（否则系统判定状态未变而跳过重绘）
                bool isActive = GetForegroundWindow() == hwnd;
                SendMessage(hwnd, WM_NCACTIVATE, isActive ? IntPtr.Zero : (IntPtr)1, IntPtr.Zero);
                SendMessage(hwnd, WM_NCACTIVATE, isActive ? (IntPtr)1 : IntPtr.Zero, IntPtr.Zero);
                RedrawWindow(hwnd, IntPtr.Zero, IntPtr.Zero,
                    RDW_INVALIDATE | RDW_FRAME | RDW_UPDATENOW);
            }
            catch
            {
                // 窗口可能已关闭，忽略。
            }
        }

        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_FRAMECHANGED = 0x0020;

        private const int WM_NCACTIVATE = 0x0086;
        private const uint RDW_INVALIDATE = 0x0001;
        private const uint RDW_FRAME = 0x0400;
        private const uint RDW_UPDATENOW = 0x0100;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetWindowPos(
            IntPtr hWnd, IntPtr hWndInsertAfter,
            int x, int y, int cx, int cy, uint flags);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);

        private static Color GetCaptionColor(string themeName)
        {
            switch (Normalize(themeName))
            {
                case DarkTheme:
                    return Color.FromRgb(32, 32, 32); // 使用的是系统窗台，无法修改窗台颜色，只能更改明亮主题
                case VioletTheme:
                    return Color.FromRgb(112, 88, 148);
                default:
                    return Color.FromRgb(245, 245, 245);
            }
        }

        private static int ColorToDwmColor(Color color)
        {
            return color.R | (color.G << 8) | (color.B << 16);
        }

        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(
            IntPtr hwnd,
            int attribute,
            ref int value,
            int valueSize);
    }
}
