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
$ShadowDark = [System.Drawing.Color]::FromArgb(120, 0, 0, 0)
$SemiWhite  = [System.Drawing.Color]::FromArgb(15, 255, 255, 255)
$SemiBlack = [System.Drawing.Color]::FromArgb(60, 0, 0, 0)
$BlueStart  = [System.Drawing.Color]::FromArgb(255, 0, 0, 128)
$BlueEnd    = [System.Drawing.Color]::FromArgb(255, 0, 0, 208)

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
    param($g, [int]$cx, [int]$cy, [int]$size, [bool]$showFace=$true)
    $w = $size; $h = $size * 9 / 10
    $x = $cx - $w/2; $y = $cy - $h/2

    # Glow
    $b1 = MakeBrush ([System.Drawing.Color]::FromArgb(40, $GhostGreen))
    $g.FillEllipse($b1, $x-15, $y-15, $w+30, $h+30); $b1.Dispose()
    $b2 = MakeBrush ([System.Drawing.Color]::FromArgb(20, $GhostGlow))
    $g.FillEllipse($b2, $x-25, $y-25, $w+50, $h+50); $b2.Dispose()

    # Shadow
    $bs = MakeBrush $ShadowDark
    $g.FillRectangle($bs, $x+6, $y+6, $w, $h); $bs.Dispose()
    # Body
    $bb = MakeBrush $DarkTeal
    $g.FillRectangle($bb, $x, $y, $w, $h); $bb.Dispose()

    # Bezel 3D
    $ph = MakePen $White; $ps = MakePen $DarkSilver
    $g.DrawLine($ph, $x, $y, $x+$w-1, $y)
    $g.DrawLine($ph, $x, $y, $x, $y+$h-1)
    $g.DrawLine($ps, $x+$w-1, $y, $x+$w-1, $y+$h-1)
    $g.DrawLine($ps, $x, $y+$h-1, $x+$w-1, $y+$h-1)
    $g.DrawLine($ph, $x+1, $y+1, $x+$w-2, $y+1)
    $g.DrawLine($ph, $x+1, $y+1, $x+1, $y+$h-2)
    $g.DrawLine($ps, $x+$w-2, $y+1, $x+$w-2, $y+$h-2)
    $g.DrawLine($ps, $x+1, $y+$h-2, $x+$w-2, $y+$h-2)
    $ph.Dispose(); $ps.Dispose()

    # Screen
    $sx = $x + $w*15/100; $sy = $y + $h*20/100
    $sw = $w*70/100; $sh = $h*55/100
    $bsd = MakeBrush $ScreenDark
    $g.FillRectangle($bsd, $sx, $sy, $sw, $sh); $bsd.Dispose()

    # Scanlines
    $sPen = MakePen ([System.Drawing.Color]::FromArgb(25, $ScanlineC))
    for ($i = 0; $i -lt $sh; $i += 3) { $g.DrawLine($sPen, $sx, $sy+$i, $sx+$sw, $sy+$i) }
    $sPen.Dispose()

    if ($showFace) {
        $eb = MakeBrush $GhostGreen
        $eg = MakeBrush ([System.Drawing.Color]::FromArgb(80, $GhostGlow))
        $eyeW = $sw * 18 / 100; $eyeH = $sh * 25 / 100; $eyeY = $sy + $sh * 25 / 100
        $g.FillRectangle($eb, $sx + $sw*25/100, $eyeY, $eyeW, $eyeH)
        $g.FillRectangle($eb, $sx + $sw*57/100, $eyeY, $eyeW, $eyeH)
        $g.FillRectangle($eg, $sx + $sw*28/100, $eyeY+2, $eyeW*2/3, $eyeH-4)
        $g.FillRectangle($eg, $sx + $sw*60/100, $eyeY+2, $eyeW*2/3, $eyeH-4)
        $mW = $sw * 30 / 100; $mH = $sh * 12 / 100
        $g.FillRectangle($eb, $sx + $sw*35/100, $sy + $sh*65/100, $mW, $mH)
        $eb.Dispose(); $eg.Dispose()
    }

    # Stand + base
    $sb = MakeBrush $DarkSilver
    $stW = $w * 30 / 100; $stH = $h * 15 / 100
    $g.FillRectangle($sb, $cx - $stW/2, $y+$h, $stW, $stH)
    $baW = $w * 50 / 100; $baH = $h * 6 / 100
    $g.FillRectangle($sb, $cx - $baW/2, $y+$h+$stH, $baW, $baH)
    $g.FillEllipse($sb, $x+4, $y+4, 5, 5)
    $g.FillEllipse($sb, $x+$w-9, $y+4, 5, 5)
    $g.FillEllipse($sb, $x+4, $y+$h-9, 5, 5)
    $g.FillEllipse($sb, $x+$w-9, $y+$h-9, 5, 5)
    $sb.Dispose()
}

