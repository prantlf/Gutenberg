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
    // Provides access to a source accessible by its URL; locally or from the Internet.
    public abstract class RemoteSource : LoggableBase
    {
        // Returns a stream with the source content. If the URL is a file system path it just
        // opens the file otherwise it downloads the content from the URL and returns a block
        // of memory with it.
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

        // Copies the stream with the source content to the ouptut stream. It supports both file
        // system paths and Internet URLs.
        public void Transfer(Stream output) {
            if (Uri.IsWellFormedUriString(Url, UriKind.Absolute)) {
                Log.Verbose("Downloading {0}...", Url);
                var client = CreateClient();
                using (var input = client.OpenRead(Url)) {
                    string sizevalue = client.ResponseHeaders[HttpResponseHeader.ContentLength];
                    // The content is downloaded by blocks of 65,536 bytes. Reporting one step
                    // for every block behaved well in the console progress bar on my laptop.
                    const int step = 65536;
                    Progress progress = null;
                    int count = 1;
                    // Content-Length might not be available; no progressbar would be available
                    // in such case. Luckily, files in the Project Gutenberg support it.
                    if (!string.IsNullOrEmpty(sizevalue)) {
                        int steps = int.Parse(sizevalue, CultureInfo.InvariantCulture) / step;
                        progress = Log.Action(steps, "Downloading {0}...", Url);
                    }
                    for (var buffer = new byte[step]; ; ) {
                        var length = input.Read(buffer, 0, buffer.Length);
                        if (length <= 0)
                            break;
                        output.Write(buffer, 0, length);
                        // The specified block size is more of a recommendation; the method can
                        // perform a step of a smaller size; let's detect the expected step size.
                        if (progress != null && count * step <= output.Length) {
                            progress.Continue("{0} bytes transferred...", output.Length);
                            ++count;
                        }
                    }
                    Log.Verbose("{0} bytes were transferred.", sizevalue);
                    if (progress != null)
                        progress.Finish();
                }
            } else {
                Log.Verbose("Copying {0}...", Url);
                using (var input = File.OpenRead(Url))
                    input.CopyTo(output);
            }
        }

        // Creates the client to download from Internet URLs. It sets a couple of headers to
        // describe the agent and its capabilities.
        WebClient CreateClient() {
            var client = new WebClient();
            // Without sending the set of headers as similar to the real browser as possible I was
            // getting HTTP error 403 when downloading files from http://www.gutenberg.org/cache.
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

        // URL of the source to download from; to be implemented by decended classes.
        protected abstract string Url { get; }
    }

    // Offers downloading of a source with a URL specified in the constructor.
    public class Downloader : RemoteSource
    {
        // Creates a new instance that will download from the specified URL.
        public Downloader(string url) {
            this.url = url;
        }

        // URL of the source to download from specified in the constructor.
        protected override string Url {
            get { return url; }
        }
        readonly string url;
    }
}
