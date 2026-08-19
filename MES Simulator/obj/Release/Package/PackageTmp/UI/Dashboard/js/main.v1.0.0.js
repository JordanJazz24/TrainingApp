// ============================================
// CONFIGURACIÓN GLOBAL Y UTILIDADES
// ============================================

const CONFIG = {
    STORAGE_KEYS: {
        RESOURCE_DATA: 'resourceData',
        LINE_CLEARANCE: 'lineClearance',
        WORK_TABLE: 'workTableContainer',
        CODE1: 'code1',
        ORDER: "order"
    },
    PARAMETERS: {

        data: [
            { title: 'Colling Time según el MSS D000281359', description: 'Colling Time según MSS D000281359' },
            { title: 'Injection speed según MSS D000281359', description: 'Resultado según MSS D000281359 VERT 1' },
            { title: 'Intensidad de Pad Printing', description: 'Ingrese la intensidad de la Pad Printing' },
            { title: 'Temperatura Zona 1', description: 'Temperatura de Molde Zona 1' },
            { title: 'Temperatura Zona 2', description: 'Temperatura de Molde Zona 2' },
            { title: 'Temperatura Zona 3', description: 'Temperatura de Molde Zona 3' },
            { title: 'Temperatura Zona 4', description: 'Temperatura de Molde Zona 4' },
            { title: 'Temperatura Zona 5', description: 'Temperatura de Molde Zona 5' },
            { title: 'Temperatura Zona 6', description: 'Temperatura de Molde Zona 6' },
            { title: 'Tiempo de curado de Pad Printing', description: 'Ingrese el tiempo de curado de la Pad Printing' }
        ],
        loadParameters: function (container) {
            container.innerHTML = `
         <div id="parameters">
             <div class="row justify-content-center">
                 <div class="col-12">
                     <div class="form-container position-relative">
                         <div class="p-1">
                             <div class="ps-1 mb-1 fs-6 border border-bottom">
                                 <strong class="text-muted">SFC:</strong> CR9A05647275 (1/1)
                             </div>
                             <div class="ps-2 fs-6 fw-semibold">
                                 <span class="text-muted">Group CD:</span> <strong class="fw-bold" id="group_dc">DCGTM_PARAMETROS_MSS_D000281359/A</strong>
                             </div>
                             <form class="border">
                                 ${this.generateParameterFields()}
                             </form>
                         </div>
                     </div>
                 </div>
             </div>
         </div>
     `;
        },
        generateParameterFields: function () {
            const parameters = this.data

            return parameters.map(param => `
         <div class="form-section border-bottom">
             <h6>${param.title}:</h6>
             <div class="row align-items-center">
                 <div class="col-12 row mb-2">
                     <div class="col-4 field-description">
                         <span class="text-muted">Descripción:</span> ${param.description}
                     </div>
                     <div class="col-4">
                         <div class="data-type">
                             <span class="text-muted">Tipo de datos:</span> Numérico
                         </div>
                     </div>
                     <div class="col-4 d-flex justify-content-end">
                         <button type="button" class="btn btn-comment">
                             <i class="bi bi-chat-dots me-1"></i>Coment.
                         </button>
                     </div>
                 </div>
                 <div class="col-12">
                     <input type="text" class="form-control w-25" placeholder="Ingrese valor numérico">
                 </div>
             </div>
         </div>
     `).join('');
        }
    },
    LCQUESTIONS: {
        data: [
            {
                questionText: "¿Se verificaron las etiquetas de calibración?",
                description: "¿Se verificaron las etiquetas de máquina?"
            },
            {
                questionText: "¿Se verificó D000128682 o D000207232?",
                description: "¿Se verificó la Bomba Elmet?"
            },
            {
                questionText: "¿Se verificó según D000128035 o D000207299?",
                description: "¿Se realizó el arranque de máquina V/H?"
            }
        ],

        loadLcQuestions: function (container) {


            container.innerHTML = '';
            this.data.forEach(item => {
                container.insertAdjacentHTML('beforeend', this.createLcQuestion(item));
            });
        },
        createLcQuestion: function (item) {
            return `
            <div class="form-section border-bottom">
                <div class="d-flex">
                    <span class="text-danger">*</span>
                    <h6>${item.questionText}:</h6>
                </div>
                <div class="row align-items-center">
                    <div class="col-12 row mb-2">
                        <div class="col-4 field-description">
                            <span class="text-muted">Descripción:</span> ${item.description}
                        </div>
                        <div class="col-4">
                            <div class="data-type">
                                <span class="text-muted">Data Type:</span> Boolean
                            </div>
                        </div>
                        <div class="col-4 d-flex justify-content-end">
                            <button type="button" class="btn btn-comment">
                                <i class="bi bi-chat-dots me-1"></i>Coment.
                            </button>
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
        `;
        }
    }

};

// Utilidades de almacenamiento
const Storage = {
    get: (key) => sessionStorage.getItem(key),
    set: (key, value) => sessionStorage.setItem(key, value),
    remove: (key) => sessionStorage.removeItem(key)
};

// Utilidades de UI
const UI = {
    show: (element) => element?.classList.remove('visually-hidden', 'd-none', 'invisible'),
    hide: (element) => element?.classList.add('visually-hidden', 'd-none', 'invisible'),
    toggle: (element, className = 'row-active') => element?.classList.toggle(className),

    showMessage: (container, type, text, duration = 3000) => {
        if (!container) return;

        const icon = type === 'success'
            ? '<i class="fa-solid fa-circle-check text-success fs-4"></i>'
            : '<i class="bi bi-exclamation-square-fill text-danger fs-5"></i>';

        container.innerHTML = `
      <article class="sms_error d-flex align-items-center gap-3 my-2">
        ${icon}
        <h6 class="mb-0 ${type === 'success' ? 'text-success' : 'text-danger'}">${text}</h6>
      </article>
    `;

        setTimeout(() => container.innerHTML = '', duration);
    }
};

// Utilidad para validación
const Validation = {
    isValidDate: (value) => {
        const regex = /^(0[1-9]|1[0-2])\/(0[1-9]|[12][0-9]|3[01])\/\d{2}$/;
        if (!regex.test(value)) return false;

        const [month, day, year] = value.split('/').map(Number);
        const fullYear = year < 50 ? 2000 + year : 1900 + year;
        const isLeapYear = (fullYear % 4 === 0 && fullYear % 100 !== 0) || (fullYear % 400 === 0);
        const daysPerMonth = [31, isLeapYear ? 29 : 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];

        return month >= 1 && month <= 12 && day >= 1 && day <= daysPerMonth[month - 1];
    },

    hasEmptyInputs: (inputs) => Array.from(inputs).some(input => !input.value.trim()),

    hasInvalidSelects: (selects) => Array.from(selects).some(select => select.selectedIndex !== 1)
};



