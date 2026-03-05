Add-Type -AssemblyName System.Drawing

$baseDir = "c:\Users\akjak\WoodLand3D\Assets\Resources\UI"
$path = Join-Path $baseDir "UpgradePanel_MoveSpeed.png"
$backup = $path + ".bak"

if (!(Test-Path $path)) { Write-Host "파일 없음: $path"; exit 1 }

$img = [System.Drawing.Bitmap]::FromFile((Resolve-Path $path))
$width = $img.Width
$height = $img.Height
$replaced = 0

for ($y = 0; $y -lt $height; $y++) {
    for ($x = 0; $x -lt $width; $x++) {
        $c = $img.GetPixel($x, $y)
        $r = [int]$c.R
        $g = [int]$c.G
        $b = [int]$c.B
        
        $isGray = [Math]::Abs($r - $g) -le 40 -and
                  [Math]::Abs($g - $b) -le 40 -and
                  [Math]::Abs($r - $b) -le 40
        if (!$isGray) { continue }
        
        $v = ($r + $g + $b) / 3
        $mx = [Math]::Max([Math]::Max($r, $g), $b)
        $mn = [Math]::Min([Math]::Min($r, $g), $b)
        $sat = $mx - $mn
        if ($sat -gt 50 -or $v -lt 40) { continue }
        
        $img.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(0, $r, $g, $b))
        $replaced++
    }
}

$outPath = Join-Path $baseDir "UpgradePanel_MoveSpeed_fixed.png"
$img.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
$img.Dispose()

try {
    if (!(Test-Path $backup)) { Copy-Item $path $backup }
    Copy-Item $outPath $path -Force
    Remove-Item $outPath -Force
    Write-Host "체커보드 픽셀 $replaced 개 투명 처리 완료."
} catch {
    Write-Host "파일 사용 중. UpgradePanel_MoveSpeed_fixed.png 로 저장됨. Unity 종료 후 해당 파일을 UpgradePanel_MoveSpeed.png 로 교체하세요."
}
