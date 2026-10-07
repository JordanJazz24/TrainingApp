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
        <div class="nc-grid" data-guide="nc-list">
            <asp:Repeater ID="RptNCCodes" runat="server">
                <ItemTemplate>
                    <div class="nc-cell" data-nc-index="<%# Container.ItemIndex %>">
                        <div class="nc-info">
                            <div class="nc-code"><%#: Eval("Code") %></div>
                            <div class="nc-desc"><%#: Eval("Description") %></div>
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
        <div class="modal-dialog modal-lg">
            <div class="modal-content">
                <%-- Mismo patrón que Data Collection Entry (Fase 6.5, entrega 8; captura
                     "MODAL NC DATA ENTRY"): barra solo con ⤢ ✕ → "NC Data Entry" con (?) →
                     fila SFC → formulario con etiquetas a la derecha → pie del MES. --%>
                <div class="modal-header mes-titlebar-bare">
                    <div class="mes-titlebar-actions">
                        <button type="button" class="mes-titlebar-btn" title="Maximize"
                            onclick="mesToggleMaximize(this)">&#10530;</button>
                        <%-- La X vuelve a la NC Selection List (no al POD) y no registra intento. --%>
                        <asp:LinkButton ID="BtnCloseEntry" runat="server" CssClass="mes-titlebar-btn"
                            ToolTip="Back to NC Selection"
                            OnClick="BtnCloseEntry_Click" CausesValidation="false">&#10005;</asp:LinkButton>
                    </div>
                </div>
                <div class="mes-subheader mes-divider">
                    <span>NC Data Entry</span>
                    <span class="mes-help" title="Help">?</span>
                </div>
                <%-- "{primer SFC} (1/N)" con N = piezas seleccionadas, como Assemble Components. --%>
                <div class="mes-sfc-band">
                    SFC: <asp:Literal ID="LitNCEntrySFC" runat="server" />
                </div>
                <div class="modal-body nc-entry-body">
                    <div class="nc-form">
                        <span class="nc-form-label"><span class="pod-star">*</span> NC Code:</span>
                        <asp:TextBox ID="TxtNCCode" runat="server"
                            CssClass="mes-input nc-form-input" ReadOnly="true" />
                    </div>
                    <%-- Solo en áreas con Area.RequiresNCComponent = 1 (spec 007). Sigue siendo
                         obligatorio (RfvComponent), pero sin asterisco: así lo muestra el real. --%>
                    <asp:Panel ID="PnlComponent" runat="server" CssClass="nc-form">
                        <span class="nc-form-label">Component:</span>
                        <span class="mes-field nc-form-input">
                            <asp:TextBox ID="TxtComponent" runat="server"
                                ValidationGroup="ncGroup" data-guide="nc-component" />
                            <span class="mes-field-icon">&#10697;</span>
                        </span>
                        <span></span>
                        <asp:RequiredFieldValidator ID="RfvComponent" runat="server"
                            ControlToValidate="TxtComponent"
                            ErrorMessage="Component required." CssClass="text-danger small"
                            Display="Dynamic" ValidationGroup="ncGroup" />
                    </asp:Panel>
                    <div class="nc-form">
                        <span class="nc-form-label">Comments:</span>
                        <asp:TextBox ID="TxtComments" runat="server" TextMode="MultiLine" Rows="2"
                            CssClass="mes-input nc-form-input nc-form-comments" />
                    </div>
                    <asp:HiddenField ID="HfNCCodeId" runat="server" />
                </div>
                <div class="modal-footer mes-footer">
                    <%-- Con varias piezas seleccionadas el NC se registra en todas (spec 008):
                         "Apply to all SFCs" marcado y fijo, a la izquierda de Add, igual que en
                         Assemble Components. --%>
                    <asp:Panel ID="PnlNCApplyAll" runat="server" CssClass="asm-apply-all" Visible="false">
                        <label class="mb-0"><input type="checkbox" checked disabled /> Apply to all SFCs</label>
                    </asp:Panel>
                    <%-- Solo Add-Done es el camino correcto. Add y Done registran un
                         intento fallido ("Not the correct button"). --%>
                    <asp:Button ID="BtnAdd" runat="server" Text="Add"
                        CssClass="mes-btn mes-ico mes-ico-check"
                        OnClick="BtnWrongButton_Click" CausesValidation="false" />
                    <asp:Button ID="BtnAddDone" runat="server" Text="Add-Done"
                        CssClass="mes-btn mes-ico mes-ico-check" data-guide="nc-add-done"
                        ValidationGroup="ncGroup"
                        OnClick="BtnAddDone_Click" />
                    <asp:Button ID="BtnReturnPOD" runat="server" Text="Return to POD"
                        CssClass="mes-btn mes-ico mes-ico-cancel"
                        OnClick="BtnReturnPOD_Click" CausesValidation="false" />
                    <asp:Button ID="BtnDone" runat="server" Text="Done"
                        CssClass="mes-btn mes-ico mes-ico-check"
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
