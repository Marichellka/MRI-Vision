using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Drawing;
using WpfAnimatedGif;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Forms;
using Microsoft.Win32;
using ScottPlot.Plottables;
using MRI_Vision.Domain.Picture;
using MRI_Vision.Domain.Model;

namespace MRI_Vision.UI.Pages
{
    /// <summary>
    /// Interaction logic for ResultsReviewPage.xaml
    /// </summary>
    public partial class ResultsReviewPage : Page
    {
        private Dictionary<MRIPictureOrientation, (MRIPicture, AnomalyPicture)> _pictures;
        private MRIPictureOrientation _currentOrientation;
        private ImageSource _loadingImageSource;

        public ResultsReviewPage(string filePath)
        {
            InitializeComponent();
            _loadingImageSource = ImageBehavior.GetAnimatedSource(UploadedImage);
            AnalyzeImageAsync(filePath);
        }

        private void GetPlot()
        {
            PlotImage.Plot.Clear();

            float[] ys = _pictures[_currentOrientation].Item2.AnomalyIndexes;
            int[] xs = Enumerable.Range(0, ys.Length).ToArray();
            var plot = PlotImage.Plot.Add.Scatter(xs, ys);
            plot.Color = ScottPlot.Color.FromHex("#961b1b");

            PlotImage.Plot.Add.HorizontalLine(0.01, color: ScottPlot.Color.FromHex("#5aa840"));
            PlotImage.Plot.Axes.AutoScale();
            PlotImage.Plot.Style.DarkMode();
            PlotImage.Refresh();
        }

        private async void AnalyzeImageAsync(string filePath)
        {
            var model = await ModelHelper.GetModelAsync();

            (var picture, var anomaly) = await model.AnalyzeImageAsync(filePath);

            _pictures = new();
            _pictures.Add(picture.Orientation, (picture, anomaly));
            _currentOrientation = picture.Orientation;
            SetScrollBar(0);
            SetSlice(0);
            PictureOrientationComboBox.SelectedItem = _currentOrientation;
            GetPlot();
            LoadingImage.Visibility = Visibility.Hidden;
        }

        protected void SetSlice(int index)
        {
            SetImage(_pictures[_currentOrientation].Item1[index], UploadedImage);
            SetImage(_pictures[_currentOrientation].Item2[index], AnomalyImage);
        }

        private void SetImage(Bitmap slice, System.Windows.Controls.Image image)
        {
            MemoryStream memory = new MemoryStream();
            slice.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
            memory.Position = 0;
            BitmapImage imageSource = new BitmapImage();
            imageSource.BeginInit();
            imageSource.StreamSource = memory;
            imageSource.CacheOption = BitmapCacheOption.OnLoad;
            imageSource.EndInit();
            ImageBehavior.SetAnimatedSource(image, imageSource);
        }

        private void SetScrollBar(int sliceInd) 
        {
            ImageScrollBar.Maximum = _pictures[_currentOrientation].Item1.Length - 1;
            ImageScrollBar.Value = sliceInd;
        }

        private void OnScroll(object sender, RoutedEventArgs e)
        {
            if(_pictures is null) return;

            int sliceIndex = (int)ImageScrollBar.Value;
            SetSlice(sliceIndex);
        }

        private void OrientationSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_pictures is null) return;

            MRIPictureOrientation orientation = (MRIPictureOrientation)PictureOrientationComboBox.SelectedItem;
            if (orientation == _currentOrientation)
                return;

