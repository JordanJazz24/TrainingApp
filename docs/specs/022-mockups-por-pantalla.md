# Brief de diseño 022: un mockup por pantalla (candidato a Fase 12)

**Status:** **Approved 2026-10-27** (usuario) · plan `022-plan.md` · **Tipo:** brief de diseño
(`docs/process.md` → "1. Clasificar el pedido", fila 3), con métricas derivadas opcionales
(D-205).
**Date:** 2026-10-27 · dos rondas de preguntas con el usuario · **Sesión:** Arquitectura y
Planeación en la nube (`docs/process.md` → "Variante temporal: specs y planes en la nube"),
rama `cloud/022-mockups-por-pantalla`.
**Decisiones:** D-202 a D-206 del usuario; propuestas de Claude respondidas por el usuario el
2026-10-27 (P-1 a P-9, abajo: **D-207**). **Briefs por pantalla:** `022-briefs.md`. **Exportaciones aprobadas:**
`022-design/<pantalla>.html`.

## Problema

La Fase 11 (brief 019) hizo con mockup solo el Resumen. Las demás pantallas se armaron con los
componentes del mockup, sin diseño propio (D-160, y D-163 para el inicio del operador). Al
usarlas, al usuario no le convencen (2026-10-27):

- **siguen siendo tablas planas:** lo importante no salta a la vista;
- **no dicen qué hacer:** muestran datos, pero no guían la decisión (a quién llamar, qué
  reforzar, qué enseñar mejor);
- **el login y el cambio de contraseña se ven genéricos**, de plantilla.

El usuario pidió "enmendar la 019 y bloquear sus E4 y E5". Como la 019 ya está cerrada e
implementada, esto es un **brief nuevo**. En la 019 solo se agrega la nota "D-160 queda
reemplazada por la spec 022".

## Qué cambia respecto a la 019

- **Cada pantalla lleva su propio mockup**, hecho en **Claude Design** con el brief de
  `022-briefs.md`. Se piden **3 alternativas que difieran en la estructura**, no solo en el
  color. El usuario elige una (o pide mezclar), y su exportación se guarda en
  `022-design/<pantalla>.html`: es el **criterio de aceptación** de esa pantalla (D-202).
- Reemplaza **D-160** (pantallas sin mockup) y la parte "sin mockup" de **D-163** (el inicio
  del operador). Lo demás de la 019 sigue vigente: D-153 a D-159, D-161, D-162 y D-164.

## Pantallas (D-203)

| Pantalla | Archivo | Quién | Implementar (orden con la 021) |
|---|---|---|---|
| Barra superior (agregada 2026-10-29, D-238) | `UI/master/Admin.Master` y `Site.Master` | los dos roles | E1b, antes de Seguimiento |
| Login | `Pages/Auth/Login.aspx` | los dos roles | libre |
| Cambio de contraseña | `Pages/Auth/ChangePassword.aspx` | los dos roles | libre (la redirección final a "Elige tu área" la pone la E3 de la 021, no esta spec) |
| Operadores | `Pages/Admin/Operators.aspx` | admin (gerencia y trainers) | después de la E5 de la 021 |
| Operador (detalle y ficha) | `Pages/Admin/Operator.aspx` | admin | después de la E5 de la 021 |
| Pasos | `Pages/Admin/Steps.aspx` | admin | libre |
| Seguimiento | `Pages/Admin/FollowUp.aspx` | admin | libre |
| Guías | `Pages/Admin/Guides.aspx` | admin | libre |
| Usuarios | `Pages/Admin/Users.aspx` | admin | después de la E4 de la 021 |
| Inicio del operador | `Pages/Training/StartSession.aspx` | Operator | después de la E3 de la 021 |

**Fuera:**
- **Resumen:** sigue con el mockup v1 aprobado (D-159, D-164). Si una pantalla aprobada cambia
  tokens o componentes, el Resumen los hereda sin rediseño.
- **"Elige tu área"** (spec 021, P-10): se hace con el sistema de diseño de `Site.Master`, sin
  mockup propio. Si los mockups del inicio del operador cambian ese lenguaje, la hereda.
- **El POD** (`Training.Master`, D-154) y los **Excel** (D-158), que no cambian.
- La **ficha impresa** del Operador sí entra, solo en su disposición: sigue en claro y sin menú,
  botones ni curva (D-157).

## Restricciones fijas (todas las pantallas)

- **Web Forms + Bootstrap 5.3.8 + Bootstrap Icons 1.13.2 locales.** Sin React, sin frameworks
  de JS, sin CDN ni fuentes web: fuente del sistema (D-156). Chart.js 4.5.1 local solo donde
  ya hay gráficas (Operador).
- **Claro y oscuro** con los tokens `--mt-*` de `UI/master/admin.css` y el interruptor de tema
  (D-157). **Contraste AA** en los dos; campos con su borde de 3:1 (D-162); ningún estado solo
  por color (D-156).
- **Solo datos existentes** (los que hoy devuelve la BLL para esa vista, más el área de la 021),
  salvo las métricas derivadas aprobadas (D-205).
- **Comportamiento intacto:** mismos controles de servidor, postbacks, filtros en la URL,
  confirmaciones (`adminConfirm`) y mensajes del servidor (SweetAlert). En el inicio del
  operador, la misma cascada y el botón Iniciar. Lo que sea cliente (desplegar, abrir un modal)
  sigue sin postback.
- **Anclas `data-guide`** y los ids que use el código, intactos.
- **Escritorio, proyector y tablet desde 768 px** (D-153): las tablas se desplazan dentro de su
  tarjeta, nunca la página (P-4 de la 019).

## Métricas nuevas: solo derivadas (D-205)

- Una alternativa puede proponer cifras o ayudas que **se calculan en la BLL con los datos que
  la vista ya carga** (ej. cuántos operadores de la lista están Reprobados en alguna guía, o
  cuántos de Seguimiento cumplen 2 o más reglas). Cada una aprobada es una **D-n**, con su regla
  escrita en el plan y su test de la BLL.
- Lo que necesite **SPs, columnas o datos nuevos** (como "N sin iniciar", D-164) **no entra**:
  se anota en "Candidatas para otra spec" de esta spec y va por la fila 1.
- Las métricas nunca cambian `ReadinessPolicy`, `StepRuns`, las reglas de Seguimiento ni los
  números que ya se muestran.

