# Plan 022: un mockup por pantalla

**Spec:** `022-mockups-por-pantalla.md` (aprobada 2026-10-27) · **Decisiones:** D-202 a D-207 ·
**Briefs:** `022-briefs.md` · **Planeado en:** la nube (`cloud/022-mockups-por-pantalla`) ·
**Implementa:** Desarrollo, en local.

Cada entrega sigue `docs/process.md` → "2. Cada entrega": implementar, verificar con los
comandos de `CLAUDE.md` → "Correr local, compilar y probar", `/code-review` con nivel y foco, el
"funciona" del usuario en el navegador **antes** del commit y commit solo con sus rutas
(`git commit -- <rutas>`), con `docs/status.md` y la casilla de este plan en el mismo commit.

## Reglas de este plan

- **Bloqueo por mockup (D-202, P-7):** una entrega no empieza hasta que su exportación está
  aprobada, guardada en `022-design/<pantalla>.html` y registrada en la spec con fecha y sus
  D-n (spec → "Aprobación"). Sin eso, Desarrollo no la toma.
- **Bloqueo por la 021 (P-7):** las entregas marcadas "después de la E*n* de la 021" esperan esa
  entrega cerrada. Las libres se intercalan con la 021 **cuando el usuario se lo pida** a
  Desarrollo; nunca a la mitad de una entrega de la 021.
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

## Checklist común de cada entrega de pantalla

Se repite en E2 a E8; cada entrega agrega lo suyo.

- [ ] Precondición: exportación aprobada en `022-design/` y registrada en la spec; la entrega
      de la 021 que la bloquea, cerrada.
- [ ] Capturas de **antes** (claro y oscuro) guardadas por el usuario, para comparar.
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
- [ ] Capturas de **después** en el registro de la entrega.

## E1 · Sistema de diseño (P-8)

**Bloqueada hasta:** el mockup del **Login** aprobado (fija los cambios al sistema, P-1). Libre
respecto a la 021, pero se coordina con Desarrollo: la 021 también toca `admin.css` (E3 a E5).

- [ ] `UI/master/admin.css`: los tokens y componentes nuevos o cambiados que aprobó el Login,
      cada uno con su D-n en un comentario, en claro y en oscuro. Sin tocar el POD
      (`Training.Master` no carga `admin.css`).
- [ ] `admin-charts.js`, solo si los tokens de gráficas cambian.
- [ ] Regresión visual del **Resumen** (AC-6) y de las pantallas aún no rediseñadas: siguen
      bien con los tokens nuevos, en claro y oscuro.
- [ ] Verificación (Desarrollo): Rebuild; `aspnet_compiler` si se tocó un master.
- [ ] Sin `/code-review` si solo es CSS (`process.md` → 3).

**Hecho cuando:** el usuario confirma el Resumen y una pantalla del dashboard sin cambios
rotos, en claro y oscuro.

> Los cambios al sistema que aprueben mockups posteriores van **al inicio de la entrega de esa
> pantalla**, con la misma regresión del Resumen.

## E2 · Login y Cambio de contraseña (libre)

**Bloqueada hasta:** E1 y los mockups de Login y Cambio de contraseña.

- [ ] `Pages/Auth/Login.aspx`: la estructura aprobada; mismos `TxtCode1`, `TxtPassword`,
      `BtnLogin` y `DefaultButton`; errores por SweetAlert como hoy.
- [ ] `Pages/Auth/ChangePassword.aspx`: su estructura aprobada (propia, P-6 descartada); mismos
      campos, `BtnSave` y `BtnCancel`. La redirección final no cambia aquí: la pone la E3 de la
      021.
- [ ] Si la identidad necesita piezas fuera de las páginas (un panel en `Admin.Master` solo para
      Auth), se hacen sin afectar las páginas del dashboard.
- [ ] `/code-review low --max-findings 3` sobre el markup y los code-behind tocados. Foco: que
      no cambie el flujo de login, primer acceso, bloqueo y anti-CSRF.
- [ ] Checklist común + login correcto, contraseña incorrecta, cuenta bloqueada, primer acceso
      → cambio de contraseña, cambio voluntario y Cancelar, con los dos roles.

**Hecho cuando:** el usuario entra con los dos roles y dice "funciona".

## E3 · Guías (libre)

**Bloqueada hasta:** E1 y el mockup de Guías.

- [ ] `Pages/Admin/Guides.aspx`: la estructura aprobada; mismos `RptGuides`, `DdlType` y
      `BtnSave` por fila, con su confirmación. Estado vacío.
