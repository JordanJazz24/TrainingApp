# Plan de la Fase 9 — Deploy del MVP

**Status:** **aprobado por el usuario (2026-10-22)** · plan directo, sin spec: no agrega
funcionalidad ni reglas de negocio. Antes de aprobarlo, el usuario confirmó lo que había
decidido Arquitectura y Planeación: regresión en Assessment, log en archivo, reciclado a
hora fija y la Fase 8.5 ✅.
**Decisiones del usuario (2026-10-22):** D-129 a D-134 (abajo).

## Objetivo

Publicar **hoy** el MVP en el IIS interno para recoger retroalimentación. Mientras tanto, el
usuario sigue con el dashboard gerencial (Fase 10) y las mejoras, y en la **2.ª semana de
noviembre** se entrega la versión final con todo. La Fase 8.5 se cierra: todas sus filas
están ✅.

## Decisiones

| D | Decisión |
|---|---|
| D-129 | El MVP se publica hoy, 2026-10-22, para recoger retroalimentación. La versión final, con el dashboard y las mejoras, es la de la 2.ª semana de noviembre. |
| D-130 | **Los datos del MVP se descartan.** En noviembre se instala una BD limpia. Hasta entonces, la BD del servidor se puede recrear con `00`–`09` (`RunAllScripts.ps1 -Recreate` + `CreateAdmin.sql`) y no hace falta escribir `ALTER` (`docs/process.md` §6 sigue como "BD de pruebas"). |
| D-131 | AUD-28, lo barato, y AUD-29, solo los parches: van en una **2.ª publicación** (E2), no en la de hoy. Bootstrap, SweetAlert2 y CodeDom 4.x quedan fuera de la Fase 9. |
| D-132 | Redefinity (`05`, datos de ejemplo) entra en el MVP **avisando que es de demostración**. El aviso va en el mensaje de lanzamiento, no en la app. |
| D-133 | **TI** administra el servidor: da la cadena de conexión, crea la BD, corre los scripts con las instrucciones de `docs/deploy.md` y configura el App Pool (idleTimeout, AlwaysRunning, preload). El usuario publica en una carpeta y se la pasa. |
| D-134 | La retroalimentación se junta **fuera de la app** (Teams, un formulario o reuniones) y la registra la sesión de Reuniones y Dudas. |

## E1 · MVP (hoy)

**Desarrollo:**

> **E1 de Desarrollo cerrada** (2026-10-22, `584cdf2`, con el "funciona" del usuario después de
> la regresión). Detalle que no cambia el plan: el perfil también excluye el `.pdb`
> (`ExcludeGeneratedDebugSymbol`), y `docs/deploy.md` trae la plantilla de
> `connectionStrings.config` completa, porque el `.example` no viaja en la carpeta publicada.

- [x] **Perfil de publicación a carpeta** `TrainingApp/Properties/PublishProfiles/MVP.pubxml`:
  - Release, `FileSystem`, `DeleteExistingFiles`;
  - **precompilado y no actualizable** (`PrecompileBeforePublish`, `EnableUpdateable=false`):
    el primer request no compila nada (los ~18 s del arranque en frío que se midieron en
    desarrollo el 2026-10-07; roadmap → "Fase 9").
- [x] **Verificar la carpeta publicada** (publicar con MSBuild usando el perfil):
  - **no** trae `connectionStrings.config`, que lo crea TI;
  - `web.config` sin `debug` ni `optimizeCompilations`, con `customErrors mode="RemoteOnly"`
    y `configSource="connectionStrings.config"`;
  - trae `Scripts/` (incluido JsBarcode), `Content/` y `bin/`, y no trae ningún `.cs`.
