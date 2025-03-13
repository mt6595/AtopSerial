-- Forward the received data
apiSetCb("uart",function (data)
    log.info("uart received",data)
end)
local sendResult = apiSend("uart","ok!")