--Parsing format [Value\r\n], character data, positive and floating point support

apiPlotConfig({PlotIndex = 0,Label = "Graph",IsVisible = "true"})
local data = uartData:split("\r\n")
local temp = tonumber(data[1])
if temp then
    apiPlotAddPoint(0,temp)
end

--return raw data
return uartData