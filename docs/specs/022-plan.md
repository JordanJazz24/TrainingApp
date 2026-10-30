# Plan 022: un mockup por pantalla

**Spec:** `022-mockups-por-pantalla.md` (aprobada 2026-10-27) · **Decisiones:** D-202 a D-207 ·
**Briefs:** `022-briefs.md` · **Planeado en:** la nube (`cloud/022-mockups-por-pantalla`) ·
**Implementa:** Desarrollo, en local.

Cada entrega sigue `docs/process.md` → "2. Cada entrega": implementar, verificar con los
comandos de `CLAUDE.md` → "Correr local, compilar y probar", `/code-review` con nivel y foco, el
"funciona" del usuario en el navegador **antes** del commit y commit solo con sus rutas
(`git commit -- <rutas>`), con `docs/status.md` y la casilla de este plan en el mismo commit.

> **Enmienda 2026-10-30 (D-250):** el review de cada entrega va con `/revisar` (en Opus) y el
> cierre con `/cerrar-entrega`. **Esfuerzo** de las entregas que faltan (resto de la E6, E7 y
> E8): `medium`, porque son de presentación; `high` solo si alguna termina tocando BLL o SQL.

## Reglas de este plan

- **Bloqueo por mockup (D-202, P-7):** una entrega no empieza hasta que su exportación está
  aprobada, guardada en `022-design/<pantalla>.html` y registrada en la spec con fecha y sus
  D-n (spec → "Aprobación"). Sin eso, Desarrollo no la toma.
- **Bloqueo por la 021 (P-7):** las entregas marcadas "después de la E*n* de la 021" esperan esa
  entrega cerrada. Las libres se intercalan con la 021 **cuando el usuario se lo pida** a
  Desarrollo; nunca a la mitad de una entrega de la 021.
  > **Enmendado 2026-10-27:** la spec 021 está **cerrada e integrada** (`TrainingApp` en
  > `1edd2ca`). Ninguna entrega de la 022 espera ya a la 021: cada una solo espera E1 y su
  > mockup.
- **Solo presentación:** markup, `admin.css` y, si hace falta, `admin-charts.js` o un JS de
  cliente. Mismos controles de servidor, ids, postbacks, filtros en la URL, `adminConfirm`,
  SweetAlert y anclas `data-guide` (spec → "Restricciones fijas"). El code-behind solo cambia
  binding y visibilidad.
- **Métricas derivadas (D-205):** solo las aprobadas con su D-n. Van en la BLL (en
  `DashboardService` o una regla pura de `BLL/Reports/`), con datos que la vista ya carga, su
  test en memoria y la página solo las pinta. Ningún SP cambia.
- **Ningún color suelto:** todo sale de un token `--mt-*`. Un token o componente nuevo solo si
  su D-n está aprobada (D-206).
- El mockup manda en cómo se ve; esta spec y la 019 (lo vigente) mandan en qué datos y qué
  comportamiento. Si chocan, se para y decide el usuario (enmienda, `process.md` → 1).
- Ninguna decisión nueva en este plan: las de cada mockup se numeran al aprobarlo, dentro del
  rango de la 022: **D-220 a D-239** y **B-30 a B-39** (encabezado de `docs/decisions.md`;
  enmendado 2026-10-27, por el choque con la 021).

## Orden con la spec 023 (enmendado 2026-10-28, D-219)

Decisión del usuario (roadmap → "Spec 023 → Orden"), para que la flecha de la 023 use los
tokens nuevos y `admin.css` no se toque a la vez:

1. **022 E1** (tokens de D-221 y `.mt-status-line`, desde `cloud/022-login-3b`).
2. **023 E1 a E4**, seguidas.
3. **022 E2a** (Login) y el resto de la 022 según sus mockups. **E4 (Pasos)**, solo después de
   cerrar la 023 (D-217).

Reemplaza el "en paralelo" de P-7 para estas entregas: ninguna entrega de la 022, salvo la E1,
se intercala con la 023.

## Checklist común de cada entrega de pantalla

