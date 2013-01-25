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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Gutenberg.FileSystem
{
    public class FileSource : PlacedSource
    {
        public Stream Open(Volume volume, ref Encoding encoding) {
            if (volume == null)
                throw new ArgumentNullException("volume");
            var path = GetPath(volume);
            if (!IsRecent(volume, path))
                Download(volume, path);
            Log.Verbose("Opening {0}...", path);
            Stream content = File.OpenRead(path);
            try {
                // If the encoding was not provided by the caller it will be inferred from the
                // MIME type charset.
                if (encoding == null)
                    encoding = GetEncoding(volume);
                // Books have a quite long common prolog and epilog with the Project Gutenberg
                // policies. Trim it for plain text books; other formats would be too complicated.
                if (volume.Formats.Any(format => format.StartsWithII("text/plain")))
                    content = TrimTextPrologAndEpilog(content, encoding);
                return content;
            } catch {
                content.Close();
                throw;
            }
        }

        public void ClearCache() {
            foreach (var directory in Directory.GetDirectories(DirectoryName))
                Directory.Delete(directory, true);
        }

        bool IsRecent(Volume volume, string path) {
            return File.Exists(path) &&
                volume.Uploaded.Ticks <= File.GetLastWriteTimeUtc(path).Ticks;
        }

        string GetPath(Volume volume) {
            var path = Path.Combine(DirectoryName, string.Join("\\", volume.Number.ToString(
                CultureInfo.InvariantCulture).Select(ch => ch.ToString()).ToArray()));
            if (!Directory.Exists(path)) {
                Log.Verbose("Creating {0}...", path);
                Directory.CreateDirectory(path);
            }
            return Path.Combine(path, PathUtility.GetChildName(volume.URL));
        }

        void Download(Volume volume, string path) {
            var url = PathUtility.JoinPath(CatalogParser.ProjectUrl, volume.URL);
            Log.Verbose("Downloading {0}...", url);
            var client = new WebClient();
            using (var input = client.OpenRead(url))
                try {
                    string sizevalue = client.ResponseHeaders[HttpResponseHeader.ContentLength];
                    const int step = 65536;
                    Progress progress = null;
                    int count = 1;
                    if (!string.IsNullOrEmpty(sizevalue)) {
                        int steps = int.Parse(sizevalue, CultureInfo.InvariantCulture) / step;
                        progress = Log.Action(steps, "Downloading {0}...", url);
                    }
                    Log.Verbose("Writing {0}...", path);
                    EnsureDirectory();
                    using (var output = File.OpenWrite(path))
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
                } catch {
                    IOUtility.DeleteTempFile(path);
                    throw;
                }
        }

        Encoding GetEncoding(Volume volume) {
            var charset = volume.Formats.FirstOrDefault(format =>
                format.StartsWithII("; charset=\""));
            if (charset != null) {
                charset = charset.Substring(charset.IndexOfII("; charset=\"")
                                            + 11).TrimEnd('"');
                return Encoding.GetEncoding(charset);
            }
            return Encoding.Default;
        }

        // Reads all the content line by line and trims the standard Project Gutenberg prolog
        // and epilog text off. It will set as content a new memory buffer with the lines left.
        Stream TrimTextPrologAndEpilog(Stream content, Encoding encoding) {
            Log.Verbose("Trimming project Gutenberg common prolog and epilog...");
            IEnumerable<string> lines;
            using (var reader = new StreamReader(content, encoding))
                lines = reader.ReadLines().ToArray();
            var trimmed = lines.SkipWhile(line => !(
                line.ContainsCI("START OF THE PROJECT GUTENBERG EBOOK") ||
                line.ContainsCI("START OF THIS PROJECT GUTENBERG EBOOK"))).ToArray();
            if (trimmed.Any())
                lines = trimmed.Skip(1).TakeWhile(line => !(
                    line.ContainsCI("END OF THE PROJECT GUTENBERG EBOOK") ||
                    line.ContainsCI("END OF THIS PROJECT GUTENBERG EBOOK"))).ToArray();
            content.Close();
            content = new MemoryStream();
            try {
                using (var writer = new StreamWriter(content, encoding))
                    foreach (var line in lines)
                        writer.WriteLine(line);
                return new MemoryStream(((MemoryStream) content).ToArray());
            } catch {
                content.Close();
                throw;
            }
        }
    }
}
