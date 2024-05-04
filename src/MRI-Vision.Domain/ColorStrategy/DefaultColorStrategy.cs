using System.Drawing;

namespace MRI_Vision.UI.Utils;

internal class DefaultColorStrategy : IColorStrategy
{
    public Color GetColorFromGrayScale(int grayScale)
    {
        return Color.FromArgb(grayScale, grayScale, grayScale);
    }
}
