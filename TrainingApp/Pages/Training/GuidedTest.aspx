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

<%-- Guía visual de Guided (spec 010): el motor solo se incluye en Guided (AC-2). Va en el
     <head> porque PodGuide.start se llama desde un script de arranque del form. --%>
<asp:Content ID="HeadContent" ContentPlaceHolderID="HeadContent" runat="server">
    <asp:PlaceHolder ID="PhGuideAssets" runat="server" Visible="false">
        <link href="<%=ResolveUrl("~/UI/master/pod-guide.css")%>" rel="stylesheet" />
        <script src="<%=ResolveUrl("~/UI/master/pod-guide.js")%>"></script>
    </asp:PlaceHolder>
</asp:Content>

<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">

    <%-- Layout del POD real (Fase 6.5, entrega 3): bloques en el flujo del documento
         — header, banner, sección gris (búsqueda + acciones + panel derecho), POD Work List
         y pie —, sin posiciones fijas apiladas. Así nada se solapa ni tapa los modales. --%>
    <div class="pod-shell">

    <%-- ══ HEADER ═══════════════════════════════════════════════════════ --%>
    <div class="pod-header">
        <span class="pod-header-title">
            Production Operator Dashboard: Site -
            <asp:Literal ID="LitSite"     runat="server" Text="CR9A" />,
            User - <asp:Literal ID="LitUserName" runat="server" />
        </span>
        <div class="pod-header-controls">
            <%-- Score del simulador (no existe en el MES real): compacto, antes de los íconos. --%>
            <asp:Panel ID="PnlScore" runat="server" CssClass="pod-score">
                Score: <span class="score-value"><asp:Literal ID="LitScore" runat="server" Text="0.0%" /></span>
                <asp:Literal ID="LitModeBadge" runat="server" />
            </asp:Panel>
            <%-- Home y Help: solo visuales, como en el MES real. No hacen nada ni cuentan
                 como intento (navegación, igual que Logout). --%>
            <button type="button" class="pod-header-icon" title="Home" aria-label="Home">
                <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                    stroke-width="1.6" stroke-linejoin="round" aria-hidden="true">
                    <path d="M3 11.5 12 4l9 7.5" /><path d="M5.5 10v10h5v-6h3v6h5V10" />
                </svg>
            </button>
            <button id="BtnLogout" runat="server" title="Logout" aria-label="Logout"
                class="pod-header-icon" onserverclick="BtnLogout_ServerClick" data-guide-free="">
                <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                    stroke-width="1.8" stroke-linecap="round" aria-hidden="true">
                    <path d="M12 3v8" /><path d="M6.3 6.8a8 8 0 1 0 11.4 0" />
                </svg>
            </button>
            <button type="button" class="pod-header-icon" title="Help" aria-label="Help">
                <svg width="22" height="22" viewBox="0 0 24 24" aria-hidden="true">
                    <circle cx="12" cy="12" r="10" class="pod-help-circle" />
                    <text x="12" y="16.5" text-anchor="middle" font-size="13" font-weight="700"
                        font-family="Arial, sans-serif" fill="#fff">?</text>
                </svg>
            </button>
        </div>
    </div>

    <%-- ══ BANNER ═══════════════════════════════════════════════════════ --%>
    <%-- Mensaje del MES entre el header y el POD Search; se oculta solo (ShowBanner). --%>
    <asp:Panel ID="PnlBannerZone" runat="server" CssClass="pod-banner" Visible="false" data-guide="pod-banner">
        <svg width="24" height="24" viewBox="0 0 26 26" aria-hidden="true">
            <circle cx="13" cy="13" r="12" fill="#74ab6d" />
            <path d="M7.5 13.5l3.8 3.8 7.2-8" fill="none" stroke="#fff" stroke-width="2.6"
                stroke-linecap="round" stroke-linejoin="round" />
        </svg>
        <span><asp:Literal ID="LitBanner" runat="server" /></span>
    </asp:Panel>

    <%-- ══ POD SEARCH (inicio de la sección gris) ═══════════════════════ --%>
    <div class="pod-search">
        <span class="pod-search-link"><span class="pod-star">*</span>
            <asp:Literal ID="LitOperationName" runat="server" /></span>
        <span class="pod-search-link"><span class="pod-star">*</span>
            <asp:Literal ID="LitResourceName" runat="server" /></span>
        <span class="pod-search-sfc">
            <span>SFC:</span>
            <%-- Spec 011: hasta el Start se escribe o se escanea la Shop Order o un SFC de
                 la orden y Enter la carga (podLoadScan); Browse Work List también vale.
                 Después del Start es de solo lectura (ApplySfcFieldMode). Sin placeholder,
                 como el MES real: en Assessment nada dice cómo cargar la orden. --%>
            <span class="pod-sfc-box">
                <asp:TextBox ID="TxtSFC" runat="server" CssClass="pod-sfc-input"
                    autocomplete="off" data-guide="pod-sfc" />
                <button type="button" class="pod-sfc-browse" data-guide="pod-browse"
                    data-bs-toggle="modal" data-bs-target="#modalBrowseWorkList"
                    title="Browse Work List">&#10697;</button>
            </span>
        </span>
        <%-- Orden cargada: la elegida en Browse (In Queue) o la iniciada (Active). El
             servidor los recalcula en cada respuesta (BindSearchHeader). --%>
        <span class="pod-search-order">
            <span>Shop Order: <strong id="podShopOrder"><asp:Literal ID="LitShopOrder" runat="server" /></strong></span>
            <span>Status (SFC): <strong id="podSfcStatus"><asp:Literal ID="LitSFCStatus" runat="server" /></strong></span>
        </span>
    </div>

    <%-- ══ ACTION BUTTONS ════════════════════════════════════════════════ --%>
    <div class="pod-actions">
        <button id="BtnStart" runat="server" class="pod-action-btn" disabled="disabled"
            onserverclick="BtnStart_ServerClick" data-guide="pod-start">
            <img src="<%=ResolveUrl("~/UI/master/imgs/clock-icon.png")%>" alt="" />
            <span>Start</span>
        </button>
        <button id="BtnAssemble" runat="server" class="pod-action-btn" disabled="disabled"
            onserverclick="BtnAssemble_ServerClick" data-guide="pod-assemble">
            <img src="<%=ResolveUrl("~/UI/master/imgs/ensamblar-icon.png")%>" alt="" />
            <span>Assemble</span>
        </button>
        <button id="BtnDCCollection" runat="server" class="pod-action-btn" disabled="disabled"
            onserverclick="BtnDCCollection_ServerClick" onclick="if (!podValidateDCCollect()) return false;"
            data-guide="pod-dc-collect">
            <img src="<%=ResolveUrl("~/UI/master/imgs/dc_collection-icon.png")%>" alt="" />
            <span>DC Collect</span>
        </button>
        <button id="BtnComplete" runat="server" class="pod-action-btn" disabled="disabled"
            onserverclick="BtnComplete_ServerClick" data-guide="pod-complete">
            <img src="<%=ResolveUrl("~/UI/master/imgs/complete-icon.png")%>" alt="" />
            <span>Complete</span>
        </button>
    </div>

    <div class="pod-body">

        <%-- ══ MAIN CONTENT ══════════════════════════════════════════════ --%>
        <div class="pod-main">

            <%-- POD Work List --%>
            <asp:Panel ID="PnlWorkList" runat="server" CssClass="pod-panel">
                <div class="pod-panel-header">
                    <span>POD Work List</span>
                    <span class="pod-help-dot" title="Help">?</span>
                </div>
                <div class="pod-panel-body">
                    <table class="pod-work-list table mb-0">
                        <thead>
                            <tr>
                                <%-- Columna selectora con el ícono Select All, como la tabla del POD real. --%>
                                <th class="row-selector-col">
                                    <button type="button" class="select-all-btn" title="Select All"
                                        onclick="toggleSelectAllWorkList()" data-guide="pod-select-all"><%= SelectAllIcon %></button>
                                </th>
                                <th>SFC</th>
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
                                    <tr class="<%# (bool)Eval("IsSelected") ? "selected" : "" %>" data-guide="pod-row"
                                        data-sfc="<%#: Eval("SFCCode") %>"
                                        data-row-id="<%#: Eval("RowId") %>"
                                        onclick="selectWorkListRow(this)">
                                        <td class="row-selector-col"></td>
                                        <td><%#: Eval("SFCCode") %></td>
                                        <td><%#: Eval("MaterialDescription") %></td>
                                        <td><%#: Eval("Qty") %></td>
                                        <td>
                                            <span class="status-badge status-<%# ((string)Eval("Status") == "Active" ? "active" : "default") %>"
                                                title="<%#: Eval("Status") %>"></span>
                                        </td>
                                        <td><%#: Eval("ProcessLot") %></td>
                                        <td><%#: Eval("ShopOrder") %></td>
                                    </tr>
                                </ItemTemplate>
                            </asp:Repeater>
                        </tbody>
                    </table>
                </div>
            </asp:Panel>

            <%-- ══ ACTIVE CONTROL AREA ═══════════════════════════════════ --%>
            <%-- Controles que reemplazan el POD Work List (Assembly, DC, LogNC).       --%>
            <%-- Spec 006: declarados estáticos — el code-behind solo alterna Visible.  --%>
            <%-- Los eventos custom se llaman OnStepSubmit/OnSplitRequested, por eso    --%>
            <%-- el atributo declarativo lleva el prefijo "On" dos veces.              --%>
            <asp:Panel ID="PnlActiveControl" runat="server" Visible="false" CssClass="pod-active-control">
                <uc:AssemblyControl       ID="CtrlAssembly"       runat="server" Visible="false" OnOnStepSubmit="HandleControlSubmit"
                                          OnOnComponentAssembled="HandleComponentAssembled"
                                          OnOnReturnToPod="HandleReturnToPod" />
                <uc:DataCollectionControl ID="CtrlDataCollection" runat="server" Visible="false" OnOnStepSubmit="HandleControlSubmit"
                                          OnOnGroupSubmit="HandleGroupSubmit" OnOnGroupOpened="HandleGroupOpened"
                                          OnOnReturnToPod="HandleReturnToPod" />
                <uc:LogNCControl          ID="CtrlLogNC"          runat="server" Visible="false" OnOnStepSubmit="HandleControlSubmit"
                                          OnOnCodeSelected="HandleNCCodeSelected"
                                          OnOnReturnToPod="HandleReturnToPod" />
                <uc:CompleteControl       ID="CtrlComplete"       runat="server" Visible="false" OnOnStepSubmit="HandleControlSubmit" />
            </asp:Panel>
        </div>

        <%-- ══ PANEL DERECHO ═══════════════════════════════════════════ --%>
        <div class="pod-sidebar">
            <button id="BtnSFCSplit" runat="server" class="pod-sidebar-btn" disabled="disabled"
                onserverclick="BtnSFCSplit_ServerClick" onclick="if (!podValidateSFCSplit()) return false;"
                data-guide="pod-sfc-split">
                <img src="<%=ResolveUrl("~/UI/master/imgs/split-icon.png")%>" alt="" />
                <span>SFC Split</span>
            </button>
            <button id="BtnLogNC" runat="server" class="pod-sidebar-btn" disabled="disabled"
                onserverclick="BtnLogNC_ServerClick" data-guide="pod-lognc">
                <img src="<%=ResolveUrl("~/UI/master/imgs/log_NC-Icon.png")%>" alt="" />
                <span>Log NC</span>
            </button>
            <button id="BtnWorkInstructions" runat="server" class="pod-sidebar-btn" disabled="disabled"
                onserverclick="BtnNonStepAction_ServerClick">
                <img src="<%=ResolveUrl("~/UI/master/imgs/intructions-icon.png")%>" alt="" />
                <span>Work Instruction</span>
            </button>
            <button id="BtnReports" runat="server" class="pod-sidebar-btn has-menu" disabled="disabled"
                onserverclick="BtnNonStepAction_ServerClick">
                <img src="<%=ResolveUrl("~/UI/master/imgs/informes-icon.png")%>" alt="" />
                <span>Reports...</span>
            </button>
            <%-- Activities: dropdown de Bootstrap — el toggle es de cliente, los ítems son
                 botones de servidor. Orden del MES real (Data Collection Standalone al final).
                 Popper con strategy fixed: el menú no queda recortado por el panel. --%>
            <div class="dropdown">
                <button type="button" class="pod-sidebar-btn has-menu"
                        id="BtnActivitiesToggle" disabled data-guide="pod-activities"
                        data-bs-toggle="dropdown" data-bs-auto-close="true"
                        data-bs-popper-config='{"strategy":"fixed"}'>
                    <img src="<%=ResolveUrl("~/UI/master/imgs/activities-icon.png")%>" alt="" />
                    <span>Activities...</span>
                </button>
                <ul class="dropdown-menu dropdown-menu-end pod-activities-menu" id="activitiesMenu">
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
                    <li>
                        <button id="BtnActivities" runat="server" class="dropdown-item"
                            disabled="disabled" onserverclick="BtnActivities_ServerClick" data-guide="pod-dc-standalone">
                            Data Collection Standalone
                        </button>
                    </li>
                </ul>
            </div>
            <button id="BtnPlaceBackInQueue" runat="server" class="pod-sidebar-btn" disabled="disabled"
                onserverclick="BtnNonStepAction_ServerClick">
                <img src="<%=ResolveUrl("~/UI/master/imgs/play_back-icon.png")%>" alt="" />
                <span>Place back in queue</span>
            </button>
        </div>
    </div>

    <%-- ══ FOOTER ════════════════════════════════════════════════════════ --%>
    <%-- Abandon es del simulador (no existe en el MES real): a la izquierda del pie. --%>
    <div class="pod-footer">
        <div class="pod-footer-side">
            <button id="BtnAbandon" runat="server" class="btn btn-sm btn-outline-danger pod-abandon-btn"
                onserverclick="BtnAbandon_Click" onclick="return confirmAbandon();" data-guide-free="">
                Abandon Session
            </button>
        </div>
        <div class="pod-footer-buttons">
            <button id="BtnResourceSetup" runat="server" class="pod-footer-btn" disabled="disabled"
                onserverclick="BtnResourceSetup_ServerClick" data-guide="pod-resource-setup">
                <img src="<%=ResolveUrl("~/UI/master/imgs/resource-setup-icon.png")%>" alt="" />
                <span>Resource Setup</span>
            </button>
            <button id="BtnToolList" runat="server" class="pod-footer-btn" disabled="disabled"
                onserverclick="BtnNonStepAction_ServerClick">
                <img src="<%=ResolveUrl("~/UI/master/imgs/tools-icon.png")%>" alt="" />
                <span>Tool List</span>
            </button>
        </div>
        <div class="pod-footer-side"></div>
    </div>

    </div><%-- /pod-shell --%>

    <%-- ══ COMPLETION PANEL ══════════════════════════════════════════════ --%>
    <asp:Panel ID="PnlCompleted" runat="server" Visible="false" CssClass="pod-completion">
        <div class="pod-completion-card" data-guide="pod-final">
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

    <%-- ══ BROWSE WORK LIST MODAL ════════════════════════════════════════ --%>
    <div class="modal fade" id="modalBrowseWorkList" tabindex="-1" aria-hidden="true">
        <div class="modal-dialog modal-lg">
            <div class="modal-content">
                <div class="modal-header mes-divider">
                    <h6 class="modal-title">
                        BROWSE WORK LIST - SFCS QUEUED -
                        <asp:Literal ID="LitBrowseContext" runat="server" />
                    </h6>
                    <div class="mes-titlebar-actions">
                        <button type="button" class="mes-titlebar-btn" title="Maximize"
                            onclick="mesToggleMaximize(this)">&#10530;</button>
                        <button type="button" class="mes-titlebar-btn" title="Close"
                            data-bs-dismiss="modal">&#10005;</button>
                    </div>
                </div>
                <div class="modal-body p-0">
                    <table class="table table-sm mb-0 pod-browse-list">
                        <thead>
                            <tr>
                                <th class="row-selector-col">
                                    <button type="button" class="select-all-btn" title="Select All"
                                        onclick="toggleSelectAllBrowse()" data-guide="browse-select-all"><%= SelectAllIcon %></button>
                                </th>
                                <th>SFC</th>
                                <th>Material Description</th>
                                <th>Shop Order Schedule Start</th>
                                <th>Shop Order Planned Completion</th>
                            </tr>
                        </thead>
                        <tbody id="browseWorkListBody">
                            <asp:Repeater ID="RptBrowseList" runat="server">
                                <ItemTemplate>
                                    <tr class="browse-row" data-guide="browse-row" data-sfc="<%#: Eval("SFCCode") %>"
                                        data-row-id="<%#: Eval("RowId") %>"
                                        data-shop-order="<%#: Eval("ShopOrder") %>"
                                        onclick="selectBrowseRow(this)" style="cursor:pointer">
                                        <td class="row-selector-col"></td>
                                        <td><%#: Eval("SFCCode") %></td>
                                        <td><%#: Eval("MaterialDescription") %></td>
                                        <td><%#: Eval("ScheduleStart") %></td>
                                        <td><%#: Eval("PlannedCompletion") %></td>
                                    </tr>
                                </ItemTemplate>
                            </asp:Repeater>
                        </tbody>
                    </table>
                </div>
                <div class="modal-footer">
                    <button type="button" class="mes-btn mes-ico mes-ico-check" id="btnBrowseOK" data-guide="browse-ok"
                        onclick="confirmBrowseSelection()">OK</button>
                    <button type="button" class="mes-btn mes-ico mes-ico-cancel"
                        data-bs-dismiss="modal">Cancel</button>
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
    var podOrderStarted = <%= PodOrderStarted ? "true" : "false" %>;

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

    // Ícono Select All del header (POD y Browse Work List): selecciona todas las filas y,
    // si ya estaban todas, las deselecciona. También en NonSerialized (decisión 2026-09-29):
    // con varias filas, el servidor aplica la regla de cada paso de una fila (DC y SFC Split
    // avisan sin intento; Log NC y Complete cuentan intento fallido, spec 007).
    function toggleSelectAll(rowSelector, cssClass) {
        var rows = document.querySelectorAll(rowSelector);
        if (rows.length === 0) return;
        var allSelected = Array.prototype.every.call(rows, r => r.classList.contains(cssClass));
        rows.forEach(r => r.classList.toggle(cssClass, !allSelected));
    }

    function toggleSelectAllWorkList() {
        toggleSelectAll('.pod-work-list tbody tr', 'selected');
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

    function toggleSelectAllBrowse() {
        toggleSelectAll('#browseWorkListBody tr', 'table-primary');
    }

    // Browse elige las piezas (o el SFC del lote) para Search/Start, no filas del POD:
    // HfSelectedRowIds solo se llena con selectWorkListRow().
    function confirmBrowseSelection() {
        var rows = document.querySelectorAll('#browseWorkListBody tr.table-primary');
        if (rows.length === 0 && !podMultiSelect) return;

        loadOrderRows(Array.prototype.slice.call(rows));

        bootstrap.Modal.getInstance(
            document.getElementById('modalBrowseWorkList')).hide();
    }

    // ── Carga de la orden para el Start (Browse Work List y escaneo, spec 011) ────
    function browseRows() {
        return Array.prototype.slice.call(document.querySelectorAll('#browseWorkListBody tr'));
    }

    // Deja cargadas las filas para el Start: lo mismo que el OK de Browse (TxtSFC, campos
    // ocultos y header In Queue) y Browse marcado igual, para que muestre lo vigente. Gana la
    // última acción: reemplaza lo cargado antes. El servidor revisa las filas en el Start.
    function loadOrderRows(rows) {
        var ids  = rows.map(r => r.getAttribute('data-row-id'));
        var sfcs = rows.map(r => r.getAttribute('data-sfc'));
        var txt  = document.getElementById('<%= TxtSFC.ClientID %>');
        if (txt) {
            txt.value = describeSelection(sfcs);
            // Lo que muestra el campo ya está cargado: el Start no lo vuelve a resolver.
            txt.setAttribute('data-loaded-text', txt.value);
            // La orden completa (o el lote) o una sola pieza: la guía avanza con la orden.
            var all = browseRows().length;
            txt.setAttribute('data-loaded', rows.length === 0 ? '' :
                (!podMultiSelect || rows.length === all) ? 'order' : 'piece');
        }
        var hf   = document.getElementById('<%= HfSelectedSFC.ClientID %>');
        if (hf) hf.value = sfcs.length === 1 ? sfcs[0] : '';
        var hfBrowse = document.getElementById('<%= HfBrowseRowIds.ClientID %>');
        if (hfBrowse) hfBrowse.value = ids.join(',');

        browseRows().forEach(r => r.classList.toggle('table-primary', rows.indexOf(r) >= 0));

        // Antes del Start, la orden elegida queda In Queue (el servidor lo repite en cada
        // respuesta). Después del Start el header ya muestra la orden Active y no cambia.
        if (!podOrderStarted) {
            var shopOrder = rows.length > 0 ? rows[0].getAttribute('data-shop-order') : '';
            document.getElementById('podShopOrder').textContent = shopOrder;
            document.getElementById('podSfcStatus').textContent = rows.length > 0 ? 'In Queue' : '';
        }
    }

    // Filas que nombra lo escaneado o escrito (sin espacios al inicio ni al final; el resto,
    // exacto): la Shop Order carga la orden completa (todas las piezas o el lote); un SFC,
    // esa pieza o el lote. Sin orden (Resource Setup pendiente) no hay filas: nada coincide.
    function resolveScan(text) {
        var value = (text || '').trim();
        var rows = browseRows();
        if (!value || rows.length === 0) return [];
        var ofOrder = rows.filter(r => r.getAttribute('data-shop-order') === value);
        if (ofOrder.length > 0)
            return podMultiSelect ? ofOrder : [ofOrder[0]];
        return rows.filter(r => r.getAttribute('data-sfc') === value);
    }

    // Enter, el lector (manda el número y Enter) o las barras de la etiqueta. Cargar es
    // navegación: un número que no es de la orden solo avisa, sin intento, y lo cargado no
    // cambia (el campo vuelve a mostrarlo). Devuelve si cargó.
    function podLoadScan(text) {
        var rows = resolveScan(text);
        var txt  = document.getElementById('<%= TxtSFC.ClientID %>');
        if (rows.length === 0) {
            if (txt) {
                txt.value = txt.getAttribute('data-loaded-text') || '';
                selectForNextScan(txt);
            }
            Swal.fire({ icon: 'warning', title: '<%= ScanNoticeTitleJs %>', text: '<%= ScanNoticeTextJs %>' });
            return false;
        }
        loadOrderRows(rows);
        if (txt) selectForNextScan(txt);
        return true;
    }

    // El lector escribe donde está el foco: con el texto seleccionado, el escaneo siguiente
    // lo reemplaza en vez de sumarse a lo cargado.
    function selectForNextScan(txt) {
        if (document.activeElement === txt) txt.select();
    }

    // Enmienda E-1: con Search como paso actual, Start carga primero lo que quedó escrito sin
    // Enter. Vacío o ya cargado: sigue (el servidor da sus avisos de siempre).
    function podLoadBeforeStart() {
        var txt = document.getElementById('<%= TxtSFC.ClientID %>');
        if (!txt || txt.readOnly) return true;
        var value = txt.value.trim();
        // Campo borrado: no queda nada cargado y el servidor da su aviso de siempre.
        if (!value) { loadOrderRows([]); return true; }
        if (value === (txt.getAttribute('data-loaded-text') || '')) return true;
        return podLoadScan(value);
    }

    // Tras un postback el campo y los ocultos vuelven con lo cargado; se recuerda como
    // cargado y Browse se marca igual. Lo escrito sin cargar queda pendiente para el Start.
    // Después del Start TxtSFC y HfSelectedSFC son de la selección del POD: no se tocan.
    function restoreOrderLoad() {
        if (podOrderStarted) return;
        var txt = document.getElementById('<%= TxtSFC.ClientID %>');
        var hfBrowse = document.getElementById('<%= HfBrowseRowIds.ClientID %>');
        if (!txt || !hfBrowse || !hfBrowse.value) return;
        var ids  = hfBrowse.value.split(',');
        var rows = browseRows().filter(r => ids.indexOf(r.getAttribute('data-row-id')) >= 0);
        if (rows.length === 0) return;
        var shown = txt.value;
        loadOrderRows(rows);
        if (shown.trim() !== txt.value) txt.value = shown;
    }
    document.addEventListener('DOMContentLoaded', function () {
        restoreOrderLoad();
        var txt = document.getElementById('<%= TxtSFC.ClientID %>');
        if (!txt) return;
        // Enter nunca envía el formulario (haría clic en el primer botón, Logout). Lo que ya
        // está cargado no se vuelve a resolver.
        txt.addEventListener('keydown', function (e) {
            if (e.key !== 'Enter') return;
            e.preventDefault();
            if (txt.readOnly) return;
            if (txt.value.trim() === (txt.getAttribute('data-loaded-text') || '')) { txt.select(); return; }
            podLoadScan(txt.value);
        });
        // Al entrar al campo, lo escaneado después reemplaza lo cargado. Diferido: con un clic,
        // el mouseup quitaría la selección.
        txt.addEventListener('focus', function () {
            if (!txt.readOnly) setTimeout(function () { txt.select(); }, 0);
        });
        // Formato: < y > no son parte de un número y ASP.NET rechaza el postback con marcado
        // (<x). El campo viaja en cualquier postback, así que se quitan al escribir.
        txt.addEventListener('input', function () {
            if (/[<>]/.test(txt.value)) txt.value = txt.value.replace(/[<>]/g, '');
        });
    });

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
