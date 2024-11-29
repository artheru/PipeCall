using System;
using System.Diagnostics;
using System.IO;

namespace PipeCall;

class Program
{
    static void Main(string[] args)
    {
        if (Path.GetFileName(Environment.ProcessPath).ToLower() == "client.exe")
        {
            PipeCallee.DelegateProcessMain<ActualDelegate>(args);
            return;
        }

        var del = PipeCaller<MyProcessDelegate>.Start("client.exe");
            
        try
        {
            Console.WriteLine("Running functional tests...");
            if (!RunFunctionalTests(del))
            {
                Console.WriteLine("Functional tests failed!");
                return;
            }
            Console.WriteLine("All functional tests passed!");

            Console.WriteLine("\nRunning performance tests...");
            RunPerformanceTests(del);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test failed: {ex}");
        }
    }

    private static bool RunFunctionalTests(MyProcessDelegate del)
    {
        bool allPassed = true;

        // Test 1: Value types
        {
            var result = del.Add(5, 3);
            if (result != 8)
            {
                Console.WriteLine($"Add test failed: expected 8, got {result}");
                allPassed = false;
            }
            else Console.WriteLine("Test 1: PASSED");
        }

        // Test 2: String array
        {
            var result = del.Concatenate(new[] { "Hello", "World", "!" });
            if (result != "Hello World !")
            {
                Console.WriteLine($"Concatenate test failed: expected 'Hello World !', got '{result}'");
                allPassed = false;
            }
            else Console.WriteLine("Test 2: PASSED");
        }

        // Test 3: Struct with string
        {
            var result = del.ProcessObject(1, "Test");
            if (result.Id != 1 || result.Name != "Processed: Test")
            {
                Console.WriteLine($"ProcessObject test failed: expected Id=1, Name='Processed: Test', got Id={result.Id}, Name='{result.Name}'");
                allPassed = false;
            }
            else Console.WriteLine("Test 3: PASSED");
        }

        // Test 4: Null handling
        {
            var nullArrayResult = del.ProcessNullArray(null);
            if (nullArrayResult.Length != 1 || nullArrayResult[0] != "was null")
            {
                Console.WriteLine("ProcessNullArray test failed for null input");
                allPassed = false;
            }
            else Console.WriteLine("Test 4a: PASSED");

            var emptyArrayResult = del.ProcessNullArray(new string[0]);
            if (emptyArrayResult.Length != 1 || emptyArrayResult[0] != "was empty")
            {
                Console.WriteLine("ProcessNullArray test failed for empty input");
                allPassed = false;
            }
            else Console.WriteLine("Test 4b: PASSED");

            var nullStringResult = del.ProcessNullString(null);
            if (nullStringResult != "was null")
            {
                Console.WriteLine("ProcessNullString test failed for null input");
                allPassed = false;
            }
            else Console.WriteLine("Test 4c: PASSED");

            var emptyStringResult = del.ProcessNullString("");
            if (emptyStringResult != "was empty")
            {
                Console.WriteLine("ProcessNullString test failed for empty input");
                allPassed = false;
            }
            else Console.WriteLine("Test 4d: PASSED");
        }

        // Test 5: Array of structs
        {
            var input = new[]
            {
                new TestObject { Id = 1, Name = "First" },
                new TestObject { Id = 2, Name = "Second" }
            };
            var result = del.ProcessObjectArray(input);
            if (result.Length != 2 || 
                result[0].Id != 2 || result[0].Name != "Array_First" ||
                result[1].Id != 4 || result[1].Name != "Array_Second")
            {
                Console.WriteLine("ProcessObjectArray test failed");
                allPassed = false;
            }
            else Console.WriteLine("Test 5: PASSED");
        }

        // Test 6: Complex struct
        {
            var input = new TestStruct
            {
                IntValue = 42,
                StringValue = "Hello",
                FloatValue = 3.14f
            };
            var result = del.ProcessStruct(input);
            if (result.IntValue != 84 || 
                result.StringValue != "Processed_Hello" ||
                Math.Abs(result.FloatValue - 3.64f) > 0.001f)
            {
                Console.WriteLine($"ProcessStruct test failed: got IntValue={result.IntValue}, StringValue='{result.StringValue}', FloatValue={result.FloatValue}");
                allPassed = false;
            }
            else Console.WriteLine("Test 6: PASSED");
        }

        // Test 7: Null in struct
        {
            var input = new TestStruct
            {
                IntValue = 100,
                StringValue = null,
                FloatValue = 1.0f
            };
            var result = del.ProcessStruct(input);
            if (result.IntValue != 200 || 
                result.StringValue != "was null" ||
                Math.Abs(result.FloatValue - 1.5f) > 0.001f)
            {
                Console.WriteLine("ProcessStruct test failed for null string");
                allPassed = false;
            }
            else Console.WriteLine("Test 7: PASSED");
        }

        // Test 8: All primitive types
        {
            Console.Write("Testing bool... ");
            var boolResult = del.ProcessBoolean(true);
            if (boolResult != false)
            {
                Console.WriteLine("FAILED");
                allPassed = false;
            }
            else Console.WriteLine("PASSED");

            Console.Write("Testing byte... ");
            var byteResult = del.ProcessByte(123);
            if (byteResult != 246)
            {
                Console.WriteLine("FAILED");
                allPassed = false;
            }
            else Console.WriteLine("PASSED");

            Console.Write("Testing sbyte... ");
            var sbyteResult = del.ProcessSByte(-64);
            if (sbyteResult != -128)
            {
                Console.WriteLine("FAILED");
                allPassed = false;
            }
            else Console.WriteLine("PASSED");

            Console.Write("Testing char... ");
            var charResult = del.ProcessChar('A');
            if (charResult != 'B')
            {
                Console.WriteLine("FAILED");
                allPassed = false;
            }
            else Console.WriteLine("PASSED");

            Console.Write("Testing short... ");
            var shortResult = del.ProcessInt16(-12345);
            if (shortResult != -24690)
            {
                Console.WriteLine("FAILED");
                allPassed = false;
            }
            else Console.WriteLine("PASSED");

            Console.Write("Testing ushort... ");
            var ushortResult = del.ProcessUInt16(12345);
            if (ushortResult != 24690)
            {
                Console.WriteLine("FAILED");
                allPassed = false;
            }
            else Console.WriteLine("PASSED");

            Console.Write("Testing int... ");
            var intResult = del.ProcessInt32(-1234567);
            if (intResult != -2469134)
            {
                Console.WriteLine("FAILED");
                allPassed = false;
            }
            else Console.WriteLine("PASSED");

            Console.Write("Testing uint... ");
            var uintResult = del.ProcessUInt32(1234567);
            if (uintResult != 2469134)
            {
                Console.WriteLine("FAILED");
                allPassed = false;
            }
            else Console.WriteLine("PASSED");

            Console.Write("Testing long... ");
            var longResult = del.ProcessInt64(-1234567890L);
            if (longResult != -2469135780L)
            {
                Console.WriteLine("FAILED");
                allPassed = false;
            }
            else Console.WriteLine("PASSED");

            Console.Write("Testing ulong... ");
            var ulongResult = del.ProcessUInt64(1234567890UL);
            if (ulongResult != 2469135780UL)
            {
                Console.WriteLine("FAILED");
                allPassed = false;
            }
            else Console.WriteLine("PASSED");

            Console.Write("Testing float... ");
            var floatResult = del.ProcessSingle(3.14159f);
            if (Math.Abs(floatResult - 6.28318f) > 0.00001f)
            {
                Console.WriteLine("FAILED");
                allPassed = false;
            }
            else Console.WriteLine("PASSED");

            Console.Write("Testing double... ");
            var doubleResult = del.ProcessDouble(3.14159265359);
            if (Math.Abs(doubleResult - 6.28318530718) > 0.00000000001)
            {
                Console.WriteLine("FAILED");
                allPassed = false;
            }
            else Console.WriteLine("PASSED");

            // Console.Write("Testing decimal... ");
            // var decimalResult = del.ProcessDecimal(123.456m);
            // if (decimalResult != 246.912m)
            // {
            //     Console.WriteLine("FAILED");
            //     allPassed = false;
            // }
            // else Console.WriteLine("PASSED");
        }

        // Test 9: Struct with all primitives
        {
            Console.Write("Testing all primitives struct... ");
            var input = new AllPrimitivesStruct
            {
                BoolValue = true,
                ByteValue = 123,
                SByteValue = -64,
                CharValue = 'A',
                ShortValue = -12345,
                UShortValue = 12345,
                IntValue = -1234567,
                UIntValue = 1234567,
                LongValue = -1234567890L,
                ULongValue = 1234567890UL,
                FloatValue = 3.14159f,
                DoubleValue = 3.14159265359,
                DecimalValue = 123.456m
            };
            
            var result = del.ProcessAllPrimitives(input);
            if (result.BoolValue != false ||
                result.ByteValue != 246 ||
                result.SByteValue != -128 ||
                result.CharValue != 'B' ||
                result.ShortValue != -24690 ||
                result.UShortValue != 24690 ||
                result.IntValue != -2469134 ||
                result.UIntValue != 2469134 ||
                result.LongValue != -2469135780L ||
                result.ULongValue != 2469135780UL ||
                Math.Abs(result.FloatValue - 6.28318f) > 0.00001f ||
                Math.Abs(result.DoubleValue - 6.28318530718) > 0.00000000001// ||
                // result.DecimalValue != 246.912m
                )
            {
                Console.WriteLine("FAILED");
                allPassed = false;
            }
            else Console.WriteLine("PASSED");
        }

        return allPassed;
    }

