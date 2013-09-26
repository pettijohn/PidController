//Via http://forums.netduino.com/index.php?/topic/215-ds1307-real-time-clock/
using System;


public static class ByteExtensionMethods
{
    /// <summary>
    /// Convert byte data from BCD - Binary Coded Decimal - to binary.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static byte FromBCD(this byte value)
    {
        var lo = value & 0x0f;
        var hi = (value & 0xf0) >> 4;
        var retValue = (byte)(hi * 10 + lo);
        return retValue;
    }

    /// <summary>
    /// Convert byte data from binary to BCD - Binary Coded Decimal
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static byte ToBCD(this byte value)
    {
        return (byte)((value / 10 << 4) + value % 10);
    }
}

public static class TimespanExtensionMethods
{
    /// <summary>
    /// Compute the total number of milliseconds represented by a TimeSpan.
    /// </summary>
    /// <param name="span"></param>
    /// <returns></returns>
    public static double TotalMilliseconds(this TimeSpan span)
    {
        double num = span.Ticks / TimeSpan.TicksPerMillisecond;

        if (num > double.MaxValue)
            return double.MaxValue;

        if (num < double.MinValue)
            return double.MinValue;
        
        return num;
    }
}