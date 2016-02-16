using System.Collections.Generic;

public static class GlobalMembersPointer
{

	// MachineAddresses- The type of addressing the machine uses
	internal const uint MA_INVALID = 0;
	internal const uint LINEAR = 1;
	internal const uint LOROM00 = 2;
	internal const uint LOROM80 = 3;
	internal const uint HIROM = 4;
	internal const uint GB = 5;

	internal const uint AddressTypeCount = 6;
	internal string[] AddressTypes = {"INVALID", "LINEAR", "LOROM00", "LOROM80", "HIROM", "GB"};
}

public class Pointer
{
	public Pointer()
	{
		AddressType = GlobalMembersPointer.LINEAR;
		HeaderSize = 0;
	}
	public void Dispose()
	{
	}
	public bool SetAddressType(string Type)
	{
		for (int i = 0; i < GlobalMembersPointer.AddressTypeCount; i++)
		{
			if (Type == GlobalMembersPointer.AddressTypes[i])
			{
				AddressType = i;
				return true;
			}
		}

		return false;
	}
	public bool SetAddressType(uint Type)
	{
		if (Type < GlobalMembersPointer.AddressTypeCount)
		{
			AddressType = Type;
			return true;
		}
		else
		{
			return false;
		}
	}
	public void SetHeaderSize(uint Size)
	{
		HeaderSize = Size;
	}

	// Pointer writing functions

	// #W16(param) - Working

//C++ TO C# CONVERTER WARNING: 'const' methods are not available in C#:
//ORIGINAL LINE: ushort Get16BitPointer(const uint ScriptPos) const
	public ushort Get16BitPointer(uint ScriptPos)
	{
		return GetAddress(ScriptPos) & 0xFFFF;
	}

	// #W24(param) - Working

//C++ TO C# CONVERTER WARNING: 'const' methods are not available in C#:
//ORIGINAL LINE: uint Get24BitPointer(const uint ScriptPos) const
	public uint Get24BitPointer(uint ScriptPos)
	{
		return GetAddress(ScriptPos) & 0xFFFFFF;
	}

	// #W32 - Working

//C++ TO C# CONVERTER WARNING: 'const' methods are not available in C#:
//ORIGINAL LINE: uint Get32BitPointer(const uint ScriptPos) const
	public uint Get32BitPointer(uint ScriptPos)
	{
		return GetAddress(ScriptPos);
	}


	// #WLB(param) - Working

//C++ TO C# CONVERTER WARNING: 'const' methods are not available in C#:
//ORIGINAL LINE: byte GetLowByte(const uint ScriptPos) const
	public byte GetLowByte(uint ScriptPos)
	{
		return GetAddress(ScriptPos) & 0xFF;
	}

	// #WHB(param) - Working

//C++ TO C# CONVERTER WARNING: 'const' methods are not available in C#:
//ORIGINAL LINE: byte GetHighByte(const uint ScriptPos) const
	public byte GetHighByte(uint ScriptPos)
	{
		return (GetAddress(ScriptPos) & 0xFF00) >> 8;
	}

	// #WBB(param) - Working

//C++ TO C# CONVERTER WARNING: 'const' methods are not available in C#:
//ORIGINAL LINE: byte GetBankByte(const uint ScriptPos) const
	public byte GetBankByte(uint ScriptPos)
	{
		return (GetAddress(ScriptPos) & 0xFF0000) >> 16;
	}
//C++ TO C# CONVERTER WARNING: 'const' methods are not available in C#:
//ORIGINAL LINE: byte GetUpperByte(const uint ScriptPos) const
	public byte GetUpperByte(uint ScriptPos)
	{
		return (GetAddress(ScriptPos) & 0xFF000000) >> 24;
	}

	// #WHW (Write High Word) - Working

//C++ TO C# CONVERTER WARNING: 'const' methods are not available in C#:
//ORIGINAL LINE: uint GetHighWord(const uint ScriptPos) const
	public uint GetHighWord(uint ScriptPos)
	{
		return ((GetAddress(ScriptPos) & 0xFFFF0000) >> 16);
	}

	protected uint AddressType;
	protected uint HeaderSize;
//C++ TO C# CONVERTER WARNING: 'const' methods are not available in C#:
//ORIGINAL LINE: virtual uint GetAddress(const uint Address) const
	protected virtual uint GetAddress(uint Address)
	{
		return GetMachineAddress(Address);
	}
//C++ TO C# CONVERTER WARNING: 'const' methods are not available in C#:
//ORIGINAL LINE: uint GetMachineAddress(uint Address) const
	protected uint GetMachineAddress(uint Address)
	{
		Address -= HeaderSize;

		switch (AddressType)
		{
		case GlobalMembersPointer.LINEAR:
			return Address;
		case GlobalMembersPointer.LOROM00:
			return GetLoROMAddress(Address);
		case GlobalMembersPointer.LOROM80:
			return GetLoROMAddress(Address) + 0x800000;
		case GlobalMembersPointer.HIROM:
			return GetHiROMAddress(Address);
		case GlobalMembersPointer.GB:
			return GetGBAddress(Address);
		default:
			return Address; // Error handling
		}
	}

