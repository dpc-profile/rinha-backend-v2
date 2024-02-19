using System.Collections.Concurrent;
using System.Text.Json;
using StackExchange.Redis;

namespace RinhaBachendV2;

public class AtualizaConcurrentDictService : BackgroundService
{
    private readonly ConcurrentDictionary<string, PessoaModel> _pessoaMap;
    private readonly IConnectionMultiplexer _multiplexer;
    private readonly ISubscriber _subscriber;

    public AtualizaConcurrentDictService(IConnectionMultiplexer multiplexer,
        ConcurrentDictionary<string, PessoaModel> pessoaMap)
    {
        _pessoaMap = pessoaMap;
        _multiplexer = multiplexer;
        _subscriber = _multiplexer.GetSubscriber();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _subscriber.SubscribeAsync("busca", async (channel, message) =>
        {
            var pessoa = JsonSerializer.Deserialize<PessoaModel>(message);

            // Adiciona no ConcurrentDict uma key com os campos que são usado na consulta, e o pessoaModel como valor
            string buscaStack = pessoa.Stack == null ? "" : string.Join("", pessoa.Stack.Select(s => s.ToString()));
            string buscaKeyValue = $"{pessoa.Apelido}{pessoa.Nome}{buscaStack}";
            _pessoaMap.TryAdd(buscaKeyValue, pessoa);
        }); 
    }
}