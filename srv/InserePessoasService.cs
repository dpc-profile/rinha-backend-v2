
using System.Collections.Concurrent;
using System.Text.Json;

using Npgsql;

using StackExchange.Redis;

namespace RinhaBachendV2;

public class InserePessoasService : BackgroundService
{
    private readonly NpgsqlConnection _conn;
    private readonly ILogger<InserePessoasService> _logger;
    ConcurrentQueue<PessoaModel> _queue;
    private readonly IDatabase _cache;

    public InserePessoasService(NpgsqlConnection conn,
        ILogger<InserePessoasService> logger,
        ConcurrentQueue<PessoaModel> queue,
        IDatabase cache)
    {
        _conn = conn;
        _logger = logger;
        _queue = queue;
        _cache = cache;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        bool connected = false;

        while (!connected)
        {
            try
            {
                await _conn.OpenAsync();
                connected = true;
                _logger.LogInformation("connected to postgres!!! yey");
            }
            catch (NpgsqlException)
            {
                _logger.LogWarning("retrying connection to postgres");
                await Task.Delay(5_000);
            }
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(2_000);

            List<PessoaModel> listaPessoa = new();

            while (_queue.TryDequeue(out PessoaModel pessoa))
            {
                listaPessoa.Add(pessoa);
            }

            if (listaPessoa.Count == 0)
                continue;

            try
            {
                var batch = _conn.CreateBatch();
                // var batchCommands = new List<NpgsqlBatchCommand>();

                foreach (var p in listaPessoa)
                {
                    var batchCmd = new NpgsqlBatchCommand(@"
                        insert into pessoas
                        (id, apelido, nome, nascimento, stack)
                        values ($1, $2, $3, $4, $5)
                        on conflict do nothing;
                    ");
                    batchCmd.Parameters.AddWithValue(p.Id);
                    batchCmd.Parameters.AddWithValue(p.Apelido);
                    batchCmd.Parameters.AddWithValue(p.Nome);
                    batchCmd.Parameters.AddWithValue(p.Nascimento.Value);
                    batchCmd.Parameters.AddWithValue(p.Stack == null ? DBNull.Value : p.Stack.Select(s => s.ToString()).ToArray());
                    batch.BatchCommands.Add(batchCmd);


                    var buscaStackValue = p.Stack == null ? "" : string.Join("", p.Stack.Select(s => s.ToString()));
                    var buscaValue = $"{p.Apelido}{p.Nome}{buscaStackValue}" ?? "";
                    await _cache.PublishAsync("busca", JsonSerializer.Serialize<PessoaModel>(p), CommandFlags.FireAndForget);

                }

                await batch.ExecuteNonQueryAsync();
            }
            catch (Exception error)
            {
                _logger.LogError("Erro não esperado ao inserir pessoas no db.", error);
            }

            // Adicionar PublishAsync("busca") aqui em caso de overhead no endpoint POST /pessoa

        }

        await _conn.CloseAsync();
        await _conn.DisposeAsync();

    }
}