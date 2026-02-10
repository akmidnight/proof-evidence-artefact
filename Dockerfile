# Stage 1: Build .NET API
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dotnet-build
WORKDIR /src
COPY FlexProof.slnx .
COPY src/ src/
COPY tests/ tests/
RUN dotnet restore FlexProof.slnx
RUN dotnet publish src/FlexProof.Api/FlexProof.Api.csproj -c Release -o /app/api

# Stage 2: Build Angular UI
FROM node:22-alpine AS ui-build
WORKDIR /ui
COPY src/FlexProof.Ui/package*.json ./
RUN npm ci
COPY src/FlexProof.Ui/ .
RUN npm run build

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=dotnet-build /app/api .
COPY --from=ui-build /ui/dist/flexproof-ui/browser wwwroot/

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "FlexProof.Api.dll"]
