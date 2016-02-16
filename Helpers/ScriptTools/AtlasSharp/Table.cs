using System.Collections.Generic;
using System;

public static class GlobalMembersTable
{

//-----------------------------------------------------------------------------
// HexToDec(...) - Converts a hex character to its dec equiv
//-----------------------------------------------------------------------------


	public static ushort HexToDec(sbyte HexChar)
	{
		switch (HexChar)
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
		case 'a':
	case 'A':
		return 10;
		case 'b':
	case 'B':
		return 11;
		case 'c':
	case 'C':
		return 12;
		case 'd':
	case 'D':
		return 13;
		case 'e':
	case 'E':
		return 14;
		case 'f':
	case 'F':
		return 15;
		}

		// Never gets here
		Console.Write("badstr");
		return 15;
	}
}

//-----------------------------------------------------------------------------
// Table - A table library by Klarth, http://rpgd.emulationworld.com/klarth
// email - stevemonaco@hotmail.com
// Open source and free to use
//-----------------------------------------------------------------------------

//-----------------------------------------------------------------------------
// Return Messages
//-----------------------------------------------------------------------------


// Other


// Structure for errors
public class TBL_ERROR
{
	public uint LineNo; // The line number which the error occurred
	public string ErrorDesc; // A description of what the error was
}

// Data Structure for a table bookmark
public class TBL_BOOKMARK
{
	public uint address;
	public string description;
}

// Data Structure for a script dump bookmark
public class TBL_DUMPMARK
{
	public uint StartAddress;
	public uint EndAddress;
	public string description;
}

// Data Structure for a script insertion bookmark
public class TBL_INSMARK
{
	public uint address;
	public string filename;
	public string description;
}

// Data Structure for a script string
public class TBL_STRING
{
	public string Text;
	public string EndToken;
}

// Data Structure for an unencoded (text) string
public class TXT_STRING
{
	public string Text;
	public string EndToken;
}


//-----------------------------------------------------------------------------
// Table Interfaces
//-----------------------------------------------------------------------------

public class Table
{
	public Table()
	{
		TblEntries = 0;
//C++ TO C# CONVERTER TODO TASK: The memory management function 'memset' has no equivalent in C#:
		memset(LongestText, 0, 256 * 4);
		LongestText[(int)'<'] = 5; // Length of <$XX>
		LongestText[(int)'('] = 5; // Length of ($XX)
		LongestHex = 1;
		StringCount = 0;
		bAddEndToken = true;
	}
	public void Dispose()
	{
		// Clear Errors
		if (Errors.Count > 0)
		{
			Errors.Clear();
		}

		// Clear the map
		if (LookupHex.Count > 0)
		{
			LookupHex.Clear();
		}
	}


	//-----------------------------------------------------------------------------
	// OpenTable() - Opens, Parses, and Loads a file to memory
	//-----------------------------------------------------------------------------

	public int OpenTable(string TableFilename)
	{
		string HexVal;
		sbyte testchar;
		string TextString;

		LineNumber = 1;
		LookupHex.Clear();
		InitHexTable();

		ifstream TblFile = new ifstream(TableFilename);
		if (!TblFile.is_open()) // File can't be opened
		{
			return DefineConstants.TBL_OPEN_ERROR;
		}

		byte[] utfheader = new byte[4];
		// Detect UTF-8 header
		if (TblFile.peek() == 0xEF)
		{
			TblFile.read((string)utfheader, 3);
			if (utfheader[0] != 0xEF || utfheader[1] != 0xBB || utfheader[2] != 0xBF)
			{
				TblFile.seekg(ios.beg); // Seek beginning, not a UTF-8 header
			}
		}

		// Read the Table File until eof
		while (!TblFile.eof())
		{
			HexVal.clear();
			TextString.clear();
			// Read the hex number, skip whitespace, skip equal sign
			parsews(TblFile);

			TblFile.get(testchar);
			if (TblFile.eof())
				break;
			TblFile.seekg(-1, ios.cur);

			switch (testchar)
			{
			case '(':
				if (parsebookmark(TblFile))
					break;
				else
				{
					return DefineConstants.TBL_PARSE_ERROR;
				}
			case '[':
				parsescriptdump(TblFile);
				break;
			case '{':
				parsescriptinsert(TblFile);
				break;
			case '/':
				if (parseendstring(TblFile))
				{
					TblEntries++;
					break;
				}
				else
				{
					return DefineConstants.TBL_PARSE_ERROR;
				}
			case '*':
				if (parseendline(TblFile))
				{
					TblEntries++;
					break;
				}
				else
				{
					return DefineConstants.TBL_PARSE_ERROR;
				}
			case '$':
		case '!':
	case '@': // Skip line, linked/dakuten/handakuten entries not supported
				while (TblFile.get() != '\n' && !TblFile.eof());
				break;
			default:
				if (parseentry(TblFile))
				{
					break;
					TblEntries++;
				}
				else
				{
					return DefineConstants.TBL_PARSE_ERROR;
				}
			}

		} // End table reading loop

		return DefineConstants.TBL_OK;
	}

