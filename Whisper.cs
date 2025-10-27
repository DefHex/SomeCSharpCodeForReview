// Required NuGet packages:
// - whisper.net (Whisper.NET)
// - NAudio (for microphone input)

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using Whisper.net;
using Whisper.net.Ggml;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Loading Whisper model...");

        // Load the model
        var modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WhisperModels", "ggml-tiny.en.bin");  // Use a smaller model for real-time processing
        if (!File.Exists(modelPath))
        {
            Console.WriteLine($"Model file '{modelPath}' not found.");
            return;
        }

        using var whisperFactory = WhisperFactory.FromPath(modelPath);
        using var processor = whisperFactory.CreateBuilder().WithThreads(Environment.ProcessorCount).WithLanguage("en").Build();
        
        

        var sampleRate = 16000;
        var bufferSeconds = 5;
        var bufferSize = sampleRate * bufferSeconds;
        var audioBuffer = new float[bufferSize];

        using var waveIn = new WaveInEvent
        {
            WaveFormat = new WaveFormat(sampleRate, 1), // Mono 16kHz
            BufferMilliseconds = 100
        };

        var bufferOffset = 0;
        var audioLock = new object();

        waveIn.DataAvailable += (s, e) =>
        {
            lock (audioLock)
            {
                for (int i = 0; i < e.BytesRecorded && bufferOffset < audioBuffer.Length; i += 2)
                {
                    short sample = BitConverter.ToInt16(e.Buffer, i);
                    audioBuffer[bufferOffset++] = sample / 32768f;
                }
            }
        };

        waveIn.StartRecording();

        Console.WriteLine("Recording... Press Ctrl+C to stop.");

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        while (!cts.Token.IsCancellationRequested)
        {
            await Task.Delay(bufferSeconds * 1000);

            float[] bufferCopy;
            lock (audioLock)
            {
                // If the buffer is full, process it
                if (bufferOffset >= audioBuffer.Length)
                {
                    bufferCopy = new float[bufferOffset];
                    Array.Copy(audioBuffer, bufferCopy, bufferOffset);
                    bufferOffset = 0;
                }
                else
                {
                    // If not full, skip processing
                    continue;
                }
            }
            lock (audioLock)
            {
                bufferCopy = new float[bufferOffset];
                Array.Copy(audioBuffer, bufferCopy, bufferOffset);
                bufferOffset = 0;
            }

            if (bufferCopy.Length > 0)
            {
                var segments = processor.ProcessAsync(bufferCopy);

                // With this corrected code:
                await foreach (var segment in processor.ProcessAsync(bufferCopy))
                {
                    Console.WriteLine($"[{segment.Start} - {segment.End}] {segment.Text}");
                    Console.WriteLine($"{segment.NoSpeechProbability}");
                }
            }
        }

        waveIn.StopRecording();
    }
}
