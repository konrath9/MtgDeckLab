FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY MtgDeckLab.sln ./
COPY src/MtgDeckLab.Domain/MtgDeckLab.Domain.csproj src/MtgDeckLab.Domain/
COPY src/MtgDeckLab.Engine/MtgDeckLab.Engine.csproj src/MtgDeckLab.Engine/
COPY src/MtgDeckLab.Application/MtgDeckLab.Application.csproj src/MtgDeckLab.Application/
COPY src/MtgDeckLab.Infrastructure/MtgDeckLab.Infrastructure.csproj src/MtgDeckLab.Infrastructure/
COPY src/MtgDeckLab.API/MtgDeckLab.API.csproj src/MtgDeckLab.API/
RUN dotnet restore src/MtgDeckLab.API/MtgDeckLab.API.csproj

COPY src/ src/
RUN dotnet publish src/MtgDeckLab.API/MtgDeckLab.API.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app .
USER app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "MtgDeckLab.API.dll"]
