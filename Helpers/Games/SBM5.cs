using System;
using System.Collections.Generic;
using System.IO;

class SBM5
{
    const uint buffersize = 0x10000;

    public static void DumpDataFromPointerTab(string romfile, string outfolder)
    {
        int offset = 0x05e234 - 8;
        int maxOffset = offset + 0x918; // 0x918 = 2328 dec
        byte[] mF = File.ReadAllBytes(romfile);
        List<string> outcsv = new List<string>();
        outcsv.Add("Pointer Offset;HIROM Offset;PC Offset;CType;Block Size;Description;");
        for (int c = offset; c < maxOffset; c += 8)
        {
            byte cType = 0;
            byte[] outSize = new byte[2];
            byte[] pData = new byte[3];

            byte[] oF = new byte[buffersize];
            byte[] outBuf = new byte[buffersize];

            cType = mF[c];
            Array.Copy(mF, c + 3, outSize, 0, 2);
            Array.Copy(mF, c + 5, pData, 0, 3);

            ushort DataLength = (ushort)((ushort)(outSize[1] << 8) | (ushort)(outSize[0]));

            uint HIROMOffset = (uint)pData[2] << 16;
            HIROMOffset = (uint)(HIROMOffset | (((uint)(pData[1])) << 8));
            HIROMOffset = (uint)(HIROMOffset | (uint)pData[0]);

            uint DataOffset = ConvPCHiROMBank((uint)pData[2]) << 16;
            DataOffset = (uint)(DataOffset | (((uint)(pData[1])) << 8));
            DataOffset = (uint)(DataOffset | (uint)pData[0]);
            string dt = "";
            if (DataOffset < mF.Length)
            {
                if ((cType > 0x01) && (cType < 0x06))
                {
                    Array.Copy(mF, DataOffset, oF, 0, buffersize);
                    BM5RLE.Decompress(oF, DataLength, "SBM5_" + cType.ToString("X").PadLeft(2, '0') + "_0x" + DataOffset.ToString("X") + ".bin");

                    //byte[] final = new byte[DataLength];
                    //Array.Copy(outBuf, 0, final, 0, DataLength);
                    //File.WriteAllBytes(, final);
                    dt = "X";
                }
                else
                {
                    //dump unknown data
                    byte[] final = new byte[DataLength];
                    Array.Copy(mF, DataOffset, final, 0, DataLength);
                    File.WriteAllBytes("SBM5_" + cType.ToString("X").PadLeft(2, '0') + "_0x" + DataOffset.ToString("X") + ".bin", final);
                    dt = "?";
                }
            }
            outcsv.Add(c.ToString("X") + ";" + HIROMOffset.ToString("X") + ";" + DataOffset.ToString("X") + ";" + cType.ToString("X").PadLeft(2, '0') + ";" + DataLength.ToString() + ";\"" + dt + "\";");
        }
        if (outcsv.Count > 1)
        {
            File.WriteAllLines("SBM5_PointerTableDocumentation.csv", outcsv);
        }
    }

    static uint ConvPCHiROMBank(uint bank)
    {
        uint final = 0;
        if (bank < 0x40)
        {
            final = bank;
        }
        else if (bank < 0x70)
        {
            final = bank - 0x40;
        }
        else if (bank < 0x80)
        {
            final = 0x70;//SRAM Address
        }
        else if (bank < 0xC0)
        {
            final = bank - 0x80;
        }
        else if (bank > 0xBF)
        {
            final = bank - 0xC0;
        }
        else
        {
            final = bank;
        }
        return final;
    }
}

