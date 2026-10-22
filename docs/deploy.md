# Instalación en el servidor — MES Training Simulator

Instrucciones para **TI** y para el **dueño de la aplicación** (Fase 9). La aplicación es un
sitio ASP.NET Web Forms (.NET Framework 4.7.2) sobre SQL Server. Solo accede a la base de datos
mediante stored procedures.

**Quién hace qué (usuario, 2026-10-22; reemplaza el reparto original de D-133):**

| Quién | Qué |
|---|---|
| Dueño de la aplicación | Prepara la base de datos (scripts y primer administrador, sección 2), publica y entrega la **carpeta publicada con su `connectionStrings.config` adentro** (sección 0). |
| TI | Instala el sitio, configura IIS y el App Pool, da el permiso del log y, al republicar, conserva lo que indica la sección 6. |

## 0. Antes de entregar (dueño de la aplicación)

1. **Carpeta real al día y compilada:** `TrainingApp` integrado, Rebuild sin errores.
2. **Base de datos:** con los scripts `00`–`10` y el primer administrador (sección 2). Si la
   versión trae scripts nuevos o cambiados, correrlos antes de entregar.
3. **Publicar** con el perfil `MVP` (Release, en `publish\MVP`). El perfil **borra la carpeta
   antes de publicar**, así que el `connectionStrings.config` se copia después, **en cada
   publicación**:
   ```powershell
   Copy-Item "TrainingApp\connectionStrings.config" "publish\MVP\"
   ```
4. **Revisar la carpeta:** trae `connectionStrings.config` (con la cadena del servidor de
   producción, nombre `Default`), no trae archivos `.cs`, y `web.config` no tiene
   `debug="true"` y sí tiene `customErrors` y `configSource="connectionStrings.config"`.
5. **Entregar la carpeta por un canal seguro** (carpeta compartida con acceso restringido o
   directo a TI): lleva la contraseña de la base de datos en texto plano. No dejar copias en
   correos ni en carpetas abiertas. La carpeta `publish\` no se versiona (`.gitignore`).

## 1. Requisitos

- **IIS** con **ASP.NET 4.7.2** (o superior de la familia 4.x) y el feature
  **Application Initialization** (`Web-AppInit`).
- **SQL Server** alcanzable desde el servidor web con la cadena de `connectionStrings.config`.
- **Salida HTTPS (puerto 443) desde el servidor web a `https://miappcr.com`**: es la API de
  empleados. Sin ella nadie puede registrarse ni iniciar sesión.

## 2. Base de datos (dueño de la aplicación)

**Estado del MVP (2026-10-22):** la base de datos de producción ya está preparada con los
scripts `00`–`10`. Falta el primer administrador (paso 4). Esta sección queda para una
instalación nueva, para correr los scripts de una versión nueva o para recrearla (sección 7).

1. Crear una **base de datos vacía** (ej. `TrainingApp`).
2. Correr los scripts de `Database/StoredProcedures/` **en orden numérico, del `00` al `10`,
   todos** (incluido `05`). Opciones:
   - con la utilidad incluida, desde la carpeta que contiene `Database/` (requiere `sqlcmd` y
     PowerShell 5.1):
     ```powershell
     .\Database\Ops\RunAllScripts.ps1 -Server "SERVIDOR\INSTANCIA" -Database TrainingApp
     # con un login SQL en vez de autenticación de Windows (pide la contraseña):
     .\Database\Ops\RunAllScripts.ps1 -Server "SERVIDOR\INSTANCIA" -Database TrainingApp -User USUARIO
     ```
     Se detiene en el primer script que falle y muestra el error. Todos se pueden volver a
     correr sobre una base existente sin perder datos.
   - o abrir cada archivo en SSMS, sobre la base de datos, y ejecutarlo en orden.
3. **Login de la aplicación.** Lo recomendado es uno de mínimo privilegio, que solo ejecute
   stored procedures (la aplicación no necesita más), distinto del login con el que se corren
   los scripts:
   ```sql
   CREATE LOGIN TrainingAppUser WITH PASSWORD = '<contraseña segura>';
   USE TrainingApp;
   CREATE USER TrainingAppUser FOR LOGIN TrainingAppUser;
   GRANT EXECUTE ON SCHEMA::dbo TO TrainingAppUser;
   ```
   Si la aplicación usa un login con más permisos (ej. el que corre los scripts), funciona igual,
   pero un error o un abuso de la aplicación tendría esos permisos sobre la base de datos.
4. **Primer administrador:** abrir `Database/Ops/CreateAdmin.sql`, poner en `@Code1` y `@Name`
   el código de empleado y el nombre del administrador, y ejecutarlo sobre la base de datos.
   Entra con la contraseña por defecto y el sistema obliga a cambiarla en el primer ingreso.

