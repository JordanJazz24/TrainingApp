<%@ Control Language="C#" AutoEventWireup="true"
    CodeBehind="SearchControl.ascx.cs"
    Inherits="TrainingApp.UI.Controls.Training.SearchControl"
    EnableViewState="false" %>

<%-- 4 modales secuenciales del proceso Start para Moldeo/R&C.
     La navegación 1→2→3→4 y las validaciones simples ocurren 100% en el cliente
     (patrón 1 postback / paso: ver feedback-webforms-postback-discipline).
     El ÚNICO postback de la secuencia es el [Save] del Modal 4 (BtnSave4),
     que entrega el resultado al servidor vía OnStepSubmit — igual que
     ResourceSetup/LineClearance. GuidedTest solo sabe del resultado final. --%>

<%-- ══ Modal 1: ID del Molde ════════════════════════════════════════════ --%>
<div class="modal fade" id="modalSearch1" tabindex="-1" data-bs-backdrop="static">
    <div class="modal-dialog">
        <div class="modal-content">
            <%-- Barra estándar (X = Cancel), subencabezado y fila de contexto (entrega 4). --%>
            <div class="modal-header">
                <h6 class="modal-title">Data Collection Entry</h6>
                <div class="mes-titlebar-actions">
                    <button type="button" class="mes-titlebar-btn" title="Maximize"
                        onclick="mesToggleMaximize(this)">&#10530;</button>
                    <button type="button" class="mes-titlebar-btn" title="Close"
                        onclick="searchCancel()">&#10005;</button>
                </div>
            </div>
            <div class="mes-subheader">
                <span>Data Collection Entry</span>
                <span class="mes-help" title="Help">?</span>
            </div>
            <div class="mes-info-row">
                DC Group: <strong>DCGTM_ID_MOLDE/A</strong> &nbsp;&nbsp;
                Resource: <asp:Literal ID="LitResource1" runat="server" />
            </div>
            <div class="modal-body">
                <div class="mb-3">
                    <label class="form-label small fw-semibold">
                        <span class="required-star">★</span> Ingrese el ID del molde:
                        <span class="text-muted ms-1 fw-normal">Data Type: Numeric</span>
                    </label>
                    <asp:TextBox ID="TxtMoldeId" runat="server" CssClass="form-control form-control-sm" />
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-sm btn-primary" onclick="searchNext1()">Save</button>
                <button type="button" class="btn btn-sm btn-secondary" onclick="searchCancel()">Cancel</button>
            </div>
        </div>
    </div>
</div>

<%-- ══ Modal 2: Verificaciones de Etiquetas ════════════════════════════ --%>
<div class="modal fade" id="modalSearch2" tabindex="-1" data-bs-backdrop="static">
    <div class="modal-dialog">
        <div class="modal-content">
            <%-- Barra estándar (X = Cancel), subencabezado y fila de contexto (entrega 4). --%>
            <div class="modal-header">
                <h6 class="modal-title">Data Collection Entry</h6>
                <div class="mes-titlebar-actions">
                    <button type="button" class="mes-titlebar-btn" title="Maximize"
                        onclick="mesToggleMaximize(this)">&#10530;</button>
                    <button type="button" class="mes-titlebar-btn" title="Close"
                        onclick="searchCancel()">&#10005;</button>
                </div>
            </div>
            <div class="mes-subheader">
                <span>Data Collection Entry</span>
                <span class="mes-help" title="Help">?</span>
            </div>
            <div class="mes-info-row">
                DC Group: <strong>DCGTM_VERIFICACIONES/A</strong> &nbsp;&nbsp;
                Resource: <asp:Literal ID="LitResource2" runat="server" />
            </div>
            <div class="modal-body">
                <div class="mb-3">
                    <label class="form-label small fw-semibold">
                        <span class="required-star">★</span> ¿Se verificaron las etiquetas de calibración?
                        <span class="text-muted ms-1 fw-normal">Data Type: Boolean</span>
                    </label>
                    <asp:DropDownList ID="DdlVerif1" runat="server" CssClass="form-select form-select-sm">
                        <asp:ListItem Value="">( Seleccione )</asp:ListItem>
                        <asp:ListItem Value="false">NO</asp:ListItem>
                        <asp:ListItem Value="true">Sí</asp:ListItem>
                    </asp:DropDownList>
                </div>
                <div class="mb-3">
                    <label class="form-label small fw-semibold">
                        <span class="required-star">★</span> ¿Se verificó D000128682 o D000207232?
                        <span class="text-muted ms-1 fw-normal">Data Type: Boolean</span>
                    </label>
                    <asp:DropDownList ID="DdlVerif2" runat="server" CssClass="form-select form-select-sm">
                        <asp:ListItem Value="">( Seleccione )</asp:ListItem>
                        <asp:ListItem Value="false">NO</asp:ListItem>
                        <asp:ListItem Value="true">Sí</asp:ListItem>
                    </asp:DropDownList>
                </div>
                <div class="mb-3">
                    <label class="form-label small fw-semibold">
                        <span class="required-star">★</span> ¿Se verificó según D000128035 o D000207299?
                        <span class="text-muted ms-1 fw-normal">Data Type: Boolean</span>
                    </label>
                    <asp:DropDownList ID="DdlVerif3" runat="server" CssClass="form-select form-select-sm">
                        <asp:ListItem Value="">( Seleccione )</asp:ListItem>
                        <asp:ListItem Value="false">NO</asp:ListItem>
                        <asp:ListItem Value="true">Sí</asp:ListItem>
                    </asp:DropDownList>
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-sm btn-primary" onclick="searchNext2()">Save</button>
                <button type="button" class="btn btn-sm btn-secondary" onclick="searchCancel()">Cancel</button>
            </div>
        </div>
    </div>
