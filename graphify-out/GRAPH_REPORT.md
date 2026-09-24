# Graph Report - mesh  (2026-09-14)

## Corpus Check
- cluster-only mode — file stats not available

## Summary
- 2127 nodes · 2724 edges · 136 communities (119 shown, 13 thin omitted)
- Extraction: 99% EXTRACTED · 1% INFERRED · 0% AMBIGUOUS · INFERRED: 20 edges (avg confidence: 0.81)
- Token cost: 6,622 input · 1,499 output

## Graph Freshness
- Built from commit: `0fb2a8c9`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Notification System
- Request Management Module
- Quotation Dialog
- Logging Infrastructure
- Invoice Comparison Logic
- Quotation Repository
- User Profile Components
- People Management Module
- Product Management Module
- User Management Module
- Financial Cost Centers
- System Logs Module
- Quotations List Module
- Comparative Map View
- Invoice Management Module
- Request Entry Dialog
- Person Details Dialog
- Invoice Import Dialog
- Product Details Dialog
- User Permissions Dialog
- Dashboard Charts
- Approval Workflow Module
- System Log Panel
- Invoice Data Model
- Public Quotation Portal
- Identity and Authorization
- Cost Center Dialog
- Email Service
- Database Context
- Product Mapping Dialog
- Identity Role Management
- Project Configuration
- Global UI Imports
- Workflow Steps Module
- Timeline Visuals
- User Data Model
- Person Data Model
- Kanban Board Module
- Supplier Invitation Model
- Workflow Step Dialog
- Product Data Model
- Request Status Visuals
- Top Navigation Bar
- Quotation Winner Model
- Request Repository
- Comparison Dialog
- Cost Center Model
- Request Item Model
- Purchase Request Model
- Quotation Status Model
- Quotation Header Model
- System Log Model
- Device Management Dialog
- Action Visual Types
- Environment Settings
- Cost Center Repository
- Invoice Repository
- Permission Policy Handlers
- Shell Layout
- Initial Database Migration
- Proposal Item Mapping
- User Repository
- Invoice Item Model
- System Log Repository
- Workflow Step Repository
- Product Repository
- Product Schema Migration
- Purchase Request Migration
- Quotations Schema Migration
- Winner Selection Migration
- Logs Schema Migration
- Notification Logs Migration
- Invoices Schema Migration
- Audit Log Model
- Authentication State Provider
- System Event Visuals
- Home Page
- User Status Migration
- Database Schema Update
- Person CRUD Migration
- Kanban Schema Migration
- Supplier Mapping Migration
- Person Type Enumeration
- Tags Schema Migration
- People and Cities Migration
- Hierarchical Cost Center Migration
- Workflow Repository
- Audit Log Repository
- Person Helper Services
- Company Data Model
- Project Dependencies
- User Tags Migration
- User Status Update Migration
- Theme Settings Migration
- Supplier Product Model
- Person Repository
- Supplier Product Repository
- City Data Model
- XML Invoice Reader
- Module Permission Discovery
- Login Page and Components
- Theme Toggle and Tooltips
- Database Seeding and Identity
- Routing and Authorization
- Alert and Error Components
- Database Context and Logging
- Company Repository
- City Repository
- City Data Seeding
- Layout and UI Providers
- Application Root Configuration
- Model Builder Configuration
- Status Item Definitions
- Product Data Management
- Service Dependency Injection
- Procurement Approval Workflow
- Error Handling Page
- Localization and Translation
- Theme Management Scripts
- Connection State Scripts
- Login Redirection Logic
- Action Area Component
- Action Bar Component
- Statistics Card Component
- Product Data Models
- Navigation Menu Component
- Reports Module UI
- Cost Center Repository
- Invoice Reader Service
- Brand Logo Assets
- Brand Logo Assets
- Brand Logo Assets

## God Nodes (most connected - your core abstractions)
1. `AppDbContext` - 65 edges
2. `Erp.Migrations` - 39 edges
3. `NotaFiscal` - 38 edges
4. `Usuario` - 38 edges
5. `Erp.Data` - 38 edges
6. `Produto` - 36 edges
7. `Pessoa` - 35 edges
8. `CotacaoRepository` - 34 edges
9. `ConviteFornecedor` - 33 edges
10. `SolicitacaoCompra` - 31 edges

## Surprising Connections (you probably didn't know these)
- `AppDbContext` --references--> `Notificacao`  [EXTRACTED]
  Data/AppDbContext.cs → Model/Notificacao/Notificacao.cs
