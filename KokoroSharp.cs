using KokoroSharp;
using KokoroSharp.Core;
using NumSharp.Extensions;

namespace KokoroSharpTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                using KokoroTTS tts = KokoroTTS.LoadModel();
                KokoroVoice sky= KokoroVoiceManager.GetVoice("af_sky");
                //if (!KokoroVoiceManager.Voices.Any())
                //    Console.WriteLine("No voices found!");
                //foreach (var voice in KokoroVoiceManager.Voices) { Console.WriteLine(voice.Name); }
                Console.WriteLine("Done listing voices.");
                //tts.Speak("Hello there handsome! How you doing?", sky);
                //tts.Speak("This is another sentence", sky);
                //tts.Speak("And this is the final sentence", sky);
                var sentance = new Queue<string>();
                sentance.Enqueue("Hello NastyA");
                Thread.Sleep(100); // Wait for a second before the next sentence;
                sentance.Enqueue("This is the last sentence in the queue.");
                ManualResetEventSlim manualResetEventSlim = new ManualResetEventSlim(false);
                tts.OnSpeechCompleted += (e) =>
                {
                    //Console.WriteLine($"Speech completed: {e.SpokenText}");
                    manualResetEventSlim.Set();
                };
                while (sentance.Count>0)
                {
                    string saying=sentance.Dequeue();
                    manualResetEventSlim.Wait();
                    tts.Speak(saying, sky);
                    manualResetEventSlim.Wait();
                }
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
