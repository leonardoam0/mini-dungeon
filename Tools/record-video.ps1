# Grava jogabilidade pelo executável e converte a sequência de quadros em MP4 (ffmpeg).
# Exemplos:
#   .\Tools\record-video.ps1 -Name referencia -GameArgs "-reference" -Seconds 15
#   .\Tools\record-video.ps1 -Name percurso -GameArgs "-mission -autoplay" -Seconds 90
# Saída: Docs\Gravacoes\<Name>.mp4 (os quadros temporários ficam em Logs\gravacao_<Name>).
param(
    [string]$Name = "referencia",
    [string]$GameArgs = "-reference",
    [int]$Fps = 30,
    [float]$Seconds = 15,
    [string]$Exe = "",
    [string]$Ffmpeg = "ffmpeg",
    [int]$TimeoutMinutes = 30
)

$root = Split-Path -Parent $PSScriptRoot
if ($Exe -eq "") { $Exe = Join-Path $root "Build\Windows\RuinasDoObelisco.exe" }
$frames = Join-Path $root "Logs\gravacao_$Name"
$outDir = Join-Path $root "Docs\Gravacoes"
New-Item -ItemType Directory -Force $outDir | Out-Null
if (Test-Path $frames) { Remove-Item -Recurse -Force $frames }
New-Item -ItemType Directory -Force $frames | Out-Null

$argList = @($GameArgs.Split(" ", [StringSplitOptions]::RemoveEmptyEntries)) + @(
    "-recordDir", "`"$frames`"", "-recordFps", "$Fps", "-recordSeconds", "$Seconds",
    "-quitWhenDone", "-windowedTest", "-saveDir", "`"$(Join-Path $root 'Logs\save_gravacao')`"",
    "-logFile", "`"$(Join-Path $root "Logs\gravacao_$Name.log")`"")
$sw = [Diagnostics.Stopwatch]::StartNew()
$p = Start-Process -FilePath $Exe -ArgumentList $argList -PassThru
if (-not $p.WaitForExit($TimeoutMinutes * 60 * 1000)) { Write-Output "TIMEOUT: encerrando o jogo"; $p.Kill() }
$count = (Get-ChildItem $frames -Filter "q_*.jpg").Count
Write-Output ("jogo encerrado em {0:n0}s; quadros: {1}" -f $sw.Elapsed.TotalSeconds, $count)
if ($count -eq 0) { Write-Output "Nenhum quadro gravado."; exit 1 }

$mp4 = Join-Path $outDir "$Name.mp4"
& $Ffmpeg -y -loglevel error -framerate $Fps -i (Join-Path $frames "q_%05d.jpg") -c:v libx264 -pix_fmt yuv420p -crf 20 -preset medium -movflags +faststart $mp4
if (Test-Path $mp4) {
    Write-Output ("vídeo: {0} ({1:n1} MB)" -f $mp4, ((Get-Item $mp4).Length / 1MB))
    Remove-Item -Recurse -Force $frames
}
