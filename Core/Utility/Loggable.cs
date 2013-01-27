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
    public interface Loggable
    {
        Log Log { get; set; }
    }

    public abstract class LoggableBase : Loggable
    {
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

        protected bool HasLog {
            get { return log != null; }
        }

        protected virtual void UpdateLog() {}
    }
}
