Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$assets = Join-Path $root 'assets'
New-Item -ItemType Directory -Force -Path $assets | Out-Null

function New-RoundRectPath([float]$x, [float]$y, [float]$w, [float]$h, [float]$r) {
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $r * 2
    $p.AddArc($x, $y, $d, $d, 180, 90)
    $p.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $p.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
    $p.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $p.CloseFigure()
    return $p
}

function Add-WavyBottom([System.Drawing.Drawing2D.GraphicsPath]$p, [float]$x1, [float]$y, [float]$x2, [float]$amp) {
    $seg = ($x2 - $x1) / 3
    for ($i = 0; $i -lt 3; $i++) {
        $x0 = $x1 + $i * $seg
        $p.AddBezier($x0, $y, $x0 + $seg / 2, $y - $amp, $x0 + $seg / 2, $y + $amp, $x0 + $seg, $y)
    }
}

function Draw-Glyph([System.Drawing.Graphics]$g, [float]$s, [bool]$onTransparent) {
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $m = $s * 0.07
    $fr = $s * 0.07
    # Monitor frame
    $mw = $s - 2 * $m
    $mh = $s - 2 * $m
    $frame = New-RoundRectPath $m $m $mw $mh $fr
    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        (New-Object System.Drawing.RectangleF($m, $m, $mw, $mh)),
        [System.Drawing.Color]::FromArgb(232,232,232), [System.Drawing.Color]::FromArgb(168,168,168), 90)
    $g.FillPath($brush, $frame)
    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(60,60,60), [Math]::Max(1, $s * 0.02))
    $g.DrawPath($pen, $frame)
    # Screen
    $sx = $m + $mw * 0.12; $sy = $m + $mh * 0.14
    $sw = $mw * 0.76; $sh = $mh * 0.60
    $scr = New-RoundRectPath $sx $sy $sw $sh ($s * 0.035)
    $g.FillPath((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(11,18,32))), $scr)
    # Ghost
    $gw = $sw * 0.44
    $gt = $sh * 0.62
    $gx = $sx + ($sw - $gw) / 2
    $gy = $sy + $sh * 0.10
    $gp = New-Object System.Drawing.Drawing2D.GraphicsPath
    $headR = $gw / 2
    $headCy = $gy + $gt * 0.30
    $gp.AddArc($gx + $gw / 2 - $headR, $headCy - $headR, $gw, $gw, 180, 180)
    $gp.AddLine($gx + $gw / 2 + $headR, $headCy, $gx + $gw, $gy + $gt * 0.55)
    Add-WavyBottom $gp ($gx + $gw) ($gy + $gt * 0.62) $gx ($gt * 0.06)
    $gp.AddLine($gx, $gy + $gt * 0.55, $gx + $gw / 2 - $headR, $headCy)
    $gp.CloseFigure()
    $g.FillPath((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(244,244,244))), $gp)
    # Eyes
    $eyeR = [Math]::Max(0.8, $gw * 0.055)
    $eyeY = $headCy + $gw * 0.06
    $eyeB = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(25,25,25))
    $g.FillEllipse($eyeB, $gx + $gw * 0.28, $eyeY, $eyeR * 2, $eyeR * 2)
    $g.FillEllipse($eyeB, $gx + $gw * 0.72 - $eyeR * 2, $eyeY, $eyeR * 2, $eyeR * 2)
    # LED
    $ledR = [Math]::Max(1, $s * 0.012)
    $g.FillEllipse((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(60,200,80))),
        $m + $mw - $ledR * 2.4 - $s * 0.03, $m + $mh - $ledR * 2.4 - $s * 0.03, $ledR * 2, $ledR * 2)
}

function New-LogoPng([string]$path) {
    $size = 512
    $bmp = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.Clear([System.Drawing.Color]::Transparent)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $bg = New-RoundRectPath 8 8 496 496 96
    $grad = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        (New-Object System.Drawing.RectangleF(0,0,$size,$size)),
        [System.Drawing.Color]::FromArgb(0,168,168), [System.Drawing.Color]::FromArgb(0,96,96), 90)
    $g.FillPath($grad, $bg)
    $g.DrawPath((New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(0,70,70), 3)), $bg)
    $g.SetClip($bg)
    Draw-Glyph $g 300 $false
    $g.ResetClip()
    # wordmark
    $f = New-Object System.Drawing.Font('Segoe UI', 40, [System.Drawing.FontStyle]::Bold)
    $f2 = New-Object System.Drawing.Font('Segoe UI', 15)
    $fmt = New-Object System.Drawing.StringFormat
    $fmt.Alignment = [System.Drawing.StringAlignment]::Center
    $g.DrawString('GhostScreen', $f, (New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)), (New-Object System.Drawing.RectangleF(0, 330, 512, 60)), $fmt)
    $g.DrawString('virtual display suite', $f2, (New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(150,230,230))), (New-Object System.Drawing.RectangleF(0, 398, 512, 30)), $fmt)
    $g.Dispose()
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "logo: $path"
}

