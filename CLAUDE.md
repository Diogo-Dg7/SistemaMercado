# Sistema de Supermercado: guia do projeto

## Visão geral
Sistema de gestão de supermercado: cadastro de produtos e categorias, controle de estoque,
clientes, fornecedores e PDV (vendas com descontos e várias formas de pagamento).

- **Backend:** C# / ASP.NET Core Web API (.NET 10), arquitetura em camadas (Clean Architecture)
- **Banco:** SQL Server via Entity Framework Core
- **Frontend:** React + TypeScript + Vite
- **Testes:** xUnit + FluentAssertions + NSubstitute (backend), Vitest + Testing Library (frontend)

## Estrutura de pastas
```
Supermercado/
├── CLAUDE.md
├── Supermercado.sln
├── src/
│   ├── Supermercado.Domain/            # Regras de negócio puras (sem dependências externas)
│   │   ├── Entities/                   # Produto, Categoria, Cliente, Fornecedor, Venda, ItemVenda, Usuario
│   │   ├── ValueObjects/               # Dinheiro, CodigoBarras, Cpf, Email
│   │   ├── Enums/                      # StatusVenda, UnidadeMedida, PerfilUsuario
│   │   ├── Pagamentos/                 # FormaPagamento (abstrata), PagamentoDinheiro, PagamentoCartaoCredito, PagamentoCartaoDebito, PagamentoPix
│   │   ├── Descontos/                  # IDescontoStrategy + implementações
│   │   ├── Interfaces/                 # Contratos de repositório (IProdutoRepository, IUnitOfWork...)
│   │   └── Exceptions/                 # DomainException, EstoqueInsuficienteException
│   │
│   ├── Supermercado.Application/       # Casos de uso; depende só do Domain
│   │   ├── DTOs/                       # ProdutoDto, CriarProdutoRequest, VendaDto...
│   │   ├── Interfaces/                 # IProdutoService, IVendaService, IEstoqueService
│   │   ├── Services/                   # Implementações dos casos de uso
│   │   ├── Validators/                 # FluentValidation
│   │   ├── Mappings/                   # Mapeamento Entity <-> DTO
│   │   └── DependencyInjection.cs      # AddApplication()
│   │
│   ├── Supermercado.Infrastructure/    # Detalhes técnicos; implementa interfaces do Domain
│   │   ├── Data/                       # SupermercadoDbContext, Configurations/ (Fluent API), Migrations/
│   │   ├── Repositories/               # ProdutoRepository, VendaRepository, UnitOfWork
│   │   ├── Services/                   # JwtTokenService, PasswordHasher
│   │   └── DependencyInjection.cs      # AddInfrastructure(configuration)
│   │
│   └── Supermercado.API/               # Porta de entrada HTTP
│       ├── Controllers/                # ProdutosController, VendasController...
│       ├── Middlewares/                # ExceptionHandlingMiddleware
│       ├── Program.cs
│       └── appsettings.json
│
├── tests/
│   ├── Supermercado.Domain.Tests/
│   ├── Supermercado.Application.Tests/
│   └── Supermercado.API.IntegrationTests/
│
└── frontend/
    └── supermercado-web/
        └── src/
            ├── api/                    # Cliente HTTP (axios) + um arquivo por recurso
            ├── components/             # Componentes reutilizáveis (Button, Table, Modal)
            ├── features/               # Uma pasta por módulo: produtos/, estoque/, vendas/, clientes/
            │   └── produtos/
            │       ├── components/
            │       ├── hooks/          # useProdutos, useCriarProduto
            │       ├── pages/
            │       └── types.ts
            ├── layouts/
            ├── routes/
            ├── contexts/               # AuthContext, CarrinhoContext
            └── utils/
```

## Regra de dependência (obrigatória)
```
API ──► Application ──► Domain
 │                         ▲
 └──► Infrastructure ──────┘
```
- `Domain` não referencia nenhum outro projeto nem pacote de infraestrutura (nada de EF Core aqui).
- `Application` referencia só `Domain`.
- `Infrastructure` referencia `Domain` e `Application`.
- `API` referencia `Application` e `Infrastructure` (apenas para registrar a injeção de dependência).

## Princípios SOLID aplicados
- **S (responsabilidade única):** cada service cuida de um caso de uso (`VendaService` não mexe em estoque diretamente; usa `IEstoqueService`). Controllers só recebem a requisição e delegam.
- **O (aberto/fechado):** descontos via `IDescontoStrategy` (`DescontoPercentual`, `DescontoLeveXPagueY`, `DescontoClienteFidelidade`). Um desconto novo é uma classe nova, sem alterar `Venda`.
- **L (substituição de Liskov):** `FormaPagamento` é abstrata; `PagamentoDinheiro`, `PagamentoCartaoCredito`, `PagamentoCartaoDebito` e `PagamentoPix` podem ser usadas em qualquer lugar que espere `FormaPagamento`, sem `if (tipo == ...)`.
- **I (segregação de interfaces):** repositórios divididos em `IReadRepository<T>` e `IWriteRepository<T>`; quem só lê depende só da leitura.
- **D (inversão de dependência):** services dependem de interfaces (`IProdutoRepository`, `IUnitOfWork`), nunca de classes concretas. Tudo registrado no contêiner de injeção de dependência.

## Regras de POO
- Entidades com **setters privados**; o estado muda só por métodos com regra (`produto.AtualizarPreco(...)`, `venda.AdicionarItem(...)`).
- Validar invariantes no construtor e nos métodos da entidade (lançar `DomainException`).
- Usar **value objects** imutáveis (`record`) para Dinheiro, CPF, código de barras.
- Coleções expostas como `IReadOnlyCollection<T>`.
- Classe base `Entity` com `Id` (Guid) e `CriadoEm`.

## Regras de negócio principais
- Venda não pode ser finalizada sem itens nem com estoque insuficiente.
- Finalizar venda baixa o estoque e registra `MovimentacaoEstoque` (entrada/saída/ajuste).
- Preço de venda não pode ser menor que o preço de custo (salvo perfil Gerente).
- Produto com estoque abaixo do mínimo aparece no alerta de reposição.
- Cancelamento de venda devolve os itens ao estoque.

## Convenções de código
- Nomes de domínio em português (Produto, Venda), sufixos técnicos em inglês (Repository, Service, Controller, Dto).
- `async/await` em todo I/O, com `CancellationToken`.
- Nunca retornar entidades no controller; sempre DTOs.
- Erros de domínio tratados no `ExceptionHandlingMiddleware`, retornando `ProblemDetails`.
- Frontend: componentes funcionais, hooks, TypeScript estrito, sem `any`.

## Comandos
```bash
dotnet build
dotnet test
dotnet ef migrations add <Nome> -p src/Supermercado.Infrastructure -s src/Supermercado.API -o Data/Migrations
dotnet ef database update -p src/Supermercado.Infrastructure -s src/Supermercado.API
dotnet run --project src/Supermercado.API

cd frontend/supermercado-web && npm install && npm run dev
```

## Forma de trabalhar
- Implementar um módulo por vez, de dentro para fora: Domain → testes do Domain → Application → Infrastructure → API → Frontend.
- Escrever testes unitários para toda regra de negócio nova.
- Rodar `dotnet build` e `dotnet test` antes de considerar uma tarefa concluída.
