# Índice de decisiones (D-n) y bugs previos (B-n) — MESTrainingSimulator

> **Solo un índice:** una línea por decisión, para encontrarla y saber si sigue vigente. El
> texto, el porqué y las alternativas están en la fuente ("Dónde"); si algo de aquí no
> coincide con la fuente, manda la fuente. Creado el 2026-10-21 (antes, la última D-n se
> buscaba con `grep`).
>
> - **Siguiente D-n: D-242 (local, rango D-240 a D-259) · D-295 (nube, spec 022; rango D-280 a D-299). Siguiente B-n: B-5 (local) · B-30
>   (nube)** (B-4 lo usó la 021; B-20, R3). La numeración es continua entre auditorías, specs,
>   planes y planes directos (`docs/process.md` → "Clasificar el pedido").
> - **Rangos por frente (desde el 2026-10-27):** la sesión local (Arquitectura y Planeación y
>   Desarrollo: spec 021 y lo demás) usa **D-208 a D-219** y **B-5 a B-19**; la nube (spec 022,
>   mockups) usa **D-220 a D-239** y **B-30 a B-39**. La 022 ya usó D-202 a D-207 y la enmienda
>   de la E4 de la 021 es D-208 (las dos tomaron D-202 a la vez; se renumeró la de la 021, que no
>   estaba commiteada). Si un frente se queda sin números, pide el siguiente rango libre.
>   **2026-10-28:** la sesión local agotó D-208 a D-219 (la 023 y D-219) y toma **D-240 a
>   D-259**; el siguiente libre es D-260.
>   **2026-10-29:** a la nube de la 022 le quedan D-238 y D-239; al agotarlos sigue con **D-280 a
>   D-299** (reservado por Arquitectura y Planeación a pedido de Diseño UI/UX). El siguiente
>   bloque libre es D-300.
> - **Spec 024 (Dashboard por área, nube `TrainingApp-Desarrollo`, desde el 2026-10-28):**
>   **D-260 a D-279** y **B-40 a B-49**. Cerrada el 2026-10-29: usó D-260 a D-274; D-275 a D-279
>   y B-40 a B-49 quedan sin usar.
> - **Rangos anteriores, cerrados:** la Fase 11 usó D-153 a D-164, la spec 021 D-165 a D-179 y
>   R3 D-180 a D-201 (y B-20).
> - Quien numera una decisión o un bug agrega su fila **en el mismo commit** que la fuente.
>   Cuando una enmienda cambia una decisión, se actualiza su estado aquí.
> - **Estado:** vigente · enmendada (sigue, con cambios) · superada (la reemplazó otra) · sin
>   efecto · cerrada (era de una sola vez y ya se cumplió) · pendiente · provisional.
> - "Dónde" es relativo a `docs/`.

## Decisiones

