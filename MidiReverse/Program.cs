using Melanchall.DryWetMidi.Core;

namespace MidiReverse
{
    class Program
    {
        private static Mode? _mode;
        private static string? _inputPath;
        private static string? _outputPath;

        private readonly static ReadingSettings _settings = new()
        {
            InvalidChannelEventParameterValuePolicy = InvalidChannelEventParameterValuePolicy.ReadValid
        };

        private readonly static MidiReverse _reverter = new();

        private static void Main(string[] args)
        {
            bool parametersAreValid = ParseCommandLineParameters(args);

            if (!parametersAreValid)
            {
                Help();
                return;
            }

            // Check if input path specified by user is a file or a folder
            if (!_mode.HasValue)
            {
                if (Directory.Exists(_inputPath))
                {
                    _mode = Mode.Directory;
                }
                else if (File.Exists(_inputPath))
                {
                    _mode = Mode.File;
                }
                else
                {
                    Console.WriteLine($"File or directory {_inputPath} not found");
                    return;
                }
            }

            string[] files;

            if (_mode == Mode.Directory)
            {
                files = Directory.GetFiles(_inputPath!, "*.mid");
            }
            else
            {
                files = [_inputPath!];
            }

            if (files.Length == 0)
            {
                Console.WriteLine($"No .mid files found in {_inputPath}");
                return;
            }

            if (_outputPath is null)
            {
                try
                {
                    if (_mode == Mode.Directory)
                    {
                        _outputPath = Path.Combine(_inputPath!, "reverse");
                    }
                    else
                    {
                        _outputPath = Path.Combine(AppContext.BaseDirectory, "reverse");
                    }
                }
                catch (ArgumentException)
                {
                    Console.WriteLine($"Invalid path: {_inputPath}");
                    return;
                }
            }

            Directory.CreateDirectory(_outputPath);

            string outputFile;
            int filesSaved = 0;
            int errors = 0;

            foreach (string inputFile in files)
            {   
                outputFile = Path.GetFileNameWithoutExtension(inputFile) + "_reverse.mid";
                outputFile = Path.Combine(_outputPath, outputFile);

                try
                {
                    ReverseFile(inputFile, outputFile);
                    filesSaved++;
                }
                catch
                {
                    errors++;
                }
            }

            Console.WriteLine($"Files saved: {filesSaved}, errors: {errors}");
        }

        private static bool ParseCommandLineParameters(string[] args)
        {
            if (args.Length > 2)
            {
                return false;
            }

            if (args.Length == 0)
            {
                if (Confirm())
                {
                    _mode = Mode.Directory;
                    _inputPath = AppContext.BaseDirectory;
                    return true;
                }

                return false;
            }

            if (args[0].ToLower() == "-confirm")
            {
                if (args.Length > 1)
                {
                    return false;
                }

                _mode = Mode.Directory;
                _inputPath = AppContext.BaseDirectory;
                return true;
            }

            _inputPath = args[0];

            if (args.Length == 2)
            {
                _outputPath = args[1];
            }

            return true;
        }

        private static void Help()
        {
            const string Help =
@"Usage:

midireverse: 
    Reverse all .mid files in the current folder (confirmation needed)

midireverse -confirm
    Reverse all .mid files in the current folder

midireverse <filename> [output_folder]
    Reverse the MIDI file

midireverse <folder> [output_folder]
    Reverse all .mid files in the folder
";

            Console.WriteLine(Help);
        }

        private static bool Confirm()
        {
            Console.WriteLine(@"Reverse all .mid files in the current folder? Output files will be put in \reverse folder.
(Y)es/(N)o ?");

            string? input = Console.ReadLine()?.ToLower();
            return input == "y" || input == "yes";
        }


        private static void ReverseFile(string inputFile, string outputFile)
        {
            MidiFile midi;

            try
            {
                midi = MidiFile.Read(inputFile, _settings);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to open file: {inputFile}. Error: {e.Message}");
                throw;
            }

            try
            {
                _reverter.Reverse(midi).Write(outputFile, overwriteFile: true);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to save file: {outputFile}. Error: {e.Message}");
                throw;
            }

            Console.WriteLine($"File saved: {outputFile}");
        }

        private enum Mode
        {
            File,
            Directory
        }
    }
}