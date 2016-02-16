using System;
using System.Collections;
using System.Collections.Generic;

class Verbs : IEnumerable
{
	public readonly Type Type;
	public readonly bool Background;
	readonly string shell = "\\shell";
	
	public Verbs (Type type, bool back)
	{
		Type = type;
		Background = back;
		if (Background) shell = "\\Background" + shell;
	}
	
	RegKey CRK { get { return Type.CR.Open(Type.Link + shell); } }
	RegKey CUK { get { return Type.CU.Open(Type.Link + shell); } }
	RegKey LMK { get { return Type.LM.Open(Type.Link + shell); } }
	RegKey SAK { get { return Type.SA.Open(Type.ID + shell); } }
	
	RegKey WCUK { get { return Type.CU.Open(Type.Link + shell, true); } }
	RegKey WLMK { get { return Type.LM.Open(Type.Link + shell, true); } }
	RegKey WSAK { get { return Type.SA.Open(Type.ID + shell, true); } }
	
	public string GetValue (string name) { return GetValue(null, name); }
	public string GetValue (string path, string name)
	{
		if (CRK != null)
		{
			RegKey k = (path != null) ? CRK.Open(path) : CRK;
			if (k != null && k[name] != null) return k[name];
		}
		
		if (SAK != null)
		{
			RegKey k = (path != null) ? SAK.Open(path) : SAK;
			if (k != null && k[name] != null) return k[name];
		}
		
		return null;
	}
	
	public void SetValue (string name, string value) { SetValue(null, name, value); }
	public void SetValue (string path, string name, string value)
	{
		string idp = Type.ID + shell; if (path != null) idp += "\\" + path;
		string lnp = Type.Link + shell; if (path != null) lnp += "\\" + path;
		
		if (value != null) Type.WCU.Ensure(lnp, true)[name] = value;
		else {
			if (Type.CU.HasKey(lnp) && Type.CU.Open(lnp).HasValue(name)) Type.WCU.Open(lnp, true)[name] = null;
			if (Type.LM.HasKey(lnp) && Type.LM.Open(lnp).HasValue(name)) Type.WLM.Open(lnp, true)[name] = null;
			if (Type.SA.HasKey(idp) && Type.SA.Open(idp).HasValue(name)) Type.WSA.Open(lnp, true)[name] = null;
		}
	}
	
	public bool HasKey (string path)
	{
		return (
			(CRK != null && CRK.HasKey(path)) ||
			(SAK != null && SAK.HasKey(path))
		);
	}
	
	public void ZapKey (string path)
	{
		if (CUK != null && CUK.HasKey(path)) WCUK.ZapKey(path);
		if (LMK != null && LMK.HasKey(path)) WLMK.ZapKey(path);
		if (SAK != null && SAK.HasKey(path)) WSAK.ZapKey(path);
	}
	
	public void NewKey (string path)
	{
		Type.WCU.Ensure(Type.Link + shell + "\\" + path);
	}
	
	public Verb this [string id]
	{
		get { return new Verb(this, id); }
	}
	
	public string Default
	{
		get {
			string def = GetValue(null);
			if (def == null) return null;
			return def.Split(' ')[0];
		}
		
		set {
			if (IsGoodID(value)) SetValue(null, value);
			else throw new Exception("Bad verb key: " + value);
		}
	}
	
	public bool Have (string id)
	{
		return HasKey(id);
	}
	
	static bool IsGoodID (string id)
	{
		return !id.Contains(" ") && !id.Contains("\\");
	}
	
	static string SanitizeID (string id)
	{
		if (IsGoodID(id)) return id;
		
		id = id.Replace(" ", "_");
		id = id.Replace("\\", "_");
		
		return id;
	}
	
	static string SaltID (string id)
	{
		return id + "_";
	}
	
	public Verb AddVerbWithTitle (string title)
	{
		string id = SanitizeID(title);
		while (Have(id)) id = SaltID(id);
		
		Type.WCU.Ensure(Type.Link + shell + "\\" + id, true)[null] = title;
		
		return new Verb(this, id);
	}
	
	public void Delete (string id)
	{
		if (CUK != null && CUK.HasKey(id)) WCUK.ZapKey(id);
		if (LMK != null && LMK.HasKey(id)) WLMK.ZapKey(id);
		if (SAK != null && SAK.HasKey(id)) WSAK.ZapKey(id);
	}
	
	public IEnumerator GetEnumerator ()
	{
		List<string> ids = new List<string>();
		
		string bg = Background ? "\\Background" : "";
		
		RegKey crk = Type.CR.Open(Type.Link + shell);
		RegKey sak = Type.SA.Open(Type.ID + shell);
		
		if (crk != null) ids.AddRange(crk.Keys);
		if (sak != null) {
			foreach (string id in sak.Keys) {
				if (!ids.Contains(id)) ids.Add(id);
			}
		}
		
		foreach (string id in ids) yield return this[id];
	}
}