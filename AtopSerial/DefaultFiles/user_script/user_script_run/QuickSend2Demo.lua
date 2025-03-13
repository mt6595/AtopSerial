--Loop the quick zone send data

--send data interval (ms)
local sendDelay = 1000

local sendList = {}

--Quick zone index,It starts at 2 and ends at 15
for i=2,15 do
    table.insert(sendList, i)
end

sys.taskInit(function ()
    while true do
        for _,i in pairs(sendList) do
            local data = apiQuickSendList(i)
            if data then
                log.info("send data",apiSendUartData(data),data)
            end
            sys.wait(sendDelay)
        end
    end
end)