﻿// Copyright (C) 2012-2013 Ferdinand Prantl <prantlf@gmail.com>
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

using NUnit.Framework;

namespace Gutenberg
{
    [TestFixture]
    public class PathUtilityTest
    {
        [Test]
        public void TestJoinPath() {
            Assert.AreEqual("", PathUtility.JoinPath(""));
            Assert.AreEqual("", PathUtility.JoinPath("", ""));
            Assert.AreEqual("a", PathUtility.JoinPath("a"));
            Assert.AreEqual("a", PathUtility.JoinPath(" a "));
            Assert.AreEqual("a/b", PathUtility.JoinPath("a", "b"));
            Assert.AreEqual("a/b", PathUtility.JoinPath("a/", "b"));
            Assert.AreEqual("a/b", PathUtility.JoinPath("a", "/b"));
            Assert.AreEqual("a/b", PathUtility.JoinPath("a/", "/b"));
        }

        [Test]
        public void TestGetChildName() {
            Assert.AreEqual("", PathUtility.GetChildName(""));
            Assert.AreEqual("", PathUtility.GetChildName("folder/"));
            Assert.AreEqual("file", PathUtility.GetChildName("file"));
            Assert.AreEqual("file", PathUtility.GetChildName("folder/file"));
            Assert.AreEqual("file", PathUtility.GetChildName("folder1/folder2/file"));
        }
    }
}
