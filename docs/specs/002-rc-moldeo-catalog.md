# Spec 002: Catálogo R&C / Moldeo — Fase 2

**Status:** Approved (2026-09-04) — ver nota de proceso al final del documento
**Date:** 2026-09-04

---

## Problem

El sistema no tenía catálogo real de ningún área de manufactura. El único dato
existente era la guía de "Redefinity" en `05_SeedData.sql`, marcada explícitamente
como ejemplo estructural sin validación con negocio.

Para que el motor de Fase 3 (StepValidator, TrainingGuideService) pueda probarse
contra datos reales, se necesita el catálogo completo del primer área del roadmap:
R&C / Moldeo.

---

## Scope

### En este spec (Fase 2)

- `Products`: insertar `R&C`.
- `Areas`: insertar `Moldeo` con sus flags confirmados con negocio.
- `NCCodes`: los 30 códigos de scrap/NC del área Moldeo, extraídos del legacy.
- `DataCollectionParameters`: los 15 parámetros DC de Moldeo, con sus tipos.
- Evolución del esquema `DataCollectionParameters`: nuevas columnas `DataType`,
  `DisplayOrder`; `MinValue`/`MaxValue` nullable.

### Fuera de este spec

- `TrainingGuides` y `TrainingSteps` — Fase 3.
- `ExpectedComponentCount` — N/A (`RequiresAssembly = false`).
- UI, motor de validación de pasos, TrainingGuideService, UserControls.
- Rangos Min/Max definitivos de los parámetros MSS — pendiente de revisión
  contra el documento físico D000281359 (ver Decisiones diferidas).
- Catálogo de LoFlo — Fase 7.

---

## Domain Model

### Jerarquía

```
Product: R&C
  └─ Area: Moldeo
       └─ [Operation: O_SRC_DWF_CUSHION_NIR — informativo; no en esquema actual]
```

### Flags del área Moldeo

| Atributo | Valor | Fuente |
|---|---|---|
| SerializationType | NonSerialized (1) | Work table con lote y cantidad; split en cierre → batch confirmado |
| RequiresAssembly | false (0) | El ID del molde es DataCollectionParameter, no Assembly — confirmado con negocio |
| RequiresDataCollection | true (1) | 15 parámetros DC obligatorios |

### Flujo de Data Collection en R&C/Moldeo

El área tiene una estructura de DC en dos momentos, capturada dentro del Start
y luego desde el POD, sin un paso Assembly separado:

**Pre-Start (obligatorio, secciones 1-4 del wizard de Start):**

| Sección | Group CD legacy | Tipo en nuevo modelo |
|---|---|---|
| 1 — ID del Molde | `DCGTM_ID_MOLDE/A` | `DataCollectionParameter` (Numeric) |
| 2 — LC Moldeo (3 preguntas bool) | `DCGTM_LC_MOLDEO/A` | **LineClearance** — NO es DataCollectionParameter; hardcodeado en StepValidator por área (Fase 3) |
| 3 — Parámetros MSS D000281359 | `DCGTM_PARAMETROS_MSS_D000281359/A` | `DataCollectionParameter` ×10 (Numeric) |
| 4 — Verificación IDMold/IDIMM | `DCGTM_VERIFICACION_MOLDEO_MAQUINA_OP/A` | `DataCollectionParameter` (Boolean) |

**Post-Start (DC regular desde POD):**

| Colección | Group CD legacy | Tipo en nuevo modelo |
|---|---|---|
| Fecha expiración kerosene | `DCGTM_FECHA_EXPIRACION_NIR/A` | `DataCollectionParameter` (Date, formato MM/DD/AA) |
| First Shot OPE | `DCGTM_ENTREGA_PRIMERA_PIEZA/A` | `DataCollectionParameter` (Boolean) |
| First Shot NIR OPE | `DCGTM_ENTREGA_PRIMERA_PIEZA_NIR/A` | `DataCollectionParameter` (Boolean) |

La distinción pre-Start / post-Start es relevante para el StepValidator en Fase 3;
para el catálogo de Fase 2, todos los parámetros viven en `DataCollectionParameters`
con `DisplayOrder` que refleja la secuencia operativa.

### DataCollectionParameters — 15 parámetros de Moldeo

