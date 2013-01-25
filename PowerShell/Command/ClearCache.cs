// Copyright (C) 2012-2013 Ferdinand Prantl <prantlf@gmail.com>
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
using Gutenberg.FileSystem;

namespace Gutenberg.PowerShell
{
    [Cmdlet(VerbsCommon.Clear, "GPCache", SupportsShouldProcess = true)]
    public class ClearCache : LoggingCmdlet
    {
        [Parameter]
        public SwitchParameter Force { get; set; }

        protected override void ProcessRecord() {
            try {
                var files = new FileSource { Log = Log };
                if (ShouldProcess("Cache", "Clear") && (Force ||
                        ShouldContinue("Do you really want to clear locally cached volumes?",
                            "Clear Cache")))
                    files.ClearCache();
            } catch (Exception exception) {
                WriteError(new ErrorRecord(exception, "ClearCacheFailed",
                    ErrorCategory.DeviceError, null));
            }
        }
    }
}
