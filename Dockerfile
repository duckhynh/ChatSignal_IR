# Multi-stage Dockerfile for .NET 8 Razor Pages app
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY Student_Management_System/Student_Management_System.csproj Student_Management_System/
RUN dotnet restore Student_Management_System/Student_Management_System.csproj

# Copy everything and publish
COPY . .
WORKDIR /src/Student_Management_System
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Bind to the port Render provides via environment variable PORT
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}

COPY --from=build /app/publish .
EXPOSE 8080

ENTRYPOINT ["dotnet", "Student_Management_System.dll"]
