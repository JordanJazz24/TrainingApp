<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="loginAdmin.aspx.cs" Inherits="MES_TRAINING.loginAdmin" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>MES SIMULATOR </title>
    <link href="UI/master/css/bootstrap.min.css" rel="stylesheet" />
    <link href="UI/master/css/bootstrap-icons.css" rel="stylesheet" />

    <script src="UI/master/js/jquery-3.6.0.js"></script>
    <script src="UI/master/js/sweetalert2.all.min.js"></script>
    <style>
        /* Estilos mínimos personalizados - Bootstrap maneja el resto */

        body {
            background-color: #f5f5f5;
        }
        
        .login-container {
            max-width: 900px;
            border: 3px solid  #3E436F;
            position:relative;
        }
        
        .btn-login {
            background-color:  #3E436F;
            border: none;
            font-weight: 600;
        }
        
        .btn-login:hover {
            background-color: #ADD8E6;
        }
        
        .form-control:focus {
            border-color:  #3E436F;
            box-shadow: 0 0 0 0.2rem rgba(243, 156, 18, 0.25);
        }
        
        .sap-title {
            color:  #3E436F;
        }
        
        .support-link {
            color:  #3E436F;
        }
        
        .support-link:hover {
            color: #e67e22;
        }
        
        .sap-logo {
            width: 60px;
            height: 30px;
            background: linear-gradient(45deg, #0073e6, #005cb3);
            font-size: 16px;
        }
        
        .bussiness-img {
            max-width: 100%;
            height: auto;
        }
        
        .image-section {
            min-height: 400px;
        }
        
        @media (max-width: 991px) {
            .image-section {
                display: none;
            }
        }

        .hidden {
            display: none;
        }

        .alert {
            border-radius: 0.375rem;
        }

        .validationMsg {
            color: #dc3545;
            font-size: 0.875rem;
            margin-top: 0.25rem;
        }

        /* Developer Info Badge */
        .developer-badge {
            position: fixed;
            top: 20px;
            right: 20px;
            z-index: 1050;
            width: 50px;
            height: 50px;
            background: linear-gradient(135deg,  #3E436F 0%, #0073e6 100%);
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            color: white;
            font-size: 24px;
            cursor: pointer;
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
            transition: all 0.3s ease;
        }

        .developer-badge:hover {
            transform: scale(1.1);
            box-shadow: 0 6px 20px rgba(0, 0, 0, 0.25);
            animation: bellRing 0.5s ease;
        }

        @keyframes bellRing {
            0%, 100% { transform: rotate(0deg) scale(1.1); }
            25% { transform: rotate(15deg) scale(1.1); }
            50% { transform: rotate(-15deg) scale(1.1); }
            75% { transform: rotate(10deg) scale(1.1); }
        }

        .developer-tooltip {
            position: absolute;
            top: 60px;
            right: 0;
            background: white;
            padding: 15px 20px;
            border-radius: 8px;
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
            min-width: 280px;
            opacity: 0;
            visibility: hidden;
            transform: translateY(-10px);
            transition: all 0.3s ease;
            border: 2px solid  #3E436F;
        }

        .developer-badge:hover .developer-tooltip {
            opacity: 1;
            visibility: visible;
            transform: translateY(0);
        }

        .developer-tooltip::before {
            content: '';
            position: absolute;
            top: -8px;
            right: 20px;
            width: 0;
            height: 0;
            border-left: 8px solid transparent;
            border-right: 8px solid transparent;
            border-bottom: 8px solid  #3E436F;
        }

        .developer-tooltip h6 {
            color:  #3E436F;
            margin: 0 0 8px 0;
            font-weight: 600;
            font-size: 14px;
        }

        .developer-tooltip p {
            margin: 0;
            color: #666;
            font-size: 13px;
            line-height: 1.5;
        }

        .developer-tooltip a {
            color: #0073e6;
            text-decoration: none;
            word-break: break-all;
        }

        .developer-tooltip a:hover {
            text-decoration: underline;
        }

        /*Loader*/
        /* From Uiverse.io by BlackisPlay */ 
        #loader{
            position: absolute;
            z-index: 999;
            width: 100%;
            height: 100%;
            top: 0;
            left: 0;
            display: flex;
            align-items: center;
            justify-content: center;
            background: rgba(255, 255, 255, 0.8);
            backdrop-filter: blur(10px);
            -webkit-backdrop-filter: blur(10px);
            border-radius: 0.375rem;
            flex-direction:column;
        }
        
        #loaderText{
            color:#f15a24;
        }

        #loader svg {
            width: 150px;
            height: 150px;
        }
        
        .cls-1 {
            fill: #f15a24;
            animation: cls1 4s ease infinite both;
        }

        @keyframes cls1 {
            0% {
                fill: #f15a24;
            }
            34% {
                fill: #ed1c24;
            }
            66% {
                fill: #f7931e;
            }
            100% {
                fill: #f15a24;
            }
        }

        .cls-1,
        .cls-2,
        .cls-3 {
            stroke-width: 0px;
        }

        .cls-2 {
            fill: #f7931e;
            animation: cls2 4s ease infinite both;
        }

        @keyframes cls2 {
            0% {
                fill: #f7931e;
            }
            34% {
                fill: #f15a24;
            }
            66% {
                fill: #ed1c24;
            }
            100% {
                fill: #f7931e;
            }
        }

        .cls-3 {
            fill: #ed1c24;
            animation: cls3 4s ease infinite both;
        }

        @keyframes cls3 {
            0% {
                fill: #ed1c24;
            }
            34% {
                fill: #f7931e;
            }
            66% {
                fill: #f15a24;
            }
            100% {
                fill: #ed1c24;
            }
        }

        .cls-4 {
            display: none;
        }

        @keyframes triangle {
            0% {
                transform: translateY(0px);
            }
            50% {
                transform: translateY(15px);
            }
            100% {
            }

        }
        .toolTip{
    color: #3E436F
}        
    </style>
