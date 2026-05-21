Add-Type -AssemblyName System.Drawing

$srcDir = "D:\flank-game\marketing\screenshots"
$dstDir = "D:\flank-game\marketing\enhanced"
New-Item -ItemType Directory -Force $dstDir | Out-Null

# Per-screenshot captions (no em-dashes or bare ampersands)
$screens = @(
    [pscustomobject]@{ file="01-main-menu";                  title="FLANK - Tactical Hex Strategy";             sub="Attacker vs Defender. Two sides. One winner." },
    [pscustomobject]@{ file="02-game-modes";                 title="Four Ways to Play";                          sub="Single Player  |  Pass-N-Play  |  LAN  |  Online" },
    [pscustomobject]@{ file="03-single-player-setup";        title="Configurable AI Opponent";                   sub="Choose difficulty, your role, and a time limit" },
    [pscustomobject]@{ file="04-how-to-play-intro";          title="Built-in Interactive Tutorial";              sub="Learn the rules before your very first move" },
    [pscustomobject]@{ file="05-how-to-play-objective";      title="Asymmetric Win Conditions";                  sub="Attackers capture flags  |  Defenders eliminate all attackers" },
    [pscustomobject]@{ file="06-how-to-play-movement";       title="Hex Movement and Jumping";                   sub="Move to adjacent tiles or leap over occupied ones" },
    [pscustomobject]@{ file="07-how-to-play-piece-captures"; title="Flanking Captures";                          sub="Surround an enemy on opposite sides to eliminate them" },
    [pscustomobject]@{ file="08-how-to-play-special-rules";  title="Asymmetric Piece Rules";                     sub="Attackers move 2 pieces  |  Defenders move 1 piece up to 2 steps" },
    [pscustomobject]@{ file="09-gameplay-board-clean";       title="Tactical Hex Board";                         sub="Plan your flanks across a dynamic hex grid" },
    [pscustomobject]@{ file="10-gameplay-board-selection";   title="Clear Visual Feedback";                      sub="Selected pieces instantly highlight every legal move" }
)

function Enhance-Screenshot {
    param([string]$SrcPath, [string]$DstPath, [string]$Title, [string]$Sub)

    $img = [System.Drawing.Image]::FromFile($SrcPath)
    $w   = $img.Width
    $h   = $img.Height

    $bmp = New-Object System.Drawing.Bitmap($w, $h)
    $g   = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode     = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAlias
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

    # Draw original
    $g.DrawImage($img, 0, 0, $w, $h)

    # Gradient caption bar: transparent -> near-black, bottom 160px
    $barH  = 160
    $barY  = $h - $barH
    $ptTop = New-Object System.Drawing.Point(0, $barY)
    $ptBot = New-Object System.Drawing.Point(0, $h)
    $cTop  = [System.Drawing.Color]::FromArgb(0,   0, 0, 0)
    $cBot  = [System.Drawing.Color]::FromArgb(215, 0, 0, 0)
    $grad  = New-Object System.Drawing.Drawing2D.LinearGradientBrush($ptTop, $ptBot, $cTop, $cBot)
    $g.FillRectangle($grad, 0, $barY, $w, $barH)
    $grad.Dispose()

    # Red / Blue accent stripe at the very bottom (5px)
    $sh    = 5
    $redB  = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 210, 45, 45))
    $blueB = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 45, 100, 220))
    $g.FillRectangle($redB,  0,        ($h - $sh), ($w / 2), $sh)
    $g.FillRectangle($blueB, ($w / 2), ($h - $sh), ($w / 2), $sh)
    $redB.Dispose()
    $blueB.Dispose()

    # String format: centred
    $sf = New-Object System.Drawing.StringFormat
    $sf.Alignment     = [System.Drawing.StringAlignment]::Center
    $sf.LineAlignment = [System.Drawing.StringAlignment]::Center

    $titleFont = New-Object System.Drawing.Font("Segoe UI", 38, [System.Drawing.FontStyle]::Bold)
    $subFont   = New-Object System.Drawing.Font("Segoe UI", 24, [System.Drawing.FontStyle]::Regular)
    $white     = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
    $silver    = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(210, 210, 210))

    $g.DrawString($Title, $titleFont, $white,  ([System.Drawing.RectangleF]::new(0, ($h - 125), $w, 55)), $sf)
    $g.DrawString($Sub,   $subFont,   $silver, ([System.Drawing.RectangleF]::new(0, ($h - 68),  $w, 40)), $sf)

    $titleFont.Dispose(); $subFont.Dispose()
    $white.Dispose(); $silver.Dispose(); $sf.Dispose()

    $bmp.Save($DstPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose(); $img.Dispose()
}

