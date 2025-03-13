--example
sys.taskInit(function ()
    while true do
        apiSend("uart","test example")
        sys.wait(500)--Delay 500ms
    end
end)