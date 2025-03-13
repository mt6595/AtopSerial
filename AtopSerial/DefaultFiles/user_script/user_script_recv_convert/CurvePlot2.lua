apiPlotConfig({PlotIndex = 0,Label = "Graph-0",IsVisible = "true"})
apiPlotConfig({PlotIndex = 1,Label = "Graph-1",IsVisible = "true"})
apiPlotConfig({PlotIndex = 2,Label = "Graph-1",IsVisible = "true"})
local data = uartData:split("\r\n")

for i=1,#data do
    local temp = data[i]:split(",")
    if #temp == 3 then
        local n1 = tonumber(temp[1])
        local n2 = tonumber(temp[2])
        local n3 = tonumber(temp[3])
        if n1 and n2 and n3 then
            apiPlotAddPointMulti(n1,n2,n3)
        end
    end
end

--return raw data
return uartData