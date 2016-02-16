using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using MBuild.Properties;
using System.Threading;

namespace MBuild
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "MBuild - A Marvelous Translation and Hacking Tool";

            Console.WriteLine("MBuild - " + Application.ProductVersion.ToString().Split('.')[0] + "." + Application.ProductVersion.ToString().Split('.')[1]);
            Console.WriteLine("A Marvelous Translation and Hacking Tool");
            Console.WriteLine("Written by DackR aka Daniel Martin Burgess\r\n");

            if (args.Length == 0)
            {
                Console.WriteLine("No Arguments specified.");
                UseXML("MBuild.MBXML", !Settings.Default.AutoBuild);
            }
            else
            {
                string mArg = args[0].Trim().ToLower();
                switch (mArg)
                {
                    case "build":
                        BuildXML(args);
                        break;
                    case "dump-script":
                        DumpScript(args);
                        break;
                    case "bin-script":
                        BuildScript(args);
                        break;
                    case "comp":
                        CompressFile(args);
                        break;
                    case "decomp":
                        DeCompressFile(args);
                        break;
                    case "bpp-convert":
                        //BuildScript(args);
                        break;
                    case "dmpptr":
                        DumpPointers();
                        break;
                    case "fixsum":
                        FixCheckSum(args);
                        break;
                    case "mbxml-shell":
                        string[] makeArg = new string[] { "-f", "-t", ".mbxml", "-si", Application.ExecutablePath, "-a", Application.ExecutablePath, "-sn", "MBXML File", "-sc", "MBuild_XML_File" };
                        //string[] assocArg = new string[] { "-f", "-t", ".txt", "-associate", Application.ExecutablePath };
                        CTypes.Main(makeArg);
                        //CTypes.Main(assocArg);
                        //FileAssociation.SetAssociation(".mbxml", "MBuild_XML_File", Application.ExecutablePath, "MBXML File", args);
                        break;
                    case "ips":
                        MakeIPS(args);
                        break;
                    case "xdelta":
                        MakexDelta(args);
                        break;
                    case "extract"://used to extract RAW binary data from a file
                        ExtractBin(args);
                        break;
                    case "dmpdata":
                        //Dump data located at the various pointers
                        DumpData(args);
                        break;
                    case "bm5dump":
                        SBM5.DumpDataFromPointerTab(@"F:\GoogleDrive\SBM5\Base\Base.sfc", @"F:\GoogleDrive\SBM5\Dump\");
                        break;
                    case "test":
                        string romfile = @"D:\GoogleDrive\SuperFamicomWars\base.sfc";
                        byte[] data = File.ReadAllBytes(romfile);
                        MemoryStream ms = LZSS.Decompress(data);
                        byte[] dcdata = ms.ToArray();
                        break;
                    default:
                        if (mArg.ToLower().Contains(".mbxml") && File.Exists(mArg))
                        {//detected xml file specified... Try to build.
                            UseXML(mArg, false);
                            //Console.WriteLine(mArg);
                            //Console.ReadKey(true);
                        }
                        break;
                }


            }
            LCompress.LunarCompressCleanup();
            xDelta.xDeltaCleanup();
            bool wait = false;
            Console.Write("Press any key to exit (\"q\" to stop count-down)");
            DateTime beginWait = DateTime.Now;
            while (!Console.KeyAvailable && DateTime.Now.Subtract(beginWait).TotalSeconds < 3)
            {
                Console.Write(".");
                Thread.Sleep(250);
            }
            if (Console.KeyAvailable && (Console.ReadKey(true).Key == ConsoleKey.Q))
            {
                wait = true;
            }
            if (wait)
            {
                Console.CursorLeft = 0;
                Console.Write(("").PadLeft(79, ' '));
                Console.CursorLeft = 0;
                Console.Write("Press any key to exit...");
                Console.ReadKey(true);
            }
            
            

        }


        static void ExtractBin(string[] args)
        {
            string input = "";
            string offset = "";
            string length = "";
            string endoffset = "";
            string output = "";
            Console.WriteLine("Parsing arguments...");
            foreach (string a in args)
            {
                string ar = a.Trim().ToLower();
                if (ar.StartsWith("/input:"))
                {
                    int idx = ar.IndexOf(':') + 1;
                    input = ar.Substring(idx);
                }
                else if (ar.StartsWith("/offset:"))
                {
                    int idx = ar.IndexOf(':') + 1;
                    offset = ar.Substring(idx);
                }
                else if (ar.StartsWith("/length:"))
                {//if both length and endoffset are supplied, endoffset is ignored
                    int idx = ar.IndexOf(':') + 1;
                    length = ar.Substring(idx);
                }
                else if (ar.StartsWith("/endoffset:"))
                {
                    int idx = ar.IndexOf(':') + 1;
                    endoffset = ar.Substring(idx);
                }
                if (ar.StartsWith("/output:"))
                {
                    int idx = ar.IndexOf(':') + 1;
                    output = ar.Substring(idx);
                }
            }
            if (input.Length > 0 && offset.Length > 0 && (length.Length > 0 || endoffset.Length > 0 ))
            {
                int os = int.Parse(offset, NumberStyles.HexNumber);
                int len = 0;
                if (length.Length > 0)
                {
                    if (length.ToLower().StartsWith("0x") || length.ToLower().StartsWith("x"))
                    {
                        length = length.Replace("0x", "").Replace("x", "");
                        len = int.Parse(length, NumberStyles.HexNumber);
                    }
                    else
                    {
                        len = int.Parse(length);
                    }
                    
                }
                else
                {
                    int eo = int.Parse(endoffset, NumberStyles.HexNumber);
                    len = eo - os;
                }

                Extract(input, output, os, len);
            }
            else
            {
                Console.WriteLine("No Input file specified.");
            }
        }

        static void Extract(string input, string output, int offset, int length)
        {
            if (output.Length == 0)
            {
                output = Path.GetDirectoryName(input) + "\\" + Path.GetFileNameWithoutExtension(input) + "_0x" + offset.ToString("X") + ".bin";
            }
            byte[] infile = File.ReadAllBytes(input);
            if (offset + length > infile.Length)
            {
                Console.WriteLine("Length would read past EOF. Truncated.");
                length = infile.Length - offset;
            }
            byte[] outfile = new byte[length];
            Array.Copy(infile, offset, outfile, 0, length);
            File.WriteAllBytes(output, outfile);
        }

        static void MakexDelta(string[] args)
        {
            string input = "";
            string input2 = "";
            string output = "";
            Console.WriteLine("Parsing arguments...");
            foreach (string a in args)
            {
                string ar = a.Trim().ToLower();
                if (ar.StartsWith("/original:"))
                {
                    int idx = ar.IndexOf(':') + 1;
                    input = ar.Substring(idx);
                }
                else if (ar.StartsWith("/modified:"))
                {
                    int idx = ar.IndexOf(':') + 1;
                    input2 = ar.Substring(idx);
                }
                if (ar.StartsWith("/output:"))
                {
                    int idx = ar.IndexOf(':') + 1;
                    output = ar.Substring(idx);
                }
            }
            if (input.Length > 0 && input2.Length > 0)
            {
                xDelta.Make(input, input2, output);
            }
            else
            {
                Console.WriteLine("No Input file specified.");
            }
        }

        static void MakeIPS(string[] args)
        {
            string input = "";
            string input2 = "";
            string output = "";
            Console.WriteLine("Parsing arguments...");
            foreach (string a in args)
            {
                string ar = a.Trim().ToLower();
                if (ar.StartsWith("/original:"))
                {
                    int idx = ar.IndexOf(':') + 1;
                    input = ar.Substring(idx);
                }
                else if (ar.StartsWith("/modified:"))
                {
                    int idx = ar.IndexOf(':') + 1;
                    input2 = ar.Substring(idx);
                }
                if (ar.StartsWith("/output:"))
                {
                    int idx = ar.IndexOf(':') + 1;
                    output = ar.Substring(idx);
                }
            }
            if (input.Length > 0 && input2.Length > 0)
            {
                IPS.Create(input, input2, output);
            }
            else
            {
                Console.WriteLine("No Input file specified.");
            }
        }

        static void FixCheckSum(string[] args)
        {
            string input = "";
            Console.WriteLine("Parsing arguments...");
            foreach (string a in args)
            {
                string ar = a.Trim().ToLower();
                if (ar.StartsWith("/input:")) //the SNES ROM file
                {
                    int idx = ar.IndexOf(':') + 1;
                    input = ar.Substring(idx);
                }
            }
            if (input.Length > 0 && File.Exists(input))
            {
                //ensure ROM is a valid size...
                PadROM(File.ReadAllBytes(input), input);
                SNESChecksum.FixROM(input);
            }
            else
            {
                Console.WriteLine("Invalid argument. No input file specified or file does not exist.");
                return;
            }
        }

        //new function- Dump (compressed and non-compressed) data from the ROM
        static void DumpData(string[] args)
        {
            string input = "";
            string output = "";
            string addrtype = "snes";
            string type = "";
            Console.WriteLine("Parsing arguments...");
            foreach (string a in args)
            {
                string ar = a.Trim().ToLower();
                if (ar.StartsWith("/input:")) //the ROM file
                {
                    int idx = ar.IndexOf(':') + 1;
                    input = ar.Substring(idx);
                }
                else if (ar.StartsWith("/type:")) //a,b,c (a=compressed, b=raw, c=compressed and raw data)
                {
                    int idx = ar.IndexOf(':') + 1;
                    type = ar.Substring(idx);
                }
                else if (ar.StartsWith("/output:")) //output folder
                {
                    int idx = ar.IndexOf(':') + 1;
                    output = ar.Substring(idx);
                }
                else if (ar.StartsWith("/pc"))
                {
                    addrtype = "pc";
                }
                else if (ar.StartsWith("/snes"))
                {
                    addrtype = "snes";
                }
            }
            Marvelous.DumpIndexedFiles(input, addrtype, type, output);
        }

        //new function- allow me to compress files!!
        static void CompressFile(string[] args)
        {
            string input = "";
            string output = "";
            string type = "";
            Console.WriteLine("Parsing arguments...");
            foreach (string a in args)
            {
                string ar = a.Trim().ToLower();
                if (ar.StartsWith("/input:"))
                {
                    int idx = ar.IndexOf(':') + 1;
                    input = ar.Substring(idx);
                }
                else if (ar.StartsWith("/type:"))
                {
                    int idx = ar.IndexOf(':') + 1;
                    type = ar.Substring(idx);
                }
                else if (ar.StartsWith("/output:"))
                {
                    int idx = ar.IndexOf(':') + 1;
                    output = ar.Substring(idx);
                }
            }
            OutputCompressedFile(input, output, type);
        }

        //new function- allow me to decompress files (or areas in files)...but just files for now!!
        static void DeCompressFile(string[] args)
        {
            string input = "";
            string output = "";
            string type = "";
            int lztype = -1;
            string rletype = "";
            string offs = "";
            int offset = 0;
            string len = "";
            int length = -1;
            Console.WriteLine("Parsing arguments...");
            foreach (string a in args)
            {
                string ar = a.Trim().ToLower();
                if (ar.StartsWith("/input:"))
                {
                    int idx = ar.IndexOf(':') + 1;
                    input = ar.Substring(idx);
                }
                else if (ar.StartsWith("/type:"))
                {
                    int idx = ar.IndexOf(':') + 1;
                    type = ar.Substring(idx).ToLower();
                    if (type.StartsWith("lz"))
                    {
                        string t = type.Replace("lz", "");
                        lztype = (int)LCompress.GetLZType(t);
                    }
                }
                else if (ar.StartsWith("/output:"))
                {
                    int idx = ar.IndexOf(':') + 1;
                    output = ar.Substring(idx);
                }
            }
            OutputDecompressedFile(input, output, type, lztype, offset, length);
        }

        static void OutputDecompressedFile(string input, string output, string type, int lztype, int offset, int length)
        {
            if (File.Exists(input))
            {
                if (!(output == ""))
                {
                    byte[] file = File.ReadAllBytes(input);

                    if (lztype > -1)
                    {//then use lunar compress...
                        Console.WriteLine("Commencing decompression with Lunar Compress Type " + lztype.ToString() + "...");
                        byte[] d1 = LCompress.Decompress(file, offset, 0x10000, (uint)lztype);
                        byte[] d2 = new byte[LCompress.LastDecompressedSize];
                        Array.Copy(d1, d2, LCompress.LastDecompressedSize);
                        File.WriteAllBytes(output, d2);
                    }
                    else
                    {
                        switch (type.ToLower())
                        {
                            case "sbm5":
                                Console.WriteLine("Commencing decompression with Super Bomberman 5 RLE Type...");
                                BM5RLE.Decompress(file, length, output);
                                break;
                            case "sfcw":
                                Console.WriteLine("Commencing decompression with Super Famicom Wars RLE Type...");
                                byte[] data = SFCWRLE.Decompress(file);
                                File.WriteAllBytes(output, data);
                                break;
                            default:
                                Console.WriteLine("Type argument was invalid. Failed.");
                                break;
                        }

                    }
                }
                else
                {
                    Console.WriteLine("Missing output file argument. Failed.");
                    //message - output file required
                }
            }
            else
            {
                Console.WriteLine("Invalid input file argument. Failed.");
                //message- input file required
            }
        }

        static void OutputCompressedFile(string input, string output, string type)
        {
            if (File.Exists(input))
            {
                if (!(output == ""))
                {
                    int lztype = -1;
                    try
                    {//if there is no error, then its an LZ type (Lunar)
                        lztype = int.Parse(type);
                        byte[] file = File.ReadAllBytes(input);
                        byte[] outbyte = null;
                        LCompress.Compress(file, out outbyte, LCompress.GetLZType(type));
                        File.WriteAllBytes(output, outbyte);
                    }
                    catch
                    {
                        switch (type.ToLower())
                        {
                            case "sbm5":
                                Console.WriteLine("Commencing Compression with Super Bomberman 5 RLE Type...");
                                BM5RLE.Compress(input, output);
                                break;
                            case "sfcw":
                                Console.WriteLine("Commencing Compression with Super Famicom Wars LZ/RLE Type...");
                                byte[] data = SFCWRLE.Compress(File.ReadAllBytes(input));
                                File.WriteAllBytes(output, data);
                                break;
                            default:
                                Console.WriteLine("Type argument was invalid. Failed.");
                                break;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Missing output file argument. Failed.");
                    //message - output file required
                }
            }
            else
            {
                Console.WriteLine("Invalid input file argument. Failed.");
                //message- input file required
            }
        }

        static void BuildXML(string[] args)
        {
            string xmlfile = "";
            Console.WriteLine("Started ROM File Build process...");
            foreach (string a in args)
            {
                string ar = a.Trim().ToLower();
                if (ar.StartsWith("/xmlfile:"))
                {
                    int idx = ar.IndexOf(':') + 1;
                    xmlfile = ar.Substring(idx);
                }
            }
            if (xmlfile == "" || !File.Exists(xmlfile))
            {
                Console.WriteLine("Invalid or missing arguments. Build process failed.");
                return;
            }
            UseXML(xmlfile, false);
        }

        static void BuildScript(string[] args)
        {
            string input = "";
            string output = "";
            string table = "";
            Console.WriteLine("Parsing arguments...");
            foreach (string a in args)
            {
                string ar = a.Trim().ToLower();
                if (ar.StartsWith("/input:"))
                {
                    int idx = ar.IndexOf(':') + 1;
                    input = ar.Substring(idx);
                }
                else if (ar.StartsWith("/table:"))
                {
                    int idx = ar.IndexOf(':') + 1;
                    table = ar.Substring(idx);
                }
                else if (ar.StartsWith("/output:"))
                {
                    int idx = ar.IndexOf(':') + 1;
                    output = ar.Substring(idx);
                }
            }
            Script.BuildScriptThread(input, output, table);
        }

        //TODO: expand to support params
        //DONE: auto-convert to PC address from HIROM (just subtract C0 from the bank byte)
        //TODO: support comments in csv format
        static void DumpPointers()
        {
            string folder = @"D:\GoogleDrive\Marvelous\~EngBuildFiles\BIN DATA\";
            string bankByteFile = folder + @"x11E3_(Highest_Byte)Bank_Pointers.bin";
            string highByteFile = folder + "x12DC_High_Byte_Pointers.bin";
            string lowByteFile = folder + "x13D5_Low_Byte_Pointers.bin";

            string outfile = "testout.txt";
            string outdata = "";

            byte[] bank = File.ReadAllBytes(bankByteFile);
            byte[] high = File.ReadAllBytes(highByteFile);
            byte[] low = File.ReadAllBytes(lowByteFile);

            if ((bank.Length == high.Length && high.Length == low.Length))
            {
                for (int c = 0; c < bank.Length; c++)
                {
                    byte bnk = Convert.ToByte(Convert.ToInt32(bank[c]) - 192);      // Converts HIROM to PC
                    outdata += bnk.ToString("X2") + high[c].ToString("X2") + low[c].ToString("X2") + "\r\n";  
                }
                if (outdata.Length > 0)
                {
                    File.WriteAllText(folder + outfile, outdata);
                }
            }
            else
            {
                Console.WriteLine("Pointer files not equal lengths.");
            }


        }

        static void DumpScript(string[] args)
        {
            string input = "";
            string output = "";
            string table = "";
            int brk = -1;
            Console.WriteLine("Script Dump Started...");
            foreach (string a in args)
            {
                string ar = a.Trim().ToLower();
                if (ar.StartsWith("/input:"))
                {
                    int idx = ar.IndexOf(':') + 1;
                    input = ar.Substring(idx);
                }
                else if (ar.StartsWith("/table:"))
                {
                    int idx = ar.IndexOf(':') + 1;
                    table = ar.Substring(idx);
                }
                else if (ar.StartsWith("/brk:"))
                {
                    int idx = ar.IndexOf(':') + 1;
                    brk = int.Parse(ar.Substring(idx));
                }
                else if (ar.StartsWith("/output:"))
                {
                    int idx = ar.IndexOf(':') + 1;
                    output = ar.Substring(idx);
                }
            }

            if (input == "" || table == "" || !File.Exists(input) || !File.Exists(table))
            {
                Console.WriteLine("Invalid or missing arguments. Failed.");
                return;
            }
            else if (output == "")
            {
                //generate output name based on the input
                output = Path.GetDirectoryName(input) + "\\" + Path.GetFileNameWithoutExtension(input);
            }
            output = output.Split('.')[0] + "_" + DateTime.Now.ToString("yyyyMMdd") + ".txt";

            Script.DumpScript(input, table, output, brk);
        }

        static void UseXML(string file, bool ask)
        {
            bool cont = true;
            if (ask)
            {
                Console.WriteLine("Do you wish to attempt a build with '" + file + "'? [Y/n]");
                if (!(Console.ReadKey(true).Key == ConsoleKey.Y || Console.ReadKey(true).Key == ConsoleKey.Spacebar || Console.ReadKey(true).Key == ConsoleKey.Enter))
                {
                    cont = false;
                }
            }
            if (cont)
            {
                if (File.Exists(file))
                {
                    ReadXML(file);
                }
                else
                {
                    NoArgs();
                    ShowArgs();
                }
            }
            else
            {
                ShowArgs();
            }
        }
        static void ShowArgs()
        {
            //show arguments...
            //marvelous build [/xml:config-file]
            //marvelous build [/path:path-to-file] [/name:output-file-name] [/original:rom-file] [/bin:name-format]
            //marvelous brute-sniff
            //marvelous dump-script [/input:rom-file] [/table:table-file] [/output:text-file]
            //marvelous script-bin [/input:text-file] [/table:table-file] [/output:bin-file]

            /* types:
             * build=       build your ROM file using a collection of files to insert, 
             *              replace, and compress data 
             * brute-sniff= similar to the sniff function included with Lunar Compress, 
             *              only if the compression type is not known-- this will try 
             *              each type and dump the resulting data to it's own folder 
             *              for analysis (has options for auto multiple formats at once)
             * dump-bin=    remove a specific region of a rom file that can be modified 
             *              and copied back into the ROM during a typical build process
             * decomp=      use specific type of decompression on the specific area within
             *              the specified file
             * dump-script= takes the source file and a table file as arguments. utilizes
             *              advanced table options to allow correct dumping of control codes.
             * bin-script=  takes a text script file and a table file as arguments. This
             *              allows the user to re-insert the script, or to make a binary
             *              file containing the script so that it can be inserted later.
             * comp=        use specific type of compression from a certain input file
             *              to produce a compressed output file
             */
        }
        static void NoArgs()
        {
            Console.WriteLine("XML File not Specified/Found.");
        }

        struct Build
        {
            public string original; //original source ROM
            public string name; //used to generate the output name
            public string version; // also used to name the output
            public int revision; // revision is added to the end of the file name
            public string revbyteloc; // if this is set, the revision header byte will be set at this location (need to depreciate?)
            public string pad; //if this is true, the SNES ROM will be padded to the next most common size (if size if larger than original) -- Checksum will also be recalculated
            public string path; //path of files
            public string diff;
            public List<section> buildSections; //all the different build types...
        }
        static Build DefaultB()
        {
            Build b = new Build();
            b.original = "";
            b.name = "";
            b.path = "";
            b.pad = "false";
            b.revision = 0;
            b.revbyteloc = "";
            b.diff = "";
            b.buildSections = new List<section>();
            return b;
        }
        struct section
        {
            public string path;
            public int offset;
            public string type;
            public string table;
            public string LZType;
        }

        static section DefaultS()
        {
            section s = new section();
            s.offset = -1;
            s.path = "";
            s.type = "";
            s.LZType = "";
            s.table = "";
            return s;
        }

        static void ReadXML(string file)
        {
            DateTime start = DateTime.Now;
            Build BUILD = new Build();
            using (XmlTextReader reader = new XmlTextReader(file))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.HasAttributes)
                        {
                            switch (reader.Name.ToLower())
                            {
                                case "build":
                                    //has this been defined already?... meh
                                    BUILD = BuildMain(reader, file);
                                    break;
                                case "lzr": //compress and replace
                                    section tlzr = BuildSection(reader, "lzr");
                                    BUILD.buildSections.Add(tlzr);
                                    break;
                                case "lzi": //compress and insert
                                    section tlzi = BuildSection(reader, "lzi");
                                    BUILD.buildSections.Add(tlzi);
                                    break;
                                case "rlr": //compress and replace
                                    section trlr = BuildSection(reader, "rlr");
                                    BUILD.buildSections.Add(trlr);
                                    break;
                                case "rli": //compress and insert
                                    section trli = BuildSection(reader, "rli");
                                    BUILD.buildSections.Add(trli);
                                    break;
                                case "rep": //overwrite existing data
                                    section trep = BuildSection(reader, "rep");
                                    BUILD.buildSections.Add(trep);
                                    break;
                                case "ins": //insert data
                                    section tins = BuildSection(reader, "ins");
                                    BUILD.buildSections.Add(tins);
                                    break;
                                case "bpr": //bitplane conversion, overwrite existing data
                                    section tbpr = BuildSection(reader, "bpr");
                                    BUILD.buildSections.Add(tbpr);
                                    break;
                                case "bpi": //bitplane conversion, insert data
                                    section tbpi = BuildSection(reader, "bpi");
                                    BUILD.buildSections.Add(tbpi);
                                    break;
                                case "sbr": // script build replace
                                    section tsbr = BuildSection(reader, "sbr");
                                    BUILD.buildSections.Add(tsbr);
                                    break;
                                case "sbi": //script build insert
                                    section tsbi = BuildSection(reader, "sbi");
                                    BUILD.buildSections.Add(tsbi);
                                    break;

                            }
                            reader.MoveToElement();
                        }
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement)
                    {//closing the element... we will perform the build.
                        if (reader.Name.ToLower() == "build")
                        {
                            Console.WriteLine("Verifying and building...");
                            RunFullBuild(BUILD);
                            Console.WriteLine("Build Complete. (Total Time: "  + GetElapsedTime(start) + ")");
                        }
                    }
                }
            }
        }

        static string GetElapsedTime(DateTime start)
        {
            TimeSpan span = DateTime.Now - start;
            int ms = (int)span.TotalMilliseconds;
            return ms.ToString() + "ms";
        }

        static void RunFullBuild(Build b)
        {
            string date = DateTime.Now.ToString("yyyyMMdd");
            string path = Path.GetDirectoryName(Application.ExecutablePath);
            //check for existing path
            if (Directory.Exists(b.path))
            {
                path = b.path;
            }
            if (!path.EndsWith(@"\")) path += @"\";
        
            string rev = b.revision.ToString().PadLeft(2, '0');
            string extension = Path.GetExtension(path + b.original);
            string outfile = b.name + "_" + b.version + "." + rev + extension;
            

            
            if (File.Exists(path + b.original))
            {
                //int tries = 0;
                string trypath = path + outfile;
                while (File.Exists(trypath))
                {
                    if (!TryDelete(trypath))
                    {
                        Console.WriteLine("Error: Unable to overwrite existing file. Process aborted.");
                    }
                }
                //make sure it doesnt exist... now copy the source file
                File.Copy(path + b.original, trypath);

                FinalizeBuild(b, trypath, path);
            }
            else
            {
                Console.WriteLine("Path to Original source file not found.");
            }
        }

        static bool TryDelete(string path)
        {
            bool done = false;
            try
            {
                File.Delete(path);
                done = true;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message + "\r\nTry Writing File Again? [Y]/n");
                if (!(Console.ReadKey(true).KeyChar.ToString().ToLower() == "n"))
                {
                    return TryDelete(path);
                }
            }
            return done;
        }

        static int[] ROMMult = new int[] { 0x25000, 0x50000, 0x100000, 0x200000, 0x300000, 0x400000, 0x50000, 0x60000, 0x70000, 0x80000 }; //Up to 64Mbit

        static void FinalizeBuild(Build b, string outfile, string outfolder)
        {
            DateTime start = DateTime.Now;
            foreach (section s in b.buildSections)
            {
                if (File.Exists(s.path))
                {
                    BuildSection(outfile, s.path, b.path, s);        
                }
                else
                {
                    if (s.path.Contains("|"))
                    {
                        string[] files = s.path.Split('|');
                        bool allgood = true;
                        string allfiles = "";
                        foreach (string fi in files)
                        {
                            if (!File.Exists(outfolder + fi))
                            {
                                allgood = false;
                                Console.WriteLine(s.path + " - File does not exist.");
                            }
                            else
                            {
                                allfiles += (allfiles == "") ? outfolder + fi : "|" + outfolder + fi; 
                            }
                        }
                        if (allgood) BuildSection(outfile, allfiles, b.path, s);
                    }
                    else
                    {
                        if (File.Exists(outfolder + s.path))
                        {
                            BuildSection(outfile, outfolder + s.path, b.path, s);
                        }
                        else
                        {
                            Console.WriteLine(s.path + " - File does not exist.");
                        }
                    }
                    
                }
            }

            if (!(b.revbyteloc == ""))
            {
                int offset = 0;
                try
                {
                    offset = int.Parse(b.revbyteloc, NumberStyles.HexNumber);
                }
                catch { Console.WriteLine("Invalid Revision Offset HEX value."); }

                if (offset > 0)
                {
                    byte[] newfile = File.ReadAllBytes(outfile);
                    newfile[offset] = Convert.ToByte(b.revision);
                    File.WriteAllBytes(outfile, newfile);
                }
            }
            Console.WriteLine("Build Sub-Sections Complete (Elapsed: " + GetElapsedTime(start) + ")");

            if (b.pad == "true")
            {
                //now check to see if the ROM file has grown larger.
                byte[] newfile = File.ReadAllBytes(outfile);
                byte[] oldfile = File.ReadAllBytes(outfolder + b.original);
                if (newfile.Length > oldfile.Length)
                {//if so, then pad the file to the next Size
                    PadROM(newfile, outfile);
                }

                //recalculate checksum
                SNESChecksum.FixROM(outfile);
            }

            //make xdelta and ips patches if specified...
            if (b.diff.Split('|').Contains("xdelta"))
            {
                xDelta.Make(outfolder + b.original, outfile);
            }
            if (b.diff.Split('|').Contains("ips"))
            {
                IPS.Create(outfolder + b.original, outfile);
            }

        }

        static void PadROM(byte[] newfile, string outfile)
        {
            for (int m = 0; m < ROMMult.Length; m++)
            {
                int sizer = ROMMult[m];
                if (sizer > newfile.Length)
                {
                    byte[] final = FillArray(new byte[sizer]);
                    LCompress.CopyBytes(newfile, final, true);
                    File.WriteAllBytes(outfile, final);
                    break;
                }
            }
        }

        static byte[] FillArray(byte[] input)
        {
            byte[] output = new byte[input.Length];
            for (int c = 0; c < input.Length; c++)
            {
                output[c] = 255;
            }
            return output;
        }

        static uint GetLZType(section s)
        {
            return LCompress.GetLZType(s.LZType);
        }

        static byte[] MergeFiles(string[] files)
        {
            byte[] finalbyte = null;
            List<byte[]> allbyte = new List<byte[]>();
            int totLen = 0;
            foreach (string fi in files)
            {
                byte[] sect = File.ReadAllBytes(fi);
                totLen += sect.Length;
                allbyte.Add(sect);
            }
            finalbyte = new byte[totLen];
            int pos = 0;
            foreach (byte[] by in allbyte)
            {   
                for (int c = 0; c < by.Length; c++)
                {
                    finalbyte[c + pos] = by[c];
                }
                pos += by.Length;
            }
            return finalbyte;
        } 

        static void BuildSection(string outfile, string sectfile, string buildpath, section s)
        {
            byte[] sect = null;
            if (sectfile.Contains("|"))
            {
                string[] fis = sectfile.Split('|');
                Console.Write("Evaluating files: ");
                for(int m = 0; m < fis.Length; m++)
                {
                     Console.Write(((m==0) ? "" : " & ") + Path.GetFileName(fis[m]));
                }
                Console.Write("...\r\n");
                sect = MergeFiles(sectfile.Split('|'));
            }
            else
            {
                Console.WriteLine("Evaluating file: " + Path.GetFileName(sectfile) + "...");
                sect = File.ReadAllBytes(sectfile);
            }
            byte[] outbyte = null;
            string tablepath = Path.Combine(buildpath, s.table);
            string[] args = new string[] { };
            string temp = "";
            switch (s.type)
            {
                case "lzr"://use lunar compress and replace data
                    LCompress.Compress(sect, out outbyte, GetLZType(s));
                    ReplaceSection(outfile, outbyte, s.offset);
                    break;
                case "lzi"://use lunar compress and insert data
                    LCompress.Compress(sect, out outbyte, GetLZType(s));
                    InsertSection(outfile, outbyte, s.offset);
                    break;
                case "rep"://replace raw data
                    ReplaceSection(outfile, sect, s.offset);
                    break;
                case "ins"://insert raw data
                    InsertSection(outfile, sect, s.offset);
                    break;
                case "bpr"://bitplane convert and replace
                    outbyte = ConvertBPP(sect, s.LZType);
                    ReplaceSection(outfile, outbyte, s.offset);
                    break;
                case "bpi"://bitplane convert and insert
                    outbyte = ConvertBPP(sect, s.LZType);
                    InsertSection(outfile, outbyte, s.offset);
                    break;
                case "rlr"://rle compression and replace
                    outbyte = RLECompression(sect, s.LZType);
                    ReplaceSection(outfile, outbyte, s.offset);
                    break;
                case "rli"://rle compression and insert
                    outbyte = RLECompression(sect, s.LZType);
                    InsertSection(outfile, outbyte, s.offset);
                    break;
                case "sbi"://script build and insert
                    temp = Script.BuildScriptThread(sectfile, "sctemp", tablepath);
                    sect = File.ReadAllBytes(temp);
                    File.Delete(temp);
                    InsertSection(outfile, sect, s.offset);
                    break;
                case "sbr"://script build and replace
                    temp = Script.BuildScriptThread(sectfile, "sctemp", tablepath);
                    sect = File.ReadAllBytes(temp);
                    File.Delete(temp);
                    ReplaceSection(outfile, sect, s.offset);
                    break;
            }

        }


        static byte[] RLECompression(byte[] indata, string type)
        {
            switch (type.ToUpper())
            {
                case "BM5":
                    return BM5RLE.Compress(indata);
                case "SFCW":
                    return SFCWRLE.Compress(indata);
                default:
                    return BM5RLE.Compress(indata);
            }
        }

        static byte[] ConvertBPP(byte[] indata, string type)
        {
            switch (type)
            {
                case "2-1IL":
                    return bppTool.Convert2BPPto1BPPIL(indata);
                case "1IL-2" :
                    return bppTool.Convert1BPPILto2BPP(indata);
                default :
                    return bppTool.Convert1BPPILto2BPP(indata);
            }
        }

        static void ReplaceSection(string outfile, byte[] data, int offset)
        {
            using (FileStream fs = new FileStream(outfile, FileMode.Open))
            {
                fs.Seek(offset, SeekOrigin.Begin);
                fs.Write(data, 0, data.Length);
            }
        }

        static void InsertSection(string outfile, byte[] data, int offset)
        {
            byte[] outf = File.ReadAllBytes(outfile);
            //outf = InsertBytes(outf, data, offset);
            int reqlen = (offset + data.Length);
            byte[] temp = new byte[outf.Length];
            if (!((outf.Length - reqlen) >= 0))
            {
                temp = new byte[reqlen];
            }
            LCompress.CopyBytes(outf, temp, true);
            File.WriteAllBytes(outfile, temp);
            ReplaceSection(outfile, data, offset);
        }

        static Build BuildMain(XmlTextReader r, string file)
        {
            Build b = DefaultB();
            while (r.MoveToNextAttribute())
            {
                switch (r.Name.ToLower())
                {
                    case "name":
                        b.name = r.Value;
                        break;
                    case "original":
                        b.original = r.Value;
                        break;
                    case "version":
                        b.version = r.Value;
                        break;
                    case "pad":
                        b.pad = r.Value.ToLower();
                        break;
                    case "diff":
                        b.diff = r.Value.ToLower();
                        break;
                    case "revision":
                        try
                        {
                            b.revision = int.Parse(r.Value);
                        }
                        catch { Console.WriteLine("Invalid Revision value."); }
                        break;
                    case "revbyteloc": // need to do header detection of hirom/lorom and then just set this byte automatically
                        b.revbyteloc = r.Value;
                        break;
                    case "path":
                        try
                        {//path is relative to the XML file!!
                            Directory.SetCurrentDirectory(Path.GetDirectoryName(file));
                        }
                        catch { } //otherwise relative to the executable 
                        b.path = Path.GetFullPath(r.Value);
                        break;
                }
            }
            return b;
        }

        static section BuildSection(XmlTextReader r, string type)
        {
            section s = DefaultS();
            s.type = type;
            while (r.MoveToNextAttribute())
            {
                switch (r.Name.ToLower())
                {
                    case "offset":
                        try
                        {
                            s.offset = int.Parse(r.Value, NumberStyles.HexNumber);
                        }
                        catch { Console.WriteLine("Offset invalid HEX value or not specified."); }
                        break;
                    case "file":
                        s.path = r.Value;
                        break;
                    case "table":
                        s.table = r.Value;
                        break;
                    case "lztype":
                        s.LZType = r.Value;
                        break;
                    case "bptype": //uses the same property as lztype
                        s.LZType = r.Value;
                        break;
                    case "rletype":
                        s.LZType = r.Value;
                        break;
                    case "type":
                        s.LZType = r.Value;
                        break;
                }
            }
            return s;
        }


    }
}
