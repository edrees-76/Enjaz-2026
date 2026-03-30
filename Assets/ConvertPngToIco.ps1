param($inPng, $outIco)
$pngBytes = [System.IO.File]::ReadAllBytes($inPng)
$ms = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter $ms

# ICO Header
$bw.Write([ushort]0) # Reserved
$bw.Write([ushort]1) # Type: Icon
$bw.Write([ushort]1) # Count

# Directory Entry
$bw.Write([byte]0)   # Width (0 for 256px)
$bw.Write([byte]0)   # Height (0 for 256px)
$bw.Write([byte]0)   # Palette size
$bw.Write([byte]0)   # Reserved
$bw.Write([ushort]1) # Color planes
$bw.Write([ushort]32)# Bits per pixel
$bw.Write([uint32]$pngBytes.Length) # Image size
$bw.Write([uint32]22) # Image offset

# Image Data
$bw.Write($pngBytes)

[System.IO.File]::WriteAllBytes($outIco, $ms.ToArray())
$bw.Close()
$ms.Close()
