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

namespace Gutenberg
{
    // Declares that the implementing class supports logging and if you wish it, you should set
    // a Log instance for it before you use it.
    public interface Loggable
    {
        // Returns the Log instance to use. It never returns null to allow safe usage.
        Log Log { get; set; }
    }

    // The minimum convenience base class for objects which need logging.
    public abstract class LoggableBase : Loggable
    {
        // Returns the Log instance to use. If it has not been explicitly set it will return
        // a dummy object doing no logging to allow safe usage - unless DebugLog is set to true
        // in the application settings; the logger writing to the Windows debugging output is
        // returned in such case.
        public Log Log {
            get {
                return log ?? (log = Settings.GetValue<bool>(typeof(Loggable), "DebugLog") ?
                            DebugLog.Instance : DummyLog.Instance);
            }
            set {
                log = value;
                UpdateLog();
            }
        }
        Log log;

        // Checks if a Log instance has been explicitly set for this object.
        protected bool HasLog {
            get { return log != null; }
        }

        // If someone set Log on this object it should be the same for all objects owned by it.
        // This method should be overriden by descendant classes and it should propagate the Log
		// instance to all owned Loggable objects.
        protected virtual void UpdateLog() {}
    }
}
