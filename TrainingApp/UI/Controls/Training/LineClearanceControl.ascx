<%@ Control Language="C#" AutoEventWireup="true"
    CodeBehind="LineClearanceControl.ascx.cs"
    Inherits="TrainingApp.UI.Controls.Training.LineClearanceControl" %>

<%-- ── Data Collection Standalone (modal principal) ───────────────── --%>
<%-- Estructura del real (Fase 6.5, entrega 6; captura "modal data collection standalone"):
     barra superior solo con ⤢ ✕, encabezado con (?) y línea celeste, campos a la
     izquierda con [Clear] / [Retrieve] a la derecha y la fila DC_LC_DONE con [Collect]. --%>
<div class="modal fade" id="modalDCStandalone" tabindex="-1" aria-hidden="true" data-bs-backdrop="static">
    <div class="modal-dialog modal-lg">
        <div class="modal-content">
            <div class="modal-header mes-titlebar-bare">
                <div class="mes-titlebar-actions">
                    <button type="button" class="mes-titlebar-btn" title="Maximize"
                        onclick="mesToggleMaximize(this)">&#10530;</button>
                    <button type="button" class="mes-titlebar-btn" title="Close"
                        data-bs-dismiss="modal" data-guide="lc-close">&#10005;</button>
                </div>
            </div>
            <div class="mes-subheader mes-divider">
                <span>Data Collection Standalone</span>
                <span class="mes-help" title="Help">?</span>
            </div>
            <div class="modal-body p-0">

                <%-- Mensaje del real tras guardar: "⚠ 1 DC Groups saved" (se oculta a los 5 s) --%>
                <asp:Panel ID="PnlSavedBanner" runat="server" Visible="false" CssClass="lc-save-banner" data-guide="lc-saved">
                    <img src="<%= ResolveUrl("~/UI/master/imgs/lineClearence-save-icon.png") %>" alt="" />
                    <span>1 DC Groups saved</span>
                </asp:Panel>

                <%-- Filter Operation / Collect Data For + [Clear] / [Retrieve] --%>
                <div class="lc-form">
                    <span class="lc-label">Filter Operation:</span>
                    <span class="mes-field lc-field"><span class="mes-field-text"><asp:Literal ID="LitOperation" runat="server" /></span><span class="mes-field-icon">&#10697;</span></span>
                    <span></span>
                    <span></span>
                    <%-- Botones anchos: el ícono va como <img> para quedar pegado al texto y
                         centrado con él (como el real), no en el borde del botón. --%>
                    <button type="button" class="mes-btn lc-side-btn" onclick="lcClear()">
                        <img src="<%= ResolveUrl("~/UI/master/imgs/anular-icon.png") %>" alt="" />Clear</button>

                    <span class="lc-label"><span class="pod-star">*</span> Collect Data For:</span>
                    <select class="rs-select lc-select" aria-label="Collect Data For">
                        <option>Resource</option>
                    </select>
                    <span class="mes-field lc-field"><span class="mes-field-text"><asp:Literal ID="LitResource" runat="server" /></span><span class="mes-field-icon">&#10697;</span></span>
                    <span></span>
                    <button type="button" class="mes-btn lc-side-btn" onclick="retrieveLCStandalone()" data-guide="lc-retrieve">
                        <img src="<%= ResolveUrl("~/UI/master/imgs/retrieve-icon.png") %>" alt="" />Retrieve</button>
                </div>

                <%-- DC list (hidden until Retrieve) --%>
                <asp:Panel ID="PnlDCList" runat="server" ClientIDMode="Static"
                    style="display:none" CssClass="lc-list">
                    <div class="lc-row">
                        <div>
                            <div class="lc-row-code">DC_LC_DONE</div>
                            <div class="lc-row-question">Realizó la Limpieza de Linea?</div>
                        </div>
                        <div class="lc-row-group">
                            DC Group/Vers.: DCGTM_LC/A
                            <div><asp:Literal ID="LitLastCollected" runat="server" /></div>
                        </div>
                        <button type="button" class="mes-btn mes-btn-stack mes-ico mes-ico-collect lc-collect" data-guide="lc-collect"
                            onclick="lcOpenEntry()">Collect</button>
                    </div>
                </asp:Panel>

                <%-- Hidden field: tracks whether Retrieve was clicked (persists across PostBack) --%>
                <asp:HiddenField ID="HfRetrieved" runat="server" ClientIDMode="Static" Value="false" />
            </div>
        </div>
    </div>
