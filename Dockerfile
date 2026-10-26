# syntax=docker/dockerfile:1

# Un solo Dockerfile, dos imágenes:
#   docker build --target api  -t prismaslice-api  .
#   docker build --target seed -t prismaslice-seed .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Primero solo los .csproj: la capa de restore se cachea mientras no cambien dependencias.
COPY global.json ./
COPY src/PrismaSlice.Domain/PrismaSlice.Domain.csproj                 src/PrismaSlice.Domain/
COPY src/PrismaSlice.Application/PrismaSlice.Application.csproj       src/PrismaSlice.Application/
COPY src/PrismaSlice.Infrastructure/PrismaSlice.Infrastructure.csproj src/PrismaSlice.Infrastructure/
COPY src/PrismaSlice.Api/PrismaSlice.Api.csproj                       src/PrismaSlice.Api/
COPY tools/PrismaSlice.SeedData/PrismaSlice.SeedData.csproj           tools/PrismaSlice.SeedData/
RUN dotnet restore src/PrismaSlice.Api/PrismaSlice.Api.csproj \
 && dotnet restore tools/PrismaSlice.SeedData/PrismaSlice.SeedData.csproj

COPY src/ src/
COPY tools/ tools/
RUN dotnet publish src/PrismaSlice.Api/PrismaSlice.Api.csproj \
        --configuration Release --no-restore --output /out/api \
 && dotnet publish tools/PrismaSlice.SeedData/PrismaSlice.SeedData.csproj \
        --configuration Release --no-restore --output /out/seed

# Imágenes "chiseled": sin shell ni gestor de paquetes, usuario no root.
FROM mcr.microsoft.com/dotnet/runtime:10.0-noble-chiseled AS seed
WORKDIR /app
COPY --from=build /out/seed .
ENTRYPOINT ["dotnet", "PrismaSlice.SeedData.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS api
WORKDIR /app
COPY --from=build /out/api .
EXPOSE 8080
ENTRYPOINT ["dotnet", "PrismaSlice.Api.dll"]
