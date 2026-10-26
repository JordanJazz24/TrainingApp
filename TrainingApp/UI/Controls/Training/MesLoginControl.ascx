<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="MesLoginControl.ascx.cs"
    Inherits="TrainingApp.UI.Controls.Training.MesLoginControl" %>

<%-- Ingreso a MES (spec 016, P-2): escritorio con el script y el Repositorio (D-69), consola
     del script, Edge genérico (D-71) con el Repositorio y Recursos MES, y el
     login de MES con el diseño de SAP NetWeaver (D-63). Capturas en
     docs/ui-reference/Screens SAP MES REAL/Pre-SAPMES/. Los campos visibles no tienen name: no
     viajan; el script copia lo que se envía a los campos ocultos (la contraseña, solo si
     tiene algo, D-73). Las anclas data-guide son para la guía de Guided (E4). --%>
<div class="mes-desktop" id="mesDesktop" data-guide="mes-desktop">

    <div class="mes-desktop-icons">
        <button type="button" class="mes-desktop-icon" id="mesIconScript" data-guide="mes-icon-script"
            ondblclick="mesLogin.openScript()">
            <img src="<%=ResolveUrl("~/UI/master/imgs/presap/icon-script.png")%>" alt="" />
            MES Limpiar historial y cache
        </button>
        <button type="button" class="mes-desktop-icon" id="mesIconRepositorio" data-guide="mes-icon-repositorio"
            ondblclick="mesLogin.openRepository()">
            <img src="<%=ResolveUrl("~/UI/master/imgs/presap/icon-repositorio.png")%>" alt="" />
            Repositorio
        </button>
    </div>

    <%-- ══ CONSOLA (cmd.exe) ══════════════════════════════════════════════ --%>
    <div class="mes-console" id="mesConsole" style="display:none" data-guide="mes-console">
        <div class="mes-console-title">
            <span>C:\Windows\system32\cmd.exe</span>
            <button type="button" tabindex="-1">&#8212;</button>
            <button type="button" tabindex="-1">&#9633;</button>
            <button type="button" class="mes-win-close" onclick="mesLogin.closeConsole()"
                data-guide="mes-console-close">&#10005;</button>
        </div>
        <div class="mes-console-body" id="mesConsoleBody" onclick="mesLogin.focusConsole()">
            <span>Por favor ingrese su code1 (exactamente 9 caracteres numericos):</span><input type="text"
                id="mesConsoleInput" class="mes-console-input" maxlength="20" autocomplete="off"
                data-guide="mes-console-input" /><span id="mesConsoleOutput"></span>
        </div>
    </div>

    <%-- ══ EDGE ═══════════════════════════════════════════════════════════ --%>
    <div class="mes-edge" id="mesEdge" style="display:none" data-guide="mes-edge">
        <div class="mes-edge-tabs">
            <button type="button" class="mes-edge-tab" id="mesTabRepo" data-tab="repo"
                onclick="mesLogin.activateTab('repo')" data-guide="mes-tab-repositorio">Repositorio de Aplicaciones</button>
            <button type="button" class="mes-edge-tab" id="mesTabRec" data-tab="rec"
                onclick="mesLogin.activateTab('rec')" data-guide="mes-tab-recursos">Recursos MES</button>
            <%-- Limpieza al salir (P-6): la pestaña de MES después del Logout. --%>
            <button type="button" class="mes-edge-tab" id="mesTabOff" data-tab="off"
                data-guide="mes-tab-loggedoff">SAP NetWeaver</button>
            <div class="mes-edge-winbtns">
                <button type="button" tabindex="-1">&#8212;</button>
                <button type="button" tabindex="-1">&#9633;</button>
                <button type="button" class="mes-win-close" onclick="mesLogin.closeEdge()"
                    title="Cerrar" data-guide="mes-edge-close">&#10005;</button>
            </div>
        </div>
        <div class="mes-edge-address">
            <span class="mes-edge-nav">&#8592;&#8594;&#8635;</span>
            <span class="mes-edge-url" id="mesEdgeUrl"></span>
        </div>

        <%-- Repositorio de Aplicaciones: las tarjetas Hora-Hora (1.0) y (3.0) se ven y no hacen
             nada (P-2, D-67, D-151). --%>
        <div class="mes-edge-page" id="mesPageRepo" style="display:none">
            <div class="mes-repo">
                <h1>Repositorio de Aplicaciones</h1>
                <div class="mes-repo-cards">
                    <button type="button" class="mes-repo-card" data-guide="mes-card-hh1">
                        <img src="<%=ResolveUrl("~/UI/master/imgs/presap/card-hh1.jpg")%>" alt="Hora-Hora (1.0)" />
                    </button>
                    <button type="button" class="mes-repo-card">
                        <img src="<%=ResolveUrl("~/UI/master/imgs/presap/card-hh3.jpg")%>" alt="Hora-Hora (3.0)" />
                    </button>
                    <button type="button" class="mes-repo-card" onclick="mesLogin.openTab('rec')" data-guide="mes-card-recursos">
                        <img src="<%=ResolveUrl("~/UI/master/imgs/presap/card-recursos.jpg")%>" alt="Recursos MES" />
                    </button>
                </div>
                <img class="mes-repo-logo" src="<%=ResolveUrl("~/UI/master/imgs/presap/philips-shield.png")%>" alt="Philips" />
            </div>
        </div>

        <%-- Recursos MES: "Inicio" e "Ingresar" son decorativos; Buscar envía (P-2). --%>
        <div class="mes-edge-page" id="mesPageRec" style="display:none">
            <div class="mes-rec">
                <div class="mes-rec-header">
                    <img src="<%=ResolveUrl("~/UI/master/imgs/presap/philips-wordmark.png")%>" alt="Philips" />
                    <nav><span>Inicio</span><span>Ingresar</span></nav>
                </div>
                <div class="mes-rec-band">
                    <div class="mes-rec-fields">
                        <div class="mes-rec-field">
                            <label for="mesRecResource">Ingresar recurso</label>
                            <input type="text" id="mesRecResource" maxlength="40" autocomplete="off"
                                data-guide="mes-rec-resource" />
                        </div>
                        <div class="mes-rec-field">
                            <label for="mesRecCode1">Ingresar code1</label>
                            <input type="text" id="mesRecCode1" maxlength="20" autocomplete="off"
                                data-guide="mes-rec-code1" />
                        </div>
                    </div>
                    <button type="button" class="mes-rec-search" onclick="mesLogin.submitRecursos()"
                        data-guide="mes-rec-search">Buscar</button>
                </div>
                <img class="mes-rec-logo" src="<%=ResolveUrl("~/UI/master/imgs/presap/sap-mes.png")%>" alt="SAP MES" />
            </div>
        </div>

        <%-- Login de MES: Buscar en Recursos MES pasa la pestaña a este login (D-63). --%>
        <div class="mes-edge-page" id="mesPageLogin" style="display:none">
            <div class="mes-nw" data-guide="mes-login">
                <div class="nw-frame">
                    <div class="nw-panel">
                        <div class="nw-image">
                            <img src="<%=ResolveUrl("~/UI/master/imgs/login.jpg")%>" alt="" />
                        </div>
                        <div class="nw-main">
                            <h1 class="nw-title">SAP NetWeaver</h1>
                            <div class="nw-content">
                                <div class="nw-form">
                                    <label class="nw-label" for="mesLoginUser">User <span class="nw-star">*</span></label>
                                    <input type="text" id="mesLoginUser" class="nw-input" maxlength="50"
                                        autocomplete="off" data-guide="mes-login-user" />
                                    <label class="nw-label" for="mesLoginPassword">Password <span class="nw-star">*</span></label>
                                    <input type="password" id="mesLoginPassword" class="nw-input"
                                        autocomplete="off" data-guide="mes-login-password" />
                                    <div class="nw-actions">
                                        <button type="button" class="nw-button" onclick="mesLogin.submitLogOn()"
                                            data-guide="mes-login-logon">Log On</button>
                                    </div>
                                </div>
                            </div>
                            <div class="nw-footer">
                                <span class="nw-copyright">Copyright &copy; SAP AG. All Rights Reserved.</span>
                                <img class="nw-logo" src="<%=ResolveUrl("~/UI/master/imgs/sap-logo.png")%>" alt="SAP" />
                            </div>
                        </div>
                    </div>
                    <div class="nw-tab"></div>
                </div>
            </div>
        </div>

        <%-- "You have logged off" (P-6, captura logout sap mes.png): Log On Again no hace nada. --%>
        <div class="mes-edge-page" id="mesPageLoggedOff" style="display:none">
            <div class="mes-nw" data-guide="mes-loggedoff">
                <div class="nw-frame">
                    <div class="nw-panel">
                        <div class="nw-image">
                            <img src="<%=ResolveUrl("~/UI/master/imgs/login.jpg")%>" alt="" />
                        </div>
                        <div class="nw-main">
                            <h1 class="nw-title">SAP NetWeaver</h1>
                            <div class="nw-content mes-nw-loggedoff">
                                <p>You have logged off</p>
                                <div class="nw-actions">
                                    <button type="button" class="mes-nw-again" data-guide="mes-logon-again">Log On Again</button>
                                </div>
                            </div>
                            <div class="nw-footer">
                                <span class="nw-copyright">Copyright &copy; SAP AG. All Rights Reserved.</span>
                                <img class="nw-logo" src="<%=ResolveUrl("~/UI/master/imgs/sap-logo.png")%>" alt="SAP" />
                            </div>
                        </div>
                    </div>
                    <div class="nw-tab"></div>
                </div>
            </div>
        </div>
    </div>

    <%-- Barra de tareas decorativa (P-2). --%>
    <div class="mes-taskbar">
        <span class="mes-taskbar-start">&#8862;</span>
        <span class="mes-taskbar-search">Type here to search</span>
        <span class="mes-taskbar-clock" id="mesClock"></span>
    </div>
