# 从游戏目录移除 BepInEx 与插件（只删我们装进去的东西）
param(
    [string]$GameDir = 'D:\epic\Flotsam'
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $GameDir)) { throw "游戏目录不存在：$GameDir" }

$targets = @(
    "$GameDir\BepInEx\plugins\FlotsamTrainer.dll",
    "$GameDir\BepInEx",
    "$GameDir\winhttp.dll",
    "$GameDir\doorstop_config.ini",
    "$GameDir\.doorstop_version"
)

foreach ($t in $targets) {
    if (Test-Path $t) {
        Remove-Item $t -Recurse -Force
        Write-Host "已删除 $t" -ForegroundColor DarkGray
    }
}

Write-Host "卸载完成。游戏本体文件未改动。" -ForegroundColor Green
