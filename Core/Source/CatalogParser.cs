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
using System.Xml;

namespace Gutenberg
{
    // Parses the Project Gutenberg catalog in the RDF format.
    public class CatalogParser : LoggableBase
    {
        // Returns a new XML reader for a stream with the Project Gutenberg catalog RDF.
        public XmlReader Open(Stream stream) {
            // The catalog is a 250 MB XML file. It exceeds XmlReader's limits which are there
            // for performance or security reasons; let's turn them off. Prohibiting DTD from
            // appearing in the XML stream *by default* is really idiotic. you have to turn it
            // off anytime you process user data which you didn;t generate yourself. Practically,
            // you cannot parse a valid XML using the XmlReader created with default settings...
            return XmlReader.Create(stream, new XmlReaderSettings {
                MaxCharactersFromEntities = 0, MaxCharactersInDocument = 0,
                ProhibitDtd = false
            });
        }

        // Gets the creation date of the Project Gutenberg catalog. This method should be called
        // before parsing the books and volumes because the creation date is at the beginning;
        // calling it later would not find the XML element with the date any more and would
        // continue reading the content to the end, skipping all content. This method expects a
        // reader returned by the method Open.
        public Date GetCreated(XmlReader reader) {
            Log.Verbose("Getting creation date...");
            if (!reader.ReadToFollowing("Description", RDF))
                throw new ApplicationException("Missing creation time.");
            if (!reader.ReadToDescendant("value", RDF))
                throw new ApplicationException("Missing creation date value.");
            return reader.ReadElementContentAsDate();
        }