    private static void RunPerformanceTests(MyProcessDelegate del)
    {
        const int warmupIterations = 1000;
        const int iterations = 10000;
        var sw = new Stopwatch();

        // Warmup
        Console.WriteLine("Warming up...");
        for (int i = 0; i < warmupIterations; i++)
        {
            del.Add(i, i + 1);
        }

        Console.WriteLine("Start...");
        // Test cases
        var testCases = new(string name, Action action)[]
        {
            ("Value type", () => del.Add(42, 43)),
            ("String array", () => del.Concatenate(new[] { "Test", "String" })),
            ("Object array", () => del.ProcessObjectArray(new[] { new TestObject { Id = 1, Name = "Test" } })),
            ("Complex struct", () => del.ProcessStruct(new TestStruct { IntValue = 42, StringValue = "Test", FloatValue = 3.14f }))
        };

        foreach (var (name, action) in testCases)
        {
            sw.Restart();
            for (int i = 0; i < iterations; i++)
            {
                action();
            }
            var elapsed = sw.ElapsedMilliseconds;
            var perOp = (double)elapsed / iterations;
            Console.WriteLine($"{name,-15} Total: {elapsed,6}ms, Per op: {perOp:F3}ms ({iterations:N0} iterations)");
        }
    }
}

// Actual implementation that runs in the client process
public class ActualDelegate : MyProcessDelegate
{
    public override int Add(int a, int b)
    {
        // Console.WriteLine($"[Client-Impl] Add({a}, {b})");
        var result = a + b;
        // Console.WriteLine($"[Client-Impl] Add result: {result}");
        return result;
    }
        
