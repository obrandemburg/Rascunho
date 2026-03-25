# Estágio de Build
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# CORREÇÃO CRÍTICA: Copiar o projeto Shared também!
COPY ["Rascunho/Rascunho.csproj", "Rascunho/"]
COPY ["Rascunho.Shared/Rascunho.Shared.csproj", "Rascunho.Shared/"]

# Restaura as dependências usando o cache
RUN dotnet restore "Rascunho/Rascunho.csproj"

# Copia todo o resto do código e compila
COPY . .
WORKDIR "/src/Rascunho"
RUN dotnet publish "Rascunho.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Estágio de Runtime (Enxuto)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final

# CORREÇÃO DE SEGURANÇA (GLOBALIZAÇÃO): Instala bibliotecas de cultura (ICU) para não quebrar datas/moedas pt-BR
RUN apk add --no-cache icu-libs tzdata
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080 
ENTRYPOINT ["dotnet", "Rascunho.dll"]