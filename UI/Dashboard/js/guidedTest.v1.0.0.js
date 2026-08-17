// ============================================
// SISTEMA DE TOURS GUIADOS INTERACTIVOS
// ============================================

/**
 * Clase para gestionar el overlay oscuro y resaltar elementos
 */
class GuideOverlay {
    constructor() {
        this.overlay = document.querySelector('.guideZone');
        this.scrollableElement = null;
        this.wheelHandler = null;
        this.activeScrollListeners = new Map();
        this.dblClickHandler = null;

        if (!this.overlay) {
            console.error('Elemento .guideZone no encontrado');
        }
    }

    show() {
        if (this.overlay) {
            this.overlay.style.display = 'block';
            this.overlay.style.opacity = '1';
            this.overlay.style.pointerEvents = 'auto';

            // ✅ PREVENIR DOBLE CLIC EN EL OVERLAY
            this.preventDoubleClick();
        }
    }

    preventDoubleClick() {
        if (this.overlay && !this.dblClickHandler) {
            this.dblClickHandler = (e) => {
                e.preventDefault();
                e.stopPropagation();
                e.stopImmediatePropagation();
              
            };

            this.overlay.addEventListener('dblclick', this.dblClickHandler, {
                capture: true,
                passive: false
            });
        }
    }

    hide() {
        if (this.overlay) {
            this.overlay.style.opacity = '0';

            // ✅ LIMPIAR LISTENER DE DOBLE CLIC
            if (this.dblClickHandler) {
                this.overlay.removeEventListener('dblclick', this.dblClickHandler, { capture: true });
                this.dblClickHandler = null;
            }

            setTimeout(() => {
                this.overlay.style.display = 'none';
                this.overlay.style.clipPath = 'none';
                this.overlay.style.webkitClipPath = 'none';
                this.overlay.style.pointerEvents = 'none';
                this.disableScroll();
            }, 300);
        }
    }

    updateClipPath(element, padding = 0) {
        if (!this.overlay || !element) return null;

        const rect = this.getRealRect(element);
        const viewportTop = Math.max(0, rect.top);
        const viewportLeft = Math.max(0, rect.left);
        const viewportRight = Math.min(window.innerWidth, rect.right);
        const viewportBottom = Math.min(window.innerHeight, rect.bottom);

        const clipPath = `polygon(
            0% 0%,
            0% 100%,
            ${viewportLeft - padding}px 100%,
            ${viewportLeft - padding}px ${viewportTop - padding}px,
            ${viewportRight + padding}px ${viewportTop - padding}px,
            ${viewportRight + padding}px ${viewportBottom + padding}px,
            ${viewportLeft - padding}px ${viewportBottom + padding}px,
            ${viewportLeft - padding}px 100%,
            100% 100%,
            100% 0%
        )`;

        this.overlay.style.clipPath = clipPath;
        this.overlay.style.webkitClipPath = clipPath;

        return rect;
    }

    getRealRect(element) {
        const rect = element.getBoundingClientRect();
        return {
            top: rect.top,
            left: rect.left,
            right: rect.right,
            bottom: rect.bottom,
            width: rect.width,
            height: rect.height
        };
    }

    enableInteraction() {
        if (this.overlay) {
            this.overlay.style.pointerEvents = 'auto';
        }
    }

    enableScroll(elementOrSelector) {
        const element = typeof elementOrSelector === 'string'
            ? document.querySelector(elementOrSelector)
            : elementOrSelector;

        if (!element) {
            console.warn('Elemento para scroll no encontrado:', elementOrSelector);
            return;
        }

        this.activeScrollListeners.forEach((listener) => {
            listener.element.removeEventListener(listener.event, listener.handler);
        });
        this.activeScrollListeners.clear();

        this.scrollableElement = element;

        element.style.pointerEvents = 'auto';
        element.style.position = 'relative';
        element.style.zIndex = '10001';
        element.style.overflowY = 'auto';
        element.style.touchAction = 'pan-y';
        element.style.overscrollBehavior = 'contain';
        element.classList.add('guide-scrollable-area');

        if (this.overlay) {
            this.overlay.style.pointerEvents = 'none';
            this.overlay.style.zIndex = '9998';
        }

        const wheelHandlerElement = (e) => {
            e.stopPropagation();
            setTimeout(() => {
                if (this.scrollableElement) {
                    this.updateClipPath(this.scrollableElement);
                }
            }, 10);
        };

        element.addEventListener('wheel', wheelHandlerElement, { passive: true });
        this.activeScrollListeners.set('element-wheel', {
            element,
            event: 'wheel',
            handler: wheelHandlerElement
        });

        this.wheelHandler = (e) => {
            const targetElement = document.elementFromPoint(e.clientX, e.clientY);

            if (this.scrollableElement &&
                (this.scrollableElement.contains(targetElement) ||
                    this.scrollableElement === targetElement)) {

                e.preventDefault();
                e.stopPropagation();

                this.scrollableElement.scrollTop += e.deltaY;

                setTimeout(() => {
                    if (this.scrollableElement) {
                        this.updateClipPath(this.scrollableElement);
                    }
                }, 10);
            }
        };

        if (this.overlay) {
            this.overlay.addEventListener('wheel', this.wheelHandler, {
                passive: false,
                capture: true
            });

            this.activeScrollListeners.set('overlay-wheel', {
                element: this.overlay,
                event: 'wheel',
                handler: this.wheelHandler
            });
        }

        let touchStartY = 0;

        const touchStartHandler = (e) => {
            touchStartY = e.touches[0].clientY;
        };

        const touchMoveHandler = (e) => {
            const touchY = e.touches[0].clientY;
            const deltaY = touchStartY - touchY;

            this.scrollableElement.scrollTop += deltaY;
            touchStartY = touchY;

            setTimeout(() => {
                if (this.scrollableElement) {
                    this.updateClipPath(this.scrollableElement);
                }
            }, 10);

            e.preventDefault();
        };

        element.addEventListener('touchstart', touchStartHandler, { passive: true });
        element.addEventListener('touchmove', touchMoveHandler, { passive: false });

        this.activeScrollListeners.set('touch-start', {
            element,
            event: 'touchstart',
            handler: touchStartHandler
        });

        this.activeScrollListeners.set('touch-move', {
            element,
            event: 'touchmove',
            handler: touchMoveHandler
        });
    }

    disableScroll(keepClipPath = false) {
        this.activeScrollListeners.forEach((listener) => {
            listener.element.removeEventListener(listener.event, listener.handler);
        });
        this.activeScrollListeners.clear();

        if (this.scrollableElement) {
            this.scrollableElement.style.pointerEvents = '';
            this.scrollableElement.style.zIndex = '';
            this.scrollableElement.style.overflowY = '';
            this.scrollableElement.style.touchAction = '';
            this.scrollableElement.style.overscrollBehavior = '';
            this.scrollableElement.classList.remove('guide-scrollable-area');

            if (!keepClipPath) {
                this.scrollableElement = null;
            }
        }

        if (this.overlay && !keepClipPath) {
            this.overlay.style.zIndex = '9998';
        }

        this.wheelHandler = null;
    }
}

