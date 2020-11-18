using System;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using McMaster.Extensions.CommandLineUtils;

using PhotoBombDetector;

namespace PhotoBombCLI
{
    public class Program
    {
        public static int Main(string[] args)
            => CommandLineApplication.Execute<Program>(args);

        [Required]
        [DirectoryExists]
        [Option("-d | --directory <DIRECTORY>", Description = "[Required] Directory containing photos to tag identified objects to")]
        public string DirectoryPath { get; }

        [FileExists]
        [Option("-m | --model <MODEL_FILEPATH>", Description = "File path to the model to be used. Default will use ./assets/Model/TinyYolo2_model.onnx")]
        public string ModelFilePath { get; }

        [Option("-c | --confidence <MINUMUM_CONFIDENCE_SCORE>", Description = "Minimum confidence score (0 to 1). Default is 0.6")]
        public float MinimumConfidence { get; } = 0.6f;

        private string[] GetPhotoFileList(string[] fileList) =>
            fileList.Where(f => (new string[] { ".jpg" }).Contains(Path.GetExtension(f).ToLower())).ToArray();

        private void SetTagPropertyItem(Image srcImg, Image tmpImg, string value)
        {
            byte[] buffer = Encoding.Unicode.GetBytes(value);
            PropertyItem propItem = tmpImg.GetPropertyItem(40094);
            propItem.Len = buffer.Length;
            propItem.Value = buffer;
            srcImg.SetPropertyItem(propItem);
        }

        private void SetImageTagList(string imageFile, string tagList)
        {
            // PropertyItem has no defined constructors. Therefore we use a
            // image file that *does* contain the needed properties as a template.
            Image tmpImg = Image.FromFile("assets\\dummy_tag_photo.jpg");// a image file contain the comment
            string newFileName = imageFile.Replace(".jpg", ".new.jpg");
            bool newFileCreated = false;
            using (Image srcImg = Image.FromFile(imageFile))
            {
                SetTagPropertyItem(srcImg, tmpImg, tagList);
                srcImg.Save(newFileName);
                newFileCreated = true;
            }
            if (newFileCreated)
            {
                File.Delete(imageFile);
                File.Move(newFileName, imageFile);
            }
        }

        private string[] GetImageTagList(string imageFile)
        {
            string[] tagList = null;

            try
            {
                using (Image img = Image.FromFile(imageFile))
                {
                    string list = Encoding.Unicode.GetString(img.GetPropertyItem(40094).Value).Replace("\0", string.Empty);
                    if (list != null && list.Contains(";"))
                    {
                        tagList = list.Split(';');
                    }
                    else
                    {
                        tagList = new string[] { list };
                    }
                }
            }
            catch (System.ArgumentException)
            {

            }

            return tagList;
        }

        private void AddImageTag(string imageFile, string tag)
        {
            string newTagList = null;

            var tagList = GetImageTagList(imageFile);

            if (tagList is null)
            {
                newTagList = tag;
            }
            else if (!tagList.Contains(tag))
            {
                newTagList = string.Join(';', tagList.Concat(new string[] { tag }));
            }

            if (!string.IsNullOrWhiteSpace(newTagList))
            {
                SetImageTagList(imageFile, newTagList);
            }
        }


        public static string GetAbsolutePath(string relativePath)
        {
            FileInfo _dataRoot = new FileInfo(typeof(Program).Assembly.Location);
            string assemblyFolderPath = _dataRoot.Directory.FullName;

            string fullPath = Path.Combine(assemblyFolderPath, relativePath);

            return fullPath;
        }

        private void OnExecute()
        {
            string modelFilePath = string.IsNullOrEmpty(ModelFilePath) ? @"./assets/Model/TinyYolo2_model.onnx" : ModelFilePath;
            var detectedObjectList = Detector.GetObjectList(DirectoryPath, modelFilePath);

            Console.WriteLine("Detection Process Complete.");

            var photoFiles = GetPhotoFileList(Directory.GetFiles(DirectoryPath));

            foreach (var file in photoFiles)
            {
                var tagList = detectedObjectList.Where(imageItem => imageItem.ImageFilename == Path.GetFileName(file))?.FirstOrDefault().TagList;

                foreach (var tag in tagList)
                {
                    if (tag.Score >= MinimumConfidence)
                    {
                        AddImageTag(file, tag.Label);
                    }
                }
            }

            Console.WriteLine("Tagging Process Complete.");
        }
    }
}