	//-----------------------------------------------------------------------------
	// EncodeStream() - Encodes text in a vector to the string tables
	//-----------------------------------------------------------------------------

	public uint EncodeStream(string scriptbuf, ref uint BadCharOffset)
	{
		TBL_STRING TblString = new TBL_STRING();
		TXT_STRING TxtString = new TXT_STRING();
		string hexstr;
		string subtextstr;
		byte i;
		uint EncodedSize = 0;
		bool bIsEndToken = false;
		SortedDictionary<string,string>.Enumerator mapit;
		uint BufOffset = 0;

		hexstr.reserve(LongestHex * 2);

		if (string.IsNullOrEmpty(scriptbuf))
		{
			return 0;
		}

		if (StringTable.Count > 0)
		{
			TBL_STRING RestoreStr = StringTable.Last.Value;
			if (string.IsNullOrEmpty(RestoreStr.EndToken)) // No end string...restore and keep adding
			{
				StringTable.RemoveLast();
				TblString.Text = RestoreStr.Text;
				TxtString = TxtStringTable.Last.Value;
				TxtStringTable.RemoveLast();
			}
		}

		while (BufOffset < scriptbuf.Length) // Translate the whole buffer
		{
			bIsEndToken = false;
			i = LongestText[(byte)scriptbuf[BufOffset]]; // Use LUT
			while (i != 0)
			{
				subtextstr = scriptbuf.Substring(BufOffset, i);
//C++ TO C# CONVERTER WARNING: The following line was determined to be a copy assignment (rather than a reference assignment) - this should be verified and a 'CopyFrom' method should be created if it does not yet exist:
//ORIGINAL LINE: mapit = LookupHex.find(subtextstr);
				mapit.CopyFrom(LookupHex.find(subtextstr));
				if (mapit == LookupHex.end()) // if the entry isn't found
				{
					i--;
					continue;
				}

//C++ TO C# CONVERTER TODO TASK: Iterators are only converted within the context of 'while' and 'for' loops:
				hexstr = mapit.second;
				TxtString.Text += subtextstr;

				// Search to see if it's an end token, if it is, add to the string table
				for (uint j = 0; j < EndTokens.Count; j++)
				{
					if (EndTokens[j] == subtextstr)
					{
						bIsEndToken = true;
						if (bAddEndToken)
						{
							AddToTable(hexstr, TblString);
						}

						TxtString.EndToken = subtextstr;
						TblString.EndToken = subtextstr;
						EncodedSize += (uint)TblString.Text.Length;
						TxtStringTable.AddLast(TxtString);
						StringTable.AddLast(TblString);
						TxtString.EndToken = "";
						TxtString.Text.clear();
						TblString.EndToken = "";
						TblString.Text.clear();
						break; // Only once
					}
				}

				if (!bIsEndToken)
				{
					AddToTable(hexstr, TblString);
				}

				BufOffset += i;
				break; // Entry is finished
			}
			if (i == 0) // no entries found
			{
				BadCharOffset = BufOffset;
				return -1;
			}
		}

		// Encode any extra data that doesn't have an EndToken
		if (!string.IsNullOrEmpty(TblString.Text))
		{
			StringTable.AddLast(TblString);
		}
		if (!string.IsNullOrEmpty(TxtString.Text))
		{
			TxtStringTable.AddLast(TxtString);
		}

		EncodedSize += (uint)TblString.Text.Length;

		scriptbuf.clear();

		return EncodedSize;
	}