</head>
<body>
 

    <div class="container-fluid">
        <div class="login-container mx-auto my-5 bg-white rounded-3 shadow overflow-hidden">
            <div class="row g-0">
                <!-- Sección de imagen -->
                <div class="col-lg-5 d-none d-lg-block">
                    <div class="image-section bg-light p-4 h-100 d-flex align-items-center justify-content-center">
                        <img src="UI/master/../master/imgs/login.jpg" alt="login" class="bussiness-img"/>
                    </div>
                </div>
                
                <!-- Sección del formulario -->
                <div class="col-lg-7">
                    <div class="p-4 p-md-5">
                        <h1 class="sap-title fw-bold fs-1 mb-4">MES SIMULATOR</h1>
                        
                        <!-- Formulario de Login -->
                        <form id="loginForm" class="position-relative">
                            <div class="mb-3">
                                <label for="user" class="form-label fw-semibold text-dark">
                                    User <span class="text-danger">*</span>
                                </label>
                                <input type="text" 
                                       class="form-control border-2" 
                                       id="user" 
                                       name="username" />
                                <p class="validationMsg"></p>
                            </div>
                            
                            <div class="mb-4">
                                <label for="password" class="form-label fw-semibold text-dark">
                                    Password <span class="text-danger">*</span>
                                </label>
                                <input type="password" 
                                       class="form-control border-2" 
                                       id="password" 
                                       name="password" />
                                <p class="validationMsg"></p>
                            </div>
                            
                            <div class="d-flex justify-content-center mb-3">
                                <button type="submit" 
                                        class="btn btn-login text-white px-4 py-2 rounded" 
                                        id="login_btn">
                                    Entrar al sistema
                                </button>
                            </div>
                            
                            <div class="text-center">
                                <span class="text-muted small">¿Problemas al entrar en el sistema? </span>
                                <a href="https://philips.service-now.com/itportal" class="support-link text-decoration-none small">Soporte</a>
                            </div>
                            
                            <!-- From Uiverse.io by BlackisPlay --> 
                            <div id="loader" class="invisible">
                                <svg id="Layer_1" data-name="Layer 1" version="1.1" viewBox="0 0 2000 2000">
                                    <polygon
                                        class="cls-1"
                                        points="928 781 1021 951 784.5 1371.97 1618 1371.97 1530.32 1544 509 1539 928 781"
                                    ></polygon>
                                    <polygon
                                        class="cls-3"
                                        points="1618 1371.97 784.5 1371.97 874.93 1211 1346 1211 923.1 456 1110.06 456 1618 1371.97"
                                    ></polygon>
                                    <g id="Layer_2" data-name="Layer 2">
                                        <polygon
                                            class="cls-2"
                                            points="418 1372.74 509 1539 928 781 1162.32 1211 1346 1211 923.1 456 418 1372.74"
                                        ></polygon>
                                    </g>
                                </svg>
                                <h3 id="loaderText">Cargando...</h3>
                            </div>
                        </form>
                    </div>
                </div>
            </div>
            
            <!-- Footer -->
            <div class="bg-light border-top p-3 px-4 d-flex flex-column flex-md-row justify-content-between align-items-center gap-2">
                <div class="text-muted small">
                    Copyright © SAP AG. Todos los derechos reservados.
                </div>
                <div class="sap-logo rounded d-flex align-items-center justify-content-center text-white fw-bold">
                    SAP
                </div>
            </div>
        </div>
    </div>
    
    <!-- Funcionalidad del login -->
    <script>
        class login {
            constructor() {
                this.$login_form = document.getElementById("loginForm");
                this.loader = document.getElementById("loader");
                this.buttons = {
                    $login_btn: document.getElementById("login_btn")
                };
                this.inputs = {
                    code1: document.getElementById("user"),
                    password: document.getElementById("password")
                };

                this.init();
            }

            init() {
                this.attachEventListeners();
            }

            //Validations
            isValidCode1(input) {
                let regex = new RegExp(/^\d{9}$/);
                return regex.test(input.value);
            }

            clearValidationMessages() {
                document.querySelectorAll('.validationMsg').forEach(msg => {
                    msg.textContent = '';
                });
            }

            showValidationError(input, message) {
                const msgElement = input.nextElementSibling;
                if (msgElement && msgElement.classList.contains('validationMsg')) {
                    msgElement.textContent = message;
                }
            }

            //Event Listeners
            attachEventListeners() {
                this.$login_form.addEventListener('submit', async (e) => {
                    e.preventDefault();

                    this.clearValidationMessages();

                    let hasErrors = false;

                    // Validar que el code1 tenga exactamente 9 dígitos
                    if (!this.isValidCode1(this.inputs.code1)) {
                        this.showValidationError(this.inputs.code1, "El usuario debe tener exactamente 9 dígitos numéricos");
                        hasErrors = true;
                    }

                    // Aquí puedes agregar más validaciones o lógica de envío
                    if (!hasErrors) {
                        // Mostrar loader
                        this.loader.classList.remove('invisible');
                        let parametros = {
                            Code1: this.inputs.code1.value,
                            Password: this.inputs.password.value

                        }

                        $.ajax({
                            type: "POST",
                            url: 'loginAdmin.aspx/UserAuthentication',
                            data: JSON.stringify(parametros),
                            contentType: "application/json; charset=utf-8",
                            dataType: "json",
                            async: true,
                            success: (data) => {
                                if (data.d === true) {

                                    this.signin()
                                }
                                else {
                                    Swal.fire({
                                        icon: 'error',
                                        title: 'Error',
                                        text: 'Code1 o contraseña incorrectas.',
                                        timer: 3000
                                    })
                                    this.loader.classList.add("invisible")
                                    this.$login_form.reset()
                                }

                            },
                            error: (result) => {
                                this.loader.classList.add('invisible');
                                Swal.fire({
                                    icon: 'error',
                                    title: 'Error',
                                    text: 'Error inesperado, contactar a soporte IT.',
                                    timer: 3000
                                })
                            }
                        });
                    }
                });
            }

            //Functions
            signin() {
                sessionStorage.setItem("isAuthAdmin", "True")
                window.location.href = "/UI/DashboardAdmin/home.aspx"
                //    "/MESSimulator/UI/DashboardAdmin/home.aspx"
            }
        }

        // Inicializar la clase cuando el DOM esté listo
        document.addEventListener('DOMContentLoaded', () => {
            new login();
        });
    </script>
    
    <script src="UI/master/js/bootstrap.bundle.min.js"></script>

</body>
</html>