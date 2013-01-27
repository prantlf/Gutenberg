// Copyright (C) 2013 Ferdinand Prantl <prantlf@gmail.com>
// All rights reserved.       
//
// This file is part of PowerShell drive for the Project Gutenberg
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
using System.Management.Automation;

namespace Gutenberg.PowerShell
{
    // Makes easier entering a GutenPosh drive as an input parameter for a cmdlet. Either a drive
    // name or a DriveInfo object retrieved by Get-PSDrive are accepted.
    public class DrivePipeInput
    {
        public DriveInfo DriveInfo { get; private set; }
        public string Name { get; private set; }

        public DrivePipeInput() { }

        public DrivePipeInput(DriveInfo drive) {
            if (drive == null)
                throw new ArgumentNullException("drive");
            DriveInfo = drive;
        }

        public DrivePipeInput(string name) {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("PowerShell drive name must not be empty.");
            Name = name;
        }

        public override string ToString() {
            if (Name != null)
                return Name;
            if (DriveInfo != null)
                return DriveInfo.ToString();
            return "no drive";
        }
    }

    public abstract class DriveCmdlet : LoggingCmdlet
    {
        [Parameter(HelpMessage = "GutenPosh drive. Provide an instance returned by " +
                                 "Get-PSProvider or just its name.")]
        public DrivePipeInput Drive { get; set; }

        [Parameter(HelpMessage = "Cache directory of GutenPosh drives. " +
                                 "The default value is %LOCALAPPDATA%\\Gutenberg.")]
        public string Directory { get; set; }

        protected string ActualDirectory {
            get {
                if (directory == null) {
                    if (Drive != null) {
                        DriveInfo driveInfo;
                        if (Drive.DriveInfo != null) {
                            driveInfo = Drive.DriveInfo;
                        } else {
                            driveInfo = SessionState.Drive.Get(Drive.Name) as DriveInfo;
                            if (driveInfo == null)
                                throw new ApplicationException("A SharePosh drive was expected.");
                        }
                        directory = driveInfo.Directory;
                    } else if (Directory != null) {
                        directory = Directory;
                    }
                }
                return directory;
            }
        }
        string directory;
    }
}
