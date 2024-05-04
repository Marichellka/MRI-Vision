using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Drawing;
using WpfAnimatedGif;
using System.IO;
using System.Collections.ObjectModel;
using MRI_Vision.Domain.Picture;

namespace MRI_Vision.UI.Pages
{
    /// <summary>
    /// Interaction logic for ImageReviewPage.xaml
    /// </summary>
    public partial class ImageReviewPage : Page
    {
        private Dictionary<MRIPictureOrientation, MRIPicture> _pictures;
        private MRIPictureOrientation _currentOrientation;
        private string _filePath;
        private ImageSource _loadingImageSource;

        public ImageReviewPage(string filePath)
        {
            InitializeComponent();
            _loadingImageSource = ImageBehavior.GetAnimatedSource(UploadedImage);
            _filePath = filePath;
            LoadImageAsync();
        }

        private async void LoadImageAsync()
        {
            var MRIPicture = await Task.Run(() => new MRIPicture(_filePath));
            _pictures = new();
            _pictures.Add(MRIPicture.Orientation, MRIPicture);
            _currentOrientation = MRIPicture.Orientation;
            SetScrollBar(0);
            SetSlice(0);
            PictureOrientationComboBox.SelectedItem = _currentOrientation;
        }

        protected void SetSlice(int index)
        {
            SetImage(_pictures[_currentOrientation][index], UploadedImage);
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

        private void SetScrollBar(int sliceInd) // TODO: rename
        {
            ImageScrollBar.Maximum = _pictures[_currentOrientation].Length - 1;
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

            MRIPictureOrientation orientation = (MRIPictureOrientation) PictureOrientationComboBox.SelectedItem;
            if (orientation == _currentOrientation)
                return;

            if (_pictures.ContainsKey(orientation))
            {
                _currentOrientation = orientation;
                int newInd = Math.Clamp((int)ImageScrollBar.Value, 0, _pictures[_currentOrientation].Length - 1);
                SetScrollBar(newInd);
                SetSlice(newInd);
            }
            else
            {
                ImageBehavior.SetAnimatedSource(UploadedImage, _loadingImageSource);
                RotatePictureAsync(orientation);
            }
        }

        private async void RotatePictureAsync(MRIPictureOrientation orientation)
        {
            var newImage = await Task.Run(() => _pictures[_currentOrientation].RotatePicture(orientation));
            _pictures.Add(orientation, newImage);
            _currentOrientation = orientation;

            int newInd = Math.Clamp((int)ImageScrollBar.Value, 0, _pictures[_currentOrientation].Length - 1);
            SetScrollBar(newInd);
            SetSlice(newInd);
        }

        private void AnalyzeButtonClick(object sender, RoutedEventArgs e)
        {
            NavigationService!.Navigate(new ResultsReviewPage(_filePath));
        }

        private void ReuploadButtonClick(object sender, RoutedEventArgs e)
        {
            NavigationService!.Navigate(new UploadPage());
        }
    }

    class PictureOrientation: ObservableCollection<MRIPictureOrientation>
    {
        public PictureOrientation()
        {
            Add(MRIPictureOrientation.Front);
            Add(MRIPictureOrientation.Side);
            Add(MRIPictureOrientation.Top);
        }
    }
}