// ============================================
// WORK TABLE MANAGER
// ============================================

class WorkTableManager {
    constructor() {
        this.container = document.getElementById('tableWorkContainer');
        this.init();
    }

    init() {
        this.container?.addEventListener('click', (e) => {
            const row = e.target.closest('tr');
            if (row) UI.toggle(row);
        });

       
    }

    getActiveRows() {
        return this.container?.querySelectorAll('tr.row-active') || [];
    }

    removeRow(row) {
        row?.remove();
        this.saveToStorage();
    }

    saveToStorage() {
        if (this.container) {
            Storage.set(CONFIG.STORAGE_KEYS.WORK_TABLE, this.container.innerHTML);
        }
    }
}

// ============================================
// RESOURCE SETUP MANAGER
// ============================================

// ============================================
// RESOURCE SETUP MANAGER
// ============================================

class ResourceManager {
    constructor() {
        this.modal = document.getElementById('Resource-setup-modal');
        this.container = document.getElementById('resourceContainer');
        this.order_number_search = document.getElementById('order_number_search')
        this.buttons = {
            insertNew: document.getElementById('btnInsertNew'),
            insertBefore: document.getElementById('btnInsertBefore'),
            insertAfter: document.getElementById('btnInsertAfter'),
            deleteSelected: document.getElementById('btnDeleteSelected'),
            deleteAll: document.getElementById('btnDeleteAll'),
            retrieve: document.getElementById('btnRetrieve'),
            btnSave: document.getElementById('btnResourceSave')

        };
        this.statusSelect = document.getElementById('statusSelect')
        this.saveResourceMsm = document.getElementById("record_change")
        this.rowTemplate = `
      <row class="row g-2 border rounded py-2 px-1">
        <div class="col-md-6 col-12">
          <select name="type" class="selectType w-75">
            <option value="Shop Order">Shop Order</option>
            <option value="Tool Number">Tool Number</option>
            <option value="Material">Material</option>
          </select>
        </div>
        <div class="col-md-6 col-12 d-flex align-items-center">
          <input type="text"  placeholder="Enter value" class="resourceValue w-100">
        </div>
      </row>
    `;

        this.init();
    }

    init() {
        this.attachEventListeners();
    }

    attachEventListeners() {
        // Modal shown
        this.modal?.addEventListener('shown.bs.modal', () => this.loadData());

        // Modal hide - validación
        this.modal?.addEventListener('hide.bs.modal', () => this.cleanEmptyRows());

        // Retrieve button
        this.buttons.retrieve?.addEventListener('click', (e) => {
            e.preventDefault();
            this.toggleVisibility();
        });

        //Change status select 
        this.statusSelect.addEventListener("change", e => {
            this.toggleVisibility();

            const isSetupMode = e.target.selectedIndex === 1 && e.target.value === "setUp";

            isSetupMode ? this.container.classList.add('invisible') : this.container.classList.remove('invisible');
            e.target.selectedIndex = isSetupMode ? 0 : 1;

            Object.values(this.buttons).forEach(btn => {
                if (btn && btn !== this.buttons.retrieve) {
                    isSetupMode ? btn.setAttribute('disabled', '') : btn.removeAttribute('disabled');
                }
            });
        })

        // Insert buttons
        this.buttons.insertNew?.addEventListener('click', (e) => {
            e.preventDefault();
            this.insertRow('beforeend');
        });

        this.buttons.insertBefore?.addEventListener('click', (e) => {
            e.preventDefault();
            this.insertRow('beforebegin');
        });

        this.buttons.insertAfter?.addEventListener('click', (e) => {
            e.preventDefault();
            this.insertRow('afterend');
        });

        // Delete buttons
        this.buttons.deleteSelected?.addEventListener('click', (e) => {
            e.preventDefault();
            this.deleteSelected();
        });

        this.buttons.deleteAll?.addEventListener('click', (e) => {
            e.preventDefault();
            this.deleteAll();
        });

        //Save Button

        this.buttons.btnSave?.addEventListener("click", e => {
            e.preventDefault()
            this.saveData()



        })

        // Row clicks
        this.container?.addEventListener('click', (e) => {
            if (e.target.matches('select') || e.target.matches('input')) return
            const row = e.target.closest('.row');
            if (row) UI.toggle(row);
        });
    }

    loadData() {
        const savedData = Storage.get(CONFIG.STORAGE_KEYS.RESOURCE_DATA);
        this.container.innerHTML = savedData || '';
    }

    saveData() {

        if (!this.validate()) return
        const rows = this.container.querySelectorAll('row');

        rows.forEach(row => {
            const select = row.querySelector('.selectType');
            const input = row.querySelector('.resourceValue');

            // Guardar estado del select
            if (select) {
                const selectedOption = select.options[select.selectedIndex];
                Array.from(select.options).forEach(opt => opt.removeAttribute('selected'));
                selectedOption?.setAttribute('selected', 'selected');
            }

            // Guardar valor del input
            if (input) input.setAttribute('value', input.value);
        });

        Storage.set(CONFIG.STORAGE_KEYS.RESOURCE_DATA, this.container.innerHTML);
        this.saveResourceMsm.classList.toggle("invisible")

        setTimeout(() => {
            this.saveResourceMsm.classList.toggle("invisible")
        }, 3000)

    }

    cleanEmptyRows() {

        const rows = this.container.querySelectorAll('row');
        const emptyRows = Array.from(rows).filter(row => {
            const input = row.querySelector('.resourceValue');
            return !input?.value.trim();
        });

        if (emptyRows.length > 0) {
            emptyRows.forEach(row => row.remove());
            this.saveData();
        }
        //cambiar el status select y la visibilidad si esta en setUP
        const isSetupMode = this.statusSelect.selectedIndex === 1 && this.statusSelect.value === "setUp";

        isSetupMode ? this.container.classList.add('invisible') : ""
        this.statusSelect.selectedIndex = isSetupMode ? 0 : 0;

        Object.values(this.buttons).forEach(btn => {
            if (btn && btn !== this.buttons.retrieve) {
                isSetupMode ? btn.setAttribute('disabled', '') : btn.removeAttribute('disabled');
            }
        });
    }

    toggleVisibility() {

        const isSetupMode = this.statusSelect.selectedIndex === 1 && this.statusSelect.value === "setUp";

        isSetupMode ? this.container.classList.add('invisible') : this.container.classList.remove('invisible');
        this.statusSelect.selectedIndex = isSetupMode ? 0 : 1;

        Object.values(this.buttons).forEach(btn => {
            if (btn && btn !== this.buttons.retrieve) {
                isSetupMode ? btn.setAttribute('disabled', '') : btn.removeAttribute('disabled');
            }
        });
    }