- `NotificacaoRepository` --references--> `AppDbContext`  [EXTRACTED]
  Repository/Notificacao/NotificacaoRepository.cs → Data/AppDbContext.cs
- `EmpresaRepository` --references--> `AppDbContext`  [EXTRACTED]
  Repository/Empresa/EmpresaRepository.cs → Data/AppDbContext.cs
- `CidadeRepository` --references--> `AppDbContext`  [EXTRACTED]
  Repository/Cidades/CidadeRepository.cs → Data/AppDbContext.cs
- `AppDbContext` --references--> `NotaFiscal`  [EXTRACTED]
  Data/AppDbContext.cs → Model/Fiscal/NotaFiscal.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Procurement Process Flow** — escopo_ponto_pedido, escopo_pessoa, escopo_aprovacao, escopo_fluxo_compras [EXTRACTED 0.95]

## Communities (136 total, 13 thin omitted)

### Community 0 - "Notification System"
Cohesion: 0.06
Nodes (34): Abrir, MarcarTodasLidas, OnInitializedAsync, Quando, AuthenticationState, BbDropdownMenu, BbDropdownMenuContent, BbDropdownMenuTrigger (+26 more)

### Community 1 - "Request Management Module"
Cohesion: 0.05
Nodes (37): Abrir, AbrirLinhaDoTempo, AlternarBusca, Centros, ConfirmarExclusao, FecharBuscaSeVazia, NovaSolicitacao, OnAfterRenderAsync (+29 more)

### Community 2 - "Quotation Dialog"
Cohesion: 0.05
Nodes (36): CarregarFormulario, Copiar, Descartar, EnviarEmail, ImportarAprovados, Link, OnInitializedAsync, OnParametersSetAsync (+28 more)

### Community 3 - "Logging Infrastructure"
Cohesion: 0.08
Nodes (24): AsyncLocal, ConcurrentDictionary, Erp.Service.Log, EventId, IDisposable, IHttpClientFactory, ILoggerProvider, IPAddress (+16 more)

### Community 4 - "Invoice Comparison Logic"
Cohesion: 0.08
Nodes (25): ConfrontoVisual, Erp.Components.Modules.Notas, Erp.Service.Fiscal, IReadOnlyCollection, IReadOnlyDictionary, List, ConfrontoNfe, LinhaConfronto (+17 more)

### Community 5 - "Quotation Repository"
Cohesion: 0.12
Nodes (13): Cotacao, ConviteFornecedor, DateTime, Expression, Func, Guid, IDbContextFactory, List (+5 more)

### Community 6 - "User Profile Components"
Cohesion: 0.06
Nodes (33): BbFieldContent, BbFormFieldMaskedInput, BbRichTextEditor, BbTabs, BbTabsContent, BbTabsList, BbTabsTrigger, BbTagInput (+25 more)

### Community 7 - "People Management Module"
Cohesion: 0.06
Nodes (33): AlternarBusca, ConfirmarExclusao, Editar, FecharBuscaSeVazia, NovoCadastro, OnAfterRenderAsync, OnInitializedAsync, PedirExclusao (+25 more)

### Community 8 - "Product Management Module"
Cohesion: 0.06
Nodes (33): AlternarBusca, ConfirmarExclusao, Editar, FecharBuscaSeVazia, NovoProduto, OnAfterRenderAsync, OnInitializedAsync, PedirExclusao (+25 more)

### Community 9 - "User Management Module"
Cohesion: 0.06
Nodes (33): AlternarBusca, ConfirmarExclusao, Editar, FecharBuscaSeVazia, NovoUsuario, OnAfterRenderAsync, OnInitializedAsync, PedirExclusao (+25 more)

### Community 10 - "Financial Cost Centers"
Cohesion: 0.06
Nodes (32): BbDataGridHierarchyColumn, CentroCustoDialog, AlternarBusca, ConfirmarExclusao, Editar, FecharBuscaSeVazia, NovoCentro, OnAfterRenderAsync (+24 more)

### Community 11 - "System Logs Module"
Cohesion: 0.06
Nodes (32): AbrirStackTrace, AlternarBusca, FecharBuscaSeVazia, OnAfterRenderAsync, OnInitializedAsync, ActionArea, AuthenticationState, BbButton (+24 more)

### Community 12 - "Quotations List Module"
Cohesion: 0.06
Nodes (31): Abrir, AbrirMapa, Badge, ConfirmarExclusao, Encerrar, NovaCotacao, OnInitializedAsync, PedirExclusao (+23 more)

