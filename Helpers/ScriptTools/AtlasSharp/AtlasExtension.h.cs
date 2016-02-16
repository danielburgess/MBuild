using System.Collections.Generic;

public static class GlobalMembersAtlasExtension
{

	public const uint MAX_RETURN_VAL = 3;

	public const uint NO_ACTION = 0;
	public const uint REPLACE_TEXT = 1;
	public const uint WRITE_POINTER = 2;
}

public class AtlasContext
{
	public uint CurrentLine;

	public LinkedList<TBL_STRING> StringTable = new LinkedList<TBL_STRING>();
	public FILE Target;

	public uint ScriptPos;
	public uint ScriptRemaining;

	public uint PointerValue;
	public uint PointerPosition;
	public uint PointerSize;
}

public delegate uint ExtensionFunction(AtlasContext[] Context);

public class ExtensionManager
{
	public ExtensionManager(VariableMap Map)
	{
		VarMap = Map;
	}
	public bool LoadExtension(string ExtId, string ExtensionFile)
	{
		// Use file extension to pick which derived AtlasExtension to use
		AtlasExtension Ext;
		uint Pos = ExtensionFile.LastIndexOfAny((Convert.ToString('.')).ToCharArray());
		if (Pos == -1 || Pos >= ExtensionFile.Length - 1)
		{
			return false;
		}
		else if (ExtensionFile.Substring(Pos + 1, ExtensionFile.Length - 1) == "dll") // dll
		{
		}
		else
		{
			Logger.ReportError(CurrentLine, "Unsupported file format used in LOADEXT");
			return false;
		}
    
		Ext = (AtlasExtension)VarMap.GetVar(ExtId).GetData();
		if (Ext != null)
		{
			Logger.ReportError(CurrentLine, "%s has alrady been initialized with LOADEXT", ExtId);
			Ext = null;
			return false;
		}
		Ext = null;
		Ext = new AtlasExtension();
		if (!Ext.LoadExtension(ExtensionFile))
		{
			Logger.ReportError(CurrentLine, "%s could not be loaded", ExtensionFile);
			Ext = null;
			return false;
		}
    
		VarMap.SetVarData(ExtId, Ext, P_EXTENSION);
		return true;
	}
	public uint ExecuteExtension(string ExtId, string FunctionName, AtlasContext[] Context)
	{
		AtlasExtension Ext;
		Ext = (AtlasExtension)VarMap.GetVar(ExtId).GetData();
		if (Ext == null)
		{
			Logger.ReportError(CurrentLine, "%s has not been initialized by LOADEXT", ExtId);
			return -1;
		}
		if (!Ext.IsLoaded())
		{
			ReportBug("Extension not loaded but initialized in ExtensionManager::ExecuteExtension");
			return -1;
		}
    
		ExtensionFunction Func = Ext.GetFunction(FunctionName);
		if (Func == null)
		{
			Logger.ReportError(CurrentLine, "Function %s was not found in the extension file", FunctionName);
			return -1;
		}
    
		uint Res = Func(Context);
		if (Res > MAX_RETURN_VAL)
		{
			Logger.ReportWarning(CurrentLine, "Extension returned invalid value %u", Res);
			Res = NO_ACTION;
		}
    
		return Res;
	}
	private VariableMap VarMap;
}

public class AtlasExtension
{
	public AtlasExtension()
	{
		this.Extension = null;
	}
	public void Dispose()
	{
		if (Extension)
		{
	//C++ TO C# CONVERTER NOTE: There is no C# equivalent to 'FreeLibrary':
	//		FreeLibrary(Extension);
		}
	}

	public bool LoadExtension(string ExtensionName)
	{
		Extension = LoadLibraryA(ExtensionName);
    
		if (Extension)
		{
			return true;
		}
		else
		{
			return false;
		}
	}
	public bool IsLoaded()
	{
		if (Extension)
		{
			return true;
		}
		else
		{
			return false;
		}
	}
	public ExtensionFunction GetFunction(string FunctionName)
	{
		if (Extension == null)
		{
			return null;
		}
    
		ExtensionFunction func = (ExtensionFunction)GetProcAddress(Extension, FunctionName);
    
		return func;
	}

	private System.IntPtr Extension;
}