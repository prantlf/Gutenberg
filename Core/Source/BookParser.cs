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

using System.Collections.Generic;
using System.IO;

namespace Gutenberg
{
    // Parses the content with books.
    public class BookParser : LibraryParser
    {
        // Deserializes all books from the binary source; it expects the reader returned by the
        // Open method. This method is supposed to be used in a single loop; the enumeration
        // reads the content to the end.
        public IEnumerable<Book> GetBooks(BinaryReader reader) {
            // There are almost 42,000 books in the project Gutenberg. We set up 10 * 5,000
            // steps here to have the progress bar stretching quite good and still having a
            // little space to grow for the project content.
            var progress = Log.Action(10, "Getting books...");
            int count = 0;
            while (reader.ReadBoolean()) {
                yield return Book.Read(reader);
                // One step after every 5,000 items run nicely on my laptop; less would load the
                // CPU with constant reporting and more would wait too long in the console.
                if (++count % 5000 == 0)
                    progress.Continue("{0} books processed...", count);
            }
            Log.Verbose("{0} books were loaded.", count);
            progress.Finish();
        }

        // Writes a single book to the output; it expects the writer returned by the Create
        // method. After writing all books the method Finalize must be called.
        public void Write(BinaryWriter writer, Book book) {
            // The initial bool true will be recognized when deserializing the books. The
            // method Finalize writes false as the end marker.
            writer.Write(true);
            book.Write(writer);
        }

        // The header identifier of books.
        protected override string Header {
            get { return "Gutenberg:Books"; }
        }

        // The version of the book source.
        protected override int Version {
            get { return 1; }
        }
    }
}
