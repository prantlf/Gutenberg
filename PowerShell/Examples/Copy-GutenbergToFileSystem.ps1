# Support Verbose, WhatIf and Confirm switches.
[CmdletBinding(SupportsShouldProcess = $true)]

Param (
    [Parameter(HelpMessage = 'Collection of books to process.')]
    [string] $Books,
    [Parameter(Mandatory = $true, HelpMessage = 'Target directory to write the books to.')]
    [string] $TargetDirectory
)

if (-not $Books) {
	#$books = ls Gutenberg: | ? Formats -like *text/plain*
	$Books = ( (gi gutenberg:1), (gi gutenberg:308) )
}
foreach ($book in $Books) {
	$name = $book.FriendlyTitle.Replace([Environment]::NewLine, " - ") + ".txt"
	Write-Output @{ Number = $book.Number; Name = $name }
	$content = cat "$($book.PSDrive):$($book.Number)" -Format text/plain
	sc $name "$TargetDirectory\$content"
}
