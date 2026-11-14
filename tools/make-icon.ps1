# Regenerates src\OpenInGoogleDocs\app.ico: a blue document page with a folded corner and text lines.
# Run from anywhere in Windows PowerShell; the .ico is committed, so you only need this to change the design.
Add-Type -AssemblyName System.Drawing
$out = Join-Path $PSScriptRoot "..\src\OpenInGoogleDocs\app.ico"
$sizes = 16, 24, 32, 48, 64, 256

function New-Frame([int]$s) {
    $scale = 8                       # draw big, then downsample for smooth edges
    $big = $s * $scale
    $bmp = New-Object System.Drawing.Bitmap $big, $big
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.Clear([System.Drawing.Color]::Transparent)
    $u = $big / 100.0

    $blue = [System.Drawing.Color]::FromArgb(66, 133, 244)
    $fold = [System.Drawing.Color]::FromArgb(161, 194, 250)

    # Page: x 20..80, y 6..94, top-right corner cut for the fold, rounded bottom corners.
    $r = 8 * $u
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddLine(20 * $u + $r, 6 * $u, 58 * $u, 6 * $u)
    $path.AddLine(58 * $u, 6 * $u, 80 * $u, 28 * $u)
    $path.AddLine(80 * $u, 28 * $u, 80 * $u, 94 * $u - $r)
    $path.AddArc(80 * $u - 2 * $r, 94 * $u - 2 * $r, 2 * $r, 2 * $r, 0, 90)
    $path.AddArc(20 * $u, 94 * $u - 2 * $r, 2 * $r, 2 * $r, 90, 90)
    $path.AddArc(20 * $u, 6 * $u, 2 * $r, 2 * $r, 180, 90)
    $path.CloseFigure()
    $g.FillPath((New-Object System.Drawing.SolidBrush $blue), $path)

    # Folded corner.
    $tri = [System.Drawing.PointF[]]@(
        (New-Object System.Drawing.PointF (58 * $u), (6 * $u)),
        (New-Object System.Drawing.PointF (80 * $u), (28 * $u)),
        (New-Object System.Drawing.PointF (58 * $u), (28 * $u)))
    $g.FillPolygon((New-Object System.Drawing.SolidBrush $fold), $tri)

    # Text lines (skip the thinnest detail at tiny sizes).
    $white = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)
    $g.FillRectangle($white, 32 * $u, 48 * $u, 36 * $u, 7 * $u)
    $g.FillRectangle($white, 32 * $u, 64 * $u, 36 * $u, 7 * $u)
    $g.FillRectangle($white, 32 * $u, 78 * $u, 24 * $u, 7 * $u)
    $g.Dispose()

    $small = New-Object System.Drawing.Bitmap $s, $s
    $g2 = [System.Drawing.Graphics]::FromImage($small)
    $g2.InterpolationMode = 'HighQualityBicubic'
    $g2.PixelOffsetMode = 'HighQuality'
    $g2.DrawImage($bmp, 0, 0, $s, $s)
    $g2.Dispose(); $bmp.Dispose()
    $ms = New-Object System.IO.MemoryStream
    $small.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $small.Dispose()
    return , $ms.ToArray()
}

$frames = $sizes | ForEach-Object { , (New-Frame $_) }
$fs = [System.IO.File]::Create($out)
$w = New-Object System.IO.BinaryWriter $fs
$w.Write([uint16]0); $w.Write([uint16]1); $w.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]; $d = $frames[$i]
    $dim = if ($s -ge 256) { 0 } else { $s }
    $w.Write([byte]$dim); $w.Write([byte]$dim); $w.Write([byte]0); $w.Write([byte]0)
    $w.Write([uint16]1); $w.Write([uint16]32)
    $w.Write([uint32]$d.Length); $w.Write([uint32]$offset)
    $offset += $d.Length
}
foreach ($d in $frames) { $w.Write($d) }
$w.Close(); $fs.Close()
Write-Host "Wrote $out"
