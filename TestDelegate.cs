namespace PipeCall;

// Define the interface for our process delegate
public abstract class MyProcessDelegate : ProcessDelegate
{
    protected MyProcessDelegate() { }  // Protected constructor
        
    public abstract int Add(int a, int b);
    public abstract string Concatenate(string[] strings);
    public abstract TestObject ProcessObject(int id, string name);
        
    // Additional test methods
    public abstract string[] ProcessNullArray(string[] arr);
    public abstract string ProcessNullString(string str);
    public abstract TestObject[] ProcessObjectArray(TestObject[] objects);
    public abstract TestStruct ProcessStruct(TestStruct data);

    // New primitive type test methods
    public abstract bool ProcessBoolean(bool value);
    public abstract byte ProcessByte(byte value);
    public abstract sbyte ProcessSByte(sbyte value);
    public abstract char ProcessChar(char value);
    public abstract short ProcessInt16(short value);
    public abstract ushort ProcessUInt16(ushort value);
    public abstract int ProcessInt32(int value);
    public abstract uint ProcessUInt32(uint value);
    public abstract long ProcessInt64(long value);
    public abstract ulong ProcessUInt64(ulong value);
    public abstract float ProcessSingle(float value);
    public abstract double ProcessDouble(double value);
    public abstract decimal ProcessDecimal(decimal value);
    public abstract AllPrimitivesStruct ProcessAllPrimitives(AllPrimitivesStruct data);
}

public struct TestObject
{
    public int Id;
    public string Name;
}

public struct TestStruct
{
    public int IntValue;
    public string StringValue;
    public float FloatValue;
}

public struct PrimitiveStruct
{
    public bool BoolValue;
    public byte ByteValue;
    public char CharValue;
    public short ShortValue;
    public long LongValue;
    public double DoubleValue;
}

// Add new struct to test all primitives
public struct AllPrimitivesStruct
{
    public bool BoolValue;
    public byte ByteValue;
    public sbyte SByteValue;
    public char CharValue;
    public short ShortValue;
    public ushort UShortValue;
    public int IntValue;
    public uint UIntValue;
    public long LongValue;
    public ulong ULongValue;
    public float FloatValue;
    public double DoubleValue;
    public decimal DecimalValue;
}