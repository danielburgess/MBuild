using System.Collections.Generic;

public static class GlobalMembersAtlasFile
{

	internal const uint STR_ENDTERM = 0;
	internal const uint STR_PASCAL = 1;
	internal const uint StringTypeCount = 2;
	internal string[] StringTypes = {"ENDTERM", "PASCAL"};
}

public class AtlasFile
{
	public AtlasFile()
	{
		tfile = null;
		pfile = null;
    
		MaxScriptPos = -1;
		ActiveTbl = null;
		BytesInserted = 0;
		TotalBytes = 0;
		TotalBytesSkipped = 0;
    
		StrType = STR_ENDTERM;
		PascalLength = 1;
    
		FixedPadValue = 0;
		StringLength = 0;
	}
	public void Dispose()
	{
		if (tfile != null)
		{
			fclose(tfile);
		}
		if (pfile != null)
		{
			fclose(pfile);
		}
	}

	public bool AutoWrite(PointerList List, string EndTag)
	{
		bool EndTokenFound = false;
		for (uint i = 0; i < ActiveTbl.EndTokens.size(); i++)
		{
			if (EndTag == ActiveTbl.EndTokens[i])
			{
				EndTokenFound = true;
			}
		}
		if (EndTokenFound)
		{
			ListAutoWrite.insert(SortedDictionary<string,PointerList*>.value_type(EndTag, List));
		}
		return EndTokenFound;
	}
	public bool AutoWrite(PointerTable Tbl, string EndTag)
	{
		bool EndTokenFound = false;
		for (uint i = 0; i < ActiveTbl.EndTokens.size(); i++)
		{
			if (EndTag == ActiveTbl.EndTokens[i])
			{
				EndTokenFound = true;
			}
		}
		if (EndTokenFound)
		{
			TblAutoWrite.insert(SortedDictionary<string,PointerTable*>.value_type(EndTag, Tbl));
		}
		return EndTokenFound;
	}
	public bool AutoWrite(AtlasExtension Ext, string FuncName, string EndTag)
	{
		bool EndTokenFound = false;
		ExtensionFunction Func = new ExtensionFunction();
    
		for (uint i = 0; i < ActiveTbl.EndTokens.size(); i++)
		{
			if (EndTag == ActiveTbl.EndTokens[i])
			{
				EndTokenFound = true;
			}
		}
		Func = Ext.GetFunction(FuncName);
		if (!EndTokenFound)
		{
			Logger.ReportError(CurrentLine, "'%s' has not been defined as an end token in the active table", EndTag);
			return false;
		}
		if (Func == null)
		{
			Logger.ReportError(CurrentLine, "Function 's' could not be found in the extension", FuncName);
			return false;
		}
    
		ExtAutoWrite.insert(SortedDictionary<string,ExtensionFunction>.value_type(EndTag, Func));
		return true;
	}
	public bool DisableAutoExtension(string FuncName, string EndTag)
	{
		SortedDictionary<string, ExtensionFunction>.Enumerator it;
		it = ExtAutoWrite.find(EndTag);
		if (it == ExtAutoWrite.end())
		{
			Logger.ReportError(CurrentLine, "'%s' has not been defined as an autoexec end token", EndTag);
			return false;
		}
		ExtAutoWrite.erase(it);
		return true;
	}
	public bool DisableWrite(string EndTag, bool isPointerTable)
	{
		if (isPointerTable)
		{
			SortedDictionary<string, PointerTable>.Enumerator it;
			it = TblAutoWrite.find(EndTag);
			if (it == TblAutoWrite.end())
			{
				return false;
			}
			TblAutoWrite.erase(it);
		}
		else
		{
			SortedDictionary<string, PointerList>.Enumerator it;
			it = ListAutoWrite.find(EndTag);
			if (it == ListAutoWrite.end())
			{
				return false;
			}
			ListAutoWrite.erase(it);
		}
		return true;
	}

