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

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ServiceModel.Activation;
using System.Text;
using Gutenberg.FileSystem;

namespace Gutenberg.LocalService
{
    [AspNetCompatibilityRequirements(RequirementsMode =
        AspNetCompatibilityRequirementsMode.Allowed)]
    public class LibraryService : Library
    {
        public Book GetBook(int number) {
            Log.Verbose("Getting the book {0}.", number);
            return OpenBook(number).ToBook();
        }

        public Book[] GetBooks() {
            Log.Verbose("Getting the first 100 books.");
            return Books.Take(100).Select(book => book.ToBook()).ToArray();
        }

        public Stream ReadContent(int number) {
            Log.Verbose("Reading the book {0} content.", number);
            Gutenberg.Book book;
            return OpenContent(number, out book);
        }

        public Volume ReadVolume(int number) {
            Log.Verbose("Reading the book {0} volume.", number);
            Gutenberg.Book book;
            using (var stream = OpenContent(number, out book))
                return book.ToVolume(stream.ReadBytes());
        }

        public Card[] EnumerateCards(int lastNumberSeen) {
            Log.Verbose("Enumerating books starting after {0}.", lastNumberSeen);
            var books = Books;
            if (lastNumberSeen != 0)
                books = books.SkipWhile(book => book.Number <= lastNumberSeen);
            return books.Take(100).Select(book => book.ToCard()).ToArray();
        }

        public Book[] FindBooks(Filter filter, int start, int count) {
            var books = Cache.Books;
            if (filter != null) {
                if (!string.IsNullOrEmpty(filter.Title))
                    books = books.Where(book => !string.IsNullOrEmpty(book.Title) &&
                                    book.Title.LikeCI(filter.Title));
                if (!string.IsNullOrEmpty(filter.Author))
                    books = books.Where(book => book.Authors != null &&
                        book.Authors.Any(name => name.LikeCI(filter.Author)));
                if (!string.IsNullOrEmpty(filter.Contributor))
                    books = books.Where(book => book.Contributors != null &&
                        book.Contributors.Any(name => name.LikeCI(filter.Contributor)));
                if (!string.IsNullOrEmpty(filter.Language))
                    books = books.Where(book => !string.IsNullOrEmpty(book.Language) &&
                                    book.Language.LikeII(filter.Language));
                if (!string.IsNullOrEmpty(filter.Format))
                    books = books.Where(book => book.Formats != null &&
                        book.Formats.Any(format => format.LikeII(filter.Format)));
                if (!string.IsNullOrEmpty(filter.Note))
                    books = books.Where(book => book.Notes != null &&
                        book.Notes.Any(note => note.LikeCI(filter.Note)));
                if (!string.IsNullOrEmpty(filter.Tag))
                    books = books.Where(book => book.Tags != null &&
                        book.Tags.Any(tag => tag.LikeCI(filter.Tag)));
                if (!string.IsNullOrEmpty(filter.Era)) {
                    var era = YearSpan.Parse(filter.Era);
                    books = books.Where(book => !book.Era.IsEmpty && !era.Intersects(book.Era));
                }
            }
            return books.Skip(start).Take(count).Select(book => book.ToBook()).ToArray();
        }

        Gutenberg.Book OpenBook(int number) {
            return Cache.GetBook(number.ToString(CultureInfo.InvariantCulture));
        }

        Stream OpenContent(int number, out Gutenberg.Book book) {
            book = OpenBook(number);
            var volume = book.GetVolume(Extensions.MimeType);
            var files = new FileSource { Log = Log };
            Encoding encoding = null;
            return files.Open(volume, ref encoding);
        }

        IEnumerable<Gutenberg.Book> Books {
            get {
                return Cache.Books.Where(book => book.Formats != null &&
                    book.Formats.Any(format => format.StartsWithII(Extensions.MimeType))).
                    OrderBy(book => book.Number);
            }
        }

        static Cache Cache {
            get {
                if (cache == null)
                    lock (cacheLock)
                        if (cache == null)
                            cache = new Cache() {
                                BookSource = new BookSource { Log = Log },
                                VolumeSource = new VolumeSource { Log = Log }, Log = Log
                            };
                return cache;
            }
        }
        static Cache cache;
        static object cacheLock = new object();

        static Log Log {
            get {
                if (log == null)
                    lock (logLock)
                        if (log == null)
                            log = new DebugLog();
                return log;
            }
        }
        static Log log;
        static Log logLock;
    }
}
