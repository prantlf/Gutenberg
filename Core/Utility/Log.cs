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
using System.Collections.Generic;
using Debugger = System.Diagnostics.Debugger;

namespace Gutenberg
{
    // Encapsulates logging functionality so that it can be used from other components than
    // the drive provider itself (which only has an access to the console).
    public interface Log
    {
        void Verbose(string message);

        void Verbose(string format, params object[] args);

        void Warning(string message);

        void Warning(string format, params object[] args);

        Progress Action(int steps, string name);

        Progress Action(int steps, string format, params object[] args);

        void Progress(int number, string name, bool last, int percent, string message);

        void Progress(int number, string name, bool last, int percent,
                      string format, params object[] args);
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

        public void Progress(int number, string name, bool last, int percent, string message) {}

        public Progress Action(int steps, string name) {
            return DummyProgress.Instance;
        }

        public Progress Action(int steps, string format, params object[] args) {
            return DummyProgress.Instance;
        }

        public void Progress(int number, string name, bool last, int percent,
                             string format, params object[] args) {}

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

        public Progress Action(int steps, string name) {
            var progress = new LogProgress(this, name, steps);
            progress.Start();
            return progress;
        }

        public Progress Action(int steps, string format, params object[] args) {
            return Action(steps, string.Format(format, args));
        }

        public void Progress(int number, string name, bool last, int percent,
                             string format, params object[] args) {
            Progress(number, name, last, percent, string.Format(format, args));
        }

        public abstract void Verbose(string message);

        public abstract void Warning(string message);

        public abstract void Progress(int number, string name, bool last, int percent,
                                      string message);
    }

    public class DebugLog : ProgressiveLog
    {
        DebugLog() {}

        public override void Verbose(string message) {
            Debugger.Log(Trace, null, message);
        }

        public override void Warning(string message) {
            Debugger.Log(Warn, null, message);
        }

        public override void Progress(int number, string name, bool last, int percent,
                                      string message) {
            if (percent == 0)
                Debugger.Log(Status, null, string.Format("{0}: {1}", number, name));
            Debugger.Log(Status, null, string.Format("{0}:   {1} {2}%", number, message, percent));
        }

        const int Trace = 0;
        const int Status = 20;
        const int Warn = 40;

        public static readonly Log Instance = new DebugLog();
    }

    public class DispatchLog : ProgressiveLog
    {
        public readonly List<Log> Logs = new List<Log>();

        public DispatchLog() {}

        public DispatchLog(IEnumerable<Log> logs) {
            if (logs == null)
                throw new ArgumentNullException("logs");
            Logs.AddRange(logs);
        }

        public override void Verbose(string message) {
            foreach (var log in Logs)
                log.Verbose(message);
        }

        public override void Warning(string message) {
            foreach (var log in Logs)
                log.Warning(message);
        }

        public override void Progress(int number, string name, bool last, int percent,
                                      string message) {
            foreach (var log in Logs)
                log.Progress(number, name, last, percent, message);
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
            Log.Progress(Number, Name, false, 0, "Starting...");
        }

        public override void Continue(string message) {
            Increment();
            Log.Progress(Number, Name, false, (int) ProgressValue, message);
        }

        public override void Finish() {
            Log.Progress(Number, Name, true, 100, "Finished.");
        }
    }
}