	// Machine Address translation functions
//C++ TO C# CONVERTER WARNING: 'const' methods are not available in C#:
//ORIGINAL LINE: uint GetLoROMAddress(uint Offset) const
	private uint GetLoROMAddress(uint Offset)
	{
		sbyte bankbyte = (sbyte)((Offset & 0xFF0000) >> 16);
		ushort Word = (ushort)(Offset & 0xFFFF);
		uint Address = 0;

		if (Word >= 0x8000)
		{
			Address = bankbyte * 0x20000 + 0x10000 + Word;
		}
		else
		{
			Address = bankbyte * 0x20000 + Word + 0x8000;
		}

		return Address;
	}
//C++ TO C# CONVERTER WARNING: 'const' methods are not available in C#:
//ORIGINAL LINE: uint GetHiROMAddress(uint Offset) const
	private uint GetHiROMAddress(uint Offset)
	{
		uint Address = 0;

		Address = Offset + 0xC00000;

		return Address;
	}
//C++ TO C# CONVERTER WARNING: 'const' methods are not available in C#:
//ORIGINAL LINE: uint GetGBAddress(uint Offset) const
	private uint GetGBAddress(uint Offset)
	{
		uint Address = 0;
		ushort Bank = 0;
		ushort Word = 0;

		Bank = Offset / 0x4000;
		Word = Offset % ((Bank + 1) * 0x4000);

		Address = Bank * 0x10000 + Word;

		return Address;
	}
}

public class CustomPointer : Pointer
{

	//--------------------------- Custom Pointer ----------------------------------
	//
	//
	//
	public bool Init(long Offsetting, uint Size, uint HeaderSize)
	{
		this.Offsetting = Offsetting;
		SetHeaderSize(HeaderSize);
		switch (Size)
		{
		case 8:
	case 16:
	case 24:
	case 32:
			this.Size = Size;
			break;
		default:
			return false;
		}
		return true;
	}
	public uint GetSize()
	{
		return Size;
	}
//C++ TO C# CONVERTER WARNING: 'const' methods are not available in C#:
//ORIGINAL LINE: uint GetAddress(const uint Address) const
	public new uint GetAddress(uint Address)
	{
		uint Val;
		Val = (uint)((long)GetMachineAddress(Address) - Offsetting);
		switch (Size)
		{
		case 8:
			return Val & 0xFF;
		case 16:
			return Val & 0xFFFF;
		case 24:
			return Val & 0xFFFFFF;
		case 32:
			return Val;
		default:
	//C++ TO C# CONVERTER TODO TASK: There is no direct equivalent in C# to the following C++ macro:
			Logger.BugReport(__LINE__, __FILE__, "Bad size in CustomPointer::GetAddress");
			return -1;
		}
	}
	private long Offsetting;
	private uint Size;
}

public class EmbeddedPointer : Pointer
{

	//--------------------------- Embedded Pointer --------------------------------
	//
	//
	//
	public EmbeddedPointer()
	{
		TextPos = -1;
		PointerPos = -1;
		Size = 0;
	}
	public new void Dispose()
	{
		base.Dispose();
	}

	public bool SetTextPosition(uint Address)
	{
		TextPos = Address;
		if (PointerPos != -1)
		{
			return true;
		}
		else
		{
			return false;
		}
	}
	public bool SetPointerPosition(uint Address)
	{
		PointerPos = Address;
		if (TextPos != -1)
		{
			return true; // Return true if pointer is ready to write
		}
		else
		{
			return false;
		}
	}
	public void SetSize(uint size)
	{
		Size = size;
	}
	public void SetOffsetting(long Offsetting)
	{
		this.Offsetting = Offsetting;
	}

//C++ TO C# CONVERTER WARNING: 'const' methods are not available in C#:
//ORIGINAL LINE: uint GetTextPosition() const
	public uint GetTextPosition()
	{
		return TextPos;
	}
//C++ TO C# CONVERTER WARNING: 'const' methods are not available in C#:
//ORIGINAL LINE: uint GetPointer() const
	public uint GetPointer()
	{
		uint Val = (uint)(GetAddress(TextPos) - Offsetting);
		switch (Size)
		{
		case 8:
			return Val & 0xFF;
		case 16:
			return Val & 0xFFFF;
		case 24:
			return Val & 0xFFFFFF;
		case 32:
			return Val & 0xFFFFFFFF;
		default:
	//C++ TO C# CONVERTER TODO TASK: There is no direct equivalent in C# to the following C++ macro:
			Logger.BugReport(__LINE__, __FILE__, "Bad embedded pointer size %d in EmbeddedPointer::GetTextPosition", Size);
			return 0;
		}
	}
//C++ TO C# CONVERTER WARNING: 'const' methods are not available in C#:
//ORIGINAL LINE: uint GetPointerPosition() const
	public uint GetPointerPosition()
	{
		return PointerPos;
	}
//C++ TO C# CONVERTER WARNING: 'const' methods are not available in C#:
//ORIGINAL LINE: uint GetSize() const
	public uint GetSize()
	{
		return Size;
	}
	private long Offsetting;
	private uint TextPos;
	private uint PointerPos;
	private uint Size;
}


