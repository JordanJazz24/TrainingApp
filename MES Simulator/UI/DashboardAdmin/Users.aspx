<%@ Page Title="" Language="C#" MasterPageFile="~/UI/master/trainingAdmin.Master" AutoEventWireup="true" CodeBehind="Users.aspx.cs" Inherits="MES_TRAINING.UI.DashboardAdmin.main" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="main-content">
        <div class="content-area p-3 h-100 overflow-hidden">
            <div class="card h-100 border-0 rounded shadow-sm d-flex flex-column">
                <div class="card-header bg-white border-bottom p-3">
                    <h4 class="mb-0 text-primary-custom">
                        <i class="bi bi-people"></i> Gestión de Usuarios
                    </h4>
                </div>
                <div class="card-body p-3 flex-grow-1 overflow-hidden d-flex flex-column">
                    <div class="table-container flex-grow-1 overflow-auto position-relative">
                        <table id="table" data-search="true" data-pagination="true"
                            class="table table-striped table-hover mb-0" data-sort-order="asc">
                            <thead>
                                <tr>
                                    <th data-field="Code1" data-sortable="true" class="text-center">Code1</th>
                                    <th data-field="Nombre" data-sortable="true">Nombre</th>
                                    <th data-field="Apellido" data-sortable="true">Apellido</th>
                                    <th data-field="Area" data-sortable="true">Área</th>
                                    <th data-field="Password" class="text-center">Acciones</th>
                                </tr>
                            </thead>
                            <tbody id="cuerpoTb">
                            </tbody>
                        </table>
                        
                        <!-- Loader -->
                        <div id="loader" class="invisible d-flex flex-column align-items-center justify-content-center rounded">
                            <svg id="Layer_1" data-name="Layer 1" version="1.1" viewBox="0 0 2000 2000">
                                <polygon class="cls-1" points="928 781 1021 951 784.5 1371.97 1618 1371.97 1530.32 1544 509 1539 928 781"></polygon>
                                <polygon class="cls-3" points="1618 1371.97 784.5 1371.97 874.93 1211 1346 1211 923.1 456 1110.06 456 1618 1371.97"></polygon>
                                <g id="Layer_2" data-name="Layer 2">
                                    <polygon class="cls-2" points="418 1372.74 509 1539 928 781 1162.32 1211 1346 1211 923.1 456 418 1372.74"></polygon>
                                </g>
                            </svg>
                            <h3 id="loaderText" class="mt-3">Cargando...</h3>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
    <script src="../DashboardAdmin/js/main.v1.0.0.js"></script>
</asp:Content>