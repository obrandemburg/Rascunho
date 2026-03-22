// Localização: Rascunho/Services/ConfiguracaoService.cs
using Microsoft.Extensions.Configuration;

namespace Rascunho.Services;

/// <summary>
/// Gerencia configurações do sistema que o Gerente pode alterar em tempo de execução.
/// 
/// Como funciona:
/// O IConfiguration do .NET é um objeto mutável quando você usa IConfigurationRoot.
/// Ao alterar um valor aqui, todos os serviços que leem via _configuration.GetValue()
/// passarão a usar o novo valor imediatamente — sem restart da aplicação.
/// 
/// IMPORTANTE: Essas alterações são IN-MEMORY. Ao reiniciar o servidor, os valores
/// voltam para o appsettings.json. Para persistência real, use um banco de dados
/// ou arquivo de configuração gravável (fase futura).
/// </summary>
public class ConfiguracaoService
{
    private readonly IConfigurationRoot _configRoot;

    // IConfigurationRoot é a interface que expõe o método Reload()
    // e permite alterar valores. É diferente de IConfiguration (somente leitura).
    public ConfiguracaoService(IConfiguration configuration)
    {
        // Cast seguro: o WebApplication.CreateBuilder() registra o IConfiguration
        // como IConfigurationRoot internamente.
        _configRoot = (IConfigurationRoot)configuration;
    }

    // ── Leituras ─────────────────────────────────────────────────

    /// <summary>Retorna o preço padrão atual de uma aula particular.</summary>
    public decimal ObterPrecoAulaParticular() =>
        _configRoot.GetValue<decimal>("AulaParticular:PrecoPadrao", 80.00m);

    /// <summary>Retorna a janela de elegibilidade para reposição em dias.</summary>
    public int ObterJanelaReposicaoDias() =>
        _configRoot.GetValue<int>("Reposicao:JanelaElegibilidadeDias", 30);

    // ── Alterações (apenas Gerente) ──────────────────────────────

    /// <summary>
    /// Atualiza o preço padrão das aulas particulares em memória.
    /// O novo valor é aplicado imediatamente em todas as novas solicitações.
    /// </summary>
    public void AtualizarPrecoAulaParticular(decimal novoPreco)
    {
        if (novoPreco <= 0)
            throw new ArgumentException("O preço deve ser maior que zero.");

        // Altera o valor diretamente no IConfigurationRoot em memória
        _configRoot["AulaParticular:PrecoPadrao"] = novoPreco.ToString("F2",
            System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Atualiza a janela de elegibilidade de reposição em dias.
    /// </summary>
    public void AtualizarJanelaReposicao(int dias)
    {
        if (dias < 7 || dias > 365)
            throw new ArgumentException("A janela deve ser entre 7 e 365 dias.");

        _configRoot["Reposicao:JanelaElegibilidadeDias"] = dias.ToString();
    }

    /// <summary>Retorna as configurações atuais em um objeto para exibição.</summary>
    public object ObterConfiguracoes() => new
    {
        PrecoAulaParticular = ObterPrecoAulaParticular(),
        JanelaReposicaoDias = ObterJanelaReposicaoDias()
    };
}