# Barra superior · propuesta 1 (aprobada 2026-10-29)

**Spec:** `../022-mockups-por-pantalla.md` (D-238) · **Brief:** 0 de `../022-briefs.md` ·
**Rama de partida:** `cloud/022-barra-superior` (desde `TrainingApp`, "no verificada") ·
**Entrega:** E1b de `../022-plan.md`.

Lista de cambios frente al v1, tal como se aprobó en el canvas "Barra superior · 022"
(sesión de diseño en la nube, en lugar de Claude Design). Antes de esta se descartaron tres
rondas (A a G); el usuario eligió la **propuesta 1** de la cuarta.

## Alternativa elegida

**1 · Banda de marca con menú agrupado.** Rompe con el v1 en estructura y en lo visual (D-237):
una sola fila en el azul del Login 3b (`--mt-brand-bg` con su retícula de 32 px), un componente
para `Admin.Master` y `Site.Master`.

- **Marca:** baldosa con `bi-display` y "TrainingApp" para los dos roles (D-238).
- **Admin:** las 6 vistas en **dos grupos con nombre**, "Dashboard" (Resumen, Operadores, Pasos,
  Seguimiento) y "Administración" (Usuarios, Guías), como pastillas sobre el azul. La vista
  actual va **rellena en `--mt-on-brand` y en negrita** (el mismo lenguaje que el control Modo de
  Pasos); no solo color.
- **Operador:** sin menú; "Tu área · Producto · Área" junto a la marca.
- **Cuenta:** iniciales en círculo, nombre y, debajo, el último acceso (admin) o "Code1 n" (operador).
  Después, el tema y Cerrar sesión como botones sobre el azul.
- **Pasos:** como la barra ya es una banda azul de lado a lado, la **franja de Pasos pasa a una
  tarjeta** con margen, esquinas de 16 px y sombra, al ancho de `.steps-wrap` (riesgo de dos bandas
  azules seguidas; commit `5a2ad7a`).

## Cambios frente al v1

### Tokens (`admin.css`; ninguno existente cambia de valor)

| Token | Claro | Oscuro | Uso |
|---|---|---|---|
| `--mt-brand-raise` | `rgba(255,255,255,.10)` | `rgba(255,255,255,.07)` | pastillas, baldosa y botones sobre la marca |
| `--mt-brand-edge` | `rgba(255,255,255,.22)` | `rgba(255,255,255,.16)` | su borde, los divisores y el hover |

Valen para toda la app. El Login (`.auth-logo-mark`) sigue con sus `rgba` fijos; puede pasar a
estos tokens sin cambio visible (no se tocó).

### Componentes

- **Nuevo `.mt-topbar`** (reemplaza a `.admin-bar`, que se borra con `.admin-brand`,
  `.admin-nav-links`, `.admin-bar-end` y `.admin-user*`): `.mt-topbar-brand`, `-mark`,
  `-brand-text`, `-nav`, `-group`, `-gk` (nombre del grupo), `-seg` (pastillas), `-menu` y
  `-current` (botón de la barra plegada), `-area`, `-area-text`, `-area-k`, `-area-v`, `-end`,
  `-account`, `-avatar`, `-who`, `-name`, `-sub` y `-btn` (botón sobre el azul). Modificadores
  `is-admin`, `is-operator` e `is-open` (menú abierto).
- **`.steps-band`** (Pasos): ahora tarjeta (ancho `calc(100% - 64px)` hasta `1136px`, margen
  superior de 24 px, radio 16 px, `--mt-shadow`); en tablet, 16 px de margen a los lados.
- **Impresión:** `@media print` oculta `.mt-topbar` (antes `.admin-bar`).

### Estructura y comportamiento

