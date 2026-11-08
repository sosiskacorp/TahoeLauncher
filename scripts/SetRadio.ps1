param(
    [string]$Kind = "WiFi",
    [string]$Action = "Get"
)
Add-Type -AssemblyName System.Runtime.WindowsRuntime
$asTaskGeneric = ([System.WindowsRuntimeSystemExtensions].GetMethods() | ? { $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1' })[0]
function Await($winRtTask, $resultType) {
    $asTask = $asTaskGeneric.MakeGenericMethod($resultType)
    $netTask = $asTask.Invoke($null, @($winRtTask))
    $netTask.Wait(-1) | Out-Null
    $netTask.Result
}
[Windows.Devices.Radios.Radio, Windows.System.Devices, ContentType = WindowsRuntime] | Out-Null
$op = [Windows.Devices.Radios.Radio]::GetRadiosAsync()
$radios = Await $op ([System.Collections.Generic.IReadOnlyList[Windows.Devices.Radios.Radio]])
$r = $radios | Where-Object { $_.Kind -eq $Kind }
if (-not $r) {
    Write-Host "RADIO_NOT_FOUND"
    exit
}

if ($Action -eq "Get") {
    Write-Host "RADIO_STATE|$Kind|$($r.State)"
    exit
}

$targetState = $null
if ($Action -eq "On") {
    $targetState = [Windows.Devices.Radios.RadioState]::On
} elseif ($Action -eq "Off") {
    $targetState = [Windows.Devices.Radios.RadioState]::Off
} elseif ($Action -eq "Toggle") {
    if ($r.State -eq [Windows.Devices.Radios.RadioState]::On) {
        $targetState = [Windows.Devices.Radios.RadioState]::Off
    } else {
        $targetState = [Windows.Devices.Radios.RadioState]::On
    }
}

if ($targetState -ne $null) {
    $status = Await ($r.SetStateAsync($targetState)) ([Windows.Devices.Radios.RadioAccessStatus])
    Write-Host "RADIO_STATE|$Kind|$targetState|$status"
}
