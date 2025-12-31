// DeepSeeArch/UI/MediaViewer.xaml.cs
using System;
using System.IO;
using System.Windows;

namespace DeepSeeArch.UI
{
    public partial class MediaViewer : Window
    {
        private Uri? _sourceUri;
        private bool _isWeb;

        public MediaViewer(string source)
        {
            InitializeComponent();
            LoadSource(source);
        }

        public void LoadSource(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                ShowHints(media: true, web: true);
                SourceText.Text = "";
                return;
            }

            SourceText.Text = source.Trim();

            // Determine Uri
            if (Uri.TryCreate(source, UriKind.Absolute, out var uri))
            {
                _sourceUri = uri;
            }
            else
            {
                // Treat as local path
                var full = Path.GetFullPath(source);
                _sourceUri = new Uri(full, UriKind.Absolute);
            }

            // Decide if web
            _isWeb = _sourceUri.Scheme == Uri.UriSchemeHttp || _sourceUri.Scheme == Uri.UriSchemeHttps;

            if (_isWeb)
            {
                Tabs.SelectedIndex = 1;
                ShowHints(media: true, web: false);
                TryNavigateWeb(_sourceUri);
            }
            else
            {
                Tabs.SelectedIndex = 0;
                ShowHints(media: false, web: true);
                TryLoadMedia(_sourceUri);
            }
        }

        private void TryLoadMedia(Uri uri)
        {
            try
            {
                Player.Stop();
                Player.Source = uri;
                Player.Play();
            }
            catch
            {
                ShowHints(media: true, web: true);
            }
        }

        private async void TryNavigateWeb(Uri uri)
        {
            try
            {
                // WebView2 braucht Core initialisiert
                await Web.EnsureCoreWebView2Async();
                Web.CoreWebView2.Navigate(uri.ToString());
            }
            catch
            {
                ShowHints(media: true, web: true);
            }
        }

        private void ShowHints(bool media, bool web)
        {
            MediaHint.Visibility = media ? Visibility.Visible : Visibility.Collapsed;
            WebHint.Visibility = web ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnPlayClick(object sender, RoutedEventArgs e)
        {
            if (_sourceUri == null) return;

            if (_isWeb)
            {
                TryNavigateWeb(_sourceUri);
            }
            else
            {
                Player.Play();
            }
        }

        private void OnPauseClick(object sender, RoutedEventArgs e)
        {
            if (_isWeb)
            {
                // Web hat kein Pause im selben Sinn
                return;
            }

            Player.Pause();
        }

        private void OnStopClick(object sender, RoutedEventArgs e)
        {
            if (_isWeb)
            {
                try
                {
                    Web?.CoreWebView2?.Stop();
                }
                catch { }
                return;
            }

            Player.Stop();
        }
    }
}
