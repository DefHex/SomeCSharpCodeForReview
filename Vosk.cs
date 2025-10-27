using Vosk;
using NAudio.Wave;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;


public class SpeechDetect
{
    //string example = "{\n  \"text\" : \" something something\"\n}";
    public static void Main(string[] arg)
    {
        string theVoiceModelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "VoskModel", "vosk-model-small-en-us-0.15");
        //string theVoiceModelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "VoskModel", "vosk-model-en-us-0.22");
        Model voiceModelPath = new Model(theVoiceModelPath);
        VoskRecognizer recording = new VoskRecognizer(voiceModelPath, 16000);
        using (var waveIn = new WaveInEvent())
        {
            waveIn.DeviceNumber = 0; // Set the device number to the first available device
            waveIn.WaveFormat = new WaveFormat(16000, 1);
            waveIn.BufferMilliseconds = 1000;
            waveIn.DataAvailable += (s, a) =>
            {
                if (recording.AcceptWaveform(a.Buffer, a.BytesRecorded))
                {
                    
                    JsonResponse jsonResponse = JsonSerializer.Deserialize<JsonResponse>(recording.FinalResult());
                    Console.WriteLine($"What is heard : {jsonResponse.text}");
                    ///Model hears the word "the" even when its not spoken 
                    ///filtering it out from the beginning and end of the sentence
                    if (jsonResponse.text.StartsWith("the ")) {
                        jsonResponse.text = jsonResponse.text.Substring(4);
                    } else if (jsonResponse.text.EndsWith("the")) {
                        jsonResponse.text = jsonResponse.text.Substring(0, jsonResponse.text.Length - 3);
                    }
                    if (!String.IsNullOrWhiteSpace(jsonResponse.text))
                    {
                        Console.WriteLine($"What is heard mod: {jsonResponse.text}");
                    }
                        
                }
                //else
                //{
                //    Console.WriteLine(recording.PartialResult());
                //}
            };
            waveIn.StartRecording();
            Console.WriteLine("Press any key to stop...");
            Console.Beep();
            Console.ReadKey();
            waveIn.StopRecording();
            waveIn.Dispose();
        }
    }
}


public class JsonResponse
{
   public string text { get; set; }
}
