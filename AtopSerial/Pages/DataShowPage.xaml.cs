using AtopSerial.Tools;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Search;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using System.Xml.Linq;
using AtopSerial.Pages;
using static AtopSerial.Pages.DataShowPage.SimpleLineEffectRecorder;
using System.Drawing;
using System.Windows.Media.Effects;

namespace AtopSerial.Pages
{
    /// <summary>
    /// DataShowPage.xaml 的交互逻辑
    /// </summary>
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public partial class DataShowPage : Page
    {
        public DataShowPage()
        {
            InitializeComponent();
        }

        public int LogCount = 0;
        public bool sendPre;
        public bool newEmptyLine = false;
        public bool LockLog { get; set; } = false;
        SimpleLineEffectRecorder lineEffectsRecorder = new SimpleLineEffectRecorder();

        #region 日志文本主题配色（浅色维持原方案，深色使用高亮变体）

        /// <summary>
        /// (浅色, 深色) 画刷对：标题、发送字符、接收字符、发送HEX、接收HEX
        /// </summary>
        private static readonly (SolidColorBrush light, SolidColorBrush dark)[] LogBrushPairs =
        {
            (System.Windows.Media.Brushes.DarkSlateGray, MakeLogBrush(0xB0, 0xBE, 0xC5)), // 标题/时间戳
            (System.Windows.Media.Brushes.DarkRed,       MakeLogBrush(0xFF, 0x8A, 0x80)), // 发送 字符
            (System.Windows.Media.Brushes.DarkGreen,     MakeLogBrush(0x81, 0xC7, 0x84)), // 接收 字符
            (System.Windows.Media.Brushes.IndianRed,     MakeLogBrush(0xFF, 0x9E, 0x80)), // 发送 HEX
            (System.Windows.Media.Brushes.ForestGreen,   MakeLogBrush(0xA5, 0xD6, 0xA7)), // 接收 HEX
        };

        private static SolidColorBrush MakeLogBrush(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }

        private static bool IsDarkLogTheme =>
            Tools.ThemeManager.Normalize(Tools.Global.setting.theme) == Tools.ThemeManager.DarkTheme;

        private static SolidColorBrush PickLogBrush(int index) =>
            IsDarkLogTheme ? LogBrushPairs[index].dark : LogBrushPairs[index].light;

        private static SolidColorBrush LogHeadBrush => PickLogBrush(0);
        private static SolidColorBrush LogCharBrush(bool send) => PickLogBrush(send ? 1 : 2);
        private static SolidColorBrush LogHexBrush(bool send) => PickLogBrush(send ? 3 : 4);

        /// <summary>
        /// 把已记录的画刷按映射表换算到目标主题，未匹配的（用户脚本自定义色等）原样保留
        /// </summary>
        private static System.Windows.Media.Brush MapLogBrush(System.Windows.Media.Brush brush, bool dark)
        {
            if (brush == null)
                return null;
            foreach (var pair in LogBrushPairs)
            {
                if (ReferenceEquals(brush, pair.light) || ReferenceEquals(brush, pair.dark))
                    return dark ? pair.dark : pair.light;
            }
            return brush;
        }

        /// <summary>
        /// 主题切换后重刷日志显示区配色：换算已显示行的画刷并重绘
        /// </summary>
        public void RefreshLogColors()
        {
            bool dark = IsDarkLogTheme;
            var effects = lineEffectsRecorder.lineEffect;
            for (int i = 0; i < effects.Length; i++)
            {
                effects[i].HeadColour = MapLogBrush(effects[i].HeadColour, dark);
                effects[i].ResColour = MapLogBrush(effects[i].ResColour, dark);
            }
            uartDataText.TextArea.TextView.Redraw();
        }

        #endregion
        /// <summary>
        /// 一次性初始化标记：Loaded 会在主题切换（OnApplyTemplate 重建窗口模板）时再次触发
        /// </summary>
        private bool pageInitialized = false;

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //添加待显示数据到缓冲区
            //注意：Loaded 可能重复触发（如主题切换），事件必须先退订再订阅，避免重复处理
            Tools.Logger.DataShowTask -= DataShowTask;
            Tools.Logger.DataShowTask += DataShowTask;
            Tools.Logger.DataClearEvent -= OnDataClear;
            Tools.Logger.DataClearEvent += OnDataClear;

            if (pageInitialized)
                return;
            pageInitialized = true;

            LockIcon.DataContext = this;
            UnLockIcon.DataContext = this;
            SearchPanel.Install(uartDataText.TextArea);
            uartDataText.TextArea.TextView.LineTransformers.Add(new LineEffectRenderer(lineEffectsRecorder));
            uartDataText.TextArea.TextView.LineTransformers.Add(fontScaler);

            // 禁用不必要的插件（如行号缩进计算）
            uartDataText.Options.EnableHyperlinks = false;
            uartDataText.Options.EnableEmailHyperlinks = false;

            // 如果不需要语法高亮，直接关闭
            uartDataText.SyntaxHighlighting = null;

            HexShowCheckBox.DataContext = Tools.Global.setting;
            HexSendCheckBox.DataContext = Tools.Global.setting;
            SubpackageShowCheckBox.DataContext = Tools.Global.setting;
            ShowSendCheckBox.DataContext = Tools.Global.setting;
            DisableLogCheckBox.DataContext = Tools.Global.setting;
        }

