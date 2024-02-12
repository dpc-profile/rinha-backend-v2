using RinhaBachendV2;

using System.Collections.Concurrent;
using StackExchange.Redis;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

string dbString = builder.Configuration.GetConnectionString("DbConn");
builder.Services.AddNpgsqlDataSource(dbString ?? "ERRO de connection string!!!");

string cacheString = builder.Configuration.GetConnectionString("CacheConn");


// builder.Services.AddHostedService<InserePessoas>();

builder.Services.AddSingleton(_ => new ConcurrentDictionary<string, PessoaModel>());
builder.Services.AddSingleton(_ => new ConcurrentQueue<PessoaModel>());
builder.Services.AddSingleton<IConnectionMultiplexer>( _ => ConnectionMultiplexer.Connect(cacheString));

ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(cacheString);
builder.Services.AddSingleton<IDatabase>(_ => redis.GetDatabase());


var app = builder.Build();

app.MapPost("/pessoas", async (
    IDatabase cache, 
    ConcurrentQueue<PessoaModel> processingQueue, 
    PessoaModel pessoaModel) =>
{
    bool pessoaModelValid = pessoaModel.IsValid();
    
    // Fazer um try/catch com as mensagens mais exatas e ver no que dá
    if (pessoaModelValid == false)
        Results.UnprocessableEntity("Os dados da requisição estão invalidos");

    // apelido já cadastrado 422 - Unprocessable Entity/Content
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

    return Results.Created($"/pessoas/{pessoaModel.Id}", pessoaModel);
});


app.Run();

