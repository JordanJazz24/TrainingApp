# Usuarios · alternativa CD-3 "Consola de cuentas" (aprobada 2026-10-30)

**Spec:** `../022-mockups-por-pantalla.md` · **Brief:** 8 de `../022-briefs.md` · **Plan:** `../022-plan.md` → E7 · **Punto de partida:** `TrainingApp` (sin rama de handoff; el paquete es `design_handoff_usuarios_cd3/`, con el prototipo `Usuarios CD.dc.html` como referencia).

Lista de cambios frente al v1 (el código actual de `Users.aspx`), tal como se aprobó en el prototipo "Usuarios · brief 8" (sesión de diseño, D-284). Una sola ronda:

1. La actual recreada y tres alternativas: **CD-1 Mesa de accesos** (la tabla con cifras arriba y Crear en el encabezado; cerca del v1), **CD-2 Bandeja de cuentas** (tarjetas por cuenta, "Requieren acción" primero y Crear en una columna lateral; intermedia) y **CD-3 Consola de cuentas** (directorio + ficha; atrevida, D-237).
2. Crítica (D-284), orden: 1.º CD-2, 2.º CD-3, 3.º CD-1. Se corrigió lo grave una vez.
3. El usuario eligió **CD-3**, sin ajustes, y confirmó tres puntos: Sin área sin preselección, Borrar a uno mismo deshabilitado y cifras solo de la página abierta. No hubo más rondas.

## Alternativa elegida

**CD-3 · Consola de cuentas.** Una tarjeta (`--mt-radius-xl`, sombra) con dos columnas (5fr / 7fr):

- **Directorio (izquierda):** eyebrow "Administración · Cuentas", título "Usuarios", botón "Crear usuario", Buscar y el resumen "En esta página: N bloqueadas · N deben cambiar la contraseña · N sin área". Una fila por cuenta (avatar con iniciales, nombre, "Code1 · Producto · Área", insignia de problema o "Activa") y el paginador al pie.
- **Ficha (derecha), de la cuenta elegida:**
  1. Encabezado en el azul de marca (`--mt-brand-bg` con su retícula): avatar, nombre, "Code1 n", insignias de rol y estado.
  2. **Acceso:** qué pasa y las acciones Desbloquear (si está bloqueada) y Resetear contraseña, cada una con su consecuencia escrita.
  3. **Área:** el área actual y "Cambiar área"; los administradores muestran "—".
  4. **Cuenta:** Alta, Último acceso (con "hace N días") y Sesiones de entrenamiento.
  5. **Zona "Borrar usuario"**, aparte, con lo que se pierde.
- Al cargar, la ficha abierta es la de la **primera cuenta con problema** de la página (si no hay, la primera fila).
- **Crear usuario** es un panel que sustituye a la ficha (mismo encabezado de marca).
- **Tablet (< 992 px):** una columna; la ficha abre como panel fijo a la derecha con "Volver a la lista" y un fondo `--mt-backdrop`.

### Resumen de la crítica (D-284)

- **Fortalezas:** protege mejor que las otras lo destructivo (Borrar lejos, con su consecuencia y su conteo de sesiones, y deshabilitado sobre uno mismo con el motivo); cambia de verdad la jerarquía (decide sobre UNA cuenta con todo a la vista); la ficha cabe en tablet.
- **Riesgos:**
  - cada acción rutinaria cuesta un clic más (elegir y luego actuar);
  - la lista oculta el área, el rol de los operadores y las fechas;
  - es la más cara: duplica el HTML de cada cuenta y suma selección en el cliente;
  - con la ficha de marca bajo la barra de marca hay dos azules cerca (precedente D-289).
- **Qué se corrigió:** en tablet la ficha no tenía salida clara (ahora "Volver a la lista"); Borrar compartía bloque con Resetear (ahora tiene su zona).

## Cambios frente al v1

### Tokens

**Ninguno nuevo ni con otro valor**, en claro ni en oscuro; no afecta a ninguna pantalla ya hecha. Verificados contra `admin.css` (claro / oscuro):

