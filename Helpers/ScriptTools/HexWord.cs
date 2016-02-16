using System;
using System.Linq;

public class HexWord
{
    public string HexVal = "";
    public int DecVal = 0;
    public byte[] ByteVal = { 0, 0 };

    private string[] hValues = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "A", "B", "C", "D", "E", "F" };


    public HexWord() { } //Defaults are above

    public HexWord(string HexValue)
    {
        if (((HexValue.Length < 3) && (HexValue.Length > 1)) && (IsHex(HexValue)))
        {
            HexVal = HexValue.ToUpper();
            DecVal = StringToDec(HexValue);
            ByteVal = StringToByte(HexValue);
        }
        else
        {
            InvalidHV(HexValue);
        }
    }

    private byte[] StringToByte(string HexValue)
    {
        byte[] full = { 0, 0 };
        int pos = 0;
        foreach (char c in HexValue.ToUpper())
        {
            if (pos == 0 && HexValue.Length == 2)
                full[0] = byte.Parse(((hValues.ToList().IndexOf(c.ToString().ToUpper())) * 16).ToString());
            if (pos == 1 || (HexValue.Length == 1 && pos == 0))
                full[1] = byte.Parse(((hValues.ToList().IndexOf(c.ToString().ToUpper()))).ToString());
            pos++;
        }
        return full;
    }

    public int StringToDec(string HexValue)
    {
        int full = 0;
        int pos = 0;
        foreach (char c in HexValue.ToUpper())
        {
            if (pos == 0 && HexValue.Length == 2)
                full += (hValues.ToList().IndexOf(c.ToString().ToUpper())) * 16;
            if (pos == 1 || (HexValue.Length == 1 && pos == 0))
                full += (hValues.ToList().IndexOf(c.ToString().ToUpper()));
            pos++;
        }
        return full;
    }

    private void InvalidHV(object input)
    {
        throw new ArgumentOutOfRangeException("HexValue", input, "Not a valid Hexidecimal value.");
    }

    private bool IsHex(string val)
    {//check up to 2 chars for 0-F
        bool meh = true;
        foreach (char c in val)
        {
            if (c.ToString() == "")
            {
                meh = false;
                break;
            }
            else if (!char.IsLetterOrDigit(c))
            {
                meh = false;
                break;
            }
            else if (char.IsLetter(c))
            {
                if (!hValues.ToList().Contains(c.ToString().ToUpper())) // checks for a-f
                {
                    meh = false;
                    break;
                }
            }
        }
        return meh;
    }

    public HexWord(int DecValue)
    {
        if (DecValue < 256)
        {
            DecVal = DecValue;
            HexVal = DecToString(DecValue);
            ByteVal = DecToByte(DecValue);
        }
        else
        {
            InvalidHV(DecValue);
        }
    }

    private byte[] DecToByte(int DecValue)
    {
        byte[] b = { 0, 0 };
        int bt = DecValue / 16;
        b[0] = Convert.ToByte(bt);
        bt = DecValue - bt;
        b[1] = Convert.ToByte(bt);
        return b;
    }

    private string DecToString(int DecValue)
    {
        string st = "";
        int bt = DecValue / 16;
        st = hValues[bt];
        int rt = bt * 16;
        bt = DecValue - rt;
        //bt = bt / 16;
        st += hValues[bt];
        return st;
    }

    public HexWord(byte[] ByteValue)
    {
        if (ByteValue.Length == 2)
        {
            DecVal = ByteToDec(ByteValue);
            HexVal = ByteToString(ByteValue);
            ByteVal = ByteValue;
        }
        else
        {
            InvalidHV(ByteValue);
        }
    }

    private string ByteToString(byte[] ByteValue)
    {
        string s = "";
        s += hValues[Convert.ToInt32(ByteValue[0])];
        s += hValues[Convert.ToInt32(ByteValue[1])];
        return s;
    }

    private int ByteToDec(byte[] ByteValue)
    {
        int i = 0;
        i += (Convert.ToInt32(ByteValue[0]) * 16);
        i += Convert.ToInt32(ByteValue[1]);
        return i;
    }
    public string[] Val()
    {
        return hValues;
    }

    public string Val(int ZeroTo15)
    {
        if (ZeroTo15 < 16)
        {
            return hValues[ZeroTo15];
        }
        else
        {
            return "";
        }
    }


}
