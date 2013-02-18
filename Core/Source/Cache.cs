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
using System.Linq;

namespace Gutenberg
{
    // Provides book enumeration and lookup with contents of book and volume sources in memory.
    // The sources are read once on the every first usage and arrays and maps are created for
    // better performance. This operation takes considerable time (like a couple of seconds).
    //
    // After creating an instance of this class, Log, BookSource and VolumeSource must be set
    // before any method is called on the instance.
    public class Cache : LoggableBase
    {
        // Source of books.
        public Books BookSource {
            get {
                // Setting the book source should be done immediately after constructing a Cache.
                if (bookSource == null)
                    throw new InvalidOperationException("Books property has not been set.");
                return bookSource;
            }
            set {
                bookSource = value;
                // The assigned book source should use the same logger as the cache regardless
                // when the logger is set; even  after assigning the volume source.
                UpdateLog();
            }
        }
        Books bookSource;

        // Source of book volumes.
        public Volumes VolumeSource {
            get {
                // The volume source should be set immediately after constructing a Cache.
                if (volumeSource == null)
                    throw new InvalidOperationException("Volumes property has not been set.");
                return volumeSource;
            }
            set {
                volumeSource = value;
                // The assigned volume source should use the same logger as the cache regardless
                // when the logger is set; even  after assigning the volume source.
                UpdateLog();
            }
        }
        Volumes volumeSource;

        // If someone set Log on this object it should be the same for all objects owned by it.
        // This method will propagate the Log instance to them.
        protected override void UpdateLog() {
            base.UpdateLog();
            if (bookSource != null)
                bookSource.Log = Log;
            if (volumeSource != null)
                volumeSource.Log = Log;
        }

        // Checks if both book and volume sources have their content available.
        public bool HasLibrary {
            get { return BookSource.HasBooks && VolumeSource.HasVolumes; }
        }

        // Gets a book by its name or number. (If the string contains only digits it will be
        // interpreted as a number.)
        public Book GetBook(string name) {
            int number;
            if (int.TryParse(name, NumberStyles.Integer,
                                CultureInfo.InvariantCulture, out number)) {
                // Numbers out of range will cause the array access throw and other invalid
                // numbers will be caught right afterwards.
                var book = BooksByNumber[number];
                if (book == null)
                    throw new ArgumentOutOfRangeException("number");
                return book;
            }
            // This will throw if the name doesn't match a book title.
            return BooksByName[name];
        }

        // Gets a book by its name or number if it exists, not throwing and exception if it does
        // not. (If the string contains only digits it will be interpreted as a number.)
        public bool TryGetBook(string name, out Book book) {
            int number;
            if (int.TryParse(name, NumberStyles.Integer,
                                CultureInfo.InvariantCulture, out number)) {
                if (number >= 0 && number < BooksByNumber.Length) {
                    book = BooksByNumber[number];
                    return book != null;
                }
                book = null;
                return false;
            }
            return BooksByName.TryGetValue(name, out book);
        }

        // Checks if a book with the specified name or number exists. (If the string contains
        // only digits it will be interpreted as a number.)
        public bool HasBook(string name) {
            int number;
            if (int.TryParse(name, NumberStyles.Integer,
                                CultureInfo.InvariantCulture, out number))
                return number >= 0 && number < BooksByNumber.Length &&
                    BooksByNumber[number] != null;
            return BooksByName.ContainsKey(name);
        }

        // Gets volumes for the specified book number.
        public bool TryGetVolumes(int number, out Volume[] volumes) {
            if (number >= 0 && number < VolumesByNumber.Length) {
                volumes = VolumesByNumber[number];
                return volumes != null;
            }
            volumes = null;
            return false;
        }

        // Enumerates over all books; it will load them to memory to allow multiple without
        // reloading the books constantly.
        public IEnumerable<Book> Books {
            get {
                if (books == null) {
                    books = BookSource.GetBooks().ToArray();
                    // There are almost 42,000 books in the project Gutenberg. We set up 10 *
                    // 5,000 steps here to have the progress bar stretching quite good and still
                    // having a little space to grow for the project content.
                    var progress = Log.Action(10, "Completing books...");
                    int bookCount = 0, skippedCount = 0;
                    foreach (var book in books) {
                        Volume[] volumes;
                        // Volumes are needed by the book to know MIME types and URLs of the
                        // files with the book content. They are in a different source and thus
                        // items from the two sources - books and volumes - must be joined here.
                        if (TryGetVolumes(book.Number, out volumes))
                            book.Volumes = volumes;
                        else {
                            // You may want to warn about a book that has no content but there are
                            // actually a couple of them in the Project Gutenberg and it is
                            // unlikely that they would get fixed. Let's not nag by a warning
                            // every time but do report it nevertheless.
                            Log.Verbose("The book {0} has no volumes.", book.Number);
                            ++skippedCount;
                        }
                        // One step after every 5,000 items run nicely on my laptop; less loaded
                        // the CPU with constant reporting and more made the console waiting long.
                        if (++bookCount % 5000 == 0)
                            progress.Continue("{0} books processed...", bookCount);
                    }
                    Log.Verbose("{0} books were completed, {1} have no volumes.",
                        bookCount, skippedCount);
                    progress.Finish();
                }
                return books;
            }
        }
        IEnumerable<Book> books;

