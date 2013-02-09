// Copyright (C) 2012-2013 Ferdinand Prantl <prantlf@gmail.com>
// All rights reserved.       
//
// This file is part of the Project Gutenberg Access API
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
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;

namespace Gutenberg
{
    public abstract class RemoteSource : LoggableBase
    {
        public Stream Open() {
            if (Uri.IsWellFormedUriString(Url, UriKind.Absolute)) {
                var output = new MemoryStream();
                try {
                    Transfer(output);
                    output.Position = 0;
                    return output;
                } catch {
                    output.Close();
                    throw;
                }
            } 
            Log.Verbose("Opening {0}...", Url);
            return File.OpenRead(Url);
        }

        public void Transfer(Stream output) {
            Log.Verbose("Downloading {0}...", Url);
            var client = CreateClient();
            using (var input = client.OpenRead(Url)) {
                string sizevalue = client.ResponseHeaders[HttpResponseHeader.ContentLength];
                const int step = 65536;
                Progress progress = null;
                int count = 1;
                if (!string.IsNullOrEmpty(sizevalue)) {
                    int steps = int.Parse(sizevalue, CultureInfo.InvariantCulture) / step;
                    progress = Log.Action(steps, "Downloading {0}...", Url);
                }
                for (var buffer = new byte[step]; ; ) {
                    var length = input.Read(buffer, 0, buffer.Length);
                    if (length <= 0)
                        break;
                    output.Write(buffer, 0, length);
                    if (progress != null && count * step <= output.Length) {
                        progress.Continue("{0} bytes transferred...", output.Length);
                        ++count;
                    }
                }
                Log.Verbose("{0} bytes were transferred.", sizevalue);
                if (progress != null)
                    progress.Finish();
            }
        }

        WebClient CreateClient() {
            var client = new WebClient();
            client.Headers[HttpRequestHeader.Accept] = "text/plain,text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            // Accept-Encoding: gzip,deflate,sdch
            client.Headers[HttpRequestHeader.AcceptCharset] = "ISO-8859-2,utf-8;q=0.7,*;q=0.3";
            client.Headers[HttpRequestHeader.AcceptLanguage] = "en,en-US;q=0.8,de;q=0.6,de-DE;q=0.4,cs;q=0.2";
            client.Headers[HttpRequestHeader.CacheControl] = "max-age=0";
            // Connection: keep-alive
            // Cookie: session_id=05773d8c2375cd211784cc09b4f7ed6b3126d91f
            client.Headers[HttpRequestHeader.Host] = new Uri(Url).Host;
            // User-Agent: Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.17 (KHTML, like Gecko) Chrome/24.0.1312.56 Safari/537.17
            var assembly = Assembly.GetCallingAssembly();
            client.Headers[HttpRequestHeader.UserAgent] =
                assembly.GetAssemblyAttribute<AssemblyTitleAttribute>().Title + "/" +
                assembly.GetAssemblyAttribute<AssemblyFileVersionAttribute>().Version + " (" +
                Environment.OSVersion + ")";
            return client;
        }

        protected abstract string Url { get; }
    }

    public class Downloader : RemoteSource
    {
        public Downloader(string url) {
            this.url = url;
        }

        protected override string Url {
            get { return url; }
        }
        string url;
    }
}
