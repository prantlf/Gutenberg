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

using System;
using System.Linq;
using System.Text;

namespace Gutenberg
{
    // Helps dealing with file paths, names and extensions and URLs.
    public class PathUtility
    {
        // Joins the first part (root) and all additional parts with slashes. Any part can start
        // or end with slash; separating slashes are added as necessary not to make them doubled.

        public static string JoinPath(string root, params string[] parts) {
            return JoinPath(root, parts, 0);
        }

        public static string JoinPath(string[] parts, int start) {
            return JoinPath("", parts, start);
        }

        public static string JoinPath(string[] parts, int start, int count) {
            return JoinPath("", parts, start, count);
        }

        public static string JoinPath(string root, string[] parts, int start) {
            if (parts == null)
                throw new ArgumentNullException("right");
            return JoinPath(root, parts, start, parts.Length - start);
        }

        public static string JoinPath(string root, string[] parts, int start, int count) {
            if (root == null)
                throw new ArgumentNullException("root");
            if (parts == null)
                throw new ArgumentNullException("right");
            if (start < 0 || start > parts.Length)
                throw new ArgumentOutOfRangeException("start");
            if (count < 0 || count > parts.Length - start)
                throw new ArgumentOutOfRangeException("count");
            var result = new StringBuilder(root.Trim());
            var trimmed = parts.Skip(start).Take(count).Select(item => item.Trim());
            foreach (var part in trimmed.Where(item => item.Any()))
                // This seems complicated but the idea is to ensure the single separating slashes
                // saving the append slash operation if not necessary.
                if (part.StartsWith('/')) {
                    if (result.Length > 0 && !result.EndsWith('/'))
                        result.Append(part);
                    else
                        result.Append(part, 1, part.Length - 1);
                } else {
                    if (result.Length > 0 && !result.EndsWith('/'))
                        result.Append('/');
                    result.Append(part);
                }
            return result.ToString();
        }

        // Gets parent and child paths of a path. The child is the part after the very last slash.
        // If there is no slash in the path it is already the child name. The parent is the part
        // from the beginning up to the very last slash. If there is no slash in the path the
        // parent is empty because the path contains only the child then.

        public static string GetChildName(string path) {
            if (path == null)
                throw new ArgumentNullException("path");
            var separator = path.LastIndexOf('/');
            return separator >= 0 ? path.Substring(separator + 1) : path;
        }
    }
}
