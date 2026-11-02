# Fin de sesión · "Fin de sesión v3", E con la tarjeta de resultado de A (aprobada 2026-11-02)

**Spec:** `../025-fin-de-sesion.md` (Enmienda 1, D-340 a D-345) · **Plan:** `../025-plan.md` → E3 (la
página) y E5 (el bloque del Inicio) · **Fuente:** el archivo `Fin de sesión v3.dc.html` del proyecto de
Claude Design "Fin de sesión del POD (spec 025)"
(<https://claude.ai/design/p/e5a1ad16-3f88-4db5-8175-3f4e96a95b77?file=Fin+de+sesi%C3%B3n+v3.dc.html>).
Las rondas 1 y 2 están en "Fin de sesión v1" y "v2" del mismo proyecto.

El handoff de Claude Design trae solo el prototipo (no un `.md`): Desarrollo escribió esta lista de
cambios **desde el v3, antes de implementar** (D-251), con D-340 a D-345 ya numeradas por
Planeación 025. Es el criterio de la E3 y la E5. Pantallas: `Pages/Training/SessionResult.aspx`
(nueva, `Site.Master`) y un bloque nuevo en `Pages/Training/StartSession.aspx`.

## Alternativa elegida

**E con la tarjeta de resultado de A** (ronda 3).

- **Fin de sesión:** título "Resultado de la sesión" y, debajo, "Producto · Área". Una **tarjeta de
  resultado** (`.res`): a la izquierda el veredicto (solo Assessment), la cifra y la barra contra el
  70 %; a la derecha Guía, Modo, Fecha y Duración. Una sola lista **"Pasos"** con el selector "A
  reforzar (N) / Todos (M)". Al final, **"Qué hacer después"** con la acción principal y las
  secundarias.
- **Inicio:** debajo del bloque de inicio ("Modo primero", sin cambios), un `<details>` plegado
  "Últimas sesiones" (como el "Ranking completo" de Pasos).

## Cambios frente a lo anterior

### Tokens

**Ninguno nuevo ni con otro valor**, en claro y oscuro. Usa los vigentes: `--mt-bg`, `--mt-surface`,
`--mt-surface-2`, `--mt-border`, `--mt-field-border`, `--mt-text`, `--mt-muted`, `--mt-accent`,
`--mt-accent-soft`, `--mt-on-accent`, `--mt-focus`, `--mt-ok/-soft`, `--mt-bad/-soft`,
`--mt-warn/-soft`, `--mt-idle/-soft`, `--mt-shadow`.

### Componentes (`admin.css`, bloque "Resultado de la sesión", D-345)

- **Página:** `.res` (tarjeta de resultado, `.mt-hero`, 2 columnas 1,3fr / 1fr; 1 columna bajo
  992 px), `.verdict` (pastilla `.ok` / `.bad`, con ícono), `.fig` y `.fig-l` (cifra de 2,5 rem y su
  etiqueta), `.bar` con `.bar-f`, `.bar-m` (la marca del 70 %) y `.bar-c`/`.bar-min` (el rótulo
  "Mínimo para aprobar: 70 %"), `.meta` (`<dl>` de 4 datos con ícono; 1 columna bajo 576 px).
- **Pasos:** `.seg` (selector de dos botones con `aria-pressed`), `.pl` y `.pl-i` (cuadrícula
  32 px · nombre · motivos · desenlace · puntos; a 767 px o menos la fila pasa a 3 columnas y los
  motivos bajan), `.mot` (chip de motivo, con "×N"), `.oc` con `.ok`, `.warn`, `.bad`, `.idle`
  (el desenlace, con ícono), `.clean` ("Ningún paso con fallos.").
- **Qué hacer después:** `.es-next`, `.next-why`, `.next-btns` (la principal a 48 px).
- **Inicio:** `.last-*` (`.last-note`, `.last-l`, `.last-i`, `.last-m`, `.last-a`, `.last-link`)
  sobre `.steps-card .steps-ranking`, que ya existe.
- **Reutilizados:** `.mt-hero`, `.mt-panel`, `.btn`, `.btn-primary`, `.btn-secondary` (el
  "ghost" del prototipo), `.admin-page-head`, `.admin-page-title`, `.admin-page-lead`.
- Los nombres genéricos del prototipo (`.bar`, `.meta`, `.seg`, `.mot`, `.oc`, `.pl`) van **dentro
  de `.res-page`** para no chocar con otras pantallas.

### Estructura de la página (`SessionResult.aspx`)

1. Encabezado: "Resultado de la sesión" y "Producto · Área" (`AreaLabel`, como la barra).
2. Tarjeta de resultado (`.res`):
   - **Assessment:** veredicto (Aprobado / Reprobado), el puntaje en % y la barra contra el 70 %.
   - **Guided:** el puntaje en %, sin veredicto ni barra.
   - **Práctica libre:** "N de M" con "pasos a la primera · Práctica libre no tiene puntaje"; ni
     puntaje ni veredicto ni barra.
   - Guía, Modo, Fecha (hora de la planta) y Duración total.
3. "Pasos": el selector, **A reforzar (N)** abierto por defecto y **Todos (M)** (M = los pasos de
   la guía). Cada fila: número, nombre, chips de motivos (solo el tipo y el conteo, nunca la
   respuesta correcta), el desenlace y, si el modo muestra puntaje, "obtenidos / posibles" y, en
   Assessment, los puntos perdidos del paso a reforzar ("−N pts").
4. "Qué hacer después": una frase y la acción principal, más Repetir e Ir al Inicio.

### Comportamiento

- **El selector A reforzar / Todos es solo del cliente, sin postback** (D-340). Las dos vistas las
  renderiza el servidor (una lista; las filas a reforzar llevan una marca); el JavaScript solo
  alterna cuál se ve. **Sin JavaScript se ve A reforzar** y el selector va oculto. Con 0 a reforzar,
  "Ningún paso con fallos." en esa vista.
- Las acciones son enlaces al Inicio con el modo y la guía en la URL (`?mode=&guide=`, D-337): **no
  inician la sesión**; Iniciar sigue siendo el único camino que la crea.
- **Sin aviso** "te llevamos al Inicio…" debajo de los botones (D-341).
- **Repetir se omite cuando coincide con la acción principal** (Práctica libre con fallos o saltos,
  D-342): la regla ya está en la BLL (`SessionResultService`, E1).
- Sin la guía visual ni anclas `data-guide`: la página no es parte del POD (P-11).

### Textos (D-343, aprobados por el usuario tal cual; no pasan por negocio)

| Caso | Acción principal | Frase |
|---|---|---|
| Guided | Practicar en Práctica libre | Ahora sin la guía visual, para afianzar lo aprendido. |
| Práctica libre sin fallos ni saltos | Hacer el Assessment | Sin fallos ni saltos: ya puedes evaluarte. |
| Práctica libre con fallos o saltos | Practicar de nuevo en Práctica libre | Aún hay pasos por afianzar. |
| Assessment aprobado | Ir al Inicio | Aprobaste este Assessment. |
| Assessment reprobado | Practicar en Práctica libre | Repasa los pasos marcados sin la presión del puntaje. |
| Guía ya no iniciable | Ir al Inicio | Esta guía ya no se puede iniciar. |

Secundarias: "Repetir <modo>" e "Ir al Inicio". Motivos: Fuera de secuencia, Decisión incorrecta,
Acción incorrecta, Sin selección, Campo obligatorio vacío. Desenlaces: "A la primera", "Con N
fallos", "Sin acierto (avance forzado)", "Saltado", "Saltado (N fallos)". Otros: "Pasos",
"A reforzar (N)", "Todos (M)", "Ningún paso con fallos.", "Mínimo para aprobar: 70 %", "Qué hacer
después.", "Puntaje de la sesión".

### Inicio: "Últimas sesiones" (E5, D-344)

Un `<details class="steps-card steps-ranking">` **plegado** ("Mostrar ▾" / "Ocultar ▴") debajo del
bloque de inicio, sin reordenarlo. Una nota ("Tus últimas N sesiones completadas." o, si hay de otra
área, "Tus últimas 5 sesiones completadas. Se indica el área si es otra que la actual.") y una fila
por sesión: modo con ícono, fecha y hora (y "· Producto · Área" solo si es de otra área) y el enlace
"Ver resultado". Sin resultado ni puntaje en la fila. **Sin sesiones completadas, el bloque no
aparece.**

### Métricas derivadas

Ninguna nueva. Lo derivado (puntos por paso, qué reforzar, la sugerencia) ya está en la BLL de la
E1 (`SessionResultRule`, `SessionResultService`), con tests.

## Para revisar en Desarrollo

- Los pesos y pasos del prototipo son de ejemplo (13 pasos, pesos en puntos): los reales salen de la
  BD (`docs/domain.md` → "Ponderación"), "Todos (M)" es la cantidad de pasos de la guía, y los
  puntos son `Weight × 100`.
- "Producto · Área" con el texto estándar de la 021 (`AreaLabel`); el prototipo lo muestra en otro
  orden en el título y en la barra.
- La verificación del diseño fue parcial (capturas; sin medir contraste ni probar clics en un
  navegador real): **AC-9 se verifica en la E3 y la E5**, en claro, oscuro y 768 px, sin scroll
  horizontal de página.
- Casos del prototipo: Assessment aprobado, Assessment reprobado con 3 pasos a reforzar, Guided
  limpio, Práctica libre con fallos y 2 saltos, Práctica libre limpia y la guía que ya no se puede
  iniciar. Del Inicio: con 5 sesiones, con 3, con una de otra área y sin sesiones.

## Desvíos de alcance

- **Datos nuevos (SQL o tablas):** ninguno en el diseño (el SP de las últimas sesiones ya es de la E1).
- **Una regla de negocio nueva:** ninguna.
- **Otro comportamiento:** el selector del cliente (D-340), ya aprobado en la spec.
- **Tocar los masters o tokens que cambien pantallas ya cerradas:** ninguno.
- **Las dos diferencias con la spec original** que señaló la sesión de diseño (una sola lista con
  selector, y sin el aviso "te llevamos al Inicio…") el usuario las aceptó uno por uno: D-340 y D-341.
