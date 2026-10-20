# Flujo Pre-SAP: preparación antes de entrar a SAP MES

> **Fuente:** el usuario, 2026-10-19, con las capturas de
> `Screens SAP MES REAL/Pre-SAPMES/`. Coincide con la WI QD001001524 → 6.4 (pasos 1 a 3) y
> con los bloques 1 y 2 de la evaluación práctica QD001043644
> (`evaluacion-practica-mes.md`). Viene de una CAPA y en planta se audita que el operador
> entre por el Repositorio (trainer de Wires, 2026-10-13). Lo implementa la spec 016.

## Cuándo se hace

Al inicio del turno y cada vez que el operador cambia de estación, antes de procesar
unidades. Al terminar (después del Logout de MES), el operador vuelve a ejecutar el script
de limpieza.

## Pasos

1. **Limpiar historial y caché.** Desde el escritorio de Windows, ejecutar
   `MES Limpiar historial y cache.bat`.
   - Captura: `ESCRITORIO DE WINDWOS .png`. Es una estación virtual (`MCR-VM-CEA3-326`) con
     varios íconos: Notepad, Reiniciar Computador, Repositorio, Hora a Hora, Production…,
     Hora-Hora v2.0, HoraHora-TV, Recursos MES, Script Wyse, el script y la papelera. Para el
     simulador (spec 016), el escritorio muestra **solo el script y el Repositorio**.
2. **Ingresar el CODE1.** La consola (`C:\Windows\system32\cmd.exe`) pide: `Por favor ingrese
   su code1 (exactamente 9 caracteres numericos):`. Después muestra `Script ejecutado por
   {code1}`, dos iteraciones de "Killing browser processes…" (los `ERROR: The process
   "iexplore.exe" / "chrome.exe" / "msedge.exe" not found.` son normales), el borrado de la
   caché de Edge y `Cache and history clearing completed.`. Termina con `Press any key to
   continue . . .`: el operador presiona cualquier tecla y la ventana se cierra.
   - Captura: `iNGRESO CODE1 Y EJECUYCION SCRIPT.png`.
3. **Abrir el Repositorio** desde el acceso directo del escritorio. URL:
   `http://crapps.philips.com/Repositorio/index.aspx`.
   - Captura: `Repositorio APLICACIONES.png`. Título "Repositorio de Aplicaciones", fondo
     oscuro, tres tarjetas con miniatura: **Hora-Hora (1.0)**, **Hora-Hora (3.0)** y
     **Recursos MES**. Logo de Philips abajo a la derecha.
4. **Hora a Hora 3.0.** En el Repositorio, elegir **Hora-Hora (3.0)**, ingresar el CODE1 e
   iniciar sesión.
   - Captura: `login hora-hora 3.0 .png`. Panel azul a la izquierda con selector de idioma
     ("Español ▾"), "BIENVENIDO", "Hora a Hora", "Tiempo Real", el interruptor "Usar modo de
     autenticación de Windows" (apagado), el campo **Code1**, el botón **Iniciar** y la
     versión `V3.0.1`. A la derecha, una foto de la línea.
   - En planta, la pestaña de Hora a Hora **queda abierta** y el operador vuelve a la
     pestaña del Repositorio. No hay captura de lo que se ve después de Iniciar.
5. **Recursos MES.** De vuelta en el Repositorio, elegir **Recursos MES**.
6. **Registrar la estación.** En Recursos MES, ingresar otra vez el CODE1 y escanear la
   estación asignada.
   - Captura: `login recursos mes .png`. Header blanco con el logo de Philips y los enlaces
     "Inicio" e "Ingresar". Franja azul con **Ingresar recurso** e **Ingresar code1**, cada
     uno con su campo, y el botón **Buscar**. Logo de SAP MES abajo a la derecha.
7. **Ingreso a MES.** Con la estación registrada, el sistema redirige al login de MES (SAP
   NetWeaver, `Screens SAP MES REAL/Login SAP NETWEAVER.png`). Ahí empieza el flujo normal
   del POD.

**Orden de los registros:** Hora a Hora y Recursos MES son obligatorios. Lo normal es Hora a
Hora primero, pero el orden no importa (usuario; la trainer dijo "en cualquier orden").

**Navegador:** en producción se usa Edge común.

## Al terminar

1. **Logout** en el POD. MES muestra la pantalla de SAP NetWeaver **"You have logged off"**,
   con el botón **Log On Again** (captura: `logout sap mes.png`).
2. El operador **cierra Edge** y vuelve al escritorio.
3. Ejecuta `MES Limpiar historial y cache.bat` con su CODE1, igual que al entrar.

No cierra sesión en Hora a Hora ni en Recursos MES: el script cierra los navegadores.

## En el simulador (spec 016, cerrada 2026-10-19)

Paso **Ingreso a MES** (primero de la guía) y la limpieza dentro del paso **Logout**, en
pantalla completa sobre el POD (`MesLoginControl`). Reglas y calificación:
`docs/domain.md` → "Flujo estándar" y "Qué cuenta como intento"; mecanismo:
`docs/architecture.md` → "Ingreso a MES y limpieza al salir".

- **Escritorio:** solo el script y el Repositorio (D-69), con la barra de tareas decorativa y
  el fondo recortado de la captura. Arriba, una barra del simulador (no imita Windows) con el
  área, el modo y Abandon (P-11).
- **Consola:** el prompt y la salida de la captura, con el Code1 del operador; "Press any
  key to continue" se cierra con cualquier tecla.
- **Repositorio, Hora a Hora y Recursos MES:** recortes de las capturas (tarjetas, foto, logos).
  Hora-Hora (1.0) no hace nada; el idioma, el interruptor de Windows, "Inicio" e "Ingresar"
  son decorativos. La estación se escanea de una etiqueta simulada del recurso del área, como
  la de la Shop Order (spec 011).
- **Login de MES** y **"You have logged off":** el marco de SAP NetWeaver de la captura
  (estilos que antes eran del login de la app). Log On Again no hace nada.

**Diferencias aceptadas** (Deferred Decisions de la spec 016):
- **Edge genérico** (D-71): pestañas y barra de dirección propias, sin captura de la
  estación. La barra muestra la URL real del Repositorio; en Hora a Hora y Recursos MES, solo
  el host `crapps.philips.com` (confirmado por el usuario); en "You have logged off", vacía.
  Las pestañas no se cierran sueltas: solo la ventana.
- **Hora a Hora después de Iniciar** (D-72): un aviso simple, "Sesión de Hora a Hora
  iniciada", **provisional** hasta tener la captura del tablero.