/**
 * Clase para gestionar tooltips informativos
 */
class Tooltip {
    constructor(config) {
        this.config = config;
        this.element = null;
        this.createTooltip();
    }

    createTooltip() {
        this.element = document.createElement('div');
        this.element.className = 'guide-tooltip';
        this.element.innerHTML = `
            <div class="guide-tooltip-content">
                <p>${this.config.text}</p>
            </div>
        `;

        Object.assign(this.element.style, {
            position: 'fixed',
            zIndex: '10000',
            backgroundColor: '#fff',
            padding: '10px',
            borderRadius: '8px',
            boxShadow: '0 4px 12px rgba(0,0,0,0.3)',
            maxWidth: '350px',
            animation: 'fadeIn 0.3s ease'
        });

        document.body.appendChild(this.element);
    }

    updatePosition(rect, position = 'top') {
        const offset = 20;

        switch (position) {
            case 'top':
                this.element.style.left = `${rect.left + rect.width / 2}px`;
                this.element.style.top = `${rect.top - offset}px`;
                this.element.style.transform = 'translate(-50%, -100%)';
                break;
            case 'bottom':
                this.element.style.left = `${rect.left + rect.width / 2}px`;
                this.element.style.top = `${rect.bottom + offset}px`;
                this.element.style.transform = 'translateX(-50%)';
                break;
            case 'left':
                this.element.style.left = `${rect.left - offset}px`;
                this.element.style.top = `${rect.top + rect.height / 2}px`;
                this.element.style.transform = 'translate(-100%, -50%)';
                break;
            case 'right':
                this.element.style.left = `${rect.right + offset}px`;
                this.element.style.top = `${rect.top + rect.height / 2}px`;
                this.element.style.transform = 'translateY(-50%)';
                break;
        }
    }

    remove() {
        if (this.element && this.element.parentNode) {
            this.element.style.opacity = '0';
            setTimeout(() => this.element.remove(), 300);
        }
    }
}

/**
 * Clase para validar elementos del formulario
 */
class ValidationManager {
    static validateElement(element, regex) {
        if (!element || !regex) return true;

        const pattern = typeof regex === 'string' ? new RegExp(regex) : regex;
        const value = element.value || element.textContent;

        return pattern.test(value.trim());
    }

    static validateMultipleElements(selector, regex) {
        const elements = document.querySelectorAll(selector);
        const errors = [];
        let isValid = true;

        elements.forEach((element, index) => {
            if (!this.validateElement(element, regex)) {
                const value = element.value || element.textContent;
                errors.push(`Elemento ${index + 1} con valor "${value.trim()}" no es válido`);
                isValid = false;
            }
        });

        return { isValid, errors, totalElements: elements.length };
    }
}

/**
 * Clase para gestionar elementos del DOM
 */
class ElementManager {
    static getElement(selector, position = null) {
        const firstChar = String(selector).charAt(0).trim();
        let element;

        if (position !== null) {
            element = document.querySelectorAll(selector)[position];
        } else if (firstChar === "#" || firstChar === ".") {
            element = document.querySelector(selector);
        } else {
            element = document.getElementById(selector);
        }

        return element;
    }

    static async waitForElement(selector, timeout = 10000) {
        return new Promise((resolve, reject) => {
            const element = document.querySelector(selector);

            if (element) {
                resolve(selector);
                return;
            }

            const observer = new MutationObserver((mutations, obs) => {
                const element = document.querySelector(selector);
                if (element) {
                    obs.disconnect();
                    resolve(selector);
                }
            });

            observer.observe(document.body, {
                childList: true,
                subtree: true
            });

            setTimeout(() => {
                observer.disconnect();
                reject(new Error(`Timeout esperando elemento: ${selector}`));
            }, timeout);
        });
    }
}

/**
 * Clase para un paso individual del tour
 */
class GuideStep {
    constructor(selector, options = {}) {
        this.selector = selector;
        this.options = {
            tooltipText: options.tooltipText || 'Haz clic aquí',
            tooltipPosition: options.tooltipPosition || 'top',
            onClose: options.onClose || null,
            waitForAction: options.waitForAction || false,
            allowInteraction: options.allowInteraction || false,
            autoSelectValue: options.autoSelectValue || null,
            elementPosition: options.elementPosition || null,
            allowScroll: options.allowScroll || false,
            scrollSelector: options.scrollSelector || null,
            autoAdvanceDelay: options.autoAdvanceDelay || null
        };

        this.element = null;
        this.overlay = new GuideOverlay();
        this.tooltip = null;
        this.shouldClose = false;
        this.stopTourOnClose = false;
        this.eventHandlers = {};
        this.autoAdvanceTimer = null;
        // ✅ Control de clic único
        this.hasBeenClicked = false;
        this.clickTimeout = null;
    }

    activate() {
        this.element = ElementManager.getElement(this.selector, this.options.elementPosition);

        if (!this.element) {
            console.error(`Elemento no encontrado: ${this.selector}`);
            return null;
        }

        const isSelect = this.element.tagName === 'SELECT';

        this.overlay.show();
        this.element.classList.add('guideActive');

        if (this.options.allowScroll) {
            const scrollTarget = this.options.scrollSelector
                ? this.options.scrollSelector
                : this.selector;
            this.overlay.enableScroll(scrollTarget);
        }

        if (this.options.allowInteraction || isSelect) {
            this.element.classList.remove('guideActive');
            this.element.style.pointerEvents = 'auto';
            this.overlay.enableInteraction();
        }

        const rect = this.overlay.updateClipPath(this.element);
        this.tooltip = new Tooltip({ text: this.options.tooltipText });
        this.tooltip.updatePosition(rect, this.options.tooltipPosition);
        this.setupEventListeners(isSelect);

        if (this.options.autoAdvanceDelay) {
            this.autoAdvanceTimer = setTimeout(() => {
                this.shouldClose = true;
                this.close();
            }, this.options.autoAdvanceDelay);
        }

        return this;
    }

