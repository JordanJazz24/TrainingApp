<%@ Page Title="Gestión de usuarios" Language="C#" MasterPageFile="~/UI/master/Admin.Master"
    AutoEventWireup="true" CodeBehind="Users.aspx.cs"
    Inherits="TrainingApp.Pages.Admin.Users" %>

<%-- Gestión de usuarios (spec 013): lista paginada en el servidor con búsqueda, alta de un
     empleado de la API con su rol, y por fila reset de contraseña, desbloqueo y borrado
     (enmienda 2026-10-13: el rol de una cuenta ya no se cambia desde aquí). Cada acción la
     verifica otra vez UserAdminService. --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <h1 class="h3 mb-4">Gestión de usuarios</h1>

    <div class="card mb-4">
        <div class="card-body">
            <h2 class="h6 mb-3">Crear usuario</h2>
            <asp:Panel ID="PnlCreate" runat="server" CssClass="row g-2 align-items-start" DefaultButton="BtnCreate">
                <div class="col-sm-5 col-lg-4">
                    <asp:TextBox ID="TxtNewCode1" runat="server" CssClass="form-control admin-no-markup" MaxLength="50"
                        placeholder="Código de empleado" />
                    <asp:RequiredFieldValidator ID="RfvNewCode1" runat="server" ControlToValidate="TxtNewCode1"
                        ValidationGroup="create" ErrorMessage="Ingrese el código de empleado."
                        CssClass="admin-validation" Display="Dynamic" />
                </div>
                <div class="col-sm-4 col-lg-3">
                    <asp:DropDownList ID="DdlNewRole" runat="server" CssClass="form-select">
                        <asp:ListItem Value="1" Text="Operador" Selected="True" />
                        <asp:ListItem Value="2" Text="Administrador" />
                    </asp:DropDownList>
                </div>
                <div class="col-sm-3 col-lg-2">
                    <asp:Button ID="BtnCreate" runat="server" Text="Crear" CssClass="btn btn-primary w-100"
                        ValidationGroup="create" OnClick="BtnCreate_Click" />
                </div>
            </asp:Panel>
            <p class="text-muted small mt-2 mb-0">
                El usuario entra con la contraseña por defecto y la cambia en su primer acceso.
            </p>
        </div>
    </div>

    <asp:Panel ID="PnlSearch" runat="server" CssClass="admin-search mb-3" DefaultButton="BtnSearch">
        <div class="input-group">
            <asp:TextBox ID="TxtSearch" runat="server" CssClass="form-control admin-no-markup" MaxLength="100"
                placeholder="Buscar por código o nombre" />
            <asp:Button ID="BtnSearch" runat="server" Text="Buscar" CssClass="btn btn-outline-primary"
                ValidationGroup="search" OnClick="BtnSearch_Click" />
        </div>
    </asp:Panel>

    <div class="table-responsive">
        <table class="table table-sm table-hover bg-white admin-users">
            <thead>
                <tr>
                    <th>Código</th>
                    <th>Nombre</th>
                    <th>Rol</th>
                    <th>Estado</th>
                    <th>Creada</th>
                    <th>Último acceso</th>
                    <th class="text-end">Acciones</th>
                </tr>
            </thead>
            <tbody>
                <asp:Repeater ID="RptUsers" runat="server" OnItemCommand="RptUsers_ItemCommand">
                    <ItemTemplate>
                        <tr>
                            <td><%#: Eval("Code1") %></td>
                            <td><%#: Eval("Name") %></td>
                            <td><%#: Eval("RoleText") %></td>
                            <td><span class='<%#: Eval("StateCss") %>'><%#: Eval("StateText") %></span></td>
                            <td><%#: Eval("CreatedText") %></td>
                            <td><%#: Eval("LastLoginText") %></td>
                            <td class="text-end text-nowrap">
                                <asp:LinkButton ID="BtnReset" runat="server" CssClass="btn btn-link btn-sm"
                                    CommandName="Reset" CommandArgument='<%# Eval("Id") %>' CausesValidation="false"
                                    data-confirm='<%# Eval("ConfirmReset") %>'
                                    OnClientClick="return adminConfirm(this);">Resetear contraseña</asp:LinkButton>
                                <asp:LinkButton ID="BtnUnlock" runat="server" CssClass="btn btn-link btn-sm"
                                    CommandName="Unlock" CommandArgument='<%# Eval("Id") %>' CausesValidation="false"
                                    Visible='<%# (bool)Eval("IsLocked") %>'
                                    data-confirm='<%# Eval("ConfirmUnlock") %>'
                                    OnClientClick="return adminConfirm(this);">Desbloquear</asp:LinkButton>
                                <%-- Nadie se borra a sí mismo (AC-13b). Con historial, la
                                     confirmación es una alerta con lo que se pierde (AC-13). --%>
                                <asp:LinkButton ID="BtnDelete" runat="server" CssClass="btn btn-link btn-sm text-danger"
                                    CommandName="Delete" CommandArgument='<%# Eval("Id") %>' CausesValidation="false"
                                    Visible='<%# !(bool)Eval("IsSelf") %>'
                                    data-confirm='<%# Eval("ConfirmDelete") %>'
                                    data-confirm-icon='<%# (bool)Eval("HasHistory") ? "warning" : "question" %>'
                                    OnClientClick="return adminConfirm(this);">Borrar</asp:LinkButton>
                            </td>
                        </tr>
                    </ItemTemplate>
                </asp:Repeater>
            </tbody>
        </table>
    </div>

    <asp:Label ID="LblEmpty" runat="server" CssClass="text-muted d-block mb-3" Visible="false"
        Text="No hay usuarios que coincidan con la búsqueda." />

    <div class="d-flex align-items-center gap-3">
        <asp:LinkButton ID="BtnPrev" runat="server" CssClass="btn btn-outline-secondary btn-sm"
            CausesValidation="false" OnClick="BtnPrev_Click">&laquo; Anterior</asp:LinkButton>
        <span class="small text-muted"><asp:Literal ID="LitPageInfo" runat="server" /></span>
        <asp:LinkButton ID="BtnNext" runat="server" CssClass="btn btn-outline-secondary btn-sm"
            CausesValidation="false" OnClick="BtnNext_Click">Siguiente &raquo;</asp:LinkButton>
    </div>

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
