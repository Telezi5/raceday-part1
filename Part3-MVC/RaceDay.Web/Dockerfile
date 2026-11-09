ARG DOTNET_VERSION=10.0

# ---------- build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src
COPY RaceDay.Web.csproj ./
RUN dotnet restore
COPY . .
RUN dotnet publish RaceDay.Web.csproj -c Release -o /app/publish --no-restore

# ---------- runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "RaceDay.Web.dll"]
