# Скрипт повышения версии (шаги 1-2 релизной процедуры из AGENTS.md).
# Использование: .\scripts\bump-version.ps1 -NewVersion 1.4.0
#                .\scripts\bump-version.ps1 -NewVersion 1.5.0-beta1
# Что делает:
#   1) Проверяет формат SemVer (X.Y.Z или X.Y.Z-суффикс).
#   2) Обновляет <Version> в Primo.MIA.csproj (единственный источник версии).
#   3) Вставляет пустую секцию "## Что нового в версии X.Y.Z" в начало CHANGELOG.md
#      (релиз по тегу блокируется CI, если секции нет — см. release.yml).
# Что НЕ делает (шаги 3-4 остаются за человеком):
#   git commit, git tag vX.Y.Z, git push <remote> vX.Y.Z.
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$NewVersion
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot

# ── 1. Валидация SemVer ────────────────────────────────────────────────────
if ($NewVersion -notmatch '^(?<maj>\d+)\.(?<min>\d+)\.(?<patch>\d+)(-[\w\.\-]+)?$') {
    throw "Версия '$NewVersion' не соответствует формату SemVer (ожидается X.Y.Z или X.Y.Z-suffix)."
}
if ($NewVersion -notmatch '^\d') {
    throw "Версия не должна начинаться с ведущего нуля в компоненте."
}

# ── 2. Обновление <Version> в csproj ───────────────────────────────────────
$csprojPath = Join-Path $repoRoot 'Primo.MIA.csproj'
$csproj = Get-Content $csprojPath -Raw -Encoding UTF8
$pattern = '(?<tag><Version>)(?<ver>\d+\.\d+\.\d+(?:-[\w\.\-]+)?)(?<close></Version>)'
$match = [regex]::Match($csproj, $pattern)
if (-not $match.Success) {
    throw "Не найден тег <Version> в $csprojPath — структура csproj изменилась?"
}
$oldVersion = $match.Groups['ver'].Value
if ($oldVersion -eq $NewVersion) {
    Write-Warning "Версия уже равна $NewVersion — изменения не требуются."
    return
}
$csproj = [regex]::Replace($csproj, $pattern, "`${tag}$NewVersion`${close}", [System.Text.RegularExpressions.RegexOptions]::Singleline)
Set-Content $csprojPath -Value $csproj -Encoding UTF8 -NoNewline
Write-Host "✅ csproj: <Version> $oldVersion → $NewVersion" -ForegroundColor Green

# ── 3. Секция в CHANGELOG.md ───────────────────────────────────────────────
$changelogPath = Join-Path $repoRoot 'CHANGELOG.md'
$changelog = Get-Content $changelogPath -Raw -Encoding UTF8
$sectionHeader = "## Что нового в версии $NewVersion"
$baseVersion = $NewVersion -replace '-.*$', ''
if ($changelog -match [regex]::Escape("## Что нового в версии $baseVersion")) {
    Write-Warning "Секция для версии $baseVersion уже существует — секция не добавлялась."
} else {
    # Заголовок файла — первая строка "# ..."; секция вставляется сразу после неё.
    $lines = $changelog -split "`r?`n"
    $titleIdx = 0
    if ($lines[0] -notlike '#*') {
        throw "Ожидается заголовок '# ...' первой строкой в CHANGELOG.md."
    }
    $blank = [string[]]@('')
    $insert = [string[]]@('', $sectionHeader, '') + $blank + [string[]]@('<!-- Опишите изменения: новые активности, исправления, тесты. -->')
    $updated = $lines[0..$titleIdx] + $insert + $lines[($titleIdx + 1)..($lines.Length - 1)]
    Set-Content $changelogPath -Value ($updated -join "`r`n") -Encoding UTF8 -NoNewline
    Write-Host "✅ CHANGELOG: добавлена секция '$sectionHeader'" -ForegroundColor Green
}

# ── Подсказка по следующим шагам ───────────────────────────────────────────
Write-Host ""
Write-Host "Следующие шаги (релизная процедура):" -ForegroundColor Cyan
Write-Host "  1. Заполнить секцию CHANGELOG для $NewVersion"
Write-Host "  2. git commit"
Write-Host "  3. git tag v$NewVersion && git push <remote> v$NewVersion  (тег запускает release.yml)"
