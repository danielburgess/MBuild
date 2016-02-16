using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
//Pulled from the Types project: http://ystr.github.io/
static class CTypes
{
	static bool FlushOnExit = false;
	
	static int PassIndex;
	static string[] RawArgs;
	
	static Type CtxType = null;
	static Verb CtxVerb = null;
	
	static string ReadParameter ()
	{
		PassIndex++; if (PassIndex >= RawArgs.Length) throw new SyntaxError();
		return RawArgs[PassIndex];
	}
	
	static void Process ()
	{
		switch (RawArgs[PassIndex].ToLower())
		{
			case "-flush": case "-f":
				FlushOnExit = true;
			break;
			
			case "-list-extensions": case "-lx":
				foreach (string t in Type.Extensions) Console.WriteLine(t);
			break;
			
			case "-list-classes": case "-lc":
				foreach (string t in Type.Classes) Console.WriteLine(t);
			break;
			
			case "-delete-type": case "-dt":
				Type.Delete(ReadParameter());
			break;
			
			case "-type": case "-t":
				CtxType = new Type(ReadParameter());
			break;
			
			default:
				CtxType = new Type(RawArgs[PassIndex]);
			break;
		}
	}
	
	static void ProcessType ()
	{
		switch (RawArgs[PassIndex].ToLower())
		{
			case "-get-class": case "-gc":
				Console.WriteLine(CtxType.Class);
			break;
			
			case "-set-class": case "-sc":
				CtxType.Class = ReadParameter();
			break;
			
			case "-get-default": case "-gd":
				Console.WriteLine(CtxType.Verbs.Default);
			break;
			
			case "-set-default": case "-sd":
				CtxType.Verbs.Default = ReadParameter();
			break;
			
			case "-list-verbs": case "-lv":
				foreach (Verb v in CtxType.Verbs) Console.WriteLine(v.ID);
			break;
			
			case "-delete-verb": case "-dv":
				CtxType.Verbs.Delete(ReadParameter());
			break;
			
			case "-get-extension-visibility": case "-gxv":
				
				switch (CtxType.ShowExtension)
				{
					case -1: Console.WriteLine("Hidden"); break;
					case  0: Console.WriteLine("Default"); break;
					case +1: Console.WriteLine("Visible"); break;
				}
				
			break;
			
			case "-set-extension-visibility": case "-sxv":
				
				switch (ReadParameter().ToLower())
				{
					case "hidden": case "h": CtxType.ShowExtension = -1; break;
					case "default": case "d": CtxType.ShowExtension = 0; break;
					case "visible": case "v": CtxType.ShowExtension = +1; break;
				}
				
			break;
			
			case "-get-icon": case "-gi":
				Console.WriteLine(CtxType.Icon);
			break;
			
			case "-set-icon": case "-si":
				CtxType.Icon = ReadParameter();
			break;
			
			case "-get-perceived": case "-gp":
				Console.WriteLine(CtxType.Perceived);
			break;
			
			case "-set-perceived": case "-sp":
				CtxType.Perceived = ReadParameter();
			break;
			
			case "-get-name": case "-gn":
				Console.WriteLine(CtxType.Title);
			break;
			
			case "-set-name": case "-sn":
				CtxType.Title = ReadParameter();
			break;
			
			case "-associate": case "-a":
				CtxType.Verbs["open"].Command = "\"" + ReadParameter() + "\" \"%1\"";
				CtxType.Verbs.Default = "open";
			break;
			
			case "-verb": case "-v":
				CtxVerb = CtxType.Verbs[ReadParameter()];
			break;
			
			default:
				CtxVerb = CtxType.Verbs[RawArgs[PassIndex]];
			break;
		}
	}
	
	static void ProcessVerb ()
	{
		switch (RawArgs[PassIndex].ToLower())
		{
			case "-get-command": case "-gc":
				Console.WriteLine(CtxVerb.Command);
			break;
			
			case "-set-command": case "-sc":
				CtxVerb.Command = ReadParameter();
			break;
			
			case "-get-hidden": case "-gh":
				Console.WriteLine(CtxVerb.IsExtended ? "True" : "False");
			break;
			
			case "-set-hidden": case "-sh":
				string p = ReadParameter().ToLower();
				if (p == "true" || p == "t") CtxVerb.IsExtended = true;
				else if (p == "false" || p == "f") CtxVerb.IsExtended = false;
				else throw new SyntaxError();
			break;
			
			case "-type": case "-t":
				CtxType = new Type(ReadParameter());
				CtxVerb = null;
			break;
			
			case "-get-icon": case "-gi":
				Console.WriteLine(CtxVerb.Icon);
			break;
			
			case "-set-icon": case "-si":
				CtxVerb.Icon = ReadParameter();
			break;
			
			case "-get-name": case "-gn":
				Console.WriteLine(CtxVerb.Title);
			break;
			
			case "-set-name": case "-sn":
				CtxVerb.Title = ReadParameter();
			break;
			
			case "-get-program": case "-gp":
				Console.WriteLine(CtxVerb.Program);
			break;
			
			case "-set-program": case "-sp":
				CtxVerb.Command = "\"" + ReadParameter() + "\" \"%1\"";
			break;
			
			case "-verb": case "-v":
				CtxVerb = CtxType.Verbs[ReadParameter()];
			break;
			
			default:
				CtxVerb = CtxType.Verbs[RawArgs[PassIndex]];
			break;
		}
	}
	
	public static int Main (string[] args)
	{ 
		RawArgs = args;
		
		if (args.Length == 0)
		{
			Console.WriteLine (
				new StreamReader (
					Assembly.GetExecutingAssembly().GetManifestResourceStream("Help.txt")
				).ReadToEnd()
			);
			
			return 0;
		}
		
		#if !DEBUG
			try {
		#endif
		
		for (PassIndex = 0; PassIndex < RawArgs.Length; PassIndex++)
		{
			if (CtxVerb != null) ProcessVerb();
			else if (CtxType != null) ProcessType();
			else Process();
		}
		
		if (FlushOnExit) FlushIcons();
		return 0;
		
		#if !DEBUG
			} catch (Exception x) { Console.WriteLine(x.Message); return 1; }
		#endif
	}
	
	[DllImport("shell32.dll")]
	static extern void SHChangeNotify (uint wEventId, uint uFlags, int dwItem1, int dwItem2);
	public static void FlushIcons () { SHChangeNotify(0x08000000, 0x0000, 0, 0); }
	
	class SyntaxError : Exception
	{
		public SyntaxError () : base ("Bad syntax") {}
		public SyntaxError (string msg) : base (msg) {}
	}
}