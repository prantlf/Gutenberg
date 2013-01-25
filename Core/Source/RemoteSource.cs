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

namespace Gutenberg
{
    public abstract class RemoteSource : LoggableBase
    {
        public Stream Open() {
            if (Uri.IsWellFormedUriString(Url, UriKind.Absolute)) {
                Log.Verbose("Downloading {0}...", Url);
                var client = new WebClient();
                using (var input = client.OpenRead(Url)) {
                    string sizevalue = client.ResponseHeaders[HttpResponseHeader.ContentLength];
                    const int step = 65536;
                    Progress progress = null;
                    int count = 1;
                    if (!string.IsNullOrEmpty(sizevalue)) {
                        int steps = int.Parse(sizevalue, CultureInfo.InvariantCulture) / step;
                        progress = Log.Action(steps, "Downloading {0}...", Url);
                    }
                    var output = new MemoryStream();
                    try {
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
                    } catch {
                        output.Close();
                        throw;
                    }
                    Log.Verbose("{0} bytes were transferred.", sizevalue);
                    if (progress != null)
                        progress.Finish();
                    output.Position = 0;
                    return output;
                }
            } 
            Log.Verbose("Opening {0}...", Url);
            return File.OpenRead(Url);
        }

        protected abstract string Url { get; }
    }
}