public class EmbeddedPointerHandler
{

	//------------------------ Embedded Pointer Handler ---------------------------
	//
	//
	//
	public EmbeddedPointerHandler()
	{
		PtrSize = 0;
		HdrSize = 0;
		Offsetting = 0;
	}
	public void Dispose()
	{
	}

	public void SetListSize(int Size)
	{
		PtrList.Capacity = Size;
		if ((int)PtrList.Count < Size)
		{
			int j = Size - (int)PtrList.Count;

			EmbeddedPointer elem = new EmbeddedPointer();
			elem.SetAddressType(AddressType);
			elem.SetSize(PtrSize);
			elem.SetHeaderSize(HdrSize);
			elem.SetOffsetting(Offsetting);
			elem.SetPointerPosition(-1);
			elem.SetTextPosition(-1);

			for (int i = 0; i < j; i++)
			{
				PtrList.Add(elem);
			}
		}
	}
	public int GetListSize()
	{
		return (int)PtrList.Count;
	}
	public bool GetPointerState(uint PointerNum, ref uint TextPos, ref uint PointerPos)
	{
		if (PtrList.Count < PointerNum)
		{
			TextPos = -1;
			PointerPos = -1;
			return false;
		}

		TextPos = GetTextPosition(PointerNum);
		PointerPos = GetPointerPosition(PointerNum);

		if (TextPos == -1 || PointerPos == -1)
		{
			return false;
		}
		else
		{
			return true;
		}
	}
	public bool SetType(string AddressString, long Offsetting, uint PointerSize)
	{
		this.Offsetting = Offsetting;
		switch (PointerSize)
		{
		case 8:
	case 16:
	case 24:
	case 32:
			PtrSize = PointerSize;
			break;
		default: // Bad size
			return false;
		}
		return SetAddressType(AddressString);
	}
	public uint GetDefaultSize()
	{
		return PtrSize;
	}
	public bool SetTextPosition(uint PointerNum, uint TextPos)
	{
		uint i = PointerNum;
		if (PtrList.Count < i)
		{
			return false;
		}

		// If still with default allocation
		if (PtrList[i].GetTextPosition() == -1 && PtrList[i].GetPointerPosition() == -1)
		{
			PtrList[i].SetAddressType(AddressType);
			PtrList[i].SetSize(PtrSize);
			PtrList[i].SetHeaderSize(HdrSize);
			PtrList[i].SetOffsetting(Offsetting);
		}

		return PtrList[i].SetTextPosition(TextPos);
	}
	public bool SetPointerPosition(uint PointerNum, uint PointerPos)
	{
		uint i = PointerNum;
		if (PtrList.Count < i)
		{
			return false;
		}

		// If still with default allocation
		if (PtrList[i].GetTextPosition() == -1 && PtrList[i].GetPointerPosition() == -1)
		{
			PtrList[i].SetAddressType(AddressType);
			PtrList[i].SetSize(PtrSize);
			PtrList[i].SetHeaderSize(HdrSize);
			PtrList[i].SetOffsetting(Offsetting);
		}

		return PtrList[i].SetPointerPosition(PointerPos);
	}
	public void SetHeaderSize(uint HeaderSize)
	{
		HdrSize = HeaderSize;
	}
	public bool SetAddressType(string Type)
	{
		for (int i = 0; i < GlobalMembersPointer.AddressTypeCount; i++)
		{
			if (Type == GlobalMembersPointer.AddressTypes[i])
			{
				AddressType = i;
				return true;
			}
		}

		return false;
	}

	public uint GetTextPosition(uint PointerNum)
	{
		if (PtrList.Count < PointerNum)
		{
			return -1;
		}

		return PtrList[PointerNum].GetTextPosition();
	}
	public uint GetPointerPosition(uint PointerNum)
	{
		if (PtrList.Count < PointerNum)
		{
			return -1;
		}

		return PtrList[PointerNum].GetPointerPosition();
	}
	public uint GetPointerValue(uint PointerNum)
	{
		if (PtrList.Count < PointerNum)
		{
			return -1;
		}

		return PtrList[PointerNum].GetPointer();
	}
	public uint GetSize(uint PointerNum)
	{
		uint i = PointerNum;
		if (PtrList.Count < i)
		{
			return -1;
		}

		return PtrList[i].GetSize();
	}

	private List<EmbeddedPointer> PtrList = new List<EmbeddedPointer>();
	private uint AddressType;
	private long Offsetting;
	private uint PtrSize;
	private uint HdrSize;
}
