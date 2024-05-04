using MRI_Vision.Python;
using MRI_Vision.UI.Utils;
using Python.Runtime;
using System.Drawing;

namespace MRI_Vision.Domain.Picture
{
    public enum MRIPictureOrientation
    {
        Front,
        Side,
        Top
    }

    public class MRIPicture
    {
        public int Length => _imageData.Length;
        public MRIPictureOrientation Orientation => _orientation;
        protected float[][][] _imageData;
        protected float _max;
        protected int[] _size;
        protected Bitmap[] _bitmapSlices;
        protected IColorStrategy _colorStrategy;
        protected MRIPictureOrientation _orientation;
        private const string _modelModulePath = @".\utils\image_helper.py";

        public MRIPicture(string path)
            : this(ReadImageAsync(path).Result)
        { }

        public MRIPicture(float[][][] data, IColorStrategy? colorStrategy = null)
            : this(data,
                  [data.Length, data[0].Length, data[0][0].Length],
                  data.Max(x => x.Max(y => y.Max())),
                  MRIPictureOrientation.Side, colorStrategy)
        { }

        protected MRIPicture(
            float[][][] data,
            int[] size,
            float max,
            MRIPictureOrientation orientation,
            IColorStrategy? colorStrategy = null)
        {
            _imageData = data;
            _max = max;
            _size = size;
            _orientation = orientation;
            _colorStrategy = colorStrategy ?? new DefaultColorStrategy();
            _bitmapSlices = BitmapUtilities.GetBitmapSlices(data, size, max, _colorStrategy);
        }

        private static async Task<float[][][]> ReadImageAsync(string path)
        {
            await PythonHelper.MoveTo();

            using (_ = Py.GIL())
            {
                dynamic os = Py.Import("os");
                dynamic sys = Py.Import("sys");

                dynamic loader = Py.Import(Path.GetFileNameWithoutExtension(_modelModulePath));

                return (float[][][])loader.load_image(path);
            }
        }

        public virtual MRIPicture RotatePicture(MRIPictureOrientation newOrientation)
        {
            if (_orientation == newOrientation) return this;

            (var newData, var newSize) = RotateImageData(newOrientation);

            return new MRIPicture(newData, newSize, _max, newOrientation);
        }

        protected (float[][][], int[]) RotateImageData(MRIPictureOrientation newOrientation)
        {
            int rotation = _orientation - newOrientation;
            rotation = rotation < 0 ? 3 + rotation : rotation;

            int[] newSize = RotateArray(_size, rotation);
            float[][][] newData = new float[newSize[0]][][];
            for (int i = 0; i < newSize[0]; i++)
            {
                newData[i] = new float[newSize[1]][];
                for (int j = 0; j < newSize[1]; j++)
                {
                    newData[i][j] = new float[newSize[2]];
                    for (int k = 0; k < newSize[2]; k++)
                    {
                        int[] indexes = RotateArray(new[] { i, j, k }, _size.Length - rotation);
                        newData[i][j][k] = _imageData[indexes[0]][indexes[1]][indexes[2]];
                    }
                }
            }

            return (newData, newSize);
        }

        private int[] RotateArray(int[] array, int rotation)
        {
            int[] newArray = new int[array.Length];
            for (int i = 0; i < array.Length - rotation; i++)
            {
                newArray[i] = array[i + rotation];
            }
            for (int i = array.Length - rotation; i < array.Length; i++)
            {
                newArray[i] = array[i + rotation - array.Length];
            }
            return newArray;
        }

        public Bitmap this[int index]
        {
            get => _bitmapSlices[index];
        }
    }
}
