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
using System.Globalization;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Gutenberg
{
    [Serializable]
    public struct YearSpan : IComparable, IComparable<YearSpan>, IEquatable<YearSpan>,
                             IFormattable, ISerializable, IXmlSerializable
    {
        public static readonly YearSpan Empty = new YearSpan();

        public int First { get; private set; }

        public int Last { get; private set; }

        public YearSpan(int first, int last) : this() {
            First = first;
            Last = last;
        }

        YearSpan(SerializationInfo info, StreamingContext context) : this() {
            First = info.GetInt32("First");
            Last = info.GetInt32("Last");
        }

        public bool IsEmpty {
            get { return First == 0 && Last == 0; }
        }

        public int CompareTo(object obj) {
            if (obj is YearSpan)
                return CompareTo((YearSpan) obj);
            throw new ArgumentException("Incomparable object type.", "obj");
        }

        public int CompareTo(YearSpan other) {
            if (First < other.First)
                return -1;
            if (First > other.First)
                return 1;
            if (Last < other.Last)
                return -1;
            if (Last > other.Last)
                return 1;
            return 0;
        }

        public static int Compare(YearSpan left, YearSpan right) {
            return left.CompareTo(right);
        }

        public override int GetHashCode() {
            return First * 10000 + Last;
        }

        public override bool Equals(object obj) {
            return obj is YearSpan && Equals((YearSpan) obj);
        }

        public bool Equals(YearSpan other) {
            return First == other.First && Last == other.Last;
        }

        public static bool Equals(YearSpan left, YearSpan right) {
            return left.Equals(right);
        }

        public bool Includes(int year) {
            return year >= First && year <= Last;
        }

        public bool Includes(Date date) {
            return Includes(date.Year);
        }

        public bool Includes(DateTime time) {
            return Includes(time.Year);
        }

        public YearSpan Union(YearSpan other) {
            return IsEmpty ? other : other.IsEmpty ? this :
                new YearSpan(Math.Min(First, other.First), Math.Max(Last, other.Last));
        }

        public YearSpan Intersection(YearSpan other) {
            return IsEmpty || other.IsEmpty || Last < other.First || First < other.Last ? Empty :
                new YearSpan(Math.Max(First, other.First), Math.Min(Last, other.Last));
        }

        public override string ToString() {
            return ToString(null);
        }

        public string ToString(IFormatProvider formatProvider) {
            return ToString(null, null);
        }

        public string ToString(string format, IFormatProvider formatProvider) {
            return string.Format(formatProvider, "{0}-{1}", First, Last);
        }

        public static YearSpan Parse(string s) {
            return Parse(s, null);
        }

        public static YearSpan Parse(string s, IFormatProvider provider) {
            if (s == null)
                throw new ArgumentNullException("s");
            var dash = s.IndexOf('-');
            if (dash < 0)
                throw new FormatException("Missing dash.");
            return new YearSpan(int.Parse(s.Substring(0, dash), provider),
                int.Parse(s.Substring(dash + 1), provider));
        }

        public static bool TryParse(string s, out YearSpan value) {
            return TryParse(s, null, out value);
        }

        public static bool TryParse(string s, IFormatProvider provider, out YearSpan value) {
            if (s == null)
                throw new ArgumentNullException("s");
            value = YearSpan.Empty;
            var dash = s.IndexOf('-');
            if (dash < 0)
                return false;
            int first, last;
            if (!int.TryParse(s.Substring(0, dash), NumberStyles.Integer, provider, out first) ||
                !int.TryParse(s.Substring(dash + 1), NumberStyles.Integer, provider, out last))
                return false;
            value = new YearSpan(first, last);
            return true;
        }

        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue("First", First);
            info.AddValue("Last", Last);
        }

        public static bool operator ==(YearSpan left, YearSpan right) {
            return left.Equals(right);
        }

        public static bool operator !=(YearSpan left, YearSpan right) {
            return !(left == right);
        }

        public static bool operator <(YearSpan left, YearSpan right) {
            return left.CompareTo(right) < 0;
        }

        public static bool operator <=(YearSpan left, YearSpan right) {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >(YearSpan left, YearSpan right) {
            return !(left <= right);
        }

        public static bool operator >=(YearSpan left, YearSpan right) {
            return !(left < right);
        }

        public XmlSchema GetSchema() {
            return null;
        }

        public void ReadXml(XmlReader reader) {
            var value = Parse(reader.ReadContentAsString(), CultureInfo.InvariantCulture);
            First = value.First;
            Last = value.Last;
        }

        public void WriteXml(XmlWriter writer) {
            writer.WriteString(ToString(CultureInfo.InvariantCulture));
        }
    }
}
