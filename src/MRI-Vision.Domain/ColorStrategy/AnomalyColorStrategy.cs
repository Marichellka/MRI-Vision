using System.Drawing;

namespace MRI_Vision.UI.Utils;

internal class AnomalyColorStrategy(Color? shift = null) : IColorStrategy
{
    public Color Shift { get; } = shift ?? Color.DarkRed;

    public Color GetColorFromGrayScale(int grayScale)
    {
        return Color.FromArgb(
            grayScale,
            grayScale * (Shift.R / 255),
            grayScale * (Shift.G / 255),
            grayScale * (Shift.B / 255));
    }
}
