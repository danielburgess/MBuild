using System.Collections.Generic;

public class AtlasParser
{
	public AtlasParser(VariableMap Map)
	{
		VarMap = Map;
    
		// Initialize the function lookup map
		for (uint i = 0; i < CommandCount; i++)
		{
			CmdMap.insert(multimap<string,uint>.value_type(CommandStrings[i], i));
		}
    
		for (uint i = 0; i < TypeCount; i++)
		{
			TypeMap.insert(multimap<string,uint>.value_type(TypeStrings[i], i));
		}
    
		CurrentLine = 0;
		CurBlock.StartLine = -1;
	}
	public void Dispose()
	{
	}
	public bool ParseFile(ifstream infile)
	{
		LinkedList<string> text = new LinkedList<string>();
		byte[] utfheader = new byte[4];
    
		string line;
    
		// Detect UTF-8 header
		if (infile.peek() == 0xEF)
		{
			infile.read((string)utfheader, 3);
			if (utfheader[0] != 0xEF || utfheader[1] != 0xBB || utfheader[2] != 0xBF)
			{
				infile.seekg(ios.beg); // Seek beginning, not a UTF-8 header
			}
		}
    
		// Read the file
		while (!infile.eof())
		{
			getline(infile, line);
			text.AddLast(line);
		}
    
		infile.close();
		CurrentLine = 1;
    
		// Parse the file and build the series of AtlasBlocks
		for (ListStringIt it = text.GetEnumerator(); it != text.end(); it++)
		{
			ParseLine(*it);
			CurrentLine++;
		}
    
		if (!CurBlock.Commands.empty() || !CurBlock.TextLines.empty())
		{
			FlushBlock();
		}
    
		for (ListErrorIt i = Logger.Errors.begin(); i != Logger.Errors.end(); i++)
		{
			if (i.Severity == FATALERROR)
			{
				return false;
			}
		}
    
		return true;
	}

	public LinkedList<AtlasBlock> Blocks = new LinkedList<AtlasBlock>();