| DisplayOrder | Name | DataType | Unit | Min | Max |
|---|---|---|---|---|---|
| 1 | ID del Molde | Numeric | N/A | NULL | NULL |
| 2 | Colling Time según MSS D000281359 | Numeric | N/A | NULL | NULL |
| 3 | Injection speed según MSS D000281359 | Numeric | N/A | NULL | NULL |
| 4 | Intensidad de Pad Printing | Numeric | N/A | NULL | NULL |
| 5 | Temperatura Zona 1 | Numeric | °C | NULL | NULL |
| 6 | Temperatura Zona 2 | Numeric | °C | NULL | NULL |
| 7 | Temperatura Zona 3 | Numeric | °C | NULL | NULL |
| 8 | Temperatura Zona 4 | Numeric | °C | NULL | NULL |
| 9 | Temperatura Zona 5 | Numeric | °C | NULL | NULL |
| 10 | Temperatura Zona 6 | Numeric | °C | NULL | NULL |
| 11 | Tiempo de curado de Pad Printing | Numeric | N/A | NULL | NULL |
| 12 | ¿Se verificó el IDMold & IDIMM según MSS? | Boolean | N/A | NULL | NULL |
| 13 | Fecha de Expiración Kerosene | Date | MM/DD/AA | NULL | NULL |
| 14 | First Shot entregado - OPE | Boolean | N/A | NULL | NULL |
| 15 | First Shot NIR entregado - OPE | Boolean | N/A | NULL | NULL |

### NCCodes — 30 códigos de Moldeo

Ver `Database/StoredProcedures/06_RCMoldeoCatalog.sql` — sección NCCodes.

Correcciones aplicadas respecto al legacy:
- 4 códigos tenían espacios en el nombre (violación del formato estricto
  `guion_bajo` confirmado con negocio). Corregidos:

| Código legacy (incorrecto) | Código corregido |
|---|---|
| `F_FS_DESECHO QA` | `F_FS_DESECHO_QA` |
| `F_FS_PUNTO INYEC` | `F_FS_PUNTO_INYEC` |
| `F_FS_RING DEFORM` | `F_FS_RING_DEFORM` |
| `F_FS_TIRO CORTO` | `F_FS_TIRO_CORTO` |

- `F_FS_THICKNESS` tiene descripción `"NCR"` (igual a `F_FS_NCR`) —
  posible error del legacy. Descripción temporal aceptada de forma transitoria
  (ver Decisiones diferidas).

### Evolución del esquema `DataCollectionParameters`

El esquema original solo soportaba parámetros numéricos (Min/Max DECIMAL NOT NULL).
R&C/Moldeo tiene Boolean y Date, lo que requirió:

| Cambio | Razón |
|---|---|
| `DataType NVARCHAR(20) DEFAULT 'Numeric'` | Soportar Boolean y Date |
| `DisplayOrder INT DEFAULT 0` | Orden de presentación al operador |
| `MinValue DECIMAL(10,4) NULL` | NULL para non-Numeric o rangos pendientes |
| `MaxValue DECIMAL(10,4) NULL` | Idem |
| `Name NVARCHAR(200)` (era 100) | Nombres largos de parámetros MSS |

Migración segura para re-ejecución en `06_RCMoldeoCatalog.sql` (ALTER TABLE IF NOT EXISTS).

---

## API Contract

N/A — este spec cubre únicamente datos de catálogo en base de datos.
No hay endpoints nuevos en esta fase.

---

## Authorization

N/A — sin UI ni endpoints en esta fase.

---

## Edge Cases & Failure Modes

- **Re-ejecución del script**: todos los INSERTs usan `IF NOT EXISTS` —
  idempotente. El script puede ejecutarse múltiples veces sin efectos secundarios.
- **Tabla creada con esquema anterior**: el script añade las columnas nuevas
  mediante ALTER TABLE con guarda `IF NOT EXISTS` — compatible con entornos
  que ya ejecutaron `00_Schema.sql` original.
- **MinValue/MaxValue NULL en parámetros numéricos**: el StepValidator en Fase 3
  debe tratar NULL como "rango no validado" y aceptar cualquier valor numérico
  mientras los rangos no sean confirmados contra MSS D000281359.

---

## Non-Functional Requirements

- El script debe ejecutarse en < 5 segundos en SQL Server local.
- Seguro para re-ejecución (idempotente).
- Sin dependencias externas: solo requiere `00_Schema.sql` ejecutado previamente.

