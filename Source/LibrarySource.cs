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

using System.IO;

namespace GutenPosh
{
    abstract class LibrarySource : BinarySource
    {
        public Date GetCreated() {
            using (var reader = Open()) {
                Log.Verbose("Getting creation date...");
                return reader.ReadDate();
            }
        }

        public BinaryWriter Create(Date created, out string path) {
            var writer = Create(out path);
            try {
                writer.Write(created);
                return writer;
            } catch {
                writer.Close();
                IOUtility.DeleteTempFile(path);
                throw;
            }
        }

        public void Finalize(BinaryWriter writer) {
            writer.Write(false);
        }

        protected BinaryReader OpenItems() {
            var reader = Open();
            try {
                var created = reader.ReadDate();
                Log.Verbose("Creation date: {0}", created);
                return reader;
            } catch {
                reader.Close();
                throw;
            }
        }
    }
}
