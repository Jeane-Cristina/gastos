# 💰 Controle de Gastos

Aplicação full-stack de controle e planejamento financeiro pessoal: API REST em C#/ASP.NET Core com autenticação JWT, banco PostgreSQL, testes automatizados e insights gerados por IA a partir das estatísticas reais de cada usuário (Gemini API). Instalável como app (PWA).

**🔗 App em produção:** [gastos-web-ten.vercel.app](https://gastos-web-ten.vercel.app)
**🔗 API em produção:** [gastos-kpca.onrender.com](https://gastos-kpca.onrender.com)
**🔗 Repositório do frontend:** [github.com/Jeane-Cristina/gastos-web](https://github.com/Jeane-Cristina/gastos-web)

> ⚠️ A API roda no plano gratuito do Render, que "dorme" após 15 minutos sem uso. O primeiro acesso pode levar de 30 a 60 segundos para responder.

---

## 📌 Sobre o projeto

Projeto pessoal que nasceu de uma necessidade real: acompanhar meus próprios gastos e metas de economia. Reúne cadastro de despesas, metas mensais e anuais, lista de desejos de compra, carteira de investimentos, contas conjuntas, importação de extrato bancário em CSV e insights de IA por período.

Foi também um espaço para exercitar, de ponta a ponta, decisões de engenharia: modelagem de dados, arquitetura em camadas, autenticação, isolamento de dados por usuário, testes, deploy e integração com um modelo de linguagem.

---

## 🛠️ Stack técnica

### Backend
| Tecnologia | Uso |
|---|---|
| **C# / ASP.NET Core** | API REST |
| **Entity Framework Core** | ORM e migrations |
| **PostgreSQL** ([Neon](https://neon.tech) em produção) | Banco de dados |
| **Docker** | PostgreSQL local e imagem de deploy |
| **JWT + BCrypt.Net** | Autenticação e hash de senha |
| **Rate limiting + IMemoryCache** | Proteção de endpoints e cache de insights |
| **Google Gemini API** | Redação dos insights financeiros |
| **xUnit + SQLite em memória** | Testes automatizados |
| **Swagger / OpenAPI** | Documentação interativa |

### Frontend
React + TypeScript + Vite, Vitest + Testing Library, vite-plugin-pwa, Recharts, PapaParse, React Markdown. Detalhes no [README do frontend](https://github.com/Jeane-Cristina/gastos-web).

### Infraestrutura
| Camada | Serviço |
|---|---|
| API | [Render](https://render.com) (Docker, plano gratuito) |
| Frontend | [Vercel](https://vercel.com) |
| Banco | [Neon](https://neon.tech) (PostgreSQL serverless) |
| IA | [Google AI Studio](https://aistudio.google.com) (Gemini API) |

---

## 🏗️ Arquitetura

```
Controller  →  Service  →  DbContext (EF Core)  →  PostgreSQL
   ↑              ↑
  DTOs      Regra de negócio
(validação)   (isolada, testável)
```

- **Controllers** traduzem HTTP em chamada de Service e resposta HTTP, sem regra de negócio.
- **Services** concentram a lógica (criar, filtrar, paginar, resumir, analisar) e não conhecem HTTP, o que permite testá-los diretamente.
- **DTOs** separam o que a API expõe do que existe no banco, com validação por Data Annotations.
- **Injeção de dependência** (`AddScoped`, `AddHttpClient<T>`, `AddMemoryCache`) e interfaces como `IExpenseService` mantêm as camadas desacopladas.
- **Isolamento por usuário:** o `UserId` vem das claims do token JWT em cada requisição, nunca do corpo enviado pelo cliente. `[Authorize]` garante quem é o usuário; cada consulta filtra por `UserId` para garantir de quem é o dado.
- **Desempenho:** paginação na listagem e índice composto `(UserId, Date)` para as consultas mais frequentes.

### Como os insights de IA funcionam

Os insights são **gerados por IA a partir de estatísticas reais do usuário**. O fluxo:

1. O backend calcula os números em C#/LINQ: gasto por semana, categoria dominante, comparação com metas e com o período anterior.
2. Monta um prompt estruturado com esses números, o perfil financeiro e a lista de compras desejadas.
3. Envia para o modelo (`gemini-flash-latest`), que interpreta os dados e redige a recomendação em linguagem natural.

A parte analítica é determinística e testável; o modelo só interpreta e comunica. O usuário escolhe o período (semanal, mensal ou trimestral) e o resultado fica em cache por 7 dias para controlar custo e latência.

Limitação conhecida: a API do Gemini às vezes responde `503` (indisponibilidade temporária). Uma política de retry com backoff exponencial está nos próximos passos.

### Princípios de engajamento ético

Antes de construir o lançamento rápido e a sequência de dias (streak), fiz uma pesquisa sobre engajamento em apps de hábito e finanças pessoais. As decisões de produto que saíram dela:

- **Reforço positivo, sem punição:** a sequência celebra constância e não pune a pausa; falhar um dia não deve gerar culpa nem "perder tudo".
- **Controle do usuário:** o usuário decide o que registrar, quando e com que frequência; nada é feito para prendê-lo ao app.
- **Honestidade na comunicação:** os insights apontam padrões e opções, sem alarmismo nem linguagem de vergonha sobre gastos.
- **Utilidade antes de retenção:** o objetivo é dar clareza financeira, não maximizar tempo de tela.

---

## ✨ Funcionalidades

### Núcleo
- 🔐 **Autenticação:** cadastro e login com JWT; senha com hash BCrypt
- 📝 **CRUD de despesas** com validação, edição inline, campo "quem pagou" e status de pago
- 🔍 **Filtros combináveis** por mês, ano, categoria, semana do mês, quem pagou e status de pagamento, com paginação
- ⚡ **Lançamento rápido** e sequência de dias com registro
- 🔁 **Despesas recorrentes** geradas automaticamente
- 📥 **Importação de extrato (CSV):** sugestão de categoria baseada no histórico do próprio usuário, detecção de possíveis duplicatas e prévia editável antes de confirmar

### Planejamento financeiro
- 🎯 **Metas** de economia mensal e anual, com orçamento por categoria
- 🛍️ **Lista de compras desejadas**, usada como contexto nos insights
- 📈 **Carteira de investimentos** com sugestões educacionais (princípios gerais de diversificação, sem indicar produto específico)
- 📊 **Histórico de metas** mês a mês
- 👥 **Contas conjuntas:** convite por nome de usuário com aceite, despesas compartilhadas, contribuição por membro, metas do grupo, sair e remover membro

### Insights e visualização
- 🤖 **Insights de IA por período** (semanal, mensal, trimestral)
- 📊 **Gráficos** de distribuição por categoria e evolução de metas
- 🌓 **Modo escuro**
- 📱 **PWA instalável** em Android e iOS

### Qualidade
- ✅ Testes automatizados nos dois lados (xUnit e Vitest), incluindo testes que provam o isolamento de dados entre usuários
- ☁️ Deploy real: API, frontend e banco gerenciado, com segredos fora do código-fonte

---

## 🚀 Rodando localmente

### Pré-requisitos
- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- Chave gratuita da [Gemini API](https://aistudio.google.com/apikey) (opcional; só para os insights)

### 1. Banco de dados

```bash
docker run --name gastos-postgres -e POSTGRES_PASSWORD=suasenha -e POSTGRES_DB=gastosdb -p 5432:5432 -d postgres:16
```

### 2. Backend

```powershell
git clone https://github.com/Jeane-Cristina/gastos
cd gastos

$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=gastosdb;Username=postgres;Password=suasenha"
$env:Jwt__Key = "uma-chave-secreta-de-pelo-menos-32-caracteres"
$env:Gemini__ApiKey = "sua-chave-da-gemini-api"

dotnet ef database update --project GastosApi.csproj
dotnet run --project GastosApi.csproj
```

A API sobe em `http://localhost:5134`, com Swagger em `/swagger`.

### 3. Frontend

Veja o [README do frontend](https://github.com/Jeane-Cristina/gastos-web).

---

## 🧪 Testes

```bash
cd GastosApi.Tests
dotnet test
```

Os testes de Service usam SQLite em memória, sem depender de servidor de banco. Cobrem criação com normalização de categoria, filtros, paginação, resumo por categoria e isolamento entre usuários.

---

## 📂 Estrutura do repositório

```
.
├── Controllers/        # Endpoints REST
├── Services/           # Regra de negócio (despesas, análises, insights, metas, contas conjuntas)
├── Models/             # Entidades do banco
├── Dtos/               # Objetos de transporte com validação
├── Data/               # DbContext (EF Core)
├── Migrations/         # Histórico de schema
├── GastosApi.Tests/    # Testes xUnit
└── Dockerfile          # Imagem de deploy (Render)
```

---

## 🧭 Evolução do projeto

1. **Base:** modelagem, CRUD, camada de Service e DTOs com validação
2. **Autenticação e isolamento:** JWT, cadastro multiusuário, vínculo de cada despesa ao seu dono, com teste automatizado cobrindo o caso
3. **Produção:** PostgreSQL com Docker no desenvolvimento, Neon em produção, deploy no Render e na Vercel, PWA
4. **Planejamento:** metas, lista de compras, investimentos e histórico
5. **IA:** insights por período a partir de estatísticas calculadas no backend
6. **Importação e produtividade:** CSV com detecção de duplicatas, lançamento rápido, recorrentes
7. **Colaboração:** contas conjuntas com convite e aceite

### Desafios técnicos resolvidos

- Falha de isolamento de dados entre usuários, corrigida com migração nullable → backfill → obrigatório, preservando dados de produção
- Migração de SQLite para PostgreSQL sem quebrar os testes, graças à separação em camadas
- Diferença de `DateTimeKind` entre .NET e PostgreSQL (`timestamp with time zone`)
- Migrations aplicadas separadamente no banco local e no Neon
- CORS entre ambientes (localhost, rede local, produção)
- Parsing de extratos com formatos numéricos variados e créditos misturados a despesas

---

## 🔮 Próximos passos

- Retry com backoff exponencial nas chamadas ao Gemini (indisponibilidades `503` intermitentes)
- Mover o filtro por semana do mês para a consulta SQL (hoje é aplicado em memória após a paginação, o que pode divergir o total da página)
- Registro de despesas por WhatsApp: webhook implementado e verificado; aguardando número dedicado para uso em produção
- Uso do refresh token no frontend
- Recuperação de senha por e-mail
- Funcionamento offline completo via Service Worker

---

## 👩‍💻 Autora

**Jeane Cristina Oliveira Martins** — Desenvolvedora Full Stack (C#/.NET, React/TypeScript)
[LinkedIn](https://linkedin.com/in/jeane-cristina-dev) · [GitHub](https://github.com/Jeane-Cristina)
