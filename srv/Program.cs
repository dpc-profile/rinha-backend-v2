using RinhaBachendV2;

using System.Collections.Concurrent;
using StackExchange.Redis;
using System.Text.Json;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

string dbString = builder.Configuration.GetConnectionString("DbConn");
builder.Services.AddNpgsqlDataSource(dbString);

string cacheString = builder.Configuration.GetConnectionString("CacheConn");


builder.Services.AddHostedService<InserePessoasService>();
builder.Services.AddHostedService<AtualizaConcurrentDictService>();

builder.Services.AddSingleton(_ => new ConcurrentDictionary<string, PessoaModel>());
builder.Services.AddSingleton(_ => new ConcurrentQueue<PessoaModel>());
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(cacheString));

ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(cacheString);
builder.Services.AddSingleton<IDatabase>(_ => redis.GetDatabase());


var app = builder.Build();

app.MapPost("/pessoas", async (IDatabase cache,
    ConcurrentQueue<PessoaModel> processingQueue,
    ConcurrentDictionary<string, PessoaModel> pessoaMap,
    IConnectionMultiplexer multiplexer,
    PessoaModel pessoaModel) =>
{

    bool pessoaModelValid = pessoaModel.IsValid();
    if (pessoaModelValid == false)
        return Results.UnprocessableEntity("Os dados da requisição estão invalidos");

    RedisValue apelidoUsado = await cache.StringGetAsync(pessoaModel.Apelido);
    if (!apelidoUsado.IsNull)
        return Results.UnprocessableEntity("Apelido já cadastrado");

    pessoaModel.Id = Guid.NewGuid();
    
    var redisPayload = new List<KeyValuePair<RedisKey, RedisValue>>
    {
        new KeyValuePair<RedisKey, RedisValue>(new RedisKey(pessoaModel.Id.ToString()), new RedisValue(JsonSerializer.Serialize(pessoaModel))),
        new KeyValuePair<RedisKey, RedisValue>(new RedisKey(pessoaModel.Apelido), RedisValue.EmptyString)
    };

    await cache.StringSetAsync(redisPayload.ToArray());
    processingQueue.Enqueue(pessoaModel);

    await cache.PublishAsync("busca", JsonSerializer.Serialize<PessoaModel>(pessoaModel), CommandFlags.FireAndForget);

    return Results.Created($"/pessoas/{pessoaModel.Id}", pessoaModel);
});

app.MapGet("/pessoas/{id}", async (IDatabase cache, string id) =>
{

    if (!Guid.TryParse(id, out _))
        return Results.BadRequest("uuid invalido.");

    var cachedPessoa = await cache.StringGetAsync(id);

    if (cachedPessoa.IsNull)
        return Results.NotFound();

    PessoaModel pessoa = JsonSerializer.Deserialize<PessoaModel>(cachedPessoa);

    return Results.Ok(pessoa);
});

app.MapGet("pessoas", (ConcurrentDictionary<string, PessoaModel> pessoaMap, string? t) =>
{

    if (string.IsNullOrEmpty(t))
        return Results.BadRequest("Termo de pesquisa não pode ser vazio.");

    PessoaModel[] pessoa = pessoaMap.Where(p => p.Key.Contains(t)).Take(50).Select(p => p.Value).ToArray();

    return Results.Ok(pessoa);
});

app.MapGet("/contagem-pessoas", async (NpgsqlConnection conn) =>
{
    await using (conn)
    {
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "select count(1) from pessoas";
        var count = await cmd.ExecuteScalarAsync();
        return count;
    }
});


app.Run();