	// File functions.  T for text file, P for pointer file
	public bool OpenFileT(string FileName)
	{
		// Reset vars for new file
		MaxScriptPos = -1;
    
		tfile = fopen(FileName, "r+b");
		return tfile != null;
	}
	public bool OpenFileP(string Filename)
	{
		pfile = fopen(Filename, "r+b");
		return pfile != null;
	}
	public void CloseFileT()
	{
		if (tfile != null)
		{
			fclose(tfile);
		}
	}
	public void CloseFileP()
	{
		if (pfile != null)
		{
			fclose(pfile);
		}
	}
	public void MoveT(uint Pos, uint ScriptBound)
	{
		if (tfile)
		{
			fseek(tfile, Pos, SEEK_SET);
		}
		MaxScriptPos = ScriptBound;
	}
	public void MoveT(uint Pos)
	{
		if (tfile)
		{
			fseek(tfile, Pos, SEEK_SET);
		}
	}
	public void WriteP(object Data, uint Size, uint DataCount, uint Pos)
	{
		uint OldPos = ftell(pfile);
		fseek(pfile, Pos, SEEK_SET);
		fwrite(Data, Size, DataCount, pfile);
		fseek(pfile, OldPos, SEEK_SET);
	}
	public void WriteT(object Data, uint Size, uint DataCount, uint Pos)
	{
		uint OldPos = ftell(tfile);
		fseek(tfile, Pos, SEEK_SET);
		fwrite(Data, Size, DataCount, tfile);
		fseek(tfile, OldPos, SEEK_SET);
	}
	public void WriteT(object Data, uint Size, uint DataCount)
	{
		fwrite(Data, Size, DataCount, tfile);
	}
	public uint GetPosT()
	{
		return ftell(tfile);
	}

	public uint GetMaxBound()
	{
		return MaxScriptPos;
	}
	public uint GetBytesInserted()
	{
		return BytesInserted;
	}
	public uint GetBytesOverflowed()
	{
		return TotalBytesSkipped;
	}

	public void SetTable(Table Tbl)
	{
		FlushText();
		ActiveTbl = Tbl;
	}
	public bool SetStringType(string Type)
	{
		for (int i = 0; i < StringTypeCount; i++)
		{
			if (Type == StringTypes[i])
			{
				StrType = i;
				return true;
			}
		}
    
		return false;
	}
	public bool SetPascalLength(uint Length)
	{
		switch (Length)
		{
		case 1:
	case 2:
	case 3:
	case 4:
			PascalLength = Length;
			break;
		default:
			return false;
		}
		return true;
	}
	public bool SetFixedLength(uint StrLength, uint PadValue)
	{
		if (PadValue > 65536)
		{
			return false;
		}
    
		StringLength = StrLength;
		FixedPadValue = (byte)PadValue;
    
		return true;
	}

