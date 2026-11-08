param(
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
[Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager, Windows.Media, ContentType = WindowsRuntime] | Out-Null
$mgrType = [Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager]
$op = $mgrType::RequestAsync()
$manager = Await $op $mgrType
$cur = $manager.GetCurrentSession()
if (-not $cur) {
    Write-Host "NO_MEDIA"
    exit
}

if ($Action -eq "Play") {
    $cur.TryPlayAsync() | Out-Null
} elseif ($Action -eq "Pause") {
    $cur.TryPauseAsync() | Out-Null
} elseif ($Action -eq "PlayPause") {
    $cur.TryTogglePlayPauseAsync() | Out-Null
} elseif ($Action -eq "Next") {
    $cur.TrySkipNextAsync() | Out-Null
} elseif ($Action -eq "Prev") {
    $cur.TrySkipPreviousAsync() | Out-Null
}

$mediaOp = $cur.TryGetMediaPropertiesAsync()
$props = Await $mediaOp ([Windows.Media.Control.GlobalSystemMediaTransportControlsSessionMediaProperties])
$playback = $cur.GetPlaybackInfo()
$timeline = $cur.GetTimelineProperties()

$hasThumb = "0"
if ($props.Thumbnail) {
    try {
        $streamOp = $props.Thumbnail.OpenReadAsync()
        $stream = Await $streamOp ([Windows.Storage.Streams.IRandomAccessStreamWithContentType])
        $m = ([System.IO.WindowsRuntimeStreamExtensions].GetMethods() | ? { $_.Name -eq 'AsStream' -and $_.GetParameters().Count -eq 1 })[0]
        $netStream = $m.Invoke($null, @($stream))
        $thumbPath = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "tahoe_cover.jpg")
        $fileStream = [System.IO.File]::Create($thumbPath)
        $netStream.CopyTo($fileStream)
        $fileStream.Dispose()
        $netStream.Dispose()
        $hasThumb = "1"
    } catch { }
}

$posSec = [int]$timeline.Position.TotalSeconds
$endSec = [int]$timeline.EndTime.TotalSeconds
$status = $playback.PlaybackStatus.ToString()
$app = $cur.SourceAppUserModelId

Write-Host "MEDIA_DATA|$($props.Title)|$($props.Artist)|$($props.AlbumTitle)|$app|$status|$posSec|$endSec|$hasThumb"
