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

## Arquitectura objetivo

- El servidor es la única fuente de verdad para progreso, puntuaciones, estado de sesión, validaciones del flujo y resultados de evaluación.
- Las páginas ASPX no deben poder saltarse pasos ni modificar el estado real de negocio directamente.
- La lógica de negocio vive en la capa BLL — nunca en code-behind ni en stored procedures.
- Cada capa tiene una responsabilidad única y no viola la dirección de dependencias: Presentación → BLL → DAL → Domain.

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

**Áreas objetivo del roadmap (en orden de migración):**

| Área | Product | Fase | Flags confirmados |
|---|---|---|---|
| **Moldeo** | R&C | Fase 2 ✅ | NonSerialized, RequiresAssembly=false, RequiresDataCollection=true |
| **LoFlo** | Sensores | Fase 7 | RequiresAssembly=true, RequiresDataCollection variable. Valida que el motor sea genérico. |

> **Notas de dominio:**
> - "Redefinity" solo existe como nombre de ejemplo en `05_SeedData.sql`
>   (marcado "DATOS DE EJEMPLO — NO USAR EN PRODUCCIÓN"). No representa una decisión de negocio.
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
                            por SECUENCIA correcta, no por contenido.
  2. Line Clearance      — pregunta sí/no.
  3. Search/Start Order  — AQUÍ aparece la diferencia por SerializationType.
                            Se califica por SECUENCIA correcta (el operador debe saber
                            que este es el paso que sigue).
     ↳ Serialized:    la Shop Order tiene múltiples SFC individuales. El operador
                        escanea SFCs uno a uno o selecciona varios desde Browse
                        Work List y ejecuta Start. El POD carga las piezas
                        individuales seleccionadas.
     ↳ NonSerialized: la Shop Order tiene un único SFC con cantidad total (ej.
                        Qty=500). El operador escanea/selecciona ese SFC una vez
                        y ejecuta Start. El POD carga ese único SFC con su
                        cantidad.
  4. Assembly            — condicional a RequiresAssembly. Proceso genérico:
                            escanear y registrar N componentes (N = dato de
                            catálogo por Area). No valida contenido del
                            escaneo, solo que se complete el conteo esperado.
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
- `DataCollectionParameters` — varían por Area (y a futuro, por Operation).
- `ExpectedComponentCount` — cantidad de componentes a escanear en Assembly, por Area.

## Seguridad

- Proteger autenticación, autorización, integridad de progreso y validaciones del lado servidor.
- El nivel de seguridad es proporcional al contexto: simulador interno de entrenamiento.
- Las contraseñas nunca se almacenan en texto plano — hashing con **PBKDF2** (`Rfc2898DeriveBytes`, nativo en .NET FW 4.7.2). Confirmado como definitivo — reemplaza cualquier mención previa de SHA256+salt o BCrypt.
- La contraseña por defecto vive en `Shared/Constants.cs` — nunca en `Web.config` ni hardcodeada en páginas.
- El estado de sesión del operador (OperatorId, Code1, Name) se establece únicamente en el servidor tras autenticación exitosa.

## Dirección de desarrollo

- Analizar el legacy para extraer reglas y flujos, no arquitectura.
- Construir la nueva plataforma con separación clara de capas.
- Si una sección del tutorial puede reutilizarse tal cual, documentarlo explícitamente.
- Si una sección cambia de parámetros o de reglas, modelarla de forma nueva en el dominio.

## Roadmap

Cada fase tiene un alcance cerrado. No mezclar tareas de otras fases — la
causa principal de deuda técnica temprana es que features de fases futuras
se filtran en fases anteriores.

### Fase 0 — Setup ✅ (completada)
**Objetivo:** Proyecto Web Forms creado, N-Tier scaffolded, build verde.
**No incluye:** lógica de negocio, UI funcional, datos reales.

---

### Fase 1 — Auth ✅ (completada)
**Objetivo:** Login.aspx + ChangePassword.aspx funcionales, flujo completo
de primer acceso (API de empleados → Operator local → PBKDF2 → FormsAuth).
**No incluye:** Dashboard, GuidedTest, catálogo de áreas, ningún paso de entrenamiento.

---

