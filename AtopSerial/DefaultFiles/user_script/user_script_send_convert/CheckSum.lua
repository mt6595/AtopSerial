--[[
 *   CheckSum_8        求和校验（低8位）
 *   CheckSum_16       求和校验（低16位）
 *   LRC               纵向冗余校验（累加取补码）
 *   CRC16_IBM         (poly=0x8005, init=0x0000, refin/refout=true,  xorout=0x0000  check=0xBB3D)
 *   CRC16_MAXIM       (poly=0x8005, init=0x0000, refin/refout=true,  xorout=0xFFFF  check=0x44C2)
 *   CRC16_USB         (poly=0x8005, init=0xFFFF, refin/refout=true,  xorout=0xFFFF  check=0xB4C8)
 *   CRC16_MODBUS      (poly=0x8005, init=0xFFFF, refin/refout=true,  xorout=0x0000  check=0x4B37)
 *   CRC16_CCITT       (poly=0x1021, init=0x0000, refin/refout=true,  xorout=0x0000  check=0x2189, 即 KERMIT)
 *   CRC16_CCITT_FALSE (poly=0x1021, init=0xFFFF, refin/refout=false, xorout=0x0000 check=0x29B1)
 *   CRC16_X25         (poly=0x1021, init=0xFFFF, refin/refout=true,  xorout=0xFFFF  check=0x906E)
 *   CRC16_XMODEM      (poly=0x1021, init=0x0000, refin/refout=false, xorout=0x0000  check=0x31C3)
 *   CRC16_DNP         (poly=0x3D65, init=0x0000, refin/refout=true,  xorout=0xFFFF  check=0xEA82)
 *   CRC32             (poly=0x04C11DB7, init=0xFFFFFFFF, refin/refout=true,  xorout=0xFFFFFFFF check=0xCBF43926)
 *   CRC32_MPEG2       (poly=0x04C11DB7, init=0xFFFFFFFF, refin/refout=false, xorout=0x00000000 check=0x0376E6E7)
--]]

local crc = {}

-- ============================================================
-- 位运算兼容层：Lua 5.3+ 原生运算符 / LuaJIT·NodeMCU bit 库
-- ============================================================
local bxor, band, bor, lshift, rshift
bxor    = function(a, b) return a ~ b end
band    = function(a, b) return a & b end
bor     = function(a, b) return a | b end
lshift  = function(a, n) return a << n end
rshift  = function(a, n) return a >> n end

-- ============================================================
-- 内部工具
-- ============================================================

-- 位反转：把 v 的低 bits 位反转
local function reflect(v, bits)
    local r = 0
    for _ = 1, bits do
        r = bor(lshift(r, 1), band(v, 1))
        v = rshift(v, 1)
    end
    return r
end

-- 生成 CRC16 算法（表驱动）
-- poly/init/xorout 为 16 位值；refin/refout 为是否按字节/结果反射
local function new_crc16(poly, init, refin, refout, xorout)
    local rpoly = refin and reflect(poly, 16) or poly
    local tbl = {}
    for i = 0, 255 do
        local c = refin and i or lshift(i, 8)
        for _ = 1, 8 do
            if refin then
                c = band(c, 1) ~= 0 and bxor(rshift(c, 1), rpoly) or rshift(c, 1)
            else
                if band(c, 0x8000) ~= 0 then
                    c = band(bxor(lshift(c, 1), rpoly), 0xFFFF)
                else
                    c = band(lshift(c, 1), 0xFFFF)
                end
            end
        end
        tbl[i] = band(c, 0xFFFF)
    end

    return function(data)
        local c = init
        if refin then
            for i = 1, #data do
                c = bxor(rshift(c, 8), tbl[band(bxor(c, data:byte(i)), 0xFF)])
            end
        else
            for i = 1, #data do
                c = band(bxor(lshift(c, 8), tbl[band(bxor(rshift(c, 8), data:byte(i)), 0xFF)]), 0xFFFF)
            end
        end
        c = band(c, 0xFFFF)
        -- 反射表实现（refin=true）算出的结果已隐含 refout，不能再反射
        if refout and not refin then c = reflect(c, 16) end
        return band(bxor(c, xorout), 0xFFFF)
    end
end

