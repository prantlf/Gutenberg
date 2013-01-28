// Copyright (C) 2012-2013 Ferdinand Prantl <prantlf@gmail.com>
// All rights reserved.       
//
// This file is part of the Project Gutenberg Access from Local File System
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
        public Stream Open(Volume volume, out Encoding encoding) {
            if (volume == null)
                throw new ArgumentNullException("volume");
            var path = GetPath(volume);
            if (!IsRecent(volume, path))
                Download(volume, path);
            Log.Verbose("Opening {0}...", path);
            encoding = GetEncoding(volume);
            return File.OpenRead(path);
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
    }
}
