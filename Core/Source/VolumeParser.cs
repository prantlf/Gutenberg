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

using System.Collections.Generic;
using System.IO;

namespace Gutenberg
{
	// Parses the content with book volumes.
    public class VolumeParser : LibraryParser
    {
		// Deserializes all volumes from the binary source; it expects the reader returned by the
		// Open method. This method is supposed to be used in a single loop; the enumeration
		// reads the content to the end.
        public IEnumerable<Volume> GetVolumes(BinaryReader reader) {
            // There are almost 520,000 files in the project Gutenberg. We set up 53 * 10,000
            // steps here to have the progress bar stretching quite good and still having a
            // little space to grow for the project content.
            var progress = Log.Action(53, "Getting volumes...");
            int count = 0;
            // Every item begins with an explicitely written bool true; false marks the end.
            while (reader.ReadBoolean()) {
                yield return Volume.Read(reader);
                // One step after every 10,000 items run nicely on my laptop; less would load the
                // CPU with constant reporting and more would wait too long in the console.
                if (++count % 10000 == 0)
                    progress.Continue("{0} volumes processed...", count);
            }
            Log.Verbose("{0} volumes were loaded.", count);
            progress.Finish();
        }

        // Writes a single book volume to the output; it expects the writer returned by the
        // Create method. After writing all book volumes the method Finalize must be called.
        public void Write(BinaryWriter writer, Volume volume) {
            // The initial bool true will be recognized when deserializing the volumes. The
            // method Finalize writes false as the end marker.
            writer.Write(true);
            volume.Write(writer);
        }

        // The header identifier of book volumes.
        protected override string Header {
            get { return "Gutenberg:Volumes"; }
        }

        // The version of the book volume source.
        protected override int Version {
            get { return 1; }
        }
    }
}
