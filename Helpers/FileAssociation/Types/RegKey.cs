using System;
using Microsoft.Win32;

class RegKey : IDisposable
{
	protected readonly RegistryKey TheKey;
	protected RegKey (RegistryKey rkey) { TheKey = rkey; }
	
	public static RegKey CR = new RegKey(Registry.ClassesRoot);
	public static RegKey CU = new RegKey(Registry.CurrentUser);
	public static RegKey LM = new RegKey(Registry.LocalMachine);
	
	public RegKey Open (string path) { return Open(path, false); }
	public RegKey Open (string path, bool write)
	{
		RegistryKey k = TheKey.OpenSubKey(path, write);
		if (k != null) return new RegKey(k); else return null;
	}
	
	public string[] Keys { get { return TheKey.GetSubKeyNames(); } }
	public string[] Values { get { return TheKey.GetValueNames(); } }
	
	public string this [string name]
	{
		get {
			object v = TheKey.GetValue(name);
			return v != null ? v.ToString() : null;
		}
		
		set {
			if (name == null) name = "";
			if (value == null) TheKey.DeleteValue(name);
			else TheKey.SetValue(name, value);
		}
	}
	
	public string Default
	{
		get { return this[null]; }
		set { this[null] = value; }
	}
	
	public RegKey Ensure (string path) { return Ensure (path, false); }
	public RegKey Ensure (string path, bool write)
	{
		string[] steps = path.Split('\\');
		
		RegistryKey root = TheKey;
		
		for (int i = 0; i < steps.Length; i++)
		{
			string step = steps[i];
			
			RegistryKey stepKey = root.OpenSubKey(step);
			if (stepKey == null) stepKey = root.CreateSubKey(step);
			else if (i + 1 < steps.Length) {
				RegistryKey nextKey = stepKey.OpenSubKey(steps[i + 1]);
				if (nextKey == null) stepKey = root.OpenSubKey(step, true);
			};
			
			if (i == steps.Length - 1) root = root.OpenSubKey(step, write);
			else root = stepKey;
		}
		
		return new RegKey(root);
	}
	
	public bool HasKey (string path)
	{
		return TheKey.OpenSubKey(path) != null;
	}
	
	public bool HasValue (string name)
	{
		return TheKey.GetValue(name) != null;
	}
	
	public void ZapKey (string path)
	{
		using (RegKey tgt = Open(path))
		{
			string[] sks = tgt.Keys;
			
			if (sks.Length > 0)
			{
				using (RegKey wtgt = Open(path, true))
				{
					foreach (string sk in sks)
					{
						wtgt.ZapKey(sk);
					}
				}
			}
		}
		
		TheKey.DeleteSubKey(path);
	}
	
	public static void Clone (RegKey kf, RegKey kt)
	{
		foreach (string v in kf.Values) kt[v] = kf[v];
		foreach (string k in kf.Keys) Clone(kf.Open(k), kt.Ensure(k));
	}
	
	public void Dispose ()
	{
		if (TheKey != null) ((IDisposable)TheKey).Dispose();
	}
}