-- 生成 CRC32 算法（表驱动）
local function new_crc32(poly, init, refin, refout, xorout)
    local rpoly = refin and reflect(poly, 32) or poly
    local tbl = {}
    for i = 0, 255 do
        local c
        if refin then
            c = i
            for _ = 1, 8 do
                if band(c, 1) ~= 0 then
                    c = bxor(rshift(c, 1), rpoly)
                else
                    c = rshift(c, 1)
                end
            end
        else
            c = lshift(i, 24)
            for _ = 1, 8 do
                if band(c, 0x80000000) ~= 0 then
                    c = band(bxor(lshift(c, 1), rpoly), 0xFFFFFFFF)
                else
                    c = band(lshift(c, 1), 0xFFFFFFFF)
                end
            end
        end
        tbl[i] = band(c, 0xFFFFFFFF)
    end

    return function(data)
        local c = init
        if refin then
            for i = 1, #data do
                c = bxor(rshift(c, 8), tbl[band(bxor(c, data:byte(i)), 0xFF)])
            end
        else
            for i = 1, #data do
                c = band(bxor(lshift(c, 8), tbl[band(bxor(rshift(c, 24), data:byte(i)), 0xFF)]), 0xFFFFFFFF)
            end
        end
        c = band(c, 0xFFFFFFFF)
        -- 反射实现（refin=true）已隐含 refout，不能再反射
        if refout and not refin then c = reflect(c, 32) end
        return band(bxor(c, xorout), 0xFFFFFFFF)
    end
end

-- ============================================================
-- 求和类校验
-- ============================================================

-- CheckSum_8：所有字节相加，取低 8 位
function crc.CheckSum_8(data)
    local s = 0
    for i = 1, #data do
        s = band(s + data:byte(i), 0xFF)
    end
    return s
end

-- CheckSum_16：所有字节相加，取低 16 位
function crc.CheckSum_16(data)
    local s = 0
    for i = 1, #data do
        s = band(s + data:byte(i), 0xFFFF)
    end
    return s
end

-- LRC：所有字节相加，取相反数的补码（低 8 位）
-- 例：Modbus ASCII 帧的 LRC
function crc.LRC(data)
    local s = 0
    for i = 1, #data do
        s = s + data:byte(i)
    end
    return band(0 - s, 0xFF)
end

-- ============================================================
-- CRC16 系列
-- ============================================================
crc.CRC16_IBM        = new_crc16(0x8005, 0x0000, true,  true,  0x0000)
crc.CRC16_MAXIM      = new_crc16(0x8005, 0x0000, true,  true,  0xFFFF)
crc.CRC16_USB        = new_crc16(0x8005, 0xFFFF, true,  true,  0xFFFF)
crc.CRC16_MODBUS     = new_crc16(0x8005, 0xFFFF, true,  true,  0x0000)
crc.CRC16_CCITT      = new_crc16(0x1021, 0x0000, true,  true,  0x0000)  -- 即 CRC-16/KERMIT
crc.CRC16_CCITT_FALSE= new_crc16(0x1021, 0xFFFF, false, false, 0x0000)
crc.CRC16_X25        = new_crc16(0x1021, 0xFFFF, true,  true,  0xFFFF)
crc.CRC16_XMODEM     = new_crc16(0x1021, 0x0000, false, false, 0x0000)
crc.CRC16_DNP        = new_crc16(0x3D65, 0x0000, true,  true,  0xFFFF)

-- ============================================================
-- CRC32 系列
-- ============================================================
crc.CRC32            = new_crc32(0x04C11DB7, 0xFFFFFFFF, true,  true,  0xFFFFFFFF)  -- 即标准 CRC-32(zlib)
crc.CRC32_MPEG2      = new_crc32(0x04C11DB7, 0xFFFFFFFF, false, false, 0x00000000)

-- ============================================================
-- 便捷工具
-- ============================================================

-- 常见协议对每种校验值的传输字节序惯例（仅供对接时参考，最终以设备协议文档为准）
crc.ENDIAN_CONVENTION = {
    CheckSum_8        = "big",    -- 单字节无需区分
    CheckSum_16       = "big",    -- 无统一惯例，随协议
    LRC               = "big",
    CRC16_IBM         = "big",    -- IBM 3708 等
    CRC16_MAXIM       = "little", -- MAXIM 系（如 DS18B20 族协议）
    CRC16_USB         = "little", -- USB 链路层 LSB 先传
    CRC16_MODBUS      = "little", -- Modbus RTU：低字节在前!
    CRC16_CCITT       = "little", -- Kermit：低字节在前
    CRC16_CCITT_FALSE = "big",    -- BISYNC
    CRC16_X25         = "little", -- X.25/LAPB 系
    CRC16_XMODEM      = "big",    -- XMODEM/YMODEM：高字节在前
    CRC16_DNP         = "little", -- DNP3：低字节在前
    CRC32             = "big",    -- PNG/zlib 存储大端
    CRC32_MPEG2       = "big",
}