    insertRow(position) {
        if (position === 'beforeend') {
            this.container.insertAdjacentHTML('beforeend', this.rowTemplate);
        } else {
            const selectedRow = this.container.querySelector('.row-active');
            if (!selectedRow) {
                alert('Please select a row first');
                return;
            }
            selectedRow.insertAdjacentHTML(position, this.rowTemplate);
        }

    }

    deleteSelected() {
        const selectedRows = this.container.querySelectorAll('.row-active');
        if (selectedRows.length === 0) {
            alert('Please select at least one row to delete');
            return;
        }
        selectedRows.forEach(row => row.remove());
        Storage.set(CONFIG.STORAGE_KEYS.RESOURCE_DATA, this.container.innerHTML);
    }

    deleteAll() {
        const rows = this.container.querySelectorAll('.row');
        if (rows.length === 0) {
            alert('No rows to delete');
            return;
        }
        this.container.textContent = '';
        Storage.set(CONFIG.STORAGE_KEYS.RESOURCE_DATA, this.container.innerHTML);
    }

    validate() {
        const rows = this.container.querySelectorAll('row');

        if (rows.length === 0) {
            Swal.fire({
                title: 'Error al guardar',
                text: 'No hay recursos creados',
                icon: 'error'
            });
            return false;
        }

        // Verificar recursos vacíos
        const hasEmpty = Array.from(rows).some(row => {
            const input = row.querySelector('.resourceValue');
            return !input?.value.trim();
        });

        if (hasEmpty) {
            Swal.fire({
                title: 'Error al guardar',
                text: 'Tienes recursos sin valor. Por favor completa todos los campos',
                icon: 'error'
            });
            return false;
        }

        // Verificar Shop Order único
        const shopOrders = Array.from(rows).filter(row =>
            row.querySelector('.selectType')?.value === 'Shop Order'
        );

        if (shopOrders.length !== 1) {
            Swal.fire({
                title: 'Error al guardar',
                text: 'Debe haber exactamente un recurso de tipo "Shop Order"',
                icon: 'error'
            });
            return false;
        }

        // luego de la validacion son el que guardamos en  this.order_number_search
        let orderValue = shopOrders[0].querySelector("input").value
        this.order_number_search.textContent = orderValue
        Storage.set(CONFIG.STORAGE_KEYS.ORDER, orderValue)
        return true;
    }
}


// ============================================
// LINE CLEARANCE MANAGER
// ============================================

class LineClearanceManager {
    constructor() {
        this.modal = document.getElementById('modalColeccionDatos');
        this.collectView = document.getElementById('lineclearanceCollect');
        this.questionView = document.getElementById('lineClearanceQuestion');
        this.selectQuestion = document.getElementById('selectLineQuestion');
        this.saveMsg = document.getElementById('saveLineSms');
        this.collectRow = document.getElementById('lineCollectRow');
        this.btnSave = document.getElementById("btnLineSave")
        this.init();
    }

    init() {
        this.attachEventListeners();
    }

    attachEventListeners() {
        this.modal?.addEventListener('hide.bs.modal', () => this.reset());

        this.modal?.addEventListener('click', (e) => {
            if (e.target.closest('#btnLineRetrieve')) {
                this.showDateTime();
            }
            if (e.target.closest('#btnCollectLine')) {
                this.toggleViews();
            }
        });

        //Save line clearance
        this.btnSave.addEventListener("click", e => {
            this.save()
        })
    }

    reset() {
        UI.hide(this.saveMsg);
        UI.show(this.collectView);
        UI.hide(this.questionView);
        UI.hide(this.collectRow);
        if (this.selectQuestion) this.selectQuestion.selectedIndex = 0;
    }

    showDateTime() {
        UI.show(this.collectRow);
        const now = new Date();
        const dateElement = document.getElementById('line_clearence_date');
        if (dateElement) {
            dateElement.textContent = `${now.toLocaleDateString('es-ES', {
                day: '2-digit', month: 'short', year: 'numeric'
            })} ${now.toLocaleTimeString('es-ES', {
                hour: '2-digit', minute: '2-digit', second: '2-digit'
            })}`;
        }
    }

    toggleViews() {
        const isCollectVisible = !this.collectView.classList.contains('d-none');

        if (isCollectVisible) {
            UI.hide(this.collectView);
            UI.show(this.questionView);
        } else {
            UI.show(this.collectView);
            UI.hide(this.questionView);
            UI.show(this.saveMsg);
            UI.hide(this.collectRow);
            if (this.selectQuestion) this.selectQuestion.selectedIndex = 0;
        }
    }

    save() {
        if (this.selectQuestion?.value === 'true') {
            Storage.set(CONFIG.STORAGE_KEYS.LINE_CLEARANCE, 'true');
            this.toggleViews();
        } else {
            Swal.fire({
                title: 'Error al guardar',
                text: 'Debes indicar que sí realizaste limpieza de línea',
                icon: 'error'
            });
        }
    }
}

// ============================================
// SFC SPLIT MANAGER
// ============================================

class SplitManager {
    constructor() {
        this.modal = document.getElementById('modalSplit');
        this.workTable = document.getElementById('tableWorkContainer');
        this.splitBtn = document.getElementById('splitBtn')
        this.init();
    }

    init() {

        this.attachEventListeners()
    }


    attachEventListeners() {

        //Modal Open
        this.modal?.addEventListener('shown.bs.modal', () => this.validate());
        //Split button

        this.splitBtn?.addEventListener("click", e => {
            this.split()
        })
    }
    validate() {
        const rows = this.workTable?.querySelectorAll('tr') || [];
        const activeRows = Array.from(rows).filter(row => row.classList.contains('row-active'));

        if (rows.length === 0) {
            this.showError('No hay filas disponibles');
            return;
        }

        if (activeRows.length === 0) {
            this.showError('Debes seleccionar 1 fila');
            return;
        }

        if (activeRows.length > 1) {
            this.showError('Solo puedes seleccionar 1 fila a la vez');
            return;
        }

        const orderValue = activeRows[0].querySelector('.orderValue');
        const splitQty = document.getElementById('splitQuantity');
        if (splitQty && orderValue) {
            splitQty.textContent = orderValue.textContent;
        }
    }

    showError(message) {
        Swal.fire({
            title: 'Error al hacer split',
            text: message,
            icon: 'error'
        });

    }

