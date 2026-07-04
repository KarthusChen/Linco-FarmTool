# Renders the vector mouse (same art as the desktop pet) into a multi-size icon.ico.
# Small sizes use uncompressed DIB frames (GDI+/tray compatible); 256 uses PNG (shell).
# Run:  ./tools/make-icon.ps1
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName WindowsBase
Add-Type -AssemblyName System.Xaml

$xaml = @'
<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" Width="256" Height="256">
  <Border CornerRadius="52" Background="#FFECE7F3"/>
  <Viewbox Margin="30,26,30,26" Stretch="Uniform">
    <Canvas Width="200" Height="160">
      <Path Stroke="#FFE39BB4" StrokeThickness="6" StrokeStartLineCap="Round" StrokeEndLineCap="Round" Data="M 146,128 C 176,124 182,100 168,92 C 158,86 152,96 160,102"/>
      <Ellipse Canvas.Left="46" Canvas.Top="34" Width="52" Height="52" Fill="#FFCFC7DA" Stroke="#FF6B6478" StrokeThickness="3"/>
      <Ellipse Canvas.Left="56" Canvas.Top="44" Width="32" Height="32" Fill="#FFF6C2D4"/>
      <Ellipse Canvas.Left="102" Canvas.Top="34" Width="52" Height="52" Fill="#FFCFC7DA" Stroke="#FF6B6478" StrokeThickness="3"/>
      <Ellipse Canvas.Left="112" Canvas.Top="44" Width="32" Height="32" Fill="#FFF6C2D4"/>
      <Ellipse Canvas.Left="52" Canvas.Top="52" Width="96" Height="94" Fill="#FFDDD6E6" Stroke="#FF6B6478" StrokeThickness="3"/>
      <Ellipse Canvas.Left="74" Canvas.Top="92" Width="52" Height="50" Fill="#FFF3EFF8"/>
      <Ellipse Canvas.Left="66" Canvas.Top="98" Width="16" Height="10" Fill="#66F191AE"/>
      <Ellipse Canvas.Left="118" Canvas.Top="98" Width="16" Height="10" Fill="#66F191AE"/>
      <Ellipse Canvas.Left="80" Canvas.Top="78" Width="16" Height="19" Fill="#FF322C3A"/>
      <Ellipse Canvas.Left="104" Canvas.Top="78" Width="16" Height="19" Fill="#FF322C3A"/>
      <Ellipse Canvas.Left="84" Canvas.Top="81" Width="6" Height="6" Fill="White"/>
      <Ellipse Canvas.Left="108" Canvas.Top="81" Width="6" Height="6" Fill="White"/>
      <Ellipse Canvas.Left="93" Canvas.Top="100" Width="14" Height="10" Fill="#FFE87DA0"/>
      <Path Stroke="#FF6B6478" StrokeThickness="2.5" StrokeStartLineCap="Round" StrokeEndLineCap="Round" Data="M 100,110 C 100,116 94,118 90,115 M 100,110 C 100,116 106,118 110,115"/>
      <Path Stroke="#FFA79FB6" StrokeThickness="1.6" StrokeStartLineCap="Round" Data="M 92,104 L 60,98 M 92,108 L 58,110 M 108,104 L 140,98 M 108,108 L 142,110"/>
      <Ellipse Canvas.Left="78" Canvas.Top="126" Width="20" Height="17" Fill="#FFCFC7DA" Stroke="#FF6B6478" StrokeThickness="2.5"/>
      <Ellipse Canvas.Left="102" Canvas.Top="126" Width="20" Height="17" Fill="#FFCFC7DA" Stroke="#FF6B6478" StrokeThickness="2.5"/>
      <Path Stroke="#FF5EA84F" StrokeThickness="4" StrokeStartLineCap="Round" Data="M 100,132 L 100,120"/>
      <Path Fill="#FF7FC66B" Stroke="#FF4F9E44" StrokeThickness="1.5" Data="M 100,124 C 92,120 88,124 90,130 C 96,130 100,128 100,124 Z"/>
      <Path Fill="#FF7FC66B" Stroke="#FF4F9E44" StrokeThickness="1.5" Data="M 100,122 C 108,117 113,121 111,128 C 104,128 100,126 100,122 Z"/>
    </Canvas>
  </Viewbox>
