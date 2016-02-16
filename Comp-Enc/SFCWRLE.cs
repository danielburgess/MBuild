using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


class SFCWRLE
{

    private static bool DEBUG = false;

    public enum EncMethod
    {//trying to optimize compression... STEP is good... RANDOM pretty much sucks... LONG will eventually be the best, but it is sucky slow right now
        STEP, RANDOM, LONG
    }

    public static byte[] Compress(byte[] oB)
    {
        return Compress(oB, EncMethod.STEP);
    }

    public static byte[] Compress(byte[] oB, EncMethod method)
    {

        byte[] cB = new byte[oB.Length];
        int idx = 0;
        int step = 1;
        int lc = 0;
        for (int c = 0; c < oB.Length; c += step)
        {
            lc = c;

            SearchResult sM = FindMatchFinal(oB, c);
            int repcnt = GetRepeating(oB, c);
            int patcnt = GetPattern(oB, c);
            int altcnt1 = GetAltPattern(oB, c, 0);
            int altcnt2 = GetAltPattern(oB, c, 1);
            if (!DEBUG)
            {
                if (Console.CursorLeft > 35)
                {
                    Console.CursorLeft = 0;
                    Console.Write("                                     ");
                    Console.CursorLeft = 0;
                }
                Console.Write(".");
            }
            switch (method)
            {
                case EncMethod.LONG:
                    LongEncoding(ref sM, ref repcnt, ref patcnt, ref altcnt1, ref altcnt2, ref oB, ref step, ref c, ref idx, ref cB);
                    break;
                case EncMethod.RANDOM:
                    RandomEncoding(ref sM, ref repcnt, ref patcnt, ref altcnt1, ref altcnt2, ref oB, ref step, ref c, ref idx, ref cB);
                    break;
                default:
                case EncMethod.STEP:
                    //using encoding method that favors the highest immediately encodable characters
                    StepEncoding(ref sM, ref repcnt, ref patcnt, ref altcnt1, ref altcnt2, ref oB, ref step, ref c, ref idx, ref cB);
                    break;
            }
            

        }
        //the final byte
        cB[idx] = 0xFF;
        byte[] finalC = new byte[idx + 1];
        Array.Copy(cB, 0, finalC, 0, idx + 1);

        Console.WriteLine("\r\nOriginal File Size: 0x" + oB.Length.ToString("X") + " (" + oB.Length + ") bytes");
        Console.WriteLine("Compressed File Size: 0x" + finalC.Length.ToString("X") + " (" + finalC.Length + ") bytes");

        return finalC;
        //Decompress(finalC, 0, 0, false, "test.bin", false);
        //File.WriteAllBytes(@"sfc_0xECDB4_rle.bin", finalC);

    }

    static void RandomEncoding(ref SearchResult sM, ref int repcnt, ref int patcnt, ref int altcnt1, ref int altcnt2, ref byte[] oB, ref int step, ref int c, ref int idx, ref byte[] cB)
    {
        Random n = new Random(step);
        int mr = n.Next(1, 500);
        n = new Random(mr + DateTime.Now.Millisecond);
        mr = n.Next(1, 10);

        switch (mr)
        {
            default:
            case 1:
                if (sM.length > 3)
                {//$bcd2
                    step = EncodeMatch(oB, c, ref sM, ref cB, ref idx, true);
                }
                else if (repcnt > 3)
                {//$bd77
                    WriteRepeat(oB, c, ref cB, ref idx, repcnt);
                    step = repcnt;
                }
                else if (patcnt > 0)
                {//$bc13
                    WritePattern(oB, c, ref cB, ref idx, patcnt);
                    step = (patcnt * 2);
                }
                else if (altcnt1 > 0)
                {
                    step = WriteFirstPattern(oB, c, ref cB, ref idx, altcnt1);
                }
                else if (altcnt2 > 0)
                {
                    step = WriteSecondPattern(oB, c, ref cB, ref idx, altcnt2);
                }
                else
                {
                    int cnt = GetNextPattern(oB, c);
                    if (cnt > 0)
                    {
                        WritePlainBytes(oB, c, ref cB, ref idx, cnt);
                        step = cnt;
                    }
                    else
                    {
                        step = 1;
                    }
                }
                break;
            case 2:
                if (repcnt > 3)
                {//$bd77
                    WriteRepeat(oB, c, ref cB, ref idx, repcnt);
                    step = repcnt;
                }
                else if (sM.length > 3)
                {//$bcd2
                    step = EncodeMatch(oB, c, ref sM, ref cB, ref idx, true);
                }
                else if (patcnt > 0)
                {//$bc13
                    WritePattern(oB, c, ref cB, ref idx, patcnt);
                    step = (patcnt * 2);
                }
                else if (altcnt1 > 0)
                {
                    step = WriteFirstPattern(oB, c, ref cB, ref idx, altcnt1);
                }
                else if (altcnt2 > 0)
                {
                    step = WriteSecondPattern(oB, c, ref cB, ref idx, altcnt2);
                }
                else
                {
                    int cnt = GetNextPattern(oB, c);
                    if (cnt > 0)
                    {
                        WritePlainBytes(oB, c, ref cB, ref idx, cnt);
                        step = cnt;
                    }
                    else
                    {
                        step = 1;
                    }
                }
                break;
            case 3:
                if (patcnt > 0)
                {//$bc13
                    WritePattern(oB, c, ref cB, ref idx, patcnt);
                    step = (patcnt * 2);
                }
                else if (repcnt > 3)
                {//$bd77
                    WriteRepeat(oB, c, ref cB, ref idx, repcnt);
                    step = repcnt;
                }
                else if (sM.length > 3)
                {//$bcd2
                    step = EncodeMatch(oB, c, ref sM, ref cB, ref idx, true);
                }
                else if (altcnt1 > 0)
                {
                    step = WriteFirstPattern(oB, c, ref cB, ref idx, altcnt1);
                }
                else if (altcnt2 > 0)
                {
                    step = WriteSecondPattern(oB, c, ref cB, ref idx, altcnt2);
                }
                else
                {
                    int cnt = GetNextPattern(oB, c);
                    if (cnt > 0)
                    {
                        WritePlainBytes(oB, c, ref cB, ref idx, cnt);
                        step = cnt;
                    }
                    else
                    {
                        step = 1;
                    }
                }
                break;
            case 4:
                if (altcnt1 > 0)
                {
                    step = WriteFirstPattern(oB, c, ref cB, ref idx, altcnt1);
                }
                else if (patcnt > 0)
                {//$bc13
                    WritePattern(oB, c, ref cB, ref idx, patcnt);
                    step = (patcnt * 2);
                }
                else if (repcnt > 3)
                {//$bd77
                    WriteRepeat(oB, c, ref cB, ref idx, repcnt);
                    step = repcnt;
                }
                else if (sM.length > 3)
                {//$bcd2
                    step = EncodeMatch(oB, c, ref sM, ref cB, ref idx, true);
                }
                else if (altcnt2 > 0)
                {
                    step = WriteSecondPattern(oB, c, ref cB, ref idx, altcnt2);
                }
                else
                {
                    int cnt = GetNextPattern(oB, c);
                    if (cnt > 0)
                    {
                        WritePlainBytes(oB, c, ref cB, ref idx, cnt);
                        step = cnt;
                    }
                    else
                    {
                        step = 1;
                    }
                }
                break;
            case 5:
                if (altcnt2 > 0)
                {
                    step = WriteSecondPattern(oB, c, ref cB, ref idx, altcnt2);
                }
                else if (altcnt1 > 0)
                {
                    step = WriteFirstPattern(oB, c, ref cB, ref idx, altcnt1);
                }
                else if (patcnt > 0)
                {//$bc13
                    WritePattern(oB, c, ref cB, ref idx, patcnt);
                    step = (patcnt * 2);
                }
                else if (repcnt > 3)
                {//$bd77
                    WriteRepeat(oB, c, ref cB, ref idx, repcnt);
                    step = repcnt;
                }
                else if (sM.length > 3)
                {//$bcd2
                    step = EncodeMatch(oB, c, ref sM, ref cB, ref idx, true);
                }
                else
                {
                    int cnt = GetNextPattern(oB, c);
                    if (cnt > 0)
                    {
                        WritePlainBytes(oB, c, ref cB, ref idx, cnt);
                        step = cnt;
                    }
                    else
                    {
                        step = 1;
                    }
                }
                break;
        }



    }