# ============================================================
# BANNER 1280x640
# ============================================================
Write-Host "Creating banner 1280x640..."
$banner = New-Object System.Drawing.Bitmap(1280, 640)
$g = [System.Drawing.Graphics]::FromImage($banner)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::None
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::SingleBitPerPixel

# BG gradient
for ($i = 0; $i -lt 1280; $i += 2) {
    $r = $i / 1280.0
    $gv = [int](128 * (1-$r) + 100 * $r)
    $bv = [int](128 * (1-$r) + 120 * $r)
    $c = [System.Drawing.Color]::FromArgb(255, 0, $gv, $bv)
    $p = MakePen $c
    $g.DrawLine($p, $i, 0, $i, 640)
    $p.Dispose()
}

# Diagonal hatching
$sp = MakePen $SemiWhite
for ($i = 0; $i -lt 1920; $i += 8) { $g.DrawLine($sp, $i, 0, $i-640, 640) }
$sp.Dispose()

# Title bar gradient
for ($i = 0; $i -lt 44; $i++) {
    $c = [System.Drawing.Color]::FromArgb(255, 0, 0, [int](128 + $i*1.8))
    $p = MakePen $c
    $g.DrawLine($p, 30, 20+$i, 1250, 20+$i)
    $p.Dispose()
}
$fTitle = New-Object System.Drawing.Font("MS Sans Serif", 18, [System.Drawing.FontStyle]::Bold)
$bWhite = MakeBrush $White
$bBlack = MakeBrush $Black
$g.DrawString("GhostScreen 95", $fTitle, $bWhite, 60, 28)

# Close button
Draw-Win95Frame $g 1220 28 28 28
$g.DrawString("X", (New-Object System.Drawing.Font("Marlett", 12)), $bBlack, 1227, 32)

# Ghost monitor
Draw-GhostMonitor $g 480 350 360 $true

# Main text
$fBig = New-Object System.Drawing.Font("MS Sans Serif", 32, [System.Drawing.FontStyle]::Bold)
$fMed = New-Object System.Drawing.Font("MS Sans Serif", 18)
$fSm = New-Object System.Drawing.Font("MS Sans Serif", 14)
$bSemiBlk = MakeBrush $SemiBlack
$bGhost = MakeBrush $GhostGreen
$bGhostGlow = MakeBrush $GhostGlow

$g.DrawString("The display", $fBig, $bSemiBlk, 682, 192)
$g.DrawString("that doesn't exist", $fBig, $bSemiBlk, 682, 242)
$g.DrawString("The display", $fBig, $bWhite, 680, 190)
$g.DrawString("that doesn't exist", $fBig, $bWhite, 680, 240)
$g.DrawString("Virtual display for headless PCs", $fMed, $bGhost, 682, 310)
$g.DrawString("Windows 95/98/XP/7/10/11", $fSm, (MakeBrush $Silver), 682, 350)
$g.DrawString("No physical monitor needed", $fSm, $bGhostGlow, 682, 380)

# Feature boxes
$features = @("Resolution up to 5K", "Chiptune & MIDI music", "11 Languages", "Invisible display")
for ($i = 0; $i -lt 4; $i++) {
    $bx = 680 + ($i % 2) * 280
    $by = 420 + [math]::Floor($i/2) * 65
    Draw-Win95Frame $g $bx $by 260 55
    $fb = MakeBrush $Teal
    $g.FillRectangle($fb, $bx+3, $by+3, 254, 49); $fb.Dispose()
    $g.DrawString($features[$i], $fSm, $bWhite, $bx+10, $by+16)
}

# Status bar
$sb2 = MakeBrush $Silver
$g.FillRectangle($sb2, 0, 600, 1280, 40); $sb2.Dispose()
Draw-Win95Frame $g 30 605 1220 30
$g.DrawString("2025 Culture Digitali Srl | Luigi Strazzullo | v1.1.0", $fSm, $bBlack, 50, 610)

