# Spec 016: Preparación antes de SAP (Ingreso a MES y limpieza al salir)

**Status:** Approved (2026-10-19) · **Cerrada:** 2026-10-19 (E1-E4, `50aca9d`…`e1227dd`; cierre `96addd8`)
**Date:** 2026-10-19 · **Approved:** 2026-10-19 (usuario, con P-1 a P-14)

> **Enmienda 1 (2026-10-26, usuario; D-151; roadmap → "Revisión con Ernesto, MES
> (2026-10-22)", R1): Hora a Hora sale del Ingreso a MES.** Ernesto aclaró que Hora a Hora 3.0
> es el conteo de producción, no parte del proceso de MES. Lo enseñan los trainers aparte. El
> Ingreso queda así: **script → Repositorio → Recursos MES → login de MES**.
> - **Repositorio:** la tarjeta Hora-Hora (3.0) **se ve y no hace nada**, igual que Hora-Hora
>   (1.0). No abre pestaña, no cuenta como intento y no muestra aviso, en ningún modo (P-2 y
>   AC-4 cambian).
> - **Se borran del código** la pantalla de Hora a Hora 3.0, su pestaña en Edge, su aviso
>   provisional (D-72 queda sin efecto), su registro en el avance del Ingreso (P-7) y su pista
>   de Práctica libre. Si algún día vuelve, se recupera del historial de git.
> - **Log On** exige solo el registro de **Recursos MES**. Como el login de MES solo se abre
>   después de Buscar en Recursos MES, ya no hay un Log On fallido por falta de registro. Si llega
>   uno (página vieja o envío manipulado), es **inválido, sin intento**, como cualquier envío
>   imposible desde la pantalla (`CLAUDE.md` → "Capas"; corrección del usuario, 2026-10-26, con
>   el aviso de Desarrollo). P-3 y P-4 pierden "Log On sin los registros". Queda sin efecto la
>   parte de D-67 sobre el orden entre Hora a Hora y Recursos MES, y el caso límite "Recursos MES
>   antes que Hora a Hora".
> - **Guía de Guided:** no menciona Hora a Hora. Del Repositorio va directo a Recursos MES (P-8,
>   AC-16).
> - **Sin cambios** en la BD, los pesos (el Ingreso sigue en 0.05), los pasos de las guías ni la
>   limpieza de Logout. No hacen falta versiones nuevas de las guías.
> - **Datos ya guardados:** los intentos con la acción `MES.HoraHora.Iniciar` se conservan y se
>   siguen viendo en el dashboard con ese código.
> - **AC-5 queda sin efecto.** AC-4 queda: "Repositorio con las tres tarjetas; Hora-Hora (1.0) y
>   (3.0) no hacen nada; Recursos MES se abre en su pestaña". AC-7 queda: "Log On con su Code1 y una
>   contraseña cualquiera completa el paso y aparece el POD; con un Code1 ajeno, intento fallido;
>   con la contraseña vacía, formato".

Fuentes: el flujo Pre-SAP del usuario y sus capturas (`docs/ui-reference/pre-sap-flow.md`,
`Screens SAP MES REAL/Pre-SAPMES/`, 2026-10-19); WI QD001001524 → 6.4 y 6.5
(`wi-produccion-disposables.md`); evaluación práctica QD001043644, bloques 1 y 2, y su
hallazgo EP-3 (`evaluacion-practica-mes.md`); retroalimentación de la trainer de Wires
(roadmap → Fase 8, 2026-10-13); roadmap → Fase 8.5, fila 7 (T10).

## Problem

Antes de entrar a SAP MES el operador hace una preparación que viene de una CAPA y que en
planta se audita: ejecutar el script de limpieza de historial y caché con su Code1, entrar
por el **Repositorio** a **Hora a Hora 3.0** y a **Recursos MES** (con su Code1 y la
estación) y recién entonces hacer login en MES. Al terminar, después del Logout, vuelve a
ejecutar el script. La evaluación práctica oficial lo exige (bloques 1 y 2) y el simulador
no lo enseña: hoy empieza directo en un login con el diseño de SAP NetWeaver, que en
realidad es el login de la cuenta de la app.

## Decisiones del usuario (2026-10-19)

- **D-61 · Dentro de la sesión de entrenamiento.** El flujo es parte de la sesión, para
  calificarlo en Guided y en Assessment. El login de la app deja de imitar a SAP.
- **D-62 · Un solo login de la app, neutro**, para operadores y administradores. El rol
  decide a dónde va cada uno. Hay **una sola** pantalla de cambio de contraseña, también
  neutra. **Enmienda la spec 013** (dos logins y dos pantallas de cambio de contraseña).
- **D-63 · El diseño de SAP NetWeaver pasa al login de MES simulado**, el último paso del
  Ingreso, dentro de la sesión.
- **D-64 · Un paso nuevo, "Ingreso a MES"** (primero de la guía, antes de Resource Setup):
  script → Repositorio → Hora a Hora 3.0 → Recursos MES → login de MES. Peso **0.05**.
- **D-65 · La limpieza final va dentro del paso Logout:** Logout → escritorio → script.
  Logout sigue valiendo 0.05.
- **D-66 · Pesos:** Data Collection baja 0.05 para hacerle lugar al Ingreso: LoFlo y
  Redefinity, de 0.25 a **0.20**; Moldeo, de 0.30 a **0.25**. Lo demás no cambia.
- **D-67 · Qué se califica:** la **secuencia** y el **Code1 propio**. No se califican la
  estación, la tarjeta de Hora-Hora ni el orden entre Hora a Hora y Recursos MES.
- **D-68 · Al fallar, vuelve a la pantalla que falló** y sigue desde ahí. Lo ya hecho queda
  hecho. Por eso cada pantalla se califica en el servidor al enviarla (como el Split de
  Moldeo, enmienda 3 de la spec 014): es una excepción a "un postback por paso".
- **D-69 · Escritorio:** solo el script y el acceso al Repositorio.
- **D-70 · Estación con etiqueta simulada:** en Recursos MES se escanea una etiqueta del
  recurso del área (el que muestra el POD), como la de la Shop Order (spec 011). Cualquier
  valor no vacío pasa como formato.
- **D-71 · Marco de Edge genérico** con pestañas y barra de dirección (en producción se usa
  Edge común). Hora a Hora se abre en su pestaña, que queda abierta, y el operador vuelve a
  la del Repositorio.
- **D-72 · Hora a Hora después de Iniciar:** un aviso simple marcado como **provisional**
  ("Sesión de Hora a Hora iniciada") hasta tener la captura del tablero.
- **D-73 · Contraseña del login de MES simulado: solo formato** (obligatoria, no se compara
  con nada). Se califica el Code1.
- **D-74 · En todas las áreas y en los dos modos.** Las tres guías ganan el paso (versión
  nueva por D-56).

## Propuestas de diseño (de Claude; P-1 a P-10 confirmadas y P-11 a P-14 decididas por el usuario el 2026-10-19)

- **P-1 · Pantallas de pantalla completa.** Mientras el paso actual es Ingreso a MES, o
  Logout después del clic, la página del POD muestra el escritorio simulado y no el POD. Un
  solo control de paso, declarado de forma estática en `GuidedTest.aspx` (spec 006).
- **P-2 · Qué hay en cada pantalla.**
  - **Escritorio:** fondo de Windows 10 y barra de tareas decorativa, con dos íconos que se
    abren con doble clic: el script y Repositorio.
  - **Consola:** reproduce la captura y pide el Code1 (siempre tiene 9 dígitos, usuario). Al enviarlo se califica. Si está
    bien, muestra la salida del script y "Press any key to continue . . ."; cualquier tecla
    la cierra.
  - **Edge:** pestañas y barra de dirección con la URL real del Repositorio (las de Hora a
    Hora y Recursos MES no se conocen: se muestra solo el host).
  - **Repositorio:** las tres tarjetas. **Hora-Hora (1.0) no hace nada**: no se califica
    (D-67).
  - **Hora a Hora 3.0:** reproduce la captura. El selector de idioma y el interruptor de
    Windows son decorativos.
  - **Recursos MES:** reproduce la captura. "Inicio" e "Ingresar" son decorativos. **Buscar**
    envía, y si está bien, la misma pestaña pasa al login de MES.
  - **Login de MES:** el diseño SAP NetWeaver de hoy, con usuario (Code1) y contraseña. **Log
    On** cierra el paso y muestra el POD.
- **P-3 · Formato vs. decisión** (`docs/domain.md` → "Qué cuenta como intento"):
  - **Formato, sin intento:** Code1 vacío en cualquier pantalla; en el script, un Code1 que
    no tenga exactamente 9 dígitos (como pide el script real); estación o contraseña vacías.
  - **Decisión, se envía al servidor:** cualquier otro Code1 (el propio está bien, uno ajeno
    es intento fallido), abrir el Repositorio antes de ejecutar el script y presionar Log On
    sin los dos registros (Hora a Hora y Recursos MES).
- **P-4 · Adónde vuelve cada fallo (D-68).**
  - Repositorio antes del script: al escritorio.
  - Code1 ajeno en el script: la consola, para reescribirlo.
  - Code1 ajeno en Hora a Hora o en Recursos MES: esa misma pantalla.
  - Code1 ajeno en el login de MES: el login.
  - Log On sin un registro: la pestaña del Repositorio.
- **P-5 · Avance forzado:** al 4.º intento fallido, el Ingreso avanza al POD como si se
  hubiera hecho, y Logout cierra la sesión con su resumen, como hoy. En Guided la guía no
  deja enviar un Code1 ajeno ni hacer algo fuera de secuencia (como D-25), así que no hay
  avance forzado.
- **P-6 · Logout (spec 015) con la limpieza:** el clic en Logout (en el último paso) ya no
  muestra el resumen.
  - Muestra la pantalla de SAP NetWeaver **"You have logged off"** dentro de Edge, como el
    real (captura del usuario, 2026-10-19). **Log On Again no hace nada** (usuario).
  - El operador cierra Edge (✕) y vuelve al escritorio, donde ejecuta el script con su
    Code1. El paso se completa al cerrar la consola, y entonces sale el resumen.
  - En el escritorio, abrir el Repositorio es intento fallido de Logout. Un Code1 ajeno en
    el script también lo es, y se queda en la consola.
  - El operador no cierra sesión en Hora a Hora ni en Recursos MES: el script cierra los
    navegadores (usuario).
- **P-7 · El avance del Ingreso vive en el servidor** (`PodState`, como el resto del POD):
  qué pantallas ya se hicieron y qué pestañas están abiertas. F5 y volver a entrar a la
  sesión lo conservan. Abandon funciona como hoy.
- **P-8 · Guía de Guided:** subpasos para cada pantalla. Enseña Hora a Hora antes que
  Recursos MES y escribe el Code1 del operador en el texto ("Ingrese su Code1:
  **{code1}**"). Incluye las notas de comprensión de la evaluación práctica que tocan a esta
  spec (EP-7): el acceso se hace al inicio del turno y al cambiar de estación; se cierra
  sesión en descansos, al terminar el turno, al cambiar de estación o actividad y al
  entregar la estación; una sesión abierta registra transacciones con el usuario
  equivocado. Los textos se aprueban con el plan (como D-50).
  **En Guided la guía impone ese orden** (solo deja la tarjeta que toca); Assessment acepta
  cualquiera (usuario).
- **P-9 · Login neutro de la app (D-62):**
  - Usa el diseño de `Admin.Master`, con el título "Simulador de entrenamiento MES".
  - Conserva todo lo de la spec 013: autorregistro de operadores, bloqueo, último acceso
    anterior y cambio de contraseña obligatorio.
  - Desaparece la regla de "rol equivocado → camino correcto": ahora hay un solo camino.
  - `Pages/Admin/Login.aspx` y la segunda página de cambio de contraseña se quitan o
    redirigen a las únicas que quedan (lo define el plan).
- **P-10 · "Verifica que su Code1 aparezca"** (evaluación práctica, bloque 1): en Guided,
  después de Log On, un subpaso Info resalta el usuario en el header del POD: "Verifique que
  su usuario aparezca correctamente." No se califica y Assessment no lo muestra (usuario).
- **P-11 · Barra del simulador** (usuario): mientras se ve el escritorio, una franja fina
  arriba, con el estilo de la guía (no imita Windows), muestra el área, el modo y
  **Abandon**. Es la única forma de salir a mitad del Ingreso o de la limpieza.
- **P-12 · Resumen al final** (usuario): al cerrar la consola de la limpieza vuelve el POD
  con el resumen, como hoy. El ícono Logout sigue cerrando la sesión de la app (D-21).
- **P-13 · Cerrar Edge a mitad del Ingreso** (usuario): vuelve al escritorio, sin intento.
  Los registros hechos (Hora a Hora, Recursos MES) se conservan, como en el real, donde
  quedan en el servidor. Abre el Repositorio de nuevo y sigue.
- **P-14 · El script una segunda vez** (usuario): después de ejecutarlo en el Ingreso, su
  ícono no hace nada hasta la limpieza final.

## Scope

### In

1. **Paso nuevo `StepType` "Ingreso a MES"** (D-64), primero en las guías de Moldeo (`07`),
   LoFlo (`09`) y Redefinity (`05`), con 0.05. DC baja 0.05 (D-66). D-56 crea las versiones
   nuevas.
2. **Pantallas simuladas** con sus capturas (P-2): escritorio, consola del script, marco de
   Edge con pestañas, Repositorio, Hora a Hora 3.0 (con el aviso provisional después de
   Iniciar), Recursos MES con la etiqueta simulada de la estación, login de MES con el
   diseño SAP NetWeaver y "You have logged off" después de Logout.
3. **Calificación en el servidor** por pantalla (D-67, D-68, P-3, P-4) y avance forzado
   (P-5).
4. **Logout con la limpieza final** (D-65, P-6).
5. **Login y cambio de contraseña únicos y neutros** (D-62, P-9).
6. **Guía de Guided** del Ingreso y de la limpieza, con las notas de EP-7 que tocan a esta
   spec (P-8).

### Out

- El tablero de Hora a Hora y su registro de unidades por hora: solo el login, más el aviso
  provisional (D-72).
- Hora-Hora (1.0): la tarjeta se ve pero no hace nada.
- Calificar la estación o compararla con la del POD (D-67).
- Verificar la contraseña real en el login de MES (D-73).
- PIN + tarjeta como alternativa al login (WI 6.4).
- El cierre automático de MES a los 15 minutos (sin verificar).
- Las demás notas de comprensión de EP-7 (Resource Setup, Line Clearance, consumo antes
  del MPI): van en su plan directo (roadmap → Fase 8.5, fila 11).

## Domain Model

- **`StepType` nuevo** para el Ingreso a MES (el número lo asigna el plan; hoy el último es
  10). `spTrainingGuideSync` acepta el rango nuevo.
- **`PodState`** guarda el avance del Ingreso (P-7) y el de la limpieza de Logout.
- **Sin tablas nuevas.** El recurso del área para la etiqueta sale de donde lo toma hoy el
  POD (`PodDefaults.ResourceName`).
- `docs/domain.md` → "Flujo estándar" gana el paso 0 "Ingreso a MES", y el paso Logout gana
  la limpieza. "Qué cuenta como intento" y "Ponderación de pasos" también se actualizan.

## API Contract

No hay API pública. El control envía cada pantalla calificable a `PodService` con un
postback. El servidor responde: avanza, intento fallido con la pantalla a la que se vuelve,
o avance forzado.

## Authorization

- Solo Operator entrena (`AccessPolicy.CanTrain`), sin cambios.
- **El login único** (D-62) autentica a los dos roles con el mismo `AuthService.Login` y
  manda a cada rol a su inicio (`SessionHelper.RedirectHome`). `Web.config` sigue
  protegiendo `Pages/Admin` y `Pages/Training` por rol.
- El Code1 contra el que se califica es el del usuario autenticado. Lo toma la BLL de la
  sesión del servidor, nunca del cliente.

## Edge Cases & Failure Modes

- **F5 o volver a entrar a mitad del Ingreso:** sigue en la pantalla donde iba (P-7).
- **Doble envío** de una pantalla: el servidor ignora el segundo, como el Logout de la
  spec 015.
- **Log On con un solo registro:** intento fallido y vuelve a la pestaña del Repositorio
  (P-4).
- **Recursos MES antes que Hora a Hora:** válido (D-67). Buscar lleva al login de MES, y
  desde ahí el operador puede volver a la pestaña del Repositorio para hacer Hora a Hora.
- **Marcado `<x`** en un campo: formato (regla general de `CLAUDE.md`).
- **Cerrar Edge antes del Log On:** escritorio, sin intento, con los registros conservados
  (P-13).
- **Abandon a mitad del Ingreso o de la limpieza:** desde la barra del simulador (P-11).
- **Sesiones en curso al cargar las guías nuevas:** siguen con su versión (D-56), pero la
  limpieza de Logout aplica a todas: es del código, no de la versión (usuario). Como la BD
  es de pruebas, el usuario puede recrearla con `00`–`09` en vez de convivir con versiones
  viejas (`docs/process.md` → "Base de datos").

## Non-Functional Requirements

Nada nuevo: las pantallas se sirven con la página del POD y sus imágenes salen de las
capturas (sin recursos externos).

## Integrations

Ninguna. El Code1 se compara con el del usuario de la sesión; no se consulta la API de
empleados.

## Acceptance Criteria

**Ingreso a MES**
- **AC-1:** Given una sesión nueva en cualquier área y modo, Then el primer paso es Ingreso
  a MES y se ve el escritorio con solo el script y el Repositorio.
- **AC-2:** Given el escritorio, When abre el Repositorio antes de ejecutar el script, Then
  en Assessment es intento fallido y vuelve al escritorio; en Guided la guía no lo deja.
- **AC-3:** Given la consola, When escribe un Code1 que no tiene 9 dígitos, Then es formato
  (sin intento); When escribe un Code1 de 9 dígitos que no es el suyo, Then es intento
  fallido y se queda en la consola; When escribe el suyo, Then se ve la salida del script y
  "Press any key", y una tecla la cierra.
- **AC-4:** Given el Repositorio, Then muestra las tres tarjetas; Hora-Hora (1.0) no hace
  nada; Hora-Hora (3.0) y Recursos MES se abren en su pestaña.
- **AC-5:** Given Hora a Hora 3.0, When Iniciar con su Code1, Then la pestaña muestra el
  aviso provisional; con un Code1 ajeno, intento fallido y se queda ahí.
- **AC-6:** Given Recursos MES, When escanea la etiqueta de la estación y su Code1 y
  presiona Buscar, Then la pestaña pasa al login de MES; con un Code1 ajeno, intento
  fallido; con la estación vacía, formato.
- **AC-7:** Given el login de MES (diseño SAP NetWeaver), When Log On con su Code1, una
  contraseña cualquiera y los dos registros hechos, Then el paso se completa y aparece el
  POD; sin un registro, intento fallido y vuelve a la pestaña del Repositorio; con la
  contraseña vacía, formato.
- **AC-8:** Given Assessment y el 4.º intento fallido del Ingreso, Then el paso queda
  fallido y aparece el POD.
- **AC-9:** Given F5 a mitad del Ingreso, Then sigue en la misma pantalla con lo ya hecho.

**Logout**
- **AC-10:** Given el último paso, When clic en Logout, Then se ve "You have logged off"
  en Edge (sin el resumen todavía); Log On Again no hace nada; cerrar Edge lleva al
  escritorio.
- **AC-11:** Given el escritorio después de Logout, When ejecuta el script con su Code1 y
  cierra la consola, Then el paso se completa y sale el resumen; abrir el Repositorio o un
  Code1 ajeno es intento fallido de Logout.

**Login de la app**
- **AC-12:** Given el login único, When entra un Operator, Then va a elegir área y modo;
  When entra un Administrator, Then va a Gestión de usuarios.
- **AC-13:** Given un primer acceso o un reset (de cualquier rol), Then va a la única
  pantalla de cambio de contraseña, neutra, y después a su inicio.
- **AC-14:** Given el bloqueo, el último acceso anterior y el autorregistro de la spec 013,
  Then funcionan igual con el login único.

**Pesos y guía**
- **AC-15:** Given las guías nuevas, Then Ingreso vale 0.05, DC 0.20 en LoFlo y Redefinity y
  0.25 en Moldeo, y todo suma 1.00; `ScoreCalculatorTests` y los tests de `StepValidator`
  siguen en verde.
- **AC-16:** Given Guided, Then la guía recorre cada pantalla del Ingreso y de la limpieza,
  con Hora a Hora antes que Recursos MES, el Code1 del operador en el texto, las notas de
  P-8 y el subpaso Info del usuario en el header (P-10).

## Deferred Decisions

| Decisión | Respaldo elegido | Se revisa cuando |
|---|---|---|
| Tablero de Hora a Hora después de Iniciar | Aviso provisional (D-72) | Haya captura del tablero |
| ~~URLs de Hora a Hora y de Recursos MES~~ | Resuelta (usuario, 2026-10-19): el host es `crapps.philips.com`, el mismo del Repositorio | — |
| Marco de Edge | Genérico (D-71) | Haya captura de la estación |

## Open Questions

_Ninguna._
