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
                        <%-- data-nc-code: ancla de la guía de Guided sobre el código del
                             hallazgo (spec 014). --%>
                        <asp:Button ID="BtnLogNC" runat="server"
                            CssClass="mes-btn mes-ico mes-ico-lognc"
                            Text="Log NC" data-guide="nc-code" data-nc-code='<%# Eval("Code") %>'
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
                <%-- Reparación extendida (captura "sub modal seleccion operaicon a ser devuelta"):
                     "NC logged" sobre la fila SFC al dar Add-Done; lo muestra el cliente. --%>
                <div class="nc-logged" id="ncLogged" style="display:none">NC logged</div>
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
                    <%-- Solo con un código de reparación (enmienda 4 de la spec 014): el desecho
                         no lo muestra en ningún área. Obligatorio (RfvComponent) en la rápida y
                         opcional en la extendida, sin asterisco: así lo muestra el real. ⧉ abre el
                         Browse con lo ensamblado (D-39); también se puede escribir. --%>
                    <asp:Panel ID="PnlComponent" runat="server" CssClass="nc-form">
                        <span class="nc-form-label">Component:</span>
                        <span class="mes-field nc-form-input">
                            <asp:TextBox ID="TxtComponent" runat="server"
                                ValidationGroup="ncGroup" data-guide="nc-component"
                                oninput="this.setAttribute('data-picked', '');" />
                            <button type="button" class="mes-field-icon" title="Browse"
                                data-guide="nc-component-browse" onclick="ncOpenBrowse();">&#10697;</button>
                        </span>
                        <span></span>
                        <asp:RequiredFieldValidator ID="RfvComponent" runat="server"
                            ControlToValidate="TxtComponent"
                            ErrorMessage="Component required." CssClass="text-danger small"
                            Display="Dynamic" ValidationGroup="ncGroup" />
                    </asp:Panel>
                    <%-- ★ y obligatorio con un código que exige Comments (P-13, captura "modal nc
                         entry extended"): formato, sin intento; el contenido no se califica (D-38). --%>
                    <div class="nc-form">
                        <span class="nc-form-label"><asp:Label ID="LblCommentsStar" runat="server"
                            CssClass="pod-star" Text="*" Visible="false" /> Comments:</span>
                        <asp:TextBox ID="TxtComments" runat="server" TextMode="MultiLine" Rows="2"
                            CssClass="mes-input nc-form-input nc-form-comments"
                            ValidationGroup="ncGroup" data-guide="nc-comments" />
                        <span></span>
                        <asp:RequiredFieldValidator ID="RfvComments" runat="server"
                            ControlToValidate="TxtComments" Enabled="false"
                            ErrorMessage="Comments required." CssClass="text-danger small"
                            Display="Dynamic" ValidationGroup="ncGroup" />
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
                    <%-- Reparación rápida (spec 014): Add-Done abre en el cliente la secuencia
                         de Remove component y Add Component (ncAddDone); el envío es uno solo. --%>
                    <asp:Button ID="BtnAddDone" runat="server" Text="Add-Done"
                        CssClass="mes-btn mes-ico mes-ico-check" data-guide="nc-add-done"
                        ValidationGroup="ncGroup" OnClientClick="if (!ncAddDone()) return false;"
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

    <%-- ══ Reparaciones y Browse (spec 014, capturas de "Split - Log NC") ═══════════════
         Ventanas del cliente sobre NC Data Entry: una a la vez (ncShowSub). Navegación y
         formato en JS; el único postback es BtnRepairSubmit con la decisión (D-59). ✕,
         Cancel y No a remover vuelven a NC Data Entry sin enviar (D-35, P-15).
         HfRepairFlow: 0 sin flujo propio, 1 reparación rápida, 2 reparación extendida. --%>
    <asp:HiddenField ID="HfRepairFlow" runat="server" Value="0" />
    <asp:HiddenField ID="HfRepairDecision" runat="server" />
    <asp:Button ID="BtnRepairSubmit" runat="server" style="display:none" tabindex="-1"
        OnClick="BtnRepairSubmit_Click" CausesValidation="false" UseSubmitBehavior="false" />
    <div id="ncSubBackdrop" class="modal-backdrop show nc-sub-backdrop" style="display:none"></div>

    <%-- Browse (browse component.png): los componentes ensamblados en Assembly. --%>
    <div class="modal nc-sub" id="ncBrowse" tabindex="-1" style="display:none" data-guide="nc-browse">
        <div class="modal-dialog modal-lg">
            <div class="modal-content">
                <div class="modal-header">
                    <h6 class="modal-title">Browse</h6>
                    <div class="mes-titlebar-actions">
                        <button type="button" class="mes-titlebar-btn" title="Maximize" onclick="mesToggleMaximize(this)">&#10530;</button>
                        <button type="button" class="mes-titlebar-btn" title="Close" onclick="ncCloseSub();">&#10005;</button>
                    </div>
                </div>
                <div class="modal-body p-0">
                    <table class="nc-browse-table">
                        <thead><tr><th class="nc-browse-sel"></th><th>Component</th><th>Description</th></tr></thead>
                        <tbody data-guide="nc-browse-list">
                            <asp:Repeater ID="RptBrowse" runat="server">
                                <ItemTemplate>
                                    <tr class="nc-browse-row" data-guide="nc-browse-row" onclick="ncPickRow(this);"
                                        data-component="<%#: Eval("PartNumber") %>"
                                        data-version="<%#: Eval("Revision") %>"
                                        data-desc="<%#: Eval("Description") %>"
                                        data-qty="<%#: FormatQty(Eval("Qty")) %>"
                                        data-batch="<%#: Eval("BatchNumber") %>"
                                        data-invsfc="<%#: Eval("InventorySfc") %>">
                                        <td class="nc-browse-sel"></td>
                                        <td><%#: Eval("PartNumber") %></td>
                                        <td><%#: Eval("Description") %></td>
                                    </tr>
                                </ItemTemplate>
                            </asp:Repeater>
                        </tbody>
                    </table>
                </div>
                <div class="modal-footer mes-footer nc-sub-footer-right">
                    <button type="button" class="mes-btn mes-ico mes-ico-check" data-guide="nc-browse-ok" onclick="ncBrowseOk();">OK</button>
                    <button type="button" class="mes-btn mes-ico mes-ico-cancel" onclick="ncCloseSub();">Cancel</button>
                </div>
            </div>
        </div>
    </div>

    <%-- Confirm (mensaje eliminar componente.png). No vuelve a NC Data Entry (D-35). --%>
    <div class="modal nc-sub" id="ncConfirmRemove" tabindex="-1" style="display:none">
        <div class="modal-dialog">
            <div class="modal-content">
                <div class="modal-header">
                    <h6 class="modal-title">Confirm</h6>
                    <div class="mes-titlebar-actions">
                        <button type="button" class="mes-titlebar-btn" title="Close" onclick="ncCloseSub();">&#10005;</button>
                    </div>
                </div>
                <div class="modal-body nc-confirm-body">
                    Do you want to remove component from <span data-f="sfc"></span>?
                </div>
                <div class="modal-footer mes-footer nc-sub-footer-right">
                    <button type="button" class="mes-btn mes-ico mes-ico-check" data-guide="nc-remove-yes" onclick="ncShowSub('ncRemove');">Yes</button>
                    <button type="button" class="mes-btn mes-ico mes-ico-cancel" onclick="ncCloseSub();">No</button>
                </div>
            </div>
        </div>
    </div>

    <%-- Remove component (remove compoenet modal .png), datos de P-11. --%>
    <div class="modal nc-sub" id="ncRemove" tabindex="-1" style="display:none">
        <div class="modal-dialog modal-lg">
            <div class="modal-content">
                <div class="modal-header">
                    <h6 class="modal-title">Remove component</h6>
                    <div class="mes-titlebar-actions">
                        <button type="button" class="mes-titlebar-btn" title="Maximize" onclick="mesToggleMaximize(this)">&#10530;</button>
                        <button type="button" class="mes-titlebar-btn" title="Close" onclick="ncCloseSub();">&#10005;</button>
                    </div>
                </div>
                <div class="modal-body p-0">
                    <div class="mes-subheader mes-divider">
                        <span>As-Built Configuration</span>
                        <span class="mes-help" title="Help">?</span>
                    </div>
                    <div class="ncr-grid">
                        <div class="ncr-col">
                            <div class="ncr-row"><span class="ncr-label">SFC:</span><span data-f="sfc"></span></div>
                            <div class="ncr-row"><span class="ncr-label">Material/Vers.:</span><span data-f="material"></span></div>
                            <div class="ncr-row"><span class="ncr-label">Routing/Vers.:</span><span data-f="routing"></span></div>
                            <div class="ncr-gap"></div>
                            <div class="ncr-row"><span class="ncr-label">Actual Component/Vers.:</span><span data-f="componentVersion"></span></div>
                            <div class="ncr-row"><span class="ncr-label">Description:</span><span data-f="componentDesc"></span></div>
                            <div class="ncr-row"><span class="ncr-label">Ref Des:</span><span></span></div>
                            <div class="ncr-gap"></div>
                            <div class="ncr-row"><span class="ncr-label">Qty Required:</span><span data-f="qty"></span></div>
                            <div class="ncr-row"><span class="ncr-label">Qty Assembled:</span><span data-f="qty"></span></div>
                        </div>
                        <div class="ncr-col">
                            <div class="ncr-row"><span class="ncr-label">Status:</span><span data-f="status"></span></div>
                            <div class="ncr-row"><span class="ncr-label">Description:</span><span data-f="description"></span></div>
                            <div class="ncr-row"><span class="ncr-label">BOM/Version:</span><span data-f="bom"></span></div>
                            <div class="ncr-gap"></div>
                            <div class="ncr-row"><span class="ncr-label">Assembly Operation:</span><span data-f="operation"></span></div>
                            <div class="ncr-row"><span class="ncr-label">Assembly Resource:</span><span data-f="resource"></span></div>
                            <div class="ncr-row"><span class="ncr-label">Inventory SFC:</span><input type="text" class="mes-input ncr-input" data-fv="invSfc" readonly /></div>
                            <div class="ncr-row"><span class="ncr-label">Batch Number:</span><input type="text" class="mes-input ncr-input" data-fv="batch" readonly /></div>
                        </div>
                    </div>
                    <div class="ncr-section">Remove assembly details</div>
                    <div class="ncr-details">
                        <div class="ncr-row"><span class="ncr-label"><span class="pod-star">*</span> Qty to Remove:</span>
                            <input type="text" id="ncQtyRemove" class="mes-input ncr-input" autocomplete="off" /></div>
                        <div class="ncr-row"><span class="ncr-label">Remove Operation:</span>
                            <span class="mes-field ncr-input"><input type="text" data-fv="operation" readonly /><span class="mes-field-icon">&#10697;</span></span></div>
                        <div class="ncr-row"><span class="ncr-label">Remove Resource:</span>
                            <span class="mes-field ncr-input"><input type="text" data-fv="resource" readonly /><span class="mes-field-icon">&#10697;</span></span></div>
                    </div>
                </div>
                <div class="modal-footer mes-footer">
                    <button type="button" class="mes-btn" data-guide="nc-remove-scrap" onclick="ncRemove('scrap');">Scrap</button>
                    <button type="button" class="mes-btn" onclick="ncRemove('return');">Return</button>
                    <button type="button" class="mes-btn" onclick="ncRemove('sendToRouting');">Send to Routing</button>
                    <button type="button" class="mes-btn" onclick="ncCloseSub();">Cancel</button>
                </div>
            </div>
        </div>
    </div>

    <%-- Confirm (confirmacion agregar compoenente .png). No es la decisión equivocada (D-34). --%>
    <div class="modal nc-sub" id="ncConfirmAdd" tabindex="-1" style="display:none">
        <div class="modal-dialog">
            <div class="modal-content">
                <div class="modal-header">
                    <h6 class="modal-title">Confirm</h6>
                    <div class="mes-titlebar-actions">
                        <button type="button" class="mes-titlebar-btn" title="Close" onclick="ncCloseSub();">&#10005;</button>
                    </div>
                </div>
                <div class="modal-body nc-confirm-body">
                    Do you want to add component to <span data-f="sfc"></span>?
                </div>
                <div class="modal-footer mes-footer nc-sub-footer-right">
                    <button type="button" class="mes-btn mes-ico mes-ico-check" data-guide="nc-add-yes" onclick="ncShowSub('ncAddComponent');">Yes</button>
                    <button type="button" class="mes-btn mes-ico mes-ico-cancel" onclick="ncSubmitRepair('scrap', false);">No</button>
                </div>
            </div>
        </div>
    </div>

    <%-- Select Disposition Function (sub modal seleccion operaicon a ser devuelta .png):
         reparación extendida. Select envía la disposición (único postback); Cancel y ✕
         vuelven a NC Data Entry sin enviar (P-15). --%>
    <div class="modal nc-sub" id="ncDisposition" tabindex="-1" style="display:none">
        <div class="modal-dialog modal-lg">
            <div class="modal-content">
                <div class="modal-header mes-titlebar-bare">
                    <div class="mes-titlebar-actions">
                        <button type="button" class="mes-titlebar-btn" title="Maximize" onclick="mesToggleMaximize(this)">&#10530;</button>
                        <button type="button" class="mes-titlebar-btn" title="Close" onclick="ncCloseSub();">&#10005;</button>
                    </div>
                </div>
                <div class="mes-subheader mes-divider">
                    <span>Select Disposition Function for <span data-f="sfc"></span></span>
                    <span class="mes-help" title="Help">?</span>
                </div>
                <div class="modal-body">
                    <table class="nc-disp-table">
                        <tbody>
                            <tr><td class="nc-disp-name">Local Rework</td>
                                <td class="nc-disp-action"><button type="button" class="mes-btn mes-ico mes-ico-check"
                                    data-guide="nc-disposition" data-disposition="localRework"
                                    onclick="ncSelectDisposition('localRework');">Select</button></td></tr>
                            <tr><td class="nc-disp-name">Return to any previous</td>
                                <td class="nc-disp-action"><button type="button" class="mes-btn mes-ico mes-ico-check"
                                    data-guide="nc-disposition" data-disposition="returnToPrevious"
                                    onclick="ncSelectDisposition('returnToPrevious');">Select</button></td></tr>
                            <tr><td colspan="2"></td></tr>
                            <tr><td colspan="2"></td></tr>
                            <tr><td colspan="2"></td></tr>
                            <tr><td colspan="2"></td></tr>
                            <tr><td colspan="2"></td></tr>
                        </tbody>
                    </table>
                </div>
                <div class="modal-footer mes-footer nc-sub-footer-right">
                    <button type="button" class="mes-btn mes-ico mes-ico-cancel" onclick="ncCloseSub();">Cancel</button>
                </div>
            </div>
        </div>
    </div>

    <%-- Add Component (modal add componenet .png; enmienda 2026-10-16 de la spec): sin
         Barcode; sin W13 (E4), Add Assembly Details pide solo el Batch Number. --%>
    <div class="modal nc-sub" id="ncAddComponent" tabindex="-1" style="display:none">
        <div class="modal-dialog modal-lg">
            <div class="modal-content">
                <div class="modal-header">
                    <h6 class="modal-title">Add Component</h6>
                    <div class="mes-titlebar-actions">
                        <button type="button" class="mes-titlebar-btn" title="Maximize" onclick="mesToggleMaximize(this)">&#10530;</button>
                        <button type="button" class="mes-titlebar-btn" title="Close" onclick="ncCloseSub();">&#10005;</button>
                    </div>
                </div>
                <div class="modal-body p-0">
                    <div class="mes-subheader mes-divider"><span>As-Built Configuration</span></div>
                    <div class="ncr-grid">
                        <div class="ncr-col">
                            <div class="ncr-row"><span class="ncr-label">SFC:</span><span data-f="sfc"></span></div>
                            <div class="ncr-row"><span class="ncr-label">Material/Vers.:</span><span data-f="material"></span></div>
                            <div class="ncr-row"><span class="ncr-label">Routing/Vers.:</span><span data-f="routing"></span></div>
                        </div>
                        <div class="ncr-col">
                            <div class="ncr-row"><span class="ncr-label">Status:</span><span data-f="status"></span></div>
                            <div class="ncr-row"><span class="ncr-label">Description:</span><span data-f="description"></span></div>
                            <div class="ncr-row"><span class="ncr-label">BOM/Version:</span><span data-f="bom"></span></div>
                        </div>
                    </div>
                    <div class="ncr-grid ncr-grid-sections">
                        <div class="ncr-col">
                            <div class="ncr-section">Component Details</div>
                            <div class="ncr-row"><span class="ncr-label"><span class="pod-star">*</span> Component:</span>
                                <span class="mes-field ncr-input"><input type="text" id="ncAddComp" autocomplete="off" /><span class="mes-field-icon">&#10697;</span></span></div>
                            <div class="ncr-row"><span class="ncr-label"><span class="pod-star">*</span> Version:</span>
                                <input type="text" id="ncAddVersion" class="mes-input ncr-input" autocomplete="off" /></div>
                            <div class="ncr-row"><span class="ncr-label">Description:</span><span data-f="componentDesc"></span></div>
                            <div class="ncr-row"><span class="ncr-label">Ref Des:</span>
                                <input type="text" class="mes-input ncr-input" autocomplete="off" /></div>
                            <div class="ncr-row"><span class="ncr-label">Qty Required:</span><span data-f="qty"></span></div>
                            <div class="ncr-row"><span class="ncr-label">Qty Assembled:</span><span>0</span></div>
                            <div class="ncr-row"><span class="ncr-label"><span class="pod-star">*</span> Qty to Assemble:</span>
                                <input type="text" id="ncAddQty" class="mes-input ncr-input" autocomplete="off" /></div>
                        </div>
                        <div class="ncr-col">
                            <div class="ncr-section">Add Assembly Details</div>
                            <div class="ncr-row"><span class="ncr-label">Assembly Operation:</span>
                                <span class="mes-field ncr-input"><input type="text" data-fv="operation" readonly /><span class="mes-field-icon">&#10697;</span></span></div>
                            <div class="ncr-row"><span class="ncr-label">Assembly Resource:</span>
                                <span class="mes-field ncr-input"><input type="text" data-fv="resource" readonly /><span class="mes-field-icon">&#10697;</span></span></div>
                            <div class="ncr-row"><span class="ncr-label"><span class="required-star">*</span> Batch Number:</span>
                                <input type="text" id="ncAddBatch" class="mes-input ncr-input" autocomplete="off" data-guide="nc-addcomp-batch" /></div>
                        </div>
                    </div>
                    <div class="ncr-add-actions">
                        <button type="button" class="mes-btn" data-guide="nc-addcomp-add" onclick="ncAddComponent();">Add</button>
                        <button type="button" class="mes-btn" onclick="ncCloseSub();">Cancel</button>
                    </div>
                </div>
            </div>
        </div>
    </div>
</asp:Panel>

<script>
// Reparación rápida y Browse (spec 014): ventanas del cliente sobre NC Data Entry.
(function () {
    var info     = <%= RepairInfoJson %>;
    var flowId   = '<%= HfRepairFlow.ClientID %>';
    var decId    = '<%= HfRepairDecision.ClientID %>';
    var submitName = '<%= BtnRepairSubmit.UniqueID %>';
    var compId   = '<%= TxtComponent.ClientID %>';
    var commId   = '<%= TxtComments.ClientID %>';
    var picked   = null;

    function byId(id) { return document.getElementById(id); }
    function subs() { return document.querySelectorAll('.nc-sub'); }
    function rows() { return document.querySelectorAll('#ncBrowse .nc-browse-row'); }

    function warn(title, text) {
        if (window.Swal) Swal.fire({ icon: 'warning', title: title, text: text });
    }

    function hideSubs() {
        Array.prototype.forEach.call(subs(), function (m) { m.classList.remove('show'); m.style.display = 'none'; });
        var b = byId('ncSubBackdrop');
        if (b) b.style.display = 'none';
        // "NC logged" acompaña a Select Disposition Function: al salir sin elegir no queda.
        var logged = byId('ncLogged');
        if (logged) logged.style.display = 'none';
    }

    // Una ventana a la vez, sobre NC Data Entry (que sigue abierto detrás).
    window.ncShowSub = function (id) {
        hideSubs();
        var m = byId(id);
        if (!m) return;
        m.style.display = 'block';
        m.classList.add('show');
        byId('ncSubBackdrop').style.display = 'block';
    };

    // ✕, Cancel y No a remover: vuelven a NC Data Entry sin enviar (D-35, P-15).
    window.ncCloseSub = hideSubs;

    window.ncOpenBrowse = function () {
        var value = (byId(compId).value || '').trim();
        Array.prototype.forEach.call(rows(), function (tr) {
            tr.classList.toggle('selected', tr.getAttribute('data-component') === value);
        });
        ncShowSub('ncBrowse');
    };

    window.ncPickRow = function (tr) {
        Array.prototype.forEach.call(rows(), function (r) { r.classList.toggle('selected', r === tr); });
    };

    window.ncBrowseOk = function () {
        var tr = document.querySelector('#ncBrowse .nc-browse-row.selected');
        if (!tr) return;
        var input = byId(compId);
        input.value = tr.getAttribute('data-component');
        // Elegido en el Browse (la guía de Guided lo espera).
        input.setAttribute('data-picked', input.value);
        hideSubs();
    };

    // Datos del componente elegido: su fila del Browse (o solo el número si se escribió otro).
    function componentData(value) {
        var found = null;
        Array.prototype.forEach.call(rows(), function (tr) {
            if (tr.getAttribute('data-component') === value) found = tr;
        });
        return {
            component: value,
            version:   found ? found.getAttribute('data-version') : '',
            desc:      found ? found.getAttribute('data-desc') : '',
            qty:       found ? found.getAttribute('data-qty') : '',
            batch:     found ? found.getAttribute('data-batch') : '',
            invSfc:    found ? found.getAttribute('data-invsfc') : ''
        };
    }

    function fill(c) {
        var values = {
            sfc: info.sfc, material: info.material, routing: info.routing, status: info.status,
            description: info.description, bom: info.bom, operation: info.operation, resource: info.resource,
            componentVersion: c.component + (c.version ? '/' + c.version : ''), componentDesc: c.desc,
            qty: c.qty, batch: c.batch, invSfc: c.invSfc
        };
        Array.prototype.forEach.call(document.querySelectorAll('.nc-sub [data-f]'), function (el) {
            el.textContent = values[el.getAttribute('data-f')] || '';
        });
        Array.prototype.forEach.call(document.querySelectorAll('.nc-sub [data-fv]'), function (el) {
            el.value = values[el.getAttribute('data-fv')] || '';
        });
        byId('ncQtyRemove').value  = '1';
        byId('ncAddComp').value    = c.component;
        byId('ncAddVersion').value = c.version;
        byId('ncAddQty').value     = c.qty;
        byId('ncAddBatch').value   = '';
    }

    // Add-Done: con un código de reparación abre su secuencia en el cliente; si no, sigue el
    // envío normal (con la validación de ncGroup: Component y Comments son formato).
    window.ncAddDone = function () {
        // ASP.NET rechaza un postback con marcado: se bloquea como formato (CLAUDE.md).
        var typed = [byId(compId), byId(commId)].map(function (el) { return el ? el.value || '' : ''; }).join('');
        if (/[<>]/.test(typed)) {
            warn('Formato inválido', 'Component y Comments no aceptan los caracteres < ni >.');
            return false;
        }
        var flow = byId(flowId);
        if (!flow || (flow.value !== '1' && flow.value !== '2')) return true;
        if (typeof Page_ClientValidate === 'function' && !Page_ClientValidate('ncGroup')) return false;
        var input = byId(compId);
        var value = input ? (input.value || '').trim() : '';
        if (flow.value === '2') {
            // Extendida (D-37): "NC logged" y Select Disposition Function. Component es opcional.
            fill(componentData(value));
            ncShowSub('ncDisposition');
            byId('ncLogged').style.display = '';
            return false;
        }
        if (!value) {
            warn('Campo requerido', 'Ingrese el componente a reemplazar.');
            return false;
        }
        picked = componentData(value);
        fill(picked);
        ncShowSub('ncConfirmRemove');
        return false;
    };

    function positiveNumber(v) { return /^\d+(\.\d+)?$/.test(v) && parseFloat(v) > 0; }

    // Remove component: Qty to Remove es formato (sin intento). Scrap sigue a "add?"; Return y
    // Send to Routing envían la decisión equivocada (D-34, D-35).
    window.ncRemove = function (action) {
        if (!positiveNumber((byId('ncQtyRemove').value || '').trim())) {
            warn('Qty inválida', 'Qty to Remove debe ser un número mayor que 0.');
            return;
        }
        if (action === 'scrap') { ncShowSub('ncConfirmAdd'); return; }
        ncSubmitRepair(action, false);
    };

    // Add Component: Component, Version, Qty to Assemble y Batch Number son formato; nada se
    // califica (enmienda 2026-10-16 de la spec).
    window.ncAddComponent = function () {
        var comp = (byId('ncAddComp').value || '').trim();
        var version = (byId('ncAddVersion').value || '').trim();
        var batch = (byId('ncAddBatch').value || '').trim();
        if (!comp || !version || /[<>]/.test(comp + version)) {
            warn('Campo requerido', 'Component y Version son obligatorios.');
            return;
        }
        if (!positiveNumber((byId('ncAddQty').value || '').trim())) {
            warn('Qty inválida', 'Qty to Assemble debe ser un número mayor que 0.');
            return;
        }
        if (!/^[A-Za-z0-9]+$/.test(batch)) {
            warn('Campo requerido', 'Batch Number es obligatorio (letras y números).');
            return;
        }
        ncSubmitRepair('scrap', true);
    };

    // Select de la extendida: la disposición es la decisión del paso (D-37b).
    window.ncSelectDisposition = function (disposition) {
        byId(decId).value = JSON.stringify({ disposition: disposition });
        __doPostBack(submitName, '');
    };

    // Único postback del flujo: la decisión viaja en un campo oculto. Se envía sin un clic
    // sobre el botón oculto: la guía de Guided ya espera el envío del clic en Add y bloquearía
    // un segundo clic.
    window.ncSubmitRepair = function (removeAction, addComponent) {
        byId(decId).value = JSON.stringify({ removeAction: removeAction, addComponent: addComponent });
        __doPostBack(submitName, '');
    };
})();

(function () {
    var PAGE_SIZE = <%= NCPageSize %>;
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
