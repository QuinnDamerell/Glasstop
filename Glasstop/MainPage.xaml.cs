using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Glasstop
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        static readonly Random s_random = new Random();

        public MainPage()
        {
            InitializeComponent();

            // Do this before we try to touch the history.
            Settings.TrimHistoryOnAppStart();

            // Load the current image if there is one, otherwise, load a new random image.
            LoadImage(Settings.CurrentImageId);
        }

        private void NextImage_Click(object sender, RoutedEventArgs e)
        {
            // Load the next image id, or if this returns null, the next random image.
            LoadImage(Settings.GetNextImageId(), saveAsNextImage: true);
        }

        private void LastImage_Click(object sender, RoutedEventArgs e)
        {
            // Load the next image id, or if this returns null, the last random image.
            LoadImage(Settings.GetLastImageId(), saveAsNextImage: false);
        }

        private void ShowErrorText(string msg)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                ui_ErrorHolder.Visibility = Visibility.Visible;
                ui_ErrorText.Text = msg;
            });
        }

        private void LoadImage(string id = null, bool saveAsNextImage = true)
        {
            // First, update the UI.
            DispatcherQueue.TryEnqueue(() =>
            {
                ui_ErrorHolder.Visibility = Visibility.Collapsed;
                ui_ImageLoadingRing.Visibility = Visibility.Visible;
                ui_NextImage.IsEnabled = false;
                ui_LastImage.IsEnabled = false;
            });

            // Ensure we aren't on the UI thread.
            Task.Run(async () =>
            {
                StorageFile f = null;
                try
                {
                    // First, get the image id we asked for or a new image.
                    bool isNewImage = false;
                    UnsplashPhotoContext c = null;
                    if(id == null)
                    {
                        // Pick one of the search queries at random.
                        string searchQuery = null;
                        var queries = Settings.SearchQueries.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        if(queries.Length > 0)
                        {
                            searchQuery = queries[s_random.Next(queries.Length)];
                            searchQuery = searchQuery.Trim();
                        }
                        if(searchQuery == null)
                        {
                            searchQuery = "wallpapers";
                        }
                        else
                        {
                            searchQuery = "wallpapers " + searchQuery;
                        }
                        searchQuery = "water, nature, mountain";

                        // Get the image.
                        c = await Unsplash.GetRandomImage(searchQuery);
                        isNewImage = true;
                    }
                    else
                    {
                        c = await Unsplash.GetImage(id);
                    }
                    if(c == null)
                    {
                        ShowErrorText("Failed get new image from Unsplash.");
                        return;
                    }

                    // Get the image file.
                    f = await Unsplash.GetImageFile(c);
                    if(f == null)
                    {
                        ShowErrorText("Failed download new image.");
                        return;
                    }

                    // If this was a new image, save it to our history.
                    if(isNewImage)
                    {
                        Settings.AddToImageIdHistory(c.Id, saveAsNextImage);
                    }

                    // Always set as the current.
                    Settings.CurrentImageId = c.Id;

                    // Show the image.
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        // ui_Image.Opacity = 0;
                        ui_Image.Source = new BitmapImage(new Uri(f.Path));
                    });
                }
                finally
                {
                    // Always hide the loading.
                    // We do this after we set the image source, but before we set the image, since that can take a long time.
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        // Don't mess with the error text, since it might have shown.
                        ui_ImageLoadingRing.Visibility = Visibility.Collapsed;
                        ui_NextImage.IsEnabled = true;
                        ui_LastImage.IsEnabled = true;
                    });
                }

                // Update the desktop wallpaper
                if(f != null)
                {
                    await ImageSetter.SetImage(f);
                }
            });
        }

        #region Image Settings UI

        private void ImageSettingsOpen_Click(object sender, RoutedEventArgs e)
        {
            // Populate the UI with the current settings.
            ui_imageSettingsSearchQueries.Text = Settings.SearchQueries;
            ui_imageSettings.Visibility = Visibility.Visible;
        }

        private void ImageSettingsClose_Click(object sender, RoutedEventArgs e)
        {
            Settings.SearchQueries = ui_imageSettingsSearchQueries.Text;
            ui_imageSettings.Visibility = Visibility.Collapsed;
        }

        private void ImageRatioButton_Click(object sender, RoutedEventArgs e)
        {
         
        }

        



        #endregion
    }
}
