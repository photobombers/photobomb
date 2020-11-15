using System;
using System.IO;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;

public class Program
{
    public static int Main(string[] args)
        => CommandLineApplication.Execute<Program>(args);

    [Required]
    [DirectoryExists]
    [Option("-d | --directory <DIRECTORY>", Description = "Directory containing photos to tag identified objects to")]
    public string DirectoryPath { get; }

    private string[] GetPhotoFileList(string[] fileList) =>
        fileList.Where(f => (new string[] { ".jpg" }).Contains(Path.GetExtension(f).ToLower())).ToArray();
    private void OnExecute()
    {
        var photoFiles = GetPhotoFileList(Directory.GetFiles(DirectoryPath));
        foreach (var file in photoFiles)
        {
            Console.WriteLine(file);
        }
    }
}