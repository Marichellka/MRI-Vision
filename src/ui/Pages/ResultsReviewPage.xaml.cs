using MRI_Vision.UI.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;
using WpfAnimatedGif;
using System.IO;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace MRI_Vision.UI.Pages
{
    /// <summary>
    /// Interaction logic for ResultsReviewPage.xaml
    /// </summary>
    public partial class ResultsReviewPage : Page
    {
        private Dictionary<MRIPictureOrientation, (MRIPicture, MRIPicture)> _pictures;
        private MRIPictureOrientation _currentOrientation;

        public ResultsReviewPage(string filePath)
        {
            InitializeComponent();
            AnalyzeImageAsync(filePath);
        }

        private async void AnalyzeImageAsync(string filePath)
        {
            var model = await Task.Run(() => new Model());

            (var picture, var anomaly) = await Task.Run(() => model.AnalyzeImage(filePath));

            _pictures = new();
            _pictures.Add(picture.Orientation, (picture, anomaly));
            _currentOrientation = picture.Orientation;
            SetScrollBar(0);
            PictureOrientationComboBox.SelectedItem = _currentOrientation;
        }

        private void SetSliceImage(Bitmap slice, System.Windows.Controls.Image image)
        {
            MemoryStream memory = new MemoryStream();
            slice.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
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
            ImageScrollBar.Maximum = _pictures[_currentOrientation].Item1.Length - 1;
            ImageScrollBar.Value = sliceInd;
            SetSliceImage(_pictures[_currentOrientation].Item1[sliceInd], UploadedImage);
            SetSliceImage(_pictures[_currentOrientation].Item2[sliceInd], AnomalyImage);
        }

        private void OnScroll(object sender, RoutedEventArgs e)
        {
            if(_pictures is null) return;

            int sliceIndex = (int)ImageScrollBar.Value;
            SetSliceImage(_pictures[_currentOrientation].Item1[sliceIndex], UploadedImage);
            SetSliceImage(_pictures[_currentOrientation].Item2[sliceIndex], AnomalyImage);
        }

        private void PictureOrientationComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_pictures is null) return;

            MRIPictureOrientation orientation = (MRIPictureOrientation) PictureOrientationComboBox.SelectedItem;
            if (orientation == _currentOrientation)
                return;
            
            if(_pictures.ContainsKey(orientation))
            {
                _currentOrientation = orientation;
            }
            else
            {
                var newImage = _pictures[_currentOrientation].Item1.RotatePicture(orientation);
                var newAnomaly = _pictures[_currentOrientation].Item2.RotatePicture(orientation);
                _pictures.Add(orientation, (newImage, newAnomaly));
                _currentOrientation = orientation;
            }

            int newInd = Math.Clamp((int)ImageScrollBar.Value, 0, _pictures[_currentOrientation].Item1.Length);
            SetScrollBar(newInd);
        }

        private void AnomalyMaskToggleButtonClick(object sender, RoutedEventArgs e)
        {
            if(AnomalyMaskToggleButton.IsChecked == false)
            {
                AnomalyImage.Visibility = Visibility.Collapsed;
            }
            else
            {
                AnomalyImage.Visibility = Visibility.Visible;
            }
        }
    }
}
