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
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Gutenberg
{
    [Serializable]
    public struct Date : IFormattable, ISerializable, IComparable, IConvertible,
                         IComparable<Date>, IEquatable<Date>, IXmlSerializable
    {
        public Date(long ticks) { time = new DateTime(ticks).Date; }

        public Date(int year, int month, int day) { time = new DateTime(year, month, day); }

        public Date(DateTime source) { time = source.Date; }

        Date(SerializationInfo info, StreamingContext context) {
            time = info.GetDateTime("date");
        }

        DateTime AsDateTime() { return time; }

        public long Ticks { get { return time.Ticks; } }

        public int Year { get { return time.Year; } }

        public int Month { get { return time.Month; } }

        public int Day { get { return time.Day; } }

        public DayOfWeek DayOfWeek { get { return time.DayOfWeek; } }

        public int DayOfYear { get { return time.DayOfYear; } }

        public static Date Today { get { return new Date(DateTime.Now); } }

        public static readonly Date MinValue = new Date(DateTime.MinValue.Date);

        public static readonly Date MaxValue = new Date(DateTime.MaxValue.Date);

        public Date AddDays(int value) { return new Date(time.AddDays(value)); }

        public Date AddMonths(int value) { return new Date(time.AddMonths(value)); }

        public Date AddYears(int value) { return new Date(time.AddYears(value)); }

        public override bool Equals(object obj) { return obj is Date && Equals((Date)obj); }

        public override int GetHashCode() { return time.GetHashCode(); }

        public override string ToString() { return ToString(null); }

        public string ToString(string format, IFormatProvider formatProvider) {
            return time.ToString(NormalizeFormat(format), formatProvider);
        }

        static string NormalizeFormat(string format) {
            if (format != null) {
                if (format == "u" || format == "s")
                    format = "yyyy-MM-dd";
                else
                    for (var i = 0; i < format.Length; ++i) {
                        var c = format[i];
                        if (c == '\\') {
                            ++i;
                            continue;
                        }
                        if (!"dMy:/- ".Contains(c)) {
                            format = "d";
                            break;
                        }
                    }
            } else {
                format = "d";
            }
            return format;
        }

        public static Date Parse(string s) {
            return Parse(s, null);
        }

        public static Date Parse(string s, IFormatProvider provider) {
            if (s == null)
                throw new ArgumentNullException("s");
            return new Date(DateTime.Parse(s, provider));
        }

        public static Date ParseExact(string s, string format) {
            return ParseExact(s, format, null);
        }

        public static Date ParseExact(string s, string format, IFormatProvider provider) {
            if (s == null)
                throw new ArgumentNullException("s");
            if (format == null)
                throw new ArgumentNullException("format");
            return new Date(DateTime.ParseExact(s, NormalizeFormat(format), provider));
        }

        public static bool TryParse(string s, out Date value) {
            return TryParse(s, null, out value);
        }

        public static bool TryParse(string s, IFormatProvider provider, out Date value) {
            if (s == null)
                throw new ArgumentNullException("s");
            DateTime time;
            if (DateTime.TryParse(s, provider, DateTimeStyles.None, out time)) {
                value = new Date(time);
                return true;
            }
            value = Date.MinValue;
            return false;
        }

        public static bool TryParseExact(string s, string format, out Date value) {
            return TryParseExact(s, format, null, out value);
        }

        public static bool TryParseExact(string s, string format, IFormatProvider provider,
                                         out Date value) {
            if (s == null)
                throw new ArgumentNullException("s");
            if (format == null)
                throw new ArgumentNullException("format");
            DateTime time;
            if (DateTime.TryParseExact(s, NormalizeFormat(format), provider,
                                       DateTimeStyles.None, out time)) {
                value = new Date(time);
                return true;
            }
            value = Date.MinValue;
            return false;
        }

        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue("date", time);
        }

        public int CompareTo(object obj) {
            if (!(obj is Date))
                throw new ArgumentException("Argument must be a date.", "obj");
            return CompareTo((Date)obj);
        }

        public int CompareTo(Date other) { return time.CompareTo(other.time); }

        public static int Compare(Date left, Date right) {
            return left.CompareTo(right);
        }

        public bool Equals(Date other) { return time == other.time; }

        public static bool Equals(Date left, Date right) {
            return left.Equals(right);
        }

        DateTime time;

        TypeCode IConvertible.GetTypeCode() {
            return TypeCode.DateTime;
        }

        bool IConvertible.ToBoolean(IFormatProvider provider) {
            throw new InvalidCastException("Cannot convert Gutenberg.Date to System.Boolean.");
        }

        byte IConvertible.ToByte(IFormatProvider provider) {
            throw new InvalidCastException("Cannot convert Gutenberg.Date to System.Byte.");
        }

        char IConvertible.ToChar(IFormatProvider provider) {
            throw new InvalidCastException("Cannot convert Gutenberg.Date to System.Char.");
        }

        DateTime IConvertible.ToDateTime(IFormatProvider provider) {
            return time;
        }

        decimal IConvertible.ToDecimal(IFormatProvider provider) {
            throw new InvalidCastException("Cannot convert Gutenberg.Date to System.Decimal.");
        }

        double IConvertible.ToDouble(IFormatProvider provider) {
            throw new NotImplementedException();
        }

        short IConvertible.ToInt16(IFormatProvider provider) {
            throw new InvalidCastException("Cannot convert Gutenberg.Date to System.Int16.");
        }

        int IConvertible.ToInt32(IFormatProvider provider) {
            throw new InvalidCastException("Cannot convert Gutenberg.Date to System.Int32.");
        }

        long IConvertible.ToInt64(IFormatProvider provider) {
            throw new InvalidCastException("Cannot convert Gutenberg.Date to System.Int64.");
        }

        sbyte IConvertible.ToSByte(IFormatProvider provider) {
            throw new InvalidCastException("Cannot convert Gutenberg.Date to System.SByte.");
        }

        float IConvertible.ToSingle(IFormatProvider provider) {
            throw new InvalidCastException("Cannot convert Gutenberg.Date to System.Single.");
        }

        public string ToString(IFormatProvider provider) {
            return ToString(null, provider);
        }

        object IConvertible.ToType(Type conversionType, IFormatProvider provider) {
            if (conversionType == typeof(Date))
                return this;
            if (conversionType == typeof(DateTime))
                return time;
            throw new InvalidCastException(string.Format("Cannot convert Gutenberg.Date to {0}.",
                                                         conversionType));
        }

        ushort IConvertible.ToUInt16(IFormatProvider provider) {
            throw new InvalidCastException("Cannot convert Gutenberg.Date to System.UInt16.");
        }

        uint IConvertible.ToUInt32(IFormatProvider provider) {
            throw new InvalidCastException("Cannot convert Gutenberg.Date to System.UInt32.");
        }

        ulong IConvertible.ToUInt64(IFormatProvider provider) {
            throw new InvalidCastException("Cannot convert Gutenberg.Date to System.UInt64.");
        }

        public XmlSchema GetSchema() {
            return null;
        }

        public void ReadXml(XmlReader reader) {
            this = Parse(reader.ReadContentAsString(), CultureInfo.InvariantCulture);
        }

        public void WriteXml(XmlWriter writer) {
            writer.WriteString(ToString(CultureInfo.InvariantCulture));
        }

        public static bool operator ==(Date left, Date right) {
            return left.Equals(right);
        }

        public static bool operator !=(Date left, Date right) {
            return !(left == right);
        }

        public static bool operator <(Date left, Date right) {
            return left.CompareTo(right) < 0;
        }

        public static bool operator <=(Date left, Date right) {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >(Date left, Date right) {
            return !(left <= right);
        }

        public static bool operator >=(Date left, Date right) {
            return !(left < right);
        }
    }
}
