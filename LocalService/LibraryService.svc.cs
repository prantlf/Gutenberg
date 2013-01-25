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

using System;
using System.Linq;
using System.ServiceModel.Activation;
using Gutenberg.FileSystem;

namespace Gutenberg.LocalService
{
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class LibraryService : Library
    {
        public Book GetBook(string name) {
            if (name == null)
                throw new ArgumentNullException("name");
            if (name.IsEmpty())
                throw new ArgumentException("Book name must not be empty.", "name");
            return Book.Create(Cache.GetBook(name));
        }

        public Book[] GetBooks(int start, int count) {
            if (start < 0 || start > Cache.Books.Count())
                throw new ArgumentOutOfRangeException("start");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count");
            return Cache.Books.Skip(start).Take(count).Select(book =>
                                                            Book.Create(book)).ToArray();
        }

        Cache Cache {
            get { return cache ?? (cache = CreateCache()); }
        }
        static Cache cache;

        internal static Cache CreateCache() {
            return new Cache() {
                BookSource = new BookSource(), VolumeSource = new VolumeSource()
            };
        }
    }
}
