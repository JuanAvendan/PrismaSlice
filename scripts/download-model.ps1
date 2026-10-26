# Descarga EmbeddingGemma (ONNX, int8) y su tokenizador a models/embeddinggemma-300m.
# Revisión fijada y SHA256 verificados: misma versión que usa el Dockerfile.
# El modelo se distribuye bajo los Gemma Terms of Use: https://ai.google.dev/gemma/terms
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'   # Invoke-WebRequest es mucho más rápido sin barra de progreso

$revision = '5090578d9565bb06545b4552f76e6bc2c93e4a66'
$base = "https://huggingface.co/onnx-community/embeddinggemma-300m-ONNX/resolve/$revision"
$dest = Join-Path $PSScriptRoot '..\models\embeddinggemma-300m'
New-Item -ItemType Directory -Force $dest | Out-Null

$files = [ordered]@{
    'tokenizer.model'                = '1299c11d7cf632ef3b4e11937501358ada021bbdf7c47638d13c0ee982f2e79c'
    'onnx/model_quantized.onnx'      = '172efde319fe1542dc41f31be6154910b05b78f7a861c265c4600eec906bd6d8'
    'onnx/model_quantized.onnx_data' = '705626e28e4c23c82ade34566b4197d97f534c12275fa406dfb71e9937d388c0'
}

foreach ($remote in $files.Keys) {
    $file = Join-Path $dest (Split-Path $remote -Leaf)
    $expected = $files[$remote]

    if ((Test-Path $file) -and (Get-FileHash $file -Algorithm SHA256).Hash -eq $expected) {
        Write-Host "ok        $(Split-Path $remote -Leaf)"
        continue
    }

    Write-Host "descargando $(Split-Path $remote -Leaf)..."
    Invoke-WebRequest -Uri "$base/$remote" -OutFile $file -UseBasicParsing
    if ((Get-FileHash $file -Algorithm SHA256).Hash -ne $expected) {
        throw "SHA256 no coincide: $file"
    }
}

Write-Host "Modelo listo en $((Resolve-Path $dest).Path)"
