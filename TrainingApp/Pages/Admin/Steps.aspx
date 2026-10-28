<%@ Page Title="Pasos" Language="C#" MasterPageFile="~/UI/master/Admin.Master"
    AutoEventWireup="true" CodeBehind="Steps.aspx.cs"
    Inherits="TrainingApp.Pages.Admin.Steps" %>
<%@ Register Src="~/UI/Controls/Admin/PeriodFilter.ascx" TagPrefix="uc" TagName="PeriodFilter" %>

<%-- Pasos (spec 018, AC-17 y AC-18; diseño de la Fase 11, D-160): el ranking de pasos por % de
     sesiones con fallo del período y el modo, de una guía o, con "Todas", un resumen con el
     mismo paso de cada guía junto (D-147), con intentos fallidos, veces sin acierto, saltos (solo
     Práctica libre), la mediana de duración y los motivos con su acción: los 2 más frecuentes a
     la vista y "Ver todos" despliega el resto en el cliente (<details>, sin postback). Cada paso,
     con el cambio de su % con fallo contra el período anterior (spec 023). Lo calcula
     DashboardService con StepRuns (D-146). Filtros en la URL. Solo lectura. --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="admin-page-head">
        <div>
            <h1 class="admin-page-title">Pasos</h1>
            <p class="admin-page-lead">Qué pasos fallan más en <asp:Literal ID="LitScope" runat="server" />.</p>
        </div>
        <asp:Panel ID="PnlFilters" runat="server" CssClass="mt-filters" DefaultButton="BtnFilter">
            <uc:PeriodFilter ID="Period" runat="server" />
            <div class="mt-field">
                <label class="mt-field-label" for="<%= DdlGuide.ClientID %>">Guía</label>
                <asp:DropDownList ID="DdlGuide" runat="server" CssClass="form-select" />
            </div>
            <div class="mt-field">
                <label class="mt-field-label" for="<%= DdlMode.ClientID %>">Modo</label>
                <asp:DropDownList ID="DdlMode" runat="server" CssClass="form-select">
                    <asp:ListItem Value="" Text="Assessment" />
                    <asp:ListItem Value="practice" Text="Práctica libre" />
                </asp:DropDownList>
            </div>
            <asp:Button ID="BtnFilter" runat="server" Text="Filtrar" CssClass="btn btn-secondary"
                OnClick="BtnFilter_Click" />
            <asp:HyperLink ID="LnkExport" runat="server" CssClass="btn btn-primary"
                ToolTip="El ranking con estos filtros, en Excel"><i class="bi bi-download" aria-hidden="true"></i>Descargar</asp:HyperLink>
        </asp:Panel>
    </div>

    <%-- Un período inválido (también editando la URL) vale Este mes (P-6). --%>
    <asp:Label ID="LblInvalidPeriod" runat="server" CssClass="mt-alert-warn d-block mb-3" Visible="false"
        role="status" Text="El período pedido no es válido (falta una fecha, el Fin es anterior al Inicio o es posterior a hoy). Se muestra Este mes." />

    <p class="mt-muted mt-small steps-note">
        Ordenados por % de sesiones con fallo: entre las sesiones que llegaron al paso, las que tuvieron
        algún intento fallido.
        <asp:PlaceHolder ID="PhAllGuidesNote" runat="server">Con todas las guías, el mismo paso de cada guía se
            suma en un solo renglón; elija una guía para ver solo sus pasos.</asp:PlaceHolder>
        <asp:PlaceHolder ID="PhSkipsNote" runat="server">Los saltos se cuentan aparte y no son fallos.</asp:PlaceHolder>
    </p>

    <asp:Repeater ID="RptGuides" runat="server">
        <ItemTemplate>
            <section class="mt-panel steps-group">
                <div class="mt-panel-body pb-0">
                    <h2 class="mt-section-title" title='<%#: Eval("GuideTitle") %>'><%#: Eval("GuideName") %></h2>
                </div>
                <div class="mt-table-wrap mt-3">
                    <table class="table mt-table steps-table">
                        <thead>
                            <tr>
                                <th scope="col" class="mt-num">#</th>
                                <th scope="col">Paso y motivos</th>
                                <th scope="col">Sesiones con fallo</th>
                                <th scope="col" class="mt-num">Intentos fallidos</th>
                                <th scope="col" class="mt-num">Veces sin acierto</th>
                                <asp:PlaceHolder runat="server" Visible='<%# ShowsSkips %>'>
                                    <th scope="col" class="mt-num">Saltos</th>
                                </asp:PlaceHolder>
                                <th scope="col" class="mt-num mt-col-secondary">Mediana</th>
                            </tr>
                        </thead>
                        <tbody>
                            <asp:Repeater runat="server" DataSource='<%# Eval("Rows") %>'>
                                <ItemTemplate>
                                    <tr>
                                        <td class="mt-num mt-muted fw-semibold"><%#: Eval("Rank") %></td>
                                        <td class="steps-name">
                                            <span class="fw-semibold"><%#: Eval("StepName") %></span>
                                            <ul class="steps-reasons">
                                                <asp:Repeater runat="server" DataSource='<%# Eval("TopReasons") %>'>
                                                    <ItemTemplate><li><%#: Container.DataItem %></li></ItemTemplate>
                                                </asp:Repeater>
                                            </ul>
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
                                        </td>
                                        <td class="steps-rate">
                                            <div class="steps-rate-line">
                                                <span class="fw-semibold"><%#: Eval("FailureRateText") %></span>
                                                <span class="mt-muted mt-small"><%#: Eval("SessionsText") %></span>
                                            </div>
                                            <div class="mt-track"><div class="mt-fill" style='width: <%#: Eval("FailureWidth") %>%'></div></div>
                                            <asp:PlaceHolder runat="server" Visible='<%# (bool)Eval("FailureRateChange.Visible") %>'>
                                                <div class="steps-change">
                                                    <span class='mt-change <%#: Eval("FailureRateChange.Css") %>'><%#: Eval("FailureRateChange.Text") %><span class="visually-hidden"> <%#: Eval("FailureRateChange.Verdict") %></span></span>
                                                    <span class="mt-change-ref"><%#: Eval("FailureRateChange.Reference") %> · <%#: Eval("FailureRateChange.Base") %></span>
                                                </div>
                                            </asp:PlaceHolder>
                                        </td>
                                        <td class="mt-num"><%#: Eval("FailedAttempts") %></td>
                                        <td class="mt-num"><%#: Eval("TimesWithoutSuccess") %></td>
                                        <asp:PlaceHolder runat="server" Visible='<%# ShowsSkips %>'>
                                            <td class="mt-num"><%#: Eval("Skips") %></td>
                                        </asp:PlaceHolder>
                                        <td class="mt-num mt-col-secondary text-nowrap"><%#: Eval("MedianText") %></td>
                                    </tr>
                                </ItemTemplate>
                            </asp:Repeater>
                        </tbody>
                    </table>
                </div>
            </section>
        </ItemTemplate>
    </asp:Repeater>

    <asp:Panel ID="PnlEmpty" runat="server" CssClass="mt-panel mt-empty" Visible="false">
        <span>Sin datos para este período. Pruebe con "Todo", con otra guía o con el otro modo.</span>
    </asp:Panel>
</asp:Content>