</div>

<%-- ══ Modal 3: Parámetros Técnicos MSS ════════════════════════════════ --%>
<div class="modal fade" id="modalSearch3" tabindex="-1" data-bs-backdrop="static">
    <div class="modal-dialog modal-lg">
        <div class="modal-content">
            <%-- Barra estándar (X = Cancel), subencabezado y fila de contexto (entrega 4). --%>
            <div class="modal-header">
                <h6 class="modal-title">Data Collection Entry</h6>
                <div class="mes-titlebar-actions">
                    <button type="button" class="mes-titlebar-btn" title="Maximize"
                        onclick="mesToggleMaximize(this)">&#10530;</button>
                    <button type="button" class="mes-titlebar-btn" title="Close"
                        onclick="searchCancel()">&#10005;</button>
                </div>
            </div>
            <div class="mes-subheader">
                <span>Data Collection Entry</span>
                <span class="mes-help" title="Help">?</span>
            </div>
            <div class="mes-info-row">
                DC Group: <strong>DCGTM_PARAMETROS_MSS_D000281359/A</strong> &nbsp;&nbsp;
                Resource: <asp:Literal ID="LitResource3" runat="server" />
            </div>
            <div class="modal-body">
                <div class="row g-2">
                    <div class="col-12">
                        <label class="form-label small fw-semibold mb-1">
                            <span class="required-star">★</span> Cooling Time según MSS D000281359:
                            <span class="text-muted fw-normal">Numeric</span>
                        </label>
                        <asp:TextBox ID="TxtMss1" runat="server" CssClass="form-control form-control-sm" />
                    </div>
                    <div class="col-12">
                        <label class="form-label small fw-semibold mb-1">
                            <span class="required-star">★</span> Velocidad de inyección según MSS D000281359:
                            <span class="text-muted fw-normal">Numeric</span>
                        </label>
                        <asp:TextBox ID="TxtMss2" runat="server" CssClass="form-control form-control-sm" />
                    </div>
                    <div class="col-12">
                        <label class="form-label small fw-semibold mb-1">
                            <span class="required-star">★</span> Intensidad de pad printing:
                            <span class="text-muted fw-normal">Numeric</span>
                        </label>
                        <asp:TextBox ID="TxtMss3" runat="server" CssClass="form-control form-control-sm" />
                    </div>
                    <div class="col-12">
                        <label class="form-label small fw-semibold mb-1">
                            <span class="required-star">★</span> Temperatura Zona 1:
                            <span class="text-muted fw-normal">Numeric</span>
                        </label>
                        <asp:TextBox ID="TxtMss4" runat="server" CssClass="form-control form-control-sm" />
                    </div>
                    <div class="col-12">
                        <label class="form-label small fw-semibold mb-1">
                            <span class="required-star">★</span> Temperatura Zona 2:
                            <span class="text-muted fw-normal">Numeric</span>
                        </label>
                        <asp:TextBox ID="TxtMss5" runat="server" CssClass="form-control form-control-sm" />
                    </div>
                    <div class="col-12">
                        <label class="form-label small fw-semibold mb-1">
                            <span class="required-star">★</span> Temperatura Zona 3:
                            <span class="text-muted fw-normal">Numeric</span>
                        </label>
                        <asp:TextBox ID="TxtMss6" runat="server" CssClass="form-control form-control-sm" />
                    </div>
                    <div class="col-12">
                        <label class="form-label small fw-semibold mb-1">
                            <span class="required-star">★</span> Temperatura Zona 4:
                            <span class="text-muted fw-normal">Numeric</span>
                        </label>
                        <asp:TextBox ID="TxtMss7" runat="server" CssClass="form-control form-control-sm" />
                    </div>
                    <div class="col-12">
                        <label class="form-label small fw-semibold mb-1">
                            <span class="required-star">★</span> Temperatura Zona 5:
                            <span class="text-muted fw-normal">Numeric</span>
                        </label>
                        <asp:TextBox ID="TxtMss8" runat="server" CssClass="form-control form-control-sm" />
                    </div>
                    <div class="col-12">
                        <label class="form-label small fw-semibold mb-1">
                            <span class="required-star">★</span> Temperatura Zona 6:
                            <span class="text-muted fw-normal">Numeric</span>
                        </label>
                        <asp:TextBox ID="TxtMss9" runat="server" CssClass="form-control form-control-sm" />
                    </div>
                    <div class="col-12">
                        <label class="form-label small fw-semibold mb-1">
                            <span class="required-star">★</span> Tiempo de curado de pad printing:
                            <span class="text-muted fw-normal">Numeric</span>
                        </label>
                        <asp:TextBox ID="TxtMss10" runat="server" CssClass="form-control form-control-sm" />
                    </div>
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-sm btn-primary" onclick="searchNext3()">Save</button>
                <button type="button" class="btn btn-sm btn-secondary" onclick="searchCancel()">Cancel</button>
            </div>
        </div>
    </div>
