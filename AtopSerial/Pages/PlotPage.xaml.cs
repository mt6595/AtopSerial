using ScottPlot;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.ComponentModel.Composition.Primitives;
using ScottPlot.Plottable;
using System.Windows.Shapes;
using static System.Windows.Forms.LinkLabel;
using System.Windows.Markup;
using Newtonsoft.Json.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AtopSerial.Model;
using ScottPlot.MarkerShapes;
using ScottPlot.SnapLogic;
using System.Security.Policy;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;

namespace AtopSerial.Pages
{
    /// <summary>
    /// PlotPage.xaml 的交互逻辑
    /// </summary>
    public partial  class PlotPage : Page
    {
        private const int MaxTable = 10;
        private const int MaxPoints = 100000;
        private ScottPlot.Styles.IStyle[] Styles = ScottPlot.Style.GetStyles();
        static private SignalPlot[] SignlGriph = new SignalPlot[MaxTable];
        private double[][] PlotDataY;
        private int[] PlotDataYIndex = new int[MaxTable];
        List<List<double>> PlotDataYList = Enumerable.Range(0, MaxTable).Select(_ => Enumerable.Repeat(0.0, MaxPoints).ToList()).ToList();
        static ManualResetEventSlim PlotRender = new ManualResetEventSlim(false);
        public PlotPage()
        {
            InitializeComponent();
            Plot.Plot.Clear();
            Plot.Plot.AxisAuto();
            Plot.Plot.Layout(0, 30, 15, 0, -8);
            Plot.Plot.SetAxisLimits();
            Plot.RightClicked -= Plot.DefaultRightClickEvent;
            Plot.RightClicked += CustomRightClickEvent;
            Plot.Plot.Legend();
            PlotDataY = PlotDataYList.Select(innerList => innerList.ToArray()).ToArray();

            Array.Clear(PlotDataYIndex, 0, PlotDataYIndex.Length);
            for (int iTemp = 0; iTemp < SignlGriph.Length; iTemp++)
            {
                Array.Clear(PlotDataY[iTemp], 0, PlotDataY[iTemp].Length);
                SignlGriph[iTemp] = Plot.Plot.AddSignal(PlotDataY[iTemp], label: "Graph-" + iTemp);
                SignlGriph[iTemp].MaxRenderIndex = 0;
                SignlGriph[iTemp].IsVisible = false;
            }

            if (Tools.Global.setting.graphTheme + 1 >= Styles.Length || Tools.Global.setting.graphTheme < 0)
                Tools.Global.setting.graphTheme = 0;
            Plot.Plot.Style(Styles[Tools.Global.setting.graphTheme]);

            PlotRender.Set();
            new Thread(() =>
            {
                TimeSpan TimeOut = TimeSpan.FromSeconds(1);
                while (true)
                {
                    if(PlotRender.Wait(TimeOut))
                    {
                        PlotRender.Reset();
                        this.Dispatcher.Invoke(new Action(delegate
                        {
                            Plot.Render();
                        }));
                        Thread.Sleep(20);
                    }
                    if (Tools.Global.isMainWindowsClosed)
                        return;
                }
            }).Start();
            LuaEnv.LuaApis.EnvluaPlotAddPoint += (sender, e) => luaPlotAddPoint(e.Index, e.Point);
            LuaEnv.LuaApis.EnvluaPlotAddPointMulti += (sender, e) => luaPlotAddPointMulti(e.Parameters);
            LuaEnv.LuaApis.EnvluaPlotConfig += (sender, e) => luaPlotConfig(e.Parameters);
            LuaEnv.LuaApis.EnvluaPlotClear += luaPlotClear;
            LuaEnv.LuaApis.EnvluaPlotInit += luaPlotInit;
        }
        private void PlotPreviewDragOver(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string file in files)
            {
                // 在这里可以执行你想要的操作，比如读取文件内容、处理文件等
                MessageBox.Show("文件路径：" + file);
            }
        }
        private void CustomRightClickEvent(object sender, EventArgs e)
        {
            MenuItem CopyImageMenuItem = new MenuItem() { Header = TryFindResource("GraphCopyImage") as string ?? "?!" };
            CopyImageMenuItem.Click += CopyAsImage;

            MenuItem SaveImageMenuItem = new MenuItem() { Header = TryFindResource("GraphSaveImage") as string ?? "?!" };
            SaveImageMenuItem.Click += SaveAsImage;

            MenuItem GraphDataExportItem = new MenuItem() { Header = TryFindResource("GraphDataExport") as string ?? "?!" };
            GraphDataExportItem.Click += GraphDataExport;
            GraphDataExportItem.IsEnabled = false;
            for (int iTemp = 0; iTemp < MaxTable; iTemp++)
            {
                if (SignlGriph[iTemp].IsVisible == true)
                {
                    GraphDataExportItem.IsEnabled = true;
                    break;
                }
            }

            MenuItem GraphDataImportItem = new MenuItem() { Header = TryFindResource("GraphDataImport") as string ?? "?!" };
            GraphDataImportItem.Click += GraphDataImport;

            MenuItem ClearGraphMenuItem = new MenuItem() { Header = TryFindResource("GraphClear") as string ?? "?!" };
            ClearGraphMenuItem.Click += ClearGraph;

            MenuItem ZoomFitMenuItem = new MenuItem() { Header = TryFindResource("GraphZoomFit") as string ?? "?!" };
            ZoomFitMenuItem.Click += ZoomFitGraph;

            MenuItem GenerateMenuItem = new MenuItem() { Header = TryFindResource("GraphGenerate") as string ?? "?!" };
            GenerateMenuItem.Click += GenerateDemoGraph;

            MenuItem ThemeGraphItem = new MenuItem() { Header = TryFindResource("GraphTheme") as string ?? "?!" };
            ThemeGraphItem.Click += ThemeGraph;

            ContextMenu rightClickMenu = new ContextMenu();
            rightClickMenu.Items.Add(CopyImageMenuItem);
            rightClickMenu.Items.Add(SaveImageMenuItem);
            rightClickMenu.Items.Add(GraphDataExportItem);
            rightClickMenu.Items.Add(GraphDataImportItem);
            rightClickMenu.Items.Add(new Separator());
            rightClickMenu.Items.Add(ClearGraphMenuItem);
            rightClickMenu.Items.Add(ZoomFitMenuItem);
            rightClickMenu.Items.Add(GenerateMenuItem);
            rightClickMenu.Items.Add(new Separator());
            rightClickMenu.Items.Add(ThemeGraphItem);
            rightClickMenu.IsOpen = true;
        }
        public void CopyAsImage(object sender, EventArgs e)
        {
            Clipboard.SetImage(WpfPlot.BmpImageFromBmp(Plot.Plot.Render()));
        }
        public void SaveAsImage(object sender, EventArgs e)
        {
            var SavePlotImage = new SaveFileDialog
            {
                FileName = DateTime.Now.ToString("yyMMddHHmmss")+".png",
                Filter = "PNG Files (*.png)|*.png" +
                         "|JPG Files (*.jpg, *.jpeg)|*.jpg;*.jpeg" +
                         "|BMP Files (*.bmp)|*.bmp" +
                         "|All files (*.*)|*.*"
            };

            if (SavePlotImage.ShowDialog() is true)
                Plot.Plot.SaveFig(SavePlotImage.FileName);
        }