	public void ParseLine(string line)
	{
		uint firstchar = line.find_first_not_of(" \t", 0);
    
		if (firstchar == -1) // All whitespace
		{
			string s = "";
			AddText(s);
			return;
		}
    
		string editline = line.Substring(firstchar, line.Length - firstchar);
    
		switch (line[firstchar])
		{
		case '#': // Atlas command
			if (CurBlock.TextLines.empty()) // No text, build more commands
			{
				ParseCommand(editline);
			}
			else // Clear, parse the command
			{
				FlushBlock();
				ParseCommand(editline);
			}
			break;
		case '/': // Possible comment
			if (line.Length > firstchar + 1)
			{
				if (line[firstchar + 1] != '/') // Not a comment "//", but text
				{
					AddText(line);
				}
				// else; Comment
			}
			else // Single text character of '/'
			{
				AddText(line);
			}
			break;
		default: // Text
			AddText(line);
			break;
		}
	}
	public void ParseCommand(string line)
	{
		if (line[0] != '#')
		{
	//C++ TO C# CONVERTER TODO TASK: There is no direct equivalent in C# to the following C++ macro:
			Console.Write("Bug, {0} {1:D}.  Should start with a '#'\n'{2}'", __FILE__, __LINE__, line);
		}
    
		uint curpos = 1;
		string CmdStr;
    
		Parameter Param = new Parameter();
		Command Command = new Command();
		Param.Type = P_INVALID;
    
		sbyte ch;
    
		while (curpos < line.Length && (ch = line[curpos]) != '(')
		{
			if (char.IsLetter(ch) || char.IsDigit(ch))
			{
				CmdStr += (char)ch;
			}
			else
			{
				Logger.ReportError(CurrentLine, "Invalid syntax: Nonalphabetical character in command");
			}
    
			curpos++;
		}
    
		curpos = line.find_first_not_of(" \t", curpos + 1); // Skip ')', get first non
															// wspace character
    
		// Parse parameters
		uint ParamNum = 1;
		while (curpos < line.Length && (ch = line[curpos]) != ')')
		{
			if (ch == ',')
			{
				// Trim trailing whitespace
				uint Last;
				for (Last = Param.Value.length() - 1; Last > 0; Last--)
				{
					if (Param.Value[Last] != ' ' && Param.Value[Last] != '\t')
						break;
				}
				if (Last < Param.Value.length())
				{
					Param.Value.erase(Last + 1);
				}
    
				Param.Type = IdentifyType(Param.Value);
				if (Param.Type == P_INVALID)
				{
					Logger.ReportError(CurrentLine, "Invalid argument for %s for parameter %d", CmdStr, ParamNum);
				}
				Command.Parameters.push_back(Param);
				Param.Type = P_INVALID;
				Param.Value.clear();
				ParamNum++;
				curpos = line.find_first_not_of(" \t", curpos + 1);
			}
			else
			{
				Param.Value += ch;
				curpos++;
			}
		}
    
		// Trim trailing whitespace
		uint Last;
		for (Last = Param.Value.length() - 1; Last > 0; Last--)
		{
			if (Param.Value[Last] != ' ' && Param.Value[Last] != '\t')
				break;
		}
		if (Last < Param.Value.length())
		{
		Param.Value.erase(Last + 1);
		}
    
		Param.Type = IdentifyType(Param.Value);
		if (Param.Type == P_INVALID)
		{
			Logger.ReportError(CurrentLine, "Invalid argument for %s for parameter %d", CmdStr, ParamNum);
		}
    
		for (ListErrorIt i = Logger.Errors.begin(); i != Logger.Errors.end(); i++)
		{
			if (i.Severity = FATALERROR)
				return;
		}
    
		Command.Parameters.push_back(Param);
    
		AddCommand(CmdStr, Command);
	}
	public void FlushBlock()
	{
		Blocks.push_back(CurBlock);
		CurBlock.TextLines.clear();
		CurBlock.Commands.clear();
		CurBlock.StartLine = -1;
	}
	public void AddText(string text)
	{
		if (CurBlock.StartLine == -1)
		{
			CurBlock.StartLine = CurrentLine;
		}
    
		CurBlock.TextLines.push_back(text);
	}
	public uint IdentifyType(ref string str)
	{
		if (string.IsNullOrEmpty(str))
		{
			return P_VOID;
		}
    
		uint charpos = 0;
    
		// Check for number (int/uint)
		if (str[0] == '$')
		{
			charpos = str.IndexOfAny((Convert.ToString('-')).ToCharArray(), 1);
			if (charpos == 1 || charpos == -1)
			{
				charpos = str.find_first_not_of("-0123456789ABCDEF", 1);
				if (charpos == -1)
				{
					return P_NUMBER;
				}
			}
		}
		else
		{
			charpos = str.IndexOfAny((Convert.ToString('-')).ToCharArray());
			if (charpos == 0 || charpos == -1)
			{
				charpos = str.find_first_not_of("0123456789", 1);
				if (charpos == -1)
				{
					return P_NUMBER;
				}
			}
		}
    
		// Check for double
		charpos = str.find_first_not_of("0123456789.", 0);
		if (charpos == -1)
		{
			return P_DOUBLE;
		}
    
		// Check for variable
		charpos = str.IndexOfAny((Convert.ToString("abcdefghijklmnopqrstuvwxyzABCDEFGHIJLMNOPQRSTUVWXYZ")).ToCharArray(), 0);
		if (charpos == 0)
		{
			charpos = str.find_first_not_of("abcdefghijklmnopqrstuvwxyzABCDEFGHIJLMNOPQRSTUVWXYZ0123456789", 0);
			if (charpos == -1)
			{
				return P_VARIABLE;
			}
		}
    
		// Check for string
		if ((str[0] == '"') && (str[str.Length - 1] == '"'))
		{
			str = str.Substring(1, str.Length - 2);
			return P_STRING;
		}
    
		return P_INVALID;
	}
	public bool AddCommand(string CmdStr, Command Cmd)
	{
		bool bFound = false;
		uint CmdNum = 0;
    
		Cmd.Line = CurrentLine;
		System.Tuple<StrCmdMapIt, StrCmdMapIt> val = CmdMap.equal_range(CmdStr);
		if (val.Item1 == val.Item2) // not found
		{
			Logger.ReportError(CurrentLine, "Invalid command %s", CmdStr);
			return false;
		}
    
		// Found one or more matches
		for (StrCmdMapIt i = val.Item1; i != val.Item2; i++)
		{
			CmdNum = i.second;
			if (ParamCount[CmdNum] != Cmd.Parameters.size())
				continue;
			// Found a matching arg count function, check types
			ListParamIt it = Cmd.Parameters.begin();
			for (uint j = 0; j < Cmd.Parameters.size(); j++, it++)
			{
				if ((it.Type == P_VARIABLE) && (CmdNum != CMD_VAR)) // Type-checking for vars
				{
					GenericVariable @var = VarMap.GetVar(it.Value);
					if (@var) // Found, check the type
					{
						if (@var.GetType() != Types[CmdNum][j]) // Type mismatch
							break;
						else
						{
							it.Type = Types[CmdNum][j];
						}
					}
					else // NULL
					{
						Logger.ReportError(CurrentLine, "Undefined variable %s", (it.Value).c_str());
						return false;
					}
				}
				if (it.Type != Types[CmdNum][j]) // Type checking for everything
					break;
				if (j == Cmd.Parameters.size() - 1) // Verified final parameter
				{
					bFound = true;
				}
			}
			if (bFound)
				break;
		}
    
		if (bFound) // Successful lookup
		{
			Cmd.Function = CmdNum;
			if (CmdNum == CMD_VAR) // Variable declaration, handled here explicitly
			{
				return AddUnitializedVariable(Cmd.Parameters[0].Value, Cmd.Parameters[1].Value);
			}
			else
			{
				// Hack for preallocating just enough embedded pointers
				if (CmdNum == CMD_EMBSET || CmdNum == CMD_EMBWRITE)
				{
					int ptrcount = StringToUInt(Cmd.Parameters[0].Value);
					if (ptrcount > MaxEmbPtr)
					{
						MaxEmbPtr = ptrcount;
					}
				}
				CurBlock.Commands.push_back(Cmd);
			}
			return true;
		}
		else
		{
			ostringstream ErrorStr = new ostringstream();
			ErrorStr << "Invalid parameters " << "(";
			if (Cmd.Parameters.size() > 0)
			{
				ErrorStr << TypeStrings[Cmd.Parameters.front().Type];
			}
			for (uint i = 1; i < Cmd.Parameters.size(); i++)
			{
				ErrorStr << "," << TypeStrings[Cmd.Parameters[i].Type];
			}
			ErrorStr << ") for " << CmdStr;
			Logger.ReportError(CurrentLine, "%s", ErrorStr.str().c_str());
			return false;
		}
	}
	public bool AddUnitializedVariable(string VarName, string Type)
	{
		StrTypeMapIt it = TypeMap.find(Type);
		if (it == TypeMap.end()) // not found
		{
			Logger.ReportError(CurrentLine, "Invalid VAR declaration of type %s", Type);
			return false;
		}
		else // Add to the variable map
		{
			VarMap.AddVar(VarName, 0, it.second);
			return true;
		}
	}

	private uint CurrentLine;
	private AtlasBlock CurBlock = new AtlasBlock();
	private StrCmdMap CmdMap = new StrCmdMap();
	private StrTypeMap TypeMap = new StrTypeMap();
	private VariableMap VarMap;
}