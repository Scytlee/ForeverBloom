FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app
COPY ./out/dbmanager .
ENTRYPOINT ["dotnet", "ForeverBloom.DatabaseManager.dll"] 