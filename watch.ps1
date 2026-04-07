# Re-export static HTML when project.toml / *.re / *.rdoc change (see Reac.csproj <Watch>).
$ErrorActionPreference = "Stop"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $here

docker compose up -d site
docker compose run --rm watch