    setupEventListeners(isSelect) {
        // ✅ PREVENIR DOBLE CLIC EN EL ELEMENTO
        this.eventHandlers.dblclick = (e) => {
            e.preventDefault();
            e.stopPropagation();
            e.stopImmediatePropagation();
            
        };

        this.element.addEventListener('dblclick', this.eventHandlers.dblclick, {
            capture: true,
            passive: false
        });

        // ✅ CONTROL DE CLIC ÚNICO (solo para elementos que NO requieren interacción)
        this.eventHandlers.click = (e) => {
            // ⚠️ IMPORTANTE: NO bloquear clics en elementos con interacción o validación
            if (this.options.waitForAction || this.options.allowInteraction || isSelect) {
                return; // Permitir clics normales
            }

            // Bloquear múltiples clics rápidos
            if (this.hasBeenClicked) {
                e.preventDefault();
                e.stopPropagation();
                e.stopImmediatePropagation();
                return;
            }

            // Marcar como clickeado
            this.hasBeenClicked = true;

            // Deshabilitar temporalmente el elemento
            this.element.style.opacity = '0.6';
            this.element.style.cursor = 'wait';

            // Procesar el cierre después del delay normal
            this.clickTimeout = setTimeout(() => {
                this.shouldClose = true;
                this.close();
            }, 300);
        };

        this.element.addEventListener('click', this.eventHandlers.click, {
            capture: true,
            passive: false
        });

        // ✅ MANEJO DE SELECT con auto-selección
        if (isSelect && this.options.autoSelectValue) {
            this.eventHandlers.change = (e) => {
                const selectedValue = e.target.value;
                if (selectedValue === this.options.autoSelectValue) {
                    // NO bloquear aquí, permitir que funcione normalmente
                    setTimeout(() => {
                        this.shouldClose = true;
                        this.close();
                    }, 500);
                }
            };
            this.element.addEventListener('change', this.eventHandlers.change);
        }

        // Listeners de resize/scroll
        this.eventHandlers.resize = () => {
            const newRect = this.overlay.updateClipPath(this.element);
            this.tooltip.updatePosition(newRect, this.options.tooltipPosition);
        };

        window.addEventListener('resize', this.eventHandlers.resize);
        window.addEventListener('scroll', this.eventHandlers.resize, true);
    }

    close(stopTour = false) {
        // ⚠️ Respetar waitForAction y allowInteraction
        if (!this.shouldClose && (this.options.waitForAction || this.options.allowInteraction)) {
            return;
        }

        // Limpiar timers
        if (this.autoAdvanceTimer) {
            clearTimeout(this.autoAdvanceTimer);
            this.autoAdvanceTimer = null;
        }

        if (this.clickTimeout) {
            clearTimeout(this.clickTimeout);
            this.clickTimeout = null;
        }

        this.stopTourOnClose = stopTour;

        // Limpiar event listeners
        Object.entries(this.eventHandlers).forEach(([event, handler]) => {
            if (event === 'resize') {
                window.removeEventListener('resize', handler);
                window.removeEventListener('scroll', handler, true);
            } else {
                this.element?.removeEventListener(event, handler, { capture: true });
            }
        });

        this.overlay.hide();
        this.tooltip?.remove();

        if (this.element) {
            this.element.classList.remove('guideActive');
            this.element.style.pointerEvents = '';
            this.element.style.opacity = '';
            this.element.style.cursor = '';
        }

        // ✅ Resetear estado de clic
        this.hasBeenClicked = false;

        setTimeout(() => {
            if (this.options.onClose) {
                this.options.onClose(this.stopTourOnClose);
            }
        }, 300);
    }
}

/**
 * Clase principal para gestionar un tour completo
 */
class GuideTour {
    constructor(steps, options = {}) {
        this.steps = steps;
        this.options = {
            onComplete: options.onComplete || null,
            onStop: options.onStop || null,
            autoNext: options.autoNext || false,
            autoNextDelay: options.autoNextDelay || 3000
        };

        this.currentStepIndex = 0;
        this.currentGuide = null;
        this.isPaused = false;
        this.isStopped = false;
        this.globalDblClickHandler = null;
    }

    async start() {
        // ✅ PREVENCIÓN GLOBAL DE DOBLE CLIC
        this.globalDblClickHandler = (e) => {
            if (!this.isStopped) {
                e.preventDefault();
                e.stopPropagation();
                e.stopImmediatePropagation();
               
            }
        };

        document.addEventListener('dblclick', this.globalDblClickHandler, {
            capture: true,
            passive: false
        });

        await this.showNextStep();
    }

    async showNextStep() {
        if (this.isStopped) return;

        if (this.currentStepIndex >= this.steps.length) {
            if (this.options.onComplete) {
                this.options.onComplete();
            }
            return;
        }

        const stepConfig = this.steps[this.currentStepIndex];

        let selector = stepConfig.selector;
        if (stepConfig.waitForElement) {
            try {
                selector = await ElementManager.waitForElement(
                    stepConfig.selector,
                    stepConfig.waitTimeout || 10000
                );
            } catch (error) {
                console.warn(`❌ Error en paso ${this.currentStepIndex + 1}:`, error);
                this.currentStepIndex++;
                this.showNextStep();
                return;
            }
        }

        const stepInfo = `<div style="font-size: 12px; color: #666; margin-top: 8px;">Paso ${this.currentStepIndex + 1} de ${this.steps.length}</div>`;

        let continueButton = '';
        const needsContinueButton = stepConfig.waitForAction ||
            (stepConfig.allowInteraction && !stepConfig.autoSelectValue) ||
            stepConfig.validationType;

        if (needsContinueButton) {
            continueButton = `<button class="guide-continue-btn" style="
                margin-top: 10px;
                padding: 8px 16px;
                background: #0d6efd;
                color: white;
                border: none;
                border-radius: 4px;
                cursor: pointer;
                font-size: 14px;
                width: 100%;
                transition: background 0.2s;
            ">Continuar ➜</button>`;
            this.isPaused = true;
        }

        const tooltipText = (stepConfig.options?.tooltipText || '') + stepInfo + continueButton;

        const stepOptions = {
            ...stepConfig.options,
            tooltipText,
            waitForAction: stepConfig.waitForAction,
            allowInteraction: stepConfig.allowInteraction,
            autoSelectValue: stepConfig.autoSelectValue,
            allowScroll: stepConfig.allowScroll,
            scrollSelector: stepConfig.scrollSelector,
            autoAdvanceDelay: stepConfig.autoAdvanceDelay,
            onClose: (shouldStop) => {
                if (shouldStop) {
                    this.stop();
                    return;
                }

                if (stepConfig.options?.onClose) {
                    stepConfig.options.onClose();
                }

                if (!this.isPaused) {
                    this.currentStepIndex++;
                    this.showNextStep();
                }
            }
        };

        this.currentGuide = new GuideStep(selector, stepOptions);
        this.currentGuide.activate();

        if (needsContinueButton) {
            this.setupContinueButton();
        } else if (this.options.autoNext) {
            setTimeout(() => {
                if (this.currentGuide && !this.isPaused) {
                    this.currentGuide.shouldClose = true;
                    this.currentGuide.close();
                }
            }, this.options.autoNextDelay);
        }
    }

