// Copyright (C) 2012-2013 Ferdinand Prantl <prantlf@gmail.com>
// All rights reserved.       
//
// This file is part of Project Gutenberg integration to PowerShell
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
using System.Reflection;
using System.Text;
using Microsoft.PowerShell.Commands;

namespace Gutenberg.PowerShell
{
    public class NewDriveParameters
    {
        [Parameter]
        public string Directory { get; set; }
    }

    // Extra parameters for the Get-Content cmdlet. They can specify encoding of the binary
    // content or raw binary processing in the same way as it is done in the file system
    // drive provider.

    public class ContentReaderParameters : FileSystemContentDynamicParametersBase
    {
        [Parameter]
        public int Index { get; set; }

        [Parameter]
        public string Format { get; set; }

        [Parameter]
        public string File { get; set; }

        public Encoding GetEncoding() {
            if (!WasStreamTypeSpecified)
                return System.Text.Encoding.Default;
            if (Encoding == FileSystemCmdletProviderEncoding.Byte)
                throw new InvalidOperationException("Byte content has no encoding.");
            var flags = BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.NonPublic;
            return (Encoding) typeof(FileSystemContentDynamicParametersBase).InvokeMember(
                "GetEncodingFromEnum", flags, null, null, new object[] { Encoding });
        }
    }
}
