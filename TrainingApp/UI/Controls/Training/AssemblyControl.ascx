<%@ Control Language="C#" AutoEventWireup="true"
    CodeBehind="AssemblyControl.ascx.cs"
    Inherits="TrainingApp.UI.Controls.Training.AssemblyControl" %>

<%-- Component List (replaces POD Work List) --%>
<div class="pod-list-view">
    <div class="list-header">
        <span>Component List</span>
        <div class="d-flex gap-2">
            <button type="button" class="btn btn-sm btn-outline-secondary" title="Help">?</button>
            <button type="button" class="btn btn-sm btn-outline-secondary"
                onclick="closeAssemblyView()">&#10007;</button>
        </div>
    </div>
    <asp:Repeater ID="RptComponents" runat="server">
        <ItemTemplate>
            <div class="pod-list-row">
                <div>
                    <div class="fw-semibold small"><%# Eval("Code") %></div>
                    <div class="text-muted" style="font-size:0.76rem">
                        Description: <%# Eval("Description") %> &nbsp;|&nbsp;
                        Qty: <%# Eval("Qty") %> &nbsp;|&nbsp;
                        Rem Qty: <span id="remQty_<%# Container.ItemIndex %>"><%# Eval("RemQty") %></span>
                    </div>
                </div>
                <div class="d-flex gap-1">
                    <button type="button" class="btn btn-sm btn-primary"
                        onclick="openAssembleModal(<%# Container.ItemIndex %>)">Assemble</button>
                    <button type="button" class="btn btn-sm btn-outline-secondary" disabled>Disassemble</button>
                </div>
            </div>
        </ItemTemplate>
    </asp:Repeater>
    <div class="pod-pagination">
        <button type="button" class="btn btn-sm btn-outline-secondary">|&lt; First</button>
        <button type="button" class="btn btn-sm btn-outline-secondary">&lt;&lt; Prev</button>
        <button type="button" class="btn btn-sm btn-outline-secondary">Next &gt;&gt;</button>
        <button type="button" class="btn btn-sm btn-outline-secondary">Last &gt;|</button>
        <span class="ms-2 text-muted">Rows 1-<asp:Literal ID="LitTotal" runat="server" /> of
            <asp:Literal ID="LitTotalOf" runat="server" /></span>
    </div>
</div>

<%-- Assemble Components modal --%>
<div class="modal fade" id="modalAssemble" tabindex="-1" aria-hidden="true" data-bs-backdrop="static">
    <div class="modal-dialog modal-lg">
        <div class="modal-content">
            <div class="modal-header">
                <h6 class="modal-title">Assemble Components</h6>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>
            <div class="modal-body">
                <div class="row g-3 mb-3">
                    <div class="col-md-6">
                        <label class="form-label small">Mode:</label>
                        <select class="form-select form-select-sm"><option>Choose</option></select>
                    </div>
                    <div class="col-md-6">
                        <label class="form-label small">Find component:</label>
                        <input type="text" class="form-control form-control-sm" />
                    </div>
                    <div class="col-md-6">
                        <label class="form-label small">Action:</label>
                        <div>
                            <label class="me-2"><input type="radio" name="assembleAction" value="assembly" checked /> Assembly</label>
                            <label><input type="radio" name="assembleAction" value="removal" /> Removal</label>
                        </div>
                    </div>
                    <div class="col-md-6">
                        <label class="form-label small">Barcode:</label>
                        <asp:TextBox ID="TxtBarcode" runat="server" CssClass="form-control form-control-sm"
                            placeholder="Scan 2D barcode..." onchange="simulateBarcodeRead(this.value)" />
                    </div>
                </div>
                <hr />
                <div class="row">
                    <div class="col-md-6">
                        <h6 class="small fw-bold">Component Details</h6>
                        <asp:Panel ID="PnlComponentDetails" runat="server" Visible="false">
                            <div class="small">
                                Component: <strong><asp:Literal ID="LitComponentCode" runat="server" /></strong><br />
                                Version: <asp:Literal ID="LitComponentVersion" runat="server" /><br />
                                Description: <asp:Literal ID="LitComponentDesc" runat="server" /><br />
                                Quantity: <asp:Literal ID="LitComponentQty" runat="server" />
                            </div>
                        </asp:Panel>
                        <asp:Panel ID="PnlComponentEmpty" runat="server" CssClass="text-muted small">
                            (scan barcode to populate)
                        </asp:Panel>
                    </div>
                    <div class="col-md-6">
                        <h6 class="small fw-bold">Assembly Data</h6>
                        <asp:Panel ID="PnlAssemblyData" runat="server" Visible="false">
                            <div class="small">
                                <span class="required-star">★</span> Batch Number:
                                <strong><asp:Literal ID="LitBatchNumber" runat="server" /></strong>
                            </div>
                        </asp:Panel>
                        <asp:Panel ID="PnlAssemblyEmpty" runat="server" CssClass="text-muted small">
                            (scan barcode to populate)
                        </asp:Panel>
                    </div>
                </div>
                <asp:HiddenField ID="HfScannedCount" runat="server" Value="0" ClientIDMode="Static" />
                <asp:HiddenField ID="HfExpectedCount" runat="server" ClientIDMode="Static" />
                <asp:HiddenField ID="HfBarcodeData" runat="server" ClientIDMode="Static" />
            </div>
            <div class="modal-footer">
                <asp:Button ID="BtnAdd" runat="server" Text="✓ Add"
                    CssClass="btn btn-sm btn-success"
                    OnClick="BtnAdd_Click"
                    style="display:none" />
                <asp:Button ID="BtnReturnPOD" runat="server" Text="✗ Return to POD"
                    CssClass="btn btn-sm btn-secondary"
                    OnClick="BtnReturnPOD_Click"
                    CausesValidation="false" />
            </div>
        </div>
    </div>
</div>

<script>
function openAssembleModal(idx) {
    var m = document.getElementById('modalAssemble');
    if (m) new bootstrap.Modal(m).show();
}
function closeAssemblyView() {
    // Signal to code-behind that user cancelled — PostBack with empty answer
}
function simulateBarcodeRead(barcode) {
    if (!barcode) return;
    // Store for PostBack
    document.getElementById('HfBarcodeData').value = barcode;
    // Show Add button (signal that barcode is populated)
    document.getElementById('<%= BtnAdd.ClientID %>').style.display = '';
}
</script>
