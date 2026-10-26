<%@ Control Language="C#" AutoEventWireup="true"
    CodeBehind="CustomResourceSetupControl.ascx.cs"
    Inherits="TrainingApp.UI.Controls.Training.CustomResourceSetupControl" %>

<%-- Custom Resource Setup (spec 020): capturas de "Screens SAP MES REAL/Custom Resourse Setup".
     Un solo postback, el Yes de la confirmación (D-189); Retrieve, Associate y los avisos
     Information y Confirmation se resuelven en el cliente. --%>
<div class="modal fade" id="modalCustomResourceSetup" tabindex="-1" aria-hidden="true" data-bs-backdrop="static">
    <div class="modal-dialog modal-xl">
        <div class="modal-content crs">
            <div class="modal-header">
                <h6 class="modal-title">Custom Resource Setup</h6>
                <div class="mes-titlebar-actions">
                    <button type="button" class="mes-titlebar-btn" title="Maximize"
                        onclick="mesToggleMaximize(this)">&#10530;</button>
                    <button type="button" class="mes-titlebar-btn" title="Close"
                        data-bs-dismiss="modal" data-guide="crs-close">&#10005;</button>
                </div>
            </div>
            <div class="crs-topbar">
                <span>Custom Resource Setup</span>
                <span class="crs-user"><asp:Literal ID="LitCode1" runat="server" /></span>
            </div>

            <div class="modal-body p-0">
                <div class="crs-panel">
                    <div class="crs-row">
                        <span class="crs-label">Site :</span>
                        <span class="crs-site"><asp:Literal ID="LitSite" runat="server" /></span>
                    </div>
                    <div class="crs-row">
                        <span class="crs-label">Resource :</span>
                        <span class="crs-input crs-readonly"><asp:Literal ID="LitResource" runat="server" /></span>
                        <button type="button" class="crs-retrieve" onclick="window.crsRetrieve()" data-guide="crs-retrieve">
                            <svg class="crs-ico" viewBox="0 0 16 16" aria-hidden="true"><circle cx="6.5" cy="6.5" r="4.5" fill="none" stroke="currentColor" stroke-width="1.8"/><path d="M10 10l4 4" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/></svg> Retrieve
                        </button>
                    </div>
                    <div class="crs-row">
                        <span class="crs-label">Operation :</span>
                        <span class="crs-input crs-readonly"><asp:Literal ID="LitOperation" runat="server" /></span>
                    </div>
                    <div class="crs-row">
                        <span class="crs-label">Setup State :</span>
                        <select id="crsSetupState" class="crs-select" disabled data-guide="crs-setup-state">
                            <option value="Open">Open</option>
                        </select>
                    </div>
                    <div class="crs-row">
                        <span class="crs-label">ShopOrder:</span>
                        <span class="crs-input crs-input-icon">
                            <input type="text" id="crsShopOrder" maxlength="20" autocomplete="off" disabled
                                data-guide="crs-shop-order" />
                            <span class="crs-field-icon" aria-hidden="true">&#10697;</span>
                        </span>
                        <button type="button" id="crsAssociateBtn" class="crs-link" disabled
                            onclick="window.crsAssociate()" data-guide="crs-associate">
                            <svg class="crs-ico" viewBox="0 0 16 16" aria-hidden="true"><path d="M2.5 2.5h9l2 2v9h-11z" fill="none" stroke="currentColor" stroke-width="1.4" stroke-linejoin="round"/><path d="M5 2.5v3.5h5V2.5M5 13.5V9.5h6v4" fill="none" stroke="currentColor" stroke-width="1.4"/></svg> Associate New ShopOrder
                        </button>
                    </div>
                </div>

                <div class="crs-table-title">Tool Number Association Table</div>
                <div class="crs-table-area" data-guide="crs-table">
                    <table class="crs-table">
                        <thead>
                            <tr>
                                <th>Tool Number</th>
                                <th>Tool Number Description</th>
                                <th>Tool Group</th>
                                <th>Tool Group Description</th>
                            </tr>
                        </thead>
                        <tbody id="crsTools">
                            <tr class="crs-nodata"><td colspan="4">No data</td></tr>
                        </tbody>
                    </table>
                </div>

                <div class="crs-actions">
                    <button type="button" id="crsSaveBtn" class="crs-link" disabled onclick="window.crsSave()" data-guide="crs-save">
                        <svg class="crs-ico" viewBox="0 0 16 16" aria-hidden="true"><path d="M2.5 2.5h9l2 2v9h-11z" fill="none" stroke="currentColor" stroke-width="1.4" stroke-linejoin="round"/><path d="M5 2.5v3.5h5V2.5M5 13.5V9.5h6v4" fill="none" stroke="currentColor" stroke-width="1.4"/></svg> Save
                    </button>
                    <button type="button" id="crsClearBtn" class="crs-link" onclick="window.crsClear()">
                        <svg class="crs-ico" viewBox="0 0 16 16" aria-hidden="true"><path d="M13 8a5 5 0 1 1-1.5-3.6" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round"/><path d="M12 1.8v3h-3" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round"/></svg> Clear
                    </button>
                </div>
            </div>

            <%-- Avisos del MES dentro del modal: la guía los encuentra en el modal de más arriba. --%>
            <div id="crsInfo" class="crs-dialog-backdrop" style="display:none">
                <div class="crs-dialog" role="dialog" aria-labelledby="crsInfoTitle">
                    <div class="crs-dialog-head crs-ok">
                        <span class="crs-dialog-ico crs-ico-ok" aria-hidden="true">&#10003;</span>
                        <span id="crsInfoTitle">Information</span>
                    </div>
                    <div class="crs-dialog-body">Shoporder is associated in resource setup. Please associate tool numbers as well</div>
                    <div class="crs-dialog-foot">
                        <button type="button" class="crs-dialog-btn crs-primary" onclick="window.crsHide('crsInfo')"
                            data-guide="crs-info-ok">OK</button>
                    </div>
                </div>
            </div>

            <div id="crsConfirm" class="crs-dialog-backdrop" style="display:none">
                <div class="crs-dialog crs-dialog-sm" role="dialog" aria-labelledby="crsConfirmTitle">
                    <div class="crs-dialog-head">
                        <span class="crs-dialog-ico crs-ico-question" aria-hidden="true">?</span>
                        <span id="crsConfirmTitle">Confirmation</span>
                    </div>
                    <div class="crs-dialog-body">Are you sure you want to proceed?</div>
                    <div class="crs-dialog-foot">
                        <asp:Button ID="BtnConfirm" runat="server" Text="Yes" CssClass="crs-dialog-btn crs-primary"
                            data-guide="crs-confirm-yes" OnClick="BtnConfirm_Click"
                            OnClientClick="if (!crsBeforeSubmit()) return false;" CausesValidation="false" UseSubmitBehavior="false" />
                        <button type="button" class="crs-dialog-btn" onclick="window.crsHide('crsConfirm')"
                            data-guide="crs-confirm-no">No</button>
                    </div>
                </div>
            </div>

            <asp:Panel ID="PnlSuccess" runat="server" Visible="false" CssClass="crs-dialog-backdrop" data-success="true" data-guide="crs-success">
                <div class="crs-dialog crs-dialog-sm" role="dialog" aria-labelledby="crsSuccessTitle">
                    <div class="crs-dialog-head crs-ok">
                        <span class="crs-dialog-ico crs-ico-ok" aria-hidden="true">&#10003;</span>
                        <span id="crsSuccessTitle">Success</span>
                    </div>
                    <div class="crs-dialog-body">Resource setup saved successfully.</div>
                    <div class="crs-dialog-foot">
                        <button type="button" class="crs-dialog-btn crs-primary" onclick="window.crsCloseAfterSuccess()"
                            data-guide="crs-success-ok">OK</button>
                    </div>
                </div>
            </asp:Panel>

            <asp:HiddenField ID="HfAnswer"     runat="server" ClientIDMode="Static" />
            <asp:HiddenField ID="HfToolGroups" runat="server" ClientIDMode="Static" />
        </div>
    </div>
