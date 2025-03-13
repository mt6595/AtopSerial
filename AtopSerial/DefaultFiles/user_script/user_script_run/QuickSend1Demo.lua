--Loop the quick zone send data

--send data interval (ms)
local sendDelay = 1000

--Quick zone index
local sendList = {1,2,4,1,6,8}

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