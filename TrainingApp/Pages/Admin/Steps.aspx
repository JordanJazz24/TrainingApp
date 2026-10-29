<%@ Page Title="Pasos" Language="C#" MasterPageFile="~/UI/master/Admin.Master"
    AutoEventWireup="true" CodeBehind="Steps.aspx.cs"
    Inherits="TrainingApp.Pages.Admin.Steps" %>
<%@ Register Src="~/UI/Controls/Admin/PeriodFilter.ascx" TagPrefix="uc" TagName="PeriodFilter" %>

<%-- Pasos (spec 018, AC-17 y AC-18; brief 022, alternativa 4a "Mesa de enseñanza" con el
     encabezado H4): el ranking de pasos por % de sesiones con fallo del período y el modo, de
     una guía o, con "Todas", un resumen con el mismo paso de cada guía junto (D-147). Arriba, una
     franja de lado a lado con el título y el rango a la izquierda y los filtros a la derecha;
     debajo, una columna por paso, el paso elegido (sus motivos con su acción, sus números y el
     cambio contra el período anterior, spec 023) y el ranking completo. Elegir un paso se
     resuelve en el cliente, sin postback; "Ver todos" despliega el resto de motivos
     (<details>). Lo calcula DashboardService con StepRuns (D-146). Filtros en la URL. Solo
     lectura. Tokens de la app (D-280). --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="steps-band">
        <div class="steps-band-inner">
            <div class="steps-band-head">
                <span class="steps-eyebrow">Pasos · ranking de pasos difíciles</span>
                <h1 class="steps-title">Qué pasos fallan más en <asp:Literal ID="LitScope" runat="server" />.</h1>
                <p class="steps-range" aria-live="polite">
                    <i class="bi bi-calendar3" aria-hidden="true"></i>
                    <span><b><asp:Literal ID="LitRange" runat="server" /></b><asp:Literal ID="LitPrevRange" runat="server" /></span>
                </p>
            </div>
            <asp:Panel ID="PnlFilters" runat="server" CssClass="steps-band-filters" DefaultButton="BtnFilter">
                <uc:PeriodFilter ID="Period" runat="server" ShowsRange="false" />
                <div class="steps-band-row">
                    <div class="mt-field">
                        <label class="mt-field-label" for="<%= DdlGuide.ClientID %>">Guía</label>
                        <asp:DropDownList ID="DdlGuide" runat="server" CssClass="form-select" />
                    </div>
                    <div class="mt-field" role="radiogroup" aria-labelledby="stepsModeLabel">
                        <span class="mt-field-label" id="stepsModeLabel">Modo</span>
                        <asp:RadioButtonList ID="RblMode" runat="server" CssClass="steps-seg"
                            RepeatLayout="Flow" RepeatDirection="Horizontal">
                            <asp:ListItem Value="" Text="Assessment" />
                            <asp:ListItem Value="practice" Text="Práctica libre" />
                        </asp:RadioButtonList>
                    </div>
                    <div class="steps-band-actions">
                        <asp:Button ID="BtnFilter" runat="server" Text="Filtrar" CssClass="btn steps-btn-ghost"
                            OnClick="BtnFilter_Click" />
                        <asp:HyperLink ID="LnkExport" runat="server" CssClass="btn steps-btn-ink"
                            ToolTip="El ranking con estos filtros, en Excel"><i class="bi bi-download" aria-hidden="true"></i>Descargar</asp:HyperLink>
                    </div>
                </div>
            </asp:Panel>
        </div>
    </div>

    <div class="steps-wrap">
        <%-- Un período inválido (también editando la URL) vale Este mes (P-6). --%>
        <asp:Label ID="LblInvalidPeriod" runat="server" CssClass="mt-alert-warn d-block" Visible="false"
            role="status" Text="El período pedido no es válido (falta una fecha, el Fin es anterior al Inicio o es posterior a hoy). Se muestra Este mes." />

        <asp:PlaceHolder ID="PhRanking" runat="server">
            <%-- Una columna por paso: su % con fallo y, debajo, su lugar, su nombre y su cambio. --%>
            <section class="steps-card steps-chart" aria-label="Dónde se falla">
                <div class="steps-card-head">
                    <h2 class="steps-kicker" title="<%: GroupTitle %>"><%: GroupName %> · % de sesiones con fallo</h2>
                    <span class="mt-muted mt-small">Toque una columna para ver el paso.
                        <asp:PlaceHolder ID="PhSkipsNote" runat="server">Los saltos se cuentan aparte y no son fallos.</asp:PlaceHolder></span>
                </div>
                <div class="steps-chart-scroll" style="--steps-n: <%= StepCount %>">
                    <div class="steps-cols">
                        <asp:Repeater ID="RptColumns" runat="server">
                            <ItemTemplate>
                                <button type="button" class='steps-col<%# Container.ItemIndex == 0 ? " is-selected" : "" %>'
                                    data-step="<%# Container.ItemIndex %>" aria-pressed='<%# Container.ItemIndex == 0 ? "true" : "false" %>'
                                    aria-label='<%#: "#" + Eval("Rank") + " " + Eval("StepName") + ": " + Eval("FailureRateText") %>'>
                                    <span class="steps-col-value"><%#: Eval("FailureRateText") %></span>
                                    <span class="steps-col-bar" style='--h: <%#: Eval("FailureWidth") %>'></span>
                                </button>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                    <div class="steps-labels">
                        <asp:Repeater ID="RptLabels" runat="server">
                            <ItemTemplate>
                                <div class='steps-label<%# Container.ItemIndex == 0 ? " is-selected" : "" %>' data-step="<%# Container.ItemIndex %>">
                                    <span class="steps-label-rank">#<%#: Eval("Rank") %><span class="steps-label-sel"> · elegido</span></span>
                                    <span class="steps-label-name"><%#: Eval("StepName") %></span>
                                    <asp:PlaceHolder runat="server" Visible='<%# (bool)Eval("FailureRateChange.Visible") %>'>
                                        <span class='mt-change <%#: Eval("FailureRateChange.Css") %>'><%#: Eval("FailureRateChange.Text") %><span class="visually-hidden"> <%#: Eval("FailureRateChange.Verdict") %></span></span>
                                    </asp:PlaceHolder>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                </div>
            </section>

            <%-- El paso elegido: uno por paso, solo el elegido a la vista (el #1 al cargar). --%>
            <div id="stepsDetail" class="steps-detail-anchor">
                <asp:Repeater ID="RptDetails" runat="server">
                    <ItemTemplate>
                        <section class="steps-card steps-detail" data-step="<%# Container.ItemIndex %>"
                            aria-label="Paso elegido" <%# Container.ItemIndex == 0 ? "" : "hidden" %>>
                            <div class="steps-detail-why">
                                <span class="steps-kicker steps-kicker-accent">#<%#: Eval("Rank") %> del ranking · por qué falla</span>
                                <h2 class="steps-detail-name"><%#: Eval("StepName") %></h2>
                                <ol class="steps-why">
                                    <asp:Repeater runat="server" DataSource='<%# Eval("TopReasons") %>'>
                                        <ItemTemplate>
                                            <li>
                                                <div class="steps-why-line">
                                                    <span class="steps-why-reason"><%#: Eval("Reason") %></span>
                                                    <span class="mt-muted mt-small text-nowrap"><%#: Eval("CountText") %> · <%#: Eval("ShareText") %></span>
                                                </div>
                                                <div class="steps-why-track"><div class="steps-why-fill" style='width: <%#: Eval("Width") %>%'></div></div>
                                                <span class="steps-why-action"><i class="bi bi-arrow-return-right" aria-hidden="true"></i><%#: Eval("Action") %></span>
                                            </li>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </ol>
                                <asp:PlaceHolder runat="server" Visible='<%# (bool)Eval("HasMoreReasons") %>'>
                                    <details class="steps-more">
                                        <summary>Ver todos (<%#: ((System.Collections.ICollection)Eval("MoreReasons")).Count %> más)</summary>
                                        <ul class="steps-reasons">
                                            <asp:Repeater runat="server" DataSource='<%# Eval("MoreReasons") %>'>
                                                <ItemTemplate><li><%#: Container.DataItem %></li></ItemTemplate>
                                            </asp:Repeater>
                                        </ul>
                                    </details>
                                </asp:PlaceHolder>
                            </div>
                            <div class="steps-detail-numbers">
                                <span class="steps-kicker">Sesiones con fallo</span>
                                <div class="steps-detail-rate">
                                    <span class="steps-figure"><%#: Eval("FailureRateText") %></span>
                                    <span class="mt-muted"><%#: Eval("SessionsText") %></span>
                                </div>
                                <dl class="steps-stats">
                                    <div><dt>Intentos fallidos</dt><dd><%#: Eval("FailedAttempts") %></dd></div>
                                    <div><dt>Veces sin acierto</dt><dd><%#: Eval("TimesWithoutSuccess") %></dd></div>
                                    <div><dt>Mediana</dt><dd><%#: Eval("MedianText") %></dd></div>
                                    <asp:PlaceHolder runat="server" Visible='<%# ShowsSkips %>'>
                                        <div><dt>Saltos</dt><dd><%#: Eval("Skips") %></dd></div>
                                    </asp:PlaceHolder>
                                </dl>
                            </div>
                            <div class="steps-detail-change">
                                <span class="steps-kicker">Contra el período anterior</span>
                                <asp:PlaceHolder runat="server" Visible='<%# (bool)Eval("FailureRateChange.Visible") %>'>
                                    <div class='steps-change-big <%#: Eval("FailureRateChange.Css") %>'>
                                        <span class="steps-change-value"><%#: Eval("FailureRateChange.Text") %></span>
                                        <span class="steps-change-verdict"><%#: Eval("ChangeVerdict") %></span>
                                    </div>
                                    <asp:PlaceHolder runat="server" Visible='<%# (bool)Eval("HasPrevious") %>'>
                                        <div class="steps-vs">
                                            <span class="mt-muted">Anterior</span>
                                            <div class="steps-vs-track"><div class="steps-vs-fill steps-vs-prev" style='width: <%#: Eval("PreviousWidth") %>%'></div></div>
                                            <span class="mt-num"><%#: Eval("PreviousRateText") %></span>
                                            <span class="mt-muted">Este período</span>
                                            <div class="steps-vs-track"><div class="steps-vs-fill" style='width: <%#: Eval("FailureWidth") %>%'></div></div>
                                            <span class="mt-num fw-semibold"><%#: Eval("FailureRateText") %></span>
                                        </div>
                                    </asp:PlaceHolder>
                                    <span class="mt-change-ref"><%#: Eval("FailureRateChange.Reference") %> · <%#: Eval("FailureRateChange.Base") %></span>
                                </asp:PlaceHolder>
                                <asp:PlaceHolder runat="server" Visible='<%# !(bool)Eval("FailureRateChange.Visible") %>'>
                                    <span class="mt-muted">Con Todo no hay comparación.</span>
                                </asp:PlaceHolder>
                            </div>
                        </section>
                    </ItemTemplate>
                </asp:Repeater>
            </div>

            <%-- El ranking completo: cada paso con su % y su motivo más frecuente. Plegado: repite
                 lo de las columnas y el panel (usuario, 2026-10-29, enmienda a D-239). --%>
            <details class="steps-card steps-ranking">
                <summary class="steps-list-head">
                    <h2 class="steps-kicker">Ranking completo</h2>
                    <span class="steps-ranking-toggle"></span>
                </summary>
                <p class="mt-muted mt-small steps-list-note">Ordenados por % de sesiones con fallo: entre las sesiones que
                    llegaron al paso, las que tuvieron algún intento fallido.
                    <asp:PlaceHolder ID="PhAllGuidesNote" runat="server">Con todas las guías, el mismo paso de cada guía se
                        suma en un solo renglón; elija una guía para ver solo sus pasos.</asp:PlaceHolder></p>
                <div class="steps-list-wrap">
                    <ol class="steps-list">
                        <asp:Repeater ID="RptList" runat="server">
                            <ItemTemplate>
                                <li class='steps-row<%# Container.ItemIndex == 0 ? " is-selected" : "" %>' data-step="<%# Container.ItemIndex %>">
                                    <span class="steps-row-rank"><%#: Eval("Rank") %></span>
                                    <span class="fw-semibold"><%#: Eval("StepName") %></span>
                                    <div class="steps-row-rate">
                                        <span class="fw-semibold"><%#: Eval("FailureRateText") %></span>
                                        <div class="steps-why-track"><div class="steps-why-fill" style='width: <%#: Eval("FailureWidth") %>%'></div></div>
                                    </div>
                                    <span class="mt-muted mt-small"><%#: Eval("FirstReason") %></span>
                                    <button type="button" class="steps-see" data-step="<%# Container.ItemIndex %>">Ver paso</button>
                                    <span class="steps-up"><i class="bi bi-eye" aria-hidden="true"></i>Arriba</span>
                                </li>
                            </ItemTemplate>
                        </asp:Repeater>
                    </ol>
                </div>
            </details>
        </asp:PlaceHolder>

        <asp:Panel ID="PnlEmpty" runat="server" CssClass="mt-panel mt-empty" Visible="false">
            <span>Sin datos para este período. Pruebe con "Todo", con otra guía o con el otro modo.</span>
        </asp:Panel>
    </div>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ScriptContent" runat="server">
    <script>
        // Elegir un paso (su columna o "Ver paso") lo muestra arriba, sin postback (brief 022, 4a).
        (function () {
            var detail = document.getElementById('stepsDetail');
            if (!detail) return;
            function select(index, fromList) {
                document.querySelectorAll('.steps-col, .steps-label, .steps-detail, .steps-row').forEach(function (el) {
                    var on = el.getAttribute('data-step') === index;
                    el.classList.toggle('is-selected', on);
                    if (el.classList.contains('steps-col')) el.setAttribute('aria-pressed', on ? 'true' : 'false');
                    if (el.classList.contains('steps-detail')) el.hidden = !on;
                });
                if (fromList) detail.scrollIntoView({ behavior: 'smooth', block: 'start' });
            }
            document.addEventListener('click', function (e) {
                var button = e.target.closest('.steps-col, .steps-see');
                if (button) select(button.getAttribute('data-step'), button.classList.contains('steps-see'));
            });
        })();
    </script>
</asp:Content>
