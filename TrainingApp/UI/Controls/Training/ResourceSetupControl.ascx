<%@ Control Language="C#" AutoEventWireup="true"
    CodeBehind="ResourceSetupControl.ascx.cs"
    Inherits="TrainingApp.UI.Controls.Training.ResourceSetupControl" %>

<div class="modal fade" id="modalResourceSetup" tabindex="-1" aria-hidden="true" data-bs-backdrop="static">
    <div class="modal-dialog modal-lg">
        <div class="modal-content">
            <%-- Barra de título estándar + banda oscura del Resource Setup real. --%>
            <div class="modal-header">
                <h6 class="modal-title">Resource Setup</h6>
                <div class="mes-titlebar-actions">
                    <button type="button" class="mes-titlebar-btn" title="Maximize"
                        onclick="mesToggleMaximize(this)">&#10530;</button>
                    <button type="button" class="mes-titlebar-btn" title="Close"
                        data-bs-dismiss="modal">&#10005;</button>
                </div>
            </div>
            <div class="mes-page-header">
                <span>Resource Setup</span>
                <span class="mes-help" title="Help">?</span>
            </div>
            <%-- Cuerpo como la captura real (Fase 6.5, entrega 5): campos generales
                 centrados, franja celeste de acciones a la izquierda, tabla y pie centrado. --%>
            <div class="modal-body p-0">

                <%-- "Record changed" como el real (shown 5s after successful save) --%>
                <asp:Panel ID="PnlRecordChange" runat="server" Visible="false"
                    CssClass="record-change-banner">
                    <svg width="20" height="20" viewBox="0 0 20 20" aria-hidden="true">
                        <rect x="1" y="1" width="18" height="18" rx="3"
                            fill="#7cc36a" stroke="#3d6b33" stroke-width="1.5" />
                        <path d="M5 10.5l3.2 3.2L15 6.5" fill="none" stroke="#1d3d17"
                            stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round" />
                    </svg>
                    <span>Record changed</span>
                </asp:Panel>

                <%-- Campos generales: etiqueta a la derecha, valor a la izquierda, centrados. --%>
                <div class="rs-form">
                    <span class="rs-label"><span class="pod-star">*</span> Site:</span>
                    <span class="rs-value"><asp:Literal ID="LitSite" runat="server" /></span>

                    <span class="rs-label"><span class="pod-star">*</span> Resource:</span>
                    <span class="rs-value">
                        <span class="mes-field mes-field-copy"><asp:Literal ID="LitResource" runat="server" /><span class="mes-field-icon">&#10697;</span></span>
                        <button type="button" class="mes-btn-outline ms-4"
                            onclick="rsRetrieve()">Retrieve</button>
                    </span>

                    <span class="rs-label">Setup State:</span>
                    <span class="rs-value">
                        <%-- State 1: plain text --%>
                        <span id="spanSetupStateOpen">Open</span>
                        <%-- State 2: dropdown (hidden until Retrieve) --%>
                        <asp:DropDownList ID="DdlSetupState" runat="server"
                            CssClass="rs-select" style="display:none"
                            ClientIDMode="Static">
                            <asp:ListItem Value="Set Up">Set Up</asp:ListItem>
                            <asp:ListItem Value="Open">Open</asp:ListItem>
                        </asp:DropDownList>
                    </span>
                </div>

                <%-- Acciones juntas a la izquierda sobre la franja celeste. Visibles desde el
                     inicio, como la captura; deshabilitadas hasta Retrieve (rsSetToolbarEnabled). --%>
                <div id="rsToolbar" class="rs-toolbar">
                    <button type="button" class="rs-tool-btn" disabled onclick="rsInsertNew()">INSERT NEW</button>
                    <button type="button" class="rs-tool-btn" disabled onclick="rsInsertBefore()">INSERT BEFORE</button>
                    <button type="button" class="rs-tool-btn" disabled onclick="rsInsertAfter()">INSERT AFTER</button>
                    <button type="button" class="rs-tool-btn" disabled onclick="rsRemoveSelected()">REMOVE SELECTED</button>
                    <button type="button" class="rs-tool-btn" disabled onclick="rsRemoveAll()">REMOVE ALL</button>
                </div>

                <%-- Tabla de dos columnas con anchos fijos desde el primer render: agregar la
                     primera fila no mueve los encabezados. La línea vertical entre columnas
                     recorre todo el alto (también el área sin filas), como el real. El ⧉ de
                     cada valor copia al portapapeles; las filas se borran con REMOVE SELECTED /
                     REMOVE ALL. --%>
                <div id="rsTableWrapper" class="rs-table-wrapper">
                    <table class="rs-table" id="rsTable">
                        <colgroup>
                            <col class="rs-col-type" />
                            <col />
                        </colgroup>
                        <thead>
                            <tr>
                                <th>Setup Type</th>
                                <th>Setup Type Value</th>
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
            <div class="modal-footer rs-footer">
                <asp:Button ID="BtnSave" runat="server" Text="Save"
                    CssClass="mes-btn-outline"
                    OnClick="BtnSave_Click"
                    OnClientClick="return rsPrepareSubmit();"
                    CausesValidation="false" />
                <asp:Button ID="BtnClear" runat="server" Text="Clear"
                    CssClass="mes-btn-outline"
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
        rsSetToolbarEnabled(true);

        // Enable Save
        var saveBtn = document.getElementById('<%= BtnSave.ClientID %>');
        if (saveBtn) saveBtn.disabled = false;
    };

    // Las acciones de la tabla solo funcionan después de Retrieve (igual que antes, cuando
    // la franja estaba oculta); ahora se ven desde el inicio, deshabilitadas.
    window.rsSetToolbarEnabled = function(enabled) {
        document.querySelectorAll('#rsToolbar .rs-tool-btn')
            .forEach(function(b) { b.disabled = !enabled; });
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

    // ⧉ de cada fila: copia el valor al portapapeles para pegarlo en otra fila (como el
    // real). navigator.clipboard exige contexto seguro (https o localhost); si no está,
    // se usa execCommand. El ícono se marca un instante como confirmación.
    window.rsCopyValue = function(btn) {
        var input = btn.parentNode.querySelector('.rs-value');
        if (!input) return;
        var done = function() {
            btn.classList.add('copied');
            setTimeout(function() { btn.classList.remove('copied'); }, 700);
        };
        var legacy = function() {
            input.select();
            try { document.execCommand('copy'); } catch (e) { /* sin portapapeles */ }
            done();
        };
        if (navigator.clipboard && window.isSecureContext)
            navigator.clipboard.writeText(input.value).then(done, legacy);
        else
            legacy();
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
            '<td><select class="rs-select rs-type">' +
                '<option value="Shop Order">Shop Order</option>' +
                '<option value="Tool Number">Tool Number</option>' +
            '</select></td>' +
            '<td><span class="mes-field mes-field-copy rs-value-box">' +
                '<input type="text" class="rs-value" />' +
                '<button type="button" class="mes-field-icon" title="Copy" ' +
                    'onclick="rsCopyValue(this)">&#10697;</button>' +
            '</span></td>';
        tr.querySelector('.rs-type').value = type;
        tr.querySelector('.rs-value').value = value;
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
        // 1. Setup State: es una decisión del operador — se envía y la califica el servidor.
        var ddl = document.getElementById('DdlSetupState');
        var setupState = ddl ? ddl.value : 'Open';

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

        // 4b. Shop Order value must be 100% numeric (spec 005 — semilla de SimulatedOrder)
        if (!/^\d+$/.test((shopOrders[0].value || '').trim())) {
            rsShowError('Shop Order inválido',
                'El valor de Shop Order debe ser numérico.');
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

        // 7. Sin < ni >: la validación de solicitudes de ASP.NET rechaza el postback con
        //    marcado y el operador vería una página de error (auditoría AUD-11).
        var markupRow = rows.some(function(r) { return /[<>]/.test(r.value); });
        if (markupRow) {
            rsShowError('Caracteres no permitidos',
                'Los valores no pueden contener los caracteres < ni >.');
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
        rsSetToolbarEnabled(true);
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