        // Provides a map of books using their name as the key. (If there are multiple books with
        // the same name the last wins and will be stored in the map.)
        IDictionary<string, Book> BooksByName {
            get {
                if (booksByName == null) {
                    // There are almost 42,000 books in the project Gutenberg. We set up 10 *
                    // 5,000 steps here to have the progress bar stretching quite good and still
                    // having a little space to grow for the project content.
                    var progress = Log.Action(10, "Organizing books by title...");
                    booksByName = new Dictionary<string, Book>(ConfigurableComparer<string>.CaseInsensitive);
                    int bookCount = 0;
                    foreach (var book in Books) {
                        // The last one wins and previous books with the same title will fall
                        // behind but well, you whould enumerate all books and pick them by
                        // comparing tiles one by one if you want to anticipate this rare case.
                        booksByName[book.FriendlyTitle] = book;
                        // One step after every 5,000 items run nicely on my laptop; less loaded
                        // the CPU with constant reporting and more made the console waiting long.
                        if (++bookCount % 5000 == 0)
                            progress.Continue("{0} books processed...", bookCount);
                    }
                    progress.Finish();
                }
                return booksByName;
            }
        }
        IDictionary<string, Book> booksByName;

        // Provides an array of books to access by the book number. (Not every number points to a
        // book; the array may contain nulls for unused numbers.)
        Book[] BooksByNumber {
            get {
                if (booksByNumber == null) {
                    // There are almost 42,000 books in the project Gutenberg. We set up 10 *
                    // 5,000 steps here to have the progress bar stretching quite good and still
                    // having a little space to grow for the project content.
                    var progress = Log.Action(10, "Organizing books by number...");
                    // Quick and dirty way how to deal with unused book numbers. Not every one
                    // is used and thus there will be holes in the array. Let's assume that the
                    // Project Gutenberg maintainers are not totally crazy and assign more
                    // numbers to books than they skip; otherwise the array will be too small
                    // and the caller will get an exception thrown from the following loop.
                    booksByNumber = new Book[Books.Count() * 2];
                    int bookCount = 0;
                    foreach (var book in Books) {
                        booksByNumber[book.Number] = book;
                        // One step after every 5,000 items run nicely on my laptop; less loaded
                        // the CPU with constant reporting and more made the console waiting long.
                        if (++bookCount % 5000 == 0)
                            progress.Continue("{0} books processed...", bookCount);
                    }
                    progress.Finish();
                }
                return booksByNumber;
            }
        }
        Book[] booksByNumber;

        // Enumerates over all book volumes; it will load them to memory to allow multiple
        // without reloading the book volumes constantly.
        IEnumerable<Volume> Volumes {
            get { return volumes ?? (volumes = VolumeSource.GetVolumes().ToArray()); }
        }
        IEnumerable<Volume> volumes;

        // Provides an array of book volumes to access by the book number. (Not every number
        // points to a book and not every book has volumes; the array may contain nulls in
        // such cases occur.)
        Volume[][] VolumesByNumber {
            get {
                if (volumesByNumber == null) {
                    // There are almost 520,000 files in the project Gutenberg. We set up 53 *
                    // 10,000 steps here to have the progress bar stretching quite good and still
                    // having a little space to grow for the project content.
                    var progress = Log.Action(53, "Organizing volumes...");
                    int count = 0;
                    // Quick and dirty way how to deal with unused book numbers. Not every one
                    // is used and thus there will be holes in the array. Let's assume that the
                    // Project Gutenberg maintainers are not totally crazy and assign more
                    // numbers to books than they skip; otherwise the array will be too small
                    // and the caller will get an exception thrown from the following loop.
                    volumesByNumber = new Volume[100000][];
                    foreach (var volume in Volumes) {
                        // Volumes are organized by their book number. The first one will create
                        // the volume array within the book, the rest will be appended to the end
                        // as they appear in the volume source.
                        Volume[] volumes = volumesByNumber[volume.Number];
                        if (volumes != null)
                            Array.Resize(ref volumes, volumes.Length + 1);
                        else
                            volumes = new Volume[1];
                        volumes[volumes.Length - 1] = volume;
                        volumesByNumber[volume.Number] = volumes;
                        // One step after every 10,000 items run nicely on my laptop; less would
                        // load the CPU with constant reporting and more would wait too long in
                        // the console.
                        if (++count % 10000 == 0)
                            progress.Continue("{0} volumes processed...", count);
                    }
                    progress.Finish();
                }
                return volumesByNumber;
            }
        }
        Volume[][] volumesByNumber;
    }
}