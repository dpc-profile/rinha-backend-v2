namespace RinhaBachendV2;

public class PessoaModel
{

    public Guid? Id { get; set; }
    public string? Apelido { get; set; }
    public string? Nome { get; set; }
    public string? Nascimento { get; set; }
    public IEnumerable<string>? Stack { get; set; }

    public int Validacao()
    {
        if (string.IsNullOrEmpty(Nome) || Nome.Length > 100 
            || string.IsNullOrEmpty(Apelido) || Apelido.Length > 32 
            || string.IsNullOrEmpty(Nascimento))
            return 1;

        foreach (string item in Stack ?? Enumerable.Empty<string>())
        {
            if (item.Length > 32 || item.Length == 0)
                return 2;
        }

        return 0;
    }
}