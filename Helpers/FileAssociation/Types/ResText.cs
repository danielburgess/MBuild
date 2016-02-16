using System;
using System.Runtime.InteropServices;
using System.Text;

static class ResText
{
	public static string Get (string text)
	{
		if (text == null) return text;
		if (!text.StartsWith("@")) return text;
		
		string[] spl = text.Substring(1).Split(',');
		if (spl.Length != 2) return text;
		
		string file = spl[0].Trim();
		if (file == "") return text;
		
		uint index;
		try { index = (uint) Math.Abs(Convert.ToInt32(spl[1].Trim())); }
		catch { return text; }
		
		const int DONT_RESOLVE_DLL_REFERENCES = 0x00000001;
		IntPtr lib = LoadLibraryEx(file, 0, DONT_RESOLVE_DLL_REFERENCES);
		if (lib == IntPtr.Zero) return text;
		
		StringBuilder sb = new StringBuilder(1024);
		int l = LoadString(lib, index, sb, sb.Capacity);
		if (l != 0) text = sb.ToString();
		
		FreeLibrary(lib);
		return text;
	}
	
	[DllImport("kernel32.dll")] static extern IntPtr LoadLibraryEx (string lpFileName, int hFile, uint dwFlags);
	[DllImport("user32.dll", CharSet = CharSet.Auto)] static extern int LoadString (IntPtr hInstance, uint uID, StringBuilder lpBuffer, int nBufferMax);
	[DllImport("kernel32.dll")] static extern int FreeLibrary (IntPtr hModule);
}