param(
    [Parameter(Mandatory=$true)]
    [string]$MigrationName
)

function Stop-DockerContainer {
    param (
        [string]$ContainerName
    )
    
    Write-Host "Stopping container $ContainerName if it exists..."
    docker stop $ContainerName 2>$null
    docker rm $ContainerName 2>$null
}

function Start-PostgresContainer {
    Write-Host "Starting PostgreSQL container..."
    docker run --name lighthouse-postgres `
        -e POSTGRES_DB=lighthouse `
        -e POSTGRES_USER=postgres `
        -e POSTGRES_PASSWORD=postgres `
        -p 5432:5432 `
        -d postgres:17.2-alpine

    Write-Host "Waiting for PostgreSQL to start..."
    Start-Sleep -Seconds 5
}

try {
    # Create SQLite Migration
    Write-Host "Creating SQLite Migration..."
    $env:Database__Provider = "sqlite"
    $tempDbName = "Migration_$(Get-Random).db"
    $env:Database__ConnectionString = "Data Source=$tempDbName"
    
    # Ensure we'll delete the temporary database after migration
    $Script:TempSqliteDb = $tempDbName

    dotnet ef migrations add $MigrationName `
        --project Lighthouse.Migrations.Sqlite `
        --startup-project Lighthouse.Backend `
        --context LighthouseAppContext
    
    if ($LASTEXITCODE -ne 0) {
        throw "SQLite migration creation failed"
    }

    # Setup PostgreSQL and create migration
    Write-Host "Creating PostgreSQL Migration..."
    Stop-DockerContainer -ContainerName "lighthouse-postgres"
    Start-PostgresContainer

    $env:Database__Provider = "postgres"
    $env:Database__ConnectionString = "Host=localhost;Database=lighthouse;Username=postgres;Password=postgres"
    
    dotnet ef migrations add $MigrationName `
        --project Lighthouse.Migrations.Postgres `
        --startup-project Lighthouse.Backend `
        --context LighthouseAppContext
    
    if ($LASTEXITCODE -ne 0) {
        throw "PostgreSQL migration creation failed"
    }

    Write-Host "Migrations created successfully!" -ForegroundColor Green
}
catch {
    Write-Host "Error creating migrations: $_" -ForegroundColor Red
    exit 1
}
finally {
    Write-Host "Cleaning up PostgreSQL container..."
    Stop-DockerContainer -ContainerName "lighthouse-postgres"

    # Remove temporary SQLite database if it exists
    if ($Script:TempSqliteDb -and (Test-Path $Script:TempSqliteDb)) {
        Write-Host "Removing temporary SQLite database..."
        Remove-Item $Script:TempSqliteDb -Force
    }

    Write-Host "Setting environment variables back to default..."
    $env:Database__Provider = ""
    $env:Database__ConnectionString = ""
}