    setupContinueButton() {
        setTimeout(() => {
            const continueBtn = document.querySelector('.guide-continue-btn');

            if (continueBtn) {
                // ✅ Prevenir doble clic en el botón "Continuar"
                let isProcessing = false;

                continueBtn.addEventListener('click', (e) => {
                    e.stopPropagation();

                    // Bloquear múltiples clics en el botón
                    if (isProcessing) {
            
                        return;
                    }

                    isProcessing = true;
                    continueBtn.style.opacity = '0.6';
                    continueBtn.style.cursor = 'wait';

                    const stepConfig = this.steps[this.currentStepIndex];
                    let validationPassed = true;

                    // ✅ VALIDACIÓN INDIVIDUAL
                    if (stepConfig.validationType === 'individual') {
                        if (!this.currentGuide.element) {
                            console.warn('❌ Elemento no encontrado para validación');
                            alert('Error: Elemento no encontrado');
                            isProcessing = false;
                            continueBtn.style.opacity = '1';
                            continueBtn.style.cursor = 'pointer';
                            return;
                        }

                        const pattern = typeof stepConfig.validation.regex === 'string'
                            ? new RegExp(stepConfig.validation.regex)
                            : stepConfig.validation.regex;

                        const value = this.currentGuide.element.value || this.currentGuide.element.textContent;

                        if (!pattern.test(value.trim())) {
                            const errorMsg = stepConfig.validation.errorMessage || 'Por favor, ingrese el valor correcto';
                            alert(errorMsg);
                            validationPassed = false;
                        }
                    }

                    // ✅ VALIDACIÓN MÚLTIPLE
                    if (stepConfig.validationType === 'multiple') {
                        const result = ValidationManager.validateMultipleElements(
                            stepConfig.validation.selector,
                            stepConfig.validation.regex
                        );

                        if (!result.isValid) {
                            console.warn('❌ Validación múltiple fallida:', result.errors);

                            const errorMsg = stepConfig.validation.errorMessage ||
                                `Validación fallida:\n${result.errors.join('\n')}\n\nTotal de elementos: ${result.totalElements}`;

                            alert(errorMsg);
                            validationPassed = false;
                        }
                    }

                    // ✅ Si la validación pasa, continuar
                    if (validationPassed) {
                        if (this.currentGuide && this.currentGuide.overlay) {
                            this.currentGuide.overlay.disableScroll();
                        }

                        this.isPaused = false;
                        if (this.currentGuide) {
                            this.currentGuide.shouldClose = true;
                            this.currentGuide.close();
                        }
                    } else {
                        // Restaurar botón si la validación falla
                        isProcessing = false;
                        continueBtn.style.opacity = '1';
                        continueBtn.style.cursor = 'pointer';
                    }
                });

                continueBtn.addEventListener('mouseenter', () => {
                    if (!isProcessing) {
                        continueBtn.style.background = '#0b5ed7';
                    }
                });
                continueBtn.addEventListener('mouseleave', () => {
                    if (!isProcessing) {
                        continueBtn.style.background = '#0d6efd';
                    }
                });
            }
        }, 100);
    }

    skip() {
        this.stop();
        this.currentStepIndex = this.steps.length;
    }

    stop() {
        this.isStopped = true;

        // ✅ REMOVER PREVENCIÓN GLOBAL
        if (this.globalDblClickHandler) {
            document.removeEventListener('dblclick', this.globalDblClickHandler, { capture: true });
            this.globalDblClickHandler = null;
        }

        if (this.currentGuide) {
            this.currentGuide.shouldClose = true;
            this.currentGuide.close();
        }
        this.isPaused = false;
        if (this.options.onStop) {
            this.options.onStop();
        }
    }

    next() {
        if (this.currentGuide) {
            this.isPaused = false;
            this.currentGuide.shouldClose = true;
            this.currentGuide.close();
        }
    }

    pause() {
        this.isPaused = true;
    }

    resume() {
        this.isPaused = false;
        this.currentStepIndex++;
        this.showNextStep();
    }

    getCurrentStep() {
        return this.currentStepIndex;
    }
}



// ============================================
// INICIALIZACIÓN Y CONFIGURACIÓN
// ============================================

document.addEventListener("DOMContentLoaded", () => {
    const btnGuideClose = document.getElementById("btnGuideClose");
    const btnGuideOpen = document.getElementById("btnGuideOpen");
    const btnEndTour = document.getElementById('btnGuideEnd');

    if (btnGuideClose) {
        btnGuideClose.addEventListener("click", () => {
            sessionStorage.clear();
            sessionStorage.setItem('isAuth', true);
            sessionStorage.setItem("guidedTest", 'close');
            window.location.reload();
        });
    }

    if (btnGuideOpen) {
        btnGuideOpen.addEventListener("click", () => {
            sessionStorage.clear();
            sessionStorage.setItem('isAuth', true);
            sessionStorage.setItem("guidedTest", 'open');
            sessionStorage.setItem("workTableContainer", 0);
            window.location.reload();
        });
    }

    if (btnEndTour) {
        btnEndTour.addEventListener('click', () => {
            sessionStorage.clear();
            sessionStorage.setItem('isAuth', true);
            window.location.reload();
        });
    }

    if (sessionStorage.getItem('guidedTest') === 'close') {
        CloseGuideTour();
    } else if (sessionStorage.getItem('guidedTest') === 'open') {
        OpenGuideTour();
    }
});

// ============================================
// TOUR DE CIERRE DE ORDEN
// ============================================

