<%@ Page Title="Operadores" Language="C#" MasterPageFile="~/UI/master/Admin.Master"
    AutoEventWireup="true" CodeBehind="Operators.aspx.cs"
    Inherits="TrainingApp.Pages.Admin.Operators" %>

<%-- Operadores (spec 018, AC-7 y AC-13): estado por guía (ReadinessPolicy), mejor y último
     puntaje de Assessment y última actividad, con búsqueda y filtros en la URL (un enlace, F5
     o Atrás conservan la vista) y 50 por página en el servidor. Solo lectura. --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <h1 class="h3 mb-4">Operadores</h1>

    <asp:Panel ID="PnlFilters" runat="server" CssClass="row g-2 align-items-end mb-3" DefaultButton="BtnFilter">
        <div class="col-md-4">
            <label class="form-label small text-muted mb-1" for="<%= TxtSearch.ClientID %>">Buscar</label>
            <asp:TextBox ID="TxtSearch" runat="server" CssClass="form-control admin-no-markup" MaxLength="100"
                placeholder="Código o nombre" />
        </div>
        <div class="col-sm-6 col-md-3">
            <label class="form-label small text-muted mb-1" for="<%= DdlState.ClientID %>">Estado</label>
            <asp:DropDownList ID="DdlState" runat="server" CssClass="form-select">
                <asp:ListItem Value="" Text="Todos" />
                <asp:ListItem Value="passed" Text="Aprobado" />
                <asp:ListItem Value="failed" Text="Reprobado" />
                <asp:ListItem Value="training" Text="En entrenamiento" />
                <asp:ListItem Value="notstarted" Text="Sin iniciar" />
            </asp:DropDownList>
        </div>
        <div class="col-sm-6 col-md-3">
            <label class="form-label small text-muted mb-1" for="<%= DdlGuide.ClientID %>">Guía</label>
            <asp:DropDownList ID="DdlGuide" runat="server" CssClass="form-select" />
        </div>
        <div class="col-md-1">
            <asp:Button ID="BtnFilter" runat="server" Text="Filtrar" CssClass="btn btn-primary w-100"
                OnClick="BtnFilter_Click" />
        </div>
        <div class="col-md-1">
            <asp:HyperLink ID="LnkExport" runat="server" CssClass="btn btn-outline-secondary w-100"
                ToolTip="Descargar la lista con estos filtros en Excel">Excel</asp:HyperLink>
        </div>
    </asp:Panel>

    <div class="table-responsive">
        <table class="table table-sm table-hover bg-white align-middle admin-operators">
            <thead>
                <tr>
                    <th>Código</th>
                    <th>Nombre</th>
                    <asp:Repeater ID="RptGuideHeaders" runat="server">
                        <ItemTemplate>
                            <th title='<%#: Eval("Title") %>'><%#: Eval("AreaName") %></th>
                        </ItemTemplate>
                    </asp:Repeater>
                    <th>Última actividad</th>
                    <th></th>
                </tr>
            </thead>
            <tbody>
                <asp:Repeater ID="RptOperators" runat="server">
                    <ItemTemplate>
                        <tr>
                            <td><%#: Eval("Code1") %></td>
                            <td>
                                <%#: Eval("Name") %>
                                <asp:PlaceHolder runat="server" Visible='<%# (bool)Eval("NotStarted") %>'>
                                    <span class="badge bg-light text-secondary border ms-1">Sin iniciar</span>
                                </asp:PlaceHolder>
                            </td>
                            <asp:Repeater runat="server" DataSource='<%# Eval("Guides") %>'>
                                <ItemTemplate>
                                    <td>
                                        <asp:PlaceHolder runat="server" Visible='<%# (bool)Eval("HasSessions") %>'>
                                            <span class='<%#: Eval("StateCss") %>'><%#: Eval("StateText") %></span>
                                            <div class="small text-muted"><%#: Eval("ScoresText") %></div>
                                        </asp:PlaceHolder>
                                        <asp:PlaceHolder runat="server" Visible='<%# !(bool)Eval("HasSessions") %>'>
                                            <span class="text-muted">—</span>
                                        </asp:PlaceHolder>
                                    </td>
                                </ItemTemplate>
                            </asp:Repeater>
                            <td class="text-nowrap"><%#: Eval("LastActivityText") %></td>
                            <td class="text-end">
                                <a class="btn btn-link btn-sm" href='<%#: Eval("DetailUrl") %>'>Ver detalle</a>
                            </td>
                        </tr>
                    </ItemTemplate>
                </asp:Repeater>
            </tbody>
        </table>
    </div>

    <asp:Label ID="LblEmpty" runat="server" CssClass="text-muted d-block mb-3" Visible="false"
        Text="No hay operadores que coincidan con los filtros." />

    <div class="d-flex align-items-center gap-3">
        <asp:HyperLink ID="LnkPrev" runat="server" CssClass="btn btn-outline-secondary btn-sm">&laquo; Anterior</asp:HyperLink>
        <span class="small text-muted"><asp:Literal ID="LitPageInfo" runat="server" /></span>
        <asp:HyperLink ID="LnkNext" runat="server" CssClass="btn btn-outline-secondary btn-sm">Siguiente &raquo;</asp:HyperLink>
    </div>

    <script>
        // Formato: < y > no son parte de un código ni de un nombre, y ASP.NET rechaza un
        // postback con marcado (<x): se quitan al escribir.
        document.querySelectorAll('.admin-no-markup').forEach(function (input) {
            input.addEventListener('input', function () {
                if (/[<>]/.test(input.value)) input.value = input.value.replace(/[<>]/g, '');
            });
        });
    </script>
</asp:Content>
