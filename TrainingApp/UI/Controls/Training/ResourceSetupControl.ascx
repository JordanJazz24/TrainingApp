<%@ Control Language="C#" AutoEventWireup="true"
    CodeBehind="ResourceSetupControl.ascx.cs"
    Inherits="TrainingApp.UI.Controls.Training.ResourceSetupControl" %>

<div class="modal fade" id="modalResourceSetup" tabindex="-1" aria-hidden="true" data-bs-backdrop="static">
    <div class="modal-dialog modal-lg">
        <div class="modal-content">
            <div class="modal-header d-flex justify-content-between align-items-center">
                <h6 class="modal-title">Resource Setup</h6>
                <div class="d-flex gap-2">
                    <button type="button" class="btn btn-sm btn-outline-light" title="Help">?</button>
                    <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                </div>
            </div>
            <div class="modal-body">

                <%-- Record Change banner (shown 5s after successful save) --%>
                <asp:Panel ID="PnlRecordChange" runat="server" Visible="false"
                    CssClass="record-change-banner alert alert-success py-1 px-2 mb-2">
                    &#10003; Record Change
                </asp:Panel>

                <%-- Site / Resource row --%>
                <div class="row g-2 mb-3 align-items-end">
                    <div class="col-auto">
                        <label class="form-label small mb-0">* Site:</label>
                        <span class="form-control-plaintext form-control-sm">
                            <asp:Literal ID="LitSite" runat="server" />
                        </span>
                    </div>
                    <div class="col-auto">
                        <label class="form-label small mb-0">* Resource:</label>
                        <span class="form-control-plaintext form-control-sm">
                            <asp:Literal ID="LitResource" runat="server" />
                        </span>
                    </div>
                    <div class="col-auto">
                        <button type="button" class="btn btn-sm btn-secondary"
                            onclick="rsRetrieve()">Retrieve</button>
                    </div>
                    <div class="col-auto">
                        <label class="form-label small mb-0">Setup State:</label>
                        <%-- State 1: plain text --%>
                        <span id="spanSetupStateOpen" class="form-control-plaintext form-control-sm">Open</span>
                        <%-- State 2: dropdown (hidden until Retrieve) --%>
                        <asp:DropDownList ID="DdlSetupState" runat="server"
                            CssClass="form-select form-select-sm" style="display:none; width:auto"
                            ClientIDMode="Static">
                            <asp:ListItem Value="Set Up">Set Up</asp:ListItem>
                            <asp:ListItem Value="Open">Open</asp:ListItem>
                        </asp:DropDownList>
                    </div>
                </div>

                <%-- Toolbar (hidden until Retrieve) --%>
                <div id="rsToolbar" class="d-flex gap-1 mb-2" style="display:none!important">
                    <button type="button" class="btn btn-sm btn-outline-secondary"
                        onclick="rsInsertNew()">INSERT NEW</button>
                    <button type="button" class="btn btn-sm btn-outline-secondary"
                        onclick="rsInsertBefore()">INSERT BEFORE</button>
                    <button type="button" class="btn btn-sm btn-outline-secondary"
                        onclick="rsInsertAfter()">INSERT AFTER</button>
                    <button type="button" class="btn btn-sm btn-outline-secondary ms-auto"
                        onclick="rsRemoveSelected()">REMOVE SELECTED</button>
                    <button type="button" class="btn btn-sm btn-outline-secondary"
                        onclick="rsRemoveAll()">REMOVE ALL</button>
                </div>

                <%-- Rows table (hidden until Retrieve) --%>
                <div id="rsTableWrapper" class="border rounded" style="display:none">
                    <table class="table table-sm table-bordered mb-0" id="rsTable">
                        <thead class="table-light">
                            <tr>
                                <th style="width:180px">Setup Type</th>
                                <th>Setup Type Value</th>
                                <th style="width:40px"></th>
                            </tr>
                        </thead>
                        <tbody id="rsTableBody">
                        </tbody>
                    </table>
                </div>

                <%-- Hidden fields --%>
                <asp:HiddenField ID="HfRows"      runat="server" ClientIDMode="Static" />
                <asp:HiddenField ID="HfSetupState" runat="server" ClientIDMode="Static" />
                <asp:HiddenField ID="HfRetrieved" runat="server" ClientIDMode="Static" Value="false" />
            </div>
            <div class="modal-footer">
                <asp:Button ID="BtnSave" runat="server" Text="Save"
                    CssClass="btn btn-sm btn-success"
                    OnClick="BtnSave_Click"
                    OnClientClick="return rsPrepareSubmit();"
                    CausesValidation="false" />
                <asp:Button ID="BtnClear" runat="server" Text="Clear"
                    CssClass="btn btn-sm btn-outline-secondary"
                    OnClick="BtnClear_Click" CausesValidation="false" />
            </div>
        </div>
    </div>
