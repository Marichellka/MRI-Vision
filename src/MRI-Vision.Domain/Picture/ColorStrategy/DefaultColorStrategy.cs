using System.Drawing;

namespace MRI_Vision.Domain.Picture.ColorStrategy;

/// <summary>
/// <inheritdoc cref="IColorStrategy"/>. 
/// Default color strategy for <see cref="MRIPicture"/>
/// </summary>
internal class DefaultColorStrategy : IColorStrategy
{
    public Color GetColorFromGrayScale(int grayScale)
    {
        return Color.FromArgb(grayScale, grayScale, grayScale);
    }
}