	public List<TBL_ERROR> Errors = new List<TBL_ERROR>(); // Errors
	public List<TBL_BOOKMARK> Bookmarks = new List<TBL_BOOKMARK>(); // Normal bookmarks
	public List<TBL_DUMPMARK> Dumpmarks = new List<TBL_DUMPMARK>(); // Script dump bookmarks
	public List<TBL_INSMARK> Insertmarks = new List<TBL_INSMARK>(); // Insertion bookmarks
	public LinkedList<TBL_STRING> StringTable = new LinkedList<TBL_STRING>(); // (Encoded) String table
	public LinkedList<TXT_STRING> TxtStringTable = new LinkedList<TXT_STRING>(); // Text String Table
	public List<string> EndTokens = new List<string>(); // String end tokens

	public SortedDictionary<string, string> LookupHex = new SortedDictionary<string, string>(); // for looking up hex values.  (insertion)

	public uint StringCount;
	public bool bAddEndToken;

	private void InitHexTable()
	{
		string textbuf = new string(new char[16]);
		string hexbuf = new string(new char[16]);

		for (uint i = 0; i < 0x100; i++)
		{
			textbuf = string.Format("<${0:X2}>", i);
			hexbuf = string.Format("{0:X2}", i);
			LookupHex.insert(SortedDictionary<string, string>.value_type((string)textbuf, (string)hexbuf));
			// WindHex style hex codes
			textbuf = string.Format("(${0:X2})", i);
			LookupHex.insert(SortedDictionary<string, string>.value_type((string)textbuf, (string)hexbuf));
		}
		for (uint i = 0x0A; i < 0x100; i += 0x10)
		{
			for (uint j = 0; j < 6; j++)
			{
				textbuf = string.Format("<${0:x2}>", i + j);
				hexbuf = string.Format("{0:X2}", i + j);
				LookupHex.insert(SortedDictionary<string, string>.value_type((string)textbuf, (string)hexbuf));
				// WindHex style hex codes (shouldn't be necessary for lowercase, though)
				textbuf = string.Format("(${0:x2})", i);
				LookupHex.insert(SortedDictionary<string, string>.value_type((string)textbuf, (string)hexbuf));
			}
		}
	}


	//-----------------------------------------------------------------------------
	// parsebookmark() - Parses a bookmark like (8000h)Text1
	//-----------------------------------------------------------------------------

	private bool parsebookmark(ifstream file)
	{
		sbyte testch;
		string bookname;
		string hexaddress;
		uint address;

		file.get(testch); // should be '('

		while (true)
		{
			file.get(testch);
			if ((file.eof()) || (testch == 'h') || (testch == 'H') || (testch == '\n'))
				break;
			hexaddress += (char)testch;
		}

		// Convert a hex string to an unsigned long
		address = strtoul(hexaddress, null, 16);

		parsews(file);
		file.get(testch); // should be ')'
		if (testch != ')')
		{
			return false;
		}

		parsews(file);

		// Get the name
		while (true)
		{
			file.get(testch);
			if ((testch == '\n') || file.eof())
				break;
			bookname += (char)testch;
		}

		TBL_BOOKMARK bookmark = new TBL_BOOKMARK();
		bookmark.address = address;
		bookmark.description = bookname;

		Bookmarks.Add(bookmark);

		return true;
	}

	//-----------------------------------------------------------------------------
	// parseendline() - parses a break line table value: ex, *FE
	// You can also define messages like *FE=<End Text>
	//-----------------------------------------------------------------------------