- Superficies y texto: `--mt-surface-2` `#EDF1F6` / `#1F2833`, `--mt-text` `#18212D` / `#E5EAF1`, `--mt-muted` `#4E5A6A` / `#A2ADBB`, `--mt-field-border` `#808B9B` / `#627083`.
- Acento y foco: `--mt-accent` `#1F55A5` / `#7EA7EB`, `--mt-accent-soft` `#E3ECF8` / `#1E2F49`, `--mt-focus` `#1F55A5` / `#9DBCF0`.
- Estados: `--mt-ok/-soft`, `--mt-bad/-soft` (`#B93229` `#FBE5E3` / `#F2786E` `#3A1E1C`), `--mt-warn/-soft` (`#8A5A00` `#FDF0D5` / `#F3C062` `#3A2E14`), `--mt-warn-bar`, `--mt-idle/-soft`.
- Marca: `--mt-brand-bg` `#16345F` / `#13233A`, `--mt-on-brand`, `--mt-on-brand-2` `#C3D2EA` / `#A9B8CF`, `--mt-brand-line`.
- Otros: `--mt-shadow`, `--mt-backdrop`, `--mt-radius-lg` 10 px, `--mt-radius-xl` 14 px, `--mt-control-h` 44 px.

### Componentes (`admin.css`, bloque nuevo "Usuarios: consola de cuentas")

- **Nuevos, solo de esta pantalla:**
  - contenedor: `.users-shell` (grid 5fr / 7fr; una columna bajo 992 px);
  - directorio: `.users-list`, `-list-head`, `-summary`, `-row` (con `is-selected`), `-avatar` (con `is-admin`), `-row-name`, `-row-sub`, `-row-ok`, `.users-tag-you`;
  - ficha: `.users-pane` (panel fijo bajo 992 px), `-pane-head` (el azul de marca), `-pane-avatar`, `-pane-name`, `-pane-sub`, `-back`, `-sec`, `-sec-k`, `-act` (renglón de acción), `-facts` (lista de datos), `-area-groups`, `-area-title`, `-area-opts`, `-area-opt` (con `is-selected`), `-danger`;
  - tablet: `.users-scrim` (usa `--mt-backdrop`).
- **Modificados:** ninguno.
- **Se borran** (revisar con `grep` que ninguna otra pantalla los use): `.admin-users`, `.admin-area-cell`, `.admin-area-edit` (y `.admin-area-edit .form-select`), `.admin-icon-btn`, `.admin-row-actions`. `.mt-col-secondary` solo si queda sin uso. Se conservan `.mt-badge*`, `.mt-field*`, `.admin-validation`, `.admin-search` y `.mt-pager`.
- `.admin-page-head` ya no se usa aquí (como en Operadores).

### Estructura y controles (`Users.aspx`)

**No cambian** (mismo id y mismo handler):

- `PnlCreate` (`DefaultButton="BtnCreate"`), `TxtNewCode1` con `RfvNewCode1` (`ValidationGroup="create"`), `DdlNewRole` (`DropDownList`, ver "Desvíos"), `BtnCreate` y su `BtnCreate_Click`;
- `PnlSearch` (`DefaultButton="BtnSearch"`), `TxtSearch`, `BtnSearch` y su `BtnSearch_Click`;
- `RptUsers` con `OnItemCommand="RptUsers_ItemCommand"` y los comandos `Reset`, `Unlock`, `Delete` y `SetArea`, con el mismo `CommandArgument`, `CausesValidation="false"`, `data-confirm`, `data-confirm-icon` y `OnClientClick="return adminConfirm(this);"` (y `usersAreaConfirm` en Guardar);
- `LblEmpty`, `BtnPrev`, `LitPageInfo`, `BtnNext`, `RptAreaGroups` en su `<template id="usersAreaOptions">`, y las funciones `adminConfirm`, `usersAreaConfirm` y el `replaceState`.

**Se borran:**

- la tabla `.admin-users` entera (`<thead>` y `<tr>` / `<td>`);
- el título "Gestión de usuarios" (`.admin-page-head`) y el panel "Crear usuario" con su `<section>`;
- el `<select name="area_{id}">` por fila y el botón de lápiz de la columna.

**Nuevos:**

