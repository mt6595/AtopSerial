uartReceive = function (data)	
	sys.publish("uartDataPush", data)
end

function GraphAnalysis(UartData, Channel, DataType)
	local graphLength= (string.byte(string.sub(UartData, 3, 3)) << 8) + string.byte(string.sub(UartData, 4, 4))
	local graphChkSum = graphLength + 1
	local graphValue={}
	local iTemp, jTemp
	for iTemp = 5, string.len(UartData) - 2, 2 do
		jTemp = (iTemp-5)/2
		graphValue[jTemp] = (string.byte(UartData, iTemp) << 8) + string.byte(UartData, iTemp + 1)
        if DataType == "0" then
            graphValue[jTemp] = (graphValue[jTemp] >= 0x80) and (graphValue[jTemp] - 0x100) or graphValue[jTemp]
        elseif DataType == "2" then
            graphValue[jTemp] = (graphValue[jTemp] >= 0x8000) and (graphValue[jTemp] - 0x10000) or graphValue[jTemp]
        elseif DataType == "4" then
            graphValue[jTemp] = (graphValue[jTemp] >= 0x80000000) and (graphValue[jTemp] - 0x100000000) or graphValue[jTemp]
        end
		graphChkSum = graphChkSum + graphValue[jTemp]
	end
	graphChkSum = graphChkSum&0xffff
	local LastTwoBytes = string.sub(UartData, -2)
	local InputChkSum = (string.byte(LastTwoBytes, 1) << 8) + string.byte(LastTwoBytes, 2)
	if(graphChkSum ~= InputChkSum) then
		return false
	end
	for iTemp = 0, #graphValue do
		apiPlotAddPoint(Channel,graphValue[iTemp])
	end
	return true
end

function DataSendWaitAns(sendData, revComp)
	while true do
		apiSend("uart",sendData)
		local revFlag,revData = sys.waitUntilExt("uartDataPush", 2000)
		if revFlag == false then
			log.info("Graph: Wait timeout, try again...")
		else
			revData = revData == nil and "" or revData
			if string.find(revData, revComp) ~= nil then
				break
			elseif string.find(revData, "NAK\r") ~= nil then
				log.info("Graph: Return error, try again...")
			end
		end
		sys.wait(500)
	end
end