        private void ClearGraph(object sender, EventArgs e)
        {
            ClearGraph();
            PlotRender.Set();
        }

        public void GenerateDemoGraph(object sender, EventArgs e)
        {
            luaPlotInit();
            Array.Copy(Generate.Sin(count: 1000, mult: 100, oscillations: 10, phase: 0), PlotDataY[0], 1000);
            Array.Copy(Generate.Sin(count: 1000, mult: 100, oscillations: 10, phase: 33.33), PlotDataY[1], 1000);
            Array.Copy(Generate.Sin(count: 1000, mult: 100, oscillations: 10, phase: 66.66), PlotDataY[2], 1000);
            for (int iTemp = 0; iTemp < 1000; iTemp++) luaPlotAddPoint(0, PlotDataY[0][iTemp]);
            for (int iTemp = 0; iTemp < 1000; iTemp++) luaPlotAddPoint(1, PlotDataY[1][iTemp]);
            for (int iTemp = 0; iTemp < 1000; iTemp++) luaPlotAddPoint(2, PlotDataY[2][iTemp]);
            SignlGriph[0].IsVisible = true;
            SignlGriph[1].IsVisible = true;
            SignlGriph[2].IsVisible = true;
            Plot.Plot.AxisAuto();
            PlotRender.Set();
        }
        public void ZoomFitGraph(object sender, EventArgs e)
        {
            Plot.Plot.AxisAuto();
            PlotRender.Set();
        }

