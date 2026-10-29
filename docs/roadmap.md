# Roadmap — MESTrainingSimulator

> **Fases y planes.** Lo escribe Arquitectura y Planeación (`docs/process.md` → "Tres
> sesiones de trabajo"): fases, su alcance ("No incluye"), planes, revisiones con negocio y
> orden de tareas. Dónde quedamos, deuda, BD del usuario y auditorías: `docs/status.md`; lo
> cerrado: `docs/history.md`. Se separaron el 2026-10-21: las referencias `roadmap →
> "Estado actual"`, "Deuda y riesgos conocidos", "BD de desarrollo del usuario",
> "Auditorías" o "Historial" de specs y auditorías apuntan ahí. Las reglas vigentes están en
> `CLAUDE.md`, el proceso en `docs/process.md` y los mecanismos en `docs/architecture.md`. Las
> secciones de cada fase describen lo que se hizo **en su momento**: si contradicen a
> `CLAUDE.md`, manda `CLAUDE.md`.

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
| 8 · Validación interna | ✅ | 2026-10-12 | revisiones y validación con técnicos |
| 8.5 · Entrega de noviembre | ✅ | 2026-10-22 | specs 011-017, filas 1-13 |
| 9 · Deploy del MVP | ✅ (en el servidor faltan la publicación, la prueba de humo, D-19, y el lanzamiento, a cargo del usuario y TI) | 2026-10-22 | `fase-9-plan.md` |
| 10 · Dashboard e historial por operador (T8) | ✅ (E1–E8; D-136 a D-150) | 2026-10-23 | `018-dashboard-training.md`, `018-plan.md` |
| — · Guía Wires / Subensamble 1 para el MVP | ✅ (`0f01f11`; scripts `11` y `12`, sin C#) | 2026-10-26 | plan directo (D-161) |
| 11 · UI/UX del dashboard y la administración | ✅ (E1–E6 y AC-7; D-153 a D-164) | 2026-10-27 | `019-ui-ux-dashboard.md` (brief de diseño), `019-plan.md` |
| — · Área asignada por operador | ✅ (E1–E5: `425e944`, `d6af08d`, `a483731`, `03cd8df`, `b06a993`; D-165 a D-179 y D-208; cierra B-20, B-3 y B-4) | 2026-10-27 | `021-area-operador.md`, `021-plan.md` |
| — · R3 Custom Resource Setup | ✅ (E1–E5: `144d845`, `a0c7c77`, `3a18960` + `82d000d`, `5d12e80`, `3336221`; D-180 a D-201) | 2026-10-27 | `020-custom-resource-setup.md`, `020-plan.md` |
| — · Filtro de período en Pasos y Resumen | ✅ (E1–E4: `2ffb614`, `869f99a`, `5359a5a`, `9c318df`, en `spec-022`, integrada en `TrainingApp` en `f8aab65`; D-209 a D-218) | 2026-10-28 | `023-filtro-periodo.md`, `023-plan.md` |
| — · Dashboard por área (Operadores y Operador) | ⏳ spec por hacer en la nube (`TrainingApp-Desarrollo`, D-260 a D-279); va antes de la E6 de la 022 | | `024-*` (por crear) |
| **12 · Mockups por pantalla** | ▶️ brief y plan aprobados el 2026-10-27 (en la nube, D-202 a D-207); Login (3b, D-220 a D-227) y Cambio de contraseña (2a, D-228 a D-232) aprobados el 2026-10-28; E1 ✅ (`642cacd`, integrada en `f8aab65`); E2a ✅ Login (`53a9d52`) y E2b ✅ Cambio de contraseña (`8ccda11`), integradas en `TrainingApp` (`07363e3`); siguen E3, E4, E5, E7 y E8 según sus mockups; E6 bloqueada por la 024 | | `022-mockups-por-pantalla.md` (brief de diseño), `022-plan.md`, `022-briefs.md` |

### Fase 7 — Catálogo de LoFlo (segunda área) ✅
**Objetivo:** script SQL con el catálogo de LoFlo (Sensores). Solo carga de datos, cero
código nuevo: valida que el motor es genérico y que `RequiresAssembly` y
`RequiresDataCollection` funcionan con otra configuración.
**El script tiene que cargar** (spec 008): los flags del área (`docs/domain.md` → "Jerarquía",
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

### Fase 8 — Validación interna ✅ (2026-10-12)
**Objetivo:** sesión con trainer/operador real sobre R&C y LoFlo. Correcciones de UX, bugs de
flujo, ajustes de catálogo.
**No incluye:** nuevas áreas, dashboard admin, reportes.
**Resultado:** revisiones con el ingeniero de SAP MES y con Ernesto (2026-10-09) y
validación con técnicos de MES (2026-10-12), que aprobaron el flujo general y la fidelidad de
la interfaz. Lo que pide funcionalidad nueva pasó a la Fase 8.5.

#### Revisión con SAP MES (2026-10-09)
30 minutos con el ingeniero de SAP MES (Diego) sobre Redefinity en Guided, desde el paso 3.
Lo que se aprendió de cada pantalla está en `docs/ui-reference/general-flow.md` (pasos 3, 5
y 8). Clasificación: **S** = funcionalidad nueva, necesita spec y sale de esta fase
(`CLAUDE.md` → "Reglas de trabajo"); **B** = backlog.

| Hallazgo | Clase | Siguiente paso |
|---|---|---|
| Start manual después de seleccionar; solo carga solo con escáner configurado | Confirmado | — |
| Line Clearance de cierre igual al de apertura | Confirmado | — |
| Flujo general (Start, Assembly, DC, LC, Complete) correcto; en piso hay muchas variantes | Confirmado | Decidir con negocio qué variantes entran |
| La cola trae piezas de otras órdenes y el procedimiento es iniciar la orden. El simulador entrena "seleccionar todo" (Start exige todas, spec 008) | S | Piezas de otra orden en Browse Work List y elegir bien se califica (hoy seleccionar filas es navegación y nunca cuenta) |
| Lo habitual es escanear la orden y dar Start | S | Escanear la Shop Order en `TxtSFC` (hoy readonly, spec 005). Misma spec que la anterior |
| Log NC tiene tres métodos en uso (fast scrap, scrap/return component, extensión de la operación), según la operación | S | Decidir con negocio cuáles se entrenan |
| DC por pieza o para todas, según la operación | B | Mantener por pieza; "aplicar a todas" como mejora |
| Mensajes sin captura (pieza de otra orden, material equivocado, avance de DC) | Por verificar | Coordinar el ambiente de calidad que ofreció el ingeniero |

Sin respuesta: Operations por área (solo un indicio: varía la configuración, no se habló de
la secuencia), validación del material contra el BOM, catálogos reales de Sensores, Status
`In Queue` en NonSerialized, uso de Disassemble/Hold y los dos pendientes de captura. El
ingeniero recomendó definir alcance y requisitos con Ernesto antes de agregar variantes.

#### Revisión con Ernesto, MES (2026-10-09)
68 minutos: contexto de negocio y demo de Guided y Assessment.
- **Contexto:** hoy no hay entrenamiento formal de MES (enseña el compañero de al lado); unas
  1600 personas usan el sistema. Ernesto libera en 1-2 semanas una **prueba práctica en
  papel** (parte de una CAPA): unos 10 puntos que suman 100, evaluada en producción. Pidió
  que sea el **mínimo** que evalúe el simulador; los pesos y penalizaciones los define el
  simulador. A futuro, la prueba en papel pasaría al simulador: eso sube la importancia de la
  integridad de Assessment (identidad del operador, historial auditable).
- **Entrega planeada:** segunda semana de noviembre de 2026 (Fase 8.5).
- **Tareas que salieron** (el plan y su orden, en la Fase 8.5):

| # | Tarea | Estado |
|---|---|---|
| T1 | Versión de prueba publicada para Ernesto y técnicos de MES | Fase 8.5 |
| T2 | Escanear la Shop Order como camino principal de Search/Start (RF-17). Decidido en la reunión: escaneo principal + aviso de que Browse Work List también vale; sin agregar variabilidad | Fase 8.5, spec 011 |
| T3 | **Modo práctica**, tercer modo: Assessment sin evaluación, sin la guía visual y con pistas ante la acción incorrecta | Fase 8.5, spec 012 |
| T4 | Log NC: practicar los tres tipos de scrap (fast scrap, scrap/return component, extensión de la operación) | Fase 8.5, spec 014 (UI espera pantallas) |
| T5 | Guías por **área** (lo que pidió el equipo de Paola) o **dos escenarios** generales, serializado y por lote (recomendación de Ernesto), con los pasos opcionales siempre incluidos | ✅ **Decidido (usuario, 2026-10-19): guías por área**, como están hoy. Sin escenarios generales |
| T6 | Mapear los criterios de la prueba en papel a pasos del simulador (puede mover pesos; deben sumar 1.00) | Documento recibido el 2026-10-19 (QD001043644); contraste en `docs/ui-reference/evaluacion-practica-mes.md` (EP-1…EP-9) |
| T7 | Revisar los mensajes de la guía de Guided con los trainers (menos texto; burbuja de "lo que hay que recordar" o botón "?") | Contenido de `GuideScriptBuilder`, en paralelo |
| T8 | Dashboard: historial por operador (intentos, fechas, duración por paso, registro de actividad) para ver dónde fallan antes de producción. Sin tiempo mínimo de práctica | Fase 10 |
| T9 | Acceso al ambiente de **testing** de SAP MES (no el sandbox): mensajes reales, pantallas de los tres scraps y Start de Moldeo | ✅ El usuario ya tiene acceso (2026-10-19). Los banners de la reparación rápida y de Local Rework se confirmaron correctos (V-3, V-5) |

Backlog: RF-16 (piezas de otras órdenes en Browse Work List; Diego a favor, Ernesto en contra
de agregar variabilidad). Data Collection "aplicar a todas las piezas" se descartó el
2026-10-12 (ver abajo).

#### Validación con técnicos de MES (2026-10-12) y contraste con la WI
Validación sin transcripción; contraste con la WI de producción QD001001524 rev. 27
(sesión "contraste-manual-sap-mes", rama `docs/wi-produccion-contraste`, archivo
`docs/ui-reference/wi-produccion-disposables.md`). Aclarado por el usuario.
- **Aprobado:** flujo general, fidelidad de la interfaz y orden actual de los pasos. La orden
  **no** se escanea antes de Resource Setup, aunque la WI 6.4 lo sugiera (descartado por el
  usuario; coincide con la spec 011).
- **Data Collection pieza por pieza:** se mantiene; "aplicar a todas las piezas" queda
  descartado.
- **Complete:** solo el flujo directo. Las variantes de la WI (Log Buyoff, firma electrónica,
  supervisor que completa por otro) quedan como **simplificación aceptada**
  (`docs/ui-reference/general-flow.md` → Paso 9).
- **Tool List:** descartado por ahora. Lo pidieron antes del LC de apertura, pero la WI solo
  lo usa para cambiar una herramienta de un SFC ya iniciado, y lo hace un técnico, ingeniero
  o supervisor, no el operador; las herramientas iniciales ya se asocian en Resource Setup.
- **Tres tipos de scrap (T4):** los piden otra vez; ya son tres fuentes (Diego, Ernesto y los
  técnicos), así que sube de prioridad. La WI documenta el flujo de cada uno: desecho rápido
  (`F_FS…`, lo que el simulador ya hace), reparación rápida (`F_FR…`, quitar y devolver o
  desechar un componente y agregar el nuevo) y reparación rápida extendida (`F_FX…`, volver a
  otra operación; con `F_FX_GENERAL`, el MPI y su revisión o el NCR en Comments). Faltan las
  pantallas: imágenes del .docx de la WI o T9.
- **T10 · Preparación antes de SAP (nueva, WI 6.4, pasos 1 a 3):** limpiar historial y caché
  con el Code1, abrir "Recursos MES" con el Code1 y el código del recurso, y login de MES
  (Code1 + contraseña, o PIN + tarjeta), verificando que el Code1 sea el correcto. Se repite
  al inicio de cada turno y al cambiar de estación. Choca con `CLAUDE.md` ("el login a SAP
  NetWeaver no se evalúa"): necesita spec. Abierto: si se evalúa en Assessment (con peso, y
  los pesos siguen sumando 1.00) o solo se enseña en Guided; si es común a todas las áreas;
  si incluye el logout obligatorio de la WI 6.5 (descansos, fin de turno, cambio de estación
  u operador). Sin capturas: no inventar UI (`docs/process.md` §8). Spec 016 (ver la
  retroalimentación de la trainer).

#### Retroalimentación de la trainer (Carla, Wires línea final, 2026-10-13)
Fuente: sesión "Reuniones y dudas". Validación final con ella en 1-2 semanas.
- **Confirmado:** flujo e interfaz ("un espejo de producción"); escanear la Shop Order como
  la spec 011 (en Wires también escanean pieza por pieza o eligen de la lista);
  `[N SELECTED]` igual al real; las dos formas de salir de Assembly existen (se deja igual);
  DC pieza por pieza y LC de cierre (Wires no hace LC de cierre: nota en la guía).
- **Los 3 errores más graves:** (1) scrapear las unidades equivocadas: el más grave, el MES no
  lo detecta y les pasa a veteranos → spec 014 (la guía indica la pieza defectuosa y, en
  Assessment, scrapear otra es intento fallido; usuario, 2026-10-13); (2) iniciar piezas que
  no son de la orden o la estación → RF-16 sigue diferido (usuario); lo cubren en parte la
  alerta de Search/Start (T7) y la spec 011; (3) no hacer Remove All de los equipos en
  Resource Setup → spec 015. Pasos más críticos para ella: Resource Setup y scrap (el peso de
  Log NC, hoy 0.10, se revisa con T6).
- **Preparación antes de SAP (T10):** viene de una CAPA y en planta se audita que entren por
  el Repositorio: script de limpieza de caché → Repositorio con dos registros obligatorios en
  cualquier orden, "Recursos" (escanear el recurso) y "Ahora Ahora" (Code1, registro de
  unidades por hora) → login de MES. El usuario: se agrega sí o sí → spec 016 (espera
  capturas).
- **Logout** como último paso después de Complete (un clic) + nota "cierre sesión siempre que
  deje la estación": hubo CAPAs por Code1 compartidos y sesiones abiertas. Según ella el MES
  se cierra solo a los 15 min (sin verificar: T9) → spec 015.
- **Decisiones del usuario (2026-10-13):** Resource Setup arranca con equipos precargados (del
  turno o lote anterior) que hay que quitar con Remove All → spec 015. **Grupo de DC
  "Trainer"** (existe en toda la planta, opcional en el MES real pero siempre usado en
  entrenamiento: el nombre de la persona en entrenamiento, solo letras, porque trabaja con el
  Code1 del trainer hasta certificarse) reemplaza el tercer grupo opcional de Redefinity y
  LoFlo → spec 015. Piezas por orden Serialized: 4-5 (hoy 3-5) para que alcancen los tres
  scraps → spec 014. **Mensajes de la guía** aprobados, con notas de **alerta** en rojo en
  Resource Setup (Retrieve y Set Up; Remove All y escanear lo que está en la estación; al
  inicio del turno y en cada cambio de lote), LC de cierre (no aplica en algunas
  operaciones), Search/Start (solo piezas de su orden y estación) y DC (grupo Trainer); y
  mensajes normales en LC de apertura, DC, Log NC y Logout → T7.
- **Menores:** texto mal escrito ("RCI" o similar) en la selección de producto o área (bug);
  lentitud por enlace remoto (Fase 9); los errores del MES son un recuadro rojo arriba y las
  confirmaciones duran 5 s (`docs/ui-reference/general-flow.md`).

#### Contraste con la WI QD001001524 (2026-10-12)
El manual oficial de ejecución de órdenes en MES (Disposables, rev. 27) confirma la secuencia
y la operación de todos los pasos del simulador. Detalle y hallazgos W1-W23:
`docs/ui-reference/wi-produccion-disposables.md`. Lo ya decidido está en "Validación con
técnicos" (arriba): W17 (Complete directo, simplificación aceptada), Tool List descartado,
T10 desde la WI 6.4 y W9 cubierto por la spec 011. Quedan para Ernesto (T6) y el ambiente de
testing (T9):
- **W3:** ¿hay operaciones sin herramientas en Resource Setup? El simulador exige al menos una.
- **W7:** ¿de dónde sale el Line Clearance de cierre? La WI solo lo repite por condiciones
  (cambio de usuario, tiempo, SFC activo, cambio de orden). La evaluación práctica (EP-8) repite las mismas
  condiciones: no la resuelve.
- **W13 (resuelto 2026-10-15):** Assembly Data depende del control del material (Batch,
  Inventory SFC + Batch o Material Document); el simulador siempre usa el Process Lot. Entró
  en la spec 014 (D-46, 2026-10-15) y se recortó el 2026-10-19 (enmienda 6): diferida hasta
  que T6, la trainer o T9 muestren errores con esos datos.
- **W15:** ícono ✔ del grupo de DC guardado (captura pendiente en `inventario-pantallas.md`).
- **W23 (2026-10-15, captura del POD real):** el SFC serializado es `{12NC}-{serial}`, no
  `{ShopOrder}-{sufijo}` como en el simulador. Ajuste de la orden simulada, sin efecto en el
  puntaje. La spec 014 cerró sin tomarlo (integrado el 2026-10-21): candidato para un plan
  directo.
- **W20:** `F_FR_GENERAL` está en el catálogo de scrap de Moldeo, pero en el MES es un código
  de reparación. Lo resuelve la spec 014 (método de cada código en el catálogo).
- Para T4, la WI ya describe los pasos de la reparación rápida (`F_FR`) y la extendida
  (`F_FX`) (W21); faltan las pantallas. La reparación rápida reutiliza el ensamble de
  Assembly para el componente nuevo (Colorado: "follow Scanning to Assemble").
- **Otro site (2026-10-15):** la WI de Colorado Springs D001372587 rev. E es casi igual.
  Separa **Normal Flow** (por SFC; el split es solo de este flujo, con cantidad > 1) y
  **Relaxed Flow** (por cantidad: ventana "Relax Flow NC Logging" con Defect Count, sin
  split); los tres métodos aplican a ambos. Prefijo de la extendida: `F_EX` allá, `F_FX` en
  Costa Rica. Moldeo en el simulador corresponde a Normal Flow con cantidad > 1. No
  versionada en el repo.

### Fase 8.5 — Entrega de noviembre ✅ (2026-10-22)
> **Cerrada el 2026-10-22** con las filas 1 a 13 ✅. La entrega de noviembre pasa a ser la
> versión final, después del MVP (Fase 9) y del dashboard (Fase 10) (D-129).

**Objetivo:** versión de entrenamiento para la segunda semana de noviembre de 2026, con la
identidad del operador y el historial de Assessment confiables (ver "Revisión con Ernesto").
Plan aprobado por el usuario el 2026-10-12 y **reordenado el 2026-10-13** dos veces: tras
la validación con técnicos y tras la trainer; el usuario terminó la 013 antes que T7
(2026-10-13). Orden vigente: **013 ✅ → T7 → 015 → 014 ✅ → 016 ✅ → notas EP-7 ✅ → 012 ✅ → 017 ✅ → W23 (fila 12) ✅ → quitar `RequiresNCComponent` (fila 13) ✅** (filas 12 y 13 agregadas el 2026-10-21; T6, 2026-10-19; si las capturas de la 016 se atrasan, las notas EP-7 van primero). Cada tarea con funcionalidad nueva lleva su `/spec` (`docs/process.md`).

| # | Tarea | Tipo | Depende de |
|---|---|---|---|
| 1 | ✅ **T1 · Versión de prueba** en IIS interno (`d60bdfb`): `DEFAULT_PASSWORD` con su valor de siempre (D-18); BD con `00`–`09`, **incluido `05`** (Redefinity y Subensamble 1 de ejemplo: avisar a los testers que son demostración); `customErrors` en Release (parte mínima de AUD-28); reset de testers con `Database/Ops/ResetUserPassword.sql`. Prueba en el servidor real diferida (D-19). Los datos de la prueba se descartan | plan directo | — |
| 2 | ✅ **T2 · Escanear la Shop Order** (2026-10-13, `bf19e54`; plan `011-plan.md`) | spec 011 | — |
| 3 | ✅ **T7 · Mensajes y alertas de la guía de Guided** (2026-10-14) (retroalimentación de la trainer): notas de alerta en rojo en Resource Setup, LC de cierre y Search/Start, y los mensajes aprobados de LC de apertura, DC y Log NC. Las notas de Logout y del grupo Trainer entran con la spec 015. Junto con el bug del texto mal escrito en la selección de área | plan directo | — |
| 4 | ✅ **Cuentas, roles y registro de actividad** (2026-10-14, `e4ef6ae`…`5b9091a`; plan `013-plan.md`): roles excluyentes Operator / Administrator (los admins no entrenan), login de admin aparte con diseño propio, gestión de usuarios (crear, resetear a la contraseña por defecto, desbloquear, borrar con su historial; el rol no se cambia desde la UI, enmienda del 2026-10-13), bloqueo de 15 min tras 5 fallos, último acceso visible y motivo + acción de cada intento. El reclamo de cuenta ajena no se cierra (D-20) | spec 013 | — |
| 5 | ✅ **Ajustes de la trainer** (2026-10-14) (spec aprobada el 2026-10-14): Resource Setup con equipos precargados (generados al abrir el control; guardar con alguno es intento fallido); **Logout** como último paso después de Complete (0.05, sale de Complete; clic simulado → resumen; antes del último paso, intento fallido en Assessment e ignorado en Guided); grupo de DC **`DCGTM_TRAINEE/A`** opcional en todas las áreas con DC (nombre: letras y espacios, formato) | spec 015 | — |
| 6 | ✅ **T4 · Tres métodos de Log NC** (2026-10-19, `b685f1d`…`0654e9d`; plan `014-plan.md`): desecho rápido, reparación rápida y extendida, obligatorios en las áreas con Assembly (LoFlo, Redefinity); Moldeo solo desecho rápido (D-53). Nota de hallazgo con la pieza defectuosa (en Assessment, otra pieza, otro código u otra decisión es intento fallido: error 1 de la trainer), 4-5 piezas por orden Serialized, cantidad del split calificada en Moldeo, reparaciones con los códigos reales `F_FR_GENERAL` y `F_FX_GENERAL` (enmienda 5) y pesos nuevos (Log NC 0.05 × 3, DC 0.25). La E4 (Assembly Data por tipo de material, W13) se recortó y quedó diferida (enmienda 6) | spec 014 | banners provisionales hasta conocer el de la reparación rápida y el de Local Rework (V-3, V-5) |
| 7 | ✅ **T10 · Preparación antes de SAP** (2026-10-19, `50aca9d`…`e1227dd`; spec aprobada el 2026-10-19, `docs/specs/016-preparacion-antes-de-sap.md`; plan `016-plan.md` aprobado el 2026-10-19): paso nuevo "Ingreso a MES" al principio de las guías (script de limpieza → Repositorio → Hora a Hora 3.0 → Recursos MES → login de MES con el diseño SAP NetWeaver), Logout con la limpieza final (EP-3), login de la app único y neutro (enmienda 2 de la spec 013); Ingreso 0.05 y DC −0.05. Flujo en `docs/ui-reference/pre-sap-flow.md` | spec 016 | — |
| 8 | ✅ **T3 · Modo práctica** (2026-10-20, `1b45779`…`56e8fcf`; spec `docs/specs/012-modo-practica.md` y plan `012-plan.md`, aprobados el 2026-10-20, con las enmiendas 1 y 2). Lo que se pidió: La Entrega 1 de su plan es el refactor **`ModePolicy`** (concentrar en un objeto lo que hoy decide cada modo: penaliza, fuerza avance, guía visual, pistas; sin cambio de comportamiento, con tests), que se implementa después de aprobar la spec porque ella define qué capacidades expresa. **Spec aprobada el 2026-10-20** (`docs/specs/012-modo-practica.md`): **Práctica libre**, sin guía ni penalización, con Ver pista siempre disponible, botones de otro paso con aviso y sin intento, reintentos ilimitados, Saltar paso / Ir a… (en vez de guías por tramo) y resumen con intentos por paso. Plan `012-plan.md` aprobado el 2026-10-20 (4 entregas: `ModePolicy`, modo, pista y saltos, resumen) | spec 012 | — |
| 9 | ✅ **T6 · Evaluación práctica → Assessment** (QD001043644, 2026-10-19): contraste y decisiones en `docs/ui-reference/evaluacion-practica-mes.md`. La evaluación es insumo, no se replica: el simulador cubre como mínimo lo que pide. Sin cambios de pesos ni del criterio de aprobación (EP-1, EP-2); W13 sigue diferida (EP-6). Salieron: EP-3 → 016, EP-7 → fila 11, EP-4/EP-5 → fila 10 | análisis | — |
| 10 | ✅ **Spec 017 · Mensajes de error del MES** (2026-10-21, `b3dfff8`…`5e07a1b`; enmiendas 1 a 7 del plan; en Práctica libre la explicación va en "¿Qué significa?" junto al mensaje, enmienda 7) (EP-4, EP-5; spec `docs/specs/017-mensajes-del-mes.md`, aprobada el 2026-10-21; insumo `docs/ui-reference/errores-mes.md`): **solo errores reactivos** (D-92). Muestra el mensaje real del MES, en inglés y con número (banner del POD, franja de ventana, OK de Split que cierra), cuando el operador provoca la situación, incluidos los clics fuera de secuencia que el MES rechazaría. Es así en los tres modos, **sin cambiar la calificación**, con explicación en español en Guided y en Ver pista. Absorbe AUD-63/64/69 (D-95). Resource Setup solo gana el Setup Type Material (D-94, D-100). Fuera: batch inválido, cantidad insuficiente, TSM expirado, SFC en otro recurso, componente no requerido y REMOVE ALL + Save → Insert New (D-96, D-97, D-100) | spec 017 (plan `017-plan.md`, aprobado el 2026-10-21, 4 entregas) | — |
| 11 | ✅ **Notas de comprensión en la guía de Guided** (EP-7, 2026-10-20, `9a5433c`): cuándo iniciar y cerrar sesión, riesgo de una sesión abierta, cuándo repetir Resource Setup y Line Clearance, consumir antes del MPI; cada una en su paso, como las de T7. Assessment no las evalúa. Las de login y limpieza de caché, con los pasos de la 016. Plan y textos aprobados el 2026-10-20: ver "Plan de la fila 11" | plan directo | — |
| 12 | ✅ **W23 · SFC serializado `{12NC}-{serial}`** (2026-10-22, `7abda99`; captura del POD real de Wires, 2026-10-15): las piezas de una orden Serialized pasan de `{ShopOrder}-{sufijo}` a `{12NC}-{serial}`, con un 12NC simulado por área y seriales consecutivos de 1 a 6 dígitos (D-121, D-122, enmendada el 2026-10-22); el As-Built de Log NC muestra el 12NC (D-123; enmienda 7 de la spec 014). Sin cambio de BD ni de puntaje. Ver "Plan de la fila 12" | plan directo | spec 017 cerrada |
| 13 | ✅ **Quitar `RequiresNCComponent`** (2026-10-22, `a3e1ce7`; con `Database/Ops/RunAllScripts.ps1` para recrear la BD) (deuda desde la enmienda 4 de la spec 014): la columna `Areas.RequiresNCComponent` y lo que la lee, sin cambio de comportamiento. Se recrea la BD con `00`–`09` (D-124). Ver "Plan de la fila 13" | plan directo | fila 12 |

**Plan de la fila 11 · notas EP-7** (aprobado por el usuario el 2026-10-20, con la
verificación de que no repiten lo de otros pasos). La spec 016 ya cubrió el inicio de
sesión (subpaso `info` del Ingreso a MES), cuándo cerrarla y el riesgo de dejarla abierta
(nota del subpaso `logout`). Quedan tres notas normales (no alertas), solo texto en
`GuideScriptBuilder`, en una entrega. No cambian BD, puntaje ni Assessment (no recibe la
guía), y ninguna prueba compara estos textos.

| Paso · subpaso | Texto aprobado | Cambio |
|---|---|---|
| Resource Setup · `open` | "Resource Setup **se repite cada vez que se sustituye un equipo**: mantenimiento preventivo, correctivo o calibración." | Nota nueva; la alerta de `removeAll` no cambia |
| Line Clearance de apertura · `activities` | "Siempre después de **Resource Setup**: la limpieza física y la del sistema.<br>Se registra de nuevo cuando:<br>• cambia el usuario o la Shop Order,<br>• queda un SFC activo,<br>• vence el tiempo configurado." | Reemplaza la nota de T7 (el cambio de orden queda dentro). El LC de cierre conserva su alerta y no lleva estas condiciones: W7 sigue abierta |
| Assembly · `open` | "**Consuma los componentes antes** de empezar la instrucción del MPI." | Nota nueva; solo en las áreas con Assembly |

**Plan de la fila 12 · W23, SFC `{12NC}-{serial}`** (**aprobado por el usuario el
2026-10-21**, con D-121 a D-128). Hallazgo: `docs/ui-reference/wi-produccion-disposables.md`
→ W23. Es dato de la orden simulada (`PodService`): no cambia el puntaje, la BD ni los
pasos. Una entrega. Verificado en el código en `5565b10`.

- **12NC simulado por área (D-121):** `PodDefaults.MaterialNumber(Area)` devuelve 12
  dígitos fijos por área, como los demás valores simulados de `PodDefaults` (ruta, BOM,
  operación): `"3000"` + el `Id` del área con 8 dígitos (ej. `300000000003`). No pasa por
  la BD. Se reemplaza por el real cuando negocio lo dé. El usuario aceptó que el 12NC de un
  área cambie entre BDs si su `Id` cambia (D-125).
- **Seriales consecutivos de 1 a 6 dígitos (D-122):** `SerialSfcGenerator.Generate` recibe el
  12NC en vez de la Shop Order. Sortea **primero cuántos dígitos** tiene el inicio (1 a 6,
  como los sufijos de hoy: `MaxSuffixDigits = 6`) y después el número, sin ceros a la
  izquierda (D-126). Numera las 4-5 piezas consecutivas: `{12NC}-{inicio}`,
  `{12NC}-{inicio+1}`… (ej. `-7` … `-11`, `-46705` … `-46709`). La última no pasa de
  `999999`: si el inicio sorteado no alcanza para las 4-5 piezas, se baja. Actualizar el
  comentario de la clase.
  - **Enmendado 2026-10-22** (aviso de Desarrollo al empezar): el plan decía "1 a 5, como
    los sufijos de hoy", pero hoy son de 1 a 6. El usuario eligió de 1 a 6, como hoy, aunque la
    única captura real tiene 5 dígitos (`-46705`).
- **`SimulatedOrder`** gana `MaterialNumber`. `SimulatedOrder.Create` lo recibe de
  `PodService` (`:314`), que conoce el área. NonSerialized no cambia: su lote sigue siendo
  `CR9A` + 8 dígitos (`DeriveSFCCode`), porque no hay captura del SFC real de un lote
  (D-127; pregunta abierta para el ambiente de testing, T9).
- **As-Built de Log NC (D-123):** `PodDefaults.MaterialVersion` pasa a `{12NC}/A`
  (`GuidedTest.aspx.cs:1450`, campo `material`). `SimBom` sigue con la Shop Order, porque es
  un dato SIM (D-128). No se califica: solo se muestra. Esto corrige el supuesto de la spec 014, P-11
  ("el material es la Shop Order, prefijo de los SFC"): se registra como enmienda fechada en
  esa spec al cerrar.
- **Verificar que nada dependa del prefijo:** hoy ningún código parte un SFC Serialized por
  `-` ni lo compara con la Shop Order (el escaneo de la spec 011 compara contra la Shop Order
  y la lista de piezas). Desarrollo lo confirma.
- **Tests:**
  - `SerialSfcGeneratorTests` (`TrainingApp.Test/Class1.cs:71`): formato
    `^\d{12}-[1-9]\d{0,5}$`, 4-5 piezas únicas y consecutivas, la última ≤ `999999`, en
    muchas semillas;
  - `PodDefaults.MaterialNumber`: 12 dígitos y estable para la misma área.

  Los tests que usan `"000123-430"` como dato no dependen del formato y pueden quedar
  igual.
- **Docs al cerrar:**
  - `docs/ui-reference/general-flow.md:511` y `docs/glossary.md` → "Pieza (Serialized)",
    con `{12NC}-{serial}`;
  - W23 en `wi-produccion-disposables.md` → resuelto;
  - la enmienda de la spec 014 (P-11);
  - `docs/architecture.md`, si nombra el formato;
  - `status.md` y `history.md` (Desarrollo); la fila 12 ✅ (Arquitectura y Planeación).

**Checklist en el navegador:**
- LoFlo y Redefinity: Resource Setup → Start. Las 4-5 piezas muestran `{12NC}-{serial}`
  consecutivos, y la Shop Order va aparte en su columna.
- Escanear la Shop Order y elegir piezas en Browse Work List funciona como antes (spec 011).
- En la reparación rápida, el As-Built muestra `{12NC}/A`.
- El `15246` de Complete (spec 017) nombra los SFC nuevos.
- Moldeo (NonSerialized) sin cambios.
- Una sesión de Assessment completa da el mismo puntaje que antes con las mismas
  respuestas.

**Plan de la fila 13 · quitar `RequiresNCComponent`** (decisión del usuario D-124;
**aprobado por el usuario el 2026-10-21**). La columna no se lee desde la enmienda 4 de la spec 014:
Component en Log NC lo decide el método del código. Sin cambio de comportamiento. Una
entrega, después de la fila 12. Verificado en el código en `5565b10`: estas son todas las
referencias.

- **BD:**
  - `00_Schema.sql`: quitar la columna, su `DF_Areas_RequiresNCComponent` y el comentario
    (`:33`, `:46`);
  - `02_spTrainingGuide.sql`: quitarla de los dos `SELECT` de `spAreaGet*` (`:67`, `:81`);
  - `05_SeedData.sql` (`:37`), `06_RCMoldeoCatalog.sql` (`:23`, `:39`, `:44`) y
    `08_SensoresLoFloCatalog.sql` (`:28`, `:38`, `:43`): quitarla de la tabla de valores,
    del `INSERT`, del `UPDATE` y del comentario.
- **Código:** `DAL/Repositories/AreaRepository.cs:43` y `Domain/Area.cs:13`.
- **Verificación:**
  - rebuild y tests;
  - los scripts en una BD descartable de LocalDB (`docs/process.md` §6: instalación limpia
    con `00`–`09`, y comparar que `Areas` tenga los mismos datos menos la columna);
  - la BD del usuario se **recrea** con `00`–`09`, sin `ALTER` aparte (D-124). Se pierden
    las sesiones de prueba.
- **Docs al cerrar:**
  - `docs/domain.md` (`:40` y `:336`, la mención de `RequiresNCComponent`);
  - `docs/status.md` → "Deuda y riesgos" (quitar la entrada) y "BD de desarrollo del
    usuario" (la fila de la recreación);
  - `history.md` (Desarrollo); la fila 13 ✅ (Arquitectura y Planeación).

**Checklist en el navegador:** con la BD recreada, el login, una sesión de cada área (Moldeo,
LoFlo, Redefinity) hasta Log NC y el campo Component de la reparación rápida, que se sigue
mostrando como antes.

Si el tiempo no alcanza, se recorta primero T3 (modo práctica), después T10 y T4 si las
capturas o las pantallas no llegan; nunca la spec 013: sin ella solo el desarrollador administra las cuentas y no se
guarda el registro de actividad que necesita el dashboard.

**Decisiones del usuario (2026-10-12 y 2026-10-13) para la spec 013 y la Fase 10:**
- **D-18:** `DEFAULT_PASSWORD` sigue en `Constants` (versionado) y, en T1, con su valor de
  siempre (corrección del usuario, 2026-10-12: no se cambia).
- **D-20 (2026-10-13, spec 013):** el reclamo de cuenta ajena **no se cierra**: solo
  detección (trainer presente en las sesiones + último acceso visible para el usuario), con
  el riesgo aceptado también en producción. Reemplaza el "obligatorio" que tenía la 013. Se
  revisa si Assessment pasa a usarse como evidencia de la CAPA o si se entrena sin trainer.
- Solo los **administradores** ven resultados y dashboards: un único rol además del de
  operador, sin alcance por área.
- La API de empleados da solo Code1, nombre completo y correo: el área de un operador no sale
  de la API. Si T5 decide escenarios, las métricas por área necesitan un área del usuario
  asignada en la app (a decidir en la spec del dashboard).
- Todos tienen cuenta corporativa (`nombre.apellido@philips.com`). Si se revisa D-20, las
  opciones son un código de activación del trainer, el correo (requiere SMTP y que el
  operador lea el correo) o Windows Authentication (solo si cada operador entrena con su
  propia sesión de Windows).

**No incluye:** dashboard y reportes (Fase 10), nuevas áreas (espera T5), RF-16 (diferido
otra vez el 2026-10-13, aunque lo pide la trainer) y lo descartado el 2026-10-12: Tool List, variantes de Complete y
Data Collection para todas las piezas.

### Fase 9 — Deploy del MVP
**Plan aprobado el 2026-10-22: `docs/specs/fase-9-plan.md`** (D-129 a D-134). El MVP se
publica el 2026-10-22 en el IIS interno para recoger retroalimentación (E1: la parte de
Desarrollo está cerrada en `584cdf2`; faltan la publicación, la prueba de humo y el
lanzamiento, que hace el usuario). AUD-28 (lo barato) y AUD-29 (parches) se suman en una
2.ª publicación (E2), que se hace **junto con la Fase 10** (D-135). Mientras tanto avanzan la
Fase 10 y las mejoras, y la **versión final se entrega en la 2.ª semana de noviembre**. Los
datos del MVP se descartan (D-130). Lo que sigue es el texto original de la fase.

**Objetivo:** deploy en servidor interno con IIS: connection string de producción, App Pool,
permisos, prueba de humo. Incluye AUD-28/29.
- **Arranque en frío en IIS** (medido 2026-10-07 en desarrollo): tras reciclar el App Pool,
  el primer request compila el markup (3-10 s por carpeta; ~18 s todo el sitio). Resolver al
  publicar: precompilar el sitio y configurar el App Pool con `startMode="AlwaysRunning"`,
  `preloadEnabled="true"` (Application Initialization) e idle timeout 0.
**No incluye:** CI/CD automatizado, monitoreo avanzado.

### Fase 10 — Dashboard de Training e historial por operador (T8) ✅ (2026-10-23)

**Cerrada el 2026-10-23** (`51193c3`): las ocho entregas, curva y ficha incluidas, probadas
por el usuario. Durante la fase se sumaron D-143 a D-150 (enmiendas 1 y 2 de la spec,
enmiendas 1 a 4 del plan). Qué se hizo: `docs/history.md` → "Fase 10". Lo que sigue:
"Después de la Fase 10".

**Spec aprobada el 2026-10-22: `docs/specs/018-dashboard-training.md`** (D-136 a D-142).
Va en la versión final de la 2.ª semana de noviembre, junto con la E2 de la Fase 9 (D-135).
El plan por entregas (`018-plan.md`, 8 entregas) se aprobó el 2026-10-22. Antes de su E1 va
la E2 de la Fase 9.

**Objetivo:** que los administradores (gerencia, trainers y el desarrollador) vean:
- quién está listo, por guía;
- dónde se falla más;
- cómo avanza el entrenamiento.

Son cinco vistas en Administración (Resumen, Operadores, Operador, Pasos y Seguimiento),
con Excel, gráficas, curva de aprendizaje y ficha imprimible. Solo lectura y con los datos
que ya se guardan.

**No incluye:**
- el historial visible para el operador;
- el área del operador (diferida);
- el rol Trainer;
- re-entrenamiento por vencimiento;
- marcar casos de seguimiento como atendidos;
- registrar las descargas;
- caché;
- cambios en cómo se entrena o se califica;
- tablas nuevas.

**Si falta tiempo,** se recortan primero la curva y la ficha.

### Fase 11 — UI/UX del dashboard ✅ (2026-10-27)

**Cerrada el 2026-10-27** (`0888717`): las seis entregas, probadas por el usuario, y AC-7
medido con la BD de volumen (todas las vistas por debajo de 3 s, sin tocar SPs ni índices;
cifras en `019-plan.md`). Decisiones D-153 a D-164. Qué se hizo: `docs/history.md` → "Fase
11". Lo que sigue: "Después de la Fase 10".

**Pedido del usuario (2026-10-23, al aprobar la E6 de la spec 018):** la funcionalidad del
dashboard es correcta, pero los diseños "son muy básicos y feos". Hay que "actualizar los
ui/ux de todo este dashboard y todos sus módulos, también incluido el diseño del excel".
Empieza **después de la Fase 10 y de R1/R2** (orden más abajo, en "Después de la Fase 10").

**Objetivo:** rediseñar la presentación del dashboard sin cambiar qué calcula.

**Incluye:**
- las vistas Resumen, Operadores, Operador, Pasos y Seguimiento, con sus filtros, tablas y
  navegación;
- las gráficas de Chart.js;
- el diseño de los `.xlsx`: encabezados, formato, anchos y estilos.

**No incluye:**
- cambios en los cálculos, reglas o datos del dashboard (spec 018: `ReadinessPolicy`,
  `StepRuns`, seguimiento, D-136 a D-148);
- vistas o descargas nuevas;
- rediseñar el POD o el área del operador.

> **Brief de diseño aprobado el 2026-10-26: `docs/specs/019-ui-ux-dashboard.md`** (D-153 a
> D-159). Cambia el alcance de arriba:
> - **entran** el login, el cambio de contraseña y Usuarios, todo lo que usa `Admin.Master`
>   (D-154);
> - **entran** Bootstrap 5.3, verificado primero en una entrega propia (D-155), el modo oscuro
>   (D-157) y Bootstrap Icons (D-156).
>
> El [mockup v1](https://claude.ai/artifact/Uxqr9utFzd8w6uBgp3EYYd) está aprobado como base,
> con las pantallas sin mockup de D-160. El **plan `019-plan.md`** tiene 6 entregas
> (Bootstrap 5.3 verificado, sistema de diseño y administración, Resumen, Operadores y
> Operador, Pasos y Seguimiento, Excel) y se aprobó el 2026-10-26.

### ⏸ Para retomar (Planeación, 2026-10-29)

**Estado:**
- ✅ **Spec 023** (filtro de período) cerrada e integrada. Tests: 459.
- **Fase 12 (022):** integradas la E1 (tokens), la E2a (Login, `53a9d52`) y la E2b (Cambio de
  contraseña, `8ccda11`) (merge `07363e3`). Los docs de Diseño (`cloud/022-mockups-por-pantalla`:
  brief 4 con `PeriodFilter`; E6 y briefs 6 y 7 bloqueados por la 024) integrados el 2026-10-29
  (`97e1daf`). Las demás pantallas esperan su mockup. Orden vigente: D-219.
- ⏳ **Spec 024 · Dashboard por área:** la nube hizo spec, plan y E1 a E4 ("no verificada").
  **Revisada en local el 2026-10-29** contra la spec y el plan: sin desvíos de la regla ni bugs;
  enmienda 1 (D-272 a D-274) en la rama local **`spec-024`** (`ef48efa`, worktree
  `.claude/worktrees/spec-024`, desde `73a2829`, sin el hook de la nube). Detalle en "Spec 024".

**Sigue, por sesión:**
- **Desarrollo (local):** la **024** en `spec-024`: `git merge TrainingApp`, copiar
  `connectionStrings.config` y la junction a `packages`, y después E1 a E4 con su ciclo (Rebuild,
  `aspnet_compiler`, `10_spDashboard.sql` en su BD, tests y el "funciona" del usuario por
  entrega; checklist de cada entrega en `024-plan.md`). Va antes de la E6 de la 022. En
  paralelo o después, en `spec-022`, la **E3 de la 022 (Guías, lista)**: aprobada la 2b
  (D-233 a D-236, integrada en `c8809ce`); punto de partida `cloud/022-guias` (`f715fe1`, "no
  verificada"), con el conteo y el orden movidos a la BLL sin test (D-235) y el aviso que nombra
  la guía (D-236); checklist en `022-plan.md` → E3. Sin tocar los archivos de la 024.
- **Diseño UI/UX (nube):** briefs 6 y 7 enmendados con la 024 y D-272/D-273 (`51e7cb2`,
  integrado el 2026-10-29 en `e3b9df3`) y Guías aprobada (2b, `2bf024c`, integrado en
  `c8809ce`). Siguen en Claude Design Seguimiento, Pasos, Operadores y Operador. La E6 se
  implementa cuando la 024 esté en `TrainingApp`.
- **Arquitectura y Planeación:** al cerrar Desarrollo la 024, marcarla ✅ aquí; revisar las
  ramas de handoff de la 022 que lleguen.

**Pendientes del usuario:**
- Antes de publicar en producción: correr los scripts que lista `docs/status.md` (`00`, `01`,
  `02`, `03`, `05`, `10`, `11` y `12`) y avisar a los trainers que los operadores eligen su área
  en el próximo ingreso (D-170).
- Subir `TrainingApp` a `main` cuando lo decida (D-5).
- Limpieza: `git worktree prune` (`fase-10`) y borrar la rama `spec-021`. No borrar
  `.claude/worktrees/fase-11`: ahí trabaja Desarrollo en `spec-022`.

**Numeración vigente:** local D-241 (rango D-240 a D-259) y B-5; Diseño (022) D-237 (hasta
D-239) y B-30; la 024, D-275 (hasta D-279) y B-40 a B-49.

**Comunicación:** la nube recibe mensajes, pero no puede contestar. Su avance llega por su rama
o lo trae el usuario. Desarrollo y "rediseño tabla operadores por área" son sesiones locales y sí
contestan.

### Spec 023 — Filtro de período en Pasos y Resumen ✅ (2026-10-28)

**✅ Cerrada el 2026-10-28** (E1 a E4 en `spec-022`: `2ffb614`, `869f99a`, `5359a5a`, `9c318df`; AC-9 del usuario; qué se hizo: `docs/history.md` → "Spec 023").

**Spec aprobada el 2026-10-28: `docs/specs/023-filtro-periodo.md`** (D-209 a D-218), con su plan
`023-plan.md` (4 entregas, aprobado el 2026-10-28). Pedido del usuario (2026-10-27): un filtro de fechas
en Pasos y Resumen con buena UX. Enmienda el punto 8 de la spec 018.

**Objetivo:** que gerencia y trainers revisen un grupo o una capacitación en fechas concretas,
saquen reportes periódicos, comparen antes y después de un cambio y sigan el día a día.

**Incluye:** en Pasos y Resumen, un control de período con 7 opciones (Hoy, Esta semana, Este
mes, Mes anterior, Últimos 30 días, Todo y Personalizado… con Inicio y Fin), Este mes al
entrar y días de la planta; la comparación con el período anterior (flecha, valor, referencia y
base) en la tasa de aprobación, las sesiones y el Top 5 del Resumen y en el % con fallo de
Pasos; la gráfica de actividad por día, semana o mes; el Excel de Pasos con las columnas de
cambio y el rango en el título de los dos Excel.

**No incluye:**
- el período en Operadores, Operador (detalle, curva y ficha) y Seguimiento;
- que el estado "listo" use el período (sigue con todo el historial, DA-1);
- comparar dos rangos libres lado a lado;
- cambio en la tasa de primer intento y en el puntaje promedio;
- una librería de calendario;
- guardar el último período entre visitas;
- tablas o índices nuevos (salvo que la medición de la E4 lo pida, con aviso a Planeación).

**Orden:** va **antes** del mockup de Pasos de la 022 (D-217). **D-219 (usuario, 2026-10-28):**
**022 E1** (tokens de D-221 y `.mt-status-line`, desde `cloud/022-login-3b`) → **023 E1 a E4** → **022 E2a** (Login) y el resto de la 022 según sus mockups (Pasos, después de cerrar la 023). Así `admin.css` no se toca a la vez. (Corregido 2026-10-28: la flecha de la 023 usa
`--mt-ok` y `--mt-bad`, no los tokens de D-221, que son del panel de marca del Login; el aviso de
período inválido suma `.mt-alert-warn` con `--mt-warn`.) La E2 de la 022 se parte en E2a (Login) y E2b (Cambio de contraseña, con
su mockup).

### Spec 024 — Dashboard por área (Operadores y Operador) ⏳

**Pedido del usuario (2026-10-28, por la sesión "rediseño tabla operadores por área"):** hoy
Operadores tiene una columna por cada guía publicada y todos los operadores salen "Sin iniciar"
en las guías de otras áreas: es ruido e información falsa. **Levanta** lo que la 021 dejó fuera
(021 → Out: filtrar el dashboard por área y "Sin iniciar" por área; D-175) y el "Queda fuera:
filtrar por área" de los briefs 6 y 7 de la 022. Es una regla nueva, no solo diseño: va con
`/spec`.

**Propuesta de partida** (la spec la pregunta y la cierra con el usuario):
- **Operadores:** sin columnas por guía; una columna "Estado en su área" (solo las guías
  publicadas de su área), un filtro de Área y otro de Guía en cascada; las guías de otras áreas,
  solo como "+N de otra área"; "Sin área" con su propio estado; el Excel igual a la pantalla.
- **Operador:** "Su área" (todas sus guías) y "Otras áreas" colapsada, solo con las guías que
  tienen sesiones; reforzar y curvas, su área primero; sesiones con el área; la ficha impresa
  con su área completa y las otras en una línea.

**Decisiones abiertas:**
1. qué significa "Sin iniciar" (hoy, en `domain.md` → Dashboard: ninguna sesión en ninguna
   guía publicada) y cómo afecta el filtro Estado;
2. el Resumen, que cuenta "Sin iniciar" sobre todos los operadores;
3. "su área" es siempre la actual, sin historial;
4. filtrar por una guía de otra área;
5. el valor por defecto del filtro de Área;
6. un parámetro de área en el SP de operadores, porque la paginación es en SQL. Por eso va como
   spec propia: la 022 no admite SPs nuevos.

**Proceso (usuario, 2026-10-28):** de punta a punta en la nube (`process.md` → "Variante
temporal" → "Spec 024") y revisión final en local contra la spec y el plan; después, el ciclo de
Desarrollo.

**Revisión local (Arquitectura y Planeación, 2026-10-29):** la nube cerró la spec (D-260 a
D-271), el plan y E1 a E4 en `cloud/024-dashboard-por-area` (marcadas "no verificada"; con Mono
compiló y pasaron 461 tests). La spec responde las seis dudas de arriba (la 2 y la 6, con P-2 y
P-3: el filtro va en la BLL, sin parámetro nuevo en el SP). El código coincide con la spec y el
plan, sin bugs: `OperatorArea` es la única regla; el filtro se aplica antes de paginar;
`CanViewReports` sigue en cada método. **Enmienda 1** (usuario): D-272 (Sesiones con columna
Área en lugar de Guía), D-273 (la ficha imprime Reforzar y Sesiones completos) y D-274 (el hook de
inicio y `tools/` de `b95b27c` no entran). Desarrollo la trae desde la rama local `spec-024`
(`ef48efa`), no desde la de la nube.

**Orden:** antes de la **E6 de la 022** (Operadores y Operador). Al aprobarse la 024, Diseño UI/UX
enmienda con fecha los briefs 6 y 7 de `022-briefs.md` (columna única y filtro de Área) antes de
mandar esas pantallas a Claude Design.

### Fase 12 — Mockups por pantalla (spec 022)

**Brief de diseño aprobado el 2026-10-27: `docs/specs/022-mockups-por-pantalla.md`**, con su
plan `022-plan.md` (9 entregas) y los briefs para Claude Design en `022-briefs.md`. Se hizo en la
nube (`cloud/022-mockups-por-pantalla`, D-202 a D-207) y se integró el 2026-10-27. Nace de que
al usuario no le gustaron las pantallas sin mockup de la Fase 11: reemplaza D-160 y el "sin
mockup" de D-163.

**Objetivo:** que cada pantalla tenga su propio mockup aprobado en Claude Design (3 alternativas
que difieren en la estructura) y se implemente contra la exportación guardada en
`docs/specs/022-design/<pantalla>.html`, que es su criterio de aceptación.

**Incluye:** Login, Cambio de contraseña, Guías, Pasos, Seguimiento, Operadores, Operador (con la
ficha impresa), Usuarios e Inicio del operador; cambios al sistema de diseño (cada uno una D-n,
para toda la app; el Login fija el sistema); métricas derivadas en la BLL con los datos que la
vista ya carga, cada una con su D-n (D-205).

**No incluye:**
- el Resumen (más allá de heredar tokens), "Elige tu área", el POD y los Excel;
- datos, SPs o columnas nuevos; cambios de reglas, filtros, estados o cálculos;
- acciones, filtros o descargas que hoy no existen;
- React u otro framework, fuentes web, CDN y colores de marca corporativa.

**Orden, en paralelo con la 021 (D-207, P-7):** cada entrega espera su mockup aprobado. E1
(sistema de diseño), E2 (Login y Cambio de contraseña), E3 (Guías), E4 (Pasos) y E5
(Seguimiento) son libres y se intercalan con la 021 cuando el usuario se lo pida a Desarrollo,
nunca a la mitad de una entrega de la 021. E6 (Operadores y Operador) va después de la E5 de la
021; E7 (Usuarios), después de su E4, y E8 (Inicio del operador), después de su E3. **La 021
se cerró el 2026-10-27:** desde entonces ninguna entrega de la 022 espera a la 021, solo su mockup.
- **Coordinación:** la 021 y la 022 tocan `admin.css` y `Users.aspx`. La 022 se implementa en su
  propia rama (`spec-022`), y la E1 se coordina con Desarrollo para no pisar la 021.
- **Enmiendas de la nube integradas (2026-10-28, `863f925`):** la 021 cerrada (ninguna entrega
  la espera, solo la E1 y su mockup); Usuarios sin "Quitar área" (D-208), con el lápiz en la
  columna Área; rango D-220 a D-239 y B-30 a B-39; los briefs se usan con Claude Design leyendo
  el repo (rama `TrainingApp`) con un prompt corto por pantalla (`022-briefs.md` → "Cómo
  usarlos").
- **Login aprobado (2026-10-28, `f197a85`):** alternativa 3b, panel dividido (D-220 a D-225):
  tokens `--mt-brand-*` y `--mt-on-brand*` (D-221), `.auth-*` y `.mt-status-line` (D-222),
  `Admin.Master` con `FullBleed` solo en el login (D-223), errores en la línea de estado en vez
  de SweetAlert (D-224) y textos nuevos (D-225). El criterio pasa a ser capturas PNG y
  `022-design/<pantalla>.md`, no el HTML exportado (D-227, excepción a P-3). Flujo con handoff
  de Claude Design (D-226, `process.md` → "Variante temporal"): la rama `cloud/022-login-3b`
  (`c7294ee`, "no verificada") es el punto de partida de la E1 y la E2a; no se integra tal cual.
- **Orden con la 023 (D-219):** ver "Spec 023 → Orden". La E2 se parte en E2a (Login) y E2b
  (Cambio de contraseña).
- **Cambio de contraseña aprobado (2026-10-28, `2fd0e8b`):** alternativa 2a (D-228): franja de
  marca y una tarjeta montada, 3 pasos en primer acceso, Nueva y Confirmar lado a lado y los
  requisitos fijos; `.pw-*` y `.mt-steps` reutilizable, sin tokens nuevos (D-229); pantalla
  completa siempre, también en el cambio voluntario (D-230); errores en `.mt-status-line` y
  botones `LinkButton` (D-231); textos nuevos (D-232). "Cancelar y cerrar sesión" solo nombra lo
  que ya hacía Cancelar (`SessionHelper.Logout`). Rama de partida `cloud/022-cambio-contrasena`
  (`221297f`, sale de la del Login): la E2b va después de la E2a.

### Después de la Fase 10

**Orden (usuario, 2026-10-23):**
1. ✅ R1 y R2, cortos: corrigen lo que hoy se le enseña al operador (2026-10-26).
2. ✅ Fase 11, UI/UX del dashboard (2026-10-27).
3. ✅ R3, Custom Resource Setup (2026-10-27). **Adelantado (usuario, 2026-10-26):** fue en
   paralelo con la Fase 11, en la sesión `Custom-RS` (spec 020).

**✅ Área asignada por operador (negocio, 2026-10-27; cerrada el 2026-10-27, `b06a993`):** cada operador tiene un **área asignada** y solo ve las
guías de esa área. Spec `021-area-operador.md` y plan `021-plan.md` (5 entregas), hechos en la
nube (`cloud/021-area-operador`) y aprobados por el usuario el 2026-10-27; integrados en
`TrainingApp` (`33158df`). **Levanta** "el área del operador (diferida)" del "No incluye" de la
Fase 10; sigue fuera filtrar el dashboard por el área del operador (spec 021 → Out). Al publicar,
todos los operadores existentes eligen su área en su próximo ingreso: avisar a los trainers.
Qué se hizo: `docs/history.md`. Pendiente del usuario: correr `00`, `01`, `02`, `05` y `10` en la
BD de producción antes de publicar (`docs/status.md`).

#### Revisión con Ernesto, MES (2026-10-22)
Registrada por Reuniones y Dudas. No cambia el foco: la Fase 10 sigue con su orden.
Ernesto **aprobó el contenido del simulador**.

| # | Tarea | Estado |
|---|---|---|
| R1 | **Quitar Hora a Hora del Ingreso a MES.** El flujo es Repositorio → Recursos MES → login de MES. Hora a Hora 3.0 es el conteo de producción, no MES; lo enseñan los trainers, pero no es parte del proceso de MES. Hoy el login califica "Log On sin Hora a Hora" como intento fallido (`MesLogin.cs`, unas 40 referencias; `docs/domain.md` → Ingreso a MES; D-72) | ✅ 2026-10-26 (`57df997`; spec 016 → enmienda 1, plan `r1-r2-plan.md`, D-151) |
| R2 | **Quitar las "acciones disciplinarias" de los textos de la guía** ("no nos corresponde": lo dicen los trainers, no la app). Queda solo "CRÍTICO". `GuideScriptBuilder.cs`: alerta de Resource Setup y Log NC | ✅ 2026-10-26 (`57df997`; plan `r1-r2-plan.md`, D-152) |
| R3 | **Custom Resource Setup:** la pantalla del futuro (piloto en noviembre en un área, sin fecha segura). Un administrador elige la pantalla con un flag por guía; la estándar queda como predeterminada. Capturas en `docs/ui-reference/Screens SAP MES REAL/Custom Resourse Setup/` | ✅ 2026-10-27 · spec `020-custom-resource-setup.md` (D-180 a D-201, enmienda 1) · plan `020-plan.md`: E1 `144d845`, E2 `a0c7c77`, E3 `3a18960` y `82d000d`, E4 `5d12e80` |

- **Confirmado:** el Logout va seguido del script de limpieza (como está, spec 016).
- **Pesos, el 70 % y la regla de aprobación:** los deja al equipo de training (no cambian).
- **MVP:** el link va a Ernesto, a Diego y a otros testers que él nombre, junto con la hoja de
  requerimientos.
- **Abierto:** la contraseña del login de MES.
- **Observado en la demo (sin numerar hasta confirmarlo):** al forzar el avance desde la BD, la
  reparación rápida pierde el campo del componente. Falta saber si pasa sin forzar; forzar
  desde la BD no es un flujo de la app. Si se confirma, es un B-n con plan directo.

