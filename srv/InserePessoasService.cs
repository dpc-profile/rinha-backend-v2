
using Npgsql;

namespace RinhaBachendV2;

public class InserePessoasService : BackgroundService
{
    private readonly NpgsqlConnection _conn;
    private readonly ILogger<InserePessoasService> _logger;

    public InserePessoasService(NpgsqlConnection conn, ILogger<InserePessoasService> logger)
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

        // Adicionar PublishAsync("busca") aqui em caso de overhead no endpoint POST /pessoa

    }
}