### Fase 2 — Dominio y catálogo de R&C ✅ (completada)
**Objetivo:** Auditoría del legacy de R&C + script SQL con el catálogo real:
`Products` (R&C), `Areas` (Moldeo, NonSerialized, RequiresAssembly=false,
RequiresDataCollection=true), `NCCodes` (30 códigos), `DataCollectionParameters`
(15 parámetros: ID molde + 10 params MSS + verificación MSS + fecha kerosene +
2 first-shot). Catálogo en `06_RCMoldeoCatalog.sql`.
ExpectedComponentCount: N/A (RequiresAssembly=false).
`DataCollectionParameters` schema evolucionado: columnas `DataType`, `DisplayOrder`
añadidas; `MinValue`/`MaxValue` son ahora nullable (rangos pendientes de MSS D000281359).
**No incluye:** UI, motor de validación de pasos, TrainingGuideService,
ninguna pantalla de entrenamiento.

---

### Fase 3 — Motor de TrainingGuide ✅ (completada)
**Objetivo:** `TrainingGuideRepository` (dual result set puebla `Steps`),
`TrainingGuideService` con `BuildValidatorForGuide`, `StepValidator` completo
con constructor injection de `StepValidationContext` (Area + NCCodes + DCParams).
Validación implementada para todos los StepTypes: DataCollection (JSON por
param Id, tipado por DataType con InvariantCulture), LogNC (lookup en catálogo),
Assembly (count vs ExpectedComponentCount), LineClearance ("true"), pasos de
secuencia siempre true.
Nuevos repositorios: `AreaRepository`, `NCCodeRepository`,
`DataCollectionParameterRepository`. Nuevos SPs: `spAreaGetById`,
`spNCCodeGetByArea`, `spDataCollectionParameterGetByArea`.
Guía real de R&C/Moldeo publicada en `07_RCMoldeoGuide.sql` (8 pasos, pesos
suman 1.00; Search hereda peso 0.15 de Assembly ausente; 3 LC instancias con
pesos 0.04/0.03/0.03).
LC Moldeo modelada como paso LineClearance en el guide seed (step 4, weight 0.03);
la validación es idéntica al LC estándar — el contenido hardcodeado de sus 3
preguntas es responsabilidad de la UI (Fase 6).
`DataCollectionParameter` POCO evolucionado: añadidos `DataType`, `DisplayOrder`,
`MinValue`/`MaxValue` como `decimal?`.
Tests NUnit diferidos a Fase 5 (cobertura de ScoreCalculator y StepValidator).
**No incluye:** TrainingSession con gate-keeping real, persistencia de intentos
optimizada, UI, ScoreCalculator tests.

---

### Fase 4 — TrainingSession y modos ✅ (completada)
**Objetivo:** `TrainingSessionService` extendido con gate-keeping real Guided vs.
Assessment, `MemoryCache` de validator keyed por `AreaId` (TTL 60 min), método
`Abandon`, score en tiempo real (`CurrentScore` en `StepSubmitResult`).
`TrainingSessionRepository` extendido: `Abandon` + `DeleteActiveByOperator`.
Nuevos SPs: `spTrainingSessionAbandon`, `spTrainingSessionDeleteActiveByOperator`,
`spProductGetAll`, `spAreaGetByProduct`.
`ProductRepository` nuevo; `AreaRepository` extendido con `GetByProduct`.
`TrainingGuideService` extendido con `GetAllProducts` y `GetAreasByProduct`.
`StartSession.aspx` — selección Product → Area → Guide → Mode, inicia sesión.
`GuidedTest.aspx` — input genérico por paso, feedback + hint hardcodeado por
`StepType`, score en vivo, abandon con confirm JS, panel de completion.
Login redirige a `StartSession.aspx` tras autenticación exitosa.
Spec de referencia: `docs/specs/003-training-session-y-modos.md` (Approved 2026-09-04).
**No incluye:** UserControls por tipo de paso, ScoreCalculator tests, LoFlo.

---

### Fase 5 — ScoreCalculator: cobertura de tests
**Objetivo:** `ScoreCalculator` ya está implementado. Esta fase lo blinda
con tests NUnit exhaustivos: todos los casos de penalización, force-advance,
pasos opcionales ausentes, sesiones completadas vs. abandonadas.
Es el componente más sensible del sistema — debe tener cobertura total
antes de que usuarios reales generen puntajes.
**No incluye:** nuevas features, cambios al algoritmo.

---

### Fase 6 — UserControls por tipo de paso
**Objetivo:** UserControls `.ascx` reutilizables para cada `StepType`:
`ResourceSetup`, `LineClearance`, `SearchOrder`, `Assembly`,
`DataCollection`, `NCScrap`, `Complete`. Cada control recibe el estado
del servidor y emite un submit — sin lógica de negocio interna.
**No incluye:** catálogo de LoFlo, nuevas áreas.

