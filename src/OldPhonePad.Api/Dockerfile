FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

COPY src/OldPhonePad/*.csproj      src/OldPhonePad/
COPY src/OldPhonePad.Api/*.csproj  src/OldPhonePad.Api/
RUN dotnet restore src/OldPhonePad.Api/OldPhonePad.Api.csproj

COPY src/ src/
RUN dotnet publish src/OldPhonePad.Api/OldPhonePad.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "OldPhonePad.Api.dll"]
