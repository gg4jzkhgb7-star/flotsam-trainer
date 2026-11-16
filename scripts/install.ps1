﻿# 安装 BepInEx 运行库与插件到游戏目录
# 用法： powershell -ExecutionPolicy Bypass -File .\scripts\install.ps1 -GameDir "D:\epic\Flotsam"
param(
    [string]$GameDir = 'D:\epic\Flotsam'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

$BepVersion = '5.4.23.6'
$BepZipName = "BepInEx_win_x64_$BepVersion.zip"

if (-not (Test-Path "$GameDir\Flotsam.exe")) {
    throw "没找到 $GameDir\Flotsam.exe，请用 -GameDir 指定游戏目录"
}

$plugin = "$root\dist\FlotsamTrainer.dll"
if (-not (Test-Path $plugin)) {
    throw "缺少 $plugin，请先运行 build.ps1"
}

# ---- 1. 准备 BepInEx ----
$bep = "$root\vendor\bepinex"
$zip = "$root\vendor\$BepZipName"

if (-not (Test-Path "$bep\BepInEx\core\BepInEx.dll")) {
    New-Item -ItemType Directory -Force -Path "$root\vendor" | Out-Null

    if (-not (Test-Path $zip)) {
        Write-Host "== 下载 BepInEx $BepVersion ==" -ForegroundColor Cyan
        $urls = @(
            "https://github.com/BepInEx/BepInEx/releases/download/v$BepVersion/$BepZipName",
            "https://ghfast.top/https://github.com/BepInEx/BepInEx/releases/download/v$BepVersion/$BepZipName",
            "https://gh-proxy.com/https://github.com/BepInEx/BepInEx/releases/download/v$BepVersion/$BepZipName"
        )

        $ok = $false
        foreach ($u in $urls) {
            try {
                Write-Host "  尝试 $u"
                Invoke-WebRequest -Uri $u -OutFile $zip -UseBasicParsing -TimeoutSec 120
                if ((Test-Path $zip) -and (Get-Item $zip).Length -gt 100000) { $ok = $true; break }
            }
            catch {
                Write-Host "  失败：$($_.Exception.Message)" -ForegroundColor DarkYellow
            }
        }

        if (-not $ok) {
            throw "BepInEx 下载失败，请手动下载 $BepZipName 放到 $root\vendor\ 后重试"
        }
    }

    Write-Host "== 解压 BepInEx ==" -ForegroundColor Cyan
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    if (Test-Path $bep) { Remove-Item $bep -Recurse -Force }
    [System.IO.Compression.ZipFile]::ExtractToDirectory($zip, $bep)
}

# ---- 2. 备份已有 BepInEx ----
if (Test-Path "$GameDir\BepInEx") {
    $stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
    $backup = "$GameDir\BepInEx_backup_$stamp"
    Write-Host "检测到已有 BepInEx，备份到 $backup" -ForegroundColor Yellow
    Copy-Item "$GameDir\BepInEx" $backup -Recurse -Force
}

# ---- 3. 复制运行库与插件 ----
Write-Host "== 复制 BepInEx 运行库 ==" -ForegroundColor Cyan
Copy-Item "$bep\*" $GameDir -Recurse -Force
Copy-Item "$bep\.doorstop_version" $GameDir -Force -ErrorAction SilentlyContinue

Write-Host "== 安装插件 ==" -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path "$GameDir\BepInEx\plugins" | Out-Null
Copy-Item $plugin "$GameDir\BepInEx\plugins\FlotsamTrainer.dll" -Force

Write-Host ""
Write-Host "安装完成。" -ForegroundColor Green
Write-Host "1) 启动游戏，进入存档"
Write-Host "2) 按 F1 打开面板"
Write-Host "3) 配置文件：$GameDir\BepInEx\config\local.flotsam.trainer.cfg"
