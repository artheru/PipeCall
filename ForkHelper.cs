using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace Medulla
{
    public abstract class ProcessDelegate
    {
    }

    public static class ForkHelper<T> where T : ProcessDelegate
    {
        public static T Start(string processPath)
        {
            Console.WriteLine($"[Server] Starting process: {processPath}");
            Console.WriteLine($"[Server] Creating proxy for type: {typeof(T).FullName}");
            
            var proxy = new ProcessProxy(processPath);
            var proxyType = ProxyGenerator.CreateProxyType(typeof(T), proxy);
            
            Console.WriteLine($"[Server] Proxy type created: {proxyType?.FullName ?? "NULL"}");
            
            if (proxyType == null)
                throw new Exception("Failed to create proxy type");
                
            return (T)Activator.CreateInstance(proxyType, proxy);
        }

        public class ProcessProxy
        {
            private Process process;
            private StreamWriter writer;
            private StreamReader reader;
            private BinaryWriter binaryWriter;
            private BinaryReader binaryReader;
            private AnonymousPipeServerStream pipeToClient;
            private AnonymousPipeServerStream pipeFromClient;
            private readonly object syncLock = new object();
            private int nextCallId = 1;

            public ProcessProxy(string clientPath)
            {
                // Create the pipes
                pipeToClient = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
                pipeFromClient = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);

                // Start the client process
                var startInfo = new ProcessStartInfo
                {
                    FileName = clientPath,
                    Arguments = $"{pipeToClient.GetClientHandleAsString()} {pipeFromClient.GetClientHandleAsString()}",
                    UseShellExecute = false,
                    // RedirectStandardOutput = true,
                    // RedirectStandardError = true
                };

                process = Process.Start(startInfo);
                writer = new StreamWriter(pipeToClient);
                reader = new StreamReader(pipeFromClient);
                binaryWriter = new BinaryWriter(pipeToClient, Encoding.UTF8, true);
                binaryReader = new BinaryReader(pipeFromClient, Encoding.UTF8, true);
                writer.AutoFlush = true;

                // Now that the client process has the handles, we can dispose of them
                pipeToClient.DisposeLocalCopyOfClientHandle();
                pipeFromClient.DisposeLocalCopyOfClientHandle();

                // Verify connection
                binaryWriter.Write("PING");
                var response = binaryReader.ReadString();
                if (response != "PONG")
                {
                    throw new Exception("Failed to establish connection with client");
                }
            }

            public object InvokeMethod(string methodName, object[] args, Type returnType)
            {
                if (string.IsNullOrEmpty(methodName))
                    throw new ArgumentException("Method name cannot be null or empty");

                lock (syncLock)
                {
                    var callId = nextCallId++;
                    
                    // Serialize arguments using FastSerializer
                    var serializedArgs = new byte[args.Length][];
                    for (int i = 0; i < args.Length; i++)
                    {
                        serializedArgs[i] = FastSerializer.Serialize(args[i]);
                    }

                    // Write request
                    binaryWriter.Write(callId);
                    binaryWriter.Write(methodName);
                    binaryWriter.Write(serializedArgs.Length);
                    foreach (var arg in serializedArgs)
                    {
                        binaryWriter.Write(arg.Length);
                        binaryWriter.Write(arg);
                    }
                    binaryWriter.Flush();

                    // Read response
                    var responseId = binaryReader.ReadInt32();
                    if (responseId != callId)
                        throw new Exception($"Response ID mismatch. Expected {callId}, got {responseId}");

                    var success = binaryReader.ReadBoolean();
                    if (!success)
                    {
                        var error = binaryReader.ReadString();
                        throw new Exception($"Method execution failed: {error}");
                    }

                    var resultLength = binaryReader.ReadInt32();
                    var resultBytes = binaryReader.ReadBytes(resultLength);
                    return FastSerializer.Deserialize(resultBytes, returnType);
                }
            }

            ~ProcessProxy()
            {
                Console.WriteLine("[Server] Cleaning up ProcessProxy...");
                try { process?.Kill(); } catch (Exception ex) { Console.WriteLine($"[Server] Kill error: {ex.Message}"); }
                try { writer?.Dispose(); } catch (Exception ex) { Console.WriteLine($"[Server] Writer dispose error: {ex.Message}"); }
                try { reader?.Dispose(); } catch (Exception ex) { Console.WriteLine($"[Server] Reader dispose error: {ex.Message}"); }
                try { binaryWriter?.Dispose(); } catch (Exception ex) { Console.WriteLine($"[Server] BinaryWriter dispose error: {ex.Message}"); }
                try { binaryReader?.Dispose(); } catch (Exception ex) { Console.WriteLine($"[Server] BinaryReader dispose error: {ex.Message}"); }
                try { pipeToClient?.Dispose(); } catch (Exception ex) { Console.WriteLine($"[Server] ToClient pipe dispose error: {ex.Message}"); }
                try { pipeFromClient?.Dispose(); } catch (Exception ex) { Console.WriteLine($"[Server] FromClient pipe dispose error: {ex.Message}"); }
            }
        }
    }

    public static class ForkHelper
    {
        public static void DelegateProcessMain<T>(string[] pipe_arg) where T : ProcessDelegate
        {
            if (pipe_arg.Length != 2)
                throw new ArgumentException("Expected pipe handles as arguments");

            using (var pipeFromServer = new AnonymousPipeClientStream(PipeDirection.In, pipe_arg[0]))
            using (var pipeToServer = new AnonymousPipeClientStream(PipeDirection.Out, pipe_arg[1]))
            {
                var reader = new StreamReader(pipeFromServer);
                var writer = new StreamWriter(pipeToServer);
                var binaryReader = new BinaryReader(pipeFromServer, Encoding.UTF8, true);
                var binaryWriter = new BinaryWriter(pipeToServer, Encoding.UTF8, true);
                writer.AutoFlush = true;

                Console.WriteLine("[Client] Creating delegate instance...");
                var instance = Activator.CreateInstance<T>();
                Console.WriteLine($"[Client] Instance created: {instance.GetType().FullName}");

                // Handle initial connection verification
                var ping = binaryReader.ReadString();
                if (ping == "PING")
                {
                    binaryWriter.Write("PONG");
                    Console.WriteLine("[Client] Connection verified");
                }

                while (true)
                {
                    try
                    {
                        // Console.WriteLine("[Client] Waiting for request...");

                        // Read request
                        var callId = binaryReader.ReadInt32();
                        var methodName = binaryReader.ReadString();
                        var argCount = binaryReader.ReadInt32();
                        var args = new object[argCount];

                        // Console.WriteLine($"[Client] Looking up method: {methodName}");
                        var method = typeof(T).GetMethod(methodName);
                        if (method == null)
                        {
                            throw new Exception($"Method not found: {methodName}");
                        }

                        // Read and deserialize arguments
                        var parameters = method.GetParameters();
                        for (int i = 0; i < argCount; i++)
                        {
                            var argLength = binaryReader.ReadInt32();
                            var argBytes = binaryReader.ReadBytes(argLength);
                            args[i] = FastSerializer.Deserialize(argBytes, parameters[i].ParameterType);
                        }

                        // Console.WriteLine($"[Client] Invoking method with pipe_arg: {string.Join(", ", args)}");
                        try
                        {
                            var result = method.Invoke(instance, args);
                            var resultBytes = FastSerializer.Serialize(result);

                            // Write success response
                            binaryWriter.Write(callId);
                            binaryWriter.Write(true);  // success
                            binaryWriter.Write(resultBytes.Length);
                            binaryWriter.Write(resultBytes);
                            binaryWriter.Flush();
                        }
                        catch (Exception ex)
                        {
                            // Console.WriteLine($"[Client] Method execution error: {ex}");
                            // Write error response
                            binaryWriter.Write(callId);
                            binaryWriter.Write(false);  // failure
                            binaryWriter.Write(ex.ToString());
                            binaryWriter.Flush();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Client] Communication error: {ex}");
                        break;
                    }
                }
            }
        }
    }
}
