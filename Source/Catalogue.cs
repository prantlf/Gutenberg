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
using System.Xml;

namespace GutenPosh
{
    class Catalogue : RemoteSource
    {
        public XmlReader Open(string path) {
            var stream = OpenUnpacked(path);
            try {
                return XmlReader.Create(stream, new XmlReaderSettings {
                    MaxCharactersFromEntities = 0, MaxCharactersInDocument = 0,
                    ProhibitDtd = false
                });
            } catch {
                stream.Close();
                throw;
            }
        }

        public Date GetCreated(XmlReader reader) {
            Log.Verbose("Getting creation date...");
            if (!reader.ReadToFollowing("Description", RDF))
                throw new ApplicationException("Missing creation time.");
            if (!reader.ReadToDescendant("value", RDF))
                throw new ApplicationException("Missing creation date value.");
            return reader.ReadElementContentAsDate();
        }

        public IEnumerable<object> GetItems(XmlReader reader) {
            Log.Verbose("Getting items...");
            while (reader.ReadToFollowingElement())
                switch (reader.LocalName) {
                case "etext":
                    var number = int.Parse(reader.GetAttribute("ID", RDF).Substring(5),
                                            CultureInfo.InvariantCulture);
                    Book book;
                    using (var subreader = reader.ReadSubtree())
                        try {
                            book = ParseBook(subreader);
                        } catch (Exception exception) {
                            throw new ApplicationException(string.Format(
                                "Parsing book {0} failed.", number), exception);
                        }
                    yield return book;
                    break;
                case "file":
                    var url = ParseVolumeUrl(reader);
                    Volume volume;
                    using (var subreader = reader.ReadSubtree())
                        try {
                            volume = ParseVolume(subreader);
                        } catch (Exception exception) {
                            throw new ApplicationException(string.Format(
                                "Parsing volume {0} failed.", url), exception);
                        }
                    yield return volume;
                    break;
                }
        }

        Book ParseBook(XmlReader reader) {
            var book = new Book();
            while (reader.Read())
                if (reader.NodeType == XmlNodeType.Element) {
                    string parseType;
                    switch (reader.LocalName) {
                    case "etext":
                        book.Number = int.Parse(reader.GetAttribute("ID", RDF).Substring(5),
                                                    CultureInfo.InvariantCulture);
                        break;
                    case "title":
                        parseType = reader.GetAttribute("parseType", RDF);
                        if (parseType != "Literal")
                            throw new ApplicationException("Unrecognized parse type.");
                        book.Title = reader.ReadElementContentAsString();
                        break;
                    case "description":
                        book.Notes = ParseText(reader);
                        break;
                    case "creator":
                        book.Authors = ParsePersonsAndEra(book, reader);
                        break;
                    case "contributor":
                        book.Contributors = ParsePersonsAndEra(book, reader);
                        break;
                    case "language":
                        if (!reader.ReadToDescendant("value", RDF))
                            throw new ApplicationException("Missing language value.");
                        book.Language = reader.ReadElementContentAsString();
                        break;
                    case "subject":
                        using (var subreader = reader.ReadSubtree())
                            book.Tags = ParseTags(subreader).ToArray();
                        break;
                    case "created":
                        if (!reader.ReadToDescendant("value", RDF))
                            throw new ApplicationException("Missing creation date value.");
                        book.Included = reader.ReadElementContentAsDate();
                        break;
                    case "downloads":
                        if (!reader.ReadToDescendant("value", RDF))
                            throw new ApplicationException("Missing download count value.");
                        book.Downloads = reader.ReadElementContentAsInt();
                        break;
                    }
                }
            return book;
        }

