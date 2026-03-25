using System;
using System.Collections.Generic;

namespace Rascunho.Shared.DTOs;

public record CriarTurmaRequest(string RitmoIdHash, string SalaIdHash, DateOnly DataInicio, int DiaDaSemana, string HorarioInicio, string HorarioFim, string Nivel, int LimiteAlunos, string LinkWhatsApp, List<string> ProfessoresIdsHash);
public record TrocarSalaRequest(string NovaSalaIdHash, int NovoLimiteAlunos);
public record MatricularRequest(string Papel);
public record MatricularAdminRequest(string AlunoIdHash, string Papel);
public record ObterTurmaResponse(string IdHash, string RitmoNome, string SalaNome, DateOnly DataInicio, int DiaDaSemana, TimeSpan HorarioInicio, TimeSpan HorarioFim, string Nivel, int LimiteAlunos, int QuantidadeMatriculados, string LinkWhatsApp, bool Ativa, List<string> NomesProfessores);

// NOVO Sprint 2: aluno matriculado simplificado (para o modal "Ver Alunos")
public record AlunoMatriculadoResponse(
    string AlunoIdHash,
    string Nome,
    string FotoUrl,
    string Papel          // "Condutor" | "Conduzido" | "Ambos"
);