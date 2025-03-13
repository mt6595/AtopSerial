--Loop the quick zone send data

--To be sent quick zone index and delay time
local sendList = {
    {1,1000},
    {3,500},
    {2,2000},
    {4,300},
    {5,6000},
    {6,1500},
}

sys.taskInit(function ()
    while true do
        for _,i in pairs(sendList) do
            local data = apiQuickSendList(i[1])
            if data then
                log.info("send data",apiSendUartData(data),data)
            end
            sys.wait(i[2])
        end
    end
end)