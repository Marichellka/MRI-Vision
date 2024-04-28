using System.Drawing;

namespace MRI_Vision.UI.Utils
{
    public class AnomalyPicture : MRIPicture
    {
        public float[] AnomalyIndexes => _anomalyIndexes;

        private float[] _anomalyIndexes;
        private const float _threshold = 0.1f;

        public AnomalyPicture(
            float[][][] data,
            Color? shift = null)
            : base(data, new AnomalyColorStrategy(shift))
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

        private float[] CalculateAnomalyIndexes()
        {
            float[] anomalyIndexes = new float[_imageData.Length];

            for (int i = 0; i < _size[0]; i++)
            {
                float anomalySum = 0;
                float anomalyCount = 0;
                for (int j = 0; j < _size[1]; j++)
                {
                    for (int k = 0; k < _size[2]; k++)
                    {
                        float anomalyIndex = _imageData[i][j][k] / _max;
                        if (anomalyIndex > _threshold)
                        {
                            anomalySum += anomalyIndex;
                            anomalyCount++;
                        }

                    }
                }
                anomalyIndexes[i] = anomalySum/anomalyCount;
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
