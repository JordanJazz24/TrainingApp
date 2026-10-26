<%@ Page Title="Seguimiento" Language="C#" MasterPageFile="~/UI/master/Admin.Master"
    AutoEventWireup="true" CodeBehind="FollowUp.aspx.cs"
    Inherits="TrainingApp.Pages.Admin.FollowUp" %>

<%-- Seguimiento (spec 018, AC-19 y AC-20; diseño de la Fase 11, D-160): los operadores que
     requieren atención, una fila por operador y guía, con todas las reglas que cumplen como chips
     con texto (S-1, S-2, S-3; FollowUpRules), primero quien cumple más, y el enlace a su detalle.
     Ignora el período; quien aprueba la guía sale. Filtro de guía en la URL. Solo lectura. --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="admin-page-head">
        <div>
            <h1 class="admin-page-title">Seguimiento</h1>
            <p class="admin-page-lead">Quién necesita atención en una guía que todavía no aprueba, y por qué.</p>
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

    <%-- Las reglas, con el mismo S-n que el Excel y el título de cada chip. --%>
    <ul class="followup-rules mt-muted mt-small">
        <li><strong>S-1</strong>: reprobó <%: TrainingApp.BLL.Reports.FollowUpRules.MinFailedAssessments %> o más Assessment
            sin aprobar ninguno.</li>
        <li><strong>S-2</strong>: <%: TrainingApp.BLL.Reports.FollowUpRules.InactiveDays %> días o más sin sesiones de esa
            guía, aunque practique otras.</li>
        <li><strong>S-3</strong>: el mismo paso terminó sin acierto en
            <%: TrainingApp.BLL.Reports.FollowUpRules.MinAssessmentsWithoutSuccess %> o más Assessment.</li>
        <li>Todo el historial. Al aprobar la guía, el operador sale de la lista.</li>
    </ul>

    <section class="mt-panel" aria-labelledby="listTitle">
        <div class="mt-panel-body pb-0 d-flex flex-wrap justify-content-between align-items-baseline gap-2">
            <h2 id="listTitle" class="mt-section-title">Requieren atención</h2>
            <span class="mt-muted mt-small"><asp:Literal ID="LitCount" runat="server" /></span>
        </div>

        <asp:PlaceHolder ID="PhTable" runat="server">
            <div class="mt-table-wrap mt-3">
                <table class="table mt-table table-hover followup-table">
                    <thead>
                        <tr>
                            <th scope="col">Operador</th>
                            <th scope="col">Guía</th>
                            <th scope="col">Estado</th>
                            <th scope="col">Reglas</th>
                            <th scope="col" class="mt-col-secondary">Última sesión de la guía</th>
                        </tr>
                    </thead>
                    <tbody>
                        <asp:Repeater ID="RptRows" runat="server">
                            <ItemTemplate>
                                <tr>
                                    <td>
                                        <a class="operators-name" href='<%#: Eval("DetailUrl") %>'><%#: Eval("Name") %></a>
                                        <span class="mt-muted mt-small d-block"><%#: Eval("Code1") %></span>
                                    </td>
                                    <td title='<%#: Eval("GuideTitle") %>'><%#: Eval("GuideName") %></td>
                                    <td><span class='<%#: Eval("StateCss") %>'><i class='<%#: Eval("StateIcon") %>' aria-hidden="true"></i><%#: Eval("StateText") %></span></td>
                                    <td>
                                        <div class="followup-chips">
                                            <asp:Repeater runat="server" DataSource='<%# Eval("Chips") %>'>
                                                <ItemTemplate>
                                                    <span class='<%#: Eval("Css") %>' title='<%#: Eval("Title") %>'><i class='<%#: Eval("Icon") %>' aria-hidden="true"></i><%#: Eval("Text") %></span>
                                                </ItemTemplate>
                                            </asp:Repeater>
                                        </div>
                                    </td>
                                    <td class="mt-col-secondary mt-muted text-nowrap"><%#: Eval("LastActivityText") %></td>
                                </tr>
                            </ItemTemplate>
                        </asp:Repeater>
                    </tbody>
                </table>
            </div>
        </asp:PlaceHolder>

        <asp:Panel ID="PnlEmpty" runat="server" CssClass="mt-empty" Visible="false">
            <span><i class="bi bi-check-circle" aria-hidden="true"></i> Ningún operador requiere atención.</span>
        </asp:Panel>
    </section>
</asp:Content>
