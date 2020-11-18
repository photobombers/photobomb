using System.Collections.Generic;
using System.Linq;
using Microsoft.ML;

using PhotoBombDetector.YoloParser;
using PhotoBombDetector.DataStructures;

namespace PhotoBombDetector
{
    public static class Detector
    {
        private static List<(string Label, float Score)> ExtractDetectedObjects(IList<YoloBoundingBox> boundingBoxes)
        {
            List<(string Label, float Score)> detectedList = new List<(string Label, float Score)>();

            foreach (var box in boundingBoxes)
            {
                detectedList.Add((box.Label, box.Confidence));
            }

            return detectedList;
        }

        public static List<(string ImageFilename, List<(string Label, float Score)> TagList)> GetObjectList(string imagesFolder, string modelFilePath)
        {
            List<(string ImageFilename, List<(string Label, float Score)> TagList)> result = new List<(string ImageFilename, List<(string Label, float Score)> TagList)>();

            MLContext mlContext = new MLContext();

            IEnumerable<ImageNetData> images = ImageNetData.ReadFromFile(imagesFolder);
            IDataView imageDataView = mlContext.Data.LoadFromEnumerable(images);

            var modelScorer = new OnnxModelScorer(imagesFolder, modelFilePath, mlContext);

            // Use model to score data
            IEnumerable<float[]> probabilities = modelScorer.Score(imageDataView);

            YoloOutputParser parser = new YoloOutputParser();

            var boundingBoxes =
                probabilities
                .Select(probability => parser.ParseOutputs(probability))
                .Select(boxes => parser.FilterBoundingBoxes(boxes, 5, .5F));

            for (var i = 0; i < images.Count(); i++)
            {
                string imageFileName = images.ElementAt(i).Label;
                IList<YoloBoundingBox> detectedObjects = boundingBoxes.ElementAt(i);

                result.Add((imageFileName, ExtractDetectedObjects(detectedObjects)));
            }

            return result;
        }
    }
}
