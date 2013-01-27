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

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Gutenberg.FileSystem
{
    public class BookSource : LibrarySource, Books
    {
        public Date GetCreated() {
            return GetCreated(new BookParser { Log = Log });
        }

        public bool HasBooks {
            get { return Exists; }
        }

        public IEnumerable<Book> GetBooks() {
            using (var stream = Open()) {
                var parser = new BookParser { Log = Log };
                Date created;
                using (var reader = parser.Open(stream, out created))
                    return parser.GetBooks(reader).ToArray();
            }
        }

        protected override string FileName {
            get { return Path.Combine(DirectoryName, "Books"); }
        }
    }
}
