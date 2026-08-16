Add-Type -AssemblyName System.Drawing

$root = "C:\Users\PCF\AppData\Local\Temp\opencode\GhostScreen"

$Teal       = [System.Drawing.Color]::FromArgb(0, 128, 128)
$DarkTeal   = [System.Drawing.Color]::FromArgb(0, 95, 95)
$Silver     = [System.Drawing.Color]::FromArgb(192, 192, 192)
$DarkSilver = [System.Drawing.Color]::FromArgb(128, 128, 128)
$White      = [System.Drawing.Color]::White
$Black      = [System.Drawing.Color]::Black
$GhostGreen = [System.Drawing.Color]::FromArgb(64, 255, 144)
$GhostGlow  = [System.Drawing.Color]::FromArgb(128, 255, 176)
$ScreenDark = [System.Drawing.Color]::FromArgb(0, 32, 32)
$ScanlineC  = [System.Drawing.Color]::FromArgb(0, 96, 96)
$Navy       = [System.Drawing.Color]::FromArgb(0, 0, 128)

function MakeBrush($c) { New-Object System.Drawing.SolidBrush($c) }
function MakePen($c) { New-Object System.Drawing.Pen($c) }

function Draw-Win95Frame {
    param($g, [int]$x, [int]$y, [int]$w, [int]$h)
    $ph = MakePen $White; $ps = MakePen $DarkSilver
    $g.FillRectangle((MakeBrush $Silver), $x+2, $y+2, $w-4, $h-4)
    $g.DrawLine($ph, $x, $y, $x+$w-1, $y)
    $g.DrawLine($ph, $x, $y, $x, $y+$h-1)
    $g.DrawLine($ps, $x+$w-1, $y, $x+$w-1, $y+$h-1)
    $g.DrawLine($ps, $x, $y+$h-1, $x+$w-1, $y+$h-1)
    $g.DrawLine($ph, $x+1, $y+1, $x+$w-2, $y+1)
    $g.DrawLine($ph, $x+1, $y+1, $x+1, $y+$h-2)
    $g.DrawLine($ps, $x+$w-2, $y+1, $x+$w-2, $y+$h-2)
    $g.DrawLine($ps, $x+1, $y+$h-2, $x+$w-2, $y+$h-2)
    $ph.Dispose(); $ps.Dispose()
}

function Draw-GhostMonitor {
    param($g, [int]$cx, [int]$cy, [int]$size)
    $w = $size; $h = [int]($size * 0.75)
    $x = $cx - $w/2; $y = $cy - $h/2

    # Glow
    $g.FillEllipse((MakeBrush ([System.Drawing.Color]::FromArgb(30, $GhostGreen))), $x-10, $y-10, $w+20, $h+20)

    # Shadow
    $g.FillRectangle((MakeBrush ([System.Drawing.Color]::FromArgb(100, $Black))), $x+5, $y+5, $w, $h)
    # Body
    $g.FillRectangle((MakeBrush $DarkTeal), $x, $y, $w, $h)

    # Bezel 3D (classic Win95 sunken)
    $ph = MakePen $White; $ps = MakePen $DarkSilver; $pd = MakePen $Black
    $g.DrawLine($ps, $x, $y, $x+$w-1, $y)
    $g.DrawLine($ps, $x, $y, $x, $y+$h-1)
    $g.DrawLine($ph, $x+$w-1, $y, $x+$w-1, $y+$h-1)
    $g.DrawLine($ph, $x, $y+$h-1, $x+$w-1, $y+$h-1)
    $g.DrawLine($pd, $x+1, $y+1, $x+$w-2, $y+1)
    $g.DrawLine($pd, $x+1, $y+1, $x+1, $y+$h-2)
    $g.DrawLine($ph, $x+$w-2, $y+2, $x+$w-2, $y+$h-2)
    $g.DrawLine($ph, $x+2, $y+$h-2, $x+$w-2, $y+$h-2)
    $ph.Dispose(); $ps.Dispose(); $pd.Dispose()

    # Screen
    $sx = $x + [int]($w * 0.12); $sy = $y + [int]($h * 0.15)
    $sw = [int]($w * 0.76); $sh = [int]($h * 0.6)
    $g.FillRectangle((MakeBrush $ScreenDark), $sx, $sy, $sw, $sh)

    # Scanlines
    $sPen = MakePen ([System.Drawing.Color]::FromArgb(20, $ScanlineC))
    for ($i = 0; $i -lt $sh; $i += 2) { $g.DrawLine($sPen, $sx, $sy+$i, $sx+$sw, $sy+$i) }
    $sPen.Dispose()

    # Ghost face
    $eb = MakeBrush $GhostGreen
    $eyeW = [int]($sw * 0.16); $eyeH = [int]($sh * 0.28)
    $eyeY = $sy + [int]($sh * 0.22)
    $g.FillRectangle($eb, $sx + [int]($sw * 0.22), $eyeY, $eyeW, $eyeH)
    $g.FillRectangle($eb, $sx + [int]($sw * 0.60), $eyeY, $eyeW, $eyeH)
    # Mouth
    $mW = [int]($sw * 0.28); $mH = [int]($sh * 0.12)
    $g.FillRectangle($eb, $sx + [int]($sw * 0.36), $sy + [int]($sh * 0.62), $mW, $mH)
    $eb.Dispose()

    # Stand
    $sb = MakeBrush $DarkSilver
    $stW = [int]($w * 0.25); $stH = [int]($h * 0.18)
    $g.FillRectangle($sb, $cx - $stW/2, $y+$h, $stW, $stH)
    $baW = [int]($w * 0.4); $baH = [int]($h * 0.08)
    $g.FillRectangle($sb, $cx - $baW/2, $y+$h+$stH, $baW, $baH)
    $sb.Dispose()

    # Screws
    $scb = MakeBrush $DarkSilver
    $g.FillEllipse($scb, $x+3, $y+3, 4, 4)
    $g.FillEllipse($scb, $x+$w-7, $y+3, 4, 4)
    $g.FillEllipse($scb, $x+3, $y+$h-7, 4, 4)
    $g.FillEllipse($scb, $x+$w-7, $y+$h-7, 4, 4)
    $scb.Dispose()
}