function CloseGuideTour() {
    const steps = [
        // SECCIÓN 1: NAVEGACIÓN INICIAL
        {
            selector: "#btnActivities",
            options: {
                tooltipText: '👆 Haz clic en el botón <strong>Activities</strong> para comenzar',
                tooltipPosition: 'left'
            }
        },
        {
            selector: "#btnStandAlone",
            options: {
                tooltipText: '📋 Selecciona <strong>Data Collection Standalone</strong> para acceder al módulo',
                tooltipPosition: 'left'
            }
        },

        // SECCIÓN 2: LIMPIEZA DE LÍNEA
        {
            selector: "#btnLineRetrieve",
            options: {
                tooltipText: '🔄 Pulsa <strong>Retrieve</strong> para cargar los datos de limpieza de línea',
                tooltipPosition: 'bottom'
            }
        },
        {
            selector: "#btnCollectLine",
            options: {
                tooltipText: '📝 Pulsa <strong>Collect</strong> para abrir el formulario de limpieza',
                tooltipPosition: 'bottom'
            }
        },
        {
            selector: "#selectLineQuestion",
            allowInteraction: true,
            autoSelectValue: 'true',
            options: {
                tooltipText: '✅ Selecciona <strong>"Sí"</strong> en el desplegable<br><small>El tour continuará automáticamente</small>',
                tooltipPosition: 'bottom'
            }
        },
        {
            selector: "#btnLineSave",
            options: {
                tooltipText: '💾 Pulsa <strong>Save</strong> para guardar la limpieza de línea',
                tooltipPosition: 'bottom'
            }
        },
{
    selector: "#saveLineSms",
        autoAdvanceDelay: 2000,
            options: {
        tooltipText: '✅ Este mensaje confirma que la limpieza de línea se guardó correctamente<br><br>⏱️ Avanzará automáticamente en 2 segundos</small>',
            tooltipPosition: 'bottom'
    }
},
        {
            selector: ".btn_close_lineClearance_bottom",
            options: {
                tooltipText: '❌ Cierra el formulario de limpieza de línea',
                tooltipPosition: 'bottom'
            }
        },

        // SECCIÓN 3: DIVISIÓN DE ORDEN (SPLIT)
        {
            selector: ".OrderRow",
            options: {
                tooltipText: '📦 Haz clic en la <strong>orden de trabajo</strong> para seleccionarla',
                tooltipPosition: 'bottom',
                elementPosition: 0
            }
        },
        {
            selector: "#btnSfc",
            options: {
                tooltipText: '🔧 <strong>División de Orden (Split)</strong><br>Aquí dividiremos la orden en porciones más pequeñas para realizar el scrap y ajustar el material físico con el sistema',
                tooltipPosition: 'left'
            }
        },
        {
            selector: "#splitValue",
            allowInteraction: true,
            waitForAction: true,
            validationType: 'individual',
            validation: {
                regex: /^\s*200\s*$/,
                errorMessage: 'Por favor, ingrese exactamente "200" como valor'
            },
            options: {
                tooltipText: `🔢 Ingrese <strong>200</strong> como cantidad a dividir<br>
                           <strong> Este valor es para efectos de practica.</strong><br><br>
                     <small>Pulse "Continuar" cuando haya ingresado el valor correcto</small>`,
                tooltipPosition: 'bottom'
            }
        },
        {
            selector: "#splitBtn",
            options: {
                tooltipText: '✂️ Pulse <strong>Dividir</strong> para dividir la orden en las porciones especificadas',
                tooltipPosition: 'bottom'
            }
        },
        {
            selector: ".swal2-confirm",
            waitForElement: true,
            waitTimeout: 60000,
            options: {
                tooltipText: '✔️ Confirme la operación para proceder con la división de la orden',
                tooltipPosition: 'bottom'
            }
        },
        {
            selector: "#btnCloseSplit",
            options: {
                tooltipText: '❌ Cierre la ventana de división',
                tooltipPosition: 'bottom'
            }
        },

        // SECCIÓN 4: REGISTRO DE NO CONFORMIDAD (SCRAP)
        {
            selector: ".OrderRow",
            options: {
                tooltipText: `
          
                ⚠️ <strong class="text-danger text-center">Verificación Crítica</strong><br>
✋ <strong>La orden NO debe estar seleccionada</strong> al momento de realizar el scrap.<br>
<strong>¿Por qué es importante?</strong><br>
Si la orden está seleccionada cuando ejecutas el scrap:<br>
- Se desechará <strong>TODO el material seleccionado</strong><br>
- Esta acción <strong>NO se puede revertir</strong><br>
- Podría resultar en una <strong class="text-danger">acción disciplinaria</strong>


`,
                tooltipPosition: 'bottom',
                elementPosition: 0
            }
        },
        {
            selector: ".OrderRow",
            options: {
                tooltipText: '🗑️ Seleccione la <strong>segunda orden</strong> (las unidades que vamos a desechar)',
                tooltipPosition: 'bottom',
                elementPosition: 1
            }
        },
        {
            selector: '#btn_logNC_dashboard',
            options: {
                tooltipText: '📊 <strong>Log NC</strong><br>Esta sección contiene todos los códigos de defecto relacionados con su producto',
                tooltipPosition: 'left'
            }
        },
        {
            selector: '.page-link#page_4',
            options: {
                tooltipText: '🔍 Navegue a la <strong>página 4</strong> para buscar el código de defecto específico',
                tooltipPosition: 'top'
            }
        },
        {
            selector: '#page_4 .btn_logNC',
            options: {
                tooltipText: '📝 <strong>Grabar Log NC</strong><br>Pulse este botón para abrir el panel de registro de no conformidad (scrap)',
                tooltipPosition: 'bottom',
                elementPosition: 1
            }
        },
        {
            selector: '#add_done',
            options: {
                tooltipText: '✅ Pulse <strong>Add Done</strong> para confirmar y realizar el scrap de las unidades',
                tooltipPosition: 'top'
            }
        },

        // SECCIÓN 5: CIERRE DE ORDEN (COMPLETE)
        {
            selector: ".OrderRow",
            options: {
                tooltipText: '🏁 <strong>Iniciar Cierre de Orden</strong><br>Seleccione la primera orden para iniciar el proceso de cierre',
                tooltipPosition: 'bottom',
                elementPosition: 0
            }
        },
        {
            selector: '#btn-completar',
            options: {
                tooltipText: '✔️ Pulse <strong>Complete</strong> para iniciar el proceso de cierre de orden',
                tooltipPosition: 'bottom'
            }
        },
        {
            selector: '.lc_questions',
            allowInteraction: true,
            waitForAction: true,
            allowScroll: true,
            scrollSelector: '.lc_questions',
            validationType: 'multiple',
            validation: {
                selector: '#completeLcQuestions select',
                regex: /^(true|si|yes)$/i,
                errorMessage: 'Por favor, responda todas las preguntas con "Sí" antes de continuar'
            },
            options: {
                tooltipText: '📋 <strong>Preguntas de Limpieza de Línea Final</strong><br>Responda todas las preguntas con "Sí" y pulse "Continuar" cuando termine.',
                tooltipPosition: 'bottom'
            }
        },
        {
            selector: "#btnCompleteSave",
            options: {
                tooltipText: '💾 Pulsa <strong>Save</strong> para guardar',
                tooltipPosition: 'bottom'
            }
        },
        {
            selector: '#completeParameters',
            allowInteraction: true,
            waitForAction: true,
            allowScroll: true,
            scrollSelector: '#completeParameters',
            validationType: 'multiple',
            validation: {
                selector: '#completeParameters input',
                regex: /^\d+$/,
                errorMessage: 'Por favor, ingrese valores numéricos en todos los parámetros antes de continuar'
            },
            options: {
                tooltipText: '📋 <strong>Parámetros Finales</strong><br>Ingrese los parámetros. Recuerda que tienes que revisarlos contra la MSS.',
                tooltipPosition: 'bottom'
            }
        },
        {
            selector: "#btnCompleteSave",
            options: {
                tooltipText: '💾 Pulsa <strong>Save</strong> para guardar',
                tooltipPosition: 'bottom'
            }
        },
        {
            selector: '.confirm_content',
            allowInteraction: true,
            waitForAction: true,
            validationType: 'multiple',
            validation: {
                selector: '.confirm_content input',
                regex: /^.+$/,
                errorMessage: 'Por favor, ingrese su código y contraseña'
            },
            options: {
                tooltipText: '📋 <strong>Autenticación final</strong><br>Debes ingresar tu usuario y contraseña para autenticarte y finalizar la orden',
                tooltipPosition: 'bottom'
            }
        },
        {
            selector: ".btn_login",
            options: {
                tooltipText: 'Pulsa Login para autenticarte',
                tooltipPosition: 'bottom'
            }
        },
        {
            selector: "confirmOrderRow",
            options: {
                tooltipText: 'Seleccione la orden',
                tooltipPosition: 'bottom'
            }
        },
        {
            selector: "btn_accept",
            options: {
                tooltipText: 'Pulse el botón de Accept para finalizar la orden',
                tooltipPosition: 'bottom'
            }
        }
    ];
 const tour = new GuideTour(steps, {
    onComplete: () => {
        // ✅ Solo mostrar alerta cuando realmente se complete TODO el tour
        if (tour.getCurrentStep() >= steps.length) {
            Swal.fire({
                icon: 'success',
                title: '¡Excelente!',
                text: '¡Felicidades, has completado un cierre de Orden!',
                confirmButtonText: 'Entendido'
            }).then(() => {
                sessionStorage.removeItem("guidedTest");
                window.location.reload()
            });
        }
    },
    onStop: () => {
        // Solo limpiar si realmente se detuvo el tour manualmente
        if (tour.isStopped) {
            sessionStorage.clear();
            sessionStorage.setItem('isAuth', true);
        }
    }
});

tour.start();
}

