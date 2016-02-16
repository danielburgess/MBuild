using MBuild.Properties;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

//used for making xdelta patches... does not support patching with xdelta... because meh


class xDelta
{
    public static void Make(string original, string changed)
    {
        Make(original, changed, "");
    }
    public static void Make(string original, string changed, string outfile)
    {
        if (xDeltaExists())
        {
            if (outfile.Length == 0)
            {
                outfile = Path.GetDirectoryName(changed) + "\\" + Path.GetFileNameWithoutExtension(changed) + ".xdelta";
            }
            using (Process xd = new Process())
            {
                xd.StartInfo = new ProcessStartInfo(Path.GetDirectoryName(Application.ExecutablePath) + '\\' + "xDelta3.exe",
                    "-9 -S djw -e -vfs \"" + original + "\" \"" + changed + "\" \"" + outfile + "\"");
                xd.StartInfo.UseShellExecute = false;
                xd.StartInfo.RedirectStandardOutput = true;
                xd.StartInfo.RedirectStandardError = true;
                xd.Start();
                Console.WriteLine("xDelta Started...");
                while (!xd.HasExited)
                {
                    //xdelta uses standard error stream for normal readout...
                    //string line = xd.StandardOutput.ReadToEnd();
                    string err = xd.StandardError.ReadToEnd().Split(new string[] { "source size" }, StringSplitOptions.RemoveEmptyEntries)[1];
                    err = "Source Size:" + err.Replace("xdelta3: ", "").Replace("0: ", "").
                        Replace("blksize", "\r\nBlock Size:").Replace("window", "\r\nWindow:");
                    err = err.Replace(": total in ", " | Total In: ").Replace(": out ", " | Out: ").Replace("KiB:", "KiB |").Replace("MiB:", "MiB |");
                    err = err.Replace("finished in", "Finished in:").Replace("in ", "In: ").Replace("; input", "; Input:").Replace(" output", "; Output:");
                    string[] er = err.Split(new string[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string l in er)
                    {
                        Console.WriteLine(l);
                    }
                }
            }
        }
    }

    // accessor functions
    public static bool xDeltaExists()
    {
        bool retval = false;
        if (!File.Exists(Path.GetDirectoryName(Application.ExecutablePath) + '\\' + "xDelta3.exe"))
        {
            try
            {
                Assembly a = Assembly.GetExecutingAssembly();
                byte[] xd = MBuild.Properties.Resources.xdelta3_3_0_11_x86_64;
                File.WriteAllBytes(Path.GetDirectoryName(Application.ExecutablePath) + '\\' + "xDelta3.exe", xd);
                retval = true;
            }
            catch { Console.WriteLine("Unable to extract xDelta!"); }
        }
        else
        {
            retval = true;
        }
        return retval;
    }

    public static bool xDeltaCleanup()
    {
        bool retval = false;
        if (File.Exists(Path.GetDirectoryName(Application.ExecutablePath) + '\\' + "xDelta3.exe"))
        {
            try
            {
                File.Delete(Path.GetDirectoryName(Application.ExecutablePath) + '\\' + "xDelta3.exe");
                retval = true;
            }
            catch { if (Settings.Default.ShowCleanupWarnings) Console.WriteLine("Unable to clean up xDelta!"); }
        }
        else
        {
            retval = true;
        }
        return retval;
    }
}

