<%@ Page Title="Iniciar Sesión" Language="C#" MasterPageFile="~/UI/master/Site.Master"
    AutoEventWireup="true" CodeBehind="Login.aspx.cs"
    Inherits="TrainingApp.Pages.Auth.Login" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="row justify-content-center mt-5">
        <div class="col-md-4">
            <h2 class="mb-4">MES Training</h2>

            <asp:Label ID="LblError" runat="server" CssClass="alert alert-danger d-block mb-3"
                Visible="false" />

            <div class="mb-3">
                <label for="TxtCode1" class="form-label">Código de empleado</label>
                <asp:TextBox ID="TxtCode1" runat="server" CssClass="form-control" MaxLength="50" />
                <asp:RequiredFieldValidator ID="RfvCode1" runat="server"
                    ControlToValidate="TxtCode1"
                    ErrorMessage="Ingrese su código de empleado."
                    CssClass="text-danger small" Display="Dynamic" />
            </div>

            <div class="mb-4">
                <label for="TxtPassword" class="form-label">Contraseña</label>
                <asp:TextBox ID="TxtPassword" runat="server" TextMode="Password"
                    CssClass="form-control" />
                <asp:RequiredFieldValidator ID="RfvPassword" runat="server"
                    ControlToValidate="TxtPassword"
                    ErrorMessage="Ingrese su contraseña."
                    CssClass="text-danger small" Display="Dynamic" />
            </div>

            <asp:Button ID="BtnLogin" runat="server" Text="Ingresar"
                CssClass="btn btn-primary w-100" OnClick="BtnLogin_Click" />
        </div>
    </div>
</asp:Content>