</div>

<%-- ── Data Collection Entry (sub-modal) ──────────────────────────── --%>
<%-- Se abre encima del principal, que queda con un velo suave (modales apilados en
     Training.Master). Barra con título, subencabezado con línea celeste y fila de contexto,
     como la captura "modal line clearence- sub modal de la pregunta". --%>
<div class="modal fade" id="modalDCEntry" tabindex="-1" aria-hidden="true" data-bs-backdrop="static">
    <div class="modal-dialog lc-entry-dialog">
        <div class="modal-content">
            <div class="modal-header">
                <h6 class="modal-title">Data Collection Entry</h6>
                <div class="mes-titlebar-actions">
                    <button type="button" class="mes-titlebar-btn" title="Maximize"
                        onclick="mesToggleMaximize(this)">&#10530;</button>
                    <button type="button" class="mes-titlebar-btn" title="Close"
                        data-bs-dismiss="modal">&#10005;</button>
                </div>
            </div>
            <div class="mes-subheader mes-divider">
                <span>Data Collection Entry</span>
                <span class="mes-help" title="Help">?</span>
            </div>
            <div class="mes-info-row">
                <strong>DC Group:</strong> DCGTM_LC/A &nbsp;&nbsp;
                <strong>Resource:</strong> <asp:Literal ID="LitResourceEntry" runat="server" />
            </div>
            <div class="modal-body p-0">
                <div class="mes-param">
                    <div>
                        <label class="mes-param-label">
                            <span class="pod-star">*</span>
                            <asp:Literal ID="LitQuestion" runat="server" />:
                        </label>
                        <asp:DropDownList ID="DdlAnswer" runat="server" CssClass="rs-select mes-param-input" data-guide="lc-answer">
                            <asp:ListItem Value="">( Seleccione )</asp:ListItem>
                            <asp:ListItem Value="false">NO</asp:ListItem>
                            <asp:ListItem Value="true">Si</asp:ListItem>
                        </asp:DropDownList>
                    </div>
                    <div class="mes-param-type">Data Type: Boolean</div>
                    <button type="button" class="mes-btn">0 files attached</button>
                </div>
            </div>
            <div class="modal-footer mes-footer">
                <asp:Button ID="BtnSave" runat="server" Text="Save"
                    CssClass="mes-btn mes-ico mes-ico-save" data-guide="lc-save"
                    OnClick="BtnSave_Click"
                    OnClientClick="return lcValidateAnswer();"
                    CausesValidation="false" />
                <button type="button" class="mes-btn" data-bs-dismiss="modal">Cancel</button>
            </div>
        </div>
    </div>
</div>

<script>
(function () {
    // ── Retrieve ──────────────────────────────────────────────────────────
    window.retrieveLCStandalone = function () {
        document.getElementById('HfRetrieved').value = 'true';
        document.getElementById('PnlDCList').style.display = '';
    };

    // ── Clear: limpia el formulario como el real (oculta la lista hasta Retrieve) ──
    window.lcClear = function () {
        document.getElementById('HfRetrieved').value = 'false';
        document.getElementById('PnlDCList').style.display = 'none';
    };

    // ── Open sub-modal ────────────────────────────────────────────────────
    window.lcOpenEntry = function () {
        showModalSafe('modalDCEntry');
    };

    // ── Restore retrieved state after PostBack ────────────────────────────
    // Called via RegisterStartupScript when HfRetrieved == "true" on PostBack.
    window.lcRestoreRetrievedState = function () {
        document.getElementById('PnlDCList').style.display = '';
    };

    // ── Client-side validation before Save PostBack ───────────────────────
    window.lcValidateAnswer = function () {
        var ddl = document.getElementById('<%= DdlAnswer.ClientID %>');
        var val = ddl ? ddl.value : '';

        if (!val || val === '') {
            Swal.fire({
                icon: 'warning',
                title: 'Selección requerida',
                text: 'Debes seleccionar una respuesta antes de guardar.'
            }).then(function () {
                var el = document.getElementById('modalDCEntry');
                if (el && !el.classList.contains('show')) showModalSafe('modalDCEntry');
            });
            return false;
        }

        // 'No' es una decisión del operador: se envía y la califica el servidor.
        return true;
    };
})();
</script>