	public bool InsertText(string Text, uint Line)
	{
		if (ActiveTbl == null)
		{
			// Add error
			Console.Write("No active table loaded\n");
			return false;
		}
		uint BadCharPos = 0;
		if (ActiveTbl.EncodeStream(Text, BadCharPos) == -1) // Failed insertion, char missing from tbl
		{
			// Add error
			Logger.ReportError(Line, "Character '%c' missing from table.  String = '%s'", Text[BadCharPos], Text);
			return false;
		}
		return true;
	}
	public bool FlushText()
	{
	//C++ TO C# CONVERTER NOTE: This static local variable declaration (not allowed in C#) has been moved just prior to the method:
	//	static uint Size;
	//C++ TO C# CONVERTER NOTE: This static local variable declaration (not allowed in C#) has been moved just prior to the method:
	//	static uint Address;
	//C++ TO C# CONVERTER NOTE: This static local variable declaration (not allowed in C#) has been moved just prior to the method:
	//	static uint WritePos;
		AtlasContext Context = null;
    
		if (ActiveTbl == null)
		{
			return false;
		}
    
		if (ActiveTbl.StringTable.empty())
		{
			return true;
		}
    
		// For every string, check autowrite/autoexec (list, table, and extension)
		// Automatically write pointer if appropriate end string is found, then write the text string to ROM
		LinkedList<TXT_STRING>.Enumerator j = ActiveTbl.TxtStringTable.begin();
		for (LinkedList<TBL_STRING>.Enumerator i = ActiveTbl.StringTable.begin(); i.MoveNext(); i++, j++)
		{
			// #ALIGNSTRING, must do before autowrite writes a pointer
			AlignString();
    
			if (!i.EndToken.empty()) // If there's an end token, check for autowrite
			{
				ListIt = ListAutoWrite.find(i.EndToken);
				if (ListIt != ListAutoWrite.end())
				{
					FlushText_Address = ListIt.second.GetAddress(GetPosT(), FlushText_Size, FlushText_WritePos);
					if (FlushText_Address != -1)
					{
						if (bSwap)
						{
							FlushText_Address = EndianSwap(FlushText_Address, FlushText_Size / 8);
						}
						WriteP(FlushText_Address, FlushText_Size / 8, 1, FlushText_WritePos);
						Logger.Log("%6u AUTOWRITE Invoked ScriptPos $%X PointerPos $%X PointerValue $%08X\n", CurrentLine, GetPosT(), FlushText_WritePos, FlushText_Address);
						Stats.IncAutoPointerWrites();
					}
					else
					{
						Stats.IncFailedListWrites();
					}
				}
				TblIt = TblAutoWrite.find(i.EndToken);
				if (TblIt != TblAutoWrite.end())
				{
					FlushText_Address = TblIt.second.GetAddress(GetPosT(), FlushText_Size, FlushText_WritePos);
					if (bSwap)
					{
						FlushText_Address = EndianSwap(FlushText_Address, FlushText_Size / 8);
					}
					WriteP(FlushText_Address, FlushText_Size / 8, 1, FlushText_WritePos);
					Logger.Log("%6u AUTOWRITE Invoked ScriptPos $%X PointerPos $%X PointerValue $%08X\n", CurrentLine, GetPosT(), FlushText_WritePos, FlushText_Address);
					Stats.IncAutoPointerWrites();
				}
				ExtIt = ExtAutoWrite.find(i.EndToken);
				if (ExtIt != ExtAutoWrite.end())
				{
					Atlas.CreateContext(Context);
					bool Success = Atlas.ExecuteExtensionFunction(ExtIt.second, Context);
					Context = null;
					Context = null;
					if (!Success)
					{
						Logger.ReportError(CurrentLine, "Autoexecuting extension with end token '%s' failed", i.EndToken);
						return false;
					}
					else
					{
						Logger.Log("%6u AUTOEXEC  Invoked ScriptPos $%X PointerPos $%X PointerValue $%08X\n", CurrentLine, GetPosT(), FlushText_WritePos, FlushText_Address);
					}
				}
			}
    
			CurTextString = j.Text;
			WriteString(i.Text);
			Logger.Log("%s\n", CurTextString.c_str());
			CurTextString.clear();
		}
    
		ActiveTbl.StringTable.clear();
    
		return true;
	}

	public uint GetMaxWritableBytes()
	{
		if (MaxScriptPos == -1)
		{
			return -1;
		}
		uint CurPos = ftell(tfile);
		if (CurPos > MaxScriptPos)
		{
			return 0;
		}
		return MaxScriptPos - CurPos + 1;
	}
	public FILE GetFileT()
	{
		return tfile;
	}
	public FILE GetFileP()
	{
		return pfile;
	}
	public void GetScriptBuf(ref LinkedList<TBL_STRING> Strings)
	{
		Strings = ActiveTbl.StringTable;
	}
	public void SetScriptBuf(LinkedList<TBL_STRING> Strings)
	{
		ActiveTbl.StringTable = Strings;
	}
	public uint GetStringType()
	{
		return StrType;
	}

	private FILE tfile; // Target file for script
	private FILE pfile; // Pointer write file
	private Table ActiveTbl;
	private PointerHandler PtrHandler;
	private SortedDictionary<string, PointerList> ListAutoWrite = new SortedDictionary<string, PointerList>();
	private SortedDictionary<string, PointerTable> TblAutoWrite = new SortedDictionary<string, PointerTable>();
	private SortedDictionary<string, ExtensionFunction> ExtAutoWrite = new SortedDictionary<string, ExtensionFunction>();
	private SortedDictionary<string, PointerList>.Enumerator ListIt;
	private SortedDictionary<string, PointerTable>.Enumerator TblIt;
	private SortedDictionary<string, ExtensionFunction>.Enumerator ExtIt;

