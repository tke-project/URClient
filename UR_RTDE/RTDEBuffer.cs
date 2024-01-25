
namespace UR_RTDE
{
    public class RTDEBuffer
    {
        public static int ByteSize(string rtdeDataType)
        {
            switch (rtdeDataType)
            {
                case "NOT_FOUND":
                case "IN_USE":
                    return 0;

                case "BOOL":
                case "UINT8":
                    return 1;

                case "UINT32":
                    return 4;

                case "UINT64":
                    return 8;

                case "INT32":
                    return 4;

                case "DOUBLE":
                    return 8;

                case "VECTOR3D":
                    return 24;

                case "VECTOR6D":
                    return 48;

                case "VECTOR6INT32":
                    return 24;

                case "VECTOR6UINT32":
                    return 24;

                default:
                    return 0;
            }
        }

        public static object? Decode(string rtdeDataType, byte[] buf)
        {

            buf = buf.Reverse().ToArray();
            switch (rtdeDataType)
            {
                case "NOT_FOUND":
                case "IN_USE":
                    return null;

                case "BOOL":
                case "UINT8":
                    return buf[0];

                case "UINT32":
                    return BitConverter.ToUInt32(buf, 0);

                case "UINT64":
                    return BitConverter.ToUInt64(buf, 0);

                case "INT32":
                    return BitConverter.ToUInt32(buf, 0);

                case "DOUBLE":
                    return BitConverter.ToDouble(buf, 0);

                case "VECTOR3D":
                    double[] values =
                    [
                        BitConverter.ToDouble(buf, 0),
                        BitConverter.ToDouble(buf, 8),
                        BitConverter.ToDouble(buf, 16),
                    ];
                    return values;

                case "VECTOR6D":
                    values =
                   [
                       BitConverter.ToDouble(buf, 0),
                       BitConverter.ToDouble(buf, 8),
                       BitConverter.ToDouble(buf, 16),
                       BitConverter.ToDouble(buf, 24),
                       BitConverter.ToDouble(buf, 32),
                       BitConverter.ToDouble(buf, 40),
                   ];
                    return values;

                case "VECTOR6INT32":
                    values =
                    [
                        BitConverter.ToInt32(buf, 0),
                        BitConverter.ToInt32(buf, 4),
                        BitConverter.ToInt32(buf, 8),
                        BitConverter.ToInt32(buf, 12),
                        BitConverter.ToInt32(buf, 16),
                        BitConverter.ToInt32(buf, 20),
                    ];
                    return values;

                case "VECTOR6UINT32":
                    values =
                    [
                        BitConverter.ToUInt32(buf, 0),
                        BitConverter.ToUInt32(buf, 4),
                        BitConverter.ToUInt32(buf, 8),
                        BitConverter.ToUInt32(buf, 12),
                        BitConverter.ToUInt32(buf, 16),
                        BitConverter.ToUInt32(buf, 20),
                    ];
                    return values;

                default:
                    return null;
            }
        }

        public static byte[]? Encode(string rtdeDataType, object value)
        {
            switch (rtdeDataType)
            {
                case "NOT_FOUND":
                case "IN_USE":
                    return null;

                case "BOOL":
                case "UINT8":
                    byte[] buf = [(byte)value];
                    return buf;

                case "UINT32":
                    buf = BitConverter.GetBytes((uint)value).Reverse().ToArray();
                    return buf;

                case "UINT64":
                    buf = BitConverter.GetBytes((ulong)value).Reverse().ToArray();
                    return buf;

                case "INT32":
                    buf = BitConverter.GetBytes((int)value).Reverse().ToArray();
                    return buf;

                case "DOUBLE":
                    buf = BitConverter.GetBytes((double)value).Reverse().ToArray();
                    return buf;

                case "VECTOR3D":
                case "VECTOR6D":
                    double[] doubleArr = (double[])value;
                    buf = doubleArr.SelectMany(x => BitConverter.GetBytes(x).Reverse().ToArray()).ToArray();
                    return buf;

                case "VECTOR6INT32":
                    int[] intArr = (int[])value;
                    buf = intArr.SelectMany(x => BitConverter.GetBytes(x).Reverse().ToArray()).ToArray();
                    return buf;

                case "VECTOR6UINT32":
                    uint[] uintArr = (uint[])value;
                    buf = uintArr.SelectMany(x => BitConverter.GetBytes(x).Reverse().ToArray()).ToArray();
                    return buf;

                default:
                    return null;

            }

        }
    }
}
