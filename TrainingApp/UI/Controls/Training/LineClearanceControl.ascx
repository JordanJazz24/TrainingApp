<%@ Control Language="C#" AutoEventWireup="true"
    CodeBehind="LineClearanceControl.ascx.cs"
    Inherits="TrainingApp.UI.Controls.Training.LineClearanceControl" %>

<%-- ── Data Collection Standalone (modal principal) ───────────────── --%>
<div class="modal fade" id="modalDCStandalone" tabindex="-1" aria-hidden="true" data-bs-backdrop="static">
    <div class="modal-dialog modal-lg">
        <div class="modal-content">
            <div class="modal-header">
                <h6 class="modal-title">Data Collection Standalone</h6>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>
            <div class="modal-body">

                <%-- Banner "dc group save" — top of modal, shown after successful Save --%>
                <asp:Panel ID="PnlSavedBanner" runat="server" Visible="false"
                    CssClass="lc-save-banner alert alert-warning py-1 px-2 mb-2">
                    &#9670; dc group save
                </asp:Panel>

                <%-- Filter / Collect For + Retrieve + Clear --%>
                <div class="row g-2 mb-3 align-items-end">
                    <div class="col-auto">
                        <label class="form-label small mb-0">Filter Operation:</label>
                        <span class="form-control-plaintext form-control-sm">
                            <asp:Literal ID="LitOperation" runat="server" />
                        </span>
                    </div>
                    <div class="col-auto">
                        <label class="form-label small mb-0">* Collect Data For:</label>
                        <span class="form-control-plaintext form-control-sm">
                            <asp:Literal ID="LitResource" runat="server" />
                        </span>
                    </div>
                    <div class="col-auto">
                        <button type="button" class="btn btn-sm btn-secondary"
                            onclick="retrieveLCStandalone()">Retrieve</button>
                        <button type="button" class="btn btn-sm btn-outline-secondary ms-2"
                            data-bs-dismiss="modal">Clear</button>
                    </div>
                </div>

                <%-- DC list (hidden until Retrieve) --%>
                <asp:Panel ID="PnlDCList" runat="server" ClientIDMode="Static"
                    style="display:none" CssClass="border rounded">
                    <div class="d-flex justify-content-between align-items-center px-3 py-2">
                        <div>
                            <div class="fw-semibold small">DC_LC_DONE</div>
                            <div class="text-muted" style="font-size:0.78rem">
                                DC Group/Vers.: DCGTM_LC/A &nbsp;|&nbsp;
                                Last collected: <asp:Literal ID="LitLastCollected" runat="server" Text="—" />
                            </div>
                        </div>
                        <button type="button" class="btn btn-sm btn-primary"
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
<div class="modal fade" id="modalDCEntry" tabindex="-1" aria-hidden="true" data-bs-backdrop="static">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header">
                <h6 class="modal-title">Data Collection Entry</h6>
            </div>
            <div class="modal-body">
                <div class="mb-2 small text-muted">
                    DC Group: <strong>DCGTM_LC/CA</strong> &nbsp;|&nbsp;
                    Resource: <asp:Literal ID="LitResourceEntry" runat="server" />
                </div>
                <hr class="mt-1 mb-2" />
                <div class="mb-3">
                    <label class="form-label">
                        <span class="text-danger">&#9733;</span>
                        <asp:Literal ID="LitQuestion" runat="server" />
                    </label>
                    <div class="text-muted small mb-1">Data Type: Boolean &nbsp; 0 files attached</div>
                    <asp:DropDownList ID="DdlAnswer" runat="server" CssClass="form-select form-select-sm">
                        <asp:ListItem Value="">( Seleccione )</asp:ListItem>
                        <asp:ListItem Value="false">NO</asp:ListItem>
                        <asp:ListItem Value="true">Si</asp:ListItem>
                    </asp:DropDownList>
                </div>
            </div>
            <div class="modal-footer">
                <asp:Button ID="BtnSave" runat="server" Text="Save"
                    CssClass="btn btn-sm btn-success"
                    OnClick="BtnSave_Click"
                    OnClientClick="return lcValidateAnswer();"
                    CausesValidation="false" />
                <button type="button" class="btn btn-sm btn-secondary"
                    data-bs-dismiss="modal">Cancel</button>
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
