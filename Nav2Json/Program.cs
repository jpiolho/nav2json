using QuakeNavSharp.Files;
using QuakeNavSharp.Json;
using QuakeNavSharp.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Nav2Json
{
    class Program
    {
        class ProgramOptions
        {
            public bool? Json { get; set; } = null;
            public bool Silent { get; set; } = false;
            public bool Overwrite { get; set; } = false;
            public string Output { get; set; } = null;
            public bool OutputToStdout { get; set; } = false;


            public string JsonMapName { get; set; } = null;
            public string JsonMapFilename { get; set; } = null;
            public string JsonMapAuthor { get; set; } = null;
            public string JsonComment { get; set; } = null;
            public List<string> JsonContributors { get; set; } = new List<string>();
            public List<string> JsonMapUrl { get; set; } = new List<string>();
        }

        static ProgramOptions options;

        static async Task Main(string[] args)
        {
            if (args.Length == 0 || !ParseOptions(args))
            {
                PrintHelp();
                return;
            }
            
            var filename = args[^1];

            if (!options.Json.HasValue)
            {
                switch (Path.GetExtension(filename))
                {
                    case ".nav": await NavToJsonAsync(filename); break;
                    case ".navjson": await JsonToNavAsync(filename); break;
                }

                if (!options.Silent)
                    Console.WriteLine("Could not identify the file");
            }
            else if(options.Json.Value)
            {
                await NavToJsonAsync(filename);
            }
            else
            {
                await JsonToNavAsync(filename);
            }
        }

        private static bool ParseOptions(string[] args)
        {
            options = new ProgramOptions();

            for (var i = 0; i < args.Length - 1; i++)
            {
                var option = args[i].ToLowerInvariant();
                switch (option)
                {
                    case "-json": options.Json = true; break;
                    case "-nav": options.Json = false; break;

                    case "-s": options.Silent = true; break;
                    case "-ow": options.Overwrite = true; break;
                    case "-out": options.Output = args[i + 1]; break;
                    case "-outstdout": options.OutputToStdout = true; break;

                    case "-map-filename": options.JsonMapFilename = args[++i]; break;
                    case "-map-author": options.JsonMapAuthor = args[++i]; break;
                    case "-map-name": options.JsonMapName = args[++i]; break;
                    case "-map-url": options.JsonMapUrl.Add(args[++i]); break;
                    case "-json-comment": options.JsonComment = args[++i]; break;
                    case "-json-contributor": options.JsonContributors.Add(args[++i]); break;

                    default: Console.WriteLine($"Unknown option '{option}'"); return false;
                }
            }

            return true;
        }

        private static void PrintHelp()
        {
            Console.WriteLine("nav2json [parameters] [input file]");
            Console.WriteLine("Available parameters:");

            Console.WriteLine("-json\tConvert from nav to a json file.");
            Console.WriteLine("-nav\tConvert from json to a nav file.");
            Console.WriteLine("NOTE: If neither of the above are specified, automatic detection will be applied based on input file extension.");
            Console.WriteLine();
            Console.WriteLine("-s\tSilent.");
            Console.WriteLine("-ow\tOverwrite without asking.");
            Console.WriteLine("-out [file]\tOutput path + filename.");
            Console.WriteLine("-outstdout\tOutput to STDOUT instead of a file.");
            Console.WriteLine();
            Console.WriteLine("-map-filename [filename]\tSpecifies the map filename for the json.");
            Console.WriteLine("-map-author [author]\tSpecifies the map author for the json.");
            Console.WriteLine("-map-name [name]\tSpecifies the map name for the json.");
            Console.WriteLine("-map-url [url]\tAdds a map url for the json. Can add multiple.");
            Console.WriteLine("-json-comment [comment]\tSpecifies a comment for the json.");
            Console.WriteLine("-json-contributor [contributor]\tAdds a contributor to the json. Can add multiple.");
        }

        private static bool AskToOverwrite()
        {
            if (options.Overwrite)
                return true;

            if (options.Silent)
                return false;

            Console.Write("The target file already exists. Do you wish to overwrite? (y/n): ");

            while(true)
            {
                switch(Console.ReadKey().Key)
                {
                    case ConsoleKey.Y: Console.WriteLine(); return true;
                    case ConsoleKey.N: Console.WriteLine(); return false;
                }
            }

            
        }

        private static async Task JsonToNavAsync(string path)
        {
            var navJson = NavJson.FromJson(await File.ReadAllTextAsync(path));
            var navGraph = navJson.ToNavigationGraph();
            var navFile = navGraph.ToNavFile();

            var filename = Path.GetFileNameWithoutExtension(path);

            if (options.OutputToStdout)
            {
                using (var stdout = Console.OpenStandardOutput())
                    await navFile.SaveAsync(stdout);
            }
            else
            {
                var targetFile = options.Output ?? Path.Combine(Path.GetDirectoryName(path), $"{filename}.nav");

                if (File.Exists(targetFile) && !AskToOverwrite())
                    return;

                using (var fs = new FileStream(targetFile, FileMode.Create))
                    await navFile.SaveAsync(fs);
            }

            if (!options.Silent)
                Console.WriteLine($"Converted {filename}.navjson to {filename}.nav");
        }

        private static async Task NavToJsonAsync(string path)
        {
            NavFile navFile;
            using (var fs = new FileStream(path, FileMode.Open))
                navFile = await NavFile.FromStreamAsync(fs);

            var navGraph = NavigationGraph.FromNavFile(navFile);
            var navJson = NavJson.FromNavigationGraph(navGraph);

            var filename = Path.GetFileNameWithoutExtension(path);

            // Fill in some details, so they at least show up in the json :)
            var map = navJson.Map = new NavJsonMap();
            map.Filename = options.JsonMapFilename ?? filename;
            map.Author = options.JsonMapAuthor ?? "";
            map.Name = options.JsonMapName ?? "";
            map.Urls = options.JsonMapUrl.ToArray();

            navJson.Contributors = options.JsonContributors.ToArray();
            navJson.Comments = options.JsonComment;


            if(options.OutputToStdout)
            {
                using (var stdout = Console.OpenStandardOutput())
                using (var writer = new StreamWriter(stdout))
                    await writer.WriteLineAsync(navJson.ToJson());
            }
            else
            {
                var targetFile = options.Output ?? Path.Combine(Path.GetDirectoryName(path), $"{filename}.navjson");

                if (File.Exists(targetFile) && !AskToOverwrite())
                    return;

                await File.WriteAllTextAsync(targetFile, navJson.ToJson());
            }

            if(!options.Silent)
                Console.WriteLine($"Converted {filename}.nav to {filename}.navjson");
        }
    }
}