| ID | Fecha | Decisión | Dónde | Estado |
|---|---|---|---|---|
| D-1 | 2026-10-01 | SFC Split forzado deja una fila de split por defecto de 100 unidades | audits/2026-10-01-auditoria.md (AUD-03) | vigente |
| D-2 | 2026-10-01 | Guided no penaliza: acertar en cualquier intento vale el 100% del paso | audits/2026-10-01-auditoria.md (AUD-07) | vigente |
| D-3 | 2026-10-01 | El cambio de contraseña pide siempre la actual (en el primer acceso, la por defecto) | audits/2026-10-01-auditoria.md (AUD-09) | vigente |
| D-4 | 2026-10-01 | Reiniciar una guía marca la sesión vieja `Abandoned` y conserva sus intentos | audits/2026-10-01-auditoria.md (AUD-01) | vigente |
| D-5 | 2026-10-01 | No se toca `main`: `TrainingApp` es la rama de desarrollo; los worktrees salen de ella | audits/2026-10-01-auditoria.md (AUD-30) | vigente |
| D-6 | 2026-10-01 | La pregunta de Line Clearance se queda en el código (igual en todas las operaciones) | audits/2026-10-01-auditoria.md (AUD-27) | vigente |
| D-7 | 2026-10-01 | Endurecer el deploy y actualizar paquetes, en la Fase 9 | audits/2026-10-01-auditoria.md (AUD-28, 29) | cerrada (2026-10-22: E2 de la Fase 9) |
| D-8 | 2026-10-01 | El usuario rota la contraseña de `sa` en su momento | audits/2026-10-01-auditoria.md (AUD-04) | pendiente |
| D-9 | 2026-10-02 | Una sola sesión activa por operador: iniciar una guía o hacer logout abandona las demás | audits/2026-10-01-auditoria.md | vigente |
| D-10 | 2026-10-02 | El estado del POD sigue en Session y la BLL lo reconstruye si falta | audits/2026-10-01-auditoria.md (AUD-26) | vigente |
| D-11 | 2026-10-02 | Los datos de simulación de `GuidedTest` pasan a constantes de la BLL | audits/2026-10-01-auditoria.md | vigente |
| D-12 | 2026-10-02 | Spec 009 = refactor sin cambios visibles; los bugs previos se deciden aparte | audits/2026-10-01-auditoria.md | cerrada |
| D-13 | 2026-10-06 | El ícono ⏻ cierra siempre la sesión de la app; `StartSession` gana logout | audits/2026-10-06-auditoria-final.md (AUD-33) | enmendada (spec 015: Logout es el último paso, D-21) |
| D-14 | 2026-10-06 | El Start de NonSerialized queda como está (`TxtSFC` de solo lectura) | audits/2026-10-06-auditoria-final.md | superada (spec 011: escanear en `TxtSFC`) |
| D-15 | 2026-10-06 | La inyección de repositorios en `PodService` espera a retomar la cobertura de tests | audits/2026-10-06-auditoria-final.md | vigente |
| D-16 | 2026-10-06 | Alcance de la auditoría final: E1-E4; AUD-40 y AUD-42 diferidos | audits/2026-10-06-auditoria-final.md | cerrada (AUD-40/42 siguen en la deuda) |
| D-17 | 2026-10-09 | La cuenta local se llama `User` (tabla `Users`), nunca `Operator` | history.md → "Refactor `Operator` → `User`" | vigente |
| D-18 | 2026-10-12 | `DEFAULT_PASSWORD` sigue versionada en `Constants`, con su valor de siempre | roadmap.md → "Fase 8.5" | vigente |
| D-19 | 2026-10-12 | T1 se aprueba como preparación, sin probarlo en el servidor real | history.md → "T1" | pendiente (prueba en el servidor real: la prueba de humo de la E1 de la Fase 9) |
| D-20 | 2026-10-13 | El reclamo de cuenta ajena no se cierra: solo detección, riesgo aceptado | specs/013-cuentas-roles-actividad.md; roadmap.md → "Fase 8.5" | vigente |
| D-21 | 2026-10-14 | Con la sesión de entrenamiento cerrada, Logout cierra la sesión de la app | specs/015-plan.md | vigente |
| D-22 | 2026-10-14 | Complete saca del POD las filas completadas | specs/015-plan.md | vigente |
| D-23 | 2026-10-14 | El grupo Trainer reemplaza en sitio el tercer grupo opcional en `05` y `08` | specs/015-plan.md | vigente |
| D-24 | 2026-10-14 | Nota del grupo Trainer en Guided: subpaso Info sobre su fila | specs/015-plan.md | vigente |
| D-25 | 2026-10-14 | En Guided, la captura de Resource Setup rechaza los valores precargados | specs/015-plan.md | vigente |
| D-26 | 2026-10-14 | Con algún precargado en Resource Setup no se aplican los avisos de formato | specs/015-plan.md | vigente |
| D-27 | 2026-10-15 | Spec 014 completa: reglas y calificación de los tres métodos de Log NC | specs/014-tres-scraps.md | vigente |
| D-28 | 2026-10-15 | Cada paso de Log NC tiene un hallazgo: pieza, defecto y disposición | specs/014-tres-scraps.md | vigente |
| D-29 | 2026-10-15 | Tres pasos de Log NC, 0.15 en total (0.05 cada uno) | specs/014-tres-scraps.md | vigente |
| D-30 | 2026-10-15 | Los tres métodos solo en las áreas con Assembly | specs/014-tres-scraps.md | vigente (de nuevo por D-53) |
| D-31 | 2026-10-15 | Se califica el código exacto que corresponde al defecto del hallazgo | specs/014-tres-scraps.md | vigente |
| D-32 | 2026-10-15 | En NonSerialized no se califica la cantidad del split | specs/014-tres-scraps.md | superada (enmienda 3: sí se califica) |
| D-33 | 2026-10-15 | El desecho rápido es de una pieza, aunque el MES permita varias | specs/014-tres-scraps.md | vigente |
| D-34 | 2026-10-15 | Reparación rápida por un solo camino: Scrap del componente y Yes a agregar | specs/014-tres-scraps.md | vigente |
| D-35 | 2026-10-15 | Qué salidas no cuentan y qué botón es equivocado en la reparación rápida | specs/014-tres-scraps.md | vigente |
| D-36 | 2026-10-15 | La ventana del componente nuevo se construye a partir de Assemble Components | specs/014-tres-scraps.md | vigente |
| D-37 | 2026-10-15 | Flujo de la extendida: Add-Done → "NC logged" → Select Disposition Function | specs/014-tres-scraps.md | vigente |
| D-38 | 2026-10-15 | Comments de `F_FX_GENERAL` obligatorio como formato | specs/014-tres-scraps.md | vigente |
| D-39 | 2026-10-15 | Component con Browse en todos los códigos | specs/014-tres-scraps.md | vigente |
| D-40 | 2026-10-15 | Moldeo también con los tres métodos | specs/014-tres-scraps.md | superada (revertida por D-53) |
| D-41 | 2026-10-15 | Componentes de Moldeo | specs/014-tres-scraps.md | sin efecto (D-53) |
| D-42 | 2026-10-15 | Split en Moldeo con tres SFC | specs/014-tres-scraps.md | sin efecto (D-53) |
| D-43 | 2026-10-15 | Alerta en rojo del subpaso 1 de Log NC (texto aprobado) | specs/014-tres-scraps.md | enmendada (D-152: sin la mención disciplinaria) |
| D-44 | 2026-10-15 | Notas de la guía en la reparación rápida (texto aprobado) | specs/014-tres-scraps.md | vigente |
| D-45 | 2026-10-15 | Pesos de Moldeo | specs/014-tres-scraps.md | sin efecto (D-53) |
| D-46 | 2026-10-15 | W13 en la 014: Assembly Data por tipo de control del material | specs/014-tres-scraps.md | en suspenso (enmienda 6: W13 diferida) |
| D-47 | 2026-10-15 | Tras la reparación rápida o Local Rework, la pieza sigue activa en el POD | specs/014-tres-scraps.md | vigente |
| D-48 | 2026-10-15 | Add y Done siguen siendo botón equivocado en los tres métodos | specs/014-tres-scraps.md | vigente |
| D-49 | 2026-10-15 | La nota muestra un hallazgo a la vez ("Hallazgo n de N") | specs/014-tres-scraps.md | vigente |
| D-50 | 2026-10-15 | Textos de la guía de los subpasos nuevos de Log NC | specs/014-tres-scraps.md | vigente |
| D-51 | 2026-10-15 | La extendida sigue la captura del sandbox, no la WI | specs/014-tres-scraps.md | vigente |
| D-52 | 2026-10-15 | Nombres de operación con el formato real (número, código y descripción) | specs/014-tres-scraps.md | vigente |
| D-53 | 2026-10-15 | Moldeo solo entrena el desecho rápido | specs/014-tres-scraps.md | vigente |
| D-54 | 2026-10-15 | Return to any previous: se cierra todo y la pieza sale del POD | specs/014-tres-scraps.md | vigente |
| D-55 | 2026-10-15 | Banner provisional al terminar la reparación rápida | specs/014-tres-scraps.md | confirmado en testing (V-3, T9) |
| D-56 | 2026-10-16 | Versión automática de las guías cuando cambia su forma | specs/014-plan.md | vigente |
| D-57 | 2026-10-16 | Pesos de transición solo entre la E2 y la E3 de la 014 | specs/014-plan.md | cerrada |
| D-58 | 2026-10-16 | Dos operaciones simuladas (`SIM`) por área en `Operations` | specs/014-plan.md | vigente |
| D-59 | 2026-10-16 | La respuesta de Log NC es un JSON que arma el control | specs/014-plan.md | vigente |
| D-60 | 2026-10-16 | Un solo control (`LogNCControl`) para los tres métodos | specs/014-plan.md | vigente |
| D-61 | 2026-10-19 | La preparación antes de SAP es parte de la sesión de entrenamiento | specs/016-preparacion-antes-de-sap.md | vigente |
| D-62 | 2026-10-19 | Un solo login de la app, neutro, para los dos roles | specs/016-preparacion-antes-de-sap.md | vigente |
| D-63 | 2026-10-19 | El diseño de SAP NetWeaver pasa al login de MES simulado | specs/016-preparacion-antes-de-sap.md | vigente |
| D-64 | 2026-10-19 | Paso nuevo "Ingreso a MES", primero de la guía | specs/016-preparacion-antes-de-sap.md | enmendada (D-151: sin Hora a Hora) |
| D-65 | 2026-10-19 | La limpieza al salir va dentro del paso Logout | specs/016-preparacion-antes-de-sap.md | vigente |
| D-66 | 2026-10-19 | El Ingreso pesa 0.05 y sale de Data Collection | specs/016-preparacion-antes-de-sap.md | vigente |
| D-67 | 2026-10-19 | Del Ingreso se califican la secuencia y el Code1 propio | specs/016-preparacion-antes-de-sap.md | enmendada (D-151: sin el orden entre Hora a Hora y Recursos MES) |
| D-68 | 2026-10-19 | Un fallo vuelve a la pantalla que falló; cada pantalla se califica al enviarla | specs/016-preparacion-antes-de-sap.md | vigente |
| D-69 | 2026-10-19 | Escritorio con solo el script y el acceso al Repositorio | specs/016-preparacion-antes-de-sap.md | vigente |
| D-70 | 2026-10-19 | La estación de Recursos MES se escanea de una etiqueta simulada | specs/016-preparacion-antes-de-sap.md | vigente |
| D-71 | 2026-10-19 | Marco de Edge genérico con pestañas y barra de dirección | specs/016-preparacion-antes-de-sap.md | provisional (sin captura) |
| D-72 | 2026-10-19 | Aviso simple de Hora a Hora después de Iniciar | specs/016-preparacion-antes-de-sap.md | sin efecto (D-151: Hora a Hora sale del Ingreso) |
| D-73 | 2026-10-19 | Contraseña del login de MES: solo formato, no viaja | specs/016-preparacion-antes-de-sap.md | vigente |
| D-74 | 2026-10-19 | El Ingreso, en todas las áreas y en los dos modos | specs/016-preparacion-antes-de-sap.md | vigente |
| D-75 | 2026-10-19 | `StepType.MesLogin = 11` | specs/016-plan.md | vigente |
| D-76 | 2026-10-19 | Un solo control estático, `MesLoginControl`, para el Ingreso y la limpieza | specs/016-plan.md | vigente |
| D-77 | 2026-10-19 | Cada acción calificable o que cambia el avance es un postback | specs/016-plan.md | vigente |
| D-78 | 2026-10-19 | Avance del Ingreso y de la limpieza en `PodState` | specs/016-plan.md | vigente |
| D-79 | 2026-10-19 | El Code1 sale de la BD por el `UserId`, nunca del cliente | specs/016-plan.md | vigente |
| D-80 | 2026-10-19 | Login único: detalle de páginas, masters y redirecciones | specs/016-plan.md | vigente |
| D-81 | 2026-10-19 | No subir la 016 a `main` hasta cerrar su E4 | specs/016-plan.md | cerrada (ya se puede subir) |
| D-82 | 2026-10-20 | `ModePolicy` vive en `BLL/Scoring/` | specs/012-plan.md | vigente |
| D-83 | 2026-10-20 | Saltar un paso = avance forzado voluntario | specs/012-plan.md | vigente |
| D-84 | 2026-10-20 | Resultado del salto y repintado del POD | specs/012-plan.md | enmendada (enmienda 1: `Response.Redirect`) |
| D-85 | 2026-10-20 | Nombres de los pasos con su variante (`StepNames.Display`) | specs/012-plan.md | vigente |
| D-86 | 2026-10-20 | El resumen de Práctica libre sale del `PodContext` | specs/012-plan.md | vigente |
| D-87 | 2026-10-20 | Las pistas salen del code-behind a `StepHints` | specs/012-plan.md | vigente |
| D-88 | 2026-10-20 | Aviso "Ese no es el paso actual." sin intento en Práctica libre | specs/012-plan.md | vigente |
| D-89 | 2026-10-20 | Barra de práctica a la derecha de Abandon | specs/012-plan.md | vigente |
| D-90 | 2026-10-20 | En Práctica libre no se muestra el puntaje | specs/012-plan.md | vigente |
| D-91 | 2026-10-20 | Ver pista disponible también con un modal abierto | specs/012-plan.md (enmienda 1) | vigente |
| D-92 | 2026-10-21 | La 017 muestra solo errores reactivos, sin cambiar la calificación | specs/017-mensajes-del-mes.md | vigente |
| D-93 | 2026-10-21 | Texto provisional para un error sin captura que el operador sí provoca | specs/017-mensajes-del-mes.md | vigente |
| D-94 | 2026-10-21 | Resource Setup solo gana el Setup Type Material | specs/017-mensajes-del-mes.md | vigente |
| D-95 | 2026-10-21 | El mecanismo visual es parte de la 017: AUD-63, 64 y 69 aprobadas | specs/017-mensajes-del-mes.md | vigente |
| D-96 | 2026-10-21 | Fuera de la 017 los errores que el operador no puede provocar | specs/017-mensajes-del-mes.md | vigente |
| D-97 | 2026-10-21 | El barcode de Assembly no se valida contra el BOM | specs/017-mensajes-del-mes.md | vigente |
| D-98 | 2026-10-21 | El mensaje real del MES se muestra en los tres modos | specs/017-mensajes-del-mes.md | vigente |
| D-99 | 2026-10-21 | Setup Type: en las filas de herramienta valen Material o Tool Number | specs/017-mensajes-del-mes.md | vigente |
| D-100 | 2026-10-21 | El procedimiento de Resource Setup no cambia | specs/017-mensajes-del-mes.md | vigente |
| D-101 | 2026-10-21 | En Práctica libre, el botón de otro paso muestra el mensaje real si existe | specs/017-mensajes-del-mes.md | vigente |
| D-102 | 2026-10-21 | Explicación en español del mensaje del MES en Guided y en Ver pista | specs/017-mensajes-del-mes.md; history.md → "Spec 017" (en Práctica libre, un botón "¿Qué significa?" junto al mensaje en lugar de Ver pista: decisión del usuario al probar la E4) | enmendada |
| D-103 | 2026-10-21 | Los avisos de formato que el MES también tiene pasan a su texto real | specs/017-mensajes-del-mes.md; specs/017-plan.md (enmienda 4: Start de Serialized sin piezas da el 22506 y el aviso del simulador) | enmendada |
| D-104 | 2026-10-21 | Regla de los mensajes fuera de secuencia (C-1) | specs/017-mensajes-del-mes.md | vigente |
| D-105 | 2026-10-21 | El Line Clearance sigue calificándose en su paso (C-4) | specs/017-mensajes-del-mes.md | vigente |
| D-106 | 2026-10-21 | SFC inexistente vs. SFC de otra orden (C-5) | specs/017-mensajes-del-mes.md | vigente |
| D-107 | 2026-10-21 | Setup Type sin calificación nueva (C-9) | specs/017-mensajes-del-mes.md | vigente |
| D-108 | 2026-10-21 | Orden de los chequeos, el del MES (C-2) | specs/017-mensajes-del-mes.md | vigente |
| D-109 | 2026-10-21 | Data Collection: un mensaje por Save (C-6) | specs/017-mensajes-del-mes.md | vigente |
| D-110 | 2026-10-21 | Log NC: Done sin NC Code muestra `18117` y es intento fallido (C-8) | specs/017-mensajes-del-mes.md; specs/017-plan.md (enmienda 5: el `18117` queda fuera, D-120) | enmendada |
| D-111 | 2026-10-21 | Texto provisional de Start sin Resource Setup (C-10) | specs/017-mensajes-del-mes.md | provisional |
| D-112 | 2026-10-21 | El banner del POD queda hasta la siguiente acción del operador (C-11) | specs/017-mensajes-del-mes.md | vigente |
| D-113 | 2026-10-21 | `15246` lista todos los parámetros pendientes de los grupos obligatorios (C-7) | specs/017-mensajes-del-mes.md | vigente |
| D-114 | 2026-10-21 | Los textos en español de D-102 son los de la tabla de la spec (C-12) | specs/017-mensajes-del-mes.md | vigente |
| D-115 | 2026-10-21 | La auditoría de fidelidad sigue en su rama hasta que el usuario la revise | specs/017-mensajes-del-mes.md | sin efecto (2026-10-22: el usuario la hizo integrar en `TrainingApp`, `dd25b63`; la revisión sigue pendiente) |
| D-116 | 2026-10-21 | Las capturas de testing se integran tal cual | specs/017-mensajes-del-mes.md | vigente |
| D-117 | 2026-10-21 | Assessment: el mensaje del MES junto con el SweetAlert de hoy | specs/017-plan.md | vigente |
| D-118 | 2026-10-21 | M-10 y M-18 quedan fuera | specs/017-plan.md | vigente |
| D-119 | 2026-10-21 | M-7 es un mensaje por pieza seleccionada; sin selección, M-4 | specs/017-plan.md (enmienda 4) | enmendada |
| D-120 | 2026-10-21 | M-17 (`18117`) queda fuera: no se puede provocar en el simulador | specs/017-plan.md (enmienda 5) | vigente |
| D-121 | 2026-10-21 | W23: 12NC simulado por área en `PodDefaults`, sin BD | roadmap.md → "Plan de la fila 12" | vigente |
| D-122 | 2026-10-21 | W23: seriales consecutivos de 1 a 5 dígitos | roadmap.md → "Plan de la fila 12" | enmendada (2026-10-22: de 1 a 6 dígitos, como hoy) |
| D-123 | 2026-10-21 | W23: el As-Built de Log NC muestra `{12NC}/A` | roadmap.md → "Plan de la fila 12" | vigente |
| D-124 | 2026-10-21 | Quitar `RequiresNCComponent` recreando la BD con `00`–`09` | roadmap.md → "Plan de la fila 13" | vigente |
| D-125 | 2026-10-21 | W23: el 12NC simulado se deriva del `Id` del área (puede cambiar entre BDs) | roadmap.md → "Plan de la fila 12" | vigente |
| D-126 | 2026-10-21 | W23: el inicio del serial sortea primero sus dígitos (1 a 5) y después el número | roadmap.md → "Plan de la fila 12" | enmendada (2026-10-22: de 1 a 6 dígitos) |
| D-127 | 2026-10-21 | W23: el lote NonSerialized no cambia (sin captura del SFC real) | roadmap.md → "Plan de la fila 12" | vigente |
| D-128 | 2026-10-21 | W23: el BOM simulado del As-Built sigue con la Shop Order | roadmap.md → "Plan de la fila 12" | vigente |
| D-129 | 2026-10-22 | MVP publicado hoy para recoger retroalimentación; la versión final es la de la 2.ª semana de noviembre | specs/fase-9-plan.md | vigente |
| D-130 | 2026-10-22 | Los datos del MVP se descartan: la BD del servidor se recrea con `00`–`09`, sin `ALTER`, hasta la versión final | specs/fase-9-plan.md | vigente |
| D-131 | 2026-10-22 | AUD-28 (lo barato) y AUD-29 (solo parches) en una 2.ª publicación; Bootstrap, SweetAlert2 y CodeDom fuera | specs/fase-9-plan.md | cerrada (2026-10-22: E2 probada) |
| D-132 | 2026-10-22 | Redefinity (`05`) entra en el MVP con aviso de demostración en el mensaje de lanzamiento | specs/fase-9-plan.md | vigente |
| D-133 | 2026-10-22 | TI administra el servidor (cadena de conexión, BD, scripts, App Pool) con `docs/deploy.md`; el usuario publica en una carpeta | specs/fase-9-plan.md | superada (D-145) |
| D-134 | 2026-10-22 | La retroalimentación del MVP se junta fuera de la app (la registra Reuniones y Dudas) | specs/fase-9-plan.md | vigente |
| D-135 | 2026-10-22 | La E2 de la Fase 9 se hace junto con la Fase 10, antes de la versión final de noviembre | specs/fase-9-plan.md (enmienda 1) | cerrada (2026-10-22: se hizo antes de la E1 de la 018) |
| D-136 | 2026-10-22 | Dashboard: estado "Aprobado" = algún Assessment completado ≥ 0.70, en `ReadinessPolicy` (la regla puede cambiar) | specs/018-dashboard-training.md | vigente |
| D-137 | 2026-10-22 | Dashboard: la unidad es la guía publicada; el área del operador queda diferida | specs/018-dashboard-training.md | vigente |
| D-138 | 2026-10-22 | Dashboard: capacidad nueva `CanViewReports` (hoy solo Administrator); el operador no ve su historial | specs/018-dashboard-training.md | vigente |
| D-139 | 2026-10-22 | Dashboard: los SP agregan sin reglas y la BLL decide (estado, tasas, seguimiento) | specs/018-dashboard-training.md | vigente |
| D-140 | 2026-10-22 | Dashboard: Chart.js local y `.xlsx` con ClosedXML; si ClosedXML falla en .NET FW 4.7.2, se para y decide el usuario | specs/018-dashboard-training.md | vigente |
| D-141 | 2026-10-22 | Dashboard: seguimiento S-1/S-2/S-3 por guía, sin período, y sale al aprobar | specs/018-dashboard-training.md | vigente |
| D-142 | 2026-10-22 | Borrar un usuario avisa cuántas sesiones se pierden y que los indicadores cambian | specs/018-dashboard-training.md | vigente |
| D-143 | 2026-10-22 | Dashboard: la lista de operadores la filtra y pagina la BLL (el SP no pagina), para no repetir la regla de Aprobado en SQL; plan B: el SP agrega sin reglas | specs/018-plan.md (enmienda 1) | vigente |
| D-144 | 2026-10-22 | La cabecera `Server` se quita (`removeServerHeader`, solo en Release) si TI confirma IIS 10+; mientras tanto queda | specs/fase-9-plan.md (enmienda 2) | pendiente (versión de IIS de TI) |
| D-145 | 2026-10-22 | El usuario prepara la BD de producción y entrega la carpeta publicada con su `connectionStrings.config`; TI solo instala el sitio, el App Pool y el permiso de `App_Data\Logs` | specs/fase-9-plan.md (enmienda 3) | vigente |
| D-146 | 2026-10-23 | Dashboard, Pasos: el SP devuelve los intentos sin agregar y la BLL calcula todo con `StepRuns` (regla de "sin acierto" y mediana); plan B: una fila por sesión y paso, sin reglas | specs/018-plan.md (enmienda 3) | vigente |
| D-147 | 2026-10-23 | Dashboard: con la guía en "Todas", Pasos, el top 5 del Resumen y el Excel de Pasos agrupan por tipo de paso (`StepNames.Display`), con todas las sesiones juntas; reemplaza DA-4 | specs/018-dashboard-training.md (enmienda 1) | vigente |
| D-148 | 2026-10-23 | Dashboard: la descarga de sesiones del Resumen con "Todo" queda fuera de AC-24 (~16 s con un año de datos) y avisa que puede tardar; en la E7 se prueba en streaming (`OpenXmlWriter`) | specs/018-dashboard-training.md (enmienda 2) | enmendada (D-149: ZipArchive; la excepción con "Todo" sigue) |
| D-149 | 2026-10-23 | Dashboard: la descarga de sesiones se arma con `ZipArchive` + `XmlWriter`, sin `System.IO.Packaging` (IsolatedStorage falla en IIS sin perfil pasados ~10 MB), validada por un test contra el esquema de SpreadsheetML | specs/018-plan.md (enmienda 4) | vigente |
| D-150 | 2026-10-23 | Dashboard: la ficha del operador se imprime desde el navegador (estilos de impresión; PDF con "Guardar como PDF"), sin PDF en el servidor (DA-6) | specs/018-dashboard-training.md (DA-6), specs/018-plan.md (E8) | vigente |
| D-151 | 2026-10-26 | Hora a Hora sale del Ingreso a MES (script → Repositorio → Recursos MES → login); la tarjeta (3.0) se ve y no hace nada; la guía no lo menciona; la pantalla se borra; Log On exige solo Recursos MES (sin él, inválido sin intento: el login no se abre) | specs/016-preparacion-antes-de-sap.md (enmienda 1), specs/r1-r2-plan.md | vigente |
| D-152 | 2026-10-26 | La guía no menciona acciones disciplinarias: las alertas de Resource Setup y Log NC quedan en "CRÍTICO" | specs/r1-r2-plan.md | vigente |
| D-153 | 2026-10-26 | Fase 11: el dashboard es para gerencia y trainers por igual (escritorio, proyector, tablet); el Resumen muestra primero quién está listo | specs/019-ui-ux-dashboard.md | vigente |
| D-154 | 2026-10-26 | Fase 11: alcance = dashboard, Excel, login, cambio de contraseña y Usuarios (`Admin.Master`); el POD no cambia de diseño | specs/019-ui-ux-dashboard.md | vigente |
| D-155 | 2026-10-26 | Bootstrap 5.3.x en una entrega propia, verificado primero con regresión completa; si rompe algo, decide el usuario | specs/019-ui-ux-dashboard.md | cerrada (Fase 11 ✅, 2026-10-27) |
| D-156 | 2026-10-26 | Fase 11: estilo moderno neutro con tokens, fuente del sistema y Bootstrap Icons local; estados nunca solo por color | specs/019-ui-ux-dashboard.md | vigente |
| D-157 | 2026-10-26 | Modo oscuro: sigue al sistema, interruptor recordado en el navegador; la ficha se imprime en claro | specs/019-ui-ux-dashboard.md | vigente |
| D-158 | 2026-10-26 | Excel profesional (título con filtros y fecha, encabezados con color, filtros, primera fila fija, formatos, estados con color) | specs/019-ui-ux-dashboard.md | vigente |
| D-159 | 2026-10-26 | Fase 11: mockup aprobado antes del plan; mockup e implementación con la skill `frontend-design`, dentro del brief | specs/019-ui-ux-dashboard.md | cerrada (Fase 11 ✅, 2026-10-27) |
| D-160 | 2026-10-26 | Fase 11: Operadores, Pasos, Seguimiento y Usuarios como tablas con los componentes del mockup (insignias por guía, barra de % y 2 motivos, chips de reglas con texto, insignias de rol y cuenta) | specs/019-ui-ux-dashboard.md | superada (D-202) |
| D-161 | 2026-10-26 | MVP: guía simulada de Wires / Subensamble 1, Serialized con la forma de LoFlo (Assembly, DC, sin checklist, mismos pesos), datos SIM, scripts `11` y `12`; para que los trainers prueben el flujo Serialized | `0f01f11` (plan directo; roadmap → tabla de fases) | vigente |
| D-162 | 2026-10-26 | Fase 11: los campos (form-control, form-select, input-group) tienen un borde propio para pasar 3:1 (`#808B9B` en claro, `#627083` en oscuro); el borde del mockup queda para tablas, paneles y divisores | specs/019-ui-ux-dashboard.md (enmienda) | vigente |
| D-163 | 2026-10-26 | Fase 11: la página de inicio del operador (`StartSession`, `Site.Master`) entra con el sistema de diseño y el tema; los modos como tarjetas; mismo flujo; sin mockup; va en la E2; el POD sigue fuera | specs/019-ui-ux-dashboard.md (enmienda) | enmendada (D-202: con mockup propio) |
| D-164 | 2026-10-26 | Fase 11, Resumen: se quitan del mockup "N sin iniciar" y el número de Seguimiento (serían datos nuevos); queda el enlace "Ver seguimiento" | specs/019-ui-ux-dashboard.md (ajuste del mockup) | vigente |
| D-165 | 2026-10-27 | Spec 021: una sola área por operador (`Users.AreaId` nullable) | specs/021-area-operador.md | vigente |
| D-166 | 2026-10-27 | Spec 021: el operador elige su área al autorregistrarse; el admin la corrige en Usuarios | specs/021-area-operador.md | vigente |
| D-167 | 2026-10-27 | Spec 021: el inicio del operador muestra su área fija como texto y solo el selector de guía | specs/021-area-operador.md | vigente |
| D-168 | 2026-10-27 | Spec 021: operador sin área: aviso y no entrena | specs/021-area-operador.md | vigente |
| D-169 | 2026-10-27 | Spec 021: pantalla "Elige tu área" tras el cambio de contraseña; Training redirige ahí mientras no tenga área | specs/021-area-operador.md | vigente |
| D-170 | 2026-10-27 | Spec 021: todo operador sin área (autorregistro, alta por admin y existentes) la elige en su próximo ingreso | specs/021-area-operador.md | vigente |
| D-171 | 2026-10-27 | Spec 021: una vez elegida, solo un admin la cambia | specs/021-area-operador.md | vigente |
| D-172 | 2026-10-27 | Spec 021: Usuarios con columna Área y acciones Cambiar área y Quitar área por fila | specs/021-area-operador.md | enmendada (D-208) |
| D-173 | 2026-10-27 | Spec 021: la BLL valida el área en `StartSession` (excepción, no intento) | specs/021-area-operador.md | vigente |
| D-174 | 2026-10-27 | Spec 021: una sesión en curso de otra área se puede terminar | specs/021-area-operador.md | vigente |
| D-175 | 2026-10-27 | Spec 021: el dashboard solo muestra el área del operador (Operadores y Operador); filtros y cálculos sin cambios | specs/021-area-operador.md | enmendada (spec 024: Operadores y Operador por área, D-260 a D-271) |
| D-176 | 2026-10-27 | Spec 021: solo son elegibles las áreas con al menos una guía publicada | specs/021-area-operador.md | vigente |
| D-177 | 2026-10-27 | Spec 021: el operador confirma antes de guardar su área | specs/021-area-operador.md | vigente |
| D-178 | 2026-10-27 | Spec 021: Redefinity es elegible como las demás; se quita el aviso "NO USAR EN PRODUCCIÓN" de `05` y `domain.md` | specs/021-area-operador.md | vigente |
| D-179 | 2026-10-27 | Spec 021: propuestas de Claude P-1 a P-12 confirmadas (esquema, solo Operators, redirección por Session con la BLL como fuente, guardado propio solo sin área, elegibles por SP, errores de `StartSession` como excepción, tests en reglas puras, textos, pantallas y docs al cerrar) | specs/021-area-operador.md | vigente |
| D-180 | 2026-10-26 | R3: existen los dos Resource Setup (Standard y Custom); el administrador elige por guía; Standard es la predeterminada | specs/020-custom-resource-setup.md | vigente |
| D-181 | 2026-10-26 | R3: Custom RS es un modal del POD, mismo botón Resource Setup, con el diseño de las capturas | specs/020-custom-resource-setup.md | vigente |
| D-182 | 2026-10-26 | R3: Tool Numbers de captura libre, al menos uno, sin catálogo; el ícono ⧉ es decorativo | specs/020-custom-resource-setup.md | enmendada (D-200/D-201) |
| D-183 | 2026-10-26 | R3: se califica secuencia + Setup State = Set Up; el Yes/No de la confirmación no se califica | specs/020-custom-resource-setup.md | vigente |
| D-184 | 2026-10-26 | R3: Resource y Operation de los datos simulados; Tool Groups simulados, 2 a 4 de un pool de 8, mismos en la sesión; un proveedor único prevé las Operations | specs/020-custom-resource-setup.md | enmendada (D-200/D-201) |
| D-185 | 2026-10-26 | R3: secuencia Retrieve, Set Up, Shop Order, Associate (OK), Tool Numbers, Save, Yes, OK | specs/020-custom-resource-setup.md | vigente |
| D-186 | 2026-10-26 | R3: Custom RS parte vacío, sin equipos precargados | specs/020-custom-resource-setup.md | vigente |
| D-187 | 2026-10-26 | R3: el tipo se cambia en una página Guías de Admin (`CanManageGuides`) | specs/020-custom-resource-setup.md | vigente |
| D-188 | 2026-10-26 | R3: la sesión copia el tipo de la guía al empezar; un cambio vale para sesiones nuevas | specs/020-custom-resource-setup.md | vigente |
| D-189 | 2026-10-26 | R3: Associate y la confirmación en el cliente; un postback, el del Save; la orden se crea al guardar | specs/020-custom-resource-setup.md | vigente |
| D-190 | 2026-10-26 | R3: Code1 arriba a la derecha; Setup State "Open" hasta Retrieve, luego Set Up por defecto; Success cierra el modal, sin "Record changed" | specs/020-custom-resource-setup.md | vigente |
| D-191 | 2026-10-26 | R3: Associate sin orden numérica y Save sin Tool Number = formato sin intento; Save sin asociar = intento fallido | specs/020-custom-resource-setup.md | enmendada (D-200/D-201) |
| D-192 | 2026-10-26 | R3: Clear vuelve al estado inicial; reasociar conserva los Tool Numbers; reiniciar la guía sortea otros Tool Groups | specs/020-custom-resource-setup.md | vigente |
| D-193 | 2026-10-26 | R3: Save con "Open" falla y crea igual la orden, como la estándar | specs/020-custom-resource-setup.md | vigente |
| D-194 | 2026-10-26 | R3: ninguna guía arranca en Custom; el piloto se marca desde Admin o un script de Ops | specs/020-custom-resource-setup.md | vigente |
| D-195 | 2026-10-26 | R3, plan: página Guías con selector y Guardar por fila, con confirmación | specs/020-plan.md | vigente |
| D-196 | 2026-10-26 | R3, plan: un Save del tipo de Resource Setup que no es el de la sesión es excepción, sin intento | specs/020-plan.md | vigente |
| D-197 | 2026-10-26 | R3, plan: status.md e history.md los escribe Desarrollo con el resumen de cada entrega | specs/020-plan.md | vigente |
| D-198 | 2026-10-26 | R3, E1: la versión nueva de una guía (spTrainingGuideSync, D-56) hereda el ResourceSetupType de la que reemplaza | specs/020-plan.md | vigente |
| D-199 | 2026-10-26 | R3, E1: la página Guías lista solo las guías publicadas | specs/020-plan.md | vigente |
| D-200 | 2026-10-26 | R3, enmienda 1: siempre 2 Tool Groups por sesión (antes 2 a 4), del mismo pool de 8 | specs/020-custom-resource-setup.md (enmienda 1) | vigente |
| D-201 | 2026-10-26 | R3, enmienda 1: con la orden asociada, cada fila de Tool Group necesita su Tool Number en todos los modos (formato sin intento) | specs/020-custom-resource-setup.md (enmienda 1) | vigente |
| D-202 | 2026-10-27 | Spec 022: cada pantalla con su propio mockup en Claude Design (3 alternativas por estructura); la exportación aprobada en `specs/022-design/` es su criterio. Reemplaza D-160 y el "sin mockup" de D-163 | specs/022-mockups-por-pantalla.md | vigente |
| D-203 | 2026-10-27 | Spec 022: pantallas Login, Cambio de contraseña, Operadores, Operador, Pasos, Seguimiento, Guías, Usuarios e Inicio del operador; fuera el Resumen (v1) y "Elige tu área" | specs/022-mockups-por-pantalla.md | vigente |
| D-204 | 2026-10-27 | Spec 022: intención de UX (jerarquía en vez de tablas planas, guiar la decisión, identidad del login); la forma la proponen las alternativas | specs/022-mockups-por-pantalla.md | vigente |
| D-205 | 2026-10-27 | Spec 022: métricas nuevas solo derivadas en la BLL con los datos que la vista ya carga, cada una con su D-n; las que piden datos nuevos van a otra spec | specs/022-mockups-por-pantalla.md | vigente |
| D-206 | 2026-10-27 | Spec 022: tokens y componentes del v1 como base, cambios marcados y cada uno una D-n; aprobación pantalla por pantalla, la primera fija el sistema | specs/022-mockups-por-pantalla.md | enmendada (D-237: el v1 es referencia, no molde) |
| D-207 | 2026-10-27 | Spec 022: propuestas de Claude respondidas (orden de diseño Login → Contraseña → libres → resto; se diseña el estado de la 021; HTML autocontenido; 3 alternativas por estructura con nota; quitar datos es una D-n; implementación en paralelo con la 021 para las libres; sistema en entrega propia; candidatas para otra spec). P-6 descartada | specs/022-mockups-por-pantalla.md | vigente |
| D-208 | 2026-10-27 | Spec 021, enmienda 1: Usuarios sin Quitar área (en la página ni en la BLL: `SetArea` rechaza `null`); Cambiar área en la columna Área, con un lápiz | specs/021-area-operador.md (enmienda 1) | vigente |
| D-209 | 2026-10-27 | Spec 023: filtro de período solo en Pasos y Resumen, para revisar un grupo o una capacitación, reportes periódicos, antes y después de un cambio y seguimiento diario | specs/023-filtro-periodo.md | vigente |
| D-210 | 2026-10-27 | Spec 023: un solo control de período, atajos + Personalizado… (Inicio y Fin al elegirlo), con el rango aplicado en palabras debajo | specs/023-filtro-periodo.md | enmendada (2026-10-28: lista reducida a 7 opciones, P-3) |
| D-211 | 2026-10-27 | Spec 023: comparación con el período anterior de igual largo; con un período en curso, contra los mismos días del anterior | specs/023-filtro-periodo.md | vigente |
| D-212 | 2026-10-27 | Spec 023: muestran el cambio la tasa de aprobación, las sesiones y el Top 5 del Resumen, y el % con fallo por paso de Pasos | specs/023-filtro-periodo.md | vigente |
| D-213 | 2026-10-27 | Spec 023: el Excel de Pasos lleva las columnas de cambio | specs/023-filtro-periodo.md | vigente |
| D-214 | 2026-10-27 | Spec 023: la gráfica de actividad del Resumen con granularidad automática (día, semana o mes) | specs/023-filtro-periodo.md | vigente |
| D-215 | 2026-10-27 | Spec 023: al entrar, Este mes (enmienda el "últimos 30 días" de la spec 018, punto 8) | specs/023-filtro-periodo.md | vigente |
| D-216 | 2026-10-27 | Spec 023: fechas con el selector nativo del navegador, sin librería | specs/023-filtro-periodo.md | vigente |
| D-217 | 2026-10-27 | Spec 023: va antes del mockup de Pasos de la 022, con el sistema de diseño actual | specs/023-filtro-periodo.md | vigente |
| D-218 | 2026-10-28 | Spec 023: propuestas P-1 a P-16 confirmadas (inicio de la sesión; días de la planta, también en Últimos 30 días; 7 atajos; anterior por atajo; flecha, valor, referencia y base; Este mes ante un rango inválido; URL; `@To` y una sola lectura; granularidad 31/366 días; Excel; Sesiones del período; un UserControl; rendimiento; textos; 4 entregas) | specs/023-filtro-periodo.md | vigente |
| D-219 | 2026-10-28 | Orden de Desarrollo entre la 022 y la 023: 022 E1 (tokens) → 023 E1 a E4 → 022 E2a (Login) y el resto de la 022 con sus mockups; la E2 de la 022 se parte en E2a (Login) y E2b (Cambio de contraseña, con su mockup) | roadmap.md (Spec 023 → Orden) | vigente |
| D-220 | 2026-10-28 | Spec 022, Login: alternativa 3b, panel dividido (identidad + formulario; franja en tablet) | specs/022-mockups-por-pantalla.md (Pantallas aprobadas) | vigente |
| D-221 | 2026-10-28 | Spec 022, Login: tokens nuevos `--mt-brand-bg`, `--mt-on-brand`, `--mt-on-brand-2`, `--mt-brand-line` (claro y oscuro); ninguno existente cambia | specs/022-mockups-por-pantalla.md | vigente |
| D-222 | 2026-10-28 | Spec 022, Login: componentes `.auth-*` (solo login) y `.mt-status-line` (reutilizable) | specs/022-mockups-por-pantalla.md | vigente |
| D-223 | 2026-10-28 | Spec 022, Login: `Admin.Master` con modo `FullBleed` (sin barra ni `.admin-main`), solo el login; el interruptor de tema va en la página | specs/022-mockups-por-pantalla.md | vigente |
| D-224 | 2026-10-28 | Spec 022, Login: errores en la línea de estado, no en SweetAlert; íconos en campos y en Entrar (`BtnLogin` → `LinkButton`) | specs/022-mockups-por-pantalla.md | vigente |
| D-225 | 2026-10-28 | Spec 022, Login: textos nuevos (lead, panel de identidad y aviso de primer acceso) | specs/022-mockups-por-pantalla.md | vigente |
| D-226 | 2026-10-28 | Spec 022: flujo con handoff — Claude Design manda el diseño a una sesión de la nube que implementa en `cloud/022-<pantalla>` ("no verificada"); la nube de planeación registra las D-n; Desarrollo verifica, el usuario prueba y se hace el commit en `TrainingApp` | specs/022-mockups-por-pantalla.md (enmienda 2026-10-28) | vigente |
| D-227 | 2026-10-28 | Spec 022: excepción a P-3 — en `022-design/` quedan capturas PNG y la lista de cambios (`<pantalla>.md`), no el HTML exportado (carga React) | specs/022-mockups-por-pantalla.md (enmienda 2026-10-28) | enmendada (D-240: las capturas las hace Desarrollo) |
| D-228 | 2026-10-28 | Spec 022, Cambio de contraseña: alternativa 2a (franja de marca, tarjeta montada, 3 pasos en primer acceso, Nueva y Confirmar lado a lado, requisitos fijos) | specs/022-mockups-por-pantalla.md (Pantallas aprobadas) | vigente |
| D-229 | 2026-10-28 | Spec 022, Cambio de contraseña: componentes `.pw-*` (solo esta pantalla) y `.mt-steps` / `.mt-step` (reutilizable); sin tokens nuevos | specs/022-mockups-por-pantalla.md | vigente |
| D-230 | 2026-10-28 | Spec 022, Cambio de contraseña: pantalla completa (`FullBleed`) siempre, también al llegar por URL sin estar obligado | specs/022-mockups-por-pantalla.md | vigente |
| D-231 | 2026-10-28 | Spec 022, Cambio de contraseña: errores en `.mt-status-line`, no SweetAlert; `BtnSave` y `BtnCancel` → `LinkButton`; Cancelar como botón de texto | specs/022-mockups-por-pantalla.md | vigente |
| D-232 | 2026-10-28 | Spec 022, Cambio de contraseña: textos nuevos (pasos, requisitos, "Guardar y continuar", "Cancelar y cerrar sesión") | specs/022-mockups-por-pantalla.md | vigente |
| D-233 | 2026-10-29 | Spec 022, Guías: alternativa 2b (chips de conteo por tipo, Custom primero, nota al pie) | specs/022-mockups-por-pantalla.md (Pantallas aprobadas) | vigente |
| D-234 | 2026-10-29 | Spec 022, Guías: componentes `.guides-head`, `.guides-counts`, `.guides-note`; sin tokens nuevos | specs/022-mockups-por-pantalla.md | vigente |
| D-235 | 2026-10-29 | Spec 022, Guías: conteo por tipo y orden Custom primero en la BLL, sin test (excepción a D-205) | specs/022-mockups-por-pantalla.md | vigente |
| D-236 | 2026-10-29 | Spec 022, Guías: el aviso de guardado nombra la guía | specs/022-mockups-por-pantalla.md | vigente |
| D-237 | 2026-10-29 | Spec 022: el v1 es referencia, no molde: al menos una alternativa por pantalla rompe con el v1 en estructura y en lo visual; cada cambio marca qué conlleva (tokens, pantallas ya hechas, componentes, esfuerzo). Precisa D-206 | specs/022-mockups-por-pantalla.md (enmienda 2026-10-29) | vigente |
| D-238 | 2026-10-29 | Spec 022: la barra superior (navbar horizontal, Admin.Master y Site.Master) entra como pantalla 0 con mockup propio y entrega E1b, antes de Seguimiento; marca siempre "TrainingApp"; la del operador la propone el diseño con datos existentes | specs/022-mockups-por-pantalla.md (enmienda 2026-10-29) | vigente |
| D-239 | 2026-10-29 | Spec 022, Pasos: alternativa 4a "mesa de enseñanza" (franja con filtros, una columna por paso, panel del paso elegido sin postback, ranking completo abajo) | specs/022-mockups-por-pantalla.md (Pantallas aprobadas) | enmendada (D-241: el ranking completo, plegado; D-289: la franja como tarjeta) |
| D-240 | 2026-10-28 | Spec 022: las capturas de `022-design/` (claro, oscuro y tablet) las hace Desarrollo, solo de la versión final aprobada de cada pantalla, no de cada iteración; si su contexto no alcanza, le pregunta al usuario antes de omitirlas o abaratarlas | roadmap.md (Fase 12) | enmendada (D-285: sin captura de tablet) |
| D-241 | 2026-10-29 | Spec 022, Pasos: el ranking completo va plegado (`<details>`, "Mostrar / Ocultar"), porque repite las columnas y el panel del elegido; la lista no se quita. Enmienda D-239 | specs/022-mockups-por-pantalla.md (enmienda 2026-10-29) | vigente |
| D-260 | 2026-10-28 | Spec 024: Operadores es la vista del trainer de un área (quién de su área falta, reprobó o está en entrenamiento) | specs/024-dashboard-por-area.md | vigente |
| D-261 | 2026-10-28 | Spec 024: Operadores sin columnas por guía; una columna "Estado en su área" | specs/024-dashboard-por-area.md | vigente |
| D-262 | 2026-10-28 | Spec 024: el filtro Área de Operadores arranca en Todas, con las áreas agrupadas por producto; sin producto completo | specs/024-dashboard-por-area.md | vigente |
| D-263 | 2026-10-28 | Spec 024: Sin iniciar = ninguna sesión en las guías publicadas de su área (cambia domain.md) | specs/024-dashboard-por-area.md | vigente |
| D-264 | 2026-10-28 | Spec 024: se quita el filtro Guía de Operadores | specs/024-dashboard-por-area.md | vigente |
| D-265 | 2026-10-28 | Spec 024: la lista muestra a cada operador solo en su área | specs/024-dashboard-por-area.md | vigente |
| D-266 | 2026-10-28 | Spec 024: la lista no muestra nada de las guías de otras áreas | specs/024-dashboard-por-area.md | vigente |
| D-267 | 2026-10-28 | Spec 024: operador sin área = "Sin área", sin insignia, fuera de Sin iniciar; opción "Sin área" en el filtro | specs/024-dashboard-por-area.md | vigente |
| D-268 | 2026-10-28 | Spec 024: hoy un operador tiene una guía que entrenar, la de su área (copia por área si varias comparten contenido); guías distintas por área u operación, futuro | specs/024-dashboard-por-area.md | vigente |
| D-269 | 2026-10-28 | Spec 024: Operador con "Su área" y "Otras áreas" colapsada (solo guías con sesiones); Reforzar y curvas, su área primero; Sesiones con el área | specs/024-dashboard-por-area.md | vigente |
| D-270 | 2026-10-28 | Spec 024: la ficha lleva su área completa y las otras áreas en una línea | specs/024-dashboard-por-area.md | vigente |
| D-271 | 2026-10-28 | Spec 024: propuestas P-1 a P-12 confirmadas (área actual sin historial; Resumen, Pasos y Seguimiento fuera; filtro Área en la BLL; URL `?area=`; "Sin guía publicada"; una insignia por guía si hay varias; Excel como la pantalla; sin área con "Otras áreas" abierta; `<details>` y línea en la ficha; regla pura en la BLL; tests solo de lo nuevo; briefs 6 y 7 los enmienda Diseño UI/UX) | specs/024-dashboard-por-area.md | vigente |
| D-272 | 2026-10-29 | Spec 024 (enmienda 1): en Sesiones del Operador, la columna "Guía" pasa a "Área" (el título de la guía al pasar el mouse), en vez de agregar otra | specs/024-dashboard-por-area.md | vigente |
| D-273 | 2026-10-29 | Spec 024 (enmienda 1): la ficha impresa lleva Pasos a reforzar y Sesiones completos (con otras áreas); solo las tarjetas de otras áreas van en la línea "También entrenó" | specs/024-dashboard-por-area.md | vigente |
| D-274 | 2026-10-29 | Spec 024 (enmienda 1): el hook de inicio de la nube y `tools/` (`b95b27c`) no entran a `TrainingApp`; la rama local `spec-024` parte de `73a2829` | specs/024-dashboard-por-area.md | vigente |
| D-280 | 2026-10-29 | Spec 022, Pasos: sin paleta propia ni índigo; tokens originales; la franja con `--mt-brand-*`; sin tokens nuevos | specs/022-mockups-por-pantalla.md | vigente |
| D-281 | 2026-10-29 | Spec 022, Pasos: `Admin.Master` con `WideMain` (y `PageCss` / `body runat` solo si siguen haciendo falta); `PeriodFilter.ShowsRange`; componentes `.steps-*` | specs/022-mockups-por-pantalla.md | vigente |
| D-282 | 2026-10-29 | Spec 022, Pasos: Modo como control segmentado (`RblMode`), mismos valores y URL | specs/022-mockups-por-pantalla.md | vigente |
| D-283 | 2026-10-29 | Spec 022, Pasos: textos nuevos; la parte de cada motivo en la BLL, sin test (como D-235) | specs/022-mockups-por-pantalla.md | vigente |
| D-284 | 2026-10-29 | Spec 022: las sesiones de diseño de la nube hacen una vuelta de crítica con `design:design-critique` antes de que el usuario elija (una corrección, resumen en el artifact); sin revisión de accesibilidad ni textos con skills aparte | specs/022-mockups-por-pantalla.md (enmienda 2026-10-29) | vigente |
| D-285 | 2026-10-29 | Spec 022: cada alternativa del mockup es una página responsive con el interruptor de tema, sin copias en claro, oscuro ni tablet; las capturas finales de Desarrollo quedan en claro y oscuro (sin tablet). Enmienda D-240 | specs/022-mockups-por-pantalla.md (enmienda 2026-10-29) | vigente |
| D-286 | 2026-10-29 | Spec 022, Barra superior: propuesta 1, banda de marca azul con el menú del admin en dos grupos ("Dashboard" y "Administración"), el área del operador y la cuenta con iniciales | specs/022-mockups-por-pantalla.md (Pantallas aprobadas) | vigente |
| D-287 | 2026-10-29 | Spec 022, Barra superior: tokens nuevos `--mt-brand-raise` y `--mt-brand-edge` (toda la app; ninguno existente cambia) | specs/022-mockups-por-pantalla.md | vigente |
| D-288 | 2026-10-29 | Spec 022, Barra superior: componente `.mt-topbar` en lugar de `.admin-bar`, controles de presentación en los masters, `TopBar.cs`, menú plegado en `theme.js` y su responsive | specs/022-mockups-por-pantalla.md | vigente |
| D-289 | 2026-10-29 | Spec 022: la franja de Pasos pasa a tarjeta, para no tener dos bandas azules (precisa D-239) | specs/022-mockups-por-pantalla.md | vigente |
| D-290 | 2026-10-29 | Spec 022, Barra superior: textos ("TrainingApp" en `Site.Master`, el nombre del admin en vez de su Code1, "Tu área", "Dashboard", "Administración", "Menú") | specs/022-mockups-por-pantalla.md | vigente |
| D-291 | 2026-10-30 | Spec 022, Seguimiento: alternativa B "cola de llamadas" con el encabezado P1 (pestañas-índice, Llamar primero en tarjetas, Después en filas, columna "En la cola") | specs/022-mockups-por-pantalla.md (Pantallas aprobadas) | vigente |
| D-292 | 2026-10-30 | Spec 022, Seguimiento: componentes `.followup-*`; se borran las tablas y chips del v1; mismo texto en el Excel; sin tokens nuevos | specs/022-mockups-por-pantalla.md | vigente |
| D-293 | 2026-10-30 | Spec 022, Seguimiento: `FollowUpSummary` en la BLL, sin test (2 reglas o más = "Llamar primero", agrupación de presentación) | specs/022-mockups-por-pantalla.md | vigente |
| D-294 | 2026-10-30 | Spec 022, Seguimiento: textos nuevos (bajada, pestañas, conteos, columna "En la cola", vacío) | specs/022-mockups-por-pantalla.md | vigente |

