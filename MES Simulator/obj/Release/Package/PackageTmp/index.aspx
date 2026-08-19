<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="index.aspx.cs" Inherits="MES_TRAINING.index" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>MES SIMULATOR Login</title>
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
        border: 3px solid #f39c12;
        position:relative;
    }
    
    .btn-login {
        background-color: #f39c12;
        border: none;
        font-weight: 600;
    }
    
    .btn-login:hover {
        background-color: #e67e22;
    }
    
    .form-control:focus {
        border-color: #f39c12;
        box-shadow: 0 0 0 0.2rem rgba(243, 156, 18, 0.25);
    }
    
    .sap-title {
        color: #f39c12;
    }
    
    .support-link {
        color: #f39c12;
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

    /* Estilos del Modal de Áreas */
    .modal-content {
        border: 3px solid #f39c12;
        border-radius: 0.375rem;
    }

    .modal-header {
        background-color: #f39c12;
        color: white;
        border-bottom: none;
    }

    .modal-title {
        font-weight: 600;
    }

    .area-card {
        padding: 30px 20px;
        margin: 10px 0;
        border-radius: 0.375rem;
        background: white;
        cursor: pointer;
        transition: all 0.3s ease;
        border: 2px solid #e0e0e0;
        text-align: center;
    }

    .area-card:hover {
        transform: translateY(-5px);
        box-shadow: 0 8px 20px rgba(243, 156, 18, 0.3);
        border-color: #f39c12;
        background-color: #fff9f0;
    }

    .area-icon {
        font-size: 3rem;
        margin-bottom: 15px;
        color: #f39c12;
    }

    .area-title {
        font-weight: 600;
        font-size: 1.3rem;
        color: #333;
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
        transform: translateY(0px);
      }
    }
    .validationMsg{
        color:red;
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
                        <h1 class="sap-title fw-bold fs-1 mb-4">SAP NetWeaver</h1>
                        
                        <!-- Formulario de Login -->
                        <form id="loginForm"  class="position-relative">
                            <div class="mb-3">
                                <label for="user" class="form-label fw-semibold text-dark">
                                    User <span class="text-danger">*</span>
                                </label>
                                <input type="text" 
                                       class="form-control border-2" 
                                       id="user" 
                                       name="username" 
                                      />
                                  <p class="validationMsg" ></p>
                            </div>
                            
                            <div class="mb-4">
                                <label for="password" class="form-label fw-semibold text-dark">
                                    Password <span class="text-danger">*</span>
                                </label>
                                <input type="password" 
                                       class="form-control border-2" 
                                       id="password" 
                                       name="password" 
                                      />
                                 <p class="validationMsg" ></p>
                            </div>
                            
                            <div class="d-flex justify-content-center mb-3">
                                <button type="submit" 
                                        class="btn btn-login text-white px-4 py-2 rounded" 
                                        id="login_btn">
                                    Ingresar al sistema
                                </button>
                            </div>
                            
                            <div class="text-center">
                                <span class="text-muted small">¿Problemas al entrar en el sistema? </span>
                                <a href="#" class="support-link text-decoration-none small">Soporte</a>
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

                        <!-- Formulario de Restablecimiento de Contraseña -->
                        <div id="resetPasswordPanel" class="hidden">
                          

                            <form id="resetPasswordForm">
                                <div class="mb-3">
                                    <label for="currentPassword" class="form-label fw-semibold text-dark">
                                        Contraseña Actual <span class="text-danger">*</span>
                                    </label>
                                    <input type="password" 
                                           class="form-control border-2" 
                                           id="currentPassword"/>
                                     <p class="validationMsg" ></p>
                                </div>

                                <div class="mb-3">
                                    <label for="newPassword" class="form-label fw-semibold text-dark">
                                       Nueva Contraseña <span class="text-danger">*</span>
                                    </label>
                                    <input type="password" 
                                           class="form-control border-2" 
                                           id="newPassword" 
                                         />
                                     <p class="validationMsg" ></p>
                                </div>

                                <div class="mb-4">
                                    <label for="confirmPassword" class="form-label fw-semibold text-dark">
                                       Confirmar Contraseña <span class="text-danger">*</span>
                                    </label>
                                    <input type="password" 
                                           class="form-control border-2" 
                                           id="confirmPassword" 
                                         
                                          />
                                      <p class="validationMsg" ></p>
                                </div>

                                <div id="resetMessage" class="mb-3 hidden"></div>

                                <div class="d-flex justify-content-center gap-2 mb-3">
                                    <button type="submit" 
                                            class="btn btn-login text-white px-4 py-2 rounded">
                                        Cambiar Contraseña
                                    </button>
                                    <button type="button" 
                                            class="btn btn-secondary px-4 py-2 rounded"
                                            id="cancelReset">
                                        Cancelar
                                    </button>
                                </div>
                            </form>
                        </div>


                         <!-- Modal -->
    <div class="modal fade" id="areasModal" data-bs-backdrop="static" data-bs-keyboard="false" tabindex="-1" aria-labelledby="staticBackdropLabel" aria-hidden="true">
    <div class="modal-dialog modal-lg modal-dialog-centered">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title" id="areasModalLabel">🏢 Debes selecionar tu área:</h5>
            </div>
            <div class="modal-body p-4">
                <div class="row" id="areasBody">
                    <!-- SRC -->
                    <div class="col-md-6 col-lg-4">
                        <div class="area-card" >
                            <div class="area-icon"><i class="bi bi-building"></i></div>
                            <div class="area-title">SRC</div>
                        </div>
                    </div>

                </div>
            </div>
        </div>
    </div>
</div>
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
                this.$resetPasswordPanel = document.getElementById("resetPasswordPanel");
                this.$resetPasswordForm = document.getElementById("resetPasswordForm");
                this.$resetMessage = document.getElementById("resetMessage");
                this.loader = document.getElementById("loader")
                this.areasModal = document.getElementById('areasModal');
                this.modalInstance = this.areasModal ? new bootstrap.Modal(this.areasModal) : null;
                this.buttons = {
                    $login_btn: document.getElementById("login_btn"),
                    $cancelReset: document.getElementById("cancelReset")
                }
                this.inputs = {
                    code1: document.getElementById("user"),
                    password: document.getElementById("password"),
                    currentPassword: document.getElementById("currentPassword"),
                    newPassword: document.getElementById("newPassword"),
                    confirmPassword : document.getElementById("confirmPassword")
                }

                this.area = false
               
                this.init()
            }

            init() {
                this.attachEventListeners()
            }
            //Validations
            isValidPassword(input) {
                let regex = new RegExp(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&.])[A-Za-z\d@$!%*?&.]{8,}$/)

                return regex.test(input.value)

            }
            isValidCode1(input) {
                let regex = new RegExp(/^\d{9}$/)
                return regex.test(input.value)

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
               
                this.$login_form.addEventListener('submit', async e => {
                    e.preventDefault();

                    this.clearValidationMessages()

                    let hasErrors = false;

                    // Validar que el code1 tenga exactamente 9 dígitos
                    if (!this.isValidCode1(this.inputs.code1)) {
                        this.showValidationError(this.inputs.code1, "El usuario debe tener exactamente 9 dígitos numéricos")
                        hasErrors = true;
                    }

                    // Validar que la contraseña no esté vacía
                    if (!this.inputs.password.value || this.inputs.password.value.trim() === '' || !this.isValidPassword(this.inputs.password)) {
                        this.showValidationError(this.inputs.password, "La contraseña debe tener al menos 8 caracteres, incluir mayúsculas, minúsculas, números y caracteres especiales (@$!.%*?&)")
                        hasErrors = true;
                    }

                    // Si hay errores, no continuar
                    if (hasErrors) {
                        return;
                    }

                    let result = await this.getData();
                    //Guardamos el area en seccionStorage

                    // Revisar si existe
                    if (result && this.inputs.password.value == "Sanjose1!") {
                        // Mostrar panel de restablecimiento
                        this.$login_form.classList.add("hidden");
                        this.$resetPasswordPanel.classList.remove("hidden");
                        this.area = result.area

                    } else if (!result && this.inputs.password.value != "Sanjose1!") {
                        swal.fire({
                            title: "Error",
                            text: "Credenciales inválidas",
                            icon: "error"
                        });
                    } else if (!result && this.inputs.password.value == "Sanjose1!") {
                        // Mostrar panel de restablecimiento
                        this.$login_form.classList.add("hidden");
                        this.$resetPasswordPanel.classList.remove("hidden");

                        this.modalInstance?.show();
                    }

                    else if (result) {
                        window.location.href = "/MESSimulator/UI/dashboard/main.aspx";
                        sessionStorage.setItem("isAuth","True")

                    } 
                });

                

                this.$resetPasswordForm.addEventListener('submit', async e => {
                    e.preventDefault();

                    this.clearValidationMessages()

                    let hasErrors = false;

                    // Validar contraseña actual
                    if (this.inputs.currentPassword.value !== "Sanjose1!") {
                        this.showValidationError(this.inputs.currentPassword, "La contraseña actual es incorrecta")
                        hasErrors = true;
                    }

                    // Validar formato de nueva contraseña
                    if (!this.isValidPassword(this.inputs.newPassword)) {
                        this.showValidationError(this.inputs.newPassword, "La contraseña debe tener al menos 8 caracteres, incluir mayúsculas, minúsculas, números y caracteres especiales (@$!%*?&)")
                        hasErrors = true;
                    }

                    // Validar que las contraseñas coincidan
                    if (this.inputs.newPassword.value !== this.inputs.confirmPassword.value) {
                        this.showValidationError(this.inputs.confirmPassword, "Las contraseñas no coinciden")
                        
                        hasErrors = true;
                    }

                    // Si hay errores, no continuar
                    if (hasErrors) {
                        return;
                    }

                    // Si todo está correcto, proceder con la actualización
                    this.updateData()
                });

                //Fyuncion de cancelar el reset 
                this.buttons.$cancelReset.addEventListener('click', e => {
                    e.preventDefault()
                    // Limpiar formulario y volver al login
                    this.$resetPasswordForm.reset();
                    this.$login_form.classList.remove("hidden");
                    this.$resetPasswordPanel.classList.add("hidden");
                })

                this.areasModal.addEventListener("click", e => {
                    const areaCard = e.target.closest(".area-card");

                    if (areaCard) {
                        this.selectArea(areaCard.querySelector(".area-title").textContent)
                        Swal.fire({
                            title: "¿Esta seguro(a)?",
                            text: `Tu area es ${areaCard.querySelector(".area-title").textContent} presiona si para confirmar`,
                            icon: 'warning',
                            showCancelButton: true,
                            confirmButtonColor: '#3085d6',
                            cancelButtonColor: '#d33',
                            confirmButtonText: "Sí",
                            cancelButtonText: "Cancelar",
                        }).then(async (result) => {
                            if (result.isConfirmed) {
                                this.selectArea(areaCard.querySelector(".area-title").textContent)
                                this.modalInstance.hide()
                            }
                        })
                       
                    }

                    
                })



            }
            //Funtions
            async getData() {
           
            this.loader.classList.remove('invisible')
            try {

                var parametros = {
                    code1: this.inputs.code1.value,
                    password: this.inputs.password.value
                };


                let result;

                result = await $.ajax({
                    type: "POST",
                    url: 'index.aspx/One',
                    data: JSON.stringify(parametros),
                    contentType: "application/json; charset=utf-8",
                    dataType: "json",
                })

                var xmlDoc = $.parseXML(result.d);
                var xml = $(xmlDoc);
                var info = xml.find("Info");
                let infoEmployee
                if (info.length > 0) {

                    $(info).each(function () {
   
                        infoEmployee = {
                            code1: $(this).find("code1").text(),
                            fullName: $(this).find("fullName").text(),
                            area: $(this).find("area").text()
                          
                        }

                    })
                    //Quitamos el loader
           
                    return infoEmployee
                }
                return false
            }

            catch (err) {

                console.log(err)

            } finally {
                //Quitamos el loader
                this.loader.classList.add('invisible')
            }



            }
            async updateData() {
                var  parametros = {
                    code1: this.inputs.code1.value,
                    password: this.inputs.confirmPassword.value,
                    area: this.area
                };
               


                

                try {
                    Swal.fire({
                        title: 'Procesando...',
                        text: 'Actualizando contraseña',
                        allowOutsideClick: false,
                        didOpen: () => {
                            Swal.showLoading();
                        }
                    });


                    await $.ajax({
                        type: "POST",
                        url: 'index.aspx/Update',
                        data: JSON.stringify(parametros),
                        contentType: "application/json; charset=utf-8",
                        dataType: "json",
                    });

                    swal.fire({
                        title: "Actualización completa",
                        text: "Tu contraseña se actualizó correctamente",
                        icon: "success"
                    });

                    // Limpiar formulario y volver al login
                    this.$login_form.reset();
                    this.$resetPasswordForm.reset();
                    this.$login_form.classList.remove("hidden");
                    this.$resetPasswordPanel.classList.add("hidden");

                } catch (error) {
                    swal.fire({
                        title: "Error",
                        text: "Ocurrió un error al actualizar la contraseña",
                        icon: "error"
                    });
                }
            }

            selectArea(area) {
                this.area  = area
            }





        }

        document.addEventListener('DOMContentLoaded', () => {
            sessionStorage.clear()
            new login()
        })
    </script>
    
    <script src="UI/master/js/bootstrap.bundle.min.js"></script>
</body>
</html>