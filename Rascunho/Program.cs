using Microsoft.EntityFrameworkCore;
using Rascunho.Data;
using Rascunho.Endpoints;
using Rascunho.Infra;
using Rascunho.Services;
using HashidsNet;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Banco de dados
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

//Serviços
builder.Services.AddScoped<UsuarioService>();

//Tratamento global de exeções
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

//Chave de encriptação de Id
builder.Services.AddSingleton<IHashids>(new Hashids("PontoDaDanca_Chave_Super_Secreta_2026", minHashLength: 8));

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapUsuarioEndpoints();
app.Run();
