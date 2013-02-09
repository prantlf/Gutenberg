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
    public class Catalog : RemoteSource
    {
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

        protected override string Url {
            get { return url; }
        }
        string url = Settings.GetValue<string>(typeof(Catalog), "CatalogURL");

        public void SetUrl(string value) {
            url = value;
        }
    }
}