## 3. Sitio web (TI)

1. Copiar la **carpeta publicada** al servidor (ej. `D:\Sites\TrainingApp`) y crear el sitio
   o la aplicación en IIS apuntando a ella.
2. **`connectionStrings.config` viene en la carpeta**, junto a `web.config`: no hace falta
   crearlo ni modificarlo. IIS no sirve archivos `.config`, así que no se puede descargar desde
   el navegador. Si hubiera que crearlo de nuevo, esta es la plantilla (el nombre de la cadena
   tiene que ser `Default`):
   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <connectionStrings>
   	<add name="Default"
   		 connectionString="Server=SERVIDOR\INSTANCIA;Database=TrainingApp;User Id=USUARIO;Password=CONTRASEÑA;"
   		 providerName="System.Data.SqlClient" />
   </connectionStrings>
   ```
   Con autenticación de Windows: `Server=…;Database=TrainingApp;Integrated Security=True;`
   (la identidad del App Pool necesita su usuario en la base de datos y el `GRANT EXECUTE` del
   paso 2.3).
3. **Permiso de escritura para el log.** La aplicación registra los errores, los inicios de
   sesión fallidos y los bloqueos de cuenta en un archivo por día en `App_Data\Logs` (sin
   contraseñas). Crear la carpeta `App_Data` en la carpeta del sitio, si no viene, y darle
   permiso de **modificación** a la identidad del App Pool (ej. `IIS AppPool\TrainingApp`):
   ```bat
   icacls "D:\Sites\TrainingApp\App_Data" /grant "IIS AppPool\TrainingApp:(OI)(CI)M"
   ```
   Sin ese permiso la aplicación funciona igual, pero no deja registro. La aplicación borra
   sola los archivos de más de 60 días. IIS no sirve el contenido de `App_Data`: el log solo
   se lee desde el servidor.

## 4. Application Pool (TI)

- **.NET CLR v4.0**, pipeline **Integrated**.
- **`idleTimeout` = 0 (obligatorio).** La aplicación guarda el avance del entrenamiento en
  memoria (Session InProc). Si el proceso se detiene por inactividad, el operador pierde la
  sesión y sale de su entrenamiento a la mitad.
- **`startMode="AlwaysRunning"`** en el App Pool y **`preloadEnabled="true"`** en el sitio,
  para que el primer usuario no espere el arranque.
- **Reciclado periódico a una hora fija fuera de turno** (ej. 03:00), en lugar del reciclado
  cada 29 horas que trae IIS por defecto: un reciclado también borra las sesiones en curso.

Ejemplo con `appcmd` (ajustar los nombres del App Pool y del sitio):

```bat
%windir%\system32\inetsrv\appcmd set apppool "TrainingApp" /managedRuntimeVersion:v4.0 /managedPipelineMode:Integrated
%windir%\system32\inetsrv\appcmd set apppool "TrainingApp" /processModel.idleTimeout:00:00:00
%windir%\system32\inetsrv\appcmd set apppool "TrainingApp" /startMode:AlwaysRunning
%windir%\system32\inetsrv\appcmd set apppool "TrainingApp" /recycling.periodicRestart.time:00:00:00
%windir%\system32\inetsrv\appcmd set apppool "TrainingApp" /+recycling.periodicRestart.schedule.[value='03:00:00']
%windir%\system32\inetsrv\appcmd set app "Default Web Site/TrainingApp" /preloadEnabled:true
```

## 5. HTTPS y versión de IIS (TI)

Avisar al dueño de la aplicación:
- si el sitio tiene (o va a tener) **certificado HTTPS**: la siguiente publicación activa las
  cookies seguras, que solo funcionan con HTTPS;
- la **versión de IIS**: con IIS 10 o superior, la siguiente publicación también quita la
  cabecera `Server` (D-144).

## 6. Republicar (versiones nuevas)

1. El dueño de la aplicación corre en la base de datos los scripts nuevos o cambiados, si la
   versión los trae, y entrega la carpeta nueva **con su `connectionStrings.config`**
   (sección 0).
2. TI reemplaza el contenido de la carpeta del sitio por la carpeta nueva, **conservando
   `App_Data\Logs`** (respaldarla antes y volver a ponerla si se borra la carpeta completa; el
   permiso del paso 3.3 también se vuelve a dar).
3. **Reciclar el App Pool.**

## 7. Recrear la base de datos (D-130, dueño de la aplicación)

Mientras dure el piloto, los datos se pueden descartar. Si hace falta (por ejemplo, porque
cambió la estructura de la base de datos):

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
