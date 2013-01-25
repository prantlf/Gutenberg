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
using System.Management.Automation;

namespace Gutenberg.PowerShell
{
    // Uses the functionality of a PowerShell provider to perform logging.
    public class DriveLog : ProgressiveLog
    {
        public DriveProvider Provider { get; private set; }

        public DriveLog(DriveProvider provider) {
            if (provider == null)
                throw new ArgumentNullException("provider");
            Provider = provider;
        }

        public override void Verbose(string message) {
            Provider.WriteVerbose(message);
        }

        public override void Warning(string message) {
            Provider.WriteWarning(message);
        }

        public override void Progress(int number, string name, bool last, int percent,
                                      string message) {
            Provider.WriteProgress(CmdletLog.CreateProgressRecord(number, name, last,
                                                                  percent, message));
        }
    }

    // Uses the functionality of a PowerShell cmdlet to perform logging.
    public class CmdletLog : ProgressiveLog
    {
        public LoggingCmdlet Cmdlet { get; private set; }

        public CmdletLog(LoggingCmdlet cmdlet) {
            if (cmdlet == null)
                throw new ArgumentNullException("cmdlet");
            Cmdlet = cmdlet;
        }

        public override void Verbose(string message) {
            Cmdlet.WriteVerbose(message);
        }

        public override void Warning(string message) {
            Cmdlet.WriteWarning(message);
        }

        public override void Progress(int number, string name, bool last, int percent,
                                      string message) {
            Cmdlet.WriteProgress(CreateProgressRecord(number, name, last, percent, message));
        }

        internal static ProgressRecord CreateProgressRecord(int number, string name, bool last,
                                                           int percent, string message) {
            return new ProgressRecord(number, name, message) {
                RecordType = last ? ProgressRecordType.Completed : ProgressRecordType.Processing,
                PercentComplete = percent
            };
        }
    }
}
