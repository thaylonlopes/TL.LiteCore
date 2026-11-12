$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$templateCsproj = Join-Path $root "TL.LiteCore.Template.csproj"
$artifactsDir = "C:\src\artifacts\nuget\TL.LiteCore.Template"
$nupkgPath = Join-Path $artifactsDir "TL.LiteCore.Template.1.0.0.nupkg"
$scratchRoot = if (Test-Path "C:\src\scratch") { "C:\src\scratch" } else { [System.IO.Path]::GetTempPath() }
$tempTestDir = Join-Path $scratchRoot ("TLLiteCoreTest_" + [Guid]::NewGuid().ToString("N").Substring(0, 8))

Write-Host "1. Empacotando TL.LiteCore.Template..."
if (-not (Test-Path $artifactsDir)) {
    New-Item -ItemType Directory -Path $artifactsDir -Force | Out-Null
}

dotnet pack $templateCsproj -c Release -o $artifactsDir
if ($LASTEXITCODE -ne 0) {
    Write-Error "Falha ao empacotar o template."
    exit 1
}

Write-Host "2. Reinstalando template localmente..."
dotnet new uninstall TL.LiteCore.Template 2>$null | Out-Null
dotnet new install $nupkgPath --force
if ($LASTEXITCODE -ne 0) {
    Write-Error "Falha ao instalar o pacote do template."
    exit 1
}

Write-Host "3. Instanciando projeto de teste em: $tempTestDir"
try {
    New-Item -ItemType Directory -Path $tempTestDir -Force | Out-Null
    dotnet new tl-litecore -n TestApp -o $tempTestDir
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Falha ao instanciar o template tl-litecore."
        exit 1
    }

    Write-Host "4. Compilando e testando TestApp..."
    $solutionPath = Join-Path $tempTestDir "TestApp.sln"
    dotnet test $solutionPath --logger "console;verbosity=normal"
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Falha nos testes automatizados do projeto gerado."
        exit 1
    }

    Write-Host "5. Homologacao E2E concluida com sucesso."
}
finally {
    if (Test-Path $tempTestDir) {
        Write-Host "Limpando diretorio temporario..."
        Remove-Item -Recurse -Force $tempTestDir -ErrorAction SilentlyContinue
    }
}
