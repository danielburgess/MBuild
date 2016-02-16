using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Reflection;
using MBuild.Properties;

//modified Class taken from LazyShell

class LCompress
{
    // external functions...
    [DllImport("Lunar Compress.dll")]
    static extern int LunarOpenRAMFile([MarshalAs(UnmanagedType.LPArray)] byte[] data, int fileMode, int size);
    [DllImport("Lunar Compress.dll")]
    static extern int LunarDecompress([MarshalAs(UnmanagedType.LPArray)] byte[] destination, int addressToStart, int maxDataSize, int format1, int format2, int DoNotUseThisYet); // int * to save end addr for calculating size
    [DllImport("Lunar Compress.dll")]
    static extern int LunarSaveRAMFile(string fileName);
    [DllImport("Lunar Compress.dll")]
    static extern int LunarRecompress([MarshalAs(UnmanagedType.LPArray)] byte[] source, [MarshalAs(UnmanagedType.LPArray)] byte[] destination, uint dataSize, uint maxDataSize, uint format, uint format2);
    // compression functions
    public static int Compress(byte[] src, out byte[] dest, uint type)
    {
        Console.WriteLine("Compressing " + src.Length.ToString() + " bytes...");
        byte[] temp = new byte[src.Length];
        dest = new byte[src.Length];
        if (!LunarCompressExists())
            return -1;
        int size = LunarRecompress(src, temp, (uint)src.Length, 0x10000, type, 0);
        dest = new byte[size];
        CopyBytes(temp, dest, false);
        Console.WriteLine("Data reduced to " + size.ToString() + " bytes.");
        return size;

    }
    public static void CopyBytes(byte[] from, byte[] to, bool fromlen)
    {
        int len = (fromlen) ? from.Length : to.Length;

        Array.Copy(from, to, len);
        //Works fine, just cleaning up
        //for (int c = 0; c < len; c++)
        //{
        //    to[c] = from[c];
        //}
    }


    private static int lcs = 0;

    public static int LastDecompressedSize
    {
        get
        {
            return lcs;
        }
        set
        {
            lcs = value;
        }
    }

    public static byte[] Decompress(byte[] data, int offset, int maxSize, uint type)
    {
        int ty = (int)type;
        if (!LunarCompressExists())
            return null;
        //
        byte[] src = new byte[maxSize];
        byte[] dst = new byte[maxSize];
        for (int i = 0; ((i < src.Length) && ((offset + i) < data.Length)); i++)
            src[i] = data[offset + i]; // Copy over all the source data
        if (LunarOpenRAMFile(src, 0, src.Length) == 0) // Load source data as RAMFile
            return null;
        LastDecompressedSize = LunarDecompress(dst, 0, dst.Length, ty, 0, 0);
        if (LastDecompressedSize != 0)
            return dst;
        return null;
    }
    public static int Decompress(byte[] data, byte[] dst, int offset, int maxSize)
    {
        if (!LunarCompressExists())
            return 0;
        //
        byte[] src = new byte[maxSize];
        for (int i = 0; ((i < src.Length) && ((offset + i) < data.Length)); i++)
            src[i] = data[offset + i]; // Copy over all the source data
        if (LunarOpenRAMFile(src, 0, src.Length) == 0) // Load source data as RAMFile
            return 0;
        LastDecompressedSize = LunarDecompress(dst, 0, dst.Length, 1, 0, 0);
        return LastDecompressedSize;
    }
    // accessor functions
    public static bool LunarCompressExists()
    {
        bool retval = false;
        if (!File.Exists(Path.GetDirectoryName(Application.ExecutablePath) + '\\' + "Lunar Compress.dll"))
        {
            try
            {
                Assembly a = Assembly.GetExecutingAssembly();
                byte[] lc = MBuild.Properties.Resources.Lunar_Compress;
                File.WriteAllBytes(Path.GetDirectoryName(Application.ExecutablePath) + '\\' + "Lunar Compress.dll", lc);
                retval = true;
            }
            catch (Exception ex) { Console.WriteLine("Unable to extract Lunar Compress.dll!\r\n" + ex.Message); }
        }
        else
        {
            retval = true;
        }
        return retval;
    }

    public static bool LunarCompressCleanup()
    {
        bool retval = false;
        if (File.Exists(Path.GetDirectoryName(Application.ExecutablePath) + '\\' + "Lunar Compress.dll"))
        {
            try
            {
                File.Delete(Path.GetDirectoryName(Application.ExecutablePath) + '\\' + "Lunar Compress.dll");
                retval = true;
            }
            catch { if(Settings.Default.ShowCleanupWarnings) Console.WriteLine("Unable to clean up Lunar Compress.dll!"); }
        }
        else
        {
            retval = true;
        }
        return retval;
    }

    public static uint GetLZType(string s)
    {
        uint lztype = 1;
        if (!(s == null))
        {
            switch (s)
            {
                case "0":
                    lztype = 0;
                    break;
                case "1":
                    lztype = 1;
                    break;
                case "2":
                    lztype = 2;
                    break;
                case "3":
                    lztype = 3;
                    break;
                case "4":
                    lztype = 4;
                    break;
                case "5":
                    lztype = 5;
                    break;
                case "6":
                    lztype = 6;
                    break;
                case "7":
                    lztype = 7;
                    break;
                case "8":
                    lztype = 8;
                    break;
                case "9":
                    lztype = 9;
                    break;
                case "10":
                    lztype = 10;
                    break;
                case "11":
                    lztype = 11;
                    break;
                case "12":
                    lztype = 12;
                    break;
                case "13":
                    lztype = 13;
                    break;
                case "14":
                    lztype = 14;
                    break;
                case "15":
                    lztype = 15;
                    break;
                case "16":
                    lztype = 16;
                    break;
                case "17":
                    lztype = 17;
                    break;
                case "18":
                    lztype = 18;
                    break;
                case "100":
                    lztype = 100;
                    break;
                case "101":
                    lztype = 101;
                    break;
                case "102":
                    lztype = 102;
                    break;
                case "103":
                    lztype = 103;
                    break;
                default:
                    lztype = 1;
                    break;
            }
        }
        return lztype;
    }

}
