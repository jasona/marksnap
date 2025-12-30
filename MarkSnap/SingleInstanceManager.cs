using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace MarkSnap
{
    public class SingleInstanceManager : IDisposable
    {
        private const string MutexName = "MarkSnap_SingleInstance_Mutex";
        private const string PipeName = "MarkSnap_Pipe";

        private Mutex? _mutex;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isFirstInstance;
        private bool _disposed;

        public event Action<string>? FileReceived;

        public bool TryStartAsPrimary()
        {
            _mutex = new Mutex(true, MutexName, out _isFirstInstance);

            if (_isFirstInstance)
            {
                StartPipeServer();
                return true;
            }

            // Not the first instance, release the mutex handle
            _mutex.Dispose();
            _mutex = null;
            return false;
        }

        public static bool SendFileToPrimary(string filePath)
        {
            try
            {
                using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                client.Connect(1000); // 1 second timeout

                using var writer = new StreamWriter(client);
                writer.WriteLine(filePath);
                writer.Flush();

                return true;
            }
            catch (Exception ex)
            {
                App.Log($"Failed to send file to primary instance: {ex.Message}");
                return false;
            }
        }

        private void StartPipeServer()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        using var server = new NamedPipeServerStream(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

                        await server.WaitForConnectionAsync(token);

                        if (token.IsCancellationRequested)
                            break;

                        using var reader = new StreamReader(server);
                        var filePath = await reader.ReadLineAsync();

                        if (!string.IsNullOrEmpty(filePath))
                        {
                            App.Log($"Received file from secondary instance: {filePath}");
                            FileReceived?.Invoke(filePath);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        App.Log($"Pipe server error: {ex.Message}");
                    }
                }
            }, token);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();

            if (_isFirstInstance && _mutex != null)
            {
                _mutex.ReleaseMutex();
                _mutex.Dispose();
            }
        }
    }
}
