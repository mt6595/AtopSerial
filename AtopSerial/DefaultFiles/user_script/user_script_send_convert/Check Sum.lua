--Add checksum at the end of the data stream

local checksum = 0
local start = 1

for i=start,#uartData do
	checksum = checksum ~ uartData:byte(i)
	checksum = checksum % 0x100
end

--Return results
return uartData..string.char(checksum)