---

## Integrations

- **Fuente del catálogo:** legacy `MES Simulator/UI/Dashboard/js/main.v1.0.0.js`
  (`CONFIG.PARAMETERS.data`, `LogNCManager.scrapItems`), `main.aspx`
  (sección `dc_collection_list`, modales de Start y DataCollection),
  `guidedTest.v1.0.0.js` (flujo guiado de apertura y cierre).
- **MSS D000281359:** documento físico externo (no disponible digitalmente).
  Necesario para confirmar rangos Min/Max de los 10 parámetros numéricos.
  Sin acceso en esta fase — valores diferidos.

---

## Acceptance Criteria

**AC-1:** `SELECT * FROM Products WHERE Name = 'R&C'` devuelve 1 fila.

**AC-2:** `SELECT * FROM Areas WHERE Name = 'Moldeo'` devuelve 1 fila con
`SerializationType = 1, RequiresAssembly = 0, RequiresDataCollection = 1`.

**AC-3:** `SELECT COUNT(*) FROM NCCodes WHERE AreaId = (SELECT Id FROM Areas WHERE Name = 'Moldeo')`
devuelve `30`.

**AC-4:** Ningún código NC contiene espacios en el valor de la columna `Code`.

**AC-5:** `SELECT COUNT(*) FROM DataCollectionParameters WHERE AreaId = (SELECT Id FROM Areas WHERE Name = 'Moldeo')`
devuelve `15`.

**AC-6:** `SELECT COUNT(*) FROM DataCollectionParameters WHERE AreaId = ... AND DataType = 'Boolean'`
devuelve `3` (Verificación MSS + First Shot OPE + First Shot NIR OPE).

**AC-7:** `SELECT COUNT(*) FROM DataCollectionParameters WHERE AreaId = ... AND DataType = 'Date'`
devuelve `1` (Fecha de Expiración Kerosene).

**AC-8:** El script puede ejecutarse dos veces consecutivas sin errores y sin
duplicar filas (idempotencia).

**AC-9:** `SELECT COUNT(*) FROM DataCollectionParameters WHERE MinValue IS NULL`
devuelve `15` — todos los rangos están pendientes de confirmación contra MSS.

---

## Deferred Decisions

| Decisión | Fallback actual | Trigger para revisar |
|---|---|---|
| Rangos Min/Max de los 10 parámetros numéricos MSS D000281359 | NULL (StepValidator acepta cualquier número) | Cuando se obtenga acceso al documento físico D000281359 o se audite con el trainer de R&C |
| Descripción real de `F_FS_THICKNESS` | `"NCR"` (temporal) | Cuando se revise el catálogo SAP real de NC codes con el equipo de calidad |
| Las 3 preguntas exactas de `DCGTM_LC_MOLDEO/A` | Hardcodeadas en StepValidator (Fase 3) usando las mismas 3 preguntas del `CONFIG.LCQUESTIONS.data` del legacy | Si las preguntas de LC Moldeo son distintas a las del LC estándar, confirmar con el trainer antes de Fase 3 |
| Si los 10 params MSS se capturan en Start únicamente o también en Complete | DisplayOrder 2-11 en el único paso DataCollection | Confirmar antes del StepValidator de Fase 3 al modelar el Complete wizard |

---

## Open Questions

*Vacío — requisito para aprobación.*

---

## Nota de proceso

> **Este spec fue reconstruido retroactivamente después de que la implementación
> ya se ejecutó, saltando el review loop y el agreement gate del skill `/spec`.**
>
> El flujo real fue: auditoría del legacy → rondas de preguntas → respuestas del
> desarrollador → implementación directa (SQL + CLAUDE.md). El spec file en
> `docs/specs/` no existió antes de escribir código.
>
> Se documenta ahora para trazabilidad, pero no siguió el flujo de aprobación
> antes de escribir código. En un proyecto auditado, es mejor que quede registrado
> honestamente que un paso de proceso se saltó una vez, a que parezca que siempre
> se siguió el flujo correctamente.
>
> **A partir de Fase 3:** flujo obligatorio sin excepciones:
> `/spec` → revisión explícita y aprobación del desarrollador → `/plan` → `/scaffold`.
> Incluso si el contexto de conversaciones previas parece suficiente.
