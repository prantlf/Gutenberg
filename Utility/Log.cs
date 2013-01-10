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

using System;
using System.Management.Automation;

namespace GutenPosh
{
    // Encapsulates logging functionality so that it can be used from other components than
    // the drive provider itself (which only has an access to the console).
    public interface Log
    {
        void Verbose(string message);

        void Verbose(string format, params object[] args);

        void Warning(string message);

        void Warning(string format, params object[] args);

        void Progress(int number, string name, ProgressRecordType type,
                      int percent, string message);

        void Progress(int number, string name, ProgressRecordType type,
                      int percent, string format, params object[] args);

        Progress Action(int steps, string name);

        Progress Action(int steps, string format, params object[] args);
    }

    public interface Progress
    {
        void Start();

        void Continue(string message);

        void Continue(string format, params object[] args);

        void Finish();
    }

    // Does nothing; it just implements the interface. It can be used at times when no drive
    // provider is available.
    public class DummyLog : Log
    {
        DummyLog() {}

        public void Verbose(string message) {}

        public void Verbose(string format, params object[] args) {}

        public void Warning(string message) {}

        public void Warning(string format, params object[] args) {}

        public void Progress(int number, string name, ProgressRecordType type,
                             int percent, string message) {}

        public void Progress(int number, string name, ProgressRecordType type,
                             int percent, string format, params object[] args) {}

        public Progress Action(int steps, string name) {
            return DummyProgress.Instance;
        }

        public Progress Action(int steps, string format, params object[] args) {
            return DummyProgress.Instance;
        }

        public static readonly Log Instance = new DummyLog();
    }

    public class DummyProgress : Progress
    {
        DummyProgress() {}

        public void Start() {}

        public void Continue(string message) { }

        public void Continue(string format, params object[] args) {}

        public void Finish() {}

        public static readonly Progress Instance = new DummyProgress();
    }

    public abstract class ProgressiveLog : Log
    {
        public void Verbose(string format, params object[] args) {
            Verbose(string.Format(format, args));
        }

        public void Warning(string format, params object[] args) {
            Warning(string.Format(format, args));
        }

        public void Progress(int number, string name, ProgressRecordType type,
                             int percent, string format, params object[] args) {
            Progress(number, name, type, percent, string.Format(format, args));
        }

        public Progress Action(int steps, string name) {
            var progress = new LogProgress(this, name, steps);
            progress.Start();
            return progress;
        }

        public Progress Action(int steps, string format, params object[] args) {
            return Action(steps, string.Format(format, args));
        }

        public abstract void Verbose(string message);

        public abstract void Warning(string message);

        public abstract void Progress(int number, string name, ProgressRecordType type,
                                      int percent, string message);
    }

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

        public override void Progress(int number, string name, ProgressRecordType type,
                      int percent, string message) {
            Provider.WriteProgress(new ProgressRecord(number, name, message) {
                RecordType = type, PercentComplete = percent
            });
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

        public override void Progress(int number, string name, ProgressRecordType type,
                      int percent, string message) {
            Cmdlet.WriteProgress(new ProgressRecord(number, name, message) {
                RecordType = type, PercentComplete = percent
            });
        }
    }

    public abstract class SteppedProgress : Progress
    {
        public string Name { get; private set; }

        protected int Number { get; set; }

        protected double ProgressValue { get; private set; }

        double ProgressIncrement { get; set; }

        static int LastNumber;

        public SteppedProgress(string name, int steps) {
            if (name == null)
                throw new ArgumentNullException("name");
            if (name.IsEmpty())
                throw new ArgumentException("Name cannot be empty.", "name");
            Name = name;
            if (LastNumber == Int32.MaxValue)
                LastNumber = 0;
            Number = ++LastNumber;
            ProgressValue = 0;
            ProgressIncrement = 100.0 / steps;
        }

        public abstract void Start();

        public void Continue(string format, params object[] args) {
            Continue(string.Format(format, args));
        }

        public abstract void Continue(string message);

        public abstract void Finish();

        protected void Increment() {
            if (ProgressValue + ProgressIncrement <= 100)
                ProgressValue += ProgressIncrement;
        }
    }

    public class LogProgress : SteppedProgress
    {
        public Log Log { get; private set; }

        public LogProgress(Log log, string name, int steps) : base(name, steps) {
            if (log == null)
                throw new ArgumentNullException("cmdlet");
            Log = log;
        }

        public override void Start() {
            Log.Progress(Number, Name, ProgressRecordType.Processing, 0, "Starting...");
        }

        public override void Continue(string message) {
            Increment();
            Log.Progress(Number, Name, ProgressRecordType.Processing, (int) ProgressValue, message);
        }

        public override void Finish() {
            Log.Progress(Number, Name, ProgressRecordType.Completed, 100, "Finished.");
        }
    }
}
