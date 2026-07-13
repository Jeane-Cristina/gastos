# 💰 Controle de Gastos

Aplicação full-stack de controle financeiro pessoal, construída do zero como projeto de estudo — do primeiro "Hello World" da API até deploy em produção, com autenticação real, banco de dados na nuvem e instalação como app (PWA).

**🔗 App em produção:** [gastos-web-ten.vercel.app](https://gastos-web-ten.vercel.app)
**🔗 API em produção:** [gastos-kpca.onrender.com](https://gastos-kpca.onrender.com)
**🔗 Repositório do frontend:** [github.com/Jeane-Cristina/gastos-web](https://github.com/Jeane-Cristina/gastos-web)

> ⚠️ A API roda no plano gratuito do Render, que "dorme" após 15 minutos sem uso. O primeiro acesso do dia pode levar de 30 a 60 segundos para responder — depois disso, funciona normalmente.

---

## 📌 Sobre o projeto

Este é um app de controle de gastos pessoais: cadastro de despesas, categorização, filtros por mês/categoria e resumo consolidado. Mas o objetivo real do projeto não foi só "criar um app de gastos" — foi usar um problema real e útil no meu dia a dia como pretexto para praticar, de ponta a ponta, o que um desenvolvedor **full-stack júnior/pleno** e **front-end pleno/sênior** precisa dominar: desde modelagem de banco de dados até deploy em produção, passando por testes automatizados, autenticação, e boas práticas de arquitetura em camadas.

Cada etapa deste projeto foi construída de forma incremental e documentada — no fim deste README, você encontra o passo a passo completo da jornada.

---

## 🛠️ Stack Técnica

### Backend
| Tecnologia | Uso |
|---|---|
| **C# / ASP.NET Core** | API REST |
| **Entity Framework Core** | ORM, migrations |
| **PostgreSQL** (via [Neon](https://neon.tech)) | Banco de dados de produção |
| **Docker** | PostgreSQL local para desenvolvimento |
| **JWT (JSON Web Token)** | Autenticação e autorização |
| **BCrypt.Net** | Hash de senha |
| **xUnit** | Testes automatizados |
| **Swagger / OpenAPI** | Documentação interativa da API |

### Frontend
| Tecnologia | Uso |
|---|---|
| **React + TypeScript** | Interface |
| **Vite** | Build tool |
| **Vitest + Testing Library** | Testes automatizados |
| **vite-plugin-pwa** | Instalação como app (PWA) |
| **CSS puro com Design Tokens** | Identidade visual própria |

### Infraestrutura
| Camada | Serviço |
|---|---|
| API | [Render](https://render.com) (free tier, via Docker) |
| Frontend | [Vercel](https://vercel.com) |
| Banco de dados | [Neon](https://neon.tech) (PostgreSQL serverless, free tier) |

---

## 🏗️ Arquitetura

O backend segue separação em camadas, um padrão comum em aplicações .NET profissionais:

```
Controller  →  Service  →  DbContext (EF Core)  →  PostgreSQL
   ↑              ↑
  DTOs      Regra de negócio
(validação)   (isolada, testável)
```

- **Controllers**: só traduzem requisição HTTP em chamada de Service e resposta HTTP — sem lógica de negócio
- **Services**: contêm a lógica real (criar, filtrar, atualizar, deletar), isolados de qualquer detalhe de HTTP — o que permite testá-los sem simular uma requisição inteira
- **DTOs**: separam o que a API expõe externamente do que existe no banco, com validação via Data Annotations
- **Interfaces** (`IExpenseService`) permitem injeção de dependência e testes desacoplados da implementação

No frontend, a mesma filosofia de separação se repete: **services** (chamadas HTTP isoladas) → **hooks customizados** (estado e lógica reaproveitável) → **componentes** (só apresentação).

---

## ✨ Funcionalidades

- 🔐 **Autenticação completa**: cadastro e login com JWT, senha protegida por hash (BCrypt)
- 📝 **CRUD completo de despesas**: criar, listar, editar e excluir, com validação de dados no backend
- 🔍 **Filtros**: por mês e por categoria, via query parameters na API
- 📊 **Resumo por categoria**: totais agregados calculados no backend (`GROUP BY` via LINQ)
- 🎨 **Identidade visual própria**: sistema de design com tokens CSS, tipografia e paleta autorais, inspirado em recibo/nota fiscal
- 📱 **PWA instalável**: funciona como app nativo em Android e iOS, sem passar por loja de aplicativos
- ✅ **Testado nos dois lados**: 11 testes no frontend (Vitest), 10 testes no backend (xUnit), cobrindo lógica pura, componentes e camada de Service
- ☁️ **Deploy real em produção**: API e frontend publicados, banco de dados gerenciado na nuvem, variáveis sensíveis fora do código-fonte

---

## 🚀 Rodando localmente

### Pré-requisitos
- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### 1. Banco de dados (PostgreSQL via Docker)

```bash
docker run --name gastos-postgres -e POSTGRES_PASSWORD=suasenha -e POSTGRES_DB=gastosdb -p 5432:5432 -d postgres:16
```

### 2. Backend

```bash
cd GastosApi

# Configure a connection string e a chave JWT como variáveis de ambiente
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=gastosdb;Username=postgres;Password=suasenha"
$env:Jwt__Key = "sua-chave-secreta-de-pelo-menos-32-caracteres"

dotnet ef database update
dotnet run
```

A API sobe em `http://localhost:5134`, com Swagger disponível em `/swagger`.

### 3. Frontend

```bash
cd gastos-web
npm install
npm run dev
```

O app abre em `http://localhost:5173`.

---

## 🧪 Rodando os testes

**Backend:**
```bash
cd GastosApi.Tests
dotnet test
```

**Frontend:**
```bash
cd gastos-web
npm run test
```

---

## 📂 Estrutura do repositório (backend)

```
GastosApi/
├── Controllers/       # Endpoints REST
├── Services/           # Lógica de negócio (interface + implementação)
├── Models/              # Entidades do banco de dados
├── Dtos/                # Objetos de transporte, com validação
├── Data/                 # DbContext (EF Core)
├── Migrations/            # Histórico de mudanças de schema
└── Dockerfile               # Imagem usada no deploy (Render)

GastosApi.Tests/
└── ExpenseServiceTests.cs, ExpenseDtoValidationTests.cs
```

---

## 🗺️ Jornada de aprendizado (o "porquê" deste projeto)

Este projeto foi construído em sessões curtas de estudo (2-3h), evoluindo incrementalmente. Documentar essa jornada é, pra mim, tão importante quanto o código em si — é a prova de progresso real, não só o resultado final:

1. **Fundamentos**: setup do projeto, modelagem inicial, conexão com banco de dados
2. **CRUD completo**: endpoints REST, camada de Service, DTOs com validação (Data Annotations)
3. **Frontend consumindo API real**: hooks customizados, tratamento de loading/erro, componentização
4. **Autenticação**: JWT, hash de senha, cadastro multiusuário, proteção de rotas
5. **Testes automatizados**: Vitest no frontend, xUnit no backend — mesma lógica (Arrange/Act/Assert), sintaxes diferentes
6. **Identidade visual**: sistema de design próprio, não genérico, construído com CSS puro e tokens
7. **Filtros e agregações**: query parameters, LINQ com `GroupBy`
8. **Infraestrutura de produção**: Docker para desenvolvimento local, migração de SQLite para PostgreSQL, PWA, deploy real com variáveis de ambiente protegidas

### Principais desafios técnicos superados
- Migração de motor de banco de dados (SQLite → PostgreSQL) sem quebrar testes existentes, graças à separação em camadas
- Configuração de CORS entre múltiplos ambientes (localhost, rede local, produção)
- Resolução de conflitos de merge e configuração de Git em ambiente com múltiplas identidades (pessoal/trabalho)
- Debugging de autenticação JWT ponta a ponta, incluindo geração, validação e expiração de token

---

## 🔮 Próximos passos

- Agente de IA para insights financeiros personalizados, baseado em histórico de gastos do usuário
- Integração com WhatsApp como canal alternativo de lançamento de despesas
- Funcionamento offline completo (cache de dados via Service Worker)

---

## 👩‍💻 Autora

**Jeane Cristina**
[GitHub](https://github.com/Jeane-Cristina)
