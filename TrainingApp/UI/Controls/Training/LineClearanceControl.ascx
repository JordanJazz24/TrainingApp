<%@ Control Language="C#" AutoEventWireup="true"
    CodeBehind="LineClearanceControl.ascx.cs"
    Inherits="TrainingApp.UI.Controls.Training.LineClearanceControl" %>

<%-- ── Data Collection Standalone modal ───────────────────────────── --%>
<div class="modal fade" id="modalDCStandalone" tabindex="-1" aria-hidden="true" data-bs-backdrop="static">
    <div class="modal-dialog modal-lg">
        <div class="modal-content">
            <div class="modal-header">
                <h6 class="modal-title">Data Collection Standalone</h6>
                <button type="button" class="btn-close" data-bs-dismiss="modal"
                    onclick="closeLCModal()"></button>
            </div>
            <div class="modal-body">
                <%-- Filter / Collect For --%>
                <div class="row g-2 mb-3">
                    <div class="col-auto">
                        <label class="form-label small mb-0">Filter Operation:</label>
                        <span class="form-control-plaintext form-control-sm">
                            <asp:Literal ID="LitOperation" runat="server" />
                        </span>
                    </div>
                    <div class="col-auto">
                        <label class="form-label small mb-0">Collect Data For:</label>
                        <span class="form-control-plaintext form-control-sm">
                            <asp:Literal ID="LitResource" runat="server" />
                        </span>
                    </div>
                    <div class="col-auto d-flex align-items-end">
                        <button type="button" class="btn btn-sm btn-secondary"
                            onclick="retrieveLCStandalone()">Retrieve</button>
                        <button type="button" class="btn btn-sm btn-outline-secondary ms-2"
                            data-bs-dismiss="modal" onclick="closeLCModal()">Clear</button>
                    </div>
                </div>

                <%-- DC list (hidden until Retrieve) --%>
                <asp:Panel ID="PnlDCList" runat="server" CssClass="border rounded" style="display:none" ClientIDMode="Static">
                    <div class="pod-list-row px-3">
                        <div>
                            <div class="fw-semibold small">DC_LC_DONE</div>
                            <div class="text-muted" style="font-size:0.78rem">
                                DC Group/Vers.: DCGTM_LC/A &nbsp;|&nbsp;
                                Last collected: <asp:Literal ID="LitLastCollected" runat="server" Text="—" />
                            </div>
                        </div>
                        <button type="button" class="btn btn-sm btn-primary"
                            onclick="openDCEntry()">Collect</button>
                    </div>
                    <%-- Success banner (shown after save) --%>
                    <asp:Panel ID="PnlSavedBanner" runat="server" Visible="false"
                        CssClass="alert alert-warning m-2 p-2 small">
                        &#9670; 1 DC Groups saved
                    </asp:Panel>
                </asp:Panel>
            </div>
        </div>
    </div>
</div>

<%-- ── Data Collection Entry sub-modal ────────────────────────────── --%>
<div class="modal fade" id="modalDCEntry" tabindex="-1" aria-hidden="true" data-bs-backdrop="static">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header">
                <h6 class="modal-title">Data Collection Entry</h6>
                <button type="button" class="btn-close" onclick="closeDCEntry()"></button>
            </div>
            <div class="modal-body">
                <div class="mb-2 small text-muted">
                    DC Group: <strong>DCGTM_LC/CA</strong> &nbsp;|&nbsp;
                    Resource: <asp:Literal ID="LitResourceEntry" runat="server" />
                </div>
                <hr class="mt-1 mb-2" />
                <div class="mb-3">
                    <label class="form-label">
                        <span class="required-star">★</span>
                        <asp:Literal ID="LitQuestion" runat="server" />
                    </label>
                    <div class="text-muted small mb-1">Data Type: Boolean &nbsp; 0 files attached</div>
                    <asp:DropDownList ID="DdlAnswer" runat="server" CssClass="form-select form-select-sm">
                        <asp:ListItem Value="">( Select )</asp:ListItem>
                        <asp:ListItem Value="false">NO</asp:ListItem>
                        <asp:ListItem Value="true">Si</asp:ListItem>
                    </asp:DropDownList>
                    <asp:RequiredFieldValidator ID="RfvAnswer" runat="server"
                        ControlToValidate="DdlAnswer"
                        InitialValue=""
                        ErrorMessage="Selection required."
                        CssClass="text-danger small"
                        Display="Dynamic"
                        ValidationGroup="lcGroup" />
                </div>
            </div>
            <div class="modal-footer">
                <asp:Button ID="BtnSave" runat="server" Text="💾 Save"
                    CssClass="btn btn-sm btn-success"
                    ValidationGroup="lcGroup"
                    OnClick="BtnSave_Click" />
                <button type="button" class="btn btn-sm btn-secondary"
                    onclick="closeDCEntry()">&#10007; Return to POD</button>
            </div>
        </div>
    </div>
</div>

<script>
    function retrieveLCStandalone() {
        document.getElementById('PnlDCList').style.display = '';
    }
    function openDCEntry() {
        new bootstrap.Modal(document.getElementById('modalDCEntry')).show();
    }
    function closeDCEntry() {
        var m = bootstrap.Modal.getInstance(document.getElementById('modalDCEntry'));
        if (m) m.hide();
    }
    function closeLCModal() {
        var m = bootstrap.Modal.getInstance(document.getElementById('modalDCStandalone'));
        if (m) m.hide();
    }
</script>
