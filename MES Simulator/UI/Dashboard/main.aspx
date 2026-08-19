<%@ Page Title="" Language="C#" MasterPageFile="~/UI/master/master.Master" AutoEventWireup="true" CodeBehind="main.aspx.cs" Inherits="MES_TRAINING.UI.main.main" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

    <!-- Main Container -->
<div class="overflow-hidden col-10 p-0 ps-1">
    <!-- Table Section -->
    <div class="h-100" id="table_container">
        <div class="main-content h-100 d-flex flex-column">
            <div class="work-list-header d-flex p-3">
                <h6 class="flex-grow-1 mb-0 fw-semibold">Lista de trabajo POD</h6>
                <i class="fas fa-question-circle"></i>
            </div>
            
            <!-- Esta es la clave: table-responsive con flex-grow-1 y overflow-auto -->
            <div class="table-responsive flex-grow-1 overflow-auto">
                <table class="table work-table mb-0 table-bordered border-secondary-subtle ">
                    <thead class="table-secondary position-sticky top-0">
                        <tr>
                            <th>CP</th>
                            <th>Descripción material</th>
                            <th>Cantidad</th>
                            <th>Estado</th>
                            <th>Lote proc.</th>
                            <th>Orden de fabricación</th>
                        </tr>
                    </thead>
                    <tbody id="tableWorkContainer">
                      
                    </tbody>
                </table>
            </div>
        </div>
    </div>
</div>


  

     <!-- MODAL RESOURCE SETUP -->
     <section class="modal fade" id="Resource-setup-modal" data-bs-backdrop="static" data-bs-keyboard="false" tabindex="-1" aria-hidden="true">
         <section class="modal-dialog">
             <div class="modal-content">
                 <!-- Header -->
                 <div class="modal-header">
                     <h5 class="modal-title">Resource SetUp</h5>
                     <button type="button" class="btn-close" data-bs-dismiss="modal" id="btn_close" aria-label="Close"></button>
                 </div>

             
                 
                 <!--END Alertas -->

                 <!-- Info section -->
                 <div class="info-section">

                     <article class="d-flex gap-2 my-2  align-items-center position-fixed invisible" id="record_change">
                           <i class="fa-solid fa-square-check text-success"></i>
                           <h6 class="mb-0 text-success">Record Change</h6>
                     </article>
                     <div class="row g-2">
                         <div class="col-12 text-center">
                             <div class="resource-info">
                                 <strong>* Site:</strong> CR9A
                             </div>
                         </div>
                         <div class="col-md-9 col-12 text-md-end text-center">
                             <div class="resource-info">
                                 <strong>* Resource:</strong> R_SRC_MOLDE_10021514
                                 <i class="fas fa-copy ms-2" style="cursor: pointer;" title="Copy"></i>
                             </div>
                         </div>
                         <div class="col-md-3 col-12 text-md-start text-center">
                             <button class="btn btn-outline-primary btn-sm" id="btnRetrieve" >Retrieve</button>
                         </div>
                         <div class="col-12 text-center mt-2">
                             <div class="resource-info">
                                 <strong>* Setup Status:</strong>
                                 <select name="select-status" id="statusSelect" class="d-inline-block w-auto ms-2">
                                     <option value="Open">Open</option>
                                     <option value="setUp">SetUp</option>
                                 </select>
                             </div>
                         </div>
                     </div>
                 </div>

                 <!-- Toolbar -->
                 <div class="toolbar-section">
                     <button class="toolbar-btn " id="btnInsertNew" disabled>INSERT NEW</button>
                     <button class="toolbar-btn" id="btnInsertBefore" disabled>INSERT BEFORE</button>
                     <button class="toolbar-btn"id="btnInsertAfter" disabled>INSERT AFTER</button>
                     <button class="toolbar-btn " id="btnDeleteSelected" disabled>DELETE SELECTED</button>
                     <button class="toolbar-btn " id="btnDeleteAll" disabled>DELETE ALL</button>
                 </div>

                 <!-- Table headers -->
                 <div class="table-headers">
                     <div class="row g-0">
                         <div class="col-6 border-end text-center py-2">
                             Setup Type
                         </div>
                         <div class="col-6 text-center py-2">
                             Setup Type Value
                         </div>
                     </div>
                 </div>

                 <!-- $resourceContainer -->
                 <section class="content-area invisible h-100" id="resourceContainer">
                    
                 </section>

                 <!-- Footer -->
                 <div class="modal-footer">
                     <button type="button" class="btn btn-primary btn-sm" id="btnResourceSave" disabled>Save</button>
                     <button type="button" class="btn btn-outline-secondary btn-sm" id="btn_cancel" data-bs-dismiss="modal">Cancel</button>
                 </div>
             </div>
         </section>
     </section>
     <!--END MODAL RESOURCE SETUP -->

 

 

<!-- ========================================
   MODAL: LINE CLEARANCE
   Colección de datos independiente
   ======================================== -->