sys.taskInit(function ()
	apiPlotInit()
	graphSnatchPara = apiGraphSnatchPara()
	graphChannelPara =  apiGraphChannelPara()
	sampWait = math.floor(tonumber(graphSnatchPara.SampleLength) * (tonumber(graphSnatchPara.SampleInterval)+1) * 80 / 1000)
	---------------------------------------------------------------------------------
	if tonumber(graphSnatchPara.SampleMode) > 3 then
		log.info("Graph: Mode parameter error!")
		return;
	end
	if tonumber(graphSnatchPara.TriggerPara) > 999 then
		log.info("Graph: TrigPara parameter error!")
		return;
	end
	if tonumber(graphSnatchPara.TriggerLag) > 999 then
		log.info("Graph: TrigDelay parameter error!")
		return;
	end
	if tonumber(graphSnatchPara.SampleLength) > 999 then
		log.info("Graph: Length parameter error!")
		return;
	end
	if tonumber(graphSnatchPara.SampleInterval) > 999 then
		log.info("Graph: Interval parameter error!")
		return;
	end
	--------------------------------------------------------------------------------
	local unselectedNum = 0
	for iTemp = 1, #graphChannelPara do
		apiPlotConfig({
			PlotIndex = tostring(tonumber(graphChannelPara[iTemp].channel)-1),
			Label = graphChannelPara[iTemp].description,
			IsVisible = graphChannelPara[iTemp].show,
			ScaleY = graphChannelPara[iTemp].scaleY,
			OffsetY = graphChannelPara[iTemp].offset,
			LineWidth = 2})
			
		if graphChannelPara[iTemp].show == "True" then
			if tonumber(graphChannelPara[iTemp].type) > 7 then
				log.info("Graph: Data type parameter error!")
				return;
			elseif(graphChannelPara[iTemp].type ~= "2" and graphChannelPara[iTemp].type ~= "3") then
				log.info("Graph: Supports only INT16S and INT16U type!")
				return;
			end

			local dataSourLen = #graphChannelPara[iTemp].dataSour
			if dataSourLen == 0 then
				log.info("Graph: The data source cannot be empty!")
				return;
			elseif dataSourLen <= 3 then
				while dataSourLen < 3 do
				    graphChannelPara[iTemp].dataSour = "0" .. graphChannelPara[iTemp].dataSour
				    dataSourLen = dataSourLen + 1
				end
			elseif dataSourLen < 8 then
				while dataSourLen < 8 do
				    graphChannelPara[iTemp].dataSour = "0" .. graphChannelPara[iTemp].dataSour
				    dataSourLen = dataSourLen + 1
				end
			end
			DataSendWaitAns("TDO"..(iTemp-1).."="..graphChannelPara[iTemp].dataSour..','..string.format("%02d", graphChannelPara[iTemp].type)..'\r', "ACK\r")
			sys.wait(100)
		else
			unselectedNum = unselectedNum+1
			if(unselectedNum == #graphChannelPara) then
				log.info("Graph: No channel is selected!")
				return
			end
		end
	end
	---------------------------------------------------------------------------------
	if graphSnatchPara.SampleMode == "0" then
		log.info("Graph: Send quick sample parameter")
		DataSendWaitAns(
				"TDTI="..
				string.format("%03d", graphSnatchPara.SampleLength)..","..
				string.format("%03d", graphSnatchPara.SampleInterval)..'\r', 
				"ACK\r")
				sys.wait(sampWait)
	elseif graphSnatchPara.SampleMode == "1" then
		log.info("Graph: Send fault trigger parameter")
		DataSendWaitAns(
				"TDTF="..
				string.format("%03d", graphSnatchPara.SampleLength)..","..
				string.format("%03d", graphSnatchPara.SampleInterval)..","..
				string.format("%03d", graphSnatchPara.TriggerPara)..","..
				string.format("%03d", graphSnatchPara.TriggerLag)..'\r', 
				"ACK\r")
		log.info("Graph: Wait Trigger...")
		sys.wait(100)
		DataSendWaitAns("TDTS\r", "1\r")
		log.info("Graph: Trigger Flag")
		sys.wait(100)
	elseif graphSnatchPara.SampleMode == "2" then
		log.info("Graph: Send event trigger parameter")
		DataSendWaitAns(
				"TDTE="..
				string.format("%03d", graphSnatchPara.SampleLength)..","..
				string.format("%03d", graphSnatchPara.SampleInterval)..","..
				string.format("%03d", graphSnatchPara.TriggerPara)..","..
				string.format("%03d", graphSnatchPara.TriggerLag)..'\r', 
				"ACK\r")
		log.info("Graph: Wait Trigger...")
		sys.wait(100)
		DataSendWaitAns("TDTS\r", "1\r")
		log.info("Graph: Trigger Flag")
		sys.wait(100)
	elseif graphSnatchPara.SampleMode == "3" then
		while true do
			apiSend("uart","TDTD\r")
			local revFlag,revData = sys.waitUntilExt("uartDataPush", 1000)
			revData = revData == nil and "" or revData
			if revFlag == false then
				log.info("Graph: Reply timeout, try again...")
			else
				local graphNormal = true;
				local graphTemp = revData:split(",")
				local graphValue = 0;
				for iTemp = 1, #graphTemp do
					if(iTemp <= #graphChannelPara) then
						graphValue = tonumber(graphTemp[iTemp])
						if(graphValue == nil) then
							graphNormal = false
							log.info("Graph: Reply timeout, try again...")
						end
					end
				end
				if graphNormal == true then
					for iTemp = 1, #graphTemp do
						if(iTemp <= #graphChannelPara) then
							graphValue = tonumber(graphTemp[iTemp])
							if(graphChannelPara[iTemp].type == "2") then
								graphValue = (graphValue >= 0x8000) and (graphValue - 0x10000) or graphValue
							end
							apiPlotAddPoint(iTemp-1,graphValue)
						end
					end
				end
			end
			sys.wait(sampWait)
		end
	end
	---------------------------------------------------------------------------------
	for iTemp = 1, #graphChannelPara do
		if graphChannelPara[iTemp].show == "True" then
			log.info("Graph: Wait polt "..iTemp.."...")
			while true do
				apiSend("uart","TDCD"..(iTemp-1).."\r")
				local revFlag,revData = sys.waitUntilExt("uartDataPush", 10000)
				revData = revData == nil and "" or revData
				if revFlag == false then
					log.info("Graph: Wait timeout, try again...")
				elseif(not GraphAnalysis(revData, iTemp-1, graphChannelPara[iTemp].type)) then 
					log.info("Graph: Polt data checksum error!")
					log.info("Graph: Try again...")
				else
					break
				end
				sys.wait(1000)
			end
		end
	end
	---------------------------------------------------------------------------------
	log.info("Graph: Finish!")
end)
--Atomiz_zhang
--No more