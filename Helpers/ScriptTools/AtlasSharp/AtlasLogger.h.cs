using System.Collections.Generic;

public static class GlobalMembersAtlasLogger
{

//C++ TO C# CONVERTER NOTE: 'extern' variable declarations are not required in C#:
	//extern AtlasLogger Logger;
}

public enum ErrorSeverity
{
	FATALERROR = 0,
	WARNING
}

public class AtlasError
{
	public string Error;
	public ErrorSeverity Severity;
	public uint LineNumber;
}


public class AtlasLogger
{
	public AtlasLogger()
	{
		output = null;
		isLogging = true;
		Errors.clear();
	}
	public void Dispose()
	{
		if (output != null && output != stdout)
		{
			fclose(output);
		}
	}

	public void ReportError(uint ScriptLine, string FormatStr, params object[] LegacyParamArray)
	{
		AtlasError Error = new AtlasError();
		Error.Severity = FATALERROR;
		Error.LineNumber = ScriptLine;
    
	//	va_list arglist;
		int ParamCount = -1;
	//	va_start(arglist, FormatStr);
		int length = _vsnprintf(buf, BufSize, FormatStr, arglist);
	//	va_end(arglist);
    
		Error.Error.assign(buf, length);
    
		Errors.push_back(Error);
	}
	public void ReportWarning(uint ScriptLine, string FormatStr, params object[] LegacyParamArray)
	{
		AtlasError Error = new AtlasError();
		Error.Severity = WARNING;
		Error.LineNumber = ScriptLine;
    
	//	va_list arglist;
		int ParamCount = -1;
	//	va_start(arglist, FormatStr);
		int length = _vsnprintf(buf, BufSize, FormatStr, arglist);
	//	va_end(arglist);
    
		Error.Error.assign(buf, length);
    
		Errors.push_back(Error);
	}
	public void Log(string FormatStr, params object[] LegacyParamArray)
	{
		if (isLogging && output)
		{
	//		va_list arglist;
			int ParamCount = -1;
	//		va_start(arglist, FormatStr);
			vfprintf(output, FormatStr, arglist);
	//		va_end(arglist);
		}
	}
	public void SetLogSource(FILE OutputSource)
	{
		output = OutputSource;
	}
	public void SetLogStatus(bool LoggingOn)
	{
		isLogging = LoggingOn;
	}
	public void BugReportLine(uint Line, string Filename, string Msg)
	{
		Console.Error.Write("Bug: {0} Line {1:D} in source file {2}\n", Msg, Line, Filename);
	}
	public void BugReport(uint Line, string Filename, string FormatStr, params object[] LegacyParamArray)
	{
		Console.Error.Write("Bug: ");
	//	va_list arglist;
		int ParamCount = -1;
	//	va_start(arglist, FormatStr);
		vfprintf(stderr, FormatStr, arglist);
	//	va_end(arglist);
		Console.Error.Write(" Line {0:D} in source file {1}\n", Line, Filename);
	}

	public LinkedList<AtlasError> Errors = new LinkedList<AtlasError>();

	private FILE output;
	private const uint BufSize = 512;
	private string buf = new string(new char[BufSize]);
	private bool isLogging;
}

//C++ TO C# CONVERTER NOTE: The following #define macro was replaced in-line:
//ORIGINAL LINE: #define ReportBug(msg) Logger.BugReport(__LINE__, __FILE__, msg)