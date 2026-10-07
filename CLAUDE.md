# MESTrainingSimulator

## Contexto de negocio

- Esta es una plataforma interna de entrenamiento y evaluación para operadores de manufactura médica.
- El objetivo es simular flujos guiados de entrenamiento inspirados en procesos tipo SAP MES, y evaluar progreso, puntaje y desempeño desde el servidor.
- Parte de los datos puede estar quemada o hardcodeada porque representa pantallas y valores que el operador verá en su día a día.
- Eso no justifica una mala arquitectura: la plataforma debe ser mantenible, separada en capas y extensible.
- El sistema puede evolucionar para incluir guías de entrenamiento, evaluaciones, puntajes, dashboards, preguntas con mayor índice de fallo, métricas por operador y reportes de desempeño.

## Legacy

- El directorio `/MES Simulator Legacy` contiene la aplicación anterior.
- `/MES Simulator Legacy/CLAUDE_Legacy_Reference.md` existe solo como referencia funcional y de dominio.
- No se debe actualizar ni usar como arquitectura destino.
- Se puede revisar para entender flujos de negocio, reglas del tutorial guiado, pantallas, pasos y datos reutilizables o migrables.
- No copiar patrones de mala arquitectura del legacy: `[WebMethod]` para lógica de negocio, `DataSet` como modelo de dominio, `sessionStorage` como fuente global de estado, ni lógica de negocio en code-behind.
- Si una sección del tutorial puede reutilizarse tal cual, documentarlo explícitamente.
- Si una sección cambia de parámetros o de reglas, modelarla de forma nueva en el dominio.

## Arquitectura objetivo y seguridad

- El servidor es la única fuente de verdad para progreso, puntuaciones, estado de sesión, validaciones del flujo y resultados de evaluación.
- Las páginas ASPX no deben poder saltarse pasos ni modificar el estado real de negocio directamente.
- La lógica de negocio vive en la capa BLL — nunca en code-behind ni en stored procedures.
- Cada capa tiene una responsabilidad única y no viola la dirección de dependencias: Presentación → BLL → DAL → Domain.
- Proteger autenticación, autorización, integridad de progreso y validaciones del lado servidor.
- El nivel de seguridad es proporcional al contexto: simulador interno de entrenamiento.
- Las contraseñas nunca se almacenan en texto plano — hashing con **PBKDF2** (`Rfc2898DeriveBytes`, nativo en .NET FW 4.7.2). Confirmado como definitivo — reemplaza cualquier mención previa de SHA256+salt o BCrypt.
- Contraseña por defecto (`Shared/Constants.cs`) y estado de sesión del operador (solo servidor, tras auth): ver Key Conventions.

## Reglas de Negocio del Dominio

Estas reglas ya están cerradas con el negocio y son la fuente de verdad para modelar
`Domain/`, `BLL/Scoring/` y cualquier stored procedure de catálogo. No re-derivar
estas reglas desde el legacy ni asumir variantes sin confirmar primero.

### Jerarquía

```
Product (producto de más alto nivel — línea de manufactura)
  └─ Area (sublínea de ensamblaje dentro del Product)
       └─ [FUTURO] Operation — hoy las guías son generales por Area, no por Operation.
            Cada Area puede tener entre 3 y 10 Operations a futuro. El esquema debe
            dejar previsto este nivel (FK nullable) aunque hoy no se use.
```

**Estructura oficial de Products y Areas** (fuente de verdad; confirmada con negocio 2026-09-25):

| Product | Areas |
|---|---|
| **Wires** | Subensamble 1, Subensamble 2, Composite Core, Línea Final, Testing, Backend |
| **Catéteres** | Redefinity, EEP, Ice, PV 35, PV 18, Otw |
| **Sensores** | LoFlo, Capnotrack, Mainstream, Source, Reusables, Detector |
| **R&C** | Moldeo |

Un área se carga en BD recién cuando se migra su catálogo (sus flags son `NOT NULL` y
solo se conocen al validarla con negocio). Hoy existen en BD: Moldeo (catálogo real),
LoFlo, Redefinity y Subensamble 1 (flags y catálogos de ejemplo, `05_SeedData.sql`).

**Áreas objetivo del roadmap (en orden de migración):**

| Área | Product | Fase | Flags confirmados |
|---|---|---|---|
| **Moldeo** | R&C | Fase 2 ✅ | NonSerialized, RequiresAssembly=false, RequiresDataCollection=true |
| **LoFlo** | Sensores | Fase 7 | RequiresAssembly=true, RequiresDataCollection variable. Valida que el motor sea genérico. |