</div>

<%-- ══ Modal 4: Verificación Final MSS ════════════════════════════════ --%>
<div class="modal fade" id="modalSearch4" tabindex="-1" data-bs-backdrop="static">
    <div class="modal-dialog">
        <div class="modal-content">
            <%-- Barra estándar (X = Cancel), subencabezado y fila de contexto (entrega 4). --%>
            <div class="modal-header">
                <h6 class="modal-title">Data Collection Entry</h6>
                <div class="mes-titlebar-actions">
                    <button type="button" class="mes-titlebar-btn" title="Maximize"
                        onclick="mesToggleMaximize(this)">&#10530;</button>
                    <button type="button" class="mes-titlebar-btn" title="Close"
                        onclick="searchCancel()">&#10005;</button>
                </div>
            </div>
            <div class="mes-subheader">
                <span>Data Collection Entry</span>
                <span class="mes-help" title="Help">?</span>
            </div>
            <div class="mes-info-row">
                DC Group: <strong>DCGTM_VERIFICACION_FINAL/A</strong> &nbsp;&nbsp;
                Resource: <asp:Literal ID="LitResource4" runat="server" />
            </div>
            <div class="modal-body">
                <div class="mb-3">
                    <label class="form-label small fw-semibold">
                        <span class="required-star">★</span> ¿Se verificó según D000128035 o D000207299?
                        <span class="text-muted ms-1 fw-normal">Data Type: Boolean</span>
                    </label>
                    <asp:DropDownList ID="DdlFinal" runat="server" CssClass="form-select form-select-sm">
                        <asp:ListItem Value="">( Seleccione )</asp:ListItem>
                        <asp:ListItem Value="false">NO</asp:ListItem>
                        <asp:ListItem Value="true">Sí</asp:ListItem>
                    </asp:DropDownList>
                </div>
            </div>
            <div class="modal-footer">
                <%-- Único postback de la secuencia (patrón 1 postback / paso).
                     searchValidate4() valida en cliente; si pasa, postea a BtnSave4_Click. --%>
                <asp:Button ID="BtnSave4" runat="server" Text="Save"
                    CssClass="btn btn-sm btn-primary"
                    OnClientClick="return searchValidate4();"
                    OnClick="BtnSave4_Click" CausesValidation="false" />
                <button type="button" class="btn btn-sm btn-secondary" onclick="searchCancel()">Cancel</button>
            </div>
        </div>
    </div>
</div>

