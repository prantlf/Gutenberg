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
    public abstract class LibraryParser : BinaryParser
    {
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

        public void Finalize(BinaryWriter writer) {
            writer.Write(false);
        }
    }
}