- [x] **`docs/deploy.md`, instrucciones para TI** (en lenguaje de TI, sin jerga del dominio):
  1. **Requisitos:** IIS con ASP.NET 4.7.2 y el feature *Application Initialization*; SQL
     Server; salida HTTPS del servidor a la API de empleados (el host de
     `Constants.EmployeeApiUrl`; sin ella nadie puede registrarse ni entrar).
  2. **BD:**
     - crear una BD vacía;
     - correr `Database/StoredProcedures/00`–`09` en orden (`Database/Ops/RunAllScripts.ps1
       -Server … -Database … [-User …]` o SSMS), incluido `05`;
     - crear el login de la app con `GRANT EXECUTE` sobre la BD, sin acceso a tablas (la app
       solo usa stored procedures);
     - primer administrador con `Database/Ops/CreateAdmin.sql` y el Code1 que dé el usuario.
  3. **Sitio:**
     - copiar la carpeta publicada;
     - crear `connectionStrings.config` junto a `web.config` con la plantilla de
       `connectionStrings.config.example`, con el nombre `Default`;
     - darle al usuario la cadena de conexión que corresponda (D-133).
  4. **App Pool:**
     - .NET CLR v4.0, Integrated;
     - **`idleTimeout` = 0 (obligatorio):** si el proceso se detiene, se pierde la Session
       InProc y el operador sale de su entrenamiento;
     - `startMode="AlwaysRunning"` y `preloadEnabled="true"` en el sitio;
     - reciclado periódico a una hora fija fuera de turno, en vez de cada 29 h.
  5. **HTTPS:** si el sitio tiene certificado, avisar al usuario (lo usa la E2).
  6. **Republicar:** reemplazar la carpeta **conservando `connectionStrings.config`** y
     reciclar el App Pool.
  7. **Recrear la BD** (D-130): `RunAllScripts.ps1 -Recreate` + `CreateAdmin.sql`.
- [x] **`docs/process.md` §6:** una línea que aclara que la BD del servidor del MVP también
  es de pruebas (D-130) hasta la versión final.

**Usuario:**

- [x] **Regresión local** antes de publicar, en Assessment: **LoFlo (Serialized) completa** y
  **Moldeo (NonSerialized) completa**, del login al resumen.
- [ ] Publicar con el perfil `MVP` y pasarle a TI la carpeta, `Database/` y `docs/deploy.md`.
- [ ] **Prueba de humo en el servidor**, cuando TI lo tenga listo (cierra D-19):
  - el `web.config` publicado no tiene `debug="true"` y sí `customErrors`;
  - desde otra PC, ya autenticado, `…/Pages/Training/GuidedTest.aspx?sessionId=<b>`
    muestra el "Runtime Error" genérico, sin detalle;
  - el admin entra, cambia la contraseña y crea o resetea un usuario;
  - un operador se registra y aparecen las guías de Moldeo, LoFlo y Redefinity;
  - una sesión de Guided llega hasta el Start (confirma que la API y la BD responden).
- [ ] Mensaje de lanzamiento: que Redefinity es de demostración (D-132) y por dónde se manda
  la retroalimentación (D-134).

**Verificación de Desarrollo:** rebuild, `aspnet_compiler` (el perfil precompila) y la
revisión de la carpeta publicada. Sin `/code-review`: no cambia código de la app.

## E2 · Endurecimiento y parches (2.ª publicación, después del MVP)

> **Enmienda 1 · 2026-10-22 (usuario, con el reporte de la E1 de Desarrollo):**
> - **Cuándo (D-135):** la E2 no va enseguida del MVP. Se hace **junto con la Fase 10**, antes
>   de la versión final de noviembre, y la Fase 9 se cierra con ella.
> - **Se suma:** corregir el comentario de `Web.config` que dice que la plantilla
>   `connectionStrings.config.example` está "en esta misma carpeta". En el servidor no está:
>   el comentario tiene que apuntar a `docs/deploy.md`, que trae la plantilla.