</div>

<%-- Etiqueta de la estación (D-70): no es parte del MES; como la de la orden (spec 011).
     Tocar las barras equivale a escanear: escribe el recurso en Ingresar recurso. --%>
<div class="pod-order-label mes-station-label" id="mesStationLabel" style="display:none" data-guide="mes-station-label">
    <div class="pod-order-label-text">
        <span class="pod-order-label-caption">Estación</span>
        <strong><%: ResourceName %></strong>
    </div>
    <svg class="pod-order-label-barcode" id="mesStationBarcode" data-guide="mes-station-barcode"
        data-value="<%: ResourceName %>" onclick="mesLogin.scanStation()"
        role="img" aria-label="Estación <%: ResourceName %>"></svg>
</div>

<asp:HiddenField ID="HfMesAction" runat="server" />
<asp:HiddenField ID="HfMesCode1" runat="server" />
<asp:HiddenField ID="HfMesResource" runat="server" />
<asp:HiddenField ID="HfMesPassword" runat="server" />
<%-- Pestañas de Edge abiertas en el cliente (abrir una tarjeta no viaja, D-77): vuelven con el
     postback para que la respuesta las conserve. F5 las pierde (D-78). --%>
<asp:HiddenField ID="HfMesTabs" runat="server" />
<asp:Button ID="BtnMesSubmit" runat="server" style="display:none" tabindex="-1"
    UseSubmitBehavior="false" OnClick="BtnMesSubmit_Click" />

