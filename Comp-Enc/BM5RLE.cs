using System;
using System.IO;
//  Adapted from c++ code by Proton


class BM5RLE
{
    const uint buffersize = 0x10000;
    const int MAX_MATCH_LENGTH = 32;
    const int MIN_MATCH_LENGTH = 1;
    const int MAX_RAW_LENGTH = 32;
    const int D = 6;
    static byte[] _distanceData = new byte[] { 1, 2, 4, 16, 32, 64 };

    struct backref_t
    {
        public ushort length, distance;
    }

    static backref_t new_backref_t()
    {
        backref_t t = new backref_t();
        t.distance = 0;
        t.length = 0;
        return t;
    }

    public static void Compress(string filename, string outfile)
    {
        byte[] cdata = Compress(filename);
        Console.WriteLine("Writing " + Path.GetFileName(outfile) + "...");
        File.WriteAllBytes(outfile, cdata);
    }

    public static byte[] Compress(string filename)
    {
        Console.WriteLine("Reading: " + Path.GetFileName(filename) + "...");
        byte[] _tempData = File.ReadAllBytes(filename);
        return Compress(_tempData);
    }

    public static byte[] Compress(byte[] indata)
    {
        Console.WriteLine("Compressing " + indata.Length.ToString() + " bytes...");
        //cout << "--===Super Bomberman 5===---" << endl;
        byte[] _inputData = new byte[buffersize];
        byte[] _outputData = new byte[buffersize];
        //byte[] _tempData = new byte[buffersize];
        byte[] _rawData = new byte[MAX_RAW_LENGTH];
        int _rawLength = 0;
        int _inputPosition = 0;
        int _outputPosition = 0;
        int _inputSize = 0;

        _inputSize = indata.Length;

        int _newSize = _inputSize;

        Array.Copy(indata, 0, _inputData, 0, _newSize);

        backref_t backref = new_backref_t();
        backref_t incrref = new_backref_t();

        //cout << hex << "Starting compression...";
        //Console.Write("Starting Compression...");

        while (_inputPosition < _newSize)
        {

            //Console.Write("\r\nIn Position: " + (_inputPosition).ToString("X") + " | Out Position: " + _outputPosition.ToString("X"));

            //cout << hex << "\nIn_pos=[" << setw(4)
            //        << (int) _inputPosition - 0x40 << "]";
            //cout << hex << " Out_pos=[" << setw(4) << (int) _outputPosition
            //        << "]";

            if (_inputPosition == 31)
            {
                //Console.Write("meh");
            }

            backref = search_backref(_inputPosition, _inputData, _newSize);
            incrref = search_incr(_inputPosition, _inputData, _newSize);

            if (incrref.length > backref.length || (incrref.length >= _rawLength && incrref.length >= MIN_MATCH_LENGTH))
            {

                //cout << " WRITE_INCRREF";

                if (_rawLength > 0)
                {
                    //cout << ", WRITE_RAW_FIRST";
                    write_raw(_outputData, _outputPosition, _rawData,
                            _rawLength);
                    _outputPosition += (_rawLength + 1);
                    _rawLength = 0;
                }

                write_incrref(_outputData, _outputPosition, incrref);

                _inputPosition += incrref.length;
                _outputPosition++;

            }

            else
            {

                if (backref.length > _rawLength
                        || backref.length > MIN_MATCH_LENGTH
                        || (backref.length < _rawLength
                                && backref.length > MIN_MATCH_LENGTH)

                        )
                {
                    //cout << " WRITE_BACKREF";

                    if (_rawLength > 0)
                    {
                        //cout << ", WRITE_RAW_FIRST";
                        write_raw(_outputData, _outputPosition, _rawData,
                                _rawLength);
                        _outputPosition += (_rawLength + 1);
                        _rawLength = 0;
                    }

                    write_backref(_outputData, _outputPosition, backref);

                    _inputPosition += backref.length;
                    _outputPosition++;

                }
                else
                {
                    _rawData[_rawLength++] = _inputData[_inputPosition++];
                    //cout << hex << " RAW_LENGTH=[" << (int) _rawLength << "]";

                    if (_rawLength == MAX_RAW_LENGTH)
                    {
                        //cout << " WRITE_RAW";
                        write_raw(_outputData, _outputPosition, _rawData,
                                _rawLength);
                        //_inputPosition += _rawLength;
                        int plus = _rawLength + 1;
                        _outputPosition += plus;
                        _rawLength = 0;
                    }
                }

            }

        }

        if (_rawLength > 0)
        {
            //cout << " WRITE_RAW";
            write_raw(_outputData, _outputPosition, _rawData, _rawLength);
            _inputPosition += _rawLength;
            int plus = _rawLength + 1;
            _outputPosition += plus;
            _rawLength = 0;

        }
        //Console.WriteLine("Compression Complete.");
        //write file here
        byte[] final = new byte[_outputPosition];
        Console.WriteLine("Data reduced to " + _outputPosition.ToString() + " bytes.");
        Array.Copy(_outputData, 0, final, 0, _outputPosition);
        //File.WriteAllBytes(outfile, final);
        return final;
    }