$banner.Save("$root\docs\assets\banner.png", [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $banner.Dispose()
Write-Host "  OK banner.png"

# ============================================================
# LOGO 512x512
# ============================================================
Write-Host "Creating logo 512x512..."
$logo = New-Object System.Drawing.Bitmap(512, 512)
$l = [System.Drawing.Graphics]::FromImage($logo)
$l.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::None
$l.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$l.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::SingleBitPerPixel
$l.Clear($Teal)

$sp2 = MakePen $SemiWhite
for ($i = 0; $i -lt 1024; $i += 8) { $l.DrawLine($sp2, $i, 0, $i-512, 512) }
$sp2.Dispose()

Draw-GhostMonitor $l 256 230 320 $true

$fL = New-Object System.Drawing.Font("MS Sans Serif", 22, [System.Drawing.FontStyle]::Bold)
$fS = New-Object System.Drawing.Font("MS Sans Serif", 14)
$l.DrawString("GhostScreen 95", $fL, $bWhite, 135, 430)
$l.DrawString("v1.1.0", $fS, (MakeBrush $Silver), 230, 465)

$logo.Save("$root\docs\assets\logo.png", [System.Drawing.Imaging.ImageFormat]::Png)
$l.Dispose(); $logo.Dispose()
Write-Host "  OK logo.png"

# ============================================================
# FAVICON.ICO (16, 32, 48, 64, 128, 256)
# ============================================================
Write-Host "Creating favicon.ico..."
$sizes = @(16, 32, 48, 64, 128, 256)
$bitmaps = @()

foreach ($s in $sizes) {
    $bm = New-Object System.Drawing.Bitmap($s, $s)
    $g2 = [System.Drawing.Graphics]::FromImage($bm)
    $g2.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::None
    $g2.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $g2.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::SingleBitPerPixel
    $g2.Clear($Teal)
    Draw-GhostMonitor $g2 ([int]($s/2)) ([int]($s*0.42)) ([int]($s*0.75)) $true
    if ($s -ge 48) {
        $fsi = [int]($s * 0.14)
        $fi = New-Object System.Drawing.Font("MS Sans Serif", $fsi, [System.Drawing.FontStyle]::Bold)
        $bws = MakeBrush $White
        $g2.DrawString("GS", $fi, $bws, [int]($s*0.15), [int]($s*0.78))
        $fi.Dispose(); $bws.Dispose()
    }
    $g2.Dispose()
    $bitmaps += ,@($s, $bm)
}

$icoPath = "$root\docs\assets\icon.ico"
$fio = [System.IO.File]::Create($icoPath)
$bw = New-Object System.IO.BinaryWriter($fio)
$bw.Write([uint16]0)
$bw.Write([uint16]1)
$bw.Write([uint16]$sizes.Count)

$off = 6 + ($sizes.Count * 16)
foreach ($e in $bitmaps) {
    $s = $e[0]; $bm = $e[1]
    $bm32 = New-Object System.Drawing.Bitmap($bm.Width, $bm.Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $gr = [System.Drawing.Graphics]::FromImage($bm32)
    $gr.DrawImage($bm, 0, 0); $gr.Dispose()
    $rect = New-Object System.Drawing.Rectangle(0, 0, $bm32.Width, $bm32.Height)
    $bd = $bm32.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::ReadOnly, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $dsz = $bd.Stride * $bd.Height
    $bm32.UnlockBits($bd); $bm32.Dispose()
    $am = New-Object byte[] (($s * $s) / 8)
    $tsz = 40 + $dsz + $am.Length
    $bw.Write([byte]([math]::Min($s, 255)))
    $bw.Write([byte]([math]::Min($s, 255)))
    $bw.Write([byte]0); $bw.Write([byte]0)
    $bw.Write([uint16]1); $bw.Write([uint16]32)
    $bw.Write([uint32]$tsz); $bw.Write([uint32]$off)
    $off += $tsz
}

foreach ($e in $bitmaps) {
    $s = $e[0]; $bm = $e[1]
    $bm32 = New-Object System.Drawing.Bitmap($bm.Width, $bm.Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $gr = [System.Drawing.Graphics]::FromImage($bm32)
    $gr.DrawImage($bm, 0, 0); $gr.Dispose()
    $rect = New-Object System.Drawing.Rectangle(0, 0, $bm32.Width, $bm32.Height)
    $bd = $bm32.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::ReadOnly, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $dsz = $bd.Stride * $bd.Height
    $bytes = New-Object byte[] $dsz
    [System.Runtime.InteropServices.Marshal]::Copy($bd.Scan0, $bytes, 0, $dsz)
    $bm32.UnlockBits($bd); $bm32.Dispose()
    $am = New-Object byte[] (($s * $s) / 8)
    $hdr = New-Object byte[] 40
    [BitConverter]::GetBytes(40).CopyTo($hdr, 0)
    [BitConverter]::GetBytes($s).CopyTo($hdr, 4)
    [BitConverter]::GetBytes($s * 2).CopyTo($hdr, 8)
    [BitConverter]::GetBytes(1).CopyTo($hdr, 12)
    [BitConverter]::GetBytes(32).CopyTo($hdr, 14)
    $bw.Write($hdr)
    $bw.Write($bytes)
    $bw.Write($am)
}

$bw.Dispose(); $fio.Dispose()
foreach ($e in $bitmaps) { $e[1].Dispose() }
Write-Host "  OK icon.ico"

# ============================================================
# Summary
# ============================================================
Write-Host ""
Write-Host "=== Assets ==="
Get-ChildItem "$root\docs\assets" | ForEach-Object { Write-Host ("  {0}  {1:N1} KB" -f $_.Name, ($_.Length/1024)) }
