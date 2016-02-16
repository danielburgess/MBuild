public static class GlobalMembersAtlasCore
{

	// Global Core variables
//C++ TO C# CONVERTER NOTE: 'extern' variable declarations are not required in C#:
	//extern uint CurrentLine;
//C++ TO C# CONVERTER NOTE: 'extern' variable declarations are not required in C#:
	//extern int bSwap;
//C++ TO C# CONVERTER NOTE: 'extern' variable declarations are not required in C#:
	//extern int StringAlign;
//C++ TO C# CONVERTER NOTE: 'extern' variable declarations are not required in C#:
	//extern int MaxEmbPtr;
//C++ TO C# CONVERTER NOTE: 'extern' variable declarations are not required in C#:
	//extern AtlasCore Atlas;

	// Misc functions
//C++ TO C# CONVERTER TODO TASK: The implementation of the following method could not be found:
	//uint StringToUInt(string NumberString);
//C++ TO C# CONVERTER TODO TASK: The implementation of the following method could not be found:
	//long StringToInt64(string NumberString);
//C++ TO C# CONVERTER TODO TASK: The implementation of the following method could not be found:
	//uint GetHexDigit(sbyte digit);
//C++ TO C# CONVERTER TODO TASK: The implementation of the following method could not be found:
	//uint EndianSwap(uint Num, int Size);
}

//-----------------------------------------------------------------------------
// AtlasCore Functionality
//-----------------------------------------------------------------------------

public class AtlasCore
{
	public AtlasCore()
	{
		this.PtrHandler = &VarMap;
		this.Parser = &VarMap;
		this.Extensions = &VarMap;
		CurrentLine = 1;
		HeaderSize = 0;
		IsInJmp = false;
	}
	public void Dispose()
	{
	}

	public bool Insert(string RomFileName, string ScriptFileName)
	{
		ifstream script = new ifstream();
		script.open(ScriptFileName, ios.in);
		if (!script.is_open())
		{
			Console.Write("Unable to open script file '{0}'\n", ScriptFileName);
			return false;
		}
    
		if (!File.OpenFileT(RomFileName))
		{
			Console.Write("Unable to open target file '{0}'\n", RomFileName);
			return false;
		}
    
		// Target and pointer files will initially be the same file
		if (!File.OpenFileP(RomFileName))
		{
			Console.Write("Unable to open pointer file '{0}'\n", RomFileName);
			return false;
		}
    
		// Parse file
		bool ParseSuccess = false;
		clock_t ParseStart = clock();
		ParseSuccess = Parser.ParseFile(script);
		clock_t ParseTime = clock() - ParseStart;
    
		PrintSummary("Parsing", ParseTime);
    
		if (!ParseSuccess)
		{
			return false;
		}
    
		EmbPtrs.SetListSize(MaxEmbPtr + 1);
    
		// Insert file
		clock_t InsertionStart = clock();
    
		for (ListBlockIt Block = Parser.Blocks.begin(); Block != Parser.Blocks.end(); Block++)
		{
			File.FlushText();
    
			// Execute list of commands
			for (ListCmdIt com = Block.Commands.begin(); com != Block.Commands.end(); com++)
			{
				CurrentLine = com.Line;
				if (!ExecuteCommand(*com))
				{
					goto InsertionSummary;
				}
			}
    
			if (Block.StartLine != -1)
			{
				CurrentLine = Block.StartLine;
			}
    
			// Insert text strings
			for (ListStringIt text = Block.TextLines.begin(); text != Block.TextLines.end(); text++)
			{
				if (!text.empty())
				{
					if (!IsInJmp)
					{
						Logger.ReportError(CurrentLine, "\"You must specify an address using JMP before inserting text\"");
						goto InsertionSummary;
					}
					if (!File.InsertText(*text, CurrentLine))
					{
						goto InsertionSummary;
					}
				}
				CurrentLine++;
			}
		}
    
		File.FlushText();
    
	InsertionSummary:
		clock_t InsertionTime = clock() - InsertionStart;
    
		PrintSummary("Insertion", InsertionTime);
    
		Stats.End(CurrentLine); // Hack for the last line
		PrintStatistics();
		if (MaxEmbPtr != 0)
		{
			PrintUnwrittenPointers();
		}
    
		return true;
	}
	public void SetDebugging(FILE output)
	{
		if (output == null)
		{
			Logger.SetLogStatus(false);
		}
		else
		{
			Logger.SetLogStatus(true);
		}
		Logger.SetLogSource(output);
	}
	public void CreateContext(AtlasContext[] Context)
	{
		if (Context == null)
		{
			Context = new AtlasContext();
		}
		Context.CurrentLine = CurrentLine;
		Context.ScriptPos = File.GetPosT();
		Context.ScriptRemaining = File.GetMaxWritableBytes();
		Context.Target = File.GetFileT();
		File.GetScriptBuf(Context.StringTable);
		Context.PointerPosition = 0;
		Context.PointerSize = 0;
		Context.PointerValue = 0;
	}
	public bool ExecuteExtension(string ExtId, string FunctionName, AtlasContext[] Context)
	{
		bool Success = false;
		CreateContext(Context);
		uint DllRet = Extensions.ExecuteExtension(ExtId, FunctionName, Context);
		if (DllRet == -1)
		{
			DllRet = NO_ACTION;
			Success = false;
		}
		if (DllRet > MAX_RETURN_VAL)
		{
			Logger.ReportWarning(CurrentLine, "Extension returned invalid value %u", DllRet);
			Success = false;
		}
		if ((DllRet & REPLACE_TEXT) != 0)
		{
			File.SetScriptBuf(Context.StringTable);
			Logger.Log("%6u EXECEXT   REPLACE_TEXT\n", CurrentLine);
			Success = true;
		}
		if ((DllRet & WRITE_POINTER) != 0)
		{
			uint Size = Context.PointerSize;
			if (Size == 8 || Size == 16 || Size == 24 || Size == 32)
			{
				Size /= 8;
				File.WriteP(Context.PointerValue, Context.PointerSize, 1, Context.PointerPosition);
				Logger.Log("%6u EXECEXT   WRITE_POINTER ScriptPos $%X PointerPos $%X PointerValue $%06X\n", CurrentLine, File.GetPosT(), Context.PointerPosition, Context.PointerValue);
				Success = true;
				Stats.IncExtPointerWrites();
			}
			else
			{
				Logger.ReportError(CurrentLine, "EXTEXEC   Extension function '%s' returning WRITE_POINTER has an unsupported PointerSize field", FunctionName);
				Success = false;
			}
		}
    
		delete(Context);
		Context = null;
		Logger.Log("%6u EXTEXEC   Executed function '%s' from '%s' successfully\n", CurrentLine, FunctionName, ExtId);
		return true;
	}
	public bool ExecuteExtensionFunction(ExtensionFunction Func, AtlasContext[] Context)
	{
		uint DllRet = Func(Context);
		bool Success = false;
		if (DllRet > MAX_RETURN_VAL)
		{
			Logger.ReportWarning(CurrentLine, "Extension returned invalid value %u", DllRet);
			Success = false;
		}
    
		if ((DllRet & REPLACE_TEXT) != 0)
		{
			File.SetScriptBuf(Context.StringTable);
			Logger.Log("%6u EXECEXT   REPLACE_TEXT\n", CurrentLine);
			Success = true;
		}
		if ((DllRet & WRITE_POINTER) != 0)
		{
			uint Size = Context.PointerSize;
			if (Size == 8 || Size == 16 || Size == 24 || Size == 32)
			{
				Size /= 8;
				File.WriteP(Context.PointerValue, Context.PointerSize, 1, Context.PointerPosition);
				Logger.Log("%6u EXTEXEC   WRITE_POINTER ScriptPos $%X PointerPos $%X PointerValue $%06X\n", CurrentLine, File.GetPosT(), Context.PointerPosition, Context.PointerValue);
				Success = true;
				Stats.IncExtPointerWrites();
			}
			else
			{
				Logger.ReportError(CurrentLine, "EXTEXEC   Extension function returning WRITE_POINTER has an unsupported PointerSize field");
				Success = false;
			}
		}
    
		delete(Context);
		Context = null;
		return true;
	}
	public uint GetHeaderSize()
	{
		return HeaderSize;
	}