<script>
// ── Navegación 100% cliente entre los 4 modales ─────────────────────────
// Los valores de TxtMoldeId/DdlVerif*/TxtMss*/DdlFinal se conservan en el DOM
// y postean automáticamente en el único postback (el Save del Modal 4).
var _searchOpenModalId = null;

function _searchCloseAll() {
    ['modalSearch1','modalSearch2','modalSearch3','modalSearch4'].forEach(function(id){
        var el = document.getElementById(id);
        if (el) {
            var inst = bootstrap.Modal.getInstance(el);
            if (inst) inst.hide();
        }
    });
}

// Cierra el modal actual y, cuando su transición termina, abre el siguiente — evita
// backdrops acumulados de Bootstrap al encadenar modales.
function _searchGo(modalId) {
    var current = _searchOpenModalId;
    _searchOpenModalId = modalId;
    if (!current || current === modalId) { showModalSafe(modalId); return; }

    var el = document.getElementById(current);
    var inst = el ? bootstrap.Modal.getInstance(el) : null;
    if (inst) {
        el.addEventListener('hidden.bs.modal', function handler() {
            el.removeEventListener('hidden.bs.modal', handler);
            showModalSafe(modalId);
        });
        inst.hide();
    } else {
        showModalSafe(modalId);
    }
}

function searchValidate1() {
    var v = document.getElementById('<%= TxtMoldeId.ClientID %>').value.trim();
    if (!v) {
        Swal.fire({ icon: 'warning', title: 'Campo requerido', text: 'Debes ingresar el ID del molde.' });
        return false;
    }
    if (isNaN(parseFloat(v)) || !isFinite(v)) {
        Swal.fire({ icon: 'warning', title: 'Valor inválido', text: 'El ID del molde debe ser un valor numérico.' });
        return false;
    }
    return true;
}
function searchNext1() { if (searchValidate1()) _searchGo('modalSearch2'); }

// Sí/No son decisiones del operador: solo se exige responder; el servidor las califica.
function searchValidate2() {
    var ddls = [
        document.getElementById('<%= DdlVerif1.ClientID %>'),
        document.getElementById('<%= DdlVerif2.ClientID %>'),
        document.getElementById('<%= DdlVerif3.ClientID %>')
    ];
    for (var i = 0; i < ddls.length; i++) {
        if (!ddls[i].value) {
            Swal.fire({ icon: 'warning', title: 'Campo requerido', text: 'Debes responder todas las preguntas antes de guardar.' });
            return false;
        }
    }
    return true;
}
function searchNext2() { if (searchValidate2()) _searchGo('modalSearch3'); }

function searchValidate3() {
    var ids = [
        '<%= TxtMss1.ClientID %>', '<%= TxtMss2.ClientID %>', '<%= TxtMss3.ClientID %>',
        '<%= TxtMss4.ClientID %>', '<%= TxtMss5.ClientID %>', '<%= TxtMss6.ClientID %>',
        '<%= TxtMss7.ClientID %>', '<%= TxtMss8.ClientID %>', '<%= TxtMss9.ClientID %>',
        '<%= TxtMss10.ClientID %>'
    ];
    for (var i = 0; i < ids.length; i++) {
        var v = document.getElementById(ids[i]).value.trim();
        if (!v) {
            Swal.fire({ icon: 'warning', title: 'Campos incompletos', text: 'Debes completar todos los parámetros antes de guardar.' });
            return false;
        }
        if (isNaN(parseFloat(v)) || !isFinite(v)) {
            Swal.fire({ icon: 'warning', title: 'Valor inválido', text: 'Todos los parámetros deben ser valores numéricos.' });
            return false;
        }
    }
    return true;
}
function searchNext3() { if (searchValidate3()) _searchGo('modalSearch4'); }

// Validación del Modal 4 — es el OnClientClick del asp:Button BtnSave4:
// devuelve true para permitir el postback final, false para bloquearlo.
function searchValidate4() {
    var v = document.getElementById('<%= DdlFinal.ClientID %>').value;
    if (!v) {
        Swal.fire({ icon: 'warning', title: 'Campo requerido', text: 'Debes seleccionar una respuesta.' });
        return false;
    }
    return true;
}

// Cancel puro de cliente: cierra los modales sin tocar el servidor. El paso NO
// avanza; BtnStart crea un SearchControl fresco la próxima vez (ver GuidedTest).
function searchCancel() { _searchOpenModalId = null; _searchCloseAll(); }
</script>