<div class="modal fade" id="modalColeccionDatos" tabindex="-1">
    <div class="modal-dialog">
        <div class="modal-content  mx-auto" id="lineclearanceCollect">
            <!-- Header -->
            <div class="modal-header border-bottom border-4 border-primary">
                <h5 class="modal-title">Colección de datos independiente</h5>
                <button type="button" class="close-btn" data-bs-dismiss="modal">
                    <i class="fas fa-times text-light" ></i>
                </button>
            </div>

            <!-- Body -->
            <div class="modal-body">
                <!-- Filters Section -->
                <div class="filter_section border-0">
                    <!-- Save Message -->
                    <div class=" d-flex align-items-center gap-2 invisible" id="saveLineSms">
                        <img src="../master/imgs/lineClearence-save-icon.png" alt="save">
                        <h6 class="mb-0">1 DC group saved</h6>
                    </div>

                    <!-- Filter Operation -->
                    <div class="row align-items-center mb-3">
                        <div class="col-3 text-end">
                            <span class="filter-label">Filtrar operación:</span>
                        </div>
                        <div class="col-6">
                            <div class="input-group input-group-sm">
                                <input type="text" class="form-control" value="Q_SRC_DWF..." readonly>
                                <button class="btn btn-outline-secondary" type="button">
                                    <i class="fas fa-copy"></i>
                                </button>
                            </div>
                        </div>
                        <div class="col-3">
                            <button class="btn btn-collection btn-sm w-100">
                                <img src="../master/imgs/anular-icon.png" alt="anular"> Anular
                            </button>
                        </div>
                    </div>

                    <!-- Collect Data For -->
                    <div class="row align-items-center mb-3">
                        <div class="col-3 text-end">
                            <span class="red-asterisk me-1">*</span>
                            <span class="filter-label">Recoger datos para:</span>
                        </div>
                        <div class="col-2">
                            <select class="form-select form-select-sm">
                                <option selected>Recurso</option>
                            </select>
                        </div>
                        <div class="col-4">
                            <div class="input-group input-group-sm">
                                <input type="text" class="form-control" value="R_SRC_MOL..." readonly>
                                <button class="btn btn-outline-secondary" type="button">
                                    <i class="fas fa-copy"></i>
                                </button>
                            </div>
                        </div>
                        <div class="col-3">
                            <button class="btn btn-collection btn-sm btn_retrieve w-100" id="btnLineRetrieve" type="button">
                                <img src="../master/imgs/retrieve-icon.png" alt="retrieve"> Retrieve
                            </button>
                        </div>
                    </div>
                </div>

                <!-- Data Grid -->
                <div class="row invisible " id="lineCollectRow">
                    <div class="col-12">
                        <div class="data-grid">
                            <div class="grid-header">
                                <div class="row">
                                    <div class="col-5">
                                        <h6>DC_LC_DONE</h6>
                                        <h6>¿Realizó la Limpieza de línea?</h6>
                                    </div>
                                    <div class="col-4">
                                        <h6>Grupo/Versión CD: DCGTM_LC/A</h6>
                                        <h6 class="text-secondary" id="line_clearence_date">27 may 2025 00:32:52</h6>
                                    </div>
                                    <div class="col-3 d-flex align-items-center justify-content-end">
                                        <button class="btn btn-collection  w-100" id="btnCollectLine" type="button">
                                            <img src="../master/imgs/record-icon.png" alt="record"> Collect
                                        </button>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
              
            <!-- Footer -->
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary btn-sm btn_close_lineClearance_bottom" data-bs-dismiss="modal">Cerrar</button>
            </div>
                      
        </div>
                                <!-- Line Clearance Question SubModal -->
<article class="modal-content d-none" id="lineClearanceQuestion">

    <!-- Header -->
<div class="modal-header border-bottom border-4 border-primary">
    <h5 class="modal-title">Entrada de colección de datos</h5>
    <button type="button" class="close-btn" data-bs-dismiss="modal">
        <i class="fas fa-times" id="closeLineQuestion"></i>
    </button>
</div>
   

    <h1 class="container-title my-2 ps-2">Entrada de colección de datos</h1>

    <!-- Body -->
    <section class="container-body p-1 ">
        <!-- Resource Info -->
        <section class="resource-info bg-secondary-subtle d-flex">
            <div class="resource-info__item me-3">
                <span class="resource-info__label ">Grupo CD:</span>
                <span class="resource-info__value">DCGTM_LC/A</span>
            </div>
            <div class="resource-info__item">
                <span class="resource-info__label">Recurso:</span>
                <span class="resource-info__value">R_SRC_MOLDE_10021514</span>
            </div>
        </section>

        <!-- Form -->
        <section id="dataCollectionForm" >
            <fieldset class="form-fieldset">
                <div class="form-section border-bottom">
                    <div class="d-flex">
                        <span class="text-danger">*</span>
                        <h6 >¿Se ejecutó la limpieza de línea?:</h6>
                    </div>
                    <div class="row align-items-center">
                        <div class="col-12 mb-2">
                            
                            <select class="form-select w-75" id="selectLineQuestion">
                                <option value="">Seleccione una opción</option>
                                <option value="true">Sí</option>
                                <option value="false">No</option>
                            </select>
                        </div>
                        <div class="col-6">
                            <div class="data-type">
                                <span class="text-muted">Tipo de datos:</span> Booleano
                            </div>
                        </div>
                        <div class="col-6 text-end">
                            <div class="btn-collection p-1 rounded d-inline-flex align-items-center gap-2">
                                <span id="fileCount">0</span>
                                <span>ficheros añadidos</span>
                            </div>
                        </div>
                    </div>
                </div>
            </fieldset>
        </section>
    </section>

    <!-- Footer -->
    <footer class="container-footer d-flex justify-content-center gap-2">
        <button  class="action-btn action-btn--primary p-3" type="button" id="btnLineSave">
            <i class="fa-solid fa-floppy-disk text-primary"></i>
            Save
        </button>
        <button type="button" class="action-btn action-btn--secondary p-3" data-bs-dismiss="modal"" >
            <i class="fa-solid fa-xmark text-danger"></i>
            Close
        </button>
    </footer>
</article>
    </div>


</div>

<!-- ========================================
   MODAL: SFC SPLIT
   División de cantidades de SFC
   ======================================== -->
<div class="modal fade" id="modalSplit" tabindex="-1">
    <div class="modal-dialog">
        <div class="modal-content">
            <!-- Header -->
            <div class="modal-header p-0 border-0">
                <h5 class="modal-title pt-3 ps-2">SFC Split Plug-in (Auto ID)</h5>
                <button class="btn-close me-2" data-bs-dismiss="modal"></button>
            </div>

            <!-- Body -->
            <div class="modal-body pt-1 position-relative">



                <article class="row">
                    <!-- Subtitle -->
                    <div class="row subtittle align-items-center mb-3 ms-0">
                        <h6 class="col-11 mb-0">SFC Split</h6>
                        <div class="col-1 text-end">
                            <i class="fa-regular fa-circle-question"></i>
                        </div>
                    </div>

                    <!-- Info Section -->
                    <div class="col-12 d-flex flex-column ">
                        <!-- Success Message -->
                        <div class="message_inf d-flex align-items-center gap-2 visually-hidden">
                            <i class="fa-solid fa-square-check text-success"></i>
                            <h6 class="text-sfc mb-0">New SFC CR9A05637090 created with quantity 1</h6>
                        </div>

                       

                        <!-- Info Fields -->
                        <div class="info-section p-3">
                            <div class="row g-2 mt-2">
                                <div class="col-12">
                                    <div class="resource-info">
                                        <strong>CP a dividir:</strong> CR9A05637090
                                    </div>
                                </div>
                                <div class="col-12">
                                    <div class="resource-info">
                                        <strong>Cantidad disponible:</strong> 
                                        <span id="splitQuantity"></span>
                                    </div>
                                </div>
                                <div class="col-12">
                                    <div class="resource-info"  id="splitInfo">
                                        <strong class="text-danger">*</strong>
                                        <strong>Quantity Split:</strong>
                                        <input type="text" id="splitValue" class="form-control d-inline-block w-auto ms-2">
                                    </div>
                                </div>
                            </div>

                            <!-- Action Buttons -->
                            <div class="d-flex justify-content-center gap-2 mt-3">
                                <button type="button" class="btn btn-outline-primary" id="splitBtn">Split</button>
                                <button type="button" class="btn btn-outline-primary" id="btnCloseSplit" data-bs-dismiss="modal">Close</button>
                            </div>
                        </div>
                    </div>
                </article>
            </div>
        </div>
    </div>
