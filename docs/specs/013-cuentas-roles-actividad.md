# Spec 013: Cuentas, roles y registro de actividad

**Status:** Approved (2026-10-13)
**Date:** 2026-10-13 · **Approved:** 2026-10-13 (usuario, con las propuestas de diseño incluidas)

## Problem

Hoy todo usuario es igual: cualquier empleado de la API activa su cuenta con su Code1 y la
contraseña por defecto (`CLAUDE.md` → "Auth Flow"), no hay administradores, una contraseña
olvidada solo se resetea por SQL (`Database/Ops/ResetUserPassword.sql`, T1) y no hay bloqueo
por intentos. De cada intento de paso solo se guarda paso, número, si fue correcto y la hora:
no se sabe **por qué** falló ni **qué** hizo el operador, y eso es lo que pide el futuro
dashboard (T8, Fase 10: "dónde fallan antes de que pase en producción"). La versión de
noviembre (Fase 8.5) necesita que alguien que no sea el desarrollador administre las cuentas.

## Scope

### In

1. **Dos roles excluyentes** (usuario): **Operator** entrena; **Administrator** administra y
   **no entrena**. Columna de rol en `Users`; mapa rol → capacidades en un solo lugar de la
   BLL (`AccessPolicy`); páginas y servicios preguntan por la capacidad, nunca por el nombre
   del rol.
2. **Un login por rol, en la misma aplicación** (usuario): el login actual, con el diseño de
   SAP NetWeaver, es parte de la simulación y queda **solo para operadores**. Los admins
   entran por una página propia, `Pages/Admin/Login.aspx`, con el diseño neutro del módulo
   administrativo. Las dos usan el mismo `AuthService.Login`, la misma cookie de Forms
   Authentication y el mismo bloqueo. Cada login acepta **solo su rol**: un admin en el login
   de SAP recibe "Use el acceso administrativo" (con enlace) y un operador en el login de
   admin, "Sin acceso administrativo". Ninguno de los dos casos cuenta como fallo para el
   bloqueo (la contraseña era correcta). Operator → selección de entrenamiento (como hoy);
   Administrator → Gestión de usuarios.
   - **Cambio de contraseña del admin** (primer acceso y tras un reset) en una página con el
     diseño administrativo; el operador sigue con la de SAP. Misma lógica
     (`AuthService.ChangePassword`).
   - Forms Authentication tiene un solo `loginUrl` (el de SAP): un request sin sesión a
     `Pages/Admin/` se reenvía al login de admin (por el `ReturnUrl`). El logout del admin
     vuelve al login de admin.
3. **Área administrativa** `Pages/Admin/` con su `Admin.Master` (hereda
   `AntiXsrfMasterPage`), protegida por rol (`<location>` en `Web.config`) y verificada otra
   vez en la BLL.
4. **Gestión de usuarios** (usuario): lista con búsqueda por Code1 o nombre que muestra
   Code1, nombre, rol, estado (activa, primer acceso pendiente, bloqueada), **fecha de
   creación** y **último acceso**. Acciones:
   - **Crear usuario:** busca el Code1 en la API de empleados, asigna el rol y lo crea con la
     contraseña por defecto y el cambio obligatorio (`IsFirstLogin = 1`).
   - **Resetear contraseña:** vuelve a la contraseña por defecto con el cambio obligatorio
     (usuario). Conserva todo el historial de entrenamiento.
   - **Desbloquear** antes de que venza el bloqueo.
   - **Dar o quitar el rol de admin.**
5. **Autorregistro y creación por admin conviven** (usuario): cualquier empleado de la API
   sigue activando su cuenta solo, como hoy.
6. **Primer admin por script** (usuario): `Database/Ops/` (como el reset de T1). Los
   siguientes, desde la UI.
7. **Reglas de admins** (usuario): nadie cambia su propio rol y el sistema nunca queda sin
   administradores.
