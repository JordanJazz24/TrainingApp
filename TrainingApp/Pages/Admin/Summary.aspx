<%@ Page Title="Resumen" Language="C#" MasterPageFile="~/UI/master/Admin.Master"
    AutoEventWireup="true" CodeBehind="Summary.aspx.cs"
    Inherits="TrainingApp.Pages.Admin.Summary" %>
<%@ Register Src="~/UI/Controls/Admin/PeriodFilter.ascx" TagPrefix="uc" TagName="PeriodFilter" %>

<%-- Resumen (spec 018, AC-9 a AC-12; diseño de la Fase 11, mockup v1), el inicio del
     administrador. Primero "¿Quién está listo?": por guía, los operadores por estado (todo el
     historial) y las tasas y el puntaje del período. Debajo, la actividad por día, semana o mes
     y el top 5 de pasos (del período). La tasa de aprobación, las sesiones y el top 5, con su
     cambio contra el período anterior (spec 023). Lo calcula DashboardService; la página arma
     los textos y le pasa la actividad a Chart.js. Filtros en la URL. Solo lectura. --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="admin-page-head">
        <div>
            <h1 class="admin-page-title">Resumen</h1>
            <p class="admin-page-lead">Cómo va el entrenamiento en el simulador.</p>
        </div>
        <asp:Panel ID="PnlFilters" runat="server" CssClass="mt-filters" DefaultButton="BtnFilter">
            <uc:PeriodFilter ID="Period" runat="server" />
            <div class="mt-field">
                <label class="mt-field-label" for="<%= DdlGuide.ClientID %>">Guía</label>
                <asp:DropDownList ID="DdlGuide" runat="server" CssClass="form-select" />
            </div>
            <asp:Button ID="BtnFilter" runat="server" Text="Filtrar" CssClass="btn btn-secondary"
                OnClick="BtnFilter_Click" />
            <asp:HyperLink ID="LnkExport" runat="server" CssClass="btn btn-primary"
                ToolTip="Sesiones e intentos por paso del período y la guía, en Excel"><i class="bi bi-download" aria-hidden="true"></i>Descargar sesiones</asp:HyperLink>
        </asp:Panel>
    </div>
    <%-- Un período inválido (también editando la URL) vale Este mes (P-6). --%>
    <asp:Label ID="LblInvalidPeriod" runat="server" CssClass="mt-alert-warn d-block mb-3" Visible="false"
        role="status" Text="El período pedido no es válido (falta una fecha, el Fin es anterior al Inicio o es posterior a hoy). Se muestra Este mes." />
    <%-- Con todo el historial el archivo es grande y tarda (D-148). --%>
    <asp:Label ID="LblExportSlow" runat="server" CssClass="mt-muted mt-small d-block text-end mb-3" Visible="false"
        Text="Con &quot;Todo&quot; el archivo es grande y la descarga puede tardar varios segundos." />

    <%-- ¿Quién está listo? Estados con todo el historial (DA-1); tasas y puntaje del período. --%>
    <section class="mt-hero summary-ready" aria-labelledby="readyTitle">
        <div class="summary-ready-head">
            <div>
                <h2 id="readyTitle" class="summary-ready-title">¿Quién está listo?</h2>
                <p class="mt-muted mb-0"><asp:Literal ID="LitReadyPeriod" runat="server" /></p>
            </div>
            <asp:HyperLink ID="LnkFollowUp" runat="server" CssClass="summary-alert-link"><i class="bi bi-exclamation-triangle" aria-hidden="true"></i>Ver seguimiento</asp:HyperLink>
        </div>

        <asp:PlaceHolder ID="PhStates" runat="server">
            <div class="summary-legend" aria-label="Leyenda">
                <span><span class="summary-dot summary-seg-ok"></span>Aprobado</span>
                <span><span class="summary-dot summary-seg-bad"></span>Reprobado</span>
                <span><span class="summary-dot summary-seg-warn"></span>En entrenamiento</span>
            </div>

            <asp:Repeater ID="RptGuides" runat="server">
                <ItemTemplate>
                    <div class="summary-guide">
                        <div class="summary-guide-name">
                            <span class="summary-guide-title" title='<%#: Eval("GuideTitle") %>'><%#: Eval("GuideName") %></span>
                            <span class="mt-muted mt-small"><%#: Eval("TotalText") %></span>
                        </div>
                        <div class="summary-guide-states">
                            <div class="summary-stack" role="img" aria-label='<%#: Eval("StatesAria") %>'>
                                <div class="summary-seg-ok" style='width: <%#: Eval("PassedWidth") %>%'></div>
                                <div class="summary-seg-bad" style='width: <%#: Eval("FailedWidth") %>%'></div>
                                <div class="summary-seg-warn" style='width: <%#: Eval("InTrainingWidth") %>%'></div>
                            </div>
                            <div class="summary-counts">
                                <span><strong><%#: Eval("Passed") %></strong> <span class="mt-muted">aprobados</span></span>
                                <span><strong><%#: Eval("Failed") %></strong> <span class="mt-muted">reprobados</span></span>
                                <span><strong><%#: Eval("InTraining") %></strong> <span class="mt-muted">en entrenamiento</span></span>
                            </div>
                        </div>
                        <div class="summary-figures">
                            <div class="mt-stat">
                                <span class="mt-stat-label">Aprobados</span>
                                <span class="summary-figure"><%#: Eval("PassRateText") %></span>
                                <span class="mt-stat-detail"><%#: Eval("PassRateDetail") %></span>
                                <asp:PlaceHolder runat="server" Visible='<%# (bool)Eval("PassRateChange.Visible") %>'>
                                    <span class='mt-change <%#: Eval("PassRateChange.Css") %>'><%#: Eval("PassRateChange.Text") %><span class="visually-hidden"> <%#: Eval("PassRateChange.Verdict") %></span></span>
                                    <span class="mt-change-ref"><%#: Eval("PassRateChange.Reference") %> · <%#: Eval("PassRateChange.Base") %></span>
                                </asp:PlaceHolder>
                            </div>
                            <div class="mt-stat">
                                <span class="mt-stat-label">Al primer intento</span>
                                <span class="summary-figure"><%#: Eval("FirstAttemptText") %></span>
                                <span class="mt-stat-detail"><%#: Eval("FirstAttemptDetail") %></span>
                            </div>
                            <div class="mt-stat">
                                <span class="mt-stat-label">Puntaje promedio</span>
                                <span class="summary-figure"><%#: Eval("AverageText") %></span>
                                <span class="mt-stat-detail">Assessment</span>
                            </div>
                        </div>
                    </div>
                </ItemTemplate>
            </asp:Repeater>

            <p class="summary-note">
                Aprobados: de los operadores con algún Assessment completado en el período, los que
                aprobaron alguno. Al primer intento: de los operadores cuyo primer Assessment de la
                guía cae en el período, los que lo aprobaron. "—" = sin operadores para calcularla.
            </p>
        </asp:PlaceHolder>
        <asp:Label ID="LblNoStates" runat="server" CssClass="mt-muted d-block" Visible="false"
            Text="Todavía no hay sesiones de entrenamiento." />
    </section>

    <div class="summary-grid">
        <%-- Actividad: sesiones por día, semana o mes y modo, del período, con el total y su
             cambio (P-11). --%>
        <section class="mt-panel mt-panel-body summary-block" aria-labelledby="activityTitle">
            <div class="summary-block-head">
                <div>
                    <h2 id="activityTitle" class="summary-block-title">Actividad</h2>
                    <p class="mt-muted mt-small mb-0"><asp:Literal ID="LitActivityPeriod" runat="server" /></p>
                </div>
                <asp:PlaceHolder ID="PhSessions" runat="server">
                    <div class="mt-stat summary-sessions">
                        <span class="mt-stat-label">Sesiones del período</span>
                        <span class="summary-figure"><%#: SessionsText %></span>
                        <asp:PlaceHolder runat="server" Visible="<%# SessionsChange.Visible %>">
                            <span class='mt-change <%#: SessionsChange.Css %>'><%#: SessionsChange.Text %><span class="visually-hidden"> <%#: SessionsChange.Verdict %></span></span>
                            <span class="mt-change-ref"><%#: SessionsChange.Reference %> · <%#: SessionsChange.Base %></span>
                        </asp:PlaceHolder>
                    </div>
                </asp:PlaceHolder>
            </div>
            <div class="summary-legend summary-legend-sm">
                <span><span class="summary-dot summary-mode-1"></span>Guided</span>
                <span><span class="summary-dot summary-mode-2"></span>Práctica libre</span>
                <span><span class="summary-dot summary-mode-3"></span>Assessment</span>
            </div>
            <asp:PlaceHolder ID="PhActivity" runat="server">
                <div class="summary-chart"><canvas id="chartActivity" aria-label="<%: ActivityAria %>" role="img"></canvas></div>
                <p class="mt-muted mt-small mb-0"><asp:Literal ID="LitActivityTotals" runat="server" /></p>
            </asp:PlaceHolder>
            <asp:Label ID="LblNoActivity" runat="server" CssClass="mt-muted d-block" Visible="false"
                Text="Sin datos para este período. Pruebe con &quot;Todo&quot; o con otra guía." />
        </section>

        <%-- Top 5 de pasos en Assessment, del período: el principio de Pasos (D-147). --%>
        <section class="mt-panel mt-panel-body summary-block" aria-labelledby="topTitle">
            <div>
                <h2 id="topTitle" class="summary-block-title">Dónde se falla más</h2>
                <p class="mt-muted mt-small mb-0">Top 5 de pasos por % de sesiones con fallo en Assessment, <asp:Literal ID="LitTopPeriod" runat="server" /></p>
            </div>
            <asp:PlaceHolder ID="PhTop" runat="server">
                <ol class="summary-top">
                    <asp:Repeater ID="RptTop" runat="server">
                        <ItemTemplate>
                            <li>
                                <span class="summary-top-rank"><%# Container.ItemIndex + 1 %></span>
                                <div class="summary-top-body">
                                    <div class="summary-top-line">
                                        <span class="fw-semibold"><%#: Eval("StepName") %></span>
                                        <span class="fw-semibold"><%#: Eval("FailureRateText") %></span>
                                    </div>
                                    <div class="mt-track"><div class="mt-fill" style='width: <%#: Eval("FailureWidth") %>%'></div></div>
                                    <span class="mt-muted mt-small"><%#: Eval("Detail") %></span>
                                    <asp:PlaceHolder runat="server" Visible='<%# (bool)Eval("FailureRateChange.Visible") %>'>
                                        <span class="mt-small">
                                            <span class='mt-change <%#: Eval("FailureRateChange.Css") %>'><%#: Eval("FailureRateChange.Text") %><span class="visually-hidden"> <%#: Eval("FailureRateChange.Verdict") %></span></span>
                                            <span class="mt-change-ref"><%#: Eval("FailureRateChange.Reference") %> · <%#: Eval("FailureRateChange.Base") %></span>
                                        </span>
                                    </asp:PlaceHolder>
                                </div>
                            </li>
                        </ItemTemplate>
                    </asp:Repeater>
                </ol>
            </asp:PlaceHolder>
            <asp:Label ID="LblNoTop" runat="server" CssClass="mt-muted d-block" Visible="false"
                Text="Sin datos para este período. Pruebe con &quot;Todo&quot; o con otra guía." />
            <asp:HyperLink ID="LnkSteps" runat="server" CssClass="summary-more">Ver todos los pasos</asp:HyperLink>
        </section>
    </div>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ScriptContent" runat="server">
    <script src="<%=ResolveUrl("~/Scripts/chart-4.5.1.umd.min.js")%>"></script>
    <script src="<%=ResolveUrl("~/UI/master/admin-charts.js")%>"></script>
    <script>
        // Actividad del servidor (JavaScriptSerializer), por día, semana o mes (spec 023, P-9).
        // Los modos van en orden de exigencia con los tres azules de los tokens (--mt-m1,
        // --mt-m2, --mt-m3); el total de cada barra se escribe sobre ella, como en el mockup.
        var summaryData = <%= ChartJson %>;

        (function () {
            var weeks = summaryData && summaryData.activity;
            if (!weeks || typeof mtCharts === 'undefined') return;

            var totals = weeks.labels.map(function (_, i) {
                return weeks.guided[i] + weeks.practice[i] + weeks.assessment[i];
            });

            mtCharts.create(document.getElementById('chartActivity'), function (t) {
                var totalLabels = {
                    id: 'totals',
                    afterDatasetsDraw: function (chart) {
                        var ctx = chart.ctx, meta = chart.getDatasetMeta(chart.data.datasets.length - 1);
                        ctx.save();
                        ctx.fillStyle = t.text;
                        ctx.font = '600 13px ' + t.font;
                        ctx.textAlign = 'center';
                        ctx.textBaseline = 'bottom';
                        meta.data.forEach(function (bar, i) {
                            if (totals[i] > 0) ctx.fillText(totals[i], bar.x, bar.y - 4);
                        });
                        ctx.restore();
                    }
                };
                return {
                    type: 'bar',
                    data: {
                        labels: weeks.labels,
                        datasets: [
                            mtCharts.bars(t, 'Guided', weeks.guided, t.m1),
                            mtCharts.bars(t, 'Práctica libre', weeks.practice, t.m2),
                            mtCharts.bars(t, 'Assessment', weeks.assessment, t.m3)
                        ]
                    },
                    options: {
                        maintainAspectRatio: false,
                        layout: { padding: { top: 20 } },
                        scales: mtCharts.axes(t, { stacked: true }),
                        plugins: {
                            legend: { display: false },
                            tooltip: {
                                mode: 'index', intersect: false,
                                callbacks: { title: function (items) { return weeks.titlePrefix + items[0].label; } }
                            }
                        }
                    },
                    plugins: [totalLabels]
                };
            });
        })();
    </script>
</asp:Content>
