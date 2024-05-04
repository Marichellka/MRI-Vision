using System.Drawing;

namespace MRI_Vision.UI.Utils;

public interface IColorStrategy
{
    Color GetColorFromGrayScale(int grayScale);
}