8. **Bloqueo** (usuario): 5 logins fallidos seguidos bloquean la cuenta **15 minutos**; se
   desbloquea sola al vencer o antes por un admin; un login correcto reinicia el contador.
9. **Último acceso visible para el usuario** (usuario, única medida de detección): tras el
   login ve la fecha y hora de su acceso anterior ("Primer acceso" si no hay). Si no fue él,
   avisa al trainer.
10. **Registro de actividad** (usuario): cada intento guarda además su **motivo** y su
    **acción**, en Guided y en Assessment. Sin UI en esta spec: lo consume la Fase 10.

### Out

- Cerrar el reclamo de cuenta ajena (**D-20**, ver Deferred Decisions).
- Historial de logins (tabla por intento de login) y lista de empleados sin cuenta.
- Desactivar cuentas (quien sale de la empresa desaparece de la API y ya no puede entrar).
- Dashboards, reportes y cualquier UI sobre el registro de actividad (Fase 10).
- Alcance por área, otros roles (supervisor, trainer) y tabla de permisos.
- Guardar la respuesta enviada en cada intento (solo motivo y acción).

## Domain Model

**`Users`** (columnas nuevas):

| Columna | Tipo | Regla |
|---|---|---|
| `Role` | `TINYINT NOT NULL DEFAULT 1` | `UserRole`: 1 = Operator, 2 = Administrator |
| `LastLoginAt` | `DATETIME2 NULL` | último login correcto (UTC); null = nunca |
| `FailedLoginCount` | `INT NOT NULL DEFAULT 0` | fallos seguidos; vuelve a 0 con un login correcto o al desbloquear |
| `LockedUntil` | `DATETIME2 NULL` | fin del bloqueo (UTC); null o en el pasado = no bloqueada |

- Estado mostrado en la lista (derivado, no se guarda): **bloqueada** si `LockedUntil` es
  futuro; si no, **primer acceso pendiente** si `IsFirstLogin`; si no, **activa**.
- "Último acceso" que ve el usuario = el `LastLoginAt` **anterior** al login actual: el
  login lo lee antes de actualizarlo.

**`TrainingSessionStepAttempts`** (columnas nuevas, null en las filas anteriores a esta spec):

| Columna | Tipo | Regla |
|---|---|---|
| `Reason` | `TINYINT NULL` | `AttemptReason` (propuesta): `None` (correcto), `OutOfSequence`, `WrongDecision`, `InvalidAction`, `MissingSelection`, `EmptyRequiredField` |
| `Action` | `NVARCHAR(50) NULL` | código de la acción del operador (botón o control), de una clase de constantes de la BLL (propuesta: `ResourceSetup.Save`, `LC.Answer`, `POD.Start`, `POD.LogNC`…) |

- `AttemptReason` es nuevo y propio del registro: **no** reutiliza `PodAttemptReason`, que
  solo distingue dos mensajes de la UI.
- `spStepAttemptRecord` recibe los dos valores; sigue siendo una transacción con el avance y
  el cierre (spec 009).

**Domain:** `User` suma `Role`, `LastLoginAt`, `FailedLoginCount`, `LockedUntil`;
`TrainingSessionStepAttempt` suma `Reason` y `Action`. Enums `UserRole` y `AttemptReason`.

## API Contract

Web Forms: servicios de la BLL, no endpoints HTTP.

- **`AuthService.Login`**: con `User` existente revisa el bloqueo antes de la contraseña;
  cuenta fallos y bloquea al quinto; en el login correcto reinicia el contador, devuelve el
  último acceso anterior y actualiza `LastLoginAt`. `LoginResult` suma `Locked` (con el fin
  del bloqueo) y el rol.
- **`UserAdminService`** (nuevo, `BLL/Admin/`): cada método recibe el `userId` del admin
  autenticado y verifica su capacidad (`AccessPolicy`); si no la tiene, excepción (invariante,
  no aviso). `Search`, `Create(code1, role)`, `ResetPassword`, `Unlock`, `SetRole`.
