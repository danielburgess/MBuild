using System;

class Verb
{
	public readonly Verbs Verbs;
	public readonly string ID;
	
	public Verb (Verbs verbs, string id)
	{
		Verbs = verbs;
		ID = id;
	}
	
	string GetValue (string name) { return GetValue(null, name); }
	string GetValue (string path, string name) {
		return Verbs.GetValue(path != null ? ID + "\\" + path : ID, name);
	}
	
	void SetValue (string name, string value) { SetValue(null, name, value); }
	void SetValue (string path, string name, string value) {
		Verbs.SetValue(path != null ? ID + "\\" + path : ID, name, value);
	}
	
	bool HasKey (string path) { return Verbs.HasKey(ID + "\\" + path); }
	void ZapKey (string path) { Verbs.ZapKey(ID + "\\" + path); }
	void NewKey (string path) { Verbs.NewKey(ID + "\\" + path); }
	
	public bool IsDefault
	{
		get { return String.Compare(Verbs.Default, ID, true) == 0; }
		set { Verbs.Default = ID; }
	}
	
	public string Title
	{
		get {
			string title = GetValue(null);
			if (title == null) title = ID;
			return ResText.Get(title);
		}
		
		set {
			SetValue(null, value);
		}
	}
	
	public string Icon
	{
		get { return GetValue("icon"); }
		set { SetValue("icon", value); }
	}
	
	public string Command
	{
		get { return GetValue("command", null); }
		set { SetValue("command", null, value); }
	}
	
	public string Program
	{
		get
		{
			string com = Command.Trim();
			
			if (com.StartsWith("\"")) com = com.Split('"')[1];
			else if (com.Contains(" ")) com = com.Split(' ')[0];
			
			return com;
		}
	}
	
	public bool IsExtended
	{
		get { return GetValue("extended") != null; }
		set { SetValue("extended", value ? "" : null); }
	}
	
	#region DDE
		
		public bool HasDDE
		{
			get { return HasKey("ddeexec"); }
			set { if (value) NewKey("ddeexec"); else ZapKey("ddeexec"); }
		}
		
		public string DDEMessage
		{
			get { return GetValue("ddeexec", null); }
			set { SetValue("ddeexec", null, value); }
		}
		
		public string DDEApp
		{
			get { return GetValue("ddeexec\\Application", null); }
			set { SetValue("ddeexec\\Application", null, value); }
		}
		
		public string DDENotRunning
		{
			get { return GetValue("ddeexec\\IfExec", null); }
			set { SetValue("ddeexec\\IfExec", null, value); }
		}
		
		public string DDETopic
		{
			get { return GetValue("ddeexec\\Topic", null); }
			set { SetValue("ddeexec\\Topic", null, value); }
		}
		
	#endregion
}