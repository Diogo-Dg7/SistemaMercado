# 🛒 Sistema de Supermercado

Sistema de gestão de supermercado desenvolvido com **C# (ASP.NET Core)** no backend e **React + TypeScript** no frontend,
aplicando **Programação Orientada a Objetos**, os princípios **SOLID** e **arquitetura em camadas**.

## Funcionalidades planejadas
- [ ] Cadastro de produtos e categorias
- [ ] Controle de estoque (entradas, saídas, alerta de estoque mínimo)
- [ ] Cadastro de clientes e fornecedores
- [ ] PDV: vendas com descontos e múltiplas formas de pagamento
- [x] Autenticação com JWT (perfis Operador e Gerente)
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

## Autenticação e autorização

A API usa **JWT** (válido por 8 horas, um turno). Quase todos os endpoints exigem token; só `/api/auth/login` e
`/health` (e a documentação, em `Development`) são anônimos.

### Configurar os segredos (obrigatório antes de rodar)
A chave de assinatura do token **é um segredo**: nunca vai no `appsettings.json`. A API **se recusa a subir** se
`Jwt:Chave` estiver ausente ou tiver menos de 32 caracteres. Em desenvolvimento use `dotnet user-secrets`:

```bash
cd src/Supermercado.API
dotnet user-secrets init   # só na primeira vez (o UserSecretsId já está no .csproj)

# chave aleatória (PowerShell):  [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
# chave aleatória (bash):        openssl rand -base64 48
dotnet user-secrets set "Jwt:Chave" "<chave-aleatoria-com-32-ou-mais-caracteres>"

# primeiro Gerente (criado na inicialização, só quando ainda não existe nenhum usuário)
dotnet user-secrets set "AdminInicial:Email" "admin@suaempresa.com"
dotnet user-secrets set "AdminInicial:Senha" "<senha-com-8-ou-mais-caracteres>"
```

`Jwt:Issuer` e `Jwt:Audience` não são segredos e ficam no `appsettings.json`. Em produção use variáveis de
ambiente (`Jwt__Chave`, `AdminInicial__Email`, `AdminInicial__Senha`). Se não houver usuários e o `AdminInicial`
não estiver configurado, a API sobe, mas registra um aviso e ninguém consegue fazer login.

### Fazer login no Scalar
1. Rode a API em `Development` e abra <https://localhost:7001/scalar>.
2. Execute `POST /api/auth/login` com o e-mail e a senha do `AdminInicial` e copie o campo `token` da resposta.
3. Clique em **Authentication** (ou no cadeado de qualquer endpoint), escolha **Bearer** e cole o token
   (sem o prefixo `Bearer `). Todas as chamadas seguintes já seguem autenticadas.
4. `GET /api/auth/me` confirma quem está logado.

### Quem pode o quê
| Perfil | Pode |
|---|---|
| **Gerente** | Tudo: criar categorias e produtos, entrada de estoque, criar e listar usuários, além do que o Operador faz |
| **Operador** | Consultas, saída de estoque e alteração de preço (não pode vender abaixo do custo: `422`) |

Sem token ou com token inválido/expirado a resposta é **401**; com token de um perfil sem permissão, **403**.
O login devolve sempre o mesmo **401** (`E-mail ou senha inválidos`) para e-mail inexistente, senha errada e usuário
inativo. Atenção: o token é validado sem consultar o banco, então desativar um usuário só vale para novos logins:
tokens já emitidos continuam válidos até expirar.

## API

Em `Development`, a documentação interativa (Scalar) fica em <https://localhost:7001/scalar> e o documento OpenAPI em
`/openapi/v1.json`. O CORS libera apenas o front em `http://localhost:5173` (Vite) e também só em `Development`.

| Método | Rota | Descrição |
|---|---|---|
| POST | `/api/auth/login` | Login (anônimo): devolve o token |
| GET | `/api/auth/me` | Usuário autenticado |
| POST | `/api/usuarios` | Cria usuário (**Gerente**) |
| GET | `/api/usuarios` | Lista usuários (**Gerente**) |
| POST | `/api/categorias` | Cria categoria (**Gerente**) |
| GET | `/api/categorias`, `/api/categorias/{id}` | Lista / obtém categorias |
| POST | `/api/produtos` | Cria produto (**Gerente**) |
| GET | `/api/produtos`, `/api/produtos/{id}` | Lista / obtém produtos |
| PUT | `/api/produtos/{id}/preco` | Atualiza custo e venda |
| POST | `/api/produtos/{id}/estoque/entrada` | Entrada de estoque (**Gerente**) |
| POST | `/api/produtos/{id}/estoque/saida` | Saída de estoque |
| GET | `/api/produtos/abaixo-estoque-minimo` | Alerta de reposição |

Erros sempre voltam como `application/problem+json`: **400** validação (com os erros por campo em `errors`),
**401** não autenticado ou credenciais inválidas, **403** sem permissão, **404** recurso inexistente, **409** estoque insuficiente ou conflito de concorrência (outra operação alterou o
mesmo registro: releia e tente de novo), **422** regra de negócio violada e **500** erro inesperado (sem detalhes
internos; o erro vai para o log).

> Instruções do frontend serão adicionadas quando ele for criado.

Detalhes completos da arquitetura e das convenções estão no [CLAUDE.md](./CLAUDE.md).
