# Publicar y republicar — dueño de la aplicación

Procedimiento de quien publica la aplicación (el usuario). Lo que hace **TI** (IIS, App Pool,
HTTPS) está en `docs/deploy.md`. Restaurado de `docs/deploy.md` antes del commit `32dfca6` y
puesto al día (AUD-73, Fase 9 E5).

| Quién | Qué |
|---|---|
| Dueño de la aplicación | Prepara la base de datos (sección 2), publica y entrega la **carpeta publicada con su `connectionStrings.config` adentro** (sección 1). |
| TI | Instala el sitio, configura IIS y el App Pool, da el permiso del log y, al republicar, conserva lo que indica la sección 4 (`docs/deploy.md`). |

## 1. Antes de entregar

1. **Carpeta real al día y compilada:** `TrainingApp` integrado, Rebuild sin errores y tests en
   verde (`CLAUDE.md` → "Correr local, compilar y probar").
2. **Base de datos al día:** correr los scripts nuevos o cambiados (sección 2). Cuáles faltan en la
   BD del servidor: `docs/status.md` → "Pendiente del usuario".
3. **Publicar** con el perfil `MVP` (Release, salida en `publish\MVP`; las instrucciones están en
   `TrainingApp/Properties/PublishProfiles/MVP.pubxml`). El perfil **borra la carpeta antes de
   publicar**, así que el `connectionStrings.config` se copia después, **en cada publicación**:
   ```powershell
   Copy-Item "TrainingApp\connectionStrings.config" "publish\MVP\"
   ```
4. **Revisar la carpeta publicada:**
   - trae `connectionStrings.config` con la cadena del servidor de producción (nombre `Default`,
     login de mínimo privilegio, sección 2.3; nunca `sa`);
   - no trae archivos `.cs`;
   - `web.config` no tiene `debug="true"`, y sí `customErrors mode="RemoteOnly"`,
     `configSource="connectionStrings.config"` y `removeServerHeader="true"` (D-144);
   - `requireSSL` y `Strict-Transport-Security` **solo si ya están descomentados** en
     `Web.Release.config` (sección 5). Con `requireSSL` y sin HTTPS nadie puede entrar.
5. **Entregar la carpeta por un canal seguro** (carpeta compartida con acceso restringido o
   directo a TI): lleva la contraseña de la base de datos en texto plano. No dejar copias en
   correos ni en carpetas abiertas. La carpeta `publish\` no se versiona (`.gitignore`).

## 2. Base de datos

1. Crear una **base de datos vacía** (ej. `TrainingApp`), si es una instalación nueva.
2. Correr los scripts de `Database/StoredProcedures/` **en orden numérico, del `00` al `12`,
   todos**. Con la utilidad incluida, desde la raíz del repo (requiere `sqlcmd` y PowerShell 5.1);
   `-Server` es obligatorio:
   ```powershell
   .\Database\Ops\RunAllScripts.ps1 -Server "SERVIDOR\INSTANCIA" -Database TrainingApp
   # con un login SQL en vez de autenticación de Windows (pide la contraseña):
   .\Database\Ops\RunAllScripts.ps1 -Server "SERVIDOR\INSTANCIA" -Database TrainingApp -User USUARIO
   ```
   Se detiene en el primer script que falle y muestra el error. Todos se pueden volver a correr
   sobre una base existente sin perder datos. O abrir cada archivo en SSMS, sobre la base de
   datos, y ejecutarlo en orden.
3. **Login de la aplicación (mínimo privilegio, AUD-71, D-8).** La aplicación solo necesita
   ejecutar stored procedures; **no usar `sa`** ni el login con el que se corren los scripts:
   ```sql
   CREATE LOGIN TrainingAppUser WITH PASSWORD = '<contraseña segura>';
   USE TrainingApp;
   CREATE USER TrainingAppUser FOR LOGIN TrainingAppUser;
   GRANT EXECUTE ON SCHEMA::dbo TO TrainingAppUser;
   ```
   Con un login de más permisos, un error o un abuso de la aplicación los tendría sobre la base
   de datos. Si se rota la contraseña de `sa`, cambiarla en toda instancia que la use (incluida
   la local).
4. **Primer administrador:** abrir `Database/Ops/CreateAdmin.sql`, poner en `@Code1` y `@Name`
   el código de empleado y el nombre, y ejecutarlo sobre la base de datos. Entra con la
   contraseña por defecto y el sistema obliga a cambiarla en el primer ingreso.

## 3. `connectionStrings.config`

Viene en la carpeta publicada, junto a `web.config` (sección 1.3): TI no lo crea ni lo modifica.
IIS no sirve archivos `.config`, así que no se descarga desde el navegador. La plantilla es
`TrainingApp/connectionStrings.config.example`; el nombre de la cadena tiene que ser `Default`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<connectionStrings>
	<add name="Default"
		 connectionString="Server=SERVIDOR\INSTANCIA;Database=TrainingApp;User Id=TrainingAppUser;Password=CONTRASEÑA;"
		 providerName="System.Data.SqlClient" />
</connectionStrings>
```

