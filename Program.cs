// See https://aka.ms/new-console-template for more information

using System.Reflection;
using CommandLine;
using Newtonsoft.Json;

public class Program
{
    private static Options _options;
    private static Dictionary<string, string[]> _commonTypesDict;
    
    public static void Main(string[] args)
    {
        Parser.Default.ParseArguments<Options>(args)
            .WithParsed(Main);
    }
    
    private static void Main(Options options)
    {
        _options = options;
        
        string path = _options.Path;
        if (path is "." or "./")
            path = Directory.GetCurrentDirectory();
        
        string commonTypes = ReadFileFromMemory("common_types.json");
        _commonTypesDict = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(commonTypes)!;
        
        CollectFiles(path);
    }

    private static string ReadFileFromMemory(string path)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        string resourceName = $"dir_sort.{path}";

        using Stream stream = assembly.GetManifestResourceStream(resourceName)!;
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }

    private static string? GetTargetFolder(string ext)
    {
        if (!_options.Collect) return ext.ToLower();
        
        // check dict values and return key
        if (_commonTypesDict.Values.Any(x => x.Contains(ext)))
            return _commonTypesDict.First(x => x.Value.Contains(ext)).Key;
            
        if (_options.IgnoreUnknown)
            return null;
            
        return "Unknown";
    }

    private static void CollectFiles(string path)
    {
        string[] files = Directory.GetFiles(path);
        int movedFiles = 0;
        foreach (string file in files)
        {
            string extension = Path.GetExtension(file).TrimStart('.');
            string? targetFolder = GetTargetFolder(extension);
            
            if (targetFolder is null)
                continue;
            
            string targetPath = Path.Combine(path, targetFolder);
            Directory.CreateDirectory(targetPath);
            File.Move(file, Path.Combine(targetPath, Path.GetFileName(file)));

            Console.WriteLine($"Moved {file} to {targetPath}");
            movedFiles++;
        }
        
        Console.WriteLine($"Done! Moved {movedFiles} files.");
    }
}

public class Options
{
    [Option('p', "path", Required = true, HelpText = "Path to the directory")]
    public string Path { get; set; }
    
    [Option('c', "collect", Default = true, HelpText = "Collect common file types")]
    public bool Collect { get; set; }
    
    [Option('i', "ignore-unknown", Default = true, HelpText = "Ignore unknown file types")]
    public bool IgnoreUnknown { get; set; }
}