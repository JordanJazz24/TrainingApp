# ============================================================
# RunAllScripts.ps1 — ejecuta Database/StoredProcedures/00–09 en orden
# numérico sobre una BD (docs/process.md → "Base de datos").
#
# Uso (desde la raíz del repo, en PowerShell):
#   .\Database\Ops\RunAllScripts.ps1                       # BD existente (re-ejecutable)
#   .\Database\Ops\RunAllScripts.ps1 -Recreate             # borra y crea la BD antes (D-124)
#   .\Database\Ops\RunAllScripts.ps1 -User sa              # login SQL: pide la contraseña
#   .\Database\Ops\RunAllScripts.ps1 -Server "(localdb)\MSSQLLocalDB" -Database Prueba -Recreate
#
# Sin -User usa la autenticación de Windows. La contraseña no se pasa por la línea de
# comandos: va a sqlcmd por la variable SQLCMDPASSWORD solo mientras corre el script.
# -Recreate pide confirmación y borra todo: sesiones, intentos y usuarios (después hay que
# volver a crear el admin con CreateAdmin.sql). Se detiene en el primer script que falle;
# cada script corre en su transacción, así que el que falla no deja nada a medias.
# Requiere: sqlcmd en el PATH.
# ============================================================
param(
    [string]$Server   = "localhost\SQLEXPRESS",
    [string]$Database = "TrainingApp",
    [string]$User,
    [switch]$Recreate
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
    throw "No se encontró sqlcmd en el PATH."
}

$scriptsDir = Join-Path $PSScriptRoot "..\StoredProcedures" | Resolve-Path
$scripts    = Get-ChildItem $scriptsDir -Filter "*.sql" |
              Where-Object { $_.Name -match '^\d{2}_' } |
              Sort-Object Name
if (-not $scripts) { throw "No hay scripts NN_*.sql en $scriptsDir." }

$auth = @("-E")
if ($User) {
    $secure = Read-Host "Contraseña de $User" -AsSecureString
    $env:SQLCMDPASSWORD = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure))
    $auth = @("-U", $User)
}

# sqlcmd escribe los errores de SQL Server en la salida estándar: se devuelve para mostrarla.
function Run-Sqlcmd([string[]]$extra) {
    $output = & sqlcmd -S $Server @auth -I -b -f 65001 @extra
    if ($LASTEXITCODE -ne 0) { $output | Write-Host; throw "sqlcmd terminó con código $LASTEXITCODE." }
}

try {
    if ($Recreate) {
        $answer = Read-Host "Se borra y se crea [$Database] en $Server (se pierden sesiones, intentos y usuarios). Escribe el nombre de la BD para confirmar"
        if ($answer -ne $Database) { Write-Host "Cancelado."; return }

        Run-Sqlcmd @("-d", "master", "-Q",
            "IF DB_ID(N'$Database') IS NOT NULL BEGIN ALTER DATABASE [$Database] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$Database]; END; CREATE DATABASE [$Database];")
        Write-Host "BD [$Database] creada."
    }

    foreach ($script in $scripts) {
        Write-Host ("{0,-32}" -f $script.Name) -NoNewline
        try {
            Run-Sqlcmd @("-d", $Database, "-i", $script.FullName)
            Write-Host "OK" -ForegroundColor Green
        }
        catch {
            Write-Host "FALLÓ" -ForegroundColor Red
            throw "Se detuvo en $($script.Name): los scripts siguientes no se ejecutaron."
        }
    }
    Write-Host "Listo: $($scripts.Count) scripts ejecutados en [$Database]."
    if ($Recreate) { Write-Host "Falta el primer admin: Database\Ops\CreateAdmin.sql." }
}
finally {
    Remove-Item Env:\SQLCMDPASSWORD -ErrorAction SilentlyContinue
}