---

### Fase 7 — Catálogo de LoFlo (segunda área)
**Objetivo:** Script SQL con el catálogo real de LoFlo (Sensores).
Solo carga de datos — cero código nuevo. Valida que el motor de Fases 3-4
es verdaderamente genérico y que `RequiresAssembly`/`RequiresDataCollection`
funcionan con la configuración real de LoFlo.
**No incluye:** cambios al motor, nuevas pantallas.

---

### Fase 8 — Validación interna
**Objetivo:** Sesión con trainer/operador real sobre R&C y LoFlo.
Correcciones de UX, bugs de flujo, ajustes de catálogo.
**No incluye:** nuevas áreas, Dashboard admin, reportes.

---

### Fase 9 — Deploy del MVP
**Objetivo:** Deploy en servidor interno con IIS. Configuración de
connection string de producción, App Pool, permisos, prueba de humo.
**No incluye:** CI/CD automatizado, monitoreo avanzado.

---

## Domain Glossary

Términos del dominio MES que aplican tanto al legacy como a la nueva plataforma.

| Término | Descripción |
|---|---|
| **SFC** | Shop Floor Control — unidad de trabajo que el operador abre y ejecuta (equivale a una orden de manufactura en piso) |
| **LC** | Line Clearance — verificación de limpieza de línea; serie de preguntas booleanas que el operador debe responder antes de iniciar |
| **Resource Setup** | Asocia el recurso (estación de trabajo) a la Shop Order y las herramientas/equipos requeridos para la operación. No define qué piezas se procesan — eso ocurre en Search/Start. Ejemplo: Resource = R_SCN_MASK_1, Shop Order = 303406950, Tool = 10033058, Tool = 10087734. |
| **DataCollection** | Captura de parámetros de proceso (temperatura, tiempo, presión, etc.) agrupados por tipo de proceso |
| **NC / Scrap** | Registro de material no conforme o scrap con código de causa |
| **TrainingGuide** | Plantilla de pasos ejecutables que define el flujo del operador: qué pasos, en qué orden, con qué validaciones. Es inmutable una vez publicada. |
| **TrainingSession** | Instancia activa de una TrainingGuide para un operador específico. Contiene estado en tiempo real: paso actual, respuestas, tiempo. Al completarse guarda el puntaje final. |
| **Guided Test** | Flujo de entrenamiento secuencial con pasos validados por el servidor; el operador no puede avanzar sin completar el paso actual |
| **Shop Order** | Orden de fabricación asociada a una línea y producto; identificador principal del contexto de trabajo |
| **SFO** | Shop Floor Order — sinónimo de Shop Order en algunos contextos del legacy |
| **Employee** | Registro de empleado obtenido de la API pública de la empresa (`Constants.EmployeeApiUrl`). Controla quién puede acceder al sistema — si el `code1` existe en la lista devuelta por la API, el empleado es válido. No se almacena localmente en la BD. La lista se cachea en memoria con fallback indefinido ante fallas de la API. |
| **Operator** | Empleado que ya completó el primer login y cambió su contraseña. Tiene cuenta activa en el sistema. |
| **Product** | Línea de producto de más alto nivel: Wires, Catéteres o Sensores. |
| **Area** | Sublínea de ensamblaje dentro de un Product (ej. R&C, LoFlo). Unidad a la que hoy se asocia una TrainingGuide general. Las áreas reales del roadmap son R&C (Fase 2) y LoFlo/Sensores (Fase 7). |
| **Operation** | [Futuro] Subdivisión de una Area — cada Area puede tener de 3 a 10 Operations, cada una con sus propios pasos específicos. Hoy no se usa para generar guías distintas, pero el esquema debe preverlo. |
| **SerializationType** | Determina cómo la Shop Order genera SFCs en Search/Start y cómo se scrappea en Cierre. `Serialized`: la Shop Order genera múltiples SFC individuales con identidad propia (ej. 989609001101-01, -02, -03); el operador puede procesar y scrapear unidades individualmente. `NonSerialized`: la Shop Order genera un único SFC con cantidad total (ej. SFC = 50012345, Qty = 500); MES controla cantidad total, no unidades. La diferencia NO aplica a Resource Setup — ese paso es idéntico en ambos tipos. |
| **SessionMode** | `Guided` (reintentos ilimitados, sin penalización, práctica) o `Assessment` (puntaje mínimo de aprobación, penalización por reintento). |
| **StepWeight** | Ponderación fija de cada tipo de paso sobre el puntaje final (no varía por Area). |