	private AtlasParser Parser = new AtlasParser();
	private AtlasFile File = new AtlasFile();
	//AtlasFile GameFile;
	//AtlasFile PtrFile;
	private VariableMap VarMap = new VariableMap(); // Variable Map for identifiers
	private PointerHandler PtrHandler = new PointerHandler();
	private Pointer DefaultPointer = new Pointer();
	private EmbeddedPointerHandler EmbPtrs = new EmbeddedPointerHandler();
	private InsertionStatistics Total = new InsertionStatistics();
	private ExtensionManager Extensions = new ExtensionManager();

	private bool IsInJmp;
	private uint HeaderSize;

	public bool ExecuteCommand(Command Cmd)
	{
	//C++ TO C# CONVERTER NOTE: This static local variable declaration (not allowed in C#) has been moved just prior to the method:
	//	static uint PtrValue;
	//C++ TO C# CONVERTER NOTE: This static local variable declaration (not allowed in C#) has been moved just prior to the method:
	//	static uint PtrNum;
	//C++ TO C# CONVERTER NOTE: This static local variable declaration (not allowed in C#) has been moved just prior to the method:
	//	static uint PtrPos;
	//C++ TO C# CONVERTER NOTE: This static local variable declaration (not allowed in C#) has been moved just prior to the method:
	//	static uint Size;
	//C++ TO C# CONVERTER NOTE: This static local variable declaration (not allowed in C#) has been moved just prior to the method:
	//	static bool Success;
	//C++ TO C# CONVERTER NOTE: This static local variable declaration (not allowed in C#) has been moved just prior to the method:
	//	static byte PtrByte;
	//C++ TO C# CONVERTER NOTE: This static local variable declaration (not allowed in C#) has been moved just prior to the method:
	//	static uint StartPos;
	//C++ TO C# CONVERTER NOTE: This static local variable declaration (not allowed in C#) has been moved just prior to the method:
	//	static PointerList* List = null;
	//C++ TO C# CONVERTER NOTE: This static local variable declaration (not allowed in C#) has been moved just prior to the method:
	//	static PointerTable* Tbl = null;
	//C++ TO C# CONVERTER NOTE: This static local variable declaration (not allowed in C#) has been moved just prior to the method:
	//	static AtlasContext* Context = null;
	//C++ TO C# CONVERTER NOTE: This static local variable declaration (not allowed in C#) has been moved just prior to the method:
	//	static AtlasExtension* Ext = null;
		string FuncName;
    
		if (IsInJmp && Cmd.Function != CMD_JMP1 && Cmd.Function != CMD_JMP2)
		{
			Stats.AddCmd(Cmd.Function);
		}
		else
		{
			Total.AddCmd(Cmd.Function);
		}
    
		switch (Cmd.Function)
		{
		case CMD_JMP1:
			File.MoveT(StringToUInt(Cmd.Parameters[0].Value), -1);
			Stats.NewStatsBlock(File.GetPosT(), -1, Cmd.Line);
			Logger.Log("%6u JMP       ROM Position is now $%X\n", Cmd.Line, StringToUInt(Cmd.Parameters[0].Value));
			IsInJmp = true;
			return true;
		case CMD_JMP2:
			File.MoveT(StringToUInt(Cmd.Parameters[0].Value), StringToUInt(Cmd.Parameters[1].Value));
			Stats.NewStatsBlock(File.GetPosT(), StringToUInt(Cmd.Parameters[1].Value), Cmd.Line);
			Logger.Log("%6u JMP       ROM Position is now $%X with max bound of $%X\n", Cmd.Line, StringToUInt(Cmd.Parameters[0].Value), StringToUInt(Cmd.Parameters[1].Value));
			IsInJmp = true;
			return true;
		case CMD_SMA:
			ExecuteCommand_Success = DefaultPointer.SetAddressType(Cmd.Parameters[0].Value);
			if (ExecuteCommand_Success)
			{
				Logger.Log("%6u SMA       Addressing type is now '%s'\n", Cmd.Line, Cmd.Parameters[0].Value.c_str());
			}
			return ExecuteCommand_Success;
		case CMD_HDR:
			uint ExecuteCommand_Size;
			ExecuteCommand_Size = StringToUInt(Cmd.Parameters[0].Value);
			EmbPtrs.SetHeaderSize(ExecuteCommand_Size);
			DefaultPointer.SetHeaderSize(ExecuteCommand_Size);
			HeaderSize = ExecuteCommand_Size;
			Logger.Log("%6u HDR       Header size is now $%X\n", Cmd.Line, StringToUInt(Cmd.Parameters[0].Value));
			return true;
		case CMD_STRTYPE:
			ExecuteCommand_Success = File.SetStringType(Cmd.Parameters[0].Value);
			if (ExecuteCommand_Success)
			{
				Logger.Log("%6u STRTYPE   String type is now '%s'\n", Cmd.Line, Cmd.Parameters[0].Value.c_str());
			}
			return ExecuteCommand_Success;
		case CMD_ADDTBL:
			ExecuteCommand_Success = AddTable(Cmd);
			if (ExecuteCommand_Success)
			{
				Logger.Log("%6u ADDTBL    Added table '%s' as '%s'\n", Cmd.Line, Cmd.Parameters[0].Value.c_str(), Cmd.Parameters[1].Value.c_str());
			}
			return ExecuteCommand_Success;
		case CMD_ACTIVETBL:
			ExecuteCommand_Success = ActivateTable(Cmd.Parameters[0].Value);
			if (ExecuteCommand_Success)
			{
				Logger.Log("%6u ACTIVETBL Active table is now '%s'\n", Cmd.Line, Cmd.Parameters[0].Value.c_str());
			}
			return ExecuteCommand_Success;
		case CMD_VAR: // Already handled by AtlasParser to validate types, should never get here
			return true;
		case CMD_WUB:
			ExecuteCommand_PtrValue = DefaultPointer.GetUpperByte(File.GetPosT());
			File.WriteP(ExecuteCommand_PtrValue, 1, 1, StringToUInt(Cmd.Parameters[0].Value));
			Logger.Log("%6u WUB       ScriptPos $%X PointerPos $%X PointerValue $%02X\n", Cmd.Line, File.GetPosT(), StringToUInt(Cmd.Parameters[0].Value), ExecuteCommand_PtrValue);
			return true;
		case CMD_WBB:
			ExecuteCommand_PtrValue = DefaultPointer.GetBankByte(File.GetPosT());
			File.WriteP(ExecuteCommand_PtrValue, 1, 1, StringToUInt(Cmd.Parameters[0].Value));
			Logger.Log("%6u WBB       ScriptPos $%X PointerPos $%X PointerValue $%02X\n", Cmd.Line, File.GetPosT(), StringToUInt(Cmd.Parameters[0].Value), ExecuteCommand_PtrValue);
			return true;
		case CMD_WHB:
			ExecuteCommand_PtrValue = DefaultPointer.GetHighByte(File.GetPosT());
			File.WriteP(ExecuteCommand_PtrValue, 1, 1, StringToUInt(Cmd.Parameters[0].Value));
			Logger.Log("%6u WHB       ScriptPos $%X PointerPos $%X PointerValue $%02X\n", Cmd.Line, File.GetPosT(), StringToUInt(Cmd.Parameters[0].Value), ExecuteCommand_PtrValue);
			return true;
		case CMD_WLB:
			ExecuteCommand_PtrValue = DefaultPointer.GetLowByte(File.GetPosT());
			File.WriteP(ExecuteCommand_PtrValue, 1, 1, StringToUInt(Cmd.Parameters[0].Value));
			Logger.Log("%6u WLB       ScriptPos $%X PointerPos $%X PointerValue $%02X\n", Cmd.Line, File.GetPosT(), StringToUInt(Cmd.Parameters[0].Value), ExecuteCommand_PtrValue);
			return true;
		case CMD_W16:
			ExecuteCommand_PtrValue = DefaultPointer.Get16BitPointer(File.GetPosT());
			if (bSwap != 0)
			{
				ExecuteCommand_PtrValue = EndianSwap(ExecuteCommand_PtrValue, 2);
			}
			File.WriteP(ExecuteCommand_PtrValue, 2, 1, StringToUInt(Cmd.Parameters[0].Value));
			Logger.Log("%6u W16       ScriptPos $%X PointerPos $%X PointerValue $%04X\n", Cmd.Line, File.GetPosT(), StringToUInt(Cmd.Parameters[0].Value), ExecuteCommand_PtrValue);
			return true;
		case CMD_W24:
			ExecuteCommand_PtrValue = DefaultPointer.Get24BitPointer(File.GetPosT());
			if (bSwap != 0)
			{
				ExecuteCommand_PtrValue = EndianSwap(ExecuteCommand_PtrValue, 3);
			}
			File.WriteP(ExecuteCommand_PtrValue, 3, 1, StringToUInt(Cmd.Parameters[0].Value));
			Logger.Log("%6u W24       ScriptPos $%X PointerPos $%X PointerValue $%06X\n", Cmd.Line, File.GetPosT(), StringToUInt(Cmd.Parameters[0].Value), ExecuteCommand_PtrValue);
			return true;
		case CMD_W32:
			ExecuteCommand_PtrValue = DefaultPointer.Get32BitPointer(File.GetPosT());
			if (bSwap != 0)
			{
				ExecuteCommand_PtrValue = EndianSwap(ExecuteCommand_PtrValue, 4);
			}
			File.WriteP(ExecuteCommand_PtrValue, 4, 1, StringToUInt(Cmd.Parameters[0].Value));
			Logger.Log("%6u W32       ScriptPos $%X PointerPos $%X PointerValue $%08\n", Cmd.Line, File.GetPosT(), StringToUInt(Cmd.Parameters[0].Value), ExecuteCommand_PtrValue);
			return true;
		case CMD_EMBSET:
			ExecuteCommand_PtrNum = StringToUInt(Cmd.Parameters[0].Value);
			ExecuteCommand_Success = EmbPtrs.SetPointerPosition(ExecuteCommand_PtrNum, File.GetPosT());
			ExecuteCommand_Size = EmbPtrs.GetSize(ExecuteCommand_PtrNum);
			if (ExecuteCommand_Size == -1)
			{
				return false;
			}
			Logger.Log("%6u EMBSET    Pointer Position %u set to $%X\n", Cmd.Line, ExecuteCommand_PtrNum, File.GetPosT());
			if (ExecuteCommand_Success) // Write out embedded pointer
			{
				ExecuteCommand_PtrValue = EmbPtrs.GetPointerValue(ExecuteCommand_PtrNum);
				if (File.GetMaxWritableBytes() > ExecuteCommand_Size / 8)
				{
					if (bSwap != 0)
					{
						ExecuteCommand_PtrValue = EndianSwap(ExecuteCommand_PtrValue, ExecuteCommand_Size / 8);
					}
					int tpos = File.GetPosT();
					File.WriteT(ExecuteCommand_PtrValue, ExecuteCommand_Size / 8, 1); // Emb pointers are within script files
					Logger.Log("%6u EMBSET    Triggered Write: ScriptPos $%X PointerPos $%X PointerValue $%X Size %dd", Cmd.Line, EmbPtrs.GetTextPosition(ExecuteCommand_PtrNum), tpos, ExecuteCommand_PtrValue, ExecuteCommand_Size);
					Stats.IncEmbPointerWrites();
				}
				else
				{
					Logger.Log("%6u EMBSET    Failed to write due to insufficient space\n");
				}
			}
			else // Reserve space so the embedded pointer and script don't compete
			{ // for the same part of the file
				if (File.GetMaxWritableBytes() > ExecuteCommand_Size / 8)
				{
					uint Zero = 0;
					File.WriteT(Zero, ExecuteCommand_Size / 8, 1);
				}
				else
				{
					Logger.Log("%6u EMBSET    Failed to write due to insufficient space\n");
				}
			}
			return true;
		case CMD_EMBTYPE:
			ExecuteCommand_Success = EmbPtrs.SetType(Cmd.Parameters[0].Value, StringToInt64(Cmd.Parameters[2].Value), StringToUInt(Cmd.Parameters[1].Value));
			if (!ExecuteCommand_Success)
			{
				Logger.ReportError(Cmd.Line, "Bad size %d for EMBTYPE", StringToUInt(Cmd.Parameters[0].Value));
			}
			else
			{
				Logger.Log("%6u EMBTYPE   Embedded Pointer size %u Offsetting %I64d\n", Cmd.Line, StringToUInt(Cmd.Parameters[1].Value), StringToInt64(Cmd.Parameters[0].Value));
			}
			return ExecuteCommand_Success;
		case CMD_EMBWRITE:
			ExecuteCommand_PtrNum = StringToUInt(Cmd.Parameters[0].Value);
			ExecuteCommand_Success = EmbPtrs.SetTextPosition(ExecuteCommand_PtrNum, File.GetPosT());
			ExecuteCommand_Size = EmbPtrs.GetSize(ExecuteCommand_PtrNum);
			if (ExecuteCommand_Size == -1)
			{
				return false;
			}
			Logger.Log("%6u EMBWRITE  Pointed Position %u set to $%X\n", Cmd.Line, ExecuteCommand_PtrNum, File.GetPosT());
			if (ExecuteCommand_Success) // Write out embedded pointer
			{
				ExecuteCommand_PtrPos = EmbPtrs.GetPointerPosition(ExecuteCommand_PtrNum);
				ExecuteCommand_PtrValue = EmbPtrs.GetPointerValue(ExecuteCommand_PtrNum);
				if (File.GetMaxWritableBytes() > ExecuteCommand_Size / 8)
				{
					if (bSwap != 0)
					{
						ExecuteCommand_PtrValue = EndianSwap(ExecuteCommand_PtrValue, ExecuteCommand_Size / 8);
					}
					File.WriteT(ExecuteCommand_PtrValue, ExecuteCommand_Size / 8, 1, ExecuteCommand_PtrPos);
					Logger.Log("%6u EMBWRITE  Triggered Write: ScriptPos $%X PointerPos $%X PointerValue $%X Size %dd\n", Cmd.Line, File.GetPosT(), ExecuteCommand_PtrPos, ExecuteCommand_PtrValue, ExecuteCommand_Size);
					Stats.IncEmbPointerWrites();
				}
				else
				{
					Logger.Log("%6u EMBWRITE  Failed to write due to insufficient space\n");
				}
			}
			return true;
		case CMD_BREAK:
			return false;
		case CMD_PTRTBL:
			ExecuteCommand_Success = PtrHandler.CreatePointerTable(Cmd.Parameters[0].Value, StringToUInt(Cmd.Parameters[1].Value), StringToUInt(Cmd.Parameters[2].Value), Cmd.Parameters[3].Value);
			if (ExecuteCommand_Success)
			{
				Logger.Log("%6u PTRTBL    Pointer Table '%s' created StartPos $%X Increment %dd CustomPointer '%s'\n", Cmd.Line, Cmd.Parameters[0].Value.c_str(), StringToUInt(Cmd.Parameters[1].Value), StringToUInt(Cmd.Parameters[2].Value), Cmd.Parameters[3].Value.c_str());
			}
			return ExecuteCommand_Success;
		case CMD_WRITETBL:
			ExecuteCommand_PtrValue = PtrHandler.GetTableAddress(Cmd.Parameters[0].Value, File.GetPosT(), ExecuteCommand_Size, ExecuteCommand_PtrPos);
			if (ExecuteCommand_PtrValue == -1)
			{
				return false;
			}
			if (bSwap != 0)
			{
				ExecuteCommand_PtrValue = EndianSwap(ExecuteCommand_PtrValue, ExecuteCommand_Size / 8);
			}
			File.WriteP(ExecuteCommand_PtrValue, ExecuteCommand_Size / 8, 1, ExecuteCommand_PtrPos);
			Logger.Log("%6u WRITE     PointerTable '%s' ScriptPos $%X PointerPos $%X PointerValue $%08X\n", Cmd.Line, Cmd.Parameters[0].Value.c_str(), File.GetPosT(), ExecuteCommand_PtrPos, ExecuteCommand_PtrValue);
			return true;
		case CMD_PTRLIST:
			ExecuteCommand_Success = PtrHandler.CreatePointerList(Cmd.Parameters[0].Value, Cmd.Parameters[1].Value.c_str(), Cmd.Parameters[2].Value);
			if (ExecuteCommand_Success)
			{
				Logger.Log("%6u PTRTBL    Pointer List '%s' created from file '%s' CustomPointer '%s'\n", Cmd.Line, Cmd.Parameters[0].Value.c_str(), Cmd.Parameters[1].Value.c_str(), Cmd.Parameters[2].Value.c_str());
			}
			return ExecuteCommand_Success;
		case CMD_WRITELIST:
			ExecuteCommand_PtrValue = PtrHandler.GetListAddress(Cmd.Parameters[0].Value, File.GetPosT(), ExecuteCommand_Size, ExecuteCommand_PtrPos);
			if (ExecuteCommand_PtrValue == -1)
			{
				return false;
			}
			if (bSwap != 0)
			{
				ExecuteCommand_PtrValue = EndianSwap(ExecuteCommand_PtrValue, ExecuteCommand_Size / 8);
			}
			File.WriteP(ExecuteCommand_PtrValue, ExecuteCommand_Size / 8, 1, ExecuteCommand_PtrPos);
			Logger.Log("%6u WRITE     PointerList '%s' ScriptPos $%X PointerPos $%X PointerValue $%08X\n", Cmd.Line, Cmd.Parameters[0].Value.c_str(), File.GetPosT(), ExecuteCommand_PtrPos, ExecuteCommand_PtrValue);
			return true;
		case CMD_AUTOWRITETBL:
			ExecuteCommand_Tbl = (PointerTable)VarMap.GetVar(Cmd.Parameters[0].Value).GetData();
			if (ExecuteCommand_Tbl == null)
			{
				Logger.ReportError(CurrentLine, "Identifier '%s' has not been initialized with PTRTBL", Cmd.Parameters[0].Value.c_str());
				return false;
			}
			ExecuteCommand_Success = File.AutoWrite(ExecuteCommand_Tbl, Cmd.Parameters[1].Value);
			if (!ExecuteCommand_Success)
			{
				Logger.ReportError(CurrentLine, "'%s' has not been defined as an end token in the active table", Cmd.Parameters[1].Value.c_str());
			}
			else
			{
				Logger.Log("%6u AUTOWRITE EndTag '%s' PointerTable '%s'\n", Cmd.Line, Cmd.Parameters[0].Value.c_str(), Cmd.Parameters[1].Value.c_str());
			}
			return ExecuteCommand_Success;
		case CMD_AUTOWRITELIST:
			ExecuteCommand_List = (PointerList)VarMap.GetVar(Cmd.Parameters[0].Value).GetData();
			if (ExecuteCommand_List == null)
			{
				Logger.ReportError(CurrentLine, "Identifier '%s' has not been initialized with PTRLIST", Cmd.Parameters[0].Value.c_str());
				return false;
			}
			ExecuteCommand_Success = File.AutoWrite(ExecuteCommand_List, Cmd.Parameters[1].Value);
			if (!ExecuteCommand_Success)
			{
				Logger.ReportError(CurrentLine, "'%s' has not been defined as an end token in the active table", Cmd.Parameters[1].Value.c_str());
			}
			else
			{
				Logger.Log("%6u AUTOWRITE EndTag '%s' PointerList '%s'\n", Cmd.Line, Cmd.Parameters[0].Value.c_str(), Cmd.Parameters[1].Value.c_str());
			}
			return ExecuteCommand_Success;
		case CMD_CREATEPTR:
			ExecuteCommand_Success = PtrHandler.CreatePointer(Cmd.Parameters[0].Value, Cmd.Parameters[1].Value, StringToInt64(Cmd.Parameters[2].Value), StringToUInt(Cmd.Parameters[3].Value), HeaderSize);
			if (ExecuteCommand_Success)
			{
				Logger.Log("%6u CREATEPTR CustomPointer '%s' Addressing '%s' Offsetting %I64d Size %dd HeaderSize $%X\n", Cmd.Line, Cmd.Parameters[0].Value.c_str(), Cmd.Parameters[1].Value.c_str(), StringToInt64(Cmd.Parameters[2].Value), StringToUInt(Cmd.Parameters[3].Value), HeaderSize);
			}
			return ExecuteCommand_Success;
		case CMD_WRITEPTR:
			ExecuteCommand_PtrValue = PtrHandler.GetPtrAddress(Cmd.Parameters[0].Value, File.GetPosT(), ExecuteCommand_Size);
			ExecuteCommand_PtrPos = StringToUInt(Cmd.Parameters[1].Value);
			if (ExecuteCommand_PtrValue == -1)
			{
				return false;
			}
			if (bSwap != 0)
			{
				ExecuteCommand_PtrValue = EndianSwap(ExecuteCommand_PtrValue, ExecuteCommand_Size / 8);
			}
			File.WriteP(ExecuteCommand_PtrValue, ExecuteCommand_Size / 8, 1, ExecuteCommand_PtrPos);
			Logger.Log("%6u WRITE     CustomPointer '%s' ScriptPos $%X PointerPos $%X PointerValue $%08X\n", Cmd.Line, Cmd.Parameters[0].Value.c_str(), File.GetPosT(), ExecuteCommand_PtrPos, ExecuteCommand_PtrValue);
			return true;
		case CMD_LOADEXT:
			ExecuteCommand_Success = Extensions.LoadExtension(Cmd.Parameters[0].Value, Cmd.Parameters[1].Value);
			if (ExecuteCommand_Success)
			{
				Logger.Log("%6u LOADEXT   Loaded extension %s successfully\n", Cmd.Line, Cmd.Parameters[1].Value.c_str());
			}
			return ExecuteCommand_Success;
		case CMD_EXECEXT:
			return ExecuteExtension(Cmd.Parameters[0].Value, Cmd.Parameters[1].Value, ExecuteCommand_Context);
		case CMD_DISABLETABLE:
			ExecuteCommand_Tbl = (PointerTable)VarMap.GetVar(Cmd.Parameters[0].Value).GetData();
			if (ExecuteCommand_Tbl == null)
			{
				Logger.ReportError(CurrentLine, "Identifier '%s' has not been initialized with PTRTBL", Cmd.Parameters[0].Value.c_str());
				return false;
			}
			ExecuteCommand_Success = File.DisableWrite(Cmd.Parameters[1].Value, true);
			if (!ExecuteCommand_Success)
			{
				Logger.ReportError(CurrentLine, "'%s' has not been defined as an autowrite end token", Cmd.Parameters[1].Value.c_str());
			}
			else
			{
				Logger.Log("%6u DISABLE   EndTag '%s' PointerTable '%s'\n", Cmd.Line, Cmd.Parameters[0].Value.c_str(), Cmd.Parameters[1].Value.c_str());
			}
			return ExecuteCommand_Success;
		case CMD_DISABLELIST:
			ExecuteCommand_List = (PointerList)VarMap.GetVar(Cmd.Parameters[0].Value).GetData();
			if (ExecuteCommand_List == null)
			{
				Logger.ReportError(CurrentLine, "Identifier '%s' has not been initialized with PTRLIST", Cmd.Parameters[0].Value.c_str());
				return false;
			}
			ExecuteCommand_Success = File.DisableWrite(Cmd.Parameters[1].Value, false);
			if (!ExecuteCommand_Success)
			{
				Logger.ReportError(CurrentLine, "'%s' has not been defined as an autowrite end token", Cmd.Parameters[1].Value.c_str());
			}
			else
			{
				Logger.Log("%6u DISABLE   EndTag '%s' PointerList '%s'\n", Cmd.Line, Cmd.Parameters[1].Value.c_str(), Cmd.Parameters[0].Value.c_str());
			}
			return ExecuteCommand_Success;
		case CMD_PASCALLEN:
			ExecuteCommand_Success = File.SetPascalLength(StringToUInt(Cmd.Parameters[0].Value));
			if (ExecuteCommand_Success)
			{
				Logger.Log("%6u PASCALLEN Length for pascal strings set to %u\n", Cmd.Line, StringToUInt(Cmd.Parameters[0].Value));
			}
			else
			{
				Logger.ReportError(CurrentLine, "Invalid length %u for PASCALLEN", StringToUInt(Cmd.Parameters[0].Value));
			}
			return ExecuteCommand_Success;
		case CMD_AUTOEXEC:
			ExecuteCommand_Ext = (AtlasExtension)VarMap.GetVar(Cmd.Parameters[0].Value).GetData();
			if (ExecuteCommand_Ext == null)
			{
				Logger.ReportError(CurrentLine, "Identifier '%s' has not been initialized with LOADEXT", Cmd.Parameters[0].Value.c_str());
				return false;
			}
			ExecuteCommand_Success = File.AutoWrite(ExecuteCommand_Ext, Cmd.Parameters[1].Value, Cmd.Parameters[2].Value);
			if (ExecuteCommand_Success)
			{
				Logger.Log("%6u AUTOEXEC  EndTag '%s' Extension '%s'\n", Cmd.Line, Cmd.Parameters[0].Value.c_str(), Cmd.Parameters[1].Value.c_str());
			}
			return ExecuteCommand_Success;
		case CMD_DISABLEEXEC:
			ExecuteCommand_Success = File.DisableAutoExtension(Cmd.Parameters[0].Value, Cmd.Parameters[1].Value);
			if (ExecuteCommand_Success)
			{
				Logger.Log("%6u DISABLE   EndTag '%s' Extension Function '%s'\n", Cmd.Line, Cmd.Parameters[1].Value.c_str(), Cmd.Parameters[0].Value.c_str());
			}
			return ExecuteCommand_Success;
		case CMD_FIXEDLENGTH:
			ExecuteCommand_Success = File.SetFixedLength(StringToUInt(Cmd.Parameters[0].Value), StringToUInt(Cmd.Parameters[1].Value));
			if (ExecuteCommand_Success)
			{
				Logger.Log("%6u FIXEDLENGTH Length %d PaddingValue %d\n", Cmd.Line, StringToUInt(Cmd.Parameters[0].Value), StringToUInt(Cmd.Parameters[0].Value));
			}
			else
			{
				Logger.ReportError(CurrentLine, "FixedLength used a padding value not in the range of 0-255");
			}
			return ExecuteCommand_Success;
		case CMD_WUBCUST:
			ExecuteCommand_PtrValue = PtrHandler.GetPtrAddress(Cmd.Parameters[0].Value, File.GetPosT(), ExecuteCommand_Size);
			ExecuteCommand_PtrPos = StringToUInt(Cmd.Parameters[1].Value);
			if (ExecuteCommand_PtrValue == -1)
			{
				return false;
			}
			ExecuteCommand_PtrByte = (ExecuteCommand_PtrValue & 0xFF000000) >> 24;
			File.WriteP(ExecuteCommand_PtrByte, 1, 1, ExecuteCommand_PtrPos);
			Logger.Log("%6u WUB       CustomPointer '%s' ScriptPos $%X PointerPos $%X PointerValue $%02X\n", Cmd.Line, Cmd.Parameters[0].Value.c_str(), File.GetPosT(), ExecuteCommand_PtrPos, ExecuteCommand_PtrByte);
			return true;
		case CMD_WBBCUST:
			ExecuteCommand_PtrValue = PtrHandler.GetPtrAddress(Cmd.Parameters[0].Value, File.GetPosT(), ExecuteCommand_Size);
			ExecuteCommand_PtrPos = StringToUInt(Cmd.Parameters[1].Value);
			if (ExecuteCommand_PtrValue == -1)
			{
				return false;
			}
			ExecuteCommand_PtrByte = (ExecuteCommand_PtrValue & 0xFF0000) >> 16;
			File.WriteP(ExecuteCommand_PtrByte, 1, 1, ExecuteCommand_PtrPos);
			Logger.Log("%6u WBB       CustomPointer '%s' ScriptPos $%X PointerPos $%X PointerValue $%02X\n", Cmd.Line, Cmd.Parameters[0].Value.c_str(), File.GetPosT(), ExecuteCommand_PtrPos, ExecuteCommand_PtrByte);
			return true;
		case CMD_WHBCUST:
			ExecuteCommand_PtrValue = PtrHandler.GetPtrAddress(Cmd.Parameters[0].Value, File.GetPosT(), ExecuteCommand_Size);
			ExecuteCommand_PtrPos = StringToUInt(Cmd.Parameters[1].Value);
			if (ExecuteCommand_PtrValue == -1)
			{
				return false;
			}
			ExecuteCommand_PtrByte = (ExecuteCommand_PtrValue & 0xFF00) >> 8;
			File.WriteP(ExecuteCommand_PtrByte, 1, 1, ExecuteCommand_PtrPos);
			Logger.Log("%6u WHB       CustomPointer '%s' ScriptPos $%X PointerPos $%X PointerValue $%02X\n", Cmd.Line, Cmd.Parameters[0].Value.c_str(), File.GetPosT(), ExecuteCommand_PtrPos, ExecuteCommand_PtrByte);
			return true;
		case CMD_WLBCUST:
			ExecuteCommand_PtrValue = PtrHandler.GetPtrAddress(Cmd.Parameters[0].Value, File.GetPosT(), ExecuteCommand_Size);
			ExecuteCommand_PtrPos = StringToUInt(Cmd.Parameters[1].Value);
			if (ExecuteCommand_PtrValue == -1)
			{
				return false;
			}
			ExecuteCommand_PtrByte = ExecuteCommand_PtrValue & 0xFF;
			File.WriteP(ExecuteCommand_PtrByte, 1, 1, ExecuteCommand_PtrPos);
			Logger.Log("%6u WLB       CustomPointer '%s' ScriptPos $%X PointerPos $%X PointerValue $%02X\n", Cmd.Line, Cmd.Parameters[0].Value.c_str(), File.GetPosT(), ExecuteCommand_PtrPos, ExecuteCommand_PtrByte);
			return true;
		case CMD_ENDIANSWAP:
			ExecuteCommand_Success = SetEndianSwap(Cmd.Parameters[0].Value);
			if (ExecuteCommand_Success)
			{
				Logger.Log("%6u ENDIANSWAP '%s'\n", Cmd.Line, Cmd.Parameters[0].Value.c_str());
			}
			return ExecuteCommand_Success;
		case CMD_STRINGALIGN:
			StringAlign = StringToUInt(Cmd.Parameters[0].Value);
			Logger.Log("%6u STRINGALIGN '%s'\n", Cmd.Line, Cmd.Parameters[0].Value.c_str());
			return true;
		case CMD_EMBPTRTABLE:
			ExecuteCommand_Success = PtrHandler.CreateEmbPointerTable(Cmd.Parameters[0].Value, File.GetPosT(), StringToUInt(Cmd.Parameters[1].Value), Cmd.Parameters[2].Value);
			ExecuteCommand_StartPos = File.GetPosT();
			ExecuteCommand_Size = PtrHandler.GetPtrSize(Cmd.Parameters[2].Value);
			if (ExecuteCommand_Success)
			{
				int TableSize = (ExecuteCommand_Size / 8) * StringToUInt(Cmd.Parameters[1].Value);
				if (File.GetMaxWritableBytes() == -1 || (int)File.GetMaxWritableBytes() > TableSize)
				{
					// Reserve space inside the script for the embedded pointer table
					byte Zero = 0;
					for (int i = 0; i < TableSize; i++)
					{
						File.WriteT(Zero, 1, 1);
					}
    
					Logger.Log("%6u EMBPTRTBL Pointer Table '%s' created StartPos $%X PtrCount %dd CustomPointer '%s'\n", Cmd.Line, Cmd.Parameters[0].Value.c_str(), ExecuteCommand_StartPos, StringToUInt(Cmd.Parameters[1].Value), Cmd.Parameters[2].Value.c_str());
				}
				else
				{
					Logger.Log("%6u EMBPTRTABLE Failed to allocate due to insufficient space within script\n", Cmd.Line);
				}
			}
			return ExecuteCommand_Success;
		case CMD_WHW:
			ExecuteCommand_PtrValue = DefaultPointer.GetHighWord(File.GetPosT());
			if (bSwap != 0)
			{
				ExecuteCommand_PtrValue = EndianSwap(ExecuteCommand_PtrValue, 2);
			}
			File.WriteP(ExecuteCommand_PtrValue, 2, 1, StringToUInt(Cmd.Parameters[0].Value));
			Logger.Log("%6u WHW       ScriptPos $%X PointerPos $%X PointerValue $%04X\n", Cmd.Line, File.GetPosT(), StringToUInt(Cmd.Parameters[0].Value), ExecuteCommand_PtrValue);
			return true;
		case CMD_WHWCUST:
			ExecuteCommand_PtrValue = PtrHandler.GetPtrAddress(Cmd.Parameters[0].Value, File.GetPosT(), ExecuteCommand_Size);
			ExecuteCommand_PtrPos = StringToUInt(Cmd.Parameters[1].Value);
			if (ExecuteCommand_PtrValue == -1)
			{
				return false;
			}
			ExecuteCommand_PtrValue = (ExecuteCommand_PtrValue & 0xFFFF0000) >> 16;
			if (bSwap != 0)
			{
				ExecuteCommand_PtrValue = EndianSwap(ExecuteCommand_PtrValue, 2);
			}
			File.WriteP(ExecuteCommand_PtrByte, 2, 1, ExecuteCommand_PtrPos);
			Logger.Log("%6u WHW       CustomPointer '%s' ScriptPos $%X PointerPos $%X PointerValue $%04X\n", Cmd.Line, Cmd.Parameters[0].Value.c_str(), File.GetPosT(), ExecuteCommand_PtrPos, ExecuteCommand_PtrValue);
			return true;
		case CMD_SETTARGETFILE:
			File.CloseFileT();
			ExecuteCommand_Success = File.OpenFileT(Cmd.Parameters[0].Value.c_str());
			IsInJmp = false;
			if (!ExecuteCommand_Success)
			{
				Logger.ReportError(CurrentLine, "SETTARGETFILE Could not open file '%s'", Cmd.Parameters[0].Value.c_str());
			}
			else
			{
				Logger.Log("%6u SETTARGETFILE '%s'", Cmd.Line, Cmd.Parameters[0].Value.c_str());
			}
			return ExecuteCommand_Success;
		case CMD_SETPTRFILE:
			File.CloseFileP();
			ExecuteCommand_Success = File.OpenFileP(Cmd.Parameters[0].Value.c_str());
			if (!ExecuteCommand_Success)
			{
				Logger.ReportError(CurrentLine, "SETPTRFILE Could not open file '%s'", Cmd.Parameters[0].Value.c_str());
			}
			else
			{
				Logger.Log("%6u SETPTRFILE '%s'", Cmd.Line, Cmd.Parameters[0].Value.c_str());
			}
			return ExecuteCommand_Success;
		case CMD_WRITEEMBTBL1:
			ExecuteCommand_PtrValue = PtrHandler.GetEmbTableAddress(Cmd.Parameters[0].Value, File.GetPosT(), ExecuteCommand_Size, ExecuteCommand_PtrPos);
			if (ExecuteCommand_PtrValue == -1)
			{
				Logger.ReportError(CurrentLine, "WRITE EMBPTRTBL '%s' could not write due to insufficient space'", Cmd.Parameters[0].Value.c_str());
				return false;
			}
			if (bSwap != 0)
			{
				ExecuteCommand_PtrValue = EndianSwap(ExecuteCommand_PtrValue, ExecuteCommand_Size / 8);
			}
			File.WriteT(ExecuteCommand_PtrValue, ExecuteCommand_Size / 8, 1, ExecuteCommand_PtrPos);
			Logger.Log("%6u WRITE     PointerTable '%s' ScriptPos $%X PointerPos $%X PointerValue $%08X\n", Cmd.Line, Cmd.Parameters[0].Value.c_str(), File.GetPosT(), ExecuteCommand_PtrPos, ExecuteCommand_PtrValue);
			return true;
		case CMD_WRITEEMBTBL2:
			ExecuteCommand_PtrValue = PtrHandler.GetEmbTableAddress(Cmd.Parameters[0].Value, File.GetPosT(), StringToUInt(Cmd.Parameters[1].Value), ExecuteCommand_Size, ExecuteCommand_PtrPos);
			if (ExecuteCommand_PtrValue == -1)
			{
				Logger.ReportError(CurrentLine, "WRITE EMBPTRTBL '%s' could not write PtrNum '%d' due to out of table range'", Cmd.Parameters[0].Value.c_str(), StringToUInt(Cmd.Parameters[1].Value));
				return false;
			}
			if (bSwap != 0)
			{
				ExecuteCommand_PtrValue = EndianSwap(ExecuteCommand_PtrValue, ExecuteCommand_Size / 8);
			}
			File.WriteT(ExecuteCommand_PtrValue, ExecuteCommand_Size / 8, 1, ExecuteCommand_PtrPos);
			Logger.Log("%6u WRITE     PointerTable '%s' ScriptPos $%X PointerPos $%X PointerValue $%08X\n", Cmd.Line, Cmd.Parameters[0].Value.c_str(), File.GetPosT(), ExecuteCommand_PtrPos, ExecuteCommand_PtrValue);
			return true;
		case CMD_WRITETBL2:
			ExecuteCommand_PtrValue = PtrHandler.GetTableAddress(Cmd.Parameters[0].Value, File.GetPosT(), StringToUInt(Cmd.Parameters[1].Value), ExecuteCommand_Size, ExecuteCommand_PtrPos);
			if (ExecuteCommand_PtrValue == -1)
			{
				return false;
			}
			if (bSwap != 0)
			{
				ExecuteCommand_PtrValue = EndianSwap(ExecuteCommand_PtrValue, ExecuteCommand_Size / 8);
			}
			File.WriteP(ExecuteCommand_PtrValue, ExecuteCommand_Size / 8, 1, ExecuteCommand_PtrPos);
			Logger.Log("%6u WRITE     PointerTable '%s' ScriptPos $%X PointerPos $%X PointerValue $%08X\n", Cmd.Line, Cmd.Parameters[0].Value.c_str(), File.GetPosT(), ExecuteCommand_PtrPos, ExecuteCommand_PtrValue);
			return true;
		default:
	//C++ TO C# CONVERTER TODO TASK: There is no direct equivalent in C# to the following C++ macro:
			Logger.BugReport(__LINE__, __FILE__, "Bad Cmd #%u", Cmd.Function);
			return false;
		}
	}

