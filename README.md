# PipeCall - Cross-Process Method Invocation Library

PipeCall is an AI-generated lightweight, high-performance library for executing .NET methods across process boundaries. It uses anonymous pipes for communication and a custom binary serializer for efficient data transfer. 

## Features

- Execute methods in a separate process with type safety
- Efficient binary serialization
- Support for primitive types, strings, arrays, and structs
- No external dependencies
- Clean process cleanup and resource management

## Example

Here's a minimal example showing how to use PipeCall:

```csharp
// Define your interface/abstract class
public abstract class MyDelegate : ProcessDelegate
{
    protected MyDelegate() { }  // Protected constructor
    
    public abstract int Add(int a, int b);
    public abstract string Concatenate(string[] strings);
}

// Implement the actual delegate
public class ActualDelegate : MyDelegate
{
    public override int Add(int a, int b)
    {
        Console.WriteLine($"Adding {a} + {b}");
        return a + b;
    }

    public override string Concatenate(string[] strings)
    {
        var result = string.Join(" ", strings);
        Console.WriteLine($"Concatenating: {result}");
        return result;
    }
}

// Use it in your main program
class Program
{
    static void Main()
    {
        // Start the delegate in a new process
        var del = ForkHelper<MyDelegate>.Start("client.exe");

        // Call methods as if they were local
        int sum = del.Add(5, 3);
        Console.WriteLine($"Result: {sum}");  // Output: Result: 8

        string concat = del.Concatenate(new[] { "Hello", "World" });
        Console.WriteLine($"Result: {concat}");  // Output: Result: Hello World
    }
}
```

## How It Works

1. You define an abstract class inheriting from `ProcessDelegate` with your desired methods
2. Implement the actual delegate class with your business logic
3. Use `ForkHelper<T>.Start()` to create a proxy in a new process
4. Call methods on the proxy as if they were local - PipeCall handles all the IPC

## Features in Detail

- **Process Isolation**: Methods execute in a separate process, providing isolation and stability
- **Efficient Serialization**: Custom binary serializer for high-performance data transfer
- **Type Safety**: Full compile-time type checking
- **Resource Management**: Automatic cleanup of processes and handles
- **Error Handling**: Proper propagation of exceptions across process boundaries

## Supported Types

- Primitives (int, float, etc.)
- Strings
- Arrays
- Structs
- Null values
- Complex combinations (arrays of structs, etc.)

## Best Practices

1. Keep method arguments and return values serializable
2. Prefer simple data types for better performance
3. Handle exceptions appropriately
4. Dispose of delegates when done (they'll clean up the child process)

## Why "PipeCall"?

PipeCall is named after the mechanism it uses—anonymous pipes—to facilitate communication between processes. Just as pipes transport water, PipeCall transports method calls and data, enabling seamless cross-process execution.

## License

MIT License - Feel free to use in your projects! 