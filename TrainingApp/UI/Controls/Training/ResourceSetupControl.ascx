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

                <%-- Record Change banner (shown after save) --%>
                <asp:Panel ID="PnlRecordChange" runat="server" Visible="false"
                    CssClass="record-change-banner">
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

                <%-- Toolbar --%>
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

                <%-- Rows table --%>
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

                <%-- Hidden field to pass rows to server --%>
                <asp:HiddenField ID="HfRows" runat="server" ClientIDMode="Static" />
                <asp:HiddenField ID="HfSetupState" runat="server" ClientIDMode="Static" />
            </div>
            <div class="modal-footer">
                <asp:Button ID="BtnSave" runat="server" Text="Save"
                    CssClass="btn btn-sm btn-success"
                    OnClick="BtnSave_Click"
                    OnClientClick="rsPrepareSubmit()" />
                <asp:Button ID="BtnClear" runat="server" Text="Clear"
                    CssClass="btn btn-sm btn-outline-secondary"
                    OnClick="BtnClear_Click" CausesValidation="false" />
            </div>
        </div>
    </div>
</div>

<script>
(function() {
    var _retrieved = false;

    window.rsRetrieve = function() {
        if (_retrieved) return;
        _retrieved = true;
        document.getElementById('spanSetupStateOpen').style.display = 'none';
        var ddl = document.getElementById('DdlSetupState');
        ddl.style.display = '';
        var toolbar = document.getElementById('rsToolbar');
        toolbar.style.removeProperty('display');
        document.getElementById('rsTableWrapper').style.display = '';
    };

    window.rsInsertNew = function() {
        addRow('Shop Order', '');
    };
    window.rsInsertBefore = function() {
        var sel = document.querySelector('#rsTableBody tr.table-primary');
        var idx = sel ? Array.from(document.querySelectorAll('#rsTableBody tr')).indexOf(sel) : -1;
        addRow('Shop Order', '', idx);
    };
    window.rsInsertAfter = function() {
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

    function addRow(type, value, atIndex) {
        var tbody = document.getElementById('rsTableBody');
        var tr = document.createElement('tr');
        tr.onclick = function() {
            document.querySelectorAll('#rsTableBody tr').forEach(r => r.classList.remove('table-primary'));
            tr.classList.add('table-primary');
        };
        tr.innerHTML =
            '<td><select class="form-select form-select-sm rs-type">' +
                '<option value="Shop Order">Shop Order</option>' +
                '<option value="Tool Number">Tool Number</option>' +
            '</select></td>' +
            '<td><input type="text" class="form-control form-control-sm rs-value" value="' + value + '" /></td>' +
            '<td><button type="button" class="btn btn-sm btn-link text-danger p-0" onclick="this.closest(\'tr\').remove()">&#128465;</button></td>';
        tr.querySelector('.rs-type').value = type;
        var rows = tbody.querySelectorAll('tr');
        if (atIndex !== undefined && atIndex >= 0 && atIndex < rows.length)
            tbody.insertBefore(tr, rows[atIndex]);
        else
            tbody.appendChild(tr);
    }

    window.rsPrepareSubmit = function() {
        var rows = [];
        document.querySelectorAll('#rsTableBody tr').forEach(function(tr) {
            rows.push({
                type:  tr.querySelector('.rs-type').value,
                value: tr.querySelector('.rs-value').value
            });
        });
        document.getElementById('HfRows').value = JSON.stringify(rows);
        var ddl = document.getElementById('DdlSetupState');
        document.getElementById('HfSetupState').value = ddl ? ddl.value : 'Open';
        return true;
    };
})();
</script>