    split() {
        const rows = this.workTable?.querySelectorAll('tr') || [];
        const activeRows = Array.from(rows).filter(row => row.classList.contains('row-active'));

        if (activeRows.length !== 1) return;

        const row = activeRows[0];
        const orderValue = row.querySelector('.orderValue');
        const splitInput = document.getElementById('splitValue');
        const orderNumber = row.querySelector('.order_number');

        const splitValue = Number(splitInput?.value);
        const currentValue = Number(orderValue?.textContent);

        if (isNaN(splitValue) || splitValue <= 0) {
            this.showError('Ingresa un valor válido para dividir');
            return;
        }

        if (splitValue >= currentValue) {
            this.showError('El valor a dividir debe ser menor al valor actual');
            return;
        }

        orderValue.textContent = currentValue - splitValue;
        document.getElementById('splitQuantity').textContent = orderValue.textContent;

        this.workTable.insertAdjacentHTML('beforeend', `
      <tr class="OrderRow">
        <td>CR9A053631091</td>
        <td>DWF L Cushion PP NIR</td>
        <td class="orderValue">${splitValue}</td>
        <td><span class="status-indicator status-green"></span>Activo</td>
        <td>-</td>
        <td class="order_number">${orderNumber?.textContent || Storage.get(CONFIG.STORAGE_KEYS.ORDER)}</td>
      </tr>
    `);

        Storage.set(CONFIG.STORAGE_KEYS.WORK_TABLE, this.workTable.innerHTML);

        Swal.fire({
            title: '¡Éxito!',
            text: 'Split realizado correctamente',
            icon: 'success'
        });

        if (splitInput) splitInput.value = '';
    }
}

// ============================================
// LOG NC MANAGER - VERSIÓN CORREGIDA
// ============================================

class LogNCManager {
    constructor() {
        this.modal = document.getElementById('log_NC_Modal');
        this.container = document.querySelector('#log_NC_container');
        this.scrapTypes = document.querySelector('#scrap_types');
        this.scrapSelected = document.querySelector('#scrap_selected');
        this.inputNC = document.querySelector('#nc_code');
        this.tableWorkContainer = document.querySelector('#tableWorkContainer');
        this.dashboardMessages = document.querySelector('.dashboard_mensages');
        this.modalInstance = this.modal ? new bootstrap.Modal(this.modal) : null;
        this.currentPage = 1;
        this.itemsPerPage = 3;
        this.scrapItems = [
            {
                scrap1: { title: "F_FR_GENERAL", description: "Fast Repair General" },
                scrap2: { title: "F_FS_0000", description: "Pruebas de ingenieria" },
            },
            {
                scrap1: { title: "F_FS_BRK_EMITTER", description: "Wire Broken" },
                scrap2: { title: "F_FS_BURBUJAS", description: "Burbujas" },
            },
            {
                scrap1: { title: "F_FS_CONTAMINADA", description: "Contaminadas" },
                scrap2: { title: "F_FS_D_MONTAJE", description: "Daño de la pieza por montaje incorrecto" },
            },
            {
                scrap1: { title: "F_FS_DEFORMES", description: "Deformes" },
                scrap2: { title: "F_FS_DESECHO QA", description: "Desecho de calidad" },
            },
            {
                scrap1: { title: "F_FS_DISBONDING", description: "Disbonding" },
                scrap2: { title: "F_FS_ELECT_FAIL", description: "Fallo Electrico" },
            },
            {
                scrap1: { title: "F_FS_ESTAB", description: "Estabilizacion (ajuste de proceso)" },
                scrap2: { title: "F_FS_FALT_MAT", description: "Falta de Material" },
            },
            {
                scrap1: { title: "F_FS_FLASH", description: "Flash" },
                scrap2: { title: "F_FS_GLUE_EXC", description: "Exceso de Goma o Adhesivo" },
            },
            {
                scrap1: { title: "F_FS_MAL_CURADO", description: "Mal Curado" },
                scrap2: { title: "F_FS_MANCHAS", description: "Manchas" },
            },
            {
                scrap1: { title: "F_FS_NCR", description: "NCR" },
                scrap2: { title: "F_FS_NOMATCH", description: "NOMATCH" },
            },
            {
                scrap1: { title: "F_FS_P_IMPRES_IN", description: "Pieza Impresa Incorrectamente" },
                scrap2: { title: "F_FS_PARTICULAS", description: "Particulas" },
            },
            {
                scrap1: { title: "F_FS_PIEL_NARANJ", description: "Piel Naranja" },
                scrap2: { title: "F_FS_PUNTO INYEC", description: "Punto inyeccion" },
            },
            {
                scrap1: { title: "F_FS_RAYAS", description: "Rayas" },
                scrap2: { title: "F_FS_RING DEFORM", description: "Ring Deformado" },
            },
            {
                scrap1: { title: "F_FS_ROTAS", description: "Rotas" },
                scrap2: { title: "F_FS_THICKNESS", description: "NCR" },
            },
            {
                scrap1: { title: "F_FS_TIRO CORTO", description: "Tiro Corto" },
                scrap2: { title: "F_FS_TNT_CORRIDA", description: "Tinta Corrida" },
            },
            {
                scrap1: { title: "F_FS_TNT_NO_CURA", description: "Tinta no Curada" },
                scrap2: { title: "F_FS_FX_GENERAL", description: "Fast Extended Repair General" },
            },
        ];

        this.init();
    }

    init() {
        this.loadData();
        this.attachEventListeners();
    }

    attachEventListeners() {
        document.addEventListener('click', (e) => {
            if (e.target.closest('#btn_logNC_dashboard')) this.open();
            if (e.target.closest('.btn_logNC')) this.selectNC(e.target);
            if (e.target.closest('#close_log_nc')) this.showScrapTypes();
            if (e.target.closest('#add_done')) this.completeAndClose();
            if (e.target.closest('.page-link')) this.handlePagination(e);
        });
    }

    checkLineClearance() {
        return Storage.get(CONFIG.STORAGE_KEYS.LINE_CLEARANCE) !== null;
    }

    open() {
        if (!this.checkLineClearance()) {
            UI.showMessage(
                this.dashboardMessages,
                'error',
                'User has changed, please perform Line Clearance (Message 22501)'
            );
            return;
        }

        this.loadData();
        this.modalInstance?.show();
    }

    closeModal() {
        this.modalInstance?.hide();
        this.cleanupModalBackdrop();
    }

    cleanupModalBackdrop() {
        const backdrops = document.querySelectorAll('.modal-backdrop');
        backdrops.forEach(backdrop => backdrop.remove());
        document.body.classList.remove('modal-open');
        document.body.style.overflow = '';
        document.body.style.paddingRight = '';
    }

    selectNC(button) {
        const ncButton = button.closest('.btn_logNC');
        const ncCode = ncButton?.dataset.ncCode;
        if (ncCode && this.inputNC) {
            this.inputNC.value = ncCode;
            this.showScrapSelected();
        }
    }

    showScrapTypes() {
        UI.hide(this.scrapSelected);
        UI.show(this.scrapTypes);
    }

    showScrapSelected() {
        UI.hide(this.scrapTypes);
        UI.show(this.scrapSelected);
    }