	private bool parseendline(ifstream file)
	{
		sbyte testch;
		string hexstr;
		string textstr;

		file.get(testch); // the *
		parsews(file);

		// Get the hex
		while (true)
		{
			file.get(testch);
			if ((testch == '\n') || file.eof() || (testch == '='))
				break;
			hexstr += (char)testch;
		}

		if (testch != '=') // normal entry
		{
			// Add to the map
			LookupHex[DefEndString] = hexstr;
		}
		else
		{
			// Get what the string is
			while (true)
			{
				file.get(testch);
				if ((testch == '\n') || file.eof())
					break;
				textstr += (char)testch;
			}

			// Add custom message to the map
			LookupHex[textstr] = hexstr;
		}

		return true;
	}

	//-----------------------------------------------------------------------------
	// parseendstring() - parses a string break table value
	// Only ones like /FF=<end>
	// and /<end> (gives a blank string value)
	//-----------------------------------------------------------------------------

	private bool parseendstring(ifstream file)
	{
		sbyte testch;
		string hexstr;
		string textstr;

		file.get(testch); // the /
		parsews(file);

		// Get the first part
		while (true)
		{
			file.get(testch);
			if ((testch == '\n') || file.eof() || (testch == '='))
				break;
			hexstr += (char)testch;
		}

		if (testch == '\n' || file.eof()) // Must be a blank value string (/<end>)
		{
			uint Pos = hexstr.IndexOfAny((Convert.ToString("0123456789ABCDEF")).ToCharArray());
			if (Pos == 0)
			{
				return false;
			}
			textstr = hexstr;
			hexstr.clear();
		}
		else if (testch == '=') // Must be a /FF=<end> type string
		{
			while (true)
			{
				file.get(testch);
				if ((testch == '\n') || file.eof())
					break;
				textstr += (char)testch;
			}
		}
		else
		{
			return false;
		}

		// Add custom string to the map
		LookupHex[textstr] = hexstr;
		EndTokens.Add(textstr);

		return true;
	}

	//-----------------------------------------------------------------------------
	// parseentry() - parses a hex = text line
	//-----------------------------------------------------------------------------

	private bool parseentry(ifstream file)
	{
		sbyte testch;
		string Hex;
		string Text;

		// get the hex
		while (true)
		{
			file.get(testch);
			if ((testch == DefineConstants.SPACE) || (testch == '='))
				break;
			else
			{
				Hex += (char)testch;
			}
		}

		// get the equal sign
		if (testch != '=')
		{
			parsews(file);
			file.get(testch);
			if (testch != '=')
			{
				return false; // bad formatting
			}
		}

		// get the value
		file.get(testch);
		while (!file.eof() && testch != '\n')
		{
			Text += (char)testch;
			file.get(testch);
		}

		// Hex entries are strings, so divide the length by two to get the bytes
		if ((Hex.Length & 1) != 0) // Not a 8n bit hex number
		{
			Hex = Hex.Insert(0, "0");
		}
		if ((Hex.Length >> 1) > LongestHex)
		{
			LongestHex = ((uint)Hex.Length / 2);
		}

		// Get the longest text string
		if (Text.Length > LongestText[(byte)Text[0]])
		{
			LongestText[(byte)Text[0]] = (int)Text.Length;
		}

		LookupHex.insert(SortedDictionary<string,string>.value_type(Text, Hex));

		return true;
	}

	//-----------------------------------------------------------------------------
	// parsescriptinsert() - Parses script insert bookmarks
	//                       ex - {8000h-TextDump.txt}Block-e 1
	//-----------------------------------------------------------------------------

