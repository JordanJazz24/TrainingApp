<%@ Control Language="C#" AutoEventWireup="true"
    CodeBehind="AssemblyControl.ascx.cs"
    Inherits="TrainingApp.UI.Controls.Training.AssemblyControl" %>

<%-- Component List (reemplaza el POD Work List): BOM de la operación (spec 008).
     Cada fila es un número de parte con su Qty por unidad; Rem Qty pasa a 0 al ensamblarla. --%>
<%-- Distribución de la captura "component list" (Fase 6.5, entrega 7): parte y Qty
     centrados a la izquierda, Description / Rem Qty como etiqueta: valor, botones grandes
     con ícono y paginación centrada con "Rows 1-N of N" debajo. --%>
<div class="pod-list-view">
    <div class="list-header">
        <span>Component List</span>
        <div class="list-header-actions">
            <span class="pod-help-dot" title="Help">?</span>
            <asp:Button ID="BtnCloseList" runat="server" Text="✕"
                CssClass="mes-titlebar-btn" ToolTip="Return to POD"
                OnClick="BtnReturnPOD_Click" CausesValidation="false" />
        </div>
    </div>
    <asp:Repeater ID="RptComponents" runat="server">
        <ItemTemplate>
            <div class="mes-list-row">
                <div class="mes-list-key">
                    <div class="mes-list-code"><%#: Eval("Code") %></div>
                    <div><%#: Eval("Qty") %></div>
                </div>
                <div class="mes-list-details">
                    <span class="mes-list-label">Description:</span><span class="mes-list-text"><%#: Eval("Description") %></span>
                    <span class="mes-list-label">Rem Qty:</span><span><%#: Eval("RemQty") %></span>
                </div>
                <div class="mes-list-actions">
                    <%-- El modal se abre en el cliente con los datos de este componente:
                         el único postback del ensamble es el Add. --%>
                    <button type="button" class="mes-btn mes-btn-stack mes-ico mes-ico-assemble"
                        data-component-id="<%#: Eval("Id") %>"
                        data-part="<%#: Eval("PartNumber") %>"
                        data-rev="<%#: Eval("Revision") %>"
                        data-desc="<%#: Eval("Description") %>"
                        data-qty="<%#: Eval("Qty") %>"
                        onclick="openAssembleModal(this)"
                        <%# (bool)Eval("IsAssembled") ? "disabled" : "" %>>Assemble</button>
                    <%-- El simulador no entrena el desensamble. --%>
                    <button type="button" class="mes-btn mes-btn-stack mes-ico mes-ico-disassemble" disabled>Disassemble</button>
                </div>
            </div>
        </ItemTemplate>
    </asp:Repeater>
    <%-- Todo el BOM entra en una página: la paginación es la del real, deshabilitada. --%>
    <div class="mes-pagination">
        <div class="mes-pagination-buttons">
            <button type="button" class="mes-btn mes-btn-page" disabled>|&laquo; First</button>
            <button type="button" class="mes-btn mes-btn-page" disabled>&laquo; Prev</button>
            <button type="button" class="mes-btn mes-btn-page" disabled>Next &raquo;</button>
            <button type="button" class="mes-btn mes-btn-page" disabled>Last &raquo;|</button>
        </div>
        <span>Rows 1-<asp:Literal ID="LitTotal" runat="server" /> of
            <asp:Literal ID="LitTotalOf" runat="server" /></span>
    </div>
</div>

<%-- Assemble Components modal — réplica del modal real (spec 008).
     Mode y Action son solo información. Component Details, Assembly Data y Add aparecen
     recién al escanear el barcode (Enter, como el lector, o al salir del campo). --%>