- `RptDirectory` (`Repeater` de solo presentación: una fila por cuenta, sin comandos, con el `Id` en `data-id`); `RptUsers` pasa a pintar **las fichas** (una por cuenta, con sus `LinkButton` de siempre), así que `OnItemCommand` no cambia;
- `HfSelected` (`HiddenField`: la cuenta elegida, para que sobreviva a un postback);
- resumen del directorio: `LitCountLocked`, `LitCountPending` y `LitCountNoArea`;
- botón "Crear usuario" (`<button type="button">`, sin postback) que muestra `PnlCreate` en lugar de la ficha; Cancelar (también cliente);
- en cada ficha: los datos de la cuenta (`LitLastAgo`, `LitSessions`, `LitDeleteNote`) y las áreas como radios `name="area_{Id}"` (se copian a la ficha desde `usersAreaOptions` al abrir, como hoy; `SetArea` sigue leyendo `Request.Form["area_" + userId]`).

`UserListRow` suma: `Initials`, `StateKey`, `StateShort`, `IsProblem`, `NoArea`, `AreaShort`, `Hint`, `LongHint`, `LastAgoText`, `SessionsText` y `DeleteNote`. Todo es presentación: lo que se mostraba (`RoleText`, `StateText`, `AreaText`, `CreatedText`, `LastLoginText`, `Confirm*`, `Who`, `IsSelf`, `HasHistory`) no cambia. Hay que actualizar `Users.aspx.designer.cs`.

### Comportamiento

- **Postbacks:** los mismos que hoy (Buscar, Anterior / Siguiente, Crear, y por cuenta Resetear, Desbloquear, Borrar y Guardar área). Elegir una cuenta, abrir Crear, abrir y cancelar el área y abrir la ficha en tablet son **cliente, sin postback**.
- **Selección:** al cargar y tras cada postback, la ficha abierta es `HfSelected`; si esa cuenta ya no está en la página (se borró, cambió la búsqueda o la página), la primera con problema, y si no hay, la primera. La clasificación "con problema" es el `StateOf` de la BLL, ya expuesto como `IsProblem`.
- **Confirmaciones (SweetAlert, `adminConfirm`):** las de hoy, con el mismo texto del servidor (`ConfirmReset`, `ConfirmUnlock`, `ConfirmDelete` con el `data-confirm-icon` `warning` si hay historial y `usersAreaConfirm` para el área). Éxitos y avisos: los de `ShowResult` y `ShowMessage`, sin cambios.
- **Área:** "Cambiar área" abre en la ficha los radios agrupados por producto con Guardar y Cancelar, sin postback hasta Guardar. Cliente: en "Sin área" no hay área preseleccionada y **Guardar queda deshabilitado hasta elegir un área distinta de la actual** (el servidor sigue validando: `SetArea` de la BLL). Los administradores no tienen "Cambiar área".
- **Borrar sobre uno mismo:** el botón sale deshabilitado con el motivo; el `LinkButton` real sigue con `Visible="false"` si `IsSelf` (la BLL devuelve `OwnAccount` por si acaso). B-3 y el `OnError` intactos.
- **Crear:** igual que hoy; tras Crear se hace `BindUsers()` como siempre (ver "Desvíos": el prototipo salta a la página del nuevo usuario, el código no lo hace).
- **Tablet:** la ficha abierta es un panel fijo con foco al abrir, Esc y "Volver a la lista" para cerrar, y el fondo `--mt-backdrop`. Sin scroll horizontal de la página.

### Textos nuevos

- Directorio: "En esta página:", "N bloqueadas", "N deben cambiar la contraseña", "N sin área", "Activa" y "tú" en la propia cuenta.
- Acceso:
  - Bloqueada: "Cinco intentos fallidos seguidos. Se desbloquea sola a las HH:mm (en N min); puedes desbloquearla ya." El "cinco" sale de `LoginLockPolicy.MaxFailedLogins` (5), no se escribe fijo.
  - Debe cambiar: "Todavía no entra. Su contraseña es la que viene por defecto y debe cambiarla antes de usar la app." (si no tiene último acceso) o "Su contraseña fue restablecida: entra con la contraseña por defecto y debe cambiarla."
  - Acciones: "Desbloquear la cuenta" / "Puede entrar de nuevo sin esperar a que venza el bloqueo." y "Resetear la contraseña" / "Entrará con la contraseña por defecto y deberá cambiarla. También desbloquea." (el reset ya desbloquea: `UserAdminService.ResetPassword`).
