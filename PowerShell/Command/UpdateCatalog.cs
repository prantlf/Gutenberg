// Copyright (C) 2012-2013 Ferdinand Prantl <prantlf@gmail.com>
// All rights reserved.       
//
// This file is part of PowerShell drive for the Project Gutenberg
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
using System.Management.Automation;
using System.Xml;
using Gutenberg.FileSystem;

namespace Gutenberg.PowerShell
{
    [Cmdlet(VerbsData.Update, "GPCatalog", SupportsShouldProcess = true)]
    public class UpdateCatalog : LoggingCmdlet
    {
        [Parameter]
        public SwitchParameter Force { get; set; }

        [Parameter]
        public SwitchParameter Always { get; set; }

        [Parameter]
        public string URL { get; set; }

        [Parameter]
        public string Directory { get; set; }

        protected override void ProcessRecord() {
            try {
                CheckOrUpdateCatalog();
            } catch (Exception exception) {
                WriteError(new ErrorRecord(exception, "CatalogUpdateFailed",
                    ErrorCategory.ResourceUnavailable, null));
            }
        }

        void CheckOrUpdateCatalog() {
            var progress = Log.Action(5, "Catalog Update");
            var books = new BookSource { Log = Log };
            var volumes = new VolumeSource { Log = Log };
            if (!string.IsNullOrEmpty(Directory)) {
                books.SetDirectory(Directory);
                volumes.SetDirectory(Directory);
            }
            Date localDate = Date.MinValue;
            if (!Always && books.Exists && volumes.Exists) {
                progress.Continue("Opening local catalog...");
                localDate = books.GetCreated();
                Log.Verbose("Local creation date: {0}.", localDate);
                if (new Date(DateTime.Now) == localDate) {
                    WriteWarning("Local catalog up-to-date.");
                    progress.Finish();
                    return;
                }
            }
            progress.Continue("Downloading remote catalog...");
            var catalog = new Catalog { Log = Log };
            if (!string.IsNullOrEmpty(URL))
                catalog.SetUrl(URL);
            using (var stream = catalog.Open()) {
                progress.Continue("Opening remote catalog...");
                var parser = new CatalogParser { Log = Log };
                using (var reader = parser.Open(stream)) {
                    progress.Continue("Getting remote creation date...");
                    var remoteDate = parser.GetCreated(reader);
                    Log.Verbose("Remote creation date: {0}.", remoteDate);
                    if (localDate == Date.MinValue || localDate < remoteDate) {
                        progress.Continue("Updating local catalog...");
                        UpdateLibrary(parser, reader, remoteDate, books, volumes);
                        Log.Verbose(localDate == Date.MinValue ?
                            "Local catalog created." : "Local catalog updated.");
                    } else {
                        WriteWarning("Local catalog up-to-date.");
                    }
                }
                progress.Finish();
            }
        }

        void UpdateLibrary(CatalogParser parser, XmlReader reader, Date created,
                           BookSource books, VolumeSource volumes) {
            var progress = Log.Action(56, "Catalog Conversion");
            string bookPath = null, volumePath = null;
            Stream bookStream = null, volumeStream = null;
            BinaryWriter bookWriter = null, volumeWriter = null;
            try {
                int bookCount = 0, skippedCount = 0, volumeCount = 0, totalCount = 0;
                bookStream = books.Create(out bookPath);
                var bookParser = new BookParser { Log = Log };
                bookWriter = bookParser.Create(bookStream, created);
                volumeStream = volumes.Create(out volumePath);
                var volumeParser = new VolumeParser { Log = Log };
                volumeWriter = volumeParser.Create(volumeStream, created);
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
                    if (++totalCount % 10000 == 0)
                        progress.Continue("{0} items processed...", totalCount);
                }
                bookParser.Finalize(bookWriter);
                volumeParser.Finalize(volumeWriter);
                bookWriter.Close();
                volumeWriter.Close();
                Log.Verbose("{0} books and {1} volumes were processed, {2} books skipped.",
                    bookCount, volumeCount, skippedCount);
                progress.Continue("Copying converted catalog...", totalCount);
                if (ShouldProcess("Catalog", "Update") && (Force ||
                    ShouldContinue("Do you really want to update the local cataogue?",
                                   "Catalog Update"))) {
                    books.Update(bookPath);
                    volumes.Update(volumePath);
                } else {
                    IOUtility.DeleteTempFile(bookPath);
                    IOUtility.DeleteTempFile(volumePath);
                }
                progress.Finish();
            } catch {
                if (bookWriter != null)
                    bookWriter.Close();
                if (bookStream != null)
                    bookStream.Close();
                if (volumeWriter != null)
                    volumeWriter.Close();
                if (volumeStream != null)
                    volumeStream.Close();
                if (bookPath != null)
                    IOUtility.DeleteTempFile(bookPath);
                if (volumePath != null)
                    IOUtility.DeleteTempFile(volumePath);
                throw;
            }
        }
    }
}
