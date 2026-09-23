# Executa o Unity em modo batch neste projeto e resume o log.
# Uso:  .\Tools\unity-batch.ps1 -Log compile.log
#       .\Tools\unity-batch.ps1 -Log forge.log -Method Ruinas.EditorTools.Forge.All
#       .\Tools\unity-batch.ps1 -Log tests.log -Tests EditMode
param(
    [string]$Log = "batch.log",
    [string]$Method = "",
    [string]$Tests = "",
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe",
    [int]$TimeoutMinutes = 60,
    [switch]$NoQuit
)

$root = Split-Path -Parent $PSScriptRoot
$logPath = Join-Path $root "Logs\$Log"
New-Item -ItemType Directory -Force (Join-Path $root "Logs") | Out-Null

$argsList = @("-batchmode", "-projectPath", "`"$root`"", "-logFile", "`"$logPath`"")
if ($Method -ne "") { $argsList += @("-executeMethod", $Method) }
if ($Tests -ne "") {
    $argsList += @("-runTests", "-testPlatform", $Tests, "-testResults", "`"$(Join-Path $root "Logs\tests-$Tests.xml")`"")
} elseif (-not $NoQuit) {
    $argsList += "-quit"
}

$sw = [Diagnostics.Stopwatch]::StartNew()
$p = Start-Process -FilePath $Unity -ArgumentList $argsList -PassThru
# WaitForExit no processo principal (Start-Process -Wait esperaria também o cliente de licença).
if (-not $p.WaitForExit($TimeoutMinutes * 60 * 1000)) {
    Write-Output "TIMEOUT: encerrando Unity"
    $p.Kill()
}
Write-Output ("exit={0} tempo={1:n0}s log={2}" -f $p.ExitCode, $sw.Elapsed.TotalSeconds, $logPath)
if (Test-Path $logPath) {
    Select-String -Path $logPath -Pattern "error CS\d+", "\[Forge\]", "Exception:", "Error building", "Build succeeded", "Build Finished", "Test run completed" |
        ForEach-Object { $_.Line.Trim() } | Select-Object -Unique | Select-Object -First 120
}
