using System.Collections.Generic;

public static class GlobalMembersAtlasStats
{


//C++ TO C# CONVERTER NOTE: 'extern' variable declarations are not required in C#:
	//extern StatisticsHandler Stats;
}

public class InsertionStatistics
{
	public InsertionStatistics()
	{
		ClearStats();
	}
	public void AddStats(InsertionStatistics Stats)
	{
		ScriptSize += Stats.ScriptSize;
		ScriptOverflowed += Stats.ScriptOverflowed;
		SpaceRemaining += Stats.SpaceRemaining;
		PointerWrites += Stats.PointerWrites;
		EmbPointerWrites += Stats.EmbPointerWrites;
		AutoPointerWrites += Stats.AutoPointerWrites;
		FailedListWrites += Stats.FailedListWrites;
		ExtPointerWrites += Stats.ExtPointerWrites;
    
		for (int j = 0; j < CommandCount; j++)
		{
			ExecCount[j] += Stats.ExecCount[j];
		}
	}
	public void ClearStats()
	{
		ScriptSize = 0;
		ScriptOverflowed = 0;
		SpaceRemaining = 0;
		PointerWrites = 0;
		EmbPointerWrites = 0;
		AutoPointerWrites = 0;
		FailedListWrites = 0;
		ExtPointerWrites = 0;
    
		StartPos = 0;
		MaxBound = -1;
    
		LineStart = 0;
		LineEnd = 0;
	//C++ TO C# CONVERTER TODO TASK: The memory management function 'memset' has no equivalent in C#:
		memset(ExecCount, 0, 4 * CommandCount);
	}
	public void Init(uint StartPos, uint UpperBound, uint LineStart)
	{
		ClearStats();
		this.StartPos = StartPos;
		this.LineStart = LineStart;
		if (UpperBound != -1)
		{
			MaxBound = UpperBound;
		}
	}
	public void AddCmd(uint CmdNum)
	{
		ExecCount[CmdNum]++;
		switch (CmdNum)
		{
		case CMD_WUB:
	case CMD_WBB:
	case CMD_WHB:
	case CMD_WLB:
	case CMD_WHW:
		case CMD_W16:
	case CMD_W24:
	case CMD_W32:
	case CMD_WRITEPTR:
		case CMD_WUBCUST:
	case CMD_WBBCUST:
	case CMD_WHBCUST:
	case CMD_WLBCUST:
		case CMD_WHWCUST:
			PointerWrites++;
			break;
		default:
			break;
		}
	}
	public bool HasCommands()
	{
		for (uint i = 0; i < CommandCount; i++)
		{
			if (ExecCount[i] != 0)
			{
				return true;
			}
		}
    
		return false;
	}

//C++ TO C# CONVERTER NOTE: This 'CopyFrom' method was converted from the original copy assignment operator:
//ORIGINAL LINE: InsertionStatistics& operator =(const InsertionStatistics& rhs);
//C++ TO C# CONVERTER TODO TASK: The implementation of the following method could not be found:
//	InsertionStatistics CopyFrom(InsertionStatistics rhs);

	public uint StartPos;
	public uint ScriptSize;
	public uint ScriptOverflowed;
	public uint SpaceRemaining;
	public uint MaxBound;

	public uint LineStart;
	public uint LineEnd;

	public uint PointerWrites;
	public uint EmbPointerWrites;
	public uint AutoPointerWrites;
	public uint FailedListWrites;
	public uint ExtPointerWrites;

	public uint[] ExecCount = new uint[CommandCount];
}

public class StatisticsHandler
{
	public StatisticsHandler()
	{
	}
	public void Dispose()
	{
	}

	public LinkedList<InsertionStatistics> Stats = new LinkedList<InsertionStatistics>();

	public void NewStatsBlock(uint StartPos, uint UpperBound, uint LineStart)
	{
		if (CurBlock.LineStart != 0) // If not first block
		{
			CurBlock.ScriptOverflowed = 0;
			CurBlock.SpaceRemaining = 0;
    
			if (CurBlock.MaxBound != -1) // if there is a MaxBound, calc overflow and remaining space
			{
				if (CurBlock.StartPos + CurBlock.ScriptSize > CurBlock.MaxBound + 1)
				{
					CurBlock.ScriptOverflowed = CurBlock.StartPos + CurBlock.ScriptSize - CurBlock.MaxBound;
				}
				else
				{
					CurBlock.ScriptOverflowed = 0;
				}
    
				if (CurBlock.MaxBound + 1 > (CurBlock.StartPos + CurBlock.ScriptSize))
				{
					CurBlock.SpaceRemaining = CurBlock.MaxBound + 1 - (CurBlock.StartPos + CurBlock.ScriptSize);
				}
				else
				{
					CurBlock.SpaceRemaining = 0;
				}
			}
			CurBlock.LineEnd = LineStart;
			Stats.push_back(CurBlock);
		}
    
		CurBlock.Init(StartPos, UpperBound, LineStart);
	}
	public void AddCmd(uint CmdNum)
	{
		if (CmdNum < CommandCount)
		{
			CurBlock.AddCmd(CmdNum);
		}
	}
	public void AddScriptBytes(uint Count)
	{
		CurBlock.ScriptSize += Count;
	}
	public void End(uint EndLine)
	{
		if (CurBlock.StartPos + CurBlock.ScriptSize > CurBlock.MaxBound)
		{
			CurBlock.ScriptOverflowed = CurBlock.StartPos + CurBlock.ScriptSize - CurBlock.MaxBound;
		}
		else
		{
			CurBlock.ScriptOverflowed = 0;
		}
    
		if (CurBlock.MaxBound != -1 && CurBlock.MaxBound > (CurBlock.StartPos + CurBlock.ScriptSize))
		{
			CurBlock.SpaceRemaining = CurBlock.MaxBound - (CurBlock.StartPos + CurBlock.ScriptSize);
		}
		else
		{
			CurBlock.SpaceRemaining = 0;
		}
    
		CurBlock.LineEnd = EndLine;
		Stats.push_back(CurBlock);
		CurBlock.ClearStats();
	}
	public void GenerateTotalStats(ref InsertionStatistics Total)
	{
		if (Stats.empty())
		{
			ReportBug("Invalid size for statistics list in StatisticsHandler::GenerateTotalStats");
			return;
		}
		else if (Stats.size() == 1)
		{
			Total = Stats.front();
			return;
		}
    
		for (ListStatsIt i = Stats.begin(); i != Stats.end(); i++)
		{
			Total.AddStats(*i);
		}
	}
	public void IncGenPointerWrites()
	{
		CurBlock.PointerWrites++;
	}
	public void IncEmbPointerWrites()
	{
		CurBlock.EmbPointerWrites++;
	}
	public void IncAutoPointerWrites()
	{
		CurBlock.AutoPointerWrites++;
	}
	public void IncFailedListWrites()
	{
		CurBlock.FailedListWrites++;
	}
	public void IncExtPointerWrites()
	{
		CurBlock.ExtPointerWrites++;
	}

	private InsertionStatistics CurBlock = new InsertionStatistics();
}