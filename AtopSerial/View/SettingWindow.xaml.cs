using AtopSerial.Pages;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Search;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml;

namespace AtopSerial
{
    /// <summary>
    /// SettingWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SettingWindow : Window
    {
        private static bool fileLoadingSend = false;
        private static bool fileLoadingRev = false;
        private static string lastLuaFileSend = "";
        private static string lastLuaFileRev = "";

        public SettingWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.DataContext = Tools.Global.setting;

            //置顶显示以免被挡住
            //this.Topmost = true;

            //初始化RTS/DTR
            RTSCheckBox.DataContext = this;
            DTRCheckBox.DataContext = this;
            Rts = false;
            Dtr = true;

            //初始化下拉框参数
            dataBitsComboBox.SelectedIndex = Tools.Global.setting.dataBits - 5;
            stopBitComboBox.SelectedIndex = Tools.Global.setting.stopBit - 1;
            dataCheckComboBox.SelectedIndex = Tools.Global.setting.parity;
            plotStyleComboBox.SelectedIndex = Tools.Global.setting.plotStyle;
            plotRenderComboBox.SelectedIndex = Tools.Global.setting.plotRenderQuality;
            if (Tools.Global.setting.language == "zh-CN")
                languageSelectionComboBox.SelectedIndex = 0;
            else
                languageSelectionComboBox.SelectedIndex = 1;

            //快速搜索
            SearchPanel.Install(textEditor.TextArea);
            SearchPanel.Install(textEditorRev.TextArea);
            string name = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + ".Lua.xshd";
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            using (System.IO.Stream s = assembly.GetManifestResourceStream(name))
            {
                using (XmlTextReader reader = new XmlTextReader(s))
                {
                    var xshd = HighlightingLoader.LoadXshd(reader);
                    textEditor.SyntaxHighlighting = HighlightingLoader.Load(xshd, HighlightingManager.Instance);
                    textEditorRev.SyntaxHighlighting = HighlightingLoader.Load(xshd, HighlightingManager.Instance);
                }
            }

            //加载上次打开的文件
            LoadLuaSendFile(Tools.Global.setting.sendScript);
            LoadLuaRevFile(Tools.Global.setting.recvScript);

            //加载编码
            var el = Encoding.GetEncodings();
            List<EncodingInfo> encodingList = new List<EncodingInfo>(el);

            //先排个序，美观点
            encodingList.Sort((x, y) => x.CodePage - y.CodePage);
            foreach (var en in encodingList)
            {
                ComboBoxItem c = new ComboBoxItem();
                c.Content = $"[{en.CodePage}] {en.Name}";
                c.Tag = en.CodePage;
                int index = encodingComboBox.Items. Add(c);
                if (Tools.Global.setting.encoding == en.CodePage)//现在用的编码
                    encodingComboBox.SelectedIndex = index;
            }

            //加载关于页面
            aboutFrame.Navigate(new Uri("Pages/AboutPage.xaml", UriKind.Relative));
        }

