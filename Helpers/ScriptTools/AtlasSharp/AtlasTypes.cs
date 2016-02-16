using System.Collections.Generic;

public static class GlobalMembersAtlasTypes
{
	/* Misc Functions */
	internal const uint CMD_JMP1 = 0;
	internal const uint CMD_JMP2 = 1;
	internal const uint CMD_SMA = 2;
	internal const uint CMD_HDR = 3;
	internal const uint CMD_STRTYPE = 4;
	internal const uint CMD_ADDTBL = 5;
	internal const uint CMD_ACTIVETBL = 6;
	internal const uint CMD_VAR = 7;
	/* Pointer Functions */
	internal const uint CMD_WUB = 8;
	internal const uint CMD_WBB = 9;
	internal const uint CMD_WHB = 10;
	internal const uint CMD_WLB = 11;
	internal const uint CMD_W16 = 12;
	internal const uint CMD_W24 = 13;
	internal const uint CMD_W32 = 14;
	internal const uint CMD_EMBSET = 15;
	internal const uint CMD_EMBTYPE = 16;
	internal const uint CMD_EMBWRITE = 17;
	/* Debugging Functions */
	internal const uint CMD_BREAK = 18;
	/* Extended Pointer Functionality */
	internal const uint CMD_PTRTBL = 19;
	internal const uint CMD_WRITETBL = 20;
	internal const uint CMD_PTRLIST = 21;
	internal const uint CMD_WRITELIST = 22;
	internal const uint CMD_AUTOWRITETBL = 23;
	internal const uint CMD_AUTOWRITELIST = 24;
	internal const uint CMD_CREATEPTR = 25;
	internal const uint CMD_WRITEPTR = 26;

	internal const uint CMD_LOADEXT = 27;
	internal const uint CMD_EXECEXT = 28;
	internal const uint CMD_DISABLETABLE = 29;
	internal const uint CMD_DISABLELIST = 30;
	internal const uint CMD_PASCALLEN = 31;
	internal const uint CMD_AUTOEXEC = 32;
	internal const uint CMD_DISABLEEXEC = 33;
	internal const uint CMD_FIXEDLENGTH = 34;

	internal const uint CMD_WUBCUST = 35;
	internal const uint CMD_WBBCUST = 36;
	internal const uint CMD_WHBCUST = 37;
	internal const uint CMD_WLBCUST = 38;
	internal const uint CMD_ENDIANSWAP = 39;
	internal const uint CMD_STRINGALIGN = 40;

	// Add these commands!
	internal const uint CMD_EMBPTRTABLE = 41;
	internal const uint CMD_WHW = 42;
	internal const uint CMD_WHWCUST = 43;
	internal const uint CMD_SETTARGETFILE = 44;
	internal const uint CMD_SETPTRFILE = 45;
	internal const uint CMD_WRITEEMBTBL1 = 46;
	internal const uint CMD_WRITEEMBTBL2 = 47;
	internal const uint CMD_WRITETBL2 = 48;

	internal const uint CommandCount = 49;

	internal string[] CommandStrings = {"JMP", "JMP", "SMA", "HDR", "STRTYPE", "ADDTBL", "ACTIVETBL", "VAR", "WUB", "WBB", "WHB", "WLB", "W16", "W24", "W32", "EMBSET", "EMBTYPE", "EMBWRITE", "BREAK", "PTRTBL", "WRITE", "PTRLIST", "WRITE", "AUTOWRITE", "AUTOWRITE", "CREATEPTR", "WRITE", "LOADEXT", "EXECEXT", "DISABLE", "DISABLE", "PASCALLEN", "AUTOEXEC", "DISABLE", "FIXEDLENGTH", "WUB", "WBB", "WHB", "WLB", "ENDIANSWAP", "STRINGALIGN", "EMBPTRTBL", "WHW", "WHW", "SETTARGETFILE", "SETPTRFILE", "WRITE", "WRITE", "WRITE"};

	// Parameter types
	internal const uint TypeCount = 12;

	internal const uint P_INVALID = 0;
	internal const uint P_VOID = 1;
	internal const uint P_STRING = 2;
	internal const uint P_VARIABLE = 3;
	internal const uint P_NUMBER = 4;
	internal const uint P_DOUBLE = 5;
	internal const uint P_TABLE = 6;
	internal const uint P_POINTERTABLE = 7;
	internal const uint P_EMBPOINTERTABLE = 8;
	internal const uint P_POINTERLIST = 9;
	internal const uint P_CUSTOMPOINTER = 10;
	internal const uint P_EXTENSION = 11;