<script>
    // Ingreso a MES (spec 016): pantallas y pestañas en el cliente; lo que se califica o
    // cambia el avance va al servidor (D-77) con un postback por acción (D-68).
    var mesLogin = (function () {
        var state = <%= StateJson %>;
        var REPO_URL = 'http://crapps.philips.com/Repositorio/index.aspx';
        // Recursos MES: solo el host, el mismo del Repositorio (spec 016,
        // confirmado por el usuario); sus rutas no se conocen.
        var HOST = 'crapps.philips.com';
        var tabs = { repo: true, rec: false };
        var active = 'repo';
        var consoleDone = false;
        var sending = false;

        function el(id) { return document.getElementById(id); }
        function show(id, visible) { var e = el(id); if (e) e.style.display = visible ? '' : 'none'; }

        function warn(title, text) {
            Swal.fire({ icon: 'warning', title: title, text: text });
        }

        function submit(action, fields) {
            if (sending) return;
            sending = true;
            fields = fields || {};
            el('<%= HfMesAction.ClientID %>').value   = action;
            el('<%= HfMesCode1.ClientID %>').value    = fields.code1 || '';
            el('<%= HfMesResource.ClientID %>').value = fields.resource || '';
            el('<%= HfMesPassword.ClientID %>').value = fields.password ? '1' : '';
            // Cerrar Edge cierra sus pestañas; Repositorio vuelve a abrir solo la suya.
            el('<%= HfMesTabs.ClientID %>').value = action === 'CloseEdge' || !tabs.rec ? '' : 'rec';
            __doPostBack('<%= BtnMesSubmit.UniqueID %>', '');
        }

        function value(id) { return (el(id).value || '').trim(); }

        // Formato, sin intento: ASP.NET rechaza un postback con marcado (< > o &#).
        function hasMarkup() {
            for (var i = 0; i < arguments.length; i++)
                if (/[<>]|&#/.test(arguments[i])) {
                    warn('Formato', 'El texto no puede contener <, > ni &#.');
                    return true;
                }
            return false;
        }

        // ── Escritorio y consola ─────────────────────────────────────────
        // El script ya ejecutado no hace nada hasta la limpieza final (P-14).
        function openScript() {
            if (state.ScriptDone || el('mesConsole').style.display !== 'none') return;
            consoleDone = false;
            el('mesConsoleInput').value = '';
            el('mesConsoleInput').style.display = '';
            el('mesConsoleOutput').textContent = '';
            show('mesConsole', true);
            focusConsole();
        }

        function focusConsole() {
            var input = el('mesConsoleInput');
            if (!consoleDone && input) input.focus();
        }

        // Cerrar la consola sin responder no cuenta; con la salida del script, la cierra. Al
        // terminar la limpieza la sesión ya se cerró: se ve el POD con el resumen (P-12).
        function closeConsole() {
            var wasDone = consoleDone;
            consoleDone = false;
            show('mesConsole', false);
            if (wasDone && state.CloseToSummary) {
                var screen = el('mesDesktop').closest('.mes-screen');
                if (screen) screen.style.display = 'none';
                var pod = document.querySelector('.pod-shell');
                if (pod) pod.classList.remove('d-none');
                var summary = document.querySelector('.pod-completion');
                if (summary) summary.classList.remove('d-none');
            }
        }

        function consoleOutput(code1) {
            var profile = 'C:\\Users\\operador\\AppData\\Local\\Microsoft\\Edge\\User Data\\Default';
            var browsers = ['iexplore.exe', 'chrome.exe', 'msedge.exe'];
            var kill = browsers.map(function (b) { return 'ERROR: The process "' + b + '" not found.'; });
            var lines = ['Script ejecutado por ' + code1, 'Iteration 1: Killing browser processes...']
                .concat(kill)
                .concat(['Clearing Microsoft Edge cache, cookies, and history...',
                         'Deleted file - ' + profile + '\\History'])
                .concat(['data_0', 'data_1', 'data_2', 'data_3', 'index'].map(function (f) {
                    return 'Deleted file - ' + profile + '\\Cache\\Cache_Data\\' + f;
                }))
                .concat(['Cache and history clearing completed.', 'Iteration 2: Killing browser processes...'])
                .concat(kill)
                .concat(['Clearing Microsoft Edge cache, cookies, and history...',
                         'Could Not Find ' + profile + '\\History',
                         'Cache and history clearing completed.',
                         'Press any key to continue . . .']);
            return lines.join('\n');
        }

        function showConsoleDone(code1) {
            consoleDone = true;
            el('mesConsoleInput').style.display = 'none';
            el('mesConsoleOutput').textContent = code1 + '\n' + consoleOutput(code1);
            show('mesConsole', true);
        }

        // Enter envía el Code1; formato del script real: exactamente 9 dígitos (P-3).
        function submitScript() {
            var code1 = value('mesConsoleInput');
            if (hasMarkup(code1)) return;
            if (!/^\d{9}$/.test(code1)) {
                warn('Formato', 'El Code1 tiene exactamente 9 dígitos numéricos.');
                return;
            }
            submit('RunScript', { code1: code1 });
        }

        // Doble clic en Repositorio: abre Edge. Antes del script lo califica el servidor.
        function openRepository() {
            if (state.EdgeOpen) { renderEdge(); return; }
            submit('OpenRepository');
        }

        // ── Edge ─────────────────────────────────────────────────────────
        // ✕ de Edge: vuelve al escritorio y conserva lo hecho (P-13).
        function closeEdge() { submit('CloseEdge'); }

        function openTab(tab) { tabs[tab] = true; activateTab(tab); }

        function activateTab(tab) {
            if (!tabs[tab]) return;
            active = tab;
            renderEdge();
        }

        function renderEdge() {
            show('mesEdge', true);
            // Limpieza al salir (P-6): una sola pestaña, la de MES con "You have logged off".
            var off = state.Mode === 'logout';
            show('mesTabOff', off);
            show('mesPageLoggedOff', off);
            show('mesTabRepo', !off);
            if (off) {
                ['mesTabRec', 'mesPageRepo', 'mesPageRec', 'mesPageLogin', 'mesStationLabel']
                    .forEach(function (id) { show(id, false); });
                el('mesTabOff').classList.add('active');
                // La URL del logout de MES no se conoce (sin captura de Edge).
                el('mesEdgeUrl').textContent = '';
                return;
            }
            show('mesTabRec', tabs.rec);
            el('mesTabRec').textContent = state.RecursosDone ? 'SAP NetWeaver' : 'Recursos MES';
            el('mesTabRepo').classList.toggle('active', active === 'repo');
            el('mesTabRec').classList.toggle('active', active === 'rec');
            show('mesPageRepo', active === 'repo');
            show('mesPageRec', active === 'rec' && !state.RecursosDone);
            show('mesPageLogin', active === 'rec' && state.RecursosDone);
            el('mesEdgeUrl').textContent = active === 'repo' ? REPO_URL : HOST;
            show('mesStationLabel', active === 'rec' && !state.RecursosDone);
        }

        // ── Pantallas que se envían ──────────────────────────────────────
        function submitRecursos() {
            var resource = value('mesRecResource'), code1 = value('mesRecCode1');
            if (!resource) { warn('Campo requerido', 'Ingrese el recurso.'); return; }
            if (!code1) { warn('Campo requerido', 'Ingrese su Code1.'); return; }
            if (hasMarkup(resource, code1)) return;
            submit('RecursosSearch', { resource: resource, code1: code1 });
        }

        // La contraseña no se compara con nada (D-73): solo viaja si tiene algo.
        function submitLogOn() {
            var code1 = value('mesLoginUser');
            if (!code1) { warn('Campo requerido', 'Ingrese su usuario.'); return; }
            if (hasMarkup(code1)) return;
            if (!el('mesLoginPassword').value) { warn('Campo requerido', 'Ingrese su contraseña.'); return; }
            submit('LogOn', { code1: code1, password: true });
        }

        // Tocar las barras de la etiqueta equivale a escanear la estación (D-70): escribe el
        // recurso y lo marca escaneado (la guía de Guided avanza con esa marca).
        function scanStation() {
            var input = el('mesRecResource');
            if (!input || state.RecursosDone) return;
            input.value = el('mesStationBarcode').getAttribute('data-value') || '';
            stationScanned();
        }

        // El lector termina con Enter; tocar las barras, igual. Con un recurso escrito, el foco
        // pasa a Ingresar code1.
        function stationScanned() {
            var input = el('mesRecResource');
            if (!value('mesRecResource')) { input.removeAttribute('data-scanned'); return; }
            input.setAttribute('data-scanned', '1');
            el('mesRecCode1').focus();
        }

        function renderStationLabel() {
            var barcode = el('mesStationBarcode');
            if (!barcode || typeof JsBarcode !== 'function') return;
            JsBarcode(barcode, barcode.getAttribute('data-value'), {
                format: 'CODE128', width: 2, height: 44, margin: 6,
                displayValue: false, background: '#ffffff', lineColor: '#000000'
            });
        }

        function tickClock() {
            var now = new Date();
            el('mesClock').innerHTML = now.toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' }) +
                '<br />' + now.toLocaleDateString();
        }

        // Pantalla de arranque: la que falló (P-4), la que sigue a lo aceptado o la del avance
        // guardado (F5, D-78).
        function start() {
            var screen = state.Screen || 'desktop';
            var posted = (el('<%= HfMesTabs.ClientID %>').value || '').split(',');
            tabs.rec = state.RecursosDone || screen === 'recursos' || screen === 'meslogin' || posted.indexOf('rec') >= 0;
            active = (screen === 'recursos' || screen === 'meslogin') ? 'rec' : 'repo';

            if (screen === 'loggedoff' ||
                (state.EdgeOpen && screen !== 'desktop' && screen !== 'console' && screen !== 'consoleDone'))
                renderEdge();
            if (screen === 'console') openScript();
            if (screen === 'consoleDone') showConsoleDone(state.ScriptCode1 || '');

            renderStationLabel();
            tickClock();
            setInterval(tickClock, 30000);

            el('mesConsoleInput').addEventListener('keydown', function (e) {
                if (e.key !== 'Enter') return;
                e.preventDefault();
                submitScript();
            });
            // "Press any key to continue": cualquier tecla cierra la consola terminada.
            document.addEventListener('keydown', function () {
                if (consoleDone) closeConsole();
            });
            // Enter en los formularios de Edge envía su pantalla, como en el navegador.
            [['mesRecResource', stationScanned],
             ['mesRecCode1', submitRecursos], ['mesLoginUser', submitLogOn],
             ['mesLoginPassword', submitLogOn]].forEach(function (pair) {
                el(pair[0]).addEventListener('keydown', function (e) {
                    if (e.key !== 'Enter') return;
                    e.preventDefault();
                    pair[1]();
                });
            });
        }

        document.addEventListener('DOMContentLoaded', start);

        return {
            openScript: openScript, focusConsole: focusConsole, closeConsole: closeConsole,
            openRepository: openRepository, closeEdge: closeEdge, openTab: openTab,
            activateTab: activateTab, submitRecursos: submitRecursos,
            submitLogOn: submitLogOn, scanStation: scanStation
        };
    })();
</script>
