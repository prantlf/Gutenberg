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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Text;
using System.Xml;

namespace GutenPosh
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

    static class StringExtension
    {
        public static bool IsEmpty(this string s) {
            return s.Length == 0;
        }

        public static bool StartsWith(this string s, char c) {
            return s.Length > 0 && s[0] == c;
        }

        public static bool EndsWith(this string s, char c) {
            return s.Length > 0 && s[s.Length - 1] == c;
        }

        public static bool StartsWith(this StringBuilder s, char c) {
            return s.Length > 0 && s[0] == c;
        }

        public static bool EndsWith(this StringBuilder s, char c) {
            return s.Length > 0 && s[s.Length - 1] == c;
        }

        // The abbreviation CI means using the flag CurrentCultureIgnoreCase for the string
        // comparison, the abbreviation II the InvariantCultureIgnoreCase flag.

        public static bool EqualsCI(this string left, string right) {
            return string.Equals(left, right, StringComparison.CurrentCultureIgnoreCase);
        }

        public static bool EqualsII(this string left, string right) {
            return string.Equals(left, right, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool ContainsCI(this string hay, string needle) {
            return hay.IndexOfCI(needle) >= 0;
        }

        public static bool ContainsII(this string hay, string needle) {
            return hay.IndexOfII(needle) >= 0;
        }

        public static bool StartsWithCI(this string hay, string needle) {
            return hay.StartsWith(needle, StringComparison.CurrentCultureIgnoreCase);
        }

        public static bool StartsWithII(this string hay, string needle) {
            return hay.StartsWith(needle, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool EndsWithCI(this string hay, string needle) {
            return hay.EndsWith(needle, StringComparison.CurrentCultureIgnoreCase);
        }

        public static bool EndsWithII(this string hay, string needle) {
            return hay.EndsWith(needle, StringComparison.InvariantCultureIgnoreCase);
        }

        public static int IndexOfCI(this string hay, string needle) {
            return hay.IndexOf(needle, StringComparison.CurrentCultureIgnoreCase);
        }

        public static int IndexOfII(this string hay, string needle) {
            return hay.IndexOf(needle, StringComparison.InvariantCultureIgnoreCase);
        }
    }

    static class IOExtension
    {
        public static void CopyTo(this Stream input, Stream output) {
            byte[] buffer = new byte[65536];
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                output.Write(buffer, 0, read);
        }

        public static byte[] ReadBytes(this Stream input) {
            var memory = input as MemoryStream;
            if (memory != null)
                return memory.ToArray();
            byte[] buffer = new byte[input.Length];
            var read = input.Read(buffer, 0, buffer.Length);
            if (read != buffer.Length)
                throw new ApplicationException("Premature end of the stream.");
            return buffer;
        }

        public static IEnumerable<string> ReadLines(this TextReader input) {
            for (string line; (line = input.ReadLine()) != null; )
                yield return line;
        }
    }

    static class BinaryReaderExtension
    {
        public static string[] ReadStrings(this BinaryReader reader) {
            var count = reader.ReadInt32();
            if (count < 0)
                return null;
            var values = new string[count];
            for (var i = 0; i < count; ++i)
                values[i] = reader.ReadString();
            return values;
        }

        public static string ReadText(this BinaryReader reader) {
            return reader.ReadBoolean() ? reader.ReadString() : null;
        }

        public static Date ReadDate(this BinaryReader reader) {
            return new Date(reader.ReadInt64());
        }

        public static DateTime ReadDateTime(this BinaryReader reader) {
            return new DateTime(reader.ReadInt64());
        }

        public static YearSpan ReadYearSpan(this BinaryReader reader) {
            var first = reader.ReadInt32();
            return first < 0 ? YearSpan.Empty : new YearSpan(first, reader.ReadInt32());
        }

        public static object ReadValue(this BinaryReader reader, Type type) {
            if (type == typeof(int))
                return reader.ReadInt32();
            if (type == typeof(string))
                return reader.ReadText();
            if (type == typeof(DateTime))
                return reader.ReadDateTime();
            if (type == typeof(Date))
                return reader.ReadDate();
            if (type == typeof(YearSpan))
                return reader.ReadYearSpan();
            if (type == typeof(string[]))
                return reader.ReadStrings();
            throw new InvalidOperationException("Unsupported type.");
        }
    }

    static class BinaryWriterExtension
    {
        public static void Write(this BinaryWriter writer, string[] values) {
            if (values != null) {
                writer.Write(values.Length);
                foreach (var value in values)
                    writer.Write(value);
            } else {
                writer.Write(-1);
            }
        }

        public static void WriteText(this BinaryWriter writer, string value) {
            if (value == null) {
                writer.Write(false);
            } else {
                writer.Write(true);
                writer.Write(value);
            }
        }

        public static void Write(this BinaryWriter writer, DateTime value) {
            writer.Write(value.Ticks);
        }

        public static void Write(this BinaryWriter writer, Date value) {
            writer.Write(value.Ticks);
        }

        public static void Write(this BinaryWriter writer, YearSpan value) {
            if (value.IsEmpty) {
                writer.Write(-1);
            } else {
                writer.Write(value.First);
                writer.Write(value.Last);
            }
        }

        public static void Write(this BinaryWriter writer, object value, Type type) {
            if (type == typeof(int))
                writer.Write((int) value);
            else if (type == typeof(string))
                writer.WriteText((string) value);
            else if (type == typeof(DateTime))
                writer.Write((DateTime) value);
            else if (type == typeof(Date))
                writer.Write((Date) value);
            else if (type == typeof(YearSpan))
                writer.Write((YearSpan) value);
            else if (type == typeof(string[]))
                writer.Write((string[]) value);
            else
                throw new InvalidOperationException("Unsupported type.");
        }
    }

    static class XmlReaderExtension
    {
        public static bool ReadToFollowingElement(this XmlReader reader) {
            while (reader.Read())
                if (reader.NodeType == XmlNodeType.Element)
                    return true;
            return false;
        }

        public static Date ReadElementContentAsDate(this XmlReader reader) {
            var value = reader.ReadElementContentAsString();
            return Date.ParseExact(value, "s", CultureInfo.InvariantCulture);
        }

        public static YearSpan ReadElementContentAsYearSpan(this XmlReader reader) {
            var value = reader.ReadElementContentAsString();
            return YearSpan.Parse(value, CultureInfo.InvariantCulture);
        }

        public static IEnumerable<string> ReadElementContentAsStrings(this XmlReader reader) {
            using (var subreader = reader.ReadSubtree()) {
                var value = subreader.ReadToFollowingElement();
                while (subreader.ReadToFollowingElement())
                    yield return subreader.ReadElementContentAsString();
            }
        }

        public static object ReadElementContentAs(this XmlReader reader, Type type) {
            if (type == typeof(int))
                return reader.ReadElementContentAsInt();
            if (type == typeof(string))
                return reader.ReadElementContentAsString();
            if (type == typeof(DateTime))
                return reader.ReadElementContentAsDateTime();
            if (type == typeof(Date))
                return reader.ReadElementContentAsDate();
            if (type == typeof(YearSpan))
                return reader.ReadElementContentAsYearSpan();
            if (type == typeof(string[]))
                return reader.ReadElementContentAsStrings();
            throw new InvalidOperationException("Unsupported type.");
        }
    }

    static class XmlWriterExtension
    {
        public static void WriteValueElement(this XmlWriter writer, string name, string value) {
            if (value != null)
                writer.WriteElementString(name, value);
        }

        public static void WriteValueElement(this XmlWriter writer, string name, int value) {
            if (value != 0) {
                writer.WriteStartElement(name);
                writer.WriteValue(value);
                writer.WriteEndElement();
            }
        }

        public static void WriteValueElement(this XmlWriter writer, string name, DateTime value) {
            if (value != DateTime.MinValue) {
                writer.WriteStartElement(name);
                writer.WriteValue(value);
                writer.WriteEndElement();
            }
        }

        public static void WriteValueElement(this XmlWriter writer, string name, Date value) {
            if (value != Date.MinValue)
                writer.WriteElementString(name, value.ToString("s", CultureInfo.InvariantCulture));
        }

        public static void WriteValueElement(this XmlWriter writer, string name, YearSpan value) {
            if (!value.IsEmpty)
                writer.WriteElementString(name, value.ToString(CultureInfo.InvariantCulture));
        }

        public static void WriteValueElement(this XmlWriter writer, string name,
                                             IEnumerable<string> values) {
            if (values != null) {
                writer.WriteStartElement(name);
                // Cut the trailing 's' from the element name.
                name = name.Substring(0, name.Length - 1);
                foreach (var value in values)
                    writer.WriteElementString(name, value);
                writer.WriteEndElement();
            }
        }

        public static void WriteValueElement(this XmlWriter writer, string name, object value) {
            if (value != null) {
                var type = value.GetType();
                if (type == typeof(int))
                    writer.WriteValueElement(name, (int) value);
                else if (type == typeof(string))
                    writer.WriteValueElement(name, (string) value);
                else if (type == typeof(DateTime))
                    writer.WriteValueElement(name, (DateTime) value);
                else if (type == typeof(Date))
                    writer.WriteValueElement(name, (Date) value);
                else if (type == typeof(YearSpan))
                    writer.WriteValueElement(name, (YearSpan) value);
                else if (type == typeof(string[]))
                    writer.WriteValueElement(name, (string[]) value);
                throw new InvalidOperationException("Unsupported type.");
            }
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
