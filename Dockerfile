# Compila com o SDK; copia o projeto antes do código para reutilizar o restore.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /codigo
COPY src/Fcg.Catalog.Domain/Fcg.Catalog.Domain.csproj src/Fcg.Catalog.Domain/
COPY src/Fcg.Catalog.Application/Fcg.Catalog.Application.csproj src/Fcg.Catalog.Application/
COPY src/Fcg.Catalog.Infrastructure/Fcg.Catalog.Infrastructure.csproj src/Fcg.Catalog.Infrastructure/
COPY src/Fcg.Catalog.API/Fcg.Catalog.API.csproj src/Fcg.Catalog.API/
RUN dotnet restore src/Fcg.Catalog.API/Fcg.Catalog.API.csproj

# Publica somente os arquivos necessários para executar a aplicação.
COPY src/ src/
RUN dotnet publish src/Fcg.Catalog.API/Fcg.Catalog.API.csproj -c Release --no-restore -o /publicado

# A imagem final contém o runtime, sem SDK nem código-fonte.
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /publicado .

# Usa o usuário sem privilégios fornecido pela imagem .NET (porta padrão 8080).
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Fcg.Catalog.API.dll"]
