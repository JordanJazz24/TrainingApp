# Spec 020: Custom Resource Setup (R3)

**Status:** Approved (2026-10-26, usuario), enmendada 2026-10-26 (enmienda 1) · plan `020-plan.md`, pendiente de aprobación
**Date:** 2026-10-26 · **Origen:** revisión con Ernesto (2026-10-22), R3 · **Sesión:** `Custom-RS`
(híbrida de planeación e implementación solo para R3, por decisión del usuario 2026-10-26)
**Decisiones:** D-180 a D-194, todas confirmadas por el usuario (rango reservado D-180 a D-199).
Capturas: `docs/ui-reference/Screens SAP MES REAL/Custom Resourse Setup/` (5).

> **Enmienda 1 (2026-10-26, usuario, al probar la E3):** afecta D-182, D-184, D-191, el punto 4
> de Scope y AC-7.
>
> - **D-200:** cada sesión recibe **siempre 2 Tool Groups** del pool de 8 (antes, de 2 a 4). El
>   pool, el sorteo por sesión y el orden alfabético no cambian.
> - **D-201:** con la orden asociada, **cada fila de la tabla necesita su Tool Number**, en
>   Guided y en Assessment (antes, al menos uno). Una fila vacía es un aviso de formato sin
>   intento, como D-191; el servidor lo rechaza igual si llega. Los Tool Numbers siguen siendo
>   de captura libre y no se califican (D-182).
> - Secuencia del punto 4: "Tool Number de al menos un grupo" pasa a "**un Tool Number por cada
>   Tool Group**". AC-7: "una sesión de 2 Tool Groups".

## Problem

La planta va a pilotear en noviembre, en un área y sin fecha segura, una pantalla nueva que
reemplaza al Resource Setup actual: **Custom Resource Setup**. En vez de la tabla libre de
"Setup Type / Setup Type Value", el operador asocia la Shop Order y registra **un Tool Number
por cada Tool Group** que la operación exige. El simulador solo entrena la pantalla estándar;
cuando el área del piloto cambie, el operador tendría que aprender la nueva sin práctica.

Hacen falta las dos pantallas a la vez: las áreas que no están en el piloto siguen con la
estándar, y un administrador tiene que poder cambiar de una a otra por guía.

## Scope

### In

**A. La pantalla** (D-180, D-181)
1. Un **modal del POD** con el diseño de las capturas, abierto por el **mismo botón Resource
   Setup**. Qué pantalla abre lo decide el tipo de Resource Setup de la sesión.
2. Campos: **Site**, **Resource** y **Operation** (solo lectura), **Retrieve**, **Setup State**,
   **ShopOrder** con **Associate New ShopOrder**, la **Tool Number Association Table**
   (Tool Number, Tool Number Description, Tool Group, Tool Group Description), **Save** y
   **Clear**. Arriba a la derecha, el Code1 del operador (como la captura).
