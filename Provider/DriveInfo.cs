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

using System.Management.Automation;

namespace GutenPosh
{
    public class DriveInfo : PSDriveInfo
    {
        public DriveInfo(PSDriveInfo driveInfo) : base(driveInfo) {
            EnsureDescription();
        }

        public DriveInfo(string name, ProviderInfo provider, string root, string description,
            PSCredential credential) : base (name, provider, root, description, credential) {
        }

        void EnsureDescription() {
            if (string.IsNullOrEmpty(Description))
                Description = string.Format("Browses the Gutenberg project catalogue at {0} " +
                    "just like the local file system.", Name);
        }

        internal Cache Cache {
            get { return cache ?? (cache = new Cache()); }
        }
        Cache cache;
    }
}