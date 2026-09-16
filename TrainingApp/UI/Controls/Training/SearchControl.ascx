<%@ Control Language="C#" AutoEventWireup="true"
    CodeBehind="SearchControl.ascx.cs"
    Inherits="TrainingApp.UI.Controls.Training.SearchControl" %>

<%-- 4 modales secuenciales del proceso Start para Moldeo/R&C.
     Toda la navegación entre modales (1→2→3→4) ocurre internamente —
     GuidedTest solo sabe del resultado final (OnStepSubmit) o del cancel. --%>

<%-- ══ Modal 1: ID del Molde ════════════════════════════════════════════ --%>
<div class="modal fade" id="modalSearch1" tabindex="-1" data-bs-backdrop="static">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header">
                <h6 class="modal-title">Data Collection Entry</h6>
            </div>
            <div class="modal-body">
                <div class="mb-2 small text-muted">
                    DC Group: <strong>DCGTM_ID_MOLDE/A</strong>
                    &nbsp;|&nbsp; Resource: <asp:Literal ID="LitResource1" runat="server" />
                </div>
                <hr class="mt-1 mb-3" />
                <div class="mb-3">
                    <label class="form-label small fw-semibold">
                        <span class="required-star">★</span> Ingrese el ID del molde:
                        <span class="text-muted ms-1 fw-normal">Data Type: Numeric</span>
                    </label>
                    <asp:TextBox ID="TxtMoldeId" runat="server" CssClass="form-control form-control-sm" />
                </div>
            </div>
            <div class="modal-footer">
                <asp:Button ID="BtnSave1" runat="server" Text="Save"
                    CssClass="btn btn-sm btn-primary"
                    OnClientClick="return searchValidate1();"
                    OnClick="BtnSave1_Click" CausesValidation="false" />
                <asp:Button ID="BtnCancel1" runat="server" Text="Cancel"
                    CssClass="btn btn-sm btn-secondary"
                    OnClick="BtnCancel_Click" CausesValidation="false" />
            </div>
        </div>
    </div>
</div>

<%-- ══ Modal 2: Verificaciones de Etiquetas ════════════════════════════ --%>
<div class="modal fade" id="modalSearch2" tabindex="-1" data-bs-backdrop="static">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header">
                <h6 class="modal-title">Data Collection Entry</h6>
            </div>
            <div class="modal-body">
                <div class="mb-2 small text-muted">
                    DC Group: <strong>DCGTM_VERIFICACIONES/A</strong>
                    &nbsp;|&nbsp; Resource: <asp:Literal ID="LitResource2" runat="server" />
                </div>
                <hr class="mt-1 mb-3" />
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
                <asp:Button ID="BtnSave2" runat="server" Text="Save"
                    CssClass="btn btn-sm btn-primary"
                    OnClientClick="return searchValidate2();"
                    OnClick="BtnSave2_Click" CausesValidation="false" />
                <asp:Button ID="BtnCancel2" runat="server" Text="Cancel"
                    CssClass="btn btn-sm btn-secondary"
                    OnClick="BtnCancel_Click" CausesValidation="false" />
            </div>
        </div>
    </div>
</div>

<%-- ══ Modal 3: Parámetros Técnicos MSS ════════════════════════════════ --%>
<div class="modal fade" id="modalSearch3" tabindex="-1" data-bs-backdrop="static">
    <div class="modal-dialog modal-lg">
        <div class="modal-content">
            <div class="modal-header">
                <h6 class="modal-title">Data Collection Entry</h6>
            </div>
            <div class="modal-body">
                <div class="mb-2 small text-muted">
                    DC Group: <strong>DCGTM_PARAMETROS_MSS_D000281359/A</strong>
                    &nbsp;|&nbsp; Resource: <asp:Literal ID="LitResource3" runat="server" />
                </div>
                <hr class="mt-1 mb-3" />
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
                <asp:Button ID="BtnSave3" runat="server" Text="Save"
                    CssClass="btn btn-sm btn-primary"
                    OnClientClick="return searchValidate3();"
                    OnClick="BtnSave3_Click" CausesValidation="false" />
                <asp:Button ID="BtnCancel3" runat="server" Text="Cancel"
                    CssClass="btn btn-sm btn-secondary"
                    OnClick="BtnCancel_Click" CausesValidation="false" />
            </div>
        </div>
    </div>
</div>

<%-- ══ Modal 4: Verificación Final MSS ════════════════════════════════ --%>
<div class="modal fade" id="modalSearch4" tabindex="-1" data-bs-backdrop="static">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header">
                <h6 class="modal-title">Data Collection Entry</h6>
            </div>
            <div class="modal-body">
                <div class="mb-2 small text-muted">
                    DC Group: <strong>DCGTM_VERIFICACION_FINAL/A</strong>
                    &nbsp;|&nbsp; Resource: <asp:Literal ID="LitResource4" runat="server" />
                </div>
                <hr class="mt-1 mb-3" />
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
                <asp:Button ID="BtnSave4" runat="server" Text="Save"
                    CssClass="btn btn-sm btn-primary"
                    OnClientClick="return searchValidate4();"
                    OnClick="BtnSave4_Click" CausesValidation="false" />
                <asp:Button ID="BtnCancel4" runat="server" Text="Cancel"
                    CssClass="btn btn-sm btn-secondary"
                    OnClick="BtnCancel_Click" CausesValidation="false" />
            </div>
        </div>
    </div>
</div>

<script>
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
        if (ddls[i].value !== 'true') {
            Swal.fire({ icon: 'warning', title: 'Respuesta incorrecta', text: 'Todas las respuestas deben ser “Sí” para continuar.' });
            return false;
        }
    }
    return true;
}

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

function searchValidate4() {
    var v = document.getElementById('<%= DdlFinal.ClientID %>').value;
    if (!v) {
        Swal.fire({ icon: 'warning', title: 'Campo requerido', text: 'Debes seleccionar una respuesta.' });
        return false;
    }
    if (v !== 'true') {
        Swal.fire({ icon: 'warning', title: 'Respuesta incorrecta', text: 'La respuesta debe ser “Sí” para continuar.' });
        return false;
    }
    return true;
}
</script>
