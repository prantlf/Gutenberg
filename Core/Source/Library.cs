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
using System.Text;
using System.Xml;

namespace Gutenberg
{
    // Represents information about a book.
    public class Book
    {
        // The book number from the Project Gutenberg catalog.
        [Property]
        public int Number { get; internal set; }

        // The book title.
        [Property]
        public string Title { get; internal set; }

        // The book title with its author. It can be used as a kind of unique name because
        // multiple books can have the same title. (If there are more authors just the first
        // one is appended.)
        public string FriendlyTitle {
            get {
                if (friendlyTitle == null)
                    // There are actually quite a few books with no author there...
                    friendlyTitle = Authors == null || Authors.Length == 0 ? Title :
                                    Title + " by " + Authors[0];
                return friendlyTitle;
            }
        }
        string friendlyTitle;

        // Authors of the book.
        [Property]
        public string[] Authors { get; internal set; }

        // Other contributors like illustrators, or people who re-typed the book for the Project
        // Gutenberg.
        [Property]
        public string[] Contributors { get; internal set; }

        // Very approximate year span when the book was written. It is computed to span across
        // lives of all authors and contributors.
        [Property]
        public YearSpan Era { get; internal set; }

        // The two-letter ISO code of the language the book was written in.
        [Property]
        public string Language { get; internal set; }

        // Notes about the book.
        [Property]
        public string[] Notes { get; internal set; }

        // Tags of the book. They can be used to infer the book category but the Project
        // Gutenberg doesn't seem to maintain the categories well here - it's a mess.
        [Property]
        public string[] Tags { get; internal set; }

        // The date when the book was included in the Project Gutenberg catalog.
        [Property]
        public Date Included { get; internal set; }

        // The number of downloads of the book. I doubt that it is real.
        [Property]
        public int Downloads { get; internal set; }

        // Returns a list of MIME types of all volumes available for this book.
        public string[] Formats {
            get {
                if (formats == null && Volumes != null)
                    // THere can be multiple book volumes and every volume can have multiple MIME
                    // types assigned. Let's return a distinct list of MIME types.
                    formats = Volumes.Where(item => item.Formats != null).SelectMany(
                        item => item.Formats).Distinct(
                            ConfigurableComparer<string>.CaseInsensitive).ToArray();
                return formats;
            }
        }
        string[] formats;

        // Returns (Project Gutenberg-) relative URLs of all volumes available for this book.
        public string[] Files {
            get {
                if (files == null && Volumes != null)
                    // It is next to impossible that two cvolumes would share the same URL but
                    // well, getting a volume by URL will look for the first one matching; let's
                    // return a distinct list here not to promise too much here...
                    files = Volumes.Select(item => PathUtility.GetChildName(item.URL)).Distinct(
                        ConfigurableComparer<string>.CaseInsensitive).ToArray();
                return files;
            }
        }
        string[] files;

        // Providesall volumes available for this book. This property is supposed to be set when
        // the catalog is loaded to memory from the separate book and volume sources and these
        // are being assigned to each other.
        public IEnumerable<Volume> Volumes { get; internal set; }

        // Gets a volume of this book. The identifier can be the (index) number of the volume,
        // the MIME type (or its start) or the relative URL (or its end) of the volume.
        public Volume GetVolume(string identifier) {
            if (Volumes == null || !Volumes.Any())
                throw new ApplicationException("No volumes available.");
            // Every volume can have more formats (MIME types) assigned. Choose the first one
            // that starts with the specified value. It helps when dealing with MIME types with
            // charset; you don't  need to know the actual charset when you want the text/plain
            // rendition, for example.
            Volume volume;
            int number;
            // Accepting no identifier is convenient for the callers which get the value from the
            // user and it is optional. If the user doesn;t care give them the first we have.
            if (string.IsNullOrEmpty(identifier)) {
                volume = Volumes.First();
            } else if (int.TryParse(identifier, NumberStyles.Integer,
                                CultureInfo.InvariantCulture, out number)) {
                volume = Volumes.ElementAtOrDefault(number);
                if (volume == null)
                    throw new ApplicationException("Volume not available.");
            // There is no easy distinction between a MIME type and a relative URL feasible by
            // simple string operations. There should be no false hits though, because their
            // values are usually very different for the Project Gutenberg book volumes.
            } else {
                volume = Volumes.FirstOrDefault(item => item.Formats.Any(
                    format => format.StartsWithII(identifier)));
                if (volume == null)
                    volume = Volumes.FirstOrDefault(item => item.URL.EndsWithCI(identifier));
                if (volume == null)
                    throw new ApplicationException("Format or file not available.");
            }
            return volume;
        }

        // Deserializes the book information from a binary book source.
        public static Book Read(BinaryReader reader) {
            return SerializationUtility.Read<Book>(reader, Properties);
        }

        // Serializes the book information to a binary book source.
        public void Write(BinaryWriter writer) {
            SerializationUtility.Write(writer, this, Properties);
        }

        // Deserializes the book information from an XML book source.
        public static Book Read(XmlReader reader) {
            return SerializationUtility.Read<Book>(reader, Properties);
        }

        // Parses the book information from a string containing the XML book representation
        // which was produced by the ToString method.
        public static Book Parse(string source) {
            using (var reader = XmlReader.Create(new StringReader(source)))
                return Read(reader);
        }

        // Serializes the book information to an XML book source.
        public void Write(XmlWriter writer) {
            SerializationUtility.Write(writer, this, Properties);
        }

        // Formats an XML with the book information. It can be parsed by the Parse method.
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

        // The list of serializable properties for a book to control the common serialization and
        // deserialization utilities. They touch only properties with the Propety attribute.
        public static readonly Property[] Properties =
            SerializationUtility.GetProperties(typeof(Book)).ToArray();
    }

    // Provides access to a book source.
    public interface Books : Loggable
    {
        // Checks if the content of this source is available.
        bool HasBooks { get; }

        // Enumerates over all book volumes in this source.
        IEnumerable<Book> GetBooks();
    }

    // Represents information about a book volume. A book volume is a file with the book content.
    // The content can have different formats which are capable of storing complete or partial
    // content of the original (paper) book. You may call it a rendition too.
    public class Volume
    {
        // The number of the related book from the Project Gutenberg catalog.
        [Property]
        public int Number { get; internal set; }

        // The (Project Gutenberg -) relative URL of this book volume.
        [Property]
        public string URL { get; internal set; }

        // Returns a list of MIME types for this this book volume. More MIME types may be needed
        // to characterize a book volume content.
        [Property]
        public string[] Formats { get; internal set; }

        // The date of the most recent upload of this book volume to the Project Gutenberg web.
        [Property]
        public Date Uploaded { get; internal set; }

        // The size of the file with the book volume.
        [Property]
        public int Size { get; internal set; }

        // Deserializes the book volume information from a binary book volume source.
        public static Volume Read(BinaryReader reader) {
            return SerializationUtility.Read<Volume>(reader, Properties);
        }

        // Serializes the book volume information to a binary book volume source.
        public void Write(BinaryWriter writer) {
            SerializationUtility.Write(writer, this, Properties);
        }

        // The list of serializable properties for a book volume to control the common
        // serialization and deserialization utilities. They touch only properties with the
        // Propety attribute.
        internal static readonly Property[] Properties =
            SerializationUtility.GetProperties(typeof(Volume)).ToArray();
    }

    // Provides access to a book volume source.
    public interface Volumes : Loggable
    {
        // Checks if the content of this source is available.
        bool HasVolumes { get; }

        // Enumerates over all book volumes in this source.
        IEnumerable<Volume> GetVolumes();
    }
}