## Sistema de diseño: el v1 es la base (D-206)

- Los tokens y componentes del v1 (D-156, D-162; `admin.css`) son el **punto de partida, no un
  límite**. Cada alternativa **marca** lo que cambia respecto al v1 (token nuevo, valor distinto,
  componente nuevo o modificado).
- Cada cambio aprobado es una **D-n** y vale para **todas** las pantallas (y el Resumen, que lo
  hereda). La primera pantalla aprobada fija los cambios; las siguientes los reutilizan (ver
  "Aprobación").

## Aprobación (D-206)

**Pantalla por pantalla.** Por cada una:
1. El usuario pega su brief (`022-briefs.md`) en Claude Design, con los adjuntos.
2. Claude Design devuelve 3 alternativas. El usuario elige una, o pide ajustes.
3. La exportación aprobada se guarda en `022-design/<pantalla>.html` y se registra aquí, con
   fecha, junto con las D-n de sus métricas y de sus cambios al sistema. Puede llegar por
   cualquiera de dos caminos, a elección del usuario: se la pasa a la sesión de la nube (que
   revisa que no cargue nada externo, la registra y hace commit en su rama `cloud/`), o la
   guarda él en local y hace commit en `TrainingApp`, y después avisa para registrar las D-n.
4. Las pantallas siguientes reciben esa exportación como adjunto: respetan los cambios ya
   aprobados y marcan solo los nuevos.

> **Enmienda 2026-10-28 (D-226, D-227): flujo con handoff.** Claude Design puede mandar el
> diseño aprobado a una sesión de la nube que lo implementa. Desde el Login, cada pantalla
> sigue así:
> 1. El usuario elige la alternativa en Claude Design y la manda a una sesión de la nube con
>    el **prompt de handoff** de `022-briefs.md`.
> 2. Esa sesión implementa en una rama `cloud/022-<pantalla>` desde `origin/TrainingApp`, con el
>    commit marcado "no verificada" (la nube no compila ni prueba), y deja en la rama
>    `docs/specs/022-design/<pantalla>.md` con los cambios frente al v1 (sin capturas, D-240).
> 3. La nube de planeación (esta spec) registra las D-n y enmienda la spec y el plan.
> 4. **Desarrollo** trae la rama a la entrega del plan: Rebuild, `aspnet_compiler`,
>    `/code-review`, el "funciona" del usuario y el commit en `TrainingApp`. La rama del handoff
>    es un punto de partida, nunca se integra sin ese ciclo.
>
> **Criterio de aceptación (D-227, excepción a P-3):** la exportación de Claude Design carga
> React desde unpkg, así que no se guarda. En `022-design/` quedan **capturas PNG** de la
> alternativa aprobada (claro, oscuro y tablet) y la **lista de cambios** (`<pantalla>.md`),
> más las D-n de abajo.
>
> **Enmienda 2026-10-28 (D-240, enmienda D-227):** las capturas las toma **Desarrollo**, solo de
> la **versión final** aprobada de cada pantalla (no de cada iteración), en la entrega del plan.
> Si su contexto no alcanza, le pregunta al usuario antes de omitirlas o abaratarlas. La sesión
> del handoff ya no agrega capturas: solo la lista de cambios.

## Pantallas aprobadas

### Login (2026-10-28, alternativa 3b, D-220 a D-225)

Rama de partida: `cloud/022-login-3b` (`c7294ee`, "no verificada"). Criterio:
`022-design/login.md` y sus capturas.

| D | Qué |
|---|---|
| D-220 | **Alternativa 3b: panel dividido.** A la izquierda, un panel de identidad (logo, frase, pie y un ícono `bi-display` de fondo); a la derecha, el formulario. En tablet (< 992 px) el panel queda como una franja con el logo, sin la frase, el pie ni el ícono de fondo |
| D-221 | **Tokens nuevos** en `admin.css` (ninguno existente cambia): `--mt-brand-bg` `#16345F` / `#13233A` (fondo del panel), `--mt-on-brand` `#FFFFFF` / `#E5EAF1` (texto), `--mt-on-brand-2` `#C3D2EA` / `#A9B8CF` (texto secundario), `--mt-brand-line` `rgba(255,255,255,.07)` / `.05` (retícula de 32 px). Valen para toda la app |
| D-222 | **Componentes nuevos:** `.auth-split` (columnas 5fr / 6fr, alto de pantalla), `.auth-brand`, `.auth-watermark`, `.auth-logo`, `.auth-pitch`, `.auth-eyebrow`, `.auth-headline` (34 px), `.auth-brand-foot`, `.auth-side`, `.auth-form` (380 px máx.) y `.auth-note`, solo del login. **`.mt-status-line`** (error del servidor en `--mt-bad`, 14 px semibold, alto reservado de 28 px) es el único pensado para reutilizarse |
| D-223 | **`Admin.Master` en modo `FullBleed`:** la página puede ir a pantalla completa, sin la barra superior ni `.admin-main`; el interruptor de tema pasa a la página (arriba a la derecha del formulario). Solo lo usa el login; las demás pantallas no cambian |
| D-224 | **Errores del login en la línea de estado** sobre Entrar, ya no en un SweetAlert (mismos mensajes y flujo). Campos con íconos (`bi-person-badge`, `bi-key`) y Entrar con `bi-box-arrow-in-right`, por lo que `BtnLogin` pasa de `asp:Button` a `asp:LinkButton` |
| D-225 | **Textos nuevos:** lead "Con tu código de empleado y tu contraseña."; en el panel, "Simulador de entrenamiento · SAP MES", "Entrena y evalúa los flujos de SAP MES de manufactura médica." y "Un solo acceso para operadores y administradores"; el aviso "¿Primer acceso? Entra con la contraseña por defecto; enseguida te pedirá crear una nueva." |

### Cambio de contraseña (2026-10-28, alternativa 2a, D-228 a D-232)

Rama de partida: `cloud/022-cambio-contrasena` (`221297f`, "no verificada"), que **sale de
`cloud/022-login-3b`**. Criterio: `022-design/cambio-contrasena.md`. Sin tokens nuevos: usa los
del Login (D-221).

