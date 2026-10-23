<%@ Page Title="Pasos" Language="C#" MasterPageFile="~/UI/master/Admin.Master"
    AutoEventWireup="true" CodeBehind="Steps.aspx.cs"
    Inherits="TrainingApp.Pages.Admin.Steps" %>

<%-- Pasos (spec 018, AC-17 y AC-18): el ranking de pasos por % de sesiones con fallo del
     período y el modo, de una guía o, con "Todas", un resumen con el mismo paso de cada guía
     junto, con intentos fallidos, veces sin acierto, saltos (solo Práctica
     libre), la mediana de duración y los motivos con su acción. Lo calcula DashboardService con
     StepRuns (D-146). Filtros en la URL. Solo lectura. --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <h1 class="h3 mb-4">Pasos</h1>

    <asp:Panel ID="PnlFilters" runat="server" CssClass="row g-2 align-items-end mb-3" DefaultButton="BtnFilter">
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
        <div class="col-sm-6 col-md-3">
            <label class="form-label small text-muted mb-1" for="<%= DdlMode.ClientID %>">Modo</label>
            <asp:DropDownList ID="DdlMode" runat="server" CssClass="form-select">
                <asp:ListItem Value="" Text="Assessment" />
                <asp:ListItem Value="practice" Text="Práctica libre" />
            </asp:DropDownList>
        </div>
        <div class="col-sm-6 col-md-3">
            <asp:Button ID="BtnFilter" runat="server" Text="Filtrar" CssClass="btn btn-primary w-100"
                OnClick="BtnFilter_Click" />
        </div>
    </asp:Panel>

    <p class="small text-muted mb-3">
        <asp:Literal ID="LitScope" runat="server" />. Ordenados por % de sesiones con fallo: entre las
        sesiones que llegaron al paso, las que tuvieron algún intento fallido.
        <asp:PlaceHolder ID="PhAllGuidesNote" runat="server">Con todas las guías, el mismo paso de cada guía se
            suma en un solo renglón; elige una guía para ver solo sus pasos.</asp:PlaceHolder>
        <asp:PlaceHolder ID="PhSkipsNote" runat="server">Los saltos se cuentan aparte y no son fallos.</asp:PlaceHolder>
    </p>

    <asp:Repeater ID="RptGuides" runat="server">
        <ItemTemplate>
            <h2 class="h5 mt-2" title='<%#: Eval("GuideTitle") %>'><%#: Eval("GuideName") %></h2>
            <div class="table-responsive mb-4">
                <table class="table table-sm bg-white align-middle">
                    <thead>
                        <tr>
                            <th>Paso</th>
                            <th class="text-end">Sesiones</th>
                            <th class="text-end">% con fallo</th>
                            <th class="text-end">Intentos fallidos</th>
                            <th class="text-end">Veces sin acierto</th>
                            <asp:PlaceHolder runat="server" Visible='<%# ShowsSkips %>'>
                                <th class="text-end">Saltos</th>
                            </asp:PlaceHolder>
                            <th class="text-end">Mediana</th>
                            <th>Motivos y acciones</th>
                        </tr>
                    </thead>
                    <tbody>
                        <asp:Repeater runat="server" DataSource='<%# Eval("Rows") %>'>
                            <ItemTemplate>
                                <tr>
                                    <td><%#: Eval("StepName") %></td>
                                    <td class="text-end"><%#: Eval("Sessions") %></td>
                                    <td class="text-end"><%#: Eval("FailureRateText") %></td>
                                    <td class="text-end"><%#: Eval("FailedAttempts") %></td>
                                    <td class="text-end"><%#: Eval("TimesWithoutSuccess") %></td>
                                    <asp:PlaceHolder runat="server" Visible='<%# ShowsSkips %>'>
                                        <td class="text-end"><%#: Eval("Skips") %></td>
                                    </asp:PlaceHolder>
                                    <td class="text-end text-nowrap"><%#: Eval("MedianText") %></td>
                                    <td class="small">
                                        <asp:Repeater runat="server" DataSource='<%# Eval("Reasons") %>'>
                                            <ItemTemplate><div><%#: Container.DataItem %></div></ItemTemplate>
                                        </asp:Repeater>
                                    </td>
                                </tr>
                            </ItemTemplate>
                        </asp:Repeater>
                    </tbody>
                </table>
            </div>
        </ItemTemplate>
    </asp:Repeater>

    <asp:Label ID="LblEmpty" runat="server" CssClass="text-muted d-block" Visible="false"
        Text="Sin datos para este período." />
</asp:Content>