## Architecture: N-Tier (3 capas)

```
MES Training Simulator/                    ← raíz del repositorio
├── TrainingApp/                           ← proyecto Web Forms (.NET FW 4.7.2)
│   ├── Pages/
│   │   ├── Auth/
│   │   │   ├── Login.aspx + .cs
│   │   │   └── ChangePassword.aspx + .cs
│   │   ├── Training/
│   │   │   ├── Dashboard.aspx + .cs
│   │   │   └── GuidedTest.aspx + .cs
│   │   └── Admin/
│   │       └── ManageGuides.aspx + .cs
│   ├── BLL/                               ← lógica de negocio, sin referencias a UI ni DAL directo
│   │   ├── Auth/
│   │   │   └── AuthService.cs
│   │   ├── Training/
│   │   │   ├── TrainingGuideService.cs
│   │   │   └── TrainingSessionService.cs
│   │   └── Scoring/
│   │       ├── ScoreCalculator.cs         ← lógica de puntaje (compartida)
│   │       └── StepValidator.cs           ← reglas de validación del Guided Test
│   ├── DAL/
│   │   ├── Infrastructure/
│   │   │   ├── Database.cs                ← wrapper ADO.NET (SqlConnection + SqlCommand)
│   │   │   └── DatabaseFactory.cs         ← lee connection string de Web.config
│   │   └── Repositories/
│   │       ├── EmployeeRepository.cs      ← HTTP client + MemoryCache + _lastKnownGood
│   │       ├── OperatorRepository.cs
│   │       ├── TrainingGuideRepository.cs
│   │       ├── TrainingSessionRepository.cs
│   │       └── StepAttemptRepository.cs
│   ├── Domain/                            ← POCOs puros, sin dependencias
│   │   ├── Employee.cs
│   │   ├── Operator.cs
│   │   ├── Product.cs
│   │   ├── Area.cs                        ← incluye SerializationType, RequiresAssembly,
│   │   │                                     RequiresDataCollection
│   │   ├── TrainingGuide.cs
│   │   ├── TrainingStep.cs                ← incluye StepWeight
│   │   ├── TrainingSession.cs             ← incluye SessionMode (Guided | Assessment)
│   │   ├── TrainingSessionStepAttempt.cs  ← SessionId, StepId, AttemptNumber, IsCorrect, Timestamp
│   │   ├── Operation.cs                   ← POCO mínimo, entidad FUTURA inactiva (hoy no se usa)
│   │   ├── NCCode.cs
│   │   └── DataCollectionParameter.cs
│   ├── Shared/
│   │   ├── Constants.cs                   ← DEFAULT_PASSWORD, SESSION_KEYS
│   │   └── PasswordHelper.cs              ← hashing y comparación
│   ├── UI/
│   │   └── master/                        ← MasterPages compartidas
│   └── Web.config
├── TrainingApp.Test/                      ← NUnit (.NET FW 4.7.2)
└── Database/
    └── StoredProcedures/                  ← scripts SQL de todos los stored procedures
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

1. Operario ingresa `code1` + contraseña en `Login.aspx`
2. `AuthService` llama a `EmployeeRepository.GetByCode1(code1)`, que consulta la
   **API pública** `Constants.EmployeeApiUrl`. La lista se cachea en memoria
   (`MemoryCache`) durante `Constants.EmployeeCacheMinutes`. Si la API falla tras el
   TTL, se sirve la **última lista exitosa conocida indefinidamente** hasta que la API
   vuelva a responder. Solo falla el login si la API nunca fue alcanzable (primer
   arranque con API caída — no hay nada en caché).
3. Si el `code1` no existe en la lista → error "Código de empleado no registrado"
4. Si existe → buscar registro `Operator` en la BD local:
   - **Sin registro local (primer acceso):** validar que la contraseña sea `DEFAULT_PASSWORD`. Si es correcta → crear `Operator` con `IsFirstLogin = true` → redirigir a `ChangePassword.aspx`. Si es incorrecta → error.
   - **Con registro local:** validar contra `Operator.PasswordHash` (PBKDF2)
5. Operario cambia la contraseña → se guarda hasheada, `IsFirstLogin = false` → cuenta activada
6. Login exitoso → `FormsAuthentication.SetAuthCookie(code1)` + `Session` con `OperatorId`, `Code1`, `Name`

## Tech Stack

| Concern | Tecnología |
|---|---|
| Framework | .NET Framework 4.7.2 |
| UI | ASP.NET Web Forms (ASPX + code-behind) |
| Data Access | ADO.NET puro + SQL Server stored procedures |
| Auth | ASP.NET Forms Authentication |
| Sesión servidor | `HttpSessionState` (ASP.NET Session — server-side) |
| Validación empleados | API pública `https://miappcr.com/api/basico/generalData/allUser/basicInfo` — TTL de caché en `Constants.EmployeeCacheMinutes` (30 min). Si la API falla tras el TTL, se sirve la última lista exitosa indefinidamente (`_lastKnownGood` en `EmployeeRepository`). |
| Hashing | PBKDF2 (`Rfc2898DeriveBytes`, en `PasswordHelper.cs`) |
| Tests | NUnit 4 (.NET FW 4.7.2) |
| Proyecto creado con | Visual Studio 2019/2022 |

