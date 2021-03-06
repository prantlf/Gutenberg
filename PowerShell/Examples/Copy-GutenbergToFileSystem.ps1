# Copy-GutenbergToFileSystem
#
# Copies textual volumes of books passed through the pipeline to the local
# file system.
#
# See the description in the Get-Help supporting comment below.
#
# Copyright � 2013 Ferdinand Prantl <prantlf@gmail.com>
# All rights reserved.
#
# This program is free software: you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation, either version 3 of the License, or
# (at your option) any later version.
#
# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU General Public License for more details.
#
# You should have received a copy of the GNU General Public License
# along with this program.  If not, see <http://www.gnu.org/licenses/>.

<#
.SYNOPSIS
    Copies textual volumes of books passed through the pipeline to the local
    file system.
.DESCRIPTION
    Goes over books from the pipeline input and picks just the books with a
    textual volume. (The books which contain at least one volume with the
    MIME type text/plain.) Content of the first textual volume of those books
    is copied to the specified directory in the local file system. The book
    friendly names are used for the file names.

    The book volumes are downloaded to the file system cache first or if they
    are not up-to-date. Then they are copied to the target directory from
    there. The automatically created drive Gutenberg: places the cache to
    the $env:LOCALAPPDATA\Gutenberg directory.

    A warning is printed out for every book from the pipeline which has no
    textual volume and will be skipped.
.PARAMETER TargetDirectory
    Target directory to write the books to.

    This parameter can be unnamed at the first positoin. If no value is
    provided the current directory will be used by default.
.INPUTS
    A book or a book collection to process. Other than textual books will be
    filtered out.
.OUTPUTS
    An object with the book number (Number) and the file name (Name) will be
    written to the pipeline for every processed book.
.EXAMPLE
    ls Gutenberg: | ? Authors -like "*Dumas*" | Copy-GutenbergToFileSystem

    Copies all works of Alexandre Dumas to the current directory. Well, it
    would catch more Dumases but there is just one in the Project Gutenberg
    - the Alexandre :-)
.NOTES
    Version:   1.0
    Date:      February 9, 2013
    Author:    Ferdinand Prantl <prantlf@gmail.com>
    Copyright: � 2013 Ferdinand Prantl, All rights reserved.
    License:   GPL
.LINK
    http://prantlf.blogspot.com
    http://github.com/prantlf/Gutenberg.git
#>

# Support Verbose, WhatIf and Confirm switches.
[CmdletBinding(SupportsShouldProcess = $true)]

Param (
    [Parameter(Mandatory = $true, ValueFromPipeline = $true,
	           HelpMessage = 'Collection of books to process. ' +
			                 'Other than textual books are filtered out.')]
    [object] $Books,
    [Parameter(Position = 0,
	           HelpMessage = 'Target directory to write the books to. ' +
			                 'Current directory is the default.')]
    [string] $TargetDirectory
)

Process {
	$book = $_;
	if ($_.Formats -like "*text/plain*") {
		# Make sure the the file name for the book volume is valid; So far,
		# question marks and line breaks have been detected.
		$name = $book.FriendlyTitle.Replace("?", "").
					Replace([Environment]::NewLine, " - ") + ".txt"
		Write-Output @{ Number = $book.Number; Name = $name }
		$content = cat "$($book.PSDrive):$($book.Number)" -Format text/plain
		$target = $name
		if ($TargetDirectory) {
			$target = "$TargetDirectory\" + $target
		}
		sc $target $content
		# Be friendly and don't hammer the Project Gutenberg web site too much.
		# Not every book volume needs an update but we cannot figure it out here.
		sleep 1
	} else {
		Write-Warning "Book $($book.Number) ($($book.FriendlyTitle)) has " +
                      "no textual volume."
	}
}
