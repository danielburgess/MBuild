using System.Collections.Generic;



public class PointerHandler
{
	public PointerHandler(VariableMap Map)
	{
		this.Map = Map;
	}
	public bool CreatePointer(string PtrId, string AddressType, long Offsetting, uint Size, uint HeaderSize)
	{
		CustomPointer Ptr = (CustomPointer)Map.GetVar(PtrId).GetData();
		if (Ptr != null) // Already initialized
		{
			Logger.ReportError(CurrentLine, "Identifier %s has already been allocated", PtrId);
			return false;
		}
		Ptr = new CustomPointer();
		if (!Ptr.Init(Offsetting, Size, HeaderSize))
		{
			Logger.ReportError(CurrentLine, "Invalid size parameter for CREATEPTR");
			return false;
		}
		if (!Ptr.SetAddressType(AddressType))
		{
			Logger.ReportError(CurrentLine, "Invalid address type for CREATEPTR");
			return false;
		}

		Map.SetVarData(PtrId, Ptr, P_CUSTOMPOINTER);
		return true;
	}
	public uint GetPtrAddress(string PtrId, uint ScriptPos, ref uint Size)
	{
		CustomPointer Ptr = (CustomPointer)Map.GetVar(PtrId).GetData();
		if (Ptr == null) // Uninitialized
		{
			Logger.ReportError(CurrentLine, "Identifier %s has not been initialized with CREATEPTR", PtrId);
			return -1;
		}
		uint Address = Ptr.GetAddress(ScriptPos);
		Size = Ptr.GetSize();
		return Address;
	}
	public bool CreatePointerList(string ListId, string Filename, string PtrId)
	{
		PointerList List = (PointerList)Map.GetVar(ListId).GetData();
		if (List != null) // Already initialized
		{
			Logger.ReportError(CurrentLine, "Identifier %s has already been allocated", ListId);
			return false;
		}
		CustomPointer Ptr = (CustomPointer)Map.GetVar(PtrId).GetData();
		if (Ptr == null)
		{
			Logger.ReportError(CurrentLine, "Identifier %s has not been initialized with CREATEPTR", PtrId);
			return false;
		}

		List = new PointerList();
		bool Success = List.Create(Filename, Ptr);
		Map.SetVarData(ListId, List, P_POINTERLIST);
		return Success;
	}
	public bool CreatePointerTable(string TblId, uint Start, uint Increment, string PtrId)
	{
		PointerTable Tbl = (PointerTable)Map.GetVar(TblId).GetData();
		if (Tbl != null) // Already allocated
		{
			Logger.ReportError(CurrentLine, "Identifier %s has already been allocated", TblId);
			return false;
		}
		CustomPointer Ptr = (CustomPointer)Map.GetVar(PtrId).GetData();
		if (Ptr == null)
		{
			Logger.ReportError(CurrentLine, "Identifier %s has not been initialized with CREATEPTR", PtrId);
			return false;
		}
		Tbl = new PointerTable();
		Tbl.Create(Increment, Start, Ptr);
		Map.SetVarData(TblId, Tbl, P_POINTERTABLE);
		return true;
	}
	public bool CreateEmbPointerTable(string TblId, uint Start, uint PtrCount, string PtrId)
	{
		EmbPointerTable Tbl = (EmbPointerTable)Map.GetVar(TblId).GetData();
		if (Tbl != null) // Already allocated
		{
			Logger.ReportError(CurrentLine, "Identifier %s has already been allocated", TblId);
			return false;
		}
		CustomPointer Ptr = (CustomPointer)Map.GetVar(PtrId).GetData();
		if (Ptr == null)
		{
			Logger.ReportError(CurrentLine, "Identifier %s has not been initialized with CREATEPTR", PtrId);
			return false;
		}

		Tbl = new EmbPointerTable();
		Tbl.Create(Start, PtrCount, Ptr);
		Map.SetVarData(TblId, Tbl, P_EMBPOINTERTABLE);
		return true;
	}
	public uint GetListAddress(string ListId, uint ScriptPos, ref uint Size, ref uint WritePos)
	{
		PointerList List = (PointerList)Map.GetVar(ListId).GetData();
		if (List == null) // Not initialized
		{
			Logger.ReportError(CurrentLine, "Identifier %s has not been initialized with PTRLIST", ListId);
			return -1;
		}
		return List.GetAddress(ScriptPos, ref Size, ref WritePos);
	}
	public uint GetTableAddress(string TblId, uint ScriptPos, ref uint Size, ref uint WritePos)
	{
		PointerTable Tbl = (PointerTable)Map.GetVar(TblId).GetData();
		if (Tbl == null) // Not initialized
		{
			Logger.ReportError(CurrentLine, "Identifier %s has not been initialized with PTRTBL", TblId);
			return -1;
		}
		return Tbl.GetAddress(ScriptPos, ref Size, ref WritePos);
	}
	public uint GetTableAddress(string TblId, uint ScriptPos, uint PtrNum, ref uint Size, ref uint WritePos)
	{
		PointerTable Tbl = (PointerTable)Map.GetVar(TblId).GetData();
		if (Tbl == null) // Not initialized
		{
			Logger.ReportError(CurrentLine, "Identifier %s has not been initialized with PTRTBL", TblId);
			return -1;
		}
		return Tbl.GetAddress(ScriptPos, PtrNum, ref Size, ref WritePos);
	}

