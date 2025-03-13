--Automatic processing of GPS NMEA data
local check = uartData:byte(2)
for i=3,uartData:len() do
	check = check ~ uartData:byte(i)
end

return uartData.."*"..
string.char(check%256):toHex().."\r\n"

