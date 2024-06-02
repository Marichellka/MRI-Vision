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
    /// <inheritdoc cref="Page"/>
    /// Interaction logic for ImageReviewPage.xaml
    /// </summary>
    public partial class ImageReviewPage : Page
    {
        private Dictionary<MRIPictureOrientation, MRIPicture> _pictures;
        private MRIPictureOrientation _currentOrientation;
        private string _filePath;
        private ImageSource _loadingImageSource;

        /// <summary>
        /// Initialize <see cref="MRIPicture"/> review page
        /// </summary>
        public ImageReviewPage(string filePath)
        {
            InitializeComponent();
            _loadingImageSource = ImageBehavior.GetAnimatedSource(UploadedImage);
            _filePath = filePath;
            _pictures = new();
            LoadImageAsync();
        }

        /// <summary>
        /// Load <see cref="MRIPicture"/> asynchronously and visualize in page
        /// </summary>
        private async void LoadImageAsync()
        {
            var MRIPicture = await Task.Run(() => new MRIPicture(_filePath));
            _pictures.Add(MRIPicture.Orientation, MRIPicture);
            _currentOrientation = MRIPicture.Orientation;
            SetScrollBar(0);
            SetSlice(0);
            PictureOrientationComboBox.SelectedItem = _currentOrientation;
        }

        /// <summary>
        /// Set <see cref="Bitmap"/> slice of <see cref="MRIPicture"/>
        /// </summary>
        protected void SetSlice(int index)
        {
            SetImage(_pictures[_currentOrientation][index], UploadedImage);
        }

        /// <summary>
        /// Set <see cref="Bitmap"/> image to <see cref="Image"/>
        /// </summary>
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

        /// <summary>
        /// Set scroll bar to the <see cref="MRIPicture"/> slice index
        /// </summary>
        private void SetScrollBar(int sliceInd)
        {
            ImageScrollBar.Maximum = _pictures[_currentOrientation].Length - 1;
            ImageScrollBar.Value = sliceInd;
        }

        /// <summary>
        /// Process event on bar scroll
        /// </summary>
        private void OnScroll(object sender, RoutedEventArgs e)
        {
            if(_pictures is null) return;

            int sliceIndex = (int)ImageScrollBar.Value;
            SetSlice(sliceIndex);
        }

        /// <summary>
        /// Process event on mouse scroll
        /// </summary>
        private void DockPanel_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            int ind = (int)ImageScrollBar.Value;
            ind = e.Delta < 0 ? ind - 1 : ind + 1;
            ind = Math.Clamp(0, ind, (int)ImageScrollBar.Maximum);
            SetSlice(ind);
            SetScrollBar(ind);
        }

        /// <summary>
        /// Change <see cref="MRIPicture"/> orientation
        /// </summary>
        private void OrientationSelectionChanged(object sender, SelectionChangedEventArgs e)
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

        /// <summary>
        /// Rotate <see cref="MRIPicture"/> asynchronously
        /// </summary>
        private async void RotatePictureAsync(MRIPictureOrientation orientation)
        {
            var newImage = await Task.Run(() => _pictures[_currentOrientation].RotatePicture(orientation));
            _pictures.Add(orientation, newImage);
            _currentOrientation = orientation;

            int newInd = Math.Clamp((int)ImageScrollBar.Value, 0, _pictures[_currentOrientation].Length - 1);
            SetScrollBar(newInd);
            SetSlice(newInd);
        }

        /// <summary>
        /// Naviagate to <see cref="ResultsReviewPage"/>
        /// </summary>
        private void AnalyzeButtonClick(object sender, RoutedEventArgs e)
        {
            NavigationService!.Navigate(new ResultsReviewPage(_filePath));
        }

        /// <summary>
        /// Navigate to <see cref="UploadPage"/>
        /// </summary>
        private void ReuploadButtonClick(object sender, RoutedEventArgs e)
        {
            NavigationService!.Navigate(new UploadPage());
        }
    }

    /// <summary>
    /// <inheritdoc cref="ObservableCollection{T}"/>
    /// </summary>
    class PictureOrientation: ObservableCollection<MRIPictureOrientation>
    {
        public PictureOrientation()
        {
            Add(MRIPictureOrientation.Side);
            Add(MRIPictureOrientation.Front);
            Add(MRIPictureOrientation.Top);
        }
    }
}
