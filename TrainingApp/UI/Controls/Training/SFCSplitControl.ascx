<%@ Control Language="C#" AutoEventWireup="true"
    CodeBehind="SFCSplitControl.ascx.cs"
    Inherits="TrainingApp.UI.Controls.Training.SFCSplitControl" %>

<div class="modal fade" id="modalSFCSplit" tabindex="-1" aria-hidden="true" data-bs-backdrop="static">
    <div class="modal-dialog">
        <div class="modal-content">
            <%-- Barra estándar + banda "SFC Split" del plug-in real. --%>
            <div class="modal-header">
                <h6 class="modal-title">SFC Split Plug-In (Auto ID)</h6>
                <div class="mes-titlebar-actions">
                    <button type="button" class="mes-titlebar-btn" title="Maximize"
                        onclick="mesToggleMaximize(this)">&#10530;</button>
                    <button type="button" class="mes-titlebar-btn" title="Close"
                        data-bs-dismiss="modal">&#10005;</button>
                </div>
            </div>
            <div class="mes-plugin-header">SFC Split</div>
            <div class="modal-body">
                <div class="mb-2">
                    <label class="form-label small">SFC to Split:</label>
                    <span class="form-control-plaintext form-control-sm">
                        <asp:Literal ID="LitSFCToSplit" runat="server" />
                    </span>
                </div>
                <div class="mb-2">
                    <label class="form-label small">Available Qty:</label>
                    <span class="form-control-plaintext form-control-sm">
                        <asp:Literal ID="LitAvailableQty" runat="server" />
                    </span>
                </div>
                <div class="mb-3">
                    <label class="form-label small">
                        <span class="required-star">★</span> Qty to Split:
                    </label>
                    <div class="d-flex gap-2">
                        <asp:TextBox ID="TxtQtyToSplit" runat="server"
                            CssClass="form-control form-control-sm" TextMode="Number"
                            style="width:100px" ValidationGroup="splitGroup" />
                        <asp:Button ID="BtnClearSplit" runat="server" Text="X"
                            CssClass="btn btn-sm btn-outline-secondary"
                            OnClick="BtnClearSplit_Click" CausesValidation="false" />
                    </div>
                    <asp:RequiredFieldValidator ID="RfvQty" runat="server"
                        ControlToValidate="TxtQtyToSplit"
                        ErrorMessage="Qty required." CssClass="text-danger small"
                        Display="Dynamic" ValidationGroup="splitGroup" />
                    <asp:CompareValidator ID="CvQty" runat="server"
                        ControlToValidate="TxtQtyToSplit"
                        Operator="LessThan" Type="Integer"
                        ErrorMessage="Qty to split must be less than Available Qty."
                        CssClass="text-danger small"
                        Display="Dynamic" ValidationGroup="splitGroup" />
                </div>
                <asp:Panel ID="PnlSplitConfirm" runat="server" Visible="false"
                    CssClass="alert alert-success small p-2">
                    &#10003; New split row created with quantity
                    <strong><asp:Literal ID="LitNewQty" runat="server" /></strong>.
                </asp:Panel>
            </div>
            <div class="modal-footer">
                <asp:Button ID="BtnSplit" runat="server" Text="Split"
                    CssClass="btn btn-sm btn-primary"
                    ValidationGroup="splitGroup" OnClick="BtnSplit_Click" />
                <asp:Button ID="BtnCloseSplit" runat="server" Text="Close"
                    CssClass="btn btn-sm btn-secondary"
                    OnClick="BtnCloseSplit_Click" CausesValidation="false" />
            </div>
        </div>
    </div>
</div>
