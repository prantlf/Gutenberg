// Copyright (C) 2012-2013 Ferdinand Prantl <prantlf@gmail.com>
// All rights reserved.       
//
// This file is part of Project Gutenberg integration to PowerShell
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
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation.Provider;
using System.Text;
using Gutenberg.FileSystem;

namespace Gutenberg.PowerShell
{
    class ContentReader : IContentReader
    {
        Stream Content { get; set; }
        StreamReader Reader { get; set; }

        public ContentReader(Book book, ContentReaderParameters parameters, Log log) {
            string identifier = null;
            if (parameters.Index > 0)
                identifier = parameters.Index.ToString(CultureInfo.InvariantCulture);
            else if (!string.IsNullOrEmpty(parameters.File))
                identifier = parameters.File;
            else if (!string.IsNullOrEmpty(parameters.Format))
                identifier = parameters.Format;
            var volume = book.GetVolume(identifier);
            var files = new FileSource { Log = log };
            Encoding encoding;
            Content = files.Open(volume, out encoding);
            if (parameters.WasStreamTypeSpecified)
                encoding = parameters.GetEncoding();
            try {
                if (!parameters.UsingByteEncoding)
                    Reader = new StreamReader(Content, encoding);
            } catch {
                Content.Close();
                throw;
            }
        }

        Volume GetVolume(Book book, ContentReaderParameters parameters) {
            // Every volume can have more formats (MIME types) assigned. Choose the first one
            // that starts with the specified value not to have to enter the entire MIME type
            // including the charset part.
            Volume volume;
            if (parameters.Index > 0) {
                volume = book.Volumes.ElementAt(parameters.Index);
            } else if (!string.IsNullOrEmpty(parameters.File)) {
                volume = book.Volumes.FirstOrDefault(item =>
                            item.URL.EndsWithCI(parameters.File));
                if (volume == null)
                    throw new ApplicationException("File not available.");
            } else if (!string.IsNullOrEmpty(parameters.Format)) {
                volume = book.Volumes.FirstOrDefault(item => item.Formats.Any(
                    format => format.StartsWithII(parameters.Format)));
                if (volume == null)
                    throw new ApplicationException("Format not available.");
            } else {
                volume = book.Volumes.First();
            }
            return volume;
        }

        public void Dispose() {
            Close();
        }

        public void Close() {
            if (Reader != null) {
                Reader.Close();
                Reader = null;
            }
            if (Content != null) {
                Content.Close();
                Content = null;
            }
        }

        public IList Read(long readCount) {
            if (Content == null)
                throw new ObjectDisposedException("The reader has been closed.");
            return Reader != null ? ReadText(readCount) : ReadBytes(readCount);
        }

        IList ReadText(long readCount) {
            return Reader.ReadLines().Take((int) readCount).ToArray();
        }

        IList ReadBytes(long readCount) {
            var buffer = new byte[readCount];
            var length = Content.Read(buffer, 0, (int) readCount);
            if (readCount != length)
                Array.Resize(ref buffer, length);
            return buffer;
        }

        public void Seek(long offset, SeekOrigin origin) {
            if (Content == null)
                throw new ObjectDisposedException("The reader has been closed.");
            if (Reader != null)
                Reader.DiscardBufferedData();
            Content.Seek(offset, origin);
        }
    }
}