| D | Qué |
|---|---|
| D-228 | **Alternativa 2a:** franja de marca a pantalla completa (logo e interruptor de tema) y una tarjeta de 480 px montada 48 px sobre ella. En primer acceso o tras un reset, **3 pasos** con línea de progreso ("Entraste", "Contraseña personal", "Tu inicio"), con la misma condición que el aviso. Nueva y Confirmar lado a lado (apiladas bajo 576 px), y la lista fija de los 3 requisitos que ya valida la app |
| D-229 | **Componentes nuevos:** `.pw-page`, `.pw-band`, `.pw-band-top`, `.pw-logo`, `.pw-logo-mark`, `.pw-card` (sobre `.admin-card`), `.pw-pair`, `.pw-rules` y `.pw-cancel`, solo de esta pantalla; **`.mt-steps` / `.mt-step`** (`is-done`, `is-current`, `is-todo`), reutilizable |
| D-230 | **Pantalla completa siempre** (`FullBleed`, D-223), para los dos roles y también para quien llega por la URL sin estar obligado (usuario, 2026-10-28). Nadie ve el menú, su Code1 ni Cerrar sesión hasta guardar; el acceso ya lo bloqueaban las redirecciones a `ChangePasswordUrl` |
| D-231 | **Errores del servidor en `.mt-status-line`** sobre Guardar, no en SweetAlert (como D-224); validadores sin cambio. `BtnSave` y `BtnCancel` pasan a `asp:LinkButton` con ícono; Cancelar queda como botón de texto bajo Guardar. Mismos `OnClick`, `CausesValidation="false"` en Cancelar, Enter = Guardar |
| D-232 | **Textos nuevos:** los 3 pasos; los requisitos "Al menos 8 caracteres", "Distinta de la contraseña por defecto" e "Igual en Nueva y Confirmar"; "Guardar y continuar" (antes "Guardar") y "Cancelar y cerrar sesión" (antes "Cancelar") |

### Guías (2026-10-29, alternativa 2b, D-233 a D-236)

Rama de partida: `cloud/022-guias` (`f715fe1`, "no verificada"), desde `TrainingApp`. Criterio:
`022-design/guias.md`. Sin tokens nuevos.

| D | Qué |
|---|---|
| D-233 | **Alternativa 2b:** la misma tabla, con dos chips de conteo en el encabezado del panel ("N Estándar", "N Custom"), las guías **Custom primero** (dentro de cada tipo, el orden de la BLL) y la nota al pie "Las guías con Custom van primero."; chips y nota se ocultan sin guías. Selector, Guardar y confirmación sin cambios |
| D-234 | **Componentes nuevos:** `.guides-head`, `.guides-counts` y `.guides-note`, solo de esta pantalla; reutiliza `.mt-chip` y `.mt-panel-body` |
| D-235 | **Métrica derivada (D-205): guías publicadas por tipo y el orden Custom primero, en la BLL** (no en el code-behind, como lo dejó el handoff), **sin test** por decisión del usuario (excepción a D-205: es contar y ordenar lo que la vista ya carga) |
| D-236 | **El aviso de guardado nombra la guía** (ej. "Moldeo · Split quedó con el Resource Setup Custom"), porque la fila puede cambiar de grupo al guardar. Solo cambia el texto del aviso, no el flujo |

> **Enmienda 2026-10-29 (D-237, usuario):** Claude Design respetaba demasiado la estructura y
> los colores del v1. El v1 y las pantallas aprobadas son **referencia, no molde**: en cada
> pantalla, al menos una alternativa rompe con el v1 en estructura y en lo visual, otra puede
> ser intermedia y solo una se queda cerca. Las "Restricciones fijas" no cambian. Cada cambio
> marca qué conlleva: tokens (y si afectan pantallas ya hechas), componentes, si toca
> `Admin.Master` o pantallas implementadas, y el esfuerzo. Precisa D-206 (el v1 como base). En
> el "Contexto común" de `022-briefs.md`.

> **Enmienda 2026-10-29 (D-238, usuario): la barra superior entra en la 022** como "pantalla
> 0", antes de seguir con Seguimiento. Un solo componente horizontal para `Admin.Master` y
> `Site.Master` (no el Login, el Cambio de contraseña ni el POD). Problema: se ve plana y
> genérica, y el menú del admin no tiene orden. La marca es siempre **"TrainingApp"** para los
> dos roles. Qué lleva la del operador lo propone el diseño, solo con datos que ya existen. Brief
> 0 de `022-briefs.md`; entrega **E1b** del plan. Las pantallas ya hechas y Pasos la heredan
> por el master; las que faltan se diseñan con ella.

### Barra superior (2026-10-29, propuesta 1 de la cuarta ronda, D-286 a D-290)

Rama de partida: `cloud/022-barra-superior` (desde `TrainingApp`, "no verificada"); implementada y
probada por el usuario en `spec-022` (E1b: `b34eafb` y cierre `feb1f4f`). Criterio:
`022-design/barra-superior.md`. Se descartaron tres rondas antes (A a G).

| D | Qué |
|---|---|
| D-286 | **Propuesta 1, "banda de marca con menú agrupado":** una fila en el azul del Login 3b (`--mt-brand-bg` con su retícula), un componente para `Admin.Master` y `Site.Master`. Marca: baldosa `bi-display` y "TrainingApp" (D-238). **Admin:** las 6 vistas en dos grupos con nombre, "Dashboard" (Resumen, Operadores, Pasos, Seguimiento) y "Administración" (Usuarios, Guías), como pastillas; la vista actual rellena en `--mt-on-brand` y en negrita, no solo color. **Operador:** sin menú, con "Tu área · Producto · Área" junto a la marca. **Cuenta:** iniciales, nombre y, debajo, el último acceso (admin) o "Code1 n" (operador); después tema y Cerrar sesión |
| D-287 | **Tokens nuevos** (valen para toda la app; ninguno existente cambia): `--mt-brand-raise` `rgba(255,255,255,.10)` / `.07` (pastillas, baldosa y botones sobre la marca) y `--mt-brand-edge` `rgba(255,255,255,.22)` / `.16` (bordes, divisores y hover). El Login puede pasar a ellos sin cambio visible (no se tocó) |
| D-288 | **Componente `.mt-topbar`**, que reemplaza a `.admin-bar` (se borran `.admin-brand`, `.admin-nav-links`, `.admin-bar-end` y `.admin-user*`). Los masters suman controles de presentación (`LitCurrent`, `PhAdminGroup`, `UserAccount`, `LitInitials`; en `Site.Master` `PhArea`, `LitArea` y `ShowArea`, que llama `StartSession`) y `UI/master/TopBar.cs` (iniciales y título, solo presentación); `theme.js` abre y cierra el menú plegado sin postback. **Responsive:** bajo 1400 px el último acceso pasa al `title`; bajo 1200 px el menú se pliega en "Menú · <vista>"; a 991 px o menos, iniciales y botones con ícono; a 575 px o menos, la baldosa sola. La impresión la oculta |
| D-289 | **Cambio a una pantalla cerrada (Pasos, E4):** la franja de Pasos pasa a ser una **tarjeta** (margen, esquinas de 16 px, sombra, al ancho de `.steps-wrap`), para no tener dos bandas azules seguidas bajo la barra. Precisa D-239 |
| D-290 | **Textos:** `Site.Master` dice "TrainingApp" en la marca y el `<title>` (antes "Simulador de entrenamiento MES"); el admin ve su **nombre** (antes su Code1; si no hay nombre, el Code1); el operador "Code1 n" y "Tu área"; nuevos "Dashboard", "Administración" y "Menú" |

