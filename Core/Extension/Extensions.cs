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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Text;
using System.Xml;

namespace Gutenberg
{
    // Additional methods and method overloads which would be useful in particular classes. They
    // are organized by the class which they attach to.

    public static class StringExtension
    {
        // If your string cannto be null you can test it for emptiness with shorter expression
        // and/or maybe better performance than the string.IsNullOrEmty provides. Well, I use it
        // because of the former benefit; the latter would be not worth doing this...
        public static bool IsEmpty(this string s) {
            return s.Length == 0;
        }

        // Methods StartsWith and EndsWith do not have overloads with a single char.

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

        // These overloads save time when writing the most common comparison options. The
        // abbreviation CI means using the flag CurrentCultureIgnoreCase for the string
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

        // If the whildcard matching is used often the following methods save time. They depend
        // on the WildcardPattern class from the System.Management.Automation namespace.
    
        public static bool Like(this string hay, string needle) {
            if (WildcardPattern.ContainsWildcardCharacters(needle)) {
                var pattern = new WildcardPattern(needle, WildcardOptions.None);
                return pattern.IsMatch(hay);
            }
            return hay.Contains(needle);
        }

        public static bool LikeCI(this string hay, string needle) {
            if (WildcardPattern.ContainsWildcardCharacters(needle)) {
                var pattern = new WildcardPattern(needle, WildcardOptions.IgnoreCase);
                return pattern.IsMatch(hay);
            }
            return hay.ContainsCI(needle);
        }

        public static bool LikeII(this string hay, string needle) {
            if (WildcardPattern.ContainsWildcardCharacters(needle)) {
                var pattern = new WildcardPattern(needle, WildcardOptions.IgnoreCase |
                WildcardOptions.CultureInvariant);
                return pattern.IsMatch(hay);
            }
            return hay.ContainsII(needle);
        }
    }

    public static class IOExtension
    {
        // Stream and TextReader lack eading or transferring the entire content by a single
        // method; at least in .NET 2.0.

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
            var start = 0, read = 0;
            for (; (read = input.Read(buffer, start, buffer.Length - start)) > 0;
                        start += read) {}
            if (start != buffer.Length)
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
        // Methods handling additional data types; basic set of data types is built in to the
        // BinaryReader.

        public static string[] ReadStrings(this BinaryReader reader) {
            var count = reader.ReadInt32();
            // If the string array was null the method Write wrote -1 for us to recognize it.
            if (count < 0)
                return null;
            // The array was serialized in a classic way - count first and the items next to
            // allow the complete array allocation when deserializing it.
            var values = new string[count];
            for (var i = 0; i < count; ++i)
                values[i] = reader.ReadString();
            return values;
        }

        public static string ReadText(this BinaryReader reader) {
            // The method Write preceeds every string with a bool; false to mark a null string
            // and true to preceed the actual string value.
            return reader.ReadBoolean() ? reader.ReadString() : null;
        }

        public static Date ReadDate(this BinaryReader reader) {
            return new Date(reader.ReadInt64());
        }

        public static DateTime ReadDateTime(this BinaryReader reader) {
            return new DateTime(reader.ReadInt64());
        }

        public static YearSpan ReadYearSpan(this BinaryReader reader) {
            // A year span is serialized as two integers - first and last year. If the year span
            // was empty the writer wrote the first number -1 for us to recognize it.
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
            // More types can be added; it is just that they were not needed for this project.
            throw new InvalidOperationException("Unsupported type.");
        }
    }

    static class BinaryWriterExtension
    {
        // Methods handling additional data types; basic set of data types is built in to the
        // BinaryWriter.

        public static void Write(this BinaryWriter writer, string[] values) {
            if (values != null) {
                // The array is serialized in a classic way - count first and the items next to
                // allow the complete array allocation when deserializing it.
                writer.Write(values.Length);
                foreach (var value in values)
                    writer.Write(value);
            } else {
                // If the string array is null the method Read will be able to recognize it by
                // detecting a negative (invalid) array length.
                writer.Write(-1);
            }
        }

        public static void WriteText(this BinaryWriter writer, string value) {
            // We preceed every string with a bool; false to mark a null string and true to
            // preceed the actual string value. This is used during deserialization.
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
            // An empty year span is recognized during the deserialization by noticing a negative
            // (invalid) value for the first year written. A valid year span has positive numbers.
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
                // More types can be added; it is just that they were not needed in this project.
                throw new InvalidOperationException("Unsupported type.");
        }
    }

    static class XmlReaderExtension
    {
        // Surprisingly, moving the reader position to the nearest element and skipping the
        // whitespace and/or other content is missing in the standard XmlReader.
        public static bool ReadToFollowingElement(this XmlReader reader) {
            while (reader.Read())
                if (reader.NodeType == XmlNodeType.Element)
                    return true;
            return false;
        }

        // Methods handling additional data types; basic set of data types is built in to the
        // XmlReader.

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
                // The sub-reader is not set up at the position of the first child element; it
                // starts somewhere in front of the parentin was created from. Firstly, we make
                // sure we point to the parent and then we walk through the children one by one.
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
            // More types can be added; it is just that they were not needed in this project.
            throw new InvalidOperationException("Unsupported type.");
        }
    }

    static class XmlWriterExtension
    {
        // Methods handling additional data types; basic set of data types is built in to the
        // XmlWriter. In general, the methods avoid writing anything if being given a null value
        // or a value which is default for the particular type; like 0 for int. In comparison
        // with binary serialization, XML can be deserialized using the element names and such
        // (missing) default value can be detected by the entirely missing element.

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
                // Cut the trailing 's' from the element name. Not generic enough but simple and
                // working for this project so far.
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
                // More types can be added; it is just that they were not needed in this project.
                throw new InvalidOperationException("Unsupported type.");
            }
        }
    }

    public static class AssemblyExtension
    {
        // If you use often multiple assembly attributes this typed getter will come handy.
        public static T GetAssemblyAttribute<T>(this Assembly assembly) {
            try {
                var attributes = assembly.GetCustomAttributes(false);
                return (T) attributes.First(item => item is T);
            } catch (InvalidOperationException) {
                throw new ApplicationException(string.Format(
                    "The assembly {0} does not have the attribute {1}.", assembly, typeof(T)));
            }
        }
    }
}