    completeAndClose() {
        const activeRows = this.tableWorkContainer?.querySelectorAll('tr.row-active') || [];
        activeRows.forEach(row => row.remove());

        UI.showMessage(
            this.dashboardMessages,
            'success',
            'All CR9A053631091 have been scraped'
        );

        Storage.set(CONFIG.STORAGE_KEYS.WORK_TABLE, this.tableWorkContainer?.innerHTML || 0);
        this.closeModal();
        this.showScrapTypes();
    }

    handlePagination(e) {
        const link = e.target.closest('.page-link');
        if (!link) return;

        e.preventDefault();

        const allLinks = document.querySelectorAll('.page-link');
        allLinks.forEach(l => l.parentElement.classList.remove('active'));
        link.parentElement.classList.add('active');

        const pageNum = parseInt(link.textContent) || this.currentPage;
        this.currentPage = pageNum;
        this.scrollToPage(pageNum);
    }

    scrollToPage(pageNum) {
        const page = document.getElementById(`page_${pageNum}`);
        if (page) {
            page.scrollIntoView({ behavior: 'smooth', inline: 'start' });
        }
    }

    loadData() {
        const fragment = document.createDocumentFragment();
        const pages = this.createPages();

        pages.forEach((page, index) => {
            const pageElement = this.createPageElement(page, index + 1);
            fragment.appendChild(pageElement);
        });

        this.container.innerHTML = '';
        this.container.appendChild(fragment);
    }

    createPages() {
        const pages = [];
        for (let i = 0; i < this.scrapItems.length; i += this.itemsPerPage) {
            pages.push(this.scrapItems.slice(i, i + this.itemsPerPage));
        }
        return pages;
    }

    createPageElement(items, pageNumber) {
        const page = document.createElement('section');
        page.className = 'page';
        page.id = `page_${pageNumber}`;
        page.dataset.value = pageNumber;

        items.forEach(item => {
            const article = this.createScrapArticle(item);
            page.appendChild(article);
        });

        return page;
    }

    createScrapArticle(item) {
        const article = document.createElement('article');
        article.className = 'row border border-secondary-subtle mb-2';

        article.innerHTML = `
            ${this.createScrapColumn(item.scrap1)}
            ${this.createScrapColumn(item.scrap2)}
        `;

        return article;
    }

    createScrapColumn(scrap) {
        return `
            <div class="col-6 border-end border-secondary-subtle p-3">
                <div class="row_log_nc mb-2">
                    <strong>${scrap.title}</strong>
                </div>
                <div class="row_log_nc d-flex justify-content-end mb-2">
                    <button class="btn_logNC btn btn-sm btn-outline-primary d-flex align-items-center gap-2" data-nc-code="${scrap.title}" type="button">
                        <img src="../master/imgs/log_NC-Icon.png" alt="log" style="height: 16px;">
                        <span>Grabar en log NC</span>
                    </button>
                </div>
                <div class="row_log_nc">
                    <h6 class="txtDescription mb-0">${scrap.description}</h6>
                </div>
            </div>
        `;
    }
}




// ============================================
// COMPLETE ORDER MANAGER - VERSIÓN ADAPTADA
// ============================================

class CompleteOrderManager {
    constructor() {
        this.modal = document.getElementById('modalCompleteSection');
        this.confirmModal = document.getElementById('complete_confirm');
        this.lcQuestions = document.getElementById('completeLcQuestions');
        this.completeParameters = document.getElementById('completeParameters');
        this.confirmOrderRow = document.getElementById('confirmOrderRow');
        this.tableWorkContainer = document.querySelector('#tableWorkContainer');
        this.dashboardMessages = document.querySelector('.dashboard_mensages');
        this.confirmBox = document.getElementById("confirm-box");
        this.modalInstance = this.modal ? new bootstrap.Modal(this.modal) : null;
        this.confirmModalInstance = this.confirmModal ? new bootstrap.Modal(this.confirmModal) : null;
        this.buttons = {
            btnComplete: document.getElementById('btn-completar'),
            saveBtn: document.getElementById("btnCompleteSave"),
            cancelBtn: document.getElementById("btnCompleteCancel"),
            returnBtn: document.getElementById("btnCompleteReturn"),
            acceptBtn: document.getElementById("btn_accept"),
            btnCompleteLogin: document.querySelector(".btn_login")
        }




        this.init();
    }

    init() {

        this.attachEventListeners();
    }




    attachEventListeners() {


        this.buttons.btnComplete.addEventListener("click", e => {
            this.openModal();
        })
        this.buttons.btnCompleteLogin.addEventListener("click", e => {
            UI.hide(this.confirmBox)
        })


        this.buttons.saveBtn.addEventListener("click", e => {
            this.handleSave();
        })
        this.buttons.cancelBtn.addEventListener('click', e => {

            this.closeModal();
        })
        this.buttons.returnBtn.addEventListener('click', e => {
            this.closeModal();
        })
        this.buttons.acceptBtn.addEventListener('click', e => {

            this.completeOrders();
        })

        this.confirmModal.addEventListener("click", e => {

            if (e.target.classList.contains("btn_close_confirm") || e.target.parentElement.classList.contains("btn_close_confirm")) {
                this.confirmModalInstance.hide()
            }
        })

        this.confirmOrderRow.addEventListener("click", e => {

            this.confirmOrderRow.classList.toggle('row-active')
        })

    }



    checkLineClearance() {
        return Storage.get(CONFIG.STORAGE_KEYS.LINE_CLEARANCE) !== null;
    }

    checkAtLeastOneRowActive() {
        const activeRows = this.tableWorkContainer?.querySelectorAll('tr.row-active');
        return activeRows && activeRows.length > 0;
    }

    openModal() {
        if (!this.checkLineClearance()) {
            UI.showMessage(
                this.dashboardMessages,
                'error',
                'User has changed, please perform Line Clearance (Message 22501)'
            );
            return;
        }

        if (!this.checkAtLeastOneRowActive()) {
            UI.showMessage(
                this.dashboardMessages,
                'error',
                'SFC is missing (Message 12906)'
            );
            return;
        }
        UI.show(this.confirmBox)
        CONFIG.LCQUESTIONS.loadLcQuestions(this.lcQuestions)
        CONFIG.PARAMETERS.loadParameters(this.completeParameters)
        this.resetModalSections();

        this.modalInstance?.show();
    }

    closeModal() {
        this.resetModalSections();
        this.modalInstance?.hide();
        this.cleanupModalBackdrop();
    }

    resetModalSections() {
        if (!this.modal) return;

        const sections = this.modal.querySelectorAll('.modal-body');
        sections.forEach(section => UI.hide(section));

        const firstSection = this.modal.querySelector('.first_section');
        UI.show(firstSection);

        const errorSelects = this.modal.querySelector('.complete_error_selects');
        const errorEmpty = this.modal.querySelector('.complete_error_empy');

        UI.hide(errorSelects);
        UI.hide(errorEmpty);
    }








