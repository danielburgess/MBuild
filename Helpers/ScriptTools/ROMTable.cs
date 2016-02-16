using System.Collections.Generic;
using System.IO;
using System.Text;

public class ROMTable
{
    public struct ROMChar
    {
        public int DecValue;
        public string HexValue;
        public string AsciiUnicode;
        public int CharCode;
        public int ByteLength;
    }

    public Dictionary<string, ROMChar> HexMap = new Dictionary<string, ROMChar>(); //indexed by hex
    public Dictionary<string, ROMChar> CharMap = new Dictionary<string, ROMChar>(); //indexed by character

    public static ROMTable LoadTable(string file)
    {
        ROMTable rt = new ROMTable();

        int errCount = 0;
        Encoding enc = TextFileEncodingDetector.DetectTextFileEncoding(file);
        if (enc == null)
        {
            enc = ASCIIEncoding.ASCII;
        }
        using (StreamReader sr = new StreamReader(file, enc))
        {
            while (!sr.EndOfStream)
            {
                string str = sr.ReadLine();
                try
                {
                    if (str.Split('=')[0].Contains("**"))
                    {//if other similar mappings are well defined, they should be before this definition
                        for (int d = 0; d < 256; d++)
                        {
                            if (str.Split('=')[0].Contains("%%"))
                            {
                                for (int e = 0; e < 256; e++)
                                {
                                    ROMTable.ROMChar chr = new ROMTable.ROMChar();
                                    chr.AsciiUnicode = str.Split('=')[1].Replace("**", d.ToString("X2")).Replace("%%", e.ToString("X2"));
                                    chr.HexValue = str.Split('=')[0].Replace("**", d.ToString("X2")).Replace("%%", e.ToString("X2"));
                                    chr.DecValue = new HexWord().StringToDec(chr.HexValue);
                                    chr.ByteLength = (chr.HexValue.Length / 2);//get the number of bytes...
                                    if (!rt.HexMap.ContainsKey(chr.HexValue))
                                    {
                                        rt.HexMap.Add(chr.HexValue, chr);
                                    }
                                    string chrprep = chr.AsciiUnicode.Replace("\\", "");
                                    if (!rt.CharMap.ContainsKey(chrprep))
                                    {
                                        rt.CharMap.Add(chrprep, chr);
                                    }
                                }
                            }
                            else
                            {
                                ROMTable.ROMChar chr = new ROMTable.ROMChar();
                                chr.AsciiUnicode = str.Split('=')[1].Replace("**", d.ToString("X2"));
                                chr.HexValue = str.Split('=')[0].Replace("**", d.ToString("X2"));
                                chr.DecValue = new HexWord().StringToDec(chr.HexValue);
                                chr.ByteLength = (chr.HexValue.Length / 2);//get the number of bytes...
                                if (!rt.HexMap.ContainsKey(chr.HexValue))
                                {
                                    rt.HexMap.Add(chr.HexValue, chr);
                                }
                                string chrprep = chr.AsciiUnicode.Replace("\\", "");
                                if (!rt.CharMap.ContainsKey(chrprep))
                                {
                                    rt.CharMap.Add(chrprep, chr);
                                }
                            }
                        }
                    }
                    else
                    {
                        ROMTable.ROMChar chr = new ROMTable.ROMChar();
                        chr.AsciiUnicode = str.Split('=')[1];
                        chr.HexValue = str.Split('=')[0];
                        chr.DecValue = new HexWord().StringToDec(chr.HexValue);
                        chr.ByteLength = (chr.HexValue.Length / 2);//get the number of bytes...
                        if (!rt.HexMap.ContainsKey(chr.HexValue))
                        {
                            rt.HexMap.Add(chr.HexValue, chr);
                        }
                        string chrprep = chr.AsciiUnicode.Replace("\\", "");
                        if (!rt.CharMap.ContainsKey(chrprep))
                        {
                            rt.CharMap.Add(chrprep, chr);
                        }
                    }
                }
                catch { errCount++; }
            }
        }
        return rt;
    }
}