### Community 13 - "Comparative Map View"
Cohesion: 0.06
Nodes (30): Anunciar, Carregar, ConfirmarMotivo, Escolher, Gravar, Limpar, OnInitializedAsync, OnParametersSetAsync (+22 more)

### Community 14 - "Invoice Management Module"
Cohesion: 0.06
Nodes (30): AbrirConfronto, ConfirmarExclusao, OnInitializedAsync, PedirExclusao, ActionArea, AuthenticationState, BbButton, BbDataGrid (+22 more)

### Community 15 - "Request Entry Dialog"
Cohesion: 0.07
Nodes (28): AdicionarLinha, CarregarFormulario, Descartar, OnInitializedAsync, OnParametersSet, ActionArea, AuthenticationState, BbBadge (+20 more)

### Community 16 - "Person Details Dialog"
Cohesion: 0.07
Nodes (27): BbDatePicker, BbSelectContent, BbSelectItem, BbSelectTrigger, BbSelectValue, CarregarFormulario, Descartar, OnInitializedAsync (+19 more)

### Community 17 - "Invoice Import Dialog"
Cohesion: 0.07
Nodes (27): Amarrado, Amarrar, Carregar, Fechar, Importar, Limpar, OnParametersSet, ActionArea (+19 more)

### Community 18 - "Product Details Dialog"
Cohesion: 0.07
Nodes (27): CarregarFormulario, Descartar, Formatar, Numero, OnInitializedAsync, OnParametersSet, ActionArea, BbButton (+19 more)

### Community 19 - "User Permissions Dialog"
Cohesion: 0.07
Nodes (27): Alternar, AtualizarHerdadas, CarregarFormulario, Concedidas, Descartar, OnInitializedAsync, OnParametersSetAsync, ActionArea (+19 more)

### Community 20 - "Dashboard Charts"
Cohesion: 0.07
Nodes (26): BarraCentro, BarraMes, BbBar, BbBarChart, BbChartLegend, BbChartTooltip, BbPie, BbPieChart (+18 more)

### Community 21 - "Approval Workflow Module"
Cohesion: 0.07
Nodes (26): Aprovar, ConfirmarMotivo, Decidir, ItensMeus, OnInitializedAsync, PedirMotivo, ActionArea, AuthenticationState (+18 more)

### Community 22 - "System Log Panel"
Cohesion: 0.08
Nodes (25): Abrir, OnInitializedAsync, BbButton, BbDataGrid, BbDataGridPropertyColumn, BbDataGridTemplateColumn, BbDialog, BbDialogContent (+17 more)

### Community 23 - "Invoice Data Model"
Cohesion: 0.08
Nodes (26): DateTime, Guid, List, NotaFiscal, Ambiente, Chave, Cotacao, CotacaoId (+18 more)

### Community 24 - "Public Quotation Portal"
Cohesion: 0.08
Nodes (24): EhFaseDaNota, Enviar, EnviarNota, ItemVencido, MontarItensVencidos, OnInitializedAsync, Preencher, BbButton (+16 more)

### Community 25 - "Identity and Authorization"
Cohesion: 0.12
Nodes (13): AuthorizeAttribute, BlazorBlueprint.Components, Erp.Service.Acesso, Erp.Model.Acesso, Erp.Repository.PermissoesUsuario, Erp.Localization, Erp.Model.Usuario, Microsoft.AspNetCore.Authorization (+5 more)

### Community 26 - "Cost Center Dialog"
Cohesion: 0.08
Nodes (23): CarregarFormulario, CarregarPaisDisponiveis, Descartar, OnInitializedAsync, OnParametersSetAsync, ActionArea, BbButton, BbCombobox (+15 more)

### Community 27 - "Email Service"
Cohesion: 0.13
Nodes (18): Erp.Service.Email, Comprador, Comprador, DateTime, ILogger, Task, Comprador, EmailOptions (+10 more)

### Community 28 - "Database Context"
Cohesion: 0.08
Nodes (24): Guid, AppDbContext, CentrosCusto, Cidades, Convites, CotacaoItens, Cotacoes, Empresas (+16 more)

### Community 29 - "Product Mapping Dialog"
Cohesion: 0.09
Nodes (22): Adicionar, Carregar, Excluir, Listar, OnParametersSetAsync, ActionArea, BbButton, BbCombobox (+14 more)

### Community 30 - "Identity Role Management"
Cohesion: 0.14
Nodes (16): Diretas, IdentityRole, IList, Guid, Papel, Descricao, Dictionary, Guid (+8 more)

