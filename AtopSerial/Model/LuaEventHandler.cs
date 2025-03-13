using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtopSerial.Model
{
    internal class luaPlotAddPoint
    {
        public int Index { get; set; }
        public double Point { get; set; }
    }
    internal class luaPlotAddPointMulti
    {
        public object[] Parameters{ get; set; }
        public luaPlotAddPointMulti(params object[] Para)
        {
            Parameters = Para;
        }
    }
    internal class luaPlotConfig
    {
        public object[] Parameters { get; set; }
        public luaPlotConfig(params object[] Para)
        {
            Parameters = Para;
        }
    }
}
