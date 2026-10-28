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
