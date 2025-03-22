--[[
	Function apiPlotAttribute([Table])
		Table key:PlotIndex					Table Value:0~9
		Table key:Label						Table Value:string
		Table key:IsVisible					Table Value:"true"/"false"
		Table key:LineStyle					Table Value:"None"/"Solid"
		Table key:Smooth					Table Value:"true"/"false"
		Table key:SmoothTension				Table Value:int/double
		Table key:OffsetY					Table Value:int/double
		Table key:ScaleY					Table Value:int/double
		Table key:LineWidth					Table Value:int/double
		Table key:IsHighlighted				Table Value:"true"/"false"
		Table key:HighlightCoefficient		Table Value:int/double

	Function apiPlotInitialize()
		Clear historical plot data and initialize default plot attributes

	Function apiPlotAddPoint(Index, Value)
		Add plot points, up to 9

	Function apiPlotAddPointMulti(...)
		Add plot points, up to 9
		Demo 1:apiPlotAddPointMulti(DataA,DataB,DataC)
		Demo 2:apiPlotAddPointMulti(nil,nil,DataC)
]]

local math = require("math")
sys.taskInit(function ()
	apiPlotInit();
	apiPlotConfig({PlotIndex = 0,Label = "Graph-0",IsVisible = "true"})
	apiPlotConfig({PlotIndex = 1,Label = "Graph-1",IsVisible = "true"})
	apiPlotConfig({PlotIndex = 2,Label = "Graph-2",IsVisible = "true"})
	apiPlotConfig({PlotIndex = 3,Label = "Graph-3",IsVisible = "true"})
	apiPlotConfig({PlotIndex = 4,Label = "Graph-4",IsVisible = "true"})
	apiPlotConfig({PlotIndex = 5,Label = "Graph-5",IsVisible = "true"})
	apiPlotConfig({PlotIndex = 6,Label = "Graph-6",IsVisible = "true"})
	apiPlotConfig({PlotIndex = 7,Label = "Graph-7",IsVisible = "true"})
	apiPlotConfig({PlotIndex = 8,Label = "Graph-8",IsVisible = "true"})
	apiPlotConfig({PlotIndex = 9,Label = "Graph-9",IsVisible = "true"})
	while true do
		for iTemp = 0, 100000-1 do
            apiPlotAddPoint(0,400*math.sin(2 * math.pi * 300 * iTemp / 299 + 10))
			apiPlotAddPoint(1,400*math.sin(2 * math.pi * 300 * iTemp / 299 + 20))
			apiPlotAddPoint(2,400*math.sin(2 * math.pi * 300 * iTemp / 299 + 30))
			apiPlotAddPoint(3,100*math.sin(2 * math.pi * 300 * iTemp / 299 + 40))
			apiPlotAddPoint(4,100*math.sin(2 * math.pi * 300 * iTemp / 299 + 50))
			apiPlotAddPoint(5,100*math.sin(2 * math.pi * 300 * iTemp / 299 + 60))
			apiPlotAddPoint(6,100*math.sin(2 * math.pi * 300 * iTemp / 299 + 70))
			apiPlotAddPoint(7,100*math.sin(2 * math.pi * 300 * iTemp / 299 + 80))
			apiPlotAddPoint(8,100*math.sin(2 * math.pi * 300 * iTemp / 299 + 90))
			apiPlotAddPoint(9,100*math.sin(2 * math.pi * 300 * iTemp / 299 + 100))
			
            --apiPlotAddPointMulti(
            --			 400*math.sin(2 * math.pi * 300 * iTemp / 299 + 10)
			--            ,400*math.sin(2 * math.pi * 300 * iTemp / 299 + 20)
			--            ,400*math.sin(2 * math.pi * 300 * iTemp / 299 + 30)
			--            ,100*math.sin(2 * math.pi * 300 * iTemp / 299 + 40)
			--            ,100*math.sin(2 * math.pi * 300 * iTemp / 299 + 50)
			--            ,100*math.sin(2 * math.pi * 300 * iTemp / 299 + 60)
			--            ,100*math.sin(2 * math.pi * 300 * iTemp / 299 + 70)
			--            ,100*math.sin(2 * math.pi * 300 * iTemp / 299 + 80)
			--            ,100*math.sin(2 * math.pi * 300 * iTemp / 299 + 90)
			--            ,100*math.sin(2 * math.pi * 300 * iTemp / 299 + 100))
			sys.wait(1)
		end
	end
end)
--Atomiz_zhang
--No more