# 💰 Controle de Gastos

Aplicação full-stack de controle e planejamento financeiro pessoal, construída do zero como projeto de estudo — do primeiro "Hello World" da API até um sistema completo com autenticação, banco de dados na nuvem, agente de IA para insights financeiros, e instalação como app (PWA).

**🔗 App em produção:** [gastos-web-ten.vercel.app](https://gastos-web-ten.vercel.app)
**🔗 API em produção:** [gastos-kpca.onrender.com](https://gastos-kpca.onrender.com)
**🔗 Repositório do frontend:** [github.com/Jeane-Cristina/gastos-web](https://github.com/Jeane-Cristina/gastos-web)

> ⚠️ A API roda no plano gratuito do Render, que "dorme" após 15 minutos sem uso. O primeiro acesso do dia pode levar de 30 a 60 segundos para responder — depois disso, funciona normalmente.

---

## 📌 Sobre o projeto

Este é um app de controle financeiro pessoal completo: cadastro de despesas, metas de economia mensal/anual, carteira de investimentos com sugestões de IA, insights semanais personalizados, e importação automática de extrato bancário. Mas o objetivo real do projeto nunca foi só "criar um app de finanças" — foi usar um problema real e útil no meu dia a dia como pretexto para praticar, de ponta a ponta, o que um desenvolvedor **full-stack júnior/pleno** e **front-end pleno/sênior** precisa dominar: desde modelagem de banco de dados até deploy em produção e integração com IA generativa, passando por testes automatizados, autenticação, e boas práticas de arquitetura em camadas.

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
| **Google Gemini API** | Geração de insights financeiros e sugestões de investimento via IA |
| **xUnit** | Testes automatizados |
| **Swagger / OpenAPI** | Documentação interativa da API |

### Frontend
| Tecnologia | Uso |
|---|---|
| **React + TypeScript** | Interface |
| **Vite** | Build tool |
| **Vitest + Testing Library** | Testes automatizados |
| **vite-plugin-pwa** | Instalação como app (PWA) |
| **Recharts** | Gráficos (pizza, radar, barras, linha) |
| **React Markdown** | Renderização dos insights gerados pela IA |
| **PapaParse** | Leitura de extratos bancários (CSV) |
| **jsPDF + html2canvas** | Exportação de relatórios em PDF |
| **Lucide React** | Ícones |
| **CSS puro com Design Tokens** | Identidade visual própria, com suporte a modo escuro |

### Infraestrutura
| Camada | Serviço |
|---|---|
| API | [Render](https://render.com) (free tier, via Docker) |
| Frontend | [Vercel](https://vercel.com) |
| Banco de dados | [Neon](https://neon.tech) (PostgreSQL serverless, free tier) |
| IA | [Google AI Studio](https://aistudio.google.com) (Gemini API, free tier) |

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
- **Services**: contêm a lógica real (criar, filtrar, atualizar, deletar, analisar), isolados de qualquer detalhe de HTTP — o que permite testá-los sem simular uma requisição inteira
- **DTOs**: separam o que a API expõe externamente do que existe no banco, com validação via Data Annotations
- **Interfaces** (`IExpenseService`) permitem injeção de dependência e testes desacoplados da implementação

Todo dado sensível do usuário (despesas, metas, investimentos) é isolado por `UserId`, extraído diretamente das claims do token JWT em cada requisição — nunca confiado a partir do que o cliente envia.

No frontend, a mesma filosofia de separação se repete: **services** (chamadas HTTP isoladas) → **hooks customizados** (estado e lógica reaproveitável) → **componentes** (só apresentação).

### Como o agente de IA funciona

O "agente" não é um modelo treinado do zero — é uma integração de **RAG simplificado**: o backend calcula estatísticas reais do histórico de despesas (gasto por semana do mês, categoria dominante por semana, comparação com metas), monta um prompt estruturado com esses números, e envia pra API do Gemini, que interpreta os dados e devolve uma recomendação em linguagem natural. A "inteligência" sobre os hábitos financeiros vem do cálculo determinístico em C#/LINQ; o papel do modelo é só interpretar e comunicar.

---

## ✨ Funcionalidades

### Núcleo
- 🔐 **Autenticação completa**: cadastro e login com JWT, senha protegida por hash (BCrypt)
- 📝 **CRUD completo de despesas**: criar, listar, editar (inline, direto no card) e excluir, com validação de dados no backend
- 🔍 **Filtros combináveis**: por mês, categoria e semana do mês, via query parameters na API
- 📥 **Importação de extrato bancário**: upload de CSV, com sugestão automática de categoria baseada no histórico do próprio usuário (reconhecimento por correspondência de texto, sem depender de IA para essa etapa)

### Planejamento financeiro
- 🎯 **Metas de economia** mensal e anual, com acompanhamento visual (radar de progresso e indicador verde/âmbar/vermelho)
- 🛍️ **Lista de compras** (desejos vs necessidades), usada como contexto para as recomendações da IA
- 📈 **Carteira de investimentos**: registro de aplicações com sugestões educacionais de melhoria geradas por IA (sem recomendação de produto específico — só princípios gerais de diversificação)
- 📊 **Histórico de metas mês a mês**, com gráfico de evolução

### Insights e visualização
- 🤖 **Insight semanal personalizado via IA**: análise de padrões de gasto por semana, pontos de economia, e estratégia para conciliar compras desejadas com as metas — respeitando limite de geração (1x por semana) para controle de custo de API
- 📊 **Gráficos** (Recharts): distribuição de gastos por categoria (pizza), comparação com o mês anterior (barras), evolução de metas (linha)
- 🌓 **Modo escuro**, com preferência persistida
- 📄 **Exportação de relatório em PDF**

### Infraestrutura e qualidade
- 📱 **PWA instalável**: funciona como app nativo em Android e iOS, sem passar por loja de aplicativos
- ✅ **Testado nos dois lados**: testes automatizados no frontend (Vitest) e backend (xUnit), cobrindo lógica pura, componentes e camada de Service — incluindo testes específicos de isolamento de dado entre usuários
- ☁️ **Deploy real em produção**: API e frontend publicados, banco de dados gerenciado na nuvem, variáveis sensíveis fora do código-fonte

---

## 🚀 Rodando localmente

### Pré-requisitos
- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- Uma chave gratuita da [Gemini API](https://aistudio.google.com/apikey) (opcional, só necessária para os insights de IA)

### 1. Banco de dados (PostgreSQL via Docker)

```bash
docker run --name gastos-postgres -e POSTGRES_PASSWORD=suasenha -e POSTGRES_DB=gastosdb -p 5432:5432 -d postgres:16
```

### 2. Backend

```bash
cd GastosApi

# Configure a connection string, a chave JWT e a chave da Gemini como variáveis de ambiente
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=gastosdb;Username=postgres;Password=suasenha"
$env:Jwt__Key = "sua-chave-secreta-de-pelo-menos-32-caracteres"
$env:Gemini__ApiKey = "sua-chave-da-gemini-api"

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
├── Controllers/       # Endpoints REST (Expenses, Auth, FinancialProfile, Goal, Investment, Insight)
├── Services/           # Lógica de negócio (Expense, Analytics, Insight, InvestmentAdvisor, CategorySuggestion, Goal)
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

Este projeto foi construído em sessões curtas de estudo (2-3h), evoluindo incrementalmente ao longo de semanas. Documentar essa jornada é, pra mim, tão importante quanto o código em si — é a prova de progresso real, não só o resultado final:

1. **Fundamentos**: setup do projeto, modelagem inicial, conexão com banco de dados
2. **CRUD completo**: endpoints REST, camada de Service, DTOs com validação (Data Annotations)
3. **Frontend consumindo API real**: hooks customizados, tratamento de loading/erro, componentização
4. **Autenticação**: JWT, hash de senha, cadastro multiusuário, proteção de rotas
5. **Testes automatizados**: Vitest no frontend, xUnit no backend — mesma lógica (Arrange/Act/Assert), sintaxes diferentes
6. **Identidade visual**: sistema de design próprio, não genérico, construído com CSS puro e tokens
7. **Filtros e agregações**: query parameters, LINQ com `GroupBy`
8. **Infraestrutura de produção**: Docker para desenvolvimento local, migração de SQLite para PostgreSQL, PWA, deploy real com variáveis de ambiente protegidas
9. **Correção de isolamento de dado**: identificação e correção de uma falha de segurança em que despesas não eram vinculadas ao usuário dono — inclui teste automatizado específico para provar que a falha não volta a acontecer
10. **Agente de IA**: integração com Gemini API via RAG simplificado, geração de insights financeiros a partir de dados reais do usuário, com controle de custo via cache semanal
11. **Planejamento financeiro completo**: metas mensais/anuais, lista de compras, carteira de investimentos com sugestões educacionais de IA
12. **Visualização de dados**: gráficos de distribuição, comparação temporal e evolução histórica, além de modo escuro e exportação em PDF
13. **Importação de extrato bancário**: parsing de CSV com tratamento de formatos numéricos variados (separador de milhar/decimal, valores negativos) e categorização automática baseada em histórico

### Principais desafios técnicos superados

- Migração de motor de banco de dados (SQLite → PostgreSQL) sem quebrar testes existentes, graças à separação em camadas
- Identificação e correção de uma falha real de isolamento de dado entre usuários, incluindo migração de schema com preservação de dado de produção (estratégia nullable → backfill → obrigatório)
- Configuração de CORS entre múltiplos ambientes (localhost, rede local, produção)
- Resolução de conflitos de merge e configuração de Git em ambiente com múltiplas identidades (pessoal/trabalho)
- Debugging de autenticação JWT ponta a ponta, incluindo geração, validação e expiração de token
- Parsing robusto de dados financeiros externos (extratos bancários) com formatos inconsistentes de número e presença de créditos/estornos misturados a despesas
- Diagnóstico de falhas silenciosas de injeção de dependência em produção, via instrumentação de log

---

## 👩‍💻 Autora

**Jeane Cristina**
[GitHub](https://github.com/Jeane-Cristina)
