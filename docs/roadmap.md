# Roadmap — MESTrainingSimulator

> **Estado e historial.** Las reglas vigentes están en `CLAUDE.md`, el proceso de trabajo en
> `docs/process.md` y los mecanismos en `docs/architecture.md`. Specs y auditorías tienen sus
> propios archivos (`docs/specs/`, `docs/audits/`); aquí solo se referencian. Las secciones
> de cada fase describen lo que se hizo **en su momento**: si contradicen a `CLAUDE.md`,
> manda `CLAUDE.md`.

---

## 📍 Estado actual — dónde quedamos

> Leer al iniciar cada sesión. Actualizar al cerrar una entrega, una fase o una sesión de
> trabajo relevante. Mantenerla corta: el detalle va en la sección de la fase, la spec o la
> auditoría.

**Última actualización:** 2026-10-07 · **Branch:** `TrainingApp` · **Último commit de
código:** Fase 7 (LoFlo) (`git log --oneline -10` para ver el estado real)

- **Ahora — Fase 8 (validación interna)**, en su momento, por decisión del usuario.
- **Lo último cerrado (2026-10-07):** la **Fase 7 · catálogo de LoFlo** (`08`/`09`; NC, DC y
  BOM simulados con marca `SIM`), probada por el usuario en el navegador. Antes, la
  **Fase 5 · tests de `ScoreCalculator`**; después,
  sin fase, la **reorganización de la documentación** (cada regla en un solo lugar: mapa en
  `CLAUDE.md`, nuevos `docs/process.md` y `docs/architecture.md`; la memoria solo guarda lo
  que no va al repo) y la **limpieza de los scripts SQL** (corridos y probados por el
  usuario). Antes, la 6.6 (overlays guiados) y un arreglo de rendimiento (ver "Historial").
