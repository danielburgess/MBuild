using System;

public static class GlobalMembersAtlasCore
{
	//-----------------------------------------------------------------------------
	// AtlasCore - A class to insert Atlas-type scripts
	// By Steve Monaco (stevemonaco@hotmail.com)
	//-----------------------------------------------------------------------------



	// Constructor

	public static AtlasCore Atlas = new AtlasCore();
	public static uint CurrentLine = 1;
	public static int bSwap = 0;
	public static int StringAlign = 0;
	public static int MaxEmbPtr = 0;

	//-----------------------------------------------------------------------------
	// StringToUInt() - Converts a $ABCD string from hexadecimal radix else decimal
	// Status - Working
	//-----------------------------------------------------------------------------

	public static uint StringToUInt(string NumberString)
	{
		uint offset = 0;

		if (NumberString[0] == '$')
		{
			offset = strtoul(NumberString.Substring(1, NumberString.Length).c_str(), null, 16);
		}
		else
		{
			offset = strtoul(NumberString, null, 10);
		}

		return offset;
	}

	//-----------------------------------------------------------------------------
	// StringToInt64() - Converts a string to int64
	// Status - Works+Fixed
	//-----------------------------------------------------------------------------

	public static long StringToInt64(string NumberString)
	{
		long Num = 0;
		bool bNeg = false;
		uint Pos = 0;
		uint long Mult;

		if (NumberString[Pos] == '$') // hex
		{
			Pos++;
			if (NumberString[Pos] == '-')
			{
				bNeg = true;
				Pos++;
			}
			uint i = NumberString.Length - 1;
			Num += GetHexDigit(NumberString[i]);
			i--;
			Mult = 16;
			for (i; i >= Pos; i--, Mult *= 16)
			{
				Num += Mult * GetHexDigit(NumberString[i]);
			}
		}
		else // dec
		{
			if (NumberString[Pos] == '-')
			{
				bNeg = true;
				Pos++;
			}
			uint i = NumberString.Length - 1;
			Num += GetHexDigit(NumberString[i]);
			if (i != 0)
			{
				i--;
				Mult = 10;
				for (i; i > Pos; i--, Mult *= 10)
				{
					Num += Mult * (NumberString[i] - '0');
				}
				Num += Mult * (NumberString[i] - '0'); // prevent underflow of i
			}
		}

		if (bNeg)
		{
			Num = -Num;
		}
		return Num;
	}

	public static uint GetHexDigit(sbyte digit)
	{
		switch (digit)
		{
		case '0':
			return 0;
		case '1':
			return 1;
		case '2':
			return 2;
		case '3':
			return 3;
		case '4':
			return 4;
		case '5':
			return 5;
		case '6':
			return 6;
		case '7':
			return 7;
		case '8':
			return 8;
		case '9':
			return 9;
		case 'A':
	case 'a':
			return 10;
		case 'B':
	case 'b':
			return 11;
		case 'C':
	case 'c':
			return 12;
		case 'D':
	case 'd':
			return 13;
		case 'E':
	case 'e':
			return 14;
		case 'F':
	case 'f':
			return 15;
		default:
			return 0;
		}
	}

	public static uint EndianSwap(uint Num, int Size)
	{
		uint a = 0;
		switch (Size)
		{
		case 1:
			return Num;
		case 2:
			a = (Num & 0xFF00) >> 8;
			a |= (Num & 0x00FF) << 8;
			return a;
		case 3:
			a = (Num & 0xFF) << 16;
			a |= (Num & 0xFF00);
			a |= (Num & 0xFF0000) >> 16;
			return a;
		case 4:
			a = (Num & 0xFF) << 24;
			a |= (Num & 0xFF00) << 8;
			a |= (Num & 0xFF0000) >> 8;
			a |= (Num & 0xFF000000) >> 24;
			return a;
		}

		return -1;
	}
}


// Destructor







//C++ TO C# CONVERTER NOTE: This was formerly a static local variable declaration (not allowed in C#):
private uint ExecuteCommand_PtrValue;
//C++ TO C# CONVERTER NOTE: This was formerly a static local variable declaration (not allowed in C#):
private uint ExecuteCommand_PtrNum;
//C++ TO C# CONVERTER NOTE: This was formerly a static local variable declaration (not allowed in C#):
private uint ExecuteCommand_PtrPos;
//C++ TO C# CONVERTER NOTE: This was formerly a static local variable declaration (not allowed in C#):
private uint ExecuteCommand_Size;
//C++ TO C# CONVERTER NOTE: This was formerly a static local variable declaration (not allowed in C#):
private bool ExecuteCommand_Success;
//C++ TO C# CONVERTER NOTE: This was formerly a static local variable declaration (not allowed in C#):
private byte ExecuteCommand_PtrByte;
//C++ TO C# CONVERTER NOTE: This was formerly a static local variable declaration (not allowed in C#):
private uint ExecuteCommand_StartPos;
//C++ TO C# CONVERTER NOTE: This was formerly a static local variable declaration (not allowed in C#):
private PointerList ExecuteCommand_List = null;
//C++ TO C# CONVERTER NOTE: This was formerly a static local variable declaration (not allowed in C#):
private PointerTable ExecuteCommand_Tbl = null;
//C++ TO C# CONVERTER NOTE: This was formerly a static local variable declaration (not allowed in C#):
private AtlasContext ExecuteCommand_Context = null;
//C++ TO C# CONVERTER NOTE: This was formerly a static local variable declaration (not allowed in C#):
private AtlasExtension ExecuteCommand_Ext = null;











internal static partial class DefineConstants
{
	public const int TBL_OK = 0x00; // Success
	public const int TBL_OPEN_ERROR = 0x01; // Cannot open the Table properly
	public const int TBL_PARSE_ERROR = 0x02; // Cannot parse how the Table is typed
	public const int NO_MATCHING_ENTRY = 0x10; // There was an entry that cannot be matched in the table
	public const int SPACE = 0x20;
}