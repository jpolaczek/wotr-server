FROM mcr.microsoft.com/dotnet/sdk:10.0.401@sha256:e70cdb7f80b0348f5cb85f19a8f670fca061f033d57eed12fa003d58b0e06317 AS build
WORKDIR /source
COPY global.json Directory.Build.props ./
COPY src/ src/
COPY content/ content/
RUN dotnet restore src/Wotr.Server/Wotr.Server.csproj --locked-mode
RUN dotnet publish src/Wotr.Server/Wotr.Server.csproj -c Release --no-restore -o /app -p:UseAppHost=false
# DIST-05: the operator's release tool ships with the server it publishes for; it holds no private key.
RUN dotnet restore src/Wotr.ReleaseTool/Wotr.ReleaseTool.csproj --locked-mode
RUN dotnet publish src/Wotr.ReleaseTool/Wotr.ReleaseTool.csproj -c Release --no-restore -o /app/release-tool -p:UseAppHost=false
RUN sha256sum content/*.json src/Wotr.Server/Migrations/*.sql > /app/release-content.sha256

FROM mcr.microsoft.com/dotnet/aspnet:10.0.9@sha256:7644f992230d35cf230017189d4038c0ae0f7388b13f4f7ae1900a155bafb597
WORKDIR /app
COPY --from=build /app/ ./
ENV ASPNETCORE_HTTP_PORTS=8080
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "Wotr.Server.dll"]