-- 每种算法校验值的字节宽度（大端拼接用）
crc.ENCODE_WIDTH = {
    CheckSum_8        = 1,
    CheckSum_16       = 2,
    LRC               = 1,
    CRC16_IBM         = 2,
    CRC16_MAXIM       = 2,
    CRC16_USB         = 2,
    CRC16_MODBUS      = 2,
    CRC16_CCITT       = 2,
    CRC16_CCITT_FALSE = 2,
    CRC16_X25         = 2,
    CRC16_XMODEM      = 2,
    CRC16_DNP         = 2,
    CRC32             = 4,
    CRC32_MPEG2       = 4,
}

crc.ALGORITHMS = {}
for name in pairs(crc.ENCODE_WIDTH) do crc.ALGORITHMS[#crc.ALGORITHMS + 1] = name end
table.sort(crc.ALGORITHMS)

-- 把校验值拼到数据尾部，返回 数据..校验字节
-- endian: "big"(默认, 高字节在前) / "little"(低字节在前)
function crc.append(data, algo, endian)
    local fn = crc[algo]
    local n = crc.ENCODE_WIDTH[algo]
    assert(fn and n, "未知校验算法: " .. tostring(algo))
    assert(endian == nil or endian == "big" or endian == "little",
           "endian 只能是 \"big\" 或 \"little\"")
    endian = endian or "big"
    local v = fn(data)
    local bytes = {}
    if endian == "little" then
        for i = 0, n - 1 do
            bytes[#bytes + 1] = string.char(band(rshift(v, i * 8), 0xFF))
        end
    else
        for i = n - 1, 0, -1 do
            bytes[#bytes + 1] = string.char(band(rshift(v, i * 8), 0xFF))
        end
    end
    return data .. table.concat(bytes)
end

-- 验证一个"数据+校验"的包（校验字节序须与 append 时一致）
-- endian: "big"(默认) / "little"
function crc.verify(packet, algo, endian)
    local fn = crc[algo]
    local n = crc.ENCODE_WIDTH[algo]
    if not fn or not n then error("未知校验算法: " .. tostring(algo), 2) end
    if #packet < n then return false end
    assert(endian == nil or endian == "big" or endian == "little",
           "endian 只能是 \"big\" 或 \"little\"")
    endian = endian or "big"
    local payload = packet:sub(1, #packet - n)
    local v = fn(payload)
    for i = 1, n do
        local b = packet:byte(#packet - n + i)
        local shift = endian == "little" and (i - 1) * 8 or (n - i) * 8
        local expect = band(rshift(v, shift), 0xFF)
        if b ~= expect then return false end
    end
    return true
end

-- 把校验值格式化为定长小写 HEX 字符串，digits 为位数（如 8 位显示 CRC32）
function crc.hex(v, digits)
    return string.format("%0" .. (digits or 8) .. "X", v)
end

-- 把校验值格式化为定长大写 HEX 字符串，digits 为位数（如 8 位显示 CRC32）
function crc.Hex(v, digits)
    return string.format("%0" .. (digits or 8) .. "X", v)
end

--return uartData
--return crc.append(uartData, "CheckSum_8", crc.ENDIAN_CONVENTION.CheckSum_8)
--return crc.append(uartData, "CheckSum_16", crc.ENDIAN_CONVENTION.CheckSum_16)
--return crc.append(uartData, "LRC", crc.ENDIAN_CONVENTION.LRC)
--return crc.append(uartData, "CRC16_IBM", crc.ENDIAN_CONVENTION.CRC16_IBM)
--return crc.append(uartData, "CRC16_MAXIM", crc.ENDIAN_CONVENTION.CRC16_MAXIM)
--return crc.append(uartData, "CRC16_USB", crc.ENDIAN_CONVENTION.CRC16_USB)
return crc.append(uartData, "CRC16_MODBUS", crc.ENDIAN_CONVENTION.CRC16_MODBUS)
--return crc.append(uartData, "CRC16_CCITT", crc.ENDIAN_CONVENTION.CRC16_CCITT)
--return crc.append(uartData, "CRC16_CCITT_FALSE", crc.ENDIAN_CONVENTION.CRC16_CCITT_FALSE)
--return crc.append(uartData, "CRC16_X25", crc.ENDIAN_CONVENTION.CRC16_X25)
--return crc.append(uartData, "CRC16_XMODEM", crc.ENDIAN_CONVENTION.CRC16_XMODEM)
--return crc.append(uartData, "CRC16_DNP", crc.ENDIAN_CONVENTION.CRC16_DNP)
--return crc.append(uartData, "CRC32", crc.ENDIAN_CONVENTION.CRC32)
--return crc.append(uartData, "CRC32_MPEG2", crc.ENDIAN_CONVENTION.CRC32_MPEG2)

--Atomiz_zhang
--No more
