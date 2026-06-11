# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Install Node.js for frontend build
RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    && curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get install -y nodejs \
    && rm -rf /var/lib/apt/lists/*

# copy csproj and restore
COPY MiniApp.csproj ./
RUN dotnet restore ./MiniApp.csproj

# copy everything else
COPY . ./

# Build frontend
WORKDIR /src/frontend
RUN npm install && npm run build

# Publish .NET app
WORKDIR /src
RUN dotnet publish ./MiniApp.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update \
	&& apt-get install -y --no-install-recommends libgssapi-krb5-2 \
	&& rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish ./
# Copy frontend dist folder explicitly
COPY --from=build /src/frontend/dist ./frontend/dist

# Kestrel listens on 8080 inside the container
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "MiniApp.dll"]
