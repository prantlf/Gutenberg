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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Provider;
using Microsoft.PowerShell.Commands;

namespace Gutenberg.PowerShell
{
    [CmdletProvider("Gutenberg", ProviderCapabilities.Filter),
     OutputType(new Type[] { typeof(Book), typeof(string) }, ProviderCmdlet = "Get-ChildItem"),
     OutputType(new Type[] { typeof(Book) }, ProviderCmdlet = "Get-Item")]
    public class DriveProvider : ContainerCmdletProvider, IContentCmdletProvider,
        IPropertyCmdletProvider
    {
        DriveInfo DriveInfo {
            get { return (DriveInfo) base.PSDriveInfo; }
        }

        protected override Collection<PSDriveInfo> InitializeDefaultDrives() {
            var drives = new Collection<PSDriveInfo>();
            drives.Add(new PSDriveInfo("Gutenberg", ProviderInfo, "", null, null));
            return drives;
        }

        protected override PSDriveInfo NewDrive(PSDriveInfo drive) {
            if (drive == null)
                throw new ArgumentNullException("drive");
            var newDrive = new DriveInfo(drive, (NewDriveParameters) DynamicParameters);
            newDrive.Cache.Log = new DriveLog(this);
            if (!newDrive.Cache.HasLibrary)
                WriteWarning("There is no catalog available. Before accessing the Gutenberg " +
                    "drive create the local catalog copy by the Update-GPCatalog command.");
            return newDrive;
        }

        protected override object NewDriveDynamicParameters() {
            return new NewDriveParameters();
        }

        protected override ProviderInfo Start(ProviderInfo providerInfo) {
            // I didn't find better place to set the default description; if it is not set
            // by the drive creator to some value for its purposes it would be empty.
            if (string.IsNullOrEmpty(providerInfo.Description))
                providerInfo.Description = "Browses the Project Gutenberg catalog " +
                    "just like the local file system.";
            return providerInfo;
        }

        // Item manipulation.

        protected override void ClearItem(string path) {
            throw new NotSupportedException("This operation is not supported.");
        }

        protected override void GetItem(string path) {
            EnsureLog();
            WriteBook(Cache.GetBook(path));
        }

        protected override void CopyItem(string path, string copyPath, bool recurse) {
            throw new NotSupportedException("This operation is not supported.");
        }

        protected override void RemoveItem(string path, bool recurse) {
            throw new NotSupportedException("This operation is not supported.");
        }

        protected override void RenameItem(string path, string newName) {
            throw new NotSupportedException("This operation is not supported.");
        }

        protected override void SetItem(string path, object value) {
            throw new NotSupportedException("This operation is not supported.");
        }

        protected override bool ItemExists(string path) {
            if (path.IsEmpty())
                return true;
            EnsureLog();
            return Cache.HasBook(path);
        }

        protected override void InvokeDefaultAction(string path) {
            EnsureLog();
            var book = Cache.GetBook(path);
            Process.Start(PathUtility.JoinPath(CatalogParser.ProjectUrl, "ebooks",
                                book.Number.ToString(CultureInfo.InvariantCulture)));
        }

        protected override object InvokeDefaultActionDynamicParameters(string path) {
            return null;
        }

        // Child items handling.

        protected override bool HasChildItems(string path) {
            return path.IsEmpty();
        }

        protected override void GetChildItems(string path, bool recurse) {
            EnsureLog();
            foreach (var book in GetBooks())
                WriteBook(book);
        }

        protected override void GetChildNames(string path, ReturnContainers returnContainers) {
            EnsureLog();
            foreach (var book in GetBooks())
                WriteBookName(book);
        }

        protected override bool IsValidPath(string path) {
            return true;
        }

        // Content access.

        public void ClearContent(string path) {
            throw new NotSupportedException("This operation is not supported.");
        }

        public object ClearContentDynamicParameters(string path) {
            return null;
        }

        public IContentReader GetContentReader(string path) {
            // If Get-Content receives the item from pipe dynamic parameters will be
            // created by the drive provider of the current container.
            var parameters = DynamicParameters as ContentReaderParameters;
            if (parameters == null) {
                parameters = new ContentReaderParameters();
                var fileParameters = DynamicParameters as FileSystemContentDynamicParametersBase;
                if (fileParameters != null)
                    parameters.Encoding = fileParameters.Encoding;
            }
            EnsureLog();
            return new ContentReader(Cache.GetBook(path), parameters, Cache.Log);
        }

        public object GetContentReaderDynamicParameters(string path) {
            return new ContentReaderParameters();
        }

        public IContentWriter GetContentWriter(string path) {
            throw new NotSupportedException("This operation is not supported.");
        }

        public object GetContentWriterDynamicParameters(string path) {
            return null;
        }

        // Property support

        public void ClearProperty(string path, Collection<string> propertyToClear) {
            throw new NotSupportedException("This operation is not supported.");
        }

        public object ClearPropertyDynamicParameters(string path,
            Collection<string> propertyToClear) {
            return null;
        }

        public void GetProperty(string path, Collection<string> providerSpecificPickList) {
            EnsureLog();
            var book = Cache.GetBook(path);
            var result = new PSObject();
            var propertyNames = providerSpecificPickList == null ? null :
                providerSpecificPickList.Where(entry => !string.IsNullOrEmpty(entry));
            if (propertyNames == null || !propertyNames.Any()) {
                foreach (var property in GetPropertyValues(result, book, null))
                    result.Properties.Add(property);
            } else {
                foreach (var propertyName in propertyNames)
                    foreach (var property in GetPropertyValues(result, book, propertyName))
                        result.Properties.Add(property);
            }
            WritePropertyObject(result, path);
        }

        public object GetPropertyDynamicParameters(string path,
                            Collection<string> providerSpecificPickList) {
            return null;
        }

        IEnumerable<PSNoteProperty> GetPropertyValues(PSObject result, Book book, string filter) {
            var properties = Book.Properties.Where(item => !result.Properties.Any(
                                item2 => item.Name.EqualsII(item2.Name)));
            if (filter != null) {
                if (WildcardPattern.ContainsWildcardCharacters(filter)) {
                    var pattern = new WildcardPattern(filter,
                        WildcardOptions.IgnoreCase | WildcardOptions.Compiled);
                    properties = properties.Where(item => pattern.IsMatch(item.Name));
                } else {
                    properties = properties.Where(item => filter.EqualsII(item.Name));
                }
            }
            foreach (var property in properties) {
                var value = property.GetValue(book);
                if (value != null)
                    yield return new PSNoteProperty(property.Name, value);
            }
        }

        public void SetProperty(string path, PSObject propertyValue) {
            throw new NotSupportedException("This operation is not supported.");
        }

        public object SetPropertyDynamicParameters(string path, PSObject propertyValue) {
            return null;
        }

        // Internal members

        IEnumerable<Book> GetBooks() {
            var books = Cache.Books;
            if (!string.IsNullOrEmpty(Filter))
                if (WildcardPattern.ContainsWildcardCharacters(Filter)) {
                    var pattern = new WildcardPattern(Filter,
                        WildcardOptions.IgnoreCase | WildcardOptions.Compiled);
                    books = books.Where(entry => pattern.IsMatch(entry.FriendlyTitle));
                } else {
                    books = books.Where(entry => entry.Title.ContainsCI(Filter));
                }
            return books;
        }

        void WriteBook(Book book) {
            WriteItemObject(book, book.FriendlyTitle, false);
        }

        void WriteBookName(Book book) {
            WriteItemObject(book.FriendlyTitle, book.FriendlyTitle, false);
        }

        void EnsureLog() {
            var log = Cache.Log as DriveLog;
            if (log == null || log.Provider != this)
                Cache.Log = new DriveLog(this);
        }

        Cache Cache {
            get { return DriveInfo.Cache; }
        }
    }
}