Confirmado como definitivo: NUnit + base de datos de test dedicada (LocalDB) para
integración. Se descartó Testcontainers por compatibilidad limitada y mayor
fricción de setup en .NET Framework 4.7.2 — NUnit es la opción más simple de
implementar en este entorno, que es el criterio priorizado.

## Key Conventions

- **Sin ORM** — toda consulta a DB pasa por stored procedures. Cero inline SQL en C#.
- **DAL devuelve POCOs** — los repositorios mapean `SqlDataReader` a entidades `Domain/`. Nunca devuelven `DataSet` ni `DataTable`.
- **Code-behind es solo UI** — el archivo `.aspx.cs` solo controla visibilidad de controles, binding de datos y redirecciones. Sin lógica de negocio, sin llamadas directas al DAL.
- **BLL valida, no el code-behind** — toda validación de reglas de negocio ocurre en los services.
- **Session solo desde el servidor** — `Session["OperatorId"]` etc. se escribe únicamente tras autenticación exitosa. Nunca se escribe desde JavaScript ni desde el cliente.
- **Contraseña por defecto en Constants.cs** — `Constants.DEFAULT_PASSWORD`. No en `Web.config`, no hardcodeada en páginas.
- **Connection string en Web.config** — clave `"Default"`, leída por `DatabaseFactory`. Nunca hardcodeada.
- **URL de la API de empleados en `Constants.EmployeeApiUrl`** — nunca hardcodeada en el repositorio. TTL de caché en `Constants.EmployeeCacheMinutes` (default 30 min). Ante falla de la API tras el TTL, `EmployeeRepository` sirve la última lista exitosa indefinidamente (`_lastKnownGood`). Solo falla si nunca hubo una llamada exitosa.
  - **Limitación conocida y aceptada:** `_lastKnownGood` es una variable en memoria del proceso de IIS. Si el App Pool se recicla (restart programado, idle timeout, crash) mientras la API externa está caída, la variable se pierde y el siguiente request fallará hasta que la API vuelva a responder. Este riesgo es aceptable dado el contexto (simulador interno de entrenamiento — no es un sistema de producción crítico). Si en el futuro se requiere persistencia ante reciclo, la lista cacheada debería guardarse en disco o en un caché distribuido.
- **Datos de simulación en stored procedures o scripts SQL** — preguntas LC, parámetros DataCollection, códigos NC van en `Database/StoredProcedures/`. No en arrays dentro de BLL ni en Web.config.
- **Sin [WebMethod] para lógica de negocio** — si se necesita AJAX, usar un `IHttpHandler` (.ashx) o un endpoint mínimo. Nunca lógica de negocio en `[WebMethod]` de una página.
- **Nullable** — habilitar `<Nullable>enable</Nullable>` en csproj cuando sea posible, o usar comentarios de nullable si el proyecto no lo soporta. Evitar referencias nulas sin validar.

## Running Locally

1. Abrir `TrainingApp.slnx` en Visual Studio 2019/2022
2. Configurar la connection string en `Web.config` (clave `"Default"`, apuntando a SQL Server local)
3. Ejecutar los scripts en `Database/StoredProcedures/` contra la base de datos
4. Correr con IIS Express (F5 desde Visual Studio)

## Testing

Proyecto `TrainingApp.Test` con NUnit 4 (.NET FW 4.7.2).

- Tests unitarios para BLL: services y scoring con datos en memoria (sin DB)
- Tests de integración para DAL: contra una base de datos de prueba local
- No existe `WebApplicationFactory` en .NET FW — las páginas ASPX no se prueban directamente

```
Test > Run All Tests   (desde Visual Studio Test Explorer)
```

## Skills Available

- `/scaffold` — add a new feature slice
- `/code-review` — multi-dimension review