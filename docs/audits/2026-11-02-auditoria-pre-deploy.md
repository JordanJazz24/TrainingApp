# Auditoría pre-deploy (versión final) — 2026-11-02

> Encargada por Planeación antes de la 2.ª publicación (versión final, 2.ª semana de
> noviembre). `docs/process.md` → "4. Auditorías": **Fase A** (solo lectura) y **Fase B**
> (esta lista). La Fase C la ordena Planeación con el usuario y la implementa Desarrollo.
> Continúa la numeración: hallazgos desde **AUD-71**. La auditoría de fidelidad de UI
> (AUD-45…70) queda fuera y no se reabre.

## Alcance

Todo `TrainingApp` (Pages, BLL, DAL, Domain, Shared, masters, `Export.ashx`, `Web.config` y
sus transformaciones), `Database/StoredProcedures`, `Database/Ops` y `TrainingApp.Test`, sobre
`c1566a0` (árbol de trabajo limpio). Se contrastó con `CLAUDE.md` y `docs/architecture.md`.
Lo que cambió desde la auditoría del 2026-10-06 (440 commits: Fase 11, specs 013–025) se
revisó a fondo; lo que no cambió, con una pasada de regresión.

## Método

1. **Línea base del compilador y los tests** (comandos de `CLAUDE.md`).
2. **Seguridad:** `Web.config` y sus transformaciones; login, bloqueo, PBKDF2, Session,
   fijación de sesión, `RoleCache`, `AccessPolicy` en cada método público de los servicios de
   Admin, Reports y Guides; anti-CSRF en los 3 masters y en `Export.ashx`; codificación al
   HTML (`<%#` y `<%=` sin `:`, `Literal`, `Text`) y al JS (`JavaScriptSerializer`,
   `JavaScriptStringEncode`, `innerHTML`, SweetAlert con `text`); dueño y estado de la
   sesión en `PodService`, `TrainingSessionService` y `SessionResultService`;
   `spStepAttemptRecord`; SQL dinámico en los scripts; secretos en lo versionado y en el
   historial; paquetes de `packages.config` y librerías JS vendorizadas.
3. **Arquitectura:** búsquedas por regla de `CLAUDE.md` (DAL fuera de la BLL, `System.Web` en
   BLL o Domain, SQL inline, `DataSet`/`DataTable`, `[WebMethod]`, `LoadControl`, `Session[`,
   `mode ==` y nombres de rol fuera de `ModePolicy` y `AccessPolicy`) y ciclos con Roslyn.
4. **Calidad:** tamaño de clases y métodos, manejo de errores, estado global, `DateTime.Now`
   y código muerto (Roslyn como candidatos, confirmados contra el markup).
5. **Verificación:** cada hallazgo se confirmó releyendo la línea citada. Lo que choca con
   una decisión D-n ya cerrada se cita y no cuenta como nuevo.

**`/code-review` por carpetas: no se corrió.** El skill revisa diffs. El de `2026-10-06..HEAD`
son 440 commits y cada entrega ya pasó su `/code-review` en su ciclo (`docs/process.md` → 3).
En su lugar se hizo la revisión manual dirigida de arriba. Si Planeación lo quiere igual, la
propuesta es un `/code-review high` sobre `BLL/Training` y `Pages/Training`, que concentran
el riesgo de puntaje.

## Fase A · Resultados

