# Update-GutenbergCache
#
# Refreshes the content of the book volume cache if necessary.
#
# See the description in the Get-Help supporting comment below.
#
# Copyright © 2013 Ferdinand Prantl <prantlf@gmail.com>
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
    Refreshes the content of the book volume cache if necessary.
.DESCRIPTION
    Goes over books from the pipeline input and picks just the books with a
    textual volume. (The books which contain at least one volume with the
    MIME type text/plain.) Content of the first textual volume of those books
    is dumped to the null output which causes the volume to be downloaded
    to the cache if it is not there or if a newer version is available.

    The automatically created drive Gutenberg: places the cache to the
    $env:LOCALAPPDATA\Gutenberg directory.

    A warning is printed out for every book from the pipeline which has no
    textual volume and will be skipped.
.INPUTS
    A book or a book collection to process. Other than textual books will be
    filtered out.
.OUTPUTS
    An object with the book number (Number) and the file name (Name) will be
    written to the pipeline for every processed book.
.EXAMPLE
    ls Gutenberg: | ? Authors -like "*Dumas*" | Update-GutenbergCache

    Makes sure that all works of Alexandre Dumas are in the file system
    cache. to the current directory. Well, it would catch more Dumases but
    there is just one in the Project Gutenberg - the Alexandre :-)
.NOTES
    Version:   1.0
    Date:      February 9, 2013
    Author:    Ferdinand Prantl <prantlf@gmail.com>
    Copyright: © 2013 Ferdinand Prantl, All rights reserved.
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
    [object] $Books
)

Process {
	$book = $_;
	if ($_.Formats -like "*text/plain*") {
		Write-Output @{ Number = $book.Number; Name = $book.FriendlyTitle }
		# The content is thrown away here but the cache will be refreshed
		# during the content retrieval if necessary.
		cat "$($book.PSDrive):$($book.Number)" -Format text/plain > $null
		# Be friendly and don't hammer the Project Gutenberg web site too much.
		# Not every book volume needs an update but we cannot figure it out here.
		sleep 1
	} else {
		Write-Warning "Book $($book.Number) has no textual volume."
	}
}