Se repite en E2a a E8; cada entrega agrega lo suyo.

- [ ] Precondición: exportación aprobada en `022-design/` y registrada en la spec; la entrega
      de la 021 que la bloquea, cerrada.
- [ ] ~~Capturas de **antes** (claro y oscuro) guardadas por el usuario, para comparar.~~
      Sin efecto desde el 2026-10-30 (D-242).
- [ ] Implementar según la exportación, con los tokens y componentes vigentes.
- [ ] Verificación (Desarrollo): Rebuild; `aspnet_compiler` (markup tocado); tests si cambió la
      BLL (métricas derivadas).
- [ ] Checklist del navegador:
  - coincide con la exportación en claro y en oscuro (AC-1);
  - contraste AA y ningún estado solo por color; campos con 3:1 (AC-2);
  - mismos filtros, postbacks, confirmaciones y mensajes; filtros en la URL y F5 conserva la
    vista (AC-3);
  - a 768 px y proyectado, sin scroll horizontal de página (AC-4);
  - los números son los mismos que antes (AC-5).
- [ ] ~~Capturas de **después**~~ (D-240, D-285): **sin efecto desde el 2026-10-30 (D-242,
      usuario)**. Ni Desarrollo las toma ni las pregunta; el criterio es la lista de cambios, el
      canvas aprobado y el "funciona" del usuario (claro, oscuro y 768 px).

