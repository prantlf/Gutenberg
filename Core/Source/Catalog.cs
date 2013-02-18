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

using System.IO;

namespace Gutenberg
{
    // Provides access to the Project Gutenberg catalog from the Internet.
    public class Catalog : RemoteSource
    {
        // Opens the Project Gutenberg catalog from the specified URL. If the content is packed
        // by ZIP it will be returned unpacked.
        public new Stream Open() {
            var stream = base.Open();
            try {
                Stream result;
                if (new Unpacker { Log = Log }.TryUnpack(stream, out result))
                    stream.Close();
                else
                    result = stream;
                return result;
            } catch {
                stream.Close();
                throw;
            }
        }

        // URL of the Project Gutenberg catalog. The default value is read from the application
        // settings property CatalogURL.
        protected override string Url {
            get { return url; }
        }
        string url = Settings.GetValue<string>(typeof(Catalog), "CatalogURL");

        // Overrides the default Project Gutenberg catalog URL.
        public void SetUrl(string value) {
            url = value;
        }
    }
}
