// See https://aka.ms/new-console-template for more information
using System.IO.Compression;

Console.WriteLine("Enter path of the source folder:");
string? sourceFolder = Console.ReadLine();

Console.WriteLine("Enter path of the destination folder:");
string? destinationFolder = Console.ReadLine();

if (sourceFolder == destinationFolder)
{
    Exit("Source and destination folder cannot point to the same location.");
}

string[]? filesInSource = Directory.GetFiles(sourceFolder, "*.*", SearchOption.AllDirectories);

if (filesInSource.Length == 0)
{
    Exit("No files found in the source folder.");
    return;
}

string[]? filesInDestination = Directory.GetFiles(destinationFolder, "*.*", SearchOption.AllDirectories);

if (filesInDestination.Length == 0)
{
    Exit("No files found in the destination folder.");
    return;
}

Console.WriteLine("Listing files only in the source folder...");
List<string> uniqueFiles = CompareFolders(sourceFolder, destinationFolder);

string uniqueFilesFile = Path.Combine(sourceFolder, "OrphanedFiles.txt");

if (File.Exists(uniqueFilesFile))
{
    File.Delete(uniqueFilesFile);
}

File.AppendAllText(uniqueFilesFile, "Orphaned files only in source folder:" + Environment.NewLine);
foreach (string file in uniqueFiles.Where(file => !IsFileExcluded(file)))
{
    File.AppendAllText(uniqueFilesFile, file + Environment.NewLine);
}

Console.WriteLine("Comparing contents of the source folder with contents of the destination folder...");
List<string> changedOrNewFiles = new()
{
    Capacity = 100
};

foreach (string file in filesInDestination)
{
    string relativePath = file[(destinationFolder.Length + 1)..];
    string correspondingFile = Path.Combine(sourceFolder, relativePath);

    if (!File.Exists(correspondingFile) || !AreFilesContentEqual(file, correspondingFile))
    {
        if (!IsFileExcluded(file))
        {
            changedOrNewFiles.Add(file);
        }

    }
}

string zipFilePath = Path.Combine(sourceFolder, "ChangedFiles.zip");
if (File.Exists(zipFilePath))
{
    ConsoleKeyInfo choice = Choice("ChangedFiles.zip already exists. Do you want to overwrite file or cancel? (o/esc)?");
    if (choice.KeyChar == 'O' || choice.KeyChar == 'o')
    {
        File.Delete(zipFilePath);
    }
    else
    {
        Exit();
    }
}

if (IsFileInUse(zipFilePath))
{
    ConsoleKeyInfo choice = Choice("ChangedFiles.zip seems to be in use. Do you want to continue or cancel? (c/esc)");
    if (choice.Key == ConsoleKey.Escape)
    {
        Exit();
    }
}

using (ZipArchive zip = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
{
    zip.CreateEntryFromFile(uniqueFilesFile, uniqueFilesFile[(destinationFolder.Length + 1)..]);
    foreach (string file in changedOrNewFiles)
    {
        string relativePath = file[(destinationFolder.Length + 1)..];
        zip.CreateEntryFromFile(file, relativePath);
    }

}

Exit($"Zip file created at: {zipFilePath}");

return;


static bool IsFileInUse(string file)
{
    if (!File.Exists(file))
    {
        return false;
    }

    try
    {
        using FileStream stream = new(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        stream.Close();
        return false;
    }
    catch (IOException)
    {
        return true;
    }
}

static List<string> CompareFolders(string? source, string destination)
{
    HashSet<string?> sourceFiles = Directory.GetFiles(source).Select(Path.GetFileName).ToHashSet();
    HashSet<string?> destinationFiles = Directory.GetFiles(destination).Select(Path.GetFileName).ToHashSet();

    sourceFiles.ExceptWith(destinationFiles);

    return sourceFiles.ToList();
}

static bool AreFilesContentEqual(string filePath1, string filePath2)
{
    byte[] file1 = File.ReadAllBytes(filePath1);
    byte[] file2 = File.ReadAllBytes(filePath2);

    // compare contents
    if (file1.Length != file2.Length)
    {
        return false;
    }

    // compare bytes
    for (int i = 0; i < file1.Length; i++)
    {
        if (file1[i] != file2[i])
        {
            return false;
        }
    }

    return true;
}

static bool IsFileExcluded(string filePath)
{
    return filePath.Contains(".git") || filePath.Contains(".vs");
}


static ConsoleKeyInfo Choice(string message)
{
    Console.WriteLine(message);
    ConsoleKeyInfo choice = Console.ReadKey();
    Console.WriteLine();
    return choice;
}

static void Exit(string message = "")
{
    if (!string.IsNullOrEmpty(message))
    {
        Console.WriteLine(message);
    }
    Console.WriteLine("Press any key to exit.");
    Console.ReadLine();
}