</div>


    <!-- ========================================
   MODAL: LOG NC (NON-CONFORMANCE)
   Registro de no conformidades con paginación
   ======================================== -->
<div class="modal fade" id="log_NC_Modal" tabindex="-1">
    <div class="modal-dialog">
        <!-- ========== VISTA 1: SELECCIÓN DE NC ========== -->
        <div class="modal-content m-auto" id="scrap_types">
            <!-- Header -->
            <div class="modal-header">
                <h5 class="modal-title">NC Selection</h5>
                <button type="button" class="btn-close"  data-bs-dismiss="modal" aria-label="Close"></button>
            </div>

            <!-- Body: NC Container con scroll horizontal -->
            <section class="modal-body" id="log_NC_container">
           
            </section>

            <!-- Footer: Paginación -->
            <div class="modal-footer justify-content-center">
                <nav aria-label="NC Pagination">
                    <ul class="pagination mb-0">
                        <li class="page-item">
                            <a class="page-link" href="#1" id="Previous_link" data-value="1" aria-label="Previous">
                                <span aria-hidden="true">&laquo;</span> Previous
                            </a>
                        </li>
                        <li class="page-item"><a class="page-link" href="#1" id="page_1">1</a></li>
                        <li class="page-item"><a class="page-link" href="#2" id="page_2">2</a></li>
                        <li class="page-item"><a class="page-link" href="#3" id="page_3">3</a></li>
                        <li class="page-item"><a class="page-link" href="#4" id="page_4">4</a></li>
                        <li class="page-item"><a class="page-link" href="#5" id="page_5">5</a></li>
                        <li class="page-item">
                            <a class="page-link" href="#2" id="next_link" data-value="2" aria-label="Next">
                                Next <span aria-hidden="true">&raquo;</span>
                            </a>
                        </li>
                    </ul>
                </nav>
            </div>
        </div>

        <!-- ========== VISTA 2: REGISTRO DE NC (Hidden) ========== -->
        <div class="modal-content m-auto visually-hidden" id="scrap_selected">
            <!-- Header -->
            <div class="modal-header">
                <h5 class="modal-title">Log NC</h5>
                <button type="button" class="btn-close" id="close_log_nc" data-bs-dismiss="modal" aria-label="Close"></button>
            </div>

            <!-- Body -->
            <section class="modal-body log_section">
                <!-- SFC Info -->
                <article class="border-bottom border-2 pb-3 mb-3">
                    <div class="d-flex align-items-center gap-2">
                        <h6 class="mb-0 text-secondary fw-normal">SFC:</h6>
                        <strong class="fw-semibold fs-6">CR9A053631091</strong>
                    </div>
                </article>

                <!-- NC Form Content -->
                <div class="content_nc">
                    <section id="nc_form">
                        <!-- NC Code Field -->
                        <div class="row mb-3">
                            <div class="col-12">
                                <div class="d-flex justify-content-center align-items-center gap-3">
                                    <label for="nc_code" class="mb-0 text-nowrap">
                                        <span class="text-danger fw-bold">*</span> NC Code:
                                    </label>
                                    <input 
                                        type="text" 
                                        id="nc_code" 
                                        class="form-control input_nc fw-bold" 
                                       
                                        placeholder="Enter NC code"
                                        style="max-width: 250px;"
                                    >
                                </div>
                            </div>
                        </div>

                        <!-- Comment Field -->
                        <div class="row mb-3">
                            <div class="col-12">
                                <div class="d-flex justify-content-center align-items-center gap-3">
                                    <label for="nc_comment" class="mb-0 text-nowrap">Comment:</label>
                                    <input 
                                        type="text" 
                                        id="nc_comment" 
                                        class="form-control"
                                        placeholder="Add optional comment"
                                        style="max-width: 350px;"
                                    >
                                </div>
                            </div>
                        </div>
                    </section>
                </div>
            </section>

            <!-- Footer: Action Buttons -->
            <div class="modal-footer justify-content-center">
                <div class="d-flex flex-wrap gap-2 justify-content-center">
                    <button type="button" class="btn-collection_nc d-flex align-items-center gap-2" id="btn_nc_add">
                        <i class="fa-solid fa-circle-check text-success"></i>
                        <span>Add</span>
                    </button>
                    
                    <button 
                        type="button" 
                        class="btn-collection_nc d-flex align-items-center gap-2" 
                        id="add_done" 
                        data-bs-dismiss="modal"
                    >
                        <i class="fa-solid fa-circle-check text-success"></i>
                        <span>Add Done</span>
                    </button>
                    
                    <button type="button" class="btn-collection_nc d-flex align-items-center gap-2" id="btn_nc_return">
                        <i class="fa-solid fa-arrow-left text-primary"></i>
                        <span>Return POD</span>
                    </button>
                    
                    <button type="button" class="btn-collection_nc d-flex align-items-center gap-2" id="btn_nc_finish">
                        <i class="fa-solid fa-flag-checkered text-success"></i>
                        <span>Finished</span>
                    </button>
                </div>
            </div>
        </div>
    </div>
</div>

    <!-- ========================================
   MODAL: COMPLETE SECTION
   Sección de completado
   ======================================== -->
