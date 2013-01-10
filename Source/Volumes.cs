// Copyright (C) 2012 Ferdinand Prantl <prantlf@gmail.com>
// All rights reserved.       
//
// This file is part of GutenPosh - PowerShell drive for the Gutenberg project
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

namespace GutenPosh
{
    class Volumes : LibrarySource
    {
        public IEnumerable<Volume> GetVolumes() {
            using (var reader = OpenItems()) {
                var progress = Log.Action(53, "Getting volumes...");
                int count = 0;
                while (reader.ReadBoolean()) {
                    yield return Volume.Read(reader);
                    if (++count % 10000 == 0)
                        progress.Continue("{0} volumes processed...", count);
                }
                Log.Verbose("{0} volumes were loaded.", count);
                progress.Finish();
            }
        }

        public void Write(BinaryWriter writer, Volume volume) {
            writer.Write(true);
            volume.Write(writer);
        }

        protected override string Header {
            get { return "GnuPosh:Volumes"; }
        }

        protected override int Version {
            get { return 1; }
        }

        protected override string FileName {
            get { return Path.Combine(DirectoryName, "Volumes"); }
        }
    }
}