### Community 31 - "Project Configuration"
Cohesion: 0.09
Nodes (22): author, bugs, url, description, devDependencies, tailwindcss, @tailwindcss/cli, homepage (+14 more)

### Community 32 - "Global UI Imports"
Cohesion: 0.09
Nodes (21): BlazorBlueprint.Icons.Lucide.Components, BlazorBlueprint.Icons.Lucide.Data, BlazorBlueprint.Primitives.Services, Microsoft.AspNetCore.Components.Authorization, NavigationManager, ToastService, DialogService, global::Erp (+13 more)

### Community 33 - "Workflow Steps Module"
Cohesion: 0.09
Nodes (21): ConfirmarExclusao, Editar, Mover, NovaEtapa, OnInitializedAsync, PedirExclusao, ActionArea, AuthenticationState (+13 more)

### Community 34 - "Timeline Visuals"
Cohesion: 0.10
Nodes (16): Carregar, Evento, OnParametersSetAsync, ActionArea, BbButton, BbDialog, BbDialogContent, BbDialogFooter (+8 more)

### Community 35 - "User Data Model"
Cohesion: 0.09
Nodes (20): Erp.Repository.Usuario, IdentityUser, StatusUsuario, Ativo, Inativo, DateTime, Guid, List (+12 more)

### Community 36 - "Person Data Model"
Cohesion: 0.09
Nodes (22): Cidade, DateTime, Pessoa, Bairro, CEP, Cidade, CidadeId, CNPJ (+14 more)

### Community 37 - "Kanban Board Module"
Cohesion: 0.10
Nodes (20): Abrir, Cartao, CentrosDeCusto, DaEtapa, Enviar, NovaSolicitacao, OnInitializedAsync, ActionArea (+12 more)

### Community 38 - "Supplier Invitation Model"
Cohesion: 0.10
Nodes (21): DateTime, List, ConviteFornecedor, Cnpj, CondicaoPagamento, CotacaoId, CriadoEm, Email (+13 more)

### Community 39 - "Workflow Step Dialog"
Cohesion: 0.10
Nodes (19): CarregarFormulario, Descartar, OnParametersSet, ActionArea, BbButton, BbDialog, BbDialogContent, BbDialogFooter (+11 more)

### Community 40 - "Product Data Model"
Cohesion: 0.10
Nodes (20): DateTime, Produto, Ativo, CentroCustoPadrao, CentroCustoPadraoId, Codigo, ConsumoMedioDiario, CriadoEm (+12 more)

### Community 41 - "Request Status Visuals"
Cohesion: 0.12
Nodes (12): BadgeVariant, StatusVisual, Erp.Repository.Solicitacao, Erp.Model.Solicitacao, Erp.Components.Modules.Solicitacoes, StatusSolicitacao, Aprovada, AprovadaParcialmente (+4 more)

### Community 42 - "Top Navigation Bar"
Cohesion: 0.11
Nodes (18): BbDropdownMenuItem, BbDropdownMenuSeparator, OnPerfil, BbButton, BbDropdownMenu, BbDropdownMenuContent, BbDropdownMenuTrigger, BbTooltip (+10 more)

### Community 43 - "Quotation Winner Model"
Cohesion: 0.11
Nodes (19): DateTime, Guid, List, CotacaoItem, ConviteVencedor, ConviteVencedorId, Cotacao, CotacaoId (+11 more)

### Community 44 - "Request Repository"
Cohesion: 0.18
Nodes (8): Expression, Func, Guid, IDbContextFactory, IEnumerable, List, Task, SolicitacaoRepository

### Community 45 - "Comparison Dialog"
Cohesion: 0.12
Nodes (14): Carregar, OnParametersSetAsync, ActionArea, BbButton, BbDialog, BbDialogContent, BbDialogFooter, BbDialogTitle (+6 more)

### Community 46 - "Cost Center Model"
Cohesion: 0.11
Nodes (17): Erp.Model.CentroCusto, ICollection, DateTime, Guid, Usuario, CentroCusto, Ativo, Codigo (+9 more)

### Community 47 - "Request Item Model"
Cohesion: 0.11
Nodes (18): DateTime, Guid, ItemSolicitacao, CentroCusto, CentroCustoId, DecididoEm, DecididoPor, DecididoPorId (+10 more)

### Community 48 - "Purchase Request Model"
Cohesion: 0.11
Nodes (18): DateTime, Guid, List, SolicitacaoCompra, CriadoEm, Editavel, EnviadaEm, Etapa (+10 more)