- **`AccessPolicy`** (nuevo): `CanTrain(role)`, `CanManageUsers(role)`.
- **Principal con rol:** en `Global.asax` → `Application_PostAuthenticateRequest` se arma un
  `GenericPrincipal` con el rol que da la BLL, con caché corta (propuesta: 2 minutos), para
  que funcionen `<location>` con `allow roles` y `User.IsInRole`. No usa la Session: la
  autorización por `<location>` corre antes de que exista.
- **SPs** nuevos o ampliados en `01_spUser.sql` (búsqueda paginada, crear con rol, reset,
  desbloquear, cambiar rol con la regla de "nunca sin admins" dentro de la transacción, login
  fallido y correcto) y `spStepAttemptRecord` con `@Reason` y `@Action`. Cambio de esquema
  según `docs/process.md` → "Base de datos".

## Authorization

| Capacidad | Operator | Administrator |
|---|---|---|
| Entrenar (`StartSession`, `GuidedTest`) | ✅ | ❌ → redirige al módulo administrativo |
| Gestión de usuarios (`Pages/Admin/`) | ❌ | ✅ |
| Cambiar la propia contraseña | ✅ | ✅ |

- El login y el cambio de contraseña de admin quedan accesibles sin sesión (un `<location>`
  más específico dentro de `Pages/Admin`).
- Doble control: `<location path="Pages/Admin">` con `allow roles="Administrator"` y la
  verificación en `UserAdminService`. Las páginas de entrenamiento redirigen a un admin con
  `AccessPolicy.CanTrain`.
- El cambio obligatorio de contraseña (`MustChangePassword`) aplica a los dos roles.
- Un usuario autenticado sin la capacidad de una página va a su inicio, no al login (Forms
  Authentication manda al login por defecto: evitar el bucle).

## Edge Cases & Failure Modes

- **Primer acceso sin `User`** con contraseña equivocada: no hay cuenta que bloquear y no se
  cuenta (la contraseña por defecto es conocida: D-20).
- **Cuenta bloqueada con la contraseña correcta:** "Cuenta bloqueada; intente de nuevo a las
  HH:mm". No revela si la contraseña era correcta.
- **Empleado que ya no está en la API** con `User` local: no puede entrar (paso 1 del login,
  sin cambios); la lista lo sigue mostrando con su historial.
- **Crear un Code1 que no está en la API o que ya tiene `User`:** aviso, no crea nada.
- **Cambio de rol con la sesión abierta:** se aplica en la caché del rol (≤ 2 min). Si un
  operador pasa a admin con un entrenamiento en curso, se abandona (una sola sesión activa,
  D-9; propuesta).
- **Reset con la sesión abierta:** la sesión actual sigue hasta el logout o el timeout; el
  próximo login pide la contraseña por defecto (coherente con D-20).
- **Dos admins quitándose el rol a la vez:** el SP valida "nunca sin admins" dentro de la
  transacción; uno de los dos falla con aviso.
- **Un admin bloqueado:** otro admin lo desbloquea o espera 15 minutos.
- **Intentos anteriores a la spec:** `Reason` y `Action` en null; el dashboard los trata como
  "sin dato".

## Non-Functional Requirements

- Hasta ~1600 usuarios: la lista se busca y se pagina en el servidor (SP paginado).
- La caché del rol evita una consulta por request.
- Contraseñas: PBKDF2 sin cambios (`CLAUDE.md` → "Arquitectura objetivo y seguridad").

## Integrations

API de empleados (`EmployeeRepository`, con su caché) para buscar el Code1 al crear un
usuario. Sin integraciones nuevas.

## Acceptance Criteria

**Roles y navegación**
- **AC-1:** Given un Operator, When hace login en el login de SAP, Then llega a la selección
  de entrenamiento y no ve nada del módulo administrativo.
- **AC-2:** Given un Administrator, When hace login en el login de admin, Then llega a
  Gestión de usuarios.
