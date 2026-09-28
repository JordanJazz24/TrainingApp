<%@ Page Title="MES Training" Language="C#" MasterPageFile="~/UI/master/Training.Master"
    AutoEventWireup="true" CodeBehind="GuidedTest.aspx.cs"
    Inherits="TrainingApp.Pages.Training.GuidedTest" %>

<%@ Register Src="~/UI/Controls/Training/ResourceSetupControl.ascx"  TagPrefix="uc" TagName="ResourceSetupControl" %>
<%@ Register Src="~/UI/Controls/Training/LineClearanceControl.ascx"  TagPrefix="uc" TagName="LineClearanceControl" %>
<%@ Register Src="~/UI/Controls/Training/SearchControl.ascx"         TagPrefix="uc" TagName="SearchControl" %>
<%@ Register Src="~/UI/Controls/Training/SFCSplitControl.ascx"       TagPrefix="uc" TagName="SFCSplitControl" %>
<%@ Register Src="~/UI/Controls/Training/AssemblyControl.ascx"       TagPrefix="uc" TagName="AssemblyControl" %>
<%@ Register Src="~/UI/Controls/Training/DataCollectionControl.ascx" TagPrefix="uc" TagName="DataCollectionControl" %>
<%@ Register Src="~/UI/Controls/Training/LogNCControl.ascx"          TagPrefix="uc" TagName="LogNCControl" %>
<%@ Register Src="~/UI/Controls/Training/CompleteControl.ascx"       TagPrefix="uc" TagName="CompleteControl" %>

