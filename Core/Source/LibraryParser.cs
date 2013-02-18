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

using System.IO;

namespace Gutenberg
{
    // Extends the binary parser for content sources storing the date when they wre created.
    public abstract class LibraryParser : BinaryParser
    {
        // Opens the binary content, reading the format name, version and the creation date.
        public BinaryReader Open(Stream stream, out Date created) {
            var reader = Open(stream);
            try {
                Log.Verbose("Getting creation date...");
                created = reader.ReadDate();
                Log.Verbose("Creation date: {0}", created);
                return reader;
            } catch {
                reader.Close();
                throw;
            }
        }

        // Starts creating a new binary content in the entered stream, writing the format name,
        // version and creation date to it. the stream is supposed to be at the position zero.
        public BinaryWriter Create(Stream stream, Date created) {
            var writer = Create(stream);
            try {
                writer.Write(created);
                return writer;
            } catch {
                writer.Close();
                throw;
            }
        }

        // This method is supposed to be called after all objects were written to the stream.
        // Instead of storing the item count at the beginning, you can write after after item
        // and at the end you put a stop mark by this method.
        public void Finalize(BinaryWriter writer) {
            writer.Write(false);
        }
    }
}
