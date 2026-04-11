# Runs `reac format` inside the SDK Docker image (see docker-compose.yml service `reac`).
# Usage (from repo root):
#   .\scripts\format-re.ps1
#   .\scripts\format-re.ps1 --check
#   .\scripts\format-re.ps1 --format-config reac.format.toml
# Paths for --format-config are resolved inside the container under /work (repo root).

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $repoRoot

docker compose run --rm reac dotnet run --project src/Reac -- format -p /work @args
exit $LASTEXITCODE
