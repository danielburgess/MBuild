using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;


class Script
{
    public static string BuildScript(string input, string output, string table)//return binary script path...
    {
        Console.WriteLine("Started Binary Script File Build...");
        if (input == "" || table == "")
        {
            Console.WriteLine("Invalid or missing arguments. Failed.");
            return "";
        }
        else if (output == "")
        {
            //generate output name based on the input
            output = input.Split('.')[0];
        }
        output = output.Split('.')[0] + "_" + DateTime.Now.ToString("yyyyMMdd") + ".bin";

        Console.WriteLine("Reading Table file...");
        ROMTable rt = ROMTable.LoadTable(table);
        Console.WriteLine("Reading Script file...");
        string script = File.ReadAllText(input).Replace("\r\n", "");
        List<byte> outfile = new List<byte>();
        List<byte[]> splitdata = new List<byte[]>();


        List<int> handled = new List<int>();

        Console.WriteLine("Evaluating data...");
        int left = Console.CursorLeft;
        int top = Console.CursorTop;

        int offset = 0;
        for (int c = 0; c < script.Length; c++)
        {
            decimal per = ((decimal)(c + 1) / (decimal)(script.Length)) * (100);
            Console.SetCursorPosition(left, top);
            Console.Write(((int)per).ToString() + "%");
            if (!(handled.Contains(c)))
            {
                int maxlen = 10;
                if ((script.Length - c) < 10)
                {
                    maxlen = (script.Length - c);
                }

                string[] sc = new string[maxlen];
                for (int p = 0; p < maxlen; p++) //look up to 10 characters ahead
                {
                    if (p > 0)
                    {
                        sc[p] = sc[p - 1];
                    }
                    sc[p] += script[c + p].ToString();
                }

                bool found = false;
                //now check from longest to shortest for a match...
                for (int p = (maxlen - 1); p > -1; p--)
                {
                    if (!found && rt.CharMap.ContainsKey(sc[p]))
                    {
                        found = true;
                        for (int b = 0; b < rt.CharMap[sc[p]].HexValue.Length; b += 2)
                        {
                            string val = rt.CharMap[sc[p]].HexValue.Substring(b, 2);
                            outfile.Add(byte.Parse(val, NumberStyles.HexNumber));
                        }
                    }
                    if (found)
                    {
                        handled.Add(c + p);
                    }
                }
            }
            if (outfile.Count > 50000)
            {
                using (FileStream fs = new FileStream(output, FileMode.Append))
                {
                    fs.Seek(offset, SeekOrigin.Begin);
                    fs.Write(outfile.ToArray(), 0, outfile.Count);
                    offset += outfile.Count;
                    outfile.Clear();
                }
            }
        }
        if (outfile.Count > 0)
        {
            using (FileStream fs = new FileStream(output, FileMode.Append))
            {
                fs.Seek(offset, SeekOrigin.Begin);
                fs.Write(outfile.ToArray(), 0, outfile.Count);
                offset += outfile.Count;
                outfile.Clear();
            }
        }

        Console.SetCursorPosition(left, top);
        Console.Write("Process Complete!!\r\n");

        Console.WriteLine("Done!");
        return output;
    }

    struct WorkerData
    {
        public int piececount;
        public string indata;
        public ROMTable rt;
        public int total;
        public int pos;
        public byte[] outfile;
    }

