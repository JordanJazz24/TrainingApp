# Auditoría de fidelidad de UI/UX contra el SAP MES real — 2026-10-19

> Pedida por el usuario el 2026-10-19: revisar **estilos, UI y UX generales** del simulador
> contra el SAP MES real, en el ambiente de testing. Continúa la numeración: hallazgos desde
> **AUD-45**. Las medidas y los mensajes reales están en
> `docs/ui-reference/estilos-y-mensajes-mes.md`; aquí solo las diferencias.

## Alcance y regla de esta auditoría

- **Se corrige solo lo que el usuario apruebe** como correcto para el simulador (usuario,
  2026-10-19). Cada hallazgo tiene su casilla: **☐ pendiente · ☑ aprobado · ✗ se queda como
  está** (con el motivo). Lo no aprobado no se toca.
- Fuente: POD real `L_TP_SC_ASSYC_DC_TL_PL_LC_ESIG` (CR9A) medido con el navegador, contra el
  simulador local (Guided, Sensores LoFlo) y su código. Revisado: POD, NC Selection, NC Data
  Entry, SFC Split, Activities, Line Clearance (Data Collection Standalone), Assemble
  Components, Component List y Data Collection List.
- **Fuera:** Resource Setup (omitido por el usuario; es una app MII en ventana aparte) y el
  interior de Data Collection Entry de DC Collect (pide la contraseña del usuario).
- En el MES de testing solo se guardó un Line Clearance (`DC_LC_DONE = SI` en
  `R_OMNI_FIRST_DISTAL_2`), autorizado por el usuario para destrabar Assemble y DC Collect.
- **Vía:** los de estilo son CSS y markup → plan directo. Los de flujo cambian
  comportamiento: al aprobarlos se decide si van con plan directo o con `/spec`
  (`docs/process.md` → 1).

## Coincide (no se toca)

Colores base del POD (`#DDDDDD`, `#BBBBBB`, `#F2F2F2`, `#E5E5E5`, `#E2E2E2`), celda selectora
`#007CBF`, banda oscura y fondo de SFC Split, grilla de NC Selection (código y descripción),
paginación de Component List y DC List, el "⚠ 1 DC Groups saved" del LC, y el llenado de Shop
Order y Status al elegir la orden.

---

## Estilos (CSS y markup)

| Hallazgo | Real | Simulador hoy | Propuesta | Aprobado |
|---|---|---|---|---|
| **AUD-45** Fuente | SAP "72" | Arial | Usar "72" si su licencia permite empaquetarla en el simulador; si no, se queda Arial | ☐ |
| **AUD-46** Tamaño de texto | 12 px en botones, celdas, links y valores; 12.8 px en etiquetas | 12.8-13.6 px (`0.8rem`-`0.85rem`) | Bajar a 12 / 12.8 px | ☐ |
| **AUD-47** Header | 29 px; título 18 px negrita | 40 px; 20.8 px | Igualar | ☐ |
| **AUD-48** Campo SFC | 189×32 fijo, borde `#BFBFBF` (`#007CBF` con foco) | Se estira (≈555 px), 28 px, borde `#7A9CC0` | Ancho fijo y bordes del real | ☐ |
| **AUD-49** Shop Order / Status | Peso normal | `<strong>` | Quitar la negrita | ☐ |
| **AUD-50** Links | `#00689E` | `#0A6EBD` | Igualar | ☐ |
| **AUD-51** Botones grandes (acciones y pie) | 55 px, ancho por texto, borde `#A3A3A3`, radio 4.8 px, sin sombra, brillo blanco arriba; ícono 40×40 | 66 / 60 px, mínimo 128 / 170 px, borde `#9D9D9D`, radio 3 px, con sombra; íconos 44 y 34×38 | Igualar | ☐ |
| **AUD-52** Botones del panel derecho | 133×70, columna 156 px, ícono 40×40 | 142×80, columna 172 px, ícono 36 px | Igualar | ☐ |
| **AUD-53** Estado activo y hover | Degradado azul `#007CBF`, borde `#00689E`, **texto blanco**; también en el botón de acción recién presionado y en el hover de los botones de las listas | `#D0E8FF` con borde `#0D6EFD`; hover gris | Igualar | ☐ |
| **AUD-54** POD Work List | Filas de 26 px; fondo `#EAEDF0` con datos; selección `#C0D8E4`; selectora de 18 px sobre `#F6F6F6` | 32 px; blanco; `#C9E1ED`; 24 px | Igualar (la cuadrícula de fondo pasa a paso de 26 px) | ☐ |
| **AUD-55** Botones de ventana (Add, Save, OK, Return to POD…) y "Log NC" chico | 21 px, fondo sólido `#E2E2E2`, borde `#888`, radio 4 px, 12 px | `.mes-btn`: 26 px, degradado, borde `#9A9A9A`, radio 3 px, 12.8 px | Igualar | ☐ |
| **AUD-56** Campos y etiquetas de ventana | Campo 32 px, borde `#BFBFBF`; etiqueta 12.8 px `#444` | 26-30 px, borde `#B9BDC2`; etiquetas de Assemble `#5F6B76` | Igualar | ☐ |
| **AUD-57** Banda "SFC: … (1/1)" de NC Data Entry | 16 px negro | 18.4 px `#444` | Igualar | ☐ |
| **AUD-58** (?) del header | No existe (solo Home y Change user) | Existe (solo visual) | Quitarlo | ☐ |
| **AUD-59** Listas del panel (Component List, DC List, NC Selection) | Filas blancas, separador `#DDD`; botón de fila 128×45; deshabilitado con borde `#BFBFBF` y texto `#999` | Fondo `#F2F2F2`, separador `#E3E3E3`; botones de mínimo 150×52; deshabilitado con opacidad | Igualar | ☐ |
| **AUD-60** Line Clearance | [Collect] de 184×45; opciones vacío / `NO` / `SI` | [Collect] de 60 px de alto; `( Seleccione )` / `NO` / `Si` | Igualar | ☐ |
| **AUD-61** SFC Split | Etiquetas negras; [Split] [Close] blancos con texto y borde `#0854A0`, 14 px, 26 px; Qty de 153×21, amarillo solo con foco y con foco al abrir | Etiquetas `#444`; Qty de 150×26 siempre amarillo | Igualar | ☐ |
| **AUD-62** Menú Activities (estilo) | Sin sombra ni radio, borde `#BFBFBF`, ítems de 32 px y 12 px negro; se abre a la izquierda del botón | Dropdown de Bootstrap (sombra, radio, 12.8 px) | Igualar | ☐ |