	public void PrintSummary(string Title, uint TimeCompleted)
	{
		uint SumErrors = 0;
		uint SumWarnings = 0;
		Console.Write("{0} summary: {1:D} msecs\n", Title, TimeCompleted);
    
		// Print errors and warnings
		for (ListErrorIt i = Logger.Errors.begin(); i != Logger.Errors.end(); i++)
		{
			if (i.Severity == FATALERROR)
			{
				Console.Write("Error: ");
				SumErrors++;
			}
			else if (i.Severity == WARNING)
			{
				Console.Write("Warning: ");
				SumWarnings++;
			}
			Console.Write("{0} on line {1:D}\n", i.Error.c_str(), i.LineNumber);
		}
		Logger.Errors.clear();
		Console.Write("{0} - {1:D} error(s), {2:D} warning(s)\n\n", Title, SumErrors, SumWarnings);
	}
	public void PrintStatistics()
	{
		string Frame = "+------------------------------------------------------------------------------";
    
		if (Stats.Stats.size() == 0)
		{
			; // Do nothing
		}
		else if (Stats.Stats.size() == 1)
		{
			PrintStatisticsBlock("Total", Stats.Stats.front());
		}
		else // Print each block's content
		{
			uint blocknum = 1;
			string buf = new string(new char[127]);
			for (ListStatsIt i = Stats.Stats.begin(); i != Stats.Stats.end(); i++)
			{
				_snprintf(buf, 127, "Block %d", blocknum);
				PrintStatisticsBlock(buf, *i);
				blocknum++;
			}
    
			Stats.GenerateTotalStats(Total);
    
			// Print out the total statistics
			Console.Write("{0}\n", Frame);
			Console.Write("| Total Statistics\n");
			Console.Write("| Script Size {0:D}\n", Total.ScriptSize);
			Console.Write("| Script Inserted {0:D}\n", Total.ScriptSize - Total.ScriptOverflowed);
			if (Total.ScriptOverflowed != 0)
			{
				Console.Write("| Script Overflowed {0:D}\n", Total.ScriptOverflowed);
			}
			if (Total.MaxBound != -1)
			{
				Console.Write("| Space Remaining {0:D}\n", Total.SpaceRemaining);
			}
			Console.Write("|\n");
    
			if (Total.HasCommands())
			{
				Console.Write("| Command Execution Listing\n");
				for (int j = 0; j < CommandCount; j++)
				{
					if (Total.ExecCount[j] != 0)
					{
						Console.Write("| {0}: {1:D}\n", CommandStrings[j], Total.ExecCount[j]);
					}
				}
			}
    
			if (Total.EmbPointerWrites > 0 || Total.AutoPointerWrites > 0 || Total.FailedListWrites > 0 || Total.ExtPointerWrites > 0)
			{
				Console.Write("| Pointer Listing\n");
			}
			if (Total.PointerWrites != 0)
			{
				Console.Write("| General Pointers Written: {0:D}\n", Total.PointerWrites);
			}
			if (Total.EmbPointerWrites != 0)
			{
				Console.Write("| Embedded Pointers Written: {0:D}\n", Total.EmbPointerWrites);
			}
			if (Total.AutoPointerWrites != 0)
			{
				Console.Write("| Autowrite Pointers Written: {0:D}\n", Total.AutoPointerWrites);
			}
			if (Total.FailedListWrites != 0)
			{
				Console.Write("| Failed PointerList Writes: {0:D}\n", Total.FailedListWrites);
			}
			if (Total.ExtPointerWrites != 0)
			{
				Console.Write("| Extension Pointer Writes: {0:D}\n", Total.ExtPointerWrites);
			}
			Console.Write("{0}\n\n", Frame);
		}
	}
	public void PrintStatisticsBlock(string Title, InsertionStatistics Stats)
	{
		string Frame = "+------------------------------------------------------------------------------";
    
		Console.Write("{0}\n", Frame);
		Console.Write("| {0}\n|   Start: Line {1:D}  File Position ${2:X}", Title, Stats.LineStart, Stats.StartPos);
		if (Stats.MaxBound != -1)
		{
			Console.Write("  Bound ${0:X}", Stats.MaxBound);
		}
		Console.Write("\n");
		Console.Write("|   End: Line {0:D}  File Position ${1:X}\n", Stats.LineEnd, Stats.StartPos + Stats.ScriptSize - Stats.ScriptOverflowed);
		Console.Write("{0}\n", Frame);
		Console.Write("| Script size {0:D}\n", Stats.ScriptSize);
		Console.Write("| Bytes Inserted {0:D}", Stats.ScriptSize - Stats.ScriptOverflowed);
		if (Stats.ScriptOverflowed != 0)
		{
			Console.Write("\n| Script Overflowed {0:D}", Stats.ScriptOverflowed);
		}
		if (Stats.MaxBound != -1)
		{
			Console.Write("\n| Space Remaining {0:D}", Stats.SpaceRemaining);
		}
		Console.Write("\n|\n");
    
		if (Stats.HasCommands())
		{
			Console.Write("| Command Execution Listing\n");
			for (int j = 0; j < CommandCount; j++)
			{
				if (Stats.ExecCount[j] != 0)
				{
					Console.Write("|   {0}: {1:D}\n", CommandStrings[j], Stats.ExecCount[j]);
				}
			}
			Console.Write("|\n");
		}
    
		Console.Write("| Pointer Listing\n");
		Console.Write("|   General Pointers Written: {0:D}\n", Stats.PointerWrites);
		if (Stats.EmbPointerWrites != 0)
		{
			Console.Write("|   Embedded Pointers Written: {0:D}\n", Stats.EmbPointerWrites);
		}
		if (Stats.AutoPointerWrites != 0)
		{
			Console.Write("|   Autowrite Pointers Written: {0:D}\n", Stats.AutoPointerWrites);
		}
		if (Stats.FailedListWrites != 0)
		{
			Console.Write("|   Failed PointerList Writes: {0:D}\n", Stats.FailedListWrites);
		}
		if (Stats.ExtPointerWrites != 0)
		{
			Console.Write("|   Extension Pointer Writes: {0:D}\n", Stats.ExtPointerWrites);
		}
		Console.Write("{0}\n\n", Frame);
	}
	public void PrintUnwrittenPointers()
	{
		string Frame = "+------------------------------------------------------------------------------";
    
		Console.Write("{0}\n", Frame);
		Console.Write("Printing Initialized But Unwritten Embedded Pointers Summary\n");
		uint TextPos;
		uint PointerPos;
		uint count = 0;
    
		for (int i = 0; i < EmbPtrs.GetListSize(); i++)
		{
			if (!EmbPtrs.GetPointerState(i, TextPos, PointerPos)) // Pointer is uninit'd or not fully written
			{
				if (TextPos == -1 && PointerPos == -1) // Uninitialized
					continue;
				Console.Write("| EmbPtr ${0:X} EMBWRITE ${1:X8} EMBSET ${2:X8}\n", i, TextPos, PointerPos);
				count++;
			}
		}
		Console.Write("|\n| {0:D} Pointer(s) Detected\n", count);
		Console.Write("{0}\n\n", Frame);
	}