### Usuarios (2026-10-30, alternativa CD-3 "Consola de cuentas", D-306 a D-314)

Primera pantalla con el handoff local de Claude Design (D-251): sin rama de la nube; Desarrollo
la implementó en `TrainingApp` (`8a9664c`, cierre `733f70d`). Criterio: `022-design/usuarios.md`.
Una ronda; sin tokens nuevos ni cambios en los masters. Planeación revisó que ningún desvío es de
alcance. Los desvíos 1, 6 y 8 de `usuarios.md` van juntos en D-311.

| D | Qué |
|---|---|
| D-306 | **Alternativa CD-3, "consola de cuentas":** una tarjeta con dos columnas. **Directorio** a la izquierda: título, "Crear usuario", Buscar, el resumen "En esta página: N bloqueadas · N deben cambiar la contraseña · N sin área", una fila por cuenta (iniciales, nombre, "Code1 · Producto · Área", insignia de problema o "Activa") y el paginador. **Ficha** a la derecha, de la cuenta elegida: encabezado en el azul de marca (nombre, Code1, rol y estado), **Acceso** (qué pasa, Desbloquear y Resetear con su consecuencia), **Área**, **Cuenta** (alta, último acceso con "hace N días", sesiones) y una zona aparte **Borrar usuario**. Al cargar se abre la primera cuenta con problema de la página (o la primera); Crear sustituye a la ficha. Bajo 992 px, una columna y la ficha como panel fijo con "Volver a la lista", foco y Esc. Elegir, abrir Crear y abrir o cancelar el área son cliente, sin postback; los postbacks y las confirmaciones (`adminConfirm`, `ShowResult`, `ShowMessage`) son los de siempre |
| D-307 | **El área se cambia en la ficha, no en una columna** (enmienda D-208 y el brief 8): no hay columna Área; "Cambiar área" (con lápiz) en la sección Área abre **radios agrupados por producto** en lugar del `<select>` con `<optgroup>`, con Guardar (`usersAreaConfirm`) y Cancelar, sin postback hasta Guardar. Mismo contrato: `area_{id}` en `Request.Form`, `SetArea` de la BLL rechaza `null` (sigue sin "Quitar área"), los administradores con "—" y sin el botón. **Cliente:** en Sin área no hay área preseleccionada y Guardar queda deshabilitado hasta elegir un área distinta de la actual (antes se preseleccionaba la primera); aprobado por el usuario |
| D-308 | **Datos que pasan a la ficha (P-5):** el directorio ya no muestra el Rol del operador (solo "Administrador" bajo el nombre), la fecha de alta ni el último acceso; están a un clic, en la ficha. Ningún dato se quita: Código, nombre, rol, estado, alta, último acceso y área siguen en la pantalla, y la ficha suma las sesiones de entrenamiento (`SessionCount`, que ya existía) |
| D-309 | **Borrar sobre uno mismo:** el botón sale **deshabilitado con el motivo** ("Es tu cuenta: nadie se borra a sí mismo…") en vez de no dibujarse; el `LinkButton` real sigue con `Visible="false"` y la BLL sigue devolviendo `OwnAccount`. B-3 intacto |
| D-310 | **Dos repeaters** (precisa el plan E7, que nombraba solo `RptUsers`): `RptDirectory`, nuevo y solo de presentación (sin comandos, el `Id` en `data-id`), y `RptUsers`, que conserva su id y su `OnItemCommand` y ahora pinta **una ficha por cuenta** (20 por página; solo la elegida visible). `HfSelected` recuerda la cuenta elegida entre postbacks; si ya no está en la página, vuelve a la primera con problema |
| D-311 | **Crear y Buscar:** se conserva `DdlNewRole` (el prototipo pedía un control segmentado, que cambiaría el id; no se implementó). `BtnSearch` y `BtnCreate` pasan a `LinkButton` con ícono (mismos ids, `OnClick`, `ValidationGroup` y `DefaultButton`; precedente D-224). Tras Crear la lista se recarga en la misma página, como antes: no se implementó el salto a la página del nuevo usuario que traía el prototipo |
| D-312 | **Componentes nuevos `.users-*`**, solo de esta pantalla (`.users-shell`, el directorio `.users-list`/`-row`/`-avatar`, la ficha `.users-pane`/`-sec`/`-act`/`-facts`/`-area-*`/`-danger` y `.users-scrim`); se borran `.admin-users`, `.admin-area-cell`, `.admin-area-edit`, `.admin-icon-btn` y `.admin-row-actions`; `.admin-page-head` ya no se usa aquí. **Sin tokens nuevos**. La ficha de marca bajo la barra de marca se aceptó (precedente D-289). `UserListRow` suma campos de presentación (iniciales, estado corto, textos de la ficha) |
| D-313 | **Métrica derivada (D-205) en la BLL, sin test** (como D-235, D-283, D-293, D-297 y D-304): `UserPageSummary` en `UserSearchPage.Summary` cuenta **solo en la página abierta** las cuentas bloqueadas, las que deben cambiar la contraseña (`UserAdminService.StateOf`) y los operadores sin área. No cambia `Search`, el orden, el total ni la paginación, ni hay SP nuevo. Contar en todas las páginas pediría una consulta agregada: candidata para otra spec, sin numerar |
| D-314 | **Textos nuevos:** el resumen del directorio, "Activa" y "tú"; los de Acceso ("qué pasa" en Bloqueada, con el máximo de `LoginLockPolicy.MaxFailedLogins`, y en Debe cambiar, con y sin último acceso) y la consecuencia de cada acción; los de Área ("Los administradores no tienen área", "Aún no eligió su área…", "Cambiar área"); los de Cuenta ("Sesiones de entrenamiento", "hoy / ayer / hace N días / Nunca ha entrado"); el motivo de Borrar sobre uno mismo; y los de Crear. "Hace N días" reutiliza `ReportText.DaysAgo`. Los textos de "qué pasa" y de las acciones son **guía nueva para el admin: los valida negocio** (si cambian, cambia solo el texto) |

