# UI Reference — Moldeo (R&C)

Diferencias y variaciones específicas del área **R&C / Moldeo** respecto al flujo general.
Leer en paralelo con `general-flow.md` — este archivo solo documenta lo que cambia.

---

## Flags del área

Flags y pesos de la guía: `docs/domain.md` → "Jerarquía" y "Ponderación de pasos" (no se copian
aquí). Impacto en las pantallas:

| Flag | Impacto |
|---|---|
| `NonSerialized` | Un SFC de lote: selección de una fila; hay SFC Split (Paso 7) |
| `RequiresAssembly = false` | Paso 4 ausente |
| `RequiresDataCollection = true` | Paso 5 presente |
| `RequiresStartChecklist = true` | Start abre los 4 modales (Paso 3.1) |

**Pasos de la guía (8):** 1 Resource Setup · 2 LC · 3 Search/Start (con 3.1, el checklist) ·
5 DC · 6 LC de cierre · 7 SFC Split · 8 Log NC · 9 Complete (numeración de
`general-flow.md`).

---

## Paso 3 — Search / Start Order (VARIANTE MOLDEO)

**Diferencia clave:** la orden **no se carga automáticamente en el POD** al seleccionarla (a diferencia del flujo general).

### Flujo del operador

1. Campo SFC → escanear o Browse Work List → seleccionar orden
2. La orden se carga en el campo SFC de la search bar
3. El operador presiona **[Start]** en la barra de acción (obligatorio en esta área)
4. Se lanza la secuencia especial de 4 modales secuenciales (ver Paso 3.1)

### Confirmación tras completar los 4 modales

Banner informativo en la esquina superior derecha, entre el header bar y la search section:
```
✓ Start process completed successfully
```

Tras el banner, el POD se puebla y el flujo continúa normalmente (Paso 5 → DC Collect).

---

## Paso 3.1 — Secuencia de 4 modales (dentro de Start)

**Nota:** esta secuencia ocurre **dentro** del paso Search/Start Order, como un flujo condicional específico de Moldeo/R&C.

> **Importante:** La secuencia incluye 4 modales (no 3): ID del Molde → Verificaciones → Parámetros MSS → Verificación Final.  
> Las 3 preguntas del Modal 2 (Verificaciones) son un LineClearance adicional específico de esta área: viven en `SearchControl` y las califica `StepValidator` en el paso Search.

### Estructura: 4 modales secuenciales

Al presionar [Start] con un SFC válido en el campo, se abren los siguientes modales en orden. Cada uno debe completarse para avanzar al siguiente. La tabla POD permanece visible detrás de todos los modales en todo momento.

---

#### Modal 1 — ID del molde (DCGTM_ID_MOLDE/A)

Captura el ID del molde como DataCollectionParameter de texto libre.

```
[Data Collection Entry]                                              [↗][X]
  DC Group: DCGTM_ID_MOLDE/A    Resource: {resourceName}
──────────────────────────────────────────────────────────────────────────
  ★ {label del parámetro}:
  Description: {descripción}              Data Type: Text    0 files attached
  [_______________texto libre_______________]
──────────────────────────────────────────────────────────────────────────
                             [💾 Save]  [✗ Return to POD]
```

- **Input:** texto libre (no validado contra catálogo)
- **[Save]:** avanza al Modal 2

---

#### Modal 2 — Preguntas de validación LC (DCGTM_LC_MOLDEO/A · parte 1)

Contiene las primeras preguntas boolean del Line Clearance de Moldeo.

```
[Data Collection Entry]                                              [↗][X]
  DC Group: DCGTM_LC_MOLDEO/A    Resource: {resourceName}
──────────────────────────────────────────────────────────────────────────
  ★ {pregunta LC 1}:
  Data Type: Boolean    0 files attached
  [ Seleccione ▼ ]  → opciones: (blank) | NO | Sí

  ★ {pregunta LC 2}:
  Data Type: Boolean    0 files attached
  [ Seleccione ▼ ]  → opciones: (blank) | NO | Sí

  ★ {pregunta LC 3}:
  Data Type: Boolean    0 files attached
  [ Seleccione ▼ ]  → opciones: (blank) | NO | Sí
──────────────────────────────────────────────────────────────────────────
                             [💾 Save]  [✗ Return to POD]
```

- **Todas las preguntas requeridas ★** — no permite Save si alguna está en blanco
- Respuesta correcta: **Sí** en todas
- **[Save]:** avanza al Modal 3

