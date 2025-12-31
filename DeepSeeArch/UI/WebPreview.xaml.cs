using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using Serilog;

namespace DeepSeeArch.UI
{
    public partial class WebPreview : Window
    {
        private string _initialUrl;

        public WebPreview(string url)
        {
            InitializeComponent();
            _initialUrl = url;
            
            Loaded += WebPreview_Loaded;
        }

        private async void WebPreview_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // WebView2 initialisieren
                await WebView.EnsureCoreWebView2Async();

                // URL anzeigen
                UrlTextBox.Text = _initialUrl;
                var uri = new Uri(_initialUrl);
                DomainText.Text = uri.Host;

                // Seite laden
                WebView.CoreWebView2.Navigate(_initialUrl);

                Log.Information("WebPreview loaded for {Url}", _initialUrl);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error initializing WebPreview");
                MessageBox.Show($"Fehler beim Laden der Webseite: {ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void WebView_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            UrlTextBox.Text = e.Uri;
            
            try
            {
                var uri = new Uri(e.Uri);
                DomainText.Text = uri.Host;
            }
            catch { }
        }

        private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;

            if (!e.IsSuccess)
            {
                MessageBox.Show($"Fehler beim Laden der Seite.\nFehlercode: {e.WebErrorStatus}", 
                    "Ladefehler", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (WebView.CoreWebView2?.CanGoBack == true)
            {
                WebView.CoreWebView2.GoBack();
            }
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            if (WebView.CoreWebView2?.CanGoForward == true)
            {
                WebView.CoreWebView2.GoForward();
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            WebView.CoreWebView2?.Reload();
        }

        private void UrlTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var url = UrlTextBox.Text;
                if (!url.StartsWith("http"))
                {
                    url = "https://" + url;
                }
                WebView.CoreWebView2?.Navigate(url);
            }
        }

        private void OpenExternal_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var url = WebView.CoreWebView2?.Source ?? _initialUrl;
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error opening URL in external browser");
                MessageBox.Show($"Fehler beim Öffnen: {ex.Message}", "Fehler", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Screenshot_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var screenshotsPath = Path.Combine(documentsPath, "DeepSeeArch", "Screenshots");
                Directory.CreateDirectory(screenshotsPath);

                var fileName = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                var filePath = Path.Combine(screenshotsPath, fileName);

                // Screenshot erstellen (nur mit CoreWebView2)
                if (WebView.CoreWebView2 != null)
                {
                    using var stream = File.Create(filePath);
                    await WebView.CoreWebView2.CapturePreviewAsync(
                        CoreWebView2CapturePreviewImageFormat.Png, 
                        stream
                    );

                    Log.Information("Screenshot saved to {Path}", filePath);
                    MessageBox.Show($"Screenshot gespeichert:\n{filePath}", 
                        "Screenshot erstellt", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating screenshot");
                MessageBox.Show($"Fehler beim Erstellen des Screenshots: {ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
