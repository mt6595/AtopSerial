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
        /// <summary>
        /// 禁止自动滚动？
        /// </summary>
        public bool LockLog { get; set; } = false;
        private bool loaded = false;
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (loaded)
                return;
            loaded = true;
            //使日志富文本区域滚动可控制
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

            HEXBox.DataContext = Tools.Global.setting;
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
            var DataTemp = e as Tools.DataShowPara;
            if (LogCount >= Tools.Global.setting.maxLogAutoClear)
            {
                LogCount = 0;
                Dispatcher.Invoke(() =>
                {
                    if (uartDataFlowDocument.Document.Blocks.FirstBlock is Paragraph uartDataParagraph)
                        uartDataParagraph.Inlines.Clear();
                });
            }
            LogCount += DataTemp.data.Length;

            //显示数据
            Dispatcher.Invoke(() =>
            {
                if (uartDataFlowDocument.IsMouseOver)
                    uartDataFlowDocument.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));
                addUartLog(new DataUart(DataTemp.data, DataTemp.time, DataTemp.send));

                if (!LockLog)//如果允许拉到最下面
                    sv.ScrollToBottom();
            });
        }

        class DataUart
        {
            public string time;
            public string title;
            public string data = null;
            public string hex = null;
            public SolidColorBrush color;
            public SolidColorBrush hexColor;

            public DataUart(byte[] data, DateTime time, bool sent)
            {
                //转换下接收数据
                if (!sent)
                {
                    try
                    {
                        data = LuaEnv.LuaLoader.Run(
                            $"{Tools.Global.setting.recvScript}.lua",
                            new System.Collections.ArrayList { "uartData", data},
                            "user_script/user_script_recv_convert/");
                    }
                    catch (Exception ex) 
                    {
                        Tools.MessageBox.Show($"receive convert lua script error\r\n" + ex.ToString());
                        return;
                    }
                    if (data == null)
                        return;
                }
                else if (!Tools.Global.setting.showSend)//显示发送出去的串口数据
                {
                    return;
                }

                this.time = time.ToString("[yyyy/MM/dd HH:mm:ss.fff]");
                title = sent ? " » " : " « ";
                color = sent ? Brushes.DarkRed : Brushes.DarkGreen;
                hexColor = sent ? Brushes.IndianRed : Brushes.ForestGreen;

                //主要数据
                this.data = Tools.Global.setting.showHexFormat switch
                {
                    2 => Tools.Global.Byte2Hex(data, " ", data.Length),
                    _ => Tools.Global.Byte2Readable(data, data.Length),
                };

                //同时显示模式时，才显示小字hex
                if (Tools.Global.setting.showHexFormat == 0)
                    hex = "HEX:" + Tools.Global.Byte2Hex(data, " ", data.Length);
            }
        }

        /// <summary>
        /// 添加串口日志数据
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="send">true为发送，false为接收</param>
        private void addUartLog(DataUart dataUart)
        {
            if (Tools.Global.setting.subpackageShow == true)
            {
                Span text = new Span(new Run(dataUart.time+dataUart.title));
                text.Foreground = Brushes.DarkSlateGray;
                (uartDataFlowDocument.Document.Blocks.FirstBlock as Paragraph).Inlines.Add(text);

                //主要显示数据
                if (dataUart.data != null)
                {
                    var lastChar = dataUart.data.Substring(dataUart.data.Length - 1);
                    if (lastChar != "\r" && lastChar != "\n")
                    {
                        dataUart.data = dataUart.data + "\r";
                    }

                    if (dataUart.hex == null)
                        dataUart.data = dataUart.data + "\r";

                    text = new Span(new Run(dataUart.data));
                    text.FontSize = 15;
                    text.Foreground = dataUart.color;
                    (uartDataFlowDocument.Document.Blocks.FirstBlock as Paragraph).Inlines.Add(text);
                }

                //同时显示模式时，才显示小字hex
                if (dataUart.hex != null)
                {
                    dataUart.hex = dataUart.hex + "\r\r";
                    text = new Span(new Run(dataUart.hex));
                    text.Foreground = dataUart.hexColor;
                    (uartDataFlowDocument.Document.Blocks.FirstBlock as Paragraph).Inlines.Add(text);
                }
            }
            else//不分包
            {
                //待显示的数据
                string stringTemp;
                if (Tools.Global.setting.showHexFormat == 2 && dataUart.hex != null)
                    stringTemp = dataUart.hex;
                else
                    stringTemp = dataUart.data;
                Span text = new Span(new Run(stringTemp));
                text.FontSize = 15;
                text.Foreground = dataUart.color;
                (uartDataFlowDocument.Document.Blocks.FirstBlock as Paragraph).Inlines.Add(text);
            }
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
