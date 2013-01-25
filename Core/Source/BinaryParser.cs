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
using System.Text;

namespace Gutenberg
{
    public abstract class BinaryParser : LoggableBase
    {
        protected BinaryReader Open(Stream stream) {
            BinaryReader reader = new BinaryReader(stream, Encoding.UTF8);
            Log.Verbose("Checking content...");
            try {
                if (reader.ReadString() != Header)
                    throw new ApplicationException("Wrong header.");
                if (reader.ReadInt32() > Version)
                    throw new ApplicationException("Wrong version.");
                return reader;
            } catch {
                reader.Close();
                throw;
            }
        }

        protected BinaryWriter Create(Stream stream) {
            BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8);
            try {
                writer.Write(Header);
                writer.Write(Version);
                return writer;
            } catch {
                writer.Close();
                throw;
            }
        }

        protected abstract string Header { get; }

        protected abstract int Version { get; }
    }
}
