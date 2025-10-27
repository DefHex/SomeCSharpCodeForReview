using LLama.Common;
using LLama;
using static LLama.Common.ChatHistory;
using System.Speech.Recognition;
using Microsoft.Recognizers.Text;
using System.Runtime.ExceptionServices;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SpeechSynt
{
    public class Program
    {
        static SpeechRecognitionEngine recognizer;
        static string recognizedText = string.Empty; // Add a static variable to store the recognized text
        private static string userInput;
        private static bool voiceDet;
        private static bool unblockListeningAfterTTS;

        static async Task Main(string[] args)
        {
            //Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("If you want voice detection press v , for text write anything else");
            voiceDet = false;
            //try 
            
            var keyInfo = Console.ReadKey(intercept: true); // Read the key press without displaying it
            if (keyInfo.Key == ConsoleKey.V)
            {
                voiceDet = true;
            }
        
            SynthInit init = new SynthInit();
            Console.WriteLine("Loading...");
            ///Initialize chat and example history
            string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "llmModel", "DeepSeek-R1-Distill-Qwen-1.5B-Q8_0.gguf");
            
            var chatHistory = new ChatHistory();
            var chatHistoryMessagesList = chatHistory.Messages;
            chatHistory.AddMessage(AuthorRole.User, "Hello, Stephen.");
            chatHistory.AddMessage(AuthorRole.Assistant, "Hello. How may I help you today?");
            chatHistory.AddMessage(AuthorRole.User, "Please tell me the largest city in Europe.");
            chatHistory.AddMessage(AuthorRole.Assistant, "Sure. The largest city in Europe is Moscow, the capital of Russia.");
            
            //chatHistory.AddMessage(AuthorRole.User, "We're going to play 20 questions game, where you would think of a word and I would ask you a yes or no question to guess the word. I have maximum 20 questions to guess the word. If I manage to guess it I win, otherwise you win.");
            //chatHistory.AddMessage(AuthorRole.Assistant, "Sounds great. Ask your first question.");
            //chatHistory.AddMessage(AuthorRole.User, "Is the thing your thinking of round.");
            //chatHistory.AddMessage(AuthorRole.Assistant, "2.No");


            var modelParams = new ModelParams(modelPath)
            {
                ContextSize = 1024,
            };

            var weights = LLamaWeights.LoadFromFile(modelParams);
            var context = new LLamaContext(weights, modelParams);
            var executor = new InteractiveExecutor(context);
            var session = new ChatSession(executor, chatHistory);

            Console.WriteLine("To quit the chat write \"ExitChat\" ");
            // Show initial prompt
            Console.WriteLine();
            foreach (var message in chatHistoryMessagesList)
            {
                Console.WriteLine($"{message.AuthorRole}: {message.Content}");
            }
            unblockListeningAfterTTS = true;
            recognizer = new SpeechRecognitionEngine();
            recognizer.SetInputToDefaultAudioDevice();
            recognizer.LoadGrammar(new DictationGrammar());
            /// Run the chat loop
            try
            {
                Console.Write("User:");
                while (true)
                {
                    var inferenceParams = new InferenceParams() { AntiPrompts = new List<string> { "User:" } };
                    var assistantResponse = "";
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    userInput = "";
                    ///
                    setUpTextInput(voiceDet);

                    var userMessage = new Message(AuthorRole.User, userInput);
                    List<string> words = new List<string>();
                    async Task ProcessAndSpeakSentences(string text)
                    {
                        unblockListeningAfterTTS = false;
                        words.Add(text);
                        if (words.Contains(".") || words.Contains("\n") || words.Contains("User"))
                        {
                            ///Don't mention User and turn on listening after 10 seconds
                            if (words.Contains("User")) {
                                words.Remove("User");
                                init.synth1.SpeakCompleted += (sender, e) =>
                                {
                                    Thread.Sleep(10000);
                                    unblockListeningAfterTTS = true;
                                };
                                init.synth1.SpeakCompleted -= (sender, e) => { };
                            }
                            ///Don't mention assistant
                            if (words.Contains("Assistant")){ words.Remove("Assistant");}
                            foreach (string word in words.ToList())
                            {
                                string sentence = string.Join(" ", words);
                                words = new List<string>();
                                init.synth1.SpeakAsync(sentence);
                            }
                            
                        }
                    }
                    // Pass only the latest message
                    await foreach (var text in session.ChatAsync(userMessage, inferenceParams)) 
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write(text);
                        await ProcessAndSpeakSentences(text);
                        assistantResponse += text;
                    }
                    chatHistory.AddMessage(AuthorRole.Assistant, assistantResponse);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static void setUpTextInput(bool voiceDet)
        {
            
            if (voiceDet)
            {
               checkLoop();
            }
            else
            {
                userInput = Console.ReadLine();
                if (userInput == "ExitChat")
                {
                    Console.Clear();
                    Console.WriteLine("Hope you liked the program");
                    //Console.BackgroundColor = ConsoleColor.Magenta;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("Bye ");
                    Environment.Exit(0);
                }
            }
        }
        private static void checkLoop()
        {
            if (unblockListeningAfterTTS)
            {
                try
                {
                    Console.Beep();
                    Console.WriteLine(" - now listening:");
                    recognizer.SpeechRecognized -= recognizer_SpeechRecognized;
                    recognizer.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(recognizer_SpeechRecognized);
                    //recognizer.RecognizeAsync(RecognizeMode.Multiple
                    if (recognizer.AudioState == AudioState.Stopped)
                    {
                        recognizer.RecognizeAsync(RecognizeMode.Multiple);
                    }
                    else
                    {
                        recognizer.RecognizeAsyncStop();
                        recognizer.RecognizeAsync(RecognizeMode.Multiple);
                    }

                    Console.WriteLine("Press Enter to stop.");
                    Console.ReadLine();
                    recognizer.RecognizeAsyncStop();
                    userInput = recognizedText; // Use the recognized text
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
            else
            {
               Thread.Sleep(1000);
               checkLoop();
            }
        }
        private static void recognizer_SpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
        {
            recognizedText = e.Result.Text; // Store the recognized text
            Console.WriteLine($"Speech User: {e.Result.Text}");
        }
    }
}

