using System.Drawing;

namespace MRI_Vision.Domain.Picture.ColorStrategy;

internal class AnomalyColorStrategy(Color? shift = null) : IColorStrategy
{
    public Color Shift { get; } = shift ?? Color.DarkRed;

    public Color GetColorFromGrayScale(int grayScale)
    {
        return Color.FromArgb(
            grayScale,
            (int)(grayScale * (Shift.R / 255.0)),
            (int)(grayScale * (Shift.G / 255.0)),
            (int)(grayScale * (Shift.B / 255.0)));
    }
}
