FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
# copy csproj file and restore
COPY stock_backend/stock_backend.csproj ./

# Copy everything else and build
COPY stock_backend/ ./
# trim-self-contained and trimmed .NET build
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish stock_backend.csproj -c Release -o out


# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "stock_backend.dll"]