# Support Verbose, WhatIf and Confirm switches.
[CmdletBinding(SupportsShouldProcess = $true)]

Param (
    [Parameter(Mandatory = $true, ValueFromPipeline = $true,
	           HelpMessage = 'Collection of books to process. ' +
			                 'All textual books are the default.')]
    [object] $Books,
    [Parameter(Mandatory = $true, Position = 0,
	           HelpMessage = 'Target directory to write the books to. ' +
			                 'Current directory is the default.')]
    [string] $TargetDirectory
)

Process {
	if ($_.Formats -like "*text/plain*") {
		$book = $_;
		$name = $book.FriendlyTitle.Replace("?", "").
					Replace([Environment]::NewLine, " - ") + ".txt"
		Write-Output @{ Number = $book.Number; Name = $name }
		$content = cat "$($book.PSDrive):$($book.Number)" -Format text/plain
		sc "$TargetDirectory\$name" $content
	} else {
		Write-Warning "Book $($book.Number) has no textual volume."
	}
}