Con autenticación de Windows: `Server=…;Database=TrainingApp;Integrated Security=True;` (la
identidad del App Pool necesita su usuario en la base de datos y el `GRANT EXECUTE` del paso 2.3).

## 4. Republicar (versiones nuevas)

1. El dueño de la aplicación corre en la base de datos los scripts nuevos o cambiados, si la
   versión los trae, y entrega la carpeta nueva **con su `connectionStrings.config`** (sección 1).
2. TI reemplaza el contenido de la carpeta del sitio por la carpeta nueva, **conservando
   `App_Data\Logs`** (respaldarla antes y volver a ponerla si se borra la carpeta completa).
3. **Permiso del log:** la aplicación escribe en `App_Data\Logs` (errores, logins fallidos,
   bloqueos y fallas de la API de empleados; un archivo por día, sin contraseñas; borra solos los
   de más de 60 días). La identidad del App Pool necesita permiso de **modificación**; si la
   carpeta se reemplazó, volver a darlo:
   ```bat
   icacls "D:\Sites\TrainingApp\App_Data" /grant "IIS AppPool\TrainingApp:(OI)(CI)M"
   ```
   Sin ese permiso la aplicación funciona igual, pero **no deja registro y no avisa** (`AppLog`).
   IIS no sirve el contenido de `App_Data`: el log solo se lee desde el servidor.
4. **Reciclar el App Pool.**

## 5. HTTPS: `requireSSL` y HSTS (D-257)

El sitio corre hoy por `http://`. `requireSSL` (cookies de sesión y de login, AUD-72) y
`Strict-Transport-Security` (AUD-80) están **comentados** en `TrainingApp/Web.Release.config`.
Se descomentan juntos en la publicación en que se cumplan las dos cosas: TI instaló el
certificado y `http://` **redirige** a `https://` (probarlo en el navegador). Si se publican sin
HTTPS, el navegador no envía las cookies y nadie puede iniciar sesión. HSTS empieza con
`max-age` corto, sin `includeSubDomains` ni `preload` (el dominio es de TI).

## 6. Recrear la base de datos (D-130)

Mientras dure el piloto, los datos se pueden descartar. Si hace falta (por ejemplo, porque
cambió la estructura):

```powershell
.\Database\Ops\RunAllScripts.ps1 -Server "SERVIDOR\INSTANCIA" -Database TrainingApp -User USUARIO -Recreate
```

`-Recreate` pide confirmación, **borra la base de datos completa** (usuarios, sesiones e
intentos) y la vuelve a crear con los scripts. Requiere permiso para borrar y crear la base de
datos. Después:

- volver a crear el usuario de la base de datos del login de la aplicación (paso 2.3; el login
  del servidor se conserva);
- volver a crear el primer administrador con `CreateAdmin.sql` (paso 2.4);
- pedirle a TI que recicle el App Pool.
