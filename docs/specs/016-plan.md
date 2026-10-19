# Plan — Spec 016: Preparación antes de SAP (Ingreso a MES y limpieza al salir)

**Spec:** `016-preparacion-antes-de-sap.md` (aprobada 2026-10-19, con P-1 a P-14) ·
**Estado:** **aprobado por el usuario (2026-10-19)**, con D-75 a D-81 y los textos de la guía.
**Autoría:** sesión de Arquitectura y Planeación (verificado contra el código de
`TrainingApp`, `5ebbf77`). Lo ejecuta "desarrollo" (`docs/process.md` → "Tres sesiones de
trabajo").

Cuatro entregas. Cada una con el ciclo de `docs/process.md` §2: rebuild, `aspnet_compiler`,
tests, review calibrado, checklist en el navegador, "funciona" y commit con el roadmap.
**BD:** es de pruebas; cada entrega que toque scripts se prueba recreando la BD con `00`–`09`,
sin `ALTER` aparte (`docs/process.md` → "Base de datos", 2026-10-19).

## Lo que se verificó en el código antes de planear

- **`StepType`** (`Domain/TrainingStep.cs`) llega hasta `LogNCExtendedRepair = 10`.
  `spTrainingGuideSync` (`02`) rechaza `StepType > 10`. Con D-56, agregar un paso al
  principio crea una versión nueva de la guía.
- **Logout** (`PodService.ClickLogout`) es `CheckSequence` + `SubmitStep(StepAnswers.Logout)`:
  en el último paso cierra la sesión y la página muestra el resumen. Antes del último paso es
  un botón más del POD (S2).
- **Precedente de calificación a mitad del paso:** `PodService.Split` registra un intento
  fallido con `RecordAttempt` sin cerrar el paso, y si es el 4.º aplica el split por defecto.
  Las pantallas del Ingreso siguen el mismo patrón (D-68).
- **Avance forzado:** `CheckSequence` y `RestoreAfterForcedAdvance` deciden qué hace el POD
  cuando un paso se fuerza; el Ingreso y la limpieza de Logout necesitan su caso.
- **`PodState`** vive en `Session` (`SessionPodStateStore`); F5 lo conserva. Si se pierde,
  `RebuildState` lo reconstruye desde los pasos resueltos: el avance a medias del Ingreso no
  se puede reconstruir y vuelve a empezar desde el escritorio (se acepta; P-7 cubre F5).
- **El Code1 del usuario** no está en `PodContext`. `PodService.LoadContext` recibe el
  `userId`; la BLL lo lee de `UserRepository` (nunca del cliente).
- **Etiqueta simulada** (spec 011): panel "no es parte del MES" con Code 128 (JsBarcode
  local) en `GuidedTest.aspx`; tocar las barras equivale a escanear. Se reutiliza para la
  estación. El recurso del área sale de `PodDefaults.ResourceName(area)` (`{Área}-01`).
- **Login:** `Pages/Auth/Login.aspx` (SAP NetWeaver, `Auth.Master`, `auth.css`) y
  `Pages/Admin/Login.aspx` (`Admin.Master`). Los dos usan `AuthService.Login`, que devuelve
  `WrongPortal` al rol equivocado. Hay dos `ChangePassword` (Auth y Admin). `Web.config`
  abre `Pages/Admin/Login` y `Pages/Admin/ChangePassword` a anónimos y su único `loginUrl` es
  el de Auth. `SessionHelper` tiene las URLs de los dos.
- **`Admin.Master`** tiene la barra "MES Training · Administración" con usuario, último
  acceso y "Cerrar sesión".
- **Guía de Guided:** `GuideScriptBuilder` hace un `switch` por `StepType`. Todo control
  nuevo necesita anclas `data-guide` y su guion (`CLAUDE.md` → "POD y controles").

## Decisiones de diseño (propuestas en este plan; se aprueban con él)

- **D-75 · `StepType.MesLogin = 11`** ("Ingreso a MES"). `spTrainingGuideSync` acepta de 0 a
  11. `StepTypes` y `GuidedTest` lo tratan como paso **de pantalla completa**: mientras es el
  paso actual no se pinta el POD.
- **D-76 · Un solo control, `MesLoginControl`**, declarado de forma estática en
  `GuidedTest.aspx` (spec 006). Contiene el escritorio, la consola, el marco de Edge con sus
  pestañas y las pantallas web: Repositorio, Hora a Hora, Recursos MES, login de MES y "You
  have logged off". Sirve al Ingreso y a la limpieza de Logout. Las imágenes y estilos
  salen de las capturas; los de SAP NetWeaver se mudan de `auth.css`.
- **D-77 · Qué viaja al servidor:** cada acción **calificable o que cambia el avance** es un
  postback a `PodService`:
  - Ingreso: abrir el Repositorio, ejecutar el script (Code1), Iniciar en Hora a Hora
    (Code1), Buscar en Recursos MES (recurso + Code1), Log On (Code1 + contraseña) y cerrar
    Edge.
  - Logout: el clic en Logout, cerrar Edge en "You have logged off", abrir el Repositorio en
    el escritorio y ejecutar el script (Code1).

  Cambiar de pestaña, abrir una tarjeta, cerrar la consola con una tecla y la tarjeta 1.0 se
  resuelven en el cliente. Los formatos (vacío, 9 dígitos, `<x`) los bloquea el control, como
  siempre.
- **D-78 · Avance en `PodState`:** `MesLoginProgress` (script hecho, Hora a Hora hecho,
  Recursos MES hecho, login de MES abierto, Edge abierto) y `LogoutProgress` (logged off,
  Edge cerrado). Después de F5, la página reconstruye la pantalla desde ahí: escritorio,
  Repositorio con las pestañas de lo ya hecho, o login de MES. Lo que no se envió (una
  tarjeta abierta sin Iniciar) se pierde con F5. Es la lectura de AC-9 que propone este
  plan.
- **D-79 · Code1 en `PodContext`:** `LoadContext` lo carga con el usuario; los métodos del
  Ingreso lo comparan sin distinguir mayúsculas ni espacios (`Trim`). Un Code1 ajeno es
  `WrongDecision` con el mensaje genérico; el orden incorrecto es `OutOfSequence`.
- **D-80 · Login único** (D-62, P-9):
  - Queda `Pages/Auth/Login.aspx` (el `loginUrl` de Forms) y `Pages/Auth/ChangePassword.aspx`,
    con el estilo neutro de `admin.css`. La barra del master, sin usuario, dice "Simulador de
    entrenamiento MES".
  - Se borran `Pages/Admin/Login.aspx` y `Pages/Admin/ChangePassword.aspx`, sus excepciones
    en `Web.config` y las URLs de `SessionHelper`.
  - `AuthService.Login` deja de recibir el portal y de devolver `WrongPortal`.
  - Después de login y del cambio de contraseña: `SessionHelper.RedirectHome`.
  - `Auth.Master` y `auth.css` se borran cuando sus estilos de SAP pasen a `MesLoginControl`
    (E2).
- **D-81 · No se sube a `main` hasta cerrar la E4.** Entre la E2 y la E4, Guided no tiene la
  guía del Ingreso (el operador lo hace sin ayuda y sin penalización) y Logout termina como
  hoy hasta la E3.

## Entrega 1 — Login único y neutro (D-62, D-80; AC-12 a AC-14)

- [ ] `AuthService.Login` sin portal ni `WrongPortal`; `LoginResult` sin ese estado. Los
      tests de la BLL que lo usen se ajustan.
- [ ] `Pages/Auth/Login.aspx` y `ChangePassword.aspx` con el diseño neutro (master neutro,
      `admin.css`), título "Simulador de entrenamiento MES". Conservan el bloqueo, el último
      acceso anterior y el cambio obligatorio.
- [ ] Se borran `Pages/Admin/Login.aspx` y `Pages/Admin/ChangePassword.aspx`, sus
      `<location>` en `Web.config` y las URLs de `SessionHelper`. `MustChangePassword` y el
      logout de admin apuntan a las páginas únicas.
- [ ] `Auth.Master` queda solo hasta la E2 si alguna página aún lo usa; si no, se borra ya.
- [ ] Checklist: Operator y Administrator entran por el mismo login y llegan a su inicio;
      primer acceso y reset de los dos roles; bloqueo a los 5 fallos; último acceso.

## Entrega 2 — Ingreso a MES (D-61, D-64, D-66 a D-75; AC-1 a AC-9, AC-15)

- [ ] **Domain y BD:** `StepType.MesLogin = 11`. `02`: `spTrainingGuideSync` acepta 0–11.
      Guías `05`, `07` y `09`: Ingreso primero (0.05) y DC −0.05 (LoFlo y Redefinity 0.20,
      Moldeo 0.25). Se prueba recreando la BD.
- [ ] **BLL:** `MesLoginProgress` en `PodState` y el Code1 en `PodContext` (D-78, D-79).
      Métodos de `PodService`, uno por acción de D-77, con `RecordAttempt` en el fallo y la
      pantalla a la que se vuelve (P-4). Log On con los dos registros cierra el paso. Avance
      forzado: el paso queda fallido y aparece el POD (P-5). `AttemptActions` para cada
      acción (registro de actividad, spec 013).
- [ ] **UI:** `MesLoginControl` (D-76) con escritorio (solo script y Repositorio, P-2),
      consola, Edge genérico con pestañas (D-71), Repositorio con las tres tarjetas (1.0 sin
      acción), Hora a Hora con el aviso provisional después de Iniciar (D-72), Recursos MES
      con la **etiqueta de la estación** (panel de la spec 011 con
      `PodDefaults.ResourceName`) y login de MES con el diseño SAP NetWeaver. Cerrar Edge
      vuelve al escritorio y conserva (P-13); el script ya ejecutado no hace nada (P-14).
- [ ] **Barra del simulador** (P-11) con área, modo y Abandon mientras se ve el escritorio.
- [ ] `GuidedTest`: el paso `MesLogin` oculta el POD y muestra el control; al cerrarse,
      aparece el POD. Anclas `data-guide` en todos los elementos de las pantallas (la guía
      llega en la E4).
- [ ] **Tests:** calificación del Ingreso en la BLL (Code1 propio y ajeno, secuencia, Log On
      sin un registro, forzado al 4.º) y pesos de las guías (AC-15). `ScoreCalculatorTests`
      en verde.

## Entrega 3 — Logout con la limpieza (D-65, P-6, P-12; AC-10, AC-11)

- [ ] **BLL:** `LogoutProgress` (D-78). El clic en Logout del último paso ya no cierra el
      paso: pasa a "You have logged off". Cerrar Edge lleva al escritorio. El script con el
      Code1 propio cierra el paso (`StepAnswers.Logout`) y la sesión. Abrir el Repositorio o
      un Code1 ajeno es intento fallido de Logout. Avance forzado: cierra la sesión con su
      resumen, como hoy.
- [ ] **UI:** "You have logged off" en Edge (Log On Again sin acción), escritorio y consola
      de `MesLoginControl`. Al cerrar la consola vuelve el POD con el resumen (P-12).
- [ ] Aplica a toda sesión, sin importar la versión de su guía.
- [ ] Se guarda la captura de "You have logged off" en `Pre-SAPMES/` (la entrega el usuario).
- [ ] **Tests:** casos de la limpieza en la BLL.

## Entrega 4 — Guía de Guided (P-8, P-10; AC-16)

- [ ] `GuideScriptBuilder`: subpasos del Ingreso y de la limpieza con los textos de abajo.
      La guía impone Hora a Hora antes que Recursos MES y no deja enviar un Code1 ajeno ni
      salir de secuencia (como D-25).
- [ ] Subpaso Info del usuario en el header después de Log On (P-10).
- [ ] Checklist completo de la spec en los dos modos y en las tres áreas.

## Textos de la guía de Guided (para aprobar con el plan)

Tratamiento de usted. `{code1}` es el Code1 del operador.

**Ingreso a MES**
| Subpaso | Texto |
|---|---|
| Info (primero) | "Antes de entrar a SAP MES se prepara la estación. Esto se hace al inicio del turno y cada vez que cambia de estación, antes de procesar unidades." |
| Script | "Haga doble clic en **MES Limpiar historial y cache** para limpiar el navegador." |
| Code1 del script | "Escriba su Code1: **{code1}** y presione Enter." |
| Cerrar la consola | "Presione cualquier tecla para cerrar la ventana." |
| Repositorio | "Haga doble clic en **Repositorio**." |
| Hora a Hora | "Seleccione **Hora-Hora (3.0)**." |
| Code1 de Hora a Hora | "Escriba su Code1: **{code1}** y presione **Iniciar**." |
| Volver al Repositorio | "Deje abierta esta pestaña y vuelva a la del **Repositorio**." |
| Recursos MES | "Seleccione **Recursos MES**." |
| Recurso | "Escanee la etiqueta de su estación en **Ingresar recurso**." |
| Code1 de Recursos MES | "Escriba su Code1: **{code1}** y presione **Buscar**." |
| Login de MES | "Ingrese su usuario (**{code1}**) y su contraseña, y presione **Log On**." |
| Info del usuario (P-10) | "Verifique que su usuario aparezca correctamente." |

**Logout con la limpieza** (después del texto de Logout de la spec 015)
| Subpaso | Texto |
|---|---|
| Logout | "Cierre sesión siempre que deje la estación: en descansos, al terminar el turno, al cambiar de estación o de actividad y al entregarla a otra persona. Una sesión abierta registra transacciones con el usuario equivocado." |
| Logged off | "Cierre el navegador con la **✕**." |
| Script | "Haga doble clic en **MES Limpiar historial y cache**." |
| Code1 del script | "Escriba su Code1: **{code1}** y presione Enter." |
| Cerrar la consola | "Presione cualquier tecla para terminar." |

## Al cerrar la spec

- [ ] `CLAUDE.md` → "Auth Flow": un solo login neutro y una sola pantalla de cambio de
      contraseña (enmienda 2 de la spec 013); el diseño de SAP NetWeaver está en el login de
      MES simulado. "POD y controles": la excepción de D-68 a "un postback por paso" (como el
      Split).
- [ ] `docs/domain.md`: paso "Ingreso a MES" en el flujo estándar, Logout con la limpieza,
      "Qué cuenta como intento" (Code1 propio, secuencia, formatos) y la tabla de pesos.
- [ ] `docs/architecture.md`: `MesLoginControl`, `MesLoginProgress` y `LogoutProgress`, y el
      Code1 en `PodContext`.
- [ ] `docs/ui-reference/pre-sap-flow.md` (diferencias aceptadas: Edge genérico, aviso de
      Hora a Hora) e `inventario-pantallas.md` (pantallas Pre-SAP).
- [ ] Roadmap: fila 7 de la Fase 8.5, "Estado actual", "Historial" y la deuda (aviso
      provisional de Hora a Hora, Edge genérico).
