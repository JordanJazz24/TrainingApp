# Login · alternativa 3b (aprobada 2026-10-28)

**Spec:** `../022-mockups-por-pantalla.md` → "Pantallas aprobadas" (D-220 a D-225) ·
**Rama de partida:** `cloud/022-login-3b` (`c7294ee`, "no verificada") · **Entregas:** E1 y E2
de `../022-plan.md`.

Lista de cambios frente al v1, tal como la entregó la sesión del handoff (Claude Design →
"Design: Comparación de diseños login alternativos").

## Tokens nuevos (`admin.css`; ningún token existente cambia de valor)

| Token | Claro | Oscuro | Uso |
|---|---|---|---|
| `--mt-brand-bg` | `#16345F` | `#13233A` | fondo del panel de identidad |
| `--mt-on-brand` | `#FFFFFF` | `#E5EAF1` | texto principal sobre el panel |
| `--mt-on-brand-2` | `#C3D2EA` | `#A9B8CF` | texto secundario sobre el panel |
| `--mt-brand-line` | `rgba(255,255,255,.07)` | `rgba(255,255,255,.05)` | retícula de 32 px |

## Componentes nuevos (solo el login, salvo `.mt-status-line`)

- **Panel dividido:** `.auth-split`, dos columnas `5fr` y `6fr`, alto de pantalla completa.
- **Panel de marca:** `.auth-brand` con la retícula y `.auth-watermark` (`bi-display` de 400 px
  al 6 % de blanco).
- **Logo:** `.auth-logo` y `.auth-logo-mark`.
- **Frase:** `.auth-pitch`, `.auth-eyebrow` y `.auth-headline` (34 px).
- **Pie del panel:** `.auth-brand-foot`.
- **Lado del formulario:** `.auth-side` (con su parte de arriba y su cuerpo) y `.auth-form`
  (380 px de ancho máximo).
- **`.mt-status-line`:** el error del servidor, con alto reservado de 28 px, en `--mt-bad`,
  14 px semibold. Es el único que podría reutilizarse en otras pantallas.
- **`.auth-note`:** el aviso de primer acceso.
- **Tablet (< 992 px):** el panel queda como una franja con el logo; se ocultan la frase, el pie
  y el ícono de fondo.

## Estructura y comportamiento

- **Pantalla completa:** sin la barra superior ni `.admin-main`. Modo nuevo de `Admin.Master`
  (`FullBleed`); las demás pantallas no cambian.
- **Interruptor de tema:** al lado del formulario, arriba a la derecha.
- **Campos** con íconos a la izquierda (`bi-person-badge`, `bi-key`).
- **Entrar** con `bi-box-arrow-in-right`: `asp:Button` pasa a `asp:LinkButton`.
- **Errores del servidor** en la línea de estado sobre Entrar, ya no en un SweetAlert.

## Textos nuevos

- Lead: "Con tu código de empleado y tu contraseña."
- Panel: "Simulador de entrenamiento · SAP MES", "Entrena y evalúa los flujos de SAP MES de
  manufactura médica." y "Un solo acceso para operadores y administradores".
- Aviso: "¿Primer acceso? Entra con la contraseña por defecto; enseguida te pedirá crear una
  nueva."

## Capturas (pendientes)

`login-claro.png`, `login-oscuro.png` y `login-tablet.png`, de la versión final. Las toma
Desarrollo en la E2a, antes de su commit (D-240). El HTML exportado no se guarda:
carga React desde unpkg (D-227).