// ============================================
// TOUR DE APERTURA DE ORDEN
// ============================================

function OpenGuideTour() {
    const steps = [
        // SECCIÓN 1: INGRESO DE EQUIPOS (RESOURCE SETUP)
        {
            selector: "btnResourceSetUp",
            options: {
                tooltipText: '👆 Haz clic en el botón <strong>Resource Setup</strong> para ingresar equipos',
                tooltipPosition: 'top'
            }
        },
        {
            selector: "btnRetrieve",
            options: {
                tooltipText: '👆 Haz clic en el botón <strong>Retrieve</strong> para mostrar los equipos ingresados',
                tooltipPosition: 'bottom'
            }
        },
        {
            selector: "#statusSelect",
            allowInteraction: true,
            validationType: 'individual',
            validation: {
                regex: /^setUp$/,
                errorMessage: 'Por favor, seleccione SetUp'
            },
            options: {
                tooltipText: 'Verifica que el estado de la orden debe estar en <strong>SetUp</strong>',
                tooltipPosition: 'bottom'
            }
        },
        {
            selector: "btnInsertNew",
            options: {
                tooltipText: 'Pulsa <strong>INSERT NEW</strong> para ingresar una nueva fila',
                tooltipPosition: 'bottom'
            }
        },
        {
            selector: ".selectType",
            waitForElement: true,
            waitTimeout: 60000,
            allowInteraction: true,
            waitForAction: true,
            validationType: 'individual',
            validation: {
                regex: /^Shop Order$/,
                errorMessage: 'Por favor, seleccione "Shop Order" del desplegable'
            },
            options: {
                tooltipText: '📋 Selecciona <strong>Shop Order</strong> para ingresar la orden<br><small>Pulse "Continuar" después de seleccionar</small>',
                tooltipPosition: 'bottom'
            }
        },
        {
            selector: ".resourceValue",
            allowInteraction: true,
            waitForAction: true,
            validationType: 'individual',
            validation: {
                regex: /^\d+$/,
                errorMessage: 'Por favor, ingrese el número de la orden (solo números)'
            },
            options: {
                tooltipText: '🔢 Ingrese el <strong>número de la orden</strong><br><small>Solo valores numéricos</small>',
                tooltipPosition: 'bottom'
            }
        },
        {
            selector: "btnInsertNew",
            options: {
                tooltipText: 'Pulsa <strong>INSERT NEW</strong> nuevamente para agregar otra fila',
                tooltipPosition: 'bottom'
            }
        },
        {
            selector: ".selectType",
            waitForElement: true,
            waitTimeout: 60000,
            allowInteraction: true,
            waitForAction: true,
            validationType: 'individual',
            validation: {
                regex: /^Tool Number$/,
                errorMessage: 'Por favor, seleccione "Tool Number" del desplegable'
            },
            options: {
                tooltipText: '🔧 Selecciona <strong>Tool Number</strong> para ingresar el ID del equipo',
                tooltipPosition: 'bottom',
                elementPosition: 1
            }
        },
        {
            selector: ".resourceValue",
            allowInteraction: true,
            waitForAction: true,
            validationType: 'individual',
            validation: {
                regex: /^\d+$/,
                errorMessage: 'Por favor, ingrese el ID del equipo (solo números)'
            },
            options: {
                tooltipText: '🔢 Ingrese el <strong>ID del equipo/herramienta</strong><br><small>Solo valores numéricos</small>',
                tooltipPosition: 'bottom',
                elementPosition: 1
            }
        },
        {
            selector: "resourceContainer",
            waitForAction: true,
            options: {
                tooltipText: '📦 <strong>Sección de Equipos</strong><br>En esta sección debes ingresar <strong>todos los equipos relacionados</strong> a tu operación<br><small>Repite el proceso anterior para cada equipo adicional</small>',
                tooltipPosition: 'bottom'
            }
        },
        {
            selector: "btnResourceSave",
            options: {
                tooltipText: '💾 Pulsa <strong>Save</strong> para guardar todos los equipos ingresados',
                tooltipPosition: 'top'
            }
        },
        {
            selector: "record_change",
            autoAdvanceDelay: 2000,
            options: {
                tooltipText: '✅ <strong>Record Change</strong><br>Este mensaje indica que la información se guardó correctamente<br><small>En caso de error, repórtalo a tu líder<br>⏱️ Avanzará automáticamente en 2 segundos</small>',
                tooltipPosition: 'bottom'
            }
        },
        {
            selector: "#btn_close",
            options: {
                tooltipText: '❌ Pulse para cerrar la ventana de Resource Setup',
                tooltipPosition: 'bottom'
            }
        },

        // SECCIÓN 2: LIMPIEZA DE LÍNEA
        {
            selector: "#btnActivities",
            options: {
                tooltipText: '👆 Haz clic en el botón <strong>Activities</strong> para realizar limpieza de línea',
                tooltipPosition: 'left'
            }
        },
        {
            selector: "#btnStandAlone",
            options: {
                tooltipText: '📋 Selecciona <strong>Data Collection Standalone</strong> para acceder al módulo',
                tooltipPosition: 'left'
            }
        },
        {
            selector: "#btnLineRetrieve",
            options: {
                tooltipText: '🔄 Pulsa <strong>Retrieve</strong> para cargar los datos de limpieza de línea',
                tooltipPosition: 'bottom'
            }
        },
        {
            selector: "#btnCollectLine",
            options: {
                tooltipText: '📝 Pulsa <strong>Collect</strong> para abrir el formulario de limpieza',
                tooltipPosition: 'bottom'
            }
        },
        {
            selector: "#selectLineQuestion",
            allowInteraction: true,
            autoSelectValue: 'true',
            options: {
                tooltipText: '✅ Selecciona <strong>"Sí"</strong> en el desplegable<br><small>El tour continuará automáticamente</small>',
                tooltipPosition: 'bottom'
            }
        },
        {
            selector: "#btnLineSave",
            options: {
                tooltipText: '💾 Pulsa <strong>Save</strong> para guardar la limpieza de línea',
                tooltipPosition: 'bottom'
            }
        },
        {
            selector: "#saveLineSms",
            autoAdvanceDelay: 2000,
            options: {
                tooltipText: '✅ Este mensaje confirma que la limpieza de línea se guardó correctamente<br><br>⏱️ Avanzará automáticamente en 2 segundos</small>',
                tooltipPosition: 'bottom'
            }
        },
        {
            selector: ".btn_close_lineClearance_bottom",
            options: {
                tooltipText: '❌ Cierra el formulario de limpieza de línea',
                tooltipPosition: 'bottom'
            }
        },

        // SECCIÓN 3: BÚSQUEDA Y SELECCIÓN DE ORDEN
        {
            selector: "seach_order_input",
            options: {
                tooltipText: '🔍 Pulsa en la <strong>barra de búsqueda</strong> para buscar la orden',
                tooltipPosition: 'bottom',
                elementPosition: 0
            }
        },
        {
            selector: "order_number_search",
            options: {
                tooltipText: '🔎 <strong>Buscar Orden</strong><br>Debes buscar la orden que coincida con la orden que estamos abriendo<br>✔️ Pulsa la orden para continuar',
                tooltipPosition: 'bottom'
            }
        },
        {
            selector: "btnOk",
            options: {
                tooltipText: '✔️ Pulsa <strong>Ok</strong> para seleccionar la orden',
                tooltipPosition: 'bottom'
            }
        },

        // SECCIÓN 4: PROCESO DE START (APERTURA)
        {
            selector: "btnStartSection",
            options: {
                tooltipText: '🚀 Pulse <strong>Start</strong> para iniciar la apertura de la orden',
                tooltipPosition: 'bottom'
            }
        },
        {
            selector: '.first_section .form-section',
            allowInteraction: true,
            waitForAction: true,
            validationType: 'multiple',
            validation: {
                selector: '.first_section .form-section .form-control',
                regex: /^\d+$/,
                errorMessage: 'Debes ingresar el ID del molde (solo números)'
            },
            options: {
                tooltipText: '📋 <strong>Ingreso del ID del Molde (Inicio)</strong><br>Ingrese el <strong>ID numérico del molde</strong><br><small>Este dato debe coincidir con la MSS</small>',
                tooltipPosition: 'bottom'
            }
        },
        {
            selector: "saveStartBtn",
            options: {
                tooltipText: '💾 Pulse <strong>Save</strong> para guardar el ID del molde',
                tooltipPosition: 'top'
            }
        },
        {
            selector: '#modalStartSection .lc_questions',
            allowInteraction: true,
            waitForAction: true,
            allowScroll: true,
            scrollSelector: '#modalStartSection .lc_questions',
            validationType: 'multiple',
            validation: {
                selector: '#modalStartSection .lc_questions select',
                regex: /^(true|si|yes)$/i,
                errorMessage: 'Por favor, responda todas las preguntas con "Sí" antes de continuar'
            },
            options: {
                tooltipText: '📋 <strong>Preguntas de Limpieza de Línea Moldeo</strong><br>Responda <strong>todas las preguntas con "Sí"</strong> y pulse "Continuar" cuando termine',
                tooltipPosition: 'bottom'
            }
        },
        {
            selector: "saveStartBtn",
            options: {
                tooltipText: '💾 Pulse <strong>Save</strong> para guardar las respuestas',
                tooltipPosition: 'top'
            }
        },
        {
            selector: '#start-parameters',
            allowInteraction: true,
            waitForAction: true,
            allowScroll: true,
            scrollSelector: '#start-parameters',
            validationType: 'multiple',
            validation: {
                selector: '#start-parameters input',
                regex: /^\d+$/,
                errorMessage: 'Por favor, ingrese valores numéricos en todos los parámetros antes de continuar'
            },
            options: {
                tooltipText: '📊 <strong>Parámetros de Inicio MSS</strong><br>Ingrese <strong>todos los parámetros numéricos</strong><br><small>⚠️ Recuerda que tienes que revisarlos contra la MSS</small>',
                tooltipPosition: 'bottom'
            }
        },
        {
            selector: "saveStartBtn",
            options: {
                tooltipText: '💾 Pulse <strong>Save</strong> para guardar los parámetros',
                tooltipPosition: 'top'
            }
        },
        {
            selector: "#moldStartQuestion .form-section .form-select",
            allowInteraction: true,
            autoSelectValue: 'si',
            options: {
                tooltipText: '✅ <strong>Verificación Molde/MSS Inicio</strong><br>Debes verificar que el <strong>ID del molde coincida con la MSS</strong><br><small>Esta es una validación crítica de calidad</small>',
                tooltipPosition: 'bottom'
            }
        },
        {
            selector: "saveStartBtn",
            options: {
                tooltipText: '💾 Pulse <strong>Save</strong> para confirmar la verificación',
                tooltipPosition: 'top'
            }
        },

        // SECCIÓN 5: DATA COLLECTION - EXPIRACIÓN DE TINTA
        {
            selector: ".OrderRow",
            options: {
                tooltipText: '📦 Selecciona la orden de trabajo',
                tooltipPosition: 'top'
            }
        },
        {
            selector: "data_collection_btn",
            options: {
                tooltipText: '📊 Pulsa <strong>DC Collection</strong> para abrir la lista de colecciones de datos',
                tooltipPosition: 'top'
            }
        },
        {
            selector: "#dc_collection_list tbody tr:nth-child(4) td:nth-child(4) .btn-register",
            options: {
                tooltipText: '📝 <strong>Colección: Fecha Expiración Tinta</strong><br>Pulsa <strong>Recopilar</strong> para registrar la fecha de expiración del kerosene/tinta',
                tooltipPosition: 'top'
            }
        },
        {
            selector: 'exp_tinta',
            allowInteraction: true,
            waitForAction: true,
            validationType: 'multiple',
            validation: {
                selector: '#exp_tinta .form-control',
                regex: /^(0[1-9]|1[0-2])\/(0[1-9]|[12][0-9]|3[01])\/\d{2}$/,
                errorMessage: 'El formato de la fecha debe ser MM/DD/AA (ejemplo: 05/15/25)'
            },
            options: {
                tooltipText: '📅 <strong>Ingreso de Fecha de Expiración</strong><br>Debe ingresar la fecha de expiración de la tinta/kerosene<br><small>⚠️ Formato obligatorio: <strong>MM/DD/AA</strong><br>Ejemplo: 05/15/25 para 15 de mayo de 2025</small>',
                tooltipPosition: 'bottom'
            }
        },
        {
            selector: "saveBtn",
            options: {
                tooltipText: '💾 Pulse <strong>Save</strong> para guardar la fecha de expiración',
                tooltipPosition: 'top'
            }
        },
        {
            selector: "close_data_collection",
            options: {
                tooltipText: '❌ Pulse para cerrar la ventana de Colección de Datos',
                tooltipPosition: 'bottom'
            }
        }
    ];

    const tour = new GuideTour(steps, {
        onComplete: () => {
            // ✅ Solo mostrar alerta cuando realmente se complete TODO el tour
            if (tour.getCurrentStep() >= steps.length) {
                Swal.fire({
                    icon: 'success',
                    title: '¡Excelente!',
                    text: '¡Felicidades, has completado una apertura de Orden!',
                    confirmButtonText: 'Entendido'
                }).then(() => {
                    sessionStorage.removeItem("guidedTest");
                    window.location.reload()
                });
            }
        },
        onStop: () => {
            // Solo limpiar si realmente se detuvo el tour manualmente
            if (tour.isStopped) {
                sessionStorage.clear();
                sessionStorage.setItem('isAuth', true);
            }
        }
    });

    tour.start();
}
// ============================================
// GUÍA DE CONFIGURACIÓN DE STEPS
// ============================================