	public uint GetEmbTableAddress(string TblId, uint ScriptPos, ref uint Size, ref uint WritePos)
	{
		EmbPointerTable Tbl = (EmbPointerTable)Map.GetVar(TblId).GetData();
		if (Tbl == null) // Not initialized
		{
			Logger.ReportError(CurrentLine, "Identifier %s has not been initialized with EMBPTRTBL", TblId);
			return -1;
		}
		return Tbl.GetAddress(ScriptPos, ref Size, ref WritePos);
	}
	public uint GetEmbTableAddress(string TblId, uint ScriptPos, uint PtrNum, ref uint Size, ref uint WritePos)
	{
		EmbPointerTable Tbl = (EmbPointerTable)Map.GetVar(TblId).GetData();
		if (Tbl == null) // Not initialized
		{
			Logger.ReportError(CurrentLine, "Identifier %s has not been initialized with EMBPTRTBL", TblId);
			return -1;
		}
		return Tbl.GetAddress(ScriptPos, PtrNum, ref Size, ref WritePos);
	}
	public uint GetPtrSize(string PtrId)
	{
		CustomPointer Ptr = (CustomPointer)Map.GetVar(PtrId).GetData();
		if (Ptr == null) // Uninitialized
		{
			Logger.ReportError(CurrentLine, "Identifier %s has not been initialized with CREATEPTR", PtrId);
			return -1;
		}

		return Ptr.GetSize();
	}
	private VariableMap Map;
}

public class PointerList
{
	public PointerList()
	{
		Location = 0;
	}
	public void Dispose()
	{
	}

	public bool Create(string Filename, CustomPointer CustPointer)
	{
		ifstream Input = new ifstream(Filename);
		if (!Input.is_open())
		{
			// File Error
			return false;
		}

		string Line;
		uint FirstPos = 0;
		bool bRet = true;
		uint Res = 0;

		uint CurLine = 1;
		while (!Input.eof())
		{
			getline(Input, Line);
			FirstPos = Line.find_first_not_of(" \t", 0);

			if (FirstPos == -1) // Whitespace line
				continue;

			if (Line.Length > FirstPos + 1)
			{
				if (Line[FirstPos] == '/' && Line[FirstPos] == '/') // Comment
					continue;
			}

			// Trim trailing whitespace
			uint Last;
			for (Last = Line.Length - 1; Last > 0; Last--)
			{
				if (Line[Last] != ' ' && Line[Last] != '\t')
					break;
			}
			if (Last < Line.Length)
			{
				Line = Line.erase(Last + 1);
			}

			if (Line[FirstPos] == '$')
			{
				FirstPos++;
				if (-1 == Line.find_first_not_of("0123456789ABCDEF", FirstPos))
				{
					Res = strtoul(Line.Substring(FirstPos, Line.Length - FirstPos).c_str(), null, 16);
				}
				else
				{
					Logger.ReportError(CurLine, "Error parsing %s in %s", Line, Filename);
					bRet = false;
				}
			}
			else
			{
				if (-1 == Line.find_first_not_of("0123456789", FirstPos))
				{
					Res = strtoul(Line.Substring(FirstPos, Line.Length - FirstPos).c_str(), null, 10);
				}
				else
				{
					Logger.ReportError(CurLine, "Error parsing %s in %s", Line, Filename);
					bRet = false;
				}
			}

			LocationList.AddLast(Res);
			CurLine++;
		}

		Input.close();
		LocationIt = LocationList.GetEnumerator();
		Location = 0;
//C++ TO C# CONVERTER WARNING: The following line was determined to be a copy assignment (rather than a reference assignment) - this should be verified and a 'CopyFrom' method should be created if it does not yet exist:
//ORIGINAL LINE: Pointer = CustPointer;
		Pointer.CopyFrom(CustPointer);
		return bRet;
	}
	public uint GetAddress(uint TextPosition, ref uint Size, ref uint WritePos)
	{
		if (Location < LocationList.Count)
		{
			Size = Pointer.GetSize();
//C++ TO C# CONVERTER TODO TASK: Iterators are only converted within the context of 'while' and 'for' loops:
			WritePos = LocationIt;
//C++ TO C# CONVERTER TODO TASK: Iterators are only converted within the context of 'while' and 'for' loops:
			LocationIt++;
			Location++;
			return Pointer.GetAddress(TextPosition);
		}
		else
		{
			return -1;
		}
	}
	private LinkedList<uint> LocationList = new LinkedList<uint>();
	private LinkedList<uint>.Enumerator LocationIt;
	private uint Location;
	private CustomPointer Pointer = new CustomPointer();
}

