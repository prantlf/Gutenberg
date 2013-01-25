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
    public class Cache : LoggableBase
    {
        public Books BookSource {
            get {
                if (bookSource == null)
                    throw new InvalidOperationException("Books property has not been set.");
                return bookSource;
            }
            set {
                bookSource = value;
                UpdateLog();
            }
        }
        Books bookSource;

        public Volumes VolumeSource {
            get {
                if (volumeSource == null)
                    throw new InvalidOperationException("Volumes property has not been set.");
                return volumeSource;
            }
            set {
                volumeSource = value;
                UpdateLog();
            }
        }
        Volumes volumeSource;

        protected override void UpdateLog() {
            base.UpdateLog();
            if (bookSource != null)
                bookSource.Log = Log;
            if (volumeSource != null)
                volumeSource.Log = Log;
        }

        public bool HasLibrary {
            get { return BookSource.HasBooks && VolumeSource.HasVolumes; }
        }

        public Book GetBook(string name) {
            int number;
            if (int.TryParse(name, NumberStyles.Integer,
                                CultureInfo.InvariantCulture, out number)) {
                var book = BooksByNumber[number];
                if (book == null)
                    throw new ArgumentOutOfRangeException("number");
                return book;
            }
            return BooksByName[name];
        }

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

        public bool HasBook(string name) {
            int number;
            if (int.TryParse(name, NumberStyles.Integer,
                                CultureInfo.InvariantCulture, out number))
                return number >= 0 && number < BooksByNumber.Length &&
                    BooksByNumber[number] != null;
            return BooksByName.ContainsKey(name);
        }

        public bool TryGetVolumes(int number, out Volume[] volumes) {
            if (number >= 0 && number < VolumesByNumber.Length) {
                volumes = VolumesByNumber[number];
                return volumes != null;
            }
            volumes = null;
            return false;
        }

        public IEnumerable<Book> Books {
            get {
                if (books == null) {
                    books = BookSource.GetBooks().ToArray();
                    var progress = Log.Action(10, "Completing books...");
                    int bookCount = 0, skippedCount = 0;
                    foreach (var book in books) {
                        Volume[] volumes;
                        if (TryGetVolumes(book.Number, out volumes))
                            book.Volumes = volumes;
                        else {
                            Log.Verbose("The book {0} has no volumes.", book.Number);
                            ++skippedCount;
                        }
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

        IDictionary<string, Book> BooksByName {
            get {
                if (booksByName == null) {
                    var progress = Log.Action(10, "Organizing books by title...");
                    booksByName = new Dictionary<string, Book>(ConfigurableComparer<string>.CaseInsensitive);
                    int bookCount = 0;
                    foreach (var book   in Books) {
                        booksByName[book.FriendlyTitle] = book;
                        if (++bookCount % 5000 == 0)
                            progress.Continue("{0} books processed...", bookCount);
                    }
                    progress.Finish();
                }
                return booksByName;
            }
        }
        IDictionary<string, Book> booksByName;

        Book[] BooksByNumber {
            get {
                if (booksByNumber == null) {
                    var progress = Log.Action(10, "Organizing books by number...");
                    booksByNumber = new Book[100000];
                    int bookCount = 0;
                    foreach (var book in Books) {
                        booksByNumber[book.Number] = book;
                        if (++bookCount % 5000 == 0)
                            progress.Continue("{0} books processed...", bookCount);
                    }
                    progress.Finish();
                }
                return booksByNumber;
            }
        }
        Book[] booksByNumber;

        IEnumerable<Volume> Volumes {
            get { return volumes ?? (volumes = VolumeSource.GetVolumes().ToArray()); }
        }
        IEnumerable<Volume> volumes;

        Volume[][] VolumesByNumber {
            get {
                if (volumesByNumber == null) {
                    var progress = Log.Action(53, "Organizing volumes...");
                    int count = 0;
                    volumesByNumber = new Volume[100000][];
                    foreach (var volume in Volumes) {
                        Volume[] volumes = volumesByNumber[volume.Number];
                        if (volumes != null)
                            Array.Resize(ref volumes, volumes.Length + 1);
                        else
                            volumes = new Volume[1];
                        volumes[volumes.Length - 1] = volume;
                        volumesByNumber[volume.Number] = volumes;
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