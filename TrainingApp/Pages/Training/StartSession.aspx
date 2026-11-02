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

            <%-- Sin área (el admin la quitó durante la Session): aviso, sin guía ni Iniciar (D-168). --%>
            <asp:Label ID="LblNoArea" runat="server" CssClass="alert alert-warning d-block mb-0" Visible="false"
                Text="Aún no tienes un área asignada. Cierra sesión y vuelve a entrar para elegirla, o pide a tu trainer que te la asigne." />

            <asp:PlaceHolder ID="PhStart" runat="server">
            <%-- Los modos son los mismos RadioButton (IDs y GroupName de siempre): la tarjeta es
                 su etiqueta y el radio se opera con el teclado como siempre (D-163). --%>
            <fieldset class="start-modes">
                <legend class="mt-field-label">¿Cómo quieres entrenar?</legend>
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

            <%-- Spec 022 (E8): Guía + Iniciar en la misma fila. El área ya está en la barra (D-286). --%>
            <div class="start-row">
                <div class="mt-field">
                    <asp:Label runat="server" AssociatedControlID="DdlGuide" CssClass="mt-field-label">Guía · <asp:Literal ID="LitGuideCount" runat="server" /></asp:Label>
                    <asp:DropDownList ID="DdlGuide" runat="server" CssClass="form-select" />
                    <asp:Label ID="LblNoGuides" runat="server" CssClass="mt-muted mt-small"
                        Text="No hay guías disponibles para esta área." Visible="false" />
                </div>
                <asp:Button ID="BtnStart" runat="server" Text="Iniciar"
                    CssClass="btn btn-primary start-go" OnClick="BtnStart_Click" />
            </div>
            </asp:PlaceHolder>
        </section>
    </div>
</asp:Content>
