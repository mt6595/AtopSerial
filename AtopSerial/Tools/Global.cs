using LibUsbDotNet.Info;
using LibUsbDotNet.LibUsb;
using AtopSerial.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Shapes;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace AtopSerial.Tools
{
    class Global
    {
        public static event EventHandler ProgramClosedEvent;
        //api接口文档网址
        public static string apiDocumentUrl = "https://github.com/mt6595/AtopSerial/blob/main/LuaApi.md";
        //主窗口是否被关闭？
        private static bool _isMainWindowsClosed = false;
        public static bool isMainWindowsClosed
        {
            get
            {
                return _isMainWindowsClosed;
            }
            set
            {
                _isMainWindowsClosed = value;
                if (value)
                {
                    uart.WaitUartReceive.Set();
                    Logger.CloseUartLog();
                    Logger.CloseLuaLog();
                    if (File.Exists(ProfilePath + "lock"))
                        File.Delete(ProfilePath + "lock");
                    ProgramClosedEvent?.Invoke(null,EventArgs.Empty);
                }
            }
        }
        //给全局使用的设置参数项
        public static Model.Settings setting;
        public static Model.Uart uart = new Model.Uart();

        //软件文件名
        private static string _fileName = "";
        public static string FileName
        {
            get
            {
                if (String.IsNullOrWhiteSpace(_fileName))
                {
                    using (var processModule = Process.GetCurrentProcess().MainModule)
                    {
                        _fileName = System.IO.Path.GetFileName(processModule?.FileName);
                    }
                }
                return _fileName;
            }
        }

        //软件根目录
        private static string _appPath = null;
        /// <summary>
        /// 软件根目录（末尾带\）
        /// </summary>
        public static string AppPath
        {
            get
            {
                if (_appPath == null)
                {
                    using (var processModule = Process.GetCurrentProcess().MainModule)
                    {
                        _appPath = System.IO.Path.GetDirectoryName(processModule?.FileName);
                    }
                    if (!_appPath.EndsWith("\\"))
                        _appPath = _appPath + "\\";
                }
                return _appPath;
            }
        }

        //配置文件路径（普通exe时，会被替换为AppPath）
        public static string ProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\AtopSerial\";

        /// <summary>
        /// 获取实际的ProfilePath路径（目前没啥用了）
        /// </summary>
        /// <returns></returns>

        /// <summary>
        /// 是否为应用商店版本？
        /// </summary>
        /// <returns></returns>
        public static bool IsMSIX()
        {
            return AppPath.ToUpper().Contains(@"\PROGRAM FILES\WINDOWSAPPS\");
        }

        /// <summary>
        /// 是否上报bug？低版本.net框架的上报行为将被限制
        /// </summary>
        public static bool ReportBug { get; set; } = true;

        /// <summary>
        /// 是否有新版本？
        /// </summary>
        public static bool HasNewVersion { get; set; } = false;


        /// <summary>
        /// 更换软件标题栏文字
        /// </summary>
        public static event EventHandler<string> ChangeTitleEvent;
        public static void ChangeTitle(string s) => ChangeTitleEvent?.Invoke(null, s);

        /// <summary>
        /// 刷新lua脚本列表
        /// </summary>
        public static event EventHandler RefreshLuaScriptListEvent;
        public static void RefreshLuaScriptList() => RefreshLuaScriptListEvent?.Invoke(null, null);

        /// <summary>
        /// 加载配置文件
        /// </summary>
        public static void LoadSetting()
        {
            if (IsMSIX())
            {
                if (Directory.Exists(ProfilePath))
                {
                    //已经开过一次了，那就继续用之前的路径
                }
                else
                {
                    //appdata路径不可靠，用文档路径替代
                    ProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\AtopSerial\\";
                    if (!Directory.Exists(ProfilePath))
                        Directory.CreateDirectory(ProfilePath);
                }
            }
            else
            {
                ProfilePath = AppPath;//普通exe时，直接用软件路径
            }
            //配置文件
            if (File.Exists(ProfilePath + "settings.json"))
            {
                try
                {
                    //cost 309ms
                    setting = JsonConvert.DeserializeObject<Model.Settings>(File.ReadAllText(ProfilePath + "settings.json"));
                    setting.SentCount = 0;
                    setting.ReceivedCount = 0;
                    setting.DisableLog = false;
                    LoadLanguageFile(setting.language);
                }
                catch
                {
                    Tools.MessageBox.Show($"Configuration file loading failed!\r\n" +
                        $"If the configuration file is corrupted, you can go to {ProfilePath}settings.json.bakupto find the backup file.\r\n" +
                        $"And use this file to replace{ProfilePath}settings.jsonto restore the configuration.");
                    Environment.Exit(1);
                }
            }
            else
            {
                setting = new Model.Settings();
                LoadLanguageFile(setting.language);
            }
        }

        /// <summary>
        /// 软件打开后，所有东西的初始化流程
        /// </summary>
        public static void Initial()
        {
            //检查.net版本
            var currentVersion = Walterlv.NdpInfo.GetCurrentVersionName();
            try
            {
                if (currentVersion.StartsWith("4."))
                {
                    var pNetVersion = int.Parse(currentVersion.Substring(2, 1));
                    if (pNetVersion < 6)
                        throw new Exception();
                }
                else
                {
                    throw new Exception();
                }
            }
            catch
            {
                Tools.MessageBox.Show($"This software only supports .NET Framework version 4.6.2 and above, while the highest version installed on this computer is {currentVersion}\r\n" +
                    $"You can choose to continue to use, but if you encounter any bugs during operation, they will not be reported to the developer.\r\n" +
                    $"Recommend upgrading to the latest version of .NET Framework.");
                ReportBug = false;
            }

            // 检查程序是否在桌面上运行
            //if (Environment.GetFolderPath(Environment.SpecialFolder.Desktop) == Environment.CurrentDirectory)
            //{
            //    Tools.MessageBox.Show("exe executor cannot run on desktop directory.！");
            //    Environment.Exit(1);
            //}

            //文件名不能改！
            if (FileName.ToUpper() != "ATOPSERIAL.EXE")
            {
                Tools.MessageBox.Show("For the normal operation of the software, please rename the executable file to AtopSerial.exe.");
                //Tools.MessageBox.Show("For the normal operation of the software, please rename the executable file to AtopSerial.exe.");
                Environment.Exit(1);
            }
            //C:\Users\chenx\AppData\Local\Temp\7zO05433053\user_script_run
            if (AppPath.ToUpper().Contains(@"\APPDATA\LOCAL\TEMP\") ||
                AppPath.ToUpper().Contains(@"\WINDOWS\TEMP\"))
            {
                Tools.MessageBox.Show("Do not run within the compressed file.");
                Environment.Exit(1);
            }

            if (IsMSIX())//商店软件的文件路径需要手动新建文件夹
            {
                if (!Directory.Exists(ProfilePath))
                {
                    Directory.CreateDirectory(ProfilePath);
                }
                //升级的时候不会自动升级核心脚本，所以先强制删掉再释放，确保是最新的
                if (Directory.Exists(ProfilePath + "core_script"))
                    Directory.Delete(ProfilePath + "core_script", true);
            }

            //检测多开
            string processName = Process.GetCurrentProcess().ProcessName;
            Process[] processes = Process.GetProcessesByName(processName);
            //如果该数组长度大于1，说明多次运行
            if (processes.Length > 1 && File.Exists(ProfilePath + "lock"))
            {
                Tools.MessageBox.Show("Not support running multiple instances in the same folder!\r\nPlease run the .exe in multiple folders.");
                Environment.Exit(1);
            }
            File.Create(ProfilePath + "lock").Close();

            try
            {
                if (!Directory.Exists(ProfilePath + "core_script"))
                {
                    Directory.CreateDirectory(ProfilePath + "core_script");
                }
                CreateFile("DefaultFiles/core_script/head.lua", ProfilePath + "core_script/head.lua", true);
                CreateFile("DefaultFiles/core_script/JSON.lua", ProfilePath + "core_script/JSON.lua", false);
                CreateFile("DefaultFiles/core_script/log.lua", ProfilePath + "core_script/log.lua", false);
                CreateFile("DefaultFiles/core_script/strings.lua", ProfilePath + "core_script/strings.lua", false);
                CreateFile("DefaultFiles/core_script/sys.lua", ProfilePath + "core_script/sys.lua", true);

                if (!Directory.Exists(ProfilePath + "logs"))
                    Directory.CreateDirectory(ProfilePath + "logs");

                if (!Directory.Exists(ProfilePath + "user_script/user_script_run"))
                {
                    Directory.CreateDirectory(ProfilePath + "user_script/user_script_run");
                    CreateFile("DefaultFiles/user_script/user_script_run/example.lua", ProfilePath + "user_script/user_script_run/example.lua");
                    CreateFile("DefaultFiles/user_script/user_script_run/InputBoxDemo.lua", ProfilePath + "user_script/user_script_run/InputBoxDemo.lua");
                    CreateFile("DefaultFiles/user_script/user_script_run/LogPrint.lua", ProfilePath + "user_script/user_script_run/LogPrint.lua");
                    CreateFile("DefaultFiles/user_script/user_script_run/PoltDemo.lua", ProfilePath + "user_script/user_script_run/PoltDemo.lua");
                    CreateFile("DefaultFiles/user_script/user_script_run/QuickSend1Demo.lua", ProfilePath + "user_script/user_script_run/QuickSend1Demo.lua");
                    CreateFile("DefaultFiles/user_script/user_script_run/QuickSend2Demo.lua", ProfilePath + "user_script/user_script_run/QuickSend2Demo.lua");
                    CreateFile("DefaultFiles/user_script/user_script_run/QuickSend3Demo.lua", ProfilePath + "user_script/user_script_run/QuickSend3Demo.lua");
                    CreateFile("DefaultFiles/user_script/user_script_run/ReceiveHandleDemo.lua", ProfilePath + "user_script/user_script_run/ReceiveHandleDemo.lua");
                    CreateFile("DefaultFiles/user_script/user_script_run/TimerDemo.lua", ProfilePath + "user_script/user_script_run/TimerDemo.lua");
                    CreateFile("DefaultFiles/user_script/user_script_run/VoltronicPowerGraph.lua", ProfilePath + "user_script/user_script_run/VoltronicPowerGraph.lua");
                    CreateFile("DefaultFiles/user_script/user_script_run/WIFI AT Command Test.lua", ProfilePath + "user_script/user_script_run/WIFI AT Command Test.lua");
                    CreateFile("DefaultFiles/user_script/user_script_run/SimulateBMS.lua", ProfilePath + "user_script/user_script_run/SimulateBMS.lua");
                }

                if (!Directory.Exists(ProfilePath + "user_script/user_script_run/requires"))
                    Directory.CreateDirectory(ProfilePath + "user_script/user_script_run/requires");

                if (!Directory.Exists(ProfilePath + "user_script/user_script_run/logs"))
                    Directory.CreateDirectory(ProfilePath + "user_script/user_script_run/logs");

                if (!Directory.Exists(ProfilePath + "user_script/user_script_send_convert"))
                {
                    Directory.CreateDirectory(ProfilePath + "user_script/user_script_send_convert");
                    CreateFile("DefaultFiles/user_script/user_script_send_convert/Check Sum.lua", ProfilePath + "user_script/user_script_send_convert/Check Sum.lua");
                    CreateFile("DefaultFiles/user_script/user_script_send_convert/Hex Conversions.lua", ProfilePath + "user_script/user_script_send_convert/Hex Conversions.lua");
                    CreateFile("DefaultFiles/user_script/user_script_send_convert/GPS NMEA.lua", ProfilePath + "user_script/user_script_send_convert/GPS NMEA.lua");
                    CreateFile("DefaultFiles/user_script/user_script_send_convert/Add CR.lua", ProfilePath + "user_script/user_script_send_convert/Add CR.lua");
                    CreateFile("DefaultFiles/user_script/user_script_send_convert/VoltronicPowerCRC.lua", ProfilePath + "user_script/user_script_send_convert/VoltronicPowerCRC.lua");
                    CreateFile("DefaultFiles/user_script/user_script_send_convert/Parsing Escape Char.lua", ProfilePath + "user_script/user_script_send_convert/Parsing Escape Char.lua");
                    CreateFile("DefaultFiles/user_script/user_script_send_convert/default.lua", ProfilePath + "user_script/user_script_send_convert/default.lua");
                    CreateFile("DefaultFiles/user_script/user_script_send_convert/ModbusCRC.lua", ProfilePath + "user_script/user_script_send_convert/ModbusCRC.lua");
                }
                if (!Directory.Exists(ProfilePath + "user_script/user_script_recv_convert"))
                {
                    Directory.CreateDirectory(ProfilePath + "user_script/user_script_recv_convert");
                    CreateFile("DefaultFiles/user_script/user_script_recv_convert/default.lua", ProfilePath + "user_script/user_script_recv_convert/default.lua");
                    CreateFile("DefaultFiles/user_script/user_script_recv_convert/CurvePlot1.lua", ProfilePath + "user_script/user_script_recv_convert/CurvePlot1.lua");
                    CreateFile("DefaultFiles/user_script/user_script_recv_convert/CurvePlot2.lua", ProfilePath + "user_script/user_script_recv_convert/CurvePlot2.lua");
                    CreateFile("DefaultFiles/user_script/user_script_recv_convert/CurvePlot3.lua", ProfilePath + "user_script/user_script_recv_convert/CurvePlot3.lua");
                }

                if (!Directory.Exists(ProfilePath + "user_script/graph_script"))
                {
                    Directory.CreateDirectory(ProfilePath + "user_script/graph_script");
                    CreateFile("DefaultFiles/user_script/graph_script/default.lua", ProfilePath + "user_script/graph_script/default.lua");
                }

                if (!Directory.Exists(ProfilePath + "user_script/graph_script/logs"))
                    Directory.CreateDirectory(ProfilePath + "user_script/graph_script/logs");

                if (!Directory.Exists(ProfilePath + "user_script/monitor_script"))
                {
                    Directory.CreateDirectory(ProfilePath + "user_script/monitor_script");
                    CreateFile("DefaultFiles/user_script/monitor_script/default.lua", ProfilePath + "user_script/monitor_script/default.lua");
                }

                if (!Directory.Exists(ProfilePath + "user_script/monitor_script/logs"))
                    Directory.CreateDirectory(ProfilePath + "user_script/monitor_script/logs");

                CreateFile("DefaultFiles/LICENSE", ProfilePath + "LICENSE", false);
                CreateFile("DefaultFiles/反馈网址.txt", ProfilePath + "反馈网址.txt", false);

                if (IntPtr.Size == 8)
                    CreateFile("DefaultFiles/libusb-1.0-x64.dll", ProfilePath + "libusb-1.0", false);
                else
                    CreateFile("DefaultFiles/libusb-1.0-x86.dll", ProfilePath + "libusb-1.0", false);
            }
            catch (Exception e)
            {
                Tools.MessageBox.Show("Failed to generate file structure. Please ensure that the software is opened in a directory with read and write permissions.\r\nError message:" + e.Message);
                Environment.Exit(1);
            }

            //加载配置文件改成单独拎出来了

            //备份一下文件好了（心理安慰）
            if (File.Exists(ProfilePath + "settings.json"))
            {
                if (File.Exists(ProfilePath + "settings.json.bakup"))
                    File.Delete(ProfilePath + "settings.json.bakup");
                File.Copy(ProfilePath + "settings.json", ProfilePath + "settings.json.bakup");
            }

            uart.serial.BaudRate = setting.baudRate;
            uart.serial.Parity = (Parity)setting.parity;
            uart.serial.DataBits = setting.dataBits;
            uart.serial.StopBits = (StopBits)setting.stopBit;
            uart.UartDataRecived += Uart_UartDataRecived;
            uart.UartDataSent += Uart_UartDataSent;
        }

        /// <summary>
        /// 已发送记录到日志
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Uart_UartDataSent(object sender, EventArgs e)
        {
            Logger.AddUartLogInfo($"»{Byte2Readable((byte[])sender)}");
            Logger.AddUartLogInfo($"[HEX]{Byte2Hex((byte[])sender, " ")}");
        }

        /// <summary>
        /// 收到的数据记录到日志
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Uart_UartDataRecived(object sender, EventArgs e)
        {
            Logger.AddUartLogInfo($"«{Byte2Readable((byte[])sender)}");
            Logger.AddUartLogInfo($"[HEX]{Byte2Hex((byte[])sender, " ")}");
        }

        public static Encoding GetEncoding() => Encoding.GetEncoding(setting.encoding);

        /// <summary>
        /// 字符串转hex值
        /// </summary>
        /// <param name="str">字符串</param>
        /// <param name="space">间隔符号</param>
        /// <returns>结果</returns>
        public static string String2Hex(string str, string space)
        {
            return BitConverter.ToString(GetEncoding().GetBytes(str)).Replace("-", space);
        }


        /// <summary>
        /// hex值转字符串
        /// </summary>
        /// <param name="mHex">hex值</param>
        /// <returns>原始字符串</returns>
        public static string Hex2String(string mHex)
        {
            mHex = Regex.Replace(mHex, "[^0-9A-Fa-f]", "");
            if (mHex.Length % 2 != 0)
                mHex = mHex.Remove(mHex.Length - 1, 1);
            if (mHex.Length <= 0) return "";
            byte[] vBytes = new byte[mHex.Length / 2];
            for (int i = 0; i < mHex.Length; i += 2)
                if (!byte.TryParse(mHex.Substring(i, 2), NumberStyles.HexNumber, null, out vBytes[i / 2]))
                    vBytes[i / 2] = 0;
            return GetEncoding().GetString(vBytes);
        }


        /// <summary>
        /// byte转string
        /// </summary>
        /// <param name="mHex"></param>
        /// <returns></returns>
        public static string Byte2String(byte[] vBytes, int len = -1)
        {
            var br = from e in vBytes
                     where e != 0
                     select e;
            if (len == -1 || len > br.Count())
                len = br.Count();
            return GetEncoding().GetString(br.Take(len).ToArray());
        }

        /// <summary>
        /// byte转string（可读）
        /// </summary>
        /// <param name="vBytes"></param>
        /// <returns></returns>
        public static string Byte2Readable(byte[] vBytes, int len = -1)
        {
            if (len == -1)
                len = vBytes.Length;
            if (vBytes == null)
                return "";
            //utf8编码下的替换不可见字符
            if (!setting.enableSymbol || setting.encoding != 65001)
                return Byte2String(vBytes, len);
            var tb = new List<byte>();
            for (int i = 0; i < len; i++)
            {
                switch (vBytes[i])
                {
                    case 0x0d://CR
                        tb.Add(vBytes[i]);
                        break;
                    case 0x0a://LF
                        tb.Add(vBytes[i]);
                        break;
                    case 0x09://TAB
                        tb.Add(vBytes[i]);
                        break;
                    default:
                        if (vBytes[i] <= 0x1f || vBytes[i] == 0x7f)
                            tb.AddRange(new byte[] {0xe2, 0x98, 0x90});
                        else
                            tb.Add(vBytes[i]);
                        break;
                }
            }
            return GetEncoding().GetString(tb.ToArray());
        }

        /// <summary>
        /// hex转byte
        /// </summary>
        /// <param name="mHex">hex值</param>
        /// <returns>原始字符串</returns>
        public static byte[] Hex2Byte(string mHex)
        {
            mHex = Regex.Replace(mHex, "[^0-9A-Fa-f]", "");
            if (mHex.Length % 2 != 0)
                mHex = mHex.Remove(mHex.Length - 1, 1);
            if (mHex.Length <= 0) return new byte[0];
            byte[] vBytes = new byte[mHex.Length / 2];
            for (int i = 0; i < mHex.Length; i += 2)
                if (!byte.TryParse(mHex.Substring(i, 2), NumberStyles.HexNumber, null, out vBytes[i / 2]))
                    vBytes[i / 2] = 0;
            return vBytes;
        }

        public static string Byte2Hex(byte[] d, string s = "", int len = -1)
        {
            if (len == -1)
                len = d.Length;
            return BitConverter.ToString(d,0,len).Replace("-", s)+" ";
        }

        /// <summary>
        /// 读取软件资源文件内容
        /// </summary>
        /// <param name="path">路径</param>
        /// <returns>内容字节数组</returns>
        public static byte[] GetAssetsFileContent(string path)
        {
            Uri uri = new Uri(path, UriKind.Relative);
            var source = System.Windows.Application.GetResourceStream(uri).Stream;
            byte[] f = new byte[source.Length];
            source.Read(f, 0, (int)source.Length);
            return f;
        }

        /// <summary>
        /// 取出文件
        /// </summary>
        /// <param name="insidePath">软件内部的路径</param>
        /// <param name="outPath">需要释放到的路径</param>
        /// <param name="d">是否覆盖</param>
        public static void CreateFile(string insidePath, string outPath, bool d = true)
        {
            if(!File.Exists(outPath) || d)
                File.WriteAllBytes(outPath, GetAssetsFileContent(insidePath));
        }

        /// <summary>
        /// 更换语言文件
        /// </summary>
        /// <param name="languagefileName"></param>
        public static void LoadLanguageFile(string languagefileName)
        {
            try
            {
                System.Windows.Application.Current.Resources.MergedDictionaries[0] = new System.Windows.ResourceDictionary()
                {
                    Source = new Uri($"pack://application:,,,/languages/{languagefileName}.xaml", UriKind.RelativeOrAbsolute)
                };
            }
            catch
            {
                System.Windows.Application.Current.Resources.MergedDictionaries[0] = new System.Windows.ResourceDictionary()
                {
                    Source = new Uri("pack://application:,,,/languages/en-US.xaml", UriKind.RelativeOrAbsolute)
                };
            }

        }
    }
}
