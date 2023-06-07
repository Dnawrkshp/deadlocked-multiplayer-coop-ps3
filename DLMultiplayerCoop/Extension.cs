using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DLMultiplayerCoop
{
    public class Extension //From PS3Lib (by iMCSx)
    {
        /// <summary>Read a signed byte.</summary>
        public static sbyte ReadSByte(ulong offset)
        {
            byte[] buffer = new byte[1];
            GetMem(offset, buffer);
            return (sbyte)buffer[0];
        }

        /// <summary>Read a byte a check if his value. This return a bool according the byte detected.</summary>
        public static bool ReadBool(ulong offset)
        {
            byte[] buffer = new byte[1];
            GetMem(offset, buffer);
            return buffer[0] != 0;
        }

        /// <summary>Read and return an integer 16 bits.</summary>
        public static short ReadInt16(ulong offset)
        {
            byte[] buffer = GetBytes(offset, 2);
            buffer = misc.revif(buffer);
            return BitConverter.ToInt16(buffer, 0);
        }

        /// <summary>Read and return an integer 32 bits.</summary>
        public static int ReadInt32(ulong offset)
        {
            byte[] buffer = GetBytes(offset, 4);
            buffer = misc.revif(buffer);
            return BitConverter.ToInt32(buffer, 0);
        }

        /// <summary>Read and return an integer 64 bits.</summary>
        public static long ReadInt64(ulong offset)
        {
            byte[] buffer = GetBytes(offset, 8);
            buffer = misc.revif(buffer);
            return BitConverter.ToInt64(buffer, 0);
        }

        /// <summary>Read and return a byte.</summary>
        public static byte ReadByte(ulong offset)
        {
            byte[] buffer = GetBytes(offset, 1);
            return buffer[0];
        }

        /// <summary>Read a string with a length to the first byte equal to an value null (0x00).</summary>
        public static byte[] ReadBytes(ulong offset, int length)
        {
            byte[] buffer = GetBytes(offset, (uint)length);
            return buffer;
        }

        /// <summary>Read and return an unsigned integer 16 bits.</summary>
        public static ushort ReadUInt16(ulong offset)
        {
            byte[] buffer = GetBytes(offset, 2);
            buffer = misc.revif(buffer);
            return BitConverter.ToUInt16(buffer, 0);
        }

        /// <summary>Read and return an unsigned integer 32 bits.</summary>
        public static uint ReadUInt32(ulong offset)
        {
            byte[] buffer = GetBytes(offset, 4);
            buffer = misc.revif(buffer);
            return BitConverter.ToUInt32(buffer, 0);
        }

        /// <summary>Read and return an unsigned integer 64 bits.</summary>
        public static ulong ReadUInt64(ulong offset)
        {
            byte[] buffer = GetBytes(offset, 8);
            buffer = misc.revif(buffer);
            return BitConverter.ToUInt64(buffer, 0);
        }

        /// <summary>Read and return a Float.</summary>
        public static float ReadFloat(ulong offset)
        {
            byte[] buffer = GetBytes(offset, 4);
            buffer = misc.revif(buffer);
            return BitConverter.ToSingle(buffer, 0);
        }

        /// <summary>Read a string very fast and stop only when a byte null is detected (0x00).</summary>
        public static string ReadString(ulong offset)
        {
            int block = 40;
            int addOffset = 0;
            string str = "";
        repeat:
            byte[] buffer = ReadBytes(offset + (uint)addOffset, block);
            buffer = misc.notrevif(buffer);
            str += Encoding.UTF8.GetString(buffer);
            addOffset += block;
            if (str.Contains('\0'))
            {
                int index = str.IndexOf('\0');
                string final = str.Substring(0, index);
                str = String.Empty;
                return final;
            }
            else
                goto repeat;
        }

        /// <summary>Write a signed byte.</summary>
        public static void WriteSByte(ulong offset, sbyte input)
        {
            byte[] buff = new byte[1];
            buff[0] = (byte)input;
            SetMem(offset, buff);
        }

        /// <summary>Write a boolean.</summary>
        public static void WriteBool(ulong offset, bool input)
        {
            byte[] buff = new byte[1];
            buff[0] = input ? (byte)1 : (byte)0;
            SetMem(offset, buff);
        }

        /// <summary>Write an interger 16 bits.</summary>
        public static void WriteInt16(ulong offset, short input)
        {
            byte[] buff = new byte[2];
            misc.revif(BitConverter.GetBytes(input)).CopyTo(buff, 0);
            SetMem(offset, buff);
        }

        /// <summary>Write an integer 32 bits.</summary>
        public static void WriteInt32(ulong offset, int input)
        {
            byte[] buff = new byte[4];
            misc.revif(BitConverter.GetBytes(input)).CopyTo(buff, 0);

            SetMem(offset, buff);
        }

        /// <summary>Write an integer 64 bits.</summary>
        public static void WriteInt64(ulong offset, long input)
        {
            byte[] buff = new byte[8];
            misc.revif(BitConverter.GetBytes(input)).CopyTo(buff, 0);
            SetMem(offset, buff);
        }

        /// <summary>Write a byte.</summary>
        public static void WriteByte(ulong offset, byte input)
        {
            byte[] buff = new byte[1];
            buff[0] = input;
            SetMem(offset, buff);
        }

        /// <summary>Write a byte array.</summary>
        public static void WriteBytes(ulong offset, byte[] input)
        {
            byte[] buff = input;
            SetMem(offset, buff);
        }

        /// <summary>Write a string.</summary>
        public static void WriteString(ulong offset, string input)
        {
            byte[] buff = Encoding.UTF8.GetBytes(input);
            Array.Resize(ref buff, buff.Length + 1);
            SetMem(offset, buff);
        }

        /// <summary>Write an unsigned integer 16 bits.</summary>
        public static void WriteUInt16(ulong offset, ushort input)
        {
            byte[] buff = new byte[2];
            BitConverter.GetBytes(input).CopyTo(buff, 0);
            buff = misc.revif(buff);
            SetMem(offset, buff);
        }

        /// <summary>Write an unsigned integer 32 bits.</summary>
        public static void WriteUInt32(ulong offset, uint input)
        {
            byte[] buff = new byte[4];
            misc.revif(BitConverter.GetBytes(input)).CopyTo(buff, 0);
            SetMem(offset, buff);
        }

        /// <summary>Write an unsigned integer 64 bits.</summary>
        public static void WriteUInt64(ulong offset, ulong input)
        {
            byte[] buff = new byte[8];
            misc.revif(BitConverter.GetBytes(input)).CopyTo(buff, 0);
            SetMem(offset, buff);
        }

        /// <summary>Write a Float.</summary>
        public static void WriteFloat(ulong offset, float input)
        {
            byte[] buff = new byte[4];
            misc.revif(BitConverter.GetBytes(input)).CopyTo(buff, 0);
            SetMem(offset, buff);
        }

        private static void SetMem(ulong Address, byte[] buffer)
        {
            SetBytes(Address, buffer);
        }

        private static void GetMem(ulong offset, byte[] buffer)
        {
            GetBytes(offset, ref buffer);
        }

        private static byte[] GetBytes(ulong offset, uint length)
        {
            byte[] buffer = new byte[length];
            GetBytes(offset, ref buffer);
            return buffer;
        }

        private static void GetBytes(ulong offset, ref byte[] buffer)
        {
            PS3TMAPI.ProcessGetMemory(0, PS3TMAPI.UnitType.PPU, Form1.ProcessID, 0, offset, ref buffer);
        }

        private static void SetBytes(ulong offset, byte[] buffer)
        {
            PS3TMAPI.SNRESULT snr = PS3TMAPI.ProcessSetMemory(0, PS3TMAPI.UnitType.PPU, Form1.ProcessID, 0, offset, buffer);
        }
    }

    public class misc
    {

        public static byte[] revif(byte[] b)
        {
            Array.Reverse(b);
            return b;
        }

        public static byte[] notrevif(byte[] b)
        {
            //Array.Reverse(b);
            return b;
        }

    }
}