<div class="modal fade" id="modalCompleteSection" tabindex="-1">
    <div class="modal-dialog modal-xl">
        <div class="modal-content">
            <!-- Header -->
            <div class="modal-header border-bottom border-4 border-primary">
                <h5 class="modal-title">Colección de datos independiente</h5>
                <button type="button" class="close-btn">
                    <i class="fas fa-times"></i>
                </button>
            </div>

            <!-- Error Messages -->
            <section class="complete_error_empy visually-hidden">
                <article class="sms_error d-flex align-items-center gap-2 my-2 mx-3">
                    <i class="fa-solid fa-circle-exclamation text-danger"></i>
                    <h6 class="mb-0 text-danger">Solo se permiten números</h6>
                </article>
            </section>

            <section class="complete_error_selects visually-hidden">
                <article class="sms_error d-flex align-items-center gap-2 my-2 mx-3">
                    <i class="fa-solid fa-circle-exclamation text-danger"></i>
                    <h6 class="mb-0 text-danger">Debes responder las preguntas con (SI)</h6>
                </article>
            </section>

            <!-- Body: First Section -->
            <section class="modal-body first_section lc_questions" data-position="1">
                <div class="ps-2 fw-semibold">
                    <span class="text-muted">Group CD:</span> DCGTM_LC_MOLDEO/A
                </div>

                <section class="border" id="completeLcQuestions" >
                   
                    
                </section>
            </section>

            <!-- Body: Parameters Section (Hidden) -->
            <section class="modal-body border overflow-y-auto visually-hidden" 
                data-position="2" id="completeParameters">
        
            </section>

            <!-- Footer -->
            <div class="modal-footer">
                <div class="d-flex justify-content-center gap-3 flex-wrap w-100">
                    <button type="button" class="btn-collection" id="btnCompleteSave">
                        <i class="fa-solid fa-floppy-disk text-primary"></i> Save
                    </button>
                    <button type="button" class="btn-collection" >
                        <i class="fa-solid fa-floppy-disk text-primary"></i> Save Draft
                    </button>
                    <button type="button" class="btn-collection" id="btnCompleteCancel">
                        <img src="../master/imgs/anular-icon.png" alt="cancel"> Cancel
                    </button>
                    <button type="button" class="btn-collection" id="btnCompleteReturn">
                        <i class="fa-solid fa-xmark text-danger"></i> Return to POD
                    </button>
                </div>
            </div>
        </div>
    </div>
</div>

<!-- ========================================
   MODAL: COMPLETE CONFIRM (LOG BUYOFF)
   Confirmación de completado con login
   ======================================== -->
<div class="modal fade " id="complete_confirm" tabindex="-1">
    <div class="modal-dialog">
        <div class="modal-content position-relative">
            <!-- Header -->
            <div class="modal-header">
                <h5 class="modal-title fw-bold">
                    <i class="bi bi-search me-2"></i>
                    Log Buyoff
                </h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal" id="btn_close_confirm"></button>
            </div>

            <!-- Body: Table -->
            <div class="modal-body p-1   ">
                <div class="table-container border border-black mb-4" style="box-shadow: 0px 15px rgba(151, 151, 151, 0.76)">
                    <div class="table-responsive" style="height: 300px; overflow: hidden;">
                        <table class="table table-hover table-striped">
                            <thead>
                                <tr class="order_tHead" role="button" tabindex="0">
                                    <th scope="col"><i class="fas fa-copy text-muted"></i></th>
                                    <th scope="col"><i class="bi bi-list-ul me-1"></i>Buyoff</th>
                                    <th scope="col"><i class="bi bi-gear me-1"></i>Buyoff State</th>
                                    <th scope="col"><i class="bi bi-circle-fill me-1"></i>SFC</th>
                                    <th scope="col"><i class="bi bi-box me-1"></i>Description</th>
                                    <th scope="col"><i class="bi bi-file-text me-1"></i>Action</th>
                                    <th scope="col"><i class="bi bi-file-text me-1"></i>Comment</th>
                                    <th scope="col"><i class="bi bi-file-text me-1"></i>User</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr id="confirmOrderRow">
                                    <td class="border"></td>
                                    <td class="text-dark">BSG_OPERATION_C...</td>
                                    <td class="font-monospace">Pending</td>
                                    <td class="text-start">
                                        <span class="text-dark" id="order_number_complete">303467636</span>
                                    </td>
                                    <td>I confirm that these t...</td>
                                    <td class="text-muted"></td>
                                    <td class="text-muted"></td>
                                    <td class="text-muted"></td>
                                </tr>
                                <!-- Filas adicionales vacías... -->
                            </tbody>
                        </table>
                    </div>
                </div>

                <!-- Comment Section -->
                <div class="d-flex align-items-start gap-2">
                    <h6 class="pt-1 mb-0" style="font-size: 12px">Comment:</h6>
                    <textarea class="form-control" style="height: 100px"></textarea>
                </div>
            </div>

            <!-- Confirm Window (Overlay) -->
            <div id="confirm-box" class="modal-body p-0 position-absolute confirm_window d-flex justify-content-center align-items-center" style="margin: auto; width: 100%; height: 60vh; background-color: rgba(167, 165, 165, 0.288);">
                <div class="confirm-box" style="width: 500px; min-height: 255px; max-height: 300px; border: 1px solid black; background-color: white;">
                    <div class="modal-header">
                        <h5 class="modal-title">Log Buyoff</h5>
                        <button type="button" class="btn-close btn_close_confirm"></button>
                    </div>

                    <div class="confirm_content p-3">
                        <article class="d-flex align-items-center justify-content-center gap-2 mt-3 ">
                            <div class=" d-flex justify-content-end align-items-center">
                                <span class="red-asterisk text-danger me-1">*</span>
                                <span class="filter-label ">User ID: </span>
                            </div>
                            <input type="text" class="form-control w-50 ms-3" id="userCode">
                        </article>

                        <article class="d-flex align-items-center justify-content-center gap-2 mt-3 ">
                            <div class=" d-flex justify-content-end align-items-center">
                                <span class="red-asterisk text-danger me-1">*</span>
                                <span class="filter-label">Password:</span>
                            </div>
                            <input type="password" class="form-control w-50" id="password" >
                        </article>
                    </div>

                    <div class="modal-footer">
                        <button type="button" class="btn-collection d-flex align-items-center gap-2 btn_login">
                            <i class="fa-solid fa-user text-success"></i>
                            <strong>Login</strong>
                        </button>
                        <button type="button" class="btn-collection d-flex align-items-center gap-2 btn_close_confirm">
                            <i class="fa-solid fa-xmark text-danger"></i>
                            <strong>Cancel</strong>
                        </button>
                    </div>
                </div>
            </div>

            <!-- Footer: Action Buttons -->
            <div class="modal-footer">
                <div class="d-flex justify-content-between align-items-center w-100 gap-2 flex-wrap">
                    <button type="button" class="btn-collection d-flex align-items-center gap-2" id="btn_accept">
                        <i class="fa-solid fa-circle-check text-success"></i>
                        <strong>Accept</strong>
                    </button>
                    <button type="button" class="btn-collection d-flex align-items-center gap-2">
                        <i class="fa-solid fa-xmark text-danger"></i>
                        <strong>Rejected</strong>
                    </button>
                    <button type="button" class="btn-collection d-flex align-items-center gap-2">
                        <i class="fa-solid fa-xmark text-danger"></i>
                        <strong>Partial</strong>
                    </button>
                    <button type="button" class="btn-collection d-flex align-items-center gap-2 disabled">
                        <i class="fa-solid fa-forward" style="color: #666565"></i>
                        <strong>Skip</strong>
                    </button>
                    <button type="button" class="btn-collection d-flex align-items-center gap-2 btn_close_confirm" data-bs-dismiss="modal" id="btn_close_bottom">
                        <i class="fa-solid fa-xmark text-danger"></i>
                        <strong>Cancelar</strong>
                    </button>
                </div>
            </div>
        </div>
    </div>
