#!/usr/bin/env sh
# Descarga EmbeddingGemma (ONNX, int8) y su tokenizador a models/embeddinggemma-300m.
# Revisión fijada y SHA256 verificados: misma versión que usa el Dockerfile.
# El modelo se distribuye bajo los Gemma Terms of Use: https://ai.google.dev/gemma/terms
set -eu

REVISION=5090578d9565bb06545b4552f76e6bc2c93e4a66
BASE="https://huggingface.co/onnx-community/embeddinggemma-300m-ONNX/resolve/$REVISION"
DEST="$(dirname "$0")/../models/embeddinggemma-300m"
mkdir -p "$DEST"

download() { # ruta-remota sha256
  file="$DEST/$(basename "$1")"
  if [ -f "$file" ] && echo "$2  $file" | sha256sum -c --status 2>/dev/null; then
    echo "ok        $(basename "$1")"
    return
  fi
  echo "descargando $(basename "$1")…"
  curl -fL --retry 3 -o "$file" "$BASE/$1"
  echo "$2  $file" | sha256sum -c --status || { echo "SHA256 no coincide: $file" >&2; exit 1; }
}

download tokenizer.model                1299c11d7cf632ef3b4e11937501358ada021bbdf7c47638d13c0ee982f2e79c
download onnx/model_quantized.onnx      172efde319fe1542dc41f31be6154910b05b78f7a861c265c4600eec906bd6d8
download onnx/model_quantized.onnx_data 705626e28e4c23c82ade34566b4197d97f534c12275fa406dfb71e9937d388c0

echo "Modelo listo en $DEST"