| Chequeo | Resultado |
|---|---|
| MSBuild Rebuild (`TrainingApp.Test`, que compila `TrainingApp`) | **0 warnings, 0 errores.** `Nullable` no está habilitado en los csproj (C# 7.3 de .NET Framework); no es hallazgo. |
| `aspnet_compiler` sobre una copia sin `obj/`, `.vs` ni `Properties/publish` | **OK** (exit 0). La deuda del entorno se confirma: en el checkout falla por esas carpetas. La copia necesita `robocopy /R:0 /W:0`: con los reintentos por defecto se traba en un archivo bloqueado. |
| vstest | **515 de 515 en verde.** |
| Capas (`arch-check` por búsquedas + Roslyn) | **Conforme.** Pages/UI/Shared/Domain sin DAL; BLL y Domain sin `System.Web`; DAL solo con stored procedures (`CommandType.StoredProcedure`, sin SQL inline, `DataSet` ni `DataTable`); sin `[WebMethod]` ni `LoadControl`; 0 ciclos de tipos. Los `SessionMode.X` de las páginas son etiquetas y radios; las reglas pasan por `ModePolicy`. Los roles solo en `AccessPolicy`. |
| Autorización en la BLL | **Conforme.** Los 8 métodos públicos de `DashboardService`, los 6 de `UserAdminService` y los 2 de `GuideAdminService` reciben el `userId` y verifican la capacidad. `PodService.LoadContext`, `TrainingSessionService.Abandon` y `SessionResultService.Get` validan dueño y estado. `StartSession` valida con `TrainingAccessRule.EnsureCanStart`, así que B-20 quedó cerrado. `spStepAttemptRecord` revalida paso y número de intento con `UPDLOCK`. |
| Codificación (XSS) | **Conforme.** Los `<%# Eval %>` sin `:` son booleanos, números o atributos de controles de servidor, que ASP.NET ya codifica. Todo JSON hacia `<script>` sale con `JavaScriptSerializer`, que escapa `<`, `>`, `&` y `'`. El HTML de la guía pasa por `WebUtility.HtmlEncode` (`GuideScriptBuilder.H`, `MesErrorNotes.Html`). Las confirmaciones de SweetAlert usan `text`, no `html`. El `.xlsx` escribe celdas `inlineStr`, sin fórmulas. |
| Anti-CSRF | **Conforme.** `Site`, `Training` y `Admin.Master` heredan `AntiXsrfMasterPage`; toda página usa uno de los tres. `Export.ashx` es GET de solo lectura y revisa sesión, capacidad y primer acceso. |
| Contraseñas | **Conforme.** PBKDF2-SHA256, 100 000 iteraciones, sal de 16 bytes y comparación en tiempo constante. |
| SQL | Sin SQL dinámico en `Database/` (sin `sp_executesql` ni `EXEC(@…)`). Los generadores de volumen se niegan a correr fuera de `TrainingApp_DashboardTest`. |
| Paquetes | Sin avisos de seguridad conocidos para las versiones de `packages.config` ni para las librerías JS (jQuery 3.7.1, Bootstrap 5.3.8, Chart.js 4.5.1, SweetAlert2 11.7.23, JsBarcode 3.11.6), según el conocimiento disponible. **No se consultó la base de avisos en línea.** Las actualizaciones diferidas siguen en D-131. |
| Código muerto | Los 51 símbolos de `find_dead_code` son handlers y propiedades enlazados en el markup (falsos positivos, verificados). Sin código muerto nuevo. `CompleteControl` sigue como deuda conocida. |
| Antipatrones (Roslyn) | Ver la nota de la sección "Sugerencias". |

## Estado de las auditorías anteriores

- **2026-10-01 (AUD-01…32):** siguen cerradas, con una excepción. **AUD-04 está a medias:**
  la cadena salió del repo, pero la contraseña de `sa` que quedó en el historial **no se
  rotó**; ahora es AUD-71. Se verificaron puntualmente AUD-08/09/10 (primer acceso,
  contraseña actual, Session re-hidratada), AUD-11 (JSON re-serializado), AUD-14/15 (sin DAL
  en páginas), AUD-17 (anti-CSRF), AUD-18 y 24 (codificación) y AUD-22 (paquetes de test
  fuera del proyecto web). De **AUD-28** sigue abierto lo que dependía de TI: la cabecera
  `Server` (D-144, confirmado: falta `removeServerHeader` en `Web.Release.config`) y
  `requireSSL`, que ahora es AUD-72 porque TI ya confirmó HTTPS. AUD-29 quedó con las
  actualizaciones diferidas de D-131. AUD-32 sigue como riesgo aceptado (D-20).
- **2026-10-06 (AUD-33…44):** siguen cerradas. Logout en `finally` (AUD-35), P-1 deja pasar
  Abandon (AUD-34), la página no deriva el flujo (AUD-38), `StartSession` borra los
  `PodState` anteriores (AUD-41) y `FindRow` está borrado (AUD-43). **AUD-40 y AUD-42 siguen
  diferidos (D-16), sin cambios:** `PodState` con 8 setters públicos; `Guid.NewGuid` y
  `Random` en `SimulatedOrder`.
- **Ya sabido, confirmado:** D-144 (falta `removeServerHeader`) y el fallo de
  `aspnet_compiler` en el checkout por `obj/Release` y `Properties/publish` (no se borraron).

## Lista única de hallazgos (AUD-71 en adelante)

Severidad: **Alto** = expone datos o credenciales, o rompe el flujo o el puntaje; **Medio** =
riesgo real acotado o deuda que encarece el mantenimiento; **Bajo** = corrección menor;
**Info** = sin acción obligatoria. **Bloquea** = hay que resolverlo antes de la 2.ª
publicación.

| ID | Sev. | Hallazgo y escenario | Dónde | Vía | Bloquea |
|---|---|---|---|---|---|
| AUD-71 | Alto | **La contraseña de `sa` filtrada en el historial no se rotó.** AUD-04 pidió rotarla (quedó en `git` y en `origin` desde el primer commit). La instancia local `YY364723\SQLEXPRESS` sigue usando esa misma contraseña: se comparó sin mostrarla. La app local se conecta como `sa`. Escenario: cualquiera con acceso de lectura al repo tiene `sysadmin` sobre toda instancia donde se haya reutilizado esa contraseña. No se pudo verificar qué login usa producción. La guía que recomendaba uno de mínimo privilegio se borró (AUD-73). | `connectionStrings.config` local; historial anterior a `3b49ecc` (`TrainingApp/Web.config`) | Plan directo, sin código: rotar la contraseña de `sa` en toda instancia que la use. Confirmar que producción usa un login propio con solo `EXECUTE` sobre `dbo`. En desarrollo, usar ese mismo login de mínimo privilegio. | **Sí** (al menos confirmar que producción no usa `sa` ni esa contraseña) |
| AUD-72 | Medio | **Las cookies siguen sin `Secure` aunque el sitio ya tiene HTTPS.** `requireSSL` está comentado "hasta que TI confirme HTTPS", y TI ya lo confirmó (D-19 cerrada el 2026-11-02). Las cookies de Forms, de la Session y `__AntiXsrfToken` (su `Secure` depende de `FormsAuthentication.RequireSSL`) viajan también por HTTP. Escenario: un enlace o marcador `http://` al sitio manda el ticket de login en claro por la red de la planta. | `Web.Release.config:22-29`; `AntiXsrfMasterPage.cs:37` | Plan directo: descomentar los dos `requireSSL` en la misma publicación que D-144. Antes, confirmar con TI que `http://` redirige a `https://`: sin eso nadie podría entrar por HTTP. | **Sí** (va con D-144) |
| AUD-73 | Medio | **Se borró el procedimiento de publicación de `docs/deploy.md`** (`32dfca6`: de 160 a 19 líneas). Se perdieron las secciones 0, 2, 3, 6 y 7: copiar `connectionStrings.config` después de cada publicación (el perfil borra la carpeta), entregarla por canal seguro, crear el login de mínimo privilegio, dar permiso de escritura a `App_Data\Logs` y conservarlo al republicar, y el `-Recreate`. Siguen apuntando a ellas `Web.config:7-9`, `Web.Release.config:22-23` ("docs/deploy.md → 5"), `AppLog.cs:10-11` y `docs/status.md:29` ("deploy.md, paso 6"). Escenario: en la 2.ª publicación se entrega una carpeta sin `connectionStrings.config` (sitio caído), o TI reemplaza la carpeta y pierde los logs y su permiso (`AppLog` deja de escribir sin avisar). | `docs/deploy.md` | Plan directo (docs): restaurar el procedimiento del dueño de la aplicación en un documento propio (ej. `docs/publicacion.md`) y dejar `deploy.md` para TI; corregir las referencias. | No (recomendado antes de republicar) |
| AUD-74 | Medio | **Clases muy grandes y métodos largos (SRP).** `GuidedTest.aspx.cs` tiene 1 903 líneas (1 407 en AUD-16), `PodService` 1 294, `GuideScriptBuilder` 1 064 y `DashboardService` 818. Métodos de más de 100 líneas: `GuideScriptBuilder.LogNC` (~133) y `GuidedTest.BuildStepViewModel` (~112). No es lógica de negocio filtrada a la UI (AUD-38 sigue cerrado): es coordinación y presentación acumuladas. Escenario: cada pantalla nueva del POD toca los mismos 3 archivos, lo que sube el riesgo de regresión y el costo de revisión. | `Pages/Training/GuidedTest.aspx.cs`, `BLL/Training/PodService.cs`, `Pages/Training/Guide/GuideScriptBuilder.cs`, `BLL/Reports/DashboardService.cs` | Spec o brief después de la publicación: partir `GuidedTest` por área de pantalla (POD, controles, Ingreso a MES), `PodService` por comando y `GuideScriptBuilder` por paso. Va con D-15 (inyectar repositorios en `PodService`). | No |
| AUD-75 | Bajo | **Las caídas en el login no quedan registradas.** `Login.aspx.cs` captura toda `InvalidOperationException` como "API de empleados caída". Una falla de BD que llega como `InvalidOperationException` (ej. el pool de conexiones agotado) muestra "El servicio de validación no está disponible" y, como queda capturada, `Application_Error` no la registra. Además `EmployeeRepository` reporta sus fallas con `Trace.TraceWarning`, que en IIS no va a ningún lado. Escenario: TI reporta que "nadie puede entrar" y `App_Data\Logs` no tiene nada. | `Pages/Auth/Login.aspx.cs:35`; `DAL/Repositories/EmployeeRepository.cs:58,88,114` | B-n (a asignar): una excepción propia para "API no disponible" y `AppLog.Write` en esas tres fallas. | No |
| AUD-76 | Bajo | **El largo mínimo de la contraseña solo vive en el markup.** `ChangePassword.aspx` exige 8 caracteres con un `RegularExpressionValidator`, pero `AuthService.ChangePassword` acepta cualquier contraseña distinta de la por defecto. Viola `CLAUDE.md` → "La BLL valida las reglas de negocio y protege sus invariantes sin confiar en que la página ya validó". Escenario: hoy ninguno (el validador corre también en el servidor); una segunda página de cambio de contraseña heredaría el hueco. | `Pages/Auth/ChangePassword.aspx:64-66`; `BLL/Auth/AuthService.cs:132-145` | Plan directo: la regla en `AuthService` (nuevo `ChangePasswordStatus`) y el validador queda solo como ayuda en el cliente. | No |
| AUD-77 | Bajo | **La página escribe la Session directamente.** `ChangePassword` hace `Session[IsFirstLogin] = false`, contra "las claves del usuario se escriben solo en `SessionHelper.SetUserSession`". Ya existe `SessionHelper.RefreshUserSession()`, que la recarga desde la BD. | `Pages/Auth/ChangePassword.aspx.cs:59` | Plan directo: `RefreshUserSession()`. | No |
| AUD-78 | Bajo | **Hora del servidor en vez de la de planta.** El banner de Line Clearance ("last collected") usa `DateTime.Now`; el resto de la app usa `PlantTime`. Escenario: si el servidor de IIS no está en la zona horaria de Costa Rica, el operador ve otra hora que la del reloj de la estación. | `UI/Controls/Training/LineClearanceControl.ascx.cs:71` | Plan directo: `PlantTime.ToLocal(DateTime.UtcNow)`. | No |
| AUD-79 | Info | **Docs y scripts desalineados con el código.** `docs/status.md:49` todavía lista B-20 como abierto, pero ya está corregido (`TrainingAccessRule.EnsureCanStart`, spec 021 E2). `RunAllScripts-PROD-DB.ps1:2` dice "00–09" y corre del 00 al 12. Ese script duplica `RunAllScripts.ps1` salvo el servidor por defecto, que versiona el nombre interno del clúster SQL de producción (`:18`). `TrainingApp.Test/Class1.cs` contiene `StepValidatorTests`. | Los archivos citados | Plan directo (limpieza): actualizar `status.md`, un solo `RunAllScripts.ps1` con `-Server` obligatorio y renombrar `Class1.cs`. | No |
| AUD-80 | Info | **Falta HSTS.** Con HTTPS confirmado no hay `Strict-Transport-Security`; CSP sigue fuera por decisión (Fase 9, "No incluye"). | `Web.config:50-58` / `Web.Release.config` | Plan directo, junto con AUD-72, solo en Release y después de confirmar la redirección HTTP → HTTPS. | No |

**Conteo:** Alto 1 · Medio 3 · Bajo 4 · Info 2. **Bloquean la publicación:** AUD-71 y AUD-72
(más D-144, ya conocido).

> **Enmienda 2026-11-03: AUD-74 ☑.** Cerrado en la Fase 13 (`docs/specs/aud-74-plan.md`, D-258 y
> D-259): `DashboardService` (818 líneas) quedó como fachada con un colaborador por reporte y sus DTOs
> aparte; `GuideScriptBuilder` (1 064), en parciales por paso y un kit común, con `LogNC` y
> `DataCollection` partidos por pantalla; `PodService` (1 294), como fachada sobre 10 colaboradores de
> `BLL/Training/Pod/`, con sus dependencias inyectadas por interfaces (D-15) y 37 tests del núcleo;
> `GuidedTest.aspx.cs` (1 903), en parciales por área (~200 líneas en el archivo principal) y helpers
> puros. Sin cambio de comportamiento, de markup ni de firmas públicas. Queda a la vista que
> `GuidedTest.BuildStepViewModel` sigue siendo un método largo (~107 líneas): el plan no lo partía
> (no se extraen métodos que tocan controles).

## Sugerencias (sin número)

- `detect_antipatterns` de Roslyn está pensado para .NET moderno. Sus avisos de `DateTime.UtcNow`
  (sugiere `TimeProvider`, que no existe en 4.7.2), de `new HttpClient()` y de
  `GetAwaiter().GetResult()` en `EmployeeRepository` no aplican: el `HttpClient` es estático y
  compartido, y el bloqueo usa `ConfigureAwait(false)`, que evita el deadlock de Web Forms. Los
  `catch` vacíos de `Global.Application_Error` y `AppLog.Write` son intencionales y están
  comentados.
- `Web.config` no fija `machineKey`. Con un solo servidor y el reciclado diario de las 03:00,
  basta si el App Pool carga el perfil de usuario (las claves autogeneradas persisten). Si
  algún día hay más de un servidor, hay que fijarla.
- SweetAlert2 11.7.23 incluye el comportamiento de protesta que se activa en dominios `.ru`,
  `.by` y `.рф`. No afecta a este sitio; es un motivo más para la actualización diferida en
  D-131.

## Falsos positivos, no volver a reportar

Además de los de las auditorías anteriores:
- Los handlers que `find_dead_code` marca como sin uso: están enlazados en el markup.
- `data-confirm='<%# Eval(…) %>'` sin `:` en `asp:LinkButton`: el control ya codifica el
  atributo (`CLAUDE.md` → "Sesión y seguridad").
- Los mensajes distintos para Code1 inexistente y contraseña incorrecta, y el reclamo de
  cuenta ajena: riesgo aceptado (AUD-32, D-18, D-20).
- La guía de Redefinity con datos aproximados ofrecida en producción (D-178).
