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
- **Historial completo** (objetivo + "No incluye" de cada fase): `docs/roadmap.md`.
- **Reglas de oro:** cada fase tiene un alcance cerrado. No mezclar tareas de otras fases — la causa principal de deuda técnica temprana es que features de fases futuras se filtran en fases anteriores. Respetar siempre el "No incluye" de la fase en curso.
- **Completadas ✅:** 0 Setup · 1 Auth · 2 Dominio y catálogo de R&C · 3 Motor de TrainingGuide · 4 TrainingSession y modos · 6 UserControls por tipo de paso (cerrada 2026-09-24, specs 004-007). **Después de la 7:** 8 Validación interna · 9 Deploy del MVP.
- **Ahora — bloqueantes de la Fase 7.** Antes de cargar LoFlo: (1) spec del Start de Serialized (una fila por pieza con SFC propio, txtSFC por fila, multi-selección); (2) Assembly completable (`ExpectedComponentCount` en BD + formato del Answer); (3) confirmar con negocio los flags reales de LoFlo. Detalle en `docs/roadmap.md`.
- **Siguiente — 7 · Catálogo de LoFlo (segunda área) ⛔ bloqueada.** Script SQL con el catálogo real de LoFlo (Sensores). Solo carga de datos — cero código nuevo; valida que el motor de Fases 3-4 es genérico y que `RequiresAssembly`/`RequiresDataCollection` funcionan con la configuración real. *No incluye:* cambios al motor, nuevas pantallas.
- **En pausa ⏸️ — 5 · ScoreCalculator: cobertura de tests.** Tests NUnit exhaustivos de `ScoreCalculator` (penalización, force-advance, pasos opcionales ausentes, completadas vs. abandonadas). Debe tener cobertura total antes de que usuarios reales generen puntajes. *No incluye:* nuevas features, cambios al algoritmo.

## Domain Glossary

Glosario de términos MES (SFC, LC, Shop Order, SFO, Employee vs. Operator, Product/Area/Operation, SerializationType, SessionMode, StepWeight…): `docs/glossary.md`.

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

1. Operario ingresa `code1` + contraseña en `Login.aspx`.
2. `AuthService` → `EmployeeRepository.GetByCode1(code1)` → API `Constants.EmployeeApiUrl` (`MemoryCache` por `Constants.EmployeeCacheMinutes`; si la API falla tras el TTL sirve la última lista exitosa indefinidamente; solo falla si la API nunca fue alcanzable).
3. `code1` no existe en la lista → error "Código de empleado no registrado".
4. Existe → buscar `Operator` local. **Sin registro (primer acceso):** contraseña debe ser `DEFAULT_PASSWORD` → crear `Operator` con `IsFirstLogin = true` → `ChangePassword.aspx` (si no, error). **Con registro:** validar contra `Operator.PasswordHash` (PBKDF2).
5. Operario cambia la contraseña → se guarda hasheada, `IsFirstLogin = false` → cuenta activada.
6. Login exitoso → `FormsAuthentication.SetAuthCookie(code1)` + `Session` con `OperatorId`, `Code1`, `Name`.

## Tech Stack

- **Plataforma:** .NET Framework 4.7.2 · ASP.NET Web Forms (ASPX + code-behind) · ADO.NET puro + SQL Server stored procedures · Visual Studio 2019/2022
- **Auth/Sesión:** ASP.NET Forms Authentication · `HttpSessionState` (server-side) · PBKDF2 (`Rfc2898DeriveBytes`, en `PasswordHelper.cs`)
- **Empleados/Tests:** API `https://miappcr.com/api/basico/generalData/allUser/basicInfo` (TTL `Constants.EmployeeCacheMinutes` = 30 min, fallback `_lastKnownGood` en `EmployeeRepository`) · NUnit 4 (.NET FW 4.7.2)

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
- **Formato vs. decisión en los controles de paso** — el control (JS + code-behind) solo bloquea errores de *formato* (campo requerido vacío, Shop Order no numérico, filas faltantes); no cuentan como intento. Toda *decisión* del operador (Sí/No, Setup State, código NC, valores de DC) se envía siempre al servidor y la califica `StepValidator`. Nunca rechazar una decisión en el control ni revelar la respuesta correcta en Assessment. Excepción aceptada: en Data Collection un campo vacío sí cuenta como intento fallido.
- **Controles de paso estáticos (spec 006)** — los UserControls de paso se declaran en `GuidedTest.aspx` y se alterna `Visible`; nunca `LoadControl` dinámico. Cerrado: las decisiones de arquitectura de `GuidedTest` están en `docs/roadmap.md` → Fase 6.
- **Nullable** — habilitar `<Nullable>enable</Nullable>` en csproj cuando sea posible, o usar comentarios de nullable si el proyecto no lo soporta. Evitar referencias nulas sin validar.

## Running Locally & Testing

- Correr local: abrir `TrainingApp.slnx` (VS 2019/2022) → connection string `"Default"` en `Web.config` (SQL Server local) → ejecutar los scripts de `Database/StoredProcedures/` → F5 con IIS Express.
- `TrainingApp.Test/` — NUnit 4 (.NET FW 4.7.2). Integración DAL contra BD de test dedicada (LocalDB); Testcontainers descartado (fricción en FW 4.7.2).
- Unit tests BLL (services, scoring) con datos en memoria, sin DB. Las ASPX no se prueban (no hay `WebApplicationFactory` en FW).
- Correr tests: Visual Studio → Test > Run All Tests (Test Explorer).

### Build y validación (proceso usado desde la Fase 6)

1. **Rebuild completo** (el build incremental puede no recompilar; los warnings MSB3061 de `bin\roslyn` bloqueado por IIS Express/VS son inofensivos):
   `& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" "TrainingApp\TrainingApp.csproj" /t:Rebuild /p:Configuration=Debug /v:minimal /nologo "/clp:ErrorsOnly;Summary" /nowarn:MSB3061`
2. **Precompilar el markup**: MSBuild **no** compila `.aspx`/`.ascx` en un Web Application Project, así que un error de markup (`Register`, atributos, eventos declarativos) aparecería recién en runtime. Hay que precompilar siempre que se toque markup:
   `& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\aspnet_compiler.exe" -v / -p "<ruta>\TrainingApp" "<carpeta temporal>"`
3. **Compilar `TrainingApp.Test`** cuando cambie algo de la BLL o el Domain (hoy los tests no se ejecutan como criterio de aceptación; ver Fase 5 en pausa).
4. **Scripts SQL**: la conexión MCP a SQL Server es de **solo lectura**, así que los scripts de `Database/StoredProcedures/` los ejecuta el usuario, en orden numérico (`00`–`07`). No hay scripts de migración: los cambios de esquema van directo a `00_Schema.sql` y el `ALTER` para la BD existente se le entrega al usuario aparte (indicando si después hay que volver a correr algún script de SPs).
5. **Verificación en navegador por el usuario**: se le entrega un checklist por criterio de aceptación; el usuario prueba y confirma ("funciona") **antes de cada commit**. Nunca afirmar que la UI funciona sin esa confirmación.
6. **Commit** con mensaje que referencie la spec (`feat: ... (spec NNN)`) y la línea `Co-Authored-By` vigente.

## Skills Available

`/scaffold` — add a new feature slice · `/code-review` — multi-dimension review
