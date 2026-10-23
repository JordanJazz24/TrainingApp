<%@ Page Title="Seguimiento" Language="C#" MasterPageFile="~/UI/master/Admin.Master"
    AutoEventWireup="true" CodeBehind="FollowUp.aspx.cs"
    Inherits="TrainingApp.Pages.Admin.FollowUp" %>

<%-- Seguimiento (spec 018, AC-19 y AC-20): los operadores que requieren atención, por guía, con
     todas las reglas que cumplen (S-1, S-2, S-3; FollowUpRules) y el enlace a su detalle. Ignora
     el período; quien aprueba la guía sale. Filtro de guía en la URL. Solo lectura. --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <h1 class="h3 mb-4">Seguimiento</h1>

    <asp:Panel ID="PnlFilters" runat="server" CssClass="row g-2 align-items-end mb-3" DefaultButton="BtnFilter">
        <div class="col-sm-6 col-md-3">
            <label class="form-label small text-muted mb-1" for="<%= DdlGuide.ClientID %>">Guía</label>
            <asp:DropDownList ID="DdlGuide" runat="server" CssClass="form-select" />
        </div>
        <div class="col-sm-6 col-md-2">
            <asp:Button ID="BtnFilter" runat="server" Text="Filtrar" CssClass="btn btn-primary w-100"
                OnClick="BtnFilter_Click" />
        </div>
        <div class="col-sm-6 col-md-2 ms-md-auto">
            <asp:HyperLink ID="LnkExport" runat="server" CssClass="btn btn-outline-secondary w-100">Descargar Excel</asp:HyperLink>
        </div>
    </asp:Panel>

    <p class="small text-muted mb-3">
        Todo el historial. Un operador aparece en una guía que todavía no aprobó si cumple alguna regla, y
        sale al aprobarla.<br />
        <strong>S-1</strong>: reprobó <%: TrainingApp.BLL.Reports.FollowUpRules.MinFailedAssessments %> o más Assessment
        sin aprobar ninguno ·
        <strong>S-2</strong>: <%: TrainingApp.BLL.Reports.FollowUpRules.InactiveDays %> días o más sin sesiones de esa
        guía, aunque practique otras ·
        <strong>S-3</strong>: el mismo paso terminó sin acierto en
        <%: TrainingApp.BLL.Reports.FollowUpRules.MinAssessmentsWithoutSuccess %> o más Assessment.
    </p>

    <div class="table-responsive">
        <table class="table table-sm table-hover bg-white align-middle">
            <thead>
                <tr>
                    <th>Código</th>
                    <th>Nombre</th>
                    <th>Guía</th>
                    <th>Estado</th>
                    <th>Última sesión de la guía</th>
                    <th>Reglas</th>
                    <th></th>
                </tr>
            </thead>
            <tbody>
                <asp:Repeater ID="RptRows" runat="server">
                    <ItemTemplate>
                        <tr>
                            <td><%#: Eval("Code1") %></td>
                            <td><%#: Eval("Name") %></td>
                            <td title='<%#: Eval("GuideTitle") %>'><%#: Eval("GuideName") %></td>
                            <td><span class='<%#: Eval("StateCss") %>'><%#: Eval("StateText") %></span></td>
                            <td class="text-nowrap"><%#: Eval("LastActivityText") %></td>
                            <td class="small">
                                <asp:Repeater runat="server" DataSource='<%# Eval("Reasons") %>'>
                                    <ItemTemplate><div><%#: Container.DataItem %></div></ItemTemplate>
                                </asp:Repeater>
                            </td>
                            <td class="text-end">
                                <a class="btn btn-link btn-sm" href='<%#: Eval("DetailUrl") %>'>Ver detalle</a>
                            </td>
                        </tr>
                    </ItemTemplate>
                </asp:Repeater>
            </tbody>
        </table>
    </div>

    <asp:Label ID="LblEmpty" runat="server" CssClass="text-muted d-block" Visible="false"
        Text="Ningún operador requiere atención." />
    <p class="small text-muted mb-0"><asp:Literal ID="LitCount" runat="server" /></p>
</asp:Content>
