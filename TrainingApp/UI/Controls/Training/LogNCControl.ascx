<%@ Control Language="C#" AutoEventWireup="true"
    CodeBehind="LogNCControl.ascx.cs"
    Inherits="TrainingApp.UI.Controls.Training.LogNCControl" %>

<%-- ── NC Selection List (reemplaza el POD Work List, misma estructura que ─── --%>
<%-- ── el Data Collection List — spec 007) ────────────────────────────────── --%>
<%-- Mismo patrón que Component List y Data Collection List (Fase 6.5, entrega 7;
     captura "mensaje confirmacion scrap"): celdas sin recuadro sobre gris, código en
     negrita, descripción en cursiva, [Log NC] con ícono y paginación centrada. --%>
<asp:Panel ID="PnlNCSelection" runat="server" Visible="false">
    <div class="pod-list-view">
        <div class="list-header">
            <span>NC Selection</span>
            <div class="list-header-actions">
                <span class="pod-help-dot" title="Help">?</span>
                <asp:Button ID="BtnCloseList" runat="server" Text="✕"
                    CssClass="mes-titlebar-btn" ToolTip="Return to POD"
                    OnClick="BtnReturnPOD_Click" CausesValidation="false" />
            </div>
        </div>
        <div class="nc-grid">
            <asp:Repeater ID="RptNCCodes" runat="server">
                <ItemTemplate>
                    <div class="nc-cell" data-nc-index="<%# Container.ItemIndex %>">
                        <div class="nc-info">
                            <div class="nc-code"><%# Eval("Code") %></div>
                            <div class="nc-desc"><%# Eval("Description") %></div>
                        </div>
                        <asp:Button ID="BtnLogNC" runat="server"
                            CssClass="mes-btn mes-ico mes-ico-lognc"
                            Text="Log NC"
                            CommandArgument='<%# Eval("Id") + "|" + Eval("Code") %>'
                            OnClick="BtnLogNCCode_Click" CausesValidation="false" />
                    </div>
                </ItemTemplate>
            </asp:Repeater>
        </div>
        <%-- Paginación en cliente (8 códigos por página): es estado intermedio de UI,
             no necesita postback. --%>
        <div class="mes-pagination">
            <div class="mes-pagination-buttons">
                <button type="button" id="<%= PnlNCSelection.ClientID %>_first" class="mes-btn mes-btn-page" onclick="ncGoPage('first')">|&laquo; First</button>
                <button type="button" id="<%= PnlNCSelection.ClientID %>_prev"  class="mes-btn mes-btn-page" onclick="ncGoPage('prev')">&laquo; Prev</button>
                <button type="button" id="<%= PnlNCSelection.ClientID %>_next"  class="mes-btn mes-btn-page" onclick="ncGoPage('next')">Next &raquo;</button>
                <button type="button" id="<%= PnlNCSelection.ClientID %>_last"  class="mes-btn mes-btn-page" onclick="ncGoPage('last')">Last &raquo;|</button>
            </div>
            <span id="<%= PnlNCSelection.ClientID %>_pageInfo"></span>
        </div>
    </div>
</asp:Panel>

<%-- Página actual de NC Selection: sobrevive al postback que abre NC Data Entry,
     así la X del modal vuelve a la misma página. --%>
<asp:HiddenField ID="HfNCPage" runat="server" Value="0" />

