--[[Versions: 1.0
TDFV: Graph version info
    Example: "TDFV\r"
    Response: "Vaa,bb,cc,dddd\r"
    [a] Tool version, 00~99
    [b] Maximum supported snatch data type, 01(Byte), 02(Word), 04(DWord)
    [c] Maximum supported snatch channel count, 01~09
    [d] Maximum sample points per snatch channel, 0001~9999

TDEL: Get debug event list
    Example: "TDEL\r"
    Response: "(NIL\r", "a...,b...,c...,...\r"
    [a...] Event 1, variable-length value
    [b...] Event 2, variable-length value
    [c...] Event 3, variable-length value
    [...] Returns a list of event records with variable-length parameters.
          Event records are stored in a fixed-capacity buffer. New events will be discarded once the buffer is full.
          The buffer is cleared every time the event list is read.

TDBB: Read memory address data (Byte, Binary format)
    Example: "TDBBa...\r", "TDBB0xa...\r"
    [a...] Channel index, variable-length
    [0xa...] Hexadecimal address, variable-length
    Response: "aaaaaaaab\r", "(NAK\r"
    [a...b] Returned value in binary format

TDBW: Read memory address data (Word, Binary format)
    Example: "TDBWa...\r", "TDBW0xa...\r"
    [a...] Channel index, variable-length
    [0xa...] Hexadecimal address, variable-length
    Response: "aaaaaaaa_aaaaaaaab\r", "(NAK\r"
    [a...b] Returned value in binary format

TDBD: Read memory address data (DWord, Binary format)
    Example: "TDBDa...\r", "TDBD0xa...\r"
    [a...] Channel index, variable-length
    [0xa...] Hexadecimal address, variable-length
    Response: "aaaaaaaa_aaaaaaaa_aaaaaaaa_aaaaaaaab\r", "(NAK\r"
    [a...b] Returned value in binary format

TDDB: Read memory address data (Byte, Decimal format)
    Example: "TDDBa...\r", "TDDB0xa...\r"
    [a...] Channel index, variable-length
    [0xa...] Hexadecimal address, variable-length
    Response: "a...\r", "(NAK\r"
    [a...] Returned value in decimal format, variable-length

TDDW: Read memory address data (Word, Decimal format)
    Example: "TDDWa...\r", "TDDW0xa...\r"
    [a...] Channel index, variable-length
    [0xa...] Hexadecimal address, variable-length
    Response: "a...\r", "(NAK\r"
    [a...] Returned value in decimal format, variable-length

TDDD: Read memory address data (DWord, Decimal format)
    Example: "TDDDa...\r", "TDDD0xa...\r"
    [a...] Channel index, variable-length
    [0xa...] Hexadecimal address, variable-length
    Response: "a...\r", "(NAK\r"
    [a...] Returned value in decimal format, variable-length

TDHB: Read memory address data (Byte, Hexadecimal format)
    Example: "TDHBa...\r", "TDHB0xa...\r"
    [a...] Channel index, variable-length
    [0xa...] Hexadecimal address, variable-length
    Response: "a...\r", "(NAK\r"
    [a...] Returned value in hexadecimal format, variable-length

TDHW: Read memory address data (Word, Hexadecimal format)
    Example: "TDHWa...\r", "TDHW0xa...\r"
    [a...] Channel index, variable-length
    [0xa...] Hexadecimal address, variable-length
    Response: "a...\r", "(NAK\r"
    [a...] Returned value in hexadecimal format, variable-length

TDHD: Read memory address data (DWord, Hexadecimal format)
    Example: "TDHDa...\r", "TDHD0xa...\r"
    [a...] Channel index, variable-length
    [0xa...] Hexadecimal address, variable-length
    Response: "a...\r", "(NAK\r"
    [a...] Returned value in hexadecimal format, variable-length

TDSB: Write memory address data (Byte)
    Example: "TDSBa...=b...\r", "TDSB0xa...=0xb...\r"
    Example: "TDSBa...=0xb...\r", "TDSB0xa...=b...\r"
    [a...] Channel index, variable-length
    [0xa...] Hexadecimal address, variable-length
    [b...] Decimal value to write, variable-length
    [0xb...] Hexadecimal value to write, variable-length
    Response: "(ACK\r", "(NAK\r"

TDSW: Write memory address data (Word)
    Example: "TDSWa...=b...\r", "TDSW0xa...=0xb...\r"
    Example: "TDSWa...=0xb...\r", "TDSW0xa...=b...\r"
    [a...] Channel index, variable-length
    [0xa...] Hexadecimal address, variable-length
    [b...] Decimal value to write, variable-length
    [0xb...] Hexadecimal value to write, variable-length
    Response: "(ACK\r", "(NAK\r"

TDSD: Write memory address data (DWord)
    Example: "TDSDa...=b...\r", "TDSD0xa...=0xb...\r"
    Example: "TDSDa...=0xb...\r", "TDSD0xa...=b...\r"
    [a...] Channel index, variable-length
    [0xa...] Hexadecimal address, variable-length
    [b...] Decimal value to write, variable-length
    [0xb...] Hexadecimal value to write, variable-length
    Response: "(ACK\r", "(NAK\r"

TDPS: Get snatch sample completion status
    Example: "TDPS\r"
    Response: "(NIL\r", "(ACK\r"

TDPC: Configure snatch channel CH-x
    Example: "TDPCa=bb,c...\r", "TDPCa=bb,0xc...\r"
    [a] Snatch channel ID, 0~9
    [b] Data type, 01(Byte), 02(Word), 04(DWord)
    [c...] Channel index, variable-length
    [0xc...] Hexadecimal address, variable-length
    Response: "(ACK\r", "(NAK\r"

TDPI: Configure trigger snatch (Immediate)
    Example: "TDPI=aaa,bbb[,ccc,ddd]\r"
    [a] Sample count, 001~999
    [b] Sampling interval, 000~999
    [c] Trigger code, 000~999, optional parameter, no functional effect
    [d] Post-trigger delay length, 000~999, optional parameter, no functional effect
    Response: "(ACK\r", "(NAK\r"

TDPF: Configure trigger snatch (Fault)
    Example: "TDPF=aaa,bbb,ccc,ddd\r"
    [a] Sample count, 001~999
    [b] Sampling interval, 000~999
    [c] Fault trigger code, 000~999
    [d] Post-trigger delay length, 000~999
    Response: "(ACK\r", "(NAK\r"

TDPE: Configure trigger snatch (Event)
    Example: "TDPE=aaa,bbb,ccc,ddd\r"
    [a] Sample count, 001~999
    [b] Sampling interval, 000~999
    [c] Event trigger code, 000~999
    [d] Post-trigger delay length, 000~999
    Response: "(ACK\r", "(NAK\r"

TDPO: Read plot data of all snatch channels at once
    Example: "TDPO\r"
    Response: "Frame header, Data type, Sample length H&L, Sample data, Checksum H&L", "(NAK\r"
    [Frame header] 0x01
    [Data type] 04(DWord)
    [Sample length H] Upper 8 bits of sample length
    [Sample length L] Lower 8 bits of sample length
    [Sample data] Total length = Data type × Sample length. Sample length is fixed to the maximum supported channel count.
    [Checksum] Sum of frame header, data type, sample length H, sample length L and sample data, range 0000~FFFF

TDPD: Read plot data of a single snatch channel
    Example: "TDPDa\r"
    [a] Snatch channel ID, 0~9
    Response: "Frame header, Data type, Sample length H&L, Sample data, Checksum H&L", "(NAK\r"
    [Frame header] 0x01
    [Data type] 01(Byte), 02(Word), 04(DWord)
    [Sample length H] Upper 8 bits of sample length
    [Sample length L] Lower 8 bits of sample length
    [Sample data] Aligned by data type. Total length = Data type × Sample length
    [Checksum] Sum of frame header, data type, sample length H, sample length L and sample data, range 0000~FFFF
--]]