> **Enmienda 2026-10-28 (D-226, D-227; D-240):** el bloqueo por mockup se cumple con la lista
> de cambios en `022-design/<pantalla>.md` y las D-n registradas en la spec (ya no con un HTML
> exportado). Las capturas no bloquean: las toma Desarrollo al final de la entrega (D-240). (Sin capturas desde el 2026-10-30, D-242.) Cada pantalla puede traer una rama de handoff `cloud/022-<pantalla>` ("no
> verificada"): es el punto de partida de su entrega; Desarrollo la trae, verifica y hace el
> commit con el ciclo normal. Nunca se integra tal cual.

## E1 · Sistema de diseño (P-8)

**Bloqueada hasta:** el mockup del **Login** aprobado (fija los cambios al sistema, P-1). Libre
respecto a la 021, pero se coordina con Desarrollo: la 021 también toca `admin.css` (E3 a E5).

- [x] `UI/master/admin.css`: los tokens y componentes nuevos o cambiados que aprobó el Login,
      cada uno con su D-n en un comentario, en claro y en oscuro. **Login aprobado (D-220 a
      D-225):** los tokens de D-221 y `.mt-status-line` (D-222); los componentes `.auth-*` y
      `FullBleed` van en la E2a. Punto de partida: `cloud/022-login-3b`. Sin tocar el POD
      (`Training.Master` no carga `admin.css`).
- [x] `admin-charts.js`, solo si los tokens de gráficas cambian (no cambian: sin tocar).
- [x] Regresión visual del **Resumen** (AC-6) y de las pantallas aún no rediseñadas: siguen
      bien con los tokens nuevos, en claro y oscuro.
- [x] Verificación (Desarrollo): Rebuild; `aspnet_compiler` si se tocó un master (no se tocó).
- [x] Sin `/code-review` si solo es CSS (`process.md` → 3).

**Hecho cuando:** el usuario confirma el Resumen y una pantalla del dashboard sin cambios
rotos, en claro y oscuro.

> Los cambios al sistema que aprueben mockups posteriores van **al inicio de la entrega de esa
> pantalla**, con la misma regresión del Resumen.

## E1b · Barra superior (agregada 2026-10-29, D-238)

**Bloqueada hasta:** el mockup de la barra superior (brief 0) aprobado. Va **antes** de la E5
(Seguimiento) y de las demás pantallas que faltan; si coincide con Pasos (E4), se coordina el
orden de `admin.css` con Desarrollo.

- [x] `UI/master/Admin.Master` y `Site.Master` (y sus `.cs`): la estructura aprobada, un solo
      componente; mismos `HyperLink` y URLs, `MarkCurrent`, `#mtThemeToggle`, `BtnLogout` y
      `PhMenu` / `PhUser`. Marca "TrainingApp" en los dos (D-238).
- [x] `admin.css`: el bloque de la barra con los tokens aprobados; el `@media print` la sigue
      ocultando.
- [x] Revisar que `FullBleed` (Login y Cambio de contraseña) siga ocultándola.
- [x] `/code-review low --max-findings 3` sobre los masters. Foco: menú solo con
      `CanViewReports`, Cerrar sesión (D-13), anti-CSRF del master.
- [x] Checklist común en cada vista del admin y en el inicio y "Elige tu área" del operador, en
      claro, oscuro y tablet; impresión de la ficha sin barra.

> **Cerrada 2026-10-29 (Desarrollo):** propuesta 1 (banda de marca con menú agrupado) desde
> `cloud/022-barra-superior` (`b34eafb`). `/code-review low` sin hallazgos (no informó qué
> leyó; los puntos del foco se revisaron leyendo el código). Sin capturas.

**Hecho cuando:** el usuario navega todas las vistas con los dos roles y dice "funciona". ✅ Probada por el usuario.

## E2a · Login (libre)

> **Enmendada 2026-10-28 (D-219, decisión del usuario):** la E2 se parte en **E2a (Login)** y
> **E2b (Cambio de contraseña)**, porque solo el Login tiene mockup. La E2a va después de la 023
> (ver "Orden con la spec 023").

**Bloqueada hasta:** E1 y las 023 E1 a E4 (orden de D-219). El mockup ya está aprobado
(D-220 a D-225; `022-design/login.md`).

- [x] `Pages/Auth/Login.aspx`: la estructura aprobada (3b, D-220); mismos `TxtCode1`,
      `TxtPassword`, `BtnLogin` y `DefaultButton`. **Enmendado 2026-10-28 (D-224):** los errores
      van en `.mt-status-line`, no en SweetAlert, y `BtnLogin` pasa a `LinkButton`. Punto de
      partida: `cloud/022-login-3b` (`c7294ee`), que además trae `Admin.Master` con `FullBleed`
      (D-223) y los `.auth-*` (D-222).
- [x] Revisar en la rama del handoff (no se compiló):
  - Enter en un campo dispara `BtnLogin` como `LinkButton` con el `DefaultButton` del panel;
  - un solo `#mtThemeToggle` en el HTML (la barra del master no se renderiza con
    `FullBleed`) y `theme.js` lo toma;
  - los validadores fuera del `input-group` se ven bien y el foco sigue al campo;
  - el mensaje de bloqueo (15 min) y el de credenciales salen en la línea de estado, con
    `aria-live`;
  - `ChangePassword.aspx` y las páginas del dashboard siguen con la barra (sin
    `FullBleed`).
- [x] Capturas `022-design/login-claro.png`, `-oscuro.png` y `-tablet.png`, de la versión
      final, tomadas por Desarrollo (D-240; checklist común).
- [x] `/code-review low --max-findings 3` sobre el markup y los code-behind tocados
      (`Login.aspx`, `Admin.Master`). Foco: que no cambie el flujo de login, primer acceso,
      bloqueo y anti-CSRF.
- [x] Checklist común + login correcto, contraseña incorrecta, cuenta bloqueada y primer acceso
      → cambio de contraseña, con los dos roles.

**Hecho cuando:** el usuario entra con los dos roles y dice "funciona".

## E2b · Cambio de contraseña (libre)

**Bloqueada hasta:** E1 y el mockup del Cambio de contraseña (D-219).

- [x] `Pages/Auth/ChangePassword.aspx`: su estructura aprobada (2a, D-228 a D-232; propia, P-6
      descartada); mismos campos, `BtnSave` y `BtnCancel` (ahora `LinkButton`, D-231). Punto
      de partida: `cloud/022-cambio-contrasena` (`221297f`), que sale de `cloud/022-login-3b`:
      se trae **después** de la E2a. Pantalla completa siempre (D-230). La redirección final no cambia (la puso la 021: después
      de la contraseña, "Elige tu área" si no tiene).
- [x] Reutiliza `FullBleed` y `.mt-status-line` del Login (D-222, D-223), sin afectar las
      páginas del dashboard.
- [x] Revisar en la rama del handoff (no se compiló):
  - Enter guarda con `BtnSave` como `LinkButton` (`DefaultButton`) y con los validadores del
    cliente;
  - `PhSteps` y `PhFirstLogin` siguen visibles tras un postback con error;
  - no queda ningún `ShowMessage` en la página;
  - `ChangePassword.aspx.designer.cs` se editó a mano (`PhSteps`, `PhError`, `LitError`,
    `LinkButton`): regenerarlo si Visual Studio no coincide.
- [x] Capturas: la rama trae `cambio-contrasena-*.png` de la sesión del handoff; Desarrollo las
      **reemplaza** por las de la versión final (D-240). *Hecho 2026-10-28: el usuario decidió
      quedarse con las del handoff.*
- [x] `/code-review low --max-findings 3`. Foco: que no cambie el cambio de contraseña (actual
      siempre pedida, D-3; la nueva no puede ser la por defecto) ni el anti-CSRF.
- [x] Checklist común + primer acceso, cambio voluntario, Cancelar, contraseña actual
      incorrecta y confirmación distinta, con los dos roles.

**Hecho cuando:** el usuario cambia la contraseña con los dos roles y dice "funciona".

## E3 · Guías (libre)

**Bloqueada hasta:** E1 y el mockup de Guías.

- [x] `Pages/Admin/Guides.aspx`: la estructura aprobada (2b, D-233, D-234); mismos `RptGuides`,
      `DdlType` y `BtnSave` por fila, con su confirmación. Estado vacío. Punto de partida:
      `cloud/022-guias` (`f715fe1`, "no verificada"; sale de `TrainingApp`).
- [x] **Mover a la BLL** el conteo por tipo y el orden Custom primero (D-235): el handoff los
      dejó en `BindGuides()` del code-behind. La página solo enlaza lo que devuelve la BLL.
      Sin test (D-235).
- [x] El aviso de guardado nombra la guía (D-236).
- [x] Revisar en la rama del handoff (no se compiló): los 4 controles nuevos en
      `Guides.aspx.designer.cs` (`PhCounts`, `LitCountStandard`, `LitCountCustom`,
      `PhOrderNote`); orden y conteos con 0, 1 y varias guías de cada tipo.
- [x] Sin `/code-review` si solo es markup; si se toca el code-behind, `low --max-findings 3`
      con foco en `CanManageGuides` y en los ids de fila.
- [x] Checklist común + cambiar una guía a Custom y volver a Estándar; la sesión en curso no
      cambia (spec 020).

**Hecho cuando:** el usuario cambia el tipo de una guía y dice "funciona".

## E4 · Pasos (libre)

**Bloqueada hasta:** E1 y el mockup de Pasos.

> **Enmendada 2026-10-28 (D-217, D-219):** además espera que la spec 023 esté cerrada. Su
> mockup incluye el control de período, el rango en palabras y la comparación por paso de la
> 023 (brief 4).

- [x] **Punto de partida:** `cloud/022-pasos` (`209383c`, "no verificada"; sale de `TrainingApp`).
      Estructura aprobada: 4a (D-239). **Antes de la barra superior (E1b):** las dos tocan
      `Admin.Master`.
- [x] **Quitar la paleta índigo (D-280):** fuera los valores de `--mt-*` redefinidos para
      `body.steps-4a` y su entrada en la regla que conecta tokens con Bootstrap; la franja con
      `--mt-brand-*` y las barras del anterior con un token existente; sin tokens nuevos.
      Quitar `PageCss` y `<body runat="server">` de `Admin.Master` si ya no hacen falta; queda
      `WideMain` (D-281).
- [x] La parte de cada motivo, a la BLL, sin test (D-283).
- [x] Revisar en la rama (no se compiló): el designer de `Steps.aspx` (`RblMode`, `LitRange`,
      `LitPrevRange`, `PhRanking`, `RptColumns`, `RptLabels`, `RptDetails`, `RptList`; sin
      `DdlMode` ni `RptGuides`), `PhRange` de `PeriodFilter`, `StepGuideGroup` quitado; que
      `RblMode` publique `""` para Assessment y arme la misma URL (D-282); elegir columnas y
      "Ver paso" con mouse y teclado (`aria-pressed`, foco visible en la franja); muchos pasos,
      "Todas", Práctica libre, "Todo", sin datos en el anterior, Personalizado… e inválido; que
      Login, Cambio de contraseña y Resumen sigan iguales.
- [x] `Pages/Admin/Steps.aspx`: la estructura aprobada; mismos filtros (período, guía, modo) y
      Descargar; "Ver todos" sigue en el cliente; los saltos solo en Práctica libre. El control
      de período, el rango y la comparación son los de la 023 (mismo comportamiento, D-217).
- [x] Métricas derivadas aprobadas (si hay): regla en la BLL con su test.
- [x] `/code-review low --max-findings 3`; `medium` si hay métricas. Foco: mismos números que
      `StepStatistics`, ninguna regla en la página.
- [x] Checklist común + Assessment y Práctica libre, "Ver todos", Descargar.

> **Cerrada 2026-10-29 (Desarrollo):** D-241 (el ranking completo, plegado). Sin `/code-review`
> sobre el markup; el de la BLL y el code-behind, `low`, sin hallazgos. Sin capturas.

**Hecho cuando:** el usuario confirma el ranking de una guía en los dos modos. ✅ Probada por el usuario.

## E5 · Seguimiento (libre)

**Bloqueada hasta:** E1 y el mockup de Seguimiento.

- [x] `Pages/Admin/FollowUp.aspx`: la estructura aprobada; chips con texto (S-1 a S-3), orden
      por número de reglas, filtro por guía y Descargar.
- [x] Métricas derivadas aprobadas (si hay): regla en la BLL con su test; las reglas de
      `FollowUpRules` no cambian. *Desvío aceptado por el usuario: `FollowUpSummary` va sin
      test, como D-235/D-283.*
- [x] `/code-review low --max-findings 3`; `medium` si hay métricas.
- [x] Checklist común + filas con 1, 2 y 3 reglas, vacío y enlace al detalle.

> **Cerrada 2026-10-30 (Desarrollo):** mockup B ("cola de llamadas") con el encabezado P1, desde
> `cloud/022-seguimiento` (`6f9042f`); el resumen de la cola, a la BLL (`FollowUpSummary`, sin
> test). D-291 a D-294 (Diseño UI/UX, 2026-10-30). `/code-review medium` sin bugs; se aplicó
> `ReportText.Operators`. Por decisión del usuario quedan como en el mockup la columna "En la
> cola" con la cola vacía y el formato de Code1 ("· Code1 123" en la tarjeta, "· 123" en
> "Después"). Queda sin uso `.mt-chip-alert` en `admin.css`. Sin capturas.