<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">

    <%-- ══ HEADER BAR ═══════════════════════════════════════════════════ --%>
    <div class="pod-header">
        <span class="pod-header-title">
            Production Operator Dashboard: Site -
            <asp:Literal ID="LitSite"     runat="server" Text="CR9A" />,
            User - <asp:Literal ID="LitUserName" runat="server" />
        </span>
        <div class="pod-header-controls">
            <button id="BtnLogout" runat="server" title="Logout"
                class="pod-header-logout" onserverclick="BtnLogout_ServerClick">&#9211;</button>
        </div>
    </div>

    <%-- ══ SCORE WIDGET (fixed top-right) ══════════════════════════════ --%>
    <asp:Panel ID="PnlScore" runat="server" CssClass="pod-score-widget">
        <div>Score: <span class="score-value"><asp:Literal ID="LitScore" runat="server" Text="0.0%" /></span></div>
        <div class="mt-1"><asp:Literal ID="LitModeBadge" runat="server" /></div>
    </asp:Panel>

    <%-- ══ BANNER ZONE ══════════════════════════════════════════════════ --%>
    <asp:Panel ID="PnlBannerZone" runat="server" CssClass="pod-banner-zone" Visible="false">
        <div class="pod-banner alert alert-success mb-0">
            <asp:Literal ID="LitBanner" runat="server" />
        </div>
    </asp:Panel>

    <%-- ══ SEARCH SECTION ═══════════════════════════════════════════════ --%>
    <div class="pod-search">
        <div class="d-flex align-items-center gap-3 flex-wrap">
            <span><span class="text-danger">&#9733;</span>
                <asp:Literal ID="LitOperationName" runat="server" /></span>
            <span><span class="text-danger">&#9733;</span>
                <asp:Literal ID="LitResourceName" runat="server" /></span>
            <div class="d-flex align-items-center gap-1">
                <span class="sfc-label">SFC:</span>
                <%-- Spec 005: TxtSFC ya no se edita a mano — la selección es solo vía
                     Browse Work List (confirmBrowseSelection lo sigue completando).
                     readonly se aplica como atributo HTML desde el code-behind: la
                     propiedad ReadOnly haría que ASP.NET descarte el valor posteado. --%>
                <asp:TextBox ID="TxtSFC" runat="server" CssClass="form-control sfc-input"
                    placeholder="Use Browse Work List..." />
                <button type="button" class="btn btn-sm btn-outline-secondary"
                    data-bs-toggle="modal" data-bs-target="#modalBrowseWorkList"
                    title="Browse Work List">&#128269;</button>
            </div>
            <span class="sfc-label ms-2">
                Shop Order: <strong><asp:Literal ID="LitShopOrder" runat="server" Text="—" /></strong>
            </span>
            <span class="sfc-label">
                Status: <strong><asp:Literal ID="LitSFCStatus" runat="server" Text="—" /></strong>
            </span>
        </div>
    </div>

    <%-- ══ ACTION BUTTONS ════════════════════════════════════════════════ --%>
    <div class="pod-actions">
        <button id="BtnStart" runat="server" class="pod-action-btn" disabled="disabled"
            onserverclick="BtnStart_ServerClick">
            <img src="<%=ResolveUrl("~/UI/master/imgs/clock-icon.png")%>" alt="" />
            <span>Start</span>
        </button>
        <button id="BtnAssemble" runat="server" class="pod-action-btn" disabled="disabled"
            onserverclick="BtnAssemble_ServerClick">
            <img src="<%=ResolveUrl("~/UI/master/imgs/ensamblar-icon.png")%>" alt="" />
            <span>Assemble</span>
        </button>
        <button id="BtnDCCollection" runat="server" class="pod-action-btn" disabled="disabled"
            onserverclick="BtnDCCollection_ServerClick" onclick="if (!podValidateDCCollect()) return false;">
            <img src="<%=ResolveUrl("~/UI/master/imgs/dc_collection-icon.png")%>" alt="" />
            <span>DC Collection</span>
        </button>
        <button id="BtnComplete" runat="server" class="pod-action-btn" disabled="disabled"
            onserverclick="BtnComplete_ServerClick">
            <img src="<%=ResolveUrl("~/UI/master/imgs/complete-icon.png")%>" alt="" />
            <span>Complete</span>
        </button>
    </div>

    <%-- ══ SIDEBAR ════════════════════════════════════════════════════════ --%>
    <div class="pod-sidebar">
        <button id="BtnSFCSplit" runat="server" class="pod-sidebar-btn" disabled="disabled"
            onserverclick="BtnSFCSplit_ServerClick" onclick="if (!podValidateSFCSplit()) return false;">
            <img src="<%=ResolveUrl("~/UI/master/imgs/split-icon.png")%>" alt="" />
            <span>SFC Split</span>
        </button>
        <button id="BtnLogNC" runat="server" class="pod-sidebar-btn" disabled="disabled"
            onserverclick="BtnLogNC_ServerClick">
            <img src="<%=ResolveUrl("~/UI/master/imgs/log_NC-Icon.png")%>" alt="" />
            <span>Log NC</span>
        </button>
        <button id="BtnWorkInstructions" runat="server" class="pod-sidebar-btn" disabled="disabled"
            onserverclick="BtnNonStepAction_ServerClick">
            <img src="<%=ResolveUrl("~/UI/master/imgs/intructions-icon.png")%>" alt="" />
            <span>Work Instructions</span>
        </button>
        <button id="BtnReports" runat="server" class="pod-sidebar-btn" disabled="disabled"
            onserverclick="BtnNonStepAction_ServerClick">
            <img src="<%=ResolveUrl("~/UI/master/imgs/informes-icon.png")%>" alt="" />
            <span>Reports</span>
        </button>
        <%-- Activities: Bootstrap dropdown — toggle is client-side, items are server-side buttons --%>
        <div class="dropdown">
            <button type="button" class="pod-sidebar-btn dropdown-toggle"
                    id="BtnActivitiesToggle" disabled
                    data-bs-toggle="dropdown" data-bs-auto-close="true">
                <img src="<%=ResolveUrl("~/UI/master/imgs/activities-icon.png")%>" alt="" />
                <span>Activities</span>
            </button>
            <ul class="dropdown-menu" id="activitiesMenu">
                <li>
                    <button id="BtnActivities" runat="server" class="dropdown-item"
                        disabled="disabled" onserverclick="BtnActivities_ServerClick">
                        Data Collection Standalone
                    </button>
                </li>
                <li><hr class="dropdown-divider" /></li>
                <li><button id="BtnAsBuiltConfig" runat="server" class="dropdown-item" disabled="disabled"
                    onserverclick="BtnNonStepAction_ServerClick">As-Built Configuration</button></li>
                <li><button id="BtnLogComment" runat="server" class="dropdown-item" disabled="disabled"
                    onserverclick="BtnNonStepAction_ServerClick">Log Comment</button></li>
                <li><button id="BtnSfcPlaceHold" runat="server" class="dropdown-item" disabled="disabled"
                    onserverclick="BtnNonStepAction_ServerClick">SFC Place Hold</button></li>
                <li><button id="BtnSfcReleaseHold" runat="server" class="dropdown-item" disabled="disabled"
                    onserverclick="BtnNonStepAction_ServerClick">SFC Release Hold</button></li>
                <li><button id="BtnRemoveFutureHold" runat="server" class="dropdown-item" disabled="disabled"
                    onserverclick="BtnNonStepAction_ServerClick">Remove Future Hold</button></li>
                <li><button id="BtnChangeEquipmentStatus" runat="server" class="dropdown-item" disabled="disabled"
                    onserverclick="BtnNonStepAction_ServerClick">Change Equipment Status Tool</button></li>
                <li><button id="BtnTimeSensitiveMaterial" runat="server" class="dropdown-item" disabled="disabled"
                    onserverclick="BtnNonStepAction_ServerClick">Time Sensitive Material Check In/Out</button></li>
            </ul>
        </div>
        <button id="BtnPlaceBackInQueue" runat="server" class="pod-sidebar-btn" disabled="disabled"
            onserverclick="BtnNonStepAction_ServerClick">
            <img src="<%=ResolveUrl("~/UI/master/imgs/play_back-icon.png")%>" alt="" />
            <span>Place Back in Queue</span>
        </button>
    </div>

    <%-- ══ MAIN CONTENT ══════════════════════════════════════════════════ --%>
    <div class="pod-main">

        <%-- POD Work List --%>
        <asp:Panel ID="PnlWorkList" runat="server">
            <table class="pod-work-list table table-bordered mb-0">
                <thead>
                    <tr>
                        <th>CP</th>
                        <th>Material Description</th>
                        <th>Qty</th>
                        <th>Status</th>
                        <th>Process Lot</th>
                        <th>Shop Order</th>
                    </tr>
                </thead>
                <tbody>
                    <asp:Repeater ID="RptWorkList" runat="server">
                        <ItemTemplate>
                            <tr class="<%# (bool)Eval("IsSelected") ? "selected" : "" %>"
                                data-sfc="<%# Eval("SFCCode") %>"
                                data-row-id="<%# Eval("RowId") %>"
                                onclick="selectWorkListRow(this)">
                                <td><%# Eval("SFCCode") %></td>
                                <td><%# Eval("MaterialDescription") %></td>
                                <td><%# Eval("Qty") %></td>
                                <td>
                                    <span class="status-badge status-<%# ((string)Eval("Status") == "Active" ? "active" : "default") %>"></span>
                                    <%# Eval("Status") %>
                                </td>
                                <td><%# Eval("ProcessLot") %></td>
                                <td><%# Eval("ShopOrder") %></td>
                            </tr>
                        </ItemTemplate>
                    </asp:Repeater>
                </tbody>
            </table>
        </asp:Panel>

    </div>

    <%-- ══ ACTIVE CONTROL AREA (outside pod-main so modals aren't clipped) ═ --%>
    <%-- Controles que reemplazan el POD Work List (Assembly, DC, LogNC).       --%>
    <%-- Spec 006: declarados estáticos — el code-behind solo alterna Visible.  --%>
    <%-- Los eventos custom se llaman OnStepSubmit/OnSplitRequested, por eso    --%>
    <%-- el atributo declarativo lleva el prefijo "On" dos veces.              --%>
    <asp:Panel ID="PnlActiveControl" runat="server" Visible="false" CssClass="pod-active-control">
        <uc:AssemblyControl       ID="CtrlAssembly"       runat="server" Visible="false" OnOnStepSubmit="HandleControlSubmit"
                                  OnOnComponentAssembled="HandleComponentAssembled"
                                  OnOnReturnToPod="HandleReturnToPod" />
        <uc:DataCollectionControl ID="CtrlDataCollection" runat="server" Visible="false" OnOnStepSubmit="HandleControlSubmit"
                                  OnOnGroupSubmit="HandleGroupSubmit"
                                  OnOnReturnToPod="HandleReturnToPod" />
        <uc:LogNCControl          ID="CtrlLogNC"          runat="server" Visible="false" OnOnStepSubmit="HandleControlSubmit"
                                  OnOnReturnToPod="HandleReturnToPod" />
        <uc:CompleteControl       ID="CtrlComplete"       runat="server" Visible="false" OnOnStepSubmit="HandleControlSubmit" />
    </asp:Panel>

    <%-- ══ COMPLETION PANEL ══════════════════════════════════════════════ --%>
    <asp:Panel ID="PnlCompleted" runat="server" Visible="false" CssClass="pod-completion">
        <div class="pod-completion-card">
            <h3 class="mb-4">Session Complete</h3>
            <div class="mb-3 fs-4">
                Score: <strong><asp:Literal ID="LitFinalScore" runat="server" /></strong>
            </div>
            <div class="mb-2">
                Mode: <asp:Literal ID="LitFinalMode" runat="server" />
            </div>
            <div class="mb-2">
                Area: <asp:Literal ID="LitFinalArea" runat="server" />
                &nbsp;|&nbsp;
                Product: <asp:Literal ID="LitFinalProduct" runat="server" />
            </div>
            <asp:Panel ID="PnlFinalResult" runat="server" CssClass="mb-4">
                Result: <strong><asp:Literal ID="LitFinalResult" runat="server" /></strong>
            </asp:Panel>
            <asp:HyperLink ID="LnkNewSession" runat="server"
                NavigateUrl="~/Pages/Training/StartSession.aspx"
                CssClass="btn btn-primary">
                Start New Session
            </asp:HyperLink>
        </div>
    </asp:Panel>

    <%-- ══ FOOTER ════════════════════════════════════════════════════════ --%>
    <div class="pod-footer">
        <button id="BtnResourceSetup" runat="server" class="pod-footer-btn" disabled="disabled"
            onserverclick="BtnResourceSetup_ServerClick">
            <img src="<%=ResolveUrl("~/UI/master/imgs/resource-setup-icon.png")%>" alt="" />
            <span>Resource Setup</span>
        </button>
        <button id="BtnToolList" runat="server" class="pod-footer-btn" disabled="disabled"
            onserverclick="BtnNonStepAction_ServerClick">
            <img src="<%=ResolveUrl("~/UI/master/imgs/tools-icon.png")%>" alt="" />
            <span>Tool List</span>
        </button>
    </div>

    <%-- ══ ABANDON (fixed bottom-left) ══════════════════════════════════ --%>
    <div class="pod-abandon">
        <button id="BtnAbandon" runat="server" class="btn btn-sm btn-outline-danger"
            onserverclick="BtnAbandon_Click" onclick="return confirmAbandon();">
            Abandon Session
        </button>
    </div>

    <%-- ══ BROWSE WORK LIST MODAL ════════════════════════════════════════ --%>
    <div class="modal fade" id="modalBrowseWorkList" tabindex="-1" aria-hidden="true">
        <div class="modal-dialog modal-lg">
            <div class="modal-content">
                <div class="modal-header">
                    <h6 class="modal-title">
                        BROWSE WORK LIST - SFCS QUEUED -
                        <asp:Literal ID="LitBrowseContext" runat="server" />
                    </h6>
                    <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                </div>
                <div class="modal-body p-0">
                    <table class="table table-sm table-hover mb-0">
                        <thead class="table-dark">
                            <tr>
                                <th>SFC</th>
                                <th>Material Description</th>
                                <th>Shop Order Schedule Start</th>
                                <th>Shop Order Planned Completion</th>
                            </tr>
                        </thead>
                        <tbody id="browseWorkListBody">
                            <asp:Repeater ID="RptBrowseList" runat="server">
                                <ItemTemplate>
                                    <tr class="browse-row" data-sfc="<%# Eval("SFCCode") %>"
                                        data-row-id="<%# Eval("RowId") %>"
                                        onclick="selectBrowseRow(this)" style="cursor:pointer">
                                        <td><%# Eval("SFCCode") %></td>
                                        <td><%# Eval("MaterialDescription") %></td>
                                        <td><%# Eval("ScheduleStart") %></td>
                                        <td><%# Eval("PlannedCompletion") %></td>
                                    </tr>
                                </ItemTemplate>
                            </asp:Repeater>
                        </tbody>
                    </table>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-sm btn-success" id="btnBrowseOK"
                        onclick="confirmBrowseSelection()">&#10003; OK</button>
                    <button type="button" class="btn btn-sm btn-secondary"
                        data-bs-dismiss="modal">&#10007; Cancel</button>
                </div>
            </div>
        </div>
    </div>

    <asp:HiddenField ID="HfSelectedSFC" runat="server" />
    <%-- Filas seleccionadas en el POD, ids separados por coma (spec 008). El RowId distingue
         filas hermanas con el mismo SFC tras un SFC Split (spec 005). --%>
    <asp:HiddenField ID="HfSelectedRowIds" runat="server" />
    <%-- Piezas elegidas en Browse Work List para el Start (spec 008). --%>
    <asp:HiddenField ID="HfBrowseRowIds" runat="server" />
    <%-- Host for modal-overlay UserControls (ResourceSetup, LineClearance, Search,  --%>
    <%-- SFC Split). No positioning — modal HTML renders here then showModalSafe     --%>
    <%-- moves it to document.forms[0] for proper Bootstrap stacking.                --%>
    <asp:Panel ID="PnlModalHost" runat="server" Visible="false">
        <uc:ResourceSetupControl ID="CtrlResourceSetup" runat="server" Visible="false" OnOnStepSubmit="HandleControlSubmit" />
        <uc:LineClearanceControl ID="CtrlLineClearance" runat="server" Visible="false" OnOnStepSubmit="HandleControlSubmit" />
        <uc:SearchControl        ID="CtrlSearch"        runat="server" Visible="false" OnOnStepSubmit="HandleControlSubmit" />
        <uc:SFCSplitControl      ID="CtrlSFCSplit"      runat="server" Visible="false" OnOnStepSubmit="HandleControlSubmit"
                                 OnOnSplitRequested="HandleSplitRequested" />
    </asp:Panel>

</asp:Content>

<asp:Content ID="ScriptContent" ContentPlaceHolderID="ScriptContent" runat="server">
<script>
    // F5 después de un postback reenviaría el último POST y volvería a ejecutar la
    // acción (split duplicado, intento fallido extra, control reabierto). Reemplazar
    // la entrada del historial hace que F5 sea un GET de esta misma URL (?sessionId=).
    if (window.history.replaceState) {
        window.history.replaceState(null, '', window.location.href);
    }

    // ── Work list row selection ──────────────────────────────────────
    // Serialized (spec 008): cada click alterna la fila y TxtSFC muestra el SFC o
    // "[N SELECTED]". NonSerialized: una sola fila; click sobre la seleccionada la deselecciona.
    var podMultiSelect = <%= PodMultiSelect ? "true" : "false" %>;

    function describeSelection(sfcs) {
        if (sfcs.length === 0) return '';
        return sfcs.length === 1 ? sfcs[0] : '[' + sfcs.length + ' SELECTED]';
    }

    function syncWorkListSelection() {
        var rows  = document.querySelectorAll('.pod-work-list tbody tr.selected');
        var ids   = Array.prototype.map.call(rows, r => r.getAttribute('data-row-id'));
        var sfcs  = Array.prototype.map.call(rows, r => r.getAttribute('data-sfc'));
        var hf    = document.getElementById('<%= HfSelectedSFC.ClientID %>');
        var hfRow = document.getElementById('<%= HfSelectedRowIds.ClientID %>');
        if (hfRow) hfRow.value = ids.join(',');
        if (hf) hf.value = sfcs.length === 1 ? sfcs[0] : '';
        if (podMultiSelect) {
            var txt = document.getElementById('<%= TxtSFC.ClientID %>');
            if (txt) txt.value = describeSelection(sfcs);
        }
    }

    function selectWorkListRow(row) {
        if (podMultiSelect) {
            row.classList.toggle('selected');
        } else if (row.classList.contains('selected')) {
            row.classList.remove('selected');
        } else {
            document.querySelectorAll('.pod-work-list tbody tr')
                .forEach(r => r.classList.remove('selected'));
            row.classList.add('selected');
        }
        syncWorkListSelection();
    }

    // El HTML del POD que vuelve de un postback no sabe qué filas estaban seleccionadas:
    // se reaplican desde HfSelectedRowIds. Las que ya no existen salen de la selección.
    // Si el POD no está en pantalla (lo reemplaza DC o NC Selection) no se toca nada:
    // el servidor todavía necesita la selección (ej. para el scrap de Log NC).
    function restoreWorkListSelection() {
        var hfRow = document.getElementById('<%= HfSelectedRowIds.ClientID %>');
        if (!hfRow || !hfRow.value) return;
        if (!document.querySelector('.pod-work-list')) return;

        var ids = hfRow.value.split(',');
        var found = [];
        document.querySelectorAll('.pod-work-list tbody tr').forEach(function (r) {
            var match = ids.indexOf(r.getAttribute('data-row-id')) >= 0;
            r.classList.toggle('selected', match);
            if (match) found.push(r.getAttribute('data-row-id'));
        });

        if (found.length !== ids.length) {
            hfRow.value = found.join(',');
            if (found.length !== 1) {
                var hf = document.getElementById('<%= HfSelectedSFC.ClientID %>');
                if (hf) hf.value = '';
            }
        }
    }
    document.addEventListener('DOMContentLoaded', restoreWorkListSelection);

    // Spec 005: DC Collect no puede iniciar sin una fila del POD seleccionada
    // (gating 100% cliente — sin postback si no hay selección).
    function podValidateDCCollect() {
        var hfRow = document.getElementById('<%= HfSelectedRowIds.ClientID %>');
        if (!hfRow || !hfRow.value) {
            Swal.fire({ icon: 'warning', title: 'Selecciona una fila',
                text: 'Debes seleccionar una fila de la orden en la tabla antes de iniciar Data Collection.' });
            return false;
        }
        return true;
    }

    // SFC Split requiere una selección explícita en cada división.
    function podValidateSFCSplit() {
        var hfRow = document.getElementById('<%= HfSelectedRowIds.ClientID %>');
        if (!hfRow || !hfRow.value) {
            Swal.fire({ icon: 'warning', title: 'Selecciona una fila para dividir',
                text: 'Debes seleccionar una fila válida de la orden antes de iniciar SFC Split.' });
            return false;
        }
        return true;
    }

    // ── Browse Work List selection ───────────────────────────────────
    // Serialized (spec 008): cada click alterna la pieza. NonSerialized: una sola fila.
    function selectBrowseRow(row) {
        if (podMultiSelect) {
            row.classList.toggle('table-primary');
            return;
        }
        document.querySelectorAll('#browseWorkListBody tr')
            .forEach(r => r.classList.remove('table-primary'));
        row.classList.add('table-primary');
    }

    // Browse elige las piezas (o el SFC del lote) para Search/Start, no filas del POD:
    // HfSelectedRowIds solo se llena con selectWorkListRow().
    function confirmBrowseSelection() {
        var rows = document.querySelectorAll('#browseWorkListBody tr.table-primary');
        if (rows.length === 0 && !podMultiSelect) return;

        var ids  = Array.prototype.map.call(rows, r => r.getAttribute('data-row-id'));
        var sfcs = Array.prototype.map.call(rows, r => r.getAttribute('data-sfc'));
        var txt  = document.getElementById('<%= TxtSFC.ClientID %>');
        if (txt) txt.value = describeSelection(sfcs);
        var hf   = document.getElementById('<%= HfSelectedSFC.ClientID %>');
        if (hf) hf.value = sfcs.length === 1 ? sfcs[0] : '';
        var hfBrowse = document.getElementById('<%= HfBrowseRowIds.ClientID %>');
        if (hfBrowse) hfBrowse.value = ids.join(',');

        bootstrap.Modal.getInstance(
            document.getElementById('modalBrowseWorkList')).hide();
    }

    // ── Abandon confirmation ─────────────────────────────────────────
    function confirmAbandon() {
        Swal.fire({
            title: 'Abandon Session?',
            text:  'Progress will be lost.',
            icon:  'warning',
            showCancelButton:    true,
            confirmButtonText:   'Yes, abandon',
            cancelButtonText:    'Cancel',
            confirmButtonColor:  '#dc3545'
        }).then(function(r) {
            if (r.isConfirmed) {
                __doPostBack('<%= BtnAbandon.UniqueID %>', '');
            }
        });
        return false;
    }
</script>
</asp:Content>