        /// <summary>
        /// 清空日志显示（独立方法以便退订，防止重复订阅）
        /// </summary>
        private void OnDataClear(object sender, EventArgs e)
        {
            LogCount = 0;
            uartDataText.Document.Text = string.Empty;
            uartDataText.ScrollToHome();
            lineEffectsRecorder.Clear();
        }

        /// <summary>
        /// 插入显示文本
        /// </summary>
        public void AppendText(string AppendText, byte eTextSize, byte eHeadLen, System.Windows.Media.Brush eHeadColour, System.Windows.Media.Brush eResColour)
        {
            uartDataText.Document.BeginUpdate();
            try
            {
                int StartLine = uartDataText.Document.LineCount;
                uartDataText.AppendText(AppendText);
                int EndLine = uartDataText.Document.LineCount;

                lineEffectsRecorder[StartLine, eTextSize, eHeadLen, eHeadColour, eResColour] = default;
                for (int Line = StartLine + 1; Line < EndLine; Line++)
                    lineEffectsRecorder[Line, eTextSize, 0, eHeadColour, eResColour] = default;
            }
            finally
            {
                uartDataText.Document.EndUpdate();
            }
        }

        /// <summary>
        /// AvalonEdit行效果记录器
        /// </summary>
        public class SimpleLineEffectRecorder
        {
            public struct LineEffect
            {
                public byte HeadLen { get; set; }
                public byte TextSize { get; set; }
                public System.Windows.Media.Brush HeadColour { get; set; }
                public System.Windows.Media.Brush ResColour { get; set; }

                public void Set(byte eTextSize, byte eHeadLen, System.Windows.Media.Brush eHeadColour, System.Windows.Media.Brush eResColour)
                {
                    TextSize = eTextSize;
                    HeadLen = eHeadLen;
                    HeadColour = eHeadColour;
                    ResColour = eResColour;
                }
            }

            public LineEffect[] lineEffect = new LineEffect[1024];
            public LineEffect this[int line, byte eTextSize, byte eHeadLen, System.Windows.Media.Brush eHeadColour, System.Windows.Media.Brush  eResColour]
            {
                set
                {
                    if (line + 10 >= lineEffect.Length)
                        Array.Resize(ref lineEffect, Math.Max(lineEffect.Length + 1024, line + 1024));
                    lineEffect[line].TextSize = eTextSize;
                    lineEffect[line].HeadLen = eHeadLen;
                    lineEffect[line].HeadColour = eHeadColour;
                    lineEffect[line].ResColour = eResColour;
                }
            }

            public void Clear()
            {
                Array.Clear(lineEffect, 0, lineEffect.Length);
                Array.Resize(ref lineEffect, 1024);
            }
        }

        /// <summary>
        /// AvalonEdit行效果渲染器
        /// </summary>
        public class LineEffectRenderer : DocumentColorizingTransformer
        {
            private readonly SimpleLineEffectRecorder _effect;
            public LineEffectRenderer(SimpleLineEffectRecorder LineEffect)
            {
                _effect = LineEffect;
            }

            protected override void ColorizeLine(DocumentLine line)
            {
                var effect = _effect.lineEffect[line.LineNumber];
                if (effect.HeadLen > 0)
                {
                    int headEndOffset = Math.Min(line.Offset + effect.HeadLen, line.EndOffset);
                    ChangeLinePart(line.Offset, headEndOffset, visualLine =>
                    {
                        var props = visualLine.TextRunProperties;
                        props.SetForegroundBrush(effect.HeadColour);
                        props.SetFontRenderingEmSize(effect.TextSize);
                    });
                }

                int resStartOffset = Math.Min(line.Offset + effect.HeadLen, line.EndOffset);
                if (resStartOffset < line.EndOffset)
                {
                    ChangeLinePart(resStartOffset, line.EndOffset, visualLine =>
                    {
                        var props = visualLine.TextRunProperties;
                        props.SetForegroundBrush(effect.ResColour);
                        props.SetFontRenderingEmSize(effect.TextSize);
                    });
                }
            }
        }