**Hecho cuando:** el usuario confirma la lista y dice "funciona". ✅ Probada por el usuario.

## E6 · Operadores y Operador (después de la E5 de la 021)

> **Bloqueada 2026-10-28 por la spec 024 "Dashboard por área"** (nube TrainingApp-Desarrollo,
> D-260 a D-279; roadmap → "Spec 024"): quita las columnas por guía, agrega un filtro de Área y
> cambia el significado de "Sin iniciar". No se diseña ni se implementa hasta que la 024 esté
> aprobada y los briefs 6 y 7 enmendados con ella.
>
> **2026-10-29:** la 024 está aprobada y los briefs 6 y 7 ya están enmendados con ella (con su
> enmienda 1, D-272 y D-273), así que ya se pueden diseñar. La implementación sigue esperando a
> que Desarrollo integre la 024 en `TrainingApp`: el diseño reacomoda su estructura, no la
> cambia.
>
> **2026-10-29 (2):** la 024 está **cerrada e integrada** en `TrainingApp` (`e86a9f3`). La E6 ya
> solo espera sus mockups, que se diseñan sobre ese código.

**Bloqueada hasta:** E1 y los mockups de Operadores y Operador. (Enmendado 2026-10-27: la E5
de la 021 ya está cerrada; la columna Área, después de Nombre, y el área en el encabezado y la
ficha ya existen.)

