# Instalación en el Servidor — MES TrainingApp

## Requisitos

- **Conectividad HTTPS (puerto 443)** hacia `https://miappcr.com` para autenticación y registro de usuarios.
- App Pool configurado con:
  - **.NET CLR v4.0**
  - **Pipeline: Integrated**
  - **idleTimeout = 0** (evita la pérdida de sesiones activas).
  - **startMode = AlwaysRunning**
- Sitio IIS con **preloadEnabled = true** para mejorar el tiempo de carga inicial.
- Configurar el reciclado del App Pool en una **hora fija fuera de turno** (ej. 03:00) y deshabilitar el reciclado predeterminado cada 29 horas.

## Consideraciones para TI

Antes de publicar, informar al responsable de la aplicación:

- Si el sitio cuenta (o contará) con **HTTPS**, ya que las cookies seguras requieren esta configuración.
- La **versión de IIS** utilizada. En **IIS 10 o superior**, la publicación puede ocultar la cabecera `Server`.