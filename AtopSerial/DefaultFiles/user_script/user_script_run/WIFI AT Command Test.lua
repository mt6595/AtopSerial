--Air202/Air720 AT TCP Connect Test
local server,ip = "180.97.80.55","12415"

--The content of the data sent
local data = string.rep("123",20)

--server information
local server,ip = "180.97.80.55","12415"

local torev = "OK"

--Register serial port receiver functions
uartReceive = function (data)
    log.info("uart receive")
    if data:find(torev) then
		--publish the message
        sys.publish("UART",data)
    end
end

function rollRun(cmd,receive,timeout)
    if not timeout then timeout = 1000 end
    while true do
        torev = receive
        log.info("uart send",apiSendUartData(cmd.."\r\n"))
        local r,d = sys.waitUntil("UART",timeout)
        if r then break end
    end
end

sys.taskInit(function ()
    log.info("check start")
    rollRun("AT","OK")
	
    log.info("close back")
    rollRun("ATE0","OK")
	
    log.info("check version")
    rollRun("ATI","OK")
	
    log.info("check sim card")
    rollRun("AT+CPIN?","+CPIN: READY")
	
    log.info("check network")
    rollRun("AT+CGATT?","+CGATT: 1")
	
    log.info("set cipmode")
    rollRun("AT+CIPQSEND=1","OK",5000)
	
    log.info("set cipmode")
    rollRun("AT+CIPMODE=0","OK",5000)
	
    log.info("set apn")
    rollRun('AT+CSTT="CMIOT"',"OK",5000)

    log.info("AT+CIICR")
    rollRun("AT+CIICR","OK",5000)

    log.info("check ip")
    rollRun("AT+CIFSR","%.",1000)

    log.info("connect server")
    rollRun([[AT+CIPSTART="TCP","]]..server..[[",]]..ip,"CONNECT OK",5000)

    while true do
        log.info("start send data")
        rollRun("AT+CIPSEND="..tostring(data:len()),">",1000)
        log.info("data content")
        rollRun(data,"DATA ACCEPT",1000)
        sys.wait(5000)
    end
end)