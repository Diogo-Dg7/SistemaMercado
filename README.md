# 🛒 Sistema de Supermercado

Sistema de gestão de supermercado desenvolvido com **C# (ASP.NET Core)** no backend e **React + TypeScript** no frontend,
aplicando **Programação Orientada a Objetos**, os princípios **SOLID** e **arquitetura em camadas**.

## Funcionalidades planejadas
- [ ] Cadastro de produtos e categorias
- [ ] Controle de estoque (entradas, saídas, alerta de estoque mínimo)
- [ ] Cadastro de clientes e fornecedores
- [ ] PDV: vendas com descontos e múltiplas formas de pagamento
- [ ] Autenticação com JWT (perfis Operador e Gerente)
- [ ] Dashboard de vendas

## Arquitetura

```
API ──► Application ──► Domain
 │                         ▲
 └──► Infrastructure ──────┘
```

| Camada | Responsabilidade |
|---|---|
| **Domain** | Entidades, regras de negócio e contratos. Não depende de nada. |
| **Application** | Casos de uso, DTOs e validações. |
| **Infrastructure** | Banco de dados (EF Core), repositórios e serviços técnicos. |
| **API** | Controllers e configuração de injeção de dependência. |
| **Frontend** | React organizado por funcionalidades (`features/`). |

## SOLID no projeto
| Princípio | Onde aparece |
|---|---|
| **S**: Responsabilidade Única | Cada service cuida de um caso de uso; controllers apenas delegam |
| **O**: Aberto/Fechado | Descontos via `IDescontoStrategy`: nova promoção = nova classe |
| **L**: Substituição de Liskov | `FormaPagamento` abstrata com `PagamentoPix`, `PagamentoDinheiro`, `PagamentoCartaoCredito`... |
| **I**: Segregação de Interfaces | Repositórios separados em leitura (`IReadRepository`) e escrita (`IWriteRepository`) |
| **D**: Inversão de Dependência | Services dependem de interfaces; implementações injetadas pela API |

## Tecnologias
- .NET 10 / ASP.NET Core Web API
- Entity Framework Core + SQL Server
- xUnit, FluentAssertions, NSubstitute
- React, TypeScript, Vite

## Banco de dados

O sistema usa **SQL Server** via Entity Framework Core. A connection string é lida da chave
`ConnectionStrings:Supermercado`.

### 1. Configurar a connection string
Em [`src/Supermercado.API/appsettings.json`](./src/Supermercado.API/appsettings.json) ajuste o `Server=` para a
sua instância (por exemplo `.\SQLEXPRESS` ou `(localdb)\MSSQLLocalDB`). O padrão usa a autenticação do Windows:

```
Server=SUA_INSTANCIA;Database=Supermercado;Trusted_Connection=True;TrustServerCertificate=True
```

> **Usa login e senha do SQL Server?** Nunca coloque credenciais no `appsettings.json` (ele vai para o Git).
> Guarde a connection string completa com `dotnet user-secrets`, que fica fora do repositório:
>
> ```bash
> cd src/Supermercado.API
> dotnet user-secrets init
> dotnet user-secrets set "ConnectionStrings:Supermercado" "Server=SUA_INSTANCIA;Database=Supermercado;User Id=SEU_USUARIO;Password=SUA_SENHA;TrustServerCertificate=True"
> ```
>
> Os user-secrets têm precedência sobre o `appsettings.json` e são carregados no ambiente `Development`.

### 2. Aplicar a migration
A ferramenta `dotnet-ef` está versionada em `dotnet-tools.json`, então basta restaurá-la:

```bash
dotnet tool restore
dotnet ef database update -p src/Supermercado.Infrastructure -s src/Supermercado.API
```

Isso cria o banco `Supermercado` (se não existir) e todas as tabelas. Para criar uma nova migration depois de
alterar o domínio:

```bash
dotnet ef migrations add <Nome> -p src/Supermercado.Infrastructure -s src/Supermercado.API -o Data/Migrations
```

### 3. Testes de integração
Os testes de `tests/Supermercado.API.IntegrationTests` usam o SQL Server local, mas em um banco **separado**,
`Supermercado_Testes`: ele é recriado (`EnsureDeleted` + `Migrate`) no início da execução e apagado no final, então
o banco `Supermercado` nunca é tocado. Ajuste o `Server=` em
[`appsettings.Testes.json`](./tests/Supermercado.API.IntegrationTests/appsettings.Testes.json) ou use a variável de
ambiente `ConnectionStrings__SupermercadoTestes`. Por segurança, os testes se recusam a rodar se a connection string
não apontar para o banco `Supermercado_Testes`.

## Como executar
Pré-requisitos: [.NET 10 SDK](https://dotnet.microsoft.com/download) e uma instância do SQL Server (a edição Express serve).

```bash
dotnet build
dotnet test
dotnet run --project src/Supermercado.API
# teste rápido: GET https://localhost:7001/health
```

> Instruções do frontend serão adicionadas quando ele for criado.

Detalhes completos da arquitetura e das convenções estão no [CLAUDE.md](./CLAUDE.md).
