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
using System.Text;
using System.Xml;

namespace GutenPosh
{
    public class Book
    {
        [Property]
        public int Number { get; internal set; }

        [Property]
        public string Title { get; internal set; }

        public string FriendlyTitle {
            get {
                if (friendlyTitle == null)
                    friendlyTitle = Authors == null || Authors.Length == 0 ? Title :
                                    Title + " by " + Authors[0];
                return friendlyTitle;
            }
        }
        string friendlyTitle;

        [Property]
        public string[] Authors { get; internal set; }

        [Property]
        public string[] Contributors { get; internal set; }

        [Property]
        public YearSpan Era { get; internal set; }

        [Property]
        public string Language { get; internal set; }

        [Property]
        public string[] Notes { get; internal set; }

        [Property]
        public string[] Tags { get; internal set; }

        [Property]
        public Date Included { get; internal set; }

        [Property]
        public int Downloads { get; internal set; }

        public string[] Formats {
            get {
                if (formats == null && Volumes != null)
                    formats = Volumes.Where(item => item.Formats != null).SelectMany(
                        item => item.Formats).Distinct(
                            ConfigurableComparer<string>.CaseInsensitive).ToArray();
                return formats;
            }
        }
        string[] formats;

        public string[] Files {
            get {
                if (files == null && Volumes != null)
                    files = Volumes.Select(item => PathUtility.GetChildName(item.URL)).Distinct(
                        ConfigurableComparer<string>.CaseInsensitive).ToArray();
                return files;
            }
        }
        string[] files;

        public IEnumerable<Volume> Volumes { get; internal set; }

        public Volume GetVolume(string identifier) {
            if (Volumes == null || !Volumes.Any())
                throw new ApplicationException("No volumes available.");
            // Every volume can have more formats (MIME types) assigned. Choose the first one
            // that starts with the specified value not to have to enter the entire MIME type
            // including the charset part.
            Volume volume;
            int number;
            if (string.IsNullOrEmpty(identifier)) {
                volume = Volumes.First();
            } else if (int.TryParse(identifier, NumberStyles.Integer,
                                CultureInfo.InvariantCulture, out number)) {
                volume = Volumes.ElementAtOrDefault(number);
                if (volume == null)
                    throw new ApplicationException("Volume not available.");
            } else if (identifier.Contains('/')) {
                volume = Volumes.FirstOrDefault(item => item.Formats.Any(
                    format => format.StartsWithII(identifier)));
                if (volume == null)
                    throw new ApplicationException("Format not available.");
            } else {
                volume = Volumes.FirstOrDefault(item => item.URL.EndsWithCI(identifier));
                if (volume == null)
                    throw new ApplicationException("File not available.");
            }
            return volume;
        }

        public static Book Read(BinaryReader reader) {
            return IOUtility.Read<Book>(reader, Properties);
        }

        public void Write(BinaryWriter writer) {
            IOUtility.Write(writer, this, Properties);
        }

        public static Book Read(XmlReader reader) {
            return IOUtility.Read<Book>(reader, Properties);
        }

        public static Book Parse(string source) {
            using (var reader = XmlReader.Create(new StringReader(source)))
                return Read(reader);
        }

        public void Write(XmlWriter writer) {
            IOUtility.Write(writer, this, Properties);
        }

        public override string ToString() {
            var builder = new StringBuilder();
            using (var writer = XmlWriter.Create(builder, new XmlWriterSettings {
                Encoding = Encoding.UTF8, Indent = true, OmitXmlDeclaration = true
            })) {
                writer.WriteStartElement("Book");
                Write(writer);
                writer.WriteEndElement();
            }
            return builder.ToString();
        }

        internal static readonly Property[] Properties =
            IOUtility.GetProperties(typeof(Book)).ToArray();
    }

    public class Volume
    {
        [Property]
        public int Number { get; set; }

        [Property]
        public string URL { get; internal set; }

        [Property]
        public string[] Formats { get; internal set; }

        [Property]
        public Date Uploaded { get; internal set; }

        [Property]
        public int Size { get; internal set; }

        public static Volume Read(BinaryReader reader) {
            return IOUtility.Read<Volume>(reader, Properties);
        }

        public void Write(BinaryWriter writer) {
            IOUtility.Write(writer, this, Properties);
        }

        internal static readonly Property[] Properties =
            IOUtility.GetProperties(typeof(Volume)).ToArray();
    }
}