    handleSave() {
        const currentSection = this.getCurrentSection();
        if (!currentSection) return;

        const position = parseInt(currentSection.dataset.position);

        if (position === 1) {
            this.validateAndProgressSection1(currentSection);
        } else if (position === 2) {
            this.validateAndProgressSection2(currentSection);
        } else {
            this.confirmModalInstance.show()
        }
    }

    getCurrentSection() {
        if (!this.modal) return null;

        const sections = this.modal.querySelectorAll('.modal-body');
        for (const section of sections) {
            if (!section.classList.contains('visually-hidden') &&
                !section.classList.contains('d-none')) {
                return section;
            }
        }
        return null;
    }

    validateAndProgressSection1(section) {
        const selects = section.querySelectorAll('select');
        const validSelects = Array.from(selects).filter(s => s.selectedIndex === 1).length;
        const errorElement = this.modal?.querySelector('.complete_error_selects');

        if (validSelects === selects.length) {
            this.progressToNextSection(section);
            UI.hide(errorElement);
        } else {
            UI.show(errorElement);
        }
    }

    validateAndProgressSection2(section) {
        const inputs = section.querySelectorAll('input[type="text"]');
        let regex = /^\d+$/
        const validInputs = Array.from(inputs).filter(input => regex.test(input.value) == true).length;
        const errorElement = this.modal?.querySelector('.complete_error_empy');

        if (validInputs === inputs.length) {
            this.openConfirmModal();
            UI.hide(errorElement);
        } else {
            UI.show(errorElement);
        }
    }

    progressToNextSection(section) {
        UI.hide(section);
        const nextSection = section.nextElementSibling;
        if (nextSection && nextSection.classList.contains('modal-body')) {
            UI.show(nextSection);
        }
    }

    openConfirmModal() {
        this.closeModal();

        const userCode = this.confirmModal?.querySelector('#userCode');
        const orderNumberComplete = this.confirmModal?.querySelector('#order_number_complete');
        const shopOrderRow = document.querySelector('#shopOrder_row');
        const orderNumber = shopOrderRow?.querySelector('#order_number');

        if (userCode) userCode.value = Storage.get('code1') || '';
        if (orderNumberComplete && orderNumber) {
            orderNumberComplete.textContent = orderNumber.textContent;
        }

        const confirmWindow = this.confirmModal?.querySelector('.confirm_window');
        UI.show(confirmWindow);

        this.confirmModalInstance?.show();
    }

    closeConfirmWindow() {
        const confirmWindow = this.confirmModal?.querySelector('.confirm_window');
        UI.hide(confirmWindow);
    }

    closeConfirmModal() {
        this.closeConfirmWindow();
        this.confirmModalInstance?.hide();
        this.cleanupModalBackdrop();
    }


    completeOrders() {


        if (!this.confirmOrderRow.classList.contains('row-active')) {

            swal.fire({
                title: "Error",
                text: "Tienes que solucionar la orden",
                icon: "Error"
            })
            return;
        }

        const activeRows = Array.from(
            this.tableWorkContainer?.querySelectorAll('tr.row-active') || []
        );

        activeRows.forEach(row => row.remove());

        this.updateWorkTableStorage();
        this.closeConfirmModal();
        UI.showMessage(
            this.dashboardMessages,
            'success',
            'All CR9A053631091 have been Close'
        );
    }

    updateWorkTableStorage() {
        const remainingRows = this.tableWorkContainer?.querySelectorAll('tr');

        if (!remainingRows || remainingRows.length === 0) {
            Storage.set(CONFIG.STORAGE_KEYS.WORK_TABLE, '0');
        } else {
            Storage.set(CONFIG.STORAGE_KEYS.WORK_TABLE, this.tableWorkContainer.innerHTML);
        }
    }

    cleanupModalBackdrop() {
        const backdrops = document.querySelectorAll('.modal-backdrop');
        backdrops.forEach(backdrop => backdrop.remove());

        document.body.classList.remove('modal-open');
        document.body.style.overflow = '';
        document.body.style.paddingRight = '';
    }
}
// ============================================
// ORDER SEARCH MANAGER - VERSIÓN ADAPTADA
// ============================================
class orderSearchManager {
    constructor() {
        this.orderSearchRow = d.getElementById('orderSearchRow')
        this.orderCode = d.getElementById('orderCode')
        this.tableWorkContainer = d.getElementById('tableWorkContainer')
        this.modal = d.getElementById('searchModal')
        this.btnOk = d.getElementById('btnOk')
        this.modalInstance = null
        this.init()

    }




    //Iniciar el proceso

    init() {
        this.attachEventListeners();
        // Inicializar instancia del modal de Bootstrap
        if (this.modal && typeof bootstrap !== 'undefined') {
            this.modalInstance = new bootstrap.Modal(this.modal);
        }
    }


    /* ========================================
       GESTIÓN DE EVENTOS
       ======================================== */
    attachEventListeners() {

        this.orderSearchRow.addEventListener('click', e => {
            e.target.parentElement.classList.toggle("row-active")
        })

        this.btnOk.addEventListener("click", e => {
            if (this.orderSearchRow.classList.contains("row-active")) {
                let input = d.getElementById('seach_order_input')
                input.value = this.orderCode.textContent
                this.modalInstance.hide()
            } else {
                swal.fire({
                    title: "Ups..",
                    text: "Debes seleccionar la orden",
                    icon: "error"

                })
            }
        })
    }


}




// ============================================
// START SECTION MANAGER - VERSIÓN ADAPTADA
// ============================================

class StartSectionManager {
    constructor() {
        this.modal = document.getElementById('modalStartSection');
        this.btnStart = document.getElementById('btnStartSection')
        this.saveBtn = document.querySelector('#saveStartBtn');
        this.searchOrderInput = document.querySelector('#seach_order_input');
        this.closeBtn = document.querySelector('#close_lineClearance');
        this.cancelBtn = document.querySelector('#cancelStartBtn');
        this.returnBtn = document.querySelector('#returnStartBtn');
        this.shopOrderRow = document.querySelector('#shopOrder_row');
        this.dashboardMessages = document.querySelector('.dashboard_mensages');
        this.tableWorkContainer = document.getElementById('tableWorkContainer')
        this.modalInstance = this.modal ? new bootstrap.Modal(this.modal) : null;

        this.init();
    }

    init() {

        this.attachEventListeners();
    }


    attachEventListeners() {


        this.btnStart.addEventListener("click", e => {
            this.openModal();
        })
        this.saveBtn.addEventListener("click", e => {
            this.handleSave();
        })
        this.cancelBtn.addEventListener('click', e => {
            this.closeModal();
        })
        this.returnBtn.addEventListener('click', e => {
            this.closeModal();
        })



    }



    checkLineClearance() {
        return Storage.get(CONFIG.STORAGE_KEYS.LINE_CLEARANCE) !== null;
    }