**Esfuerzo (D-250):** `medium` para el detalle y la ficha; `high` si el mockup aprobado agrega
una métrica derivada en la BLL (como `OperatorSummary` en la lista). *Enmendado 2026-10-30: el
detalle aprobado agrega `OperatorActivity` (D-304), así que va en `high`.*

- [x] `Pages/Admin/Operators.aspx`: la estructura aprobada; columnas dinámicas por guía con la
      insignia (`ReportText.StateCss`/`StateIcon`), el Área, filtros, Descargar y paginación de
      50.
- [ ] **Punto de partida del detalle:** `cloud/022-operador` (`bfdad5c`, "no verificada"; sale de
      `TrainingApp` con Seguimiento y Operadores; se integra sin conflictos). Estructura
      aprobada: H "tres preguntas con historial por guía" (D-299 a D-302); se aparta del brief
      en las Sesiones (D-300).
- [ ] `Pages/Admin/Operator.aspx`: la estructura aprobada; estado por guía, pasos a reforzar,
      curva (Chart.js con `admin-charts.js`), sesiones y el área en el encabezado. Mismo `?id=`
      y regreso con `?missing=1`, sin postbacks.
- [ ] Revisar en la rama (no se compiló, `operador.md` → "Para revisar en Desarrollo"; su renglón
      de capturas no aplica, D-242): el `.designer.cs` escrito a mano (`<%# %>` en atributos de
      etiquetas literales, `<%= %>`/`<%: %>` en `PhOtherSteps` y `PhMix`, el repeater anidado
      en `RptHistory`); `DashboardServiceTests` y `DashboardPerformanceTests` en verde (cambió
      el armado de `GetOperator`); la curva, con el puntaje arriba (orden de los ejes apilados,
      no verificado), rótulos sin cortar, una guía solo con Práctica libre y una solo con Guided;
      que `mtCharts.valueLabels` no cambie las demás gráficas (Resumen, Pasos); casos con
      entrenamiento cruzado, Sin área, área sin guía publicada, Sin iniciar, sin sesiones y un
      Assessment abandonado o en curso; claro, oscuro y 768 px; contraste de "Aprobó" y "Sin
      aprobar".
