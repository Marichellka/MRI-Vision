using Python.Runtime;
using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace MRI_Vision.UI.Utils
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

        private float[][][] _imageData;
        private Bitmap[] _bitmapSlices;
        private float _max;
        private MRIPictureOrientation _orientation;
        private int[] _size;
        private Color _colorShift;
        private const string _modelModulePath = @"C:\Users\maric\Studying\Diploma\Project\MRI-Vision\src\network\model\utils\loader.py";

        public MRIPicture(string path)
        {
            _imageData = ReadImage(path);
            _max = _imageData.Max(x => x.Max(y => y.Max()));
            _size = new[] { _imageData.Length, _imageData[0].Length, _imageData[0][0].Length };
            _orientation = MRIPictureOrientation.Side;
            _colorShift = Color.FromArgb(255, 255, 255);
            _bitmapSlices = GetBitmapSlices();
        }

        public MRIPicture(float[][][] data, Color? shift = null)
        {
            _imageData = data;
            _max = _imageData.Max(x => x.Max(y => y.Max()));
            _size = new[] { _imageData.Length, _imageData[0].Length, _imageData[0][0].Length };
            _orientation = MRIPictureOrientation.Side;
            _colorShift = shift ?? Color.FromArgb(255, 255, 255);
            _bitmapSlices = GetBitmapSlices();
        }

        public MRIPicture(
            float[][][] data, int[] size, float max, 
            MRIPictureOrientation orientation, Color? shift = null)
        {
            _imageData = data;
            _max = max;
            _size = size;
            _orientation = orientation;
            _colorShift = shift ?? Color.FromArgb(255, 255, 255);
            _bitmapSlices = GetBitmapSlices();
        }

        private float[][][] ReadImage(string path)
        {
            using (_ = Py.GIL())
            {
                dynamic os = Py.Import("os");
                dynamic sys = Py.Import("sys");
                sys.path.append(os.path.dirname(_modelModulePath));

                dynamic loader = Py.Import(Path.GetFileNameWithoutExtension(_modelModulePath));

                return (float[][][])loader.load_image(path);
            }
        }

        private Bitmap[] GetBitmapSlices()
        {
            Bitmap[] slices = new Bitmap[_imageData.Length];
            for (int i = 0; i < slices.Length; i++)
            {
                slices[i] = GetBitmapSlice(i);
            }
            return slices;
        }


        private Bitmap GetBitmapSlice(int index)
        {
            float[][] slice = _imageData[index];
            Bitmap bitmap = new Bitmap(_size[1], _size[2]);
            for (int i = 0; i < _size[1]; i++)
            {
                for (int j = 0; j < _size[2]; j++)
                {
                    int grayScale = (int)(slice[i][j] * 255 / _max);
                    Color pixelColor = Color.FromArgb(
                        grayScale*(_colorShift.R/255), grayScale*(_colorShift.G / 255), grayScale*(_colorShift.B / 255));
                    bitmap.SetPixel(i, j, pixelColor);
                }
            }

            return bitmap;
        }

        public MRIPicture RotatePicture(MRIPictureOrientation newOrientation)
        {
            if (_orientation == newOrientation) return this;

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
                        int[] indexes = RotateArray(new[] {i, j, k}, _size.Length-rotation);
                        newData[i][j][k] = _imageData[indexes[0]][indexes[1]][indexes[2]];
                    }
                }
            }

            return new MRIPicture(newData, newSize, _max, newOrientation, _colorShift);
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
