using Microsoft.ML.Data;

namespace PhotoBombDetector.DataStructures
{
    public class ImageNetPrediction
    {
        [ColumnName("grid")]
        public float[] PredictedLabels;
    }
}