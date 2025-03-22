-- Parse the hexadecimal structure
-- This routine uses Little-endian encod to parse the hexadecimal data stream into an unsigned char, a signed long, and a signed float
-- Split by structure, reference https://cloudwu.github.io/lua53doc/manual.html#6.4.2

apiPlotConfig({PlotIndex = 0,Label = "Graph-0",IsVisible = "true"})
apiPlotConfig({PlotIndex = 1,Label = "Graph-1",IsVisible = "true"})
apiPlotConfig({PlotIndex = 2,Label = "Graph-2",IsVisible = "true"})

local u8,i32,f32 = string.unpack("<Blf",uartData)
apiPlotAddPointMulti(u8,i32,f32)

--return raw data
return uartData