- **Pendientes del usuario (diferidos):** los códigos NC más comunes, para que la guía los
  sugiera (spec 010), y las capturas de los 4 modales del Start de Moldeo (inventario #8).
  El detalle por paso en el resumen final se verá en su momento.
- **Acción del usuario, en su momento (D-8):** rotar la contraseña de `sa` y actualizar
  `connectionStrings.config`.

### Deuda y riesgos conocidos

Solo lo abierto; lo resuelto queda en la auditoría o la spec que lo cerró. La auditoría
final (D-16) repasó esta lista.

- **Botón Atrás del navegador:** todavía puede reenviar un POST viejo (F5 sí está resuelto).
  Riesgo aceptado.
- **B-2 de Log NC:** solo se da con manipulación del cliente (`docs/specs/009-plan.md`, E4).
- **Cobertura de tests:** `ScoreCalculator` cubierto (Fase 5); `StepValidator` con cobertura
  parcial. `PodService` crea sus repositorios internamente, así que lo que hace alrededor
  del puntaje (registrar el intento, avance forzado, guardar `FinalScore`) no se puede probar
  sin BD; se probó en el navegador (spec 009, auditoría final). Cubrirlo sería otra fase
  (interfaces de repositorio o integración contra LocalDB).
- **`CompleteControl`** sin UI (Complete se resuelve desde `BtnComplete`).
- **Detalle por paso en el resumen final** (intentos y puntaje por paso): diferido
  (spec 007) hasta que lo pida un instructor o la Fase 8.
- **AUD-28/29** (endurecimiento del deploy y paquetes): en la Fase 9 (D-7).
- **AUD-40 y AUD-42** (`PodState` mutable fuera de `PodService`; orden no reproducible tras
  reconstruir en Serialized): diferidos (D-16).

### BD de desarrollo del usuario

Al día con los scripts `00`–`09`. Una instalación limpia se arma con ellos en orden
(verificado en LocalDB el 2026-09-25: idéntica a la BD de desarrollo). Últimos scripts que
corrió el usuario:

| Fecha | Qué corrió | Por qué |
|---|---|---|
| 2026-09-28 | ALTER de la spec 008 | Esquema Serialized (grupos DC, BOM, `RequiresStartChecklist`) |
| 2026-10-01 | ALTER + `02`, `05`, `06` | Columna `DataCollectionParameters.Description` (Fase 6.5, E8) |
| 2026-10-02 | `03`, después `03` y `04` | Auditoría E1 y E3: `spTrainingSessionAbandonActiveByOperator`; `spStepAttemptRecord` reemplaza a los SPs de intento, avance y cierre |
| 2026-10-05 | `03` | Spec 009 E6: `spTrainingSessionAbandonActiveByOperator` sin `@TrainingGuideId` |
| 2026-10-07 | `01`–`07` | Limpieza de scripts (2026-10-07): `CREATE OR ALTER`, catálogos como tablas de valores, transacciones, `spTrainingGuideSync` |
| 2026-10-07 | `08`, `09` | Fase 7: catálogo y guía de LoFlo |

`05_SeedData.sql` es **solo dev**: Redefinity (Serialized con Assembly, 2 DC Groups
obligatorios + 1 opcional, BOM `SIM-100001`/`SIM-100002`) y Subensamble 1.
Limpieza del 2026-09-25: las migraciones (`08`–`11` y los ALTER de `06`) se integraron al
esquema y se borraron. Cómo se entregan los cambios de esquema: `docs/process.md` → "Base de
datos".

### Auditorías

| Fecha | Archivo | Alcance | Estado |
|---|---|---|---|
| 2026-10-01 / 02 | `docs/audits/2026-10-01-auditoria.md` | Proyecto completo: AUD-01…32, D-1…D-12, entregas E1-E5 + spec 009 | ✅ (AUD-28/29 en la Fase 9) |
| 2026-10-06 | `docs/audits/2026-10-06-auditoria-final.md` | Spec 009 + regresión completa en el navegador, antes de la 6.6: AUD-33…44, D-13…D-16 | ✅ (AUD-40/42 diferidos) |

---

## Fases

| Fase | Estado | Cierre | Spec / plan |
|---|---|---|---|
| 0 · Setup | ✅ | — | — |
| 1 · Auth | ✅ | — | — |
| 2 · Dominio y catálogo de R&C | ✅ | — | `002-rc-moldeo-catalog.md` |
| 3 · Motor de TrainingGuide | ✅ | — | — |
| 4 · TrainingSession y modos | ✅ | — | `003-training-session-y-modos.md` |
| 5 · ScoreCalculator: cobertura de tests | ✅ | 2026-10-07 | `fase-5-plan.md` |
| 6 · UserControls por tipo de paso | ✅ | 2026-09-24 | `004`, `005`, `006`, `007` |
| — · Ajustes tras la 6 (S1/S2) y spec 008 (Serialized) | ✅ | 2026-09-28 | `008-*.md` |
| 6.5 · Fidelidad de UI/UX con el SAP MES real | ✅ | 2026-10-02 | `fase-6.5-plan.md` |
| — · Auditoría + spec 009 (motor del POD) + auditoría final | ✅ | 2026-10-06 | `009-*.md`, `docs/audits/` |
| 6.6 · Overlays guiados | ✅ | 2026-10-07 | `010-*.md` |
| 7 · Catálogo de LoFlo | ✅ | 2026-10-07 | plan directo |
| **8 · Validación interna** | ▶️ siguiente | | |
| 9 · Deploy del MVP | pendiente | | |

### Fase 7 — Catálogo de LoFlo (segunda área) ✅
**Objetivo:** script SQL con el catálogo de LoFlo (Sensores). Solo carga de datos, cero
código nuevo: valida que el motor es genérico y que `RequiresAssembly` y
`RequiresDataCollection` funcionan con otra configuración.
**El script tiene que cargar** (spec 008): los flags del área (`CLAUDE.md` → "Jerarquía",
incluido `RequiresStartChecklist = 0`), NC Codes, `DataCollectionGroups` con sus obligatorios,
sus `DataCollectionParameters`, el BOM en `AssemblyComponents` (≥1 componente) y la guía con
sus pasos y pesos.
**No incluye:** cambios al motor, nuevas pantallas.
**Desbloqueo:** los bloqueantes #1 (Start de Serialized), #2 (Assembly sin completar) y #4
(huecos de UI de Assembly) los resolvió la spec 008; el #3 (flags reales) lo resolvió negocio
el 2026-10-07: todas las áreas de Sensores son Serialized con Assembly, DC y Component en NC,
sin checklist de Start; DC Groups, parámetros, códigos NC y BOM se simulan por ahora.
**Resultado (2026-10-07, plan directo):** `08_SensoresLoFloCatalog.sql` (flags reales; 6
códigos NC, 3 DC Groups —2 obligatorios— con 6 parámetros y un BOM de 3 componentes, todo
simulado y marcado `SIM` para retirarlo a mano cuando llegue el catálogo real) y
`09_SensoresLoFloGuide.sql` (pesos de Redefinity). `05` dejó de tocar LoFlo. El motor resultó
genérico: los únicos cambios de código fueron dos bugs de `DataCollectionControl`, ambos
anteriores a LoFlo: los campos numéricos rechazaban decimales (`step="any"`) y, tras cerrar un
grupo con la ✕ y abrir otro, el Save podía caer con una excepción de validación de eventos
(los campos dinámicos usaban IDs automáticos que se corrían; ahora tienen un ID fijo por
parámetro). Probado en LocalDB y por el usuario en el navegador.

### Fase 8 — Validación interna
**Objetivo:** sesión con trainer/operador real sobre R&C y LoFlo. Correcciones de UX, bugs de
flujo, ajustes de catálogo.
**No incluye:** nuevas áreas, dashboard admin, reportes.

### Fase 9 — Deploy del MVP
**Objetivo:** deploy en servidor interno con IIS: connection string de producción, App Pool,
permisos, prueba de humo. Incluye AUD-28/29.
- **Arranque en frío en IIS** (medido 2026-10-07 en desarrollo): tras reciclar el App Pool,
  el primer request compila el markup (3-10 s por carpeta; ~18 s todo el sitio). Resolver al
  publicar: precompilar el sitio y configurar el App Pool con `startMode="AlwaysRunning"`,
  `preloadEnabled="true"` (Application Initialization) e idle timeout 0.
**No incluye:** CI/CD automatizado, monitoreo avanzado.

---

## Historial

### Fase 0 — Setup ✅
**Objetivo:** proyecto Web Forms creado, N-Tier scaffolded, build verde.
**No incluye:** lógica de negocio, UI funcional, datos reales.

### Fase 1 — Auth ✅
**Objetivo:** `Login.aspx` + `ChangePassword.aspx` con el flujo completo de primer acceso
(API de empleados → `Operator` local → PBKDF2 → FormsAuth).
**No incluye:** dashboard, GuidedTest, catálogo de áreas, pasos de entrenamiento.

### Fase 2 — Dominio y catálogo de R&C ✅
**Objetivo:** auditoría del legacy de R&C + catálogo real en `06_RCMoldeoCatalog.sql`:
`Products` (R&C), `Areas` (Moldeo), `NCCodes` (30 códigos), `DataCollectionParameters` (15:
ID molde + 10 parámetros MSS + verificación MSS + fecha kerosene + 2 first-shot). El esquema
de `DataCollectionParameters` sumó `DataType` y `DisplayOrder`; `MinValue`/`MaxValue` quedaron
nullable (rangos pendientes del MSS D000281359).
**No incluye:** UI, motor de validación, `TrainingGuideService`, pantallas.
**Deuda de proceso:** se saltó la aprobación de la spec 002 antes de implementar.

### Fase 3 — Motor de TrainingGuide ✅
**Objetivo:** `TrainingGuideRepository` (un SP con dos result sets puebla `Steps`),
`TrainingGuideService.BuildValidatorForGuide` y `StepValidator` con `StepValidationContext`
inyectado (Area + NCCodes + DCParams), con validación para todos los `StepType`. Repositorios
`Area`, `NCCode`, `DataCollectionParameter` y sus SPs. Guía real de Moldeo en
`07_RCMoldeoGuide.sql`.
*Superado después:* la LC Moldeo era un paso propio de la guía (con pesos 0.04/0.03/0.03 en
3 instancias de LC) y Assembly se validaba contra `ExpectedComponentCount`. Hoy el checklist
va dentro de Search, Assembly usa el BOM (spec 008) y los pesos están en `CLAUDE.md`.
**No incluye:** gate-keeping real, UI, tests de `ScoreCalculator`.

### Fase 4 — TrainingSession y modos ✅
**Objetivo:** gate-keeping real Guided vs. Assessment en `TrainingSessionService`, `Abandon`,
puntaje en tiempo real. `StartSession.aspx` (Product → Area → Guía → Modo) y un
`GuidedTest.aspx` genérico por paso. Spec `003-training-session-y-modos.md` (aprobada
2026-09-04).
**No incluye:** UserControls por tipo de paso, tests de `ScoreCalculator`, LoFlo.

### Fase 5 — ScoreCalculator: cobertura de tests ✅ (2026-10-07)
**Objetivo:** blindar `ScoreCalculator` (el componente más sensible) con tests NUnit
exhaustivos antes de que usuarios reales generen puntajes: penalización, avance forzado,
pasos opcionales ausentes, sesiones completadas vs. abandonadas. Decisión del usuario: antes
de la 7 y la 8.
**No incluye:** nuevas features, cambios al algoritmo.
**Resultado:** 3 entregas, sin spec (`docs/specs/fase-5-plan.md`).
`TrainingApp.Test/ScoreCalculatorTests.cs` cubre todas las ramas de `ScoreCalculator`, las
guías reales de Moldeo y Redefinity, el límite de aprobación y un barrido de las 65 536
combinaciones de cada guía. El código cumplía todas las reglas: no hubo cambios al
algoritmo. De paso, los pesos de `CLAUDE.md` se alinearon con la BD (manda la BD).

### Fase 6 — UserControls por tipo de paso ✅ (2026-09-24)
**Objetivo:** un UserControl `.ascx` por `StepType` que recibe el estado del servidor y emite
un submit, sin lógica de negocio.
**No incluye:** catálogo de LoFlo, nuevas áreas.
**Specs:** `004-fase6-dashboard-usercontrols.md` (POD, botonera y contrato de controles) ·
`005-pod-orden-dinamica.md` (orden simulada creada por Resource Setup, SFC Split como paso
propio, DC agrupado por DC Group; aprobada de forma retroactiva) ·
`006-controles-de-paso-estaticos.md` · `007-cierre-flujo-lognc-complete.md`.
**Decisiones de arquitectura** (controles estáticos, `Initialize`, guard de sender, F5 =
GET…): ahora en `docs/architecture.md` → "`GuidedTest` y controles de paso".
**Commits de cierre:** `3567e07` (controles estáticos), `51636a7` (DC, txtSFC, resaltado,
puntaje final), `f54ff50` (spec 007), `3a38b86` (F5).
**Cierre:** el flujo completo de R&C/Moldeo (NonSerialized) funciona de punta a punta en
Guided y Assessment, verificado por el usuario.

### Ajustes tras la Fase 6 y spec 008 ✅ (2026-09-25 a 2026-09-28)
- **Assessment real (S1 + S4, sin spec):** el 50% del puntaje nunca podía fallar porque los
  controles rechazaban las decisiones antes de enviarlas. Se separó formato de decisión (hoy
  `CLAUDE.md` → "Qué cuenta como intento"); el 70% pasó a `ScoreCalculator.PassingScore`; los
  banners de guardado solo aparecen si el servidor aceptó el paso.
- **S2, secuencia en Assessment (sin spec):** reglas cerradas con el usuario el 2026-09-25,
  hoy en `CLAUDE.md` → "Qué cuenta como intento".
- **Spec 008 (5 entregas, `008-plan.md`):** el motor soporta áreas Serialized: 3-5 piezas
  con SFC propio, Browse Work List y POD con multi-selección, `[N SELECTED]`, Start directo
  sin checklist (`Areas.RequiresStartChecklist`); Assembly con el BOM (se eliminó
  `ExpectedComponentCount`); DC por pieza con grupos obligatorios (Moldeo exige su grupo de
  fechas de expiración); Log NC y Complete multi-fila. Pendiente diferido en la spec: recorrido
  pieza por pieza. Verificado: Redefinity de punta a punta y regresión de Moldeo.
- **Checklist de la spec 006 cerrado (2026-09-29):** el punto 6 (volver a guardar Resource
  Setup) se resolvió por análisis de código (`BtnSave_Click` con `if (!Visible) return;` + el
  guard de sender); el 5 y el 7 los cubrió la spec 008.

### Fase 6.5 — Fidelidad de UI/UX con el SAP MES real ✅ (2026-09-29 a 2026-10-02)
**Objetivo:** que cada pantalla y modal del POD replique su captura del SAP MES real, y dejar
la interfaz cerrada antes de los overlays de la 6.6.
**No incluye:** reglas de negocio, motor, puntaje, pasos nuevos, overlays, LoFlo, páginas que
no son del MES (Login, Cambio de contraseña, StartSession).
**Proceso:** sin spec; cada tanda de correcciones del usuario = una entrega, con plan en
`docs/specs/fase-6.5-plan.md`.
**Resultado:** inventario `docs/ui-reference/inventario-pantallas.md` con todas las pantallas
con captura en ✅. Entregas 1-9: columna SFC y Select All; Collect deshabilitado por pieza;
Shop Order / Status; layout y colores del MES; header estándar de los 12 modales; Resource
Setup; botones grises (`.mes-btn` + `.mes-ico-*`); Line Clearance con submodal apilado;
listas; DC Entry y NC Data Entry (columna nueva `DataCollectionParameters.Description`);
SFC Split.
**Regla cambiada por decisión del usuario:** el SFC Split crea un **SFC nuevo** de la misma
Shop Order (`SplitSfcGenerator`; enmienda 2026-10-01 de la spec 005).
**Commits:** `48b67c1`, `fbcd132`, `8fde133`, `b832e77`, `f432462` (inventario), `c38fe94`,
`70e5e8e`, `03d767b`, `4ca602c`, `f9aa1c7`, `c8d99d4`, `2f550eb`.

### Auditoría, spec 009 y auditoría final ✅ (2026-10-01 a 2026-10-06)
- **Auditoría del proyecto (2026-10-01/02):** 32 hallazgos (AUD-01…32), decisiones D-1…D-8
  y 5 entregas (E1 bugs del flujo · E2 auth y secretos · E3 integridad del puntaje · E4
  capas · E5 limpieza). Detalle: `docs/audits/2026-10-01-auditoria.md`.
- **Spec 009 · Motor del POD en la BLL (2026-10-05, 6 entregas `d5331ab`…`df08c44`):**
  `PodService` decide el flujo y la página solo pinta; contexto una vez por request (Line
  Clearance: de ~21 a 5 conexiones); el estado del POD se reconstruye si se pierde (D-10);
  una sesión activa por operador (D-9). Cerró AUD-16, 25 y 26. Detalle:
  `docs/specs/009-motor-pod.md` y `009-plan.md`.
- **Auditoría final (2026-10-06):** revisión de la spec 009 + regresión completa en el
  navegador: AUD-33…44 (1 Medio: el logout), D-13…D-16 y 4 entregas (E1 logout · E2
  invariantes de la BLL · E3 la página lee el flujo del contexto · E4 limpieza). Detalle:
  `docs/audits/2026-10-06-auditoria-final.md`.

### Fase 6.6 — Overlays guiados ✅ (2026-10-07)
**Objetivo:** en Guided, overlays que guían al operador paso a paso sobre la interfaz cerrada
en la 6.5.
**No incluye:** cambios de UI de las pantallas del MES, reglas de negocio nuevas.
**Resultado (spec 010, 5 entregas + E2b, `4e24ce5`…`39482d3`):** guía clic por clic en Guided
sobre los 9 pasos, siempre activa y bloqueante (Logout y Abandon libres), con respuestas y
valores de ejemplo, que solo avanza con la respuesta correcta (R10), con mensajes del MES que
esperan a la guía (R9), falla segura y globo final. Nada en Assessment. Mecanismo:
`docs/architecture.md` → "Guía visual de Guided". De paso, en Serialized el POD vuelve sin
selección tras Start, Assembly y DC, como el MES real. La investigación previa (legacy y app)
quedó como anexo en `docs/specs/010-overlays-guiados.md`.

### Reorganización de la documentación (2026-10-07, sin spec)
Cada regla en un solo lugar. `CLAUDE.md` quedó con solo reglas vigentes y un mapa de
documentos (de 37,5 KB a 22,9 KB); el proceso pasó a `docs/process.md` y los mecanismos a
`docs/architecture.md`; el roadmap quedó como estado + historial; el glosario, solo
definiciones. Se quitaron los pesos copiados en `docs/ui-reference/` (estaban desalineados
de la BD). La memoria pasó de 10 a 4 entradas (perfil, reporte semanal, `/btw` y dónde
anotar cada cosa).

### Limpieza de scripts SQL (2026-10-07, sin spec)
Mismos 8 archivos y numeración (decisión del usuario). Los SPs pasan a `CREATE OR ALTER` sin
los `DROP` de SPs ya eliminados. Cada catálogo se declara una vez como tabla de valores y se
actualiza o inserta en orden (los Ids no cambian), todo dentro de una transacción. Las guías
se cargan con `spTrainingGuideSync` (nuevo, en `02`), que actualiza los pesos y aborta si no
suman 1.00. Se corrigió un bug: los literales sin `N` guardaban "Ω" como "O". Se probó en
LocalDB: la instalación limpia y la re-ejecución sobre una BD con sesiones dan los mismos
datos e Ids que los scripts anteriores. Reglas: `docs/process.md` → "Base de datos".

### Arreglo de rendimiento del arranque ✅ (2026-10-07, sin spec)
`optimizeCompilations` en `Web.config` (Login tras un rebuild: de ~4 s a ~1,7 s); la lista de
empleados se precarga al arrancar y se refresca en segundo plano (`docs/architecture.md` →
"API de empleados"); `/bundles` y `/Content` accesibles sin login (Login y ChangePassword
recibían el HTML del Login en vez de sus JS). B-1: una lista vacía de la API no reemplaza la
última. Pendiente para la Fase 9: el arranque en frío en IIS.
