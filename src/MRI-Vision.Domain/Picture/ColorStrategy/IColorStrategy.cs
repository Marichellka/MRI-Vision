using System.Drawing;

namespace MRI_Vision.Domain.Picture.ColorStrategy;

public interface IColorStrategy
{
    Color GetColorFromGrayScale(int grayScale);
}
