param(
    [Parameter(Mandatory = $true)]
    [string]$FontPath,

    [string]$OutputDir = "C:\Users\Scramble\AppData\Local\Temp\opencode\tileset-inspection",

    [int]$Scale = 4,

    [int[]]$Glyphs = @(176..255),

    [string]$Name = "tileset"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

$resolvedFontPath = Resolve-Path -LiteralPath $FontPath
$fontDir = Split-Path -Parent $resolvedFontPath
$font = Get-Content -LiteralPath $resolvedFontPath -Raw | ConvertFrom-Json
$imagePath = Join-Path $fontDir $font.FilePath
if (-not (Test-Path -LiteralPath $imagePath)) {
    throw "Tilesheet image not found: $imagePath"
}

if (-not (Test-Path -LiteralPath $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
}

$glyphWidth = [int]$font.GlyphWidth
$glyphHeight = [int]$font.GlyphHeight
$glyphPadding = [int]$font.GlyphPadding
$columns = [int]$font.Columns
$sheet = [System.Drawing.Bitmap]::FromFile((Resolve-Path -LiteralPath $imagePath))
$rows = [int][Math]::Ceiling($sheet.Height / [double]($glyphHeight + $glyphPadding))
$totalGlyphs = $columns * $rows

function New-ContactSheet {
    param(
        [Parameter(Mandatory = $true)] [int[]]$Indexes,
        [Parameter(Mandatory = $true)] [string]$Path,
        [int]$ColumnsPerRow = 16
    )

    $tileW = $glyphWidth * $Scale
    $tileH = $glyphHeight * $Scale
    $labelH = 13
    $margin = 4
    $cellW = [Math]::Max($tileW, 28) + $margin
    $cellH = $tileH + $labelH + $margin
    $outColumns = [Math]::Min($ColumnsPerRow, [Math]::Max(1, $Indexes.Count))
    $outRows = [int][Math]::Ceiling($Indexes.Count / [double]$outColumns)
    $bitmap = New-Object System.Drawing.Bitmap ($outColumns * $cellW + $margin), ($outRows * $cellH + $margin)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.Clear([System.Drawing.Color]::Black)
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
    $fontForLabels = New-Object System.Drawing.Font "Consolas", 8
    $labelBrush = [System.Drawing.Brushes]::White
    $borderPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::DimGray)

    for ($i = 0; $i -lt $Indexes.Count; $i++) {
        $glyph = [int]$Indexes[$i]
        $outX = $margin + ($i % $outColumns) * $cellW
        $outY = $margin + [int][Math]::Floor($i / $outColumns) * $cellH
        if ($glyph -lt 0 -or $glyph -ge $totalGlyphs) {
            $graphics.DrawString("$glyph?", $fontForLabels, $labelBrush, $outX, $outY + $tileH + 1)
            continue
        }

        $srcX = ($glyph % $columns) * ($glyphWidth + $glyphPadding)
        $srcY = [int][Math]::Floor($glyph / $columns) * ($glyphHeight + $glyphPadding)
        $srcRect = New-Object System.Drawing.Rectangle $srcX, $srcY, $glyphWidth, $glyphHeight
        $destRect = New-Object System.Drawing.Rectangle $outX, $outY, $tileW, $tileH
        $graphics.DrawImage($sheet, $destRect, $srcRect, [System.Drawing.GraphicsUnit]::Pixel)
        $graphics.DrawRectangle($borderPen, $destRect)
        $graphics.DrawString($glyph.ToString(), $fontForLabels, $labelBrush, $outX, $outY + $tileH + 1)
    }

    $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $borderPen.Dispose()
    $fontForLabels.Dispose()
    $graphics.Dispose()
    $bitmap.Dispose()
}

$allGlyphsPath = Join-Path $OutputDir "$Name-all-glyphs.png"
$selectedGlyphsPath = Join-Path $OutputDir "$Name-selected-glyphs.png"
New-ContactSheet -Indexes @(0..($totalGlyphs - 1)) -Path $allGlyphsPath -ColumnsPerRow $columns
New-ContactSheet -Indexes $Glyphs -Path $selectedGlyphsPath -ColumnsPerRow 16

$metadataPath = Join-Path $OutputDir "$Name-metadata.txt"
@(
    "Font: $($font.Name)",
    "FontPath: $resolvedFontPath",
    "ImagePath: $imagePath",
    "ImageSize: $($sheet.Width)x$($sheet.Height)",
    "GlyphSize: ${glyphWidth}x${glyphHeight}",
    "GlyphPadding: $glyphPadding",
    "Columns: $columns",
    "Rows: $rows",
    "TotalGlyphs: $totalGlyphs",
    "AllGlyphs: $allGlyphsPath",
    "SelectedGlyphs: $selectedGlyphsPath"
) | Set-Content -LiteralPath $metadataPath

$sheet.Dispose()

"Wrote $allGlyphsPath"
"Wrote $selectedGlyphsPath"
"Wrote $metadataPath"
