--Parsing carriage return and line break escape characters
return uartData:gsub("\\r","\r"):gsub("\\n","\n")
