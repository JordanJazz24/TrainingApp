<%@ Page Title="Resumen" Language="C#" MasterPageFile="~/UI/master/Admin.Master"
    AutoEventWireup="true" CodeBehind="Summary.aspx.cs"
    Inherits="TrainingApp.Pages.Admin.Summary" %>

<%-- Resumen (spec 018, AC-9 a AC-12), el inicio del administrador: operadores por estado (todo
     el historial), tasa de aprobación, actividad por semana y top 5 de pasos (del período). Lo
     calcula DashboardService; la página arma las tablas y le pasa los datos a Chart.js. Cada
     gráfica tiene su tabla al lado. Filtros en la URL. Solo lectura. --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <h1 class="h3 mb-4">Resumen</h1>

    <asp:Panel ID="PnlFilters" runat="server" CssClass="row g-2 align-items-end mb-4" DefaultButton="BtnFilter">
        <div class="col-sm-6 col-md-3">
            <label class="form-label small text-muted mb-1" for="<%= DdlPeriod.ClientID %>">Período</label>
            <asp:DropDownList ID="DdlPeriod" runat="server" CssClass="form-select">
                <asp:ListItem Value="" Text="Últimos 30 días" />
                <asp:ListItem Value="all" Text="Todo" />
            </asp:DropDownList>
        </div>
        <div class="col-sm-6 col-md-3">
            <label class="form-label small text-muted mb-1" for="<%= DdlGuide.ClientID %>">Guía</label>
            <asp:DropDownList ID="DdlGuide" runat="server" CssClass="form-select" />
        </div>
        <div class="col-sm-6 col-md-2">
            <asp:Button ID="BtnFilter" runat="server" Text="Filtrar" CssClass="btn btn-primary w-100"
                OnClick="BtnFilter_Click" />
        </div>
        <div class="col-sm-6 col-md-3 ms-md-auto">
            <asp:HyperLink ID="LnkExport" runat="server" CssClass="btn btn-outline-secondary w-100"
                ToolTip="Sesiones e intentos por paso del período y la guía, en Excel">Descargar sesiones (Excel)</asp:HyperLink>
            <%-- Con todo el historial el archivo es grande y tarda (D-148). --%>
            <asp:Label ID="LblExportSlow" runat="server" CssClass="small text-muted d-block mt-1" Visible="false"
                Text="Con &quot;Todo&quot; el archivo es grande y la descarga puede tardar varios segundos." />
        </div>
    </asp:Panel>

    <div class="row g-4">
        <%-- Operadores por estado: todo el historial (DA-1). --%>
        <div class="col-lg-6">
            <div class="card h-100">
                <div class="card-body">
                    <h2 class="h6 mb-0">Operadores por estado</h2>
                    <p class="small text-muted mb-3">Todo el historial</p>
                    <asp:PlaceHolder ID="PhStates" runat="server">
                        <div class="summary-chart"><canvas id="chartStates" aria-label="Operadores por estado y guía" role="img"></canvas></div>
                        <table class="table table-sm mt-3 mb-0">
                            <thead>
                                <tr>
                                    <th>Guía</th>
                                    <th class="text-end">Aprobado</th>
                                    <th class="text-end">Reprobado</th>
                                    <th class="text-end">En entrenamiento</th>
                                </tr>
                            </thead>
                            <tbody>
                                <asp:Repeater ID="RptStates" runat="server">
                                    <ItemTemplate>
                                        <tr>
                                            <td title='<%#: Eval("GuideTitle") %>'><%#: Eval("GuideName") %></td>
                                            <td class="text-end"><%#: Eval("Passed") %></td>
                                            <td class="text-end"><%#: Eval("Failed") %></td>
                                            <td class="text-end"><%#: Eval("InTraining") %></td>
                                        </tr>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </tbody>
                        </table>
                    </asp:PlaceHolder>
                    <asp:Label ID="LblNoStates" runat="server" CssClass="text-muted" Visible="false"
                        Text="Sin datos." />
                </div>
            </div>
        </div>

        <%-- Tasa de aprobación: del período. --%>
        <div class="col-lg-6">
            <div class="card h-100">
                <div class="card-body">
                    <h2 class="h6 mb-0">Tasa de aprobación</h2>
                    <p class="small text-muted mb-3"><asp:Literal ID="LitRatesPeriod" runat="server" /></p>
                    <asp:PlaceHolder ID="PhRates" runat="server">
                    <div class="table-responsive">
                        <table class="table table-sm mb-2">
                            <thead>
                                <tr>
                                    <th>Guía</th>
                                    <th class="text-end">Operadores evaluados</th>
                                    <th class="text-end">% aprobados</th>
                                    <th class="text-end">% al primer intento</th>
                                    <th class="text-end">Puntaje promedio</th>
                                </tr>
                            </thead>
                            <tbody>
                                <asp:Repeater ID="RptRates" runat="server">
                                    <ItemTemplate>
                                        <tr>
                                            <td title='<%#: Eval("GuideTitle") %>'><%#: Eval("GuideName") %></td>
                                            <td class="text-end"><%#: Eval("Assessed") %></td>
                                            <td class="text-end"><%#: Eval("PassRateText") %></td>
                                            <td class="text-end"><%#: Eval("FirstAttemptText") %></td>
                                            <td class="text-end"><%#: Eval("AverageText") %></td>
                                        </tr>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </tbody>
                        </table>
                    </div>
                    <p class="small text-muted mb-0">
                        % aprobados: de los operadores con algún Assessment completado en el período, los que
                        aprobaron alguno. % al primer intento: de los operadores cuyo primer Assessment de la
                        guía cae en el período, los que lo aprobaron. "—" = sin operadores para calcularla.
                    </p>
                    </asp:PlaceHolder>
                    <asp:Label ID="LblNoRates" runat="server" CssClass="text-muted" Visible="false"
                        Text="Sin datos para este período." />
                </div>
            </div>
        </div>

        <%-- Actividad: sesiones por semana (lunes a domingo) y modo, del período. --%>
        <div class="col-lg-6">
            <div class="card h-100">
                <div class="card-body">
                    <h2 class="h6 mb-0">Actividad</h2>
                    <p class="small text-muted mb-3">Sesiones por semana (de lunes a domingo) y modo · <asp:Literal ID="LitActivityPeriod" runat="server" /></p>
                    <asp:PlaceHolder ID="PhActivity" runat="server">
                        <div class="summary-chart"><canvas id="chartActivity" aria-label="Sesiones por semana y modo" role="img"></canvas></div>
                        <p class="small text-muted mt-2 mb-0"><asp:Literal ID="LitActivityTotals" runat="server" /></p>
                    </asp:PlaceHolder>
                    <asp:Label ID="LblNoActivity" runat="server" CssClass="text-muted" Visible="false"
                        Text="Sin datos para este período." />
                </div>
            </div>
        </div>

        <%-- Top 5 de pasos en Assessment, del período: el principio de Pasos (D-147). --%>
        <div class="col-lg-6">
            <div class="card h-100">
                <div class="card-body">
                    <div class="d-flex justify-content-between align-items-start">
                        <h2 class="h6 mb-0">Top 5 de pasos con más fallos en Assessment</h2>
                        <asp:HyperLink ID="LnkSteps" runat="server" CssClass="btn btn-link btn-sm p-0">Ver Pasos &raquo;</asp:HyperLink>
                    </div>
                    <p class="small text-muted mb-3">% de sesiones con fallo · <asp:Literal ID="LitTopPeriod" runat="server" /></p>
                    <asp:PlaceHolder ID="PhTop" runat="server">
                        <div class="summary-chart"><canvas id="chartTop" aria-label="Top 5 de pasos por porcentaje de sesiones con fallo" role="img"></canvas></div>
                        <table class="table table-sm mt-3 mb-0">
                            <thead>
                                <tr>
                                    <th>Paso</th>
                                    <th class="text-end">Sesiones</th>
                                    <th class="text-end">% con fallo</th>
                                </tr>
                            </thead>
                            <tbody>
                                <asp:Repeater ID="RptTop" runat="server">
                                    <ItemTemplate>
                                        <tr>
                                            <td><%#: Eval("StepName") %></td>
                                            <td class="text-end"><%#: Eval("Sessions") %></td>
                                            <td class="text-end"><%#: Eval("FailureRateText") %></td>
                                        </tr>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </tbody>
                        </table>
                    </asp:PlaceHolder>
                    <asp:Label ID="LblNoTop" runat="server" CssClass="text-muted" Visible="false"
                        Text="Sin datos para este período." />
                </div>
            </div>
        </div>
    </div>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ScriptContent" runat="server">
    <script src="<%=ResolveUrl("~/Scripts/chart-4.5.1.umd.min.js")%>"></script>
    <script>
        // Datos del servidor (JavaScriptSerializer). Colores (skill dataviz, paleta de referencia,
        // modo claro: el módulo administrativo no tiene modo oscuro): estados con la paleta de
        // estado (bien / advertencia / crítico, como sus badges), modos con los tres primeros
        // colores categóricos en orden fijo, el top 5 con uno solo.
        var summaryData = <%= ChartJson %>;

        (function () {
            if (typeof Chart === 'undefined' || !summaryData) return;

            var ink = { muted: '#898781', grid: '#e1e0d9', axis: '#c3c2b7', surface: '#ffffff' };
            Chart.defaults.font.family = 'system-ui, -apple-system, "Segoe UI", sans-serif';
            Chart.defaults.color = ink.muted;

            function axes(stacked, horizontal, percent) {
                var value = {
                    stacked: stacked, beginAtZero: true,
                    grid: { color: ink.grid }, border: { color: ink.axis },
                    ticks: { precision: 0, callback: percent ? function (v) { return v + '%'; } : undefined }
                };
                if (percent) value.max = 100;
                var category = { stacked: stacked, grid: { display: false }, border: { color: ink.axis } };
                return horizontal ? { x: value, y: category } : { x: category, y: value };
            }

            // Barras finas, extremos redondeados de 4px y 2px de superficie entre segmentos.
            function bars(label, data, color) {
                return {
                    label: label, data: data, backgroundColor: color,
                    borderColor: ink.surface, borderWidth: 2, borderRadius: 4, borderSkipped: false,
                    maxBarThickness: 28
                };
            }

            var legend = { position: 'bottom', labels: { usePointStyle: true, pointStyle: 'rectRounded', boxWidth: 10 } };

            var states = summaryData.states;
            if (states && document.getElementById('chartStates')) {
                new Chart(document.getElementById('chartStates'), {
                    type: 'bar',
                    data: {
                        labels: states.labels,
                        datasets: [
                            bars('Aprobado', states.passed, '#0ca30c'),
                            bars('Reprobado', states.failed, '#d03b3b'),
                            bars('En entrenamiento', states.training, '#fab219')
                        ]
                    },
                    options: {
                        indexAxis: 'y', maintainAspectRatio: false,
                        scales: axes(true, true, false),
                        plugins: { legend: legend, tooltip: { mode: 'index', intersect: false } }
                    }
                });
            }

            var weeks = summaryData.weeks;
            if (weeks && document.getElementById('chartActivity')) {
                new Chart(document.getElementById('chartActivity'), {
                    type: 'bar',
                    data: {
                        labels: weeks.labels,
                        datasets: [
                            bars('Guided', weeks.guided, '#2a78d6'),
                            bars('Assessment', weeks.assessment, '#eb6834'),
                            bars('Práctica libre', weeks.practice, '#1baf7a')
                        ]
                    },
                    options: {
                        maintainAspectRatio: false,
                        scales: axes(true, false, false),
                        plugins: {
                            legend: legend,
                            tooltip: {
                                mode: 'index', intersect: false,
                                callbacks: { title: function (items) { return 'Semana del ' + items[0].label; } }
                            }
                        }
                    }
                });
            }

            var top = summaryData.top;
            if (top && document.getElementById('chartTop')) {
                new Chart(document.getElementById('chartTop'), {
                    type: 'bar',
                    data: { labels: top.labels, datasets: [bars('% de sesiones con fallo', top.rates, '#2a78d6')] },
                    options: {
                        indexAxis: 'y', maintainAspectRatio: false,
                        scales: axes(false, true, true),
                        plugins: {
                            legend: { display: false },
                            tooltip: {
                                callbacks: {
                                    label: function (item) {
                                        return item.parsed.x.toFixed(1) + '% de ' + top.sessions[item.dataIndex] + ' sesiones';
                                    }
                                }
                            }
                        }
                    }
                });
            }
        })();
    </script>
</asp:Content>
