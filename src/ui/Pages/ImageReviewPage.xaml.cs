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
    /// Interaction logic for ImageReviewPage.xaml
    /// </summary>
    public partial class ImageReviewPage : Page
    {
        private Dictionary<MRIPictureOrientation, MRIPicture> _pictures;
        private MRIPictureOrientation _currentOrientation;

        public ImageReviewPage(string filePath)
        {
            InitializeComponent();
            LoadImageAsync(filePath);
        }

        private async void LoadImageAsync(string filePath)
        {
            var MRIPicture = await Task.Run(() => new MRIPicture(filePath));
            _pictures = new();
            _pictures.Add(MRIPicture.Orientation, MRIPicture);
            _currentOrientation = MRIPicture.Orientation;
            SetScrollBar(0);
            PictureOrientationComboBox.SelectedItem = _currentOrientation;
        }

        private void SetSliceImage(Bitmap slice)
        {
            MemoryStream memory = new MemoryStream();
            slice.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
            memory.Position = 0;
            BitmapImage imageSource = new BitmapImage();
            imageSource.BeginInit();
            imageSource.StreamSource = memory;
            imageSource.CacheOption = BitmapCacheOption.OnLoad;
            imageSource.EndInit();
            ImageBehavior.SetAnimatedSource(UploadedImage, imageSource);
        }

        private void SetScrollBar(int sliceInd) // TODO: rename
        {
            ImageScrollBar.Maximum = _pictures[_currentOrientation].Length - 1;
            ImageScrollBar.Value = sliceInd;
            SetSliceImage(_pictures[_currentOrientation][sliceInd]);
        }

        private void OnScroll(object sender, RoutedEventArgs e)
        {
            if(_pictures is null) return;

            int sliceIndex = (int)ImageScrollBar.Value;
            SetSliceImage(_pictures[_currentOrientation][sliceIndex]);
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
                var newImage = _pictures[_currentOrientation].RotatePicture(orientation);
                _pictures.Add(orientation, newImage);
                _currentOrientation = orientation;
            }

            int newInd = Math.Clamp((int)ImageScrollBar.Value, 0, _pictures[_currentOrientation].Length);
            SetScrollBar(newInd);
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
