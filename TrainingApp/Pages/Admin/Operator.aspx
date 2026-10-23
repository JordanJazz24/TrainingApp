<%@ Page Title="Operador" Language="C#" MasterPageFile="~/UI/master/Admin.Master"
    AutoEventWireup="true" CodeBehind="Operator.aspx.cs"
    Inherits="TrainingApp.Pages.Admin.Operator" %>

<%-- Detalle de un Operador (spec 018, AC-14): estado por guía (ReadinessPolicy), pasos a
     reforzar en sus Assessment, curva de aprendizaje (AC-15) y sus sesiones, con horas de la
     planta. Un id inexistente, de un administrador o manipulado vuelve a Operadores con un
     aviso, sin datos. Solo lectura.
     Ficha imprimible (AC-16, D-150): la misma página impresa desde el navegador ("Guardar como
     PDF" para el PDF). Al imprimir se ocultan el menú, los botones y la curva (d-print-none y
     admin.css → @media print), y aparece el encabezado de la ficha. --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <asp:HyperLink ID="LnkBack" runat="server" CssClass="btn btn-link btn-sm px-0 mb-2 d-print-none">&laquo; Operadores</asp:HyperLink>
    <p class="d-none d-print-block small text-muted mb-2">
        Ficha de entrenamiento · <asp:Literal ID="LitPrinted" runat="server" /> · Simulador de entrenamiento MES.
        No es un registro oficial de entrenamiento.
    </p>
    <div class="d-flex justify-content-between align-items-start">
        <div>
            <h1 class="h3 mb-1"><asp:Literal ID="LitName" runat="server" /></h1>
            <p class="text-muted mb-4">Código <asp:Literal ID="LitCode1" runat="server" /></p>
        </div>
        <button type="button" class="btn btn-outline-secondary btn-sm d-print-none" onclick="window.print();">Imprimir ficha</button>
    </div>

    <h2 class="h5 mb-3">Estado por guía</h2>
    <div class="row g-3 mb-4">
        <asp:Repeater ID="RptGuides" runat="server">
            <ItemTemplate>
                <div class="col-md-4">
                    <div class="card h-100">
                        <div class="card-body">
                            <div class="d-flex justify-content-between align-items-start mb-2">
                                <h3 class="h6 mb-0" title='<%#: Eval("GuideTitle") %>'><%#: Eval("GuideName") %></h3>
                                <span class='<%#: Eval("StateCss") %>'><%#: Eval("StateText") %></span>
                            </div>
                            <div class="small text-muted">
                                Mejor puntaje: <%#: Eval("BestText") %><br />
                                Último puntaje: <%#: Eval("LastText") %><br />
                                Última actividad: <%#: Eval("LastActivityText") %>
                            </div>
                        </div>
                    </div>
                </div>
            </ItemTemplate>
        </asp:Repeater>
    </div>

    <h2 class="h5 mb-1">Pasos a reforzar</h2>
    <p class="small text-muted mb-3">
        En sus Assessment: pasos que terminaron sin acierto o con 2 o más intentos fallidos, hasta 5
        por guía.
    </p>
    <asp:Repeater ID="RptReinforcement" runat="server">
        <ItemTemplate>
            <h3 class="h6"><%#: Eval("GuideName") %></h3>
            <div class="table-responsive mb-3">
                <table class="table table-sm bg-white">
                    <thead>
                        <tr>
                            <th>Paso</th>
                            <th class="text-end">Veces sin acierto</th>
                            <th class="text-end">Intentos fallidos</th>
                            <th>Motivo más frecuente</th>
                        </tr>
                    </thead>
                    <tbody>
                        <asp:Repeater runat="server" DataSource='<%# Eval("Steps") %>'>
                            <ItemTemplate>
                                <tr>
                                    <td><%#: Eval("StepName") %></td>
                                    <td class="text-end"><%#: Eval("TimesWithoutSuccess") %></td>
                                    <td class="text-end"><%#: Eval("FailedAttempts") %></td>
                                    <td><%#: Eval("MainReasonText") %></td>
                                </tr>
                            </ItemTemplate>
                        </asp:Repeater>
                    </tbody>
                </table>
            </div>
        </ItemTemplate>
    </asp:Repeater>
    <asp:Label ID="LblNoReinforcement" runat="server" CssClass="text-muted d-block mb-4" Visible="false"
        Text="Sin pasos a reforzar en sus Assessment." />

    <%-- Curva de aprendizaje (AC-15): no va en la ficha impresa. --%>
    <asp:PlaceHolder ID="PhCurves" runat="server">
        <div class="d-print-none">
            <h2 class="h5 mb-1 mt-2">Curva de aprendizaje</h2>
            <p class="small text-muted mb-3">
                Sus sesiones de Práctica libre y Assessment en orden, con los intentos fallidos de cada una (Guided no
                se grafica: la guía evita los errores). El puntaje de cada Assessment aparece al pasar el mouse.
            </p>
            <div class="row g-3 mb-4">
                <asp:Repeater ID="RptCurves" runat="server">
                    <ItemTemplate>
                        <div class="col-lg-6">
                            <div class="card h-100">
                                <div class="card-body">
                                    <h3 class="h6 mb-1" title='<%#: Eval("GuideTitle") %>'><%#: Eval("GuideName") %></h3>
                                    <p class="small mb-2"><%#: Eval("PracticeText") %></p>
                                    <asp:PlaceHolder runat="server" Visible='<%# (bool)Eval("HasPoints") %>'>
                                        <div class="summary-chart"><canvas id='curve-<%# Container.ItemIndex %>' role="img"
                                            aria-label='<%#: "Curva de aprendizaje de " + Eval("GuideName") %>'></canvas></div>
                                        <p class="small text-muted mt-2 mb-0"><%#: Eval("ScoresText") %></p>
                                    </asp:PlaceHolder>
                                    <asp:PlaceHolder runat="server" Visible='<%# !(bool)Eval("HasPoints") %>'>
                                        <p class="small text-muted mb-0">Todavía no tiene sesiones de Práctica libre ni Assessment.</p>
                                    </asp:PlaceHolder>
                                </div>
                            </div>
                        </div>
                    </ItemTemplate>
                </asp:Repeater>
            </div>
        </div>
    </asp:PlaceHolder>

    <h2 class="h5 mb-3 mt-2">Sesiones</h2>
    <div class="table-responsive">
        <table class="table table-sm table-hover bg-white">
            <thead>
                <tr>
                    <th>Inicio</th>
                    <th>Guía</th>
                    <th>Modo</th>
                    <th>Estado</th>
                    <th class="text-end">Puntaje</th>
                    <th class="text-end">Duración</th>
                    <th class="text-end">Intentos fallidos</th>
                </tr>
            </thead>
            <tbody>
                <asp:Repeater ID="RptSessions" runat="server">
                    <ItemTemplate>
                        <tr>
                            <td class="text-nowrap"><%#: Eval("StartedText") %></td>
                            <td><%#: Eval("GuideName") %></td>
                            <td><%#: Eval("ModeText") %></td>
                            <td><%#: Eval("StatusText") %></td>
                            <td class="text-end"><%#: Eval("ScoreText") %></td>
                            <td class="text-end text-nowrap"><%#: Eval("DurationText") %></td>
                            <td class="text-end"><%#: Eval("FailedAttempts") %></td>
                        </tr>
                    </ItemTemplate>
                </asp:Repeater>
            </tbody>
        </table>
    </div>
    <asp:Label ID="LblNoSessions" runat="server" CssClass="text-muted d-block" Visible="false"
        Text="Todavía no tiene sesiones de entrenamiento." />
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ScriptContent" runat="server">
    <script src="<%=ResolveUrl("~/Scripts/chart-4.5.1.umd.min.js")%>"></script>
    <script>
        // Curvas del servidor (JavaScriptSerializer), una por canvas curve-<i>. Colores de los modos
        // como en la actividad del Resumen (paleta de referencia de la skill dataviz): Práctica
        // libre y Assessment con sus colores categóricos, una barra por sesión.
        var curveData = <%= ChartJson %>;

        (function () {
            if (typeof Chart === 'undefined' || !curveData) return;
            Chart.defaults.font.family = 'system-ui, -apple-system, "Segoe UI", sans-serif';
            Chart.defaults.color = '#898781';

            function bars(label, data, color) {
                return {
                    label: label, data: data, backgroundColor: color, skipNull: true,
                    borderColor: '#ffffff', borderWidth: 2, borderRadius: 4, borderSkipped: false, maxBarThickness: 28
                };
            }

            curveData.forEach(function (curve, i) {
                var canvas = document.getElementById('curve-' + i);
                if (!canvas || !curve) return;
                new Chart(canvas, {
                    type: 'bar',
                    data: {
                        labels: curve.labels,
                        datasets: [
                            bars('Práctica libre', curve.practice, '#1baf7a'),
                            bars('Assessment', curve.assessment, '#eb6834')
                        ]
                    },
                    options: {
                        maintainAspectRatio: false,
                        scales: {
                            x: { grid: { display: false }, border: { color: '#c3c2b7' } },
                            y: { beginAtZero: true, ticks: { precision: 0 }, grid: { color: '#e1e0d9' },
                                 border: { color: '#c3c2b7' }, title: { display: true, text: 'Intentos fallidos' } }
                        },
                        plugins: {
                            legend: { position: 'bottom', labels: { usePointStyle: true, pointStyle: 'rectRounded', boxWidth: 10 } },
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
                });
            });
        })();
    </script>
</asp:Content>