    enum EncType
    {
        RLE=1, REP=2, PAT=3, AL1=4, AL2=5, NON=0
    } 
    struct Match
    {
        public EncType type;
        public List<EncType> tHistory;
        public int length;
        public int score;
    }

    static Match newMatch()
    {
        return newMatch(0);
    }

    static Match newMatch(int len)
    {
        Match m = new Match();
        m.type = EncType.NON;
        m.length = len;
        m.tHistory = new List<EncType>();
        return m;
    }

    static Match newMatch(int len, EncType t)
    {
        Match m = newMatch(len);
        m.type = t;
        return m;
    }

    static Match BestScore(List<Match> matches)
    {
        Match m = newMatch();
        foreach (Match a in matches)
        {
            if (a.score > m.score)
            {
                m = a;
            }
        }
        return m;
    }

    static Match BestMatch (Match last, int maxmatch, int count, byte[] oB, int cindex, int score)
    {
        Match m = new Match();
        if (count == 0)
        {
            m.tHistory = new List<EncType>();
            m.length = 0;
            m.score = 0;
        }
        else
        {
            m.type = last.type;
            m.length = cindex;
            m.score = score;
            m.tHistory = new List<EncType>();
            foreach (EncType e in last.tHistory)
            {
                m.tHistory.Add(e);
            }
        }
        
        if (count <= maxmatch)
        {
            SearchResult sM = FindMatchFinal(oB, cindex);
            Match sMatch = newMatch(m.length + sM.length, EncType.RLE);
            int repcnt = GetRepeating(oB, cindex);
            Match rMatch = newMatch(m.length + repcnt, EncType.REP);
            int patcnt = GetPattern(oB, cindex) * 2;
            Match pMatch = newMatch(m.length + patcnt, EncType.PAT);
            int altcnt1 = GetAltPattern(oB, cindex, 0);
            Match a1Match = newMatch(m.length + altcnt1, EncType.AL1);
            int altcnt2 = GetAltPattern(oB, cindex, 1);
            Match a2Match = newMatch(m.length + altcnt2, EncType.AL2);
            int cnt = GetNextPattern(oB, cindex);
            Match nMatch = newMatch(m.length + cnt, EncType.NON);
            List<Match> mList = new List<Match>();
            //BackgroundWorker w1 = new BackgroundWorker();
            //BackgroundWorker w2 = new BackgroundWorker();
            //BackgroundWorker w3 = new BackgroundWorker();
            //BackgroundWorker w4 = new BackgroundWorker();
            //BackgroundWorker w5 = new BackgroundWorker();
            //BackgroundWorker w6 = new BackgroundWorker();
            //int w1hash = 0;
            //int w2hash = 0;
            //int w3hash = 0;
            //int w4hash = 0;
            //int w5hash = 0;
            //int w6hash = 0;
            if (sM.length > 3)
            {
                //w1hash = w1.GetHashCode();
                //w1.DoWork += new DoWorkEventHandler(Match_DoWork);
                //w1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Match_RunWorkerCompleted);
                //w1.RunWorkerAsync(newmyWorkArgs(m, maxmatch, count + 1, oB, cindex + sM.length, sM.length, EncType.RLE, w1hash));

                sMatch = BestMatch(m, maxmatch, count + 1, oB, cindex + sM.length, sM.length);
                sMatch.type = EncType.RLE;
                mList.Add(sMatch);
            }
            if (repcnt > 3)
            {
                //w2hash = w2.GetHashCode();
                //w2.DoWork += new DoWorkEventHandler(Match_DoWork);
                //w2.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Match_RunWorkerCompleted);
                //w2.RunWorkerAsync(newmyWorkArgs(m, maxmatch, count + 1, oB, cindex + repcnt, repcnt, EncType.REP, w2hash));

                rMatch = BestMatch(m, maxmatch, count + 1, oB, cindex + repcnt, repcnt);
                rMatch.type = EncType.REP;
                mList.Add(rMatch);
            }
            if (patcnt > 1)
            {
                //w3hash = w3.GetHashCode();
                //w3.DoWork += new DoWorkEventHandler(Match_DoWork);
                //w3.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Match_RunWorkerCompleted);
                //w3.RunWorkerAsync(newmyWorkArgs(m, maxmatch, count + 1, oB, cindex + patcnt, patcnt, EncType.PAT, w3hash));

                pMatch = BestMatch(m, maxmatch, count + 1, oB, cindex + patcnt, patcnt);
                pMatch.type = EncType.PAT;
                mList.Add(pMatch);
            }
            if (altcnt1 > 1)
            {
                //w4hash = w4.GetHashCode();
                //w4.DoWork += new DoWorkEventHandler(Match_DoWork);
                //w4.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Match_RunWorkerCompleted);
                //w4.RunWorkerAsync(newmyWorkArgs(m, maxmatch, count + 1, oB, cindex + altcnt1, altcnt1, EncType.AL1, w4hash));
                a1Match = BestMatch(m, maxmatch, count + 1, oB, cindex + altcnt1, altcnt1);
                a1Match.type = EncType.AL1;
                mList.Add(a1Match);
            }
            if (altcnt2 > 1)
            {
                //w5hash = w5.GetHashCode();
                //w5.DoWork += new DoWorkEventHandler(Match_DoWork);
                //w5.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Match_RunWorkerCompleted);
                //w5.RunWorkerAsync(newmyWorkArgs(m, maxmatch, count + 1, oB, cindex + altcnt2, altcnt2, EncType.AL2, w5hash));
                a2Match = BestMatch(m, maxmatch, count + 1, oB, cindex + altcnt2, altcnt2);
                a2Match.type = EncType.AL2;
                mList.Add(a2Match);
            }
            if (cnt > 1)
            {
                //w6hash = w6.GetHashCode();
                //w6.DoWork += new DoWorkEventHandler(Match_DoWork);
                //w6.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Match_RunWorkerCompleted);
                //w6.RunWorkerAsync(newmyWorkArgs(m, maxmatch, count + 1, oB, cindex + cnt, 0, EncType.NON, w6hash));
                nMatch = BestMatch(m, maxmatch, count + 1, oB, cindex + cnt, 0);
                nMatch.type = EncType.NON;
                mList.Add(nMatch);
            }
            //while (w1.IsBusy || w2.IsBusy || w3.IsBusy || w4.IsBusy || w5.IsBusy || w6.IsBusy)
            //{
            //    System.Threading.Thread.Sleep(1500);
            //    if (DEBUG) Console.Write(".");
            //}
            //mList = GetMatches(w1hash, w2hash, w3hash, w4hash, w5hash, w6hash);
            Match highscore = BestScore(mList);
            m.type = highscore.type;
            m.score += highscore.score;
            foreach (EncType e in highscore.tHistory)
            {
                m.tHistory.Add(e);
            }
        }
        return m;
    }

