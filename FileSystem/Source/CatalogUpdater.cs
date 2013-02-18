// Copyright (C) 2012-2013 Ferdinand Prantl <prantlf@gmail.com>
// All rights reserved.       
//
// This file is part of the Project Gutenberg Access from Local File System
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
using System.Xml;

namespace Gutenberg.FileSystem
{
    public class CatalogUpdater : LoggableBase
    {
        public bool ShouldUpdate(BookSource books, VolumeSource volumes, out Date date) {
            var progress = Log.Action(3, "Checking local catalog...");
            progress.Continue("Opening local catalog...");
            if (books.Exists && volumes.Exists) {
                progress.Continue("Getting local creation date...");
                date = Date.Min(books.GetCreated(), volumes.GetCreated());
                Log.Verbose("Local creation date: {0}.", date);
                if (new Date(DateTime.Now) == date) {
                    Log.Warning("Local catalog up-to-date.");
                    progress.Finish();
                    return false;
                }
            } else {
                date = Date.MinValue;
            }
            progress.Finish();
            return true;
        }

        public bool TryUpdate(Stream catalogStream, BookSource books, VolumeSource volumes,
                              Func<Date, bool> update, Func<bool> confirm) {
            var progress = Log.Action(5, "Trying catalog update...");
            progress.Continue("Opening remote catalog...");
            var parser = new CatalogParser { Log = Log };
            using (var reader = parser.Open(catalogStream)) {
                progress.Continue("Checking remote creation date...");
                var date = parser.GetCreated(reader);
                Log.Verbose("Remote creation date: {0}.", date);
                if (update(date)) {
                    progress.Continue("Updating local catalog...");
                    string bookPath = null, volumePath = null;
                    try {
                        using (var bookStream = books.Create(out bookPath))
                        using (var volumeStream = volumes.Create(out volumePath))
                            Convert(parser, reader, date, bookStream, volumeStream);
                        progress.Continue("Copying converted catalog...");
                        if (confirm()) {
                            books.Update(bookPath);
                            volumes.Update(volumePath);
                            Log.Verbose("Local catalog updated.");
                            progress.Finish();
                            return true;
                        }
                    } finally {
                        if (bookPath != null)
                            IOUtility.DeleteTempFile(bookPath);
                        if (volumePath != null)
                            IOUtility.DeleteTempFile(volumePath);
                    }
                    Log.Warning("Local catalog left intact.");
                    progress.Finish();
                    return false;
                }
                Log.Warning("Local catalog up-to-date.");
                progress.Finish();
                return false;
            }
        }

        public void Convert(CatalogParser parser, XmlReader reader, Date created,
                            Stream bookStream, Stream volumeStream) {
            // There are almost 42,000 books and 520,000 files in the project Gutenberg. We set
            // up 59 * 10,000 steps here to have the progress bar stretching quite good and still
            // having a little space to grow for the project content.
            var progress = Log.Action(59, "Converting catalog...");
            int bookCount = 0, skippedCount = 0, volumeCount = 0, totalCount = 0;
            var bookParser = new BookParser { Log = Log };
            var volumeParser = new VolumeParser { Log = Log };
            using (var bookWriter = bookParser.Create(bookStream, created))
            using (var volumeWriter = volumeParser.Create(volumeStream, created)) {
                foreach (var item in parser.GetItems(reader)) {
                    var book = item as Book;
                    if (book != null) {
                        // Book 9140 and a couple of others are invalid.
                        if (string.IsNullOrEmpty(book.Title)) {
                            Log.Verbose("Skipping book {0} with no title.", book.Number);
                            ++skippedCount;
                        } else {
                            bookParser.Write(bookWriter, book);
                        }
                        ++bookCount;
                    } else {
                        volumeParser.Write(volumeWriter, (Volume) item);
                        ++volumeCount;
                    }
					// One step after every 10,000 items run nicely on my laptop; less loaded the
					// CPU with constant reporting and more made the console wait for too long.
                    if (++totalCount % 10000 == 0)
                        progress.Continue("{0} items processed...", totalCount);
                }
                bookParser.Finalize(bookWriter);
                volumeParser.Finalize(volumeWriter);
            }
            Log.Verbose("{0} books and {1} volumes were processed, {2} books skipped.",
                bookCount, volumeCount, skippedCount);
            progress.Finish();
        }
    }
}
