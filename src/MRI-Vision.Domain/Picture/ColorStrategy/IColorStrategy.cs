using System.Drawing;

namespace MRI_Vision.Domain.Picture.ColorStrategy;

/// <summary>
/// Represents strategy for coverting grayscale to <see cref="Color"/>
/// </summary>
public interface IColorStrategy
{
    /// <summary>
    /// Get <see cref="Color"/> from grayscale
    /// </summary>
    /// <param name="grayScale"></param>
    /// <returns>Converted <see cref="Color"/></returns>
    Color GetColorFromGrayScale(int grayScale);
}
