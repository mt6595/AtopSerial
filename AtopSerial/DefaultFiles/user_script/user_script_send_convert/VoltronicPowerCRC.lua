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
return uartData..sCRC16(uartData,uartData:len())..'\r'
--Atomiz_zhang
--No more