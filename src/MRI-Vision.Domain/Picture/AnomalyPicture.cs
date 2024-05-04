using MRI_Vision.UI.Utils;
using System.Drawing;

namespace MRI_Vision.Domain.Picture
{
    public class AnomalyPicture : MRIPicture
    {
        public float[] AnomalyIndexes => _anomalyIndexes;

        private float[] _anomalyIndexes;
        private const float _threshold = 0.2f;
        private const float _epsilon = 0.01f;

        public AnomalyPicture(
            float[][][] inputData,
            float[][][] restoredData,
            Color? shift = null)
            : base(GetAnomalyData(inputData, restoredData), new AnomalyColorStrategy(shift))
        {
            _anomalyIndexes = CalculateAnomalyIndexes();
        }

        public AnomalyPicture(
            float[][][] data,
            int[] size,
            float max,
            MRIPictureOrientation orientation,
            IColorStrategy colorStrategy) : base(data, size, max, orientation, colorStrategy)
        {
            _anomalyIndexes = CalculateAnomalyIndexes();
        }

        private static float[][][] GetAnomalyData(float[][][] inputData, float[][][] restoredData)
        {
            float[][][] anomalyData = new float[inputData.Length][][];
            float minInput = inputData.Min(x => x.Min(y => y.Min()));
            float minRestored = inputData.Min(x => x.Min(y => y.Min()));

            for (int i = 0; i < anomalyData.Length; i++)
            {
                anomalyData[i] = new float[inputData[i].Length][];
                for (int j = 0; j < anomalyData[i].Length; j++)
                {
                    anomalyData[i][j] = new float[inputData[i][j].Length];
                    for (int k = 0; k < anomalyData[i][j].Length; k++)
                    {
                        if (restoredData[i][j][k] <= minRestored + _epsilon || inputData[i][j][k] <= minInput + _epsilon)
                            anomalyData[i][j][k] = 0;
                        else
                        {
                            float anomaly = Math.Abs(inputData[i][j][k] - restoredData[i][j][k]);

                            if (anomaly > _threshold)
                                anomalyData[i][j][k] = anomaly;
                            else
                                anomalyData[i][j][k] = 0;
                        }
                    }
                }
            }

            return anomalyData;
        }

        private float[] CalculateAnomalyIndexes()
        {
            float[] anomalyIndexes = new float[_imageData.Length];
            int sliceSize = _size[1] * _size[2];

            for (int i = 0; i < _size[0]; i++)
            {
                float anomalySum = 0;
                int anomalyCount = 0;
                for (int j = 0; j < _size[1]; j++)
                {
                    for (int k = 0; k < _size[2]; k++)
                    {
                        float anomalyIndex = _imageData[i][j][k] / _max;
                        if (anomalyIndex > _epsilon)
                        {
                            anomalySum += anomalyIndex;
                            anomalyCount++;
                        }
                    }
                }
                anomalyIndexes[i] = anomalySum / (sliceSize - anomalyCount);
            }

            return anomalyIndexes;
        }

        public override AnomalyPicture RotatePicture(MRIPictureOrientation newOrientation)
        {
            if (_orientation == newOrientation) return this;

            (var newData, var newSize) = RotateImageData(newOrientation);

            return new AnomalyPicture(newData, newSize, _max, newOrientation, _colorStrategy);
        }
    }
}
