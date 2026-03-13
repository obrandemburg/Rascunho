using HashidsNet;
using Rascunho.Entities;

namespace Rascunho.DTOs;

// 1. O que o Front-end envia para a API (O ato de fazer a chamada)
public record AlunoPresencaRequest(string AlunoIdHash, bool Presente);
public record RegistrarChamadaRequest(DateOnly DataAula, List<AlunoPresencaRequest> Presencas);

// 2. O que a API envia para o Front-end (A lista de alunos para a tela do professor)
public record AlunoChamadaResponse(string AlunoIdHash, string Nome, string FotoUrl, string Papel, bool Presente);
public record ObterChamadaResponse(string TurmaIdHash, DateOnly DataAula, List<AlunoChamadaResponse> Alunos);