FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app
COPY ./out/backend .
EXPOSE 8080
ENTRYPOINT ["dotnet", "ForeverBloom.Api.dll"]