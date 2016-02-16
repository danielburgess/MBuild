using System;
using System.IO;

class Marvelous
{
    public static void DumpIndexedFiles(string inputrom, string addrtype, string outtype, string outfolder)
    {
        byte[] romfile = File.ReadAllBytes(inputrom);

        //the locations for the pointers... and the total number of pointers
        const int bankloc = 0x11E3;
        const int highloc = 0x12DC;
        const int lowloc = 0x13D5;
        const int bounds = 0xF9;

        byte[] bank = new byte[bounds];
        for (int c = 0; c < bounds; c++)
        {
            bank[c] = romfile[bankloc + c];
        }
        byte[] high = new byte[bounds];
        for (int c = 0; c < bounds; c++)
        {
            high[c] = romfile[highloc + c];
        }
        byte[] low = new byte[bounds];
        for (int c = 0; c < bounds; c++)
        {
            low[c] = romfile[lowloc + c];
        }

        for (int c = 0; c < bank.Length; c++)
        {
            byte bnk = Convert.ToByte(Convert.ToInt32(bank[c]) - 192);      // Converts HIROM to PC
            string loc = bnk.ToString("X2") + high[c].ToString("X2") + low[c].ToString("X2");
            int iloc = Convert.ToInt32(loc, 16);
            byte[] d1 = LCompress.Decompress(romfile, iloc, 0x10000, 1);
            byte[] d2 = new byte[LCompress.LastDecompressedSize];
            for (int b = 0; b < LCompress.LastDecompressedSize; b++)
            {
                d2[b] = d1[b];
            }
            File.WriteAllBytes(outfolder + @"\0x" + loc + ".bin", d2);
        }
    }

}