    static backref_t search_backref(int pos, byte[] buffer, int inputsize)
    {

        backref_t variant = new_backref_t();
        int match = 0;

        int backref_len = 0;
        int backref_dist = 0;

        int searchPosition = 0;

        for (int idx = 0; idx < D; idx++)
        {
            searchPosition = pos - _distanceData[idx];
            if (searchPosition > -1)
            {//added to try and fix the missing data bug...
                if ((buffer[searchPosition] == buffer[pos]))
                {
                    match = 1;

                    while ((buffer[searchPosition + match] == buffer[pos + match])
                            && (pos + match < inputsize)
                            && ((searchPosition + MAX_MATCH_LENGTH)
                                    < (pos + MAX_MATCH_LENGTH)))
                    {

                        if (match >= MAX_MATCH_LENGTH)
                        {
                            break;
                        }
                        match++;
                    }

                    if (match > backref_len)
                    {
                        backref_len = match;
                        backref_dist = _distanceData[idx];
                    }
                }
            }
        }

        variant.length = (ushort)backref_len;
        variant.distance = (ushort)backref_dist;
        //cout << hex << " len=[" << setw(4) << (int)variant.length << "]";
        //cout << hex << " dist=[" << setw(4) << (int)variant.distance << "]";

        return variant;
    }

    static backref_t search_incr(int pos, byte[] buffer, int inputsize)
    {

        backref_t variant = new_backref_t();
        int match = 0;

        int backref_len = 0;

        int searchPosition = 0;

        //int i = 1;
        searchPosition = pos - 1;
        if (searchPosition > -1)
        {//added to try and fix the missing data bug...
            int c = buffer[searchPosition];
            int n = buffer[searchPosition] + 1;
            //cout << hex << " c=[" << setw(4) << (int)c << "]";
            //cout << hex << " n=[" << setw(4) << (int)n << "]";

            if (buffer[pos] == n)
            {

                match = 1;
                //cout << hex << " match1=[" << setw(4) << (int)match << "]";

                while (buffer[pos + match] == (n + match))
                {

                    if (match >= MAX_MATCH_LENGTH)
                    {
                        break;
                    }
                    match++;

                }

                if (match > backref_len)
                {
                    backref_len = match;
                }
            }
        }

        variant.length = (ushort)backref_len;

        return variant;
    }

    static void write_raw(byte[] outb, int out_pos, byte[] inb, int insize)
    {
        int size = insize - 1;
        outb[out_pos++] = (byte)(0x00 | size);
        for (int i = 0; i < insize; i++)
        {
            outb[out_pos++] = inb[i];
        }
    }

    static void write_backref(byte[] outb, int out_pos, backref_t backref)
    {

        int size = backref.length - 1;

        switch (backref.distance)
        {
            case 1:
                outb[out_pos] = (byte)(0x40 | size);
                break;
            case 2:
                outb[out_pos] = (byte)(0x60 | size);
                break;
            case 4:
                outb[out_pos] = (byte)(0x80 | size);
                break;
            case 16:
                outb[out_pos] = (byte)(0xA0 | size);
                break;
            case 32:
                outb[out_pos] = (byte)(0xC0 | size);
                break;
            case 64:
                outb[out_pos] = (byte)(0xE0 | size);
                break;
        }

    }

    static void write_incrref(byte[] outb, int out_pos, backref_t backref)
    {
        int size = backref.length - 1;
        outb[out_pos] = (byte)(0x20 | size);
    }


    public static void Decompress(byte[] DataIn, int OutLen, string OutFile)
    {
        byte[] cdata = Decompress(DataIn, OutLen);
        File.WriteAllBytes(OutFile, cdata);
    }

