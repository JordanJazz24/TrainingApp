<%@ Control Language="C#" AutoEventWireup="true"
    CodeBehind="SFCSplitControl.ascx.cs"
    Inherits="TrainingApp.UI.Controls.Training.SFCSplitControl" %>

<%-- Estructura del real (Fase 6.5, entrega 9; captura "modal sfc split"): barra con el título
     del plug-in, banda oscura "SFC Split", mensaje de confirmación arriba y el formulario
     centrado con [Split] [Close] debajo. --%>
<div class="modal fade" id="modalSFCSplit" tabindex="-1" aria-hidden="true" data-bs-backdrop="static">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header">
                <h6 class="modal-title">SFC Split Plug-In (Auto ID)</h6>
                <div class="mes-titlebar-actions">
                    <button type="button" class="mes-titlebar-btn" title="Maximize"
                        onclick="mesToggleMaximize(this)">&#10530;</button>
                    <button type="button" class="mes-titlebar-btn" title="Close"
                        data-bs-dismiss="modal">&#10005;</button>
                </div>
            </div>
            <div class="mes-page-header">
                <span>SFC Split</span>
                <span class="mes-help" title="Help">?</span>
            </div>
            <div class="modal-body split-body">
                <%-- Confirmación del real, arriba: "☑ New SFC {sfc} created with quantity {n}" --%>
                <asp:Panel ID="PnlSplitConfirm" runat="server" Visible="false" CssClass="split-message" data-guide="split-created">
                    <svg width="16" height="16" viewBox="0 0 20 20" aria-hidden="true">
                        <rect x="1" y="1" width="18" height="18" rx="3"
                            fill="#7cc36a" stroke="#3d6b33" stroke-width="1.5" />
                        <path d="M5 10.5l3.2 3.2L15 6.5" fill="none" stroke="#1d3d17"
                            stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round" />
                    </svg>
                    <span>New SFC <asp:Literal ID="LitNewSfc" runat="server" /> created with quantity
                        <asp:Literal ID="LitNewQty" runat="server" /></span>
                </asp:Panel>

                <div class="split-form">
                    <span class="split-label">SFC to Split:</span>
                    <span><asp:Literal ID="LitSFCToSplit" runat="server" /></span>

                    <span class="split-label">Available Qty:</span>
                    <span><asp:Literal ID="LitAvailableQty" runat="server" /></span>

                    <span class="split-label"><span class="pod-star">*</span> Qty to Split:</span>
                    <span>
                        <%-- La × limpia el campo en el cliente (sin postback) y solo se ve con valor.
                             Sin mensajes de validación bajo el campo (corrección del usuario): una
                             cantidad vacía o fuera de rango la avisa BtnSplit_Click con SweetAlert. --%>
                        <span class="split-input-box">
                            <asp:TextBox ID="TxtQtyToSplit" runat="server"
                                CssClass="split-input" TextMode="Number" data-guide="split-qty"
                                oninput="splitSyncClear(this)" />
                            <button type="button" class="split-clear" title="Clear"
                                onclick="splitClearQty(this)">&#10005;</button>
                        </span>
                    </span>
                </div>

                <div class="split-actions">
                    <asp:Button ID="BtnSplit" runat="server" Text="Split"
                        CssClass="mes-btn-outline" data-guide="split-split"
                        CausesValidation="false" OnClick="BtnSplit_Click" />
                    <asp:Button ID="BtnCloseSplit" runat="server" Text="Close"
                        CssClass="mes-btn-outline" data-guide="split-close"
                        OnClick="BtnCloseSplit_Click" CausesValidation="false" />
                </div>
            </div>
        </div>
    </div>
</div>

<script>
(function () {
    // × dentro del campo Qty to Split: visible solo cuando hay un valor.
    window.splitSyncClear = function (input) {
        var btn = input.parentNode.querySelector('.split-clear');
        if (btn) btn.style.visibility = input.value ? 'visible' : 'hidden';
    };
    window.splitClearQty = function (btn) {
        var input = btn.parentNode.querySelector('input');
        if (!input) return;
        input.value = '';
        window.splitSyncClear(input);
        input.focus();
    };
    document.addEventListener('DOMContentLoaded', function () {
        var input = document.querySelector('#modalSFCSplit .split-input');
        if (input) window.splitSyncClear(input);
    });
})();
</script>
