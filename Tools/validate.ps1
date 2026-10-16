param([string]$Unity = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe')
$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
New-Item -ItemType Directory -Force -Path Documentation/Validation | Out-Null
function Invoke-UnityCheck([string]$method, [string]$name, [bool]$quit) {
    $arguments = '-batchmode -projectPath "' + $project + '" -executeMethod ' + $method + ' -logFile "' + $project + '\Logs\' + $name + '.log"'
    if ($quit) { $arguments += ' -quit' }
    $process = Start-Process -FilePath $Unity -ArgumentList $arguments -WindowStyle Hidden -PassThru
    if (-not $process.WaitForExit(360000)) {
        Stop-Process -Id $process.Id
        throw "$name exceeded six minutes; see Logs/$name.log"
    }
    $process.Refresh()
    if ($process.ExitCode -ne 0) { throw "$name failed with exit $($process.ExitCode); see Logs/$name.log" }
    Write-Output "$name completed"
}
Invoke-UnityCheck 'LastStep.Editor.DuelInvariantValidation.Run' 'duel-invariants' $false
Invoke-UnityCheck 'LastStep.Editor.UpgradeValidation.Run' 'staircase-final' $false
Copy-Item -LiteralPath Logs/upgrade-validation.txt -Destination Documentation/Validation/staircase.txt
Invoke-UnityCheck 'LastStep.Editor.UpgradeValidation.RunNMU' 'nmu-final' $false
Copy-Item -LiteralPath Logs/upgrade-validation.txt -Destination Documentation/Validation/nmu.txt
Invoke-UnityCheck 'LastStep.Editor.WindowsBuild.RunMono' 'windows-final' $true
Copy-Item -LiteralPath Logs/windows-mono-build.txt -Destination Documentation/Validation/windows-build.txt
