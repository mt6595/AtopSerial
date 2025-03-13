apiPlotConfig({PlotIndex = 0,Label = "Graph-0",IsVisible = "true"})
apiPlotConfig({PlotIndex = 1,Label = "Graph-1",IsVisible = "true"})
apiPlotConfig({PlotIndex = 2,Label = "Graph-1",IsVisible = "true"})

--Split by structure, reference https://cloudwu.github.io/lua53doc/manual.html#6.4.2
local u8,i32,f32 = string.unpack("<Blf",uartData)
apiPlotAddPointMulti(u8,i32,f32)

--return raw data
return uartData