- **AC-2b:** Given un Administrator en el login de SAP (o un Operator en el de admin) con la
  contraseña correcta, Then no entra, ve el aviso con el camino correcto y no suma un fallo
  al bloqueo.
- **AC-2c:** Given un request sin sesión a `Pages/Admin/`, Then llega al login de admin; y
  el logout del admin vuelve ahí.
- **AC-2d:** Given un admin con cambio obligatorio de contraseña, Then lo hace en la página
  con diseño administrativo.
- **AC-3:** Given un Administrator, When abre `StartSession` o `GuidedTest`, Then lo redirige
  al módulo administrativo.
- **AC-4:** Given un Operator, When abre una página de `Pages/Admin/`, Then no la ve y vuelve
  a su inicio (sin bucle al login).
- **AC-5:** Given un usuario sin la capacidad, When llama un método de `UserAdminService`,
  Then la BLL lanza excepción (test unitario).
- **AC-6:** Given un admin creado por script con la contraseña por defecto, When entra, Then
  tiene que cambiarla antes de usar el módulo.

**Gestión de usuarios**
- **AC-7:** Given la lista, Then muestra Code1, nombre, rol, estado, creada y último acceso,
  con búsqueda por Code1 o nombre y paginación.
- **AC-8:** Given un Code1 de la API sin `User`, When el admin lo crea con un rol, Then queda
  con la contraseña por defecto, cambio obligatorio y ese rol.
- **AC-9:** Given un Code1 que no está en la API o que ya existe, When intenta crearlo, Then
  aviso y no crea nada.
- **AC-10:** Given un usuario, When el admin resetea su contraseña, Then vuelve a la
  contraseña por defecto con cambio obligatorio y conserva su historial.
- **AC-11:** Given un usuario bloqueado, When el admin lo desbloquea, Then puede entrar y su
  contador vuelve a 0.
- **AC-12:** Given un admin, When intenta cambiar su propio rol, Then no puede.
- **AC-13:** Given que quitar un rol dejaría el sistema sin admins, Then se rechaza (también
  con dos admins a la vez).

**Login, bloqueo y último acceso**
- **AC-14:** Given 4 fallos seguidos, When falla el quinto, Then la cuenta queda bloqueada 15
  minutos y lo dice.
- **AC-15:** Given una cuenta bloqueada, When entra con la contraseña correcta antes de que
  venza, Then sigue bloqueada; When vence, Then entra.
- **AC-16:** Given fallos previos, When entra bien, Then el contador vuelve a 0.
- **AC-17:** Given un login correcto, Then ve la fecha y hora de su acceso anterior, o
  "Primer acceso".
- **AC-18:** Given un primer acceso sin `User` con contraseña equivocada, Then no se bloquea
  nada (no hay cuenta).

**Registro de actividad**
- **AC-19:** Given un intento en Guided o en Assessment, Then se guarda con su `Reason` y su
  `Action`, en la misma transacción que el avance (spec 009).
- **AC-20:** Given un click fuera de secuencia en Assessment, Then el intento queda con
  `Reason = OutOfSequence` y la acción del botón presionado.
- **AC-21:** Given los intentos anteriores a la spec, Then siguen leyéndose (con `Reason` y
  `Action` en null) y el puntaje no cambia (`ScoreCalculatorTests` en verde).

## Deferred Decisions

| Decisión | Respaldo elegido | Se revisa cuando |
|---|---|---|
| **D-20:** cerrar el reclamo de cuenta ajena (código del trainer, correo o Windows Auth) | Solo detección: trainer presente + último acceso visible; riesgo aceptado (usuario, 2026-10-13) | Assessment pase a usarse como evidencia de la CAPA, o se entrene sin trainer |
| Historial de logins | No se guarda | Auditoría o investigación de un reclamo |
| Admin que quiere probar las guías (los admins no entrenan) | Usar una cuenta de operador aparte | Lo pida un trainer |
| Catálogo final de `Action` | Constantes definidas en el plan | Diseño del dashboard (Fase 10) |

## Open Questions

_Ninguna._