	public bool WriteString(string text)
	{
		uint StringSize = 0;
		int PadBytes;
    
		// Write string type
		if (StrType == STR_ENDTERM)
		{
			StringSize = WriteNullString(text);
		}
		else if (StrType == STR_PASCAL)
		{
			StringSize = WritePascalString(text);
		}
		else
		{
			return false;
		}
    
		// #FIXEDLENGTH padding
		if (StringLength != 0)
		{
			PadBytes = StringLength - StringSize;
			if (PadBytes > 0)
			{
				for (int i = 0; i < PadBytes; i++)
				{
					fputc((int)FixedPadValue, tfile);
				}
				BytesInserted += PadBytes;
			}
		}
    
		return true;
	}
	public uint WriteNullString(string text)
	{
		uint size = (uint)text.Length;
		uint maxwrite = GetMaxWritableBytes();
    
		Stats.AddScriptBytes(size);
    
		// Truncate string if it overflows ROM bounds
		if (maxwrite < size)
		{
			int overflowbytes = size - maxwrite;
			TotalBytesSkipped += overflowbytes;
			size = maxwrite;
		}
    
		// Truncate string if it's too long for a fixed length string
		if (size > StringLength && StringLength != 0)
		{
			TotalBytesSkipped += (size - StringLength);
			size = StringLength;
			Console.Write("Changed string length for {0} to {1:D} at {2:X}\n", CurTextString.c_str(), StringLength, GetPosT());
		}
    
		fwrite(text.data(), 1, size, tfile);
		BytesInserted += size;
    
		return size;
	}
	public uint WritePascalString(string text)
	{
		uint size = (uint)text.Length;
		uint maxwrite = GetMaxWritableBytes();
    
		Stats.AddScriptBytes(size + PascalLength);
    
		// Truncate string if it overflows ROM bounds
		if (PascalLength > maxwrite) // PascalLength doesn't even fit
		{
			goto nowrite;
		}
		if (maxwrite < size + PascalLength) // PascalLength and maybe partial string fits
		{
			int overflowbytes = (size + PascalLength) - maxwrite;
			TotalBytesSkipped += overflowbytes;
			size = maxwrite - PascalLength;
		}
    
		// Truncate string if it's too long for a fixed length string
		if (size > StringLength && StringLength != 0)
		{
			TotalBytesSkipped += (size - StringLength);
			size = StringLength - PascalLength;
			Console.Write("Changed string length for {0} to {1:D} at {2:X}\n", text, StringLength, GetPosT());
		}
    
		int swaplen = size;
		if (bSwap)
		{
			swaplen = EndianSwap(size, PascalLength);
		}
    
		fwrite(swaplen, PascalLength, 1, tfile);
		fwrite(text, 1, size, tfile);
		BytesInserted += size + PascalLength;
    
	nowrite:
		return size + PascalLength;
	}
	public void AlignString()
	{
		if (StringAlign != 0) // String align turned on
		{
			int curoffset = GetPosT() - Atlas.GetHeaderSize();
			int PadBytes = StringAlign - (curoffset % StringAlign);
			if (PadBytes == StringAlign)
			{
				PadBytes = 0;
			}
			if (PadBytes > 0)
			{
				for (int i = 0; i < PadBytes; i++)
				{
					fputc(0, tfile);
				}
				BytesInserted += PadBytes;
			}
		}
	}

	private uint MaxScriptPos;
	private uint BytesInserted;
	private uint TotalBytesSkipped;
	private uint TotalBytes;

	private uint StrType;
	private uint PascalLength;

	private uint StringLength;
	private byte FixedPadValue;
	private string CurTextString; // Used to keep track and report text that overflows the FIXEDLENGTH value
}