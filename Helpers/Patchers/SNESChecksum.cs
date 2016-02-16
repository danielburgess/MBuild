using System;
using System.Diagnostics;
using System.IO;
using System.Text;

//Adapted from SNES9x 1.53 memmap.cpp & memmap.h
class SNESChecksum
{
    const int MAX_ROM_SIZE = 0x800000;
    const int ROM_NAME_LEN = 21;

    enum EXTFMT
    { NOPE, YEAH, BIGFIRST, SMALLFIRST };

    static EXTFMT ExtendedFormat = EXTFMT.NOPE;
    static byte[] ROM;
    static string ROMName;
    static byte[] RawROMName = new byte[ROM_NAME_LEN];
    static int CompanyId;
    static string ROMId;
    static byte ROMRegion;
    static byte ROMSpeed;
    static byte ROMType;
    static byte ROMSize;
    static uint ROMChecksum;
    static uint ROMComplementChecksum;
    //static uint ROMCRC32;

    static bool HiROM = false;
    static bool LoROM = false;
    static byte SRAMSize;
    //static uint SRAMMask;
    static uint CalculatedSize = 0;
    static uint CalculatedChecksum = 0;

    public static void FixROM(string filename)
    {
        ROM = File.ReadAllBytes(filename);
        if (FixROM())
        {
            try
            {
                File.WriteAllBytes(filename, ROM);
                Console.WriteLine("Checksum OK");
            }
            catch(Exception ex)
            {
                Console.WriteLine("Failed to Correct Checksum: \r\n" + ex.Message);
            }
        }
    }

    public static byte[] FixROM(byte[] indata)
    {
        ROM = new byte[indata.Length];
        Array.Copy(indata, ROM, indata.Length);
        if (FixROM())
        {
            return ROM;
        }
        else
        {
            return indata;
        }
    }

    //the mother method-- currently just used for correcting checksums... but could be used for a lot of other things
    private static bool FixROM()
    {
        //will be ignoring copier headers completely... cause DON'T USE THEM
        //ROM is loaded elsewhere
        int totalFileSize = ROM.Length;

        int hi_score = ScoreHiROM(false, 0);
        int lo_score = ScoreLoROM(false, 0);

        CalculatedSize = (uint)((totalFileSize / 0x2000) * 0x2000);

        if (CalculatedSize > 0x400000 &&
        (ROM[0x7fd5] + (ROM[0x7fd6] << 8)) != 0x4332 && // exclude S-DD1
        (ROM[0x7fd5] + (ROM[0x7fd6] << 8)) != 0x4532 &&
        (ROM[0xffd5] + (ROM[0xffd6] << 8)) != 0xF93a && // exclude SPC7110
        (ROM[0xffd5] + (ROM[0xffd6] << 8)) != 0xF53a)
            ExtendedFormat = EXTFMT.YEAH;

        // if both vectors are invalid, it's type 1 interleaved LoROM
        if (ExtendedFormat == EXTFMT.NOPE &&
            ((ROM[0x7ffc] + (ROM[0x7ffd] << 8)) < 0x8000) &&
            ((ROM[0xfffc] + (ROM[0xfffd] << 8)) < 0x8000))
        {
            Debug.WriteLine("Interleaved ROMs are unsupported. Because WHY?");
        }
        // CalculatedSize is now set, so rescore
        hi_score = ScoreHiROM(false, 0);
        lo_score = ScoreLoROM(false, 0);

        if (String.Compare(Encoding.ASCII.GetString(ROM, 0x7fc0, 22), "YUYU NO QUIZ DE GO!GO!") == 0 ||
            String.Compare(Encoding.ASCII.GetString(ROM, 0xffc0, 21), "BATMAN--REVENGE JOKER") == 0)
        {
            LoROM = true;
            HiROM = false;
        }

        if (lo_score >= hi_score)
        {
            LoROM = true;
            HiROM = false;

            // ignore map type byte if not 0x2x or 0x3x
            if ((ROM[0x7fd5] & 0xf0) == 0x20 || (ROM[0x7fd5] & 0xf0) == 0x30)
            {
                switch (ROM[0x7fd5] & 0xf)
                {
                    case 1:
                    case 5:
                        Debug.WriteLine("Interleaved ROMs are unsupported. Because WHY?");
                        break;
                }
            }
        }
        else
        {
            LoROM = false;
            HiROM = true;

            if ((ROM[0xffd5] & 0xf0) == 0x20 || (ROM[0xffd5] & 0xf0) == 0x30)
            {
                switch (ROM[0xffd5] & 0xf)
                {
                    case 0:
                    case 3:
                        Debug.WriteLine("Interleaved ROMs are unsupported. Because WHY?");
                        break;
                }
            }
        }

        int hPos = (HiROM) ? 0xFFB0 : 0x7FB0;
        ROMId = Encoding.ASCII.GetString(ROM, hPos + 2, 4);
        //get ROM Name
        Array.Copy(ROM, hPos + 0x10, RawROMName, 0, ROM_NAME_LEN);
        ROMName = Encoding.ASCII.GetString(RawROMName);
        //BS ROMs are unsupported
        ROMSize = ROM[hPos + 0x27];
        SRAMSize = ROM[hPos + 0x28];
        ROMSpeed = ROM[hPos + 0x25];
        ROMType = ROM[hPos + 0x26];
        ROMRegion = ROM[hPos + 0x29];

        ROMChecksum = (uint)(ROM[hPos + 0x2E] + (ROM[hPos + 0x2F] << 8));
        ROMComplementChecksum = (uint)(ROM[hPos + 0x2C] + (ROM[hPos + 0x2D] << 8));

        if (ROM[hPos + 0x2A] != 0x33)
        {
            CompanyId = ((ROM[hPos + 0x2A] >> 4) & 0x0F) * 36 + (ROM[hPos + 0x2A] & 0x0F);
        }
        else
        {
            string co = Encoding.ASCII.GetString(ROM, hPos, 2);
            if (char.IsNumber(co[0]) && char.IsNumber(co[1]))
            {
                int l, r, l2, r2;
                l = co[0];
                r = co[1];
                l2 = (l > '9') ? l - '7' : l - '0';
                r2 = (r > '9') ? r - '7' : r - '0';
                CompanyId = l2 * 36 + r2;
            }
        }

        CalculatedChecksum = Checksum_Calculate(hPos);

        bool isChecksumOK = (ROMChecksum + ROMComplementChecksum == 0xffff) &
                         (ROMChecksum == CalculatedChecksum);

        if (!isChecksumOK)
        {
            Console.WriteLine("Correcting checksum...");
            uint CalcComplement = 0xffff - CalculatedChecksum;
            //Write New Checksum
            ROM[hPos + 0x2F] = (byte)(CalculatedChecksum >> 8);
            ROM[hPos + 0x2E] = (byte)(CalculatedChecksum & 0xFF);
            //Write New Compliment
            ROM[hPos + 0x2D] = (byte)(CalcComplement >> 8);
            ROM[hPos + 0x2C] = (byte)(CalcComplement & 0xFF);
            return true;
        }
        else
        {
            Console.WriteLine("Checksum OK");
            return false;
        }
    }