<%-- ── NC Data Entry modal ─────────────────────────────────────────── --%>
<asp:Panel ID="PnlNCEntry" runat="server" Visible="false">
    <div class="modal fade show d-block" id="modalNCEntry" tabindex="-1" data-bs-backdrop="static">
        <div class="modal-dialog">
            <div class="modal-content">
                <%-- Barra estándar y subencabezado con el SFC, como la captura real. --%>
                <div class="modal-header">
                    <h6 class="modal-title">NC Data Entry</h6>
                    <div class="mes-titlebar-actions">
                        <button type="button" class="mes-titlebar-btn" title="Maximize"
                            onclick="mesToggleMaximize(this)">&#10530;</button>
                        <%-- La X vuelve a la NC Selection List (no al POD) y no registra intento. --%>
                        <asp:LinkButton ID="BtnCloseEntry" runat="server" CssClass="mes-titlebar-btn"
                            ToolTip="Back to NC Selection"
                            OnClick="BtnCloseEntry_Click" CausesValidation="false">&#10005;</asp:LinkButton>
                    </div>
                </div>
                <div class="mes-subheader">
                    <span>SFC: <asp:Literal ID="LitNCEntrySFC" runat="server" /></span>
                    <span class="mes-help" title="Help">?</span>
                </div>
                <div class="modal-body">
                    <div class="mb-3">
                        <label class="form-label small">
                            <span class="required-star">★</span> NC Code:
                        </label>
                        <asp:TextBox ID="TxtNCCode" runat="server"
                            CssClass="form-control form-control-sm" ReadOnly="true" />
                    </div>
                    <%-- Solo en áreas con Area.RequiresNCComponent = 1 (spec 007). --%>
                    <asp:Panel ID="PnlComponent" runat="server" CssClass="mb-3">
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
                    </asp:Panel>
                    <div class="mb-3">
                        <label class="form-label small">Comments:</label>
                        <asp:TextBox ID="TxtComments" runat="server"
                            CssClass="form-control form-control-sm" />
                    </div>
                    <asp:HiddenField ID="HfNCCodeId" runat="server" />
                </div>
                <div class="modal-footer">
                    <%-- Solo Add-Done es el camino correcto. Add y Done registran un
                         intento fallido ("Not the correct button"). --%>
                    <asp:Button ID="BtnAdd" runat="server" Text="✓ Add"
                        CssClass="btn btn-sm btn-outline-success"
                        OnClick="BtnWrongButton_Click" CausesValidation="false" />
                    <asp:Button ID="BtnAddDone" runat="server" Text="✓ Add-Done"
                        CssClass="btn btn-sm btn-success"
                        ValidationGroup="ncGroup"
                        OnClick="BtnAddDone_Click" />
                    <asp:Button ID="BtnReturnPOD" runat="server" Text="✗ Return to POD"
                        CssClass="btn btn-sm btn-secondary"
                        OnClick="BtnReturnPOD_Click" CausesValidation="false" />
                    <asp:Button ID="BtnDone" runat="server" Text="✓ Done"
                        CssClass="btn btn-sm btn-outline-success"
                        OnClick="BtnWrongButton_Click" CausesValidation="false" />
                </div>
            </div>
        </div>
    </div>
    <div class="modal-backdrop fade show"></div>
</asp:Panel>

<script>
(function () {
    var PAGE_SIZE = 8;
    var listId    = '<%= PnlNCSelection.ClientID %>';
    var hfPageId  = '<%= HfNCPage.ClientID %>';

    function cells() {
        var list = document.getElementById(listId);
        return list ? list.querySelectorAll('.nc-cell') : [];
    }

    function setDisabled(name, disabled) {
        var btn = document.getElementById(listId + '_' + name);
        if (btn) btn.disabled = disabled;
    }

    function render(page) {
        var all = cells();
        if (!all.length) return;

        var pages = Math.ceil(all.length / PAGE_SIZE);
        page = Math.max(0, Math.min(page, pages - 1));

        all.forEach(function (cell, i) {
            var visible = i >= page * PAGE_SIZE && i < (page + 1) * PAGE_SIZE;
            cell.style.display = visible ? '' : 'none';
        });

        var hf = document.getElementById(hfPageId);
        if (hf) hf.value = page;

        var info = document.getElementById(listId + '_pageInfo');
        if (info) info.textContent = 'Rows ' + (page * PAGE_SIZE + 1) + '-' +
            Math.min((page + 1) * PAGE_SIZE, all.length) + ' of ' + all.length;

        setDisabled('first', page === 0);
        setDisabled('prev',  page === 0);
        setDisabled('next',  page === pages - 1);
        setDisabled('last',  page === pages - 1);
    }

    function currentPage() {
        var hf = document.getElementById(hfPageId);
        return parseInt(hf && hf.value, 10) || 0;
    }

    window.ncGoPage = function (action) {
        var page  = currentPage();
        var pages = Math.ceil(cells().length / PAGE_SIZE);
        if (action === 'first') page = 0;
        else if (action === 'prev') page = page - 1;
        else if (action === 'next') page = page + 1;
        else if (action === 'last') page = pages - 1;
        render(page);
    };

    document.addEventListener('DOMContentLoaded', function () { render(currentPage()); });
})();
</script>