    static void ScriptWorker_DoWork(object sender, DoWorkEventArgs e)
    {//argument is WorkerData
        WorkerData d = (WorkerData)e.Argument;
        List<int> handled = new List<int>();
        List<byte> outfile = new List<byte>();
        for (int c = 0; c < d.indata.Length; c++)
        {
            if (!(handled.Contains(c)))
            {
                int maxlen = 10;
                if ((d.indata.Length - c) < 10)
                {
                    maxlen = (d.indata.Length - c);
                }

                string[] sc = new string[maxlen];
                for (int p = 0; p < maxlen; p++) //look up to 10 characters ahead
                {
                    if (p > 0)
                    {
                        sc[p] = sc[p - 1];
                    }
                    sc[p] += d.indata[c + p].ToString();
                }

                bool found = false;
                //now check from longest to shortest for a match...
                for (int p = (maxlen - 1); p > -1; p--)
                {
                    if (!found && d.rt.CharMap.ContainsKey(sc[p]))
                    {
                        found = true;
                        for (int b = 0; b < d.rt.CharMap[sc[p]].HexValue.Length; b += 2)
                        {
                            string val = d.rt.CharMap[sc[p]].HexValue.Substring(b, 2);
                            outfile.Add(byte.Parse(val, NumberStyles.HexNumber));
                        }
                    }
                    if (found)
                    {
                        handled.Add(c + p);
                    }
                }

                if (!found)
                {//didn't find a matching value for this character??
                    //write a default byte? experimental...
                    string display = "";
                    for (int h = 0; h < sc.Length; h++)
                    {
                        display += sc[h];
                    }
                    Console.WriteLine("Character match not found in table! (" + display + ")");
                    outfile.Add(0x20);
                    handled.Add(c);
                }
            }
        }
        d.outfile = outfile.ToArray();
        e.Result = d;
    }
    static void ScriptWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    {//returns WorkerData
        Final.Add((WorkerData)e.Result);
    }

    static List<WorkerData> Final;