        private void SettingWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //自动保存脚本
            if (lastLuaFileSend != "")
                SaveLuaSendFile(lastLuaFileSend);
            if (lastLuaFileRev != "")
                SaveLuaRevFile(lastLuaFileRev);
            if (Tools.Global.isMainWindowsClosed)
            {
                //说明软件关了
                e.Cancel = false;
            }
            else
            {
                e.Cancel = true;//取消这次关闭事件
                Hide();//隐藏窗口，以便下次调用show
            }
        }
        private void SettingWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }

        /// <summary>
        /// 加载lua脚本文件
        /// </summary>
        /// <param name="fileName">文件名，不带.lua</param>
        private void LoadLuaSendFile(string fileName)
        {
            try
            {
                if (!File.Exists(Tools.Global.ProfilePath + $"user_script/user_script_send_convert/{fileName}.lua"))
                {
                    File.Create(Tools.Global.ProfilePath + $"user_script/user_script_send_convert/{fileName}.lua").Close();
                }
                Tools.Global.setting.sendScript = fileName;
                textEditor.Text = File.ReadAllText(Tools.Global.ProfilePath + $"user_script/user_script_send_convert/{Tools.Global.setting.sendScript}.lua");
            }
            catch
            {
                Tools.MessageBox.Show("File load failed.\r\n" +
                    "Do not open this file in other application!");
                return;
            }
            RefreshLuaSendList();

            //重载脚本
            LuaEnv.LuaLoader.ClearRun();
        }

        private void RefreshLuaSendList()
        {
            //刷新文件列表
            DirectoryInfo luaFileDir = new DirectoryInfo(Tools.Global.ProfilePath + "user_script/user_script_send_convert/");
            FileSystemInfo[] luaFiles = luaFileDir.GetFileSystemInfos();
            fileLoadingSend = true;
            luaFileListSend.Items.Clear();
            for (int i = 0; i < luaFiles.Length; i++)
            {
                FileInfo file = luaFiles[i] as FileInfo;
                if (file != null && file.Name.ToLower().EndsWith(".lua"))
                {
                    string name = file.Name.Substring(0, file.Name.Length - 4);
                    luaFileListSend.Items.Add(name);
                    if (name == Tools.Global.setting.sendScript)
                    {
                        luaFileListSend.SelectedIndex = luaFileListSend.Items.Count - 1;
                    }
                }
            }
            if (Tools.Global.setting.sendScript == "")
            {
                luaFileListSend.SelectedIndex = 0;
                Tools.Global.setting.sendScript = luaFileListSend.Text;
            }
            lastLuaFileSend = Tools.Global.setting.sendScript;
            fileLoadingSend = false;
        }

        private void LoadLuaRevFile(string fileName)
        {
            try
            {
                if (!File.Exists(Tools.Global.ProfilePath + $"user_script/user_script_recv_convert/{fileName}.lua"))
                {
                    File.Create(Tools.Global.ProfilePath + $"user_script/user_script_recv_convert/{fileName}.lua").Close();
                }
                Tools.Global.setting.recvScript = fileName;
                textEditorRev.Text = File.ReadAllText(Tools.Global.ProfilePath + $"user_script/user_script_recv_convert/{Tools.Global.setting.recvScript}.lua");
            }
            catch
            {
                Tools.MessageBox.Show("File load failed.\r\n" +
                    "Do not open this file in other application!");
                return;
            }
            RefreshLuaRevList();

            //重载脚本
            LuaEnv.LuaLoader.ClearRun();
        }
        private void RefreshLuaRevList()
        {
            //刷新文件列表
            DirectoryInfo luaFileDir = new DirectoryInfo(Tools.Global.ProfilePath + "user_script/user_script_recv_convert/");
            FileSystemInfo[] luaFiles = luaFileDir.GetFileSystemInfos();
            fileLoadingRev = true;
            luaFileListRev.Items.Clear();
            for (int i = 0; i < luaFiles.Length; i++)
            {
                FileInfo file = luaFiles[i] as FileInfo;
                if (file != null && file.Name.ToLower().EndsWith(".lua"))
                {
                    string name = file.Name.Substring(0, file.Name.Length - 4);
                    luaFileListRev.Items.Add(name);
                    if (name == Tools.Global.setting.recvScript)
                    {
                        luaFileListRev.SelectedIndex = luaFileListRev.Items.Count - 1;
                    }
                }
            }
            if(Tools.Global.setting.recvScript == "")
            {
                luaFileListRev.SelectedIndex = 0;
                Tools.Global.setting.recvScript = luaFileListRev.Text;
            }
            lastLuaFileRev = Tools.Global.setting.recvScript;
            fileLoadingRev = false;
        }

        private void SaveLuaSendFile(string fileName)
        {
            File.WriteAllText(Tools.Global.ProfilePath + $"user_script/user_script_send_convert/{fileName}.lua", textEditor.Text);
            LuaEnv.LuaLoader.ClearRun();
        }

        private void SaveLuaRevFile(string fileName)
        {
            File.WriteAllText(Tools.Global.ProfilePath + $"user_script/user_script_recv_convert/{fileName}.lua", textEditorRev.Text);
            LuaEnv.LuaLoader.ClearRun();
        }

        private void ApiDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(Tools.Global.apiDocumentUrl);
        }

        private void OpenScriptFolderButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", Tools.Global.ProfilePath + "user_script/user_script_send_convert");
        }

        private void DataBitsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(dataBitsComboBox.SelectedItem != null)
            {
                Tools.Global.setting.dataBits = dataBitsComboBox.SelectedIndex + 5;
            }
        }

        private void plotLineWidth_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var _mainWindow = Application.Current.Windows
                .Cast<Window>()
                .FirstOrDefault(window => window is MainWindow) as MainWindow;
                _mainWindow?.PlotSettingNavigation();
            }
        }

        private void plotLineWidth_LostFocus(object sender, RoutedEventArgs e)
        {
            var _mainWindow = Application.Current.Windows
            .Cast<Window>()
            .FirstOrDefault(window => window is MainWindow) as MainWindow;
            _mainWindow?.PlotSettingNavigation();
        }
        
        private void languageSelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (languageSelectionComboBox.SelectedItem != null)
            {
                if(languageSelectionComboBox.SelectedIndex == 0)
                    Tools.Global.setting.language = "zh-CN";
                else
                    Tools.Global.setting.language = "en-US";
            }
        }

        private void plotStyleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (plotStyleComboBox.SelectedItem != null)
            {
                Tools.Global.setting.plotStyle = plotStyleComboBox.SelectedIndex;
                var _mainWindow = Application.Current.Windows
                .Cast<Window>()
                .FirstOrDefault(window => window is MainWindow) as MainWindow;
                _mainWindow?.PlotSettingNavigation();
            }
        }

        private void plotRenderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (plotRenderComboBox.SelectedItem != null)
            {
                Tools.Global.setting.plotRenderQuality = plotRenderComboBox.SelectedIndex;
                var _mainWindow = Application.Current.Windows
                .Cast<Window>()
                .FirstOrDefault(window => window is MainWindow) as MainWindow;
                _mainWindow?.PlotSettingNavigation();
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
                    PlotPage.PlotScaleY(Item.channel - 1, result);
                }
            }
        }

        private void StopBitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (stopBitComboBox.SelectedItem != null)
            {
                Tools.Global.setting.stopBit = stopBitComboBox.SelectedIndex + 1;
            }
        }

        private void DataCheckComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dataCheckComboBox.SelectedItem != null)
            {
                Tools.Global.setting.parity = dataCheckComboBox.SelectedIndex;
            }
        }

        private void NewScriptButton_Click(object sender, RoutedEventArgs e)
        {
            luaTestWrapPanel.Visibility = Visibility.Collapsed;
            newLuaFileWrapPanel.Visibility = Visibility.Visible;
        }

        private void TestScriptButton_Click(object sender, RoutedEventArgs e)
        {
            newLuaFileWrapPanel.Visibility = Visibility.Collapsed;
            luaTestWrapPanel.Visibility = Visibility.Visible;
        }

        private void LuaFileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (luaFileListSend.SelectedItem != null && !fileLoadingSend)
            {
                if (lastLuaFileSend != "")
                    SaveLuaSendFile(lastLuaFileSend);
                string fileName = luaFileListSend.SelectedItem as string;
                LoadLuaSendFile(fileName);
            }
        }

        private void DeleteScriptButtonSend_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.DialogResult Result = Tools.MessageBox.ShowConfirm(TryFindResource("DeleteScriptConfirm") as string ?? "?!");
            if (Result == System.Windows.Forms.DialogResult.OK)
            {
                if (File.Exists(Tools.Global.ProfilePath + "user_script/user_script_send_convert/" + luaFileListSend.SelectedItem.ToString() + ".lua"))
                {
                    File.Delete(Tools.Global.ProfilePath + "user_script/user_script_send_convert/" + luaFileListSend.SelectedItem.ToString() + ".lua");
                }
                Tools.Global.setting.sendScript = "";
                RefreshLuaSendList();
                string fileName = luaFileListSend.SelectedItem as string;
                LoadLuaSendFile(fileName);
            }
        }

        private void RefreshScriptListButtonSend_Click(object sender, RoutedEventArgs e)
        {
            RefreshLuaSendList();
        }

        private void NewLuaFileCancelbutton_Click(object sender, RoutedEventArgs e)
        {
            newLuaFileWrapPanel.Visibility = Visibility.Collapsed;
        }

        private void NewLuaFilebutton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(newLuaFileNameTextBox.Text))
            {
                Tools.MessageBox.Show(TryFindResource("LuaNoName") as string ?? "?!");
                return;
            }
            if (File.Exists(Tools.Global.ProfilePath + $"user_script/user_script_send_convert/{newLuaFileNameTextBox.Text}.lua"))
            {
                Tools.MessageBox.Show(TryFindResource("LuaExist") as string ?? "?!");
                return;
            }

            try
            {
                File.Create(Tools.Global.ProfilePath + $"user_script/user_script_send_convert/{newLuaFileNameTextBox.Text}.lua").Close();
                LoadLuaSendFile(newLuaFileNameTextBox.Text);
            }
            catch
            {
                Tools.MessageBox.Show(TryFindResource("LuaCreateFail") as string ?? "?!");
                return;
            }
            newLuaFileWrapPanel.Visibility = Visibility.Collapsed;
        }

        private void LuaTestbutton_Click(object sender, RoutedEventArgs e)
        {
            if (luaFileListSend.SelectedItem != null && !fileLoadingSend)
            {
                try
                {
                    byte[] r = LuaEnv.LuaLoader.Run($"{luaFileListSend.SelectedItem as string}.lua",
                                        new System.Collections.ArrayList{"uartData",
                                           Tools.Global.GetEncoding().GetBytes(luaTestTextBox.Text)});
                    Tools.MessageBox.Show($"{TryFindResource("SettingLuaRunResult") as string ?? "?!"}\r\nHEX：" + Tools.Global.Byte2Hex(r) +
                        $"\r\n{TryFindResource("SettingLuaRawText") as string ?? "?!"}" + Tools.Global.Byte2Readable(r));
                }
                catch(Exception ex)
                {
                    Tools.MessageBox.Show($"{TryFindResource("ErrorScript") as string ?? "?!"}\r\n" + ex.ToString());
                }

            }
        }

        private void LuaTestCancelbutton_Click(object sender, RoutedEventArgs e)
        {
            luaTestWrapPanel.Visibility = Visibility.Collapsed;
        }

        private void TextEditor_LostFocus(object sender, RoutedEventArgs e)
        {
            if (lastLuaFileSend != "")
                SaveLuaSendFile(lastLuaFileSend);
        }

        private void OpenLogButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", Tools.Global.ProfilePath + "logs");
            }
            catch
            {
                Tools.MessageBox.Show($"Folder opening failed, please open manually.Path:{Tools.Global.ProfilePath}logs");
            }
        }

        private void ExtendDataSendGrid_Click(object sender, EventArgs e)
        {
            var _mainWindow = Application.Current.Windows
            .Cast<Window>()
            .FirstOrDefault(window => window is MainWindow) as MainWindow;
            _mainWindow?.RenderDataSendGrid();
        }
        
        private void PlotSetting_Click(object sender, EventArgs e)
        {
            var _mainWindow = Application.Current.Windows
            .Cast<Window>()
            .FirstOrDefault(window => window is MainWindow) as MainWindow;
            _mainWindow?.PlotSettingNavigation();
        }

        private void encodingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox c = sender as ComboBox;
            if ((int)((ComboBoxItem)c.SelectedItem).Tag == Tools.Global.setting.encoding)
                return;
            Tools.Global.setting.encoding = (int)((ComboBoxItem)c.SelectedItem).Tag;
        }

        private void luaFileListRev_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (luaFileListRev.SelectedItem != null && !fileLoadingRev)
            {
                if (lastLuaFileRev != "")
                    SaveLuaRevFile(lastLuaFileRev);
                string fileName = luaFileListRev.SelectedItem as string;
                LoadLuaRevFile(fileName);
            }
        }
        private void newScriptButtonRev_Click(object sender, RoutedEventArgs e)
        {
            luaTestWrapPanelRev.Visibility = Visibility.Collapsed;
            newLuaFileWrapPanelRev.Visibility = Visibility.Visible;
        }

        private void testScriptButtonRev_Click(object sender, RoutedEventArgs e)
        {
            newLuaFileWrapPanelRev.Visibility = Visibility.Collapsed;
            luaTestWrapPanelRev.Visibility = Visibility.Visible;
        }

        private void openScriptFolderButtonRev_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", Tools.Global.ProfilePath + "user_script/user_script_recv_convert");
        }

        private void DeleteScriptButtonRev_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.DialogResult Result = Tools.MessageBox.ShowConfirm(TryFindResource("DeleteScriptConfirm") as string ?? "?!");
            if (Result == System.Windows.Forms.DialogResult.OK)
            {
                if (File.Exists(Tools.Global.ProfilePath + "user_script/user_script_recv_convert/" + luaFileListRev.SelectedItem.ToString() + ".lua"))
                {
                    File.Delete(Tools.Global.ProfilePath + "user_script/user_script_recv_convert/" + luaFileListRev.SelectedItem.ToString() + ".lua");
                }
                Tools.Global.setting.recvScript = "";
                RefreshLuaRevList();
                string fileName = luaFileListRev.SelectedItem as string;
                LoadLuaRevFile(fileName);
            }
        }

        private void RefreshScriptListButtonRev_Click(object sender, RoutedEventArgs e)
        {
            RefreshLuaRevList();
        }

        private void newLuaFilebuttonRev_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(newLuaFileNameTextBoxRev.Text))
            {
                Tools.MessageBox.Show(TryFindResource("LuaNoName") as string ?? "?!");
                return;
            }
            if (File.Exists(Tools.Global.ProfilePath + $"user_script/user_script_recv_convert/{newLuaFileNameTextBoxRev.Text}.lua"))
            {
                Tools.MessageBox.Show(TryFindResource("LuaExist") as string ?? "?!");
                return;
            }

            try
            {
                File.Create(Tools.Global.ProfilePath + $"user_script/user_script_recv_convert/{newLuaFileNameTextBoxRev.Text}.lua").Close();
                LoadLuaRevFile(newLuaFileNameTextBoxRev.Text);
            }
            catch
            {
                Tools.MessageBox.Show(TryFindResource("LuaCreateFail") as string ?? "?!");
                return;
            }
            newLuaFileWrapPanelRev.Visibility = Visibility.Collapsed;
        }

        private void newLuaFileCancelbuttonRev_Click(object sender, RoutedEventArgs e)
        {
            newLuaFileWrapPanelRev.Visibility = Visibility.Collapsed;
        }

        private void luaTestbuttonRev_Click(object sender, RoutedEventArgs e)
        {
            if (luaFileListRev.SelectedItem != null && !fileLoadingRev)
            {
                try
                {
                    byte[] r = LuaEnv.LuaLoader.Run(
                        $"{luaFileListRev.SelectedItem as string}.lua",
                        new System.Collections.ArrayList{
                            "uartData",
                            Tools.Global.GetEncoding().GetBytes(luaTestTextBoxRev.Text)
                        },
                        "user_script/user_script_recv_convert/");
                    Tools.MessageBox.Show($"{TryFindResource("SettingLuaRunResult") as string ?? "?!"}\r\nHEX：" + Tools.Global.Byte2Hex(r) +
                        $"\r\n{TryFindResource("SettingLuaRawText") as string ?? "?!"}" + Tools.Global.Byte2Readable(r));
                }
                catch (Exception ex)
                {
                    Tools.MessageBox.Show($"{TryFindResource("ErrorScript") as string ?? "?!"}\r\n" + ex.ToString());
                }
            }
        }
        private void luaTestCancelbuttonRev_Click(object sender, RoutedEventArgs e)
        {
            luaTestWrapPanelRev.Visibility = Visibility.Collapsed;
        }

        private void textEditorRev_LostFocus(object sender, RoutedEventArgs e)
        {
            if (lastLuaFileRev != "")
                SaveLuaRevFile(lastLuaFileRev);
        }

        public bool Rts
        {
            get
            {
                return Tools.Global.uart.Rts;
            }
            set
            {
                Tools.Global.uart.Rts = value;
            }
        }
        public bool Dtr
        {
            get
            {
                return Tools.Global.uart.Dtr;
            }
            set
            {
                Tools.Global.uart.Dtr = value;
            }
        }
    }
}