	private bool parsescriptinsert(ifstream file)
	{
		sbyte testch;
		int HexAddress;
		string HexOff;
		string FileName;
		string Description;
		TBL_INSMARK insertmark = new TBL_INSMARK();

		file.get(testch); // {

		// Get the hex offset
		while (true)
		{
			file.get(testch);
			if ((file.eof()) || (testch == '-') || (testch == '\n'))
				break;
			HexOff += (char)testch;
		}

		HexAddress = strtoul(HexOff, null, 16);
		parsews(file);

		// Get the filename
		while (true)
		{
			file.get(testch);
			if ((file.eof()) || (testch == ')') || (testch == '\n'))
				break;
			HexOff += (char)testch;
		}

		parsews(file);

		// Get the description
		while (true)
		{
			file.get(testch);
			if ((testch == '\n') || file.eof())
				break;
			Description += (char)testch;
		}

		insertmark.address = HexAddress;
		insertmark.filename = FileName;
		insertmark.description = Description;

		Insertmarks.Add(insertmark);

		return 1;
	}

	//-----------------------------------------------------------------------------
	// parsescriptdump() - Parses a script dump entry, like [8000h-8450h]Block 1
	//-----------------------------------------------------------------------------

	private bool parsescriptdump(ifstream file)
	{
		sbyte testch;
		uint HexAddr1;
		uint HexAddr2;
		string HexOff1;
		string HexOff2;
		string Description;
		TBL_DUMPMARK dumpmark = new TBL_DUMPMARK();

		file.get(testch); // the '['

		// The first hex entry
		while (true)
		{
			file.get(testch);
			if ((file.eof()) || (testch == '-') || (testch == '\n'))
				break;
			HexOff1 += (char)testch;
		}

		HexAddr1 = strtoul(HexOff1, null, 16);
		parsews(file);

		// The second hex entry
		while (true)
		{
			file.get(testch);
			if ((file.eof()) || (testch == ']') || (testch == '\n'))
				break;
			HexOff2 += (char)testch;
		}

		HexAddr2 = strtoul(HexOff2, null, 16);
		parsews(file);

		// The name of the scriptdump
		while (true)
		{
			file.get(testch);
			if ((testch == '\n') || file.eof())
				break;
			Description += (char)testch;
		}

		if (HexAddr1 <= HexAddr2)
		{
			dumpmark.StartAddress = HexAddr1;
			dumpmark.EndAddress = HexAddr2;
		}
		else
		{
			dumpmark.StartAddress = HexAddr2;
			dumpmark.EndAddress = HexAddr1;
		}

		dumpmark.description = Description;

		Dumpmarks.Add(dumpmark);

		return 1;
	}

	//-----------------------------------------------------------------------------
	// parsews() - Eats all blanks and eoln's until a valid character or eof
	//-----------------------------------------------------------------------------

	private void parsews(ifstream file)
	{
		sbyte testch;
		do
		{
			file.get(testch);
			if (testch == '\n')
			{
				LineNumber++;
			}
		}while (((testch == DefineConstants.SPACE) || (testch == '\n')) && (!file.eof()));

		if ((!file.eof()) || (testch != '\n'))
		{
			file.seekg(-1, ios.cur);
		}
	}


	//-----------------------------------------------------------------------------
	// GetHexValue() - Returns a Hex value from a Text string from the table
	//-----------------------------------------------------------------------------

	private string GetHexValue(string Textstring)
	{
		return (LookupHex.find(Textstring)).second;
	}

	private void AddToTable(string Hexstring, TBL_STRING TblStr)
	{
		for (uint k = 0; k < Hexstring.Length; k += 2)
		{
			TblStr.Text += (GlobalMembersTable.HexToDec(Hexstring[k + 1]) | (GlobalMembersTable.HexToDec(Hexstring[k]) << 4));
		}
	}

	private string DefEndLine;
	private string DefEndString;
	private uint LineNumber; // The line number that the library is reading
	private uint TblEntries; // The number of table entries
	private uint LongestHex; // The longest hex entry, in bytes

	private uint[] LongestText = new uint[256];
}


internal static partial class DefineConstants
{
	public const int TBL_OK = 0x00; // Success
	public const int TBL_OPEN_ERROR = 0x01; // Cannot open the Table properly
	public const int TBL_PARSE_ERROR = 0x02; // Cannot parse how the Table is typed
	public const int NO_MATCHING_ENTRY = 0x10; // There was an entry that cannot be matched in the table
	public const int SPACE = 0x20;
}