### Community 49 - "Quotation Status Model"
Cohesion: 0.12
Nodes (11): Erp.Model.Cotacao, StatusConvite, Cancelado, NotaEnviada, Pendente, Respondido, Vencedor, StatusCotacao (+3 more)

### Community 50 - "Quotation Header Model"
Cohesion: 0.12
Nodes (17): DateTime, Guid, List, Cotacao, Convites, CriadoEm, CriadoPor, CriadoPorId (+9 more)

### Community 51 - "System Log Model"
Cohesion: 0.12
Nodes (17): DateTime, Guid, LogSistema, Detalhes, Dispositivo, Evento, Id, Identificacao (+9 more)

### Community 52 - "Device Management Dialog"
Cohesion: 0.12
Nodes (15): BbDialogDescription, BbDialogHeader, Carregar, OnParametersSetAsync, AuthenticationState, BbButton, BbDialog, BbDialogContent (+7 more)

### Community 53 - "Action Visual Types"
Cohesion: 0.14
Nodes (13): AcaoVisual, TipoAcao, Acesso, Alteracao, Aprovacao, Criacao, Devolucao, Envio (+5 more)

### Community 54 - "Environment Settings"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 55 - "Cost Center Repository"
Cohesion: 0.20
Nodes (8): Expression, Func, Guid, HashSet, IDbContextFactory, List, Task, CentroCustoRepository

### Community 56 - "Invoice Repository"
Cohesion: 0.23
Nodes (6): Expression, Func, IDbContextFactory, List, Task, NotaFiscalRepository

### Community 57 - "Permission Policy Handlers"
Cohesion: 0.14
Nodes (13): AuthorizationHandler, AuthorizationHandlerContext, AuthorizationOptions, AuthorizationPolicy, DefaultAuthorizationPolicyProvider, IAuthorizationRequirement, IOptions, Task (+5 more)

### Community 58 - "Shell Layout"
Cohesion: 0.13
Nodes (14): Logout, Navegar, OnAfterRenderAsync, OnInitializedAsync, OnParametersSet, AuthenticationState, IJSRuntime, LayoutComponentBase (+6 more)

### Community 59 - "Initial Database Migration"
Cohesion: 0.15
Nodes (10): Erp.Migrations, DateTime, DateTimeOffset, Guid, MigrationBuilder, DateTime, DateTimeOffset, Guid (+2 more)

### Community 60 - "Proposal Item Mapping"
Cohesion: 0.13
Nodes (13): ModelBuilder, PropostaItem, ConviteFornecedor, ConviteFornecedorId, CotacaoItem, CotacaoItemId, Id, Observacao (+5 more)

### Community 61 - "User Repository"
Cohesion: 0.23
Nodes (9): IdentityResult, Expression, Func, Guid, IDbContextFactory, List, Task, UserManager (+1 more)

### Community 62 - "Invoice Item Model"
Cohesion: 0.13
Nodes (15): NotaFiscalItem, Cfop, CodigoFornecedor, Descricao, Id, Ncm, NotaFiscal, NotaFiscalId (+7 more)

### Community 63 - "System Log Repository"
Cohesion: 0.20
Nodes (10): Exception, Expression, Func, Guid, IDbContextFactory, ILogger, List, LogSistema (+2 more)

### Community 64 - "Workflow Step Repository"
Cohesion: 0.14
Nodes (12): Erp.Repository.Etapa, Erp.Model.Etapa, DateTime, Etapa, Ativo, Cor, CriadoEm, Descricao (+4 more)

### Community 65 - "Product Repository"
Cohesion: 0.21
Nodes (7): Erp.Repository.Produto, Expression, Func, IDbContextFactory, List, Task, ProdutoRepository

### Community 66 - "Product Schema Migration"
Cohesion: 0.15
Nodes (9): DateTime, Guid, MigrationBuilder, DateTime, DateTimeOffset, Guid, List, ModelBuilder (+1 more)

### Community 67 - "Purchase Request Migration"
Cohesion: 0.15
Nodes (9): DateTime, Guid, MigrationBuilder, DateTime, DateTimeOffset, Guid, List, ModelBuilder (+1 more)

### Community 68 - "Quotations Schema Migration"
Cohesion: 0.15
Nodes (9): DateTime, Guid, MigrationBuilder, DateTime, DateTimeOffset, Guid, List, ModelBuilder (+1 more)

