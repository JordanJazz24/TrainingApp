# UI Reference — Moldeo (R&C)

Diferencias y variaciones específicas del área **R&C / Moldeo** respecto al flujo general.
Leer en paralelo con `general-flow.md` — este archivo solo documenta lo que cambia.

---

## Flags del área

| Flag | Valor | Impacto |
|---|---|---|
| `SerializationType` | `NonSerialized` | Lotes/batch (ver variaciones en Paso 3 y Paso 7) |
| `RequiresAssembly` | `false` | Paso 4 ausente — su peso (15%) absorbido por Search en el seed |
| `RequiresDataCollection` | `true` | Paso 5 presente |

**Pasos activos en la guía:** 1 · 2 · 3 · 3.1 (LC Moldeo) · 5 · 6 · 7 · 8

---

## Paso 3 — Search / Start Order (VARIANTE MOLDEO)

**Diferencia clave:** la orden **no se carga automáticamente en el POD** al seleccionarla (a diferencia del flujo general).

### Flujo del operador

1. Campo SFC → escanear o Browse Work List → seleccionar orden
2. La orden se carga en el campo SFC de la search bar
3. El operador presiona **[Start]** en la barra de acción (obligatorio en esta área)
4. Se lanza la secuencia especial de 3 modales secuenciales (ver Paso 3.1)

### Confirmación tras completar los 3 modales

Banner informativo en la esquina superior derecha, entre el header bar y la search section:
```
✓ Start process completed successfully
```

Tras el banner, el POD se puebla y el flujo continúa normalmente (Paso 5 → DC Collect).

---

## Paso 3.1 — LC Moldeo (secuencia especial dentro de Start)

**StepType:** `LineClearance` (en el seed: step 4 condicional, weight 0.03) \
**Nota:** esta secuencia ocurre **dentro** del Start, no como un paso independiente del dashboard.

> Las 3 preguntas de LC Moldeo **no son DataCollectionParameters** — son un LineClearance
> adicional específico de esta área. Su contenido está hardcodeado en el `StepValidator` por área.

### Estructura: 3 modales secuenciales

Al presionar [Start], se abren los siguientes modales en orden. Cada uno debe completarse para avanzar al siguiente.

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
> hardcodeado en `StepValidator` por área (`AreaId` = R&C/Moldeo).

---

#### Modal 3 — Validación IDMold & IDIMM

Contiene la pregunta de verificación del IDMold e IDIMM según MSS.

```
[Data Collection Entry]                                              [↗][X]
  DC Group: DCGTM_LC_MOLDEO/A    Resource: {resourceName}
──────────────────────────────────────────────────────────────────────────
  ★ ¿Se verificó el IDMold & IDIMM según MSS?:
  Data Type: Boolean    0 files attached
  [ Seleccione ▼ ]  → opciones: (blank) | NO | Sí
──────────────────────────────────────────────────────────────────────────
                             [💾 Save]  [✗ Return to POD]
```

- **[Save]:** cierra la secuencia de 3 modales → aparece banner de confirmación → POD se puebla

---

## Paso 4 — Assembly (AUSENTE en Moldeo)

`RequiresAssembly = false` → el paso no existe en la guía. \
**Su peso (15%) está absorbido por Search en el seed (`07_RCMoldeoGuide.sql`).**

---

## Paso 7 — Log NC / SFC Split (VARIANTE NonSerialized)

Al ser NonSerialized, el flujo de cierre incluye el sub-paso SFC Split antes de Log NC.

> El comportamiento de SFC Split y Log NC es idéntico al documentado en `general-flow.md` Paso 7 — Sub-flujo A: SFC Split (solo NonSerialized).

No hay diferencias adicionales respecto al flujo general.

---

## Resumen de variaciones

| Elemento | Flujo general | Moldeo (R&C) |
|---|---|---|
| Carga de orden al POD | Automática tras selección | Requiere presionar [Start] manualmente |
| Secuencia especial en Start | No | 3 modales: ID Molde → LC preguntas → IDMold & IDIMM |
| Assembly | Condicional | Ausente (RequiresAssembly=false) |
| SFC Split | Solo NonSerialized | Sí (área NonSerialized) |
| DC Collect | Condicional | Presente (RequiresDataCollection=true) |
| Closing Line Clearance | Condicional | Presente |
| Complete | Siempre | Sin variación |