    public override string Concatenate(string[] strings)
    {
        // Console.WriteLine($"[Client-Impl] Concatenate({string.Join(", ", strings.Select(s => $"'{s}'"))})");
        var result = string.Join(" ", strings);
        // Console.WriteLine($"[Client-Impl] Concatenate result: '{result}'");
        return result;
    }
        
    public override TestObject ProcessObject(int id, string name)
    {
        // Console.WriteLine($"[Client-Impl] ProcessObject({id}, '{name}')");
        var result = new TestObject { Id = id, Name = $"Processed: {name}" };
        // Console.WriteLine($"[Client-Impl] ProcessObject result: Id={result.Id}, Name='{result.Name}'");
        return result;
    }

    public override string[] ProcessNullArray(string[] arr)
    {
        // Console.WriteLine($"[Client-Impl] ProcessNullArray({(arr == null ? "null" : $"[{string.Join(", ", arr)}]")})");
        string[] result;
        if (arr == null) result = new[] { "was null" };
        else if (arr.Length == 0) result = new[] { "was empty" };
        else result = arr;
        // Console.WriteLine($"[Client-Impl] ProcessNullArray result: [{string.Join(", ", result)}]");
        return result;
    }

    public override string ProcessNullString(string str)
    {
        // Console.WriteLine($"[Client-Impl] ProcessNullString('{str}')");
        var result = str == null ? "was null" : (str.Length == 0 ? "was empty" : str);
        // Console.WriteLine($"[Client-Impl] ProcessNullString result: '{result}'");
        return result;
    }