function New-Icon([string]$path) {
    $sizes = @(16,24,32,48,64,128,256)
    $pngs = New-Object System.Collections.Generic.List[byte[]]
    foreach ($s in $sizes) {
        $bmp = New-Object System.Drawing.Bitmap($s, $s, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        $g.Clear([System.Drawing.Color]::Transparent)
        Draw-Glyph $g $s $true
        $g.Dispose()
        $ms = New-Object System.IO.MemoryStream
        $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
        $pngs.Add($ms.ToArray())
        $ms.Dispose()
    }
    $n = $sizes.Count
    $header = New-Object System.IO.MemoryStream
    $bw = New-Object System.IO.BinaryWriter($header)
    $bw.Write([UInt16]0); $bw.Write([UInt16]1); $bw.Write([UInt16]$n)
    $offset = 6 + 16 * $n
    for ($i = 0; $i -lt $n; $i++) {
        $s = $sizes[$i]
        $bw.Write([Byte]($(if($s -ge 256){0}else{$s})))
        $bw.Write([Byte]($(if($s -ge 256){0}else{$s})))
        $bw.Write([Byte]0); $bw.Write([Byte]0)
        $bw.Write([UInt16]1); $bw.Write([UInt16]32)
        $bw.Write([UInt32]$pngs[$i].Length)
        $bw.Write([UInt32]$offset)
        $offset += $pngs[$i].Length
    }
    foreach ($png in $pngs) { $bw.Write($png) }
    $bw.Flush()
    [System.IO.File]::WriteAllBytes($path, $header.ToArray())
    $bw.Dispose(); $header.Dispose()
    Write-Host "icon: $path"
}

function New-Banner([string]$path) {
    $w = 1280; $h = 640
    $bmp = New-Object System.Drawing.Bitmap($w, $h, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $grad = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        (New-Object System.Drawing.RectangleF(0,0,$w,$h)),
        [System.Drawing.Color]::FromArgb(10,14,28), [System.Drawing.Color]::FromArgb(20,50,60), 35)
    $g.FillRectangle($grad, 0, 0, $w, $h)
    # glow
    $glow = New-Object System.Drawing.Drawing2D.GraphicsPath
    $glow.AddEllipse(-160, 80, 700, 480)
    $gb = New-Object System.Drawing.Drawing2D.PathGradientBrush($glow)
    $gb.CenterColor = [System.Drawing.Color]::FromArgb(60, 0, 140, 140)
    $gb.SurroundColors = @([System.Drawing.Color]::FromArgb(0, 10, 14, 28))
    $g.FillPath($gb, $glow)
    $g.TranslateTransform(70, 70)
    Draw-Glyph $g 420 $true
    $g.ResetTransform()
    # text
    $fTitle = New-Object System.Drawing.Font('Segoe UI', 62, [System.Drawing.FontStyle]::Bold)
    $fTag = New-Object System.Drawing.Font('Segoe UI', 27)
    $fSub = New-Object System.Drawing.Font('Segoe UI', 19)
    $fmt = New-Object System.Drawing.StringFormat
    $g.DrawString('GhostScreen', $fTitle, (New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)), (New-Object System.Drawing.RectangleF(560, 180, 660, 90)), $fmt)
    $g.DrawString('Give your headless PC real resolution.', $fTag, (New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(127,212,212))), (New-Object System.Drawing.RectangleF(560, 285, 660, 60)), $fmt)
    $g.DrawString('One click. Full HD up to 4K. No monitor required.', $fSub, (New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(180,190,200))), (New-Object System.Drawing.RectangleF(560, 350, 660, 45)), $fmt)
    $g.DrawString('MIT - Windows 10/11 - Signed driver - ~400 KB', $fSub, (New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(120,140,150))), (New-Object System.Drawing.RectangleF(560, 420, 660, 40)), $fmt)
    $g.Dispose()
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "banner: $path"
}

New-LogoPng (Join-Path $assets 'logo.png')
New-Icon (Join-Path $assets 'icon.ico')
New-Banner (Join-Path $assets 'banner.png')
Write-Host "DONE"