3. Los avisos del MES, tal como las capturas: **Information** ("Shoporder is associated in
   resource setup. Please associate tool numbers as well"), **Confirmation** ("Are you sure
   you want to proceed?", Yes / No) y **Success** ("Resource setup saved successfully.").

**B. Secuencia y calificación** (D-183, D-185)
4. Secuencia del operador: **Retrieve → Setup State = Set Up → escribir la Shop Order →
   Associate New ShopOrder (OK) → Tool Number de al menos un grupo → Save → Yes → OK**.
5. El paso sigue siendo `ResourceSetup`, con el mismo **peso** y el mismo intento
   (`ResourceSetup.Save`). Se califica la **secuencia** y **Setup State = "Set Up"**, y que la
   **orden esté asociada** al guardar. Responder **No** a la confirmación no cuenta. Los Tool
   Numbers solo se validan como formato (D-182). Los avisos de formato y el Save sin asociar
   siguen D-191; Save con "Open" crea igual la orden (D-193).
6. Todas las decisiones llegan a la BLL; ningún botón del modal avanza el paso por sí solo.
   Un solo postback, el del Save tras el **Yes**: asociar y confirmar se resuelven en el cliente
   (CLAUDE.md → "Un postback por paso"). La orden simulada se crea al guardar, como en la
   estándar (D-189).

**C. Datos simulados con costura hacia las Operations** (D-184, D-186)
7. **Resource** = `PodContext.ResourceName`; **Operation** = `PodContext.CurrentOperationName`;
   **Site** = `PodDefaults.Site`. Los tres ya existen y son simulados.
8. **Tool Groups:** un pool de **8** (los de la captura: `TG_FLUID_DISPENSER`,
   `TG_HEATER_BLOCK`, `TG_HOLE_GAUGE`, `TG_RULER`, `TG_SOLDERING_STATION`, `TG_TIMER`,
   `TG_UV_LIGHT`, `TG_WELLER_HOT_AIR`, con sus descripciones). Cada sesión recibe **entre 2 y 4**,
   el **mismo conjunto durante toda la sesión** (se deriva del Id de la sesión, no se guarda),
   en orden alfabético como la captura.
9. Todo eso sale de un solo punto de acceso en la BLL (`CustomResourceSetupProvider`). Cuando
   existan las Operations reales, solo cambia de dónde lee ese proveedor (tablas de
   Tool Groups por Operation); ni la pantalla ni la calificación se tocan.
10. **Sin equipos precargados** (D-186): la Shop Order y los Tool Numbers los escribe el
    operador; no hay el error de "equipos del turno anterior" de la spec 015.

**D. Qué pantalla usa cada guía** (D-180, D-187, D-188)
11. `TrainingGuides.ResourceSetupType` (0 = Standard, predeterminado; 1 = Custom). Una guía, un
    tipo. `spTrainingGuideSync` no lo toca.
12. `TrainingSessions.ResourceSetupType`: la sesión **copia el tipo de la guía al empezar**. Un
    cambio del administrador vale para las **sesiones nuevas**; la que está en curso no cambia
    de pantalla a la mitad.
13. **Página "Guías" en Admin** (D-187): lista de guías (producto, área, título, publicada) con
    un selector Estándar / Custom por guía. Capacidad nueva `AccessPolicy.CanManageGuides`
    (solo Administrator). Se hace con el sistema de diseño del brief 019.
14. Antes de que exista la página, el piloto se configura con un script de `Database/Ops/`.
    Ninguna guía arranca en Custom (D-194).

**E. Guía visual y modos**
15. En **Guided**, la guía de Custom RS: anclas `data-guide` nuevas (`crs-*`) y su guion en
    `GuideScriptBuilder` (CLAUDE.md → "Guía visual de Guided"). **Assessment** y **Práctica**
    reciben la misma pantalla sin el guion; los botones fuera de secuencia, el avance forzado
    y la reconstrucción del POD se comportan como en la estándar.

### Out (no incluye)

- Catálogo real de Tool Numbers, la descripción que se llena al teclear un Tool Number y la
  ayuda de búsqueda del ícono ⧉ (D-182: el ícono es decorativo; no hay captura de lo que abre).
- Las Operations reales y las tablas de Tool Groups por Operation (solo se deja la costura).
- Cambios en el Resource Setup estándar, en los pesos, en el dashboard o en las demás pantallas.
- Un flag por Operation: el flag es por guía.
- Reglas de calificación sobre qué Tool Number va con qué grupo.

## Decisiones del usuario (2026-10-26)

| ID | Decisión |
|---|---|
| D-180 | Existen **los dos** Resource Setup; el administrador elige cuál usa cada guía; la estándar es la predeterminada |
| D-181 | Custom RS es un **modal** del POD, mismo botón, con el diseño de las capturas |
| D-182 | Tool Numbers de **captura libre**, al menos uno; sin catálogo; el ícono ⧉ es decorativo |
| D-183 | Se califica **secuencia + Setup State = Set Up**; el Yes/No de la confirmación no se califica |
| D-184 | Resource y Operation salen de los **datos simulados** actuales; Tool Groups **simulados, 2 a 4 de un pool de 8**, el mismo conjunto en toda la sesión; hay que **prever las Operations** (proveedor único) |
| D-185 | Secuencia confirmada: Retrieve, Set Up, Shop Order, Associate (OK), Tool Numbers, Save, Yes, OK |
| D-186 | Custom RS **parte vacío**: sin equipos precargados |
| D-187 | El flag se cambia en una **página de Guías en Admin** (con selector por guía) |
| D-188 | Un cambio del flag vale para **sesiones nuevas** |
| D-189 | Associate y la confirmación se resuelven en el cliente; **un postback, el del Save**; la orden simulada se crea al guardar |

## Decisiones confirmadas por el usuario (propuestas de Claude aprobadas, 2026-10-26)

Claude las propuso y el usuario las aprobó todas, en rondas agrupadas.

| ID | Decisión | Propuestas |
|---|---|---|
| D-190 | **Presentación:** el Code1 del operador va arriba a la derecha (como la captura). Antes de Retrieve se ve "Open" con el selector deshabilitado; después trae Set Up / Open con "Set Up" por defecto. Tras el Success, el modal se cierra con el OK y el paso avanza, sin el banner "Record changed" | P-1, P-7, P-8 |
| D-191 | **Validación:** Associate con la Shop Order vacía o no numérica = aviso de formato, sin intento. Save sin haber asociado la orden = intento fallido por decisión equivocada (mensaje genérico en Assessment). Save con la orden asociada y ningún Tool Number = aviso de formato, sin intento; las filas de Tool Group vacías se ignoran y `<` o `>` se bloquea como formato (AUD-11) | P-2, P-3, P-4 |
| D-192 | **Comportamiento:** Clear deja la pantalla en el estado inicial (sin Retrieve, sin orden asociada, tabla vacía). Volver a asociar otra orden conserva los Tool Numbers ya escritos. Al reiniciar la guía (otra sesión) los Tool Groups se sortean de nuevo | P-5, P-6, P-9 |
| D-193 | Un Save con Setup State = "Open" es intento fallido y **crea igual la orden simulada**, como la estándar (spec 005) | P-10 |
| D-194 | Ninguna guía arranca en Custom: todas quedan en Estándar hasta que el administrador (o el script de `Database/Ops/`) marque la del piloto | P-11 |

## Acceptance criteria

- **AC-1** Given una guía en Custom, When el operador pulsa Resource Setup, Then abre el modal de
  Custom RS con Site, Resource, Operation y la tabla vacía hasta asociar la orden.
- **AC-2** Given una guía en Standard (o sin cambios), Then el Resource Setup es el de hoy, sin
  diferencias.
- **AC-3** Given Custom RS, When Retrieve → Set Up → orden → Associate → un Tool Number → Save →
  Yes, Then el paso es correcto, la orden simulada se crea con esa Shop Order y aparece el Success.
- **AC-4** Given Setup State = "Open" al guardar, Then es intento fallido (en Guided, la guía no
  deja enviarlo).
- **AC-5** Given Save sin Associate, Then es intento fallido y no se crea la orden. En
  Assessment, con el mensaje genérico.
- **AC-6** Given la confirmación, When el operador pulsa No, Then no hay postback ni intento.
- **AC-7** Given una sesión de 2 a 4 Tool Groups, When se abre Custom RS varias veces o se
  recarga la página, Then el conjunto y el orden son los mismos.
- **AC-8** Given el administrador cambia el tipo de una guía, Then las sesiones en curso conservan
  su pantalla y las nuevas usan la nueva.
- **AC-9** Given un Operator, Then no ve ni abre la página de Guías (redirige a su inicio, no al
  login).
- **AC-10** Given Guided, Then la guía recorre la secuencia completa de Custom RS con sus anclas
  `data-guide` (ninguna con id).
- **AC-11** Given el avance forzado o la sesión reconstruida en una guía Custom, Then el POD se
  rehace con la orden por defecto, como en la estándar.
- **AC-12** El puntaje de las guías existentes no cambia (los tests de `ScoreCalculator` siguen
  en verde) y el modal se ve bien con Bootstrap 5.3.8 (la Fase 11 E1 ya lo integró en
  `TrainingApp`, `a474c7d`).

## Riesgos y coordinación

- **Fase 11** (Desarrollo, worktree `fase-11`), revisada el 2026-10-26 contra el repo:
  - E1 (Bootstrap 5.3.8) y E2 (sistema de diseño, `Admin.Master`, login, Usuarios e inicio del
    operador) **ya están en `TrainingApp`** (`a474c7d`, `840a208`). `Admin.Master` y los tokens
    de `UI/master/admin.css` están estables: la página de Guías se hace con ellos y con Bootstrap
    Icons, sin esperar nada.
  - Desarrollo está en la E3 (Resumen), sin commitear, y las E3 a E6 tocan solo
    `Pages/Admin/{Summary,Operators,Operator,Steps,Tracking}`, `SummaryRows.cs`, `admin.css`,
    `admin-charts.js`, `Bundle.config`, `ExcelReports.cs`, `XlsxStreamWriter.cs` y
    `TrainingApp.csproj`. **No tocan** `GuidedTest`, `PodService`, `BLL/Scoring`, `Domain`,
    `Database/StoredProcedures/00, 02, 03` ni el POD (su "No incluye").
  - Archivos que los dos frentes tocaremos: **`TrainingApp.csproj`** (cada uno agrega sus
    archivos) y **`UI/master/admin.css`** (la página de Guías puede necesitar un par de clases).
    Se commitea solo por rutas; el que llegue segundo hace el merge de ese archivo.
  - La página de Guías va en la **última entrega** y agrega una línea de menú a `Admin.Master`.
- `GuidedTest.aspx(.cs)` es compartido solo en teoría: nadie más lo toca ahora. Los cambios se
  mantienen mínimos (un control estático más, como los demás controles de paso).
- `docs/decisions.md` y `docs/specs/019-ui-ux-dashboard.md` tienen cambios **sin commitear**
  de Planeación (D-164, ajuste del mockup del Resumen). Al registrar D-180 a D-194 se commitea
  solo el fragmento de R3 de `decisions.md`, sin arrastrar el D-164.
- Scripts: el esquema va en `00`, los SP en `02` y `03` (BD de pruebas: se recrea o se corre el
  `ALTER` que se entrega aparte). El 13 reservado probablemente no haga falta.
- La spec lleva el número **020**: no existe otra con ese número en el repo ni en el roadmap.
