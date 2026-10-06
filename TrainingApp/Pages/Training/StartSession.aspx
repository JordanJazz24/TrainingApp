<%@ Page Title="Iniciar Entrenamiento" Language="C#" MasterPageFile="~/UI/master/Site.Master"
    AutoEventWireup="true" CodeBehind="StartSession.aspx.cs"
    Inherits="TrainingApp.Pages.Training.StartSession" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="row justify-content-center mt-5">
        <div class="col-md-5">
            <h3 class="mb-4">Iniciar sesión de entrenamiento</h3>

            <asp:Label ID="LblError" runat="server" CssClass="alert alert-danger d-block mb-3"
                Visible="false" />

            <div class="mb-3">
                <label class="form-label">Producto</label>
                <asp:DropDownList ID="DdlProduct" runat="server" CssClass="form-select"
                    AutoPostBack="true" OnSelectedIndexChanged="DdlProduct_SelectedIndexChanged" />
            </div>

            <div class="mb-3">
                <label class="form-label">Área</label>
                <asp:DropDownList ID="DdlArea" runat="server" CssClass="form-select"
                    AutoPostBack="true" OnSelectedIndexChanged="DdlArea_SelectedIndexChanged" />
            </div>

            <div class="mb-3">
                <label class="form-label">Guía</label>
                <asp:DropDownList ID="DdlGuide" runat="server" CssClass="form-select" />
                <asp:Label ID="LblNoGuides" runat="server" CssClass="text-muted small mt-1"
                    Text="No hay guías disponibles para esta área." Visible="false" />
            </div>

            <div class="mb-4">
                <label class="form-label">Modo</label>
                <div>
                    <asp:RadioButton ID="RbGuided" runat="server" GroupName="Mode"
                        Text=" Guided (práctica, reintentos ilimitados)" Checked="true"
                        CssClass="form-check-input me-1" />
                </div>
                <div class="mt-1">
                    <asp:RadioButton ID="RbAssessment" runat="server" GroupName="Mode"
                        Text=" Assessment (evaluación con puntaje)"
                        CssClass="form-check-input me-1" />
                </div>
            </div>

            <asp:Button ID="BtnStart" runat="server" Text="Iniciar"
                CssClass="btn btn-primary w-100" OnClick="BtnStart_Click" />

            <%-- Salir sin cerrar el navegador (auditoría D-13): en una PC compartida el
                 siguiente operador no debe entrenar con esta identidad. --%>
            <asp:LinkButton ID="BtnLogout" runat="server" Text="Cerrar sesión"
                CssClass="btn btn-link w-100 mt-2" CausesValidation="false"
                OnClick="BtnLogout_Click" />
        </div>
    </div>
</asp:Content>
