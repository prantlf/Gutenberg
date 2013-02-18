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
    // Encapsulates logging functionality so that it can be used from common components
    // independent on the system which provides the logging target (PowerShell, file etc.).
    public interface Log
    {
        // Issues a verbose message to the log.
        void Verbose(string message);

        // Issues a formatted verbose message to the log.
        void Verbose(string format, params object[] args);

        // Issues a warning message to the log.
        void Warning(string message);

        // Issues a formatted warning message to the log.
        void Warning(string format, params object[] args);

        // Returns an object which will be used to report progress of a long running action.
        // The number of steps which will be reported should be estimated as well as possible.
        Progress Action(int steps, string name);

        // Returns an object which will be used to report progress of a long running action.
        // The action name is formattable.
        Progress Action(int steps, string format, params object[] args);

        // Reports a progress of a long running action. This method is not to be called directly;
        // it is called by Action implementations.
        void Progress(int number, string name, bool last, int percent, string message);

        // Reports a progress of a long running action. This method is not to be called directly;
        // it is called by Action implementations. The progress message is formattable.
        void Progress(int number, string name, bool last, int percent,
                      string format, params object[] args);
    }

    // Encapsulates long operation progress reporting functionality so that it can be used from
    // common components independent on the rendering system (PowerShell, file etc.).
    public interface Progress
    {
        // Reports that the action has started.
        void Start();

        // Reports another step in the action progress.
        void Continue(string message);

        // Reports another step in the action progtess as a formattable message.
        void Continue(string format, params object[] args);

        // Reports that the action has finished.
        void Finish();
    }

    // Does nothing; it just implements the interface. It can be used if logging has been turned
    // off or no Log instance as been set for the working object.
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

    // Does nothing; it just implements the interface. It can be used if logging has been turned
    // off or no Log instance as been set for the working object. It is used by DummyLog.
    public class DummyProgress : Progress
    {
        DummyProgress() {}

        public void Start() {}

        public void Continue(string message) { }

        public void Continue(string format, params object[] args) {}

        public void Finish() {}

        public static readonly Progress Instance = new DummyProgress();
    }

    // Base class for loggers providing common implementation for convenience method overloads.
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
            // While a Progress object can be constructed and started later, there is no such
            // case in this project. For convenience, loggers in this project start the progress
            // of the just created action immediately.
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

    // Logger writing to the Windows debugging output. It can be watched by a debugger attached
    // to the logging process. There are tools like DbgView from www.sysinternals.com which
    // attach to processes not to debug, but to listen on the Windows debugging output and print
    // the buzz, which makes this logger the most lightweight logging option.
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

        // Numbers 0-100 can be used to express the severity of the message but we use the API
        // just for course logging and not for error reporting.
        const int Trace = 0;
        const int Status = 20;
        const int Warn = 40;

        public static readonly Log Instance = new DebugLog();
    }

    // Special logger which doesn't have a specific target but delegates the writing to other
    // loggers which are attached to it. It can be used to log to two targets simultaneously;
    // print to the PowerShell console but still filling the debugging output, for example.
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

    // Base class for progres reporters handling the action progress the most usual way: by incrementing
    // the advance counter from zero to the specified step count. It is actually built in to the
    // Log interface, so the loggers have actually no choose...
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
            // Progress reports are identified by a unique number so that we can nest internal
            // long running actions in their higher-level ones. We just use a rotating counter.
            if (LastNumber == Int32.MaxValue)
                LastNumber = 0;
            Number = ++LastNumber;
            // The progress value used by the descendant classes will be a real number <0;100>.
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

    // The progress reporter depending exclusively on a logger to send its status to.
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