    public static string BuildScriptThread(string input, string output, string table)//return binary script path...
    {
        Final = new List<WorkerData>();
        Console.WriteLine("Started Binary Script File Build...");
        if (input == "" || table == "")
        {
            Console.WriteLine("Invalid or missing arguments. Failed.");
            return "";
        }
        else if (output == "")
        {
            //generate output name based on the input
            output = Path.GetFileNameWithoutExtension(input);//.Split('.')[0];
        }
        else
        {
            output = Path.GetFullPath(output);
            if (!File.Exists(output))
            {
                output = output.Split('.')[0] + "_" + DateTime.Now.ToString("yyyyMMdd") + ".bin";
            }
        }

        Console.WriteLine("Reading Table file...");
        ROMTable rt = ROMTable.LoadTable(table);
        Console.WriteLine("Reading Script file...");
        string script = File.ReadAllText(input).Replace("\r\n", "");
        List<byte> outfile = new List<byte>();
        List<byte[]> splitdata = new List<byte[]>();


        List<int> handled = new List<int>();

        Console.WriteLine("Evaluating data...");
        int left = Console.CursorLeft;
        int top = Console.CursorTop;

        List<int> ControlPos = new List<int>();

        int lastpos = 0;
        //find each index of a control character...
        for (int c = 0; c < script.Length; c = lastpos + 3)
        {
            decimal per = ((decimal)(c + 1) / (decimal)(script.Length)) * (100);
            Console.SetCursorPosition(left, top);
            Console.Write(((int)per).ToString() + "%");

            lastpos = script.IndexOf("[en", c);
            if (!(lastpos == -1))
            {
                ControlPos.Add(lastpos);
            }
            else
            {
                break;
            }
        }

        //make background workers...
        BackgroundWorker w1 = new BackgroundWorker();
        BackgroundWorker w2 = new BackgroundWorker();
        BackgroundWorker w3 = new BackgroundWorker();
        BackgroundWorker w4 = new BackgroundWorker();
        int piece = ControlPos.Count / 4; // make 4 worker threads

        if (piece < 4)
        {//then just do a single worker...
            WorkerData d1 = new WorkerData();
            d1.piececount = 1;
            d1.indata = script;
            d1.total = script.Length;
            d1.pos = 0;
            d1.rt = rt;

            w1.DoWork += new DoWorkEventHandler(ScriptWorker_DoWork);
            w1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ScriptWorker_RunWorkerCompleted);
            w1.RunWorkerAsync(d1);

            Console.SetCursorPosition(left, top);
            Console.Write(("100").ToString() + "%");
        }
        else
        {
            WorkerData d1 = new WorkerData();
            d1.piececount = 1;
            d1.indata = script.Substring(0, ControlPos[piece]);
            d1.total = piece;
            d1.pos = 0;
            d1.rt = rt;
            int nextpiece = ControlPos[piece * 2] - ControlPos[piece];
            WorkerData d2 = new WorkerData();
            d2.piececount = 2;
            d2.indata = script.Substring(ControlPos[piece], nextpiece);
            d2.total = piece;
            d2.pos = 0;
            d2.rt = rt;
            nextpiece = ControlPos[piece * 3] - ControlPos[piece * 2];
            WorkerData d3 = new WorkerData();
            d3.piececount = 3;
            d3.indata = script.Substring(ControlPos[piece * 2], nextpiece);
            d3.total = piece;
            d3.pos = 0;
            d3.rt = rt;
            int lastpiece = script.Length - (ControlPos[piece * 3]);
            WorkerData d4 = new WorkerData();
            d4.piececount = 4;
            d4.indata = script.Substring(ControlPos[piece * 3], lastpiece);
            d4.total = lastpiece;
            d4.pos = 0;
            d4.rt = rt;

            w1.DoWork += new DoWorkEventHandler(ScriptWorker_DoWork);
            w1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ScriptWorker_RunWorkerCompleted);
            w1.RunWorkerAsync(d1);
            w2.DoWork += new DoWorkEventHandler(ScriptWorker_DoWork);
            w2.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ScriptWorker_RunWorkerCompleted);
            w2.RunWorkerAsync(d2);
            w3.DoWork += new DoWorkEventHandler(ScriptWorker_DoWork);
            w3.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ScriptWorker_RunWorkerCompleted);
            w3.RunWorkerAsync(d3);
            w4.DoWork += new DoWorkEventHandler(ScriptWorker_DoWork);
            w4.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ScriptWorker_RunWorkerCompleted);
            w4.RunWorkerAsync(d4);
            Console.SetCursorPosition(left, top);
            Console.Write(("100").ToString() + "%");
        }

        Console.WriteLine("\r\nWaiting for build process to complete...");
        left = Console.CursorLeft;
        top = Console.CursorTop;
        int permod = 0;
        while (w1.IsBusy || w2.IsBusy || w3.IsBusy || w4.IsBusy)
        {
            int percent = ((w1.IsBusy) ? 0 : 25) + ((w2.IsBusy) ? 0 : 25) + ((w3.IsBusy) ? 0 : 25) + ((w4.IsBusy) ? 0 : 25) + permod;
            Console.SetCursorPosition(left, top);
            Console.Write("~" + (percent).ToString() + "%");
            permod++;
            if (permod > (100 - percent))
            {
                permod = (100 - percent);
            }
            System.Threading.Thread.Sleep(100);
        }
        Console.SetCursorPosition(left, top);
        Console.Write(("100").ToString() + "%");


        Console.WriteLine("\r\nWriting binary script file...");
        outfile.Clear();
        if (Final.Count > 0)
        {
            for (int c = 1; c < 5; c++)
            {
                foreach (WorkerData wd in Final)
                {
                    if (wd.piececount == c)
                    {
                        outfile.AddRange(wd.outfile);
                        break;
                    }
                }
            }
            if (File.Exists(output))
            {
                File.Delete(output);
            }
            using (FileStream fs = new FileStream(output, FileMode.Append))
            {
                fs.Write(outfile.ToArray(), 0, outfile.Count);
                outfile.Clear();
            }
            Console.Write("File saved: " + output + "\r\n");
        }

        //Console.SetCursorPosition(left, top);
        Console.Write("Process Complete!!\r\n");

        Console.WriteLine("Done!");
        return output;
    }


    public static void DumpScript(string input, string table, string output, int brk)
    {
        Console.WriteLine("Reading Table file...");
        ROMTable rt = ROMTable.LoadTable(table);
        Console.WriteLine("Reading input file...");
        byte[] myfile = File.ReadAllBytes(input);
        string outstring = "";
        List<int> handled = new List<int>();

        Console.WriteLine("Evaluating data...");
        int left = Console.CursorLeft;
        int top = Console.CursorTop;
        for (int c = 0; c < myfile.Length; c++)
        {
            decimal per = ((decimal)(c + 1) / (decimal)(myfile.Length)) * (100);
            Console.SetCursorPosition(left, top);
            Console.Write(((int)per).ToString() + "%");
            if (!(handled.Contains(c)))
            {
                string hx3 = ((c + 2) < myfile.Length) ? myfile[c].ToString("X2") + myfile[c + 1].ToString("X2") + myfile[c + 2].ToString("X2") : "@@";
                string hx2 = ((c + 1) < myfile.Length) ? myfile[c].ToString("X2") + myfile[c + 1].ToString("X2") : "@@";
                string hx1 = myfile[c].ToString("X2");
                string value = "";
                if (rt.HexMap.ContainsKey(hx3))
                {
                    value = rt.HexMap[hx3].AsciiUnicode;
                    handled.AddRange(new int[] { c, c + 1, c + 2 });
                }
                else if (rt.HexMap.ContainsKey(hx2))
                {
                    value = rt.HexMap[hx2].AsciiUnicode;
                    handled.AddRange(new int[] { c + 1, c });
                }
                else if (rt.HexMap.ContainsKey(hx1))
                {
                    value = rt.HexMap[hx1].AsciiUnicode;
                    handled.Add(c);
                }
                else
                {//not in table... dump hex code
                    value = "[" + hx1 + "]";
                    handled.Add(c);
                }
                outstring += (value.Contains(']')) ? value.Replace("\\", "\r\n") : value; //supports new line triggers
            }
        }
        Console.SetCursorPosition(left, top);
        Console.Write("Process Complete!!\r\n");
        Console.WriteLine("Writing Output file...");
        if (brk > 0)
        {//line-break every so many characters
            int lastslen = 0;
            using (StreamWriter sr = new StreamWriter(output))
            {
                for (int b = 0; b < outstring.Length; b += lastslen)
                {
                    string line = GetStringNumBytes(outstring, b, brk, rt);
                    lastslen = (line.Length - 2); //subtract 2 to get the length without the newline flags
                    sr.Write(line);
                }
            }
        }
        else
        {
            File.WriteAllText(output, outstring);
        }
        Console.WriteLine("Done!");
    }

    static string GetStringNumBytes(string instring, int pos, int bytelen, ROMTable rt)
    {
        List<int> handled = new List<int>();
        string outstring = "";
        int totallen = 0;

        for (int c = pos; c < instring.Length; c++)
        {
            if (!(handled.Contains(c)))
            {
                int maxlen = 10;//each string value can be a max of 10 characters long-- otherwise we shan't be looking for it
                if (((pos + (bytelen * 10)) - c) < 10)
                {
                    maxlen = ((pos + (bytelen * 10)) - c);
                }
                if ((c + maxlen > instring.Length))
                {
                    maxlen = instring.Length - c;
                }

                string[] sc = new string[maxlen];
                for (int p = 0; p < maxlen; p++) //look up to 10 characters ahead
                {
                    if (p > 0)
                    {
                        sc[p] = sc[p - 1];
                    }
                    sc[p] += instring[c + p].ToString();
                }

                bool found = false;
                //now check from longest to shortest for a match...
                for (int p = (maxlen - 1); p > -1; p--)
                {
                    if (!found && rt.CharMap.ContainsKey(sc[p]))
                    {
                        found = true;
                        totallen += rt.CharMap[sc[p]].ByteLength;
                        outstring += sc[p];
                        //for (int b = 0; b < rt.CharMap[sc[p]].HexValue.Length; b += 2)
                        //{
                        //    //string val = rt.CharMap[sc[p]].HexValue.Substring(b, 2);
                        //    //outfile.Add(byte.Parse(val, NumberStyles.HexNumber));
                        //}
                    }
                    if (found)
                    {
                        handled.Add(c + p);
                    }
                }
            }
            if (totallen >= bytelen)
            {
                break;
            }
        }
        //outstring.Substring(b, brk) + "\r\n"
        return outstring + "\r\n";
    }

}