## Bugs que ya existían (B-n)

Hasta el 2026-10-21 se numeraban por contexto, por eso hay dos B-1. Desde B-3 la numeración
es global.

| ID | Fecha | Bug | Dónde | Estado |
|---|---|---|---|---|
| B-1 (009) | 2026-10-05 | Avance forzado por secuencia no cerraba la Component List o la DC List abierta | specs/009-plan.md (E4/E5) | corregido |
| B-2 (009) | 2026-10-05 | Log NC se puede acertar sin scrapear, solo manipulando el cliente | specs/009-plan.md (E4) | riesgo aceptado (D-16) |
| B-1 (rendimiento) | 2026-10-07 | Una lista vacía de la API reemplazaba la última lista de empleados | history.md → "Arreglo de rendimiento del arranque" | corregido |
| B-1 (011) | 2026-10-13 | Browse después del Start reescribía `TxtSFC` y la selección | specs/011-plan.md (E2) | corregido |
| B-3 | 2026-10-22 | `Users.aspx` manda al login, y no al inicio del rol, cuando la BLL niega el acceso (spec 013) | specs/018-plan.md (enmienda 2, E2) | corregido (018, E2, `33a6a9d`); la 021 (12b, E4) lo verificó con Cambiar área (Quitar área salió por D-208) |
| B-4 | 2026-10-27 | `PodService.StartSession` acepta un modo de sesión no definido: abandona la sesión en curso y guarda una que falla al cargar (POST manipulado) | specs/021-plan.md (E2, enmienda 1) | corregido (021, E2) |
| B-20 | 2026-10-26 | `PodService.StartSession` no revisa que la guía exista y esté publicada: solo protege la página (lista y EventValidation); un envío manipulado podría iniciar una versión despublicada (D-56) con sus pasos y pesos | specs/020-plan.md (E2, `/code-review`) | corregido (021, E2: `TrainingAccessRule.EnsureCanStart`) |