        // Enumerates over all items in the Project Gutenberg catalog. If you need the creation
        // date you must get it by calling the GetCreated method before this one. The returned
        // items can be either meta-data for books or files (book volumes). Books come first.
        public IEnumerable<object> GetItems(XmlReader reader) {
            Log.Verbose("Getting items...");
            while (reader.ReadToFollowingElement())
                switch (reader.LocalName) {
                case "etext":
                    // The number is parsed within the ParseBook method. This copy is for logging
                    // purposes only.
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
                    // The URL is parsed within the ParseVolume method. This copy is for logging
                    // purposes only.
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

        // Parses a single book.
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
                        // Eras of contributors do not affect the era of the book intentionally.
                        // Contributors might be not only illustrators, who would live at the
                        // same time as authors, but also people who retyped the book and
                        // uploaded it to the Project Gutenberg web site. They would mess the
                        // era of the book which is supposed to embrace the time when the actual
                        // paper book was written.
                        book.Contributors = ParsePersons(reader);
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

        // Parses a single book volume.
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

        // Parses a textual property; both single and multi-line.
        string[] ParseText(XmlReader reader) {
            // Checking the attribute parseType is a quick and dirty way to detect a multivalue
            // property without inspecting the entire element structure and luckily works here.
            var parseType = reader.GetAttribute("parseType", RDF);
            if (parseType == "Literal")
                return new[] { reader.ReadElementContentAsString() };
            using (var subreader = reader.ReadSubtree())
                return ParseLines(subreader);
        }

        // Parses the content of a multiline property. It is to be called from the ParseText
        // method only.
        string[] ParseLines(XmlReader reader) {
            var lines = new List<string>();
            while (reader.ReadToFollowing("li", RDF)) {
                // So far, I haven't noticed other multivalues than those consisting of literals.
                // When they occur I'll implement better vaue parsing here. Now I save time.
                var parseType = reader.GetAttribute("parseType", RDF);
                if (parseType != "Literal")
                    throw new ApplicationException("Unrecognized parse type.");
                lines.Add(reader.ReadElementContentAsString());
            }
            return lines.Any() ? lines.ToArray() : null;
        }

        // Parses information about single or multiple persons (authors or contributors). Names
        // of the persons are returned; they usually include also the life span of the person.
        string[] ParsePersons(XmlReader reader) {
            // Checking the attribute parseType is a quick and dirty way to detect a multivalue
            // property without inspecting the entire element structure and luckily works here.
            var parseType = reader.GetAttribute("parseType", RDF);
            if (parseType == "Literal")
                return new[] { reader.ReadElementContentAsString() };
            using (var subreader = reader.ReadSubtree())
                return ParseMultiplePersons(subreader);
        }

        // Parses the subtree of multiple persons. It is to be called from the ParsePersons
        // method only.
        string[] ParseMultiplePersonsAndEra(XmlReader reader) {
            var persons = new List<string>();
            while (reader.ReadToFollowing("li", RDF)) {
                // So far, I haven't noticed other multivalues than those consisting of literals.
                // When they occur I'll implement better vaue parsing here. Now I save time.
                var parseType = reader.GetAttribute("parseType", RDF);
                if (parseType != "Literal")
                    throw new ApplicationException("Unrecognized parse type.");
                persons.Add(reader.ReadElementContentAsString());
            }
            return persons.Any() ? persons.ToArray() : null;
        }

        // Parses information about single or multiple persons (authors or contributors) which
        // contain era (year span) of their life. Names of the persons are returned and the
        // era in the book is updated to span across all life spans of all persons.
        string[] ParsePersonsAndEra(Book book, XmlReader reader) {
            // Checking the attribute parseType is a quick and dirty way to detect a multivalue
            // property without inspecting the entire element structure and luckily works here.
            var parseType = reader.GetAttribute("parseType", RDF);
            if (parseType == "Literal") {
                var person = reader.ReadElementContentAsString();
                ParsePersonEra(book, person);
                return new[] { person };
            }
            using (var subreader = reader.ReadSubtree())
                return ParseMultiplePersonsAndEra(book, subreader);
        }

        // Parses the subtree of multiple persons. It is to be called from the ParsePersonsAndEra
        // method only.
        string[] ParseMultiplePersonsAndEra(Book book, XmlReader reader) {
            var persons = new List<string>();
            while (reader.ReadToFollowing("li", RDF)) {
                // So far, I haven't noticed other multivalues than those consisting of literals.
                // When they occur I'll implement better vaue parsing here. Now I save time.
                var parseType = reader.GetAttribute("parseType", RDF);
                if (parseType != "Literal")
                    throw new ApplicationException("Unrecognized parse type.");
                string person = reader.ReadElementContentAsString();
                ParsePersonEra(book, person);
                persons.Add(person);
            }
            return persons.Any() ? persons.ToArray() : null;
        }

        // Extracts the era of a person's life from the line with the person's name and updates
        // the book so that its era includes the person's one.
        void ParsePersonEra(Book book, string value) {
            YearSpan era;
            var comma = value.LastIndexOf(',');
            if (comma > 0 && YearSpan.TryParse(value.Substring(comma + 1).Trim(),
                                                CultureInfo.InvariantCulture, out era))
                book.Era = book.Era.Union(era);
        }

        // Parses the tags of the current book.
        IEnumerable<string> ParseTags(XmlReader reader) {
            while (reader.ReadToFollowing("value", RDF))
                yield return reader.ReadElementContentAsString();
        }

        // Parses the URL of a book volume.
        string ParseVolumeUrl(XmlReader reader) {
            var url = reader.GetAttribute("about", RDF);
            // If the volume URL starts with the Project Gutenberg web site then cut it. We don't
            // need to have it stored for every book volume; it can be prepended when needed.
            if (url.StartsWithCI(ProjectUrl))
                url = url.Substring(ProjectUrl.Length);
            // Why is the Project Gutenberg web site checked once more? The ProjectUrl can be
            // customized to point to other web site or to a local directory which would contain
            // a copy of the original catalog with the original URLs. Doh.
            else if (url.StartsWithCI("http://www.gutenberg.org/"))
                url = url.Substring(25);
            return url;
        }

        // The namespace of XML elements and attributes with the information about books and
        // book volumes.
        const string RDF = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";

        // URL of the Project Gutenberg web site root. The default value is read from the
        // application settings property ProjectURL.
        public static readonly string ProjectUrl = Settings.GetValue<string>(
                                                        typeof(CatalogParser), "ProjectURL");
    }
}
