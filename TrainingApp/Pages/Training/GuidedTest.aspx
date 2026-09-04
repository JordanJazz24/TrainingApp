<%@ Page Title="Entrenamiento" Language="C#" MasterPageFile="~/UI/master/Site.Master"
    AutoEventWireup="true" CodeBehind="GuidedTest.aspx.cs"
    Inherits="TrainingApp.Pages.Training.GuidedTest" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="container mt-4">

        <%-- Header de sesión --%>
        <div class="d-flex justify-content-between align-items-center mb-3">
            <div>
                <h4 class="mb-0"><asp:Literal ID="LitGuideTitle" runat="server" /></h4>
                <small class="text-muted">Modo: <strong><asp:Literal ID="LitMode" runat="server" /></strong></small>
            </div>
            <asp:Button ID="BtnAbandon" runat="server" Text="Abandonar"
                CssClass="btn btn-outline-danger btn-sm"
                OnClientClick="return confirm('¿Estás seguro de que quieres abandonar esta sesión? El progreso se perderá.');"
                OnClick="BtnAbandon_Click" />
        </div>

        <%-- Progreso y score --%>
        <div class="d-flex justify-content-between mb-4">
            <span class="badge bg-secondary fs-6">
                <asp:Literal ID="LitProgress" runat="server" />
            </span>
            <span>
                Puntaje actual: <strong><asp:Literal ID="LitScore" runat="server" /></strong>
            </span>
        </div>

        <%-- Panel del paso activo --%>
        <asp:Panel ID="PnlStep" runat="server">
            <div class="card">
                <div class="card-header">
                    <span class="text-muted small">Tipo de paso:</span>
                    <strong> <asp:Literal ID="LitStepType" runat="server" /></strong>
                </div>
                <div class="card-body">
                    <div class="mb-3">
                        <label class="form-label">Respuesta</label>
                        <asp:TextBox ID="TxtAnswer" runat="server" CssClass="form-control" />
                    </div>
                    <asp:Button ID="BtnSubmit" runat="server" Text="Enviar"
                        CssClass="btn btn-primary" OnClick="BtnSubmit_Click" />
                </div>
            </div>

            <%-- Feedback --%>
            <asp:Panel ID="PnlFeedback" runat="server" Visible="false" CssClass="mt-3">
                <asp:Label ID="LblFeedback" runat="server" />
                <asp:Panel ID="PnlHint" runat="server" Visible="false"
                    CssClass="alert alert-info mt-2">
                    <strong>Pista:</strong> <asp:Literal ID="LitHint" runat="server" />
                </asp:Panel>
                <asp:Button ID="BtnContinue" runat="server" Text="Continuar al siguiente paso"
                    CssClass="btn btn-warning mt-2" Visible="false"
                    OnClick="BtnContinue_Click" />
            </asp:Panel>
        </asp:Panel>

        <%-- Panel de sesión completada --%>
        <asp:Panel ID="PnlCompleted" runat="server" Visible="false" CssClass="text-center mt-5">
            <div class="card border-success">
                <div class="card-body">
                    <h3 class="text-success">¡Sesión completada!</h3>
                    <p class="fs-4 mt-3">
                        Puntaje final: <strong><asp:Literal ID="LitFinalScore" runat="server" /></strong>
                    </p>
                    <asp:HyperLink ID="LnkNewSession" runat="server"
                        NavigateUrl="~/Pages/Training/StartSession.aspx"
                        CssClass="btn btn-primary mt-2">
                        Iniciar nueva sesión
                    </asp:HyperLink>
                </div>
            </div>
        </asp:Panel>

    </div>
</asp:Content>
