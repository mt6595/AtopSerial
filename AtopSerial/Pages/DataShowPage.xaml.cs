using AtopSerial.Tools;
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

        ScrollViewer sv;
        private int LogCount = 0;
        public bool LockLog { get; set; } = false;
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            sv = uartDataFlowDocument.Template.FindName("PART_ContentHost", uartDataFlowDocument) as ScrollViewer;
            sv.CanContentScroll = true;
            //添加待显示数据到缓冲区
            Tools.Logger.DataShowTask += DataShowTask;
            Tools.Logger.DataClearEvent += (xx,x) =>
            {
                LogCount = 0;
                if (uartDataFlowDocument.Document.Blocks.FirstBlock is Paragraph uartDataParagraph)
                    uartDataParagraph.Inlines.Clear();
            };
            LockIcon.DataContext = this;
            UnLockIcon.DataContext = this;

            HexShowCheckBox.DataContext = Tools.Global.setting;
            HexSendCheckBox.DataContext = Tools.Global.setting;
            SubpackageShowCheckBox.DataContext = Tools.Global.setting;
            ShowSendCheckBox.DataContext = Tools.Global.setting;
            DisableLogCheckBox.DataContext = Tools.Global.setting;
            uartDataFlowDocument.Document.Blocks.Add(new Paragraph(new Run("")));
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
                    if (uartDataFlowDocument.Document.Blocks.FirstBlock is Paragraph uartDataParagraph)
                        uartDataParagraph.Inlines.Clear();
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

                if (uartDataFlowDocument.IsMouseOver)
                    uartDataFlowDocument.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));

                if (Tools.Global.setting.subpackageShow == 1)
                {
                    if (Tools.Global.setting.showHexFormat == 2)
                    {
                        DataHex = Tools.Global.Byte2Hex(DataPack.data, " ", DataPack.data.Length);
                        Span text = new Span(new Run(DataHex));
                        text.FontSize = 12;
                        text.Foreground = HexColor;
                        (uartDataFlowDocument.Document.Blocks.FirstBlock as Paragraph).Inlines.Add(text);
                    }
                    else
                    {
                        DataChar = Tools.Global.Byte2Readable(DataPack.data, DataPack.data.Length);
                        Span text = new Span(new Run(DataChar));
                        text.FontSize = 15;
                        text.Foreground = CharColor;
                        (uartDataFlowDocument.Document.Blocks.FirstBlock as Paragraph).Inlines.Add(text);
                    }
                }
                else
                {
                    if (Tools.Global.setting.subpackageShow == 0)
                        DataTitle = DataPack.time.ToString("[yyyy/MM/dd HH:mm:ss.fff]");
                    else
                        DataTitle = "";
                    DataTitle += DataPack.send ? " » " : " « ";
                    Span text = new Span(new Run(DataTitle));
                    text.Foreground = Brushes.DarkSlateGray;
                    (uartDataFlowDocument.Document.Blocks.FirstBlock as Paragraph).Inlines.Add(text);

                    if (Tools.Global.setting.showHexFormat != 2)
                    {
                        DataChar = Tools.Global.Byte2Readable(DataPack.data, DataPack.data.Length);
                        var lastChar = DataChar.Substring(DataChar.Length - 1);
                        if (lastChar != "\r" && lastChar != "\n")
                            DataChar = DataChar + "\r";
                        text = new Span(new Run(DataChar));
                        text.FontSize = 15;
                        text.Foreground = CharColor;
                        (uartDataFlowDocument.Document.Blocks.FirstBlock as Paragraph).Inlines.Add(text);
                        DataHex = "HEX:";
                    }
                    else
                        DataHex = "";

                    if (Tools.Global.setting.showHexFormat != 1)
                    {
                        DataHex += Tools.Global.Byte2Hex(DataPack.data, " ", DataPack.data.Length)+ "\r";
                        text = new Span(new Run(DataHex));
                        if (Tools.Global.setting.showHexFormat != 2)
                        {
                            DataHex = DataHex + "\r";
                        }
                        text.FontSize = 12;
                        text = new Span(new Run(DataHex));
                        text.Foreground = HexColor;
                        (uartDataFlowDocument.Document.Blocks.FirstBlock as Paragraph).Inlines.Add(text);
                    }
                }

                if (!LockLog)//如果允许拉到最下面
                    sv.ScrollToBottom();
            });
        }

        private void LockLogButton_Click(object sender, RoutedEventArgs e)
        {
            LockLog = !LockLog;
        }

        private void uartDataFlowDocument_MouseLeave(object sender, MouseEventArgs e)
        {
            uartDataFlowDocument.IsEnabled = true;
        }
    }
}
