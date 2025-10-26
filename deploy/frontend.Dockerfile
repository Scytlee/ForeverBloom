FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
# Add globalization support
RUN apk add --no-cache icu-libs icu-data-full
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=0 \
    LANG=pl_PL.UTF-8 \
    LC_ALL=pl_PL.UTF-8
WORKDIR /app
COPY ./out/frontend .
EXPOSE 8080
ENTRYPOINT ["dotnet", "ForeverBloom.Frontend.RazorPages.dll"]