
using Microsoft.VisualBasic;

using Npgsql;

namespace RinhaBachendV2;

public class InserePessoas : BackgroundService
{
    private readonly NpgsqlConnection _conn;
    private readonly ILogger<InserePessoas> _logger;

    private PessoaModel _pessoaModel;
    public InserePessoas(NpgsqlConnection conn, ILogger<InserePessoas> logger)
    {
        _conn = conn;
        _logger = logger;
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

    }
}