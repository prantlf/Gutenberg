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
using System.IO;
using System.Linq;

namespace Gutenberg
{
    // Supports unpacking content with one zipped file.
    public class Unpacker : LoggableBase
    {
        // Tries to unpack the packed stream. If the input is recognized as zip the first file
        // will be extracted and returned as a memory stream; the position in the input stream
        // will be undetermninable. If the input is not a zip its position will be set to zero.
        public bool TryUnpack(Stream input, out Stream output) {
            Log.Verbose("Checking compression...");
            if (ZipStorer.IsZip(input))
                using (var store = ZipStorer.Open(input, FileAccess.Read)) {
                    var entry = store.ReadCentralDir().First();
                    Log.Verbose("Extracting {0}...", entry.FilenameInZip);
                    output = new MemoryStream();
                    // The unpacked stream is extracted by blocks of 65,536 bytes. Reporting one
                    // step every 160 blocks run nicely on my laptop; less loaded the CPU with
                    // constant reporting and more made the console wait for too long.
                    const int step = 65536, factor = 160;
                    Progress progress = Log.Action((int) (entry.FileSize / (step * factor) + 2),
                        "Extracting {0}...", entry.FilenameInZip);
                    int count = 1;
                    if (store.ExtractStream(entry, output, step, size => {
                        // The specified block size is more a recommendation; the method can
                        // perform a step of a smaller size; let's detect the expected step size.
                        if (count * step * factor <= size) {
                            progress.Continue("{0} bytes extracted...", size);
                            ++count;
                        }
                    })) {
                        // Restore the output stream position after the ZIP detection. The memory
                        // stream will be returned and the caller will want to read it completely.
                        output.Position = 0;
                        Log.Verbose("{0} bytes were extracted.", output.Length);
                        progress.Finish();
                        return true;
                    }
                    output.Dispose();
                    throw new ApplicationException("Compression not supported.");
                }
            // Restore the original stream position after the ZIP detection. The caller will want
            // to read the complete stream content which has not been packed.
            input.Position = 0;
            output = null;
            return false;
        }
    }
}