    static List<Match> GetMatches(int m1, int m2, int m3, int m4, int m5, int m6)
    {
        List<Match> lm = new List<Match>();
        if (!(m1 == 0))
        {
            lm.Add(MatchResults[m1]);
            MatchResults.Remove(m1);
        }
        if (!(m2 == 0))
        {
            lm.Add(MatchResults[m2]);
            MatchResults.Remove(m2);
        }
        if (!(m3 == 0))
        {
            lm.Add(MatchResults[m3]);
            MatchResults.Remove(m3);
        }
        if (!(m4 == 0))
        {
            lm.Add(MatchResults[m4]);
            MatchResults.Remove(m4);
        }
        if (!(m5 == 0))
        {
            lm.Add(MatchResults[m5]);
            MatchResults.Remove(m5);
        }
        if (!(m6 == 0))
        {
            lm.Add(MatchResults[m6]);
            MatchResults.Remove(m6);
        }
        return lm;
    }

    static Dictionary<int, Match> MatchResults = new Dictionary<int, Match>();

    private static void Match_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
        myWorkResult r = (myWorkResult)e.Result;
        if (DEBUG) Console.Write(r.uid + " - Worker Completed!");
        MatchResults.Add(r.uid, r.match);
    }

    struct myWorkArgs
    {
        public Match m;
        public int maxcount;
        public int count;
        public byte[] oB;
        public int cindex;
        public int score;
        public EncType type;
        public int uid;
    }

    static myWorkArgs newmyWorkArgs(Match m, int maxcount, int count, byte[] oB, int cindex, int score, EncType type, int uid)
    {
        myWorkArgs a = new myWorkArgs();
        a.m = m;
        a.maxcount = maxcount;
        a.count = count;
        a.oB = oB;
        a.cindex = cindex;
        a.score = score;
        a.type = type;
        a.uid = uid;
        return a;
    }

    private static void Match_DoWork(object sender, DoWorkEventArgs e)
    {
        myWorkArgs a = (myWorkArgs)e.Argument;
        Match sMatch = BestMatch(a.m, a.maxcount, a.count, a.oB, a.cindex, a.score);
        sMatch.type = a.type;
        e.Result = wrkResult(a.uid, sMatch);
    }

    static myWorkResult wrkResult(int uid, Match match)
    {
        myWorkResult m = new myWorkResult();
        m.uid = uid;
        m.match = match;
        return m;
    }

    struct myWorkResult
    {
        public int uid;
        public Match match;
    }

    static void LongEncoding(ref SearchResult sM, ref int repcnt, ref int patcnt, ref int altcnt1, ref int altcnt2, ref byte[] oB, ref int step, ref int c, ref int idx, ref byte[] cB)
    {
        //used to try and determine the best outcome even if the most obvious immediate technique seems to be best
        int lookAhead = 3;

        Match bm = BestMatch(newMatch(), lookAhead, 0, oB, c, 0);

        switch (bm.type)
        {
            case EncType.RLE:
                step = EncodeMatch(oB, c, ref sM, ref cB, ref idx, true);
                break;
            case EncType.REP:
                WriteRepeat(oB, c, ref cB, ref idx, repcnt);
                step = repcnt;
                break;
            case EncType.PAT:
                WritePattern(oB, c, ref cB, ref idx, patcnt);
                step = (patcnt * 2);
                break;
            case EncType.AL1:
                step = WriteFirstPattern(oB, c, ref cB, ref idx, altcnt1);
                break;
            default:
            case EncType.NON:
                int cnt = GetNextPattern(oB, c);
                if (cnt > 0)
                {
                    WritePlainBytes(oB, c, ref cB, ref idx, cnt);
                    step = cnt;
                }
                else
                {
                    step = 1;
                }
                break;
        }
    }


    static void StepEncoding(ref SearchResult sM, ref int repcnt, ref int patcnt, ref int altcnt1, ref int altcnt2, ref byte[] oB, ref int step, ref int c, ref int idx, ref byte[] cB)
    {
        if (sM.length > 1 && sM.length > repcnt && sM.length > patcnt && sM.length > altcnt1 && sM.length > altcnt2)
        {//$bcd2
            step = EncodeMatch(oB, c, ref sM, ref cB, ref idx, true);
        }
        else if (repcnt > 3 && repcnt > patcnt && repcnt > altcnt1 && repcnt > altcnt2)
        {//$bd77
            WriteRepeat(oB, c, ref cB, ref idx, repcnt);
            step = repcnt;
        }
        else if (patcnt > 0 && patcnt > altcnt1 && patcnt > altcnt2)
        {//$bc13
            WritePattern(oB, c, ref cB, ref idx, patcnt);
            step = (patcnt * 2);
        }
        else if (altcnt1 > 0 && altcnt1 > altcnt2)
        {
            step = WriteFirstPattern(oB, c, ref cB, ref idx, altcnt1);
        }
        else if (altcnt2 > 0)
        {
            step = WriteSecondPattern(oB, c, ref cB, ref idx, altcnt2);
        }
        else
        {
            int cnt = GetNextPattern(oB, c);
            if (cnt > 0)
            {
                WritePlainBytes(oB, c, ref cB, ref idx, cnt);
                step = cnt;
            }
            else
            {
                step = 1;
            }
        }
    }

    static int EncodeMatch(byte[] oB, int c, ref SearchResult sr, ref byte[] cb, ref int idx)
    {
        return EncodeMatch(oB, c, ref sr, ref cb, ref idx, false);
    }
    //return the final length and encode the bytes
    static int EncodeMatch(byte[] oB, int c, ref SearchResult sr, ref byte[] cb, ref int idx, bool test)
    {
        int i = idx;
        int len = 0;
        if (sr.location > 0x3FF)
        {
            len = sr.length;
            byte[] en = EncodeLong(sr);
            
            cb[idx] = en[0];
            cb[idx + 1] = en[1];
            cb[idx + 2] = en[2];
            if (!test) { idx += 3; } else
            {//only for testing
                //now compare with the original bytes...
                if (DEBUG)
                {
                    cb[idx + 3] = 0xFF;

                    if (BytesMisMatch(oB, cb, c, len))
                    {
                        if (DEBUG) Console.WriteLine("Bytes Mismatch!! - Writing Plain Encoding.");
                        WritePlainBytes(oB, c, ref cb, ref idx, len);
                    }
                    else
                    {
                        idx += 3;
                    }
                }else
                {
                    idx += 3;
                }
            }
        }
        else
        {//any
            len = sr.length;
            byte[] en = EncodeNormal(sr);
            cb[idx] = en[0];
            cb[idx + 1] = en[1];
            if (!test) { idx += 2; } else
            {
                if (DEBUG)
                {
                    cb[idx + 2] = 0xFF;

                    //now compare w-ith the original bytes...
                    if (BytesMisMatch(oB, cb, c, len))
                    {
                        if (DEBUG) Console.WriteLine("Bytes Mismatch!! - Writing Plain Encoding.");
                        WritePlainBytes(oB, c, ref cb, ref idx, len);
                    }
                    else
                    {
                        idx += 2;
                    }
                }
                else
                {
                    idx += 2;
                }
                
            }
        }

        return len;
    }

    static bool BytesMisMatch(byte[] oB, byte[] cb, int c, int len)
    {
        bool ma = false;
        string bm = "";
        byte[] data = Decompress(len, c, cb);
        if (data.Length >= 3)
        {
            byte[] d3 = new byte[data.Length];
            Array.Copy(oB, c, d3, 0, data.Length);
            if (!data.SequenceEqual(d3))
            {
                ma = true;
            }
            else
            {
                if (DEBUG) Console.WriteLine("Matched Compression!");
            }    
            
        }
        return ma;
    }

    static int EncodeMatchShort(byte[] oB, int c, SearchResult sr, ref byte[] cb, ref int idx)
    {
        int i = idx;
        int len = 0;
        if (sr.location > 0x3FF)
        {
            len = sr.length;
            byte[] en = EncodeLong(sr);
            cb[idx] = en[0];
            idx++;
            cb[idx] = en[1];
            idx++;
            cb[idx] = en[2];
            idx++;
        }
        else
        {//any
            len = sr.length;
            byte[] en = EncodeNormal(sr);
            cb[idx] = en[0];
            idx++;
            cb[idx] = en[1];
            idx++;
        }

        return len;
    }
    static int EncodeMatchLong(byte[] oB, int c, SearchResult sr, ref byte[] cb, ref int idx)
    {
        int i = idx;
        int len = 0;
        if (sr.location > 0x3FF)
        {
            len = sr.length;
            byte[] en = EncodeLong(sr);
            cb[idx] = en[0];
            idx++;
            cb[idx] = en[1];
            idx++;
            cb[idx] = en[2];
            idx++;
        }
        else
        {//any
            len = sr.length;
            byte[] en = EncodeNormal(sr);
            cb[idx] = en[0];
            idx++;
            cb[idx] = en[1];
            idx++;
        }

        return len;
    }

    static byte[] EncodeLong(SearchResult sr)
    {
        byte[] ret = new byte[3];

        switch (sr.length)
        {
            case 0x02:
            case 0x03:
                ret[0] = 0xC0;
                break;
            case 0x04:
            case 0x05:
                ret[0] = 0xC1;
                break;
            case 0x06:
            case 0x07:
                ret[0] = 0xC2;
                break;
            case 0x08:
            case 0x09:
                ret[0] = 0xC3;
                break;
            case 0x0A:
            case 0x0B:
                ret[0] = 0xC4;
                break;
            case 0x0C:
            case 0x0D:
                ret[0] = 0xC5;
                break;
            case 0x0E:
            case 0x0F:
                ret[0] = 0xC6;
                break;
            case 0x10:
            case 0x11:
                ret[0] = 0xC7;
                break;
            case 0x12:
            case 0x13:
                ret[0] = 0xC8;
                break;
            case 0x14:
            case 0x15:
                ret[0] = 0xC9;
                break;
            case 0x16:
            case 0x17:
                ret[0] = 0xCA;
                break;
            case 0x18:
            case 0x19:
                ret[0] = 0xCB;
                break;
            case 0x1A:
            case 0x1B:
                ret[0] = 0xCC;
                break;
            case 0x1C:
            case 0x1D:
                ret[0] = 0xCD;
                break;
            case 0x1E:
            case 0x1F:
                ret[0] = 0xCE;
                break;
            case 0x20:
            case 0x21:
                ret[0] = 0xCF;
                break;
        }
        ret[1] = shiftRight((short)sr.location, 8).lsb;
        ret[2] = (byte)sr.location;
        return ret;
    }

    //there is a better way to do this... I just cant think of what it is right now... so its ugly.
    static byte[] EncodeNormal(SearchResult sr)
    {
        byte[] ret = new byte[2];

        switch (sr.length)
        {
            case 0x02:
                if (sr.location >= 0x0100)
                {
                    if (sr.location >= 0x0200)
                    {
                        if (sr.location >= 0x0300)
                        {
                            ret[0] = 0x03;
                        }
                        else
                        {
                            ret[0] = 0x02;
                        }
                    }
                    else
                    {
                        ret[0] = 0x01;
                    }
                }
                else
                {
                    ret[0] = 0x00;
                }
                break;
            case 0x03:
                if (sr.location >= 0x0100)
                {
                    if (sr.location >= 0x0200)
                    {
                        if (sr.location >= 0x0300)
                        {
                            ret[0] = 0x07;
                        }
                        else
                        {
                            ret[0] = 0x06;
                        }
                    }
                    else
                    {
                        ret[0] = 0x05;
                    }
                }
                else
                {
                    ret[0] = 0x04;
                }
                break;
            case 0x04:
                if (sr.location >= 0x0100)
                {
                    if (sr.location >= 0x0200)
                    {
                        if (sr.location >= 0x0300)
                        {
                            ret[0] = 0x0B;
                        }
                        else
                        {
                            ret[0] = 0x0A;
                        }
                    }
                    else
                    {
                        ret[0] = 0x09;
                    }
                }
                else
                {
                    ret[0] = 0x08;
                }
                break;
            case 0x05:
                if (sr.location >= 0x0100)
                {
                    if (sr.location >= 0x0200)
                    {
                        if (sr.location >= 0x0300)
                        {
                            ret[0] = 0x0F;
                        }
                        else
                        {
                            ret[0] = 0x0E;
                        }
                    }
                    else
                    {
                        ret[0] = 0x0D;
                    }
                }
                else
                {
                    ret[0] = 0x0C;
                }
                break;
            case 0x06:
                if (sr.location >= 0x0100)
                {
                    if (sr.location >= 0x0200)
                    {
                        if (sr.location >= 0x0300)
                        {
                            ret[0] = 0x13;
                        }
                        else
                        {
                            ret[0] = 0x12;
                        }
                    }
                    else
                    {
                        ret[0] = 0x11;
                    }
                }
                else
                {
                    ret[0] = 0x10;
                }
                break;
            case 0x07:
                if (sr.location >= 0x0100)
                {
                    if (sr.location >= 0x0200)
                    {
                        if (sr.location >= 0x0300)
                        {
                            ret[0] = 0x17;
                        }
                        else
                        {
                            ret[0] = 0x16;
                        }
                    }
                    else
                    {
                        ret[0] = 0x15;
                    }
                }
                else
                {
                    ret[0] = 0x14;
                }
                break;
            case 0x08:
                if (sr.location >= 0x0100)
                {
                    if (sr.location >= 0x0200)
                    {
                        if (sr.location >= 0x0300)
                        {
                            ret[0] = 0x1B;
                        }
                        else
                        {
                            ret[0] = 0x1A;
                        }
                    }
                    else
                    {
                        ret[0] = 0x19;
                    }
                }
                else
                {
                    ret[0] = 0x18;
                }
                break;
            case 0x09:
                if (sr.location >= 0x0100)
                {
                    if (sr.location >= 0x0200)
                    {
                        if (sr.location >= 0x0300)
                        {
                            ret[0] = 0x1F;
                        }
                        else
                        {
                            ret[0] = 0x1E;
                        }
                    }
                    else
                    {
                        ret[0] = 0x1D;
                    }
                }
                else
                {
                    ret[0] = 0x1C;
                }
                break;
            case 0x0A:
                if (sr.location >= 0x0100)
                {
                    if (sr.location >= 0x0200)
                    {
                        if (sr.location >= 0x0300)
                        {
                            ret[0] = 0x23;
                        }
                        else
                        {
                            ret[0] = 0x22;
                        }
                    }
                    else
                    {
                        ret[0] = 0x21;
                    }
                }
                else
                {
                    ret[0] = 0x20;
                }
                break;
            case 0x0B:
                if (sr.location >= 0x0100)
                {
                    if (sr.location >= 0x0200)
                    {
                        if (sr.location >= 0x0300)
                        {
                            ret[0] = 0x27;
                        }
                        else
                        {
                            ret[0] = 0x26;
                        }
                    }
                    else
                    {
                        ret[0] = 0x25;
                    }
                }
                else
                {
                    ret[0] = 0x24;
                }
                break;
            case 0x0C:
                if (sr.location >= 0x0100)
                {
                    if (sr.location >= 0x0200)
                    {
                        if (sr.location >= 0x0300)
                        {
                            ret[0] = 0x2B;
                        }
                        else
                        {
                            ret[0] = 0x2A;
                        }
                    }
                    else
                    {
                        ret[0] = 0x29;
                    }
                }
                else
                {
                    ret[0] = 0x28;
                }
                break;
            case 0x0D:
                if (sr.location >= 0x0100)
                {
                    if (sr.location >= 0x0200)
                    {
                        if (sr.location >= 0x0300)
                        {
                            ret[0] = 0x2F;
                        }
                        else
                        {
                            ret[0] = 0x2E;
                        }
                    }
                    else
                    {
                        ret[0] = 0x2D;
                    }
                }
                else
                {
                    ret[0] = 0x2C;
                }
                break;
            case 0x0E:
                if (sr.location >= 0x0100)
                {
                    if (sr.location >= 0x0200)
                    {
                        if (sr.location >= 0x0300)
                        {
                            ret[0] = 0x33;
                        }
                        else
                        {
                            ret[0] = 0x32;
                        }
                    }
                    else
                    {
                        ret[0] = 0x31;
                    }
                }
                else
                {
                    ret[0] = 0x30;
                }
                break;
            case 0x0F:
                if (sr.location >= 0x0100)
                {
                    if (sr.location >= 0x0200)
                    {
                        if (sr.location >= 0x0300)
                        {
                            ret[0] = 0x37;
                        }
                        else
                        {
                            ret[0] = 0x36;
                        }
                    }
                    else
                    {
                        ret[0] = 0x35;
                    }
                }
                else
                {
                    ret[0] = 0x34;
                }
                break;
            case 0x10:
                if (sr.location >= 0x0100)
                {
                    if (sr.location >= 0x0200)
                    {
                        if (sr.location >= 0x0300)
                        {
                            ret[0] = 0x3B;
                        }
                        else
                        {
                            ret[0] = 0x3A;
                        }
                    }
                    else
                    {
                        ret[0] = 0x39;
                    }
                }
                else
                {
                    ret[0] = 0x38;
                }
                break;
            case 0x11:
                if (sr.location >= 0x0100)
                {
                    if (sr.location >= 0x0200)
                    {
                        if (sr.location >= 0x0300)
                        {
                            ret[0] = 0x3F;
                        }
                        else
                        {
                            ret[0] = 0x3E;
                        }
                    }
                    else
                    {
                        ret[0] = 0x3D;
                    }
                }
                else
                {
                    ret[0] = 0x3C;
                }
                break;
        }
        ret[1] = (byte)sr.location;
        ret[0] |= 0x80;
        return ret;
    }

    enum SearchCountType
    {
        Normal, Even, Odd
    }

    struct SearchResult
    {
        public int location;
        public int length;
        public byte[] data;
    }

    static SearchResult DefaultSR()
    {
        SearchResult d = new SearchResult();
        d.location = 0;
        d.length = 0;
        d.data = null;
        return d;
    }


    //Contains some repetition... bad lazy me
    static SearchResult FindBestMatch(byte[] oB, int c, int countMax)
    {
        SearchResult res = DefaultSR();

        //the modified max count based on available data selection
        int cm = (c > countMax) ? countMax : c;

        if (cm > 0x3 && c > 0x3FF)
        {
            if (cm > 0x20) cm = 0x20; //(this is the highest allowed count -- 32 bytes)

            if (c > 0x8000)
            {//do an odd count search before an even count search
                short oS = (short)((c < 0xFFFF) ? c : 0xFFFF);
                res = FindMatch(oB, c, cm, 3, oS, SearchCountType.Odd);
            }
            short eS = (short)((c < 0x7FFF) ? c : 0x7FFF); // check not necessary, but for consistency...
            SearchResult even = FindMatch(oB, c, cm, 3, eS, SearchCountType.Even);
            if (res.length < even.length)
            {
                res = even;
            }
            if (cm > 0x17) cm = 0x17;
            short nS = (short)((c < 0x3FF) ? c : 0x3FF);
            SearchResult resnorm = FindMatch(oB, c, cm, 2, nS, SearchCountType.Normal);
            if (res.length < resnorm.length)
            {
                res = resnorm;
            }
        }
        else
        {
            short nS = (short)((c < 0x3FF) ? c : 0x3FF);
            res = FindMatch(oB, c, cm, 2, nS, SearchCountType.Normal);
            if (c > 0x3FF)
            {
                if (cm > 0x20) cm = 0x20; //(this is the highest allowed count -- 32 bytes)
                if (c > 0x8000)
                {//do an odd count search before an even count search
                    short oS = (short)((c < 0xFFFF) ? c : 0xFFFF);
                    SearchResult odd = FindMatch(oB, c, cm, 3, oS, SearchCountType.Odd);
                    if (res.length < odd.length)
                    {
                        res = odd;
                    }
                }
                short eS = (short)((c < 0x7FFF) ? c : 0x7FFF); // check not necessary, but for consistency...
                SearchResult even = FindMatch(oB, c, cm, 3, eS, SearchCountType.Even);
                if (res.length < even.length)
                {
                    res = even;
                }
            }
        }

        return res;
    }


    static SearchResult FindBestMatchLong(byte[] oB, int c, int countMax)
    {
        SearchResult res = DefaultSR();

        //the modified max count based on available data selection
        int cm = (c > countMax) ? countMax : c;

        //try to find a close match...
        short nS = (short)((c < 0x3FF) ? c : 0x3FF);
        res = FindMatch(oB, c, cm, 2, nS, SearchCountType.Normal);

        if (c > 0x3FF)
        {
            if (cm > 0x20) cm = 0x20; //(this is the highest allowed count -- 32 bytes)
            if (c > 0x8000)
            {//do an odd count search before an even count search
                short oS = (short)((c < 0xFFFF) ? c : 0xFFFF);
                SearchResult odd = FindMatch(oB, c, cm, 3, oS, SearchCountType.Odd);
                if (res.length < odd.length)
                {
                    res = odd;
                }
            }
            short eS = (short)((c < 0x7FFF) ? c : 0x7FFF); // check not necessary, but for consistency...
            SearchResult even = FindMatch(oB, c, cm, 3, eS, SearchCountType.Even);
            if (res.length < even.length)
            {
                res = even;
            }
        }


        return res;
    }

    //Contains some repetition... bad lazy me
    static SearchResult FindBestMatchShort(byte[] oB, int c, int countMax)
    {
        SearchResult res = DefaultSR();

        //the modified max count based on available data selection
        int cm = (c > countMax) ? countMax : c;

        if (cm > 0x20) cm = 0x20; //(this is the highest allowed count -- 32 bytes)

        if (c > 0x8000)
        {//do an odd count search before an even count search
            short oS = (short)((c < 0xFFFF) ? c : 0xFFFF);
            res = FindMatch(oB, c, cm, 3, oS, SearchCountType.Odd);
        }
        short eS = (short)((c < 0x7FFF) ? c : 0x7FFF); // check not necessary, but for consistency...
        SearchResult even = FindMatch(oB, c, cm, 3, eS, SearchCountType.Even);
        if (res.length < even.length)
        {
            res = even;
        }
        if (cm > 0x17) cm = 0x17;
        short nS = (short)((c < 0x3FF) ? c : 0x3FF);
        SearchResult resnorm = FindMatch(oB, c, cm, 2, nS, SearchCountType.Normal);
        if (res.length < resnorm.length)
        {
            res = resnorm;
        }


        return res;
    }


    static SearchResult FindMatchFinal(byte[] oB, int c)
    {
        SearchResult res = DefaultSR();
        int countMin = 3;
        int countMax = (c < 0x3FFF) ? 17 : (c < 0x8000) ? 32 : 33;  //merging all search functions into one... we will always use
        int searchMax =  (c > 0xFFFF) ? 0xFFFF : c;
        int leastOB = (c - searchMax);
        if (leastOB < 0) leastOB = 0;

        if (countMax > c) countMax = c;


        //get the current search byte
        for (int b = countMax; b >= countMin; b--)
        {
            if ((oB.Length - c) > b)
            {
                res.data = new byte[b];
                Array.Copy(oB, c, res.data, 0, b); //load up the bytes we are searching for...
                                                   //our search position
                for (int p = (c - searchMax); p < (c - b); p++)
                {
                    byte[] spyglass = new byte[b];
                    Array.Copy(oB, p, spyglass, 0, b); //load up the bytes we are searching for...

                    if (spyglass.SequenceEqual(res.data))
                    {//found a match
                        res.location = c - p;
                        res.length = b;
                        break;
                    }
                }
                if (res.length > 0)
                {
                    break;
                }
            }
        }

        if (res.location > 0x3FF)
        {//make sure that even/odd lengths are correct before the next step
            if (IsOdd(res.length) && res.location < 0x8000)
            {
                res.length--;
                byte[] temp = new byte[res.length];
                Array.Copy(res.data, temp, res.length);
                res.data = new byte[res.length];
                Array.Copy(temp, res.data, res.length);
            }
        }

        return res;
    }

    static SearchResult FindMatch(byte[] oB, int c, int countMax, int countMin, short searchMax, SearchCountType s)
    {
        SearchResult res = DefaultSR();
        //make sure we can 
        int leastOB = (c - searchMax);
        if (leastOB < 0) leastOB = 0;

        int cm = countMax;
        if (IsOdd(cm))
        {
            cm--;
        }
        //get the current search byte
        for (int b = cm; b >= countMin; b = (s == SearchCountType.Normal) ? (b - 1) : (b - 2))
        {
            if ((oB.Length - c) > b)
            {
                res.data = new byte[b];
                Array.Copy(oB, c, res.data, 0, b); //load up the bytes we are searching for...
                                                   //our search position
                for (int p = (c - searchMax); p < (c - b); p++)
                {
                    byte[] spyglass = new byte[b];
                    Array.Copy(oB, p, spyglass, 0, b); //load up the bytes we are searching for...

                    if (spyglass.SequenceEqual(res.data))
                    {//found a match
                        res.location = c - p;
                        res.length = b;
                        break;
                    }
                }
                if (res.length > 0)
                {
                    break;
                }
            }
        }
        return res;
    }

    static bool IsOdd(int value)
    {
        return value % 2 != 0;
    }

    static void WritePattern(byte[] oB, int c, ref byte[] cb, ref int idx, int rep)
    {
        int i = idx;
        int nrep = rep - 1;
        cb[idx] = (byte)(0x50 | nrep);
        string b1 = cb[idx].ToString("X").PadLeft(2, '0');
        idx++;
        string bytes = "";
        for (int r = c; r < c + (rep * 2); r += 2)
        {
            cb[idx] = oB[r];
            bytes += oB[r].ToString("X").PadLeft(2, '0');
            idx++;
        }

        if (DEBUG)
        {
            Console.WriteLine(b1 + " - " + bytes);
            byte[] ctest = new byte[(idx - i) + 1];
            Array.Copy(cb, i, ctest, 0, idx - i);
            ctest[(idx - i)] = 0xFF;
            byte[] test = Decompress(ctest);
            byte[] ort = new byte[rep * 2];
            Array.Copy(oB, c, ort, 0, rep * 2);
            if (!test.SequenceEqual(ort))
            {
                Console.WriteLine("Problem!");
            }
        }
    }

    //Compression of repeating bytes
    static void WriteRepeat(byte[] oB, int c, ref byte[] cb, ref int idx, int rep)
    {
        int i = idx;
        if (rep < 0x8)
        {
            int nrep = rep - 3;
            cb[idx] = (byte)(0xF0 | nrep);
            string b1 = cb[idx].ToString("X").PadLeft(2, '0');
            idx++;
            cb[idx] = oB[c];
            string b2 = cb[idx].ToString("X").PadLeft(2, '0');
            idx++;
            if (DEBUG) Console.WriteLine(b1 + " - " + b2);
        }
        else
        {
            cb[idx] = 0xE0;
            string b1 = cb[idx].ToString("X").PadLeft(2, '0');
            idx++;
            int nrep = rep - 3;
            //if (IsBitSet((byte)nrep, 0)) nrep--;
            cb[idx] = (byte)nrep;
            string b2 = cb[idx].ToString("X").PadLeft(2, '0');
            idx++;
            cb[idx] = oB[c];
            string b3 = cb[idx].ToString("X").PadLeft(2, '0');
            idx++;
            if (DEBUG) Console.WriteLine(b1 + " - " + b2 + " - " + b3);
        }
        if (DEBUG)
        {
            byte[] ctest = new byte[(idx - i) + 1];
            Array.Copy(cb, i, ctest, 0, idx - i);
            ctest[(idx - i)] = 0xFF;
            byte[] test = Decompress(ctest);
            byte[] ort = new byte[rep];
            Array.Copy(oB, c, ort, 0, rep);
            if (!test.SequenceEqual(ort))
            {
                Console.WriteLine("Problem!");
            }
        }
    }



    static void WritePlainBytes(byte[] oB, int c, ref byte[] cb, ref int idx, int count)
    {
        int i = idx;
        int nrep = count - 1;
        int mcount = count;
        if ((c + count) > oB.Length)
        {
            nrep = (oB.Length - c) - 1;
            mcount = (oB.Length - c);
        }
        cb[idx] = (byte)(nrep);
        idx++;
        string bytes = "";
        for (int r = c; r < c + mcount; r++)
        {
            cb[idx] = oB[r];
            bytes += oB[r].ToString("X").PadLeft(2, '0');
            idx++;
        }
        if (DEBUG)
        {
            Console.WriteLine(nrep.ToString("X").PadLeft(2, '0') + " - " + bytes);
            byte[] ctest = new byte[(idx - i) + 1];
            Array.Copy(cb, i, ctest, 0, idx - i);
            ctest[(idx - i)] = 0xFF;
            byte[] test = Decompress(ctest);
            byte[] ort = new byte[count];
            Array.Copy(oB, c, ort, 0, count);
            if (!test.SequenceEqual(ort))
            {
                Console.WriteLine("Problem!");
            }
        }
    }

    static int GetNextPattern(byte[] oB, int c)
    {
        int count = -3;
        if (oB.Length > 0)
        {
            int srchlimit = ((oB.Length - c) > 0x2C) ? 0x2C : (oB.Length - c);
            int bytelimit = srchlimit * 2;
            if (bytelimit > (oB.Length - (c + 3)))
            {
                bytelimit = oB.Length - (c + 3);
            }
            if (bytelimit > 0)
            {
                byte[] p = new byte[bytelimit];
                Array.Copy(oB, c + 3, p, 0, bytelimit);

                for (int l = 0; l < srchlimit; l++)
                {
                    int cnt = GetRepeating(p, l);
                    if (cnt <= 3)
                    {
                        cnt = (GetPattern(p, l) * 2);
                        if (cnt == 0)
                        {
                            cnt = GetAltPattern(p, l, 0) * 2;
                            if (cnt == 0)
                            {
                                cnt = GetAltPattern(p, l, 1) * 2;
                                count = l + 1;
                                if (!(cnt == 0))
                                {
                                    break;
                                }
                            }
                            else
                            {
                                count = l + 1;
                                break;
                            }
                        }
                        else
                        {
                            count = l + 1;
                            break;
                        }
                    }
                    else
                    {
                        count = l + 1;
                        break;
                    }

                }
            }
        }
        //number of bytes ahead the next pattern is waiting
        return count + 3; //at least 4 bytes ahead
    }

    //are the next bytes repeating?
    static int GetRepeating(byte[] oB, int c)
    {
        int rCount = 0;
        if (oB.Length > 0)
        {
            int replimit = ((oB.Length - c) > 0xFE) ? 0xFE : (oB.Length - c);
            if (replimit > 0)
            {
                byte[] p = new byte[replimit];
                Array.Copy(oB, c, p, 0, replimit);
                byte chk = 0;
                for (int l = 0; l < p.Length; l++)
                {
                    if (l == 0) chk = p[l];
                    if (!(chk == p[l]))
                    {
                        break;
                    }
                    rCount++;
                }
            }
        }
        return rCount;
    }

    //get the alternating pattern count
    static int GetAltPattern(byte[] oB, int c, int offset)
    {
        int rCount = 0;
        if (oB.Length > 0)
        {
            int replimit = ((oB.Length - c) > 0x0F) ? 0x0F : (oB.Length - c);
            if (replimit > 0)
            {
                byte[] p = new byte[replimit];
                Array.Copy(oB, c, p, 0, replimit);
                byte chk = 0;

                for (int l = offset; l < p.Length; l += 2)
                {
                    if (l == offset) chk = p[l];
                    if (!(chk == p[l]))
                    {
                        break;
                    }
                    rCount++;
                }
                if (rCount < 2) rCount = 0;
            }
        }
        return rCount;
    }


    static int WriteFirstPattern(byte[] oB, int c, ref byte[] cb, ref int idx, int rep)
    {
        int i = idx;
        int finalcount = 0;
        int nrep = (rep - 2);

        //define pattern type and number of repeats
        cb[idx] = (byte)(0x60 | nrep);
        idx++;

        //save the repeating byte
        cb[idx] = oB[c];
        idx++;

        //save the in-between bytes
        for (int r = c + 1; r < ((c + 1) + (rep * 2)); r += 2)
        {
            cb[idx] = oB[r];
            idx++;
            finalcount++;
        }
        if (DEBUG)
        {
            byte[] ctest = new byte[(idx - i) + 1];
            Array.Copy(cb, i, ctest, 0, idx - i);
            ctest[(idx - i)] = 0xFF;
            byte[] test = Decompress(ctest);
            byte[] ort = new byte[finalcount * 2];
            Array.Copy(oB, c, ort, 0, finalcount * 2);

            if (!test.SequenceEqual(ort))
            {
                Console.WriteLine("Problem!");
            }
        }
        return finalcount * 2;
    }

    static int WriteSecondPattern(byte[] oB, int c, ref byte[] cb, ref int idx, int rep)
    {
        int i = idx;
        int finalcount = 0;
        int nrep = (rep - 2);

        //define pattern type and number of repeats
        cb[idx] = (byte)(0x70 | nrep);
        idx++;

        //save the repeating byte
        cb[idx] = oB[c + 1];
        idx++;

        //save the in-between bytes
        for (int r = c; r < (c + (rep * 2)); r += 2)
        {
            cb[idx] = oB[r];
            idx++;
            finalcount++;
        }
        if (DEBUG)
        {
            byte[] ctest = new byte[(idx - i) + 1];
            Array.Copy(cb, i, ctest, 0, idx - i);
            ctest[(idx - i)] = 0xFF;
            byte[] test = Decompress(ctest);
            byte[] ort = new byte[finalcount * 2];
            Array.Copy(oB, c, ort, 0, finalcount * 2);
            if (!test.SequenceEqual(ort))
            {
                Console.WriteLine("Problem!");
            }
        }
        return finalcount * 2;
    }


    //get the alternating pattern count
    static int GetPattern(byte[] oB, int c)
    {
        int rCount = 0;
        if (oB.Length > 0)
        {
            int replimit = ((oB.Length - c) > (0x0F * 2)) ? (0x0F * 2) : (oB.Length - c);
            if (replimit > 0)
            {
                byte[] p = new byte[replimit];
                Array.Copy(oB, c, p, 0, replimit);
                byte chk = 0;

                for (int l = 0; l < p.Length; l += 2)
                {
                    chk = p[l];
                    try {
                        if (p.Length > 1 && !(chk == p[l + 1]))
                        {
                            break;
                        }
                    }
                    catch { break; }
                    rCount++;
                }
                if (rCount < 2) rCount = 0;
            }
        }
        return rCount;
    }

    public static byte[] Decompress(byte[] mF)
    {
        return Decompress(0, 0, mF);
    }

    public static byte[] Decompress(int len, int cidx, byte[] mF)
    {
        int outPos = 0;

        byte[] mOut = new byte[0x100000];
        byte[] sOut = new byte[0x100000]; // for selective output

        byte[] testout = new byte[0x100000];// File.ReadAllBytes(@"C:\Users\Administrator\Google Drive\SuperFamicomWars\sfcwars_logo.bin");
        int x = outPos; // this is the output counter

        //for logging capability...
        int lx = x;
        int ly = 0;

        for (int y = ly; y < mF.Length; y++)
        {
            shift s = shiftLeft(mF[y]);
            if (!s.carried)
            {//$bc9b
                if (s.neg)
                {//$bc35
                    shift s2 = shiftRight(s.lsb);
                    if (s2.lsb < 0x60)
                    {//$bc0c
                        if (s2.lsb >= 0x50)
                        {//$bc13 
                            byte b = (byte)(s2.lsb & 0x0F);
                            for (int c = b; c >= 0; c--)
                            {
                                y++;
                                byte b2 = mF[y];
                                //mOut[x] = b2;
                                SetOut(mOut, testout, x, b2, sOut, len);
                                x++;
                                //mOut[x] = b2;
                                SetOut(mOut, testout, x, b2, sOut, len);
                                x++;
                            }
                            //if (clog) writeClog(clogf, "$bc13", mF, mOut, y, ref ly, x, ref lx);
                        }
                    }
                    else
                    {//$bc3a
                        y++;
                        byte xba = s2.lsb;//save lsb
                        byte b03 = mF[y];
                        bool test = (xba >= 0x70);
                        xba &= 0x0f;
                        xba++;
                        if (test)
                        {//$bc7c
                            for (int c = xba; c >= 0; c--)
                            {
                                y++;
                                byte b3 = mF[y];
                                //mOut[x] = b3;
                                SetOut(mOut, testout, x, b3, sOut, len);
                                x++;
                                //mOut[x] = b03;
                                SetOut(mOut, testout, x, b03, sOut, len);
                                x++;
                            }
                            //if (clog) writeClog(clogf, "$bc7c", mF, mOut, y, ref ly, x, ref lx);
                        }
                        else
                        {//$bc5d
                            for (int c = xba; c >= 0; c--)
                            {
                                SetOut(mOut, testout, x, b03, sOut, len);
                                x++;
                                y++;
                                byte b3 = mF[y];
                                SetOut(mOut, testout, x, b3, sOut, len);
                                x++;
                            }
                            //if (clog) writeClog(clogf, "$bc5d", mF, mOut, y, ref ly, x, ref lx);
                        }
                    }
                }
                else
                {//$bc9d
                    shift s2 = shiftRight(s.lsb);
                    for (int c = s2.lsb; c >= 0; c--)
                    {
                        y++;
                        byte b2 = mF[y];
                        SetOut(mOut, testout, x, b2, sOut, len);
                        x++;
                    }
                    //if (clog) writeClog(clogf, "$bc9d", mF, mOut, y, ref ly, x, ref lx);
                }
            }
            else if (s.neg)
            {//$bd07
                byte b = ShiftRightCarry(s);
                if (b >= 0xE0)
                {//$bd31
                    if (b >= 0xF0)
                    {//$bd73
                        if (b >= 0xF8)
                        {//$bd90
                            if (b >= 0xFC)
                            {//$bde9
                                if (b >= 0xFE)
                                {//$be0c
                                    //if (split)
                                    //{
                                    //    byte[] mWrite = new byte[x];
                                    //    for (int c = 0; c < x; c++)
                                    //    {
                                    //        mWrite[c] = mOut[c];
                                    //    }
                                    //    string file = fileout + "_0x" + len.ToString("X") + ".bin";

                                    //    if (mWrite.Length > 250)
                                    //    {
                                    //        Console.WriteLine("Writing 0x" + len.ToString("X") + "...");
                                    //        File.WriteAllBytes(file, mWrite);
                                    //    }
                                    //    mOut = null;
                                    //    mWrite = null;

                                    //    ly = y;
                                    break;
                                    //}
                                }
                            }
                        }
                        else
                        {//$bd77
                            b &= 0x07;
                            short bytes = (short)(b + 0x02);
                            b = (byte)bytes;
                            y++;
                            byte b2 = mF[y];
                            for (int c = b; c >= 0; c--)
                            {
                                SetOut(mOut, testout, x, b2, sOut, len);
                                x++;
                            }
                            //if (clog) writeClog(clogf, "$bd77", mF, mOut, y, ref ly, x, ref lx);
                        }
                    }
                    else
                    {//$bd35
                        b &= 0x0F;
                        y++;
                        short b2 = mF[y];
                        y++;
                        byte b3 = mF[y];

                        b2 += 0x0003;
                        shift s2 = shiftRight((byte)b2);
                        for (int lp = s2.bytes; lp > 0; lp--)
                        {
                            //applying b3...
                            SetOut(mOut, testout, x, b3, sOut, len);
                            x++;
                            SetOut(mOut, testout, x, b3, sOut, len);
                            x++;
                        }

                        if (s2.carried)
                        {//$bd6c
                            SetOut(mOut, testout, x, b3, sOut, len);
                            x++;
                        }
                        //if (clog) writeClog(clogf, "$bd35", mF, mOut, y, ref ly, x, ref lx);
                    }
                }
                else
                {//$bd0c
                    b &= 0x1F;
                    short bs = (short)(b * 0x100);

                    y++;
                    short b2 = mF[y];
                    bs |= b2;
                    shift s2 = shiftLeft(bs);

                    byte msbCounter = (s2.carried) ? (byte)(s2.carrybyte + 1) : (byte)1;

                    //shift s3 = shiftRight((byte)s2.lsb);
                    short bs2 = (short)(b2 * 0x100);//(short)(s3.lsb * 0x100);

                    y++;
                    short b3 = (short)(bs2 | (short)mF[y]);

                    short tempy = (short)x;
                    tempy -= b3;

                    for (int c = msbCounter; c >= 0; c--)
                    {
                        try
                        {
                            byte tempa = mOut[tempy];
                            SetOut(mOut, testout, x, tempa, sOut, len);
                            tempy++;
                            x++;
                        }
                        catch { }
                    }
                    //if (clog) writeClog(clogf, "$bd0c", mF, mOut, y, ref ly, x, ref lx);
                }
            }
            else
            {//$bcd2
                shift s2 = shiftRight(s.lsb);
                short tempa = s2.lsb;
                shift s4 = shiftRight(s2.lsb, 2);
                byte msbCounter = (byte)(s4.lsb + 1);
                tempa &= 0x03;
                if (tempa > 0x00)
                {
                    tempa *= 0x100;
                }

                y++;
                // used to calculate position
                short mya = 0;
                try
                {//$bcdd
                    byte mb = mF[y];
                    mya = (short)(tempa | (short)mb);
                }
                catch { } // in case we are missing data

                short myy = (short)x;
                myy -= (short)mya;

                for (int c = msbCounter; c >= 0; c--)
                {
                    try
                    {
                        byte ta = mOut[myy];
                        SetOut(mOut, testout, x, ta, sOut, len);
                    }
                    catch
                    {//out of range... means we are missing data... zero fill
                        SetOut(mOut, testout, x, 0, sOut, len);
                    }
                    myy++;
                    x++;
                }
                //if (clog) writeClog(clogf, "$bcd2", mF, mOut, y, ref ly, x, ref lx);
            }
        }

        if (DEBUG) Console.WriteLine("Total Original Size: " + ((ly-len == 0) ? mF.Length.ToString("X") : (ly - len).ToString("X")));
        if (DEBUG) Console.WriteLine("Total Decompressed Size: " + x.ToString("X"));

        byte[] mWrite = new byte[x];
        if (len > 0)
        {
            mWrite = new byte[len];
            Array.Copy(sOut, x - len, mWrite, 0, len);
        }
        else
        {
            Array.Copy(mOut, mWrite, x);
        }
        
        


        return mWrite;
        //string file = fileout + "_0x" + start.ToString("X") + ".bin";
        //File.WriteAllBytes(file, mWrite);

    }

    static void SetOut(byte[] mainOut, byte[] testOut, int index, byte set, byte[] selOut, int len)
    {
        try
        {
            //if (!(testOut[index] == set))
            //{
            //    byte b = testOut[index];
            //    Console.WriteLine(index.ToString("X") + " - Does not match! " + set.ToString("X") + " should be " + b.ToString("X"));
            //}
            mainOut[index] = set;
            if (len > 0)
            {
                selOut[index] = set;
            }
        }
        catch { }

    }

    //shift in the extra byte
    static byte ShiftRightCarry(shift s)
    {
        byte b = 0;
        if (s.carried)
        {
            b = (byte)(s.bytes >> 1);
        }
        else
        {
            b = (byte)(s.lsb >> 1);
        }
        return b;
    }

    static shift shiftLeft(byte b)
    {
        return shiftLeft((short)b, 1);
    }

    static shift shiftLeft(short b)
    {
        return shiftLeft(b, 1);
    }

    static shift shiftLeft(short b, int times)
    {
        shift s = new shift();
        short shft = (short)(b << times);
        s.carried = (shft > 255);
        s.bytes = (short)shft;
        s.lsb = (byte)shft;
        s.neg = IsBitSet(s.lsb, 7); // is the most significant bit set?
        if (s.carried)
        {
            s.carrybyte = (byte)(shft >> 8 & 0xFF); // shift to find the data that we've stripped
        }
        else
        {
            s.carrybyte = null;
        }

        return s;
    }


    static shift shiftRight(short b)
    {
        return shiftRight(b, 1);
    }
    static shift shiftRight(short b, int times)
    {
        shift s = new shift();
        int shft = b >> times;
        s.bytes = (short)shft;
        s.lsb = (byte)shft;
        s.neg = false;
        s.carried = IsBitSet(b, 0);
        s.carrybyte = null;
        return s;
    }

    static shift shiftRight(byte b)
    {
        return shiftRight((short)b, 1);
    }

    static bool IsBitSet(byte b, int pos)
    {
        return (b & (1 << pos)) != 0;
    }
    static bool IsBitSet(short b, int pos)
    {
        return (b & (1 << pos)) != 0;
    }

    struct shift
    {
        public byte lsb;
        public bool carried;
        public bool neg;
        public byte? carrybyte;
        public short bytes;
    }
}