- [ ] **Ficha impresa:** la disposición aprobada en el `@media print` de `admin.css`; sigue en
      claro, sin menú, botones ni curva (D-157), con las tres preguntas, todos los pasos y la
      tabla única de Sesiones (D-303).
- [ ] Métricas derivadas aprobadas (si hay): regla en la BLL con su test. *Enmendado
      2026-10-30: `OperatorActivity` va sin test, como D-235/D-283 (D-304; confirmado por el usuario a
      Planeación el mismo día).*
- [ ] `/code-review low --max-findings 5`; `medium` si hay métricas. Foco: insignias de una sola
      fuente, mismos estados de `ReadinessPolicy`, ficha en claro.
- [ ] Checklist común + filtros por estado y guía, paginación, la ficha impresa (vista previa
      de impresión con tema oscuro activo) y la curva en los dos temas.

> **Operadores (lista) cerrada 2026-10-30 (Desarrollo, `1afe5ed`):** alternativa D con el
> encabezado K1 (barra proporcional) desde `cloud/022-operadores` (`94c0405`); el resumen por
> estado, a la BLL (`OperatorSummary`, sin test, como D-235/D-283). D-295 a D-298 (Diseño
> UI/UX, 2026-10-30). Los textos de qué hacer (D-298) los valida negocio antes de cerrar la E6. El `/code-review low` formal devolvió vacío; revisión manual de la BLL y el
> code-behind, sin hallazgos. Ajustes de Desarrollo: título, campos y "Ver detalle"
> (`.operators-go`, que la nube dejó sin el color de acento del canvas). El generador
> `GenerateDashboardData.sql` pasa a 100 operadores con perfiles fijos. Sin capturas (D-242).
> **Falta:** `Operator.aspx` (detalle), la ficha impresa y su `/code-review`; esperan su mockup.

