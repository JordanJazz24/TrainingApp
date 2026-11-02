<%@ Page Title="Resumen" Language="C#" MasterPageFile="~/UI/master/Admin.Master"
    AutoEventWireup="true" CodeBehind="Summary.aspx.cs"
    Inherits="TrainingApp.Pages.Admin.Summary" %>
<%@ Register Src="~/UI/Controls/Admin/PeriodFilter.ascx" TagPrefix="uc" TagName="PeriodFilter" %>

<%-- Resumen (spec 018, AC-9 a AC-12; diseño de la Fase 11, alternativa I "Sala" de la spec 022,
     E8b, docs/specs/022-design/resumen.md). Pensado para proyectar: los filtros y el encabezado
     no cambian; debajo, cuatro pestañas y un escenario que muestra un paso a la vez: 1 ¿Quién
     está listo? ¿Va mejor o peor?, 2 Por guía, 3 Dónde se falla y 4 Actividad. Cambiar de paso
     es del cliente, sin postback; sin JavaScript se ven los cuatro apilados. Los números los
     calcula DashboardService (los totales, SummaryTotals); la página arma los textos y le pasa
     la actividad a Chart.js. La tasa de aprobación, las sesiones y el top 5, con su cambio
     contra el período anterior (spec 023). Filtros en la URL. Solo lectura. --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="admin-page-head summary-head">
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
    <asp:Label ID="LblExportSlow" runat="server" CssClass="mt-muted mt-small d-block summary-slow" Visible="false"
        Text="Con &quot;Todo&quot; el archivo es grande y la descarga puede tardar varios segundos." />

    <%-- Las pestañas y Anterior / Siguiente no hacen nada sin JavaScript: el script las muestra. --%>
    <div class="summary-bar" id="sumBar" hidden>
        <div class="summary-tabs" role="tablist" aria-label="Preguntas del resumen">
            <button type="button" class="summary-tab" role="tab" id="sumTab1" aria-controls="sumSlide1" aria-selected="true"><span class="summary-tab-n">1</span>Listos y mejor o peor</button>
            <button type="button" class="summary-tab" role="tab" id="sumTab2" aria-controls="sumSlide2" aria-selected="false" tabindex="-1"><span class="summary-tab-n">2</span>Por guía</button>
            <button type="button" class="summary-tab" role="tab" id="sumTab3" aria-controls="sumSlide3" aria-selected="false" tabindex="-1"><span class="summary-tab-n">3</span>Dónde se falla</button>
            <button type="button" class="summary-tab" role="tab" id="sumTab4" aria-controls="sumSlide4" aria-selected="false" tabindex="-1"><span class="summary-tab-n">4</span>Actividad</button>
        </div>
        <div class="summary-nav">
            <button type="button" class="summary-nav-btn" id="sumPrev" aria-label="Anterior"><i class="bi bi-arrow-left" aria-hidden="true"></i></button>
            <button type="button" class="summary-nav-btn" id="sumNext" aria-label="Siguiente"><i class="bi bi-arrow-right" aria-hidden="true"></i></button>
        </div>
    </div>

    <section class="summary-stage" aria-live="polite">

        <%-- 1 · ¿Quién está listo? ¿Va mejor o peor? Estados con todo el historial (DA-1), sumados por
             guía; la aprobación global del período y, por guía, su tasa y su veredicto. --%>
        <div class="summary-slide" id="sumSlide1" role="tabpanel" aria-labelledby="sumTab1">
            <asp:PlaceHolder ID="PhGlobal" runat="server">
                <div class="summary-s1">
                    <asp:PlaceHolder runat="server" Visible="<%# HasStates %>">
                        <div class="summary-s1-ring">
                            <div class="summary-ring summary-ring-global" role="img" aria-label='<%#: GlobalPassedText %> <%#: GlobalPassedDetail %>' style='--ring-ok: <%#: GlobalRingOk %>%; --ring-bad: <%#: GlobalRingBad %>%'>
                                <div class="summary-ring-hole"><span class="summary-ring-big"><%#: GlobalPassedText %></span><span class="summary-ring-cap">aprobados</span></div>
                            </div>
                            <span class="mt-muted mt-small"><%#: GlobalPassedDetail %></span>
                        </div>
                    </asp:PlaceHolder>
                    <div class="summary-s1-main">
                        <span class="summary-eyebrow">1 · ¿Quién está listo? ¿Va mejor o peor?</span>
                        <asp:PlaceHolder runat="server" Visible="<%# HasStates %>">
                            <div class="summary-rate-line">
                                <span class="summary-rate-big"><%#: GlobalRateText %></span>
                                <span class='<%#: GlobalVerdictCss %>'><i class='<%#: GlobalVerdictIcon %>' aria-hidden="true"></i><%#: GlobalVerdictText %></span>
                            </div>
                            <span class="mt-muted"><%#: GlobalRateDetail %> · <%#: GlobalChipRef %></span>
                            <div class="summary-glances">
                                <asp:Repeater ID="RptGlance" runat="server">
                                    <ItemTemplate>
                                        <div class="summary-glance">
                                            <span class="summary-glance-name" title='<%#: Eval("GuideTitle") %>'><%#: Eval("GuideName") %></span>
                                            <span class="summary-glance-rate"><%#: Eval("PassRateText") %></span>
                                            <span class='<%#: Eval("VerdictCss") %>'><i class='<%#: Eval("VerdictIcon") %>' aria-hidden="true"></i><%#: Eval("VerdictText") %></span>
                                        </div>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </div>
                        </asp:PlaceHolder>
                        <asp:Label ID="LblNoStates" runat="server" CssClass="summary-empty" Visible="false"
                            Text="Todavía no hay sesiones de entrenamiento." />
                        <div class="summary-s1-foot">
                            <asp:HyperLink ID="LnkFollowUp" runat="server" CssClass="summary-alert-link"><i class="bi bi-exclamation-triangle" aria-hidden="true"></i>Ver seguimiento</asp:HyperLink>
                        </div>
                    </div>
                </div>
            </asp:PlaceHolder>
        </div>

        <%-- 2 · Por guía: la barra de estados, las tres cifras del período y su cambio. --%>
        <div class="summary-slide" id="sumSlide2" role="tabpanel" aria-labelledby="sumTab2">
            <div>
                <span class="summary-eyebrow">2 · Por guía</span>
                <h2 class="summary-h2">El detalle de cada guía</h2>
                <p class="mt-muted mb-0"><asp:Literal ID="LitReadyPeriod" runat="server" /></p>
            </div>
            <asp:PlaceHolder ID="PhStates" runat="server">
                <div class="summary-gcards">
                    <asp:Repeater ID="RptGuides" runat="server">
                        <ItemTemplate>
                            <article class="summary-gcard">
                                <div class="summary-gcard-head">
                                    <div class="summary-gcard-name">
                                        <span class="summary-gcard-title" title='<%#: Eval("GuideTitle") %>'><%#: Eval("GuideName") %></span>
                                        <span class="mt-muted mt-small"><%#: Eval("TotalText") %></span>
                                    </div>
                                    <span class='<%#: Eval("VerdictCss") %>'><i class='<%#: Eval("VerdictIcon") %>' aria-hidden="true"></i><%#: Eval("VerdictText") %></span>
                                </div>
                                <div class="summary-stack" role="img" aria-label='<%#: Eval("StatesAria") %>'>
                                    <div class="summary-seg-ok" style='width: <%#: Eval("PassedWidth") %>%'></div>
                                    <div class="summary-seg-bad" style='width: <%#: Eval("FailedWidth") %>%'></div>
                                    <div class="summary-seg-warn" style='width: <%#: Eval("InTrainingWidth") %>%'></div>
                                </div>
                                <div class="summary-counts">
                                    <span><i class="bi bi-check-circle" aria-hidden="true"></i><strong><%#: Eval("Passed") %></strong> <span class="mt-muted">aprobados</span></span>
                                    <span><i class="bi bi-x-circle" aria-hidden="true"></i><strong><%#: Eval("Failed") %></strong> <span class="mt-muted">reprobados</span></span>
                                    <span><i class="bi bi-hourglass-split" aria-hidden="true"></i><strong><%#: Eval("InTraining") %></strong> <span class="mt-muted">en entrenamiento</span></span>
                                </div>
                                <div class="summary-gfigs">
                                    <div class="mt-stat">
                                        <span class="mt-stat-label">Aprobados</span>
                                        <span class="summary-figure"><%#: Eval("PassRateText") %></span>
                                        <span class="mt-stat-detail"><%#: Eval("PassRateDetail") %></span>
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
                                <asp:PlaceHolder runat="server" Visible='<%# (bool)Eval("PassRateChange.Visible") %>'>
                                    <div>
                                        <span class='mt-change <%#: Eval("PassRateChange.Css") %>'><i class='<%#: Eval("PassRateChange.PillIcon") %>' aria-hidden="true"></i> <%#: Eval("PassRateChange.Full") %></span>
                                        <span class="mt-change-ref"><%#: Eval("PassRateChange.Reference") %> · <%#: Eval("PassRateChange.Base") %></span>
                                    </div>
                                </asp:PlaceHolder>
                            </article>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>
                <p class="summary-note">
                    Aprobados: de los operadores con algún Assessment completado en el período, los que
                    aprobaron alguno. Al primer intento: de los operadores cuyo primer Assessment de la
                    guía cae en el período, los que lo aprobaron. "—" = sin operadores para calcularla.
                </p>
            </asp:PlaceHolder>
        </div>

        <%-- 3 · Dónde se falla: top 5 de pasos en Assessment, del período; el principio de Pasos
             (D-147). El n.º 1 en grande, con su anillo. --%>
        <div class="summary-slide" id="sumSlide3" role="tabpanel" aria-labelledby="sumTab3">
            <div>
                <span class="summary-eyebrow">3 · Assessment, <asp:Literal ID="LitTopPeriod" runat="server" /></span>
                <h2 class="summary-h2">¿Dónde se falla más?</h2>
            </div>
            <asp:PlaceHolder ID="PhTop" runat="server">
                <ol class="summary-top">
                    <asp:Repeater ID="RptTop" runat="server">
                        <ItemTemplate>
                            <asp:PlaceHolder runat="server" Visible='<%# Container.ItemIndex == 0 %>'>
                                <li class="summary-top1">
                                    <div class="summary-ring summary-ring-fill" role="img" aria-label='<%#: Eval("FailureRateText") %> con fallo' style='--ring-fill: <%#: Eval("RingFill") %>%'>
                                        <div class="summary-ring-hole"><span class="summary-ring-big"><%#: Eval("FailureRateText") %></span><span class="summary-ring-cap">con fallo</span></div>
                                    </div>
                                    <div class="summary-top1-body">
                                        <span class="summary-top1-tag">Paso n.º 1</span>
                                        <span class="summary-top1-name"><%#: Eval("StepName") %></span>
                                        <span class="summary-top1-detail"><%#: Eval("Detail") %></span>
                                        <asp:PlaceHolder runat="server" Visible='<%# (bool)Eval("FailureRateChange.Visible") %>'>
                                            <span class="summary-top1-change">
                                                <span class='mt-change <%#: Eval("FailureRateChange.Css") %>'><i class='<%#: Eval("FailureRateChange.PillIcon") %>' aria-hidden="true"></i> <%#: Eval("FailureRateChange.Full") %></span>
                                                <span class="mt-change-ref"><%#: Eval("FailureRateChange.Reference") %> · <%#: Eval("FailureRateChange.Base") %></span>
                                            </span>
                                        </asp:PlaceHolder>
                                    </div>
                                </li>
                            </asp:PlaceHolder>
                            <asp:PlaceHolder runat="server" Visible='<%# Container.ItemIndex > 0 %>'>
                                <li class="summary-top-row" title='<%#: Eval("Detail") %>'>
                                    <span class="summary-top-rank"><%# Container.ItemIndex + 1 %></span>
                                    <span class="summary-top-name"><%#: Eval("StepName") %></span>
                                    <div class="mt-track summary-top-track" aria-hidden="true"><div class="mt-fill" style='width: <%#: Eval("FailureWidth") %>%'></div></div>
                                    <span class="summary-top-rate"><%#: Eval("FailureRateText") %></span>
                                    <asp:PlaceHolder runat="server" Visible='<%# (bool)Eval("FailureRateChange.Visible") %>'>
                                        <span class='mt-change summary-top-change <%#: Eval("FailureRateChange.Css") %>'><%#: Eval("FailureRateChange.Full") %></span>
                                    </asp:PlaceHolder>
                                </li>
                            </asp:PlaceHolder>
                        </ItemTemplate>
                    </asp:Repeater>
                </ol>
            </asp:PlaceHolder>
            <asp:Label ID="LblNoTop" runat="server" CssClass="summary-empty" Visible="false"
                Text="Sin datos para este período. Pruebe con &quot;Todo&quot; o con otra guía." />
            <div>
                <asp:HyperLink ID="LnkSteps" runat="server" CssClass="summary-more">Ver todos los pasos<i class="bi bi-arrow-right" aria-hidden="true"></i></asp:HyperLink>
            </div>
        </div>

        <%-- 4 · Actividad: sesiones por día, semana o mes y modo, del período, con el total y su
             cambio (P-11), en un libro de barras horizontales. --%>
        <div class="summary-slide" id="sumSlide4" role="tabpanel" aria-labelledby="sumTab4">
            <div class="summary-s4-head">
                <div>
                    <span class="summary-eyebrow">4 · <%: PeriodText %></span>
                    <h2 class="summary-h2">Actividad</h2>
                    <p class="mt-muted mb-0"><asp:Literal ID="LitActivityPeriod" runat="server" /></p>
                </div>
                <asp:PlaceHolder ID="PhSessions" runat="server">
                    <div class="summary-sessions">
                        <span class="summary-sessions-n"><%#: SessionsText %></span>
                        <span class="mt-muted">sesiones</span>
                        <asp:PlaceHolder runat="server" Visible="<%# SessionsChange.Visible %>">
                            <span class='mt-change <%#: SessionsChange.Css %>'><i class='<%#: SessionsChange.PillIcon %>' aria-hidden="true"></i> <%#: SessionsChange.Full %></span>
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
                <%-- Con muchas filas (semanas de un año, meses de Todo) el libro se desplaza dentro del escenario. --%>
                <div class="summary-ledger-scroll">
                    <div class="summary-ledger" style="height: <%: LedgerHeight %>px"><canvas id="chartActivity" aria-label="<%: ActivityAria %>" role="img"></canvas></div>
                </div>
                <p class="summary-note"><asp:Literal ID="LitActivityTotals" runat="server" /></p>
            </asp:PlaceHolder>
            <asp:Label ID="LblNoActivity" runat="server" CssClass="summary-empty" Visible="false"
                Text="Sin datos para este período. Pruebe con &quot;Todo&quot; o con otra guía." />
        </div>
    </section>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ScriptContent" runat="server">
    <script src="<%=ResolveUrl("~/Scripts/chart-4.5.1.umd.min.js")%>"></script>
    <script src="<%=ResolveUrl("~/UI/master/admin-charts.js")%>"></script>
    <script>
        // Actividad del servidor (JavaScriptSerializer), por día, semana o mes (spec 023, P-9).
        // Los modos van en orden de exigencia con los tres azules de los tokens (--mt-m1,
        // --mt-m2, --mt-m3). Libro de barras horizontales: una fila por día, semana o mes, apilada
        // por modo, con el total al final de cada barra (spec 022, E8b).
        var summaryData = <%= ChartJson %>;
        var summaryLedger = null;

        (function () {
            var weeks = summaryData && summaryData.activity;
            if (!weeks || typeof mtCharts === 'undefined') return;

            var totals = weeks.labels.map(function (_, i) {
                return weeks.guided[i] + weeks.practice[i] + weeks.assessment[i];
            });

            summaryLedger = mtCharts.create(document.getElementById('chartActivity'), function (t) {
                var totalLabels = {
                    id: 'totals',
                    afterDatasetsDraw: function (chart) {
                        var ctx = chart.ctx, meta = chart.getDatasetMeta(chart.data.datasets.length - 1);
                        ctx.save();
                        ctx.fillStyle = t.text;
                        ctx.font = '600 13px ' + t.font;
                        ctx.textAlign = 'left';
                        ctx.textBaseline = 'middle';
                        meta.data.forEach(function (bar, i) {
                            if (totals[i] > 0) ctx.fillText(totals[i], bar.x + 6, bar.y);
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
                        indexAxis: 'y',
                        maintainAspectRatio: false,
                        // Barras horizontales: el índice se busca por la fila (eje y), no por x.
                        interaction: { mode: 'index', axis: 'y', intersect: false },
                        layout: { padding: { right: 36 } },
                        scales: mtCharts.axes(t, { stacked: true, horizontal: true }),
                        plugins: {
                            legend: { display: false },
                            tooltip: {
                                mode: 'index', axis: 'y', intersect: false,
                                callbacks: { title: function (items) { return weeks.titlePrefix + items[0].label; } }
                            }
                        }
                    },
                    plugins: [totalLabels]
                };
            });
        })();

        // Los cuatro pasos del escenario (spec 022, E8b): solo cliente, sin postback ni estado.
        // Sin JavaScript se ven los cuatro apilados; con él entra siempre en el paso 1 (también
        // tras Filtrar). Pestañas, Anterior / Siguiente y las flechas ← / → (fuera de los campos).
        (function () {
            var bar = document.getElementById('sumBar');
            var prev = document.getElementById('sumPrev');
            var next = document.getElementById('sumNext');
            var tabs = [], slides = [];
            for (var n = 1; n <= 4; n++) {
                tabs.push(document.getElementById('sumTab' + n));
                slides.push(document.getElementById('sumSlide' + n));
            }
            if (!bar || !prev || !next || tabs.indexOf(null) >= 0 || slides.indexOf(null) >= 0) return;

            var current = 0;
            function show(index, focusTab) {
                current = Math.max(0, Math.min(tabs.length - 1, index));
                for (var k = 0; k < tabs.length; k++) {
                    var on = k === current;
                    tabs[k].setAttribute('aria-selected', on ? 'true' : 'false');
                    tabs[k].tabIndex = on ? 0 : -1;
                    slides[k].hidden = !on;
                }
                if (focusTab) tabs[current].focus();
                // Chart.js mide 0 en un contenedor oculto: se ajusta al mostrar la actividad.
                if (current === 3 && summaryLedger && summaryLedger.chart) summaryLedger.chart.resize();
            }

            tabs.forEach(function (tab, k) { tab.addEventListener('click', function () { show(k, false); }); });
            prev.addEventListener('click', function () { show(current - 1, false); });
            next.addEventListener('click', function () { show(current + 1, false); });
            document.addEventListener('keydown', function (e) {
                if (e.key !== 'ArrowLeft' && e.key !== 'ArrowRight') return;
                if (e.altKey || e.ctrlKey || e.metaKey || e.shiftKey) return;
                var el = e.target, tag = el && el.tagName;
                if (tag === 'INPUT' || tag === 'SELECT' || tag === 'TEXTAREA' || (el && el.isContentEditable)) return;
                e.preventDefault();
                show(current + (e.key === 'ArrowRight' ? 1 : -1), true);
            });

            bar.hidden = false;
            show(0, false);
        })();
    </script>
</asp:Content>