# ============================================================
# BANNER 624 x 140 (fits the GUI header area exactly)
# ============================================================
Write-Host "Creating faithful Win95 banner 624x140..."
$banner = New-Object System.Drawing.Bitmap(624, 140)
$g = [System.Drawing.Graphics]::FromImage($banner)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::None
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::SingleBitPerPixel

# Win95 teal background
$g.Clear($Teal)

# Subtle diagonal lines (Win95 pattern)
$sp = MakePen ([System.Drawing.Color]::FromArgb(12, $White))
for ($i = 0; $i -lt 800; $i += 6) { $g.DrawLine($sp, $i, 0, $i-140, 140) }
$sp.Dispose()

# Ghost monitor (left side, slightly smaller)
Draw-GhostMonitor $g 100 72 130

# Text (right side)
$bWhite = MakeBrush $White
$bGhost = MakeBrush $GhostGreen
$bSilver = MakeBrush $Silver

$fBig = New-Object System.Drawing.Font("MS Sans Serif", 20, [System.Drawing.FontStyle]::Bold)
$fMed = New-Object System.Drawing.Font("MS Sans Serif", 11)
$fSm = New-Object System.Drawing.Font("MS Sans Serif", 9)

# Shadow
$bSh = MakeBrush ([System.Drawing.Color]::FromArgb(60, $Black))
$g.DrawString("GhostScreen 95", $fBig, $bSh, 204, 18)
$g.DrawString("The display that doesn't exist", $fMed, $bSh, 204, 50)
$bSh.Dispose()

# Main text
$g.DrawString("GhostScreen 95", $fBig, $bWhite, 202, 16)
$g.DrawString("The display that doesn't exist", $fMed, $bGhost, 202, 48)
$g.DrawString("Virtual display for headless PCs", $fSm, $bSilver, 202, 70)

# Feature tags (Win95 button style)
$tagFont = New-Object System.Drawing.Font("MS Sans Serif", 8)
$tags = @("5K resolution", "Chiptune music", "11 languages", "Win95 style")
$tagX = 202
foreach ($tag in $tags) {
    $tw = [int]($g.MeasureString($tag, $tagFont).Width) + 12
    # 3D raised border
    Draw-Win95Frame $g $tagX 96 $tw 20
    $bTag = MakeBrush $Black
    $g.DrawString($tag, $tagFont, $bTag, $tagX+6, 99)
    $bTag.Dispose()
    $tagX += $tw + 6
}

# Bottom edge (3D effect)
$g.DrawLine((MakePen $DarkSilver), 0, 139, 624, 139)

$banner.Save("$root\docs\assets\banner.png", [System.Drawing.Imaging.ImageFormat]::Png)
$banner.Save("$root\assets\banner.png", [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $banner.Dispose()
Write-Host "  OK banner.png"

# ============================================================
# LOGO 256x256 (smaller, for compact display)
# ============================================================
Write-Host "Creating logo 256x256..."
$logo = New-Object System.Drawing.Bitmap(256, 256)
$l = [System.Drawing.Graphics]::FromImage($logo)
$l.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::None
$l.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$l.Clear($Teal)

$sp2 = MakePen ([System.Drawing.Color]::FromArgb(12, $White))
for ($i = 0; $i -lt 512; $i += 6) { $l.DrawLine($sp2, $i, 0, $i-256, 256) }
$sp2.Dispose()

Draw-GhostMonitor $l 128 110 160

$fL = New-Object System.Drawing.Font("MS Sans Serif", 14, [System.Drawing.FontStyle]::Bold)
$fS = New-Object System.Drawing.Font("MS Sans Serif", 9)
$l.DrawString("GhostScreen 95", $fL, (MakeBrush $White), 52, 210)
$l.DrawString("v1.1.0", $fS, (MakeBrush $Silver), 100, 232)

$logo.Save("$root\docs\assets\logo.png", [System.Drawing.Imaging.ImageFormat]::Png)
$logo.Save("$root\assets\logo.png", [System.Drawing.Imaging.ImageFormat]::Png)
$l.Dispose(); $logo.Dispose()
Write-Host "  OK logo.png"

Write-Host "`nDone!"
Get-ChildItem "$root\docs\assets" | ForEach-Object { Write-Host ("  {0}  {1:N1} KB" -f $_.Name, ($_.Length/1024)) }
