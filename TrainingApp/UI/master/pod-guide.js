// Guía visual del modo Guided (spec 010). Motor genérico: no conoce textos ni respuestas.
// En cada respuesta el servidor manda los subpasos y la clave de entrada con
// PodGuide.start({ entry, substeps }). El motor solo escucha: nunca guarda estado ni envía
// nada al servidor. Solo se incluye en Guided; Assessment no recibe este archivo (AC-2).
(function () {
    'use strict';

    var POLL_MS          = 100;   // búsqueda del ancla y reubicación
    var REALIGN_MS       = 1500;  // entrada fuera del modal abierto: se busca dentro de él
    var FIND_TIMEOUT_MS  = 4000;  // sin ancla: falla segura (R5)
    var NO_POSTBACK_MS   = 1500;  // un clic con postback que no navegó (ni avisó): se vuelve a mostrar

    var FOCUSABLE = 'a[href],button:not([disabled]),input:not([disabled]):not([type="hidden"]),' +
                    'select:not([disabled]),textarea:not([disabled]),[tabindex]:not([tabindex="-1"])';

    var substeps = [];
    var idx = -1;
    // idle → searching → shown | failsafe; waiting = clic que envía la página; done = terminó.
    var state = 'idle';
    var target = null;
    var since = 0;
    var timer = null;
    var entered = false;       // ya se mostró el primer subpaso de esta respuesta
    var navigating = false;    // la página se está enviando (beforeunload)
    var paused = false;        // hay un SweetAlert abierto
    var pendingRetry = null;   // subpaso al que volver al cerrar un aviso de formato
    var frame = null, globe = null;
    var lifted = [];           // elementos libres del subpaso, subidos sobre el oscurecido

    function current() { return substeps[idx] || null; }

    // Un desplegable con respuesta esperada (decisión) también avanza con "Continuar", que se
    // habilita solo con el valor correcto; elegir no avanza solo (R11).
    function needsContinue(s) {
        return s.mode === 'capture' || s.mode === 'verify' || s.mode === 'info' || s.mode === 'final' ||
            (s.mode === 'change' && s.expect != null);
    }

    // Subpasos que solo se leen: lo resaltado no se toca (R11).
    function readOnly(s) {
        return s.mode === 'info' || s.mode === 'final';
    }

    // ── Elementos ────────────────────────────────────────────────────

    function isVisible(el) {
        if (!el || !el.isConnected || el.getClientRects().length === 0) return false;
        return window.getComputedStyle(el).visibility !== 'hidden';
    }

    function topModal() {
        var open = document.querySelectorAll('.modal.show');
        return open.length ? open[open.length - 1] : null;
    }

    // Elemento del subpaso en pantalla. Con un modal abierto solo cuenta lo que está dentro
    // del modal de más arriba: lo de atrás no se puede usar.
    function locate(s) {
        if (!s) return null;
        var modal = topModal();
        var all = Array.prototype.filter.call(document.querySelectorAll(s.anchor), function (el) {
            return isVisible(el) && (!modal || modal.contains(el));
        });
        if (all.length === 0) return null;
        return s.last ? all[all.length - 1] : all[0];
    }

    function findAnchor(s) {
        var el = locate(s);
        // Un botón deshabilitado todavía no se puede usar (ej. REMOVE ALL antes de Retrieve).
        if (el && (s.mode === 'click' || s.mode === 'change') && el.disabled) return null;
        return el;
    }

    function swalOpen() {
        return !!(window.Swal && window.Swal.isVisible());
    }

    function focusables(root) {
        if (!root) return [];
        var list = root.matches(FOCUSABLE) ? [root] : [];
        return list.concat(Array.prototype.slice.call(root.querySelectorAll(FOCUSABLE))).filter(isVisible);
    }

    function isTextField(el) {
        if (!el) return false;
        if (el.tagName === 'TEXTAREA') return true;
        return el.tagName === 'INPUT' && !/^(submit|button|reset|image|checkbox|radio|file)$/i.test(el.type);
    }

    // ── Formato de "Continuar" (el mismo que exige el control; nunca decisiones) ──

    function formatOk(format, value) {
        var v = (value || '').trim();
        if (format === 'digits') return /^\d+$/.test(v);
        if (format === 'number') return v !== '' && !isNaN(parseFloat(v)) && isFinite(v);
        if (format === 'required') return v !== '' && !/[<>]/.test(v);
        return true;
    }

    // Valores que la captura no acepta, como un formato (spec 015, D-25: los equipos
    // precargados de Resource Setup). Sin espacios, con el conjunto de PreloadedEquipmentRule.
    function rejected(s, value) {
        if (!s.reject) return false;
        var v = (value || '').replace(/[ \t\r\n]/g, '');
        return s.reject.some(function (r) { return (r || '').replace(/[ \t\r\n]/g, '') === v; });
    }

    // Regla de un campo de un bloque con decisión (Data Collection): las de StepValidator.
    function fieldOk(rule, el) {
        if (!el) return false;
        var v = (el.value || '').trim();
        if (rule.expect != null) return v === rule.expect;
        if (rule.rule === 'date') return v !== '';
        if (rule.rule === 'number') {
            if (!/^-?\d+(\.\d+)?$/.test(v)) return false;
            var n = parseFloat(v);
            return (rule.min == null || n >= rule.min) && (rule.max == null || n <= rule.max);
        }
        return v !== '' && !/[<>]/.test(v);
    }

    // ¿El subpaso ya está hecho? (filas ya seleccionadas, Add visible tras escanear). Cada
    // selector debe tener un elemento visible; con "!" adelante, ninguno (ej. "ninguna fila
    // seleccionada").
    function doneNow(s) {
        if (!s || !s.done) return false;
        return s.done.every(function (sel) {
            var negate = sel.charAt(0) === '!';
            var any = Array.prototype.some.call(document.querySelectorAll(negate ? sel.substring(1) : sel), isVisible);
            return negate ? !any : any;
        });
    }

    // Avanza cuando la pantalla queda como pide el subpaso, no con el clic: escaneo (Add
    // visible) y selección de filas (un clic puede deseleccionar).
    function waitsForDone(s) {
        return s.mode === 'scan' || (s.mode === 'click' && !s.postback && s.done != null);
    }

    // Formato y, en una decisión, la respuesta correcta (R10: sin aviso propio; "Continuar"
    // simplemente no se habilita).
    function answerOk(s, el) {
        if (s.fields) {
            return s.fields.every(function (rule) { return fieldOk(rule, el.querySelector(rule.sel)); });
        }
        var fields = el.matches('input,select,textarea')
            ? [el] : el.querySelectorAll('input,select,textarea');
        // Solo lo que el operador completa: sin campos ocultos ni botones.
        fields = Array.prototype.filter.call(fields, function (f) {
            return !(f.tagName === 'INPUT' && /^(hidden|button|submit|reset|image)$/i.test(f.type));
        });
        return fields.every(function (f) {
            return formatOk(s.format, f.value) && !rejected(s, f.value) &&
                (s.expect == null || f.value === s.expect);
        });
    }

    function continueEnabled() {
        var s = current();
        if (!s) return false;
        if (state === 'failsafe' || readOnly(s) || !target) return true;
        return answerOk(s, target);
    }

    // ── Dibujo ───────────────────────────────────────────────────────

    function ensureDom() {
        if (frame) return;
        frame = document.createElement('div');
        frame.className = 'pg-frame';
        // El globo queda fuera de los modales: dentro de uno, la sombra del marco lo taparía.
        // Con un modal abierto se usa con el mouse o con Enter (ver keydown).
        globe = document.createElement('div');
        globe.className = 'pg-globe';
        globe.innerHTML =
            '<div class="pg-head"><span class="pg-step"></span><span class="pg-count"></span></div>' +
            '<div class="pg-text"></div>' +
            '<div class="pg-note"></div>' +
            '<div class="pg-actions"><button type="button" class="pg-continue">Continuar</button></div>';
        document.body.appendChild(frame);
        document.body.appendChild(globe);
        globe.querySelector('.pg-continue').addEventListener('click', onContinue);
    }

    function render() {
        var s = current();
        globe.querySelector('.pg-step').textContent  = s.step;
        globe.querySelector('.pg-count').textContent = s.mode === 'final' ? '' : 'Subpaso ' + s.index + ' de ' + s.total;
        // HTML armado y codificado en el servidor (GuideScriptBuilder).
        globe.querySelector('.pg-text').innerHTML = s.html;
        // Nota de la trainer (T7), normal o alerta; oculta si el subpaso no la trae.
        var note = globe.querySelector('.pg-note');
        note.innerHTML = s.note || '';
        note.style.display = s.note ? '' : 'none';
        note.classList.toggle('pg-note-alert', s.alert === true);
        globe.querySelector('.pg-actions').style.display = needsContinue(s) ? '' : 'none';
        globe.querySelector('.pg-continue').textContent = s.mode === 'final' ? 'OK' : 'Continuar';
        updateContinue();
    }

    function updateContinue() {
        if (globe) globe.querySelector('.pg-continue').disabled = !continueEnabled();
    }

    function clamp(v, min, max) { return Math.max(min, Math.min(v, max)); }

    function place() {
        var r = target.getBoundingClientRect(), pad = 4, gap = 12, margin = 8;
        frame.style.left   = (r.left - pad) + 'px';
        frame.style.top    = (r.top - pad) + 'px';
        frame.style.width  = (r.width + pad * 2) + 'px';
        frame.style.height = (r.height + pad * 2) + 'px';

        var vw = window.innerWidth, vh = window.innerHeight;
        var gw = globe.offsetWidth, gh = globe.offsetHeight;
        var top, left = r.left;
        if (r.bottom + gap + gh <= vh - margin) {
            top = r.bottom + gap;
        } else if (r.top - gap - gh >= margin) {
            top = r.top - gap - gh;
        } else {
            top  = r.top;
            left = r.right + gap + gw <= vw - margin ? r.right + gap : r.left - gap - gw;
        }
        globe.style.left = clamp(left, margin, vw - gw - margin) + 'px';
        globe.style.top  = clamp(top, margin, vh - gh - margin) + 'px';
    }

    function blocks(st) { return st === 'shown' || st === 'searching' || st === 'waiting'; }

    // Único lugar que cambia el estado. Mientras la guía bloquea, los elementos libres (Abandon;
    // Logout solo con la sesión cerrada, spec 015) quedan por encima de todo, también de los
    // modales (R2).
    function setState(next) {
        state = next;
        var lift = blocks(next);
        document.querySelectorAll('[data-guide-free]').forEach(function (b) {
            b.classList.toggle('pg-lift', lift);
        });
        liftFree(next === 'shown' ? current() : null);
    }

    // Elementos libres del subpaso (spec 011): además del resaltado, se pueden usar mientras
    // el subpaso está en pantalla (ej. el campo SFC junto a la etiqueta que se escanea).
    function freeEls(s) {
        if (!s || !s.free) return [];
        var els = [];
        s.free.forEach(function (sel) {
            Array.prototype.forEach.call(document.querySelectorAll(sel), function (el) {
                if (isVisible(el)) els.push(el);
            });
        });
        return els;
    }

    function liftFree(s) {
        lifted.forEach(function (el) { el.classList.remove('pg-lift'); });
        lifted = freeEls(s);
        lifted.forEach(function (el) { el.classList.add('pg-lift'); });
    }

    function inLifted(node) {
        return lifted.some(function (el) { return el.contains(node); });
    }

    function hideVisuals() {
        if (!frame) return;
        frame.style.display = 'none';
        globe.style.display = 'none';
    }

    function show(el) {
        target  = el;
        entered = true;
        setState('shown');
        render();
        globe.classList.remove('pg-failsafe');
        frame.style.display = 'block';
        globe.style.display = 'block';
        place();

        var s = current();
        if (s.mode === 'capture' || s.mode === 'verify' || s.mode === 'scan' ||
            (s.mode === 'change' && s.expect != null)) {
            // Sin campo en lo resaltado (la etiqueta), el foco va al primer elemento libre: el
            // lector escribe donde está el foco.
            var field = focusables(el)[0] || freeFields()[0];
            if (field && document.activeElement !== field) field.focus();
        }
    }

    // Falla segura (R5): sin oscurecido ni bloqueo, la instrucción en un globo fijo.
    function failsafe() {
        target = null;
        setState('failsafe');
        render();
        frame.style.display = 'none';
        globe.classList.add('pg-failsafe');
        globe.style.left = '';
        globe.style.top  = '';
        globe.style.display = 'block';
    }

    // ── Avance ───────────────────────────────────────────────────────

    function indexOfKey(key) {
        for (var i = 0; i < substeps.length; i++)
            if (substeps[i].key === key) return i;
        return -1;
    }

    function search() {
        // El foco no se queda en el campo del subpaso anterior: lo que se escriba ahora no
        // debe cambiarlo.
        var active = document.activeElement;
        if (target && active && target.contains(active)) active.blur();
        target = null;
        since  = Date.now();
        setState('searching');
        hideVisuals();
    }

    function goTo(i) {
        if (i >= substeps.length) { finish(); return; }
        idx = i;
        search();
    }

    function advance() { goTo(idx + 1); }

    function finish() {
        target = null;
        setState('done');
        hideVisuals();
        if (timer) { clearInterval(timer); timer = null; }
    }

    function onContinue() {
        var s = current();
        if (!s || !needsContinue(s) || !continueEnabled()) return;
        // Mensaje del MES que esperaba a la guía (R9): se oculta al seguir.
        if (s.hide && target) target.style.display = 'none';
        advance();
    }

    // Aviso de formato del control sin subpaso propio: vuelve al primer subpaso de captura
    // (o verificación) del paso que está dentro del modal abierto.
    function formatFallbackKey() {
        var s = current(), modal = topModal();
        if (!s || !modal) return null;
        for (var i = 0; i < substeps.length; i++) {
            var c = substeps[i];
            if (c.step !== s.step || (c.mode !== 'capture' && c.mode !== 'verify')) continue;
            if (locate(c)) return c.key;
        }
        return null;
    }

    // Solo para la entrada: un camino del servidor que reabrió un modal sin anotar el momento
    // (ej. un aviso de formato del servidor) deja la entrada detrás del modal. Se entra en el
    // primer subpaso del mismo paso que está dentro del modal abierto, hacia adelante y
    // después hacia atrás.
    function realign() {
        var s = current();
        if (entered || !s || !topModal()) return false;
        var order = [];
        for (var i = idx + 1; i < substeps.length; i++) order.push(i);
        for (var j = 0; j < idx; j++) order.push(j);
        for (var k = 0; k < order.length; k++) {
            var c = substeps[order[k]];
            if (c.step !== s.step) continue;
            var el = findAnchor(c);
            if (el) { idx = order[k]; show(el); return true; }
        }
        return false;
    }

    function onTargetClick() {
        var s = current();
        if (s.mode !== 'click' || waitsForDone(s)) return;
        if (s.postback) {
            // La página se envía: no se muestra nada ni se acepta otro clic hasta la respuesta.
            since = Date.now();
            setState('waiting');
            hideVisuals();
            return;
        }
        // Después de los handlers del propio elemento (ej. Retrieve habilita la tabla).
        setTimeout(advance, 0);
    }

    function tick() {
        if (state === 'idle' || state === 'done') return;

        if (swalOpen()) {
            if (!paused) {
                paused = true;
                // El clic que iba a enviar la página terminó en un aviso de formato.
                if (state === 'waiting' && !navigating)
                    pendingRetry = (current() && current().retry) || formatFallbackKey() || '';
                hideVisuals();
            }
            return;
        }
        if (paused) {
            paused = false;
            if (pendingRetry !== null) {
                var i = indexOfKey(pendingRetry);
                pendingRetry = null;
                goTo(i >= 0 ? i : idx);
            } else if (!navigating) {
                search();
            }
            return;
        }

        if (state === 'waiting') {
            // Sin navegación ni aviso, el clic no envió la página: se vuelve a mostrar.
            if (!navigating && Date.now() - since > NO_POSTBACK_MS) search();
            return;
        }

        // Ya hecho al llegar (ej. las filas ya estaban seleccionadas): se salta.
        if (state === 'searching' && doneNow(current())) { advance(); return; }

        var el = findAnchor(current());
        if (state === 'searching') {
            var waited = Date.now() - since;
            if (el) show(el);
            else if (waited > REALIGN_MS && realign()) return;
            else if (waited > FIND_TIMEOUT_MS) failsafe();
            return;
        }
        if (state === 'failsafe') {
            if (el) { show(el); return; }
            // El operador ya hizo el subpaso sin la guía: retoma en el siguiente.
            var next = findAnchor(substeps[idx + 1]);
            if (next) { idx++; show(next); }
            return;
        }
        // shown
        // Escaneo (R6: el lector termina en Enter y el control muestra Add) y selección de
        // filas: se avanza cuando la pantalla queda como pide el subpaso.
        if (waitsForDone(current()) && doneNow(current())) { advance(); return; }
        if (!el) { search(); return; }
        target = el;
        place();
    }

    // ── Bloqueo de mouse y teclado ───────────────────────────────────

    function isFree(node) {
        return !!(node && node.closest &&
            (node.closest('[data-guide-free]') || node.closest('.swal2-container')));
    }

    // Lo resaltado en un subpaso informativo (o el globo final) es de solo lectura (R11).
    function targetEditable() {
        var s = current();
        return !!(target && s && !readOnly(s));
    }

    function allowed(node) {
        if (isFree(node)) return true;
        if (state !== 'shown' || !node || !node.closest) return false;
        return (targetEditable() && target.contains(node)) || inLifted(node) ||
            (globe && globe.contains(node));
    }

    function freeFields() {
        return lifted.reduce(function (list, el) { return list.concat(focusables(el)); }, []);
    }

    function blocking() {
        return blocks(state) && !swalOpen();
    }

    ['pointerdown', 'mousedown', 'mouseup', 'click', 'dblclick', 'contextmenu'].forEach(function (type) {
        document.addEventListener(type, function (e) {
            if (!blocking()) return;
            if (allowed(e.target)) {
                if (type === 'click' && state === 'shown' && target && target.contains(e.target))
                    onTargetClick();
                return;
            }
            e.preventDefault();
            e.stopPropagation();
        }, true);
    });

    document.addEventListener('change', function (e) {
        if (state !== 'shown' || !target || !target.contains(e.target)) return;
        var s = current();
        // Un desplegable sin respuesta esperada avanza con el cambio; una decisión espera a
        // "Continuar" (R11).
        if (s.mode === 'change' && s.expect == null) setTimeout(advance, 0);
        else updateContinue();
    }, true);

    document.addEventListener('input', function (e) {
        if (state === 'shown' && target && target.contains(e.target)) updateContinue();
    }, true);

    function cycleFocus(back) {
        // Con un modal abierto su trampa de foco no deja salir: solo el elemento resaltado.
        var list = targetEditable() ? focusables(target) : [];
        list = list.concat(freeFields());
        if (!topModal()) list = list.concat(focusables(globe));
        if (list.length === 0) return;
        var i = list.indexOf(document.activeElement);
        var next = back ? (i <= 0 ? list.length - 1 : i - 1) : (i + 1) % list.length;
        list[next].focus();
    }

    // Recargar (F5, Ctrl+R) y las teclas de función siguen funcionando.
    function isBrowserKey(e) {
        return /^F\d{1,2}$/.test(e.key) || ((e.ctrlKey || e.metaKey) && !e.altKey);
    }

    document.addEventListener('keydown', function (e) {
        if (!blocking() || isBrowserKey(e)) return;
        var key = e.key, active = document.activeElement;
        var stop = function () { e.preventDefault(); e.stopPropagation(); };

        // Esc cerraría el modal fuera del guion.
        if (key === 'Escape') { stop(); return; }
        if (isFree(active)) return;
        if (state !== 'shown') { stop(); return; }
        if (key === 'Tab') { stop(); cycleFocus(e.shiftKey); return; }

        var s = current();
        // En captura, verificación e informativo Enter equivale a "Continuar" (salvo sobre un
        // botón, que hace su propio clic). Nunca envía el formulario.
        if (key === 'Enter' && needsContinue(s) && !(active && active.matches('button, input[type="submit"], input[type="button"]'))) {
            stop();
            onContinue();
            return;
        }
        // Fuera del elemento resaltado (y del globo) no se escribe ni se activa nada.
        if (!allowed(active)) { stop(); return; }
        // Escaneo: el Enter del lector lo procesa el propio control (muestra Add).
        if (s.mode === 'scan') return;
        if (key === 'Enter' && isTextField(active)) stop();
    }, true);

    window.addEventListener('beforeunload', function () { navigating = true; });

    // ── Entrada ──────────────────────────────────────────────────────

    function init(config) {
        substeps = (config && config.substeps) || [];
        if (substeps.length === 0) return;
        ensureDom();
        idx = Math.max(0, indexOfKey(config.entry));
        search();
        timer = setInterval(tick, POLL_MS);
    }

    window.PodGuide = {
        // Se llama desde un script de arranque (al final del form): espera al DOM y a los
        // demás scripts de arranque (modales que se reabren, SweetAlerts).
        start: function (config) {
            var begin = function () { setTimeout(function () { init(config); }, 0); };
            if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', begin);
            else begin();
        },

        // ¿La guía va a mostrar este mensaje en un subpaso informativo que todavía no pasó?
        // podAutoHide (Training.Master) no lo oculta mientras tanto (R9); un mensaje que la
        // guía no usa se oculta como siempre.
        holds: function (el) {
            if (state === 'idle' || state === 'done' || !el) return false;
            for (var i = Math.max(idx, 0); i < substeps.length; i++) {
                var s = substeps[i];
                if (s.hide && el.matches(s.anchor)) return true;
            }
            return false;
        }
    };
})();
