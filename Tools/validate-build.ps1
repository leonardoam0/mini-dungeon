# Validações na build (Windows): save/recarga, percurso completo pelo piloto automático e desempenho.
# Uso: powershell -File Tools\validate-build.ps1 [-TimeScale 3] [-PerfSeconds 150]
# Logs e relatórios em Logs\validacao_*.log e Logs\perf_*.json.
param(
    [float]$TimeScale = 3,
    [int]$PerfSeconds = 150,
    [string]$Exe = ""
)

$root = Split-Path -Parent $PSScriptRoot
if ($Exe -eq "") { $Exe = Join-Path $root "Build\Windows\RuinasDoObelisco.exe" }
$logs = Join-Path $root "Logs"
New-Item -ItemType Directory -Force $logs | Out-Null

function Run-Game([string]$name, [string[]]$gameArgs, [int]$timeoutMin, [switch]$Headless) {
    $log = Join-Path $logs "validacao_$name.log"
    # Sem janela quando a validação não depende de imagem (não atrapalha quem está usando a máquina).
    $mode = if ($Headless) { @("-batchmode", "-nographics") } else { @("-windowedTest") }
    $all = $gameArgs + $mode + @("-logFile", "`"$log`"")
    $sw = [Diagnostics.Stopwatch]::StartNew()
    $p = Start-Process -FilePath $Exe -ArgumentList $all -PassThru
    if (-not $p.WaitForExit($timeoutMin * 60 * 1000)) { Write-Output "[$name] TIMEOUT"; $p.Kill() }
    Write-Output ("[{0}] encerrado em {1:n0}s (código {2})" -f $name, $sw.Elapsed.TotalSeconds, $p.ExitCode)
    if (Test-Path $log) {
        Select-String -Path $log -Pattern "\[SaveTest\]|\[AutoPilot\]|\[Perf\]|\[Boot\]|Exception|NullReference" |
            ForEach-Object { "    " + $_.Line.Trim() } | Select-Object -First 40
    }
}

function Fresh([string]$dir) {
    $d = Join-Path $logs $dir
    if (Test-Path $d) { Remove-Item -Recurse -Force $d }
    New-Item -ItemType Directory -Force $d | Out-Null
    return $d
}

# 1. Save: novo jogo, grava, relê e compara (resultado=OK esperado).
$s1 = Fresh "save_teste"
Run-Game "save" @("-mission", "-testSaveLoad", "-quitWhenDone", "-saveDir", "`"$s1`"") 5 -Headless

# 2. Continuar a partir do save gravado no passo 1 (o log mostra o carregamento sem erros).
Run-Game "continuar" @("-continue", "-quitAfter", "8", "-saveDir", "`"$s1`"") 3 -Headless

# 3. Percurso completo pelo piloto automático (sistemas reais), acelerado.
$s3 = Fresh "save_auto"
Run-Game "autopiloto" @("-mission", "-autoplay", "-timescale", "$TimeScale", "-quitWhenDone", "-saveDir", "`"$s3`"") 30 -Headless

# 4. Desempenho em tempo real: arena de referência (roteiro) e percurso (piloto automático em 1x).
Run-Game "perf_referencia" @("-reference", "-perf", "-perfFile", "`"$(Join-Path $logs 'perf_referencia.json')`"", "-quitAfter", "20") 3
$s4 = Fresh "save_perf"
Run-Game "perf_missao" @("-mission", "-autoplay", "-timescale", "1", "-perf", "-perfFile", "`"$(Join-Path $logs 'perf_missao.json')`"", "-quitAfter", "$PerfSeconds", "-saveDir", "`"$s4`"") 10

foreach ($f in @("perf_referencia.json", "perf_missao.json")) {
    $p = Join-Path $logs $f
    if (Test-Path $p) { Write-Output "== $f"; Get-Content $p }
}
