<%@ Control Language="C#" AutoEventWireup="true"
    CodeBehind="SearchControl.ascx.cs"
    Inherits="TrainingApp.UI.Controls.Training.SearchControl" %>

<%-- Renders only when CurrentSubStep >= 1 (Moldeo Start sequence).
     Standard flow (SubStep 0) is handled directly by GuidedTest.aspx.cs via TxtSFC. --%>

<asp:Panel ID="PnlSubStep" runat="server" Visible="false">

    <div class="modal fade" id="modalMoldeoStart" tabindex="-1" aria-hidden="true" data-bs-backdrop="static">
        <div class="modal-dialog">
            <div class="modal-content">
                <div class="modal-header">
                    <h6 class="modal-title">Data Collection Entry</h6>
                    <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                </div>
                <div class="modal-body">
                    <div class="mb-2 small text-muted">
                        DC Group: <strong><asp:Literal ID="LitDCGroup" runat="server" /></strong>
                        &nbsp;|&nbsp; Resource: <asp:Literal ID="LitResource" runat="server" />
                    </div>
                    <hr class="mt-1 mb-2" />

                    <%-- Sub-step 1: single text field (ID Molde) --%>
                    <asp:Panel ID="PnlSubStep1" runat="server" Visible="false">
                        <div class="mb-3">
                            <label class="form-label small">
                                <span class="required-star">★</span>
                                <asp:Literal ID="LitParam1Label" runat="server" />
                            </label>
                            <div class="text-muted small mb-1">Data Type: Text &nbsp; 0 files attached</div>
                            <asp:TextBox ID="TxtParam1" runat="server" CssClass="form-control form-control-sm"
                                ValidationGroup="searchGroup" />
                            <asp:RequiredFieldValidator ID="RfvParam1" runat="server"
                                ControlToValidate="TxtParam1"
                                ErrorMessage="Value required."
                                CssClass="text-danger small" Display="Dynamic"
                                ValidationGroup="searchGroup" />
                        </div>
                    </asp:Panel>

                    <%-- Sub-step 2: 3 boolean questions (LC Moldeo) --%>
                    <asp:Panel ID="PnlSubStep2" runat="server" Visible="false">
                        <asp:Repeater ID="RptBoolQuestions" runat="server">
                            <ItemTemplate>
                                <div class="mb-3">
                                    <label class="form-label small">
                                        <span class="required-star">★</span>
                                        <%# Eval("QuestionText") %>
                                    </label>
                                    <div class="text-muted small mb-1">Data Type: Boolean &nbsp; 0 files attached</div>
                                    <select name="lcq_<%# Container.ItemIndex %>"
                                        class="form-select form-select-sm lc-bool-select"
                                        data-idx="<%# Container.ItemIndex %>">
                                        <option value="">( Select )</option>
                                        <option value="false">NO</option>
                                        <option value="true">Si</option>
                                    </select>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                        <asp:HiddenField ID="HfBoolAnswers" runat="server" ClientIDMode="Static" />
                    </asp:Panel>

                    <%-- Sub-step 3: single boolean question (IDMold & IDIMM) --%>
                    <asp:Panel ID="PnlSubStep3" runat="server" Visible="false">
                        <div class="mb-3">
                            <label class="form-label small">
                                <span class="required-star">★</span>
                                <asp:Literal ID="LitParam3Label" runat="server" />
                            </label>
                            <div class="text-muted small mb-1">Data Type: Boolean &nbsp; 0 files attached</div>
                            <asp:DropDownList ID="DdlParam3" runat="server"
                                CssClass="form-select form-select-sm"
                                ValidationGroup="searchGroup">
                                <asp:ListItem Value="">( Select )</asp:ListItem>
                                <asp:ListItem Value="false">NO</asp:ListItem>
                                <asp:ListItem Value="true">Si</asp:ListItem>
                            </asp:DropDownList>
                            <asp:RequiredFieldValidator ID="RfvParam3" runat="server"
                                ControlToValidate="DdlParam3"
                                InitialValue=""
                                ErrorMessage="Selection required."
                                CssClass="text-danger small" Display="Dynamic"
                                ValidationGroup="searchGroup" />
                        </div>
                    </asp:Panel>
                </div>
                <div class="modal-footer">
                    <asp:Button ID="BtnSave" runat="server" Text="💾 Save"
                        CssClass="btn btn-sm btn-success"
                        ValidationGroup="searchGroup"
                        OnClientClick="prepareBoolAnswers()"
                        OnClick="BtnSave_Click" />
                    <button type="button" class="btn btn-sm btn-secondary"
                        data-bs-dismiss="modal">&#10007; Return to POD</button>
                </div>
            </div>
        </div>
    </div>

</asp:Panel>

<script>
function prepareBoolAnswers() {
    var answers = [];
    document.querySelectorAll('.lc-bool-select').forEach(function(sel) {
        answers.push(sel.value);
    });
    var hf = document.getElementById('HfBoolAnswers');
    if (hf) hf.value = JSON.stringify(answers);
    return true;
}
</script>
