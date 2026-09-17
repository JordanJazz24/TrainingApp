<%@ Page Title="MES Training" Language="C#" MasterPageFile="~/UI/master/Training.Master"
    AutoEventWireup="true" CodeBehind="GuidedTest.aspx.cs"
    Inherits="TrainingApp.Pages.Training.GuidedTest" %>

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
                <asp:TextBox ID="TxtSFC" runat="server" CssClass="form-control sfc-input"
                    placeholder="Scan or type..." />
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
            onserverclick="BtnDCCollection_ServerClick">
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
            onserverclick="BtnSFCSplit_ServerClick">
            <img src="<%=ResolveUrl("~/UI/master/imgs/split-icon.png")%>" alt="" />
            <span>SFC Split</span>
        </button>
        <button id="BtnLogNC" runat="server" class="pod-sidebar-btn" disabled="disabled"
            onserverclick="BtnLogNC_ServerClick">
            <img src="<%=ResolveUrl("~/UI/master/imgs/log_NC-Icon.png")%>" alt="" />
            <span>Log NC</span>
        </button>
        <button type="button" class="pod-sidebar-btn" disabled>
            <img src="<%=ResolveUrl("~/UI/master/imgs/intructions-icon.png")%>" alt="" />
            <span>Work Instructions</span>
        </button>
        <button type="button" class="pod-sidebar-btn" disabled>
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
                <li><button type="button" class="dropdown-item" disabled>As-Built Configuration</button></li>
                <li><button type="button" class="dropdown-item" disabled>Log Comment</button></li>
                <li><button type="button" class="dropdown-item" disabled>SFC Place Hold</button></li>
                <li><button type="button" class="dropdown-item" disabled>SFC Release Hold</button></li>
                <li><button type="button" class="dropdown-item" disabled>Remove Future Hold</button></li>
                <li><button type="button" class="dropdown-item" disabled>Change Equipment Status Tool</button></li>
                <li><button type="button" class="dropdown-item" disabled>Time Sensitive Material Check In/Out</button></li>
            </ul>
        </div>
        <button type="button" class="pod-sidebar-btn" disabled>
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
    <%-- List-view controls (Assembly, DC, LogNC) render inline here.           --%>
    <%-- Modal controls (ResourceSetup, LC, Search) render their Bootstrap      --%>
    <%-- modals at body level — no overflow/fixed parent to clip them.          --%>
    <asp:Panel ID="PnlActiveControl" runat="server" Visible="false" CssClass="pod-active-control">
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
        <button type="button" class="pod-footer-btn" disabled>
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

    <%-- Host for modal-overlay UserControls (ResourceSetup, LineClearance, Search). --%>
    <%-- No positioning — modal HTML renders here then showModalSafe moves it         --%>
    <%-- to document.forms[0] for proper Bootstrap stacking.                          --%>
    <asp:Panel ID="PnlModalHost" runat="server" Visible="false" />

</asp:Content>

<asp:Content ID="ScriptContent" ContentPlaceHolderID="ScriptContent" runat="server">
<script>
    // ── Work list row selection ──────────────────────────────────────
    function selectWorkListRow(row) {
        document.querySelectorAll('.pod-work-list tbody tr')
            .forEach(r => r.classList.remove('selected'));
        row.classList.add('selected');
        var hf = document.getElementById('<%= HfSelectedSFC.ClientID %>');
        if (hf) hf.value = row.getAttribute('data-sfc');
    }

    // ── Browse Work List selection ───────────────────────────────────
    var _selectedBrowseSFC = null;
    function selectBrowseRow(row) {
        document.querySelectorAll('#browseWorkListBody tr')
            .forEach(r => r.classList.remove('table-primary'));
        row.classList.add('table-primary');
        _selectedBrowseSFC = row.getAttribute('data-sfc');
    }

    function confirmBrowseSelection() {
        if (!_selectedBrowseSFC) return;
        var txt = document.getElementById('<%= TxtSFC.ClientID %>');
        if (txt) txt.value = _selectedBrowseSFC;
        var hf  = document.getElementById('<%= HfSelectedSFC.ClientID %>');
        if (hf)  hf.value  = _selectedBrowseSFC;
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