- `Admin.Master`: mismo `AdminBar` (`<header runat="server">`), mismos `PhMenu`, `PhUser`,
  `LnkSummary` a `LnkGuides` (mismas URLs), `#mtThemeToggle`, `BtnLogout` y `MarkCurrent`.
  `FullBleed` sigue ocultando la barra; `WideMain` no cambia.
  - **Controles nuevos** (en el `.designer.cs`): `LitCurrent` (nombre de la vista actual en el
    botón Menú; lo llena `MarkCurrent`), `PhAdminGroup` (el grupo Administración se oculta si no
    hay Usuarios ni Guías), `UserAccount` (`<div runat="server">` para su `title`) y `LitInitials`.
  - `PhUser` ya no envuelve el tema: el botón de tema queda siempre visible (como antes) y la
    cuenta se ve delante con `order: -1`.
- `Site.Master`: la misma barra con `is-operator`. **Controles nuevos:** `PhArea`, `LitArea`,
  `UserAccount` y `LitInitials`. Método nuevo `ShowArea(string)`, que llama `StartSession` tras
  cargar el área (queda en el ViewState para los postbacks). "Elige tu área" no la llama (aún no
  hay área). El enlace de la marca sigue yendo a `StartSession`.
- `UI/master/TopBar.cs` (nuevo, en el `.csproj`): `Initials` y `AccountTitle`, solo presentación.
- `theme.js`: `wireMenu()` abre y cierra el menú plegado (clase `is-open` y `aria-expanded`;
  Escape lo cierra y devuelve el foco). Sin postback.
- **Responsive:**
  - menos de 1400 px: el último acceso (o el Code1) sale de la barra y queda en el `title` de la cuenta;
  - menos de 1200 px: el menú se pliega en "Menú · <vista>" y, abierto, baja los dos grupos como
    listas de 44 px en la banda;
  - 991 px o menos: el admin queda con las iniciales; tema y Cerrar sesión solo con ícono (el
    texto queda para lectores de pantalla y en el `title`);
  - 575 px o menos: la baldosa sin el texto, sin "Tu área" ni iniciales, y el botón Menú solo con
    la vista.

### Textos

- `Site.Master`: `<title>` "… - TrainingApp" (antes "… - Simulador de entrenamiento MES") y la marca
  "TrainingApp" (antes "Simulador de entrenamiento MES").
- Admin: muestra su **nombre** (antes su Code1) y el último acceso; si no hay nombre, el Code1.
- Operador: "Code1 104582" (antes solo el número) y "Tu área" con su área.
- Nuevos: "Dashboard", "Administración" (nombres de grupo), "Menú".

## Qué debe revisar Desarrollo (no se compiló ni se probó)

1. **Rebuild** y **`aspnet_compiler`** (cambian `Admin.Master` y `Site.Master`): los controles nuevos
   en los dos `.designer.cs`, `TopBar.cs` en el `.csproj`, y `(Master as SiteMaster)?.ShowArea(…)`
   en `StartSession.aspx.cs`.
2. `UserAccount.Attributes["title"]`: confirmar que sale codificado una sola vez (HtmlControl
   codifica el atributo; no se pasó por `HtmlEncode`).
3. `MarkCurrent` llena `LitCurrent` con `HtmlEncode(link.Text)`: revisar que "Guías" no salga
   codificado dos veces.
4. El menú plegado: abrir y cerrar con el mouse, el teclado (Tab, Enter, Escape) y en tablet; que
   no deje la página con scroll horizontal a 768 px (AC-4).
5. Contraste AA en claro y oscuro: texto `--mt-on-brand-2` sobre `--mt-brand-raise` (pastillas sin
   marcar) y el foco (`--mt-focus` pasa a `--mt-on-brand` dentro de la barra).
6. **Pasos** en claro, oscuro y 768 px con la franja en tarjeta: alineación con `.steps-wrap`, los
   filtros y el modo segmentado.
7. Regresión: Login y Cambio de contraseña (FullBleed, sin barra), Resumen, Operador (ficha
   impresa sin barra), Usuarios, Guías, Seguimiento, Inicio del operador (un postback con error
   conserva "Tu área") y "Elige tu área".
8. Capturas finales en claro y oscuro (D-240, D-285).

## Cierre en Desarrollo (2026-10-29, E1b)
- Rebuild, `aspnet_compiler` y 461 tests sin errores; sin cambios al código de la rama. Probada por el
  usuario con los dos roles. Sin capturas (decisión del usuario, D-240, como en las E2b, E3 y E4).
