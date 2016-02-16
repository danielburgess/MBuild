using System;

public static class GlobalMembersAtlasMain
{
	// Atlas main



	public static int _tmain(int argc, _TCHAR[] argv)
	{
		clock_t StartTime = new clock_t();
		clock_t EndTime = new clock_t();
		clock_t ElapsedTime = new clock_t();
		int argoff = 0;

		Logger.SetLogStatus(false);
		StartTime = clock();

		Console.Write("Atlas 1.11 by Klarth\n\n");
		if (argc != 3 && argc != 5)
		{
			Console.Write("Usage: {0} [switches] ROM.ext Script.txt\n", argv[0]);
			Console.Write("Switches: -d filename or -d stdout (debugging)\n");
			Console.Write("Arguments in brackets are optional\n");
			return 1;
		}

		if (string.Compare("-d", argv[1]) == 0)
		{
			if (string.Compare("stdout", argv[2]) == 0)
			{
				Atlas.SetDebugging(stdout);
			}
			else
			{
				Atlas.SetDebugging(fopen(argv[2], "w"));
			}
			argoff += 2;
		}

		if (!Atlas.Insert(argv[1 + argoff], argv[2 + argoff]))
		{
			Console.Write("Insertion failed\n\n");
		}

		EndTime = clock();

		ElapsedTime = EndTime - StartTime;

		Console.Write("Execution time: {0:D} msecs\n", (uint)ElapsedTime);

		return 0;
	}
}

