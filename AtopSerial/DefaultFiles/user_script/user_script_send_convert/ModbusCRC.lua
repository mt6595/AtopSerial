function ModbusCRC(data, len)
    local CRC = 0xFFFF
    for i = 1, #data do
        CRC = CRC ~ string.byte(data, i)
        for j = 1, 8 do
            local carry = CRC & 0x0001
            CRC = CRC >> 1
            if carry == 1 then
                CRC = CRC ~ 0xA001
            end
        end
    end
    bCRCLow = CRC&0xFF;
    bCRCHigh = (CRC>>8)&0xFF;
    return string.char(bCRCLow)..string.char(bCRCHigh);
end
return uartData..ModbusCRC(uartData,uartData:len())
--Atomiz_zhang
--No more