        /// <summary>
        /// AvalonEdit滚轮缩放事件
        /// </summary>
        FontScalingTransformer fontScaler = new FontScalingTransformer();
        private void uartDataText_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                e.Handled = true;
                double scale = e.Delta > 0 ? 1.1 : 0.9;
                fontScaler.FontScale *= scale;
                fontScaler.FontScale = Math.Max(0.7, Math.Min(2, fontScaler.FontScale));
                uartDataText.TextArea.TextView.Redraw();
            }
        }

        /// <summary>
        /// AvalonEdit字体缩放效果渲染器
        /// </summary>
        public sealed class FontScalingTransformer : DocumentColorizingTransformer
        {
            public double FontScale { get; set; } = 1.0;

            protected override void ColorizeLine(DocumentLine line)
            {
                int startOffset = line.Offset;
                int endOffset = line.EndOffset;

                ChangeLinePart(startOffset, endOffset, element =>
                {
                    if (element.TextRunProperties.Typeface != null)
                    {
                        element.TextRunProperties.SetFontRenderingEmSize(
                        element.TextRunProperties.FontRenderingEmSize * FontScale
                        );
                    }
                });
            }
        }

        /// <summary>
        /// 分发显示数据的任务
        /// </summary>
        private void DataShowTask(object sender, Tools.DataShow e)
        {
            string DataTitle = "";
            string DataChar = "";
            string DataHex = "";
            SolidColorBrush CharColor;
            SolidColorBrush HexColor;
            var DataPack = e as Tools.DataShowPara;

            Dispatcher.Invoke(() =>
            {
                // 数据包为空或数据为空时直接丢弃，避免后续裸取 Length 抛 NRE
                if (DataPack == null || DataPack.data == null)
                    return;

                if (LogCount >= Tools.Global.setting.maxLogAutoClear)
                {
                    LogCount = 0;
                    uartDataText.Document.Text = string.Empty;
                    uartDataText.ScrollToHome();
                    lineEffectsRecorder.Clear();
                }
                LogCount += DataPack.data.Length;

                if (!DataPack.send)
                {
                    try
                    {
                        DataPack.data = LuaEnv.LuaLoader.Run(
                            $"{Tools.Global.setting.recvScript}.lua",
                            new System.Collections.ArrayList { "uartData", DataPack.data },
                            "user_script/user_script_recv_convert/");
                    }
                    catch (Exception ex)
                    {
                        Tools.MessageBox.Show($"receive convert lua script error\r\n" + ex.ToString());
                        return;
                    }
                    if (DataPack.data == null)
                        return;
                }
                else if (!Tools.Global.setting.showSend)
                {
                    return;
                }

                CharColor = LogCharBrush(DataPack.send);
                HexColor = LogHexBrush(DataPack.send);

                if (uartDataText.Document.TextLength == 0)
                    sendPre = DataPack.send;

                if (Tools.Global.setting.subpackageShow == 1)
                {
                    if (sendPre != DataPack.send)
                        uartDataText.AppendText(Environment.NewLine);

                    if (Tools.Global.setting.showHexFormat == 2)
                    {
                        DataHex = Tools.Global.Byte2Hex(DataPack.data, " ", DataPack.data.Length);
                        AppendText(DataHex, 14, 0, LogHeadBrush, HexColor);
                    }
                    else
                    {
                        DataChar = Tools.Global.Byte2Readable(DataPack.data, DataPack.data.Length);
                        AppendText(DataChar, 14, 0, LogHeadBrush, CharColor);
                    }
                    newEmptyLine = true;
                }
                else
                {
                    if (newEmptyLine == true)
                        uartDataText.AppendText(Environment.NewLine);
                    newEmptyLine = false;

                    if (Tools.Global.setting.subpackageShow == 0)
                        DataTitle = DataPack.time.ToString("[yyyy/MM/dd HH:mm:ss.fff]") + (DataPack.send ? " » " : " « ");
                    else
                        DataTitle = (DataPack.send ? "» " : "« ");

                    if (Tools.Global.setting.showHexFormat != 2)
                    {
                        DataChar = Tools.Global.Byte2Readable(DataPack.data, DataPack.data.Length);
                        DataChar += ((DataChar[DataChar.Length - 1] == '\r') || (DataChar[DataChar.Length - 1] == '\n'))?"": Environment.NewLine;
                        AppendText(DataTitle + DataChar, 14, (byte)DataTitle.Length, LogHeadBrush, CharColor);

                        if(Tools.Global.setting.showHexFormat == 0)
                        {
                            DataHex = "HEX " + Tools.Global.Byte2Hex(DataPack.data, " ", DataPack.data.Length) + Environment.NewLine + Environment.NewLine;
                            AppendText(DataHex, 11, 0, LogHeadBrush, HexColor);
                        }
                    }
                    else if (Tools.Global.setting.showHexFormat != 1)
                    {
                        DataHex += Tools.Global.Byte2Hex(DataPack.data, " ", DataPack.data.Length) + Environment.NewLine;
                        AppendText(DataTitle + DataHex, 14, (byte)DataTitle.Length, LogHeadBrush, HexColor);
                    }

                    //有不显示的情况
                }
                sendPre = DataPack.send;

                if (!LockLog)
                    uartDataText.ScrollToEnd();
            });
        }

        private void LockLogButton_Click(object sender, RoutedEventArgs e)
        {
            LockLog = !LockLog;
        }
    }
}
