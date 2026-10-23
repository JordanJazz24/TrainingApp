<%@ Page Title="Operador" Language="C#" MasterPageFile="~/UI/master/Admin.Master"
    AutoEventWireup="true" CodeBehind="Operator.aspx.cs"
    Inherits="TrainingApp.Pages.Admin.Operator" %>

<%-- Detalle de un Operador (spec 018, AC-14): estado por guía (ReadinessPolicy), pasos a
     reforzar en sus Assessment y sus sesiones, con horas de la planta. Un id inexistente, de
     un administrador o manipulado vuelve a Operadores con un aviso, sin datos. Solo lectura. --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <asp:HyperLink ID="LnkBack" runat="server" CssClass="btn btn-link btn-sm px-0 mb-2">&laquo; Operadores</asp:HyperLink>
    <h1 class="h3 mb-1"><asp:Literal ID="LitName" runat="server" /></h1>
    <p class="text-muted mb-4">Código <asp:Literal ID="LitCode1" runat="server" /></p>

    <h2 class="h5 mb-3">Estado por guía</h2>
    <div class="row g-3 mb-4">
        <asp:Repeater ID="RptGuides" runat="server">
            <ItemTemplate>
                <div class="col-md-4">
                    <div class="card h-100">
                        <div class="card-body">
                            <div class="d-flex justify-content-between align-items-start mb-2">
                                <h3 class="h6 mb-0" title='<%#: Eval("GuideTitle") %>'><%#: Eval("GuideName") %></h3>
                                <span class='<%#: Eval("StateCss") %>'><%#: Eval("StateText") %></span>
                            </div>
                            <div class="small text-muted">
                                Mejor puntaje: <%#: Eval("BestText") %><br />
                                Último puntaje: <%#: Eval("LastText") %><br />
                                Última actividad: <%#: Eval("LastActivityText") %>
                            </div>
                        </div>
                    </div>
                </div>
            </ItemTemplate>
        </asp:Repeater>
    </div>

    <h2 class="h5 mb-1">Pasos a reforzar</h2>
    <p class="small text-muted mb-3">
        En sus Assessment: pasos que terminaron sin acierto o con 2 o más intentos fallidos, hasta 5
        por guía.
    </p>
    <asp:Repeater ID="RptReinforcement" runat="server">
        <ItemTemplate>
            <h3 class="h6"><%#: Eval("GuideName") %></h3>
            <div class="table-responsive mb-3">
                <table class="table table-sm bg-white">
                    <thead>
                        <tr>
                            <th>Paso</th>
                            <th class="text-end">Veces sin acierto</th>
                            <th class="text-end">Intentos fallidos</th>
                            <th>Motivo más frecuente</th>
                        </tr>
                    </thead>
                    <tbody>
                        <asp:Repeater runat="server" DataSource='<%# Eval("Steps") %>'>
                            <ItemTemplate>
                                <tr>
                                    <td><%#: Eval("StepName") %></td>
                                    <td class="text-end"><%#: Eval("TimesWithoutSuccess") %></td>
                                    <td class="text-end"><%#: Eval("FailedAttempts") %></td>
                                    <td><%#: Eval("MainReasonText") %></td>
                                </tr>
                            </ItemTemplate>
                        </asp:Repeater>
                    </tbody>
                </table>
            </div>
        </ItemTemplate>
    </asp:Repeater>
    <asp:Label ID="LblNoReinforcement" runat="server" CssClass="text-muted d-block mb-4" Visible="false"
        Text="Sin pasos a reforzar en sus Assessment." />

    <h2 class="h5 mb-3 mt-2">Sesiones</h2>
    <div class="table-responsive">
        <table class="table table-sm table-hover bg-white">
            <thead>
                <tr>
                    <th>Inicio</th>
                    <th>Guía</th>
                    <th>Modo</th>
                    <th>Estado</th>
                    <th class="text-end">Puntaje</th>
                    <th class="text-end">Duración</th>
                    <th class="text-end">Intentos fallidos</th>
                </tr>
            </thead>
            <tbody>
                <asp:Repeater ID="RptSessions" runat="server">
                    <ItemTemplate>
                        <tr>
                            <td class="text-nowrap"><%#: Eval("StartedText") %></td>
                            <td><%#: Eval("GuideName") %></td>
                            <td><%#: Eval("ModeText") %></td>
                            <td><%#: Eval("StatusText") %></td>
                            <td class="text-end"><%#: Eval("ScoreText") %></td>
                            <td class="text-end text-nowrap"><%#: Eval("DurationText") %></td>
                            <td class="text-end"><%#: Eval("FailedAttempts") %></td>
                        </tr>
                    </ItemTemplate>
                </asp:Repeater>
            </tbody>
        </table>
    </div>
    <asp:Label ID="LblNoSessions" runat="server" CssClass="text-muted d-block" Visible="false"
        Text="Todavía no tiene sesiones de entrenamiento." />
</asp:Content>