- Área: "— Los administradores no tienen área.", "Aún no eligió su área: no cuenta como «Sin iniciar» en Operadores.", "Producto · Área. Se puede corregir aquí." y "Cambiar área".
- Cuenta: "Sesiones de entrenamiento" con "Ninguna", "N sesión / sesiones" o "No entrena (administrador)"; "hoy / ayer / hace N días / Nunca ha entrado".
- Borrar: la nota con lo que se pierde (la del `ConfirmDelete`, sin cambios de fondo), "Borrar a {nombre}" y, sobre uno mismo, "Es tu cuenta: nadie se borra a sí mismo. Pídele a otro administrador que lo haga."
- Crear: "Crear usuario", "Un empleado de la lista de la planta, con su rol", "Volver a la lista", y la nota "Entra con la contraseña por defecto y la cambia en su primer acceso. El área la elige después el propio operador, o se corrige aquí desde su ficha."
- Los textos de "qué pasa" y de cada acción son guía nueva para el admin: **los valida negocio** antes de cerrar la E7.

### Métricas derivadas (D-205), en la BLL y sin test (como D-235, D-283, D-293, D-297 y D-304)

- `BLL/Admin/UserPageSummary.cs` (nuevo, en el `.csproj`), en `UserSearchPage.Summary`: cuenta, **solo sobre `page.Users`** (las 20 de la página), las cuentas **Bloqueadas** y **PendingFirstLogin** (con `UserAdminService.StateOf` y el mismo `nowUtc`) y los **operadores sin área** (`Role == Operator` y `AreaId == null`). Es agrupar lo que `Search` ya carga: no cambia `Search`, el orden, el total ni la paginación, ni hay SP nuevo.
- **No cuenta toda la lista:** un bloqueado en la página 3 no suma en la 1. Contar en todas las páginas pediría una consulta agregada (SP nuevo), que D-205 deja fuera ("candidata para otra spec", sin numerar).
- "Se desbloquea sola en N min", "hace N días" y "N sesiones" son presentación: salen de `LockedUntil`, `LastLoginAt` y `SessionCount`, que ya llegan. Para "hace N días" se reutiliza `ReportText.DaysAgo` (D-298).

## Desvíos del brief 8 o del plan E7

Se anotan sin corregirlos; **los decide el usuario** antes de implementar.

1. **`DdlNewRole` y el control segmentado (plan E7).** El prototipo muestra Rol como control segmentado (Operador / Administrador, valores 1 y 2). E7 conserva `DdlNewRole`. Un `RadioButtonList` cambia el id (precedente: `RblMode`, D-282, aprobado). **Por defecto se implementa `DdlNewRole` tal cual** (la lista de hoy); el segmentado solo si el usuario lo aprueba y se numera.
2. **El lápiz no está "en la columna Área" (brief 8, D-208).** CD-3 no tiene columna Área: el área se cambia con un botón "Cambiar área" (con ícono de lápiz) en la sección Área de la ficha, y el selector son **radios agrupados por producto**, no el `<select>` con `<optgroup>`. Mismo contrato (Guardar con confirmación, Cancelar, sin postback hasta Guardar, `area_{id}` en `Request.Form`, admin sin lápiz y con "—").
3. **Datos que dejan de verse en la lista y pasan a un clic (P-5).** El directorio no muestra Rol (solo "Administrador" bajo el nombre; el Operador se infiere), Alta ni Último acceso: están en la ficha. P-5 pide marcarlo y que cada uno sea una D-n. Ningún dato se quita: Código, Nombre, Rol, Estado, Alta, Último acceso y Área siguen disponibles.
4. **Borrar sobre uno mismo (brief: "no sobre uno mismo").** Hoy el botón no se dibuja (`Visible="false"`); el prototipo lo muestra **deshabilitado con el motivo**. Es presentación; si no se quiere, la zona dice solo la nota.
5. **Dos repeaters en lugar de uno (plan E7: `RptUsers`).** `RptUsers` se conserva con su id y su `OnItemCommand`, pero ahora pinta las fichas; el directorio es un repeater nuevo (`RptDirectory`). Duplica el HTML de cada cuenta (20 fichas por página).
6. **Crear: el prototipo salta a la página del nuevo usuario y limpia la búsqueda.** El código **no hace eso** (hace `BindUsers()` en la misma página). Es del prototipo; **no se implementa**: el comportamiento de Crear no cambia.
7. **Sin área, sin preselección (cliente).** Hoy se preselecciona la primera área. Es un cambio de comportamiento de cliente aprobado por el usuario (2026-10-30); la BLL sigue validando.
8. **Dos botones pasan a `LinkButton` con ícono** (`BtnSearch` y `BtnCreate`), como `BtnLogin` en D-224. Mismos ids, `OnClick`, `ValidationGroup` y `DefaultButton`. Si no se quiere, quedan como `asp:Button` sin ícono.
9. **Métrica derivada nueva** (`UserPageSummary`), sin test, con el mismo trato que D-235 y D-297: necesita su D-n, que numera la sesión Diseño UI/UX.
10. **Lo que el brief deja fuera sigue fuera.** No hay edición de nombre ni de Code1, ni cambio de rol, ni filtro por área, ni área en el alta, ni historial de cambios. "Sesiones de entrenamiento" en la ficha usa `SessionCount`, que ya existe (hoy solo aparece dentro de la confirmación de Borrar).

