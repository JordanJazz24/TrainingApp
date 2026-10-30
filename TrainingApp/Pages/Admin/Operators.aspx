<%@ Page Title="Operadores" Language="C#" MasterPageFile="~/UI/master/Admin.Master"
    AutoEventWireup="true" CodeBehind="Operators.aspx.cs"
    Inherits="TrainingApp.Pages.Admin.Operators" %>

<%-- Operadores (spec 018, AC-7 y AC-13; diseño de la Fase 11, D-160; por área, spec 024; spec 022,
     brief 6: "pestañas por estado" con el encabezado de barra proporcional). El estado en las guías
     de su área (OperatorArea, D-261), mejor y último puntaje de Assessment y última actividad, con
     búsqueda y filtros Área y Estado en la URL (un enlace, F5 o Atrás conservan la vista) y 50 por
     página en el servidor. El Estado se elige en la barra del encabezado: cada tramo es un enlace
     con su número (OperatorSummary) y su ancho es cuántos hay. Solo lectura. --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <section class="operators-head" aria-label="Encabezado de Operadores">
        <div class="operators-head-top">
            <div>
                <div class="operators-eyebrow">Estado en la guía de su área · <asp:Literal ID="LitAreaLabel" runat="server" /></div>
                <h1 class="operators-title">Operadores</h1>
            </div>
            <asp:Panel ID="PnlFilters" runat="server" CssClass="mt-filters" DefaultButton="BtnFilter">
                <div class="mt-field operators-search">
                    <label class="mt-field-label" for="<%= TxtSearch.ClientID %>">Buscar</label>
                    <asp:TextBox ID="TxtSearch" runat="server" CssClass="form-control admin-no-markup" MaxLength="100"
                        placeholder="Código o nombre" />
                </div>
                <div class="mt-field">
                    <%-- Área (D-262): un <select> nativo agrupado por producto, como "Elige tu área"
                         (DropDownList no tiene optgroup); la BLL valida el valor (P-4). --%>
                    <label class="mt-field-label" for="operatorsArea">Área</label>
                    <select id="operatorsArea" name="area" class="form-select">
                        <option value=""<%= AreaAllSelected ? " selected" : "" %>>Todas</option>
                        <asp:Repeater ID="RptAreaGroups" runat="server" EnableViewState="false">
                            <ItemTemplate>
                                <optgroup label="<%#: Eval("ProductName") %>">
                                    <asp:Repeater runat="server" DataSource='<%# Eval("Areas") %>'>
                                        <ItemTemplate>
                                            <option value="<%#: Eval("AreaId") %>"<%# (bool)Eval("Selected") ? " selected" : "" %>><%#: Eval("AreaName") %></option>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </optgroup>
                            </ItemTemplate>
                        </asp:Repeater>
                        <option value="none"<%= AreaNoneSelected ? " selected" : "" %>>Sin área</option>
                    </select>
                </div>
                <%-- El Estado aplicado viaja con Filtrar (se elige en la barra de abajo). --%>
                <asp:HiddenField ID="HfState" runat="server" />
                <asp:Button ID="BtnFilter" runat="server" Text="Filtrar" CssClass="btn btn-secondary"
                    OnClick="BtnFilter_Click" />
                <asp:HyperLink ID="LnkExport" runat="server" CssClass="btn btn-primary"
                    ToolTip="La lista con estos filtros, en Excel"><i class="bi bi-download" aria-hidden="true"></i>Descargar</asp:HyperLink>
            </asp:Panel>
        </div>

        <%-- Estado en su área: un tramo por estado (y Sin área); el ancho es cuántos hay. --%>
        <div class="operators-meter-k">
            <span>Elige un tramo para ver ese grupo · el ancho es cuántos hay</span>
            <asp:HyperLink ID="LnkAll" runat="server"><i class="bi bi-people" aria-hidden="true"></i>Ver todos<b><asp:Literal ID="LitAllCount" runat="server" /></b></asp:HyperLink>
        </div>
        <nav class="operators-meter" aria-label="Estado en su área">
            <asp:Repeater ID="RptMeter" runat="server">
                <ItemTemplate>
                    <a class='<%#: "operators-seg " + Eval("Css") + ((bool)Eval("Selected") ? " is-selected" : "") %>'
                        href='<%#: Eval("Url") %>' style='flex-grow: <%#: Eval("Grow") %>'
                        <%# (bool)Eval("Selected") ? "aria-current=\"true\"" : "" %>>
                        <span class="operators-seg-n"><%#: Eval("Count") %></span>
                        <span class="operators-seg-l"><i class='<%#: Eval("Icon") %>' aria-hidden="true"></i><%#: Eval("Label") %></span>
                        <span class="operators-seg-p"><%#: Eval("ShareText") %></span>
                    </a>
                </ItemTemplate>
            </asp:Repeater>
        </nav>
        <div class="operators-callout">
            <i id="IcoHint" runat="server" aria-hidden="true"></i>
            <span><b><asp:Literal ID="LitHintTitle" runat="server" /></b> <asp:Literal ID="LitHintText" runat="server" /></span>
            <asp:PlaceHolder ID="PhInactive" runat="server">
                <span class="operators-inactive-count"><i class="bi bi-clock-history" aria-hidden="true"></i><asp:Literal ID="LitInactive" runat="server" /></span>
            </asp:PlaceHolder>
        </div>
    </section>

    <section class="mt-panel" aria-labelledby="listTitle">
        <h2 id="listTitle" class="visually-hidden">Lista de operadores</h2>
        <asp:PlaceHolder ID="PhTable" runat="server">
            <div class="mt-table-wrap">
                <table class="table mt-table table-hover operators-table">
                    <thead>
                        <tr>
                            <th scope="col">Operador</th>
                            <th scope="col">Área</th>
                            <th scope="col">Estado en su área</th>
                            <th scope="col">Última actividad</th>
                            <th scope="col"><span class="visually-hidden">Detalle</span></th>
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
                                    <td>
                                        <asp:PlaceHolder runat="server" Visible='<%# (bool)Eval("HasAreaNote") %>'>
                                            <span class="mt-muted"><%#: Eval("AreaNote") %></span>
                                        </asp:PlaceHolder>
                                        <asp:Repeater runat="server" DataSource='<%# Eval("AreaGuides") %>'>
                                            <ItemTemplate>
                                                <div class="operators-area-guide">
                                                    <span class='<%#: Eval("StateCss") %>'><i class='<%#: Eval("StateIcon") %>' aria-hidden="true"></i><%#: Eval("StateText") %></span>
                                                    <span class="mt-muted mt-small d-block mt-1"><%#: Eval("ScoresText") %></span>
                                                </div>
                                            </ItemTemplate>
                                        </asp:Repeater>
                                    </td>
                                    <td class="text-nowrap">
                                        <span class="d-block"><%#: Eval("LastActivityText") %></span>
                                        <asp:PlaceHolder runat="server" Visible='<%# (bool)Eval("HasAgo") %>'>
                                            <span class='<%# (bool)Eval("Inactive") ? "operators-ago is-inactive" : "operators-ago" %>'><asp:PlaceHolder runat="server" Visible='<%# (bool)Eval("Inactive") %>'><i class="bi bi-clock-history" aria-hidden="true"></i></asp:PlaceHolder><%#: Eval("AgoText") %></span>
                                        </asp:PlaceHolder>
                                    </td>
                                    <td class="text-end">
                                        <a class="btn btn-secondary btn-sm operators-go" href='<%#: Eval("DetailUrl") %>'>Ver detalle<i class="bi bi-chevron-right" aria-hidden="true"></i><span class="visually-hidden"> de <%#: Eval("Name") %></span></a>
                                    </td>
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
