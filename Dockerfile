# Estágio de Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia o arquivo de projeto e restaura as dependências primeiro (otimiza o cache do Docker)
COPY ["Rascunho/Rascunho.csproj", "Rascunho/"]
RUN dotnet restore "Rascunho/Rascunho.csproj"

# Copia todo o resto do código e compila
COPY . .
WORKDIR "/src/Rascunho"
RUN dotnet publish "Rascunho.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Estágio de Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080 
ENTRYPOINT ["dotnet", "Rascunho.dll"]