        Volume ParseVolume(XmlReader reader) {
            var volume = new Volume();
            var formats = new List<string>();
            while (reader.Read())
                if (reader.NodeType == XmlNodeType.Element)
                    switch (reader.LocalName) {
                    case "file":
                        volume.URL = ParseVolumeUrl(reader);
                        break;
                    case "format":
                        if (!reader.ReadToDescendant("value", RDF))
                            throw new ApplicationException("Missing format value.");
                        formats.Add(reader.ReadElementContentAsString());
                        break;
                    case "extent":
                        volume.Size = reader.ReadElementContentAsInt();
                        break;
                    case "modified":
                        if (!reader.ReadToDescendant("value", RDF))
                            throw new ApplicationException("Missing modified date value.");
                        volume.Uploaded = reader.ReadElementContentAsDate();
                        break;
                    case "isFormatOf":
                        volume.Number = int.Parse(reader.GetAttribute("resource", RDF).
                                                    Substring(6), CultureInfo.InvariantCulture);
                        break;
                    }
            volume.Formats = formats.Count > 0 ? formats.ToArray() : null;
            return volume;
        }

        string[] ParseText(XmlReader reader) {
            var parseType = reader.GetAttribute("parseType", RDF);
            if (parseType == "Literal")
                return new[] { reader.ReadElementContentAsString() };
            using (var subreader = reader.ReadSubtree())
                return ParseLines(subreader);
        }

        string[] ParseLines(XmlReader reader) {
            var lines = new List<string>();
            while (reader.ReadToFollowing("li", RDF)) {
                var parseType = reader.GetAttribute("parseType", RDF);
                if (parseType != "Literal")
                    throw new ApplicationException("Unrecognized parse type.");
                lines.Add(reader.ReadElementContentAsString());
            }
            return lines.Any() ? lines.ToArray() : null;
        }

        string[] ParsePersonsAndEra(Book book, XmlReader reader) {
            var parseType = reader.GetAttribute("parseType", RDF);
            if (parseType == "Literal") {
                var person = reader.ReadElementContentAsString();
                ParsePersonEra(book, person);
                return new[] { person };
            }
            using (var subreader = reader.ReadSubtree())
                return ParseMultiplePersonsAndEra(book, subreader);
        }

        string[] ParseMultiplePersonsAndEra(Book book, XmlReader reader) {
            var persons = new List<string>();
            while (reader.ReadToFollowing("li", RDF)) {
                var parseType = reader.GetAttribute("parseType", RDF);
                if (parseType != "Literal")
                    throw new ApplicationException("Unrecognized parse type.");
                string person = reader.ReadElementContentAsString();
                ParsePersonEra(book, person);
                persons.Add(person);
            }
            return persons.Any() ? persons.ToArray() : null;
        }

        void ParsePersonEra(Book book, string value) {
            YearSpan era;
            var comma = value.LastIndexOf(',');
            if (comma > 0 && YearSpan.TryParse(value.Substring(comma + 1).Trim(),
                                                CultureInfo.InvariantCulture, out era))
                book.Era = book.Era.Union(era);
        }

        IEnumerable<string> ParseTags(XmlReader reader) {
            while (reader.ReadToFollowing("value", RDF))
                yield return reader.ReadElementContentAsString();
        }

        string ParseVolumeUrl(XmlReader reader) {
            var url = reader.GetAttribute("about", RDF);
            if (url.StartsWith(ProjectUrl))
                url = url.Substring(25);
            return url;
        }

        Stream OpenUnpacked(string path) {
            Log.Verbose("Opening {0}...", path);
            if (ZipStorer.IsZip(path))
                using (var store = ZipStorer.Open(path, FileAccess.Read)) {
                    var entry = store.ReadCentralDir().First();
                    Log.Verbose("Extracting {0}...", entry.FilenameInZip);
                    var content = new MemoryStream();
                    if (!store.ExtractStream(entry, content)) {
                        content.Dispose();
                        throw new ApplicationException("Compression not supported.");
                    }
                    content.Position = 0;
                    return content;
                }
            else
                return File.OpenRead(path);
        }

        protected override string Url {
            get { return url; }
        }
        string url = "http://www.gutenberg.org/feeds/catalog.rdf.zip";

        public void SetUrl(string value) {
            url = value;
        }

        const string RDF = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
        public const string ProjectUrl = "http://www.gutenberg.org/";
    }
}