</div>

    <!-- ========================================
   MODAL: ORDER SEARCH
   Búsqueda de órdenes de trabajo
   ======================================== -->
<div class="modal fade" id="searchModal" tabindex="-1">
    <div class="modal-dialog modal-xl">
        <div class="modal-content shopOrder_search">
            <!-- Header -->
            <div class="modal-header">
                <h5 class="modal-title fw-bold">
                    <i class="bi bi-search me-2"></i>
                    BUSCAR POOL DE TRABAJO - SFC EN COLA - O_SRC_DWF_CUSHION_NIR,R_SRC_MOLDE_10021514,32003656B
                </h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>

            <!-- Body -->
            <div class="modal-body p-0">
                <div class="table-container">
                    <div class="table-responsive">
                        <table class="table table-hover table-striped mb-0">
                            <thead>
                                <tr class="order_tHead" role="button" tabindex="0">
                                    <th scope="col"><i class="fas fa-copy text-muted fs-5"></i></th>
                                    <th scope="col"><i class="bi bi-list-ul me-1"></i>CP</th>
                                    <th scope="col"><i class="bi bi-gear me-1"></i>Order</th>
                                    <th scope="col"><i class="bi bi-circle-fill me-1"></i>State</th>
                                    <th scope="col"><i class="bi bi-box me-1"></i>Shop Order</th>
                                    <th scope="col"><i class="bi bi-file-text me-1"></i>Material Description</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr class="table-row-selectable" id="orderSearchRow">
                                    <td class="border"></td>
                                    <td class="text-dark" id="orderCode">CR9A05637091</td>
                                    <td class="font-monospace"></td>
                                    <td class="text-start"><span class="text-dark">Activo</span></td>
                                    <td id="order_number_search">303467636</td>
                                    <td class="text-muted">DWF L Cushion PP NIR</td>
                                </tr>
                                <!-- Filas adicionales vacías para scroll -->
                                <tr class="table-row-selectable" role="button" tabindex="0">
                                    <td class="border"></td>
                                    <td></td><td></td><td></td><td></td><td></td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>

            <!-- Footer -->
            <div class="modal-footer d-flex justify-content-end">
                <button type="button" class="btn-collection d-flex align-items-center gap-2" id="btnOk" >
                    <i class="fa-solid fa-circle-check text-success"></i>
                    <strong>OK</strong>
                </button>
                <button type="button" class="btn-collection d-flex align-items-center gap-2" data-bs-dismiss="modal">
                    <i class="fa-solid fa-xmark text-danger"></i>
                    <strong>Cancelar</strong>
                </button>
            </div>
        </div>
    </div>
</div>
<!-- ========================================
   MODAL: START SECTION
   Sección de inicio de datos
   ======================================== -->
<div class="modal fade" id="modalStartSection" tabindex="-1">
    <div class="modal-dialog">
        <div class="modal-content">
            <!-- Header -->
            <div class="modal-header border-bottom border-4 border-primary">
                <h5 class="modal-title">Data Collection Entry</h5>
                <button type="button" class="close-btn">
                    <i class="fas fa-times" id="close_lineClearance"></i>
                </button>
            </div>

            <!-- Error Messages -->
            <section class="start_error_empy visually-hidden">
                <article class="sms_error d-flex align-items-center gap-2 my-2 mx-3">
                    <i class="fa-solid fa-circle-exclamation text-danger"></i>
                    <h6 class="mb-0 text-danger">Solo se permiten números</h6>
                </article>
            </section>

            <section class="start_error_selects visually-hidden">
                <article class="sms_error d-flex align-items-center gap-2 my-2 mx-3">
                    <i class="fa-solid fa-circle-exclamation text-danger"></i>
                    <h6 class="mb-0 text-danger">Debes responder las preguntas con (SI)</h6>
                </article>
            </section>

            <!-- Body: First Section (ID Molde) -->
            <section class="modal-body first_section" data-position="1">
                <div class="ps-2 fw-semibold">
                    <span class="text-muted">Group CD:</span> DCGTM_ID_MOLDE/A
                </div>

                <section class="border">
                    <div class="form-section border-bottom">
                        <div class="d-flex">
                            <span class="text-danger">*</span>
                            <h6>Ingrese el ID del Molde:</h6>
                        </div>
                        <div class="row align-items-center">
                            <div class="col-12 mb-2">
                                <div class="field-description">
                                    <span class="text-muted">Descripción:</span> ID Molde
                                </div>
                                <div class="data-type">
                                    <span class="text-muted">Tipo de datos:</span> Numérico
                                </div>
                            </div>
                            <div class="col-12">
                                <input type="text" class="form-control w-25">
                            </div>
                        </div>
                    </div>
                </section>
            </section>

            <!-- Body: LC Questions (Hidden) -->
            <section class="modal-body visually-hidden lc_questions" data-position="2">
                <div class="ps-2 fw-semibold">
                    <span class="text-muted">Group CD:</span> DCGTM_LC_MOLDEO/A
                </div>

                <section class="border">
                    <div class="form-section border-bottom">
                        <div class="d-flex">
                            <span class="text-danger">*</span>
                            <h6>¿Se verificaron las etiquetas de calibración?:</h6>
                        </div>
                        <div class="row align-items-center">
                            <div class="col-12 mb-2">
                                <div class="field-description">
                                    <span class="text-muted">Descripción:</span> ¿Se verificaron las etiquetas de máquina?
                                </div>
                                <div class="data-type">
                                    <span class="text-muted">Data Type:</span> Boolean
                                </div>
                            </div>
                            <div class="col-12">
                                <select class="form-select w-50">
                                    <option></option>
                                    <option value="si">Si</option>
                                    <option value="no">No</option>
                                </select>
                            </div>
                        </div>
                    </div>
                    <!-- Más preguntas similares... -->
                </section>
            </section>

            <!-- Body: Parameters (Hidden) -->
            <section class="modal-body border overflow-y-auto visually-hidden" id="start-parameters" data-position="3">
              
              
            </section>

            <!-- Body: Verification (Hidden) -->
            <section class="modal-body border visually-hidden" data-position="4" id="moldStartQuestion">
                <div class="ps-2 fw-semibold">
                    <span class="text-muted">Group CD:</span> DCGTM_VERIFICACION_MOLDEO_MAQUINA_OP/A
                </div>
                <div class="form-section border-bottom ps-2">
                    <div class="d-flex">
                        <span class="text-danger">*</span>
                        <h6>¿Se verificó el IDMold & IDIMM según MSS?:</h6>
                    </div>
                    <div class="row align-items-center">
                        <div class="col-12 mb-2">
                            <div class="field-description">
                                <span class="text-muted">Descripción:</span> ID Mold & IDIMM segun MSS?
                            </div>
                            <div class="data-type">
                                <span class="text-muted">Data Type:</span> Boolean
                            </div>
                        </div>
                        <div class="col-12">
                            <select class="form-select w-50">
                                <option></option>
                                <option value="si">Si</option>
                                <option value="no">No</option>
                            </select>
                        </div>
                    </div>
                </div>
            </section>

            <!-- Footer -->
            <div class="modal-footer">
                <div class="d-flex justify-content-center gap-3 flex-wrap w-100">
                    <button type="button" class="btn-collection" id="saveStartBtn">
                        <i class="fa-solid fa-floppy-disk text-primary"></i> Save
                    </button>
                    <button type="button" class="btn-collection" type="button">
                        <i class="fa-solid fa-floppy-disk text-primary"></i> Save Draft
                    </button>
                    <button type="button" class="btn-collection" id="cancelStartBtn">
                        <img src="../master/imgs/anular-icon.png" alt="cancel"> Cancel
                    </button>
                    <button type="button" class="btn-collection" id="returnStartBtn">
                        <i class="fa-solid fa-xmark text-danger"></i> Return to POD
                    </button>
                </div>
            </div>
        </div>
    </div>
