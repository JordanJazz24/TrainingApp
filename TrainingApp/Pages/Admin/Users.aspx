<%@ Page Title="Gestión de usuarios" Language="C#" MasterPageFile="~/UI/master/Admin.Master"
    AutoEventWireup="true" CodeBehind="Users.aspx.cs"
    Inherits="TrainingApp.Pages.Admin.Users" %>

<%-- Gestión de usuarios (spec 013; spec 022, brief 8: "Consola de cuentas", CD-3): el directorio
     paginado en el servidor con búsqueda, a la izquierda, y la ficha de la cuenta elegida a la
     derecha (reset de contraseña, desbloqueo, área y borrado). Alta de un empleado de la API con
     su rol en un panel que sustituye a la ficha. Elegir una cuenta, abrir Crear y abrir o
     cancelar el área son del cliente, sin postback. Cada acción la verifica otra vez
     UserAdminService. --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div id="usersShell" class="users-shell">
        <section class="users-list" aria-label="Lista de usuarios">
            <div class="users-list-head">
                <div class="users-list-top">
                    <div>
                        <div class="users-eyebrow">Administración · Cuentas</div>
                        <h1 class="users-title">Usuarios</h1>
                    </div>
                    <button type="button" class="btn btn-primary btn-sm" onclick="usersShowCreate();"><i class="bi bi-person-plus" aria-hidden="true"></i>Crear usuario</button>
                </div>

                <asp:Panel ID="PnlSearch" runat="server" CssClass="admin-search" DefaultButton="BtnSearch">
                    <div class="input-group">
                        <asp:TextBox ID="TxtSearch" runat="server" CssClass="form-control admin-no-markup" MaxLength="100"
                            placeholder="Buscar por código o nombre" aria-label="Buscar por código o nombre" />
                        <asp:LinkButton ID="BtnSearch" runat="server" CssClass="btn btn-secondary"
                            ValidationGroup="search" OnClick="BtnSearch_Click"><i class="bi bi-search" aria-hidden="true"></i><span class="visually-hidden">Buscar</span></asp:LinkButton>
                    </div>
                </asp:Panel>

                <div class="users-summary">
                    <span>En esta página:</span>
                    <span><b><asp:Literal ID="LitCountLocked" runat="server" /></b> bloqueadas</span>
                    <span><b><asp:Literal ID="LitCountPending" runat="server" /></b> deben cambiar la contraseña</span>
                    <span><b><asp:Literal ID="LitCountNoArea" runat="server" /></b> sin área</span>
                </div>
            </div>

            <%-- Solo presentación: una fila por cuenta, sin comandos. Elegirla muestra su ficha (cliente). --%>
            <div role="listbox" aria-label="Usuarios">
                <asp:Repeater ID="RptDirectory" runat="server">
                    <ItemTemplate>
                        <button type="button" role="option" class='users-row<%# (bool)Eval("IsSelected") ? " is-selected" : "" %>'
                            data-id='<%#: Eval("Id") %>' aria-selected='<%# (bool)Eval("IsSelected") ? "true" : "false" %>'
                            onclick="usersSelect(this);">
                            <span class='users-avatar<%# (bool)Eval("IsAdmin") ? " is-admin" : "" %>' aria-hidden="true"><%#: Eval("Initials") %></span>
                            <span class="users-row-name">
                                <b><%#: Eval("Name") %><%# (bool)Eval("IsSelf") ? "<span class=\"users-tag-you\">tú</span>" : "" %></b>
                                <span class="users-row-sub"><%#: Eval("Code1") %> · <%#: Eval("AreaShort") %></span>
                            </span>
                            <asp:PlaceHolder runat="server" Visible='<%# (bool)Eval("IsProblem") %>'>
                                <span class='<%#: Eval("StateCss") %>'><i class='<%#: Eval("StateIcon") %>' aria-hidden="true"></i><%#: Eval("StateShort") %></span>
                            </asp:PlaceHolder>
                            <asp:PlaceHolder runat="server" Visible='<%# !(bool)Eval("IsProblem") %>'>
                                <span class="users-row-ok"><i class="bi bi-check-circle" aria-hidden="true"></i>Activa</span>
                            </asp:PlaceHolder>
                            <i class="bi bi-chevron-right mt-muted" aria-hidden="true"></i>
                        </button>
                    </ItemTemplate>
                </asp:Repeater>
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

        <div class="users-scrim" onclick="usersCloseDetail();"></div>

        <div class="users-detail">
            <%-- Crear usuario: sustituye a la ficha (cliente). --%>
            <asp:Panel ID="PnlCreate" runat="server" CssClass="users-pane users-create" DefaultButton="BtnCreate" hidden="hidden" aria-label="Crear usuario">
                <div class="users-pane-head" tabindex="-1">
                    <span class="users-pane-avatar" aria-hidden="true"><i class="bi bi-person-plus"></i></span>
                    <div>
                        <div class="users-pane-name">Crear usuario</div>
                        <div class="users-pane-sub">Un empleado de la lista de la planta, con su rol</div>
                    </div>
                    <button type="button" class="btn btn-secondary btn-sm users-back" onclick="usersCloseCreate();"><i class="bi bi-arrow-left" aria-hidden="true"></i>Volver a la lista</button>
                </div>
                <div class="users-sec">
                    <div class="users-create-form">
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
                        <div class="users-area-edit-actions">
                            <asp:LinkButton ID="BtnCreate" runat="server" CssClass="btn btn-primary"
                                ValidationGroup="create" OnClick="BtnCreate_Click"><i class="bi bi-plus-lg" aria-hidden="true"></i>Crear</asp:LinkButton>
                            <button type="button" class="btn btn-secondary" onclick="usersCloseCreate();">Cancelar</button>
                        </div>
                        <p class="mt-muted mt-small mb-0">
                            Entra con la contraseña por defecto y la cambia en su primer acceso. El área la elige después el propio
                            operador, o se corrige aquí desde su ficha.
                        </p>
                    </div>
                </div>
            </asp:Panel>

            <%-- Una ficha por cuenta; solo la elegida está visible (HfSelected). --%>
            <asp:HiddenField ID="HfSelected" runat="server" />
            <asp:Repeater ID="RptUsers" runat="server" OnItemCommand="RptUsers_ItemCommand">
                <ItemTemplate>
                    <section class="users-pane" data-id='<%#: Eval("Id") %>'<%# (bool)Eval("IsSelected") ? "" : " hidden" %>
                        aria-label='<%#: "Ficha de " + Eval("Who") %>'>
                        <div class="users-pane-head" tabindex="-1">
                            <span class="users-pane-avatar" aria-hidden="true"><%#: Eval("Initials") %></span>
                            <div>
                                <div class="users-pane-name"><%#: Eval("Name") %><%# (bool)Eval("IsSelf") ? "<span class=\"users-tag-you\">tú</span>" : "" %></div>
                                <div class="users-pane-sub">Code1 <%#: Eval("Code1") %></div>
                            </div>
                            <div class="users-pane-badges">
                                <span class='<%#: Eval("RoleCss") %>'><i class='<%#: Eval("RoleIcon") %>' aria-hidden="true"></i><%#: Eval("RoleText") %></span>
                                <span class='<%#: Eval("StateCss") %>'><i class='<%#: Eval("StateIcon") %>' aria-hidden="true"></i><%#: Eval("StateText") %></span>
                            </div>
                            <button type="button" class="btn btn-secondary btn-sm users-back" onclick="usersCloseDetail();"><i class="bi bi-arrow-left" aria-hidden="true"></i>Volver a la lista</button>
                        </div>

                        <div class="users-sec">
                            <div class="users-sec-k">Acceso</div>
                            <p class="mb-3"><i class='<%#: Eval("HintIcon") %>' aria-hidden="true"></i> <%#: Eval("LongHint") %></p>
                            <asp:PlaceHolder runat="server" Visible='<%# (bool)Eval("IsLocked") %>'>
                                <div class="users-act">
                                    <div class="users-act-t"><b>Desbloquear la cuenta</b><small>Puede entrar de nuevo sin esperar a que venza el bloqueo.</small></div>
                                    <asp:LinkButton ID="BtnUnlock" runat="server" CssClass="btn btn-primary"
                                        CommandName="Unlock" CommandArgument='<%# Eval("Id") %>' CausesValidation="false"
                                        data-confirm='<%# Eval("ConfirmUnlock") %>'
                                        OnClientClick="return adminConfirm(this);"><i class="bi bi-unlock" aria-hidden="true"></i>Desbloquear</asp:LinkButton>
                                </div>
                            </asp:PlaceHolder>
                            <div class="users-act">
                                <div class="users-act-t"><b>Resetear la contraseña</b><small>Entrará con la contraseña por defecto y deberá cambiarla. También desbloquea.</small></div>
                                <asp:LinkButton ID="BtnReset" runat="server" CssClass="btn btn-secondary"
                                    CommandName="Reset" CommandArgument='<%# Eval("Id") %>' CausesValidation="false"
                                    data-confirm='<%# Eval("ConfirmReset") %>'
                                    OnClientClick="return adminConfirm(this);"><i class="bi bi-arrow-counterclockwise" aria-hidden="true"></i>Resetear contraseña</asp:LinkButton>
                            </div>
                        </div>

                        <div class="users-sec">
                            <div class="users-sec-k">Área</div>
                            <asp:PlaceHolder runat="server" Visible='<%# (bool)Eval("IsAdmin") %>'>
                                <p class="mt-muted mb-0">— Los administradores no tienen área.</p>
                            </asp:PlaceHolder>
                            <%-- Spec 021 (D-172, P-11): solo en cuentas de Operator (AC-10). "Cambiar área" abre aquí
                                 los radios agrupados por producto; Guardar confirma, sin postback hasta entonces. --%>
                            <asp:PlaceHolder runat="server" Visible='<%# (bool)Eval("IsOperator") %>'>
                                <div class="users-act users-area-idle">
                                    <div class="users-act-t"><b><%#: Eval("AreaText") %></b><small><%#: Eval("AreaNote") %></small></div>
                                    <button type="button" class="btn btn-secondary" onclick="usersAreaOpen(this);"
                                        aria-label='<%#: "Cambiar el área de " + Eval("Who") %>'><i class="bi bi-pencil" aria-hidden="true"></i>Cambiar área</button>
                                </div>
                                <div class="users-area-edit" data-area-id='<%#: Eval("AreaId") %>' hidden>
                                    <div class="users-area-groups" role="radiogroup" aria-label='<%#: "Área de " + Eval("Who") %>'></div>
                                    <div class="users-area-edit-actions">
                                        <asp:LinkButton ID="BtnSetArea" runat="server" CssClass="btn btn-primary disabled" aria-disabled="true"
                                            CommandName="SetArea" CommandArgument='<%# Eval("Id") %>' CausesValidation="false"
                                            data-who='<%# Eval("Who") %>'
                                            OnClientClick="return usersAreaConfirm(this);"><i class="bi bi-check2" aria-hidden="true"></i>Guardar</asp:LinkButton>
                                        <button type="button" class="btn btn-secondary" onclick="usersAreaClose(this);">Cancelar</button>
                                    </div>
                                </div>
                            </asp:PlaceHolder>
                        </div>

                        <div class="users-sec">
                            <div class="users-sec-k">Cuenta</div>
                            <dl class="users-facts">
                                <div><dt>Alta</dt><dd><%#: Eval("CreatedText") %></dd></div>
                                <div><dt>Último acceso</dt><dd><%#: Eval("LastLoginText") %></dd><dd class="users-facts-sub"><%#: Eval("LastAgoText") %></dd></div>
                                <div><dt>Sesiones de entrenamiento</dt><dd><%#: Eval("SessionsText") %></dd></div>
                            </dl>
                        </div>

                        <%-- Nadie se borra a sí mismo (AC-13b): el botón sale deshabilitado con el motivo. Con
                             historial, la confirmación es una alerta con lo que se pierde (AC-13). --%>
                        <div class="users-danger">
                            <h3><i class="bi bi-trash3" aria-hidden="true"></i>Borrar usuario</h3>
                            <p><%#: Eval("DeleteNote") %></p>
                            <asp:LinkButton ID="BtnDelete" runat="server" CssClass="btn btn-danger-soft"
                                CommandName="Delete" CommandArgument='<%# Eval("Id") %>' CausesValidation="false"
                                Visible='<%# !(bool)Eval("IsSelf") %>'
                                data-confirm='<%# Eval("ConfirmDelete") %>'
                                data-confirm-icon='<%# (bool)Eval("HasHistory") ? "warning" : "question" %>'
                                OnClientClick="return adminConfirm(this);"><i class="bi bi-trash3" aria-hidden="true"></i>Borrar a <%#: Eval("Name") %></asp:LinkButton>
                            <asp:PlaceHolder runat="server" Visible='<%# (bool)Eval("IsSelf") %>'>
                                <button type="button" class="btn btn-secondary" disabled><i class="bi bi-trash3" aria-hidden="true"></i>Borrar</button>
                            </asp:PlaceHolder>
                        </div>
                    </section>
                </ItemTemplate>
            </asp:Repeater>
        </div>
    </div>

    <%-- Áreas que se pueden asignar (spec 021, D-176), agrupadas por producto: cada ficha las
         copia, como radios, al abrir Cambiar área. --%>
    <template id="usersAreaOptions">
        <asp:Repeater ID="RptAreaGroups" runat="server" EnableViewState="false">
            <ItemTemplate>
                <div>
                    <div class="users-area-title"><%#: Eval("ProductName") %></div>
                    <div class="users-area-opts">
                        <asp:Repeater runat="server" DataSource='<%# Eval("Areas") %>'>
                            <ItemTemplate>
                                <label class="users-area-opt"><input type="radio" value="<%#: Eval("AreaId") %>" data-label="<%#: Eval("Label") %>"><%#: Eval("AreaName") %></label>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                </div>
            </ItemTemplate>
        </asp:Repeater>
    </template>

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

        var usersShell = document.getElementById('usersShell');
        var usersCreate = document.getElementById('<%= PnlCreate.ClientID %>');
        var usersSelected = document.getElementById('<%= HfSelected.ClientID %>');
        var usersNarrow = window.matchMedia('(max-width: 991.98px)');

        // Bajo 992 px la ficha es un panel fijo: se abre con foco y se cierra con Esc o "Volver a la lista".
        function usersOpenDetail(pane) {
            if (!usersNarrow.matches) return;
            usersShell.classList.add('is-open');
            var head = pane.querySelector('.users-pane-head');
            if (head) head.focus();
        }

        function usersCloseDetail() {
            usersShell.classList.remove('is-open');
        }

        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && usersShell.classList.contains('is-open')) usersCloseDetail();
        });

        function usersPane(id) {
            return document.querySelector('.users-pane[data-id="' + id + '"]');
        }

        // Elegir una cuenta: muestra su ficha (sin postback). HfSelected la recuerda para el
        // próximo postback.
        function usersSelect(row) {
            var id = row.getAttribute('data-id');
            usersShow(id);
            usersOpenDetail(usersPane(id));
        }

        function usersShow(id) {
            usersAreaResetAll();
            document.querySelectorAll('.users-row').forEach(function (r) {
                var on = r.getAttribute('data-id') === id;
                r.classList.toggle('is-selected', on);
                r.setAttribute('aria-selected', on ? 'true' : 'false');
            });
            document.querySelectorAll('.users-pane[data-id]').forEach(function (p) {
                p.hidden = p.getAttribute('data-id') !== id;
            });
            usersCreate.hidden = true;
            usersSelected.value = id;
        }

        function usersShowCreate() {
            usersAreaResetAll();
            document.querySelectorAll('.users-row').forEach(function (r) {
                r.classList.remove('is-selected');
                r.setAttribute('aria-selected', 'false');
            });
            document.querySelectorAll('.users-pane[data-id]').forEach(function (p) { p.hidden = true; });
            usersCreate.hidden = false;
            usersOpenDetail(usersCreate);
            document.getElementById('<%= TxtNewCode1.ClientID %>').focus();
        }

        // Cancelar o volver: la ficha que estaba elegida.
        function usersCloseCreate() {
            if (usersSelected.value && usersPane(usersSelected.value)) usersShow(usersSelected.value);
            else usersCreate.hidden = true;
            usersCloseDetail();
        }

        // Cambiar área (spec 021, P-11): copia las áreas, agrupadas por producto, a la ficha y
        // preselecciona la actual (ninguna si no tiene). Guardar queda deshabilitado hasta elegir
        // un área distinta de la actual; la BLL valida igual (SetArea).
        function usersAreaOpen(button) {
            var pane = button.closest('.users-pane');
            var edit = pane.querySelector('.users-area-edit');
            var box = edit.querySelector('.users-area-groups');
            if (!box.firstChild) {
                box.appendChild(document.getElementById('usersAreaOptions').content.cloneNode(true));
                var current = edit.getAttribute('data-area-id');
                box.querySelectorAll('input[type=radio]').forEach(function (radio) {
                    radio.name = 'area_' + pane.getAttribute('data-id');
                    radio.checked = radio.value === current;
                    radio.addEventListener('change', function () { usersAreaSync(pane); });
                });
            }
            pane.querySelector('.users-area-idle').hidden = true;
            edit.hidden = false;
            usersAreaSync(pane);
            var first = box.querySelector('input:checked') || box.querySelector('input');
            if (first) first.focus();
        }

        function usersAreaSync(pane) {
            var edit = pane.querySelector('.users-area-edit');
            var chosen = edit.querySelector('input:checked');
            edit.querySelectorAll('.users-area-opt').forEach(function (label) {
                label.classList.toggle('is-selected', label.querySelector('input').checked);
            });
            var save = edit.querySelector('.btn-primary');
            var off = !chosen || chosen.value === edit.getAttribute('data-area-id');
            save.classList.toggle('disabled', off);
            save.setAttribute('aria-disabled', off ? 'true' : 'false');
        }

        // Cancelar: vuelve al área y "Cambiar área", sin postback.
        function usersAreaClose(button) {
            usersAreaReset(button.closest('.users-pane'));
        }

        function usersAreaReset(pane) {
            var edit = pane.querySelector('.users-area-edit');
            if (!edit) return;
            edit.querySelector('.users-area-groups').innerHTML = '';
            edit.hidden = true;
            pane.querySelector('.users-area-idle').hidden = false;
        }

        function usersAreaResetAll() {
            document.querySelectorAll('.users-pane[data-id]').forEach(usersAreaReset);
        }

        // Guardar: la confirmación de siempre, con el área elegida (texto, no HTML).
        function usersAreaConfirm(link) {
            if (link.getAttribute('data-confirmed') !== '1') {
                var option = link.closest('.users-area-edit').querySelector('input:checked');
                if (!option || link.classList.contains('disabled')) return false;
                link.setAttribute('data-confirm', '¿Cambiar el área de ' + link.getAttribute('data-who') +
                    ' a ' + option.getAttribute('data-label') + '?');
            }
            return adminConfirm(link);
        }

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
