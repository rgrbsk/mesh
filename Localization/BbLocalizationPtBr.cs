using BlazorBlueprint.Components;

namespace Erp.Localization;

/// <summary>
/// Traduções pt-BR para todas as strings de UI do BlazorBlueprint.Components
/// (data grid, construtor de filtros, paginação, editores, tema, etc.).
/// Aplicado no startup via <c>AddBlazorBlueprintComponents(BbLocalizationPtBr.Configure)</c>.
///
/// As chaves seguem a notação de ponto do <see cref="DefaultBbLocalizer"/> e foram
/// extraídas do pacote 3.15.0. Placeholders {0}/{1}/{2} devem ser preservados.
/// Ao atualizar o pacote, rode novamente o dump e complete as chaves novas
/// (as não sobrescritas caem no default em inglês).
/// </summary>
public static class BbLocalizationPtBr
{
    public static void Configure(DefaultBbLocalizer loc)
    {
        foreach (var (key, value) in Strings)
            loc.Set(key, value);
    }

    private static readonly Dictionary<string, string> Strings = new()
    {
        // ── Alert ──
        ["Alert.Dismiss"] = "Dispensar",

        // ── Breadcrumb ──
        ["Breadcrumb.Breadcrumb"] = "trilha de navegação",
        ["Breadcrumb.More"] = "Mais",

        // ── Calendar ──
        ["Calendar.GoToPreviousMonth"] = "Ir para o mês anterior",
        ["Calendar.GoToNextMonth"] = "Ir para o próximo mês",

        // ── Carousel ──
        ["Carousel.NextSlide"] = "Próximo slide",
        ["Carousel.PreviousSlide"] = "Slide anterior",

        // ── Combobox ──
        ["Combobox.EmptyMessage"] = "Nenhum resultado encontrado.",
        ["Combobox.Placeholder"] = "Selecione uma opção...",
        ["Combobox.SearchPlaceholder"] = "Pesquisar...",

        // ── Command ──
        ["Command.CommandMenu"] = "Menu de comandos",
        ["Command.CommandList"] = "Lista de comandos",

        // ── CopyText ──
        ["CopyText.Copied"] = "Copiado!",
        ["CopyText.ClickToCopy"] = "Clique para copiar",

        // ── DashboardGrid ──
        ["DashboardGrid.Loading"] = "Carregando painel",
        ["DashboardGrid.NoWidgets"] = "Nenhum widget para exibir",
        ["DashboardGrid.NoWidgetsDescription"] = "Comece adicionando seu primeiro widget.",
        ["DashboardGrid.AddWidget"] = "Adicionar widget",
        ["DashboardGrid.RemoveWidget"] = "Remover widget",
        ["DashboardGrid.ResizeWidget"] = "Redimensionar widget",

        // ── DataGrid ──
        ["DataGrid.Loading"] = "Carregando...",
        ["DataGrid.NoResultsFound"] = "Nenhum resultado encontrado",
        ["DataGrid.NoResultsFilterDescription"] = "Tente ajustar ou limpar os filtros.",
        ["DataGrid.PreviousPage"] = "Página anterior",
        ["DataGrid.NextPage"] = "Próxima página",
        ["DataGrid.ExpandAll"] = "Expandir tudo",
        ["DataGrid.CollapseAll"] = "Recolher tudo",
        ["DataGrid.SelectAllOnPage"] = "Selecionar todos nesta página ({0} itens)",
        ["DataGrid.SelectAllItems"] = "Selecionar todos os {0} itens",
        ["DataGrid.ClearSelection"] = "Limpar seleção",
        ["DataGrid.SelectRowsAriaLabel"] = "Selecionar linhas - clique para ver as opções",
        ["DataGrid.SelectAllRows"] = "Selecionar todas as linhas",
        ["DataGrid.SelectThisRow"] = "Selecionar esta linha",
        ["DataGrid.ExpandRow"] = "Expandir linha",
        ["DataGrid.CollapseRow"] = "Recolher linha",
        ["DataGrid.Expand"] = "Expandir",
        ["DataGrid.Collapse"] = "Recolher",
        ["DataGrid.ExpandGroup"] = "Expandir grupo",
        ["DataGrid.CollapseGroup"] = "Recolher grupo",
        ["DataGrid.FilterPlaceholder"] = "Filtrar {0}",
        ["DataGrid.ColumnMenu"] = "Opções da coluna {0}",
        ["DataGrid.GroupByColumn"] = "Agrupar por {0}",
        ["DataGrid.UngroupColumn"] = "Remover agrupamento",
        ["DataGrid.PinnedColumnTooltip"] = "Esta coluna está fixada e não pode ser movida",
        ["DataGrid.ActiveFilters"] = "{0} filtro(s) ativo(s)",
        ["DataGrid.ClearAll"] = "Limpar tudo",
        ["DataGrid.ShowingRange"] = "Exibindo {0}–{1} de {2}",
        ["DataGrid.RowsSelected"] = "{0} de {1} linha(s) selecionada(s)",
        ["DataGrid.GroupItemCount"] = "({0} itens)",
        ["DataGrid.CountLabel"] = "Contagem",
        ["DataGrid.SumLabel"] = "Soma",
        ["DataGrid.AverageLabel"] = "Média",
        ["DataGrid.MinLabel"] = "Mín",
        ["DataGrid.MaxLabel"] = "Máx",
        ["DataGrid.FilterColumnEnterValue"] = "Digite o valor...",
        ["DataGrid.FilterColumnMin"] = "Mín",
        ["DataGrid.FilterColumnAnd"] = "e",
        ["DataGrid.FilterColumnMax"] = "Máx",
        ["DataGrid.FilterColumnAmount"] = "Valor",
        ["DataGrid.FilterColumnPickDate"] = "Escolha uma data",
        ["DataGrid.FilterColumnSelectValues"] = "Selecione os valores...",
        ["DataGrid.FilterColumnSelectValue"] = "Selecione o valor...",
        ["DataGrid.FilterColumnClear"] = "Limpar",
        ["DataGrid.FilterColumnApply"] = "Aplicar",
        ["DataGrid.SearchPlaceholder"] = "Pesquisar...",

        // ── DataTable ──
        ["DataTable.Loading"] = "Carregando...",
        ["DataTable.NoResultsFound"] = "Nenhum resultado encontrado",
        ["DataTable.SelectRowsAriaLabel"] = "Selecionar linhas - clique para ver as opções",
        ["DataTable.SelectAllOnPage"] = "Selecionar todos nesta página ({0} itens)",
        ["DataTable.SelectAllItems"] = "Selecionar todos os {0} itens",
        ["DataTable.ClearSelection"] = "Limpar seleção",
        ["DataTable.SelectAllRows"] = "Selecionar todas as linhas",
        ["DataTable.SelectThisRow"] = "Selecionar esta linha",
        ["DataTable.Search"] = "Pesquisar...",
        ["DataTable.Columns"] = "Colunas",
        ["DataTable.ToggleColumns"] = "Alternar colunas",

        // ── DataView ──
        ["DataView.SearchPlaceholder"] = "Pesquisar...",
        ["DataView.NoResultsFound"] = "Nenhum resultado encontrado",
        ["DataView.Loading"] = "Carregando...",
        ["DataView.LoadingMore"] = "Carregando mais...",
        ["DataView.LoadMore"] = "Carregar mais",
        ["DataView.ListView"] = "Visualização em lista",
        ["DataView.GridView"] = "Visualização em grade",
        ["DataView.Sort"] = "Ordenar",

        // ── DatePicker ──
        ["DatePicker.Placeholder"] = "Escolha uma data",
        ["DatePicker.OpenCalendar"] = "Abrir calendário",

        // ── DateRangePicker ──
        ["DateRangePicker.Placeholder"] = "Selecione o período",
        ["DateRangePicker.QuickSelect"] = "Seleção rápida",
        ["DateRangePicker.SelectEndDate"] = "Selecione a data final",
        ["DateRangePicker.DaysSelected"] = "{0} dia(s) selecionado(s)",
        ["DateRangePicker.Clear"] = "Limpar",
        ["DateRangePicker.Apply"] = "Aplicar",
        ["DateRangePicker.Today"] = "Hoje",
        ["DateRangePicker.Yesterday"] = "Ontem",
        ["DateRangePicker.Last7Days"] = "Últimos 7 dias",
        ["DateRangePicker.Last30Days"] = "Últimos 30 dias",
        ["DateRangePicker.ThisMonth"] = "Este mês",
        ["DateRangePicker.LastMonth"] = "Mês passado",
        ["DateRangePicker.ThisYear"] = "Este ano",
        ["DateRangePicker.Custom"] = "Personalizado",

        // ── DateTimePicker ──
        ["DateTimePicker.Placeholder"] = "Escolha data e hora",
        ["DateTimePicker.Hour"] = "Hora",
        ["DateTimePicker.Minute"] = "Min",
        ["DateTimePicker.Second"] = "Seg",
        ["DateTimePicker.IncrementHour"] = "Aumentar hora",
        ["DateTimePicker.DecrementHour"] = "Diminuir hora",
        ["DateTimePicker.IncrementMinute"] = "Aumentar minuto",
        ["DateTimePicker.DecrementMinute"] = "Diminuir minuto",
        ["DateTimePicker.IncrementSecond"] = "Aumentar segundo",
        ["DateTimePicker.DecrementSecond"] = "Diminuir segundo",
        ["DateTimePicker.Now"] = "Agora",
        ["DateTimePicker.Clear"] = "Limpar",

        // ── Dialog ──
        ["Dialog.Close"] = "Fechar",

        // ── Dock ──
        ["Dock.Close"] = "Fechar",
        ["Dock.CloseTab"] = "Fechar {0}",
        ["Dock.CloseOtherTabs"] = "Fechar outras abas",
        ["Dock.CloseAllButPinned"] = "Fechar todas exceto as fixadas",
        ["Dock.CloseAllTabs"] = "Fechar todas as abas",
        ["Dock.PinTab"] = "Fixar aba",
        ["Dock.UnpinTab"] = "Desafixar aba",
        ["Dock.ShowHiddenTabs"] = "Mostrar {0} aba(s) oculta(s)",
        ["Dock.MaximizePanelGroup"] = "Maximizar grupo de painéis",
        ["Dock.RestorePanelGroup"] = "Restaurar grupo de painéis",
        ["Dock.NoPanelsOpen"] = "Nenhum painel aberto.",

        // ── EventCalendar ──
        ["EventCalendar.Today"] = "Hoje",
        ["EventCalendar.Month"] = "Mês",
        ["EventCalendar.Week"] = "Semana",
        ["EventCalendar.Agenda"] = "Agenda",
        ["EventCalendar.PreviousPeriod"] = "Ir para o período anterior",
        ["EventCalendar.NextPeriod"] = "Ir para o próximo período",
        ["EventCalendar.ViewSwitcher"] = "Visualização do calendário",
        ["EventCalendar.MoreEvents"] = "+{0} mais",
        ["EventCalendar.NoEvents"] = "Nenhum evento para exibir.",
        ["EventCalendar.AllDay"] = "Dia inteiro",

        // ── FilterBuilder ──
        ["FilterBuilder.FilterBuilderAriaLabel"] = "Construtor de filtros",
        ["FilterBuilder.SelectField"] = "Selecione o campo...",
        ["FilterBuilder.RemoveCondition"] = "Remover condição",
        ["FilterBuilder.RemoveGroup"] = "Remover grupo",
        ["FilterBuilder.AddCondition"] = "Adicionar condição",
        ["FilterBuilder.AddGroup"] = "Adicionar grupo",
        ["FilterBuilder.FilterCondition"] = "Condição de filtro",
        ["FilterBuilder.EnterValue"] = "Digite o valor...",
        ["FilterBuilder.Min"] = "Mín",
        ["FilterBuilder.And"] = "e",
        ["FilterBuilder.Max"] = "Máx",
        ["FilterBuilder.Amount"] = "Valor",
        ["FilterBuilder.PickDate"] = "Escolha uma data",
        ["FilterBuilder.SelectValues"] = "Selecione os valores...",
        ["FilterBuilder.SelectValue"] = "Selecione o valor...",
        ["FilterBuilder.Today"] = "hoje",
        ["FilterBuilder.Yesterday"] = "ontem",
        ["FilterBuilder.Tomorrow"] = "amanhã",
        ["FilterBuilder.ThisWeek"] = "esta semana",
        ["FilterBuilder.LastWeek"] = "semana passada",
        ["FilterBuilder.NextWeek"] = "próxima semana",
        ["FilterBuilder.ThisMonth"] = "este mês",
        ["FilterBuilder.LastMonth"] = "mês passado",
        ["FilterBuilder.NextMonth"] = "próximo mês",
        ["FilterBuilder.ThisQuarter"] = "este trimestre",
        ["FilterBuilder.LastQuarter"] = "trimestre passado",
        ["FilterBuilder.ThisYear"] = "este ano",
        ["FilterBuilder.LastYear"] = "ano passado",
        ["FilterBuilder.Days"] = "dias",
        ["FilterBuilder.Weeks"] = "semanas",
        ["FilterBuilder.Months"] = "meses",
        ["FilterBuilder.Hours"] = "horas",
        ["FilterBuilder.Minutes"] = "minutos",
        ["FilterBuilder.Seconds"] = "segundos",
        ["FilterBuilder.Where"] = "Onde",
        ["FilterBuilder.OperatorAnd"] = "E",
        ["FilterBuilder.OperatorOr"] = "OU",
        ["FilterBuilder.OperatorEquals"] = "igual a",
        ["FilterBuilder.OperatorNotEquals"] = "diferente de",
        ["FilterBuilder.OperatorIsEmpty"] = "está vazio",
        ["FilterBuilder.OperatorIsNotEmpty"] = "não está vazio",
        ["FilterBuilder.OperatorContains"] = "contém",
        ["FilterBuilder.OperatorNotContains"] = "não contém",
        ["FilterBuilder.OperatorStartsWith"] = "começa com",
        ["FilterBuilder.OperatorEndsWith"] = "termina com",
        ["FilterBuilder.OperatorGreaterThan"] = "maior que",
        ["FilterBuilder.OperatorLessThan"] = "menor que",
        ["FilterBuilder.OperatorGreaterOrEqual"] = "maior ou igual",
        ["FilterBuilder.OperatorLessOrEqual"] = "menor ou igual",
        ["FilterBuilder.OperatorBetween"] = "entre",
        ["FilterBuilder.OperatorInLast"] = "nos últimos",
        ["FilterBuilder.OperatorInNext"] = "nos próximos",
        ["FilterBuilder.OperatorIn"] = "é qualquer um de",
        ["FilterBuilder.OperatorNotIn"] = "não é nenhum de",
        ["FilterBuilder.OperatorIsTrue"] = "é verdadeiro",
        ["FilterBuilder.OperatorIsFalse"] = "é falso",
        ["FilterBuilder.OperatorDateIs"] = "é",
        ["FilterBuilder.OperatorDateIsNot"] = "não é",
        ["FilterBuilder.OperatorGreaterThanDate"] = "é depois de",
        ["FilterBuilder.OperatorLessThanDate"] = "é antes de",

        // ── FormWizard ──
        ["FormWizard.WizardProgress"] = "Progresso do assistente",
        ["FormWizard.Back"] = "Voltar",
        ["FormWizard.Next"] = "Avançar",
        ["FormWizard.Skip"] = "Pular",
        ["FormWizard.Complete"] = "Concluir",

        // ── MarkdownEditor ──
        ["MarkdownEditor.SelectHeadingLevel"] = "Selecionar nível de título",
        ["MarkdownEditor.Bold"] = "Negrito (Ctrl+B)",
        ["MarkdownEditor.Italic"] = "Itálico (Ctrl+I)",
        ["MarkdownEditor.Underline"] = "Sublinhado (Ctrl+U)",
        ["MarkdownEditor.BulletList"] = "Lista com marcadores",
        ["MarkdownEditor.NumberedList"] = "Lista numerada",

        // ── MultiSelect ──
        ["MultiSelect.EmptyMessage"] = "Nenhum resultado encontrado.",
        ["MultiSelect.Placeholder"] = "Selecione os itens...",
        ["MultiSelect.SearchPlaceholder"] = "Pesquisar...",
        ["MultiSelect.SelectAll"] = "Selecionar todos",
        ["MultiSelect.Clear"] = "Limpar",
        ["MultiSelect.Close"] = "Fechar",

        // ── NumericInput ──
        ["NumericInput.IncreaseValue"] = "Aumentar valor",
        ["NumericInput.DecreaseValue"] = "Diminuir valor",

        // ── Pagination ──
        ["Pagination.Pagination"] = "Paginação",
        ["Pagination.Previous"] = "Anterior",
        ["Pagination.Next"] = "Próximo",
        ["Pagination.MorePages"] = "Mais páginas",
        ["Pagination.GoToFirstPage"] = "Ir para a primeira página",
        ["Pagination.GoToLastPage"] = "Ir para a última página",
        ["Pagination.RowsPerPage"] = "Linhas por página",
        ["Pagination.ShowingFormat"] = "Exibindo {0}-{1} de {2}",
        ["Pagination.PageFormat"] = "Página {0} de {1}",
        ["Pagination.NoItems"] = "Nenhum item",

        // ── Rating ──
        ["Rating.Rating"] = "Avaliação",

        // ── ResponsiveNav ──
        ["ResponsiveNav.ToggleMenu"] = "Alternar menu",

        // ── RichTextEditor ──
        ["RichTextEditor.Normal"] = "Normal",
        ["RichTextEditor.Heading1"] = "Título 1",
        ["RichTextEditor.Heading2"] = "Título 2",
        ["RichTextEditor.Heading3"] = "Título 3",
        ["RichTextEditor.Bold"] = "Negrito (Ctrl+B)",
        ["RichTextEditor.Italic"] = "Itálico (Ctrl+I)",
        ["RichTextEditor.Underline"] = "Sublinhado (Ctrl+U)",
        ["RichTextEditor.Strikethrough"] = "Tachado",
        ["RichTextEditor.BulletList"] = "Lista com marcadores",
        ["RichTextEditor.NumberedList"] = "Lista numerada",
        ["RichTextEditor.InsertLink"] = "Inserir link",
        ["RichTextEditor.Blockquote"] = "Citação",
        ["RichTextEditor.CodeBlock"] = "Bloco de código",
        ["RichTextEditor.EditLink"] = "Editar link",
        ["RichTextEditor.InsertLinkTitle"] = "Inserir link",
        ["RichTextEditor.EditLinkDescription"] = "Atualize a URL ou remova o link.",
        ["RichTextEditor.InsertLinkDescription"] = "Digite a URL para o texto selecionado.",
        ["RichTextEditor.RemoveLink"] = "Remover link",
        ["RichTextEditor.Cancel"] = "Cancelar",
        ["RichTextEditor.Update"] = "Atualizar",
        ["RichTextEditor.Insert"] = "Inserir",

        // ── Sheet ──
        ["Sheet.Close"] = "Fechar",

        // ── Sidebar ──
        ["Sidebar.ToggleSidebar"] = "Alternar barra lateral",

        // ── TagInput ──
        ["TagInput.Placeholder"] = "Adicionar tag...",
        ["TagInput.RemoveTag"] = "Remover {0}",
        ["TagInput.ClearAllTags"] = "Limpar todas as tags",
        ["TagInput.TagSuggestions"] = "Sugestões de tags",

        // ── Theme ──
        ["Theme.Switcher.Label"] = "Personalizar tema",
        ["Theme.Switcher.Title"] = "Personalizar",
        ["Theme.Switcher.Description"] = "Escolha uma cor e um raio para seus componentes.",
        ["Theme.Color"] = "Cor",
        ["Theme.BaseColor"] = "Cor base",
        ["Theme.PrimaryColor"] = "Cor primária",
        ["Theme.Default"] = "Padrão",
        ["Theme.Radius"] = "Raio",
        ["Theme.Mode"] = "Modo",
        ["Theme.Light"] = "Claro",
        ["Theme.Dark"] = "Escuro",
        ["Theme.SwitchToLight"] = "Mudar para o modo claro",
        ["Theme.SwitchToDark"] = "Mudar para o modo escuro",

        // ── Timeline ──
        ["Timeline.Timeline"] = "Linha do tempo",
    };
}