    checkSearchOrderInput() {
        return this.searchOrderInput &&
            this.searchOrderInput.value.trim() !== '';
    }
    checkOrderResouce() {
        return sessionStorage.getItem('resourceData')
    }

    openModal() {

        if (!this.checkLineClearance()) {
            UI.showMessage(
                this.dashboardMessages,
                'error',
                'User has changed, please perform Line Clearance (Message 22501)'
            );
            return;
        }

        if (!this.checkSearchOrderInput()) {
            UI.showMessage(
                this.dashboardMessages,
                'error',
                'SFC is missing (Message 12906)'
            );
            return;
        }
        if (!this.checkOrderResouce()) {
            UI.showMessage(
                this.dashboardMessages,
                'error',
                'Debes ingresar la shop order y los equipos'
            );
            this.searchOrderInput.value = ""
            return;
        }

        this.resetModalSections();



        this.modalInstance?.show();
    }

    closeModal() {
        this.resetModalSections();

        this.modalInstance?.hide();
    }

    resetModalSections() {
        if (!this.modal) return;

        const sections = this.modal.querySelectorAll('.modal-body');
        sections.forEach(section => UI.hide(section));

        const firstSection = this.modal.querySelector('.first_section');
        UI.show(firstSection);

        const errorEmpty = this.modal.querySelector('.start_error_empy');
        const errorSelects = this.modal.querySelector('.start_error_selects');

        UI.hide(errorEmpty);
        UI.hide(errorSelects);
    }

    handleSave() {
        const currentSection = this.getCurrentSection();
        if (!currentSection) return;

        const position = parseInt(currentSection.dataset.position);

        if (position === 1) {

            this.validateAndProgressInputs(currentSection);
            currentSection.querySelector("input").value = ""
        } else if (position === 2) {
            this.validateAndProgressSelects(currentSection);
        } else if (position === 3) {
            this.validateAndProgressInputs(currentSection);
        } else if (position === 4) {
            this.validateAndCompleteStart(currentSection);
            currentSection.querySelector("select").selectedIndex = 0
        }
    }

    getCurrentSection() {
        if (!this.modal) return null;

        const sections = this.modal.querySelectorAll('.modal-body');
        for (const section of sections) {
            if (!section.classList.contains('visually-hidden') &&
                !section.classList.contains('d-none')) {
                return section;
            }
        }
        return null;
    }

    validateAndProgressInputs(section) {
        const inputs = section.querySelectorAll('input');
        let regex = /^\d+$/
        const validInputs = Array.from(inputs).filter(input => regex.test(input.value) == true).length;
        const errorElement = this.modal?.querySelector('.start_error_empy');

        if (validInputs === inputs.length) {
            this.progressToNextSection(section);
            UI.hide(errorElement);
        } else {
            UI.show(errorElement);
        }
    }

    validateAndProgressSelects(section) {
        const selects = section.querySelectorAll('select');
        const validSelects = Array.from(selects).filter(s => s.selectedIndex === 1).length;
        const errorElement = this.modal?.querySelector('.start_error_selects');

        if (validSelects === selects.length) {
            this.progressToNextSection(section);
            UI.hide(errorElement);
        } else {
            UI.show(errorElement);
        }
    }

    validateAndCompleteStart(section) {
        const selects = section.querySelectorAll('select');
        const validSelects = Array.from(selects).filter(s => s.selectedIndex === 1).length;
        const errorElement = this.modal?.querySelector('.start_error_selects');

        if (validSelects === selects.length) {
            this.completeStart();
            UI.hide(errorElement);
        } else {
            UI.show(errorElement);
        }
    }

    progressToNextSection(section) {
        UI.hide(section);
        const nextSection = section.nextElementSibling;
        if (nextSection && nextSection.classList.contains('modal-body')) {
            if (nextSection.dataset.position == 2) {
                CONFIG.LCQUESTIONS.loadLcQuestions(nextSection)
            }
            if (nextSection.dataset.position == 3) {
                CONFIG.PARAMETERS.loadParameters(nextSection)
            }
            UI.show(nextSection);
        }
    }

    completeStart() {
        UI.show(this.shopOrderRow);
        this.tableWorkContainer.insertAdjacentHTML('beforeend', `
        <tr class="OrderRow" >

                      <td >CR9A053631091</td>
                      <td>DWF L Cushion PP NIR</td>
                      <td class="orderValue">3000</td>
                      <td>
                        <span class="status-indicator status-green"></span>
                        Activo
                      </td>
                      <td>-</td>
                      <td class="order_number">${Storage.get(CONFIG.STORAGE_KEYS.ORDER)}</td>
                    </tr>
        `)
        seccionStorage.setItem("workTableContainer", this.tableWorkContainer.innerHTML)
        this.closeModal();

        UI.showMessage(
            this.dashboardMessages,
            'success',
            'Start process completed successfully'
        );
    }

    cleanupModalBackdrop() {
        const backdrops = document.querySelectorAll('.modal-backdrop');
        backdrops.forEach(backdrop => backdrop.remove());

        document.body.classList.remove('modal-open');
        document.body.style.overflow = '';
        document.body.style.paddingRight = '';
    }
}

// ============================================
// DATA COLLECTION MANAGER - VERSIÓN CORREGIDA
// ============================================

class DataCollectionManager {
    constructor() {
        this.modal = document.querySelector('#dataCollection');
        this.collectionList = document.querySelector('#dc_collection_list');
        this.dataLogContainer = document.querySelector('#data_log_container');
        this.expTinta = document.querySelector('#exp_tinta');
        this.fsEntrega = document.querySelector('#fs_entrega');
        this.parametersFooter = document.querySelector('#parameters_footer');
        this.shopOrderRow = document.querySelector('#shopOrder_row');
        this.dashboardMessages = document.querySelector('.dashboard_mensages');
        this.tableWorkContainer = document.querySelector('#tableWorkContainer');

        this.currentProcessType = null;
        this.activeButton = null;

        this.processTypes = {
            EXP_TINTA: 'FECHA EXP NIR',
            FIRST_SHOT: 'DCGTM_ENTREGA_PRIMERA_PIEZA/A',
            FIRST_SHOT_NIR: 'DCGTM_ENTREGA_PRIMERA_PIEZA_NIR/A',  
            PARAMETERS: 'PARAMETERS'
        };

        this.errorClasses = {
            emptyParams: '.parameters_error_empy',
            wrongFormat: '.exp_wrong_format',
            errorSelects: '.dataCL_error_selects'
        };

        this.init();
    }

    init() {
        this.attachEventListeners();
        this.setupRegisterButtons();
    }

