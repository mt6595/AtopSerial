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
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //添加待显示数据到缓冲区
            Tools.Logger.DataShowTask += DataShowTask;
            Tools.Logger.DataClearEvent += (xx,x) =>
            {
                LogCount = 0;
                uartDataText.Document.Text = string.Empty;
                uartDataText.ScrollToHome();
                lineEffectsRecorder.Clear();
            };
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

                CharColor = DataPack.send ? System.Windows.Media.Brushes.DarkRed : System.Windows.Media.Brushes.DarkGreen;
                HexColor = DataPack.send ? System.Windows.Media.Brushes.IndianRed : System.Windows.Media.Brushes.ForestGreen;

                if (uartDataText.Document.TextLength == 0)
                    sendPre = DataPack.send;

                if (Tools.Global.setting.subpackageShow == 1)
                {
                    if (sendPre != DataPack.send)
                        uartDataText.AppendText(Environment.NewLine);

                    if (Tools.Global.setting.showHexFormat == 2)
                    {
                        DataHex = Tools.Global.Byte2Hex(DataPack.data, " ", DataPack.data.Length);
                        AppendText(DataHex, 14, 0, System.Windows.Media.Brushes.DarkSlateGray, HexColor);
                    }
                    else
                    {
                        DataChar = Tools.Global.Byte2Readable(DataPack.data, DataPack.data.Length);
                        AppendText(DataChar, 14, 0, System.Windows.Media.Brushes.DarkSlateGray, CharColor);
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
                        AppendText(DataTitle + DataChar, 14, (byte)DataTitle.Length, System.Windows.Media.Brushes.DarkSlateGray, CharColor);

                        if(Tools.Global.setting.showHexFormat == 0)
                        {
                            DataHex = "HEX " + Tools.Global.Byte2Hex(DataPack.data, " ", DataPack.data.Length) + Environment.NewLine + Environment.NewLine;
                            AppendText(DataHex, 11, 0, System.Windows.Media.Brushes.DarkSlateGray, HexColor);
                        }
                    }
                    else if (Tools.Global.setting.showHexFormat != 1)
                    {
                        DataHex += Tools.Global.Byte2Hex(DataPack.data, " ", DataPack.data.Length) + Environment.NewLine;
                        AppendText(DataTitle + DataHex, 14, (byte)DataTitle.Length, System.Windows.Media.Brushes.DarkSlateGray, HexColor);
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