/*
CONFIGURACIÓN COMPLETA DE UN STEP:

{
    // ============================================
    // SELECTOR DEL ELEMENTO
    // ============================================
    selector: "#miElemento",              // ID, clase, o cualquier selector CSS válido
    
    // ============================================
    // ESPERA DE ELEMENTOS
    // ============================================
    waitForElement: false,                // true: espera a que el elemento aparezca en el DOM
    waitTimeout: 10000,                   // Milisegundos a esperar (solo con waitForElement: true)
    
    // ============================================
    // INTERACCIÓN DEL USUARIO
    // ============================================
    allowInteraction: false,              // true: permite que el usuario interactúe con el elemento
    waitForAction: false,                 // true: muestra botón "Continuar" y pausa el tour
    autoSelectValue: null,                // 'valor': avanza automáticamente cuando se selecciona este valor en un <select>
    
    // ============================================
    // SCROLL
    // ============================================
    allowScroll: false,                   // true: permite hacer scroll en el elemento
    scrollSelector: null,                 // Selector del contenedor con scroll (si es diferente al elemento principal)
    
    // ============================================
    // AUTO-AVANCE CON TEMPORIZADOR
    // ============================================
    autoAdvanceDelay: null,               // Milisegundos para avanzar automáticamente (ejemplo: 3000 para 3 segundos)
                                          // Útil para mensajes de confirmación o notificaciones
    
    // ============================================
    // VALIDACIÓN INDIVIDUAL
    // ============================================
    validationType: 'individual',         // Tipo de validación para UN elemento
    validation: {
        regex: /^\d+$/,                   // Expresión regular para validar
        errorMessage: "Mensaje de error"  // Mensaje que se muestra si falla la validación
    },
    
    // ============================================
    // VALIDACIÓN MÚLTIPLE
    // ============================================
    validationType: 'multiple',           // Tipo de validación para MÚLTIPLES elementos
    validation: {
        selector: ".mis-campos",          // Selector que obtiene TODOS los elementos a validar
        regex: /^\d+$/,                   // Expresión regular que todos deben cumplir
        errorMessage: "Mensaje de error"  // Mensaje si alguno falla la validación
    },
    
    // ============================================
    // OPCIONES DEL TOOLTIP
    // ============================================
    options: {
        tooltipText: "Tu mensaje aquí",  // Texto a mostrar (acepta HTML)
        tooltipPosition: "top",           // Posición: "top" | "bottom" | "left" | "right"
        elementPosition: null,            // Índice si hay múltiples elementos: 0, 1, 2, etc.
        onClose: () => {                  // Función que se ejecuta al cerrar este paso
            // Tu código personalizado aquí
            console.log('Paso cerrado');
        }
    }
}

// ============================================
// EJEMPLOS PRÁCTICOS
// ============================================

// Ejemplo 1: Paso simple con clic
{
    selector: "#btnGuardar",
    options: {
        tooltipText: 'Haz clic en Guardar',
        tooltipPosition: 'bottom'
    }
}

// Ejemplo 2: Input con validación
{
    selector: "#inputCantidad",
    allowInteraction: true,
    waitForAction: true,
    validationType: 'individual',
    validation: {
        regex: /^\d{1,3}$/,
        errorMessage: 'Ingrese un número entre 1 y 999'
    },
    options: {
        tooltipText: 'Ingrese la cantidad deseada',
        tooltipPosition: 'bottom'
    }
}

// Ejemplo 3: Sección con múltiples campos y scroll
{
    selector: '.formulario-completo',
    allowInteraction: true,
    waitForAction: true,
    allowScroll: true,
    scrollSelector: '.formulario-completo',
    validationType: 'multiple',
    validation: {
        selector: '.formulario-completo input',
        regex: /^.+$/,
        errorMessage: 'Complete todos los campos'
    },
    options: {
        tooltipText: 'Complete todos los campos del formulario',
        tooltipPosition: 'top'
    }
}

// Ejemplo 4: Mensaje de confirmación con auto-avance
{
    selector: "#mensajeExito",
    autoAdvanceDelay: 3000,
    options: {
        tooltipText: '✅ Guardado exitoso<br><small>Avanzará en 3 segundos</small>',
        tooltipPosition: 'bottom'
    }
}

// Ejemplo 5: Select con valor específico
{
    selector: "#selectPais",
    allowInteraction: true,
    autoSelectValue: 'CR',
    options: {
        tooltipText: 'Seleccione "Costa Rica" del desplegable',
        tooltipPosition: 'bottom'
    }
}

// Ejemplo 6: Esperar elemento dinámico
{
    selector: ".modal-dinamico",
    waitForElement: true,
    waitTimeout: 5000,
    options: {
        tooltipText: 'Interactúa con este modal',
        tooltipPosition: 'top'
    }
}
*/