            if (_pictures.ContainsKey(orientation))
            {
                _currentOrientation = orientation;
                int newInd = Math.Clamp((int)ImageScrollBar.Value, 0, _pictures[_currentOrientation].Item1.Length - 1);
                SetScrollBar(newInd);
                SetSlice(newInd);
                GetPlot();
            }
            else
            {
                ImageBehavior.SetAnimatedSource(UploadedImage, _loadingImageSource);
                SetImage(new Bitmap(1, 1), AnomalyImage);
                RotatePictureAsync(orientation);
            }
        }

        private async void RotatePictureAsync(MRIPictureOrientation orientation)
        {
            LoadingImage.Visibility = Visibility.Visible;
            var newImage = await Task.Run(() => _pictures[_currentOrientation].Item1.RotatePicture(orientation));
            var newAnomalyImage = await Task.Run(() => _pictures[_currentOrientation].Item2.RotatePicture(orientation));
            _pictures.Add(orientation, (newImage, newAnomalyImage));
            _currentOrientation = orientation;

            int newInd = Math.Clamp((int)ImageScrollBar.Value, 0, _pictures[_currentOrientation].Item1.Length - 1);
            SetScrollBar(newInd);
            SetSlice(newInd);
            GetPlot();
            LoadingImage.Visibility = Visibility.Hidden;
        }

        private void AnomalyMaskCheckBoxClick(object sender, RoutedEventArgs e)
        {
            if(AnomalyMaskCheckBox.IsChecked == false)
            {
                AnomalyImage.Visibility = Visibility.Hidden;
            }
            else
            {
                AnomalyImage.Visibility = Visibility.Visible;
            }
        }

        private void GeneralViewButtonClick(object sender, RoutedEventArgs e)
        {
            PlotImage.Visibility = Visibility.Visible;
            DetailsButton.Visibility = Visibility.Visible;
            ImageScrollBar.Visibility = Visibility.Hidden;
            GeneralViewButton.Visibility = Visibility.Hidden;
            AnomalyMaskCheckBox.Visibility = Visibility.Hidden;
        }

        private void DetailsButtonClick(object sender, RoutedEventArgs e)
        {
            PlotImage.Visibility = Visibility.Hidden;
            DetailsButton.Visibility = Visibility.Hidden;
            ImageScrollBar.Visibility = Visibility.Visible;
            GeneralViewButton.Visibility = Visibility.Visible;
            AnomalyMaskCheckBox.Visibility = Visibility.Visible;
        }

        private void ReuploadButtonClick(object sender, RoutedEventArgs e)
        {
            NavigationService!.Navigate(new UploadPage());
        }

        private void DownloadButtonClick(object sender, RoutedEventArgs e)
        {

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.DefaultExt = ".png";
            dialog.Filter = "PNG Image|*.png";

            bool result = dialog.ShowDialog() ?? false;
            if (result && !string.IsNullOrWhiteSpace(dialog.FileName))
            {
                if (PlotImage.Visibility == Visibility.Visible)
                {
                    PlotImage.Plot.SavePng(dialog.FileName,(int)PlotImage.ActualWidth,(int)PlotImage.ActualHeight);
                }
                else
                {
                    if (AnomalyMaskCheckBox.IsChecked == true)
                    {
                        var image = _pictures[_currentOrientation].Item1[(int)ImageScrollBar.Value];
                        var anomaly = _pictures[_currentOrientation].Item2[(int)ImageScrollBar.Value];
                        var merged = new Bitmap(image.Width, image.Height);
                        var oppacity = AnomalyImage.Opacity;
                        for (int i = 0; i< image.Width; i++)
                        {
                            for (int j = 0; j < image.Height; j++)
                            {
                                var anomalyPixel = anomaly.GetPixel(i, j);
                                var imagePixel = image.GetPixel(i, j);
                                var colors = new double[]{
                                    (anomalyPixel.R * oppacity * (anomalyPixel.A/255.0)) + imagePixel.R,
                                    (anomalyPixel.G * oppacity * (anomalyPixel.A/255.0)) + imagePixel.G,
                                    (anomalyPixel.B * oppacity * (anomalyPixel.A/255.0)) + imagePixel.B};
                                var max = colors.Max();
                                max = max > 255 ? max : 255;

                                var newColor = System.Drawing.Color.FromArgb(
                                    (int)(colors[0]/max*255),
                                    (int)(colors[1]/max*255),
                                    (int)(colors[2]/max*255));
                                merged.SetPixel(i, j, newColor);
                            }
                        }

                        merged.Save(dialog.FileName);
                    }
                    else
                    {
                        var image = _pictures[_currentOrientation].Item1[(int)ImageScrollBar.Value];
                        image.Save(dialog.FileName);
                    }
                }
            }
        }
    }
}
