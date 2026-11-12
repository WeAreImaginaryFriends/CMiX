# Serves the UI kit on http://localhost:<port>/ for a browser that cannot open a local file with its style sheets.
# A browser on the computer opens gallery.html directly and does not need this script.
param([int]$Port = 5178)

$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$types = @{
    '.html' = 'text/html; charset=utf-8'; '.css' = 'text/css; charset=utf-8'; '.js' = 'text/javascript; charset=utf-8'
    '.svg' = 'image/svg+xml'; '.png' = 'image/png'; '.json' = 'application/json'
}

$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add("http://localhost:$Port/")
$listener.Start()
Write-Output "UI kit: http://localhost:$Port/gallery.html"

try {
    while ($listener.IsListening) {
        $context = $listener.GetContext()
        $response = $context.Response
        try {
            $relative = [Uri]::UnescapeDataString($context.Request.Url.AbsolutePath).TrimStart('/')
            if ($relative -eq '') { $relative = 'gallery.html' }
            $path = [IO.Path]::GetFullPath((Join-Path $root $relative))
            if ($path.StartsWith($root + [IO.Path]::DirectorySeparatorChar) -and (Test-Path -LiteralPath $path -PathType Leaf)) {
                $bytes = [IO.File]::ReadAllBytes($path)
                $type = $types[[IO.Path]::GetExtension($path).ToLowerInvariant()]
                if (-not $type) { $type = 'application/octet-stream' }
                $response.ContentType = $type
                $response.Headers.Add('Cache-Control', 'no-store')
                $response.OutputStream.Write($bytes, 0, $bytes.Length)
            }
            else {
                $response.StatusCode = 404
            }
        }
        catch {
            $response.StatusCode = 500
        }
        finally {
            $response.Close()
        }
    }
}
finally {
    $listener.Stop()
}
