﻿# 编译并覆盖游戏里的插件 DLL（游戏必须处于关闭状态）
param(
    [string]$GameDir = 'D:\epic\Flotsam'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

$plugins = "$GameDir\BepInEx\plugins"
if (-not (Test-Path $plugins)) {
    throw "游戏里还没装 BepInEx，先跑 scripts\install.ps1"
}

if (Get-Process -Name 'Flotsam' -ErrorAction SilentlyContinue) {
    throw "游戏正在运行，请先退出游戏"
}

& "$root\build.ps1"

if ($LASTEXITCODE -ne 0) { throw "编译失败" }

Copy-Item "$root\dist\FlotsamTrainer.dll" "$plugins\FlotsamTrainer.dll" -Force

Write-Host "已部署到 $plugins\FlotsamTrainer.dll" -ForegroundColor Green