</Grid>
'@

$root = [System.Windows.Markup.XamlReader]::Parse($xaml)
$root.Measure([System.Windows.Size]::new(256,256))
$root.Arrange([System.Windows.Rect]::new(0,0,256,256))
$root.UpdateLayout()

function Get-Bgra([int]$s) {
    $dpi = 96.0 * $s / 256.0
    $rtb = New-Object System.Windows.Media.Imaging.RenderTargetBitmap($s, $s, $dpi, $dpi, [System.Windows.Media.PixelFormats]::Pbgra32)
    $rtb.Render($root)
    $conv = New-Object System.Windows.Media.Imaging.FormatConvertedBitmap($rtb, [System.Windows.Media.PixelFormats]::Bgra32, $null, 0)
    $stride = $s * 4
    $buf = New-Object 'byte[]' ($stride * $s)
    $conv.CopyPixels($buf, $stride, 0)
    return ,$buf   # top-down BGRA (comma stops PS from unrolling the array)
}

function New-DibFrame([int]$s) {
    $bgra = Get-Bgra $s
    $stride = $s * 4
    $andRow = [int]([math]::Floor(($s + 31) / 32) * 4)   # 1bpp mask row padded to 4 bytes
    $ms = New-Object System.IO.MemoryStream
    $bw = New-Object System.IO.BinaryWriter($ms)
    # BITMAPINFOHEADER
    $bw.Write([uint32]40); $bw.Write([int32]$s); $bw.Write([int32]($s * 2))
    $bw.Write([uint16]1); $bw.Write([uint16]32); $bw.Write([uint32]0)
    $bw.Write([uint32]0); $bw.Write([int32]0); $bw.Write([int32]0)
    $bw.Write([uint32]0); $bw.Write([uint32]0)
    # XOR pixels, bottom-up
    for ($row = $s - 1; $row -ge 0; $row--) { $bw.Write($bgra, $row * $stride, $stride) }
    # AND mask, all zero
    $zero = New-Object 'byte[]' ($andRow * $s)
    $bw.Write($zero)
    $bw.Flush()
    return ,$ms.ToArray()
}

function New-PngFrame([int]$s) {
    $dpi = 96.0 * $s / 256.0
    $rtb = New-Object System.Windows.Media.Imaging.RenderTargetBitmap($s, $s, $dpi, $dpi, [System.Windows.Media.PixelFormats]::Pbgra32)
    $rtb.Render($root)
    $enc = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
    $enc.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($rtb))
    $ms = New-Object System.IO.MemoryStream
    $enc.Save($ms)
    return ,$ms.ToArray()
}

# size -> frame bytes (small = DIB, 256 = PNG)
$sizes = 16,24,32,48,64,128,256
$frames = @()
foreach ($s in $sizes) {
    if ($s -ge 256) { $frames += ,(New-PngFrame $s) } else { $frames += ,(New-DibFrame $s) }
}

$outDir = Join-Path $PSScriptRoot "..\src\LincoFarmTool\Assets"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$out = Join-Path $outDir "icon.ico"

$fs = [System.IO.File]::Create($out)
$bw = New-Object System.IO.BinaryWriter($fs)
$n = $frames.Count
$bw.Write([uint16]0); $bw.Write([uint16]1); $bw.Write([uint16]$n)
$offset = 6 + 16 * $n
for ($i = 0; $i -lt $n; $i++) {
    $s = $sizes[$i]; $len = $frames[$i].Length
    $dim = if ($s -ge 256) { 0 } else { $s }
    $bw.Write([byte]$dim); $bw.Write([byte]$dim); $bw.Write([byte]0); $bw.Write([byte]0)
    $bw.Write([uint16]1); $bw.Write([uint16]32)
    $bw.Write([uint32]$len); $bw.Write([uint32]$offset)
    $offset += $len
}
foreach ($f in $frames) { $bw.Write($f) }
$bw.Flush(); $bw.Close(); $fs.Close()

Write-Host ("Wrote {0} ({1} sizes, {2} bytes)" -f $out, $n, (Get-Item $out).Length) -ForegroundColor Green