### Community 69 - "Winner Selection Migration"
Cohesion: 0.15
Nodes (9): DateTime, Guid, MigrationBuilder, DateTime, DateTimeOffset, Guid, List, ModelBuilder (+1 more)

### Community 70 - "Logs Schema Migration"
Cohesion: 0.15
Nodes (9): DateTime, Guid, MigrationBuilder, DateTime, DateTimeOffset, Guid, List, ModelBuilder (+1 more)

### Community 71 - "Notification Logs Migration"
Cohesion: 0.15
Nodes (9): DateTime, Guid, MigrationBuilder, DateTime, DateTimeOffset, Guid, List, ModelBuilder (+1 more)

### Community 72 - "Invoices Schema Migration"
Cohesion: 0.15
Nodes (9): DateTime, Guid, MigrationBuilder, DateTime, DateTimeOffset, Guid, List, ModelBuilder (+1 more)

### Community 73 - "Audit Log Model"
Cohesion: 0.14
Nodes (14): DateTime, Guid, RegistroLog, Acao, Descricao, Entidade, EntidadeId, Id (+6 more)

### Community 74 - "Authentication State Provider"
Cohesion: 0.19
Nodes (11): CancellationToken, IdentityOptions, IServiceScopeFactory, RevalidatingServerAuthenticationStateProvider, AuthenticationState, ClaimsPrincipal, Task, UserManager (+3 more)

### Community 75 - "System Event Visuals"
Cohesion: 0.18
Nodes (9): EventoVisual, TipoEventoSistema, AcessoNegado, Excecao, Inicializacao, Login, LoginFalho, Logout (+1 more)

### Community 76 - "Home Page"
Cohesion: 0.15
Nodes (12): OnParametersSetAsync, AuthenticationState, BbButton, BbEmpty, ChildContent, Icon, LucideIcon, NavigationManager (+4 more)

### Community 77 - "User Status Migration"
Cohesion: 0.18
Nodes (8): Migration, DateTime, MigrationBuilder, DateTime, DateTimeOffset, Guid, ModelBuilder, AddAtivoToUser

### Community 78 - "Database Schema Update"
Cohesion: 0.17
Nodes (8): DateTime, MigrationBuilder, DateTime, DateTimeOffset, Guid, List, ModelBuilder, ad

### Community 79 - "Person CRUD Migration"
Cohesion: 0.17
Nodes (8): DateTime, MigrationBuilder, DateTime, DateTimeOffset, Guid, List, ModelBuilder, AjustesPessoaCrud

### Community 80 - "Kanban Schema Migration"
Cohesion: 0.17
Nodes (8): DateTime, MigrationBuilder, DateTime, DateTimeOffset, Guid, List, ModelBuilder, EtapasEKanban

### Community 81 - "Supplier Mapping Migration"
Cohesion: 0.17
Nodes (8): DateTime, MigrationBuilder, DateTime, DateTimeOffset, Guid, List, ModelBuilder, DeParaProdutoFornecedor

### Community 82 - "Person Type Enumeration"
Cohesion: 0.15
Nodes (13): TipoPessoa, Cliente, Colaborador, Consultor, Distribuidor, Fornecedor, Indeterminado, Lead (+5 more)

### Community 83 - "Tags Schema Migration"
Cohesion: 0.18
Nodes (7): MigrationBuilder, DateTime, DateTimeOffset, Guid, List, ModelBuilder, TagsArray

### Community 84 - "People and Cities Migration"
Cohesion: 0.18
Nodes (7): MigrationBuilder, DateTime, DateTimeOffset, Guid, List, ModelBuilder, CreatePessoasAndCidades

### Community 85 - "Hierarchical Cost Center Migration"
Cohesion: 0.18
Nodes (7): MigrationBuilder, DateTime, DateTimeOffset, Guid, List, ModelBuilder, CentroCustoHierarquico

### Community 86 - "Workflow Repository"
Cohesion: 0.27
Nodes (6): Expression, Func, IDbContextFactory, List, Task, EtapaRepository

### Community 87 - "Audit Log Repository"
Cohesion: 0.21
Nodes (9): Exception, Expression, Func, Guid, IDbContextFactory, ILogger, List, Task (+1 more)

### Community 88 - "Person Helper Services"
Cohesion: 0.18
Nodes (7): BlazorBlueprint.Primitives, Erp.Model.Pessoa, Erp.Repository.Pessoa, Erp.Helpers.Pessoa, List, PessoaHelper, SelectOption

### Community 89 - "Company Data Model"
Cohesion: 0.18
Nodes (10): Erp.Model.Empresa, DateTime, Guid, Empresa, CNPJ, Cor, CriadoEm, Id (+2 more)

