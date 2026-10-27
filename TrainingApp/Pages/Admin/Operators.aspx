<%@ Page Title="Operadores" Language="C#" MasterPageFile="~/UI/master/Admin.Master"
    AutoEventWireup="true" CodeBehind="Operators.aspx.cs"
    Inherits="TrainingApp.Pages.Admin.Operators" %>

<%-- Operadores (spec 018, AC-7 y AC-13; diseño de la Fase 11, D-160): estado por guía
     (ReadinessPolicy), mejor y último puntaje de Assessment y última actividad, con búsqueda y
     filtros en la URL (un enlace, F5 o Atrás conservan la vista) y 50 por página en el
     servidor. Solo lectura. --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="admin-page-head">
        <div>
            <h1 class="admin-page-title">Operadores</h1>
            <p class="admin-page-lead">El estado de cada operador en cada guía.</p>
        </div>
    </div>

    <section class="mt-panel" aria-labelledby="listTitle">
        <h2 id="listTitle" class="visually-hidden">Lista de operadores</h2>
        <asp:Panel ID="PnlFilters" runat="server" CssClass="mt-panel-body mt-filters" DefaultButton="BtnFilter">
            <div class="mt-field operators-search">
                <label class="mt-field-label" for="<%= TxtSearch.ClientID %>">Buscar</label>
                <asp:TextBox ID="TxtSearch" runat="server" CssClass="form-control admin-no-markup" MaxLength="100"
                    placeholder="Código o nombre" />
            </div>
            <div class="mt-field">
                <label class="mt-field-label" for="<%= DdlState.ClientID %>">Estado</label>
                <asp:DropDownList ID="DdlState" runat="server" CssClass="form-select">
                    <asp:ListItem Value="" Text="Todos" />
                    <asp:ListItem Value="passed" Text="Aprobado" />
                    <asp:ListItem Value="failed" Text="Reprobado" />
                    <asp:ListItem Value="training" Text="En entrenamiento" />
                    <asp:ListItem Value="notstarted" Text="Sin iniciar" />
                </asp:DropDownList>
            </div>
            <div class="mt-field">
                <label class="mt-field-label" for="<%= DdlGuide.ClientID %>">Guía</label>
                <asp:DropDownList ID="DdlGuide" runat="server" CssClass="form-select" />
            </div>
            <asp:Button ID="BtnFilter" runat="server" Text="Filtrar" CssClass="btn btn-secondary"
                OnClick="BtnFilter_Click" />
            <asp:HyperLink ID="LnkExport" runat="server" CssClass="btn btn-primary ms-md-auto"
                ToolTip="La lista con estos filtros, en Excel"><i class="bi bi-download" aria-hidden="true"></i>Descargar</asp:HyperLink>
        </asp:Panel>

        <asp:PlaceHolder ID="PhTable" runat="server">
            <div class="mt-table-wrap">
                <table class="table mt-table table-hover operators-table">
                    <thead>
                        <tr>
                            <th scope="col">Operador</th>
                            <th scope="col">Área</th>
                            <asp:Repeater ID="RptGuideHeaders" runat="server">
                                <ItemTemplate>
                                    <th scope="col" title='<%#: Eval("Title") %>'><%#: Eval("AreaName") %></th>
                                </ItemTemplate>
                            </asp:Repeater>
                            <th scope="col" class="mt-col-secondary">Última actividad</th>
                        </tr>
                    </thead>
                    <tbody>
                        <asp:Repeater ID="RptOperators" runat="server">
                            <ItemTemplate>
                                <tr>
                                    <td>
                                        <a class="operators-name" href='<%#: Eval("DetailUrl") %>'><%#: Eval("Name") %></a>
                                        <span class="mt-muted mt-small d-block"><%#: Eval("Code1") %></span>
                                    </td>
                                    <td class="text-nowrap"><%#: Eval("AreaText") %></td>
                                    <asp:Repeater runat="server" DataSource='<%# Eval("Guides") %>'>
                                        <ItemTemplate>
                                            <td>
                                                <span class='<%#: Eval("StateCss") %>'><i class='<%#: Eval("StateIcon") %>' aria-hidden="true"></i><%#: Eval("StateText") %></span>
                                                <span class="mt-muted mt-small d-block mt-1"><%#: Eval("ScoresText") %></span>
                                            </td>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                    <td class="mt-col-secondary mt-muted text-nowrap"><%#: Eval("LastActivityText") %></td>
                                </tr>
                            </ItemTemplate>
                        </asp:Repeater>
                    </tbody>
                </table>
            </div>
        </asp:PlaceHolder>

        <asp:Panel ID="PnlEmpty" runat="server" CssClass="mt-empty" Visible="false">
            <span>Ningún operador coincide con estos filtros.</span>
            <asp:HyperLink ID="LnkClear" runat="server" CssClass="btn btn-secondary btn-sm">Quitar filtros</asp:HyperLink>
        </asp:Panel>

        <asp:PlaceHolder ID="PhPager" runat="server">
            <div class="mt-panel-body mt-pager">
                <asp:HyperLink ID="LnkPrev" runat="server" CssClass="btn btn-secondary btn-sm"><i class="bi bi-chevron-left" aria-hidden="true"></i>Anterior</asp:HyperLink>
                <span><asp:Literal ID="LitPageInfo" runat="server" /></span>
                <asp:HyperLink ID="LnkNext" runat="server" CssClass="btn btn-secondary btn-sm">Siguiente<i class="bi bi-chevron-right" aria-hidden="true"></i></asp:HyperLink>
            </div>
        </asp:PlaceHolder>
    </section>

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