    static uint Checksum_Calculate(int hPos)
    {
        // from NSRT
        uint sum = 0;
        string COProcessor = GetROMType(hPos);

        if (COProcessor == "SPC7110")
        {
            sum = checksum_calc_sum(ROM, CalculatedSize);
            if (CalculatedSize == 0x300000)
                sum += sum;
        }
        else
        {
            if ((CalculatedSize & 0x7fff) > 0)
                sum = checksum_calc_sum(ROM, CalculatedSize);
            else
            {
                uint length = CalculatedSize;
                sum = checksum_mirror_sum(ROM, length, 0x800000);
            }
        }
        //if (Settings.BS && !Settings.BSXItself)
        //    sum = checksum_calc_sum(ROM, CalculatedSize) - checksum_calc_sum(ROM + (HiROM ? 0xffb0 : 0x7fb0), 48);
        return sum;
    }

    static string GetROMType(int hPos)
    {

        int DSP = 0;
        // DSP1/2/3/4
        if (ROMType == 0x03)
        {
            if (ROMSpeed == 0x30)
                DSP = 4; // DSP4
            else
                DSP = 1; // DSP1
        }
        else
        {
            if (ROMType == 0x05)
            {
                if (ROMSpeed == 0x20)
                    DSP = 2; // DSP2
                else
                    if (ROMSpeed == 0x30 && ROM[hPos + 0x2a] == 0xb2)
                        DSP = 3; // DSP3
                    else
                        DSP = 1; // DSP1
            }
        }

        uint identifier = (uint)(((ROMType & 0xff) << 8) + (ROMSpeed & 0xff));
        string COProcessor = "";
        switch (identifier)
        {
            // SRTC
            case 0x5535:
                COProcessor = "SRTC";
                break;

            // SPC7110
            case 0xF93A:
                COProcessor = "SPC7110RTC";
                break;
            case 0xF53A:
                COProcessor = "SPC7110";
                break;

            // OBC1
            case 0x2530:
                COProcessor = "OBC1";
                break;

            // SA1
            case 0x3423:
            case 0x3523:
                COProcessor = "SA1";
                break;

            // SuperFX
            case 0x1320:
            case 0x1420:
            case 0x1520:
            case 0x1A20:
                COProcessor = "SuperFX";
                //S9xInitSuperFX();
                if (ROM[0x7FDA] == 0x33)
                    SRAMSize = ROM[0x7FBD];
                else
                    SRAMSize = 5;
                break;

            // SDD1
            case 0x4332:
            case 0x4532:
                COProcessor = "SDD1";
                break;

            // ST018
            case 0xF530:
                COProcessor = "ST_018";
                SRAMSize = 2;
                break;

            // ST010/011
            case 0xF630:
                if (ROM[0x7FD7] == 0x09)
                {
                    COProcessor = "ST_011";
                }
                else
                {
                    COProcessor = "ST_010";
                }
                SRAMSize = 2;
                break;

            // C4
            case 0xF320:
                COProcessor = "C4";
                break;
        }
        return (DSP > 0) ? "DSP" + DSP.ToString() : COProcessor;
    }

