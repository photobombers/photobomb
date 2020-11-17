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
    public class Detector
    {
        private List<(string Label, float Score)> GetDetectedObjects(IList<YoloBoundingBox> boundingBoxes)
        {
            List<(string Label, float Score)> detectedList = new List<(string Label, float Score)>();

            foreach (var box in boundingBoxes)
            {
                detectedList.Add((box.Label, box.Confidence));
            }

            return detectedList;
        }
    }
}
