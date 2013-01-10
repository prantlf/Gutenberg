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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace GutenPosh
{
    static class IOUtility
    {
        public static void DeleteTempFile(string path) {
            try {
                File.Delete(path);
            } catch {}
        }

        public static IEnumerable<Property> GetProperties(Type type) {
            return type.GetProperties().Where(property =>
                property.GetCustomAttributes(typeof(PropertyAttribute), false).Any()).Select(
                    property => new Property {
                        Name = property.Name, Type = property.PropertyType,
                        GetValue = item => property.GetValue(item, null),
                        SetValue = (item, value) => property.SetValue(item, value, null)
                    });
        }

        public static T Read<T>(XmlReader reader,
                                IEnumerable<Property> properties) where T : new() {
            var item = new T();
            using (var subreader = reader.ReadSubtree())
                while (subreader.ReadToFollowingElement()) {
                    var property = properties.FirstOrDefault(entry =>
                        entry.Name.EqualsII(reader.LocalName));
                    if (property != null)
                        property.SetValue(item, reader.ReadElementContentAs(property.Type));
                }
            return item;
        }

        public static void Write(XmlWriter writer, object value,
                                 IEnumerable<Property> properties) {
            foreach (var property in properties)
                writer.WriteValueElement(property.Name, property.GetValue(value));
        }

        public static T Read<T>(BinaryReader reader,
                                IEnumerable<Property> properties) where T : new() {
            var item = new T();
            foreach (var property in properties)
                property.SetValue(item, reader.ReadValue(property.Type));
            return item;
        }

        public static void Write(BinaryWriter writer, object value,
                                 IEnumerable<Property> properties) {
            foreach (var property in properties)
                writer.Write(property.GetValue(value), property.Type);
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    class PropertyAttribute : Attribute {}

    class Property
    {
        public string Name { get; set; }

        public Type Type { get; set; }

        public Func<object, object> GetValue { get; set; }

        public Action<object, object> SetValue { get; set; }
    }
}
