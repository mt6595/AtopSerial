-----------------------------Graph Info---------------------------------
sampleLenght  	= 500;
sampleInterval 	= 0;

Channel_1 = {Sour=95, Name="Graph-1", Symbol=true};
Channel_2 = {Sour=96, Name="Graph-2", Symbol=true};
Channel_3 = {Sour=97, Name="Graph-3", Symbol=true};
Channel_4 = {Sour=98, Name="Graph-4", Symbol=true};
------------------------------------------------------------------------

local UartRevHook =
	function (uartData)
		sys.publish("uartData", uartData)
	end

function GraphAnalysis(UartData, Channel, Symbol)
	local graphLength= (string.byte(string.sub(UartData, 3, 3)) << 8) + string.byte(string.sub(UartData, 4, 4))
	local graphChkSum = graphLength + 1
	local graphValue={}
	local iTemp, jTemp
	for iTemp = 5, string.len(UartData) - 2, 2 do
		jTemp = (iTemp-5)/2
		graphValue[jTemp] = (string.byte(UartData, iTemp) << 8) + string.byte(UartData, iTemp + 1)
        graphValue[jTemp] = (graphValue[jTemp] >= 0x8000 and Symbol==true) and (graphValue[jTemp] - 0x10000) or graphValue[jTemp]
		graphChkSum = graphChkSum + graphValue[jTemp]
	end
	graphChkSum = graphChkSum&0xffff
	local lastTwoBytes = string.sub(UartData, -2)
	local inputChkSum = (string.byte(lastTwoBytes, 1) << 8) + string.byte(lastTwoBytes, 2)
	if(graphChkSum ~= inputChkSum) then
		return false
	end
	for iTemp = 0, #graphValue do
		apiPlotAddPoint(Channel,graphValue[iTemp])
	end
	return true
end

sys.taskInit(function ()
	sampWait  = math.floor(sampleLenght * (sampleInterval+1) * 80 / 1000);
	apiPlotInit()
	apiPlotConfig({PlotIndex = 0,Label = Channel_1.Name,IsVisible = "true",ScaleY=1,OffsetY=0})
	apiPlotConfig({PlotIndex = 1,Label = Channel_2.Name,IsVisible = "true",ScaleY=1,OffsetY=0})
	apiPlotConfig({PlotIndex = 2,Label = Channel_3.Name,IsVisible = "true",ScaleY=1,OffsetY=0})
	apiPlotConfig({PlotIndex = 3,Label = Channel_4.Name,IsVisible = "true",ScaleY=1,OffsetY=0})
	
	log.info("Graph: Send parameter")
	SendOrderFrame = "Q3"..
	 				string.format("%03d ", sampleLenght)..
	 				string.format("%03d ", sampleInterval)..
	 				string.format("%02d ", Channel_1.Sour)..
	 				string.format("%02d ", Channel_2.Sour)..
	 				string.format("%02d ", Channel_3.Sour)..
	 				string.format("%02d ", Channel_4.Sour)..
	 				"00 ".."1 ".."0 ".."0 ".."00000"..'\r'
	apiSetCb("uart", UartRevHook)
	
	apiSend("uart",SendOrderFrame)
	local revFlag,uartData = sys.waitUntilExt("uartData", 2000)
	if revFlag == false or not string.find(uartData, "Q3") then
		log.info("Graph: Sample fail 001")
		return
	end
	log.info("Graph: Wait Sampling")
	sys.wait(sampWait)

	log.info("Graph: Wait Polt 1...")
	apiSend("uart","QD0\r")
	local revFlag,uartData = sys.waitUntilExt("uartData", 10000)
	if revFlag == false then
		log.info("Graph: Sample fail 002")
		return
	elseif(not GraphAnalysis(uartData, 0, Channel_1.Symbol)) then 
		log.info("Graph: Sample fail 003")
		return
	end

	log.info("Graph: Wait Polt 2...")
	apiSend("uart","QD1\r")
	local revFlag,uartData = sys.waitUntilExt("uartData", 10000)
	if revFlag == false then
		log.info("Graph: Sample fail 004")
		return
	elseif(not GraphAnalysis(uartData, 1, Channel_2.Symbol)) then 
		log.info("Graph: Sample fail 005")
		return
	end

	log.info("Graph: Wait Polt 3...")
	apiSend("uart","QD2\r")
	local revFlag,uartData = sys.waitUntilExt("uartData", 10000)
	if revFlag == false then
		log.info("Graph: Sample fail 006")
		return
	elseif(not GraphAnalysis(uartData, 2, Channel_3.Symbol)) then 
		log.info("Graph: Sample fail 003")
		return
	end

	log.info("Graph: Wait Polt 4...")
	apiSend("uart","QD3\r")
	local revFlag,uartData = sys.waitUntilExt("uartData", 10000)
	if revFlag == false then
		log.info("Graph: Sample fail 007")
		return
	elseif(not GraphAnalysis(uartData, 3, Channel_4.Symbol)) then 
		log.info("Graph: Sample fail 008")
		return
	end
	log.info("Graph: Finish!")
end)
--Atomiz_zhang
--No more