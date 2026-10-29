// Tema claro u oscuro del sistema de diseño (Fase 11, D-157). Lo cargan Admin.Master y
// Site.Master en el <head>, sin async ni defer: el tema se pone antes de pintar y no parpadea.
// Training.Master (el POD) no lo carga nunca.
//
// Elige la preferencia guardada en este navegador o, si no hay, la del sistema. Es
// presentación: no va a Session ni al servidor. Sin localStorage (bloqueado o modo privado)
// sigue al sistema. El botón #mtThemeToggle de la barra lo cambia; su texto dice a qué modo
// cambia. Avisa con el evento "mt-theme" (las gráficas se redibujan con los colores del tema).
// También abre y cierra el menú plegado de la barra superior (wireMenu).
var mtTheme = (function () {
    var KEY = 'mt-theme';
    var root = document.documentElement;
    var media = window.matchMedia ? window.matchMedia('(prefers-color-scheme: dark)') : null;

    function saved() {
        var value = null;
        try { value = window.localStorage.getItem(KEY); } catch (e) { }
        return value === 'light' || value === 'dark' ? value : null;
    }

    function current() { return root.getAttribute('data-bs-theme') === 'dark' ? 'dark' : 'light'; }

    function paint() {
        var button = document.getElementById('mtThemeToggle');
        if (!button) return;
        var dark = current() === 'dark';
        button.setAttribute('aria-label', dark ? 'Cambiar a modo claro' : 'Cambiar a modo oscuro');
        button.querySelector('.bi').className = dark ? 'bi bi-sun' : 'bi bi-moon';
        button.querySelector('span').textContent = dark ? 'Claro' : 'Oscuro';
    }

    function apply(theme) {
        root.setAttribute('data-bs-theme', theme);
        paint();
        document.dispatchEvent(new CustomEvent('mt-theme', { detail: theme }));
    }

    function toggle() {
        var next = current() === 'dark' ? 'light' : 'dark';
        try { window.localStorage.setItem(KEY, next); } catch (e) { }
        apply(next);
    }

    // Antes de pintar.
    root.setAttribute('data-bs-theme', saved() || (media && media.matches ? 'dark' : 'light'));

    // Sin preferencia guardada, sigue los cambios del sistema.
    if (media) {
        var follow = function (e) { if (!saved()) apply(e.matches ? 'dark' : 'light'); };
        if (media.addEventListener) media.addEventListener('change', follow);
        else if (media.addListener) media.addListener(follow);
    }

    // Barra superior plegada (spec 022, brief 0; Admin.Master): el botón Menú abre y cierra
    // los dos grupos en el cliente, sin postback. Escape la cierra y devuelve el foco al botón.
    function wireMenu() {
        var menu = document.getElementById('mtTopbarMenu');
        var bar = menu && menu.closest('.mt-topbar');
        if (!bar) return;
        function set(open) {
            bar.classList.toggle('is-open', open);
            menu.setAttribute('aria-expanded', open ? 'true' : 'false');
        }
        menu.addEventListener('click', function () { set(!bar.classList.contains('is-open')); });
        bar.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && bar.classList.contains('is-open')) { set(false); menu.focus(); }
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        var button = document.getElementById('mtThemeToggle');
        if (button) button.addEventListener('click', toggle);
        paint();
        wireMenu();
    });

    return { toggle: toggle, current: current };
})();
