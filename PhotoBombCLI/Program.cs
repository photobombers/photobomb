using System;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using McMaster.Extensions.CommandLineUtils;

namespace PhotoBombCLI
{
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

        private void OnExecute()
        {
            var photoFiles = GetPhotoFileList(Directory.GetFiles(DirectoryPath));

            foreach (var file in photoFiles)
            {
                Console.WriteLine(file);
                AddImageTag(file, "DummyTestTag");
            }
        }
    }
}