        public void ClearGraph()
        {
            Array.Clear(PlotDataYIndex, 0, PlotDataYIndex.Length);
            for (int iTemp = 0; iTemp < SignlGriph.Length; iTemp++)
            {
                Array.Clear(PlotDataY[iTemp], 0, PlotDataY[iTemp].Length);
                SignlGriph[iTemp].MaxRenderIndex = 0;
            }
            PlotRender.Set();
        }

        public void ThemeGraph(object sender, EventArgs e)
        {
            if (Tools.Global.setting.graphTheme+1 >= Styles.Length)
                Tools.Global.setting.graphTheme = 0;
            else
                Tools.Global.setting.graphTheme++;
            Plot.Plot.Style(Styles[Tools.Global.setting.graphTheme]);
            PlotRender.Set();
        }
        public void GraphDataExport(object sender, EventArgs e)
        {
            System.Windows.Forms.SaveFileDialog saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            saveFileDialog.Filter = "CSV File (*.csv)|*.csv";
            saveFileDialog.FileName = DateTime.Now.ToString("yyMMddHHmmss");
            saveFileDialog.Title = "GriphData";
            if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                new Thread(() =>
                {
                    try
                    {
                        string filePath = saveFileDialog.FileName;
                        using (StreamWriter writer = new StreamWriter(filePath))
                        {
                            // 写入标题行
                            StringBuilder headerBuilder = new StringBuilder();
                            for (int Index = 0; Index < MaxTable; Index++)
                            {
                                if (SignlGriph[Index].IsVisible == true)
                                {
                                    headerBuilder.Append(SignlGriph[Index].Label + ",");
                                }
                            }
                            writer.WriteLine(headerBuilder.ToString().TrimEnd(','));
                            for (int jTemp = 0; jTemp < PlotDataYIndex.Max(); jTemp++)
                            {
                                StringBuilder lineBuilder = new StringBuilder();
                                for (int iTemp = 0; iTemp < MaxTable; iTemp++)
                                {
                                    if (SignlGriph[iTemp].IsVisible == true)
                                    {
                                        if (jTemp < PlotDataYIndex[iTemp])
                                            lineBuilder.Append(PlotDataY[iTemp][jTemp] + ",");
                                        else
                                            lineBuilder.Append(",");
                                    }
                                }
                                writer.WriteLine(lineBuilder.ToString().TrimEnd(','));
                            }
                        }
                    }
                    catch { }
                }).Start();
            }
        }
        public void GraphDataImport(object sender, EventArgs e)
        {
            System.Windows.Forms.OpenFileDialog OpenFileDialog = new System.Windows.Forms.OpenFileDialog();
            OpenFileDialog.Filter = "CSV File (*.csv)|*.csv";
            if (OpenFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                new Thread(() =>
                {
                    luaPlotInit();
                    try
                    {
                        using (var Reader = new StreamReader(OpenFileDialog.FileName))
                        {
                            using (var csvParser = new TextFieldParser(Reader))
                            {
                                csvParser.TextFieldType = FieldType.Delimited;
                                csvParser.SetDelimiters(",");

                                string[] Fields = csvParser.ReadFields();
                                for (int iTemp = 0; iTemp < Fields.Length && iTemp < MaxTable; iTemp++)
                                {
                                    SignlGriph[iTemp].Label = Fields[iTemp];
                                    SignlGriph[iTemp].IsVisible = IsVisible;
                                }

                                while (!csvParser.EndOfData)
                                {
                                    Fields = csvParser.ReadFields();
                                    for (int iTemp = 0; iTemp < Fields.Length && iTemp < MaxTable; iTemp++)
                                    {
                                        float plotDouble;
                                        if (float.TryParse(Fields[iTemp], out plotDouble))
                                            luaPlotAddPoint(iTemp, plotDouble);
                                    }
                                }
                                Plot.Plot.AxisAuto();
                            }
                        }
                    }
                    catch (Exception err)
                    {
                        Tools.MessageBox.Show(err.Message);
                        return;
                    }
                }).Start();
            }
            PlotRender.Set();
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
        }
        private void Plot_MouseMove(object sender, MouseEventArgs e)
        {
        }
        
        private void Plot_Drop(object sender, DragEventArgs e)
        {
            new Thread(() =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] FilePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
                    foreach (string FilePath in FilePaths)
                    {
                        if (FilePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                        {
                            luaPlotInit();
                            try
                            {
                                using (var Reader = new StreamReader(FilePath))
                                {
                                    using (var csvParser = new TextFieldParser(Reader))
                                    {
                                        csvParser.TextFieldType = FieldType.Delimited;
                                        csvParser.SetDelimiters(",");

                                        string[] Fields = csvParser.ReadFields();
                                        for (int iTemp = 0; iTemp < Fields.Length && iTemp < MaxTable; iTemp++)
                                        {
                                            SignlGriph[iTemp].Label = Fields[iTemp];
                                            SignlGriph[iTemp].IsVisible = IsVisible;
                                        }

                                        while (!csvParser.EndOfData)
                                        {
                                            Fields = csvParser.ReadFields();
                                            for (int iTemp = 0; iTemp < Fields.Length && iTemp < MaxTable; iTemp++)
                                            {
                                                float plotDouble;
                                                if (float.TryParse(Fields[iTemp], out plotDouble))
                                                    luaPlotAddPoint(iTemp, plotDouble);
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception err)
                            {
                                Tools.MessageBox.Show(err.Message);
                                return;
                            }
                            Plot.Plot.AxisAuto();
                            PlotRender.Set();
                            break;
                        }
                    }
                }
            }).Start();
        }

        public static void PlotOffsetY(int PoltIndex, double OffsetY)
        {
            if(PoltIndex < MaxTable)
                SignlGriph[PoltIndex].OffsetY = OffsetY;
            PlotRender.Set();
        }
        public static void PlotScaleY(int PoltIndex, double OffsetY)
        {
            if (PoltIndex < MaxTable)
                SignlGriph[PoltIndex].ScaleY = OffsetY;
            PlotRender.Set();
        }
        public static void PlotIsVisible(int PoltIndex, bool IsVisible)
        {
            if (PoltIndex < MaxTable)
                SignlGriph[PoltIndex].IsVisible = IsVisible;
            PlotRender.Set();
        }

        //==================================================================================================================
        //Lua接口
        //==================================================================================================================
        private bool LineStyleTryParse(string value, out LineStyle result)
        {
            result = LineStyle.None;
            if (value == "None")
                result = LineStyle.None;
            else if (value == "Solid")
                result = LineStyle.Solid;
            else if (value == "Dash")
                result = LineStyle.Dash;
            else if (value == "DashDot")
                result = LineStyle.DashDot;
            else if (value == "DashDotDot")
                result = LineStyle.DashDotDot;
            else if (value == "Dot")
                result = LineStyle.Dot;
            else if (value == "Custom")
                result = LineStyle.Custom;
            else
                return false;
            return true;
        }

        private void luaPlotClear()
        {
            ClearGraph();
            Array.Clear(PlotDataYIndex, 0, PlotDataYIndex.Length);
            PlotRender.Set();
        }

        private void luaPlotInit()
        {
            ClearGraph();
            Array.Clear(PlotDataYIndex, 0, PlotDataYIndex.Length);
            for (int iTemp = 0; iTemp < SignlGriph.Length; iTemp++)
            {
                Array.Clear(PlotDataY[iTemp], 0, PlotDataY[iTemp].Length);
                SignlGriph[iTemp].Label = "Graph-" + iTemp;
                SignlGriph[iTemp].IsVisible = false;
                SignlGriph[iTemp].LineStyle = LineStyle.Solid;
                SignlGriph[iTemp].Smooth = false;
                SignlGriph[iTemp].SmoothTension = 0.5;
                SignlGriph[iTemp].OffsetY = 0;
                SignlGriph[iTemp].ScaleY = 1;
                SignlGriph[iTemp].LineWidth = 1;
                SignlGriph[iTemp].IsHighlighted = false;
                SignlGriph[iTemp].HighlightCoefficient = 2f;
            }
            PlotRender.Set();
        }
        
        private void luaPlotConfig(params object[] GraphParams)
        {
            int GraphIndex = 0;
            string GraphData;
            string KeyValue;
            XLua.LuaTable Table;

            if (GraphParams[GraphIndex] is XLua.LuaTable)
             {
                Table = (XLua.LuaTable)GraphParams[GraphIndex];
                Table.Get<string, string>("PlotIndex", out KeyValue);
                GraphIndex = int.Parse(KeyValue);
                if (KeyValue == null || GraphIndex >= MaxTable) return;
                Table.Get<string, string>("IsVisible", out KeyValue);
                if(bool.TryParse(KeyValue, out bool IsVisible)) SignlGriph[GraphIndex].IsVisible = IsVisible;
                Table.Get<string, string>("Label", out KeyValue);
                if(KeyValue != null) SignlGriph[GraphIndex].Label = KeyValue.Substring(0, Math.Min(KeyValue.Length, 20));
                Table.Get<string, string>("LineStyle", out KeyValue);
                if(LineStyleTryParse(KeyValue, out LineStyle LineStyle)) SignlGriph[GraphIndex].LineStyle = LineStyle;
                Table.Get<string, string>("Smooth", out KeyValue);
                if (bool.TryParse(KeyValue, out bool Smooth)) SignlGriph[GraphIndex].Smooth = Smooth;
                Table.Get<string, string>("SmoothTension", out KeyValue);
                if(KeyValue != null) SignlGriph[GraphIndex].SmoothTension = int.Parse(KeyValue);
                Table.Get<string, string>("OffsetY", out KeyValue);
                if (KeyValue != null) SignlGriph[GraphIndex].OffsetY = double.Parse(KeyValue);
                Table.Get<string, string>("ScaleY", out KeyValue);
                if (KeyValue != null) SignlGriph[GraphIndex].ScaleY = double.Parse(KeyValue);
                Table.Get<string, string>("LineWidth", out KeyValue);
                if (KeyValue != null) SignlGriph[GraphIndex].LineWidth = int.Parse(KeyValue);
                Table.Get<string, string>("IsHighlighted", out KeyValue);
                if (bool.TryParse(KeyValue, out bool IsHighlighted)) SignlGriph[GraphIndex].IsHighlighted = IsHighlighted;
                Table.Get<string, string>("HighlightCoefficient", out KeyValue);
                if (KeyValue != null) SignlGriph[GraphIndex].HighlightCoefficient = int.Parse(KeyValue);
                //Table.Get<string, string>("Color", out KeyValue);
                PlotRender.Set();
                return;
            }

            foreach (object Value in GraphParams)
            {
                if (GraphIndex >= MaxTable) break;
                GraphData = (string)Value;
                SignlGriph[GraphIndex].Label = GraphData.Substring(0, Math.Min(GraphData.Length, 20));
                GraphIndex++;
            }
            PlotRender.Set();
        }
        private void luaPlotAddPoint(int Index, double Point)
        {
            if(Index >= MaxTable) return;
            PlotDataY[Index][PlotDataYIndex[Index]] = Point;
            SignlGriph[Index].MaxRenderIndex = PlotDataYIndex[Index];
            if (++PlotDataYIndex[Index] >= MaxPoints) PlotDataYIndex[Index] = 0;
            PlotRender.Set();
        }
        private void luaPlotAddPointMulti(params object[] GraphParams)
        {
            int Index = 0;
            double Point;

            foreach (object Value in GraphParams)
            {
                if (Index >= MaxTable) break;
                if (Value is double) Point = (double)Value;
                else if(Value is long) Point = (double)((long)Value);
                else{ Index++;continue;}
                PlotDataY[Index][PlotDataYIndex[Index]++] = Point;
                SignlGriph[Index].MaxRenderIndex = PlotDataYIndex[Index];
                if (++PlotDataYIndex[Index] >= MaxPoints) PlotDataYIndex[Index] = 0;
                Index++;
                PlotRender.Set();
            }
        }
    }
}
