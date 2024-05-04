using MRI_Vision.UI.Utils;
using System.Drawing;

namespace MRI_Vision.Domain.Picture;

internal static class BitmapUtilities
{
    public static Bitmap[] GetBitmapSlices(
        float[][][] imageData, int[] size, float max, IColorStrategy colorStrategy)
    {
        Bitmap[] slices = new Bitmap[imageData.Length];
        for (int i = 0; i < slices.Length; i++)
        {
            slices[i] = GetBitmapSlice(imageData, size, max, i, colorStrategy);
        }
        return slices;
    }


    private static Bitmap GetBitmapSlice(
        float[][][] imageData, int[] size, float max, int index, IColorStrategy colorStrategy)
    {
        float[][] slice = imageData[index];
        int width = size[1];
        int height = size[2];

        Bitmap bitmap = new Bitmap(width, height);

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                int grayScale = (int)(slice[width - 1 - i][height - 1 - j] * 255 / max);
                bitmap.SetPixel(i, j, colorStrategy.GetColorFromGrayScale(grayScale));
            }
        }

        return bitmap;
    }
}
