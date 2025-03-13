uartReceive = function (data)	
	sys.publish("uartDataPush", data)
end

local revData
local revFlag
sys.taskInit(function ()
	dataMonitorPara = apiDataMonitorPara()
	while true do
		local recordTitle = {}
		local sampleNum = 0
		for index, monitorPara in ipairs(dataMonitorPara) do
			if monitorPara.sampleEn == "True" then
				sampleNum = sampleNum + 1
				while true do
					apiSend("uart","TDTO="..string.format("%03d", (index-1)).."\r")
					revFlag,revData = sys.waitUntilExt("uartDataPush", 1000)
                    revData = revData or ""
                    if not revFlag or revData == "" then
						log.info("Monitor: Reply timeout, try again...")
					else
						--log.info(monitorPara.description.." = "..revData)
						break
					end
				end
				revData = revData:gsub("[\r\n]", "")
				recordTitle[index] = revData
				apiDataMonitorValue(index-1, tostring(revData))
			else
				recordTitle[index] = ""
			end
		end
		if sampleNum > 0 then
			apiDataMonitorRecord(recordTitle)
		end
		sys.wait(1)
	end
end)
--Atomiz_zhang
--No more