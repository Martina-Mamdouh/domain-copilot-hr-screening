FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["src/DomainCopilot.Api/DomainCopilot.Api.csproj", "DomainCopilot.Api/"]
COPY ["src/DomainCopilot.Tests/DomainCopilot.Tests.csproj", "DomainCopilot.Tests/"]
RUN dotnet restore "DomainCopilot.Api/DomainCopilot.Api.csproj"

# Copy everything else and build
COPY src/ .
WORKDIR "/src/DomainCopilot.Api"
RUN dotnet build "DomainCopilot.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DomainCopilot.Api.csproj" -c Release -o /app/publish

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=publish /app/publish .

# Expose ports
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "DomainCopilot.Api.dll"]