</div>

<script>
// Los onclick llaman por window.: dentro de un manejador inline, un id del formulario con el
// mismo nombre que la función la tapa (crsAssociate era el botón, no la función).
(function () {
    var state = { retrieved: false, associated: false, shopOrder: '' };

    function $(id) { return document.getElementById(id); }

    function toolGroups() {
        var hf = $('HfToolGroups');
        if (!hf || !hf.value) return [];
        try { return JSON.parse(hf.value) || []; } catch (e) { return []; }
    }

    function setEnabled(enabled) {
        $('crsShopOrder').disabled = !enabled;
        $('crsAssociateBtn').disabled = !enabled;
        $('crsSaveBtn').disabled      = !enabled;
    }

    // D-190: "Open" sin opciones hasta Retrieve; después Set Up / Open con Set Up por defecto.
    function setSetupState(retrieved, value) {
        var ddl = $('crsSetupState');
        ddl.innerHTML = '';
        (retrieved ? ['Set Up', 'Open'] : ['Open']).forEach(function (v) {
            var o = document.createElement('option');
            o.value = v; o.textContent = v;
            ddl.appendChild(o);
        });
        ddl.value    = retrieved ? (value || 'Set Up') : 'Open';
        ddl.disabled = !retrieved;
    }

    function cell(text) {
        var td = document.createElement('td');
        var box = document.createElement('span');
        box.className = 'crs-input crs-readonly';
        box.textContent = text || '';
        td.appendChild(box);
        return td;
    }

    // Una fila por Tool Group de la sesión; conserva los Tool Numbers ya escritos (D-192).
    function renderTools(values) {
        var body = $('crsTools');
        var keep = values || currentTools();
        body.innerHTML = '';
        toolGroups().forEach(function (g) {
            var tr = document.createElement('tr');
            tr.setAttribute('data-group', g.Code);

            var td = document.createElement('td');
            var box = document.createElement('span');
            box.className = 'crs-input crs-input-icon';
            var input = document.createElement('input');
            input.type = 'text';
            input.className = 'crs-tool';
            input.maxLength = 40;
            input.autocomplete = 'off';
            input.setAttribute('data-guide', 'crs-tool-number');
            input.value = keep[g.Code] || '';
            var ico = document.createElement('span');
            ico.className = 'crs-field-icon';
            ico.setAttribute('aria-hidden', 'true');
            ico.textContent = '⧉';
            box.appendChild(input); box.appendChild(ico);
            td.appendChild(box);

            tr.appendChild(td);
            tr.appendChild(cell(''));
            tr.appendChild(cell(g.Code));
            tr.appendChild(cell(g.Description));
            body.appendChild(tr);
        });
    }

    function renderNoData() {
        $('crsTools').innerHTML = '<tr class="crs-nodata"><td colspan="4">No data</td></tr>';
    }

    function currentTools() {
        var map = {};
        document.querySelectorAll('#crsTools tr[data-group]').forEach(function (tr) {
            map[tr.getAttribute('data-group')] = tr.querySelector('.crs-tool').value;
        });
        return map;
    }

    function warn(title, text) {
        Swal.fire({ icon: 'warning', title: title, text: text }).then(function () {
            var el = $('modalCustomResourceSetup');
            if (el && !el.classList.contains('show')) showModalSafe('modalCustomResourceSetup');
        });
    }

    window.crsHide = function (id) { $(id).style.display = 'none'; };

    // Enter (el lector de código lo manda al final) no envía el formulario: dispararía el
    // primer botón de la página. Como TxtSFC del POD.
    document.addEventListener('keydown', function (e) {
        if (e.key !== 'Enter') return;
        var t = e.target;
        if (t && t.closest && t.closest('#modalCustomResourceSetup') && t.tagName === 'INPUT') e.preventDefault();
    });

    // Cambiar la ShopOrder después de asociarla deja la orden sin asociar: hay que volver a
    // presionar Associate. Los Tool Numbers escritos se conservan (D-192).
    document.addEventListener('input', function (e) {
        if (e.target && e.target.id === 'crsShopOrder' && state.associated &&
            (e.target.value || '').trim() !== state.shopOrder)
            state.associated = false;
    });

    window.crsRetrieve = function () {
        state.retrieved = true;
        setSetupState(true, 'Set Up');
        setEnabled(true);
    };

    window.crsAssociate = function () {
        var v = ($('crsShopOrder').value || '').trim();
        if (!/^\d+$/.test(v)) {
            warn('ShopOrder inválido', 'El valor de ShopOrder debe ser numérico.');
            return;
        }
        state.associated = true;
        state.shopOrder  = v;
        renderTools();
        $('crsInfo').style.display = 'flex';
    };

    window.crsSave = function () {
        var tools = currentTools();
        var values = Object.keys(tools).map(function (k) { return tools[k]; });
        values.push($('crsShopOrder').value);
        // ASP.NET rechaza el postback con marcado (AUD-11).
        if (values.some(function (v) { return /[<>]/.test(v || ''); })) {
            warn('Caracteres no permitidos', 'Los valores no pueden contener los caracteres < ni >.');
            return;
        }
        // D-201: con la orden asociada, un Tool Number en cada fila, en todos los modos (formato,
        // sin intento). Sin asociar se envía: es una decisión que califica el servidor.
        var toolValues = values.slice(0, -1);
        if (state.associated && (toolValues.length === 0 ||
                toolValues.some(function (v) { return (v || '').trim() === ''; }))) {
            warn('Tool Number requerido', 'Registre el Tool Number de cada herramienta de la tabla.');
            return;
        }
        $('HfAnswer').value = JSON.stringify({
            kind: 'custom',
            setupState: $('crsSetupState').value,
            shopOrderAssociated: state.associated,
            shopOrder: ($('crsShopOrder').value || '').trim(),
            tools: Object.keys(tools).map(function (k) { return { group: k, value: tools[k] }; })
        });
        $('crsConfirm').style.display = 'flex';
    };

    window.crsBeforeSubmit = function () {
        $('crsConfirm').style.display = 'none';
        return !!$('HfAnswer').value;
    };

    // D-192: vuelve al estado inicial.
    window.crsClear = function () {
        state = { retrieved: false, associated: false, shopOrder: '' };
        $('crsShopOrder').value = '';
        $('HfAnswer').value = '';
        setSetupState(false);
        setEnabled(false);
        renderNoData();
    };

    window.crsCloseAfterSuccess = function () {
        var el = $('modalCustomResourceSetup');
        var success = el.querySelector('.crs-dialog-backdrop[data-success]');
        if (success) success.style.display = 'none';
        (bootstrap.Modal.getInstance(el) || new bootstrap.Modal(el)).hide();
    };

    // Tras un postback (intento fallido o éxito) la pantalla vuelve como se envió.
    window.crsRestore = function () {
        var answer;
        try { answer = JSON.parse($('HfAnswer').value || 'null'); } catch (e) { answer = null; }
        if (!answer) { window.crsClear(); return; }
        state = { retrieved: true, associated: !!answer.shopOrderAssociated, shopOrder: answer.shopOrder || '' };
        setSetupState(true, answer.setupState);
        setEnabled(true);
        $('crsShopOrder').value = state.shopOrder;
        if ((answer.tools || []).length > 0) {
            var values = {};
            (answer.tools || []).forEach(function (t) { values[t.group] = t.value; });
            renderTools(values);
        } else {
            renderNoData();
        }
    };
})();
</script>