- [ ] Sin `/code-review` si solo es markup; si se toca el code-behind, `low --max-findings 3`
      con foco en `CanManageGuides` y en los ids de fila.
- [ ] Checklist común + cambiar una guía a Custom y volver a Estándar; la sesión en curso no
      cambia (spec 020).

**Hecho cuando:** el usuario cambia el tipo de una guía y dice "funciona".

## E4 · Pasos (libre)

**Bloqueada hasta:** E1 y el mockup de Pasos.

- [ ] `Pages/Admin/Steps.aspx`: la estructura aprobada; mismos filtros (período, guía, modo) y
      Descargar; "Ver todos" sigue en el cliente; los saltos solo en Práctica libre.
- [ ] Métricas derivadas aprobadas (si hay): regla en la BLL con su test.
- [ ] `/code-review low --max-findings 3`; `medium` si hay métricas. Foco: mismos números que
      `StepStatistics`, ninguna regla en la página.
- [ ] Checklist común + Assessment y Práctica libre, "Ver todos", Descargar.

**Hecho cuando:** el usuario confirma el ranking de una guía en los dos modos.

## E5 · Seguimiento (libre)

**Bloqueada hasta:** E1 y el mockup de Seguimiento.

- [ ] `Pages/Admin/FollowUp.aspx`: la estructura aprobada; chips con texto (S-1 a S-3), orden
      por número de reglas, filtro por guía y Descargar.
- [ ] Métricas derivadas aprobadas (si hay): regla en la BLL con su test; las reglas de
      `FollowUpRules` no cambian.
- [ ] `/code-review low --max-findings 3`; `medium` si hay métricas.
- [ ] Checklist común + filas con 1, 2 y 3 reglas, vacío y enlace al detalle.

**Hecho cuando:** el usuario confirma la lista y dice "funciona".

## E6 · Operadores y Operador (después de la E5 de la 021)

**Bloqueada hasta:** E1, los mockups de Operadores y Operador, y la E5 de la 021 cerrada (la
columna Área ya existe).

- [ ] `Pages/Admin/Operators.aspx`: la estructura aprobada; columnas dinámicas por guía con la
      insignia (`ReportText.StateCss`/`StateIcon`), el Área, filtros, Descargar y paginación de
      50.
- [ ] `Pages/Admin/Operator.aspx`: la estructura aprobada; estado por guía, pasos a reforzar,
      curva (Chart.js con `admin-charts.js`), sesiones y el área en el encabezado.
- [ ] **Ficha impresa:** la disposición aprobada en el `@media print` de `admin.css`; sigue en
      claro, sin menú, botones ni curva (D-157).
- [ ] Métricas derivadas aprobadas (si hay): regla en la BLL con su test.
- [ ] `/code-review low --max-findings 5`; `medium` si hay métricas. Foco: insignias de una sola
      fuente, mismos estados de `ReadinessPolicy`, ficha en claro.
- [ ] Checklist común + filtros por estado y guía, paginación, la ficha impresa (vista previa
      de impresión con tema oscuro activo) y la curva en los dos temas.

**Hecho cuando:** el usuario confirma la lista, el detalle y la ficha impresa.

## E7 · Usuarios (después de la E4 de la 021)

> **Enmendada 2026-10-27** (enmienda 1 de la spec 021, D-208): sin "Quitar área"; el área se
> cambia con un lápiz en su columna.

**Bloqueada hasta:** E1, el mockup de Usuarios y la E4 de la 021 cerrada (columna Área con su
lápiz, B-3).

- [ ] `Pages/Admin/Users.aspx`: la estructura aprobada; mismos `BtnCreate`, `DdlNewRole`,
      `TxtNewCode1`, `TxtSearch`/`BtnSearch`, `RptUsers`, `BtnPrev`/`BtnNext`; en la columna
      Área, el lápiz que abre ahí el selector agrupado con Guardar (`adminConfirm`) y Cancelar,
      sin postback hasta Guardar; en Acciones, solo Resetear, Desbloquear y Borrar, con
      `adminConfirm`.
- [ ] `/code-review low --max-findings 5`. Foco: ids de fila, que las acciones de operador no
      salgan en filas de admin, B-3 intacto.
- [ ] Checklist común + crear, buscar, paginar, cada acción por fila, cambiar el área con el
      lápiz (Guardar y Cancelar), y la fila propia.

**Hecho cuando:** el usuario prueba cada acción y dice "funciona".

## E8 · Inicio del operador (después de la E3 de la 021)

**Bloqueada hasta:** E1, el mockup del Inicio y la E3 de la 021 cerrada (área como texto, solo
el selector de guía).

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