</div>

<script>
(function() {
    // ── Retrieve ─────────────────────────────────────────────────────────────
    window.rsRetrieve = function() {
        // Mark retrieved in hidden field so code-behind can restore state on PostBack
        document.getElementById('HfRetrieved').value = 'true';

        document.getElementById('spanSetupStateOpen').style.display = 'none';
        var ddl = document.getElementById('DdlSetupState');
        if (ddl) ddl.style.display = '';
        document.getElementById('rsToolbar').style.removeProperty('display');
        document.getElementById('rsTableWrapper').style.display = '';

        // Enable Save
        var saveBtn = document.getElementById('<%= BtnSave.ClientID %>');
        if (saveBtn) saveBtn.disabled = false;
    };

    // ── Toolbar actions ───────────────────────────────────────────────────────
    window.rsInsertNew    = function() { addRow('Shop Order', ''); };
    window.rsInsertBefore = function() {
        var sel = document.querySelector('#rsTableBody tr.table-primary');
        var idx = sel ? Array.from(document.querySelectorAll('#rsTableBody tr')).indexOf(sel) : -1;
        addRow('Shop Order', '', idx);
    };
    window.rsInsertAfter  = function() {
        var sel = document.querySelector('#rsTableBody tr.table-primary');
        var idx = sel ? Array.from(document.querySelectorAll('#rsTableBody tr')).indexOf(sel) : -1;
        addRow('Shop Order', '', idx + 1);
    };
    window.rsRemoveSelected = function() {
        var sel = document.querySelector('#rsTableBody tr.table-primary');
        if (sel) sel.remove();
    };
    window.rsRemoveAll = function() {
        document.getElementById('rsTableBody').innerHTML = '';
    };

    // Restore rows from JSON after PostBack (DOM rows don't survive page re-render)
    window.rsRestoreRows = function(rows) {
        if (!rows || !rows.length) return;
        document.getElementById('rsTableBody').innerHTML = '';
        rows.forEach(function(row) { addRow(row.type, row.value); });
    };

    function addRow(type, value, atIndex) {
        var tbody = document.getElementById('rsTableBody');
        var tr = document.createElement('tr');
        tr.onclick = function() {
            document.querySelectorAll('#rsTableBody tr').forEach(function(r) { r.classList.remove('table-primary'); });
            tr.classList.add('table-primary');
        };
        tr.innerHTML =
            '<td><select class="form-select form-select-sm rs-type">' +
                '<option value="Shop Order">Shop Order</option>' +
                '<option value="Tool Number">Tool Number</option>' +
            '</select></td>' +
            '<td><input type="text" class="form-control form-control-sm rs-value" value="' + value + '" /></td>' +
            '<td><button type="button" class="btn btn-sm btn-link text-danger p-0" ' +
                'onclick="this.closest(\'tr\').remove()">&#128465;</button></td>';
        tr.querySelector('.rs-type').value = type;
        var rows = tbody.querySelectorAll('tr');
        if (atIndex !== undefined && atIndex >= 0 && atIndex < rows.length)
            tbody.insertBefore(tr, rows[atIndex]);
        else
            tbody.appendChild(tr);
    }

    // ── Error helper: show SweetAlert then re-assert Bootstrap modal ─────────
    // BUG 2 fix: SweetAlert2 manipulates body overflow / backdrop on close,
    // which can cause Bootstrap to dismiss its own static modal. After the
    // SweetAlert resolves we explicitly re-show the Bootstrap modal if needed.
    function rsShowError(title, text) {
        Swal.fire({ icon: 'warning', title: title, text: text }).then(function() {
            var el = document.getElementById('modalResourceSetup');
            if (el && !el.classList.contains('show')) showModalSafe('modalResourceSetup');
        });
    }

    // ── Validation + submit preparation ──────────────────────────────────────
    window.rsPrepareSubmit = function() {
        // 1. Status validation (highest priority)
        var ddl = document.getElementById('DdlSetupState');
        var setupState = ddl ? ddl.value : 'Open';
        if (setupState !== 'Set Up') {
            rsShowError('Setup State incorrecto',
                'El Setup State debe estar en "Set Up" antes de guardar.');
            return false;
        }

        // Collect rows
        var rows = [];
        document.querySelectorAll('#rsTableBody tr').forEach(function(tr) {
            rows.push({
                type:  tr.querySelector('.rs-type').value,
                value: tr.querySelector('.rs-value').value
            });
        });

        // 2. At least one row exists
        if (rows.length === 0) {
            rsShowError('Sin recursos',
                'No existen recursos creados. Agrega al menos un Shop Order y un Tool Number.');
            return false;
        }

        // 3. At least 2 rows
        if (rows.length < 2) {
            rsShowError('Recursos insuficientes',
                'Se requieren mínimo 2 recursos: 1 Shop Order y al menos 1 Tool Number.');
            return false;
        }

        // 4. Exactly 1 Shop Order
        var shopOrders = rows.filter(function(r) { return r.type === 'Shop Order'; });
        if (shopOrders.length === 0) {
            rsShowError('Shop Order requerido',
                'Debe existir exactamente 1 fila de tipo Shop Order.');
            return false;
        }
        if (shopOrders.length > 1) {
            rsShowError('Shop Order duplicado',
                'Solo puede existir 1 fila de tipo Shop Order.');
            return false;
        }

        // 5. At least 1 Tool Number
        var toolNumbers = rows.filter(function(r) { return r.type === 'Tool Number'; });
        if (toolNumbers.length === 0) {
            rsShowError('Tool Number requerido',
                'Debe existir al menos 1 fila de tipo Tool Number.');
            return false;
        }

        // 6. No empty values
        var emptyRow = rows.some(function(r) { return !r.value || r.value.trim() === ''; });
        if (emptyRow) {
            rsShowError('Valores incompletos',
                'Todas las filas deben tener un valor. Ninguna fila puede estar vacía.');
            return false;
        }

        // All validations passed — populate hidden fields for PostBack
        document.getElementById('HfRows').value       = JSON.stringify(rows);
        document.getElementById('HfSetupState').value = setupState;
        return true;
    };

    // ── Restore UI + rows on PostBack (called via RegisterStartupScript) ──────
    // BUG 3 fix: after a successful Save PostBack the modal re-renders from scratch.
    // We restore the retrieved UI state AND re-add the rows from HfRows so the
    // modal looks unchanged to the operator (only the Record Change banner is new).
    window.rsRestoreRetrievedState = function() {
        document.getElementById('spanSetupStateOpen').style.display = 'none';
        var ddl = document.getElementById('DdlSetupState');
        if (ddl) ddl.style.display = '';
        document.getElementById('rsToolbar').style.removeProperty('display');
        document.getElementById('rsTableWrapper').style.display = '';
        var saveBtn = document.getElementById('<%= BtnSave.ClientID %>');
        if (saveBtn) saveBtn.disabled = false;

        // Restore rows from the hidden field that was submitted with the form
        var hfRows = document.getElementById('HfRows');
        if (hfRows && hfRows.value && hfRows.value !== '[]' && hfRows.value !== '') {
            try {
                var rows = JSON.parse(hfRows.value);
                document.getElementById('rsTableBody').innerHTML = '';
                rows.forEach(function(r) { addRow(r.type, r.value); });
            } catch (e) { /* ignore malformed JSON */ }
        }
    };
})();
</script>
