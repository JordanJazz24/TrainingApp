# Instalación en el servidor — MES Training Simulator

Instrucciones para **TI** (Fase 9, D-133). TI administra el servidor: crea la base de datos,
corre los scripts, configura IIS y da la cadena de conexión. El dueño de la aplicación entrega
tres cosas:

- la **carpeta publicada** (sitio web ya compilado);
- la carpeta **`Database/`** (scripts SQL y utilidades);
- este documento.

La aplicación es un sitio ASP.NET Web Forms (.NET Framework 4.7.2) sobre SQL Server. Solo
accede a la base de datos mediante stored procedures.

## 1. Requisitos

- **IIS** con **ASP.NET 4.7.2** (o superior de la familia 4.x) y el feature
  **Application Initialization** (`Web-AppInit`).
- **SQL Server** (cualquier edición soportada; la base de datos es pequeña).
- **Salida HTTPS (puerto 443) desde el servidor web a `https://miappcr.com`**: es la API de
  empleados. Sin ella nadie puede registrarse ni iniciar sesión.
- Para correr los scripts con la utilidad incluida: `sqlcmd` y PowerShell 5.1. Alternativa:
  SQL Server Management Studio.

## 2. Base de datos

1. Crear una **base de datos vacía** (ej. `TrainingApp`).
2. Correr los scripts de `Database/StoredProcedures/` **en orden numérico, del `00` al `09`,
   todos** (incluido `05`). Opciones:
   - con la utilidad incluida, desde la carpeta que contiene `Database/`:
     ```powershell
     .\Database\Ops\RunAllScripts.ps1 -Server "SERVIDOR\INSTANCIA" -Database TrainingApp
     # con un login SQL en vez de autenticación de Windows (pide la contraseña):
     .\Database\Ops\RunAllScripts.ps1 -Server "SERVIDOR\INSTANCIA" -Database TrainingApp -User USUARIO
     ```
     Se detiene en el primer script que falle y muestra el error. Se puede volver a correr.
   - o abrir cada archivo en SSMS, sobre la base de datos nueva, y ejecutarlo en orden.
3. **Login de la aplicación**, de mínimo privilegio: solo ejecutar stored procedures, sin
   acceso directo a tablas. Ejemplo con un login SQL (o usar la cuenta del App Pool con
   autenticación de Windows):
   ```sql
   CREATE LOGIN TrainingAppUser WITH PASSWORD = '<contraseña segura>';
   USE TrainingApp;
   CREATE USER TrainingAppUser FOR LOGIN TrainingAppUser;
   GRANT EXECUTE ON SCHEMA::dbo TO TrainingAppUser;
   ```
4. **Primer administrador:** abrir `Database/Ops/CreateAdmin.sql`, poner en `@Code1` y
   `@Name` el código de empleado y el nombre que indique el dueño de la aplicación, y
   ejecutarlo sobre la base de datos. La contraseña inicial la conoce el dueño de la
   aplicación; el sistema obliga a cambiarla en el primer ingreso.

## 3. Sitio web

1. Copiar la **carpeta publicada** al servidor (ej. `D:\Sites\TrainingApp`) y crear el sitio
   o la aplicación en IIS apuntando a ella.
2. Crear el archivo **`connectionStrings.config`** en esa carpeta, **junto a `Web.config`**.
   El nombre de la cadena tiene que ser `Default`:
   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <connectionStrings>
   	<add name="Default"
   		 connectionString="Server=SERVIDOR\INSTANCIA;Database=TrainingApp;User Id=TrainingAppUser;Password=CONTRASEÑA;"
   		 providerName="System.Data.SqlClient" />
   </connectionStrings>
   ```
   Con autenticación de Windows: `Server=…;Database=TrainingApp;Integrated Security=True;`
   (la identidad del App Pool necesita el usuario y el `GRANT EXECUTE` del paso 2.3).
   Este archivo no viene en la carpeta publicada y no se versiona: solo existe en el servidor.
3. Dar al dueño de la aplicación la cadena de conexión que corresponda (D-133), sin la
   contraseña si se usa un login SQL.
4. **Permiso de escritura para el log.** La aplicación registra los errores, los inicios de
   sesión fallidos y los bloqueos de cuenta en un archivo por día en `App_Data\Logs` (sin
   contraseñas). Crear la carpeta `App_Data` en la carpeta del sitio, si no viene, y darle
   permiso de **modificación** a la identidad del App Pool (ej. `IIS AppPool\TrainingApp`):
   ```bat
   icacls "D:\Sites\TrainingApp\App_Data" /grant "IIS AppPool\TrainingApp:(OI)(CI)M"
   ```
   Sin ese permiso la aplicación funciona igual, pero no deja registro. La aplicación borra
   sola los archivos de más de 60 días. IIS no sirve el contenido de `App_Data`: el log solo
   se lee desde el servidor.

## 4. Application Pool

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

## 5. HTTPS

Si el sitio tiene (o va a tener) certificado HTTPS, **avisar al dueño de la aplicación**: la
siguiente publicación activa las cookies seguras, que solo funcionan con HTTPS.

## 6. Republicar (versiones nuevas)

1. Reemplazar el contenido de la carpeta del sitio por la carpeta publicada nueva,
   **conservando `connectionStrings.config` y `App_Data\Logs`** (respaldarlos antes y volver
   a ponerlos si se borra la carpeta completa; el permiso del paso 3.4 también se vuelve a
   dar).
2. **Reciclar el App Pool.**

## 7. Recrear la base de datos (D-130)

Mientras dure el piloto, los datos se pueden descartar. Si el dueño de la aplicación lo pide
(por ejemplo, porque cambió la estructura de la base de datos):

```powershell
.\Database\Ops\RunAllScripts.ps1 -Server "SERVIDOR\INSTANCIA" -Database TrainingApp -Recreate
```

`-Recreate` pide confirmación, **borra la base de datos completa** (usuarios, sesiones e
intentos) y la vuelve a crear con los scripts. Después:

- volver a crear el usuario de la base de datos y el `GRANT EXECUTE` (paso 2.3; el login del
  servidor se conserva);
- volver a crear el primer administrador con `CreateAdmin.sql` (paso 2.4);
- reciclar el App Pool.