    attachEventListeners() {
        document.addEventListener('click', (e) => {
            const target = e.target;

            // Abrir modal
            if (target.closest('#data_collection_btn')) {
                this.openModal();
                return;
            }

            // Guardar
            if (target.closest('#saveBtn')) {
                this.handleSave();
                return;
            }

            // Cerrar modal
            if (target.closest('#close_data_collection') ||
                target.closest('#cancelBtn') ||
                target.closest('#returnBtn')) {
                this.closeModal();
                return;
            }
        });
    }

    setupRegisterButtons() {
        if (!this.collectionList) return;

        const registerButtons = this.collectionList.querySelectorAll('.btn-register');

        registerButtons.forEach(btn => {
            btn.addEventListener('click', (e) => {
                const row = e.target.closest('tr');
                const processName = row?.querySelector('.process-name')?.textContent.trim();

                if (!processName) return;

                this.activeButton = e.target.closest(".btn-register");
                
                this.activeButton.setAttribute('disabled', '');

                if (processName.includes('FECHA EXP NIR')) {
                    this.switchToDataEntry(this.processTypes.EXP_TINTA);
                } else if (processName === 'DCGTM_ENTREGA_PRIMERA_PIEZA/A') {
                    this.switchToDataEntry(this.processTypes.FIRST_SHOT);
                } else if (processName === 'DCGTM_ENTREGA_PRIMERA_PIEZA_NIR/A') {
                    this.switchToDataEntry(this.processTypes.FIRST_SHOT);
                } else {
                    this.switchToDataEntry(this.processTypes.PARAMETERS);
                }
            });
        });
    }

    checkLineClearance() {
        return Storage.get(CONFIG.STORAGE_KEYS.LINE_CLEARANCE) !== null;
    }

    checkShopOrderActive() {
        const rows = this.tableWorkContainer?.querySelectorAll('tr');
        const activeRows = Array.from(rows || []).filter(row => row.classList.contains('row-active'));
        return activeRows.length > 0;
    }

    openModal() {
        if (!this.checkLineClearance()) {
            UI.showMessage(
                this.dashboardMessages,
                'error',
                'User has changed, please perform Line Clearance (Message 22501)'
            );
            return;
        }

        if (!this.checkShopOrderActive()) {
            UI.showMessage(
                this.dashboardMessages,
                'error',
                'SFC is missing (Message 12906)'
            );
            return;
        }

        if (this.modal) {
            this.modal.classList.add('show');
            this.modal.style.display = 'block';
            this.resetModal();
        }
    }

    closeModal() {
        if (!this.modal) return;

        this.modal.classList.remove('show');
        this.modal.style.display = 'none';
        this.resetModal();
        this.clearErrors();

        if (this.activeButton) {
            this.activeButton.removeAttribute('disabled');
            this.activeButton = null;
        }

        this.currentProcessType = null;
    }

    resetModal() {
        UI.show(this.collectionList);
        UI.hide(this.dataLogContainer);
        UI.hide(this.expTinta);
        UI.hide(this.fsEntrega);
        UI.hide(this.parametersFooter);
    }

    switchToDataEntry(processType) {
        this.currentProcessType = processType;

        UI.hide(this.collectionList);
        UI.show(this.parametersFooter);

        switch (processType) {
            case this.processTypes.EXP_TINTA:
                UI.show(this.expTinta);
                break;
            case this.processTypes.FIRST_SHOT:
                UI.show(this.fsEntrega);
                break;
            case this.processTypes.PARAMETERS:
                CONFIG.PARAMETERS.loadParameters(this.dataLogContainer)
                UI.show(this.dataLogContainer);
                break;
        }
    }

    handleSave() {
        switch (this.currentProcessType) {
            case this.processTypes.PARAMETERS:
                this.validateParameters();
                break;
            case this.processTypes.EXP_TINTA:
                this.validateExpTinta();
                break;
            case this.processTypes.FIRST_SHOT:
                this.validateFirstShot();
                break;
        }
    }

    validateParameters() {
        if (!this.dataLogContainer || this.dataLogContainer.classList.contains('visually-hidden')) {
            return;
        }

        const inputs = this.dataLogContainer.querySelectorAll('input[type="text"]');
        let regex = /^\d+$/
        const validInputs = Array.from(inputs).filter(input => regex.test(input.value) == true).length;

        if (validInputs === inputs.length) {
            this.clearErrors();
            this.returnToCollectionList();
        } else {
            this.showError('emptyParams');
        }
    }

    validateExpTinta() {
        if (!this.expTinta || this.expTinta.classList.contains('visually-hidden')) {
            return;
        }

        const inputs = this.expTinta.querySelectorAll('input[type="text"]');
        const emptyInputs = Array.from(inputs).filter(input => input.value.trim() === '');
        const invalidFormats = Array.from(inputs).filter(input => {
            return input.value.trim() !== '' && !Validation.isValidDate(input.value);
        });

        if (emptyInputs.length > 0) {
            this.showError('emptyParams');
            return;
        }

        if (invalidFormats.length > 0) {
            this.clearError('emptyParams');
            this.showError('wrongFormat');
            return;
        }

        this.clearErrors();
        this.returnToCollectionList();
    }

    validateFirstShot() {
        if (!this.fsEntrega || this.fsEntrega.classList.contains('visually-hidden')) {
            return;
        }

        const selects = this.fsEntrega.querySelectorAll('select');
        const validSelects = Array.from(selects).filter(select => select.selectedIndex === 1).length;

        if (validSelects === selects.length) {
            this.clearErrors();
            this.returnToCollectionList();
        } else {
            this.showError('errorSelects');
        }
    }

    returnToCollectionList() {
        UI.hide(this.dataLogContainer);
        UI.hide(this.expTinta);
        UI.hide(this.fsEntrega);
        UI.hide(this.parametersFooter);
        UI.show(this.collectionList);

        if (this.activeButton) {
            this.activeButton = null;
        }

        this.currentProcessType = null;
        this.clearErrors();
    }

    showError(errorType) {
        const errorSelector = this.errorClasses[errorType];
        const errorElement = this.modal?.querySelector(errorSelector);
        UI.show(errorElement);
    }

    clearError(errorType) {
        const errorSelector = this.errorClasses[errorType];
        const errorElement = this.modal?.querySelector(errorSelector);
        UI.hide(errorElement);
    }

    clearErrors() {
        Object.values(this.errorClasses).forEach(selector => {
            const errorElement = this.modal?.querySelector(selector);
            UI.hide(errorElement);
        });
    }


}

document.addEventListener('DOMContentLoaded', () => {

    if (!sessionStorage.getItem("isAuth")) {
        window.location.href = '/MESSimulator/index.aspx'
    }
    new WorkTableManager();
    new ResourceManager();
    new LineClearanceManager();
    new SplitManager();
    new StartSectionManager();
    new LogNCManager();
    new CompleteOrderManager();
    new DataCollectionManager();
    new orderSearchManager();

});