# Process all screenshots
foreach ($s in $screens) {
    $src = "$srcDir\$($s.file).png"
    $dst = "$dstDir\$($s.file).png"
    if (-not (Test-Path $src)) { Write-Host "SKIP: $src"; continue }
    Enhance-Screenshot -SrcPath $src -DstPath $dst -Title $s.title -Sub $s.sub
    Write-Host "Enhanced: $($s.file).png"
}

# Steam small capsule 460x215
# Ratio 460:215 = 2.14:1. Source 1920x1080 ratio = 1.78:1.
# Crop a 1920x897 strip from the TOP (logo visible), then scale down to 460x215.
Write-Host ""
Write-Host "Building Steam capsule 460x215..."
$menuImg  = [System.Drawing.Image]::FromFile("$srcDir\01-main-menu.png")
$cap      = New-Object System.Drawing.Bitmap(460, 215)
$gc       = [System.Drawing.Graphics]::FromImage($cap)
$gc.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$capSrcH  = [int]([double]1920 * 215.0 / 460.0)   # 897 — height to crop from source
$srcRectC = New-Object System.Drawing.Rectangle(0, 0, 1920, $capSrcH)
$dstRectC = New-Object System.Drawing.Rectangle(0, 0, 460, 215)
$gc.DrawImage($menuImg, $dstRectC, $srcRectC, [System.Drawing.GraphicsUnit]::Pixel)
$cap.Save("$dstDir\steam-capsule-460x215.png", [System.Drawing.Imaging.ImageFormat]::Png)
$gc.Dispose(); $cap.Dispose(); $menuImg.Dispose()
Write-Host "Steam capsule saved."

# Steam header / library hero 920x430 (same aspect, double pixels)
Write-Host "Building Steam header 920x430..."
$menuImg  = [System.Drawing.Image]::FromFile("$srcDir\01-main-menu.png")
$hdr      = New-Object System.Drawing.Bitmap(920, 430)
$gh       = [System.Drawing.Graphics]::FromImage($hdr)
$gh.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$srcRectH = New-Object System.Drawing.Rectangle(0, 0, 1920, $capSrcH)
$dstRectH = New-Object System.Drawing.Rectangle(0, 0, 920, 430)
$gh.DrawImage($menuImg, $dstRectH, $srcRectH, [System.Drawing.GraphicsUnit]::Pixel)
$hdr.Save("$dstDir\steam-header-920x430.png", [System.Drawing.Imaging.ImageFormat]::Png)
$gh.Dispose(); $hdr.Dispose(); $menuImg.Dispose()
Write-Host "Steam header saved."

# Website hero banner 1920x620
# Crop the top 620px — shows logo + dramatic backdrop without cutting the logo.
Write-Host "Building website hero 1920x620..."
$menuImg  = [System.Drawing.Image]::FromFile("$srcDir\01-main-menu.png")
$hero     = New-Object System.Drawing.Bitmap(1920, 620)
$gg       = [System.Drawing.Graphics]::FromImage($hero)
$gg.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$srcRectW = New-Object System.Drawing.Rectangle(0, 0, 1920, 620)
$dstRectW = New-Object System.Drawing.Rectangle(0, 0, 1920, 620)
$gg.DrawImage($menuImg, $dstRectW, $srcRectW, [System.Drawing.GraphicsUnit]::Pixel)
$hero.Save("$dstDir\website-hero-1920x620.png", [System.Drawing.Imaging.ImageFormat]::Png)
$gg.Dispose(); $hero.Dispose(); $menuImg.Dispose()
Write-Host "Website hero saved."

Write-Host ""
Write-Host "All done. Output: $dstDir"
