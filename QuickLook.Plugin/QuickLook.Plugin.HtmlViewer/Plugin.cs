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

using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using QuickLook.Common.Plugin;

namespace QuickLook.Plugin.HtmlViewer
{
    public class Plugin : IViewer
    {
        private static readonly string[] Extensions =
            "html,htm,mht,mhtml,pdf,csv,xlsx,svg,md,markdown,gltf,glb,c++,h++,bat,c,cmake,cpp,cs,css,go,h,hpp,java,js,json,jsx,lua,perl,pl,ps1,psm1,py,rb,sass,scss,sh,sql,tex,ts,tsx,txt,webp,yaml,yml".Split(',');
            // {".mht", ".mhtml", ".htm", ".html"};
        private static readonly string[] SupportedProtocols = {"http", "https"};

        private static WebpagePanel _panel;

        public int Priority => 1;

        public void Init()
        {
            _panel = new WebpagePanel();
        }

        public bool CanHandle(string path)
        {
            return !Directory.Exists(path) && (Extensions.Any(path.ToLower().EndsWith) ||
                                               path.ToLower().EndsWith(".url") &&
                                               SupportedProtocols.Contains(Helper.GetUrlPath(path).Split(':')[0]
                                                   .ToLower()));
        }

        public void Prepare(string path, ContextObject context)
        {
            var desiredSize = new Size(1200, 1600);
            context.SetPreferredSizeFit(desiredSize, 0.8);
        }

        public void View(string path, ContextObject context)
        {
            context.ViewerContent = _panel;
            context.Title = Path.IsPathRooted(path) ? Path.GetFileName(path) : path;

            if (path.ToLower().EndsWith(".url"))
                path = Helper.GetUrlPath(path);
            _panel.NavigateToFile(path);
            _panel.Dispatcher.Invoke(() => { context.IsBusy = false; }, DispatcherPriority.Loaded);
        }

        public void Cleanup()
        {
            //_panel?.Dispose();
            //_panel = null;
        }
    }
}