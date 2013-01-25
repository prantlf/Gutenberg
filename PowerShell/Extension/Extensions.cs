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

using System.Linq;
using System.Management.Automation;
using System.Reflection;

namespace Gutenberg.PowerShell
{
    // Additional methods and method overloads which would be useful in particular classes. They
    // are organized by the class which they attach to.

    static class AssemblyExtension
    {
        public static T GetAssemblyAttribute<T>(this Assembly assembly) {
            var attributes = assembly.GetCustomAttributes(false);
            return (T) attributes.First(item => item is T);
        }
    }

    static class PSObjectExtension
    {
        public static object GetBaseObject(this object source) {
            var wrapper = source as PSObject;
            return wrapper != null ? wrapper.BaseObject : source;
        }
    }
}