public class PointerTable
{
	public PointerTable()
	{
		Increment = 0;
		CurOffset = 0;
	}
	public void Dispose()
	{
	}
	public bool Create(uint Inc, uint StartOffset, CustomPointer CustPointer)
	{
		Increment = Inc;
		CurOffset = StartOffset;
//C++ TO C# CONVERTER WARNING: The following line was determined to be a copy assignment (rather than a reference assignment) - this should be verified and a 'CopyFrom' method should be created if it does not yet exist:
//ORIGINAL LINE: Pointer = CustPointer;
		Pointer.CopyFrom(CustPointer);
		TableStart = StartOffset;
		return true;
	}
	public uint GetAddress(uint TextPosition, ref uint Size, ref uint WritePos)
	{
		Size = Pointer.GetSize();
		WritePos = CurOffset;
		CurOffset += Increment;
		return Pointer.GetAddress(TextPosition);
	}
	public uint GetAddress(uint TextPosition, uint PtrNum, ref uint Size, ref uint WritePos)
	{
		Size = Pointer.GetSize();

		WritePos = TableStart + (PtrNum * Increment);
		CurOffset = WritePos + Increment;
		return Pointer.GetAddress(TextPosition);
	}

	private uint Increment;
	private uint CurOffset;
	private uint TableStart;
	private CustomPointer Pointer = new CustomPointer();
}

public class EmbPointerTable
{
	public EmbPointerTable()
	{
		TableStart = 0;
		CurPointer = 0;
		PtrCount = 0;
	}
	public void Dispose()
	{
	}

	public bool Create(uint StartOffset, uint PointerCount, CustomPointer CustPointer)
	{
		TableStart = StartOffset;
		CurPointer = 0;
		PtrCount = PointerCount;
//C++ TO C# CONVERTER WARNING: The following line was determined to be a copy assignment (rather than a reference assignment) - this should be verified and a 'CopyFrom' method should be created if it does not yet exist:
//ORIGINAL LINE: Pointer = CustPointer;
		Pointer.CopyFrom(CustPointer);
		return true;
	}
	public uint GetAddress(uint TextPosition, ref uint Size, ref uint WritePos)
	{
		if (CurPointer >= PtrCount)
		{
			return -1; // Out of bounds
		}
		Size = Pointer.GetSize();

		WritePos = TableStart + CurPointer * (Size / 8);
		CurPointer++;
		return Pointer.GetAddress(TextPosition);
	}
	public uint GetAddress(uint TextPosition, uint PtrNum, ref uint Size, ref uint WritePos)
	{
		if (PtrNum >= PtrCount)
		{
			return -1; // Out of bounds
		}

		Size = Pointer.GetSize();

		WritePos = TableStart + PtrNum * (Size / 8);
		CurPointer = PtrNum + 1;
		return Pointer.GetAddress(TextPosition);
	}

	private uint TableStart;
	private uint CurPointer;
	private uint PtrCount;
	private CustomPointer Pointer = new CustomPointer();
}
