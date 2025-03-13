
--Register serial port receiver functions
apiSetCb("uart", function (data)
    log.info("uart receive",data)
	--publish the message
    sys.publish("UART",data)
end)

sys.taskInit(function()
    while true do
        --Wait message， Wait timed out 1000ms.
        local r,udata = sys.waitUntil("UART",1000)
        log.info("uart wait",r,udata)
        if r then
            --Serial data answer
            local sendResult = apiSend("uart","ok!")
            log.info("uart send",sendResult)
        end
    end
end)

--Create a new task and continue hibernating every 1000ms
sys.taskInit(function()
    while true do
        sys.wait(1000)
        log.info("task wait",os.time())
    end
end)

--1000ms timer,Print the "timer test" log
sys.timerLoopStart(log.info,1000,"timer test")