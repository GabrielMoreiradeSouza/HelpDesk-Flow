#Requires -Version 7.4
param([switch]$ApiOnly)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
Push-Location (Join-Path $PSScriptRoot '..')
try {
    docker info --format '{{.ServerVersion}}' | Out-Null
    $localConfig = Join-Path $PWD '.env.helpdesk'
    if (-not (Test-Path -LiteralPath $localConfig)) {
        $dbPassword = [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(24))
        $jwtKey = [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
        $demoPassword = [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(8))
        @(
            "HELPDESK_DB_PASSWORD=$dbPassword"
            "HELPDESK_JWT_KEY=$jwtKey"
            "HELPDESK_DEMO_PASSWORD=$demoPassword"
        ) | Set-Content -LiteralPath $localConfig -Encoding ascii
    }

    $config = ConvertFrom-StringData (Get-Content -LiteralPath $localConfig -Raw)
    $env:ConnectionStrings__DefaultConnection = "Host=127.0.0.1;Port=55432;Database=helpdesk_flow;Username=helpdesk;Password=$($config.HELPDESK_DB_PASSWORD)"
    $env:Jwt__Key = $config.HELPDESK_JWT_KEY
    $env:Demo__Password = $config.HELPDESK_DEMO_PASSWORD
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    docker compose --env-file $localConfig up -d --wait

    if (-not $ApiOnly) {
        $lockHash = (Get-FileHash frontend/package-lock.json).Hash
        $stamp = 'frontend/node_modules/.helpdesk-lock'
        if (-not (Test-Path $stamp) -or
            (Get-Content $stamp -Raw).Trim() -ne $lockHash -or
            -not (Test-Path 'frontend/node_modules/@angular/cli/bin/ng.js')) {
            npm.cmd --prefix frontend ci --no-fund --no-audit --prefer-offline
            Set-Content -LiteralPath $stamp -Value $lockHash -Encoding ascii
        }
        npm.cmd --prefix frontend run build
    }

    Write-Host 'Aplicação   http://localhost:5031'
    Write-Host 'Swagger     http://localhost:5031/swagger'
    Write-Host 'Contas      admin@helpdesk.test | tecnico@helpdesk.test | usuario@helpdesk.test'
    Write-Host "Senha das contas de demonstração em $localConfig, campo HELPDESK_DEMO_PASSWORD."
    dotnet run --no-launch-profile --urls http://localhost:5031
}
finally {
    Pop-Location
}
