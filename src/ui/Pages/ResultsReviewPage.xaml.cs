using MRI_Vision.UI.Utils;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Drawing;
using WpfAnimatedGif;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Media3D;

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
            PlotImage.Visibility = Visibility.Visible;
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

        private void PictureOrientationComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
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
            var newImage = await Task.Run(() => _pictures[_currentOrientation].Item1.RotatePicture(orientation));
            var newAnomalyImage = await Task.Run(() => _pictures[_currentOrientation].Item2.RotatePicture(orientation));
            _pictures.Add(orientation, (newImage, newAnomalyImage));
            _currentOrientation = orientation;

            int newInd = Math.Clamp((int)ImageScrollBar.Value, 0, _pictures[_currentOrientation].Item1.Length - 1);
            SetScrollBar(newInd);
            SetSlice(newInd);
            GetPlot();
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
    }
}
