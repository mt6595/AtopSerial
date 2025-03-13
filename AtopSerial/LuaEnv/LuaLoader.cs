using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtopSerial.LuaEnv
{
    class LuaLoader
    {

        /// <summary> 
        /// 初始化lua对象
        /// </summary>
        /// <param name="lua"></param>
        public static void Initial(XLua.LuaEnv lua, string t = "script")
        {
            //utf8转gbk编码的hex值
            lua.DoString("apiUtf8ToHex = CS.AtopSerial.LuaEnv.LuaApis.Utf8ToAsciiHex");
            lua.DoString("apiAscii2Utf8 = CS.AtopSerial.LuaEnv.LuaApis.Ascii2Utf8");
            //获取软件目录路径
            lua.DoString("apiGetPath = CS.AtopSerial.LuaEnv.LuaApis.GetPath");
            //输出日志
            lua.DoString("apiPrintLog = CS.AtopSerial.LuaEnv.LuaApis.PrintLog");
            //获取快捷发送区数据
            lua.DoString("apiQuickSendList = CS.AtopSerial.LuaEnv.LuaApis.QuickSendList");
            //输入框
            lua.DoString("apiInputBox = CS.AtopSerial.LuaEnv.LuaApis.InputBox");
            //单条曲线添加
            lua.DoString("apiPlotAddPoint = CS.AtopSerial.LuaEnv.LuaApis.luaPlotAddPoint");
            //多条曲线添加
            lua.DoString("apiPlotAddPointMulti = CS.AtopSerial.LuaEnv.LuaApis.luaPlotAddPointMulti");
            //曲线属性
            lua.DoString("apiPlotConfig = CS.AtopSerial.LuaEnv.LuaApis.luaPlotConfig");
            //曲线初始化
            lua.DoString("apiPlotClear = CS.AtopSerial.LuaEnv.LuaApis.luaPlotClear");
            //曲线初始化
            lua.DoString("apiPlotInit = CS.AtopSerial.LuaEnv.LuaApis.luaPlotInit");
            //抓图参数
            lua.DoString("apiGraphSnatchPara = CS.AtopSerial.LuaEnv.LuaApis.luaGraphSnatchPara");
            //抓图通道参数
            lua.DoString("apiGraphChannelPara = CS.AtopSerial.LuaEnv.LuaApis.luaGraphChannelPara");
            //数据监测采集使能
            lua.DoString("apiDataMonitorPara = CS.AtopSerial.LuaEnv.LuaApis.luaDataMonitorPara");
            //数据监测数值返回
            lua.DoString("apiDataMonitorValue = CS.AtopSerial.LuaEnv.LuaApis.luaDataMonitorValue");
            //数据监测数值记录
            lua.DoString("apiDataMonitorRecord = CS.AtopSerial.LuaEnv.LuaApis.luaDataMonitorRecord");

            //发送数据到通用通道
            lua.DoString("apiSend = CS.AtopSerial.LuaEnv.LuaApis.Send");

            if (t != "send")
            {
                //定时器
                lua.DoString("apiStartTimer = CS.AtopSerial.LuaEnv.LuaRunEnv.StartTimer");
                lua.DoString("apiStopTimer = CS.AtopSerial.LuaEnv.LuaRunEnv.StopTimer");
            }

            //加上需要require的路径
            lua.DoString(@"
local rootPath = '"+ LuaApis.Utf8ToAsciiHex(LuaApis.GetPath()) + @"'
rootPath = rootPath:gsub('[%s%p]', ''):upper()
rootPath = rootPath:gsub('%x%x', function(c)
                                    return string.char(tonumber(c, 16))
                                end)
package.path = package.path..
';'..rootPath..'core_script/?.lua'..
';'..rootPath..'?.lua'..
';'..rootPath..'user_script/user_script_run/requires/?.lua'
package.cpath = package.cpath..
';'..rootPath..'core_script/?.lua'..
';'..rootPath..'?.lua'..
';'..rootPath..'user_script/user_script_run/requires/?.lua'
");

            //运行初始化文件
            lua.DoString("require 'core_script.head'");

            if (t == "send")
            {
                lua.DoString(@"
--只运行一次的代码
local rootPath = apiUtf8ToHex(apiGetPath()):fromHex()
--读到的文件
local script = {}
_G[""!once!""] = function()
    runLimitStart(3)
    if not script[_G[""!file!""]] then
        script[_G[""!file!""]] = load(CS.System.IO.File.ReadAllText(_G[""!file!""]))
    end
    local result = script[_G[""!file!""]]()
    runLimitStop()
    return result
end
");
            }
        }


        private static XLua.LuaEnv luaRunner = null;
        /// <summary>
        /// 运行lua文件并获取结果
        /// </summary>
        /// <param name="file"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static byte[] Run(string file, ArrayList args = null, string path = "user_script/user_script_send_convert/")
        {
            //文件不存在
            if (!File.Exists(Tools.Global.ProfilePath + path + file))
                return new byte[] { };

            if (luaRunner == null)
            {
                luaRunner = new XLua.LuaEnv();
                lock(luaRunner)
                {
                    luaRunner.Global.SetInPath("runType", "send");//一次性处理标志
                    Initial(luaRunner, "send");
                }
            }
            lock (luaRunner)
            {
                var pathIn = Tools.Global.ProfilePath + path + file;
                luaRunner.Global.SetInPath("!file!", pathIn);
                while(luaRunner.Global.GetInPath<string>("!file!") != pathIn)
                    luaRunner.Global.SetInPath("!file!", pathIn);

                if (args != null)
                    for (int i = 0; i < args.Count; i += 2)
                    {
                        luaRunner.Global.SetInPath((string)args[i], args[i + 1]);
                    }

                XLua.LuaFunction f = null;
                try
                {
                    while(f == null)
                        f = luaRunner.Global.Get<XLua.LuaFunction>("!once!");
                    var lr = f.Call(null, new Type[] { typeof(byte[]) });
                    var r = lr[0] as byte[];
                    return r;
                }
                catch (Exception e)
                {
                    luaRunner.Dispose();
                    luaRunner = null;
                    throw new Exception(e.ToString());
                }
            }
        }

        /// <summary>
        /// 清除运行用的脚本虚拟机，实现重新加载所有文件的功能
        /// </summary>
        public static void ClearRun()
        {
            luaRunner = null;
        }
    }
}
