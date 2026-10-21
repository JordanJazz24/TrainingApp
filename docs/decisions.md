# Índice de decisiones (D-n) y bugs previos (B-n) — MESTrainingSimulator

> **Solo un índice:** una línea por decisión, para encontrarla y saber si sigue vigente. El
> texto, el porqué y las alternativas están en la fuente ("Dónde"); si algo de aquí no
> coincide con la fuente, manda la fuente. Creado el 2026-10-21 (antes, la última D-n se
> buscaba con `grep`).
>
> - **Siguiente D-n: D-120. Siguiente B-n: B-3.** La numeración es continua entre
>   auditorías, specs, planes y planes directos (`docs/process.md` → "Clasificar el pedido").
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
| D-7 | 2026-10-01 | Endurecer el deploy y actualizar paquetes, en la Fase 9 | audits/2026-10-01-auditoria.md (AUD-28, 29) | vigente |
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
| D-19 | 2026-10-12 | T1 se aprueba como preparación, sin probarlo en el servidor real | history.md → "T1" | pendiente (prueba en el servidor real) |
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
| D-43 | 2026-10-15 | Alerta en rojo del subpaso 1 de Log NC (texto aprobado) | specs/014-tres-scraps.md | vigente |
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
| D-64 | 2026-10-19 | Paso nuevo "Ingreso a MES", primero de la guía | specs/016-preparacion-antes-de-sap.md | vigente |
| D-65 | 2026-10-19 | La limpieza al salir va dentro del paso Logout | specs/016-preparacion-antes-de-sap.md | vigente |
| D-66 | 2026-10-19 | El Ingreso pesa 0.05 y sale de Data Collection | specs/016-preparacion-antes-de-sap.md | vigente |
| D-67 | 2026-10-19 | Del Ingreso se califican la secuencia y el Code1 propio | specs/016-preparacion-antes-de-sap.md | vigente |
| D-68 | 2026-10-19 | Un fallo vuelve a la pantalla que falló; cada pantalla se califica al enviarla | specs/016-preparacion-antes-de-sap.md | vigente |
| D-69 | 2026-10-19 | Escritorio con solo el script y el acceso al Repositorio | specs/016-preparacion-antes-de-sap.md | vigente |
| D-70 | 2026-10-19 | La estación de Recursos MES se escanea de una etiqueta simulada | specs/016-preparacion-antes-de-sap.md | vigente |
| D-71 | 2026-10-19 | Marco de Edge genérico con pestañas y barra de dirección | specs/016-preparacion-antes-de-sap.md | provisional (sin captura) |
| D-72 | 2026-10-19 | Aviso simple de Hora a Hora después de Iniciar | specs/016-preparacion-antes-de-sap.md | provisional (sin captura) |
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
| D-102 | 2026-10-21 | Explicación en español del mensaje del MES en Guided y en Ver pista | specs/017-mensajes-del-mes.md | vigente |
| D-103 | 2026-10-21 | Los avisos de formato que el MES también tiene pasan a su texto real | specs/017-mensajes-del-mes.md | vigente |
| D-104 | 2026-10-21 | Regla de los mensajes fuera de secuencia (C-1) | specs/017-mensajes-del-mes.md | vigente |
| D-105 | 2026-10-21 | El Line Clearance sigue calificándose en su paso (C-4) | specs/017-mensajes-del-mes.md | vigente |
| D-106 | 2026-10-21 | SFC inexistente vs. SFC de otra orden (C-5) | specs/017-mensajes-del-mes.md | vigente |
| D-107 | 2026-10-21 | Setup Type sin calificación nueva (C-9) | specs/017-mensajes-del-mes.md | vigente |
| D-108 | 2026-10-21 | Orden de los chequeos, el del MES (C-2) | specs/017-mensajes-del-mes.md | vigente |
| D-109 | 2026-10-21 | Data Collection: un mensaje por Save (C-6) | specs/017-mensajes-del-mes.md | vigente |
| D-110 | 2026-10-21 | Log NC: Done sin NC Code muestra `18117` y es intento fallido (C-8) | specs/017-mensajes-del-mes.md | vigente |
| D-111 | 2026-10-21 | Texto provisional de Start sin Resource Setup (C-10) | specs/017-mensajes-del-mes.md | provisional |
| D-112 | 2026-10-21 | El banner del POD queda hasta la siguiente acción del operador (C-11) | specs/017-mensajes-del-mes.md | vigente |
| D-113 | 2026-10-21 | `15246` lista todos los parámetros pendientes de los grupos obligatorios (C-7) | specs/017-mensajes-del-mes.md | vigente |
| D-114 | 2026-10-21 | Los textos en español de D-102 son los de la tabla de la spec (C-12) | specs/017-mensajes-del-mes.md | vigente |
| D-115 | 2026-10-21 | La auditoría de fidelidad sigue en su rama hasta que el usuario la revise | specs/017-mensajes-del-mes.md | vigente |
| D-116 | 2026-10-21 | Las capturas de testing se integran tal cual | specs/017-mensajes-del-mes.md | vigente |
| D-117 | 2026-10-21 | Assessment: el mensaje del MES junto con el SweetAlert de hoy | specs/017-plan.md | vigente |
| D-118 | 2026-10-21 | M-10 y M-18 quedan fuera | specs/017-plan.md | vigente |
| D-119 | 2026-10-21 | M-7 es un mensaje por pieza | specs/017-plan.md | vigente |

## Bugs que ya existían (B-n)

Hasta el 2026-10-21 se numeraban por contexto, por eso hay dos B-1. Desde B-3 la numeración
es global.

| ID | Fecha | Bug | Dónde | Estado |
|---|---|---|---|---|
| B-1 (009) | 2026-10-05 | Avance forzado por secuencia no cerraba la Component List o la DC List abierta | specs/009-plan.md (E4/E5) | corregido |
| B-2 (009) | 2026-10-05 | Log NC se puede acertar sin scrapear, solo manipulando el cliente | specs/009-plan.md (E4) | riesgo aceptado (D-16) |
| B-1 (rendimiento) | 2026-10-07 | Una lista vacía de la API reemplazaba la última lista de empleados | history.md → "Arreglo de rendimiento del arranque" | corregido |
| B-1 (011) | 2026-10-13 | Browse después del Start reescribía `TxtSFC` y la selección | specs/011-plan.md (E2) | corregido |