## Para revisar en Desarrollo (no compilado)

- Rebuild y `aspnet_compiler`: se toca `.aspx` y el `.designer.cs` se edita a mano (`RptDirectory`, `HfSelected`, `LitCount*`, y los literales de cada ficha; `<%# %>` en atributos de controles de servidor y `<%#: %>` en HTML literal, como `CLAUDE.md`).
- Los tests de `UserAdminService` siguen en verde (no cambia nada de la BLL salvo el resumen).
- Casos a probar:
  - **Operador y administrador:** la ficha del administrador sin "Cambiar área", con "—" y sin sesiones; el operador con y sin área.
  - **Bloqueada:** el texto con la hora y los minutos; Desbloquear y Resetear (que también desbloquea).
  - **Debe cambiar la contraseña:** con y sin último acceso.
  - **Sin área:** "Sin área" en el directorio y la ficha; Guardar deshabilitado hasta elegir; guardar y cancelar.
  - **Fila propia:** "tú", Borrar deshabilitado con el motivo y ningún postback posible; B-3 intacto.
  - **Borrar** sin historial y con historial (alerta `warning` con lo que se pierde); si el usuario ya no existe ("El usuario ya no existe.").
  - **Crear:** código vacío (validador), no está en la lista, ya tiene cuenta, servicio caído, éxito; `DdlNewRole` en los dos roles.
  - **Búsqueda sin resultados:** `LblEmpty` y ninguna ficha (sin error de selección); búsqueda con resultados.
  - **Paginación:** Anterior y Siguiente (la selección vuelve a la primera con problema); página 3 con un bloqueado que no suma en las cifras de la 1.
  - Después de cada acción F5 no la repite (el `replaceState` sigue).
- **Claro, oscuro y 768 px:** sin scroll horizontal de la página; en tablet la ficha abre como panel, con foco, Esc y "Volver a la lista"; el directorio no se desborda con nombres largos.
- **Contraste AA (y 3:1 en campos):**
  - `--mt-accent` sobre `--mt-accent-soft` (fila elegida y "tú"), `--mt-warn` sobre `--mt-warn-soft`, `--mt-bad` sobre `--mt-bad-soft` (insignia, texto de "qué pasa" y zona Borrar), `--mt-muted` sobre `--mt-surface` y sobre `--mt-surface-2`;
  - `--mt-on-brand-2` sobre `--mt-brand-bg` en el encabezado de la ficha y el botón "Volver a la lista" sobre la marca (el foco usa `--mt-on-brand` ahí, como en la barra);
  - el borde de los radios de área (`--mt-field-border`) y el foco de 3 px.
  - Ningún estado solo por color: cada uno lleva ícono y texto.
- La ficha de marca bajo la barra de marca (dos azules cerca): confirmar que se lee bien (precedente D-289).
- Que los textos de "qué pasa" y de las acciones los valide negocio.
