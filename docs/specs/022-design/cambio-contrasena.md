# Cambio de contraseña · alternativa 2a (aprobada 2026-10-28)

**Spec:** `../022-mockups-por-pantalla.md` → "Pantallas aprobadas" (D-228 a D-232) · **Rama de partida:** `cloud/022-cambio-contrasena` (desde `cloud/022-login-3b`,
"no verificada") · **Brief:** 2 de `../022-briefs.md`.

Lista de cambios frente al v1, tal como la entregó Claude Design ("Comparación de diseños login
alternativos", `Cambio de contrasena.dc.html`, ronda 2, opción 2a).

## Tokens
Ninguno nuevo ni con otro valor. Usa los del Login (D-221).

## Reutiliza del Login (D-221 a D-224)
- `FullBleed` de `Admin.Master` y `#mtThemeToggle` en la página.
- Tokens `--mt-brand-*` y la retícula de 32 px.
- Campos con ícono (`.input-group-text`): `bi-key`, `bi-shield-lock`, `bi-shield-check`.
- `.mt-status-line` para los errores del servidor (ya no SweetAlert).
- Botones como `asp:LinkButton` con ícono.

## Componentes nuevos
- **`.pw-page`, `.pw-band`, `.pw-band-top`, `.pw-logo`, `.pw-logo-mark`:** franja de marca a
  pantalla completa con logo e interruptor.
- **`.mt-steps` / `.mt-step`** (`is-done`, `is-current`, `is-todo`): 3 pasos con línea de progreso,
  solo en primer acceso o tras un reset. Reutilizable.
- **`.pw-card`** (sobre `.admin-card`): 480 px, montada 48 px sobre la franja.
- **`.pw-pair`:** Nueva y Confirmar lado a lado; se apilan en menos de 576 px.
- **`.pw-rules`:** lista fija de los 3 requisitos que ya valida la app.
- **`.pw-cancel`:** Cancelar como `btn-link` centrado y subrayado.

## Estructura y comportamiento
- **Pantalla completa siempre:** ningún rol ve el menú, su Code1 ni Cerrar sesión hasta guardar.
  Hoy un admin obligado veía el menú; el acceso real ya lo bloqueaban las redirecciones a
  `ChangePasswordUrl`.
- **Pasos** en un `PlaceHolder` nuevo (`PhSteps`), con la misma condición que `PhFirstLogin`.
- **Errores del servidor** en la línea de estado sobre Guardar; validadores sin cambio.
- **Botones:** `BtnSave` y `BtnCancel` pasan de `asp:Button` a `asp:LinkButton` (ícono). Cancelar pasa
  de botón secundario a botón de texto bajo Guardar. Mismos `OnClick`; `CausesValidation="false"`
  en Cancelar; Enter = Guardar.

## Textos nuevos
- Pasos: "Entraste", "Contraseña personal", "Tu inicio".
- Requisitos: "Al menos 8 caracteres", "Distinta de la contraseña por defecto", "Igual en Nueva y
  Confirmar".
- Botones: "Guardar y continuar" (antes "Guardar") y "Cancelar y cerrar sesión" (antes "Cancelar").

## Pendiente de confirmar
- ~~Llegada sin obligación (por URL)~~: resuelto por D-230 (pantalla completa siempre).

## Revisar en Desarrollo (no compilado)
- `LinkButton` con `DefaultButton` (Enter) y con los validadores del cliente.
- Que `PhSteps.Visible` se conserve en los postbacks con error (ViewState, igual que `PhFirstLogin`).
- Que no quede ningún `ShowMessage` en la página.
- `ChangePassword.aspx.designer.cs` editado a mano (no venía en el handoff): `PhSteps`, `PhError`,
  `LitError` nuevos y `BtnSave`/`BtnCancel` como `LinkButton`. Regenerarlo si Visual Studio lo pide.

## Capturas
`cambio-contrasena-claro.png`, `cambio-contrasena-oscuro.png`, `cambio-contrasena-tablet.png`, de
la sesión del handoff (primer acceso). **No se reemplazaron por las de la versión final**
(decisión del usuario, 2026-10-28, al cerrar la E2b): la versión implementada es la misma
estructura.
