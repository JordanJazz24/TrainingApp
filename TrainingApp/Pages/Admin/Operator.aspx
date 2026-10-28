<%@ Page Title="Operador" Language="C#" MasterPageFile="~/UI/master/Admin.Master"
    AutoEventWireup="true" CodeBehind="Operator.aspx.cs"
    Inherits="TrainingApp.Pages.Admin.Operator" %>

<%-- Detalle de un Operador (spec 018, AC-14; diseño de la Fase 11, D-160; por área, spec 024):
     el estado en las guías de su área y, aparte, las de otras áreas que entrenó (D-269), pasos a
     reforzar en sus Assessment, curva de aprendizaje (AC-15) y sus
     sesiones, con horas de la planta. Un id inexistente, de un administrador o manipulado vuelve
     a Operadores con un aviso, sin datos. Solo lectura.
     Ficha imprimible (AC-16, D-150): la misma página impresa desde el navegador ("Guardar como
     PDF" para el PDF), siempre en claro (D-157). Al imprimir se ocultan la barra, los botones y
     la curva (d-print-none y admin.css → @media print), y aparece el encabezado de la ficha. Las
     otras áreas van en una línea, no en su desplegable (D-270). --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <asp:HyperLink ID="LnkBack" runat="server" CssClass="btn btn-link btn-sm px-0 mb-2 d-print-none"><i class="bi bi-chevron-left" aria-hidden="true"></i>Operadores</asp:HyperLink>
    <p class="d-none d-print-block mt-small mt-muted mb-2">
        Ficha de entrenamiento, impresa el <asp:Literal ID="LitPrinted" runat="server" />. Simulador de
        entrenamiento MES: no es un registro oficial de entrenamiento.
    </p>
    <div class="admin-page-head">
        <div>
            <h1 class="admin-page-title"><asp:Literal ID="LitName" runat="server" /></h1>
            <%-- Su área (spec 021): también en la ficha impresa. --%>
            <p class="admin-page-lead">Código <asp:Literal ID="LitCode1" runat="server" /> · Área <asp:Literal ID="LitArea" runat="server" /></p>
        </div>
        <button type="button" class="btn btn-secondary d-print-none" onclick="window.print();"><i class="bi bi-printer" aria-hidden="true"></i>Imprimir ficha</button>
    </div>

    <%-- Arriba: si está listo en su área y qué reforzar (spec 024, D-269). --%>
    <section class="mt-hero operator-top" aria-labelledby="guidesTitle">
        <h2 id="guidesTitle" class="mt-section-title">Su área</h2>
        <div class="operator-guides">
            <asp:Repeater ID="RptGuides" runat="server">
                <ItemTemplate>
                    <div class="operator-guide">
                        <span class="operator-guide-name" title='<%#: Eval("GuideTitle") %>'><%#: Eval("GuideName") %></span>
                        <span class='<%#: Eval("StateCss") %> mt-badge-lg'><i class='<%#: Eval("StateIcon") %>' aria-hidden="true"></i><%#: Eval("StateText") %></span>
                        <dl class="operator-guide-data">
                            <div><dt>Mejor puntaje</dt><dd><%#: Eval("BestText") %></dd></div>
                            <div><dt>Último puntaje</dt><dd><%#: Eval("LastText") %></dd></div>
                            <div><dt>Última actividad</dt><dd><%#: Eval("LastActivityText") %></dd></div>
                        </dl>
                    </div>
                </ItemTemplate>
            </asp:Repeater>
            <%-- Sin área o sin guía publicada en su área (D-267, P-5, P-8). --%>
            <asp:Label ID="LblAreaNote" runat="server" CssClass="mt-muted" Visible="false" />
        </div>

        <%-- Otras áreas (entrenamiento cruzado, D-269): solo las guías con sesiones, en un
             desplegable nativo cerrado; abierto si no tiene área (P-8, P-9). No se imprime: la
             ficha lleva la línea de abajo (D-270). --%>
        <asp:PlaceHolder ID="PhOtherGuides" runat="server">
            <details id="DetOtherGuides" runat="server" class="operator-other d-print-none">
                <summary class="mt-block-title">Otras áreas (entrenamiento cruzado)</summary>
                <div class="operator-guides">
                    <asp:Repeater ID="RptOtherGuides" runat="server">
                        <ItemTemplate>
                            <div class="operator-guide">
                                <span class="operator-guide-name" title='<%#: Eval("GuideTitle") %>'><%#: Eval("GuideName") %></span>
                                <span class='<%#: Eval("StateCss") %> mt-badge-lg'><i class='<%#: Eval("StateIcon") %>' aria-hidden="true"></i><%#: Eval("StateText") %></span>
                                <dl class="operator-guide-data">
                                    <div><dt>Mejor puntaje</dt><dd><%#: Eval("BestText") %></dd></div>
                                    <div><dt>Último puntaje</dt><dd><%#: Eval("LastText") %></dd></div>
                                    <div><dt>Última actividad</dt><dd><%#: Eval("LastActivityText") %></dd></div>
                                </dl>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>
            </details>
            <p class="d-none d-print-block mt-small mb-0"><asp:Literal ID="LitAlsoTrained" runat="server" /></p>
        </asp:PlaceHolder>

        <div class="operator-reinforce">
            <div>
                <h2 class="mt-section-title">Pasos a reforzar</h2>
                <p class="mt-muted mt-small mb-0">
                    En sus Assessment: pasos que terminaron sin acierto o con 2 o más intentos fallidos,
                    hasta 5 por guía.
                </p>
            </div>
            <asp:Repeater ID="RptReinforcement" runat="server">
                <ItemTemplate>
                    <div class="operator-reinforce-guide">
                        <h3 class="mt-block-title"><%#: Eval("GuideName") %></h3>
                        <div class="mt-table-wrap">
                            <table class="table mt-table">
                                <thead>
                                    <tr>
                                        <th scope="col">Paso</th>
                                        <th scope="col" class="mt-num">Veces sin acierto</th>
                                        <th scope="col" class="mt-num">Intentos fallidos</th>
                                        <th scope="col">Motivo más frecuente</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <asp:Repeater runat="server" DataSource='<%# Eval("Steps") %>'>
                                        <ItemTemplate>
                                            <tr>
                                                <td class="fw-semibold"><%#: Eval("StepName") %></td>
                                                <td class="mt-num"><%#: Eval("TimesWithoutSuccess") %></td>
                                                <td class="mt-num"><%#: Eval("FailedAttempts") %></td>
                                                <td><%#: Eval("MainReasonText") %></td>
                                            </tr>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </tbody>
                            </table>
                        </div>
                    </div>
                </ItemTemplate>
            </asp:Repeater>
            <asp:Panel ID="PnlNoReinforcement" runat="server" CssClass="operator-reinforce-none" Visible="false">
                <i class="bi bi-check-circle" aria-hidden="true"></i>Ningún paso a reforzar en sus Assessment.
            </asp:Panel>
        </div>
    </section>

    <%-- Curva de aprendizaje (AC-15): no va en la ficha impresa. --%>
    <asp:PlaceHolder ID="PhCurves" runat="server">
        <section class="d-print-none operator-section" aria-labelledby="curveTitle">
            <div class="operator-section-head">
                <div>
                    <h2 id="curveTitle" class="mt-section-title">Curva de aprendizaje</h2>
                    <p class="mt-muted mt-small mb-0">
                        Sus sesiones de Práctica libre y Assessment en orden, con los intentos fallidos de cada
                        una. Guided no se grafica: la guía evita los errores. El puntaje de cada Assessment
                        aparece al pasar el mouse.
                    </p>
                </div>
                <div class="summary-legend summary-legend-sm">
                    <span><span class="summary-dot summary-mode-2"></span>Práctica libre</span>
                    <span><span class="summary-dot summary-mode-3"></span>Assessment</span>
                </div>
            </div>
            <div class="operator-curves">
                <asp:Repeater ID="RptCurves" runat="server">
                    <ItemTemplate>
                        <div class="mt-panel mt-panel-body operator-curve">
                            <div>
                                <h3 class="mt-block-title" title='<%#: Eval("GuideTitle") %>'><%#: Eval("GuideName") %></h3>
                                <p class="mt-small mb-0"><%#: Eval("PracticeText") %></p>
                            </div>
                            <asp:PlaceHolder runat="server" Visible='<%# (bool)Eval("HasPoints") %>'>
                                <div class="summary-chart"><canvas id='curve-<%# Container.ItemIndex %>' role="img"
                                    aria-label='<%#: "Curva de aprendizaje de " + Eval("GuideName") %>'></canvas></div>
                                <p class="mt-muted mt-small mb-0"><%#: Eval("ScoresText") %></p>
                            </asp:PlaceHolder>
                            <asp:PlaceHolder runat="server" Visible='<%# !(bool)Eval("HasPoints") %>'>
                                <p class="mt-muted mt-small mb-0">Todavía no tiene sesiones de Práctica libre ni Assessment.</p>
                            </asp:PlaceHolder>
                        </div>
                    </ItemTemplate>
                </asp:Repeater>
            </div>
        </section>
    </asp:PlaceHolder>

    <section class="mt-panel operator-section" aria-labelledby="sessionsTitle">
        <div class="mt-panel-body pb-0">
            <h2 id="sessionsTitle" class="mt-section-title">Sesiones</h2>
        </div>
        <asp:PlaceHolder ID="PhSessions" runat="server">
            <div class="mt-table-wrap mt-3">
                <table class="table mt-table table-hover">
                    <thead>
                        <tr>
                            <th scope="col">Inicio</th>
                            <th scope="col">Área</th>
                            <th scope="col">Modo</th>
                            <th scope="col">Estado</th>
                            <th scope="col" class="mt-num">Puntaje</th>
                            <th scope="col" class="mt-num mt-col-secondary">Duración</th>
                            <th scope="col" class="mt-num">Intentos fallidos</th>
                        </tr>
                    </thead>
                    <tbody>
                        <asp:Repeater ID="RptSessions" runat="server">
                            <ItemTemplate>
                                <tr>
                                    <td class="text-nowrap"><%#: Eval("StartedText") %></td>
                                    <td title='<%#: Eval("GuideTitle") %>'><%#: Eval("AreaText") %></td>
                                    <td><%#: Eval("ModeText") %></td>
                                    <td><%#: Eval("StatusText") %></td>
                                    <td class="mt-num fw-semibold"><%#: Eval("ScoreText") %></td>
                                    <td class="mt-num mt-col-secondary text-nowrap"><%#: Eval("DurationText") %></td>
                                    <td class="mt-num"><%#: Eval("FailedAttempts") %></td>
                                </tr>
                            </ItemTemplate>
                        </asp:Repeater>
                    </tbody>
                </table>
            </div>
        </asp:PlaceHolder>
        <asp:Label ID="LblNoSessions" runat="server" CssClass="mt-empty" Visible="false"
            Text="Todavía no tiene sesiones de entrenamiento." />
    </section>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ScriptContent" runat="server">
    <script src="<%=ResolveUrl("~/Scripts/chart-4.5.1.umd.min.js")%>"></script>
    <script src="<%=ResolveUrl("~/UI/master/admin-charts.js")%>"></script>
    <script>
        // Curvas del servidor (JavaScriptSerializer), una por canvas curve-<i>. Los modos con sus
        // azules de los tokens, como en la Actividad del Resumen: Práctica libre --mt-m2 y
        // Assessment --mt-m3, una barra por sesión.
        var curveData = <%= ChartJson %>;

        (function () {
            if (!curveData || typeof mtCharts === 'undefined') return;

            curveData.forEach(function (curve, i) {
                if (!curve) return;
                mtCharts.create(document.getElementById('curve-' + i), function (t) {
                    var scales = mtCharts.axes(t);
                    scales.y.title = { display: true, text: 'Intentos fallidos' };
                    var practice = mtCharts.bars(t, 'Práctica libre', curve.practice, t.m2);
                    var assessment = mtCharts.bars(t, 'Assessment', curve.assessment, t.m3);
                    practice.skipNull = assessment.skipNull = true;
                    practice.maxBarThickness = assessment.maxBarThickness = 28;
                    return {
                        type: 'bar',
                        data: { labels: curve.labels, datasets: [practice, assessment] },
                        options: {
                            maintainAspectRatio: false,
                            scales: scales,
                            plugins: {
                                legend: { display: false },
                                tooltip: {
                                    callbacks: {
                                        label: function (item) { return item.dataset.label + ': ' + item.parsed.y + ' intentos fallidos'; },
                                        afterLabel: function (item) {
                                            var score = curve.scores[item.dataIndex];
                                            return score ? 'Puntaje: ' + score : '';
                                        }
                                    }
                                }
                            }
                        }
                    };
                });
            });
        })();
    </script>
</asp:Content>
