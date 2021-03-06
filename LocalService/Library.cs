﻿// Copyright (C) 2013 Ferdinand Prantl <prantlf@gmail.com>
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

using System;
using System.IO;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace Gutenberg.LocalService
{
    [ServiceContract, XmlSerializerFormat]
    public interface Library
    {
        [OperationContract]
        Book GetBook(int number);

        [OperationContract]
        Book[] GetBooks();

        [OperationContract]
        Stream ReadContent(int number);

        [OperationContract]
        Volume ReadVolume(int number);

        [OperationContract]
        Card[] EnumerateCards(int lastNumberSeen);

        [OperationContract]
        Book[] FindBooks(Filter filter, int start, int count);
    }

    [DataContract]
    public class Filter
    {
        [DataMember(IsRequired = true)]
        public string Title { get; set; }

        [DataMember(IsRequired = true)]
        public string Author { get; set; }

        [DataMember(IsRequired = true)]
        public string Contributor { get; set; }

        [DataMember(IsRequired = true)]
        public string Era { get; set; }

        [DataMember(IsRequired = true)]
        public string Language { get; set; }

        [DataMember(IsRequired = true)]
        public string Format { get; set; }

        [DataMember(IsRequired = true)]
        public string Note { get; set; }

        [DataMember(IsRequired = true)]
        public string Tag { get; set; }
    }

    [DataContract]
    public class Card
    {
        [DataMember(IsRequired = true)]
        public int Number;

        [DataMember(IsRequired = true)]
        public DateTime LastModified;
    }

    [DataContract]
    public class Book
    {
        [DataMember(IsRequired = true)]
        public int Number;

        [DataMember(IsRequired = true)]
        public DateTime LastModified;

        [DataMember(IsRequired = true)]
        public string Title;

        [DataMember(IsRequired = true)]
        public string Author { get; set; }

        [DataMember(IsRequired = true)]
        public string Contributor { get; set; }

        [DataMember(IsRequired = true)]
        public string Notes { get; set; }

        [DataMember(IsRequired = true)]
        public string Tags { get; set; }

        [DataMember(IsRequired = true)]
        public string Language { get; set; }

        [DataMember(IsRequired = true)]
        public int Size;

        [DataMember(IsRequired = true)]
        public string Era;

        [DataMember(IsRequired = true)]
        public DateTime Included;

        [DataMember(IsRequired = true)]
        public int Downloads;

        [DataMember(IsRequired = true)]
        public string FileName;

        [DataMember(IsRequired = true)]
        public string MimeType;
    }

    [DataContract]
    public class Volume
    {
        [DataMember(IsRequired = true)]
        public string FileName;

        [DataMember(IsRequired = true)]
        public string MimeType;

        [DataMember(IsRequired = true)]
        public byte[] Content;
    }

    static class Extensions
    {
        public const string MimeType = "text/plain";

        public static Card ToCard(this Gutenberg.Book book) {
            return new Card {
                Number = book.Number,
                LastModified = new DateTime(book.GetVolume(MimeType).Uploaded.Ticks)
            };
        }

        public static Book ToBook(this Gutenberg.Book book) {
            var volume = book.GetVolume(MimeType);
            return new Book {
                Number = book.Number, Title = book.Title, MimeType = MimeType,
                Language = book.Language, LastModified = new DateTime(volume.Uploaded.Ticks),
                Size = volume.Size, FileName = book.GetFileName(), Era = book.Era.ToString(),
                Author = book.Authors == null ? null : string.Join(", ", book.Authors),
                Contributor = book.Contributors == null ? null :
                                    string.Join(", ", book.Contributors),
                Notes = book.Notes == null ? null : string.Join("\r\n", book.Notes),
                Tags = book.Tags == null ? null : string.Join("\r\n", book.Tags),
                Downloads = book.Downloads, Included = new DateTime(book.Included.Ticks)
            };
        }

        public static Volume ToVolume(this Gutenberg.Book book, byte[] content) {
            return new Volume {
                MimeType = MimeType, FileName = book.GetFileName(), Content = content
            };
        }

        static string GetFileName(this Gutenberg.Book book) {
            return book.FriendlyTitle.Replace("\r\n", " - ").Replace("\n", " - ").
                                            Replace("\r", " - ") + ".txt";
        }
    }
}