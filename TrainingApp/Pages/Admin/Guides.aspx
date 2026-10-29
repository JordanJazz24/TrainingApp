<%@ Page Title="Guías" Language="C#" MasterPageFile="~/UI/master/Admin.Master"
    AutoEventWireup="true" CodeBehind="Guides.aspx.cs"
    Inherits="TrainingApp.Pages.Admin.Guides" %>

<%-- Atributos de controles de servidor (LinkButton, DropDownList) con <%# %>: ASP.NET ya los
     codifica al pintarlos; con <%#: %> saldrían codificados dos veces.
     Guías (spec 020): el tipo de Resource Setup de cada guía publicada (D-199), con un selector
     y un Guardar por fila (D-195). Vale para las sesiones que empiecen después (D-188). Cada
     cambio lo verifica otra vez GuideAdminService. --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="admin-page-head">
        <h1 class="admin-page-title">Guías</h1>
        <p class="admin-page-lead">
            Elija la pantalla de Resource Setup de cada guía. El cambio vale para las sesiones que
            empiecen desde ahora; las que están en curso siguen con la suya.
        </p>
    </div>

    <section class="mt-panel" aria-labelledby="guidesTitle">
        <div class="mt-panel-body guides-head">
            <h2 id="guidesTitle" class="mt-block-title">Guías publicadas</h2>
            <%-- Conteo por tipo (brief 022, 2b); se oculta si no hay guías. --%>
            <asp:PlaceHolder ID="PhCounts" runat="server">
                <div class="guides-counts">
                    <span class="mt-chip"><i class="bi bi-gear" aria-hidden="true"></i><b><asp:Literal ID="LitCountStandard" runat="server" /></b> Estándar</span>
                    <span class="mt-chip"><i class="bi bi-stars" aria-hidden="true"></i><b><asp:Literal ID="LitCountCustom" runat="server" /></b> Custom</span>
                </div>
            </asp:PlaceHolder>
        </div>

        <div class="mt-table-wrap">
            <table class="table mt-table table-hover">
                <thead>
                    <tr>
                        <th scope="col">Producto</th>
                        <th scope="col">Área</th>
                        <th scope="col">Guía</th>
                        <th scope="col">Resource Setup</th>
                        <th scope="col" class="text-end">Cambiar a</th>
                    </tr>
                </thead>
                <tbody>
                    <asp:Repeater ID="RptGuides" runat="server" OnItemCommand="RptGuides_ItemCommand">
                        <ItemTemplate>
                            <tr>
                                <td><%#: Eval("ProductName") %></td>
                                <td><%#: Eval("AreaName") %></td>
                                <td class="fw-semibold"><%#: Eval("Title") %></td>
                                <td><span class='<%#: Eval("TypeCss") %>'><i class='<%#: Eval("TypeIcon") %>' aria-hidden="true"></i><%#: Eval("TypeText") %></span></td>
                                <td class="text-end">
                                    <div class="admin-row-actions">
                                        <asp:DropDownList ID="DdlType" runat="server" CssClass="form-select form-select-sm w-auto"
                                            SelectedValue='<%# Eval("TypeValue") %>'
                                            aria-label='<%# "Resource Setup de " + Eval("Title") %>'>
                                            <asp:ListItem Value="0" Text="Estándar" />
                                            <asp:ListItem Value="1" Text="Custom" />
                                        </asp:DropDownList>
                                        <asp:LinkButton ID="BtnSave" runat="server" CssClass="btn btn-secondary btn-sm"
                                            CommandName="SetType" CommandArgument='<%# Eval("Id") %>' CausesValidation="false"
                                            data-confirm='<%# Eval("ConfirmText") %>'
                                            OnClientClick="return adminConfirm(this);"><i class="bi bi-check2" aria-hidden="true"></i>Guardar</asp:LinkButton>
                                    </div>
                                </td>
                            </tr>
                        </ItemTemplate>
                    </asp:Repeater>
                </tbody>
            </table>
        </div>

        <asp:Label ID="LblEmpty" runat="server" CssClass="mt-empty" Visible="false"
            Text="No hay guías publicadas." />

        <asp:PlaceHolder ID="PhOrderNote" runat="server">
            <p class="guides-note"><i class="bi bi-info-circle" aria-hidden="true"></i>Las guías con Custom van primero.</p>
        </asp:PlaceHolder>
    </section>

    <script>
        // F5 después de guardar sería un GET, no el reenvío del último postback (como Usuarios).
        if (window.history.replaceState) {
            window.history.replaceState(null, '', window.location.href);
        }

        // Confirmación antes de guardar (D-195): el texto viene del servidor en data-confirm.
        // Al confirmar se repite el click, que hace el postback.
        function adminConfirm(link) {
            if (link.getAttribute('data-confirmed') === '1') {
                link.removeAttribute('data-confirmed');
                return true;
            }
            Swal.fire({
                icon: 'question',
                text: link.getAttribute('data-confirm'),
                showCancelButton: true,
                confirmButtonText: 'Sí, guardar',
                cancelButtonText: 'Cancelar'
            }).then(function (r) {
                if (!r.isConfirmed) return;
                link.setAttribute('data-confirmed', '1');
                link.click();
            });
            return false;
        }
    </script>
</asp:Content>
