namespace RinhaBachendV2;

public class PessoaModel
{

    public Guid? Id { get; set; }
    public string? Apelido { get; set; }
    public string? Nome { get; set; }
    public DateOnly? Nascimento { get; set; }
    public IEnumerable<string>? Stack { get; set; }

    public bool IsValid()
    {
        if (string.IsNullOrEmpty(Nome) || Nome.Length > 100 
            || string.IsNullOrEmpty(Apelido) || Apelido.Length > 32 
            || !Nascimento.HasValue)
            return false;

        foreach (string item in Stack ?? Enumerable.Empty<string>())
        {
            if (item.Length > 32 || item.Length == 0)
                return false;
        }

        return true;
    }
}