public static class GlobalMembersAtlasStats
{

	public static StatisticsHandler Stats = new StatisticsHandler();

//C++ TO C# CONVERTER NOTE: This 'CopyFrom' method was converted from the original copy assignment operator:
//ORIGINAL LINE: InsertionStatistics& InsertionStatistics::operator =(const InsertionStatistics& rhs)
	public static InsertionStatistics InsertionStatistics.CopyFrom(InsertionStatistics rhs)
	{
		if (this == rhs)
		{
			return this;
		}

		ScriptSize = rhs.ScriptSize;
		ScriptOverflowed = rhs.ScriptOverflowed;
		SpaceRemaining = rhs.SpaceRemaining;
		PointerWrites = rhs.PointerWrites;
		EmbPointerWrites = rhs.EmbPointerWrites;

		StartPos = rhs.StartPos;
		MaxBound = rhs.MaxBound;

		LineStart = rhs.LineStart;
		LineEnd = rhs.LineEnd;

		for (uint i = 0; i < CommandCount; i++)
		{
			ExecCount[i] = rhs.ExecCount[i];
		}

		return this;
	}
}

















