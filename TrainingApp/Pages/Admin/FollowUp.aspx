<%@ Page Title="Seguimiento" Language="C#" MasterPageFile="~/UI/master/Admin.Master"
    AutoEventWireup="true" CodeBehind="FollowUp.aspx.cs"
    Inherits="TrainingApp.Pages.Admin.FollowUp" %>

<%-- Seguimiento (spec 018, AC-19 y AC-20; spec 022, brief 5: alternativa B "cola de llamadas" con
     el encabezado P1 "encabezado-índice"). Los casos (operador y guía) que requieren atención, con
     las reglas que cumplen como frases con su S-n (S-1, S-2, S-3; FollowUpRules). "Llamar primero"
     (2 reglas o más, FollowUpSummary) en tarjetas; "Después" (1 regla) en filas compactas; a la
     derecha, los casos por regla. Las pestañas del encabezado solo bajan a su sección (anclas, sin
     postback). Ignora el período; quien aprueba la guía sale. Filtro de guía en la URL. Solo lectura. --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <section class="followup-head" aria-label="Encabezado de Seguimiento">
        <div class="followup-head-top">
            <div>
                <h1 class="admin-page-title">Seguimiento</h1>
                <p class="admin-page-lead">A quién llamar primero y de qué hablarle, en guías que todavía no aprueba.</p>
            </div>
            <asp:Panel ID="PnlFilters" runat="server" CssClass="mt-filters" DefaultButton="BtnFilter">
                <div class="mt-field">
                    <label class="mt-field-label" for="<%= DdlGuide.ClientID %>">Guía</label>
                    <asp:DropDownList ID="DdlGuide" runat="server" CssClass="form-select" />
                </div>
                <asp:Button ID="BtnFilter" runat="server" Text="Filtrar" CssClass="btn btn-secondary"
                    OnClick="BtnFilter_Click" />
                <asp:HyperLink ID="LnkExport" runat="server" CssClass="btn btn-primary"
                    ToolTip="La lista con este filtro, en Excel"><i class="bi bi-download" aria-hidden="true"></i>Descargar</asp:HyperLink>
            </asp:Panel>
        </div>

        <%-- Índice de la cola: cada pestaña baja a su sección; los conteos por regla, con su S-n. --%>
        <asp:PlaceHolder ID="PhTabs" runat="server">
            <nav class="followup-tabs" aria-label="Ir a una parte de la cola">
                <a class="followup-tab is-lead" href="#fu-first">
                    <span class="followup-tab-n"><asp:Literal ID="LitTabFirst" runat="server" /></span>
                    <span class="followup-tab-t">Llamar primero<small>Cumplen <%: TrainingApp.BLL.Reports.FollowUpSummary.CallFirstMinRules %> o más reglas</small></span>
                    <i class="bi bi-chevron-down" aria-hidden="true"></i>
                </a>
                <asp:PlaceHolder ID="PhTabLater" runat="server">
                    <a class="followup-tab" href="#fu-later">
                        <span class="followup-tab-n"><asp:Literal ID="LitTabLater" runat="server" /></span>
                        <span class="followup-tab-t">Después<small>Cumplen 1 regla</small></span>
                        <i class="bi bi-chevron-down" aria-hidden="true"></i>
                    </a>
                </asp:PlaceHolder>
                <div class="followup-rule-counts">
                    <span class="followup-rule-count" title="Reprobó <%: TrainingApp.BLL.Reports.FollowUpRules.MinFailedAssessments %> o más Assessment sin aprobar ninguno"><span class="followup-tag is-alert">S-1</span><b><asp:Literal ID="LitS1" runat="server" /></b></span>
                    <span class="followup-rule-count" title="<%: TrainingApp.BLL.Reports.FollowUpRules.InactiveDays %> días o más sin sesiones de esa guía"><span class="followup-tag">S-2</span><b><asp:Literal ID="LitS2" runat="server" /></b></span>
                    <span class="followup-rule-count" title="El mismo paso terminó sin acierto en <%: TrainingApp.BLL.Reports.FollowUpRules.MinAssessmentsWithoutSuccess %> o más Assessment"><span class="followup-tag">S-3</span><b><asp:Literal ID="LitS3" runat="server" /></b></span>
                    <span class="mt-muted mt-small"><asp:Literal ID="LitCount" runat="server" /></span>
                </div>
            </nav>
        </asp:PlaceHolder>
    </section>

    <div class="followup-layout">
        <div class="followup-col">
            <asp:PlaceHolder ID="PhQueue" runat="server">
                <section id="fu-first" aria-labelledby="firstTitle">
                    <div class="followup-sec-head">
                        <h2 id="firstTitle" class="mt-section-title"><i class="bi bi-telephone" aria-hidden="true"></i>Llamar primero</h2>
                        <span class="mt-muted mt-small"><asp:Literal ID="LitFirstCount" runat="server" /></span>
                    </div>
                    <asp:PlaceHolder ID="PhFirst" runat="server">
                        <ol class="followup-queue">
                            <asp:Repeater ID="RptFirst" runat="server">
                                <ItemTemplate>
                                    <li class="followup-card">
                                        <div class="followup-card-top">
                                            <div>
                                                <h3 class="followup-card-name"><a href='<%#: Eval("DetailUrl") %>'><%#: Eval("Name") %></a> <span class="followup-code">· Code1 <%#: Eval("Code1") %></span></h3>
                                                <p class="followup-card-guide"><b><%#: Eval("GuideName") %></b> <span class="mt-muted">· <%#: Eval("GuideTitle") %></span></p>
                                            </div>
                                            <div class="followup-card-side">
                                                <span class="followup-count"><%#: Eval("RuleCountText") %></span>
                                                <span class='<%#: Eval("StateCss") %>'><i class='<%#: Eval("StateIcon") %>' aria-hidden="true"></i><%#: Eval("StateText") %></span>
                                            </div>
                                        </div>
                                        <ul class="followup-why" aria-label="Por qué">
                                            <asp:Repeater runat="server" DataSource='<%# Eval("Lines") %>'>
                                                <ItemTemplate>
                                                    <li class='<%#: Eval("Css") %>'><span class='<%#: Eval("TagCss") %>'><%#: Eval("Code") %></span><i class='<%#: Eval("Icon") %>' aria-hidden="true"></i><span><%#: Eval("Text") %></span></li>
                                                </ItemTemplate>
                                            </asp:Repeater>
                                        </ul>
                                        <div class="followup-card-foot">
                                            <span class="mt-muted mt-small">Última sesión de la guía: <%#: Eval("LastActivityText") %></span>
                                            <a class="btn btn-secondary" href='<%#: Eval("DetailUrl") %>'>Ver detalle<i class="bi bi-chevron-right" aria-hidden="true"></i></a>
                                        </div>
                                    </li>
                                </ItemTemplate>
                            </asp:Repeater>
                        </ol>
                    </asp:PlaceHolder>
                    <asp:Panel ID="PnlNoFirst" runat="server" CssClass="followup-none" Visible="false">
                        Nadie cumple <%: TrainingApp.BLL.Reports.FollowUpSummary.CallFirstMinRules %> o más reglas con este filtro.
                    </asp:Panel>
                </section>

                <asp:PlaceHolder ID="PhLater" runat="server">
                    <section id="fu-later" aria-labelledby="laterTitle">
                        <div class="followup-sec-head">
                            <h2 id="laterTitle" class="mt-section-title">Después</h2>
                            <span class="mt-muted mt-small"><asp:Literal ID="LitLaterCount" runat="server" /></span>
                        </div>
                        <ul class="followup-later">
                            <asp:Repeater ID="RptLater" runat="server">
                                <ItemTemplate>
                                    <li>
                                        <span class="followup-l-who"><a href='<%#: Eval("DetailUrl") %>'><%#: Eval("Name") %></a> <span class="followup-code">· <%#: Eval("Code1") %></span></span>
                                        <span class="followup-l-guide" title='<%#: Eval("GuideTitle") %>'><%#: Eval("GuideName") %></span>
                                        <span class="followup-l-state"><span class='<%#: Eval("StateCss") %>'><i class='<%#: Eval("StateIcon") %>' aria-hidden="true"></i><%#: Eval("StateText") %></span></span>
                                        <span class="followup-l-why">
                                            <asp:Repeater runat="server" DataSource='<%# Eval("Lines") %>'>
                                                <ItemTemplate>
                                                    <span class='followup-l-line <%#: Eval("Css") %>'><span class='<%#: Eval("TagCss") %>'><%#: Eval("Code") %></span><i class='<%#: Eval("Icon") %>' aria-hidden="true"></i><%#: Eval("Text") %></span>
                                                </ItemTemplate>
                                            </asp:Repeater>
                                        </span>
                                        <span class="followup-l-last">Última sesión <%#: Eval("LastActivityText") %></span>
                                        <a class="followup-l-go" href='<%#: Eval("DetailUrl") %>' title="Ver detalle"><i class="bi bi-chevron-right" aria-hidden="true"></i><span class="visually-hidden">Ver detalle de <%#: Eval("Name") %></span></a>
                                    </li>
                                </ItemTemplate>
                            </asp:Repeater>
                        </ul>
                    </section>
                </asp:PlaceHolder>
            </asp:PlaceHolder>

            <asp:Panel ID="PnlEmpty" runat="server" CssClass="mt-panel" Visible="false">
                <div class="mt-empty">
                    <span class="followup-empty-title"><i class="bi bi-check-circle" aria-hidden="true"></i> <asp:Literal ID="LitEmptyTitle" runat="server" /></span>
                    <span>Ningún operador requiere atención: nadie cumple S-1, S-2 ni S-3 con este filtro.</span>
                    <span class="mt-small">Quien aprobó la guía ya no aparece.</span>
                </div>
            </asp:Panel>
        </div>

        <%-- Casos por regla (métrica derivada, FollowUpSummary) y las reglas explicadas. --%>
        <aside class="followup-aside" aria-label="Resumen de la cola">
            <section class="mt-panel mt-panel-body">
                <h2 class="mt-block-title">En la cola</h2>
                <p class="followup-total"><b><asp:Literal ID="LitTotal" runat="server" /></b><span class="mt-muted"><asp:Literal ID="LitTotalText" runat="server" /></span></p>
                <ul class="followup-bars">
                    <li>
                        <div class="followup-bar-top"><span class="followup-tag is-alert">S-1</span>Reprobados<b><asp:Literal ID="LitBarS1" runat="server" /></b></div>
                        <div class="mt-track" aria-hidden="true"><asp:Panel ID="BarS1" runat="server" CssClass="mt-fill" /></div>
                        <div class="followup-bar-d">Reprobó <%: TrainingApp.BLL.Reports.FollowUpRules.MinFailedAssessments %> o más Assessment sin aprobar ninguno.</div>
                    </li>
                    <li>
                        <div class="followup-bar-top"><span class="followup-tag">S-2</span>Sin practicar<b><asp:Literal ID="LitBarS2" runat="server" /></b></div>
                        <div class="mt-track" aria-hidden="true"><asp:Panel ID="BarS2" runat="server" CssClass="mt-fill" /></div>
                        <div class="followup-bar-d"><%: TrainingApp.BLL.Reports.FollowUpRules.InactiveDays %> días o más sin sesiones de esa guía, aunque practique otras.</div>
                    </li>
                    <li>
                        <div class="followup-bar-top"><span class="followup-tag">S-3</span>Paso trabado<b><asp:Literal ID="LitBarS3" runat="server" /></b></div>
                        <div class="mt-track" aria-hidden="true"><asp:Panel ID="BarS3" runat="server" CssClass="mt-fill" /></div>
                        <div class="followup-bar-d">El mismo paso terminó sin acierto en <%: TrainingApp.BLL.Reports.FollowUpRules.MinAssessmentsWithoutSuccess %> o más Assessment.</div>
                    </li>
                </ul>
            </section>
            <p class="mt-muted mt-small followup-aside-note">Casos (operador y guía) de todo el historial. Un caso puede contar en varias reglas. Al aprobar la guía, el operador sale de la cola.</p>
        </aside>
    </div>
</asp:Content>
