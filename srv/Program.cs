using RinhaBachendV2;

var builder = WebApplication.CreateBuilder(args);

string dbString = builder.Configuration.GetConnectionString("DbConn");
builder.Services.AddNpgsqlDataSource(dbString ?? "ERRO de connection string!!!");

builder.Services.AddHostedService<InserePessoas>();

var app = builder.Build();

app.Run();

