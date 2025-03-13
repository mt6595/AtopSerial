CRC_Table=
{
    0x0000,0x1021,0x2042,0x3063,0x4084,0x50a5,0x60c6,0x70e7,
    0x8108,0x9129,0xa14a,0xb16b,0xc18c,0xd1ad,0xe1ce,0xf1ef
};

function sCRC16(Buffer,BuffLen)
    local CRC = 0;
    local CRCTemp = 0;
    local BufferPoint = 0;
    local bCRCHigh = 0;
    local bCRCLow = 0;
 
	while(BuffLen ~= 0) do
		BuffLen = BuffLen-1;
		BufferPoint = BufferPoint+1;
		CRCTemp=(CRC>>12)&0x0F;
		CRC= (CRC<<4) & 0xFFFF;
		CRC = CRC~CRC_Table[(CRCTemp~(string.byte(Buffer,BufferPoint)>>4))+1];
		CRC = CRC&0xFFFF;
		CRCTemp=(CRC>>12)&0x0F;
		CRC= (CRC<<4) & 0xFFFF;
		CRC = CRC~CRC_Table[(CRCTemp~(string.byte(Buffer,BufferPoint)&0x0F))+1];
		CRC = CRC&0xFFFF;
	end
    bCRCLow = CRC&0xFF;
    bCRCHigh = (CRC>>8)&0xFF;
    if(bCRCLow==0x28 or bCRCLow==0x0d or bCRCLow==0x0a) then
        bCRCLow = bCRCLow+1;
    end
    if(bCRCHigh==0x28 or bCRCHigh==0x0d or bCRCHigh==0x0a) then
        bCRCHigh = bCRCHigh+1;
    end
    return string.char(bCRCHigh)..string.char(bCRCLow);
end


sys.taskInit(function ()

	BMSBatPercent = 080
	BMSBatVolt = 540
	BMSBatCurrent = 0
	BMSBatCVVolt = 560
	BMSBatFloatVolt = 550
	BMSCutOffVol = 420
	BMSMaxChgCurrent = 100
	BMSMaxDisChgCurr = 100
	BMSWarningCode = 0
	BMSStopDisChgFlag = 0
	BMSStopChgFlag = 0
	BMSForceChgFlag = 0
	
	log.info("You can modify the following parameter values with the Lua directive","\r")
	log.info("BMSBatPercent\t\t"..BMSBatPercent)
	log.info("BMSBatVolt\t\t"..BMSBatVolt)
	log.info("BMSBatCurrent\t\t"..BMSBatCurrent)
	log.info("BMSBatCVVolt\t\t"..BMSBatCVVolt)
	log.info("BMSBatFloatVolt\t"..BMSBatFloatVolt)
	log.info("BMSCutOffVol\t\t"..BMSCutOffVol)
	log.info("BMSMaxChgCurrent\t"..BMSMaxChgCurrent)
	log.info("BMSMaxDisChgCurr\t"..BMSMaxDisChgCurr)
	log.info("BMSWarningCode\t"..BMSWarningCode)
	log.info("BMSStopDisChgFlag\t"..BMSStopDisChgFlag)
	log.info("BMSStopChgFlag\t"..BMSStopChgFlag)
	log.info("BMSForceChgFlag\t"..BMSForceChgFlag)
	
	while true do
		sendBMSInfo = "^D054BMS"
		sendBMSInfo = sendBMSInfo..string.format("%04d", BMSBatVolt)
		sendBMSInfo = sendBMSInfo..','..string.format("%03d", BMSBatPercent)
		sendBMSInfo = sendBMSInfo..','..((BMSBatCurrent < 0) and '1' or '0')
		sendBMSInfo = sendBMSInfo..','..string.format("%04d", -BMSBatCurrent)
		sendBMSInfo = sendBMSInfo..','..string.format("%01d", BMSWarningCode)
		sendBMSInfo = sendBMSInfo..','..string.format("%01d", BMSForceChgFlag)
		sendBMSInfo = sendBMSInfo..','..string.format("%04d", BMSBatCVVolt)
		sendBMSInfo = sendBMSInfo..','..string.format("%04d", BMSBatFloatVolt)
		sendBMSInfo = sendBMSInfo..','..string.format("%04d", BMSMaxChgCurrent)
		sendBMSInfo = sendBMSInfo..','..string.format("%01d", BMSStopDisChgFlag)
		sendBMSInfo = sendBMSInfo..','..string.format("%01d", BMSStopChgFlag)
		sendBMSInfo = sendBMSInfo..','..string.format("%04d", BMSCutOffVol)
		sendBMSInfo = sendBMSInfo..','..string.format("%04d", BMSMaxDisChgCurr)
		sendBMSInfo = sendBMSInfo..sCRC16(sendBMSInfo,sendBMSInfo:len())..'\r'
        apiSend("uart",sendBMSInfo)
        sys.wait(2000)
    end
end)
--Atomiz_zhang
--No more