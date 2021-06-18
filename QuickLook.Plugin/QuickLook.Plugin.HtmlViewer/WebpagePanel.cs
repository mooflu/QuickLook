// Copyright © 2021 Paddy Xu and Frank Becker
// 
// This file is part of QuickLook program.
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using QuickLook.Common.Helpers;

namespace QuickLook.Plugin.HtmlViewer
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class FileProps
    {
        public string Name { get; set; } = "";
        public long Size { get; set; } = 0;
        public string Contents { get; set; }
        public byte[] BinContents { get; set; }
        public string extensions { get; set; }

        public void reset()
        {
            Name = "";
            Size = 0;
            Contents = null;
            BinContents = null;
        }
    }

    public class WebpagePanel : UserControl
    {
        private Uri _currentUri;
        private WebView2 _webView;
        private FileProps _fileProps = new FileProps();
        private string nextPath = null;

        public string[] extensions { get { return _fileProps.extensions.Split(','); } }

        private static readonly IntPtr HWND_MESSAGE = new IntPtr(-3);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CreateWindowEx(
           uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle, int x, int y,
           int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        private static readonly string[] BinExtensions = "pdf,xlsx,gltf,glb,webp".Split(',');

        public WebpagePanel()
        {
            if (!Helper.IsWebView2Available())
            {
                Content = CreateDownloadButton();
            }
            else
            {
                var userFolder = Path.Combine(App.LocalDataPath, @"WebView2_Data\");
                _webView = new WebView2
                {
                    CreationProperties = new CoreWebView2CreationProperties
                    {
                        UserDataFolder = userFolder
                    }
                };
                _webView.NavigationStarting += NavigationStarting_CancelNavigation;
                _webView.NavigationCompleted += NavigationCompleted;
                _webView.CoreWebView2InitializationCompleted += CoreWebView2InitializationCompleted;
                _webView.EnsureCoreWebView2Async();

                /*
                var opts = new CoreWebView2EnvironmentOptions("--disable-web-security --allow-file-access-from-files --allow-file-access");
                CoreWebView2Environment.CreateAsync(options: opts).ContinueWith(_ => Dispatcher.Invoke(() =>
                {
                    _webView.EnsureCoreWebView2Async(_.Result);
                }));
                */

                /*
                var ver = CoreWebView2Environment.GetAvailableBrowserVersionString();
                // var appFolder = @"C:\Program Files (x86)\Microsoft\Edge Beta\Application\89.0.774.27";
                CoreWebView2Environment.CreateAsync(userDataFolder: userFolder).ContinueWith(_ => Dispatcher.Invoke(() =>
                {
                    IntPtr hiddenwin = CreateWindowEx(0, "STATIC", null, 0, 0, 0, 0, 0,
                        HWND_MESSAGE, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

                    // var version = _.Result.BrowserVersionString;
                    _.Result.CreateCoreWebView2ControllerAsync(hiddenwin).ContinueWith(task2 => Dispatcher.Invoke(() =>
                    {
                        var wvController = task2.Result;
                        wvController.CoreWebView2.Navigate(Helper.FilePathToFileUrl(webviewPlus).ToString());
                        wvController.CoreWebView2.ExecuteScriptAsync("console.log('GOT HERE');");
                        wvController.CoreWebView2.NavigationCompleted += NavigationCompleted;
                    }));
                }));
                CoreWebView2Environment.CreateAsync(userDataFolder: userFolder).ContinueWith(_ => Dispatcher.Invoke(() =>
                {
                    _webView.EnsureCoreWebView2Async(_.Result);
                }));
                */

                Content = _webView;
            }
        }

        public void NavigateToFile(string path)
        {
            if (_webView.CoreWebView2 == null)
            {
                nextPath = path;
                return;
            }
            var uri = Path.IsPathRooted(path) ? Helper.FilePathToFileUrl(path) : new Uri(path);
            var fileInfo = new FileInfo(path);
            _fileProps.Name = fileInfo.Name;
            _fileProps.Size = fileInfo.Length;

            if (BinExtensions.Any(path.ToLower().EndsWith))
            {
                _fileProps.Contents = null;
                _fileProps.BinContents = File.ReadAllBytes(path);
            }
            else
            {
                _fileProps.Contents = File.ReadAllText(path);
                _fileProps.BinContents = null;
            }

            _webView.CoreWebView2.ExecuteScriptAsync($"window.WebviewPlus.openFile('{fileInfo.Name}','{fileInfo.Length}')");

            // NavigateToUri(uri);
        }

        public void NavigateToHtml(string html)
        {
            // markdown handled via webviewplus
            /*
            _fileProps.reset();

            _webView.EnsureCoreWebView2Async()
                .ContinueWith(_ => Dispatcher.Invoke(() => _webView.NavigateToString(html)));
            */
        }

        private void CoreWebView2InitializationCompleted(object sender, EventArgs e)
        {
            _fileProps.reset();

            var webviewPlus = Path.Combine(App.LocalDataPath, @"webviewplus\index.html");
            if (File.Exists(webviewPlus))
            {
                var webviewPlusFolder = Path.Combine(App.LocalDataPath, @"webviewplus");
                // needed for script modules and file access when webviewplus was built with vitejs
                _webView.CoreWebView2.SetVirtualHostNameToFolderMapping("webviewplus", webviewPlusFolder, CoreWebView2HostResourceAccessKind.Allow);
                var uri = new Uri("https://webviewplus/index.html");
                _webView.Source = uri; // Helper.FilePathToFileUrl(webviewPlus);
                _currentUri = _webView.Source;
            }
        }

        private void NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            // expose FileProps to webviewPlus
            // use in js: await chrome.webview.hostObjects.fileProps.Size;
            _webView.CoreWebView2.AddHostObjectToScript("fileProps", _fileProps);
            if (nextPath != null)
            {
                NavigateToFile(nextPath);
                nextPath = null;
            }
        }

        private void NavigationStarting_CancelNavigation(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (e.Uri.StartsWith("data:")) // when using NavigateToString
                return;

            var newUri = new Uri(e.Uri);
            if (newUri != _currentUri) e.Cancel = true;
        }

        public void Dispose()
        {
            _webView.Dispose();
            _webView = null;
        }

        private object CreateDownloadButton()
        {
            var button = new Button
            {
                Content = TranslationHelper.Get("WEBVIEW2_NOT_AVAILABLE"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(20, 6, 20, 6)
            };
            button.Click += (sender, e) => Process.Start("https://go.microsoft.com/fwlink/p/?LinkId=2124703");

            return button;
        }
    }
}