	internal string[] TypeStrings = {"INVALID", "VOID", "STRING", "VARIABLE", "NUMBER", "DOUBLE", "TABLE", "POINTERTABLE", "EMBPOINTERTABLE", "POINTERLIST", "CUSTOMPOINTER", "EXTENSION"};

	internal readonly uint[,] Types =
	{
		{P_NUMBER, 0, 0, 0, 0},
		{P_NUMBER, P_NUMBER, 0, 0, 0},
		{P_STRING, 0, 0, 0, 0},
		{P_NUMBER, 0, 0, 0, 0},
		{P_STRING, 0, 0, 0, 0},
		{P_STRING, P_TABLE, 0, 0, 0},
		{P_TABLE, 0, 0, 0, 0},
		{P_VARIABLE, P_VARIABLE, 0, 0, 0},
		{P_NUMBER, 0, 0, 0, 0},
		{P_NUMBER, 0, 0, 0, 0},
		{P_NUMBER, 0, 0, 0, 0},
		{P_NUMBER, 0, 0, 0, 0},
		{P_NUMBER, 0, 0, 0, 0},
		{P_NUMBER, 0, 0, 0, 0},
		{P_NUMBER, 0, 0, 0, 0},
		{P_NUMBER, 0, 0, 0, 0},
		{P_STRING, P_NUMBER, P_NUMBER, 0, 0},
		{P_NUMBER, 0, 0, 0, 0},
		{P_VOID, 0, 0, 0, 0},
		{P_POINTERTABLE, P_NUMBER, P_NUMBER, P_CUSTOMPOINTER, 0},
		{P_POINTERTABLE, 0, 0, 0, 0},
		{P_POINTERLIST, P_STRING, P_CUSTOMPOINTER, 0, 0},
		{P_POINTERLIST, 0, 0, 0, 0},
		{P_POINTERTABLE, P_STRING, 0, 0, 0},
		{P_POINTERLIST, P_STRING, 0, 0, 0},
		{P_CUSTOMPOINTER, P_STRING, P_NUMBER, P_NUMBER, 0},
		{P_CUSTOMPOINTER, P_NUMBER, 0, 0, 0},
		{P_EXTENSION, P_STRING, 0, 0, 0},
		{P_EXTENSION, P_STRING, 0, 0, 0},
		{P_POINTERTABLE, P_STRING, 0, 0, 0},
		{P_POINTERLIST, P_STRING, 0, 0, 0},
		{P_NUMBER, 0, 0, 0, 0},
		{P_EXTENSION, P_STRING, P_STRING, 0, 0},
		{P_STRING, P_STRING, 0, 0, 0},
		{P_NUMBER, P_NUMBER, 0, 0, 0},
		{P_CUSTOMPOINTER, P_NUMBER, 0, 0, 0},
		{P_CUSTOMPOINTER, P_NUMBER, 0, 0, 0},
		{P_CUSTOMPOINTER, P_NUMBER, 0, 0, 0},
		{P_CUSTOMPOINTER, P_NUMBER, 0, 0, 0},
		{P_STRING, 0, 0, 0, 0},
		{P_NUMBER, 0, 0, 0, 0},
		{P_EMBPOINTERTABLE, P_NUMBER, P_CUSTOMPOINTER, 0, 0},
		{P_NUMBER, 0, 0, 0, 0},
		{P_CUSTOMPOINTER, P_NUMBER, 0, 0, 0},
		{P_STRING, 0, 0, 0, 0},
		{P_STRING, 0, 0, 0, 0},
		{P_EMBPOINTERTABLE, 0, 0, 0, 0},
		{P_EMBPOINTERTABLE, P_NUMBER, 0, 0, 0},
		{P_POINTERTABLE, P_NUMBER, 0, 0, 0}
	}; // JMP1 JMP2

	internal readonly uint[] ParamCount = {1, 2, 1, 1, 1, 2, 1, 2, 1, 1, 1, 1, 1, 1, 1, 1, 3, 1, 1, 4, 1, 3, 1, 2, 2, 4, 2, 2, 2, 2, 2, 1, 3, 2, 2, 2, 2, 2, 2, 1, 1, 3, 1, 2, 1, 1, 1, 2, 2}; // JMP1 JMP2 SMA
}

public class Parameter
{
	public string Value;
	public uint Type;
}

public class Command
{
	public uint Function;
	public List<Parameter> Parameters = new List<Parameter>();
	public uint Line;
}

public class AtlasBlock
{
	public LinkedList<Command> Commands = new LinkedList<Command>();
	public LinkedList<string> TextLines = new LinkedList<string>();
	public uint StartLine;
}


