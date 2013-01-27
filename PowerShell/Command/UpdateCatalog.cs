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
using System.Management.Automation;
using Gutenberg.FileSystem;

namespace Gutenberg.PowerShell
{
    [Cmdlet(VerbsData.Update, "GPCatalog", SupportsShouldProcess = true)]
    public class UpdateCatalog : DriveCmdlet
    {
        [Parameter]
        public SwitchParameter Force { get; set; }

        [Parameter]
        public SwitchParameter Always { get; set; }

        [Parameter]
        public string URL { get; set; }

        protected override void ProcessRecord() {
            try {
                CheckOrUpdateCatalog();
            } catch (Exception exception) {
                WriteError(new ErrorRecord(exception, "CatalogUpdateFailed",
                    ErrorCategory.ResourceUnavailable, null));
            }
        }

        void CheckOrUpdateCatalog() {
            var progress = Log.Action(2, "Checking and/or updating catalog...");
            var books = new BookSource { Log = Log };
            var volumes = new VolumeSource { Log = Log };
            if (!string.IsNullOrEmpty(ActualDirectory)) {
                books.SetDirectory(ActualDirectory);
                volumes.SetDirectory(ActualDirectory);
            }
            var updater = new CatalogUpdater { Log = Log };
            Date localDate = Date.MinValue;
            if (Always || updater.ShouldUpdate(books, volumes, out localDate)) {
                progress.Continue("Downloading remote catalog...");
                var catalog = new Catalog { Log = Log };
                if (!string.IsNullOrEmpty(URL))
                    catalog.SetUrl(URL);
                using (var stream = catalog.Open())
                    updater.TryUpdate(stream, books, volumes,
                        remoteDate => localDate == Date.MinValue || localDate < remoteDate,
                        () => ShouldProcess("Catalog", "Update") && (Force ||
                        ShouldContinue("Do you really want to update the local catalog?",
                                       "Catalog Update")));
            }
            progress.Finish();
        }
    }
}
