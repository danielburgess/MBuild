using System;
using System.IO;

//Used Rich Smith's (MyNameIsMeerkat) IPS Python library as source documentation to get me started
//Added RLE support as described here: http://www.zerosoft.zophar.net/ips.php

//Currently only supports IPS creation- 
//I probably won't bother adding the ability to apply IPS patches, even though it looks very simple.

class IPS
{
    //16MB Max size of an IPS file - 3byte int
    const int FILE_LIMIT = 0xFFFFFF;

    // Max size of an individual record - 2 byte int
    const int RECORD_LIMIT = 0xFFFF;

    //IPS file header 'PATCH'
    static byte[] PATCH_ASCII = new byte[] { 0x50, 0x41, 0x54, 0x43, 0x48 };//"PATCH";

    //IPS file footer 'EOF'
    static byte[] EOF_ASCII = new byte[] { 0x45, 0x4F, 0x46 };//"EOF";
    const int EOF_INTEGER = 4542278;

    public static void Create(string oFile, string cFile)
    {
        Create(oFile, cFile, "");
    }

    public static void Create(string oFile, string cFile, string outFile)
    {
        if (outFile.Length == 0)
        {
            outFile = Path.GetDirectoryName(cFile) + "\\" + Path.GetFileNameWithoutExtension(cFile) + ".ips";
        }
        //TODO: Make sure the files are not over the limit (16MB)
        byte[] oB = File.ReadAllBytes(oFile);
        byte[] cB = File.ReadAllBytes(cFile);
        byte[] tIPS = new byte[FILE_LIMIT];

        Array.Copy(PATCH_ASCII, 0, tIPS, 0, PATCH_ASCII.Length);
        //the offset of the ips file
        int iOffset = PATCH_ASCII.Length-1;


        //I suppose we assume that the changed file must be the same size or larger
        for (int c = 0; c < cB.Length; c++)
        {
            if (c >= oB.Length || !(oB[c].Equals(cB[c])))
            {
                byte[] record = new byte[RECORD_LIMIT];
                int o = 0;
                RLE rle = new RLE();
                for (o = 0; o < RECORD_LIMIT; o++)
                {
                    int p = (c + o);
                    if ((c == EOF_INTEGER) && o == 0)
                    {//A workaround because this position looks like EOF in ASCII
                        c--;
                        p = (c + o);
                        record[o] = cB[p];
                    }
                    else if (o < 5 || c >= oB.Length || !(oB[p].Equals(cB[p])) || ((o > 4) && Differences(oB, cB, p, 16)))
                    {
                        record[o] = cB[p];
                    }
                    else
                    {
                        break;
                    }
                }
                //check for RLE encode-able data
                rle = RLECheck(record, o);

                //then write the record
                if (o > 0)
                {
                    if ((rle.max) > 8)
                    {
                        if (rle.start > 0)
                        {//then write IPS and RLE record
                            int oriO = o;
                            o = rle.start - 1;
                            WriteIPS(tIPS, record, o, ref c, ref iOffset);
                            WriteRLE(tIPS, record, rle, ref c, ref iOffset);
                            //in case rle doesnt go to the end of the record...
                            o = oriO - (rle.start + rle.max) + 1;
                            if (o > 0)
                            {
                                WriteIPS(tIPS, record, o, ref c, ref iOffset);
                            }
                        }
                        else
                        {//only write RLE
                            WriteRLE(tIPS, record, rle, ref c, ref iOffset);
                        }
                    }
                    else
                    {
                        WriteIPS(tIPS, record, o, ref c, ref iOffset);
                    }
                }
            }
        }
        //Now write the EOF and finalize
        Array.Copy(EOF_ASCII, 0, tIPS, iOffset += 1, EOF_ASCII.Length);
        iOffset += EOF_ASCII.Length;

        byte[] final = new byte[iOffset];
        Array.Copy(tIPS, final, iOffset);
        File.WriteAllBytes(outFile, final);
    }

    static void WriteRLE(byte[] tIPS, byte[] record, RLE rle, ref int c, ref int iOffset)
    {

        //Write IPS Offset
        tIPS[iOffset += 1] = (byte)((c >> 16) & 0xFF);
        tIPS[iOffset += 1] = (byte)((c >> 8) & 0xFF);
        tIPS[iOffset += 1] = (byte)(c & 0xFF);

        //Write IPS Size of Record
        tIPS[iOffset += 1] = 0;
        tIPS[iOffset += 1] = 0;

        //Write RLE Size of Record
        tIPS[iOffset += 1] = (byte)((rle.max >> 8) & 0xFF);
        tIPS[iOffset += 1] = (byte)(rle.max & 0xFF);

        //Write Record...
        tIPS[iOffset += 1] = (byte)record[rle.start];
        c += rle.max;
        //return iOffset;
    }

    static void WriteIPS(byte[] tIPS, byte[] record, int o, ref int c, ref int iOffset)
    {
        //Write IPS Offset
        tIPS[iOffset += 1] = (byte)((c >> 16) & 0xFF);
        tIPS[iOffset += 1] = (byte)((c >> 8) & 0xFF);
        tIPS[iOffset += 1] = (byte)(c & 0xFF);

        //Write IPS Size of Record
        tIPS[iOffset += 1] = (byte)(o >> 8);
        tIPS[iOffset += 1] = (byte)(o & 0xFF);
        //Write Record...
        Array.Copy(record, 0, tIPS, iOffset + 1, o);
        iOffset += o;
        c += o;
        //return iOffset;
    }

    struct RLE
    {
        public int max; //length
        public int start; //record offset
    }

    static RLE RLECheck(byte[] record, int o)
    {
        RLE rle = new RLE();
        int count = 0;
        int start = 0;
        rle.start = 0;
        rle.max = 0;
        byte rcheck = record[0];
        for (int c = 1; c < o; c++)
        {
            if (rcheck.Equals(record[c]))
            {
                if (count == 0) start = c;
                count++;
                if (count > rle.max)
                {
                    rle.start = start;
                    rle.max = count;
                }
            }
            else
            {
                count = 0;
                rcheck = record[c];
            }
        }
        if (rle.max > 0) rle.max++; //because we need this not to be a zero-based number
        return rle;
    }

    static bool Differences(byte[] oB, byte[] cB, int index, int offset)
    {
        bool found = false;
        for (int c = 0; c < offset; c++)
        {
            if (!(oB[index + c].Equals(cB[index + c])))
            {
                found = true;
                break;
            }
        }
        return found;
    }


}

