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
- Entity Framework Core
- xUnit, FluentAssertions, NSubstitute
- React, TypeScript, Vite

## Como executar
Pré-requisito: [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet build
dotnet test
dotnet run --project src/Supermercado.API
# teste rápido: GET https://localhost:7001/health
```

> Instruções do frontend serão adicionadas quando ele for criado.

Detalhes completos da arquitetura e das convenções estão no [CLAUDE.md](./CLAUDE.md).
