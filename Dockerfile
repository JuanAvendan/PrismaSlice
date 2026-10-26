# syntax=docker/dockerfile:1

# Un solo Dockerfile, dos imágenes:
#   docker build --target api  -t prismaslice-api  .
#   docker build --target seed -t prismaslice-seed .

# Modelo de embeddings (EmbeddingGemma, ONNX int8). Revisión fijada y SHA256
# verificados: el build falla si un archivo cambia. Ver THIRD_PARTY_NOTICES.md.
FROM busybox:1.37 AS model
ARG MODEL_URL=https://huggingface.co/onnx-community/embeddinggemma-300m-ONNX/resolve/5090578d9565bb06545b4552f76e6bc2c93e4a66
ADD --checksum=sha256:1299c11d7cf632ef3b4e11937501358ada021bbdf7c47638d13c0ee982f2e79c \
    ${MODEL_URL}/tokenizer.model                /models/embeddinggemma-300m/tokenizer.model
ADD --checksum=sha256:172efde319fe1542dc41f31be6154910b05b78f7a861c265c4600eec906bd6d8 \
    ${MODEL_URL}/onnx/model_quantized.onnx      /models/embeddinggemma-300m/model_quantized.onnx
ADD --checksum=sha256:705626e28e4c23c82ade34566b4197d97f534c12275fa406dfb71e9937d388c0 \
    ${MODEL_URL}/onnx/model_quantized.onnx_data /models/embeddinggemma-300m/model_quantized.onnx_data
# ADD desde URL deja los archivos en 600; las imágenes finales corren sin root.
RUN chmod -R a+rX /models

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Primero solo los .csproj: la capa de restore se cachea mientras no cambien dependencias.
COPY global.json ./
COPY src/PrismaSlice.Domain/PrismaSlice.Domain.csproj                 src/PrismaSlice.Domain/
COPY src/PrismaSlice.Application/PrismaSlice.Application.csproj       src/PrismaSlice.Application/
COPY src/PrismaSlice.Infrastructure/PrismaSlice.Infrastructure.csproj src/PrismaSlice.Infrastructure/
COPY src/PrismaSlice.Api/PrismaSlice.Api.csproj                       src/PrismaSlice.Api/
COPY tools/PrismaSlice.SeedData/PrismaSlice.SeedData.csproj           tools/PrismaSlice.SeedData/
# linux-x64: solo las librerías nativas de ONNX Runtime que la imagen necesita.
RUN dotnet restore src/PrismaSlice.Api/PrismaSlice.Api.csproj --runtime linux-x64 \
 && dotnet restore tools/PrismaSlice.SeedData/PrismaSlice.SeedData.csproj --runtime linux-x64

COPY src/ src/
COPY tools/ tools/
RUN dotnet publish src/PrismaSlice.Api/PrismaSlice.Api.csproj \
        --configuration Release --runtime linux-x64 --self-contained false \
        --no-restore --output /out/api \
 && dotnet publish tools/PrismaSlice.SeedData/PrismaSlice.SeedData.csproj \
        --configuration Release --runtime linux-x64 --self-contained false \
        --no-restore --output /out/seed

# Imágenes "chiseled": sin shell ni gestor de paquetes, usuario no root.
FROM mcr.microsoft.com/dotnet/runtime:10.0-noble-chiseled AS seed
WORKDIR /app
COPY --from=build /out/seed .
COPY --from=model /models ./models
COPY THIRD_PARTY_NOTICES.md .
ENTRYPOINT ["dotnet", "PrismaSlice.SeedData.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS api
WORKDIR /app
COPY --from=build /out/api .
COPY --from=model /models ./models
COPY THIRD_PARTY_NOTICES.md .
EXPOSE 8080
ENTRYPOINT ["dotnet", "PrismaSlice.Api.dll"]
