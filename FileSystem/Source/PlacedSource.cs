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

namespace Gutenberg.FileSystem
{
    public abstract class PlacedSource : LoggableBase
    {
        protected void EnsureDirectory() {
            if (!Directory.Exists(DirectoryName)) {
                Log.Verbose("Creating {0}...", DirectoryName);
                Directory.CreateDirectory(DirectoryName);
            }
        }

        protected string DirectoryName {
            get { return directoryName; }
        }
        string directoryName = Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData), "Gutenberg");

        public void SetDirectory(string value) {
            directoryName = value;
        }
    }
}
