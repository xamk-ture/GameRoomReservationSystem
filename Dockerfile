# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY Server/gameroombookingsys.csproj ./
RUN dotnet restore "gameroombookingsys.csproj"

# Copy the rest of the source
COPY Server/. ./

# Publish
RUN dotnet publish "gameroombookingsys.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Set ASP.NET Core environment
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Copy published output
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "gameroombookingsys.dll"]