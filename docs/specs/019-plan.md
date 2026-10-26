# Plan del brief 019 — UI/UX del dashboard y la administración (Fase 11)

**Status:** **aprobado por el usuario (2026-10-26)** · brief
`019-ui-ux-dashboard.md`, aprobado el 2026-10-26 (D-153 a D-160), con su
[mockup v1](https://claude.ai/artifact/Uxqr9utFzd8w6uBgp3EYYd) aprobado como base.

Cada entrega se cierra como dice `docs/process.md` §2:
1. verificar;
2. `/code-review` calibrado;
3. checklist en el navegador;
4. "funciona" del usuario;
5. commit con `docs/status.md`.

Se trabaja en una rama y un worktree propios, `fase-11` (§7).

## Reglas para todas las entregas

- **Solo presentación** (brief → Out):
  - no cambian los cálculos, las reglas, los datos ni los SPs;
  - no cambian los filtros ni sus parámetros en la URL;
  - no cambian las columnas de los datos, ni el comportamiento de Usuarios, el login o el
    cambio de contraseña.
  - Si algo pide cambiar comportamiento, Desarrollo avisa antes (process §1, "escalar").
- **El mockup manda** (D-159): colores, tipografía, componentes y el Resumen. Las pantallas sin
  mockup siguen "Pantallas sin mockup" del brief (D-160).
- **Skill `frontend-design`** en cada entrega de pantallas, dentro del brief: si propone algo
  fuera del brief (otra librería, fuentes, animaciones), se pregunta antes.
- **Sin CDN:** Bootstrap 5.3, Bootstrap Icons y Chart.js, locales y con versión fija.
- **Tokens:** todo color, tipografía, espacio, radio y sombra sale de los tokens de
  `UI/master/admin.css`. Ninguna página tiene colores sueltos.
- **Modo oscuro** (D-157):
  - `data-bs-theme` en `<html>`, puesto por un script corto en el `<head>` de `Admin.Master`
    antes de pintar, para que no parpadee;
  - elige la preferencia guardada en `localStorage` o, si no hay, `prefers-color-scheme`;
  - el interruptor va en la barra superior. Es una preferencia de presentación: no va a
    `Session` ni al servidor;
  - la ficha impresa sale siempre en claro (`@media print`).
- **Accesibilidad:** contraste AA en los dos modos; estados con texto o ícono además del color;
  foco visible; botones y enlaces reales; `aria-label` en los botones que solo tienen ícono.
- **Verificación de cada entrega:**
  - rebuild y `aspnet_compiler` (se toca markup);
  - tests si se toca la BLL o `XlsxStreamWriter`;
  - capturas de antes y después de cada pantalla tocada, en claro y en oscuro, anotadas en el
    plan (AC-1);
  - revisión en 768 px (DevTools) y con zoom del 150 % (proyector).

## E1 · Bootstrap 5.3, primero verificado (D-155)

- [x] Actualizar el paquete NuGet `bootstrap` de 5.2.3 a la última 5.3.x (CSS y
  `bootstrap.bundle`). Confirmar la versión al momento de actualizar. **5.3.8** (última en
  NuGet el 2026-10-26; hash del `.nupkg` verificado contra el catálogo).
- [x] Revisar lo que cambia de 5.2 a 5.3 contra los estilos propios (`pod.css`, `presap.css`,
  `pod-guide.css`, `admin.css`, `Site.css`) y los componentes que usa la app (modales,
  `form-control`, `btn`, tablas). Sin `data-bs-theme` todo queda en claro, como hoy.
- [x] **Regresión completa** (usuario, 2026-10-26: "funciona"):
  - el login, el cambio de contraseña y Usuarios;
  - una sesión completa de **Guided, Práctica libre y Assessment**, entre las tres cubriendo
    Moldeo (NonSerialized), LoFlo (Serialized) y Redefinity: el Ingreso a MES, el POD con sus
    modales, Log NC, el Split, Logout con la limpieza y el resumen;
  - la guía de Guided con sus anclas;
  - las cinco vistas del dashboard y sus descargas;
  - SweetAlert2.
- [x] Capturas del POD de antes y después: se tienen que ver iguales (comparadas por el
  usuario en su navegador).
- [x] **Si algo se rompe y no se arregla sin cambiar comportamiento: se para y decide el
  usuario.** No se rompió nada.
- [x] AUD-29: el inventario suma Bootstrap 5.3.x.
- [x] `/code-review low` sobre los CSS tocados: no aplica, no se tocó ningún CSS propio (solo
  los archivos de Bootstrap); la revisión es la comparación de reglas de abajo.
- **Checklist del usuario:** la regresión de arriba en su navegador.

> **Revisión de 5.2.3 → 5.3.8 (Desarrollo, 2026-10-26).** Reglas de Bootstrap de las 127
> clases que usa la app, comparadas entre las dos versiones. **Nada que rompa el layout:**
> - **Tablas:** en 5.3 `--bs-table-bg` es `var(--bs-body-bg)` (blanco) y no transparente. Las
>   del POD están sobre blanco; `.pod-work-list` ya lo fijaba y la selección por
>   `--bs-table-accent-bg` en el `tr` sigue funcionando (sin `table-hover` ni `table-striped`
>   en el POD). Las del dashboard llevan `bg-white` o van en tarjetas.
> - **`.modal-header`** pierde `justify-content: space-between`; los botones del MES
>   (`.mes-titlebar-actions`) ya usan `margin-left: auto`.
> - **Matices de color** (solo se notan de cerca): bordes de `form-control`, `form-select` e
>   `input-group-text` de `#ced4da` a `#dee2e6`; radios y checks de `rgba(0,0,0,.25)` a
>   `#dee2e6`; `.text-muted` y placeholders de `#6c757d` a `rgba(33,37,41,.75)`; texto de
>   `.alert-danger` de `#842029` a `#58151c`; texto de tablas de `#212529` a `#000`; fondo de
>   `input-group-text` de `#e9ecef` a `#f8f9fa`.
> - Capturas de antes (5.2.3) del Ingreso a MES y del POD en Práctica libre y Assessment, del
>   login y del dashboard: en el job de Desarrollo. La regresión y la comparación las hace el
>   usuario en su navegador (usuario, 2026-10-26).

## E2 · Sistema de diseño, barra, login, cambio de contraseña, Usuarios e inicio del operador

> **Enmiendas durante la E2 (2026-10-26, usuario, por medio de Arquitectura y Planeación):**
> - **D-162 · Borde de los campos:** `--border` del mockup no llega a 3:1 en los controles
>   (WCAG 1.4.11; 1.38:1 sobre la superficie). Token aparte, `--mt-field-border`: `#808B9B` en
>   claro y `#627083` en oscuro, solo en `form-control`, `form-select` e `input-group`.
>   `--border` sigue igual en tablas, paneles y divisores.
> - **D-163 · Inicio del operador:** la E2 suma `Site.Master` y `StartSession` (abajo).
> - **Ninguna superficie con color fijo:** las utilidades `bg-white`, `bg-light`, `text-dark`,
>   `text-muted`… de las vistas pasan por los tokens en `admin.css` hasta que cada vista se
>   rehaga en su entrega. El tema va solo en `Admin.Master` y `Site.Master`; `Training.Master`
>   (el POD) no lo recibe.

- [x] **`UI/master/admin.css` como sistema de diseño:**
  - los tokens del mockup, en claro y en oscuro (`[data-bs-theme=dark]`), conectados a las
    variables de Bootstrap 5.3 (`--bs-body-bg`, `--bs-body-color`, `--bs-border-color`,
    `--bs-primary`…), para que los componentes de Bootstrap los tomen;
  - la fuente del sistema y la escala del mockup;
  - los componentes del brief: tarjeta de indicador, panel, tabla, insignias de estado, chips
    de reglas, barra de filtros, botones, paginación, estado vacío y barra de %.
- [x] **Bootstrap Icons local**, con versión fija, en `Content/` o `fonts/` (como Chart.js en
  `Scripts/`). Suma al inventario de AUD-29. **1.13.2** (paquete npm oficial; en NuGet no hay
  paquete), en `Content/bootstrap-icons-1.13.2/`, hash verificado; `.woff` y `.woff2` en
  `Web.config` (`remove` + `mimeMap`).
- [x] **`Admin.Master`:**
  - la barra superior del mockup: nombre de la app, menú (Resumen, Operadores, Pasos,
    Seguimiento y Usuarios, solo con `CanViewReports` como hoy), interruptor de tema y el
    usuario;
  - el script del tema en el `<head>`.
- [x] **Login y cambio de contraseña:** una tarjeta centrada con el sistema de diseño y el
  interruptor de tema. Mismo flujo y mismos mensajes (spec 013, D-62).
- [x] **Usuarios** (D-160): una tabla con el rol y el estado de la cuenta como insignias
  (Activa, Bloqueada, Debe cambiar la contraseña), el último acceso y las acciones de hoy como
  botones de la fila. El aviso al borrar (D-142) se conserva.
- [x] Capturas de antes y después (claro y oscuro): las revisó el usuario en su navegador
  (2026-10-26, "funciona").
- [x] `/code-review low`: el script del tema (que no rompa sin `localStorage`), las anclas y los
  postbacks de Usuarios.
- [x] **Inicio del operador** (D-163):
  - `Site.Master` con el sistema de diseño: la barra con el nombre de la app, el interruptor de
    tema, el usuario y Cerrar sesión (el mismo `SessionHelper.Logout()` de antes, D-13);
  - `StartSession`: el título y el último acceso; Producto, Área y Guía como listas, con la
    misma cascada y los mismos postbacks; los 3 modos como tarjetas grandes con ícono y una
    línea (los mismos `RadioButton`, mismos IDs y `GroupName`), accesibles con el teclado y con
    el foco visible; el botón Iniciar.
  - Un solo script del tema para los dos masters (`UI/master/theme.js`).
- **Checklist del usuario:**
  - el tema sigue al sistema, el interruptor lo cambia y se recuerda al recargar;
  - login, primer acceso, bloqueo y cambio de contraseña, en claro y en oscuro;
  - en Usuarios: reset, rol y borrar, con su aviso;
  - en el inicio del operador: elegir producto, área, guía y cada modo, e iniciar; el POD
    arranca en claro, con el aspecto de siempre, aunque el tema esté en oscuro;
  - 768 px.

## E3 · Resumen

- [x] `Summary.aspx` como el mockup:
  - **"¿Quién está listo?"** arriba, con la barra apilada por guía y las tres cifras (aprobados,
    al primer intento y puntaje promedio);
  - el acceso a Seguimiento;
  - después, Actividad y "Dónde se falla más".
- [x] **Gráficas con los tokens:** un JS común (por ejemplo `UI/master/admin-charts.js`) lee
  las variables CSS para colores, fuente y grilla, y redibuja al cambiar de tema. Los modos usan
  los tres azules y los estados sus colores.
- [x] Las mismas cifras que hoy, del mismo `DashboardService` (sin cambios en la BLL). Sin
  "N sin iniciar" ni número en Seguimiento (D-164). `Site.css`, sin uso, se borró.
- [ ] Rendimiento: el Resumen sigue en menos de 3 s con la BD de volumen (AC-7). *No se midió
  en la E3:* la BLL y las consultas son las mismas; se mide con `DashboardPerformanceTests`
  antes de cerrar la fase.
- **Checklist del usuario** ✅ (2026-10-26, probado por el usuario):
  - claro y oscuro contra el mockup;
  - proyector (zoom del 150 %) y 768 px;
  - "Sin datos" con la BD vacía;
  - los enlaces a Pasos, Operadores y Seguimiento.

## E4 · Operadores y Operador

- [x] **Operadores** (D-160): la barra de filtros, una columna por guía con su insignia, el mejor
  puntaje, la última actividad y la paginación. El mejor (y el último) puntaje va por guía dentro
  de su celda, como antes: una cifra única entre guías sería un cálculo nuevo. "Sin iniciar" es
  su insignia gris, no "—".
- [x] **Operador:**
  - arriba, el estado por guía como insignias grandes y los pasos a reforzar;
  - la curva con los tokens;
  - las sesiones en tabla;
  - **la ficha se imprime en claro**, sin barra, menú, botones ni curva (D-150, D-157).
- **Checklist del usuario** ✅ (2026-10-26, probado por el usuario):
  - los filtros y la paginación de siempre (en la URL, F5);
  - el detalle en claro y en oscuro;
  - la vista previa de impresión en claro con el tema oscuro activo.

## E5 · Pasos y Seguimiento

- [ ] **Pasos** (D-160):
  - la tabla con la barra de %, los conteos, la mediana y los saltos en Práctica libre;
  - los 2 motivos más frecuentes, y "Ver todos" que despliega el resto en el cliente.
- [ ] **Seguimiento** (D-160): una fila por operador y guía, con las reglas como chips con texto,
  la última sesión y el enlace al detalle.
  - **Orden:** primero quien cumple más reglas. Es un orden de presentación: si hoy lo decide
    `DashboardService`, se ajusta ahí sin cambiar qué reglas aplican.
- **Checklist del usuario:**
  - Pasos con "Todas" (agrupado por tipo de paso, D-147) y con una guía;
  - Práctica libre con saltos;
  - Seguimiento con un operador que cumple 2 reglas.

## E6 · Excel (D-158)

- [ ] **`ExcelReports.cs` (ClosedXML):** Operadores, Pasos y Seguimiento con:
  - la fila de título (vista, filtros y fecha de descarga);
  - los encabezados con color, los filtros de Excel y la primera fila fija;
  - anchos y formatos (%, fechas en la hora de la planta, duraciones);
  - los estados con color.
- [ ] **`XlsxStreamWriter.cs`** (descarga de sesiones, D-149): los mismos estilos escritos en
  `styles.xml`, la fila de título, `sheetViews` con la primera fila fija y `autoFilter`.
  - Sigue en streaming.
  - El test de esquema (`OpenXmlValidator`) sigue en 0 errores, y los tests que lo leen con
    ClosedXML suman los estilos.
- [ ] Mismas columnas y datos. Medir la descarga de sesiones con 30 días y con "Todo": no puede
  empeorar más de lo razonable (D-148).
- **Checklist del usuario:** abrir cada archivo en Excel:
  - sin el aviso de "reparar";
  - con título, colores, filtros, la primera fila fija y los formatos.

## No incluye

- El POD, el área del operador (`Training.Master`, `Site.Master`) y su diseño (solo la
  regresión de E1).
- Cambios en cálculos, reglas, datos, SPs, filtros o columnas.
- Vistas, descargas o gráficas nuevas.
- Colores de marca, fuentes web y animaciones de entrada.

## Docs al cerrar

- [ ] **Rendimiento (AC-7), al cerrar la fase:** Resumen, Operadores y Pasos con la BD de volumen
  (`DashboardPerformanceTests`), juntos (Planeación, 2026-10-26).
- [ ] *Si sobra tiempo:* renombrar `BLL.Reports.GuideSummary` (por ejemplo `GuideSummaryRow`) y
  quitar el alias de `Summary.aspx.cs` (choca con `Domain.GuideSummary` de la spec 020).
- **Desarrollo:**
  - `docs/architecture.md`: el sistema de diseño (`admin.css` y sus tokens), el tema (script,
    `localStorage`, impresión) y `admin-charts.js`;
  - `CLAUDE.md` → "Sesión y seguridad": que `Admin.Master` usa el sistema de diseño;
  - la auditoría `2026-10-01`: Bootstrap 5.3.x y Bootstrap Icons en el inventario de AUD-29;
  - `status.md` y `history.md`.
- **Arquitectura y Planeación:** la Fase 11 ✅ en el roadmap y D-153 a D-160.
