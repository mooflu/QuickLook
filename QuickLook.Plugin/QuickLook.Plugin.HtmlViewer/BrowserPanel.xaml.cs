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

        public BrowserPanel()
        {
            InitializeComponent();
        }

        public void LoadFile(string path)
        {
            if (Path.IsPathRooted(path))
                path = Helper.FilePathToFileUrl(path);  

            Navigate(path);
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
            var newUri = new Uri(e.Uri);
            if (newUri != _currentUri)
            {
                e.Cancel = true;
            }
        }
    }
}