## Flujos y UX (cambian comportamiento)

| Hallazgo | Real | Simulador hoy | Propuesta | Aprobado |
|---|---|---|---|---|
| **AUD-63** Mensajes de error del MES | Banner rojo (POD) o franja roja dentro de la ventana; en inglés, con número; el del POD queda hasta la siguiente acción; a veces `Messages (n):` en el panel | SweetAlert en español (`PodNotice`, `BtnSplit_Click`, barcode de Assemble); el banner solo tiene la variante de éxito y se oculta a los 3 s | Variante de error del banner y de la franja de ventana, con los **textos reales** de la referencia donde el MES tiene el mismo caso (ej. `SfcRequired` → 22506, `SfcNotInOrder` → 22507/13005, split vacío → 13875). Los avisos propios del simulador (`SelectAllPieces`, `StartWholeOrder`, `SplitRequired`…) siguen como avisos del simulador | ☐ |
| **AUD-64** Errores de SFC Split | El mensaje reemplaza el formulario con [OK], y el OK **cierra la ventana** | SweetAlert sobre la ventana abierta | Igualar (parte de AUD-63) | ☐ |
| **AUD-65** Activities: ítems habilitados | **Todos se ven habilitados** | Los ajenos al paso, deshabilitados en gris: **en Assessment delata cuál elegir** | Mostrarlos todos habilitados; el que no corresponde, como intento o aviso según `docs/domain.md` | ☐ |
| **AUD-66** NC Selection | Paginación por links numerados; NC por página según el alto disponible; × para cerrar | First/Prev/Next/Last con "Rows 1-N of N"; página fija | Igualar la paginación y la × | ☐ |
| **AUD-67** Assemble abre el modal directo | Assemble abre Assemble Components de inmediato, con la Component List detrás | Primero la Component List; el modal se abre desde una fila | **Confirmar con planta** si siempre es así o depende de la orden, antes de decidir | ☐ |
| **AUD-68** "Unsaved Data" | Return to POD con datos escritos pide confirmación [OK] [Cancel] | Sale sin preguntar | Agregar la confirmación | ☐ |
| **AUD-69** Barcode inválido en Assemble | Franja roja 17150 y se despliegan Component Details / Assembly Data vacíos con [Add] | SweetAlert "Barcode inválido" | Igualar (parte de AUD-63) | ☐ |
| **AUD-70** Firma electrónica de DC | Workstation `_ESIG`: Data Collection Authentication antes de DC Entry | No existe | **Confirmar con planta** si las workstations que se entrenan la tienen | ☐ |

## Decisiones

Ninguna abierta: cada casilla la marca el usuario. AUD-67 y AUD-70 primero necesitan la
respuesta de planta.