</div>




<!-- ========================================
   MODAL: DATA COLLECTION
   Lista de colección de datos
   ======================================== -->
<div class="modal fade" id="dataCollection" tabindex="-1">
    <div class="modal-dialog modal-lg">
        <div class="modal-content">
            <!-- Header -->
            <div class="modal-header">
                <h5 class="modal-title">Data Collection List</h5>
                <button type="button" class="btn-close" id="close_data_collection" data-bs-dismiss="modal"></button>
            </div>

            <!-- Error Messages -->
            <section class="parameters_error_empy visually-hidden">
                <article class="sms_error d-flex align-items-center gap-2 my-2 mx-3">
                    <i class="fa-solid fa-circle-exclamation text-danger"></i>
                    <h6 class="mb-0 text-danger">Solo se permiten números</h6>
                </article>
            </section>

            <section class="exp_wrong_format visually-hidden">
                <article class="sms_error d-flex align-items-center gap-2 my-2 mx-3">
                    <i class="fa-solid fa-circle-exclamation text-danger"></i>
                    <h6 class="mb-0 text-danger">Formato incorrecto. El formato correcto es MM/DD/AA</h6>
                </article>
            </section>

            <section class="dataCL_error_selects visually-hidden">
                <article class="sms_error d-flex align-items-center gap-2 my-2 mx-3">
                    <i class="fa-solid fa-circle-exclamation text-danger"></i>
                    <h6 class="mb-0 text-danger">Debes responder la pregunta con (SI)</h6>
                </article>
            </section>

            <!-- Body: Collection List -->
            <div class="modal-body p-0 overflow-y-auto" id="dc_collection_list" style="max-height: 500px;">
                <div class="container-fluid p-2">
                    <div class="table-responsive">
                        <table class="table table-hover mb-0">
                            <tbody>
                                <!-- Primera Pieza -->
                                <tr>
                                    <td class="col-3">
                                        <div class="process-name">DCGTM_ENTREGA_PRIMERA_PIEZA/A</div>
                                    </td>
                                    <td class="col-3">
                                        <div class="description">Se entrega First shot (OPE)</div>
                                    </td>
                                    <td class="col-4">
                                        <small>Recoger datos en: <span class="fw-bold">En cualquier momento</span></small>
                                    </td>
                                    <td class="col-2">
                                        <button class="btn btn-register" type="button">
                                            <img src="../master/imgs/dc_collection-icon.png" alt="collect">
                                            Collect
                                        </button>
                                    </td>
                                </tr>

                                <!-- Primera Pieza NIR -->
                                <tr>
                                    <td class="col-3">
                                        <div class="process-name">DCGTM_ENTREGA_PRIMERA_PIEZA_NIR/A</div>
                                    </td>
                                    <td class="col-3">
                                        <div class="description">Se entrega First shot NIR (OPE)</div>
                                    </td>
                                    <td class="col-4">
                                        <small>Recoger datos en: <span class="fw-bold">En cualquier momento</span></small>
                                    </td>
                                    <td class="col-2">
                                        <button class="btn btn-register" type="button">
                                            <img src="../master/imgs/dc_collection-icon.png" alt="collect">
                                            Collect
                                        </button>
                                    </td>
                                </tr>

                                <!-- Aprobación Primera Pieza QA -->
                                <tr>
                                    <td class="col-3">
                                        <div class="process-name">DCGTM_APROBACION_PRIMERA_PIEZA_QA/A</div>
                                    </td>
                                    <td class="col-3">
                                        <div class="description">First shot se encuentra aprobado (QA)</div>
                                    </td>
                                    <td class="col-4">
                                        <small>Recoger datos en: <span class="fw-bold">En cualquier momento</span></small>
                                    </td>
                                    <td class="col-2">
                                        <button class="btn btn-register" disabled>
                                            <img src="../master/imgs/dc_collection-icon.png" alt="collect">
                                            Collect
                                        </button>
                                    </td>
                                </tr>
                                  <!-- Fecha Expiración NIR -->
  <tr>
      <td class="col-3">
          <div class="process-name">FECHA EXP NIR</div>
      </td>
      <td class="col-3">
          <div class="description">Expiración de la tinta</div>
      </td>
      <td class="col-4">
          <small>Recoger datos en: <span class="fw-bold">En cualquier momento</span></small>
      </td>
      <td class="col-2">
          <button class="btn btn-register" type="button">
              <img src="../master/imgs/dc_collection-icon.png" alt="collect">
              Collect
          </button>
      </td>
  </tr>


                                <!-- Inspección QA Completa -->
                                <tr>
                                    <td class="col-3">
                                        <div class="process-name">DCGTM_INSPECCION_QA_COMPLETA/A</div>
                                    </td>
                                    <td class="col-3">
                                        <div class="description">Inspección de calidad completa</div>
                                    </td>
                                    <td class="col-4">
                                        <small>Recoger datos en: <span class="fw-bold">En cualquier momento</span></small>
                                    </td>
                                    <td class="col-2">
                                        <button class="btn btn-register" disabled>
                                            <img src="../master/imgs/dc_collection-icon.png" alt="collect">
                                            Collect
                                        </button>
                                    </td>
                                </tr>

                               

                              
                                <!-- Parámetros MSS 1 -->
                                <tr>
                                    <td class="col-3">
                                        <div class="process-name">DCGTM_PARAMETROS_MSS_D000281359_1/A</div>
                                    </td>
                                    <td class="col-3">
                                        <div class="description">Ingrese los parámetros de MSS D000281359</div>
                                    </td>
                                    <td class="col-4">
                                        <small>Recoger datos en: <span class="fw-bold">En cualquier momento</span></small>
                                    </td>
                                    <td class="col-2">
                                        <button class="btn btn-register" disabled>
                                            <img src="../master/imgs/dc_collection-icon.png" alt="collect">
                                            Collect
                                        </button>
                                    </td>
                                </tr>

                                <!-- Parámetros MSS 2 -->
                                <tr>
                                    <td class="col-3">
                                        <div class="process-name">DCGTM_PARAMETROS_MSS_D000281359_2/A</div>
                                    </td>
                                    <td class="col-3">
                                        <div class="description">Ingrese los parámetros de MSS D000281359</div>
                                    </td>
                                    <td class="col-4">
                                        <small>Recoger datos en: <span class="fw-bold">En cualquier momento</span></small>
                                    </td>
                                    <td class="col-2">
                                        <button class="btn btn-register" type="button">
                                            <img src="../master/imgs/dc_collection-icon.png" alt="collect">
                                            Collect
                                        </button>
                                    </td>
                                </tr>

                                <!-- Parámetros MSS 3 -->
                                <tr>
                                    <td class="col-3">
                                        <div class="process-name">DCGTM_PARAMETROS_MSS_D000281359_3/A</div>
                                    </td>
                                    <td class="col-3">
                                        <div class="description">Ingrese los parámetros de MSS D000281359</div>
                                    </td>
                                    <td class="col-4">
                                        <small>Recoger datos en: <span class="fw-bold">En cualquier momento</span></small>
                                    </td>
                                    <td class="col-2">
                                        <button class="btn btn-register" type="button">
                                            <img src="../master/imgs/dc_collection-icon.png" alt="collect">
                                            Collect
                                        </button>
                                    </td>
                                </tr>

                                <!-- Parámetros MSS 4 -->
                                <tr>
                                    <td class="col-3">
                                        <div class="process-name">DCGTM_PARAMETROS_MSS_D000281359_4/A</div>
                                    </td>
                                    <td class="col-3">
                                        <div class="description">Ingrese los parámetros de MSS D000281359</div>
                                    </td>
                                    <td class="col-4">
                                        <small>Recoger datos en: <span class="fw-bold">En cualquier momento</span></small>
                                    </td>
                                    <td class="col-2">
                                        <button class="btn btn-register" type="button">
                                            <img src="../master/imgs/dc_collection-icon.png" alt="collect">
                                            Collect
                                        </button>
                                    </td>
                                </tr>

                                <!-- Parámetros MSS 5 -->
                                <tr>
                                    <td class="col-3">
                                        <div class="process-name">DCGTM_PARAMETROS_MSS_D000281359_5/A</div>
                                    </td>
                                    <td class="col-3">
                                        <div class="description">Ingrese los parámetros de MSS D000281359</div>
                                    </td>
                                    <td class="col-4">
                                        <small>Recoger datos en: <span class="fw-bold">Completar</span></small>
                                    </td>
                                    <td class="col-2">
                                        <button class="btn btn-register" type="button">
                                            <img src="../master/imgs/dc_collection-icon.png" alt="collect">
                                            Collect
                                        </button>
                                    </td>
                                </tr>

                                <!-- Verificación Parámetros 1 -->
                                <tr>
                                    <td class="col-3">
                                        <div class="process-name">DCGTM_VERIFICACION_PARAMETROS_1/A</div>
                                    </td>
                                    <td class="col-3">
                                        <div class="description">Se verificó la prueba de MSS</div>
                                    </td>
                                    <td class="col-4">
                                        <small>Recoger datos en: <span class="fw-bold">En cualquier momento</span></small>
                                    </td>
                                    <td class="col-2">
                                        <button class="btn btn-register" disabled>
                                            <img src="../master/imgs/dc_collection-icon.png" alt="collect">
                                            Collect
                                        </button>
                                    </td>
                                </tr>

                                <!-- Verificación Parámetros 2 -->
                                <tr>
                                    <td class="col-3">
                                        <div class="process-name">DCGTM_VERIFICACION_PARAMETROS_2/A</div>
                                    </td>
                                    <td class="col-3">
                                        <div class="description">Se verificó la prueba de MSS</div>
                                    </td>
                                    <td class="col-4">
                                        <small>Recoger datos en: <span class="fw-bold">En cualquier momento</span></small>
                                    </td>
                                    <td class="col-2">
                                        <button class="btn btn-register" disabled>
                                            <img src="../master/imgs/dc_collection-icon.png" alt="collect">
                                            Collect
                                        </button>
                                    </td>
                                </tr>

                                <!-- Verificación Parámetros 3 -->
                                <tr>
                                    <td class="col-3">
                                        <div class="process-name">DCGTM_VERIFICACION_PARAMETROS_3/A</div>
                                    </td>
                                    <td class="col-3">
                                        <div class="description">Se verificó la prueba de MSS</div>
                                    </td>
                                    <td class="col-4">
                                        <small>Recoger datos en: <span class="fw-bold">En cualquier momento</span></small>
                                    </td>
                                    <td class="col-2">
                                        <button class="btn btn-register" disabled>
                                            <img src="../master/imgs/dc_collection-icon.png" alt="collect">
                                            Collect
                                        </button>
                                    </td>
                                </tr>

                                <!-- Verificación Parámetros 4 -->
                                <tr>
                                    <td class="col-3">
                                        <div class="process-name">DCGTM_VERIFICACION_PARAMETROS_4/A</div>
                                    </td>
                                    <td class="col-3">
                                        <div class="description">Se verificó la prueba de MSS</div>
                                    </td>
                                    <td class="col-4">
                                        <small>Recoger datos en: <span class="fw-bold">En cualquier momento</span></small>
                                    </td>
                                    <td class="col-2">
                                        <button class="btn btn-register" disabled>
                                            <img src="../master/imgs/dc_collection-icon.png" alt="collect">
                                            Collect
                                        </button>
                                    </td>
                                </tr>

                                <!-- Verificación Parámetros 5 -->
                                <tr>
                                    <td class="col-3">
                                        <div class="process-name">DCGTM_VERIFICACION_PARAMETROS_5/A</div>
                                    </td>
                                    <td class="col-3">
                                        <div class="description">Se verificó la prueba de MSS</div>
                                    </td>
                                    <td class="col-4">
                                        <small>Recoger datos en: <span class="fw-bold">En cualquier momento</span></small>
                                    </td>
                                    <td class="col-2">
                                        <button class="btn btn-register" disabled>
                                            <img src="../master/imgs/dc_collection-icon.png" alt="collect">
                                            Collect
                                        </button>
                                    </td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>

            <!-- Body: Data Log (Hidden) -->
            <div class="modal-body overflow-y-auto visually-hidden" id="data_log_container" style="max-height: 500px;"></div>

            <!-- Body: Exp Tinta (Hidden) -->
            <div class="modal-body overflow-y-auto visually-hidden" id="exp_tinta" style="max-height: 500px;">
                <div id="parameters">
                    <div class="ps-1 mb-1 border-bottom">
                        <strong class="text-muted">SFC:</strong> CR9A05647275 (1/1)
                    </div>
                    <div class="ps-2 fw-semibold mb-3">
                        <span class="text-muted">Group CD:</span> DCGTM_FECHA_EXPIRACION_NIR/A
                    </div>

                    <section class="border">
                        <div class="form-section border-bottom p-3">
                            <div class="d-flex mb-2">
                                <span class="text-danger me-1">*</span>
                                <h6 class="mb-0">Ingrese la fecha de expiración kerosene:</h6>
                            </div>
                            <div class="row align-items-center">
                                <div class="col-12 mb-3">
                                    <div class="field-description mb-1">
                                        <span class="text-muted">Descripción:</span> Ingrese la fecha de expiración kerosene
                                    </div>
                                    <div class="data-type">
                                        <span class="text-muted">Data Type:</span> Text (MM/DD/AA)
                                    </div>
                                </div>
                                <div class="col-12">
                                    <input type="text" class="form-control w-50" placeholder="MM/DD/AA">
                                </div>
                            </div>
                        </div>
                    </section>
                    <section class="border">
                        <div class="form-section border-bottom p-3">
                            <div class="d-flex mb-2">
                                <span class="text-danger me-1">*</span>
                                <h6 class="mb-0">*Ingrese la fecha de expiración de Tintas:</h6>
                            </div>
                            <div class="row align-items-center">
                                <div class="col-12 mb-3">
                                    <div class="field-description mb-1">
                                        <span class="text-muted">Descripción:</span> Ingrese la fecha de expiración de Priscone
                                    </div>
                                    <div class="data-type">
                                        <span class="text-muted">Data Type:</span> Text (MM/DD/AA)
                                    </div>
                                </div>
                                <div class="col-12">
                                    <input type="text" class="form-control w-50" placeholder="MM/DD/AA">
                                </div>
                            </div>
                        </div>
                    </section>
                </div>
            </div>

            <!-- Body: First Shot Entrega (Hidden) -->
            <section class="modal-body border visually-hidden" id="fs_entrega" style="max-height: 500px;">
                <div class="ps-2 fw-semibold mb-3">
                    <span class="text-muted">Group CD:</span> DCGTM_ENTREGA_PRIMERA_PIEZA/A
                </div>
                <div class="form-section border-bottom p-3">
                    <div class="d-flex mb-2">
                        <span class="text-danger me-1">*</span>
                        <h6 class="mb-0">Se entrega First Shot a calidad:</h6>
                    </div>
                    <div class="row align-items-center">
                        <div class="col-12 mb-3">
                            <div class="data-type">
                                <span class="text-muted">Data Type:</span> Boolean
                            </div>
                        </div>
                        <div class="col-12">
                            <select class="form-select w-50">
                                <option value="">Seleccione una opción</option>
                                <option value="si">Si</option>
                                <option value="no">No</option>
                            </select>
                        </div>
                    </div>
                </div>
            </section>

            <!-- Footer -->
            <div class="modal-footer action-buttons visually-hidden" id="parameters_footer">
                <div class="d-flex justify-content-center gap-3 flex-wrap">
                    <button type="button" class="btn action-btn " id="saveBtn">
                        <i class="fa-solid fa-xl fa-floppy-disk text-info"></i> Save
                    </button>
                    <button type="button" class="btn action-btn" id="draftBtn">
                        <i class="fa-solid fa-xl fa-floppy-disk text-info"></i> Save Draft
                    </button>
                    <button type="button" class="btn action-btn" id="cancelBtn">
                        <i class="fa-solid fa-xl fa-ban text-danger"></i> Cancel
                    </button>
                    <button type="button" class="btn action-btn" id="returnBtn">
                        <i class="fa-solid fa-xl fa-xmark text-danger"></i> Return to POD
                    </button>
                </div>
            </div>
        </div>
    </div>
</div>

    <section class="guideZone">


     <button id="btnGuideEnd" class="position-absolute start-1 bottom-0 ">Finalizar guía</button>
    </section>



     <!--Important Scripts -->
    <script src="../Dashboard/js/main.v1.0.0.js"></script>
    <script src="../Dashboard/js/guidedTest.v1.0.0.js"></script>
</asp:Content>