> Las etiquetas exactas de las preguntas vienen de las imágenes de referencia. El texto está
> hardcodeado en `SearchControl.ascx`; `StepValidator` califica las respuestas.

---

#### Modal 3 — Parámetros Técnicos MSS (DCGTM_PARAMETROS_MSS_D000281359/A)

Captura 10 parámetros numéricos del proceso de moldeo según MSS D000281359.

```
[Data Collection Entry]                                              [↗][X]
  DC Group: DCGTM_PARAMETROS_MSS_D000281359/A    Resource: {resourceName}
──────────────────────────────────────────────────────────────────────────
  ★ Cooling Time según MSS D000281359:              Data Type: Numeric  [___]
  ★ Velocidad de inyección según MSS D000281359:    Data Type: Numeric  [___]
  ★ Intensidad de pad printing:                     Data Type: Numeric  [___]
  ★ Temperatura Zona 1:                             Data Type: Numeric  [___]
  ★ Temperatura Zona 2:                             Data Type: Numeric  [___]
  ★ Temperatura Zona 3:                             Data Type: Numeric  [___]
  ★ Temperatura Zona 4:                             Data Type: Numeric  [___]
  ★ Temperatura Zona 5:                             Data Type: Numeric  [___]
  ★ Temperatura Zona 6:                             Data Type: Numeric  [___]
  ★ Tiempo de curado de pad printing:               Data Type: Numeric  [___]
──────────────────────────────────────────────────────────────────────────
                             [💾 Save]  [✗ Return to POD]
```

- **Validaciones:** Todos los campos no vacíos + todos valores numéricos
- **[Save]:** avanza al Modal 4

---

#### Modal 4 — Verificación Final MSS (DCGTM_VERIFICACION_FINAL/A)

Pregunta final de verificación según MSS.

```
[Data Collection Entry]                                              [↗][X]
  DC Group: DCGTM_VERIFICACION_FINAL/A    Resource: {resourceName}
──────────────────────────────────────────────────────────────────────────
  ★ ¿Se verificó según D000128035 o D000207299?:
  Data Type: Boolean    0 files attached
  [ Seleccione ▼ ]  →  opciones: (blank) | NO | Sí
──────────────────────────────────────────────────────────────────────────
                             [💾 Save]  [✗ Return to POD]
```

- **Validación:** Dropdown seleccionado + respuesta = "Sí"
- **[Save]:** cierra la secuencia de 4 modales → aparece banner de confirmación → POD se puebla

---

## Paso 4 — Assembly (AUSENTE en Moldeo)

`RequiresAssembly = false` → el paso no existe en la guía. Search absorbe su peso
(`docs/domain.md` → "Ponderación de pasos").

---

## Paso 5 — DC Collect (grupo obligatorio)

Moldeo tiene 6 DC Groups y **solo `DCGTM_FECHA_EXPIRACION_NIR/A` es obligatorio** (fechas
de expiración de Kerosene y Tintas; confirmado con el usuario 2026-09-28, spec 008). El
paso se completa al guardar ese grupo; guardar otro grupo vuelve a la Data Collection List
sin completar el paso. Hay una sola pieza (el lote): el indicador muestra `(1/1)`.

---

## Paso 7 — SFC Split (NonSerialized)

Moldeo incluye `SFC_Split` como paso independiente. Permite múltiples splits; la X vuelve al POD para seleccionar otra fila y [Close] avanza sólo después de un split exitoso.

## Paso 8 — Log NC

`LogNC` conserva únicamente el flujo NC/scrap. Al completarlo, avanza a Complete.

---

## Resumen de variaciones

| Elemento | Flujo general | Moldeo (R&C) |
|---|---|---|
| Carga de orden al POD | Automática tras selección | Requiere presionar [Start] manualmente |
| Secuencia especial en Start | No | 4 modales: ID Molde → Verificaciones → Parámetros MSS → Verificación Final |
| Assembly | Condicional | Ausente (RequiresAssembly=false) |
| SFC Split | Solo NonSerialized | Sí (área NonSerialized) |
| DC Collect | Condicional | Presente (RequiresDataCollection=true); obligatorio solo el grupo de fechas de expiración |
| Start con checklist | `RequiresStartChecklist = 0` (Start directo) | `RequiresStartChecklist = 1` (4 modales) |
| Closing Line Clearance | Condicional | Presente |
| Complete | Siempre | Sin variación |
