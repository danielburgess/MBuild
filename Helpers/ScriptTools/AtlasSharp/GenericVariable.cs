using System.Collections.Generic;



public class GenericVariable
{
	public GenericVariable()
	{
		DataType = P_INVALID;
		DataPointer = null;
	}
	public GenericVariable(object Data, uint Type)
	{
		DataType = Type;
		DataPointer = Data;
	}
	public void Dispose()
	{
		Free();
	}

	public uint GetType()
	{
		return DataType;
	}
	public object GetData()
	{
		return DataPointer;
	}
	public void SetData(object Data, uint Type)
	{
		Free();
		DataType = Type;
		DataPointer = Data;
	}
//C++ TO C# CONVERTER TODO TASK: The implementation of the following method could not be found:
//	void SetData(object Data);

	private bool Free()
	{
		if (DataPointer == null)
		{
			return true;
		}
		switch (DataType)
		{
		case P_INVALID:
			break;
		case P_STRING:
			delete(string)DataPointer;
			break;
		case P_NUMBER:
			delete(long)DataPointer;
			break;
		case P_DOUBLE:
			delete(double)DataPointer;
			break;
		case P_TABLE:
			delete(Table)DataPointer;
			break;
		case P_POINTERTABLE:
			delete(PointerTable)DataPointer;
			break;
		case P_POINTERLIST:
			delete(PointerList)DataPointer;
			break;
		case P_CUSTOMPOINTER:
			delete(CustomPointer)DataPointer;
			break;
		case P_EXTENSION:
			delete(AtlasExtension)DataPointer;
			break;
		default:
			return false;
		}

		return true;
	}

	private object DataPointer;
	private uint DataType;
}


public class VariableMap
{
	public VariableMap()
	{
	}
	public void Dispose()
	{
		for (VarMapIt = VarMap.GetEnumerator(); VarMapIt.MoveNext();)
		{
			VarMapIt.Current.Value = null;
		}
	}

	public bool AddVar(string Identifier, object Data, uint Type)
	{
//C++ TO C# CONVERTER WARNING: The following line was determined to be a copy assignment (rather than a reference assignment) - this should be verified and a 'CopyFrom' method should be created if it does not yet exist:
//ORIGINAL LINE: VarMapIt = VarMap.find(string(Identifier));
		VarMapIt.CopyFrom(VarMap.find((string)Identifier));
//C++ TO C# CONVERTER TODO TASK: Iterators are only converted within the context of 'while' and 'for' loops:
		if (VarMapIt != VarMap.end()) // Already a variable under that Identifier
		{
			return false;
		}

		GenericVariable CVar = new GenericVariable();
		CVar.SetData(Data, Type);
		VarMap[Identifier] = CVar;
		return true;
	}
	public bool Exists(string Identifier, uint Type)
	{
//C++ TO C# CONVERTER WARNING: The following line was determined to be a copy assignment (rather than a reference assignment) - this should be verified and a 'CopyFrom' method should be created if it does not yet exist:
//ORIGINAL LINE: VarMapIt = VarMap.find(Identifier);
		VarMapIt.CopyFrom(VarMap.find(Identifier));
		if (VarMapIt == VarMap.end()) // Identifier not found
		{
			return false;
		}
		if (VarMap[(string)Identifier].GetType() != Type)
		{
			return false;
		}
		return true;
	}
	public bool Exists(string Identifier)
	{
//C++ TO C# CONVERTER WARNING: The following line was determined to be a copy assignment (rather than a reference assignment) - this should be verified and a 'CopyFrom' method should be created if it does not yet exist:
//ORIGINAL LINE: VarMapIt = VarMap.find(Identifier);
		VarMapIt.CopyFrom(VarMap.find(Identifier));
		if (VarMapIt == VarMap.end()) // Not found
		{
			return false;
		}
		return true;
	}
	public GenericVariable GetVar(string Identifier)
	{
//C++ TO C# CONVERTER WARNING: The following line was determined to be a copy assignment (rather than a reference assignment) - this should be verified and a 'CopyFrom' method should be created if it does not yet exist:
//ORIGINAL LINE: VarMapIt = VarMap.find(Identifier);
		VarMapIt.CopyFrom(VarMap.find(Identifier));
		if (VarMapIt == VarMap.end())
		{
			return null;
		}
		else
		{
//C++ TO C# CONVERTER TODO TASK: Iterators are only converted within the context of 'while' and 'for' loops:
			return VarMapIt.second;
		}
	}
	public void SetVarData(string Identifier, object Data, uint Type)
	{
		VarMapIt = VarMap.lower_bound(Identifier);
//C++ TO C# CONVERTER TODO TASK: Iterators are only converted within the context of 'while' and 'for' loops:
		if (VarMapIt != VarMap.end() && !(VarMap.key_comp()(Identifier, VarMapIt.first)))
		{
//C++ TO C# CONVERTER TODO TASK: Iterators are only converted within the context of 'while' and 'for' loops:
			(VarMapIt.second).SetData(Data, Type);
		}
		else // Add variable
		{
			VarMap.insert(VarMapIt, VariableMapValue(Identifier, new GenericVariable(Data, Type)));
		}
	}
	public void SetVar(string Identifier, GenericVariable Var)
	{
		VarMapIt = VarMap.lower_bound(Identifier);
//C++ TO C# CONVERTER TODO TASK: Iterators are only converted within the context of 'while' and 'for' loops:
		if (VarMapIt != VarMap.end() && !(VarMap.key_comp()(Identifier, VarMapIt.first)))
		{
//C++ TO C# CONVERTER TODO TASK: Iterators are only converted within the context of 'while' and 'for' loops:
			if (VarMapIt.second != null)
				VarMapIt.second.Dispose();
//C++ TO C# CONVERTER TODO TASK: Iterators are only converted within the context of 'while' and 'for' loops:
//C++ TO C# CONVERTER WARNING: The following line was determined to be a copy assignment (rather than a reference assignment) - this should be verified and a 'CopyFrom' method should be created if it does not yet exist:
//ORIGINAL LINE: VarMapIt->second = Var;
			VarMapIt.second.CopyFrom(Var);
		}
		else // Add variable
		{
			VarMap.insert(VarMapIt, VariableMapValue(Identifier, Var));
		}
	}
	public object GetData(string Identifier)
	{
		if (!Exists(Identifier))
		{
			return null;
		}
		return VarMap[Identifier].GetData();
	}
	public uint GetVarType(string Identifier)
	{
		if (!Exists(Identifier))
		{
			return P_INVALID;
		}
		return VarMap[(string)Identifier].GetType();
	}

	private SortedDictionary<string,GenericVariable> VarMap = new SortedDictionary<string,GenericVariable>(); // Maps strings to variables
	private SortedDictionary<string,GenericVariable>.Enumerator VarMapIt; // Iterator for the map
}


internal static partial class DefineConstants
{
	public const int TBL_OK = 0x00; // Success
	public const int TBL_OPEN_ERROR = 0x01; // Cannot open the Table properly
	public const int TBL_PARSE_ERROR = 0x02; // Cannot parse how the Table is typed
	public const int NO_MATCHING_ENTRY = 0x10; // There was an entry that cannot be matched in the table
	public const int SPACE = 0x20;
}