    static ushort checksum_calc_sum(byte[] data, uint length)
    {
        ushort sum = 0;
        for (uint i = 0; i < length; i++)
            sum += data[i];
        return sum;
    }

    static ushort checksum_mirror_sum (byte[] start, uint length, uint mask)
    {
	    // from NSRT
	    while (!((length & mask) > 0))
		    mask >>= 1;

	    ushort	part1 = checksum_calc_sum(start, mask);
	    ushort	part2 = 0;

	    uint	next_length = length - mask;
	    if (next_length > 0)
	    {
            byte[] newstart = new byte[next_length];
            Array.Copy(start, mask, newstart, 0, next_length);
		    part2 = checksum_mirror_sum(newstart, next_length, mask >> 1);

		    while (next_length < mask)
		    {
			    next_length += next_length;
			    part2 += part2;
		    }

		    length = mask + mask;
	    }

	    return (ushort)(part1 + part2);
    }

    static bool allASCII (byte[] b, int offset, int size)
    {
	    for (int i = 0; i < size; i++)
	    {
		    if (b[i] < 32 || b[i] > 126)
			    return false;
	    }
	    return true;
    }

    static int ScoreHiROM(bool skip_header, int romoff)
    {
        byte[] buf = new byte[0xFF];
        Array.Copy(ROM, romoff + 0xff00 + (skip_header ? 0x200 : 0), buf, 0, 0xFF);
        //ROM + 0xff00 + romoff + (skip_header ? 0x200 : 0);

        int score = 0;

        if ((buf[0xd5] & 0x1) > 0)
            score += 2;

        // Mode23 is SA-1
        if (buf[0xd5] == 0x23)
            score -= 2;

        if (buf[0xd4] == 0x20)
            score += 2;

        if ((buf[0xdc] + (buf[0xdd] << 8)) + (buf[0xde] + (buf[0xdf] << 8)) == 0xffff)
        {
            score += 2;
            if (0 != (buf[0xde] + (buf[0xdf] << 8)))
                score++;
        }

        if (buf[0xda] == 0x33)
            score += 2;

        if ((buf[0xd5] & 0xf) < 4)
            score += 2;

        if (!((buf[0xfd] & 0x80) > 0))
            score -= 6;

        if ((buf[0xfc] + (buf[0xfd] << 8)) > 0xffb0)
            score -= 2; // reduced after looking at a scan by Cowering

        if (CalculatedSize > 1024 * 1024 * 3)
            score += 4;

        if ((1 << (buf[0xd7] - 7)) > 48)
            score -= 1;

        if (!allASCII(buf, 0xb0, 6))
            score -= 1;

        if (!allASCII(buf, 0xc0, ROM_NAME_LEN - 1))
            score -= 1;

        return (score);
    }

    static int ScoreLoROM(bool skip_header, int romoff)
    {
        byte[] buf = new byte[0xFF];
        Array.Copy(ROM, romoff + 0x7f00 + (skip_header ? 0x200 : 0), buf, 0, 0xFF);
        //uint8	*buf = ROM + 0x7f00 + romoff + (skip_header ? 0x200 : 0);
        int score = 0;

        if (!((buf[0xd5] & 0x1) > 0))
            score += 3;

        // Mode23 is SA-1
        if (buf[0xd5] == 0x23)
            score += 2;

        if ((buf[0xdc] + (buf[0xdd] << 8)) + (buf[0xde] + (buf[0xdf] << 8)) == 0xffff)
        {
            score += 2;
            if (0 != (buf[0xde] + (buf[0xdf] << 8)))
                score++;
        }

        if (buf[0xda] == 0x33)
            score += 2;

        if ((buf[0xd5] & 0xf) < 4)
            score += 2;

        if (!((buf[0xfd] & 0x80) > 0))
            score -= 6;

        if ((buf[0xfc] + (buf[0xfd] << 8)) > 0xffb0)
            score -= 2; // reduced per Cowering suggestion

        if (CalculatedSize <= 1024 * 1024 * 16)
            score += 2;

        if ((1 << (buf[0xd7] - 7)) > 48)
            score -= 1;

        if (!allASCII(buf, 0xb0, 6))
            score -= 1;

        if (!allASCII(buf, 0xc0, ROM_NAME_LEN - 1))
            score -= 1;

        return (score);
    }


}

