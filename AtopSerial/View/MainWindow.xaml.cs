using FontAwesome.WPF;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Search;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using AtopSerial.Model;
using System.Text.RegularExpressions;
using AtopSerial.Tools;
using AtopSerial.Pages;
using ICSharpCode.AvalonEdit.Folding;
using RestSharp;
using System.Threading;
using System.Windows.Interop;
using CoAP;
using ScottPlot.Statistics;
using System.Collections;
using System.Web.UI;
using System.Collections.Specialized;
using ScottPlot;
using XLua;
using System.Reflection;
using System.Xml.Linq;
using System.Windows.Markup;

namespace AtopSerial
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Tools.Global.LoadSetting();
            if (Tools.Global.setting.windowHeight != 0 &&
                Tools.Global.setting.windowLeft > 0 &&
                Tools.Global.setting.windowTop > 0 &&
                Tools.Global.setting.windowTop < SystemParameters.FullPrimaryScreenHeight &&
                Tools.Global.setting.windowLeft < SystemParameters.FullPrimaryScreenWidth)
            {
                this.Left = Tools.Global.setting.windowLeft;
                this.Top = Tools.Global.setting.windowTop;
                this.Width = Tools.Global.setting.windowWidth;
                this.Height = Tools.Global.setting.windowHeight;
            }
            Tools.Global.setting.logScreenWidth = SystemParameters.FullPrimaryScreenWidth - 20;
        }

        private static bool fileLoadingRunScript = false;
        private static string lastFileRunScript = "";
        private static string lastFileDataMonitor = "";
        private static string lastFileGraphTool = "";
        private static DateTime lastFileTimeRunScript = DateTime.Now;
        private static DateTime lastChangeTimeRunScript = DateTime.Now;
        private static DateTime lastFileTimeDataMonitor = DateTime.Now;
        private static DateTime lastChangeTimeDataMonitor = DateTime.Now;
        private static DateTime lastFileTimeGraphTool = DateTime.Now;
        private static DateTime lastChangeTimeGraphTool = DateTime.Now;

        ObservableCollection<ToSendData> toSendListItems = new ObservableCollection<ToSendData>();
        ObservableCollection<ToDataMonitorItems> toMonitorItems = new ObservableCollection<ToDataMonitorItems>();
        ObservableCollection<ToSnatchGraphItems> toGraphItems = new ObservableCollection<ToSnatchGraphItems>();

        public static AtopSerial.ToDataMonitor toDataMonitor = new AtopSerial.ToDataMonitor();
        public static AtopSerial.ToGraphTool toGraphTool = new AtopSerial.ToGraphTool();
        private bool forcusClosePort = true;
        private bool canSaveSendList = true;
        private bool isOpeningPort = false;
        private bool sendListHexPickOn = true;
        static CancellationTokenSource autoSendTaskTokenSource;
        static Thread autoSendThread;
        object lockSendUartDataObject = new object();
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //延迟启动，加快软件第一屏出现速度
            Task.Run(() =>
            {
                    this.Dispatcher.Invoke(new Action(delegate {
                    //接收到、发送数据成功回调
                    Tools.Global.uart.UartDataRecived += Uart_UartDataRecived;
                    Tools.Global.uart.UartDataSent += Uart_UartDataSent;

                    //初始化所有数据
                    Tools.Global.Initial();

                    //窗口置顶事件
                    Tools.Global.setting.MainWindowTop += new EventHandler(topEvent);

                    //收发数据显示页面
                    dataShowFrame.Navigate(new Uri("Pages/DataShowPage.xaml", UriKind.Relative));

                    //绘制曲线
                    PlotFrame.Navigate(new Uri("Pages/PlotPage.xaml", UriKind.Relative));
                    
                    //加载初始波特率
                    var baudRateTemp = Tools.Global.setting.baudRate.ToString();
                    if(baudRateComboBox.Items.Contains(baudRateTemp))
                        baudRateComboBox.Text = Tools.Global.setting.baudRate.ToString();
                    else
                    {
                        baudRateComboBox.Items[baudRateComboBox.Items.Count - 1] = baudRateTemp;
                        baudRateComboBox.Text = baudRateTemp;
                    }

                    // 绑定事件监听,用于监听HID设备插拔
                    (PresentationSource.FromVisual(this) as HwndSource)?.AddHook(WndProc);

                    //刷新设备列表
                    refreshPortList();

                    //绑定数据
                    this.DataContext = Tools.Global.setting;
                    this.graphSwIcon.DataContext = Tools.Global.setting;
                    this.dataShowFrame.DataContext = Tools.Global.setting;
                    this.PlotFrame.DataContext = Tools.Global.setting;
                    this.toSendDataTextBox.DataContext = Tools.Global.setting;
                    this.dataMonitorSelectShowIcon.DataContext = AtopSerial.MainWindow.toDataMonitor;
                    this.dataMonitorAllSelectIcon.DataContext = AtopSerial.MainWindow.toDataMonitor;
                    this.graphToolSampleLenTextBox.DataContext = AtopSerial.MainWindow.toGraphTool;
                    this.graphToolSampleIntervalTextBox.DataContext = AtopSerial.MainWindow.toGraphTool;
                    this.graphToolTriggerParaTextBox.DataContext = AtopSerial.MainWindow.toGraphTool;
                    this.graphToolTriggerLagTextBox.DataContext = AtopSerial.MainWindow.toGraphTool;
                    this.graphToolModeComboBox.DataContext = AtopSerial.MainWindow.toGraphTool;
                    this.sentCountTextBlock.DataContext = Tools.Global.setting;
                    this.receivedCountTextBlock.DataContext = Tools.Global.setting;
                    this.toSendList.ItemsSource = toSendListItems;
                    this.toSnatchGraphList.ItemsSource = toGraphItems;
                    this.toDataMonitorList.ItemsSource = toMonitorItems;
                    QuiclListName0.DataContext = Tools.Global.setting;
                    QuiclListName1.DataContext = Tools.Global.setting;
                    QuiclListName2.DataContext = Tools.Global.setting;
                    QuiclListName3.DataContext = Tools.Global.setting;
                    QuiclListName4.DataContext = Tools.Global.setting;
                    QuiclListName5.DataContext = Tools.Global.setting;
                    QuiclListName6.DataContext = Tools.Global.setting;
                    QuiclListName7.DataContext = Tools.Global.setting;
                    QuiclListName8.DataContext = Tools.Global.setting;
                    QuiclListName9.DataContext = Tools.Global.setting;

                    //初始化快捷发送栏的数据
                    LoadQuickSendList();

                    //初始化数据监测区的数据
                    LoadDataMonitorList();
                         
                    //初始化抓图工具区的数据
                    LoadSnatchGraphList();

                    //运行脚本的Text编辑器
                    SearchPanel.Install(textEditorRunScript.TextArea);
                    string runScriptStreamName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + ".Lua.xshd";
                    System.Reflection.Assembly runScriptAssembly = System.Reflection.Assembly.GetExecutingAssembly();
                    using (System.IO.Stream s = runScriptAssembly.GetManifestResourceStream(runScriptStreamName))
                    {
                        using (XmlTextReader reader = new XmlTextReader(s))
                        {
                            var xshd = HighlightingLoader.LoadXshd(reader);
                            textEditorRunScript.SyntaxHighlighting = HighlightingLoader.Load(xshd, HighlightingManager.Instance);
                        }
                    }

                    //数据监控区的Text编辑器
                    SearchPanel.Install(dataMonitorScriptEdit.TextArea);
                    string dataMonitorStreamName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + ".Lua.xshd";
                    System.Reflection.Assembly dataMonitorAssembly = System.Reflection.Assembly.GetExecutingAssembly();
                    using (System.IO.Stream s = dataMonitorAssembly.GetManifestResourceStream(dataMonitorStreamName))
                    {
                        using (XmlTextReader reader = new XmlTextReader(s))
                        {
                            var xshd = HighlightingLoader.LoadXshd(reader);
                            dataMonitorScriptEdit.SyntaxHighlighting = HighlightingLoader.Load(xshd, HighlightingManager.Instance);
                        }
                    }

                    //抓图工具区的Text编辑器
                    SearchPanel.Install(graphToolScriptEdit.TextArea);
                    string graphToolStreamName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + ".Lua.xshd";
                    System.Reflection.Assembly graphToolAssembly = System.Reflection.Assembly.GetExecutingAssembly();
                    using (System.IO.Stream s = graphToolAssembly.GetManifestResourceStream(graphToolStreamName))
                    {
                        using (XmlTextReader reader = new XmlTextReader(s))
                        {
                            var xshd = HighlightingLoader.LoadXshd(reader);
                            graphToolScriptEdit.SyntaxHighlighting = HighlightingLoader.Load(xshd, HighlightingManager.Instance);
                        }
                    }

                    //加载脚本文件到运行脚本区
                    LoadFileRunScript(Tools.Global.setting.runScript);

                    //加载脚本文件到数据监测区
                    LoadFileDataMonitor();

                    //加载脚本文件到抓图工具区
                    LoadFileGraphTool();

                    //加载lua日志打印事件
                    LuaEnv.LuaApis.PrintLuaLog += LuaApis_PrintLuaLog;

                    //lua代码出错/结束运行事件
                    LuaEnv.LuaRunEnv.LuaRunError += LuaRunEnv_LuaRunError;

                    //lua获取抓图参数事件
                    LuaEnv.LuaApis.EnvLuaGraphSnatchPara += luaGraphSnatchPara;

                    //lua获取抓图通道参数事件
                    LuaEnv.LuaApis.EnvLuaGraphChannelPara += luaGraphChannelPara;

                    //lua获取数据监测采集使能事件
                    LuaEnv.LuaApis.EnvLuaDataMonitorPara += luaDataMonitorPara;

                    //lua设置数据监测数值返回事件
                    LuaEnv.LuaApis.EnvLuaDataMonitorValue += luaDataMonitorValue;

                    //lua设置数据监测数值记录
                    LuaEnv.LuaApis.EnvLuaDataMonitorRecord += luaDataMonitorRecord;

                    new Thread(LuaLogPrintTask).Start();
                    RenderDataSendGrid();

                    //加载完了，可以允许点击
                    MainGrid.IsEnabled = true;

                    //更换标题栏
                    var title = "";
                    title = this.Title;
                    Tools.Global.ChangeTitleEvent += (n, s) =>
                    {
                        this.Dispatcher.Invoke(() => this.Title = title + s);
                    };

                    Tools.Global.RefreshLuaScriptListEvent += (n, s) =>
                    {
                        this.Dispatcher.Invoke(() => RefreshLuaSendList());
                    };
                }));
            });
        }

        public void RenderDataSendGrid()
        {
            this.Dispatcher.Invoke(new Action(delegate
            {
                Grid graphTextGrid = FindName("GraphTextGrid") as Grid;
                Grid dataSendGrid = FindName("DataSendGrid") as Grid;
                GridSplitter gridSplitter = FindName("MainGridSplitter") as GridSplitter;

                if (Tools.Global.setting.extendDataSendGrid == true)
                {
                    Grid.SetRowSpan(graphTextGrid, 1);
                    Grid.SetColumnSpan(dataSendGrid, 3);
                    Grid.SetRowSpan(gridSplitter, 1);
                    sendDataButton.Margin = new Thickness(0, 0, 5, 0);
                }
                else
                {
                    Grid.SetRowSpan(graphTextGrid, 2);
                    Grid.SetColumnSpan(dataSendGrid, 1);
                    Grid.SetRowSpan(gridSplitter, 2);
                    sendDataButton.Margin = new Thickness(0, 0, 0, 0);
                }
            }));
        }

        private bool DoInvoke(Action action)
        {
            if (Tools.Global.isMainWindowsClosed)
                return false;
            Dispatcher.Invoke(action);
            return true;
        }

        /// <summary>
        /// 加载快捷发送区数据
        /// </summary>
        private void LoadQuickSendList()
        {
            canSaveSendList = false;
            ToSendData.DataChanged += QuickSendListSave;
            if (Global.setting.quickSendSelect == -1)
                Global.setting.quickSendSelect = 0;

            if (Tools.Global.setting.quickSend.Count == 0)
            {
                Tools.Global.setting.quickSend = new List<ToSendData>
                        {
                            new ToSendData{id = 1,text="",hex=false},
                            new ToSendData{id = 2,text="",hex=false},
                            new ToSendData{id = 3,text="",hex=false},
                            new ToSendData{id = 4,text="",hex=false},
                            new ToSendData{id = 5,text="",hex=false},
                            new ToSendData{id = 6,text="",hex=false},
                            new ToSendData{id = 7,text="",hex=false},
                            new ToSendData{id = 8,text="",hex=false},
                            new ToSendData{id = 9,text="",hex=false},
                            new ToSendData{id = 10,text="",hex=false},
                        };
            }
            foreach (var iTemp in Tools.Global.setting.quickSend)
            {
                if (iTemp.commit == null)
                    iTemp.commit = TryFindResource("QuickSendButton") as string ?? "?!";
                if (toSendListItems.Count < 200)
                {
                    toSendListItems.Add(iTemp);
                }
            }
            CheckToSendListId();
            QuickListPageTextBlock.Text = Global.setting.GetQuickListNameNow();
            canSaveSendList = true;QuickSendListSave(null, EventArgs.Empty);
            QuickSendListSave(null, EventArgs.Empty);
        }
        
        /// <summary>
        /// 加载数据监测区数据
        /// </summary>
        private void LoadDataMonitorList()
        {
            if (Tools.Global.setting.dataMonitorListNum < 0) Tools.Global.setting.dataMonitorListNum = 10;
            if (Tools.Global.setting.dataMonitorListNum > 200) Tools.Global.setting.dataMonitorListNum = 200;
            if (Tools.Global.setting.toDataMonitorItemsSave.Count < Tools.Global.setting.dataMonitorListNum)
            {
                for (var iTemp = Tools.Global.setting.toDataMonitorItemsSave.Count; iTemp < Tools.Global.setting.dataMonitorListNum; iTemp++)
                {
                    Tools.Global.setting.toDataMonitorItemsSave.Add(new ToDataMonitorItems { visibility = Visibility.Visible, channel = iTemp + 1, description = "", result = "", sampleEn = false });
                }
            }
            else if (Tools.Global.setting.toDataMonitorItemsSave.Count > Tools.Global.setting.dataMonitorListNum)
            {
                for (var iTemp = Tools.Global.setting.toDataMonitorItemsSave.Count; iTemp > Tools.Global.setting.dataMonitorListNum; iTemp--)
                {
                    Tools.Global.setting.toDataMonitorItemsSave.RemoveAt(Tools.Global.setting.toDataMonitorItemsSave.Count - 1);
                }
            }
            foreach (var iTemp in Tools.Global.setting.toDataMonitorItemsSave)
            {
                iTemp.result = "";
                toMonitorItems.Add(iTemp);
            }
            Tools.Global.setting.dataMonitorListNum = toMonitorItems.Count;
            ToDataMonitorItems.DataChanged += MonitorListSave;
            MonitorListSave(null, EventArgs.Empty);
        }
        /// <summary>
        /// 加载抓图工具区数据
        /// </summary>
        private void LoadSnatchGraphList()
        {
            if (Tools.Global.setting.snatchGraphListNum < 0) Tools.Global.setting.snatchGraphListNum = 10;
            if (Tools.Global.setting.snatchGraphListNum > 10) Tools.Global.setting.snatchGraphListNum = 10;
            if (Tools.Global.setting.toSnatchGraphItemsSave.Count < Tools.Global.setting.snatchGraphListNum)
            {
                for (var iTemp = Tools.Global.setting.toSnatchGraphItemsSave.Count; iTemp < Tools.Global.setting.snatchGraphListNum; iTemp++)
                {
                    Tools.Global.setting.toSnatchGraphItemsSave.Add(new ToSnatchGraphItems { channel = iTemp + 1, show = false, description = "", dataSour = "", scaleY = "1", offset = "0", type = 0 });
                }
            }
            else if (Tools.Global.setting.toSnatchGraphItemsSave.Count > Tools.Global.setting.snatchGraphListNum)
            {
                for (var iTemp = Tools.Global.setting.toSnatchGraphItemsSave.Count; iTemp > Tools.Global.setting.snatchGraphListNum; iTemp--)
                {
                    Tools.Global.setting.toSnatchGraphItemsSave.RemoveAt(Tools.Global.setting.toSnatchGraphItemsSave.Count - 1);
                }
            }
            foreach (var iTemp in Tools.Global.setting.toSnatchGraphItemsSave)
            {
                toGraphItems.Add(iTemp);
            }
            Tools.Global.setting.snatchGraphListNum = toGraphItems.Count;
            ToSnatchGraphItems.DataChanged += GraphListSave;
            GraphListSave(null, EventArgs.Empty);
        }

        private void Uart_UartDataSent(object sender, EventArgs e)
        {
            Tools.Logger.ShowData(sender as byte[], true);
        }

        private void Uart_UartDataRecived(object sender, EventArgs e)
        {
            Tools.Logger.ShowData(sender as byte[], false);
        }

        private bool refreshLock = false;
        /// <summary>
        /// 刷新设备列表
        /// </summary>
        private void refreshPortList(string lastPort = null)
        {
            if (refreshLock)
                return;
            refreshLock = true;
            serialPortsListComboBox.Items.Clear();
            List<string> strs = new List<string>();
            Task.Run(() =>
            {
                while(true)
                {
                    try
                    {
                        ManagementObjectSearcher searcher =new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PnPEntity");
                        Regex regExp = new Regex("\\(COM\\d+\\)");
                        foreach (ManagementObject queryObj in searcher.Get())
                        {
                            if ((queryObj["Caption"] != null) && regExp.IsMatch(queryObj["Caption"].ToString()))
                            {
                                strs.Add(queryObj["Caption"].ToString());
                            }
                        }
                        break;
                    }
                    catch
                    {
                        Task.Delay(500).Wait();
                    }
                }

                try
                {
                    foreach (string p in SerialPort.GetPortNames())//加上缺少的com口
                    {
                        //有些人遇到了微软库的bug，所以需要手动从0x00截断
                        var pp = p;
                        if (p.IndexOf("\0") > 0)
                            pp = p.Substring(0, p.IndexOf("\0"));
                        bool notMatch = true;
                        foreach (string n in strs)
                        {
                            if (n.Contains($"({pp})"))//如果和选中项目匹配
                            {
                                notMatch = false;
                                break;
                            }
                        }
                        if (notMatch)
                            strs.Add($"Serial Port {pp} ({pp})");//如果列表中没有，就自己加上
                    }
                }
                catch{ }


                this.Dispatcher.Invoke(new Action(delegate {
                    foreach (string i in strs)
                        serialPortsListComboBox.Items.Add(i);
                    if (strs.Count >= 1)
                    {
                        openClosePortButton.IsEnabled = true;
                        serialPortsListComboBox.SelectedIndex = 0;
                    }
                    else
                    {
                        openClosePortButton.IsEnabled = false;
                    }
                    refreshLock = false;

                    if (string.IsNullOrEmpty(lastPort))
                        lastPort = Tools.Global.uart.GetName();
                    //选定上次的com口
                    foreach (string c in serialPortsListComboBox.Items)
                    {
                        if (c.Contains($"({lastPort})"))
                        {
                            serialPortsListComboBox.Text = c;
                            //自动重连，不管结果
                            if (!forcusClosePort && Tools.Global.setting.autoReconnect && !isOpeningPort)
                            {
                                Task.Run(() =>
                                {
                                    isOpeningPort = true;
                                    try
                                    {
                                        Tools.Global.uart.Open();
                                        Dispatcher.Invoke(new Action(delegate
                                        {
                                            openClosePortTextBlock.Text = (TryFindResource("OpenPort_close") as string ?? "?!");
                                            serialPortsListComboBox.IsEnabled = false;
                                        }));
                                    }
                                    catch
                                    {
                                        //MessageBox.Show("串口打开失败！");
                                    }
                                    isOpeningPort = false;
                                });
                            }
                            break;
                        }
                    }
                }));
            });
        }

        private void RefreshLuaSendList()
        {
            //刷新文件列表
            DirectoryInfo luaFileDir = new DirectoryInfo(Tools.Global.ProfilePath + @"user_script\user_script_run\");
            FileSystemInfo[] luaFiles = luaFileDir.GetFileSystemInfos();
            fileLoadingRunScript = true;
            luaFileListSend.Items.Clear();
            for (int i = 0; i < luaFiles.Length; i++)
            {
                FileInfo file = luaFiles[i] as FileInfo;
                if (file != null && file.Name.ToLower().EndsWith(".lua"))
                {
                    string name = file.Name.Substring(0, file.Name.Length - 4);
                    luaFileListSend.Items.Add(name);
                    if (name == Tools.Global.setting.runScript)
                    {
                        luaFileListSend.SelectedIndex = luaFileListSend.Items.Count - 1;
                    }
                }
            }
            if (Tools.Global.setting.runScript == "")
            {
                luaFileListSend.SelectedIndex = 0;
                Tools.Global.setting.runScript = luaFileListSend.Text;
            }
            lastFileRunScript = Tools.Global.setting.runScript;
            fileLoadingRunScript = false;
        }

        private static int UsbPluginDeley = 0;
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x219 && !Tools.Global.uart.IsOpen())// 监听USB设备插拔消息
            {
                if (UsbPluginDeley == 0)
                {
                    ++UsbPluginDeley;   // Task启动需要准备时间,这里提前对公共变量加一
                    Task.Run(() =>
                    {
                        do Task.Delay(100).Wait();
                        while (++UsbPluginDeley < 10);
                        UsbPluginDeley = 0;
                        Dispatcher.Invoke(() =>
                        {
                            UsbDeviceNotifier_OnDeviceNotify();
                        });
                        //Logger.AddUartLogInfo($"[USB拔插事件] {DateTime.Now:HH:mm:ss.fff}");
                    });
                }
                else UsbPluginDeley = 1;
                handled = true;
            }
            return IntPtr.Zero;
        }
        private void UsbDeviceNotifier_OnDeviceNotify()
        {
            if (Tools.Global.uart.IsOpen())
            {
                refreshPortList();
                foreach (string c in serialPortsListComboBox.Items)
                {
                    if (c.Contains($"({Tools.Global.uart.GetName()})"))
                    {
                        serialPortsListComboBox.Text = c;
                        break;
                    }
                }
            }
            else
            {
                openClosePortTextBlock.Text = (TryFindResource("OpenPort_open") as string ?? "?!");
                serialPortsListComboBox.IsEnabled = true;
                refreshPortList();
            }
        }

        /// <summary>
        /// 响应其他代码传来的窗口置顶事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void topEvent(object sender, EventArgs e)
        {
            this.Topmost = (bool)sender;
        }
        
        /// <summary>
         /// 窗口关闭事件
         /// </summary>
         /// <param name="sender"></param>
         /// <param name="e"></param>
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Tools.Global.setting.windowLeft = this.Left;
            Tools.Global.setting.windowTop = this.Top;
            Tools.Global.setting.windowWidth = this.Width;
            Tools.Global.setting.windowHeight = this.Height;
            autoSendTaskTokenSource?.Cancel();
            //自动保存脚本
            if (lastFileRunScript != "")
                SaveFileRunScript(lastFileRunScript);
            Tools.Global.isMainWindowsClosed = true;
            foreach (Window win in App.Current.Windows)
            {
                if (win != this)
                {
                    win.Close();
                }
            }
            e.Cancel = false;//正常关闭
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Normal)
            {
                if (CardTabControl.ActualWidth < 350)
                {
                    CardTabControl.MinWidth = 350;
                }
            }
        }

        Window settingPage = new SettingWindow();
        private void MoreSettingButton_Click(object sender, RoutedEventArgs e)
        {
            settingPage.Show();
            settingPage.Focus();
        }

        private void ApiDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(Tools.Global.apiDocumentUrl);
        }

        private void OpenScriptFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", Tools.Global.ProfilePath + @"user_script\user_script_run");
            }
            catch
            {
                Tools.MessageBox.Show($"Failed to open the folder. Please open this path manually:{Tools.Global.ProfilePath}user_script/user_script_run");
            }
        }

        private void RefreshScriptListButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshLuaSendList();
        }

        private byte[] toSendData = null;//待发送的数据
        private void openPort()
        {
            //Tools.Logger.AddUartLogDebug($"[openPort]{isOpeningPort},{serialPortsListComboBox.SelectedItem}");
            if (isOpeningPort)
                return;
            if (serialPortsListComboBox.SelectedItem != null)
            {
                string[] ports;//获取所有串口列表
                try
                {
                    //Tools.Logger.AddUartLogDebug($"[openPort]GetPortNames");
                    ports = SerialPort.GetPortNames();
                    //Tools.Logger.AddUartLogDebug($"[openPort]GetPortNames{ports.Length}");
                }
                catch(Exception e)
                {
                    ports = new string[0];
                    //Tools.Logger.AddUartLogDebug($"[openPort]GetPortNames Exception:{e.Message}");
                }
                string port = "";//最终串口名
                foreach (string p in ports)//循环查找符合名称串口
                {
                    //有些人遇到了微软库的bug，所以需要手动从0x00截断
                    var pp = p;
                    if (p.IndexOf("\0") > 0)
                        pp = p.Substring(0, p.IndexOf("\0"));
                    if ((serialPortsListComboBox.SelectedItem as string).Contains($"({pp})"))//如果和选中项目匹配
                    {
                        port = pp;
                        break;
                    }
                }
                //Tools.Logger.AddUartLogDebug($"[openPort]PortName:{port},isOpeningPort:{isOpeningPort}");
                if (port != "")
                {
                    Task.Run(() =>
                    {
                        isOpeningPort = true;
                        try
                        {
                            forcusClosePort = false;//不再强制关闭串口
                            //Tools.Logger.AddUartLogDebug($"[openPort]SetName");
                            Tools.Global.uart.SetName(port);
                            //Tools.Logger.AddUartLogDebug($"[openPort]open");
                            Tools.Global.uart.Open();
                            //Tools.Logger.AddUartLogDebug($"[openPort]change show");
                            this.Dispatcher.Invoke(new Action(delegate
                            {
                                openClosePortTextBlock.Text = (TryFindResource("OpenPort_close") as string ?? "?!");
                                serialPortsListComboBox.IsEnabled = false;
                            }));
                            //Tools.Logger.AddUartLogDebug($"[openPort]check to send");
                            if (toSendData != null)
                            {
                                sendUartData(toSendData);
                                toSendData = null;
                            }
                            //Tools.Logger.AddUartLogDebug($"[openPort]done");
                        }
                        catch(Exception e)
                        {
                            Tools.Logger.AddUartLogDebug($"[openPort]open error:{e.Message}");
                            //串口打开失败！
                            Tools.MessageBox.Show(TryFindResource("ErrorOpenPort") as string ?? "?!");
                        }
                        isOpeningPort = false;
                        //Tools.Logger.AddUartLogDebug($"[openPort]all done");
                    });

                }
            }
        }
        private void OpenClosePortButton_Click(object sender, RoutedEventArgs e)
        {
            //Tools.Logger.AddUartLogDebug($"[OpenClosePortButton]now:{Tools.Global.uart.IsOpen()}");
            if (!Tools.Global.uart.IsOpen())//打开串口逻辑
            {
                openPort();
            }
            else//关闭串口逻辑
            {
                string lastPort = null;//记录一下上次的串口号
                try
                {
                    //Tools.Logger.AddUartLogDebug($"[OpenClosePortButton]close");
                    forcusClosePort = true;//不再重新开启串口
                    lastPort = Tools.Global.uart.GetName();//串口号
                    Tools.Global.uart.Close();
                    //Tools.Logger.AddUartLogDebug($"[OpenClosePortButton]close done");
                }
                catch
                {
                    //串口关闭失败！
                    Tools.MessageBox.Show(TryFindResource("ErrorClosePort") as string ?? "?!");
                }
                //Tools.Logger.AddUartLogDebug($"[OpenClosePortButton]change show");
                openClosePortTextBlock.Text = (TryFindResource("OpenPort_open") as string ?? "?!");
                serialPortsListComboBox.IsEnabled = true;
                //Tools.Logger.AddUartLogDebug($"[OpenClosePortButton]change show done");
                refreshPortList(lastPort);
            }

        }

        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            Tools.Logger.ClearData();
        }

        private void BaudRateComboBox_DropDownClosed(object sender, EventArgs e)
        {
            if (baudRateComboBox.SelectedItem != null)
            {
                if (baudRateComboBox.SelectedIndex == baudRateComboBox.Items.Count - 1)
                {
                    int br = 0;
                    Tuple<bool, string> ret = Tools.InputDialog.OpenDialog(TryFindResource("ShowBaudRate") as string ?? "?!",
                        "115200", TryFindResource("OtherRate") as string ?? "?!");

                    if (ret.Item1 && int.TryParse(ret.Item2, out br))
                    {
                        Tools.Global.setting.baudRate = br;
                    }

                    Task.Run(() =>
                    {
                        this.Dispatcher.Invoke(new Action(delegate {
                            var text = Tools.Global.setting.baudRate.ToString();
                            baudRateComboBox.Items[baudRateComboBox.Items.Count - 1] = text;
                            baudRateComboBox.Text = text;
                        }));
                    });
                }
                else
                {
                    Tools.Global.setting.baudRate =
                        int.Parse((baudRateComboBox.SelectedItem as ComboBoxItem).Content.ToString());
                    baudRateComboBox.Items[baudRateComboBox.Items.Count - 1] = TryFindResource("OtherRate") as string ?? "?!";
                }
            }
        }

        private void BaudRateComboBox_DropDownOpened(object sender, EventArgs e)
        {
            baudRateComboBox.Items[baudRateComboBox.Items.Count - 1] = TryFindResource("OtherRate") as string ?? "?!";
        }

        /// <summary>
        /// 发串口数据
        /// </summary>
        /// <param name="data"></param>
        private void sendUartData(byte[] data, bool? is_hex = null)
        {
            //lock (lockSendUartDataObject)
            {
                if (Tools.Global.uart.IsOpen())
                {
                    byte[] dataConvert;
                    try
                    {
                        dataConvert = LuaEnv.LuaLoader.Run(
                            $"{Tools.Global.setting.sendScript}.lua",
                            new System.Collections.ArrayList 
                            { 
                                "uartData",
                                is_hex == null ?
                                (Tools.Global.setting.hexSend ? Tools.Global.Hex2Byte(Tools.Global.Byte2String(data)) : data) : data
                            });
                    }
                    catch (Exception ex)
                    {
                        Tools.MessageBox.Show($"{TryFindResource("ErrorScript") as string ?? "?!"}\r\n" + ex.ToString());
                        return;
                    }
                    try
                    {
                        Tools.Global.uart.SendData(dataConvert);
                    }
                    catch(Exception ex)
                    {
                        Tools.MessageBox.Show($"{TryFindResource("ErrorSendFail") as string ?? "?!"}\r\n"+ ex.ToString());
                        return;
                    }
                }
            }
        }

        private void SendUartData_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            string data = toSendDataTextBox.Text;
            if (Tools.Global.setting.escapeSend)
                data = data.Replace(@"\r", "\r").Replace(@"\n", "\n").Replace(@"\t", "\t").Replace(@"\\", "\\").Replace(@"\0", "\0");
            
            if (!Tools.Global.uart.IsOpen())
                openPort();

            sendUartData(Global.GetEncoding().GetBytes(data));
        }

        private void QuiclList_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            //AtopSerial.Tools.MessageBox.Show("Right-click detected on Menu!");
            ToSendData data = ((Menu)sender).Tag as ToSendData;
            Tuple<bool, string> ret = Tools.InputDialog.OpenDialog(TryFindResource("QuickSendGroupRenameInfo") as string ?? "?!",
                Global.setting.GetQuickListNameNow(), TryFindResource("QuickSendGroupRenamePopup") as string ?? "?!");
            if (ret.Item1)
            {
                Global.setting.SetQuickListNameNow(ret.Item2);
                QuickListPageTextBlock.Text = ret.Item2;
            }
        }

        private void CardTabControl_Click(object sender, SelectionChangedEventArgs e)
        {
            if (e.OriginalSource is TabControl)
            {
                if (CardTabControl.SelectedIndex == 0)
                {
                    if (Tools.Global.setting.viewAutoSw == true)
                        Tools.Global.setting.graphSw = false;
                }
                else if (CardTabControl.SelectedIndex == 1)
                {

                }
                else if (CardTabControl.SelectedIndex == 2)
                {
                    if (Tools.Global.setting.viewAutoSw == true)
                        Tools.Global.setting.graphSw = false;
                }
                else if (CardTabControl.SelectedIndex == 3)
                {
                    if (Tools.Global.setting.viewAutoSw == true)
                        Tools.Global.setting.graphSw = true;
                }
            }

        }
        private void QuiclListHxeButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (ToSendData item in toSendListItems)
                item.hex = sendListHexPickOn;
            sendListHexPickOn = !sendListHexPickOn;
            QuickSendListSave(null, EventArgs.Empty);
        }
        private void QuickAddSendListButton_Click(object sender, RoutedEventArgs e)
        {
            if (toSendListItems.Count < 200)
            {
                toSendListItems.Add(new ToSendData() { id = toSendListItems.Count + 1, text = "", hex = false, commit = TryFindResource("QuickSendButton") as string ?? "?!" });
                QuickScrollViewer.ScrollToVerticalOffset(QuickScrollViewer.ScrollableHeight + 100);
            }
            QuickSendListSave(null, EventArgs.Empty);
        }

        private void QuickDelSendListButton_Click(object sender, RoutedEventArgs e)
        {
            if (toSendListItems.Count > 0)
            {
                toSendListItems.RemoveAt(toSendListItems.Count - 1);
                QuickScrollViewer.ScrollToVerticalOffset(QuickScrollViewer.ScrollableHeight);
            }
            QuickSendListSave(null, EventArgs.Empty);
        }

        private void QuickSendImportButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog OpenFileDialog = new System.Windows.Forms.OpenFileDialog();
            OpenFileDialog.Filter = TryFindResource("QuickSendAtopSerialFile") as string ?? "?!";
            if (OpenFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                List<ToSendData> data = null;
                try
                {
                    data = JsonConvert.DeserializeObject<List<ToSendData>>(
                        File.ReadAllText(OpenFileDialog.FileName));
                }
                catch (Exception err)
                {
                    Tools.MessageBox.Show(err.Message);
                    return;
                }
                this.Dispatcher.Invoke(new Action(delegate
                {
                    canSaveSendList = false;
                    toSendListItems.Clear();
                    foreach (var d in data)
                    {
                        if (toSendListItems.Count < 200)
                        {
                            toSendListItems.Add(d);
                        }
                    }
                    canSaveSendList = true;
                    QuickSendListSave(0, EventArgs.Empty);//保存并刷新数据列表
                }));
            }
        }

        private void QuickSendExportButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog SaveFileDialog = new System.Windows.Forms.SaveFileDialog();
            SaveFileDialog.FileName = System.Text.RegularExpressions.Regex.Replace(QuickListPageTextBlock.Text, "[<>/\\|:\"?*]", "-");
            SaveFileDialog.Filter = TryFindResource("QuickSendAtopSerialFile") as string ?? "?!";
            if (SaveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    File.WriteAllText(SaveFileDialog.FileName, JsonConvert.SerializeObject(toSendListItems));
                    Tools.MessageBox.Show(TryFindResource("QuickSendSaveFileDone") as string ?? "?!");
                }
                catch (Exception err)
                {
                    Tools.MessageBox.Show(err.Message);
                }
            }
        }
        private void QuickRemoveAllListButton_Click(object sender, RoutedEventArgs e)
        {
            (bool r, string s) = Tools.InputDialog.OpenDialog(TryFindResource("DeleteConfirmationMsg") as string ?? "?!",
                "", TryFindResource("DeleteConfirmation") as string ?? "?!");
            if (r && s == "YES")
            {
                toSendListItems.Clear();
                QuickSendListSave(null, EventArgs.Empty);
            }
        }
        private void QuickSendButton_click(object sender, RoutedEventArgs e)
        {
            ToSendData sendData = ((Button)sender).Tag as ToSendData;
            if (sendData.hex)
                sendUartData(Tools.Global.Hex2Byte(sendData.text), true);
            else
            {
                string data = sendData.text;
                if (Tools.Global.setting.escapeSend)
                    data = data.Replace(@"\r", "\r").Replace(@"\n", "\n").Replace(@"\t", "\t").Replace(@"\\", "\\").Replace(@"\0", "\0");
                sendUartData(Global.GetEncoding().GetBytes(data), false);
            }
        }

        private void QuickSendButton_rightClick(object sender, MouseButtonEventArgs e)
        {
            ToSendData data = ((Button)sender).Tag as ToSendData;
            Tuple<bool, string> ret = Tools.InputDialog.OpenDialog(TryFindResource("QuickSendSetButton") as string ?? "?!",
                data.commit, TryFindResource("QuickSendChangeButton") as string ?? "?!");
            if(ret.Item1)
            {
                ((Button)sender).Content = data.commit = ret.Item2;
            }
        }

        private void AutomaticSendTask(object token)
        {
            CancellationToken autoSendTaskToken = (CancellationToken)token;
            int sendTimer = Tools.Global.setting.automaticSendTimer;
            if (sendTimer == 0) sendTimer = 1;

            while (!autoSendTaskToken.IsCancellationRequested)
            {
                try
                {
                    if (Tools.Global.isMainWindowsClosed)
                        return;

                    if (Tools.Global.uart.IsOpen())
                    {
                        for (int iTemp = 0; iTemp < toSendListItems.Count; iTemp++)
                        { 
                            ToSendData Item = toSendListItems[iTemp];
                            if (Item.text != null)
                            {
                                string data = Item.text;
                                if(Item.hex)
                                {
                                    sendUartData(Tools.Global.Hex2Byte(data), true);
                                }
                                else
                                {
                                    if (Tools.Global.setting.escapeSend)
                                        data = data.Replace(@"\r", "\r").Replace(@"\n", "\n").Replace(@"\t", "\t").Replace(@"\\", "\\").Replace(@"\0", "\0");
                                    sendUartData(Global.GetEncoding().GetBytes(data), false);
                                }
                            }
                            Thread.Sleep(sendTimer);
                            if (autoSendTaskToken.IsCancellationRequested)
                                return;
                        }
                    }
                }
                catch (ThreadInterruptedException)
                {
                    return;
                }
            }
        }

        private void AutomaticSendCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            AutomaticSendTextBox.IsEnabled = false;
            autoSendTaskTokenSource?.Cancel();
            autoSendTaskTokenSource = new CancellationTokenSource();
            autoSendThread = new Thread(AutomaticSendTask);
            autoSendThread?.Start(autoSendTaskTokenSource.Token);
        }

        private void AutomaticSendCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            autoSendTaskTokenSource.Cancel();
            AutomaticSendTextBox.IsEnabled = true;
        }
        private void AutomaticSendTextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            AutomaticSendCheckBox.IsChecked = !AutomaticSendCheckBox.IsChecked;
        }
        
        /// <summary>
        /// 检查并更正快捷发送区序号
        /// </summary>
        public void CheckToSendListId()
        {
            for (int i = 0; i < toSendListItems.Count; i++)
            {
                if (toSendListItems[i].id != i + 1)
                {
                    var item = toSendListItems[i];
                    toSendListItems.RemoveAt(i);
                    item.id = i + 1;
                    toSendListItems.Insert(i, item);
                }
            }
        }

        /// <summary>
        /// 检查并更正数据监测区通道序号
        /// </summary>
        public void CheckToDataMonitorChannel()
        {
            for (int iTemp = 0; iTemp < toMonitorItems.Count; iTemp++)
            {
                if (toMonitorItems[iTemp].channel != iTemp + 1)
                {
                    var item = toMonitorItems[iTemp];
                    toMonitorItems.RemoveAt(iTemp);
                    item.channel = iTemp + 1;
                    toMonitorItems.Insert(iTemp, item);
                }
            }
        }
        /// <summary>
        /// 检查并更正抓图工具区通道序号
        /// </summary>
        public void CheckToSnatchGraphChannel()
        {
            for (int iTemp = 0; iTemp < toGraphItems.Count; iTemp++)
            {
                if (toGraphItems[iTemp].channel != iTemp + 1)
                {
                    var item = toGraphItems[iTemp];
                    toGraphItems.RemoveAt(iTemp);
                    item.channel = iTemp + 1;
                    toGraphItems.Insert(iTemp, item);
                }
            }
        }
        
        public void QuickSendListSave(object sender, EventArgs e)
        {
            if (!canSaveSendList)
                return;
            CheckToSendListId();
            Tools.Global.setting.quickSend = new List<ToSendData>(toSendListItems);
        }

        public void MonitorListSave(object sender, EventArgs e)
        {
            CheckToDataMonitorChannel();
            Tools.Global.setting.toDataMonitorItemsSave = new List<ToDataMonitorItems>(toMonitorItems);
            Tools.Global.setting.dataMonitorListNum = toMonitorItems.Count;
        }
        public void GraphListSave(object sender, EventArgs e)
        {
            CheckToSnatchGraphChannel();
            Tools.Global.setting.toSnatchGraphItemsSave = new List<ToSnatchGraphItems>(toGraphItems);
            Tools.Global.setting.snatchGraphListNum = toGraphItems.Count;
        }

        private void NewScriptButton_Click(object sender, RoutedEventArgs e)
        {
            newLuaFileWrapPanel.Visibility = Visibility.Visible;
        }

        private void RunScriptButton_Click(object sender, RoutedEventArgs e)
        {
            LuaRunScriptWork = LuaScriptWorkState.ScriptRun;
        }

        private void NewLuaFilebutton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(newLuaFileNameTextBox.Text))
            {
                Tools.MessageBox.Show(TryFindResource("LuaNoName") as string ?? "?!");
                return;
            }
            if (File.Exists(Tools.Global.ProfilePath + $@"user_script\user_script_run\{newLuaFileNameTextBox.Text}.lua"))
            {
                Tools.MessageBox.Show(TryFindResource("LuaExist") as string ?? "?!");
                return;
            }

            try
            {
                File.Create(Tools.Global.ProfilePath + $@"user_script\user_script_run\{newLuaFileNameTextBox.Text}.lua").Close();
                LoadFileRunScript(newLuaFileNameTextBox.Text);
            }
            catch
            {
                Tools.MessageBox.Show(TryFindResource("LuaCreateFail") as string ?? "?!");
                return;
            }
            newLuaFileWrapPanel.Visibility = Visibility.Collapsed;
        }

        private void NewLuaFileCancelbutton_Click(object sender, RoutedEventArgs e)
        {
            newLuaFileWrapPanel.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 加载lua脚本文件
        /// </summary>
        /// <param name="fileName">文件名，不带.lua</param>
        /// 
        private void LoadFileRunScript(string fileName)
        {
            try
            {
                if (!File.Exists(Tools.Global.ProfilePath + $@"user_script\user_script_run\{fileName}.lua"))
                {
                    File.Create(Tools.Global.ProfilePath + $@"user_script\user_script_run\{fileName}.lua").Close();
                }
                Tools.Global.setting.runScript = fileName;
                textEditorRunScript.Text = File.ReadAllText(Tools.Global.ProfilePath + $@"user_script\user_script_run\{Tools.Global.setting.runScript}.lua");
            }
            catch
            {
                Tools.MessageBox.Show("File load failed.\r\n" +
                    "Do not open this file in other application!");
                return;
            }

            lastFileTimeRunScript = File.GetLastWriteTime(Tools.Global.ProfilePath + $@"user_script\user_script_run\{Tools.Global.setting.runScript}.lua");
            lastChangeTimeRunScript = lastFileTimeRunScript;
            RefreshLuaSendList();
        }

        /// <summary>
        /// 加载数据监测脚本文件
        /// </summary>
        private void LoadFileDataMonitor()
        {
            try
            {
                if (!File.Exists(Tools.Global.ProfilePath + @"user_script\monitor_script\default.lua"))
                {
                    File.Create(Tools.Global.ProfilePath + @"user_script\monitor_script\default.lua").Close();
                }
                dataMonitorScriptEdit.Text = File.ReadAllText(Tools.Global.ProfilePath + @"user_script\monitor_script\default.lua");
            }
            catch
            {
                dataMonitorScriptEdit.Text = "";
            }
            lastFileTimeDataMonitor = File.GetLastWriteTime(Tools.Global.ProfilePath + @"user_script\monitor_script\default.lua");
            lastChangeTimeDataMonitor = lastFileTimeDataMonitor;
        }

        /// <summary>
        /// 加载抓图工具脚本文件
        /// </summary>
        private void LoadFileGraphTool()
        {
            try
            {
                if (!File.Exists(Tools.Global.ProfilePath + @"user_script\graph_script\default.lua"))
                {
                    File.Create(Tools.Global.ProfilePath + @"user_script\graph_script\default.lua").Close();
                }
                graphToolScriptEdit.Text = File.ReadAllText(Tools.Global.ProfilePath + @"user_script\graph_script\default.lua");
            }
            catch
            {
                graphToolScriptEdit.Text = "";
            }
            lastFileTimeGraphTool = File.GetLastWriteTime(Tools.Global.ProfilePath + @"user_script\graph_script\default.lua");
            lastChangeTimeGraphTool = lastFileTimeGraphTool;
        }

        /// <summary>
        /// 保存运行脚本脚本文件
        /// </summary>
        /// <param name="fileName">文件名，不带.lua</param>
        private void SaveFileRunScript(string fileName)
        {
            try
            {
                //如果修改时间大于文件时间才执行保存操作
                if (lastChangeTimeRunScript > lastFileTimeRunScript)
                {
                    File.WriteAllText(Tools.Global.ProfilePath + $@"user_script\user_script_run\{fileName}.lua", textEditorRunScript.Text);
                    //记录最后时间
                    lastFileTimeRunScript = File.GetLastWriteTime(Tools.Global.ProfilePath + $@"user_script\user_script_run\{fileName}.lua");
                }
            }
            catch { }
        }

        /// <summary>
        /// 保存数据监测脚本文件
        /// </summary>
        private void SaveFileDataMonitor()
        {
            try
            {
                //如果修改时间大于文件时间才执行保存操作
                if (lastChangeTimeDataMonitor > lastFileTimeDataMonitor)
                {
                    File.WriteAllText(Tools.Global.ProfilePath + $@"user_script\monitor_script\default.lua", dataMonitorScriptEdit.Text);
                    lastFileTimeDataMonitor = File.GetLastWriteTime(Tools.Global.ProfilePath + @"user_script\monitor_script\default.lua");
                }
            }
            catch { }
        }

        /// <summary>
        /// 保存抓图工具脚本文件
        /// </summary>
        private void SaveFileGraphTool()
        {
            try
            {
                //如果修改时间大于文件时间才执行保存操作
                if (lastChangeTimeGraphTool > lastFileTimeGraphTool)
                {
                    File.WriteAllText(Tools.Global.ProfilePath + $@"user_script\graph_script\default.lua", graphToolScriptEdit.Text);
                    lastFileTimeGraphTool = File.GetLastWriteTime(Tools.Global.ProfilePath + @"user_script\graph_script\default.lua");
                }
            }
            catch { }
        }

        private void LuaFileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (luaFileListSend.SelectedItem != null && !fileLoadingRunScript)
            {
                if (lastFileRunScript != "")
                    SaveFileRunScript(lastFileRunScript);
                string fileName = luaFileListSend.SelectedItem as string;
                LoadFileRunScript(fileName);
            }
        }
        private void TextEditorRunScript_LostFocus(object sender, RoutedEventArgs e)
        {
            if (lastFileRunScript != "")
                SaveFileRunScript(lastFileRunScript);
        }
        private void TextEditorDataMonitor_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                if (lastChangeTimeDataMonitor > lastFileTimeDataMonitor)
                {
                    File.WriteAllText(Tools.Global.ProfilePath + @"user_script\monitor_script\default.lua", dataMonitorScriptEdit.Text);
                    lastFileTimeDataMonitor = File.GetLastWriteTime(Tools.Global.ProfilePath + @"user_script\monitor_script\default.lua");
                }
            }
            catch { }
        }
        private void TextEditorGraphTool_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                if (lastChangeTimeGraphTool > lastFileTimeGraphTool)
                {
                    File.WriteAllText(Tools.Global.ProfilePath + @"user_script\graph_script\default.lua", graphToolScriptEdit.Text);
                    lastFileTimeGraphTool = File.GetLastWriteTime(Tools.Global.ProfilePath + @"user_script\graph_script\default.lua");
                }
            }
            catch { }
        }
        private void Window_Deactivated(object sender, EventArgs e)
        {
            //窗口变为后台,可能在切换编辑器,自动保存脚本
            if (lastFileRunScript != "")
                SaveFileRunScript(lastFileRunScript);
            if (lastFileDataMonitor != "")
                SaveFileDataMonitor();
            if (lastFileGraphTool != "")
                SaveFileGraphTool();
        }
        private void Window_Activated(object sender, EventArgs e)
        {
            if (lastFileRunScript != "")
            {
                //当前文件最后时间
                DateTime fileTime = File.GetLastWriteTime(Tools.Global.ProfilePath + $@"user_script\user_script_run\{lastFileRunScript}.lua");
                if (fileTime > lastFileTimeRunScript)//代码在外部被修改
                {
                    LoadFileRunScript(lastFileRunScript);
                }
            }
            if (lastFileDataMonitor != "")
            {
                //当前文件最后时间
                DateTime fileTime = File.GetLastWriteTime(Tools.Global.ProfilePath + $@"user_script\monitor_script\default.lua");
                if (fileTime > lastFileTimeDataMonitor)//代码在外部被修改
                {
                    LoadFileDataMonitor();
                }
            }
            if (lastFileGraphTool != "")
            {
                //当前文件最后时间
                DateTime fileTime = File.GetLastWriteTime(Tools.Global.ProfilePath + $@"user_script\graph_script\default.lua");
                if (fileTime > lastFileTimeGraphTool)//代码在外部被修改
                {
                    LoadFileGraphTool();
                }
            }
        }
        private enum LuaScriptWorkState : byte
        {
            ScriptStop,
            ScriptPause,
            ScriptRun,
        }
        private LuaScriptWorkState _luaRunScriptWork = LuaScriptWorkState.ScriptStop;
        private LuaScriptWorkState LuaRunScriptWork
        {
            get
            {
                return _luaRunScriptWork;
            }
            set
            {
                this.Dispatcher.Invoke(new Action(delegate
                {
                    if(value == LuaScriptWorkState.ScriptStop)
                    {
                        luaScriptEditorGrid.Visibility = Visibility.Visible;
                        luaLogShowGrid.Visibility = Visibility.Collapsed;
                    }
                    else if (value == LuaScriptWorkState.ScriptPause)
                    {
                        LuaEnv.LuaRunEnv.StopLua("");
                        LuaEnv.LuaRunEnv.canRun = false;

                        lock (luaLogsBuff)
                            luaLogsBuff.Clear();

                        luaLogTextBox.AppendText("script stop!\r\n");
                        luaLogTextBox.ScrollToEnd();

                        luaScriptEditorGrid.Visibility = Visibility.Collapsed;
                        luaLogShowGrid.Visibility = Visibility.Visible;

                        stopLuaOrExitIcon.Icon = FontAwesomeIcon.SignOut;
                        stopLuaButton.ToolTip = TryFindResource("LuaQuit") as string ?? "?!";
                    }
                    else if (value == LuaScriptWorkState.ScriptRun)
                    {
                        if (LuaDataMonitorWork == LuaScriptWorkState.ScriptRun)
                        {
                            LuaDataMonitorWork = LuaScriptWorkState.ScriptStop;
                        }
                        if (LuaGraphToolWork == LuaScriptWorkState.ScriptRun)
                        {
                            LuaGraphToolWork = LuaScriptWorkState.ScriptStop;
                        }
                        if (luaFileListSend.SelectedItem != null && !fileLoadingRunScript)
                        {
                            LuaEnv.LuaRunEnv.canRun = false;
                            luaLogTextBox.Clear();
                            luaLogTextBox.AppendText("script run!\r\n");
                            luaLogTextBox.ScrollToEnd();
                            LuaEnv.LuaRunEnv.New($"user_script/user_script_run/{luaFileListSend.SelectedItem as string}.lua");
                            luaScriptEditorGrid.Visibility = Visibility.Collapsed;
                            luaLogShowGrid.Visibility = Visibility.Visible;
                            stopLuaOrExitIcon.Icon = FontAwesomeIcon.Stop;
                            stopLuaButton.ToolTip = TryFindResource("LuaStop") as string ?? "?!";
                            LuaEnv.LuaRunEnv.canRun = true;
                        }
                    }
                }));
                _luaRunScriptWork = value;
            }
        }

        private LuaScriptWorkState _luaDataMonitorWork = LuaScriptWorkState.ScriptStop;
        private LuaScriptWorkState LuaDataMonitorWork
        {
            get
            {
                return _luaDataMonitorWork;
            }
            set
            {
                this.Dispatcher.Invoke(new Action(delegate
                {
                    if (value == LuaScriptWorkState.ScriptStop)
                    {
                        _luaDataMonitorWork = value;
                        LuaEnv.LuaRunEnv.StopLua("");
                        LuaEnv.LuaRunEnv.canRun = false;

                        lock (luaLogsBuff)
                            luaLogsBuff.Clear();

                        dataMonitorLogTextBox.AppendText("script stop!\r\n");
                        dataMonitorLogTextBox.ScrollToEnd();
                        dataMonitorScriptRunButton.Content = TryFindResource("DataMonitorScriptRun") as string ?? "?!";
                        dataMonitorRecordWriter?.Dispose();
                        dataMonitorRecordWriter = null;
                    }
                    else if (value == LuaScriptWorkState.ScriptRun)
                    {
                        if(LuaRunScriptWork == LuaScriptWorkState.ScriptRun)
                        {
                            LuaRunScriptWork = LuaScriptWorkState.ScriptPause;
                        }
                        if (LuaGraphToolWork == LuaScriptWorkState.ScriptRun)
                        {
                            LuaGraphToolWork = LuaScriptWorkState.ScriptStop;
                        }
                        if (luaFileListSend.SelectedItem != null && !fileLoadingRunScript)
                        {
                            LuaEnv.LuaRunEnv.canRun = false;
                            dataMonitorLogTextBox.Clear();
                            dataMonitorLogTextBox.AppendText("script run!\r\n");
                            dataMonitorLogTextBox.ScrollToEnd();
                            LuaEnv.LuaRunEnv.New($"user_script/monitor_script/default.lua");
                            LuaEnv.LuaRunEnv.canRun = true;
                            dataMonitorScriptRunButton.Content = TryFindResource("DataMonitorScriptStop") as string ?? "?!";
                        }
                        _luaDataMonitorWork = value;
                    }
                }));
            }
        }
        
        private LuaScriptWorkState _luaGraphToolWork = LuaScriptWorkState.ScriptStop;
        private LuaScriptWorkState LuaGraphToolWork
        {
            get
            {
                return _luaGraphToolWork;
            }
            set
            {
                this.Dispatcher.Invoke(new Action(delegate
                {
                    if (value == LuaScriptWorkState.ScriptStop)
                    {
                        _luaGraphToolWork = value;
                        LuaEnv.LuaRunEnv.StopLua("");
                        LuaEnv.LuaRunEnv.canRun = false;

                        lock (luaLogsBuff)
                            luaLogsBuff.Clear();

                        graphToolLogTextBox.AppendText("script stop!\r\n");
                        graphToolLogTextBox.ScrollToEnd();
                        graphScriptRunButton.Content = TryFindResource("GraphScriptRun") as string ?? "?!";
                    }
                    else if (value == LuaScriptWorkState.ScriptRun)
                    {
                        if (LuaRunScriptWork == LuaScriptWorkState.ScriptRun)
                        {
                            LuaRunScriptWork = LuaScriptWorkState.ScriptPause;
                        }
                        if (LuaDataMonitorWork == LuaScriptWorkState.ScriptRun)
                        {
                            LuaDataMonitorWork = LuaScriptWorkState.ScriptStop;
                        }
                        if (luaFileListSend.SelectedItem != null && !fileLoadingRunScript)
                        {
                            LuaEnv.LuaRunEnv.canRun = false;
                            graphToolLogTextBox.Clear();
                            graphToolLogTextBox.AppendText("script run!\r\n");
                            graphToolLogTextBox.ScrollToEnd();
                            LuaEnv.LuaRunEnv.New($"user_script/graph_script/default.lua");
                            LuaEnv.LuaRunEnv.canRun = true;
                            graphScriptRunButton.Content = TryFindResource("GraphScriptStop") as string ?? "?!";
                        }
                        _luaGraphToolWork = value;
                    }
                }));
            }
        }
        /// <summary>
        /// 消息来的信号量
        /// </summary>
        private EventWaitHandle luaWaitQueue = new AutoResetEvent(false);
        private List<string> luaLogsBuff = new List<string>();
        private void LuaApis_PrintLuaLog(object sender, EventArgs e)
        {
            if(sender is string && sender != null)
            { 
                lock(luaLogsBuff)
                {
                    if (luaLogsBuff.Count > 500)
                    {
                        luaLogsBuff.Clear();
                        luaLogsBuff.Add("too many logs!");
                    }
                    else
                        luaLogsBuff.Add(sender as string);
                }
                luaWaitQueue.Set();
            }
        }

        private void LuaLogPrintTask()
        {
            luaWaitQueue.Reset();
            Tools.Global.ProgramClosedEvent += (_, _) =>
            {
                luaWaitQueue.Set();
            };
            while (true)
            {
                luaWaitQueue.WaitOne();
                if (Tools.Global.isMainWindowsClosed)
                    return;
                var logsb = new StringBuilder();
                lock (luaLogsBuff)
                {
                    for(int i=0;i<luaLogsBuff.Count;i++)
                    {
                        logsb.AppendLine(luaLogsBuff[i]);
                    }
                    luaLogsBuff.Clear();
                }
                
                if (logsb.Length == 0)
                    continue;

                var logs = logsb.ToString();
                DoInvoke(()=>
                {
                    if (LuaRunScriptWork == LuaScriptWorkState.ScriptRun)
                    {
                        luaLogTextBox.AppendText(logs);
                        luaLogTextBox.ScrollToEnd();
                    }
                    else if (LuaDataMonitorWork == LuaScriptWorkState.ScriptRun)
                    {
                        dataMonitorLogTextBox.AppendText(logs);
                        dataMonitorLogTextBox.ScrollToEnd();
                    }
                    else if (LuaGraphToolWork == LuaScriptWorkState.ScriptRun)
                    {
                        graphToolLogTextBox.AppendText(logs);
                        graphToolLogTextBox.ScrollToEnd();
                    }
                });
                Thread.Sleep(10);
            }
        }

        private void luaLogTextBox_MouseLeave(object sender, MouseEventArgs e)
        {
            //luaLogTextBox.IsEnabled = true;
        }
        private void dataMonitorLogTextBox_MouseLeave(object sender, MouseEventArgs e)
        {
        }

        private void graphToolLogTextBox_MouseLeave(object sender, MouseEventArgs e)
        {
        }

        private void StopLuaButton_Click(object sender, RoutedEventArgs e)
        {
            if (LuaRunScriptWork == LuaScriptWorkState.ScriptRun)
                LuaRunScriptWork = LuaScriptWorkState.ScriptPause;
            else
                LuaRunScriptWork = LuaScriptWorkState.ScriptStop;
        }

        private void LuaRunEnv_LuaRunError(object sender, EventArgs e)
        {
        }

        StreamWriter dataMonitorRecordWriter;
        private void luaDataMonitorRecord(XLua.LuaTable Record)
        {
            string DataMonitorRecordFile;
            DoInvoke(() =>
            {
                try
                {
                    if (LuaDataMonitorWork == LuaScriptWorkState.ScriptRun)
                    {
                        if (dataMonitorRecordWriter == null)
                        {
                            DataMonitorRecordFile = Tools.Global.ProfilePath + @"\user_script\monitor_script\logs\" + DateTime.Now.ToString("yyMMddHHmmss") + ".csv";
                            if (!File.Exists(DataMonitorRecordFile))
                            {
                                File.Create(DataMonitorRecordFile).Close();
                            }
                            dataMonitorRecordWriter = new StreamWriter(DataMonitorRecordFile);

                            StringBuilder headerBuilder = new StringBuilder();
                            headerBuilder.Append("Time,");
                            foreach (var iTemp in toMonitorItems)
                            {
                                headerBuilder.Append(iTemp.description + ",");
                            }
                            dataMonitorRecordWriter.WriteLine(headerBuilder.ToString().TrimEnd(','));
                        }
                        if (dataMonitorRecordWriter != null)
                        {
                            StringBuilder rowBuilder = new StringBuilder();
                            rowBuilder.Append(DateTime.Now.ToString("yy-MM-dd HH:mm:ss") + ",");
                            Record.ForEach<int, String>((Key, RecordData) =>
                            {
                                rowBuilder.Append(RecordData + ",");
                            });
                            dataMonitorRecordWriter.WriteLine(rowBuilder.ToString().TrimEnd(','));
                        }
                    }
                }
                catch { }
            });
        }

        private XLua.LuaTable luaDataMonitorPara()
        {
            XLua.LuaTable Table = AtopSerial.LuaEnv.LuaRunEnv.lua.NewTable();
            foreach (var iTemp in toMonitorItems)
            {
                XLua.LuaTable TableSub = AtopSerial.LuaEnv.LuaRunEnv.lua.NewTable();
                TableSub.Set<String, String>("channel", iTemp.channel.ToString());
                TableSub.Set<String, String>("description", iTemp.description.ToString());
                TableSub.Set<String, String>("sampleEn", iTemp.sampleEn.ToString());
                Table.Set<int, XLua.LuaTable>(iTemp.channel, TableSub);
            }
            return Table;
        }

        private void luaDataMonitorValue(int Channel, String Value)
        {
            if (Channel >= 0 && Channel < toMonitorItems.Count)
            {
                toMonitorItems[Channel].result = Value;
            }
        }

        private XLua.LuaTable luaGraphSnatchPara()
        {
            XLua.LuaTable Table = AtopSerial.LuaEnv.LuaRunEnv.lua.NewTable();
            Table.Set<String, String>("SampleLength", toGraphTool.graphToolSampleLen.ToString());
            Table.Set<String, String>("SampleInterval", toGraphTool.graphToolSampleInterval.ToString());
            Table.Set<String, String>("SampleMode", toGraphTool.graphToolSampleMode.ToString());
            Table.Set<String, String>("TriggerPara", toGraphTool.graphToolTriggerPara.ToString());
            Table.Set<String, String>("TriggerLag", toGraphTool.graphToolTriggerLag.ToString());
            return Table;
        }

        private XLua.LuaTable luaGraphChannelPara()
        {
            XLua.LuaTable Table = AtopSerial.LuaEnv.LuaRunEnv.lua.NewTable();

            foreach (var iTemp in toGraphItems)
            {
                XLua.LuaTable TableSub = AtopSerial.LuaEnv.LuaRunEnv.lua.NewTable();
                TableSub.Set<String, String>("channel", iTemp.channel.ToString());
                TableSub.Set<String, String>("show", iTemp.show.ToString());
                TableSub.Set<String, String>("description", iTemp.description.ToString());
                TableSub.Set<String, String>("dataSour", iTemp.dataSour.ToString());
                TableSub.Set<String, String>("scaleY", iTemp.scaleY.ToString());
                TableSub.Set<String, String>("offset", iTemp.offset.ToString());
                TableSub.Set<String, String>("type", iTemp.type.ToString());
                Table.Set<int, XLua.LuaTable>(iTemp.channel, TableSub);
            }
            return Table;
        }

        private void reRunLuaButton_Click(object sender, RoutedEventArgs e)
        {
            LuaRunScriptWork = LuaScriptWorkState.ScriptRun;
        }

        private void SendLuaScriptButton_Click(object sender, RoutedEventArgs e)
        {
            LuaEnv.LuaRunEnv.RunCommand(runOneLineLuaTextBox.Text);
        }

        private void RunOneLineLuaTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
                LuaEnv.LuaRunEnv.RunCommand(runOneLineLuaTextBox.Text);
        }

        private void DeleteScriptButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.DialogResult Result = Tools.MessageBox.ShowConfirm(TryFindResource("DeleteScriptConfirm") as string ?? "?!");
            if (Result == System.Windows.Forms.DialogResult.OK)
            {
                if (File.Exists(Tools.Global.ProfilePath + @"user_script\user_script_run\" + luaFileListSend.SelectedItem.ToString() + ".lua"))
                {
                    File.Delete(Tools.Global.ProfilePath + @"user_script\user_script_run\" + luaFileListSend.SelectedItem.ToString() + ".lua");
                }
                Tools.Global.setting.runScript = "";
                RefreshLuaSendList();
                string fileName = luaFileListSend.SelectedItem as string;
                LoadFileRunScript(fileName);
            }
        }

        private void RefreshPortButton_Click(object sender, RoutedEventArgs e)
        {
            refreshPortList();
        }

        private void sentCountTextBlock_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Tools.Global.setting.SentCount = 0;
        }

        private void receivedCountTextBlock_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Tools.Global.setting.ReceivedCount = 0;
        }

        private void Language_Click(object sender, RoutedEventArgs e)
        {
            
            if (Tools.Global.setting.language == "zh-CN")
                Tools.Global.setting.language = "en-US";
            else
                Tools.Global.setting.language = "zh-CN";
            if (Tools.Global.uart.IsOpen())
                openClosePortTextBlock.Text = TryFindResource("OpenPort_close") as string ?? "?!";
            else
                openClosePortTextBlock.Text = TryFindResource("OpenPort_open") as string ?? "?!";
        }

        private void GraphSwButton_Click(object sender, RoutedEventArgs e)
        {
            Tools.Global.setting.graphSw = !Tools.Global.setting.graphSw;
        }

        //id序号右击事件
        private void TextBlock_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            ToSendData data;
            try
            {
                data = ((TextBlock)sender).Tag as ToSendData;
            }
            catch
            {
                data = ((Grid)sender).Tag as ToSendData;
            }
            Tuple<bool, string> ret = Tools.InputDialog.OpenDialog(TryFindResource("QuickSendChangeIdButton") as string ?? "?!",
                data.id.ToString(), (TryFindResource("QuickSendChangeIdTitle") as string ?? "?!") + data.id.ToString());

            if (!ret.Item1)
                return;
            CheckToSendListId();
            if (ret.Item2.Trim().Length == 0)//留空删除该项目
            {
                toSendListItems.RemoveAt(data.id-1);
            }
            else
            {
                int index = -1;
                int.TryParse(ret.Item2, out index);
                if (index == data.id || index <= 0 || index > toSendListItems.Count) return;
                //移动到指定位置
                var item = toSendListItems[data.id-1];
                toSendListItems.RemoveAt(data.id-1);
                toSendListItems.Insert(index - 1, item);
            }
            QuickSendListSave(null, EventArgs.Empty);
        }
        private void MenuItem_Click_QuickSendList(object sender, RoutedEventArgs e)
        {
            canSaveSendList = false;
            int select = int.Parse((string)((MenuItem)sender).Tag);
            toSendListItems.Clear();
            Global.setting.quickSendSelect = select;
            LoadQuickSendList();
            canSaveSendList = true;
        }

        private void QuickListNameStackPanel_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Tuple<bool, string> ret = Tools.InputDialog.OpenDialog("", Global.setting.GetQuickListNameNow(), TryFindResource("QuickSendListNameChangeTip") as string ?? "?!");
            if (!ret.Item1)
                return;

            Global.setting.SetQuickListNameNow(ret.Item2);
            QuickListPageTextBlock.Text = ret.Item2;
        }

        private void ClearLuaPrintButton_Click(object sender, RoutedEventArgs e)
        {
            luaLogTextBox.Clear();
        }

        private void textEditorRunScript_TextChanged(object sender, EventArgs e)
        {
            lastChangeTimeRunScript = DateTime.Now;
        }
        private void textEditorDataMonitor_TextChanged(object sender, EventArgs e)
        {
            lastChangeTimeDataMonitor = DateTime.Now;
        }
        private void textEditorrGraphTool_TextChanged(object sender, EventArgs e)
        {
            lastChangeTimeGraphTool = DateTime.Now;
        }

        private void dataMonitorSampleEn_click(object sender, RoutedEventArgs e)
        {
            ToDataMonitorItems Item = ((CheckBox)sender).Tag as ToDataMonitorItems;
            if(Item.sampleEn == false && AtopSerial.MainWindow.toDataMonitor.selectShowSw == true)
                Item.visibility = Visibility.Collapsed;
        }

        private void MonitorAddSendListButton_Click(object sender, RoutedEventArgs e)
        {
            if (toMonitorItems.Count < 200)
            {
                toMonitorItems.Add(new ToDataMonitorItems() { channel = toMonitorItems.Count + 1, description = "", result = "", sampleEn = false });
                MonitorScrollViewer.ScrollToVerticalOffset(MonitorScrollViewer.ScrollableHeight + 100);
            }
            MonitorListSave(null, EventArgs.Empty);
        }

        private void MonitorDelSendListButton_Click(object sender, RoutedEventArgs e)
        {
            if (toMonitorItems.Count > 0)
            {
                toMonitorItems.RemoveAt(toMonitorItems.Count - 1);
                MonitorScrollViewer.ScrollToVerticalOffset(MonitorScrollViewer.ScrollableHeight);
            }
            MonitorListSave(null, EventArgs.Empty);
        }
        private void MonitorSendImportButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog OpenFileDialog = new System.Windows.Forms.OpenFileDialog();
            OpenFileDialog.Filter = TryFindResource("MonitorAtopSerialFile") as string ?? "?!";
            if (OpenFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                List<ToDataMonitorItems> data = null;
                try
                {
                    data = JsonConvert.DeserializeObject<List<ToDataMonitorItems>>(
                        File.ReadAllText(OpenFileDialog.FileName));
                }
                catch (Exception err)
                {
                    Tools.MessageBox.Show(err.Message);
                    return;
                }
                this.Dispatcher.Invoke(new Action(delegate
                {
                    foreach (var d in data)
                    {
                        if (toMonitorItems.Count < 200)
                        {
                            toMonitorItems.Add(d);
                        }
                    }
                }));
            }
        }
        private void MonitorSendExportButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog SaveFileDialog = new System.Windows.Forms.SaveFileDialog();
            SaveFileDialog.FileName = System.Text.RegularExpressions.Regex.Replace("", "[<>/\\|:\"?*]", "-");
            SaveFileDialog.Filter = TryFindResource("MonitorAtopSerialFile") as string ?? "?!";
            if (SaveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    File.WriteAllText(SaveFileDialog.FileName, JsonConvert.SerializeObject(toMonitorItems));
                    Tools.MessageBox.Show(TryFindResource("MonitorSaveFileDone") as string ?? "?!");
                }
                catch (Exception err)
                {
                    Tools.MessageBox.Show(err.Message);
                }
            }
        }
        private void MonitorRemoveAllListButton_Click(object sender, RoutedEventArgs e)
        {
            (bool r, string s) = Tools.InputDialog.OpenDialog(TryFindResource("DeleteConfirmationMsg") as string ?? "?!",
                "", TryFindResource("DeleteConfirmation") as string ?? "?!");
            if (r && s == "YES")
            {
                toMonitorItems.Clear();
            }
            MonitorListSave(null, EventArgs.Empty);
        }
        private void MonitorClearValueButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (ToDataMonitorItems Item in toMonitorItems)
            {
                Item.result = "";
            }
            //MonitorListSave(null, EventArgs.Empty);
        }

        private void uartDataFlowDocument_LostFocus(object sender, RoutedEventArgs e)
        {
            dataShowFrame.BorderThickness = new Thickness(0);
        }

        private void graphScaleY_LostFocus(object sender, RoutedEventArgs e)
        {
            double result;
            ToSnatchGraphItems Item = ((TextBox)sender).Tag as ToSnatchGraphItems;
            if(double.TryParse(Item.scaleY, out result))
            {
                PlotPage.PlotScaleY(Item.channel - 1, result);
            }

        }
        private void graphScaleY_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            double result;
            if (e.Key == Key.Enter)
            {
                ToSnatchGraphItems Item = ((TextBox)sender).Tag as ToSnatchGraphItems;
                if (double.TryParse(Item.scaleY, out result))
                {
                    PlotPage.PlotScaleY(Item.channel-1, result);
                }
            }
        }
        private void graphOffset_LostFocus(object sender, RoutedEventArgs e)
        {
            double result;
            ToSnatchGraphItems Item = ((TextBox)sender).Tag as ToSnatchGraphItems;
            if (double.TryParse(Item.offset, out result))
            {
                PlotPage.PlotOffsetY(Item.channel-1, result);
            }
        }
        private void graphOffset_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            double result;
            if (e.Key == Key.Enter)
            {
                ToSnatchGraphItems Item = ((TextBox)sender).Tag as ToSnatchGraphItems;
                if (double.TryParse(Item.offset, out result))
                {
                    PlotPage.PlotOffsetY(Item.channel - 1, result);
                }
            }
        }
        private void graphShow_Click(object sender, RoutedEventArgs e)
        {
            ToSnatchGraphItems Item = ((CheckBox)sender).Tag as ToSnatchGraphItems;
            PlotPage.PlotIsVisible(Item.channel - 1, Item.show);
        }

        private void graphScriptRunButton_Click(object sender, RoutedEventArgs e)
        {
            if (LuaGraphToolWork == LuaScriptWorkState.ScriptRun)
            {
                //graphScriptRunButton.Background = new SolidColorBrush(Colors.DarkTurquoise);
                LuaGraphToolWork = LuaScriptWorkState.ScriptStop;
            }
            else
            {
                //graphScriptRunButton.Background = new SolidColorBrush(Colors.Tomato);
                LuaGraphToolWork = LuaScriptWorkState.ScriptRun;
            }
        }
        private void graphEditScriptButton_Click(object sender, RoutedEventArgs e)
        {
            if (graphToolScriptEditGrid.Visibility == Visibility.Visible)
            {
                graphToolListGrid.Visibility = Visibility.Visible;
                graphToolScriptEditGrid.Visibility = Visibility.Collapsed;
                graphToolLogGrid.Visibility = Visibility.Collapsed;
            }
            else
            {
                graphToolListGrid.Visibility = Visibility.Collapsed;
                graphToolScriptEditGrid.Visibility = Visibility.Visible;
                graphToolLogGrid.Visibility = Visibility.Collapsed;
            }
        }
        private void graphLogButton_Click(object sender, RoutedEventArgs e)
        {
            if (graphToolLogGrid.Visibility == Visibility.Visible)
            {
                graphToolListGrid.Visibility = Visibility.Visible;
                graphToolScriptEditGrid.Visibility = Visibility.Collapsed;
                graphToolLogGrid.Visibility = Visibility.Collapsed;
            }
            else
            {
                graphToolListGrid.Visibility = Visibility.Collapsed;
                graphToolScriptEditGrid.Visibility = Visibility.Collapsed;
                graphToolLogGrid.Visibility = Visibility.Visible;
            }
        }
        private void dataMonitorScriptRunButton_Click(object sender, RoutedEventArgs e)
        {
            if (LuaDataMonitorWork == LuaScriptWorkState.ScriptRun)
            {
                //dataMonitorScriptRunButton.Background = new SolidColorBrush(Colors.DarkTurquoise);
                LuaDataMonitorWork = LuaScriptWorkState.ScriptStop;
            }
            else
            {
                //dataMonitorScriptRunButton.Background = new SolidColorBrush(Colors.Tomato);
                LuaDataMonitorWork = LuaScriptWorkState.ScriptRun;
            }
        }
        private void dataMonitorLogButton_Click(object sender, RoutedEventArgs e)
        {
            if (dataMonitorLogGrid.Visibility == Visibility.Visible)
            {
                dataMonitorListGrid.Visibility = Visibility.Visible;
                dataMonitorScriptEditGrid.Visibility = Visibility.Collapsed;
                dataMonitorLogGrid.Visibility = Visibility.Collapsed;
            }
            else
            {
                dataMonitorListGrid.Visibility = Visibility.Collapsed;
                dataMonitorScriptEditGrid.Visibility = Visibility.Collapsed;
                dataMonitorLogGrid.Visibility = Visibility.Visible;
            }
        }
        private void dataMonitorScriptEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (dataMonitorScriptEditGrid.Visibility == Visibility.Visible)
            {
                dataMonitorListGrid.Visibility = Visibility.Visible;
                dataMonitorScriptEditGrid.Visibility = Visibility.Collapsed;
                dataMonitorLogGrid.Visibility = Visibility.Collapsed;
            }
            else
            {
                dataMonitorListGrid.Visibility = Visibility.Collapsed;
                dataMonitorScriptEditGrid.Visibility = Visibility.Visible;
                dataMonitorLogGrid.Visibility = Visibility.Collapsed;
            }
        }
        private void dataMonitorOpenLogButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", Tools.Global.ProfilePath+ @"user_script\monitor_script\logs");
            }
            catch
            {
                Tools.MessageBox.Show($"Folder opening failed, please open manually.Path:{Tools.Global.ProfilePath}user_script/monitor_script/logs");
            }
        }

        private void dataMonitorSelectShowButton_Click(object sender, RoutedEventArgs e)
        {
            if(AtopSerial.MainWindow.toDataMonitor.selectShowSw == true)
            {
                AtopSerial.MainWindow.toDataMonitor.selectShowSw = false;
                for (var iTemp = 0; iTemp < toMonitorItems.Count; iTemp++)
                {
                    toMonitorItems[iTemp].visibility = Visibility.Visible;
                }
            }
            else
            {
                AtopSerial.MainWindow.toDataMonitor.selectShowSw = true;
                for (var iTemp = 0; iTemp < toMonitorItems.Count; iTemp++)
                {
                    if (toMonitorItems[iTemp].sampleEn == true)
                        toMonitorItems[iTemp].visibility = Visibility.Visible;
                    else
                        toMonitorItems[iTemp].visibility = Visibility.Collapsed;
                }
            }
        }
        private void dataMonitorAllSelectButton_Click(object sender, RoutedEventArgs e)
        {   
            if (AtopSerial.MainWindow.toDataMonitor.allSelectSw == true)
            {
                AtopSerial.MainWindow.toDataMonitor.allSelectSw = false;
                for (var iTemp = 0; iTemp < toMonitorItems.Count; iTemp++)
                {
                    toMonitorItems[iTemp].sampleEn = false;
                    if (AtopSerial.MainWindow.toDataMonitor.selectShowSw == true)
                        toMonitorItems[iTemp].visibility = Visibility.Collapsed;
                }
            }
            else
            {
                AtopSerial.MainWindow.toDataMonitor.allSelectSw = true;
                for (var iTemp = 0; iTemp < toMonitorItems.Count; iTemp++)
                {
                    toMonitorItems[iTemp].visibility = Visibility.Visible;
                    toMonitorItems[iTemp].sampleEn = true;
                }
            }
        }

        private void PlotFrame_DragEnter(object sender, DragEventArgs e)
        {

        }
    }

    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class ToDataMonitor
    {
        public bool allSelectSw { get; set; } = false;
        public bool selectShowSw { get; set; } = false;
    }

    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class ToDataMonitorItems
    {
        public static event EventHandler DataChanged;
        private Visibility _visibility = Visibility.Visible;
        private int _channel = 0;
        private string _description = "";
        private string _result = "";
        private bool _sampleEn = false;

        public Visibility visibility
        {
            get => _visibility;
            set
            {
                _visibility = value;
                DataChanged?.Invoke(0, EventArgs.Empty);
            }
        }
        public int channel
        {
            get => _channel;
            set
            {
                _channel = value;
                //DataChanged?.Invoke(0, EventArgs.Empty);
            }
        }
        public string description
        {
            get => _description;
            set
            {
                _description = value;
                DataChanged?.Invoke(0, EventArgs.Empty);
            }
        }
        public string result
        {
            get => _result;
            set
            {
                _result = value;
                //DataChanged?.Invoke(0, EventArgs.Empty);
            }
        }
        public bool sampleEn
        {
            get => _sampleEn;
            set
            {
                _sampleEn = value;
                DataChanged?.Invoke(0, EventArgs.Empty);
            }
        }
    }

    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class ToGraphTool
    {
        public int graphToolSampleLen { get; set; } = 500;
        public int graphToolSampleInterval { get; set; } = 0;
        public int graphToolTriggerPara { get; set; } = 0;
        public int graphToolTriggerLag { get; set; } = 0;
        public int graphToolSampleMode { get; set; } = 0;
    }

    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class ToSnatchGraphItems
    {
        public static event EventHandler DataChanged;
        private int _channel = 0;
        private bool _show = false;
        private string _description = "";
        private string _dataSour = "";
        private string _scaleY = "";
        private string _offset = "";
        private int _type = 0;
        public int channel
        {
            get => _channel;
            set
            {
                _channel = value;
                DataChanged?.Invoke(0, EventArgs.Empty);
            }
        }
        public bool show
        {
            get => _show;
            set
            {
                _show = value;
                DataChanged?.Invoke(0, EventArgs.Empty);
            }
        }
        public string description
        {
            get => _description;
            set
            {
                _description = value;
                DataChanged?.Invoke(0, EventArgs.Empty);
            }
        }
        public string dataSour
        {
            get => _dataSour;
            set
            {
                _dataSour = value;
                DataChanged?.Invoke(0, EventArgs.Empty);
            }
        }
        public string scaleY
        {
            get => _scaleY;
            set
            {
                _scaleY = value;
                DataChanged?.Invoke(0, EventArgs.Empty);
            }
        }
        public string offset
        {
            get => _offset;
            set
            {
                _offset = value;
                DataChanged?.Invoke(0, EventArgs.Empty);
            }
        }
        public int type
        {
            get => _type;
            set
            {
                _type = value;
                DataChanged?.Invoke(0, EventArgs.Empty);
            }
        }
    }
}
