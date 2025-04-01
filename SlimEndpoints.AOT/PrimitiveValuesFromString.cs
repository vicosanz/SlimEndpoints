using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimEndpoints.AOT;

public static class PrimitiveValuesFromString
{
    public delegate bool TryParseFromStringDelegate<T>(string value, out T result);
    public delegate T ParseFromStringDelegate<T>(string value);

    private static T? InternalParseFromString<T>(TryParseFromStringDelegate<T> tryParse, string value) =>
        !tryParse(value, out T result) ? default : result;

    private static object InternalParseFromStringToObject<T>(string value)
    {
        if (typeof(T) == typeof(Guid))
            return InternalParseFromString<Guid>(Guid.TryParse, value);

        if (typeof(T) == typeof(DateTimeOffset))
            return InternalParseFromString<DateTimeOffset>(DateTimeOffset.TryParse, value);

        if (typeof(T) == typeof(TimeSpan))
            return InternalParseFromString<TimeSpan>(TimeSpan.TryParse, value);

        return Type.GetTypeCode(typeof(T)) switch
        {
            TypeCode.Boolean => InternalParseFromString<bool>(bool.TryParse, value),
            TypeCode.Char => InternalParseFromString<char>(char.TryParse, value),
            TypeCode.SByte => InternalParseFromString<sbyte>(sbyte.TryParse, value),
            TypeCode.Byte => InternalParseFromString<byte>(byte.TryParse, value),
            TypeCode.Int16 => InternalParseFromString<short>(short.TryParse, value),
            TypeCode.Int32 => InternalParseFromString<int>(int.TryParse, value),
            TypeCode.UInt16 => InternalParseFromString<ushort>(ushort.TryParse, value),
            TypeCode.UInt32 => InternalParseFromString<uint>(uint.TryParse, value),
            TypeCode.Int64 => InternalParseFromString<long>(long.TryParse, value),
            TypeCode.UInt64 => InternalParseFromString<ulong>(ulong.TryParse, value),
            TypeCode.Single => InternalParseFromString<float>(float.TryParse, value),
            TypeCode.Double => InternalParseFromString<double>(double.TryParse, value),
            TypeCode.Decimal => InternalParseFromString<decimal>(decimal.TryParse, value),
            TypeCode.DateTime => InternalParseFromString<DateTime>(DateTime.TryParse, value),
            TypeCode.String => value,
            _ => throw new InvalidCastException()
        };
    }

    public static T? ParseFromString<T>(string value) => (T?)InternalParseFromStringToObject<T>(value);
}
