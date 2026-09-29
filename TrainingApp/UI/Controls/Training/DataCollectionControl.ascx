<%@ Control Language="C#" AutoEventWireup="true"
    CodeBehind="DataCollectionControl.ascx.cs"
    Inherits="TrainingApp.UI.Controls.Training.DataCollectionControl" %>

<%-- Data Collection List (replaces POD Work List) --%>
<div class="pod-list-view">
    <div class="list-header">
        <span>Data Collection List</span>
        <div class="d-flex gap-2">
            <button type="button" class="btn btn-sm btn-outline-secondary" title="Help">?</button>
            <asp:Button ID="BtnCloseList" runat="server" Text="✗"
                CssClass="btn btn-sm btn-outline-secondary" ToolTip="Return to POD"
                OnClick="BtnReturnPOD_Click" CausesValidation="false" />
        </div>
    </div>
    <asp:Repeater ID="RptGroups" runat="server">
        <ItemTemplate>
            <div class="pod-list-row">
                <div>
                    <div class="fw-semibold small"><%# Eval("GroupCode") %></div>
                    <div class="text-muted" style="font-size:0.76rem">
                        <%# Eval("Description") %><br />
                        Collect Data At: <%# Eval("CollectDataAt") %>
                    </div>
                </div>
                <asp:Button ID="BtnCollect" runat="server"
                    CssClass="btn btn-sm btn-primary"
                    Text="Collect"
                    CommandArgument='<%# Eval("GroupCode") %>'
                    Enabled='<%# !(bool)Eval("IsSaved") %>'
                    OnClick="BtnCollect_Click" />
            </div>
        </ItemTemplate>
    </asp:Repeater>
    <div class="pod-pagination">
        <button type="button" class="btn btn-sm btn-outline-secondary">|&lt; First</button>
        <button type="button" class="btn btn-sm btn-outline-secondary">&lt;&lt; Prev</button>
        <button type="button" class="btn btn-sm btn-outline-secondary">Next &gt;&gt;</button>
        <button type="button" class="btn btn-sm btn-outline-secondary">Last &gt;|</button>
    </div>
</div>

<%-- Data Collection Entry modal --%>
<div class="modal fade" id="modalDCEntry" tabindex="-1" aria-hidden="true" data-bs-backdrop="static">
    <div class="modal-dialog modal-lg">
        <div class="modal-content">
            <div class="modal-header">
                <h6 class="modal-title">Data Collection Entry</h6>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>
            <div class="modal-body">
                <div class="mb-2 small">
                    SFC: <strong><asp:Literal ID="LitSFCCounter" runat="server" /></strong>
                    &nbsp;|&nbsp; DC Group: <strong><asp:Literal ID="LitActiveGroup" runat="server" /></strong>
                </div>
                <hr class="mt-1 mb-2" />

                <%-- Dynamic parameter fields rendered by code-behind --%>
                <asp:PlaceHolder ID="PhParameters" runat="server" />

                <%-- Hidden field to collect all answers as JSON --%>
                <asp:HiddenField ID="HfAnswers" runat="server" ClientIDMode="Static" />
                <asp:HiddenField ID="HfActiveGroup" runat="server" ClientIDMode="Static" />
            </div>
            <div class="modal-footer">
                <asp:Button ID="BtnSave" runat="server" Text="💾 Save"
                    CssClass="btn btn-sm btn-success"
                    OnClientClick="collectDCAnswers()"
                    OnClick="BtnSave_Click" />
                <%-- Save Draft no guarda nada en el simulador: solo orienta al operador,
                     sin postback para no perder los valores ya cargados. --%>
                <button type="button" class="btn btn-sm btn-outline-secondary"
                    onclick="Swal.fire({icon:'info',title:'Save Draft',text:'Use \'Save\' to save'});">&#128190; Save Draft</button>
                <button type="button" class="btn btn-sm btn-danger"
                    onclick="clearDCFields()">&#128308; Clear</button>
                <asp:Button ID="BtnReturnPOD" runat="server" Text="✗ Return to POD"
                    CssClass="btn btn-sm btn-secondary"
                    OnClick="BtnReturnPOD_Click" CausesValidation="false" />
            </div>
        </div>
    </div>
</div>

<script>
function collectDCAnswers() {
    var answers = {};
    document.querySelectorAll('[data-paramid]').forEach(function(el) {
        answers[el.getAttribute('data-paramid')] = el.value;
    });
    document.getElementById('HfAnswers').value = JSON.stringify(answers);
    return true;
}
function clearDCFields() {
    document.querySelectorAll('[data-paramid]').forEach(function(el) {
        el.value = '';
    });
}
</script>