<div class="modal fade" id="modalAssemble" tabindex="-1" aria-hidden="true" data-bs-backdrop="static">
    <div class="modal-dialog modal-lg">
        <div class="modal-content">
            <div class="modal-header">
                <h6 class="modal-title">Assemble Components</h6>
                <div class="mes-titlebar-actions">
                    <button type="button" class="mes-titlebar-btn" title="Maximize"
                        onclick="mesToggleMaximize(this)">&#10530;</button>
                    <button type="button" class="mes-titlebar-btn" title="Close"
                        data-bs-dismiss="modal">&#10005;</button>
                </div>
            </div>
            <div class="modal-body p-0">
                <div class="mes-subheader">
                    <span>Assemble Components</span>
                    <span class="mes-help" title="Help">?</span>
                </div>

                <div class="asm-top row g-0">
                    <div class="col-md-6">
                        <div class="asm-field">
                            <span class="asm-label">Mode:</span>
                            <span>Choose</span>
                        </div>
                        <div class="asm-field">
                            <span class="asm-label">Action:</span>
                            <span class="asm-static"><input type="radio" checked disabled />Assembly</span>
                            <span class="asm-static"><input type="radio" disabled />Removal</span>
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="asm-field">
                            <span class="asm-label">Find component:</span>
                            <input type="text" class="form-control form-control-sm" />
                        </div>
                        <div class="asm-field">
                            <span class="asm-label">Barcode:</span>
                            <asp:TextBox ID="TxtBarcode" runat="server" CssClass="form-control form-control-sm"
                                ClientIDMode="Static" autocomplete="off"
                                onkeydown="return asmBarcodeKeyDown(event);"
                                onchange="asmScan();" />
                        </div>
                    </div>
                </div>

                <div id="asmEmpty" class="asm-empty"></div>
                <div id="asmScanned" class="asm-details" style="display:none">
                    <div class="asm-col">
                        <h6>Component Details</h6>
                        <div class="asm-field">
                            <span class="asm-label">Component:</span>
                            <div class="input-group input-group-sm asm-component">
                                <input id="asmCompCode" type="text" class="form-control" readonly />
                                <span class="input-group-text">&#10697;</span>
                            </div>
                        </div>
                        <div class="asm-field">
                            <span class="asm-label">Version:</span>
                            <input id="asmCompVersion" type="text" class="form-control form-control-sm asm-input-sm" readonly />
                        </div>
                        <div class="asm-field">
                            <span class="asm-label">Description:</span>
                            <span id="asmCompDesc"></span>
                        </div>
                        <div class="asm-field">
                            <span class="asm-label">Quantity:</span>
                            <input id="asmCompQty" type="text" class="form-control form-control-sm asm-input-sm" readonly />
                        </div>
                    </div>
                    <div class="asm-col">
                        <h6>Assembly Data</h6>
                        <div class="asm-field">
                            <span class="asm-label"><span class="required-star">★</span> Batch Number:</span>
                            <input id="asmBatch" type="text" class="form-control form-control-sm" readonly />
                        </div>
                    </div>
                </div>
                <asp:HiddenField ID="HfComponentId" runat="server" ClientIDMode="Static" />
                <%-- Process Lot de la orden: es el Batch Number que se carga al escanear. --%>
                <asp:HiddenField ID="HfProcessLot" runat="server" ClientIDMode="Static" />
            </div>
            <div class="modal-footer asm-footer">
                <%-- Cada escaneo se aplica a todas las piezas seleccionadas (spec 008); solo
                     tiene sentido con más de una pieza. Aparece con Add, a su izquierda, como
                     en el MES real (Fase 6.5, entrega 1). --%>
                <asp:Panel ID="PnlApplyAll" runat="server" CssClass="asm-apply-all" style="display:none">
                    <label class="mb-0"><input type="checkbox" checked disabled /> Apply to all SFCs</label>
                </asp:Panel>
                <button id="BtnAdd" runat="server" class="btn btn-sm asm-btn" style="display:none"
                    onclick="if (!assembleValidate()) return false;"
                    onserverclick="BtnAdd_Click">
                    <span class="text-success">&#10004;</span> Add
                </button>
                <button id="BtnReturnPOD" runat="server" class="btn btn-sm asm-btn"
                    onserverclick="BtnReturnPOD_Click">
                    <span class="text-danger">&#10006;</span> Return to POD
                </button>
            </div>
        </div>
    </div>
</div>

<script>
var _asmComponent = null;

function openAssembleModal(btn) {
    document.getElementById('HfComponentId').value = btn.getAttribute('data-component-id');
    _asmComponent = {
        part: btn.getAttribute('data-part'),
        rev:  btn.getAttribute('data-rev'),
        desc: btn.getAttribute('data-desc'),
        qty:  btn.getAttribute('data-qty')
    };
    document.getElementById('TxtBarcode').value = '';
    asmShowScanned(false);
    showModalSafe('modalAssemble');
    setTimeout(function () { document.getElementById('TxtBarcode').focus(); }, 300);
}

// El lector termina el escaneo con Enter: en un form de Web Forms Enter enviaría el
// postback con el botón por defecto, así que se intercepta y se procesa el escaneo.
function asmBarcodeKeyDown(e) {
    if (e.key === 'Enter') {
        e.preventDefault();
        asmScan();
        return false;
    }
    return true;
}

// Formato (sin intento): el barcode es obligatorio y numérico. No se valida contra el
// número de parte (spec 008).
function asmBarcodeIsValid() {
    var v = document.getElementById('TxtBarcode').value.trim();
    return v !== '' && /^\d+$/.test(v);
}

function asmScan() {
    var v = document.getElementById('TxtBarcode').value.trim();
    if (!v) { asmShowScanned(false); return; }
    if (!asmBarcodeIsValid()) {
        asmShowScanned(false);
        Swal.fire({ icon: 'warning', title: 'Barcode inválido',
            text: 'Escanea un barcode numérico.' });
        return;
    }
    // Los datos del componente salen del catálogo (BOM); el Batch Number es el lote al
    // que pertenecen las piezas (Process Lot de la orden), no el barcode escaneado.
    document.getElementById('asmCompCode').value     = _asmComponent ? _asmComponent.part : '';
    document.getElementById('asmCompVersion').value  = _asmComponent ? _asmComponent.rev : '';
    document.getElementById('asmCompDesc').textContent = _asmComponent ? _asmComponent.desc : '';
    document.getElementById('asmCompQty').value      = _asmComponent ? _asmComponent.qty : '';
    document.getElementById('asmBatch').value        = document.getElementById('HfProcessLot').value;
    asmShowScanned(true);
}

function asmShowScanned(scanned) {
    document.getElementById('asmScanned').style.display = scanned ? '' : 'none';
    document.getElementById('asmEmpty').style.display   = scanned ? 'none' : '';
    document.getElementById('<%= BtnAdd.ClientID %>').style.display = scanned ? '' : 'none';
    // Con una sola pieza el panel no se renderiza (PnlApplyAll.Visible = false).
    var applyAll = document.getElementById('<%= PnlApplyAll.ClientID %>');
    if (applyAll) applyAll.style.display = scanned ? '' : 'none';
}

function assembleValidate() {
    if (!asmBarcodeIsValid()) {
        Swal.fire({ icon: 'warning', title: 'Barcode inválido',
            text: 'Escanea un barcode numérico antes de presionar Add.' });
        return false;
    }
    return true;
}
</script>
