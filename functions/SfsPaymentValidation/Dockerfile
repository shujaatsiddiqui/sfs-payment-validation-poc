# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copy project and restore
COPY *.csproj ./
RUN dotnet restore

# Copy and build
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# Runtime stage - Use .NET 8 aspnet as base and add Functions runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /home/site/wwwroot

# Install Azure Functions Core Tools
RUN apt-get update && \
    apt-get install -y wget unzip && \
    wget https://github.com/Azure/azure-functions-core-tools/releases/download/4.0.5907/Azure.Functions.Cli.linux-x64.4.0.5907.zip && \
    unzip Azure.Functions.Cli.linux-x64.4.0.5907.zip -d /azure-functions-runtime && \
    rm Azure.Functions.Cli.linux-x64.4.0.5907.zip && \
    chmod +x /azure-functions-runtime/func && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Copy published app and config
COPY --from=build /app/publish ./
COPY --from=build /src/host.json ./

# Environment
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    PATH="/azure-functions-runtime:${PATH}"

EXPOSE 80

# Start Azure Functions host
CMD ["/azure-functions-runtime/func", "start", "--port", "80"]
