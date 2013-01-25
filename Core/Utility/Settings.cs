// Copyright (C) 2013 Ferdinand Prantl <prantlf@gmail.com>
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
using System.Configuration;
using System.IO;

namespace Gutenberg
{
    public static class Settings
    {
        public static T GetValue<T>(Type type, string name) {
            var fullName = type.Assembly.GetName().Name + "." + name;
            var value = ConfigurationManager.AppSettings[fullName];
            if (string.IsNullOrEmpty(value)) {
                var configuration = GetConfiguration(type);
                if (configuration != null)
                    value = configuration.AppSettings.Settings[name].Value;
            }
            if (string.IsNullOrEmpty(value))
                throw new ApplicationException(string.Format("Value of {0} was empty.", fullName));
            return (T) Convert.ChangeType(value, typeof(T));
        }

        static Configuration GetConfiguration(Type type) {
            var map = new ExeConfigurationFileMap();
            map.ExeConfigFilename = type.Assembly.Location + ".config";
            return File.Exists(map.ExeConfigFilename) ?
                ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None) :
                null;
        }
    }
}