### Operador (2026-10-30, alternativa H "tres preguntas con historial por guía", D-299 a D-305)

Rama de partida: `cloud/022-operador` (`bfdad5c`, "no verificada"), desde `TrainingApp` (con
Seguimiento y Operadores). Criterio: `022-design/operador.md` (su renglón de capturas quedó sin
efecto por D-242). Tres rondas; sin tokens nuevos ni cambios en los masters.

| D | Qué |
|---|---|
| D-299 | **Alternativa H, "tres preguntas con historial por guía":** encabezado con volver a Operadores, nombre, "Code1 n", la etiqueta de su área e "Imprimir ficha". Arriba, **tres tarjetas**: "¿Está listo en su área?" (por guía de su área: insignia, mejor y último puntaje, "hace N días", la diferencia con su mejor y la línea de qué hacer de Operadores; Sin área y Sin guía publicada con su nota), "¿Qué le falla?" (pasos a reforzar por guía con la etiqueta Su área / Otra área; los de otras áreas, ocultos tras "Ver también otras áreas (N pasos)" en el cliente, salvo que no haya de su área) y "¿Qué ha hecho?" (N sesiones, la última, el reparto por modo y "También entrenó"). Debajo, **"Historial por guía"**: un `<details>` por guía con sesiones (su área primero y abierta, las otras plegadas; todas abiertas si no tiene área), con el resumen de la guía, su curva, su lectura y su tabla de sesiones. Mismo `?id=`, mismo regreso con `?missing=1`, sin postbacks |
| D-300 | **Se aparta del brief 7 (Sesiones "tabla completa, todas las áreas"):** en pantalla las sesiones se parten por guía dentro del historial, **sin la columna Área** (la da el encabezado del bloque), y el resumen de cada bloque reemplaza las tarjetas de "Otras áreas". La **ficha impresa conserva la tabla única** de todas las áreas con su columna Área (D-269), así que no se pierde ningún dato. Aceptado por el usuario al elegir H |
| D-301 | **Curva nueva** (Chart.js 4, `admin-charts.js`): dos paneles apilados y alineados por sesión, el puntaje de cada Assessment arriba (✓ y punto verde si aprobó, línea punteada del mínimo, `ScoreCalculator.PassingScore`) y los intentos fallidos de Práctica libre y Assessment abajo, con los valores escritos (`mtCharts.valueLabels`, opcional por dataset). Guided no se grafica (sale en la tabla). El JSON de cada curva suma `scores`, `passed` y `passing`; la curva de un bloque plegado se dibuja al abrirlo. Al lado, la lectura (práctica antes del primer aprobado, "Puntajes de Assessment: a → b → c" y tres cifras). En la tabla, el puntaje con un medidor (marca en el mínimo), "Aprobó / Sin aprobar" y la etiqueta "Mejor" |
| D-302 | **Componentes:** se borran los `.operator-*` del v1 (`-top`, `-guides`, `-guide*`, `-reinforce`, `-reinforce-guide`, `-section*`, `-curves`, `-curve`, `-other`); entran `.operator-head`, `.operator-tag`, `.operator-q*`, `.operator-mix*`, `.operator-hist*`, `.operator-meter`, `.operator-verdict`, `.operator-fails` y afines, solo de esta pantalla. Se conservan `.mt-badge-lg` y `.operator-reinforce-none`. Modos con `--mt-m1` a `--mt-m3` y el mínimo con `--mt-ok`: **sin tokens nuevos**. Controles: se borran `LblAreaNote`, `PhOtherGuides`, `DetOtherGuides`, `RptOtherGuides`, `PhCurves`, `RptCurves` y `PhSessions`; los nuevos y las filas de presentación, en `operador.md` |
| D-303 | **Ficha impresa** (precisa D-157 y D-270): siempre en claro, sin barra, botones, curva ni historial; las tres preguntas apiladas (con la línea de qué hacer y el reparto por modo), **todos** los pasos a reforzar (también los ocultos en pantalla), "También entrenó: …" y la tabla única de Sesiones con Área |
| D-304 | **Métricas derivadas (D-205) en la BLL, sin test** (como D-235, D-283, D-293 y D-297): `OperatorActivity` en `OperatorDetail.Activity` cuenta las sesiones por modo, la última y sus días, y por guía (`GuideHistory`) las sesiones, los Assessment que cuentan y los aprobados (`ReadinessPolicy.CountsAsAssessment` e `IsPassed`), los intentos fallidos, la mejor sesión, último menos mejor y los días; `OperatorSessionRow` suma `Passed`. Solo sobre lo que `GetOperator` ya cargó, con el reloj inyectable del servicio. **No es una regla nueva:** `ReadinessPolicy`, `OperatorArea`, `FollowUpRules` y el mínimo no cambian |
| D-305 | **Textos nuevos:** las tres preguntas con su bajada, "Mejor puntaje", "Último puntaje", "Última actividad … · hace N días.", "N puntos menos que su mejor" / "Igual que su mejor", "Su área" / "Otra área", "Ver también otras áreas (N pasos)" / "Ocultar las otras áreas", "N sesiones", "Última: …", "Historial por guía" y su bajada, la lectura de la curva, "Todavía no tiene sesiones de Práctica libre ni Assessment: Guided no se grafica.", "Mejor" y "Aprobó" / "Sin aprobar". La línea de qué hacer es la de Operadores (`StateHint`, D-298): la valida negocio y, si cambia, cambia en las dos pantallas |

### Operadores (2026-10-30, alternativa D con el encabezado K1, D-295 a D-298)

