namespace euSindico.Api.Tests;

// RunAsync aplica migrations do MySQL (Pomelo), que não rodam no banco em memória dos testes — a execução
// real é validada subindo o docker-compose de produção (ver deploy/README.md). Aqui fica só a decisão de
// quando entrar no modo "só migrations", que é o que impede a API de subir sem querer nesse modo (ou vice-versa).
public class MigrationCommandTests
{
    [Theory]
    [InlineData("--migrate")]
    [InlineData("--MIGRATE")]
    public void Entra_no_modo_migrations_com_o_argumento(string argumento)
    {
        Assert.True(MigrationCommand.IsRequested([argumento]));
    }

    [Fact]
    public void Entra_no_modo_migrations_mesmo_com_outros_argumentos()
    {
        Assert.True(MigrationCommand.IsRequested(["--urls", "http://+:8080", "--migrate"]));
    }

    [Theory]
    [InlineData]
    [InlineData("--urls", "http://+:8080")]
    [InlineData("migrate")]
    public void Sem_o_argumento_sobe_a_api_normalmente(params string[] argumentos)
    {
        Assert.False(MigrationCommand.IsRequested(argumentos));
    }
}