    public override TestObject[] ProcessObjectArray(TestObject[] objects)
    {
        // Console.WriteLine($"[Client-Impl] ProcessObjectArray({(objects == null ? "null" : objects.Length.ToString())})");
        if (objects == null)
        {
            // Console.WriteLine("[Client-Impl] ProcessObjectArray result: null");
            return null;
        }

        var result = new TestObject[objects.Length];
        for (int i = 0; i < objects.Length; i++)
        {
            result[i] = new TestObject 
            { 
                Id = objects[i].Id * 2,
                Name = $"Array_{objects[i].Name}"
            };
            // Console.WriteLine($"[Client-Impl] ProcessObjectArray result[{i}]: Id={result[i].Id}, Name='{result[i].Name}'");
        }
        return result;
    }

    public override TestStruct ProcessStruct(TestStruct data)
    {
        // Console.WriteLine($"[Client-Impl] ProcessStruct(IntValue={data.IntValue}, StringValue='{data.StringValue}', FloatValue={data.FloatValue})");
        var result = new TestStruct
        {
            IntValue = data.IntValue * 2,
            StringValue = data.StringValue == null ? "was null" : $"Processed_{data.StringValue}",
            FloatValue = data.FloatValue + 0.5f
        };
        // Console.WriteLine($"[Client-Impl] ProcessStruct result: IntValue={result.IntValue}, StringValue='{result.StringValue}', FloatValue={result.FloatValue}");
        return result;
    }

    public override bool ProcessBoolean(bool value) => !value;
    public override byte ProcessByte(byte value) => (byte)(value * 2);
    public override char ProcessChar(char value) => (char)(value + 1);
    public override short ProcessInt16(short value) => (short)(value * 2);
    public override long ProcessInt64(long value) => value * 2;
    public override double ProcessDouble(double value) => value * 2;

    public override sbyte ProcessSByte(sbyte value) => (sbyte)(value * 2);
    public override ushort ProcessUInt16(ushort value) => (ushort)(value * 2);
    public override int ProcessInt32(int value) => value * 2;
    public override uint ProcessUInt32(uint value) => value * 2;
    public override ulong ProcessUInt64(ulong value) => value * 2;
    public override float ProcessSingle(float value) => value * 2;
    public override decimal ProcessDecimal(decimal value) => value * 2;

    public override AllPrimitivesStruct ProcessAllPrimitives(AllPrimitivesStruct data)
    {
        return new AllPrimitivesStruct
        {
            BoolValue = !data.BoolValue,
            ByteValue = (byte)(data.ByteValue * 2),
            SByteValue = (sbyte)(data.SByteValue * 2),
            CharValue = (char)(data.CharValue + 1),
            ShortValue = (short)(data.ShortValue * 2),
            UShortValue = (ushort)(data.UShortValue * 2),
            IntValue = data.IntValue * 2,
            UIntValue = data.UIntValue * 2,
            LongValue = data.LongValue * 2,
            ULongValue = data.ULongValue * 2,
            FloatValue = data.FloatValue * 2,
            DoubleValue = data.DoubleValue * 2,
            DecimalValue = data.DecimalValue * 2
        };
    }
}