### Community 90 - "Project Dependencies"
Cohesion: 0.18
Nodes (11): Erp, net10.0, BlazorBlueprint.Components (3.15.0), BlazorBlueprint.Icons.Lucide (2.0.2), BlazorBlueprint.Primitives (3.15.0), Microsoft.AspNetCore.Identity.EntityFrameworkCore (10.0.0), Microsoft.EntityFrameworkCore.Design (10.0.0), Microsoft.EntityFrameworkCore.Tools (10.0.0) (+3 more)

### Community 91 - "User Tags Migration"
Cohesion: 0.20
Nodes (6): MigrationBuilder, DateTime, DateTimeOffset, Guid, ModelBuilder, AddTagToUser

### Community 92 - "User Status Update Migration"
Cohesion: 0.20
Nodes (6): MigrationBuilder, DateTime, DateTimeOffset, Guid, ModelBuilder, AlterStatusUser

### Community 93 - "Theme Settings Migration"
Cohesion: 0.20
Nodes (6): MigrationBuilder, DateTime, DateTimeOffset, Guid, ModelBuilder, AddTema

### Community 94 - "Supplier Product Model"
Cohesion: 0.18
Nodes (11): DateTime, ProdutoFornecedor, CodigoFornecedor, CriadoEm, DescricaoFornecedor, Fornecedor, FornecedorId, Id (+3 more)

### Community 95 - "Person Repository"
Cohesion: 0.25
Nodes (6): Expression, Func, IDbContextFactory, List, Task, PessoaRepository

### Community 96 - "Supplier Product Repository"
Cohesion: 0.24
Nodes (6): Dictionary, IDbContextFactory, List, ProdutoFornecedor, Task, ProdutoFornecedorRepository

### Community 97 - "City Data Model"
Cohesion: 0.20
Nodes (8): Erp.Model.Cidades, Cidade, CodigoIbge, CodigoPais, Descricao, EstadoString, Id, PaisString

### Community 98 - "XML Invoice Reader"
Cohesion: 0.36
Nodes (5): DateTime, NotaFiscal, LeitorNfe, XElement, XNamespace

### Community 99 - "Module Permission Discovery"
Cohesion: 0.33
Nodes (6): ClaimsPrincipal, IReadOnlyList, Modulo, Modulos, Erp.Components.Shell, Type

### Community 100 - "Login Page and Components"
Cohesion: 0.25
Nodes (7): AntiforgeryToken, BbLabel, BbButton, BbInput, PageTitle, route:/, route:/login

### Community 101 - "Theme Toggle and Tooltips"
Cohesion: 0.25
Nodes (7): Alternar, OnAfterRenderAsync, BbTooltip, BbTooltipContent, BbTooltipTrigger, IJSRuntime, LucideIcon

### Community 102 - "Database Seeding and Identity"
Cohesion: 0.32
Nodes (5): IServiceProvider, RoleManager, Task, UserManager, DbSeeder

### Community 103 - "Routing and Authorization"
Cohesion: 0.29
Nodes (6): AuthorizeRouteView, Microsoft.AspNetCore.Components.Authorization, Found, NotAuthorized, RedirectToLogin, Router

### Community 104 - "Alert and Error Components"
Cohesion: 0.29
Nodes (6): BbAlert, BbAlertDescription, BbAlertTitle, ChildContent, Icon, LucideIcon

### Community 105 - "Database Context and Logging"
Cohesion: 0.33
Nodes (4): Erp.Data, Erp.Repository.Log, AppDbContextModelSnapshot, ModelSnapshot

### Community 106 - "Company Repository"
Cohesion: 0.29
Nodes (5): Erp.Repository.Empresa, IDbContextFactory, List, Task, EmpresaRepository

### Community 107 - "City Repository"
Cohesion: 0.29
Nodes (5): Erp.Repository.Cidades, IDbContextFactory, List, Task, CidadeRepository

### Community 108 - "City Data Seeding"
Cohesion: 0.33
Nodes (4): Cidade, List, Task, CidadeSeeder

### Community 109 - "Layout and UI Providers"
Cohesion: 0.33
Nodes (5): BbContainerPortalHost, BbDialogProvider, BbOverlayPortalHost, BbToastProvider, LayoutComponentBase

### Community 110 - "Application Root Configuration"
Cohesion: 0.33
Nodes (5): HeadOutlet, ImportMap, ReconnectModal, ResourcePreloader, Routes

