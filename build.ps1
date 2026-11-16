﻿# 编译插件并输出到 dist\
param(
    [string]$GameManagedDir = 'D:\epic\Flotsam\Flotsam_Data\Managed'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

if (-not (Test-Path "$GameManagedDir\Assembly-CSharp.dll")) {
    throw "找不到 $GameManagedDir\Assembly-CSharp.dll，请用 -GameManagedDir 指定游戏托管程序集目录"
}

Write-Host "== 编译 FlotsamTrainer ==" -ForegroundColor Cyan
dotnet build "$root\src\FlotsamTrainer\FlotsamTrainer.csproj" -c Release --nologo -p:GameManagedDir="$GameManagedDir"
if ($LASTEXITCODE -ne 0) { throw "编译失败" }

$dll = "$root\src\FlotsamTrainer\bin\Release\FlotsamTrainer.dll"
New-Item -ItemType Directory -Force -Path "$root\dist" | Out-Null
Copy-Item $dll "$root\dist\FlotsamTrainer.dll" -Force

Write-Host "== 完成：$root\dist\FlotsamTrainer.dll ==" -ForegroundColor Green
