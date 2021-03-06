﻿// Copyright (C) 2012-2013 Ferdinand Prantl <prantlf@gmail.com>
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

using System.Management.Automation;

namespace Gutenberg.PowerShell
{
    // Base class for cmdlets providing convenience methods for logging.
    public abstract class LoggingCmdlet : PSCmdlet
    {
        protected Log Log {
            get {
                if (log == null) {
                    log = new CmdletLog(this);
                    if (Settings.GetValue<bool>(typeof(Loggable), "DebugLog"))
                        log = new DispatchLog(new[] { DebugLog.Instance, log });
                }
                return log;
            }
        }
        Log log;

    }
}
