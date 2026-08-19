<%@ Page Title="" Language="C#" MasterPageFile="~/UI/master/trainingAdmin.Master" AutoEventWireup="true" CodeBehind="home.aspx.cs" Inherits="MES_TRAINING.UI.DashboardAdmin.home" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

     <!-- Main Content -->
            <div class="border p-2 m-2 rounded-3 " id="homeContent">
                <h1 class="page-title">Home</h1>

                <section>
                    <!-- Introduction Card -->
<div class="info-card">
    <h5>Introduction</h5>
    <p>System of security  that allows users to recover account access to Training MES when they forget their password.</p>
</div>

<!-- Service Now Portal Card -->
<div class="info-card">
    <h5>Service Now Portal</h5>
    <p>To rise a request ticket for any issues about the application please <a href="https://philips.service-now.com/itportal" target="_blank">visit Service Now Portal</a></p>
</div>

<!-- Point of Contact Card -->
<div class="info-card">
    <h5>Point Of Contact</h5>
    <p>Marianela.Zumbado@philips.com</p>
</div>

<!-- Administrator Card -->
<div class="info-card">
    <h5>Administrator/Software Owner</h5>
    <p>Marianela Zumbado</p>
</div>
                </section>
            </div>
       
</asp:Content>
