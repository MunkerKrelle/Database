FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Relationel Database.csproj", "./"]
RUN dotnet restore "Relationel Database.csproj"

COPY . .
RUN dotnet publish "Relationel Database.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Relationel Database.dll"]
