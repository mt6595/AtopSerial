apiPlotConfig({PlotIndex = 0,Label = "Graph",IsVisible = "true"})
local data = uartData:split("\r\n")

for i=1,#data do
    local temp = tonumber(data[i])
    if temp then
        apiPlotAddPoint(0,temp)
    end
end

--return raw data
return uartData