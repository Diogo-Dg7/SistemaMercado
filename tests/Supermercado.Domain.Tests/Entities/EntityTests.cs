using Supermercado.Domain.Entities;
using Supermercado.Domain.Exceptions;

namespace Supermercado.Domain.Tests.Entities;

public class EntityTests
{
    private sealed class EntidadeA : Entity
    {
        public EntidadeA() { }
        public EntidadeA(Guid id) : base(id) { }
    }

    private sealed class EntidadeB : Entity
    {
        public EntidadeB(Guid id) : base(id) { }
    }

    [Fact]
    public void Construtor_deve_gerar_id_nao_vazio()
    {
        new EntidadeA().Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Construtor_deve_gerar_ids_diferentes_para_instancias_diferentes()
    {
        new EntidadeA().Id.Should().NotBe(new EntidadeA().Id);
    }

    [Fact]
    public void Construtor_deve_preencher_criado_em_com_data_utc_atual()
    {
        var antes = DateTime.UtcNow;
        var entidade = new EntidadeA();
        var depois = DateTime.UtcNow;

        entidade.CriadoEm.Should().BeOnOrAfter(antes).And.BeOnOrBefore(depois);
        entidade.CriadoEm.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Construtor_deve_aceitar_id_informado()
    {
        var id = Guid.NewGuid();

        new EntidadeA(id).Id.Should().Be(id);
    }

    [Fact]
    public void Construtor_deve_rejeitar_id_vazio()
    {
        var act = () => new EntidadeA(Guid.Empty);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Entidades_do_mesmo_tipo_e_mesmo_id_devem_ser_iguais()
    {
        var id = Guid.NewGuid();

        new EntidadeA(id).Should().Be(new EntidadeA(id));
        (new EntidadeA(id) == new EntidadeA(id)).Should().BeTrue();
        new EntidadeA(id).GetHashCode().Should().Be(new EntidadeA(id).GetHashCode());
    }

    [Fact]
    public void Entidades_com_ids_diferentes_nao_devem_ser_iguais()
    {
        (new EntidadeA() != new EntidadeA()).Should().BeTrue();
    }

    [Fact]
    public void Entidades_de_tipos_diferentes_com_mesmo_id_nao_devem_ser_iguais()
    {
        var id = Guid.NewGuid();

        new EntidadeA(id).Equals(new EntidadeB(id)).Should().BeFalse();
    }

    [Fact]
    public void Comparacao_com_nulo_deve_ser_falsa()
    {
        var entidade = new EntidadeA();

        entidade.Equals(null).Should().BeFalse();
        (entidade == null).Should().BeFalse();
        ((Entity?)null == null).Should().BeTrue();
    }
}
