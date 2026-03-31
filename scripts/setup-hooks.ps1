#
# Sets up local Git hooks for conventional commit validation.
#

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
$hooksDir = Join-Path $repoRoot ".git" "hooks"

Copy-Item (Join-Path $scriptDir "commit-msg") (Join-Path $hooksDir "commit-msg") -Force

Write-Host "Git hooks installed successfully."
Write-Host "Commit messages will now be validated against Conventional Commits format."