    public static byte[] Decompress(byte[] r, int DataLength)
    {
        int inPos = 0;
        int outPos = 0;
        byte[] o = new byte[buffersize];

        while (outPos < DataLength)
        {
            byte ctrl;
            int rlePos;
            int i, cnt;
            byte e;
            byte chr;

            ctrl = r[inPos++];

            e = (byte)(ctrl >> 5);

            switch (e)
            {
                //RLE PREVIOUS - 0x02
                //011x xxxx
                //0x60 - 0x7F
                case (0x03):
                    cnt = (ctrl & 0x1f) + 1;
                    rlePos = outPos - 2;
                    if (rlePos > 0)
                    {
                        for (i = 0; i < cnt; i++)
                        {
                            o[outPos++] = o[rlePos++];
                        }
                    }
                    else
                    {
                        for (i = 0; i < cnt; i++)
                        {
                            o[outPos++] = 0x00;
                        }
                    }
                    break;
                //RLE PREVIOUS - 0x10
                //011x xxxx
                //0xA0 - 0xBF
                case (0x05):
                    //cout << hex << "\tRLE: 0xA0 - 0xBF";
                    cnt = (ctrl & 0x1f) + 1;
                    rlePos = outPos - 0x10;
                    if (rlePos > 0)
                    {
                        for (i = 0; i < cnt; i++)
                        {
                            o[outPos++] = o[rlePos++];
                        }
                    }
                    else
                    {
                        for (i = 0; i < cnt; i++)
                        {
                            o[outPos++] = 0x00;
                        }
                    }
                    break;
                //RLE PREVIOUS - 0x04
                //100x xxxx
                //0x80 - 0x9F
                case (0x04):
                    //cout << hex << "\tRLE: 0x80 - 0x9F";
                    cnt = (ctrl & 0x1f) + 1;
                    rlePos = outPos - 4;
                    if (rlePos > 0)
                    {
                        for (i = 0; i < cnt; i++)
                        {
                            o[outPos++] = o[rlePos++];
                        }
                    }
                    else
                    {
                        for (i = 0; i < cnt; i++)
                        {
                            o[outPos++] = 0x00;
                        }
                    }
                    break;

                //RLE PREVIOUS - 0x20
                //100x xxxx
                //0xC0 - 0xDF
                case (0x06):
                    //cout << hex << "\tRLE: 0xC0 - 0xDF";
                    cnt = (ctrl & 0x1f) + 1;
                    rlePos = outPos - 0x20;
                    if (rlePos > 0)
                    {
                        for (i = 0; i < cnt; i++)
                        {
                            o[outPos++] = o[rlePos++];
                        }
                    }
                    else
                    {
                        for (i = 0; i < cnt; i++)
                        {
                            o[outPos++] = 0x00;
                        }
                    }
                    break;

                //RLE PREVIOUS - 0x40
                //111x xxxx
                //0xE0 - 0xFF
                case (0x07):

                    //cout << hex << "\tRLE: 0xE0 - 0xFF";
                    cnt = (ctrl & 0x1f) + 1;
                    rlePos = outPos - 0x40;
                    if (rlePos > 0)
                    {
                        for (i = 0; i < cnt; i++)
                        {
                            o[outPos++] = o[rlePos++];
                        }
                    }
                    else
                    {
                        for (i = 0; i < cnt; i++)
                        {
                            o[outPos++] = 0x00;
                        }
                    }
                    break;

                //RLE PREVIOUS - 0x01
                //010x xxxx
                //0x40 - 0x5F
                case (0x02):

                    //cout << hex << "\tRLE: 0x40 - 0x5F";
                    cnt = (ctrl & 0x1f) + 1;

                    if (outPos == 0)
                    {
                        chr = 0x00;

                        o[outPos++] = chr;
                        rlePos = 0;
                        for (i = 0; i < cnt - 1; i++)
                        {
                            o[outPos++] = o[rlePos++];
                        }

                    }
                    else
                    {
                        rlePos = outPos - 1;
                        for (i = 0; i < cnt; i++)
                        {
                            o[outPos++] = o[rlePos++];
                        }
                    }

                    break;

                //RAW
                //000x xxxx
                //0x00 - 0x1F
                case (0x00):
                    //cout << hex << "\tRAW: 0x00 - 0x1F";
                    cnt = (ctrl & 0x1f) + 1;
                    for (i = 0; i < cnt; i++) o[outPos++] = r[inPos++];
                    break;

                //RLE PREVIOUS+INCREMENT
                //001x xxxx
                //0x20 - 0x3F
                case (0x01):
                    //cout << "\tRLE: 0x20 - 0x3F";
                    cnt = (ctrl & 0x1f) + 1;
                    chr = o[((outPos > 0) ? (outPos - 1) : outPos)];

                    for (i = 0; i < cnt; i++) o[outPos++] = ++chr;
                    break;

            }

        }

        byte[] final = new byte[DataLength];
        Array.Copy(o, 0, final, 0, DataLength);

        return final;
    }



}

