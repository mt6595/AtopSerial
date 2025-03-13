--InputBox demo
sys.taskInit(function ()
    while true do
        local ok, result = apiInputBox("Uart Data Send:","","InputBox")
        apiSend("uart",result)
        log.info("uart send",result)
        sys.wait(2000)--Delay 2000ms
    end
end)