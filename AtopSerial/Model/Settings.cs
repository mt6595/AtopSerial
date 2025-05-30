using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Forms;

namespace AtopSerial.Model
{
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    class Settings
    {
        public event EventHandler MainWindowTop;
        private double _windowTop = 0;
        private double _windowLeft = 0;
        private double _windowWidth = 0;
        private double _windowHeight = 0;
        private double _logScreenWidth = 0;
        private string _dataToSend = "uart data";
        private int _baudRate = 115200;
        private bool _autoReconnect = true;
        private bool _autoSaveLog = true;
        private int _showHexFormat = 0;
        private bool _hexSend = false;
        private int _subpackageShow = 0;
        private bool _showSend = true;
        private int _parity = 0;
        private int _timeout = 20;
        private int _dataBits = 8;
        private int _stopBit = 1;
        private bool _escapeSend = false;
        private int _graphTheme = 11;
        private string _sendScript = "default";
        private string _recvScript = "default";
        private string _runScript = "example";
        private bool _topmost = false;
        private uint _maxLength = 10240;
        private int _maxGraphLength = 1000;
        private string _language = "en-US";
        private int _encoding = 65001;
        private bool _enableSymbol = true;
        private bool _graphSw = true;
        private bool _viewAutoSw = true;
        private bool _extendDataSendGrid = false;
        private int _maxLogPackShow = 1024 * 2;
        private int _maxLogAutoClear = 1024*100;
        private int _automaticSendTimer = 1000;
        private int _plotDoubleClickFullScreen = 0;
        private bool _plotShowRefreshRate = false;
        private bool _plotGridShow = true;
        private int _plotLineWidth = 1;
        private int _plotStyle = 1;
        private int _plotRenderQuality = 2;
        private string _quickListName0 = "Quick Group 0";
        private string _quickListName1 = "Quick Group 1";
        private string _quickListName2 = "Quick Group 2";
        private string _quickListName3 = "Quick Group 3";
        private string _quickListName4 = "Quick Group 4";
        private string _quickListName5 = "Quick Group 5";
        private string _quickListName6 = "Quick Group 6";
        private string _quickListName7 = "Quick Group 7";
        private string _quickListName8 = "Quick Group 8";
        private string _quickListName9 = "Quick Group 9";
        private int _quickSendSelect = -1;
        private int _snatchGraphListNum = -1;
        private int _dataMonitorListNum = -1;
        public List<List<ToSendData>> _quickSendList = new List<List<ToSendData>>();
        public List<ToDataMonitorItems> _toDataMonitorItemsSave = new List<ToDataMonitorItems>();
        public List<ToSnatchGraphItems> _toSnatchGraphItemsSave = new List<ToSnatchGraphItems>();

        //窗口大小与位置
        public double windowTop { get { return _windowTop; } set { _windowTop = value; SaveConfig(); } }
        public double windowLeft { get { return _windowLeft; } set { _windowLeft = value; SaveConfig(); } }
        public double windowWidth { get { return _windowWidth; } set { _windowWidth = value; SaveConfig(); } }
        public double windowHeight { get { return _windowHeight; } set { _windowHeight = value; SaveConfig(); } }
        public double logScreenWidth { get { return _logScreenWidth; } set { _logScreenWidth = value; SaveConfig(); } }

        public int SentCount { get; set; } = 0;
        public int ReceivedCount { get; set; } = 0;

        /// <summary>
        /// 保存配置
        /// </summary>
        private void SaveConfig()
        {
            File.WriteAllText(Tools.Global.ProfilePath+"settings.json", JsonConvert.SerializeObject(this));
        }

        /// <summary>
        /// 串口接收每包最大长度
        /// </summary>
        public uint maxLength
        {
            get
            {
                return _maxLength;
            }
            set
            {
                _maxLength = value;
                SaveConfig();
            }
        }
        /// <summary>
        /// 曲线最大长度设定
        /// </summary>
        public int maxGraphLength
        {
            get
            {
                return _maxGraphLength;
            }
            set
            {
                _maxGraphLength = value;
                SaveConfig();
            }
        }

        /// <summary>
        /// 文本视图和曲线视图切换
        /// </summary>
        public bool graphSw
        {
            get
            {
                return _graphSw;
            }
            set
            {
                _graphSw = value;
                SaveConfig();
            }
        }
        /// <summary>
        /// 自动切换文本视图和曲线视图
        /// </summary>
        public bool viewAutoSw
        {
            get
            {
                return _viewAutoSw;
            }
            set
            {
                _viewAutoSw = value;
                SaveConfig();
            }
        }
        /// <summary>
        /// 扩展数据发送区域
        /// </summary>
        public bool extendDataSendGrid
        {
            get
            {
                return _extendDataSendGrid;
            }
            set
            {
                _extendDataSendGrid = value;
                SaveConfig();
            }
        }
        
        /// <summary>
        /// 当前选中的快捷发送列表数据
        /// </summary>
        public List<ToSendData> quickSend
        {
            get
            {
                if (_quickSendSelect < 0 || _quickSendSelect > 10)
                    return new List<ToSendData>();
                if (_quickSendList.Count < 10)
                {
                    for (var i = _quickSendList.Count; i < 10; i++)
                        _quickSendList.Add(new List<ToSendData>());
                }
                return _quickSendList[_quickSendSelect];
            }
            set
            {
                if (_quickSendSelect < 0 || _quickSendSelect > 10)
                    return;
                if (_quickSendList.Count < 10)
                {
                    for (var iTemp = _quickSendList.Count; iTemp < 10; iTemp++)
                        _quickSendList.Add(new List<ToSendData>());
                }
                _quickSendList[_quickSendSelect] = value;
                SaveConfig();
            }
        }

        /// <summary>
        /// 数据监测区列表数据
        /// </summary>
        public List<ToDataMonitorItems> toDataMonitorItemsSave
        {
            get
            {
                if (_dataMonitorListNum < 0)
                    return new List<ToDataMonitorItems>();
                return _toDataMonitorItemsSave;
            }
            set
            {
                if (_dataMonitorListNum < 0)
                    return;
                _toDataMonitorItemsSave = value;
                SaveConfig();
            }
        }
        /// <summary>
        /// 抓图工具区列表数据
        /// </summary>
        public List<ToSnatchGraphItems> toSnatchGraphItemsSave
        {
            get
            {
                if (_snatchGraphListNum < 0)
                    return new List<ToSnatchGraphItems>();
                return _toSnatchGraphItemsSave;
            }
            set
            {
                if (_snatchGraphListNum < 0)
                    return;
                _toSnatchGraphItemsSave = value;
                SaveConfig();
            }
        }

        /// <summary>
        /// 自动循环发送时间间隔
        /// </summary>
        public int automaticSendTimer
        {
            get
            {
                return _automaticSendTimer;
            }
            set
            {
                _automaticSendTimer = value;
                SaveConfig();
            }
        }

        /// <summary>
        /// 当前选中的快速发送列表编号
        /// </summary>
        public int quickSendSelect
        {
            get
            {
                return _quickSendSelect;
            }
            set
            {
                _quickSendSelect = value;
                SaveConfig();
            }
        }

        /// <summary>
        /// 数据监测列表数量
        /// </summary>
        public int dataMonitorListNum
        {
            get
            {
                return _dataMonitorListNum;
            }
            set
            {
                _dataMonitorListNum = value;
                SaveConfig();
            }
        }

        /// <summary>
        /// 抓图工具列表数量
        /// </summary>
        public int snatchGraphListNum
        {
            get
            {
                return _snatchGraphListNum;
            }
            set
            {
                _snatchGraphListNum = value;
                SaveConfig();
            }
        }

        /// <summary>
        /// 串口发送区数据
        /// </summary>
        public string dataToSend
        {
            get
            {
                return _dataToSend;
            }
            set
            {
                _dataToSend = value;
                SaveConfig();
            }
        }
        /// <summary>
        /// 串口波特率设置
        /// </summary>
        public int baudRate
        {
            get
            {
                return _baudRate;
            }
            set
            {
                try
                {
                    Tools.Global.uart.serial.BaudRate = value;
                    _baudRate = value;
                    SaveConfig();
                }
                catch(Exception e)
                {
                    Tools.MessageBox.Show(e.Message);
                }
            }
        }

        /// <summary>
        /// 自动恢复被断开的串口连接
        /// </summary>
        public bool autoReconnect
        {
            get
            {
                return _autoReconnect;
            }
            set
            {
                _autoReconnect = value;
                SaveConfig();
            }
        }

        /// <summary>
        /// 自动保存Log
        /// </summary>
        public bool autoSaveLog
        {
            get
            {
                return _autoSaveLog;
            }
            set
            {
                _autoSaveLog = value;
                SaveConfig();
            }
        }

        /// <summary>
        /// 串口数据显示格式
        /// 0 都显示
        /// 1 只显示字符串
        /// 2 只显示Hex
        /// </summary>
        public int showHexFormat
        {
            get
            {
                return _showHexFormat;
            }
            set
            {
                _showHexFormat = value;
                SaveConfig();
            }
        }

        /// <summary>
        /// 主数据发送框是否发hex
        /// </summary>
        public bool hexSend
        {
            get
            {
                return _hexSend;
            }
            set
            {
                _hexSend = value;
                SaveConfig();
            }
        }

        /// <summary>
        /// 是否需要时间戳分包显示
        /// </summary>
        public int subpackageShow
        {
            get
            {
                return _subpackageShow;
            }
            set
            {
                _subpackageShow = value;
                SaveConfig();
            }
        }

        /// <summary>
        /// 文本区显示串口发送出去的数据
        /// </summary>
        public bool showSend
        {
            get
            {
                return _showSend;
            }
            set
            {
                _showSend = value;
                SaveConfig();
            }
        }

        /// <summary>
        /// 串口校验方式
        /// </summary>
        public int parity
        {
            get
            {
                return _parity;
            }
            set
            {
                try
                {
                    _parity = value;
                    Tools.Global.uart.serial.Parity = (Parity)value;
                    SaveConfig();
                }
                catch (Exception e)
                {
                    Tools.MessageBox.Show(e.Message);
                }
            }
        }

        /// <summary>
        /// 分包数据超时时间
        /// </summary>
        public int timeout
        {
            get
            {
                return _timeout;
            }
            set
            {
                _timeout = value;
                SaveConfig();
            }
        }

        /// <summary>
        /// 数据位长度
        /// </summary>
        public int dataBits
        {
            get
            {
                return _dataBits;
            }
            set
            {
                try
                {
                    _dataBits = value;
                    Tools.Global.uart.serial.DataBits = value;
                    SaveConfig();
                }
                catch (Exception e)
                {
                    Tools.MessageBox.Show(e.Message);
                }
            }
        }

        /// <summary>
        /// 停止位长度
        /// </summary>
        public int stopBit
        {
            get
            {
                return _stopBit;
            }
            set
            {
                try
                {
                    _stopBit = value;
                    Tools.Global.uart.serial.StopBits = (StopBits)value;
                    SaveConfig();
                }
                catch (Exception e)
                {
                    Tools.MessageBox.Show(e.Message);
                }
            }
        }
        /// <summary>
        /// 停止位长度
        /// </summary>
        public bool escapeSend
        {
            get
            {
                return _escapeSend;
            }
            set
            {
                _escapeSend = value;
                SaveConfig();
            }
        }
        
        /// <summary>
        /// 曲线主题
        /// </summary>
        public int graphTheme
        {
            get
            {
                return _graphTheme;
            }
            set
            {
                _graphTheme = value;
                SaveConfig();
            }
        }

        /// <summary>
        /// 曲线渲染质量
        /// </summary>
        public int plotRenderQuality
        {
            get
            {
                return _plotRenderQuality;
            }
            set
            {
                _plotRenderQuality = value;
                SaveConfig();
            }
        }

        /// <summary>
        /// 曲线双击满屏
        /// </summary>
        public int plotDoubleClickFullScreen
        {
            get
            {
                return _plotDoubleClickFullScreen;
            }
            set
            {
                _plotDoubleClickFullScreen = value;
                SaveConfig();
            }
        }
        

        /// <summary>
        /// 曲线显示刷新率
        /// </summary>
        public bool plotShowRefreshRate
        {
            get
            {
                return _plotShowRefreshRate;
            }
            set
            {
                _plotShowRefreshRate = value;
                SaveConfig();
            }
        }

        /// <summary>
        /// 曲线显示刷新率
        /// </summary>
        public bool plotGridShow
        {
            get
            {
                return _plotGridShow;
            }
            set
            {
                _plotGridShow = value;
                SaveConfig();
            }
        }

        /// <summary>
        /// 曲线线宽
        /// </summary>
        public int plotLineWidth
        {
            get
            {
                return _plotLineWidth;
            }
            set
            {
                _plotLineWidth = value;
                SaveConfig();
            }
        }
        /// <summary>
        /// 曲线风格
        /// </summary>
        public int plotStyle
        {
            get
            {
                return _plotStyle;
            }
            set
            {
                _plotStyle = value;
                SaveConfig();
            }
        }

        /// <summary>
        /// 发送预处理脚本文件名
        /// </summary>
        public string sendScript
        {
            get
            {
                return _sendScript;
            }
            set
            {
                _sendScript = value;
                SaveConfig();
            }
        }

        /// <summary>
        /// 接收预处理脚本文件名
        /// </summary>
        public string recvScript
        {
            get
            {
                return _recvScript;
            }
            set
            {
                _recvScript = value;
                SaveConfig();
            }
        }

        /// <summary>
        /// 运行脚本区脚本文件名
        /// </summary>
        public string runScript
        {
            get
            {
                return _runScript;
            }
            set
            {
                _runScript = value;
                SaveConfig();
            }
        }

        /// <summary>
        /// 串口置顶
        /// </summary>
        public bool topmost
        {
            get
            {
                return _topmost;
            }
            set
            {
                _topmost = value;
                try
                {
                    MainWindowTop(value, EventArgs.Empty);
                }
                catch { }
                SaveConfig();
            }
        }

        /// <summary>
        /// 语言选择
        /// </summary>
        public string language
        {
            get
            {
                return _language;
            }
            set
            {
                _language = value;
                Tools.Global.LoadLanguageFile(value);
                SaveConfig();
            }
        }

        /// <summary>
        /// 字符编码
        /// </summary>
        public int encoding
        {
            get
            {
                return _encoding;
            }
            set
            {
                try
                {
                    Encoding.GetEncoding(value);
                    _encoding = value;
                    SaveConfig();
                }
                catch { }//获取出错说明编码不对
            }
        }

        /// <summary>
        /// 日志记录
        /// </summary>
        public bool DisableLog { get; set; } = false;

        /// <summary>
        /// 替换不可见字符
        /// </summary>
        public bool enableSymbol
        {
            get => _enableSymbol;
            set
            {
                _enableSymbol = value;
                SaveConfig();
            }
        }

        public string quickListName0 { get { return _quickListName0; } set { _quickListName0 = value; SaveConfig(); } }
        public string quickListName1 { get { return _quickListName1; } set { _quickListName1 = value; SaveConfig(); } }
        public string quickListName2 { get { return _quickListName2; } set { _quickListName2 = value; SaveConfig(); } }
        public string quickListName3 { get { return _quickListName3; } set { _quickListName3 = value; SaveConfig(); } }
        public string quickListName4 { get { return _quickListName4; } set { _quickListName4 = value; SaveConfig(); } }
        public string quickListName5 { get { return _quickListName5; } set { _quickListName5 = value; SaveConfig(); } }
        public string quickListName6 { get { return _quickListName6; } set { _quickListName6 = value; SaveConfig(); } }
        public string quickListName7 { get { return _quickListName7; } set { _quickListName7 = value; SaveConfig(); } }
        public string quickListName8 { get { return _quickListName8; } set { _quickListName8 = value; SaveConfig(); } }
        public string quickListName9 { get { return _quickListName9; } set { _quickListName9 = value; SaveConfig(); } }
        public string GetQuickListNameNow()
        {
            return _quickSendSelect switch
            {
                0 => quickListName0,
                1 => quickListName1,
                2 => quickListName2,
                3 => quickListName3,
                4 => quickListName4,
                5 => quickListName5,
                6 => quickListName6,
                7 => quickListName7,
                8 => quickListName8,
                9 => quickListName9,
                _ => "??",
            };
        }
        public void SetQuickListNameNow(string name)
        {
            switch (_quickSendSelect)
            {
                case 0:
                    quickListName0 = name;
                    break;
                case 1:
                    quickListName1 = name;
                    break;
                case 2:
                    quickListName2 = name;
                    break;
                case 3:
                    quickListName3 = name;
                    break;
                case 4:
                    quickListName4 = name;
                    break;
                case 5:
                    quickListName5 = name;
                    break;
                case 6:
                    quickListName6 = name;
                    break;
                case 7:
                    quickListName7 = name;
                    break;
                case 8:
                    quickListName8 = name;
                    break;
                case 9:
                    quickListName9 = name;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// log显示时，一包最大显示长度
        /// </summary>
        public int maxLogPackShow { get { return _maxLogPackShow; } set { _maxLogPackShow = value; SaveConfig(); } }

        /// <summary>
        /// log显示时，最大的总数据量
        /// </summary>
        public int maxLogAutoClear { get { return _maxLogAutoClear; } set { _maxLogAutoClear = value; SaveConfig(); } }
    }
}
