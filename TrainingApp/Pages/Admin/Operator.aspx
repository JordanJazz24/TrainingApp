<%@ Page Title="Operador" Language="C#" MasterPageFile="~/UI/master/Admin.Master"
    AutoEventWireup="true" CodeBehind="Operator.aspx.cs"
    Inherits="TrainingApp.Pages.Admin.Operator" %>

<%-- Detalle de un Operador (spec 018, AC-14; por área, spec 024; diseño de la spec 022, brief 7,
     alternativa H "Tres preguntas con historial por guía"). Arriba, tres preguntas: ¿está listo en
     su área? (su estado y qué hacer), ¿qué le falla? (los pasos a reforzar, su área primero) y
     ¿qué ha hecho? (sus sesiones por modo). Debajo, un bloque plegable por guía con su curva
     (Chart.js: puntaje de Assessment arriba, intentos fallidos abajo), su lectura y sus sesiones.
     Un id inexistente, de un administrador o manipulado vuelve a Operadores con un aviso, sin
     datos. Solo lectura: desplegar se resuelve en el cliente, sin postback.
     Ficha imprimible (AC-16, D-150): la misma página impresa desde el navegador ("Guardar como
     PDF"), siempre en claro (D-157). Al imprimir se ocultan la barra, los botones y el historial
     con las curvas (d-print-none y admin.css → @media print); salen todos los pasos a reforzar,
     "También entrenó" (D-270) y la tabla única de Sesiones con su Área. --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <asp:HyperLink ID="LnkBack" runat="server" CssClass="btn btn-link btn-sm px-0 mb-2 d-print-none"><i class="bi bi-chevron-left" aria-hidden="true"></i>Operadores</asp:HyperLink>
    <p class="d-none d-print-block mt-small mt-muted mb-2">
        Ficha de entrenamiento, impresa el <asp:Literal ID="LitPrinted" runat="server" />. Simulador de
        entrenamiento MES: no es un registro oficial de entrenamiento.
    </p>
    <div class="operator-head">
        <div>
            <h1 class="admin-page-title"><asp:Literal ID="LitName" runat="server" /></h1>
            <%-- Su área (spec 021): también en la ficha impresa. --%>
            <p class="operator-head-sub">
                <span>Code1 <asp:Literal ID="LitCode1" runat="server" /></span>
                <span class="operator-tag<%= HasArea ? " is-mine" : "" %>">Área · <asp:Literal ID="LitArea" runat="server" /></span>
            </p>
        </div>
        <button type="button" class="btn btn-secondary d-print-none" onclick="window.print();"><i class="bi bi-printer" aria-hidden="true"></i>Imprimir ficha</button>
    </div>

    <div class="operator-qs">
        <%-- ¿Está listo en su área? (spec 024, D-269): sus guías, también Sin iniciar, con la línea
             de qué hacer de Operadores. Sin área o sin guía publicada, el texto (D-267, P-5). --%>
        <section class="mt-hero operator-q" aria-labelledby="qReady">
            <h2 id="qReady" class="operator-q-title">¿Está listo en su área?<small>Estado de la guía de su área</small></h2>
            <asp:Repeater ID="RptGuides" runat="server">
                <ItemTemplate>
                    <div class="operator-q-guide">
                        <span class='<%#: Eval("StateCss") %> mt-badge-lg'><i class='<%#: Eval("StateIcon") %>' aria-hidden="true"></i><%#: Eval("StateText") %></span>
                        <div class="operator-q-name"><%#: Eval("GuideName") %><small><%#: Eval("GuideTitle") %></small></div>
                        <div class="operator-q-pair">
                            <div><span>Mejor puntaje</span><b><%#: Eval("BestText") %></b></div>
                            <div><span>Último puntaje</span><b><%#: Eval("LastText") %></b></div>
                        </div>
                        <p class="operator-q-note">Última actividad <%#: Eval("LastActivityText") %><%#: string.IsNullOrEmpty((string)Eval("AgoText")) ? "" : " · " + Eval("AgoText") %>.<%#: string.IsNullOrEmpty((string)Eval("GapText")) ? "" : " " + Eval("GapText") + "." %></p>
                        <p class="operator-q-do"><i class='<%#: Eval("HintIcon") %> <%#: Eval("HintTone") %>' aria-hidden="true"></i><span><b class='<%#: Eval("HintTone") %>'><%#: Eval("HintTitle") %></b> <%#: Eval("HintText") %></span></p>
                    </div>
                </ItemTemplate>
            </asp:Repeater>
            <asp:PlaceHolder ID="PhAreaNote" runat="server" Visible="false">
                <div class="operator-q-guide">
                    <span class="mt-badge mt-badge-idle mt-badge-lg"><i class="bi bi-geo-alt" aria-hidden="true"></i><asp:Literal ID="LitAreaNote" runat="server" /></span>
                </div>
                <asp:PlaceHolder ID="PhAreaHint" runat="server">
                    <p class="operator-q-do"><i class="bi bi-geo-alt operators-tone-none" aria-hidden="true"></i><span><b class="operators-tone-none"><asp:Literal ID="LitAreaHintTitle" runat="server" /></b> <asp:Literal ID="LitAreaHintText" runat="server" /></span></p>
                </asp:PlaceHolder>
            </asp:PlaceHolder>
        </section>

        <%-- ¿Qué le falla?: los pasos a reforzar de sus Assessment, primero su área (D-269). Los
             de otras áreas se despliegan en el cliente; la ficha los imprime todos. --%>
        <section class="mt-hero operator-q operator-q-wide" aria-labelledby="qFails">
            <h2 id="qFails" class="operator-q-title">¿Qué le falla?<small>Pasos de sus Assessment sin acierto o con 2 o más intentos fallidos, hasta 5 por guía</small></h2>
            <asp:Repeater ID="RptReinforcement" runat="server">
                <ItemTemplate>
                    <div class='operator-q-group<%# (bool)Eval("Collapsed") ? " operator-q-other" : "" %>'<%# (bool)Eval("Collapsed") ? " hidden" : "" %>>
                        <div class="operator-q-group-head"><h3 class="mt-block-title"><%#: Eval("GuideName") %></h3><span class='<%#: Eval("TagCss") %>'><%#: Eval("Tag") %></span></div>
                        <asp:Repeater runat="server" DataSource='<%# Eval("Steps") %>'>
                            <ItemTemplate>
                                <div class="operator-q-step">
                                    <span class="operator-q-step-name"><%#: Eval("StepName") %></span>
                                    <span class="operator-q-step-fails"><b><%#: Eval("FailedAttempts") %></b><span>intentos fallidos</span></span>
                                    <span class="operator-q-step-why"><%#: Eval("MainReasonText") %> · <%#: Eval("TimesWithoutSuccess") %> veces sin acierto</span>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                </ItemTemplate>
            </asp:Repeater>
            <asp:Panel ID="PnlNoReinforcement" runat="server" CssClass="operator-reinforce-none" Visible="false">
                <i class="bi bi-check-circle" aria-hidden="true"></i>Ningún paso a reforzar en sus Assessment.
            </asp:Panel>
            <asp:PlaceHolder ID="PhOtherSteps" runat="server" Visible="false">
                <button type="button" id="operatorOtherSteps" class="btn btn-secondary btn-sm align-self-start d-print-none" aria-expanded="false"
                    data-show="<%: OtherStepsShowText %>" data-hide="Ocultar las otras áreas"><%: OtherStepsShowText %></button>
            </asp:PlaceHolder>
        </section>

        <%-- ¿Qué ha hecho?: todas sus sesiones por modo, con los colores de la Actividad del
             Resumen (métricas derivadas, OperatorActivity). --%>
        <section class="mt-hero operator-q" aria-labelledby="qDone">
            <h2 id="qDone" class="operator-q-title">¿Qué ha hecho?<small>Todas sus sesiones, de todas las áreas</small></h2>
            <div>
                <div class="operator-q-total"><asp:Literal ID="LitSessions" runat="server" /></div>
                <asp:PlaceHolder ID="PhLastSession" runat="server"><p class="operator-q-note">Última: <asp:Literal ID="LitLastSession" runat="server" /></p></asp:PlaceHolder>
            </div>
            <asp:PlaceHolder ID="PhMix" runat="server">
                <div class="operator-mix" role="img" aria-label="<%: MixLabel %>">
                    <span class="summary-mode-1" style="flex: <%= GuidedCount %> 1 0"></span><span class="summary-mode-2" style="flex: <%= PracticeCount %> 1 0"></span><span class="summary-mode-3" style="flex: <%= AssessmentCount %> 1 0"></span>
                </div>
            </asp:PlaceHolder>
            <ul class="operator-mix-counts">
                <li><span><span class="summary-dot summary-mode-1"></span>Guided</span><b><%= GuidedCount %></b></li>
                <li><span><span class="summary-dot summary-mode-2"></span>Práctica libre</span><b><%= PracticeCount %></b></li>
                <li><span><span class="summary-dot summary-mode-3"></span>Assessment</span><b><%= AssessmentCount %></b></li>
            </ul>
            <%-- Otras áreas (entrenamiento cruzado) en una línea, también en la ficha (D-270). --%>
            <asp:PlaceHolder ID="PhAlsoTrained" runat="server">
                <p class="operator-q-note operator-q-also"><asp:Literal ID="LitAlsoTrained" runat="server" /></p>
            </asp:PlaceHolder>
        </section>
    </div>

    <%-- Historial por guía: su área abierta y las otras plegadas (abiertas sin área, P-8). La
         curva de un bloque plegado se dibuja al abrirlo (Chart.js no mide un canvas oculto). No va
         en la ficha impresa. --%>
    <asp:PlaceHolder ID="PhHistory" runat="server">
        <section class="d-print-none" aria-labelledby="historyTitle">
            <div class="operator-history-head">
                <div>
                    <h2 id="historyTitle" class="mt-section-title">Historial por guía</h2>
                    <p class="mt-muted mt-small mb-0">Cada guía reúne su curva y sus sesiones. Su área primero; las otras áreas (entrenamiento cruzado) vienen plegadas.</p>
                </div>
                <div class="summary-legend summary-legend-sm">
                    <span><span class="summary-dot summary-mode-2"></span>Práctica libre</span>
                    <span><span class="summary-dot summary-mode-3"></span>Assessment</span>
                    <span><span class="summary-dot summary-mode-1"></span>Guided (solo en la tabla)</span>
                </div>
            </div>
            <asp:Repeater ID="RptHistory" runat="server">
                <ItemTemplate>
                    <details class="mt-hero operator-hist" data-curve='<%# Eval("CurveIndex") %>'<%# (bool)Eval("Open") ? " open" : "" %>>
                        <summary class="operator-hist-sum">
                            <i class="bi bi-chevron-right operator-hist-chev" aria-hidden="true"></i>
                            <span class="operator-hist-name"><%#: Eval("AreaText") %><small><%#: Eval("GuideTitle") %></small></span>
                            <span class="operator-hist-figs">
                                <span><span>Estado</span><span class='<%#: Eval("StateCss") %>'><i class='<%#: Eval("StateIcon") %>' aria-hidden="true"></i><%#: Eval("StateText") %></span></span>
                                <span><span>Mejor</span><b><%#: Eval("BestText") %></b></span>
                                <span><span>Último</span><b><%#: Eval("LastText") %></b></span>
                                <span><span>Sesiones</span><b><%#: Eval("Sessions") %></b></span>
                            </span>
                            <span class="operator-hist-act"><span class='<%#: Eval("TagCss") %>'><%#: Eval("Tag") %></span><span><%#: Eval("LastActivityText") %></span><span><%#: Eval("AgoText") %></span></span>
                        </summary>
                        <div class="operator-hist-body">
                            <div class="operator-hist-chart">
                                <asp:PlaceHolder runat="server" Visible='<%# (bool)Eval("HasPoints") %>'>
                                    <canvas id='curve-<%# Eval("CurveIndex") %>' role="img"
                                        aria-label='<%#: "Evolución de " + Eval("AreaText") + ": puntaje de cada Assessment e intentos fallidos de cada sesión" %>'></canvas>
                                </asp:PlaceHolder>
                                <asp:PlaceHolder runat="server" Visible='<%# !(bool)Eval("HasPoints") %>'>
                                    <p class="mt-muted mt-small mb-0">Todavía no tiene sesiones de Práctica libre ni Assessment: Guided no se grafica.</p>
                                </asp:PlaceHolder>
                            </div>
                            <div class="operator-hist-read">
                                <p class="operator-hist-practice"><%#: Eval("PracticeText") %></p>
                                <asp:PlaceHolder runat="server" Visible='<%# !string.IsNullOrEmpty((string)Eval("ScoresText")) %>'>
                                    <p class="mt-small mb-0"><%#: Eval("ScoresText") %></p>
                                </asp:PlaceHolder>
                                <dl class="operator-hist-facts">
                                    <div><dt>Assessment</dt><dd><%#: Eval("Assessments") %></dd></div>
                                    <div><dt>aprobados</dt><dd><%#: Eval("PassedAssessments") %></dd></div>
                                    <div><dt>intentos fallidos</dt><dd><%#: Eval("FailedAttempts") %></dd></div>
                                </dl>
                            </div>
                        </div>
                        <div class="mt-table-wrap operator-hist-table">
                            <table class="table mt-table">
                                <thead>
                                    <tr>
                                        <th scope="col">Inicio</th>
                                        <th scope="col">Modo</th>
                                        <th scope="col">Estado</th>
                                        <th scope="col" class="mt-num">Puntaje</th>
                                        <th scope="col" class="mt-num mt-col-secondary">Duración</th>
                                        <th scope="col" class="mt-num">Intentos fallidos</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <asp:Repeater runat="server" DataSource='<%# Eval("Rows") %>'>
                                        <ItemTemplate>
                                            <tr>
                                                <td class="text-nowrap"><span class="operator-day"><%#: Eval("DayText") %></span><span class="operator-time"><%#: Eval("TimeText") %></span></td>
                                                <td><span class="operator-mode"><span class='<%#: Eval("ModeCss") %>'></span><%#: Eval("ModeText") %></span></td>
                                                <td><span class="operator-status"><i class='<%#: Eval("StatusIcon") %>' aria-hidden="true"></i><%#: Eval("StatusText") %></span></td>
                                                <td class="mt-num">
                                                    <asp:PlaceHolder runat="server" Visible='<%# (bool)Eval("HasScore") %>'>
                                                        <span class="operator-score">
                                                            <span class="operator-score-value"><%#: Eval("ScoreText") %><asp:PlaceHolder runat="server" Visible='<%# (bool)Eval("IsBest") %>'><span class="operator-tag is-mine">Mejor</span></asp:PlaceHolder></span>
                                                            <span class="operator-meter" aria-hidden="true"><i class='<%# (bool)Eval("Passed") ? "is-pass" : "" %>' style='width: <%# Eval("ScoreWidth") %>%'></i></span>
                                                            <span class='operator-verdict <%# (bool)Eval("Passed") ? "is-pass" : "is-fail" %>'><%# (bool)Eval("Passed") ? "Aprobó" : "Sin aprobar" %></span>
                                                        </span>
                                                    </asp:PlaceHolder>
                                                    <asp:PlaceHolder runat="server" Visible='<%# !(bool)Eval("HasScore") %>'><span class="mt-muted"><%#: Eval("ScoreText") %></span></asp:PlaceHolder>
                                                </td>
                                                <td class="mt-num mt-col-secondary text-nowrap"><%#: Eval("DurationText") %></td>
                                                <td class="mt-num"><span class="operator-fails"><%#: Eval("FailedAttempts") %><i style='width: <%# Eval("FailWidth") %>px' aria-hidden="true"></i></span></td>
                                            </tr>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </tbody>
                            </table>
                        </div>
                    </details>
                </ItemTemplate>
            </asp:Repeater>
        </section>
    </asp:PlaceHolder>
    <asp:Label ID="LblNoSessions" runat="server" CssClass="mt-empty" Visible="false"
        Text="Todavía no tiene sesiones de entrenamiento." />

    <%-- Ficha impresa: la tabla única de Sesiones, de todas las áreas, con su Área (spec 024). --%>
    <asp:PlaceHolder ID="PhPrintSessions" runat="server">
        <section class="d-none d-print-block operator-print-sessions" aria-labelledby="printSessionsTitle">
            <h2 id="printSessionsTitle" class="mt-section-title">Sesiones</h2>
            <table class="table mt-table">
                <thead>
                    <tr>
                        <th scope="col">Inicio</th>
                        <th scope="col">Área</th>
                        <th scope="col">Modo</th>
                        <th scope="col">Estado</th>
                        <th scope="col" class="mt-num">Puntaje</th>
                        <th scope="col" class="mt-num">Duración</th>
                        <th scope="col" class="mt-num">Intentos fallidos</th>
                    </tr>
                </thead>
                <tbody>
                    <asp:Repeater ID="RptSessions" runat="server">
                        <ItemTemplate>
                            <tr>
                                <td class="text-nowrap"><%#: Eval("StartedText") %></td>
                                <td><%#: Eval("AreaText") %></td>
                                <td><%#: Eval("ModeText") %></td>
                                <td><%#: Eval("StatusText") %></td>
                                <td class="mt-num fw-semibold"><%#: Eval("ScoreText") %></td>
                                <td class="mt-num text-nowrap"><%#: Eval("DurationText") %></td>
                                <td class="mt-num"><%#: Eval("FailedAttempts") %></td>
                            </tr>
                        </ItemTemplate>
                    </asp:Repeater>
                </tbody>
            </table>
        </section>
    </asp:PlaceHolder>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ScriptContent" runat="server">
    <script src="<%=ResolveUrl("~/Scripts/chart-4.5.1.umd.min.js")%>"></script>
    <script src="<%=ResolveUrl("~/UI/master/admin-charts.js")%>"></script>
    <script>
        // Curvas del servidor (JavaScriptSerializer), una por canvas curve-<i>: arriba el puntaje
        // de cada Assessment con el mínimo para aprobar, abajo los intentos fallidos de cada
        // sesión (Práctica libre --mt-m2, Assessment --mt-m3), alineados por sesión.
        var curveData = <%= ChartJson %>;

        (function () {
            // "Ver también otras áreas": muestra u oculta los grupos de otras áreas, sin postback.
            var more = document.getElementById('operatorOtherSteps');
            if (more) {
                more.addEventListener('click', function () {
                    var open = more.getAttribute('aria-expanded') !== 'true';
                    more.setAttribute('aria-expanded', open ? 'true' : 'false');
                    more.textContent = open ? more.getAttribute('data-hide') : more.getAttribute('data-show');
                    document.querySelectorAll('.operator-q-other').forEach(function (el) { el.hidden = !open; });
                });
            }

            if (!curveData || typeof mtCharts === 'undefined') return;

            function draw(i) {
                var curve = curveData[i];
                var canvas = document.getElementById('curve-' + i);
                if (!curve || !canvas) return;
                mtCharts.create(canvas, function (t) {
                    var practice = mtCharts.bars(t, 'Práctica libre', curve.practice, t.m2);
                    var assessment = mtCharts.bars(t, 'Assessment', curve.assessment, t.m3);
                    [practice, assessment].forEach(function (d) {
                        d.yAxisID = 'fails';
                        d.skipNull = true;
                        d.maxBarThickness = 28;
                        d.order = 2;
                        d.mtLabels = d.data.map(function (v) { return v === null ? '' : String(v); });
                    });
                    var score = {
                        type: 'line', label: 'Puntaje', data: curve.scores, yAxisID: 'score', order: 0,
                        spanGaps: true, borderColor: t.text, borderWidth: 2.5, tension: 0,
                        pointRadius: 6, pointHoverRadius: 7,
                        pointBackgroundColor: curve.passed.map(function (p) { return p ? t.ok : t.surface; }),
                        pointBorderColor: curve.passed.map(function (p) { return p ? t.surface : t.text; }),
                        pointBorderWidth: curve.passed.map(function (p) { return p ? 2 : 3; }),
                        mtLabels: curve.scores.map(function (v, k) { return v === null ? '' : v.toFixed(1) + '%' + (curve.passed[k] ? ' ✓' : ''); })
                    };
                    var minimum = {
                        type: 'line', label: 'Mínimo para aprobar', data: curve.labels.map(function () { return curve.passing; }),
                        yAxisID: 'score', order: 1, borderColor: t.ok, borderWidth: 1.5, borderDash: [6, 5],
                        pointRadius: 0, pointHoverRadius: 0
                    };
                    return {
                        type: 'bar',
                        data: { labels: curve.labels, datasets: [score, minimum, practice, assessment] },
                        options: {
                            maintainAspectRatio: false,
                            layout: { padding: { top: 18 } },
                            scales: {
                                x: { grid: { display: false }, border: { color: t.border } },
                                score: {
                                    type: 'linear', position: 'left', stack: 'curve', stackWeight: 1.2,
                                    min: 0, max: 100, grid: { color: t.border }, border: { display: false },
                                    ticks: { stepSize: 50, callback: function (v) { return v + '%'; } },
                                    title: { display: true, text: 'Puntaje' }
                                },
                                fails: {
                                    type: 'linear', position: 'left', stack: 'curve', stackWeight: 1,
                                    beginAtZero: true, grace: '15%', grid: { color: t.border }, border: { display: false },
                                    ticks: { precision: 0 },
                                    title: { display: true, text: 'Intentos fallidos' }
                                }
                            },
                            plugins: {
                                legend: { display: false },
                                tooltip: {
                                    filter: function (item) { return item.dataset.label !== 'Mínimo para aprobar'; },
                                    callbacks: {
                                        title: function (items) {
                                            var label = items.length ? curve.labels[items[0].dataIndex] : '';
                                            return Array.isArray(label) ? label.join(' · ') : label;
                                        },
                                        label: function (item) {
                                            if (item.dataset.yAxisID === 'score')
                                                return 'Puntaje: ' + item.parsed.y.toFixed(1) + '%' + (curve.passed[item.dataIndex] ? ' (aprobó)' : ' (no aprobó)');
                                            return item.dataset.label + ': ' + item.parsed.y + ' intentos fallidos';
                                        }
                                    }
                                }
                            }
                        },
                        plugins: [mtCharts.valueLabels(t)]
                    };
                });
            }

            // Su área abierta se dibuja al cargar; un bloque plegado, la primera vez que se abre.
            document.querySelectorAll('details.operator-hist').forEach(function (block) {
                var i = parseInt(block.getAttribute('data-curve'), 10), drawn = false;
                function show() {
                    if (drawn || !block.open) return;
                    drawn = true;
                    draw(i);
                }
                show();
                block.addEventListener('toggle', show);
            });
        })();
    </script>
</asp:Content>
