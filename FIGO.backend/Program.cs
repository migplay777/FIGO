using FIGO.backend;

var viagemService = new ViagemServices();
var receitaService = new ReceitaService();
var despesaService = new DespesaService();
var resumoService = new ResumoService();

var viagem = new Viagem
{
    Nome = "Viagem Missionária São Paulo",
    Destino = "São Paulo - SP",
    DataInicio = new DateTime(2026, 10, 12),
    DataFim = new DateTime(2026, 10, 15),
    QuantidadeParticipantes = 18
};
viagemService.Adicionar(viagem);
Console.WriteLine($"Viagem criada com Id {viagem.Id}");

viagem.QuantidadeParticipantes = 20;
viagemService.Editar(viagem);
Console.WriteLine("Viagem editada: participantes atualizado para 20");

receitaService.Adicionar(new Receita
{
    ViagemId = viagem.Id,
    Descricao = "Oferta",
    Valor = 3000m,
    Data = new DateTime(2026, 10, 1),
    Origem = "Igreja"
});

despesaService.Adicionar(new Despesa
{
    ViagemId = viagem.Id,
    Descricao = "Combustível",
    Valor = 850m,
    Data = new DateTime(2026, 10, 12),
    Categoria = "Transporte"
});
despesaService.Adicionar(new Despesa
{
    ViagemId = viagem.Id,
    Descricao = "Hotel",
    Valor = 900m,
    Data = new DateTime(2026, 10, 13),
    Categoria = "Hospedagem"
});
despesaService.Adicionar(new Despesa
{
    ViagemId = viagem.Id,
    Descricao = "Almoço",
    Valor = 420m,
    Data = new DateTime(2026, 10, 12),
    Categoria = "Alimentação"
});

Console.WriteLine();
Console.WriteLine("Todas as viagens cadastradas:");
foreach (var v in viagemService.Listar())
    Console.WriteLine($"  [{v.Id}] {v.Nome} - {v.Destino}");

var resumo = resumoService.ObterResumo(viagem.Id);
if (resumo is not null)
{
    Console.WriteLine();
    Console.WriteLine($"Viagem: {resumo.Nome}");
    Console.WriteLine($"Receitas: R$ {resumo.TotalReceitas}");
    Console.WriteLine($"Despesas: R$ {resumo.TotalDespesas}");
    Console.WriteLine($"Saldo:    R$ {resumo.Saldo}");
}

Console.WriteLine();
Console.WriteLine("Despesas por categoria:");
foreach (var item in despesaService.ObterPorCategoria(viagem.Id))
    Console.WriteLine($"  {item.Categoria}: R$ {item.Total}");
