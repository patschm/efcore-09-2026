# Build context is the repo root (not this folder) - ProjectReference targets live in sibling
# directories (Catalog/Domain, Catalog/Application, SharedKernel, BuildingBlocks, etc.).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish "Catalog/Api/WebShop.Catalog.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "WebShop.Catalog.Api.dll"]
