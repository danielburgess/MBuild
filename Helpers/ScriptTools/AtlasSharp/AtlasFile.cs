﻿using System.Collections.Generic;
using System;











// Does not revert file offset


















//C++ TO C# CONVERTER NOTE: This was formerly a static local variable declaration (not allowed in C#):
private uint FlushText_Size;
//C++ TO C# CONVERTER NOTE: This was formerly a static local variable declaration (not allowed in C#):
private uint FlushText_Address;
//C++ TO C# CONVERTER NOTE: This was formerly a static local variable declaration (not allowed in C#):
private uint FlushText_WritePos;







internal static partial class DefineConstants
{
	public const int TBL_OK = 0x00; // Success
	public const int TBL_OPEN_ERROR = 0x01; // Cannot open the Table properly
	public const int TBL_PARSE_ERROR = 0x02; // Cannot parse how the Table is typed
	public const int NO_MATCHING_ENTRY = 0x10; // There was an entry that cannot be matched in the table
	public const int SPACE = 0x20;
}