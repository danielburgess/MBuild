using System;
using System.Collections.Generic;

class Type
{
	static public readonly RegKey CR = RegKey.CR;
	static public readonly RegKey FX = RegKey.CU.Open(@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts");
	static public readonly RegKey CU = RegKey.CU.Open(@"Software\Classes");
	static public readonly RegKey LM = RegKey.LM.Open(@"Software\Classes");
	static public readonly RegKey SA = RegKey.LM.Open(@"Software\Classes\SystemFileAssociations");
	
	static public RegKey WFX { get { return RegKey.CU.Open(@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts", true); } }
	static public RegKey WCU { get { return RegKey.CU.Open(@"Software\Classes", true); } }
	static public RegKey WLM { get { return RegKey.LM.Open(@"Software\Classes", true); } }
	static public RegKey WSA { get { return RegKey.LM.Open(@"Software\Classes\SystemFileAssociations", true); } }
	
	public readonly string ID;
	public Type (string id) { ID = id; }
	
	#region Display
		
		public string Title
		{
			get {
				RegKey k = CR.Open(Link);
				if (k == null) return null;
				string ftn = k["FriendlyTypeName"];
				if (ftn != null) return ResText.Get(ftn);
				else return k.Default;
			}
			
			set {
				WCU.Ensure(Link, true)["FriendlyTypeName"] = value;
			}
		}
		
		public string Icon
		{
			get { return Actual.GetValue("DefaultIcon", null); }
			set { Actual.SetValue("DefaultIcon", null, value); }
		}
		
	#endregion
	
	#region Access
		
		string GetValue (string name) { return GetValue(null, name); }
		string GetValue (string path, string name)
		{
			string act = ID;
			if (path != null) act += "\\" + path;
			
			RegKey k = CR.Open(act);
			
			if (k != null) return k[name];
			else return null;
		}
		
		void SetValue (string name, string value) { SetValue(null, name, value); }
		void SetValue (string path, string name, string value)
		{
			string act = ID;
			if (path != null) act += "\\" + path;
			
			if (value != null) WCU.Ensure(act, true)[name] = value;
			else {
				if (CU.HasKey(act) && CU.Open(act).HasValue(name)) WCU.Open(act, true)[name] = null;
				if (LM.HasKey(act) && LM.Open(act).HasValue(name)) WLM.Open(act, true)[name] = null;
			}
		}
		
	#endregion
	
	#region Class
		
		public string Class
		{
			get
			{
				RegKey ek = FX.Open(ID + "\\UserChoice"); if (ek != null)
				{
					string progid = ek["ProgID"];
					if (progid != null && ClassExists(progid)) return progid;
				}
				
				RegKey rk = CR.Open(ID); if (rk != null)
				{
					string co = rk.Default;
					if (co != null && ClassExists(co)) return co;
				}
				
				return null;
			}
			
			set
			{
				string fxk = ID + "\\UserChoice"; if (FX.HasKey(fxk)) WFX.ZapKey(fxk);
				string pid = ID + "\\OpenWithProgids"; if (FX.HasKey(pid)) WFX.ZapKey(pid);
				
				if (value != null) WCU.Ensure(ID, true).Default = value;
				else {
					if (CU.HasKey(ID) && CU.Open(ID).Default != null && Exists(CU.Open(ID).Default)) WCU.Open(ID, true).Default = null;
					if (LM.HasKey(ID) && LM.Open(ID).Default != null && Exists(LM.Open(ID).Default)) WLM.Open(ID, true).Default = null;
				}
			}
		}
		
		public string Link
		{
			get {
				if (Class != null) return Class;
				else return ID;
			}
		}
		
		public Type Actual
		{
			get { return new Type(Link); }
		}
		
	#endregion
	
	#region Misc
		
		public string Perceived
		{
			get { return GetValue("PerceivedType"); }
			set { SetValue("PerceivedType", value); }
		}
		
		public int ShowExtension
		{
			get
			{
				if (Actual.GetValue("AlwaysShowExt") != null) return +1;
				if (Actual.GetValue("NeverShowExt") != null) return -1;
				
				RegKey sk = SA.Open(ID); if (sk != null)
				{
					if (sk.HasValue("AlwaysShowExt")) return +1;
					if (sk.HasValue("NeverShowExt")) return -1;
				}
				
				return 0;
			}
			
			set
			{
				bool curalws = (Actual.GetValue("AlwaysShowExt") != null);
				bool curnevr = (Actual.GetValue("NeverShowExt") != null);
				
				bool setalws = (value > 0);
				bool setnevr = (value < 0);
				
				if (curalws && !setalws) Actual.SetValue("AlwaysShowExt", null);
				if (curnevr && !setnevr) Actual.SetValue("NeverShowExt", null);
				if (setalws && !curalws) Actual.SetValue("AlwaysShowExt", "");
				if (setnevr && !curnevr) Actual.SetValue("NeverShowExt", "");
				
				RegKey sk = SA.Open(ID); if (sk != null)
				{
					bool sacuralws = sk.HasValue("AlwaysShowExt");
					bool sacurnevr = sk.HasValue("NeverShowExt");
					
					RegKey wsk = SA.Open(ID, true);
					
					if (sacuralws && !setalws) wsk["AlwaysShowExt"] = null;
					if (sacurnevr && !setnevr) wsk["NeverShowExt"] = null;
				}
			}
		}
		
	#endregion
	
	#region Management
		
		public static string[] Extensions
		{
			get {
				List<string> list = new List<string>(CR.Keys);
				foreach (string t in FX.Keys) if (!list.Contains(t)) list.Add(t);
				string[] alist = Array.FindAll(list.ToArray(), IsValidExtension);
				Array.Sort(alist); return alist;
			}
		}
		
		public static string[] Classes
		{
			get { return Array.FindAll(CR.Keys, IsValidClass); }
		}
		
		public static bool ClassExists (string type)
		{
			return IsValidClass(type) && CR.HasKey(type);
		}
		
		public static bool Exists (string type)
		{
			return CR.HasKey(type) || FX.HasKey(type);
		}
		
		public static void Create (string type)
		{
			WCU.Ensure(type);
		}
		
		public static void Delete (string type)
		{
			Exception ee = null;
			
			if (CU.HasKey(type)) { try { WCU.ZapKey(type); } catch (Exception e) { ee = e; } }
			if (FX.HasKey(type)) { try { WFX.ZapKey(type); } catch (Exception e) { ee = e; } }
			if (LM.HasKey(type)) { try { WLM.ZapKey(type); } catch (Exception e) { ee = e; } }
			
			if (ee != null) throw ee;
		}
		
		static void Clone (string from, string to)
		{
			RegKey tk = WCU.Ensure(to);
			
			using (RegKey fk = LM.Open(from)) { if (fk != null) RegKey.Clone(fk, tk); }
			using (RegKey fk = CU.Open(from)) { if (fk != null) RegKey.Clone(fk, tk); }
		}
		
		public static void CloneExtension (string from, string to)
		{
			Clone(from, to);
			
			RegKey tuk = WFX.Ensure(to + "\\UserChoice");
			RegKey fuk = FX.Open(from + "\\UserChoice");
			
			if (fuk != null)
			{
				string pid = fuk["Progid"];
				if (pid != null) tuk["Progid"] = pid;
			}
		}
		
	#endregion
	
	#region Service
		
		public static bool IsValidExtension (string test)
		{
			return (
				test.Length > 1 && test.StartsWith(".") &&
				!test.Substring(1).Contains(".") &&
				test.IndexOfAny("\\/:*?\"><|".ToCharArray()) == -1
			);
		}
		
		public static bool IsValidClass (string test)
		{
			return (
				test.Length > 0 && !test.StartsWith(".") &&
				test != "SystemFileAssociations" &&
				test != "CLSID" && test != "Interface" &&
				test != "TypeLib" && test != "AppID" &&
				test != "Applications"
			);
		}
		
	#endregion
	
	#region Verbs
		
		public Verbs Verbs {
			get {
				return new Verbs(this, false);
			}
		}
		
		public Verbs BackgroundVerbs {
			get {
				if (CR.HasKey(ID + "\\Background")) return new Verbs(this, true);
				else return null;
			}
		}
		
	#endregion
}