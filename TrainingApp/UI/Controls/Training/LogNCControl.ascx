<%@ Control Language="C#" AutoEventWireup="true"
    CodeBehind="LogNCControl.ascx.cs"
    Inherits="TrainingApp.UI.Controls.Training.LogNCControl" %>

<%-- ── NC Selection grid (replaces POD Work List) ─────────────────── --%>
<asp:Panel ID="PnlNCSelection" runat="server" Visible="false">
    <div class="pod-list-view">
        <div class="list-header">
            <span>NC Selection</span>
        </div>
        <div class="nc-grid">
            <asp:Repeater ID="RptNCCodes" runat="server">
                <ItemTemplate>
                    <div class="nc-cell">
                        <div class="nc-info">
                            <div class="nc-code"><%# Eval("Code") %></div>
                            <div class="nc-desc"><%# Eval("Description") %></div>
                        </div>
                        <asp:Button ID="BtnLogNC" runat="server"
                            CssClass="btn btn-sm btn-outline-primary"
                            Text="Log NC"
                            CommandArgument='<%# Eval("Id") + "|" + Eval("Code") %>'
                            OnClick="BtnLogNCCode_Click" />
                    </div>
                </ItemTemplate>
            </asp:Repeater>
        </div>
    </div>
</asp:Panel>

<%-- ── NC Data Entry modal ─────────────────────────────────────────── --%>
<asp:Panel ID="PnlNCEntry" runat="server" Visible="false">
    <div class="modal fade show d-block" id="modalNCEntry" tabindex="-1" data-bs-backdrop="static">
        <div class="modal-dialog">
            <div class="modal-content">
                <div class="modal-header">
                    <h6 class="modal-title">NC Data Entry</h6>
                    <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                </div>
                <div class="modal-body">
                    <div class="mb-2 small text-muted">
                        SFC: <strong><asp:Literal ID="LitNCEntrySFC" runat="server" /></strong>
                    </div>
                    <div class="mb-3">
                        <label class="form-label small">
                            <span class="required-star">★</span> NC Code:
                        </label>
                        <asp:TextBox ID="TxtNCCode" runat="server"
                            CssClass="form-control form-control-sm" ReadOnly="true" />
                    </div>
                    <div class="mb-3">
                        <label class="form-label small">
                            <span class="required-star">★</span> Component:
                        </label>
                        <div class="d-flex gap-2">
                            <asp:TextBox ID="TxtComponent" runat="server"
                                CssClass="form-control form-control-sm"
                                ValidationGroup="ncGroup"
                                placeholder="Enter component..." />
                            <button type="button" class="btn btn-sm btn-outline-secondary"
                                title="Copy">&#128203;</button>
                        </div>
                        <asp:RequiredFieldValidator ID="RfvComponent" runat="server"
                            ControlToValidate="TxtComponent"
                            ErrorMessage="Component required." CssClass="text-danger small"
                            Display="Dynamic" ValidationGroup="ncGroup" />
                    </div>
                    <div class="mb-3">
                        <label class="form-label small">Comments:</label>
                        <asp:TextBox ID="TxtComments" runat="server"
                            CssClass="form-control form-control-sm" />
                    </div>
                    <asp:HiddenField ID="HfNCCodeId" runat="server" />
                </div>
                <div class="modal-footer">
                    <asp:Button ID="BtnAdd" runat="server" Text="✓ Add"
                        CssClass="btn btn-sm btn-outline-success"
                        OnClick="BtnAdd_Click" CausesValidation="false" />
                    <asp:Button ID="BtnAddDone" runat="server" Text="✓ Add-Done"
                        CssClass="btn btn-sm btn-success"
                        ValidationGroup="ncGroup"
                        OnClick="BtnAddDone_Click" />
                    <asp:Button ID="BtnReturnPOD" runat="server" Text="✗ Return to POD"
                        CssClass="btn btn-sm btn-secondary"
                        OnClick="BtnReturnPOD_Click" CausesValidation="false" />
                    <asp:Button ID="BtnDone" runat="server" Text="✓ Done"
                        CssClass="btn btn-sm btn-outline-success"
                        OnClick="BtnDone_Click" CausesValidation="false" />
                </div>
            </div>
        </div>
    </div>
    <div class="modal-backdrop fade show"></div>
</asp:Panel>
