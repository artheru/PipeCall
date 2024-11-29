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