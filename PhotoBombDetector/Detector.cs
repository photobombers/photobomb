using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
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

        public static List<(string ImagePath, List<(string Label, float Score)>)> GetObjectList(string imagesFolder, string modelFilePath)
        {
            List<(string ImagePath, List<(string Label, float Score)>)> result = new List<(string ImagePath, List<(string Label, float Score)>)>();

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