**Hecho cuando:** el usuario confirma la lista, el detalle y la ficha impresa. *(La lista ✅.)*

## E7 · Usuarios (después de la E4 de la 021)

> **Enmendada 2026-10-27** (enmienda 1 de la spec 021, D-208): sin "Quitar área"; el área se
> cambia con un lápiz en su columna.

**Bloqueada hasta:** E1 y el mockup de Usuarios. (Enmendado 2026-10-27: la E4 de la 021 ya
está cerrada e integrada; la columna Área con su lápiz y B-3 ya existen.)

**Esfuerzo (D-250):** `medium`; `high` si toca la BLL de gestión de usuarios o SQL.

- [ ] `Pages/Admin/Users.aspx`: la estructura aprobada; mismos `BtnCreate`, `DdlNewRole`,
      `TxtNewCode1`, `TxtSearch`/`BtnSearch`, `RptUsers`, `BtnPrev`/`BtnNext`; en la columna
      Área, el lápiz que abre ahí el selector agrupado con Guardar (`adminConfirm`) y Cancelar,
      sin postback hasta Guardar; las filas de admin muestran "—" y no tienen lápiz; en
      Acciones, solo Resetear, Desbloquear y Borrar, con `adminConfirm`.
- [ ] `/code-review low --max-findings 5`. Foco: ids de fila, que las acciones de operador no
      salgan en filas de admin, B-3 intacto.
- [ ] Checklist común + crear, buscar, paginar, cada acción por fila, cambiar el área con el
      lápiz (Guardar y Cancelar), y la fila propia.

**Hecho cuando:** el usuario prueba cada acción y dice "funciona".

## E8 · Inicio del operador (después de la E3 de la 021)

**Bloqueada hasta:** E1 y el mockup del Inicio. (Enmendado 2026-10-27: la E3 de la 021 ya
está cerrada; el área como texto y solo el selector de guía ya existen.)

**Esfuerzo (D-250):** `medium`; `high` si toca `StartSession`, la BLL de Training o SQL.

- [ ] `Pages/Training/StartSession.aspx` (y `Site.Master` si la estructura lo pide): la
      disposición aprobada; mismos `DdlGuide`, `RbGuided`/`RbFreePractice`/`RbAssessment`,
      `BtnStart` y `LblError`; el aviso sin área (D-168) y el de "no hay guías".
- [ ] "Elige tu área" (`ChooseArea.aspx`) hereda el lenguaje si cambia `Site.Master`; se revisa
      que siga bien (fuera de la spec, solo regresión).
- [ ] El POD (`Training.Master`) no cambia (D-154).
- [ ] `/code-review low --max-findings 3`. Foco: el orden de redirecciones del `Page_Load`
      intacto, ninguna lógica en la página.
- [ ] Checklist común + iniciar una sesión en cada modo en tablet, sin área y sin guías.

**Hecho cuando:** el usuario inicia una sesión en los tres modos y dice "funciona".

## E9 · Cierre

- [ ] AC-7: rendimiento de las vistas con la BD de volumen (menos de 3 s,
      `DashboardPerformanceTests`), si hubo métricas derivadas.
- [ ] Docs (Desarrollo): `docs/architecture.md` → "Sistema de diseño y tema" (tokens y
      componentes nuevos, la referencia a `022-design/`), `docs/decisions.md` (D-n de los
      mockups como vigentes), `docs/status.md` y `docs/history.md`. Avisar a Arquitectura y
      Planeación para el roadmap.

**Hecho cuando:** todas las entregas tienen su "funciona" y la spec pasa a Implemented.