> **Notas de dominio:**
> - Redefinity (Catéteres) es un área real, pero sus flags, su guía y su catálogo en
>   `05_SeedData.sql` son **datos de ejemplo** (marcado "DATOS DE EJEMPLO — NO USAR EN
>   PRODUCCIÓN") para probar el camino Serialized en dev. No representan decisiones de negocio.
> - El catálogo real de R&C/Moldeo vive en `06_RCMoldeoCatalog.sql`.
> - En R&C/Moldeo el ID del molde (DCGTM_ID_MOLDE/A) es un **DataCollectionParameter**,
>   no Assembly. RequiresAssembly=false está confirmado con negocio.
> - Las 3 preguntas de LC Moldeo (DCGTM_LC_MOLDEO/A, capturadas dentro del Start)
>   NO son DataCollectionParameters — son un paso LineClearance adicional específico
>   del Start de R&C. El StepValidator (Fase 3) las gestiona hardcodeadas por área.

### Ejes de variación — son independientes entre sí, no se derivan uno del otro

- **SerializationType** (`Serialized | NonSerialized`): la diferencia aparece en el paso
  Search/Start Order — NO en Resource Setup. Resource Setup solo asocia el recurso
  (estación) a la Shop Order y las herramientas requeridas; es idéntico en ambos tipos.

  - `Serialized`: la Shop Order genera múltiples SFC individuales (ej. 989609001101-01,
    989609001101-02, 989609001101-03). Cada SFC tiene identidad y trazabilidad propia.
    El operador escanea SFCs individuales o selecciona varios desde Browse Work List.
    MES mantiene cada SFC como entidad independiente. Al scrapear: selección individual.
  - `NonSerialized`: la Shop Order genera un único SFC con una cantidad asociada (ej.
    SFC = 50012345, Qty = 500). No existen SFCs individuales tipo 50012345-001.
    El operador escanea o selecciona la Shop Order/SFC una vez. MES controla cantidad
    total, no unidades. Al scrapear: split de cantidad del lote.

  Afecta el modo de interacción en Search/Start y en el paso de Cierre/Scrap únicamente.
- **RequiresAssembly** (`bool`) y **RequiresDataCollection** (`bool`): flags
  independientes, hoy a nivel `Area` (con intención de moverse a `Operation` cuando
  exista esa granularidad). NO se derivan de `SerializationType` — una operación
  serializada puede no requerir Data Collection, y esto ya se confirmó con negocio.
  No asumir que "serializado implica Assembly" sin el flag explícito.
- El login a SAP NetWeaver NO es parte de la evaluación — es solo carga de escenario.

### Flujo estándar de una TrainingGuide (Apertura + Cierre)

Los primeros 3 pasos de Apertura y los 4 pasos de Cierre son siempre iguales para
todas las Areas/Products. Solo cambia el catálogo de datos (códigos NC, parámetros
de Data Collection), nunca la secuencia de tipos de paso:

```
Apertura:
  1. Resource Setup      — asocia el recurso (estación) a la Shop Order y las
                            herramientas/equipos requeridos para la operación.
                            Ejemplo: Resource = R_SCN_MASK_1,
                              Shop Order = 303406950, Tool = 10033058, Tool = 10087734.
                            NO define qué piezas se van a procesar — eso es Search/Start.
                            Captura libre (sin validar contra catálogo). Se califica
                            por SECUENCIA correcta y por Setup State = "Set Up"
                            (guardar en "Open" es intento fallido); los recursos
                            capturados no se califican.
  2. Line Clearance      — pregunta sí/no.
  3. Search/Start Order  — AQUÍ aparece la diferencia por SerializationType.
                            Se califica por SECUENCIA correcta (el operador debe saber
                            que este es el paso que sigue).
     ↳ Serialized:    la Shop Order tiene múltiples SFC individuales. El operador
                        escanea SFCs uno a uno o selecciona varios desde Browse
                        Work List y ejecuta Start. El POD carga las piezas
                        individuales seleccionadas. En el simulador (spec 008):
                        3-5 piezas por orden, Start/Assembly/DC/Complete trabajan
                        todas las piezas juntas (selección parcial = aviso sin
                        penalización) y TxtSFC muestra "[N SELECTED]".
     ↳ NonSerialized: la Shop Order tiene un único SFC con cantidad total (ej.
                        Qty=500). El operador escanea/selecciona ese SFC una vez
                        y ejecuta Start. El POD carga ese único SFC con su
                        cantidad.
  4. Assembly            — condicional a RequiresAssembly. La Component List es el
                            BOM de la operación (AssemblyComponents, simulado por
                            ahora). Cada componente se ensambla con un escaneo
                            numérico que cubre toda su Qty y se aplica a todas las
                            piezas seleccionadas ("Apply to all SFCs"). El paso se
                            completa con todo el BOM ensamblado. No valida el
                            barcode contra el número de parte; se califica solo por
                            secuencia (spec 008).
  5. Data Collection     — condicional a RequiresDataCollection. Parámetros
                            de catálogo por Area (ej. "posición del detector").

Cierre:
  6. Line Clearance      — igual que el de apertura.
  7. SFC Split           — paso independiente sólo en guías NonSerialized;
                            permite dividir filas del lote antes de continuar.
  8. Log NC/Scrap        — catálogo de códigos por Area.
  9. Complete            — cierra el proceso de manufactura.
```

### Modos de sesión (`TrainingSession.Mode`)

- **Guided**: reintentos ilimitados por paso hasta acertar. Se guarda un contador
  de intentos por paso (para reporte al instructor), pero no bloquea el avance ni
  aplica penalización — es práctica, no evaluación.
- **Assessment**: hay puntaje mínimo de aprobación. Cada paso debe completarse
  correctamente antes de desbloquear el siguiente (gate-keeping en tiempo real).
  Aplica penalización por reintento:

  ```
  PenaltyFactor(intento 1) = 1.00  (100%)
  PenaltyFactor(intento 2) = 0.80  (80%)
  PenaltyFactor(intento 3) = 0.60  (60%)
  PenaltyFactor(intento 4+) = 0.00 — el paso se marca fallido pero el sistema
                                      FUERZA el avance al siguiente paso (no
                                      debe quedar bloqueada la evaluación).

  StepScore = StepWeight × PenaltyFactor(intento_en_que_acertó)
  ```

  Esta fórmula aplica igual a pasos con contenido validable (Data Collection,
  NC/Scrap) y a pasos sin contenido (Resource Setup, Search/Navigation) — en
  estos últimos, cada intento fuera de secuencia cuenta como intento fallido.

### Ponderación de pasos (`StepWeight`) — fija, no varía por Area

```
Resource Setup:   20%
Line Clearance:   10%
Data Collection:  30%
Assembly:         15%
SFC Split:         5% (sólo NonSerialized)
NC/Scrap:         10%
Complete:         10%
```

### Catálogos que deben vivir en BD, nunca hardcodeados en código

- `NCCodes` — códigos de scrap/NC, varían por Area.
- `DataCollectionGroups` — DC Groups por Area (a futuro, por Operation) con `IsRequired`:
  una pieza completa Data Collection al guardar todos los grupos obligatorios; si el
  área no tiene ninguno, basta uno cualquiera (spec 008). Moldeo: solo
  `DCGTM_FECHA_EXPIRACION_NIR/A` es obligatorio.
- `DataCollectionParameters` — cada uno pertenece a un DC Group (`DataCollectionGroupId`).
- `AssemblyComponents` — BOM de la operación (hoy por Area, `OperationId` nullable):
  `PartNumber`/`Revision`, `Description`, `Qty` decimal. **Por ahora simulado.** No existe
  un conteo esperado de escaneos (se eliminó `ExpectedComponentCount`, spec 008).
- Flags de `Areas`: `SerializationType`, `RequiresAssembly`, `RequiresDataCollection`,
  `RequiresNCComponent` y `RequiresStartChecklist` (Start abre la secuencia de 4 modales;
  hoy solo Moldeo).

## Roadmap

- **Dónde quedamos:** al iniciar una sesión, leer primero `docs/roadmap.md` → sección **"📍 Estado actual"** (último commit, bloqueantes, deuda conocida y proceso acordado).
- **Historial completo** (objetivo + "No incluye" de cada fase): `docs/roadmap.md`. Las auditorías viven aparte, una por archivo en `docs/audits/` (el roadmap solo las referencia).
- **Reglas de oro:** cada fase tiene un alcance cerrado. No mezclar tareas de otras fases — la causa principal de deuda técnica temprana es que features de fases futuras se filtran en fases anteriores. Respetar siempre el "No incluye" de la fase en curso.
- **Completadas ✅:** 0 Setup · 1 Auth · 2 Dominio y catálogo de R&C · 3 Motor de TrainingGuide · 4 TrainingSession y modos · 6 UserControls por tipo de paso (cerrada 2026-09-24, specs 004-007) · 6.5 Fidelidad de UI/UX (cerrada 2026-10-02) · 6.6 Overlays guiados (cerrada 2026-10-07, spec 010). **Después de la 7:** 8 Validación interna · 9 Deploy del MVP.
- **Spec 008 cerrada ✅ (2026-09-28):** el motor soporta áreas Serialized (piezas con SFC propio, multi-selección, Start directo), Assembly con BOM y Data Collection por pieza con grupos obligatorios. Resolvió los bloqueantes #1, #2 y #4 de la Fase 7.
- **Auditoría del proyecto ✅ (2026-10-01/02):** lista única de 32 hallazgos (AUD-01…32), decisiones D-1…D-8 y 5 entregas verificadas (E1 bugs del flujo · E2 auth y secretos · E3 integridad del puntaje · E4 capas · E5 limpieza). Detalle y pendientes en `docs/audits/2026-10-01-auditoria.md`. AUD-16, 25 y 26 los cerró la spec 009. AUD-28/29 en la Fase 9.
- **6.5 · Fidelidad de UI/UX con el SAP MES real ✅** (cerrada 2026-10-02): cada pantalla y modal del POD replica su captura real (inventario `docs/ui-reference/inventario-pantallas.md`: ✅ 14 · ⚪ 2 sin captura).
- **Spec 009 · Motor del POD en la BLL cerrada ✅ (2026-10-05, 6 entregas):** `PodService` decide el flujo del POD (orden simulada, selección como intento o aviso, secuencia, avance forzado); la página solo pinta. Contexto una vez por request (Line Clearance: de ~21 a 5 conexiones). El estado del POD sigue en Session y, si se pierde, se reconstruye (D-10). Una sola sesión activa por operador (D-9). Cerró AUD-16, 25 y 26. Detalle: `docs/specs/009-motor-pod.md` y `009-plan.md`.
- **Auditoría final antes de la 6.6 ✅ (2026-10-06):** revisión de la spec 009 + regresión completa en el navegador. 12 hallazgos (AUD-33…44, 1 Medio: el logout), decisiones D-13…D-16 y 4 entregas verificadas (E1 logout · E2 invariantes de la BLL · E3 la página lee el flujo del contexto y los catálogos se cargan una vez por request · E4 limpieza). AUD-40/42 diferidos. Detalle: `docs/audits/2026-10-06-auditoria-final.md`.
- **Spec 010 · 6.6 Overlays guiados cerrada ✅ (2026-10-07, 5 entregas + E2b):** en Guided, guía clic por clic por los 9 pasos (siempre activa, bloqueante, solo avanza con la respuesta correcta, globo final); nada en Assessment. Solo presentación. De paso: en Serialized el POD vuelve sin selección tras Start, Assembly y DC. Detalle: `docs/specs/010-overlays-guiados.md` y `010-plan.md`. Diferidas: códigos NC comunes y capturas ⚪.
- **Ahora — 7 · Catálogo de LoFlo (segunda área) ⛔ bloqueada por el #3** (confirmar con negocio los flags reales de LoFlo). Script SQL con el catálogo real de LoFlo (Sensores): flags, NC Codes, DC Groups (con obligatorios) y parámetros, BOM (simulado) y guía. Solo carga de datos — cero código nuevo; valida que el motor es genérico. *No incluye:* cambios al motor, nuevas pantallas.
- **En pausa ⏸️ — 5 · ScoreCalculator: cobertura de tests.** Tests NUnit exhaustivos de `ScoreCalculator` (penalización, force-advance, pasos opcionales ausentes, completadas vs. abandonadas). Debe tener cobertura total antes de que usuarios reales generen puntajes. *No incluye:* nuevas features, cambios al algoritmo.

## Domain Glossary

Glosario de términos MES (SFC, LC, Shop Order, SFO, Employee vs. Operator, Product/Area/Operation, SerializationType, SessionMode, StepWeight…): `docs/glossary.md`.

## Architecture: N-Tier (3 capas)

```
MES Training Simulator/                    ← raíz del repositorio
├── TrainingApp/                           ← proyecto Web Forms (.NET FW 4.7.2)
│   ├── Pages/
│   │   ├── SessionHelper.cs               ← re-hidrata Session (vía AuthService), aviso de
│   │   │                                     cambio de contraseña del primer acceso y logout
│   │   ├── Auth/
│   │   │   ├── Login.aspx + .cs
│   │   │   └── ChangePassword.aspx + .cs
│   │   └── Training/
│   │       ├── StartSession.aspx + .cs    ← elegir Product → Area → Guía → modo
│   │       ├── GuidedTest.aspx + .cs      ← el POD: host de los controles de paso
│   │       ├── SessionPodStateStore.cs    ← IPodStateStore sobre la Session (spec 009)
│   │       ├── Guide/                     ← guía visual de Guided (spec 010): GuideScriptBuilder
│   │       │                                 (guion por tipo de paso y textos), GuideSubstep,
│   │       │                                 GuideMoment (qué dejó la página en pantalla)
│   │       └── Models/                    ← modelos de UI: StepViewModel (+ EventArgs),
│   │                                         IStepControl, PodWorkListRow
│   ├── UI/
│   │   ├── Controls/Training/             ← un UserControl por tipo de paso (spec 006):
│   │   │                                     ResourceSetup, LineClearance, Search (4 modales
│   │   │                                     Moldeo), Assembly, DataCollection, SFCSplit,
│   │   │                                     LogNC, Complete
│   │   └── master/                        ← MasterPages (heredan AntiXsrfMasterPage) + pod.css
│   │                                         + íconos + pod-guide.js/.css (motor de la guía,
│   │                                         solo en Guided)
│   ├── BLL/                               ← lógica de negocio, sin referencias a UI ni DAL directo
│   │   ├── Auth/
│   │   │   └── AuthService.cs
│   │   ├── Training/
│   │   │   ├── TrainingGuideService.cs    ← guías y catálogos (grupos DC, BOM, NC, Products)
│   │   │   ├── TrainingSessionService.cs  ← iniciar y abandonar sesiones
│   │   │   ├── PodService.cs              ← motor del POD (spec 009): LoadContext (una vez
│   │   │   │                                 por request) y comandos que registran intentos
│   │   │   │                                 (intento + avance atómico con spStepAttemptRecord);
│   │   │   │                                 arma el StepValidator con los catálogos del contexto
│   │   │   ├── PodContext.cs              ← sesión, guía, área, intentos, puntaje, estado y
│   │   │   │                                 catálogos del área (perezosos, una vez por request)
│   │   │   ├── PodCommandResult.cs        ← resultado de un comando: PodNotice, intento, scrap…
│   │   │   ├── StepSubmitResult.cs        ← resultado de un intento (outcome, intentos restantes)
│   │   │   ├── PodState.cs                ← estado del POD (orden + avance) e IPodStateStore
│   │   │   ├── PodDefaults.cs             ← datos de simulación (Shop Order por defecto, etc.)
│   │   │   ├── SimulatedOrder.cs (+Row)   ← orden simulada y sus operaciones (split, scrap…)
│   │   │   ├── StepAnswers.cs             ← answers fijos (complete, start, acción inválida)
│   │   │   ├── PieceSelectionRule.cs      ← qué filas exige cada paso (Serialized)
│   │   │   ├── SerialSfcGenerator.cs      ← 3-5 piezas por orden Serialized
│   │   │   ├── SplitSfcGenerator.cs       ← SFC nuevo de cada SFC Split (Fase 6.5)
│   │   │   ├── AssemblyProgress.cs        ← avance de Assembly (en PodState)
│   │   │   └── DataCollectionProgress.cs  ← avance de DC por pieza (en PodState)
│   │   └── Scoring/
│   │       ├── ScoreCalculator.cs         ← puntaje, penalización, umbral de aprobación
│   │       ├── StepValidator.cs           ← reglas de validación por tipo de paso
│   │       ├── StepValidationContext.cs   ← área y catálogos que usa el validador
│   │       └── DataCollectionCompletionRule.cs ← grupos DC obligatorios
│   ├── DAL/
│   │   ├── Infrastructure/
│   │   │   ├── Database.cs                ← wrapper ADO.NET (SqlConnection + SqlCommand)
│   │   │   └── DatabaseFactory.cs         ← lee connection string de Web.config
│   │   └── Repositories/                  ← uno por tabla: Employee (API, lista en memoria
│   │                                         con precarga y refresco), Operator, Product, Area,
│   │                                         TrainingGuide, TrainingSession, StepAttempt,
│   │                                         NCCode, DataCollectionGroup,
│   │                                         DataCollectionParameter, AssemblyComponent
│   ├── Domain/                            ← POCOs puros, sin dependencias
│   │   ├── Employee.cs, Operator.cs, Product.cs
│   │   ├── Area.cs                        ← SerializationType + flags (Requires*)
│   │   ├── TrainingGuide.cs, TrainingStep.cs (StepWeight)
│   │   ├── TrainingSession.cs (SessionMode), TrainingSessionStepAttempt.cs
│   │   ├── Operation.cs                   ← POCO mínimo, entidad FUTURA inactiva
│   │   ├── NCCode.cs, DataCollectionGroup.cs, DataCollectionParameter.cs
│   │   └── AssemblyComponent.cs           ← BOM (Qty decimal)
│   ├── Shared/
│   │   ├── Constants.cs                   ← DEFAULT_PASSWORD, SessionKeys
│   │   └── PasswordHelper.cs              ← hashing y comparación
│   └── Web.config
├── TrainingApp.Test/                      ← NUnit (.NET FW 4.7.2), tests de reglas BLL
├── Database/
│   └── StoredProcedures/                  ← 00 esquema · 01-04 SPs · 05 datos dev · 06-07 Moldeo
└── docs/                                  ← roadmap.md (estado actual), glossary.md, audits/,
                                              specs/ (NNN-*.md + NNN-plan.md), ui-reference/
```

### Regla de dependencias

```
Pages (code-behind)
    └── BLL (services)
            └── DAL (repositories)
                    └── Domain (POCOs)
```

Ninguna capa puede referenciar hacia arriba. Code-behind nunca llama a DAL directamente.

### Domain/ — cuándo usarlo

Solo entidades POCO puras. Sin lógica, sin atributos de ORM, sin referencias a System.Web ni ADO.NET. Si una clase tiene lógica que cruza features (scoring, validación de pasos), va en `BLL/Scoring/`.

**Colecciones de navegación:** `TrainingGuide` expone `List<TrainingStep> Steps` porque una guía sin pasos es un objeto incompleto para cualquier uso. El repositorio que cargue una `TrainingGuide` **debe** poblar `Steps` explícitamente (JOIN en el stored proc o segunda consulta). Nunca asumir que la lista está cargada si no fue pedida explícitamente.

Para relaciones que se componen situacionalmente (ej. `TrainingSession` + sus `TrainingSessionStepAttempt`), **no** se expone colección en el POCO — la BLL pide cada repositorio por separado y compone el resultado. Evita listas que podrían ser `null` silenciosamente.

## Auth Flow

1. Operario ingresa `code1` + contraseña en `Login.aspx`.
2. `AuthService` → `EmployeeRepository.GetByCode1(code1)` → lista de la API `Constants.EmployeeApiUrl` en memoria del proceso (se precarga al arrancar y se refresca en segundo plano cada `Constants.EmployeeCacheMinutes`: el login no espera a la API; si la API falla se sigue sirviendo la última lista; solo falla si nunca se pudo descargar).
3. `code1` no existe en la lista → error "Código de empleado no registrado".
4. Existe → buscar `Operator` local. **Sin registro (primer acceso):** contraseña debe ser `DEFAULT_PASSWORD` → crear `Operator` con `IsFirstLogin = true` → `ChangePassword.aspx` (si no, error). **Con registro:** validar contra `Operator.PasswordHash` (PBKDF2).
5. Operario cambia la contraseña (`AuthService.ChangePassword`: pide **siempre** la actual —en el primer acceso es la por defecto— y la nueva no puede ser la por defecto) → se guarda hasheada, `IsFirstLogin = false` → cuenta activada.
6. Login exitoso → `FormsAuthentication.SetAuthCookie(code1)` + `SessionHelper.SetOperatorSession` (`OperatorId`, `Code1`, `Name`, `IsFirstLogin`; la misma escritura al re-hidratar la Session). Mientras `IsFirstLogin`, `StartSession` y `GuidedTest` redirigen a `ChangePassword` (auditoría AUD-08).

## Tech Stack

- **Plataforma:** .NET Framework 4.7.2 · ASP.NET Web Forms (ASPX + code-behind) · ADO.NET puro + SQL Server stored procedures · Visual Studio 2019/2022
- **Auth/Sesión:** ASP.NET Forms Authentication · `HttpSessionState` (server-side) · PBKDF2 (`Rfc2898DeriveBytes`, en `PasswordHelper.cs`)
- **Empleados/Tests:** API `https://miappcr.com/api/basico/generalData/allUser/basicInfo` (refresco cada `Constants.EmployeeCacheMinutes` = 30 min, timeout `EmployeeApiTimeoutSeconds` = 10 s, en `EmployeeRepository`) · NUnit 4 (.NET FW 4.7.2)

## Key Conventions

- **Sin ORM** — toda consulta a DB pasa por stored procedures. Cero inline SQL en C#.
- **DAL devuelve POCOs** — los repositorios mapean `SqlDataReader` a entidades `Domain/`. Nunca devuelven `DataSet` ni `DataTable`.
- **Code-behind es solo UI** — el archivo `.aspx.cs` solo controla visibilidad de controles, binding de datos y redirecciones. Sin lógica de negocio, sin llamadas directas al DAL.
- **BLL valida, no el code-behind** — toda validación de reglas de negocio ocurre en los services.
- **Session solo desde el servidor** — `Session["OperatorId"]` etc. se escribe únicamente tras autenticación exitosa. Nunca se escribe desde JavaScript ni desde el cliente.
- **Contraseña por defecto en Constants.cs** — `Constants.DEFAULT_PASSWORD`. No en `Web.config`, no hardcodeada en páginas.
- **Connection string fuera del repo** — clave `"Default"`, leída por `DatabaseFactory`. Vive en `TrainingApp/connectionStrings.config` (`configSource` desde `Web.config`, en `.gitignore`); en un checkout nuevo se copia de `connectionStrings.config.example`. Nunca hardcodeada ni versionada (auditoría AUD-04).
- **La BLL protege sus invariantes** — `PodService.LoadContext` recibe el `operatorId` autenticado y solo devuelve contexto para una sesión propia y en curso; los comandos de `PodService` que registran intentos usan ese contexto (el paso actual lo define el servidor), y `spStepAttemptRecord` vuelve a validar en su transacción estado, paso y número de intento: si la sesión cambió, falla sin guardar nada. `TrainingSessionService.Abandon` recibe el `operatorId` y valida el dueño. Violarlos es una excepción, no un intento. El intento y el avance/cierre se guardan juntos (transacción). No confiar en que la página ya validó (auditoría AUD-05/06, spec 009).
- **Estado del POD solo en la BLL (spec 009)** — la orden simulada y el avance de Assembly/DC viven en un `PodState` en Session (una clave por sesión, `SessionPodStateStore`) y solo los modifica `PodService`. La página y los controles arman la UI con el `PodContext` del request y nunca tocan esas claves ni modifican la orden. Los avisos de formato llegan como `PodNotice` y la página elige el texto. Si el estado falta (Session reciclada), `PodService.LoadContext` lo reconstruye con la orden por defecto (D-10). Una sola sesión activa por operador: iniciar una guía (`PodService.StartSession`) o hacer logout (`SessionHelper.Logout`) abandona las `InProgress` (D-9), e iniciar una guía también descarta el estado del POD de las sesiones anteriores (AUD-41).
- **Seguridad en presentación** — toda página usa `Site.Master` o `Training.Master` (heredan `AntiXsrfMasterPage`, anti-CSRF). Texto que va a un `<script>` desde el servidor: `HttpUtility.JavaScriptStringEncode` o `JavaScriptSerializer`; data-binding de texto al HTML con `<%#: %>`. ASP.NET rechaza postbacks con marcado (`<x`): los controles lo bloquean como formato en el cliente (auditoría E2/E5).
- **URL de la API de empleados en `Constants.EmployeeApiUrl`** — nunca hardcodeada en el repositorio. `EmployeeRepository` guarda la lista en memoria del proceso (`_current`): `Global.Application_Start` la precarga en segundo plano (`AuthService.WarmUpEmployees`) y, cuando vence (`Constants.EmployeeCacheMinutes`, default 30 min), el siguiente login la sigue usando y dispara un refresco en segundo plano (uno a la vez). Si la API falla, se sigue sirviendo la última lista y se reintenta a los `EmployeeApiRetryMinutes`; cada descarga espera como máximo `EmployeeApiTimeoutSeconds`. La primera descarga es una sola aunque lleguen varios logins a la vez. Solo falla si nunca hubo una descarga exitosa (performance, 2026-10-07).
  - **Limitación conocida y aceptada:** `_current` es una variable en memoria del proceso de IIS. Si el App Pool se recicla (restart programado, idle timeout, crash) mientras la API externa está caída, la variable se pierde y el siguiente request fallará hasta que la API vuelva a responder. Este riesgo es aceptable dado el contexto (simulador interno de entrenamiento — no es un sistema de producción crítico). Si en el futuro se requiere persistencia ante reciclo, la lista cacheada debería guardarse en disco o en un caché distribuido.
- **Datos de simulación en stored procedures o scripts SQL** — preguntas LC, parámetros DataCollection, códigos NC van en `Database/StoredProcedures/`. No en arrays dentro de BLL ni en Web.config. Excepción aceptada (D-6 de la auditoría, 2026-10-02): la pregunta del paso Line Clearance ("¿La limpieza de linea se ejecuto?", en `GuidedTest`) es la misma en todas las operaciones y queda en el código.
- **Sin [WebMethod] para lógica de negocio** — si se necesita AJAX, usar un `IHttpHandler` (.ashx) o un endpoint mínimo. Nunca lógica de negocio en `[WebMethod]` de una página.
- **Formato vs. decisión en los controles de paso** — el control (JS + code-behind) solo bloquea errores de *formato* (campo requerido vacío, Shop Order no numérico, filas faltantes); no cuentan como intento. Toda *decisión* del operador (Sí/No, Setup State, código NC, valores de DC) se envía siempre al servidor y la califica `StepValidator`. Nunca rechazar una decisión en el control ni revelar la respuesta correcta en Assessment. Excepción aceptada: en Data Collection un campo vacío sí cuenta como intento fallido. En Guided, la guía visual (spec 010, enmienda R10) no deja avanzar con una decisión incorrecta: es presentación, el control y `StepValidator` no cambian y el servidor le manda las respuestas esperadas solo en Guided.
- **Guía visual de Guided (spec 010)** — es solo presentación: no decide nada del flujo ni guarda estado. En cada respuesta, solo en Guided, `GuidedTest.OnPreRender` arma el guion con `GuideScriptBuilder` (subpasos y textos, en el código) a partir del `PodContext`, y lo pasa con `JavaScriptSerializer` a `PodGuide.start`; Assessment no recibe ni el motor ni el guion (AC-2). Los handlers anotan el `GuideMoment` (abrió, incorrecto, avance, resuelto) que fija la entrada. Los elementos se anclan con `data-guide` (nunca ids). Un control o pantalla nueva del POD necesita sus anclas y su guion. Los mensajes del MES que se ocultan solos pasan por `podAutoHide` (Training.Master): esperan a la guía solo si ella los muestra (R9).
- **Controles de paso estáticos (spec 006)** — los UserControls de paso se declaran en `GuidedTest.aspx` y se alterna `Visible`; nunca `LoadControl` dinámico. Cerrado: las decisiones de arquitectura de `GuidedTest` están en `docs/roadmap.md` → Fase 6.
- **Nullable** — habilitar `<Nullable>enable</Nullable>` en csproj cuando sea posible, o usar comentarios de nullable si el proyecto no lo soporta. Evitar referencias nulas sin validar.

## Running Locally & Testing

- Correr local: abrir `TrainingApp.slnx` (VS 2019/2022) → crear `TrainingApp/connectionStrings.config` desde `connectionStrings.config.example` (SQL Server local) → ejecutar los scripts de `Database/StoredProcedures/` → F5 con IIS Express.
- `TrainingApp.Test/` — NUnit 4 (.NET FW 4.7.2). Integración DAL contra BD de test dedicada (LocalDB); Testcontainers descartado (fricción en FW 4.7.2).
- Unit tests BLL (services, scoring) con datos en memoria, sin DB. Las ASPX no se prueban (no hay `WebApplicationFactory` en FW).
- Correr tests: Visual Studio → Test > Run All Tests (Test Explorer).

### Build y validación (proceso usado desde la Fase 6)

1. **Rebuild completo** (el build incremental puede no recompilar; los warnings MSB3061 de `bin\roslyn` bloqueado por IIS Express/VS son inofensivos):
   `& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" "TrainingApp\TrainingApp.csproj" /t:Rebuild /p:Configuration=Debug /v:minimal /nologo "/clp:ErrorsOnly;Summary" /nowarn:MSB3061`
2. **Precompilar el markup**: MSBuild **no** compila `.aspx`/`.ascx` en un Web Application Project, así que un error de markup (`Register`, atributos, eventos declarativos) aparecería recién en runtime. Hay que precompilar siempre que se toque markup. Usar una carpeta temporal de ruta **corta** (con rutas largas falla por el límite de Windows, ej. `bootstrap...css.map`):
   `& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\aspnet_compiler.exe" -v / -p "<ruta>\TrainingApp" (Join-Path $env:TEMP "mespc")`
   `Web.config` tiene `optimizeCompilations="true"` (tras un rebuild IIS Express solo recompila el markup que cambió). Si después de cambiar la firma de algo que usa el markup aparece un error raro en runtime, borrar `%TEMP%\Temporary ASP.NET Files\vs`. Release lo quita.
3. **Compilar `TrainingApp.Test` y correr los tests** cuando cambie algo de la BLL o el Domain (hoy los tests cubren las reglas puras de BLL; no son criterio de aceptación, ver Fase 5 en pausa; en la auditoría 2026-10 el usuario los dejó fuera):
   `& "C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" "TrainingApp.Test\bin\Debug\TrainingApp.Test.dll"`
4. **Scripts SQL**: la conexión MCP a SQL Server es de **solo lectura**, así que los scripts de `Database/StoredProcedures/` los ejecuta el usuario, en orden numérico (`00`–`07`). No hay scripts de migración: los cambios de esquema van directo a `00_Schema.sql` y el `ALTER` para la BD existente se le entrega al usuario aparte (indicando si después hay que volver a correr algún script de SPs).
5. **Verificación en navegador por el usuario**: se le entrega un checklist por criterio de aceptación; el usuario prueba y confirma ("funciona") **antes de cada commit**. Nunca afirmar que la UI funciona sin esa confirmación.
6. **Commit** con mensaje que referencie la spec (`feat: ... (spec NNN)`) y la línea `Co-Authored-By` vigente.

## Skills Available

`/scaffold` — add a new feature slice · `/code-review` — multi-dimension review