Rama de partida: `cloud/022-operadores` (`94c0405`, "no verificada"), desde `TrainingApp` (con la
024 integrada). Criterio: `022-design/operadores.md`. Cuatro rondas; sin tokens nuevos ni cambios
en los masters.

| D | Qué |
|---|---|
| D-295 | **Alternativa D, "pestañas por estado", con el encabezado K1 "barra proporcional"**, todo en una tarjeta como Seguimiento: antetítulo "Estado en la guía de su área · <área>", título y filtros (Buscar, Área, Filtrar, Descargar); debajo, la **barra por estado**, un tramo por estado (Reprobado, Sin iniciar, En entrenamiento, Aprobado) y uno para Sin área, cada uno un enlace con los filtros en la URL (`?state=`, `?area=none`), con su número, ícono, nombre y "N% de la vista"; ancho proporcional con un mínimo del 7%, un estado con 0 no sale salvo que esté elegido, el elegido con borde y `aria-current`; "Ver todos N". Al pie, la línea de **qué hacer** con el grupo elegido y "N sin actividad en 14 días o más". **Tabla:** la del v1 más "hace N días" bajo la última actividad (ámbar con reloj desde 14) y la columna "Ver detalle". Orden, paginación de 50, vacío, "Quitar filtros" y Excel sin cambios. Se quita `DdlState` (el estado aplicado viaja en `HfState`); elegir un estado conserva el área salvo Sin área, que vuelve a Todas (D-267). Sin postbacks nuevos. A 767 px o menos la barra pasa a dos tramos por fila |
| D-296 | **Componentes nuevos `.operators-*`**, solo de esta pantalla (encabezado, barra y sus tramos `-seg-*`, línea de qué hacer `.operators-callout` con `.operators-tone-*`, `.operators-ago` y `.operators-go`); `.admin-page-head` ya no se usa aquí. `OperatorListRow` suma `AgoText`, `Inactive` y `HasAgo`, con `OperatorMeterSegment` y `OperatorHint`; el % y el ancho de cada tramo son presentación (code-behind) |
| D-297 | **Métricas derivadas (D-205) en la BLL, sin test** (como D-235, D-283 y D-293): `OperatorSummary` en `OperatorPage.Summary` cuenta por estado (con la búsqueda y el área, sin el filtro de estado; con Sin área, en todas las áreas), Todos, Sin área y los inactivos de la lista filtrada; `OperatorRow` suma `DaysSinceActivity` e `Inactive`. "Inactivo" = guía en su área, no Sin iniciar y 14 días o más desde `LastActivity`, con el mismo umbral de S-2 (`FollowUpRules.InactiveDays`): **no es una regla nueva** ni cambia `ReadinessPolicy`, `OperatorArea` ni las reglas de Seguimiento. `FindOperators` arma primero a todos los de la búsqueda y después aplica área y estado: filas, orden, total y paginación iguales |
| D-298 | **Textos nuevos:** el antetítulo (se quita la bajada del v1), "Elige un tramo para ver ese grupo · el ancho es cuántos hay", "Ver todos", "N% de la vista", "N sin actividad en 14 días o más", "hoy / hace 1 día / hace N días", "Ver detalle" y las seis líneas de **qué hacer** (Reprobado, Sin iniciar, En entrenamiento, Aprobado, Sin área, Todos), en `ReportText.StateHint`. Las de qué hacer son **guía nueva para el admin: las valida negocio** antes de cerrar la E6 (si cambian, cambia solo el texto) |

> **Integración 2026-10-30 (Desarrollo, `1afe5ed`):** ajustes de Desarrollo en el título, los
> campos y "Ver detalle" (`.operators-go` con el color de acento del canvas, que la rama no traía).
> Sin D-n nueva: es lo aprobado en D-295 y D-296.

### Seguimiento (2026-10-30, alternativa B con el encabezado P1, D-291 a D-294)

Rama de partida: `cloud/022-seguimiento` (`6f9042f`, "no verificada"), desde `TrainingApp`.
Criterio: `022-design/seguimiento.md`. Cuatro rondas; sin tokens nuevos ni cambios en los masters.

| D | Qué |
|---|---|
| D-291 | **Alternativa B, "cola de llamadas", con el encabezado P1 "encabezado-índice":** una tarjeta con título, bajada y filtros, y debajo pestañas-índice con la cifra en grande ("N Llamar primero", "N Después") que son anclas sin postback, más los casos por regla y "N casos de N operadores". **Llamar primero** (2 reglas o más): una tarjeta por caso con las reglas como frases con su etiqueta S-n, la última sesión y "Ver detalle". **Después** (1 regla): una fila compacta por caso. **Columna lateral "En la cola":** el total y una barra por regla con su definición. Vacío propio. Orden, filtro en la URL, Descargar y enlace al detalle sin cambios |
| D-292 | **Componentes nuevos `.followup-*`**, solo de esta pantalla (encabezado y pestañas, etiqueta S-n con `is-alert` para S-1, distribución con columna lateral, tarjetas, filas compactas, vacío y barras); se borran `.followup-rules`, `.followup-table` y `.followup-chips`. `.mt-chip` sigue para Guías. `FollowUpListRow` cambia `Chips` por `Lines`; `ReportText.FollowUpLines` (Excel) mantiene el mismo texto |
| D-293 | **Métrica derivada (D-205) en la BLL, sin test** (como D-235 y D-283): `FollowUpSummary` en `FollowUpReport.Summary` cuenta los casos con 2 reglas o más ("Llamar primero", `CallFirstMinRules = 2`) y con 1, los casos por regla y los operadores, solo sobre lo que `GetFollowUp` ya devuelve. "Llamar primero" es una **agrupación de presentación**, no una regla de seguimiento nueva: `FollowUpRules`, `ReadinessPolicy` y el orden no cambian |
| D-294 | **Textos nuevos:** la bajada "A quién llamar primero y de qué hablarle…", las pestañas, los conteos ("N casos de N operadores"), "N de 3 reglas", "Última sesión de la guía", "Ver detalle", la columna "En la cola" con los nombres "Reprobados", "Sin practicar" y "Paso trabado", y el vacío "Nadie en la cola de <guía>". Las frases de las reglas no cambian (son las del Excel) |

