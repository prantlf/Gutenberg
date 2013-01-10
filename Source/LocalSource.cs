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
    abstract class LocalSource : PlacedSource
    {
        public bool HasLocal() {
            if (!Directory.Exists(DirectoryName)) {
                Log.Verbose("Creating {0}...", DirectoryName);
                Directory.CreateDirectory(DirectoryName);
            }
            if (!File.Exists(FileName)) {
                Log.Warning("No file at {0}.", FileName);
                return false;
            }
            return true;
        }

        public void MakeLocal(string path) {
            if (File.Exists(FileName))
                File.Delete(FileName);
            File.Move(path, FileName);
        }

        protected abstract string FileName { get; }
    }
}
