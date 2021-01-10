using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Windows.Controls;

namespace QuickLook.Plugin.HtmlViewer
{
    /// <summary>
    /// Interaction logic for BrowserPanel.xaml
    /// </summary>
    public partial class BrowserPanel : UserControl
    {
        private Uri _currentUri;

        private static string svgWrapperBegin =
            @"<html>
<head>  
   <meta charset=""utf-8"">
   <script src=""https://cdn.jsdelivr.net/npm/svg-pan-zoom@3.5.0/dist/svg-pan-zoom.min.js""></script>
   <script>
      window.onload = function() {
        window.zoomTiger = svgPanZoom('svg', {zoomScaleSensitivity: 0.5});
      };
    </script>
    <style>SVG {width: 100%; height: 100%;}</style>
</head>
<body style=""margin: 0"">
";
        private static string svgWrapperEnd = "</body></html>";

        public BrowserPanel()
        {
            InitializeComponent();
        }

        public async void LoadFile(string path)
        {
            if (path.ToLower().EndsWith(".svg"))
            {
                await webView.EnsureCoreWebView2Async();
                string svgText = File.ReadAllText(path);
                string htmlContent = $@"{svgWrapperBegin}{svgText}{svgWrapperEnd}";
                // File.WriteAllText(path + ".html, htmlContent);
                webView.NavigateToString(htmlContent);
            }
            else
            {
                if (Path.IsPathRooted(path))
                    path = Helper.FilePathToFileUrl(path);
                Navigate(path);
            }
        }

        public void Navigate(string uri)
        {
            if (!string.IsNullOrWhiteSpace(uri) && Uri.IsWellFormedUriString(uri, UriKind.Absolute))
                try
                {
                    webView.Source = new Uri(uri);
                }
                catch (UriFormatException)
                {
                    // just don't crash because of a malformed url
                }
            else
                webView.Source = null;

            _currentUri = webView.Source;
        }

        public void Dispose()
        {
            webView.Dispose();
        }

        void NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (e.Uri.StartsWith("data:text")) return; // arriving via NavigateToString

            var newUri = new Uri(e.Uri);
            if (newUri != _currentUri)
            {
                e.Cancel = true;
            }
        }
    }
}