> **E2 de Desarrollo integrada a `TrainingApp` sin probar** (2026-10-22, rama `fase-10`; el
> usuario pidió integrar sin el "funciona": falta su checklist). Detalle que no cambia el
> plan:
> - el login renueva el ID con `Session.Abandon` + la cookie expirada (como `Logout`); la página
>   siguiente llena la Session desde la BD. El "Último acceso" viaja en el ticket de Forms (cifrado),
>   así que también se ve tras re-hidratar la Session. Quien vuelve a entrar con un entrenamiento
>   a medias pierde el avance de Assembly/DC en memoria (D-10 lo reconstruye); no hay forma de
>   retomarlo desde la UI (D-9), así que no cambia el flujo;
> - log en `App_Data/Logs/AAAA-MM-DD.log` (UTC), con retención de 60 días; los 404 no se
>   registran; el Code1 que no existe en la API se enmascara (puede ser una contraseña escrita en
>   el campo equivocado); de las excepciones de validación y de ViewState solo el tipo y el stack;
> - `requireSSL` queda comentado en `Web.Release.config` hasta que TI confirme HTTPS;
> - `System.*` (solo los usa `TrainingApp.Test`): parches DiagnosticSource 6.0.2 y
>   Reflection.Metadata 8.0.1; los demás no tienen parche en su versión menor;
> - del `/code-review medium` (9 hallazgos) se aplicaron 5 (mensajes de excepción limpios y sin
>   ViewState, `Application_Error` que no lanza, Code1 enmascarado, caracteres de control,
>   retención) y se descartaron 4 (PodState al volver a entrar, ver arriba; dos procesos
>   escribiendo el log en un reciclado; la cabecera `Server`, porque `removeServerHeader` solo
>   existe en IIS 10 y rompe el sitio en uno anterior; reconstruir el ticket a mano en vez de
>   `GetAuthCookie`).

- [x] **AUD-28, lo barato:**
  - **renovar el ID de Session en el login** (contra la fijación de sesión): la Session del
    usuario se crea con un ID nuevo, sin que cambie el flujo de login, primer acceso ni
    cambio de contraseña;
  - **`Application_Error`** en `Global.asax.cs`: registra el error (fecha, URL, usuario si lo
    hay, excepción) en un archivo diario en `App_Data/Logs`, sin contraseñas ni datos de
    formularios; la página que ve el usuario sigue siendo la de `customErrors`;
  - **registro de los logins fallidos y de los bloqueos** en el mismo archivo (Code1, motivo
    y hora; nunca la contraseña);
  - **cabeceras:** `X-Content-Type-Options: nosniff`, `X-Frame-Options: SAMEORIGIN`,
    `Referrer-Policy: same-origin`; quitar `X-AspNet-Version`
    (`enableVersionHeader="false"`) y `X-Powered-By`. Sin CSP: los scripts inline del POD
    obligarían a reescribirlos;
  - **cookies:** `httpOnlyCookies="true"`; `requireSSL` (en `httpCookies` y en `forms`)
    **solo si TI confirma HTTPS**, en `Web.Release.config`.
- [x] **AUD-29, parches:** Newtonsoft.Json 13.0.4, jQuery 3.7.1,
  Microsoft.Web.Infrastructure 2.0.1 y `System.*`, uno por uno, con rebuild y tests después
  de cada uno. Confirmar la última versión de parche al momento de actualizar.
- [x] Comentario de `Web.config` sobre la plantilla de `connectionStrings.config`: que apunte a
  `docs/deploy.md` (enmienda 1).
- [x] `docs/deploy.md`: permiso de escritura en `App_Data/Logs` para la identidad del App
  Pool.
- [x] Verificación: rebuild, `aspnet_compiler`, tests y `/code-review medium` sobre
  `Global.asax.cs`, `SessionHelper`, el login, `Web.config` y `Web.Release.config`.
- [ ] Checklist del usuario:
  - login, primer acceso, cambio de contraseña, bloqueo tras 5 fallos y logout;
  - el ID de la cookie de Session cambia al hacer login;
  - un error provocado queda en el archivo de log;
  - las cabeceras se ven en las herramientas del navegador;
  - una sesión completa de Serialized y una de NonSerialized (jQuery cambia).
- [ ] Republicar (paso 6 de `docs/deploy.md`).

## No incluye

- Dashboard e historial por operador (Fase 10).
- Un botón de retroalimentación en la app (D-134).
- Bootstrap 5.3.8, SweetAlert2 11.26 y CodeDom 4.x (D-131).
- Configurar el certificado HTTPS: es de TI.
- CSP.
- CI/CD y monitoreo avanzado.
- Conservar los datos del MVP (D-130).

## Docs al cerrar

- **Arquitectura y Planeación:** roadmap (Fase 8.5 ✅ hoy; Fase 9 ✅ al cerrar la E2) y
  `decisions.md`.
- **Desarrollo:**
  - `status.md`: el estado, AUD-28/29 en "Deuda y riesgos", D-19 y D-8, que pasa a no
    aplicar en el servidor porque TI da su login;
  - `history.md`;
  - la auditoría `2026-10-01` (AUD-28/29 ☑ vía la Fase 9).