--Serial port data reception handle, pushing messages to subscribers
uartReceive = function (data)
    sys.publish("uartDataPush", data)
end

--Analyze graph snatch data frames(Vertical axis channel)
function graphAnalysisVAxis (uartReceiveRaw, graphSampleLength, graphChannelPara)
    local matchTable  = {"i1", "I1", "i2", "I2", "i4", "I4", "f"}
    local dataHead, dataUnit, dataNum, dataNextPos
    local dataMatch, dataChkSum, dataChkSumCal = "", 0, 0
    local plotData={}
    
    dataHead, dataNextPos = string.unpack(">I1", uartReceiveRaw, 1)
    dataUnit, dataNextPos = string.unpack(">I1", uartReceiveRaw, dataNextPos)
    dataNum, dataNextPos = string.unpack(">I2", uartReceiveRaw, dataNextPos)
    dataChkSum = string.unpack(">I2", uartReceiveRaw, #uartReceiveRaw - 1)
    for iTemp = 1, #uartReceiveRaw - 2 do dataChkSumCal = (dataChkSumCal + uartReceiveRaw:byte(iTemp)) & 0xffff end
    
    if(#uartReceiveRaw < 6) then log.info("Snatch graph data frame error"); return nil;
    elseif dataHead ~= 0x01 then log.info("Snatch graph data frame error"); return nil;
    elseif dataNum ~= graphSampleLength then log.info("Snatch graph data len error"); return nil;
    elseif dataChkSum ~= dataChkSumCal then log.info("Snatch graph data checksum error"); return nil;
    elseif dataUnit ~= 0x04 then log.info("Snatch graph data type error"); return nil;
    else
        if graphSampleLength > #graphChannelPara then graphSampleLength = #graphChannelPara end
        for plotIndex = 1, graphSampleLength do
            if matchTable[graphChannelPara[plotIndex].type] == "i1" or matchTable[graphChannelPara[plotIndex].type] == "I1" then
                dataMatch = dataMatch.."xxx"..matchTable[graphChannelPara[plotIndex].type]
            elseif matchTable[graphChannelPara[plotIndex].type] == "i2" or matchTable[graphChannelPara[plotIndex].type] == "I2" then
                dataMatch = dataMatch.."xx"..matchTable[graphChannelPara[plotIndex].type]
            else
                dataMatch = dataMatch..matchTable[graphChannelPara[plotIndex].type]
            end
        end
    end
    plotData = {string.unpack(">"..dataMatch, uartReceiveRaw, dataNextPos)}
    return plotData
end

--Analyze graph snatch data frames(Horizontal axis channel)
function graphAnalysisHAxis (uartReceiveRaw, graphSampleLength, graphChannelXPara)
    local matchTable  = {"i1", "I1", "i2", "I2", "i4", "I4", "f"}
    local dataHead, dataUnit, dataNum, dataNextPos
    local dataMatch, dataChkSum, dataChkSumCal = "", 0, 0
    local plotData={}
    
    dataHead, dataNextPos = string.unpack(">I1", uartReceiveRaw, 1)
    dataUnit, dataNextPos = string.unpack(">I1", uartReceiveRaw, dataNextPos)
    dataNum, dataNextPos = string.unpack(">I2", uartReceiveRaw, dataNextPos)
    dataChkSum = string.unpack(">I2", uartReceiveRaw, #uartReceiveRaw - 1)
    for iTemp = 1, #uartReceiveRaw - 2 do dataChkSumCal = (dataChkSumCal + uartReceiveRaw:byte(iTemp)) & 0xffff end
    
    if(#uartReceiveRaw < 6) then log.info("Snatch graph data frame error"); return nil;
    elseif dataHead ~= 0x01 then log.info("Snatch graph data frame error"); return nil;
    elseif dataNum ~= graphSampleLength then log.info("Snatch graph data len error"); return nil;
    elseif dataChkSum ~= dataChkSumCal then log.info("Snatch graph data checksum error"); return nil;
    elseif dataUnit == 0x02 then
        if matchTable[graphChannelXPara.type] == "i1" or matchTable[graphChannelXPara.type] == "I1" then
            dataMatch = "x"..matchTable[graphChannelXPara.type]
        elseif matchTable[graphChannelXPara.type] == "i2" or matchTable[graphChannelXPara.type] == "I2" then
            dataMatch = matchTable[graphChannelXPara.type]
        else
            log.info("Snatch graph data type error"); return nil;
        end
    elseif dataUnit == 0x04 then
        if matchTable[graphChannelXPara.type] == "i1" or matchTable[graphChannelXPara.type] == "I1"  then
            dataMatch = "xxx"..matchTable[graphChannelXPara.type]
        elseif matchTable[graphChannelXPara.type] == "i2" or matchTable[graphChannelXPara.type] == "I2" then
            dataMatch = "xx"..matchTable[graphChannelXPara.type]
        else
            dataMatch = matchTable[graphChannelXPara.type]
        end
    else log.info("Snatch graph data type error"); return nil;
    end
    plotData = {string.unpack(">"..string.rep(dataMatch, graphSampleLength), uartReceiveRaw, dataNextPos)}
    return plotData
end

--Request for graph data
function graphRequest()
    local uartReceiveCnt
    local uartReceiveRaw = ""
    local dataHead, dataUnit, dataNum, dataNextPos
    
    for uartReceiveCnt = 1, 50 do
        local revFlag,revData = sys.waitUntilExt("uartDataPush", 100)
        if revFlag then
            uartReceiveRaw = uartReceiveRaw..(revData == nil and "" or revData)
            if(#uartReceiveRaw >= 6) then
                dataHead, dataNextPos = string.unpack(">I1", uartReceiveRaw, 1)
                dataUnit, dataNextPos = string.unpack(">I1", uartReceiveRaw, dataNextPos)
                dataNum, dataNextPos = string.unpack(">I2", uartReceiveRaw, dataNextPos)
                if dataHead == 0x01 and (dataUnit == 0x02 or dataUnit == 0x04) then
                    if #uartReceiveRaw >= (dataNum * dataUnit + 6) then
                        return uartReceiveRaw
                    end
                else
                    uartReceiveRaw = ""
                end
            end
        end
    end
    log.info("Snatch graph data request error")
    return nil
end

--Send the command and wait for response
function commandSend(sendData, expectData, timeOut)
    while true do
        apiSend("uart",sendData)
        
        if expectData == nil or  expectData == "" then
            return ""
        end
        
        local revFlag,revData = sys.waitUntilExt("uartDataPush", timeOut)
        if revFlag  then
            revData = revData == nil and "" or revData
            if string.find(revData, expectData) ~= nil then
                return revData
            end
        	sys.wait(500)
        else
            log.info("Command wait timeout, try again...")
        end
    end
end

--Main Function
sys.taskInit(function ()
    local graphInfoStruct
    local graphInfoApiVersion
    local graphInfoMaxUnitSize
    local graphInfoMaxSnatchPlot
    local graphInfoMaxSnatchPoint
    local graphDataSizeTable = {1, 1, 2, 2, 4, 4, 4}
    
    local graphSnatchPara = apiGraphSnatchPara()
    local graphChannelPara =  apiGraphChannelPara()
    assert(#graphChannelPara > 0, "The script tool is abnormal")
    ---------------------------------------------------------------------------------
    
    --Convert char-type parameters to numeric values
    graphSnatchPara.SampleMode = tonumber(graphSnatchPara.SampleMode)
    graphSnatchPara.SampleLength = tonumber(graphSnatchPara.SampleLength)
    graphSnatchPara.SampleInterval = tonumber(graphSnatchPara.SampleInterval)
    graphSnatchPara.TriggerPara = tonumber(graphSnatchPara.TriggerPara)
    graphSnatchPara.TriggerLag = tonumber(graphSnatchPara.TriggerLag)
    for plotIndex = 1, #graphChannelPara do
        graphChannelPara[plotIndex].type = tonumber(graphChannelPara[plotIndex].type) + 1
    end
    
    ---------------------------------------------------------------------------------
    
    --Initialize the plot tool and parameter validity check
    apiPlotInit()
    
    for plotIndex = 1, #graphChannelPara do
        if graphChannelPara[plotIndex].show == "True" then
            break
        end
        assert(plotIndex < #graphChannelPara, "No channel is selected")
    end
    
    for plotIndex = 1, #graphChannelPara do
        if graphChannelPara[plotIndex].show == "True" then
            graphChannelPara[plotIndex].dataSour = graphChannelPara[plotIndex].dataSour:gsub("[\x20\t\n\r\v\f\xA0           ]", "")
            graphChannelPara[plotIndex].dataSour = graphChannelPara[plotIndex].dataSour:gsub("0X", "0x")
            assert(#graphChannelPara[plotIndex].dataSour ~= 0, "Invalid data source")
            assert(#graphChannelPara[plotIndex].dataSour <= 10, "Invalid data source")
            if graphChannelPara[plotIndex].dataSour:sub(1,2) == "0x" then
                assert(tonumber(graphChannelPara[plotIndex].dataSour) ~= nil, "Invalid data source")
                graphChannelPara[plotIndex].dataSour = string.format("0x%08x", graphChannelPara[plotIndex].dataSour)
            else
                assert(tonumber(graphChannelPara[plotIndex].dataSour) ~= nil, "Invalid data source")
                graphChannelPara[plotIndex].dataSour = string.format("%d", graphChannelPara[plotIndex].dataSour)
            end
        end
    end
    
    assert(graphSnatchPara.SampleLength > 0, "Sample Length parameter error")
    assert(graphSnatchPara.SampleMode <= 3, "Sample Mode parameter error")
    assert(graphSnatchPara.TriggerPara <= 999, "Trigger Para parameter error")
    assert(graphSnatchPara.TriggerLag <= 999, "Trigger Lag parameter error")
    assert(graphSnatchPara.SampleInterval <= 999, "Sample Interval parameter error")
    
    for plotIndex = 1, #graphChannelPara do
        apiPlotConfig({ PlotIndex = tostring(plotIndex - 1),
                        Label = graphChannelPara[plotIndex].description,
                        IsVisible = graphChannelPara[plotIndex].show,
                        ScaleY = graphChannelPara[plotIndex].scaleY,
                        OffsetY = graphChannelPara[plotIndex].offset,
                        LineWidth = 2})
    end
    
    ---------------------------------------------------------------------------------
    
    --Get the API information of the device
    graphInfoStruct = commandSend("TDFV\r", "^V%d+,%d+,%d+,%d+\r", 1000)
    graphInfoStruct = graphInfoStruct:split(",")
    graphInfoVersion = graphInfoStruct[1]
    graphInfoMaxUnitSize = tonumber(graphInfoStruct[2])
    graphInfoMaxSnatchPlot = tonumber(graphInfoStruct[3])
    graphInfoMaxSnatchPoint = tonumber(graphInfoStruct[4])
    log.info("Snatch graph version:"..graphInfoVersion)
    log.info("Snatch graph max unit:"..graphInfoMaxUnitSize)
    log.info("Snatch graph max plot:"..graphInfoMaxSnatchPlot)
    log.info("Snatch graph max point:"..graphInfoMaxSnatchPoint)
    assert(graphInfoVersion == "V10", "Device version does not match")
    assert(graphInfoMaxSnatchPoint >= graphSnatchPara.SampleLength, "Sample Length parameter error")
    for plotIndex = 1, #graphChannelPara do
        if graphChannelPara[plotIndex].show == "True" then
            assert(graphInfoMaxUnitSize >= graphDataSizeTable[graphChannelPara[plotIndex].type], "Data type not supported")
        end
    end
    
    --------------------------------------------------------------------------------
    
    --Configure the snatch channel
    for plotIndex = 1, #graphChannelPara do
        if graphChannelPara[plotIndex].show == "True" then
            commandSend("TDPC"..(plotIndex-1).."="..string.format("%02d", graphDataSizeTable[graphChannelPara[plotIndex].type])..','..
                        graphChannelPara[plotIndex].dataSour..'\r', "ACK\r", 500)
            sys.wait(100)
        end
    end
    
    ---------------------------------------------------------------------------------
    
    --Fast Sample Mode
    if graphSnatchPara.SampleMode == 0 then
        log.info("Send fast sample parameter")
        commandSend("TDPI="..
                    string.format("%03d", graphSnatchPara.SampleLength)..","..
                    string.format("%03d", graphSnatchPara.SampleInterval)..'\r', 
                    "ACK\r", 2000)
        log.info("Wait sampling...")
    
    ---------------------------------------------------------------------------------
    
    --Fault Trigger Mode
    elseif graphSnatchPara.SampleMode == 1 then
        log.info("Send fault trigger parameter")
        commandSend("TDPF="..
                    string.format("%03d", graphSnatchPara.SampleLength)..","..
                    string.format("%03d", graphSnatchPara.SampleInterval)..","..
                    string.format("%03d", graphSnatchPara.TriggerPara)..","..
                    string.format("%03d", graphSnatchPara.TriggerLag)..'\r', 
                    "ACK\r", 2000)
        log.info("Wait trigger...")
    
    ---------------------------------------------------------------------------------
        
    --Event Trigger Mode
    elseif graphSnatchPara.SampleMode == 2 then
        log.info("Send event trigger parameter")
        commandSend("TDPE="..
                    string.format("%03d", graphSnatchPara.SampleLength)..","..
                    string.format("%03d", graphSnatchPara.SampleInterval)..","..
                    string.format("%03d", graphSnatchPara.TriggerPara)..","..
                    string.format("%03d", graphSnatchPara.TriggerLag)..'\r', 
                    "ACK\r", 2000)
        log.info("Wait trigger...")
    
    ---------------------------------------------------------------------------------
        
    --Scatter Trigger Sample Mode
    elseif graphSnatchPara.SampleMode == 3 then
        local graphChannelNum = #graphChannelPara
        if graphChannelNum > graphInfoMaxSnatchPlot then graphChannelNum = graphInfoMaxSnatchPlot end
        while true do
            apiSend("uart","TDPO\r")
            local uartReceiveRaw = graphRequest()
            if uartReceiveRaw == nil then 
                log.info("Snatch response timeout")
            else
                local plotData = graphAnalysisVAxis (uartReceiveRaw, graphInfoMaxSnatchPlot, graphChannelPara)
                if plotData == nil then 
                    log.info("Snatch analysis error")
                end
                for plotIndex = 1, graphChannelNum do
                    if graphChannelPara[plotIndex].show == "True" then
                        apiPlotAddPoint(plotIndex - 1, plotData[plotIndex])
                    else
                        apiPlotAddPoint(plotIndex - 1, 0)
                    end
                end
            end
            sys.wait((graphSnatchPara.SampleInterval * 10) + 1)
        end
    end
    
    ---------------------------------------------------------------------------------
    
    --Fast Sample Mode/Fault Trigger Mode/Event Trigger Mode
    sys.wait(100)
    commandSend("TDPS\r", "ACK\r", 1000)
    for plotIndex = 1, #graphChannelPara do
        if graphChannelPara[plotIndex].show == "True" then
            log.info("Wait plot "..plotIndex.."...")
            apiSend("uart","TDPD"..(plotIndex-1).."\r")
            local uartReceiveRaw = graphRequest()
            if uartReceiveRaw == nil then 
                log.info("Snatch graph fail")
                sys.waitUntil("LuaEndExecution")
            end
            local plotData = graphAnalysisHAxis (uartReceiveRaw, graphSnatchPara.SampleLength, graphChannelPara[plotIndex])
            if plotData == nil then 
                log.info("Snatch graph fail")
                sys.waitUntil("LuaEndExecution")
            end
            for plotDataIndex = 1, graphSnatchPara.SampleLength do
                apiPlotAddPoint(plotIndex - 1, plotData[plotDataIndex])
            end
            sys.wait(100)
        end
    end
    
    ---------------------------------------------------------------------------------
    
    log.info("Snatch graph finish")
    
    ---------------------------------------------------------------------------------
end)

--Atomiz_zhang
--No more