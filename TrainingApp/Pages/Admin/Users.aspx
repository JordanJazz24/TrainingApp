<%@ Page Title="Gestión de usuarios" Language="C#" MasterPageFile="~/UI/master/Admin.Master"
    AutoEventWireup="true" CodeBehind="Users.aspx.cs"
    Inherits="TrainingApp.Pages.Admin.Users" %>

<%-- Gestión de usuarios (spec 013): lista paginada en el servidor con búsqueda, alta de un
     empleado de la API con su rol, y por fila reset de contraseña, desbloqueo y borrado
     (enmienda 2026-10-13: el rol de una cuenta ya no se cambia desde aquí). Cada acción la
     verifica otra vez UserAdminService. --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="admin-page-head">
        <h1 class="admin-page-title">Gestión de usuarios</h1>
    </div>

    <section class="mt-panel mb-4" aria-labelledby="createTitle">
        <div class="mt-panel-body">
            <h2 id="createTitle" class="mt-block-title mb-3">Crear usuario</h2>
            <asp:Panel ID="PnlCreate" runat="server" CssClass="mt-filters" DefaultButton="BtnCreate">
                <div class="mt-field">
                    <asp:Label runat="server" AssociatedControlID="TxtNewCode1" CssClass="mt-field-label">Código de empleado</asp:Label>
                    <asp:TextBox ID="TxtNewCode1" runat="server" CssClass="form-control admin-no-markup" MaxLength="50" />
                    <asp:RequiredFieldValidator ID="RfvNewCode1" runat="server" ControlToValidate="TxtNewCode1"
                        ValidationGroup="create" ErrorMessage="Ingrese el código de empleado."
                        CssClass="admin-validation" Display="Dynamic" />
                </div>
                <div class="mt-field">
                    <asp:Label runat="server" AssociatedControlID="DdlNewRole" CssClass="mt-field-label">Rol</asp:Label>
                    <asp:DropDownList ID="DdlNewRole" runat="server" CssClass="form-select">
                        <asp:ListItem Value="1" Text="Operador" Selected="True" />
                        <asp:ListItem Value="2" Text="Administrador" />
                    </asp:DropDownList>
                </div>
                <asp:Button ID="BtnCreate" runat="server" Text="Crear" CssClass="btn btn-primary"
                    ValidationGroup="create" OnClick="BtnCreate_Click" />
            </asp:Panel>
            <p class="mt-muted mt-small mt-3 mb-0">
                El usuario entra con la contraseña por defecto y la cambia en su primer acceso.
            </p>
        </div>
    </section>

    <section class="mt-panel" aria-labelledby="listTitle">
        <div class="mt-panel-body d-flex flex-wrap align-items-end justify-content-between gap-3">
            <h2 id="listTitle" class="mt-block-title">Usuarios</h2>
            <asp:Panel ID="PnlSearch" runat="server" CssClass="admin-search" DefaultButton="BtnSearch">
                <div class="input-group">
                    <asp:TextBox ID="TxtSearch" runat="server" CssClass="form-control admin-no-markup" MaxLength="100"
                        placeholder="Buscar por código o nombre" aria-label="Buscar por código o nombre" />
                    <asp:Button ID="BtnSearch" runat="server" Text="Buscar" CssClass="btn btn-secondary"
                        ValidationGroup="search" OnClick="BtnSearch_Click" />
                </div>
            </asp:Panel>
        </div>

        <div class="mt-table-wrap">
            <table class="table mt-table table-hover admin-users">
                <thead>
                    <tr>
                        <th scope="col">Código</th>
                        <th scope="col">Nombre</th>
                        <th scope="col">Rol</th>
                        <th scope="col">Estado</th>
                        <th scope="col" class="mt-col-secondary">Creada</th>
                        <th scope="col">Último acceso</th>
                        <th scope="col" class="text-end">Acciones</th>
                    </tr>
                </thead>
                <tbody>
                    <asp:Repeater ID="RptUsers" runat="server" OnItemCommand="RptUsers_ItemCommand">
                        <ItemTemplate>
                            <tr>
                                <td class="fw-semibold"><%#: Eval("Code1") %></td>
                                <td><%#: Eval("Name") %></td>
                                <td><span class='<%#: Eval("RoleCss") %>'><i class='<%#: Eval("RoleIcon") %>' aria-hidden="true"></i><%#: Eval("RoleText") %></span></td>
                                <td><span class='<%#: Eval("StateCss") %>'><i class='<%#: Eval("StateIcon") %>' aria-hidden="true"></i><%#: Eval("StateText") %></span></td>
                                <td class="mt-col-secondary mt-muted"><%#: Eval("CreatedText") %></td>
                                <td class="mt-muted text-nowrap"><%#: Eval("LastLoginText") %></td>
                                <td class="text-end">
                                    <div class="admin-row-actions">
                                        <asp:LinkButton ID="BtnReset" runat="server" CssClass="btn btn-secondary btn-sm"
                                            CommandName="Reset" CommandArgument='<%# Eval("Id") %>' CausesValidation="false"
                                            data-confirm='<%# Eval("ConfirmReset") %>'
                                            OnClientClick="return adminConfirm(this);"><i class="bi bi-arrow-counterclockwise" aria-hidden="true"></i>Resetear contraseña</asp:LinkButton>
                                        <asp:LinkButton ID="BtnUnlock" runat="server" CssClass="btn btn-secondary btn-sm"
                                            CommandName="Unlock" CommandArgument='<%# Eval("Id") %>' CausesValidation="false"
                                            Visible='<%# (bool)Eval("IsLocked") %>'
                                            data-confirm='<%# Eval("ConfirmUnlock") %>'
                                            OnClientClick="return adminConfirm(this);"><i class="bi bi-unlock" aria-hidden="true"></i>Desbloquear</asp:LinkButton>
                                        <%-- Nadie se borra a sí mismo (AC-13b). Con historial, la
                                             confirmación es una alerta con lo que se pierde (AC-13). --%>
                                        <asp:LinkButton ID="BtnDelete" runat="server" CssClass="btn btn-danger-soft btn-sm"
                                            CommandName="Delete" CommandArgument='<%# Eval("Id") %>' CausesValidation="false"
                                            Visible='<%# !(bool)Eval("IsSelf") %>'
                                            data-confirm='<%# Eval("ConfirmDelete") %>'
                                            data-confirm-icon='<%# (bool)Eval("HasHistory") ? "warning" : "question" %>'
                                            OnClientClick="return adminConfirm(this);"><i class="bi bi-trash3" aria-hidden="true"></i>Borrar</asp:LinkButton>
                                    </div>
                                </td>
                            </tr>
                        </ItemTemplate>
                    </asp:Repeater>
                </tbody>
            </table>
        </div>

        <asp:Label ID="LblEmpty" runat="server" CssClass="mt-empty" Visible="false"
            Text="No hay usuarios que coincidan con la búsqueda." />

        <div class="mt-panel-body mt-pager">
            <asp:LinkButton ID="BtnPrev" runat="server" CssClass="btn btn-secondary btn-sm"
                CausesValidation="false" OnClick="BtnPrev_Click"><i class="bi bi-chevron-left" aria-hidden="true"></i>Anterior</asp:LinkButton>
            <span><asp:Literal ID="LitPageInfo" runat="server" /></span>
            <asp:LinkButton ID="BtnNext" runat="server" CssClass="btn btn-secondary btn-sm"
                CausesValidation="false" OnClick="BtnNext_Click">Siguiente<i class="bi bi-chevron-right" aria-hidden="true"></i></asp:LinkButton>
        </div>
    </section>

    <script>
        // F5 después de una acción sería un GET, no el reenvío del último postback (como en
        // GuidedTest): una acción sobre un usuario no se repite sin querer.
        if (window.history.replaceState) {
            window.history.replaceState(null, '', window.location.href);
        }

        // Formato: < y > no son parte de un código ni de un nombre, y ASP.NET rechaza un
        // postback con marcado (<x). Los campos viajan en cualquier postback de la página
        // (paginación, acciones), así que se quitan al escribir.
        document.querySelectorAll('.admin-no-markup').forEach(function (input) {
            input.addEventListener('input', function () {
                if (/[<>]/.test(input.value)) input.value = input.value.replace(/[<>]/g, '');
            });
        });

        // Confirmación con SweetAlert antes de una acción sobre un usuario: el texto viene del
        // servidor en data-confirm. Al confirmar se repite el click, que hace el postback.
        function adminConfirm(link) {
            if (link.getAttribute('data-confirmed') === '1') {
                link.removeAttribute('data-confirmed');
                return true;
            }
            Swal.fire({
                icon: link.getAttribute('data-confirm-icon') || 'question',
                text: link.getAttribute('data-confirm'),
                showCancelButton: true,
                confirmButtonText: 'Sí, continuar',
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
