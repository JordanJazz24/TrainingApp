<%@ Control Language="C#" AutoEventWireup="true"
    CodeBehind="DataCollectionControl.ascx.cs"
    Inherits="TrainingApp.UI.Controls.Training.DataCollectionControl" %>

<%-- Data Collection List (replaces POD Work List) --%>
<%-- Distribución de la captura "tabla de data collection list" (Fase 6.5, entrega 7):
     código del grupo a la izquierda, descripción y "Collect Data At" al centro, [Collect]
     con ícono a la derecha y paginación centrada con "Rows 1-N of N" debajo. --%>
<div class="pod-list-view">
    <div class="list-header">
        <span>Data Collection List</span>
        <div class="list-header-actions">
            <span class="pod-help-dot" title="Help">?</span>
            <asp:Button ID="BtnCloseList" runat="server" Text="✕"
                CssClass="mes-titlebar-btn" ToolTip="Return to POD"
                OnClick="BtnReturnPOD_Click" CausesValidation="false" />
        </div>
    </div>
    <asp:Repeater ID="RptGroups" runat="server">
        <ItemTemplate>
            <div class="mes-list-row">
                <div class="mes-list-key">
                    <div class="mes-list-code"><%# Eval("GroupCode") %></div>
                </div>
                <div class="mes-list-center">
                    <div class="mes-list-text"><%# Eval("Description") %></div>
                    <div class="mes-list-sub">
                        <span class="mes-list-label">Collect Data At:</span> <%# Eval("CollectDataAt") %>
                    </div>
                </div>
                <div class="mes-list-actions">
                    <asp:Button ID="BtnCollect" runat="server"
                        CssClass="mes-btn mes-btn-stack mes-ico mes-ico-dccollect"
                        Text="Collect"
                        CommandArgument='<%# Eval("GroupCode") %>'
                        Enabled='<%# !(bool)Eval("IsSaved") %>'
                        OnClick="BtnCollect_Click" />
                </div>
            </div>
        </ItemTemplate>
    </asp:Repeater>
    <%-- Todos los grupos entran en una página: la paginación es la del real, deshabilitada. --%>
    <div class="mes-pagination">
        <div class="mes-pagination-buttons">
            <button type="button" class="mes-btn mes-btn-page" disabled>|&laquo; First</button>
            <button type="button" class="mes-btn mes-btn-page" disabled>&laquo; Prev</button>
            <button type="button" class="mes-btn mes-btn-page" disabled>Next &raquo;</button>
            <button type="button" class="mes-btn mes-btn-page" disabled>Last &raquo;|</button>
        </div>
        <span>Rows 1-<asp:Literal ID="LitGroupsTotal" runat="server" /> of
            <asp:Literal ID="LitGroupsTotalOf" runat="server" /></span>
    </div>
</div>

<%-- Data Collection Entry modal --%>
<div class="modal fade" id="modalDCEntry" tabindex="-1" aria-hidden="true" data-bs-backdrop="static">
    <div class="modal-dialog modal-lg">
        <div class="modal-content">
            <%-- Barra estándar, subencabezado con la pieza "SFC: … (n/N)" y fila del DC
                 Group, como la captura real. --%>
            <div class="modal-header">
                <h6 class="modal-title">Data Collection Entry</h6>
                <div class="mes-titlebar-actions">
                    <button type="button" class="mes-titlebar-btn" title="Maximize"
                        onclick="mesToggleMaximize(this)">&#10530;</button>
                    <button type="button" class="mes-titlebar-btn" title="Close"
                        data-bs-dismiss="modal">&#10005;</button>
                </div>
            </div>
            <div class="mes-subheader">
                <span>SFC: <asp:Literal ID="LitSFCCounter" runat="server" /></span>
                <span class="mes-help" title="Help">?</span>
            </div>
            <div class="mes-info-row">
                DC Group: <strong><asp:Literal ID="LitActiveGroup" runat="server" /></strong>
            </div>
            <div class="modal-body">

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