### Community 111 - "Model Builder Configuration"
Cohesion: 0.33
Nodes (5): DateTime, DateTimeOffset, Guid, List, ModelBuilder

### Community 112 - "Status Item Definitions"
Cohesion: 0.33
Nodes (5): StatusItem, Aprovado, Devolvido, Pendente, Recusado

### Community 113 - "Product Data Management"
Cohesion: 0.40
Nodes (4): Codigo, Descricao, ProdutoId, IEnumerable

### Community 114 - "Service Dependency Injection"
Cohesion: 0.40
Nodes (3): Erp.Service, IServiceCollection, RegistroServicos

### Community 115 - "Procurement Approval Workflow"
Cohesion: 0.50
Nodes (5): Aprovação por Centro de Custo, Fluxo de Compras, Pessoa (Fornecedor), Ponto de Pedido, MESH - ESCOPO E SITUACAO

### Community 116 - "Error Handling Page"
Cohesion: 0.50
Nodes (3): OnInitialized, PageTitle, System.Diagnostics

### Community 117 - "Localization and Translation"
Cohesion: 0.50
Nodes (3): DefaultBbLocalizer, Dictionary, BbLocalizationPtBr

### Community 118 - "Theme Management Scripts"
Cohesion: 0.83
Nodes (3): alternar(), aplicar(), definir()

## Knowledge Gaps
- **1223 isolated node(s):** `Abrir`, `MarcarTodasLidas`, `OnInitializedAsync`, `Quando`, `AuthenticationState` (+1218 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 1547 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **13 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `AppDbContext` connect `Database Context` to `Notification System`, `Quotation Repository`, `Invoice Data Model`, `Identity and Authorization`, `Identity Role Management`, `User Data Model`, `Person Data Model`, `Supplier Invitation Model`, `Product Data Model`, `Quotation Winner Model`, `Request Repository`, `Cost Center Model`, `Request Item Model`, `Purchase Request Model`, `System Log Model`, `Cost Center Repository`, `Invoice Repository`, `Proposal Item Mapping`, `User Repository`, `Invoice Item Model`, `System Log Repository`, `Workflow Step Repository`, `Product Repository`, `Audit Log Model`, `Workflow Repository`, `Audit Log Repository`, `Company Data Model`, `Supplier Product Model`, `Person Repository`, `Supplier Product Repository`, `City Data Model`, `Database Seeding and Identity`, `Company Repository`, `City Repository`, `City Data Seeding`?**
  _High betweenness centrality (0.359) - this node is a cross-community bridge._
- **Why does `Erp.Data` connect `Database Context and Logging` to `Notification System`, `Quotation Repository`, `Identity and Authorization`, `User Data Model`, `Request Status Visuals`, `Comparison Dialog`, `Initial Database Migration`, `Workflow Step Repository`, `Product Repository`, `Product Schema Migration`, `Purchase Request Migration`, `Quotations Schema Migration`, `Winner Selection Migration`, `Logs Schema Migration`, `Notification Logs Migration`, `Invoices Schema Migration`, `User Status Migration`, `Database Schema Update`, `Person CRUD Migration`, `Kanban Schema Migration`, `Supplier Mapping Migration`, `Tags Schema Migration`, `People and Cities Migration`, `Hierarchical Cost Center Migration`, `Person Helper Services`, `User Tags Migration`, `User Status Update Migration`, `Theme Settings Migration`, `Supplier Product Repository`, `Database Seeding and Identity`, `Company Repository`, `City Repository`, `City Data Seeding`, `Cost Center Repository`?**
  _High betweenness centrality (0.227) - this node is a cross-community bridge._
- **Why does `CentroCustoRepository` connect `Cost Center Repository` to `Financial Cost Centers`, `Approval Workflow Module`, `Cost Center Dialog`, `Database Context`, `Cost Center Repository`?**
  _High betweenness centrality (0.083) - this node is a cross-community bridge._
- **What connects `Abrir`, `MarcarTodasLidas`, `OnInitializedAsync` to the rest of the system?**
  _1223 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Notification System` be split into smaller, more focused modules?**
  _Cohesion score 0.06090808416389812 - nodes in this community are weakly interconnected._
- **Should `Request Management Module` be split into smaller, more focused modules?**
  _Cohesion score 0.05263157894736842 - nodes in this community are weakly interconnected._
- **Should `Quotation Dialog` be split into smaller, more focused modules?**
  _Cohesion score 0.05405405405405406 - nodes in this community are weakly interconnected._