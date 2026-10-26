<%@ Page Title="Iniciar Entrenamiento" Language="C#" MasterPageFile="~/UI/master/Site.Master"
    AutoEventWireup="true" CodeBehind="StartSession.aspx.cs"
    Inherits="TrainingApp.Pages.Training.StartSession" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="start-wrap">
        <div class="admin-page-head">
            <div>
                <h1 class="admin-page-title">Iniciar sesión de entrenamiento</h1>
                <%-- Último acceso anterior (spec 013): si no fue el operador, avisa al trainer. --%>
                <asp:Label ID="LblLastAccess" runat="server" CssClass="admin-page-lead d-block" />
            </div>
        </div>

        <section class="mt-hero start-card" aria-label="Iniciar sesión de entrenamiento">
            <asp:Label ID="LblError" runat="server" CssClass="alert alert-danger d-block mb-4"
                Visible="false" />

            <div class="start-fields">
                <div class="mt-field">
                    <asp:Label runat="server" AssociatedControlID="DdlProduct" CssClass="mt-field-label">Producto</asp:Label>
                    <asp:DropDownList ID="DdlProduct" runat="server" CssClass="form-select"
                        AutoPostBack="true" OnSelectedIndexChanged="DdlProduct_SelectedIndexChanged" />
                </div>

                <div class="mt-field">
                    <asp:Label runat="server" AssociatedControlID="DdlArea" CssClass="mt-field-label">Área</asp:Label>
                    <asp:DropDownList ID="DdlArea" runat="server" CssClass="form-select"
                        AutoPostBack="true" OnSelectedIndexChanged="DdlArea_SelectedIndexChanged" />
                </div>

                <div class="mt-field">
                    <asp:Label runat="server" AssociatedControlID="DdlGuide" CssClass="mt-field-label">Guía</asp:Label>
                    <asp:DropDownList ID="DdlGuide" runat="server" CssClass="form-select" />
                    <asp:Label ID="LblNoGuides" runat="server" CssClass="mt-muted mt-small"
                        Text="No hay guías disponibles para esta área." Visible="false" />
                </div>
            </div>

            <%-- Los modos son los mismos RadioButton (IDs y GroupName de siempre): la tarjeta es
                 su etiqueta y el radio se opera con el teclado como siempre (D-163). --%>
            <fieldset class="start-modes">
                <legend class="mt-field-label">Modo</legend>
                <label class="start-mode">
                    <asp:RadioButton ID="RbGuided" runat="server" GroupName="Mode" Checked="true" />
                    <i class="bi bi-signpost-2" aria-hidden="true"></i>
                    <span class="start-mode-name">Guided</span>
                    <span class="start-mode-line">Paso a paso con la guía</span>
                </label>
                <label class="start-mode">
                    <asp:RadioButton ID="RbFreePractice" runat="server" GroupName="Mode" />
                    <i class="bi bi-lightbulb" aria-hidden="true"></i>
                    <span class="start-mode-name">Práctica libre</span>
                    <span class="start-mode-line">Sin guía ni puntaje, con pistas</span>
                </label>
                <label class="start-mode">
                    <asp:RadioButton ID="RbAssessment" runat="server" GroupName="Mode" />
                    <i class="bi bi-clipboard-check" aria-hidden="true"></i>
                    <span class="start-mode-name">Assessment</span>
                    <span class="start-mode-line">Evaluación con puntaje</span>
                </label>
            </fieldset>

            <asp:Button ID="BtnStart" runat="server" Text="Iniciar"
                CssClass="btn btn-primary w-100" OnClick="BtnStart_Click" />
        </section>
    </div>
</asp:Content>
