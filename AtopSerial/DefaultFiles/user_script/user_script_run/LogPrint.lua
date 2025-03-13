--Log print demonstration
sys.taskInit(function ()
    while true do
        log.info("info Log:", "...")
        sys.wait(200);
        log.warn("warn Log:", "...")
        sys.wait(200);
        log.error("error Log:", "...")
        sys.wait(200);
        log.fatal("fatal Log:", "...")
        sys.wait(200);
    end
end)