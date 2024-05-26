using MRI_Vision.Domain.Picture.ColorStrategy;
using MRI_Vision.Python;
using Python.Runtime;
using System.Drawing;

namespace MRI_Vision.Domain.Picture
{
    /// <summary>
    /// Orientation of <see cref="MRIPicture"/>
    /// </summary>
    public enum MRIPictureOrientation
    {
        Front,
        Side,
        Top,
    }

    /// <summary>
    /// Contains all information and methods to present MRI picture.
    /// </summary>
    public class MRIPicture
    {
        /// <summary>
        /// Number of slices in picture
        /// </summary>
        public int Length => _imageData.Length;
        /// <summary>
        /// Orientation of picture
        /// </summary>
        public MRIPictureOrientation Orientation => _orientation;

        /// <summary>
        /// Image raw data
        /// </summary>
        protected float[][][] _imageData;
        /// <summary>
        /// max value in raw data
        /// </summary>
        protected float _max;
        /// <summary>
        /// Size (x, y, z) of 3D image data
        /// </summary>
        protected int[] _size;
        /// <summary>
        /// Array of <see cref="Bitmap"/> image slices
        /// </summary>
        protected Bitmap[] _bitmapSlices;
        /// <summary>
        /// Color strategy for image
        /// </summary>
        protected IColorStrategy _colorStrategy;
        /// <summary>
        /// Picture orientation
        /// </summary>
        protected MRIPictureOrientation _orientation;

        private const string _modelModulePath = @".\utils\image_helper.py";

        /// <summary>
        /// Create instance from reading file
        /// </summary>
        public MRIPicture(string path)
            : this(ReadImageAsync(path).Result)
        { }

        /// <summary>
        /// Create instance using given picture data
        /// </summary>
        public MRIPicture(float[][][] data, IColorStrategy? colorStrategy = null)
            : this(data,
                  [data.Length, data[0].Length, data[0][0].Length],
                  data.Max(x => x.Max(y => y.Max())),
                  MRIPictureOrientation.Side, colorStrategy)
        { }

        /// <summary>
        /// Main constructor
        /// </summary>
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

        /// <summary>
        /// Read picture data from file asynchronously
        /// </summary>
        private static async Task<float[][][]> ReadImageAsync(string path)
        {
            await PythonHelper.MoveTo();

            using (_ = Py.GIL())
            {
                dynamic os = Py.Import("os");
                dynamic sys = Py.Import("sys");
                sys.path.append(os.path.dirname(_modelModulePath));

                dynamic loader = Py.Import(Path.GetFileNameWithoutExtension(_modelModulePath));

                return (float[][][])loader.ImageHelper.load_image(path);
            }
        }

        /// <summary>
        /// Create new picture with given <see cref="MRIPictureOrientation"/>
        /// </summary>
        public virtual MRIPicture RotatePicture(MRIPictureOrientation newOrientation)
        {
            if (_orientation == newOrientation) return this;

            (var newData, var newSize) = RotateImageData(newOrientation);

            return new MRIPicture(newData, newSize, _max, newOrientation);
        }

        /// <summary>
        /// Rotate image data using given <see cref="MRIPictureOrientation"/>
        /// </summary>
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

        /// <summary>
        /// Rotate 1D array by given rotation
        /// </summary>
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
