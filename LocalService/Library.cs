// Copyright (C) 2013 Ferdinand Prantl <prantlf@gmail.com>
// All rights reserved.       
//
// This file is part of the Project Gutenberg Local Web Service
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
using System.Runtime.Serialization;
using System.ServiceModel;

namespace Gutenberg.LocalService
{
    [ServiceContract]
    public interface Library
    {
        [OperationContract]
        Book GetBook(string name);

        [OperationContract]
        Book[] GetBooks(int start, int count);
    }

    [DataContract]
    public class Book
    {
        [DataMember]
        public int Number { get; set; }

        [DataMember]
        public string Title { get; set; }

        [DataMember]
        public string[] Authors { get; set; }

        [DataMember]
        public string[] Contributors { get; set; }

        [DataMember]
        public YearSpan Era { get; set; }

        [DataMember]
        public string Language { get; set; }

        [DataMember]
        public string[] Notes { get; set; }

        [DataMember]
        public string[] Tags { get; set; }

        [DataMember]
        public Date Included { get; set; }

        [DataMember]
        public int Downloads { get; set; }

        [DataMember]
        public Volume[] Volumes { get; set; }

        internal static Book Create(Gutenberg.Book book) {
            return new Book {
                Number = book.Number, Title = book.Title, Authors = book.Authors,
                Contributors = book.Contributors, Era = book.Era, Language = book.Language,
                Notes = book.Notes, Tags = book.Tags, Included = book.Included,
                Downloads = book.Downloads, Volumes = book.Volumes == null ? null :
                    book.Volumes.Select(volume => Volume.Create(volume)).ToArray()
            };
        }
    }

    [DataContract]
    public class Volume
    {
        [DataMember]
        public int Number { get; set; }

        [DataMember]
        public string URL { get; set; }

        [DataMember]
        public string[] Formats { get; set; }

        [DataMember]
        public Date Uploaded { get; set; }

        [DataMember]
        public int Size { get; set; }

        internal static Volume Create(Gutenberg.Volume volume) {
            return new Volume {
                Number = volume.Number, URL = volume.URL, Formats = volume.Formats,
                Uploaded = volume.Uploaded, Size = volume.Size
            };
        }
    }
}