> **Integración 2026-10-30 (Desarrollo, `11d4173`):** por decisión del usuario quedan como en
> el mockup la columna "En la cola" también con la cola vacía y el formato de Code1 ("· Code1 123"
> en la tarjeta, "· 123" en "Después"). Sin D-n nueva: es lo aprobado en D-291 y D-294.

### Pasos (2026-10-29, alternativa 4a con el encabezado H4, D-239 y D-280 a D-283)

Rama de partida: `cloud/022-pasos` (`209383c`, "no verificada"), desde `TrainingApp`. Criterio:
`022-design/pasos.md`, **con el cambio de D-280** (la rama trae la paleta índigo que se quita).

| D | Qué |
|---|---|
| D-239 | **Alternativa 4a, "mesa de enseñanza":** una franja de lado a lado bajo la barra (etiqueta, título y rango a la izquierda; Período, Guía, Modo, Filtrar y Descargar a la derecha; en tablet bajan); **una columna por paso** (HTML/CSS) con su %, "#n", nombre y cambio; el **panel del paso elegido** (el #1 al cargar) con los 2 motivos, "Ver todos", los números, el cambio con Mejora / Empeora y Anterior vs. Este período; y el **ranking completo** abajo. Elegir un paso se resuelve en el cliente, sin postback. Lo que antes se veía de todos los pasos a la vez (2.º motivo, conteos, mediana, saltos y referencia del cambio) queda a un clic, en el panel del elegido |
| D-280 | **Sin paleta propia (usuario):** se quita el acento índigo y todos los valores de `--mt-*` que la rama redefine para `body.steps-4a`; Pasos usa los **tokens originales** de la app. **Sin tokens nuevos:** la franja usa los del Login (`--mt-ink` → `--mt-brand-bg`, `--mt-on-ink` → `--mt-on-brand`, `--mt-on-ink-2` → `--mt-on-brand-2`, `--mt-on-ink-line` → `--mt-brand-line`) y las barras del período anterior (`--mt-bar-2`) un token existente con contraste suficiente (lo elige Desarrollo y lo revisa en claro y oscuro). La barra superior de Pasos no se tiñe |
| D-281 | **Piezas compartidas:** `Admin.Master` suma `WideMain` (contenido de lado a lado con la barra visible), solo para Pasos; `PageCss` y `<body runat="server">` solo si siguen haciendo falta sin la paleta (D-280). `PeriodFilter` suma `ShowsRange` (por defecto `true`; Resumen no cambia). Componentes nuevos `.steps-*`, solo de Pasos |
| D-282 | **Modo** pasa de `DdlMode` (lista) a `RblMode` (control segmentado), con los mismos valores y la misma URL: el comportamiento de la 023 no cambia (D-217) |
| D-283 | **Textos y métrica derivada:** los textos nuevos de `pasos.md` (título "Qué pasos fallan más en…", "Mesa de enseñanza"…). La **parte de cada motivo** sobre los registrados en el paso ("6 veces · 54.5%") va **en la BLL, sin test**, como D-235; la rama la calcula en el code-behind |

> **Enmienda 2026-10-29 (D-241, usuario, Desarrollo):** al probar Pasos, el usuario vio que el
> ranking completo repite las columnas y el panel del elegido. Se mantiene, pero **plegado**
> (`<details>`, cerrado al cargar, "Mostrar / Ocultar"); no se quita (opción C de tres). "Ver
> paso" y la nota de orden siguen dentro. Enmienda D-239.

> **Enmienda 2026-10-29 (D-284, usuario):** en las sesiones de diseño de la nube, una **vuelta de
> crítica** con la skill `design:design-critique` sobre las 3 alternativas, antes de que el
> usuario elija: con los criterios del brief, un orden y al menos un riesgo por alternativa; se
> corrige lo grave una sola vez y el artifact muestra el resumen de la crítica. Sin revisión de
> accesibilidad ni de textos con skills aparte (el usuario la descartó; la accesibilidad la sigue
> revisando Desarrollo). Plantilla: `022-briefs.md` → "Prompt de sesión de diseño en la nube".

> **Enmienda 2026-10-29 (D-285, usuario):** cada alternativa del mockup es **una sola página
> responsive con el interruptor de tema**, sin copias aparte en claro, oscuro ni tablet. La
> regla de 768 px sin scroll horizontal (AC-4) no cambia. Las capturas finales de Desarrollo
> (D-240) quedan en **claro y oscuro**, sin la de tablet.

> **Enmienda 2026-10-30 (D-242, usuario: "quita esa regla, no pidas capturas"):** la 022 va
> **sin capturas**. Desarrollo no las toma ni las pregunta, y al usuario no se le piden las de
> antes. El criterio de cada pantalla es la lista de cambios `022-design/<pantalla>.md`, el
> canvas aprobado en Claude Design y el "funciona" del usuario en el navegador, en claro, oscuro
> y a 768 px. Deja sin efecto D-240 y la parte de capturas de D-285, y enmienda D-227 y AC-1 ("capturas
> de antes y después en el plan"). Las capturas de `login-*.png` y `cambio-contrasena-*.png` que
> ya están en `022-design/` se quedan. No cambia lo que el usuario adjunta a Claude Design como
> entrada de cada brief ("Capturas:" en `022-briefs.md`). La regla general de los briefs de diseño (`process.md` →
> "Clasificar el pedido") también queda sin capturas de antes y después.

## Decisiones del usuario (2026-10-27)

| D | Decisión |
|---|---|
| D-202 | Cada pantalla lleva **su propio mockup en Claude Design**, con 3 alternativas que difieren en la estructura; la exportación aprobada en `022-design/` es su criterio de aceptación. Reemplaza D-160 y la parte "sin mockup" de D-163 |
| D-203 | **Pantallas:** Login, Cambio de contraseña, Operadores, Operador, Pasos, Seguimiento, Guías, Usuarios e Inicio del operador. Fuera: el Resumen (sigue el v1) y "Elige tu área" (021, P-10) |
| D-204 | **Intención:** dejar de ser tablas planas (jerarquía), decir qué hacer (guiar la decisión de cada pantalla) y darle identidad al login y al cambio de contraseña. Cómo se resuelve cada una, incluida la forma de la ayuda arriba de la tabla y la estructura del login, lo proponen las alternativas |
| D-205 | **Métricas nuevas, solo derivadas** en la BLL con datos que la vista ya carga; cada una aprobada es una D-n. Las que piden datos o SPs nuevos van a otra spec |
| D-206 | **Tokens y componentes del v1 como base**; los cambios se marcan y cada uno aprobado es una D-n para todas las pantallas. **Aprobación pantalla por pantalla**: la primera aprobada fija los cambios del sistema |

## Propuestas de Claude, respondidas por el usuario (2026-10-27)

Registradas como **D-207** al aprobar (2026-10-27). P-1, P-6 y P-7 cambiaron con la respuesta del usuario.

| # | Propuesta | Respuesta |
|---|---|---|
| P-1 | **Orden de diseño:** Login → Cambio de contraseña → Guías → Pasos → Seguimiento → Operadores → Operador → Usuarios → Inicio del operador. **El Login es la primera pantalla aprobada y fija los cambios al sistema de diseño** (D-206); las pantallas libres van primero, como su implementación (P-7) | usuario: Login primero y después las libres (Claude proponía empezar por Operadores) |
| P-2 | **Se diseña el estado de la 021**, no el actual: columna Área en Operadores y Usuarios (con Cambiar y Quitar área), área en el encabezado del Operador, y el inicio con "Producto · Área" como texto y solo el selector de guía (D-167; y el aviso sin área, D-168) | confirmada |
| P-3 | **Exportación:** un HTML autocontenido por pantalla (CSS y SVG inline, sin CDN), con el interruptor de tema funcionando y datos de ejemplo realistas (nombres, Code1, guías de Moldeo, LoFlo, Subensamble 1 y Redefinity). Al guardarlo en `022-design/` se revisa que no cargue nada externo | confirmada |
| P-4 | **Las 3 alternativas difieren en la estructura** (qué va arriba, cómo se agrupa, dónde está la acción principal), y cada una lleva una nota corta: qué decide el usuario con ella y qué cambia del v1 | confirmada |
| P-5 | **Ninguna alternativa quita datos que hoy se muestran** sin marcarlo; quitar o esconder algo detrás de un clic es una D-n | confirmada |
| P-6 | ~~El cambio de contraseña usa el mismo lenguaje que el login aprobado~~ | **descartada:** el cambio de contraseña tiene sus propias 3 alternativas; la coherencia con el login viene solo de los tokens y componentes aprobados |
| P-7 | **Implementación en paralelo con la 021**, para las pantallas libres: Login, Cambio de contraseña, Guías, Pasos y Seguimiento pueden implementarse intercaladas con la 021, cuando el usuario se lo pida a Desarrollo. Operadores y Operador, después de la E5 de la 021; Usuarios, después de su E4; Inicio del operador, después de su E3. Cada entrega queda **bloqueada hasta que su mockup esté aprobado y guardado** en `022-design/` | usuario: en paralelo (Claude proponía empezar al cerrar la 021) |
| P-8 | **Los cambios del sistema** aprobados van en una entrega propia al inicio del plan (`admin.css` y, si hace falta, `admin-charts.js`), con la regresión visual del Resumen, antes de las pantallas. Como va en paralelo con la 021, esa entrega se coordina con Desarrollo para no chocar en `admin.css` con las E3 a E5 de la 021 | confirmada |
| P-9 | **"Candidatas para otra spec":** las métricas con datos nuevos que propongan las alternativas se anotan aquí con fecha, sin numerar, para que el usuario decida después si abre una spec | confirmada |

> **Enmienda 2026-10-27** (por la enmienda 1 de la spec 021, D-208): en P-2, "con Cambiar y
> Quitar área" pasa a ser **el área con un lápiz que abre el selector agrupado en su columna,
> con Guardar y Cancelar**; "Quitar área" ya no existe. El brief de Usuarios y la E7 del plan
> están enmendados. **Rango de numeración de la 022:** D-220 a D-239 y B-30 a B-39
> (`docs/decisions.md`).
>
> **Enmienda 2026-10-27 (2):** la spec 021 quedó **cerrada e integrada** (`TrainingApp` en
> `1edd2ca`). La columna "Implementar" de la tabla de pantallas y P-7 ya no bloquean nada:
> cada entrega solo espera la E1 y su mockup aprobado. El área ya está en la app (Operadores,
> Operador y su ficha, Usuarios con el lápiz, e Inicio con el área como texto), así que las
> capturas actuales ya la muestran.

## Criterios de aceptación

- **AC-1** Cada pantalla de D-203 coincide con su exportación aprobada en `022-design/`, en claro
  y en oscuro (capturas de antes y después en el plan).
- **AC-2** Contraste AA en los dos modos; ningún estado solo por color; los campos con 3:1.
- **AC-3** Mismos controles, postbacks, filtros en la URL, confirmaciones y mensajes que antes;
  anclas `data-guide` intactas. El flujo del operador (login → contraseña → "Elige tu área" →
  inicio → POD) funciona igual.
- **AC-4** A 768 px y proyectado, ninguna pantalla tiene scroll horizontal de página.
- **AC-5** Cada métrica derivada aprobada tiene su D-n, su regla en el plan y su test de la BLL;
  ningún número existente cambia.
- **AC-6** El Resumen sigue igual al v1, salvo los tokens y componentes aprobados (D-206).
- **AC-7** Rendimiento: las vistas siguen en menos de 3 s con la BD de volumen (AC-7 de la 019).

## Scope

### In
- El diseño de las 9 pantallas de D-203, en claro y en oscuro.
- Los cambios de tokens y componentes que se aprueben (D-206).
- Las métricas derivadas que se aprueben (D-205).

### Out
- El Resumen (más allá de heredar tokens), "Elige tu área", el POD y los Excel.
- Datos, SPs o columnas nuevos; cambios de reglas, filtros, estados o cálculos.
- Comportamiento nuevo: acciones, filtros o descargas que hoy no existen.
- React u otro framework, fuentes web, CDN, colores de marca corporativa.

## Coordinación con la spec 021

Diseñar todo ya es solo documentación y no pisa nada. Desarrollo está con la 021: las
pantallas libres de la 022 se pueden intercalar cuando el usuario lo pida; las demás esperan
la entrega de la 021 que indica la tabla de pantallas (P-7). Riesgo: la 021 y la 022 tocan
`admin.css` y `Users.aspx`; la entrega del sistema (P-8) y la de Usuarios se coordinan con
Desarrollo.

## Candidatas para otra spec

_Ninguna todavía._

## Open Questions

_Ninguna._
