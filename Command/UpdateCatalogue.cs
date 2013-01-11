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
using System.IO;
using System.Management.Automation;
using System.Xml;

namespace GutenPosh
{
    [Cmdlet(VerbsData.Update, "GPCatalogue", SupportsShouldProcess = true)]
    public class UpdateCatalogue : LoggingCmdlet
    {
        [Parameter]
        public SwitchParameter Force { get; set; }

        [Parameter]
        public SwitchParameter Always { get; set; }

        [Parameter]
        public string URL { get; set; }

        protected override void ProcessRecord() {
            try {
                CheckOrUpdateCatalogue();
            } catch (Exception exception) {
                WriteError(new ErrorRecord(exception, "CatalogueUpdateFailed",
                    ErrorCategory.ResourceUnavailable, null));
            }
        }

        void CheckOrUpdateCatalogue() {
            var progress = Log.Action(5, "Catalogue Update");
            var books = new Books() { Log = Log };
            var volumes = new Volumes() { Log = Log };
            Date localDate = Date.MinValue;
            if (!Always && books.HasLocal() && volumes.HasLocal()) {
                progress.Continue("Opening local catalogue...");
                localDate = books.GetCreated();
                Log.Verbose("Local creation date: {0}.", localDate);
                if (new Date(DateTime.Now) == localDate) {
                    WriteWarning("Local catalogue up-to-date.");
                    progress.Finish();
                    return;
                }
            }
            progress.Continue("Downloading remote catalogue...");
            var catalogue = new Catalogue { Log = Log };
            if (!string.IsNullOrEmpty(URL))
                catalogue.SetUrl(URL);
            string cataloguePath = catalogue.DownloadRemote();
            try {
                progress.Continue("Opening remote catalogue...");
                using (var reader = catalogue.Open(cataloguePath)) {
                    progress.Continue("Getting remote creation date...");
                    var remoteDate = catalogue.GetCreated(reader);
                    Log.Verbose("Remote creation date: {0}.", remoteDate);
                    if (localDate == Date.MinValue || localDate < remoteDate) {
                        progress.Continue("Updating local catalogue...");
                        UpdateLibrary(catalogue, reader, remoteDate, books, volumes);
                        Log.Verbose(localDate == Date.MinValue ?
                            "Local catalogue created." : "Local catalogue updated.");
                    } else {
                        WriteWarning("Local catalogue up-to-date.");
                    }
                }
                progress.Finish();
            } finally {
                IOUtility.DeleteTempFile(cataloguePath);
            }
        }

        void UpdateLibrary(Catalogue catalogue, XmlReader reader, Date created,
                           Books books, Volumes volumes) {
                               var progress = Log.Action(56, "Catalogue Conversion");
            string bookPath = null, volumePath = null;
            BinaryWriter bookWriter = null, volumeWriter = null;
            try {
                int bookCount = 0, skippedCount = 0, volumeCount = 0, totalCount = 0;
                bookWriter = books.Create(created, out bookPath);
                volumeWriter = volumes.Create(created, out volumePath);
                foreach (var item in catalogue.GetItems(reader)) {
                    var book = item as Book;
                    if (book != null) {
                        // Book 9140 and a couple of others are invalid.
                        if (string.IsNullOrEmpty(book.Title)) {
                            Log.Verbose("Skipping book {0} with no title.", book.Number);
                            ++skippedCount;
                        } else {
                            books.Write(bookWriter, book);
                        }
                        ++bookCount;
                    } else {
                        volumes.Write(volumeWriter, (Volume) item);
                        ++volumeCount;
                    }
                    if (++totalCount % 10000 == 0)
                        progress.Continue("{0} items processed...", totalCount);
                }
                books.Finalize(bookWriter);
                volumes.Finalize(volumeWriter);
                bookWriter.Close();
                volumeWriter.Close();
                Log.Verbose("{0} books and {1} volumes were processed, {2} books skipped.",
                    bookCount, volumeCount, skippedCount);
                progress.Continue("Copying converted catalogue...", totalCount);
                if (ShouldProcess("Catalogue", "Update") && (Force ||
                    ShouldContinue("Do you really want to update the local cataogue?",
                                   "Catalogue Update"))) {
                    books.MakeLocal(bookPath);
                    volumes.MakeLocal(volumePath);
                } else {
                    IOUtility.DeleteTempFile(bookPath);
                    IOUtility.DeleteTempFile(volumePath);
                }
                progress.Finish();
            } catch {
                if (bookWriter != null)
                    bookWriter.Close();
                if (volumeWriter != null)
                    volumeWriter.Close();
                if (bookPath != null)
                    IOUtility.DeleteTempFile(bookPath);
                if (volumePath != null)
                    IOUtility.DeleteTempFile(volumePath);
                throw;
            }
        }
    }
}
