using System;
using System.IO;
using System.Net.Http;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace DeepSeeArch.UI
{
    public partial class MediaViewer : Window
    {
        private string _url;

        public MediaViewer(string url)
        {
            InitializeComponent();
            _url = url;
            Loaded += async (s, e) => await LoadMediaAsync();
        }

        private async System.Threading.Tasks.Task LoadMediaAsync()
        {
            if (_url.EndsWith(".jpg") || _url.EndsWith(".png") || _url.EndsWith(".gif"))
            {
                using var client = new HttpClient();
                var bytes = await client.GetByteArrayAsync(_url);
                using var ms = new MemoryStream(bytes);
                var bmp = new System.Windows.Media.Imaging.BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bmp.StreamSource = ms;
                bmp.EndInit();
                ImageControl.Source = bmp;
                ImageContainer.Visibility = Visibility.Visible;
            }
            else
            {
                await VideoWebView.EnsureCoreWebView2Async();
                VideoWebView.CoreWebView2.Navigate(_url);
                VideoWebView.Visibility = Visibility.Visible;
            }
        }
    }
}
EOF
cat /tmp/MediaViewer.xaml.cs
Ausgabe

using System;
using System.IO;
using System.Net.Http;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace DeepSeeArch.UI
{
    public partial class MediaViewer : Window
    {
        private string _url;

        public MediaViewer(string url)
        {
            InitializeComponent();
            _url = url;
            Loaded += async (s, e) => await LoadMediaAsync();
        }

        private async System.Threading.Tasks.Task LoadMediaAsync()
        {
            if (_url.EndsWith(".jpg") || _url.EndsWith(".png") || _url.EndsWith(".gif"))
            {
                using var client = new HttpClient();
                var bytes = await client.GetByteArrayAsync(_url);
                using var ms = new MemoryStream(bytes);
                var bmp = new System.Windows.Media.Imaging.BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bmp.StreamSource = ms;
                bmp.EndInit();
                ImageControl.Source = bmp;
                ImageContainer.Visibility = Visibility.Visible;
            }
            else
            {
                await VideoWebView.EnsureCoreWebView2Async();
                VideoWebView.CoreWebView2.Navigate(_url);
                VideoWebView.Visibility = Visibility.Visible;
            }
        }
    }
}
✅ FERTIG! Kopiere di