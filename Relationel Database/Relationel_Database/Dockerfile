FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Kopiér csproj med underscore-navn
COPY ["Relationel_Database.csproj", "./"]
RUN dotnet restore "Relationel_Database.csproj"

COPY . .
RUN dotnet publish "Relationel_Database.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app

# Installer Postgres client til wait-scriptet
RUN apt-get update && apt-get install -y postgresql-client && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Kopiér wait-script
COPY wait-for-postgres.sh /wait-for-postgres.sh
RUN chmod +x /wait-for-postgres.sh

# Ingen ENTRYPOINT her — Compose styrer det