	public bool AddTable(Command Cmd)
	{
		Table Tbl;
		GenericVariable Var;
		Tbl = (Table)(VarMap.GetVar(Cmd.Parameters[1].Value)).GetData();
		if (!LoadTable(Cmd.Parameters[0].Value, Tbl))
		{
			return false;
		}
    
		Var = new GenericVariable(Tbl, P_TABLE);
		VarMap.SetVar(Cmd.Parameters[1].Value, Var);
		return true;
	}
	public bool ActivateTable(string TableName)
	{
		Table Tbl = (Table)(VarMap.GetVar(TableName)).GetData();
		if (Tbl == null)
		{
			ostringstream ErrorStr = new ostringstream();
			ErrorStr << "Uninitialized variable " << TableName << " used";
			Logger.ReportError(CurrentLine, "Uninitialized variable '%s' used", TableName);
			return false;
		}
		else
		{
			File.SetTable(Tbl);
			return true;
		}
	}
	public bool LoadTable(string FileName, Table[] Tbl)
	{
		if (Tbl != null) // Initialized already, overwrite
		{
			Tbl = null;
			Tbl = null;
		}
		Tbl = new Table();
		int Result = Tbl.OpenTable(FileName);
    
		ostringstream ErrorStr = new ostringstream();
		switch (Result)
		{
		case DefineConstants.TBL_OK:
			break;
		case DefineConstants.TBL_PARSE_ERROR:
			Logger.ReportError(CurrentLine, "The table file '%s' is incorrectly formatted", FileName);
			return false;
		case DefineConstants.TBL_OPEN_ERROR:
			Logger.ReportError(CurrentLine, "The table file '%s' could not be opened", FileName);
			return false;
		}
    
		return true;
	}
	public bool SetEndianSwap(string Swap)
	{
		if (Swap == "TRUE")
		{
			bSwap = 1;
		}
		else if (Swap == "FALSE")
		{
			bSwap = 0;
		}
		else
		{
			return false;
		}
    
		return true;
	}
}