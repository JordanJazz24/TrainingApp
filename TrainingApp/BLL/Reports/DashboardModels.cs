using System;
using System.Collections.Generic;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Reports
{
    // Filtros de la lista de Operadores (spec 018, AC-13). Los valida el servicio: lo que llega
    // de la URL puede venir manipulado.
    public class OperatorFilter
    {
        public string Text { get; set; }

        // null = todos los estados. Se evalúa en las guías de su área (spec 024, D-263).
        public ReadinessState? State { get; set; }

        // Área del operador (spec 024, D-262): null = todas; WithoutArea = los que no la eligieron
        // (y entonces AreaId no cuenta). Sin filtro de guía (D-264).
        public int? AreaId { get; set; }
        public bool WithoutArea { get; set; }

        // Desde 1.
        public int Page { get; set; } = 1;
    }

    // Un operador en la lista: su estado en las guías de su área (spec 024, D-261) y su última
    // actividad. Las guías de otras áreas no salen en la lista (D-266).
    public class OperatorRow
    {
        public int UserId { get; set; }
        public string Code1 { get; set; }
        public string Name { get; set; }

        // "Producto · Área" o "Sin área" (spec 021, P-9).
        public string AreaText { get; set; }
        public OperatorAreaStatus AreaStatus { get; set; }

        // Una por guía publicada de su área (hoy una, D-268); vacía sin área o sin guía publicada.
        public IList<GuideReadiness> AreaGuides { get; set; }

        // Sin iniciar (D-263): ninguna sesión en las guías de su área (OperatorArea).
        public bool NotStarted => OperatorArea.IsNotStarted(AreaStatus, AreaGuides);

        // Inicio de su sesión más reciente en cualquier guía publicada (AC-13); null si nunca
        // entrenó.
        public DateTime? LastActivity { get; set; }

        // Días completos desde LastActivity (null sin actividad) y si lleva OperatorSummary.InactiveDays
        // o más sin practicar teniendo guía en su área y no siendo Sin iniciar (spec 022, brief 6;
        // métrica derivada). Los pone OperatorSummary.MarkActivity.
        public int? DaysSinceActivity { get; set; }
        public bool Inactive { get; set; }
    }

    public class OperatorPage
    {
        // Los filtros que se aplicaron, ya validados: la página los muestra y arma sus enlaces
        // con ellos (Page queda abajo).
        public OperatorFilter Applied { get; set; }

        // Las opciones del filtro Área: las que tienen guía publicada, por producto (D-262).
        public IList<ReportArea> Areas { get; set; }
        public IList<OperatorRow> Rows { get; set; }
        public int Total { get; set; }
        public int Page { get; set; }       // desde 1
        public int PageCount { get; set; }

        // Operadores por estado y sin actividad reciente (spec 022, brief 6; métricas derivadas).
        public OperatorSummary Summary { get; set; }
    }

    // Una sesión en el detalle del Operador (AC-14).
    public class OperatorSessionRow
    {
        public DashboardSession Session { get; set; }

        // Null si sigue en curso ("—").
        public TimeSpan? Duration { get; set; }
        public int FailedAttempts { get; set; }

        // Null si no se muestra (DashboardQueryKit.VisibleScore).
        public decimal? Score { get; set; }

        // "Producto · Área" de la guía de la sesión (spec 024, D-269).
        public string AreaText { get; set; }

        // Un Assessment completado con el puntaje mínimo (ReadinessPolicy.IsPassed).
        public bool Passed { get; set; }
    }

    // Los pasos a reforzar de una guía.
    public class GuideReinforcement
    {
        public int TrainingGuideId { get; set; }
        public IList<ReinforcementStep> Steps { get; set; }
    }

    // Detalle de un Operador (spec 018, AC-14).
    public class OperatorDetail
    {
        public int UserId { get; set; }
        public string Code1 { get; set; }
        public string Name { get; set; }

        // "Producto · Área" o "Sin área" (spec 021).
        public string AreaText { get; set; }

        // Todas las guías publicadas: los nombres de las de abajo salen de aquí.
        public IList<DashboardGuide> Guides { get; set; }

        // Su área y las otras (spec 024, D-269; OperatorArea): las guías de su área, también Sin
        // iniciar, y las de otras áreas con sesiones (entrenamiento cruzado).
        public OperatorAreaStatus AreaStatus { get; set; }
        public IList<GuideReadiness> AreaGuides { get; set; }
        public IList<GuideReadiness> OtherGuides { get; set; }

        // De la más reciente a la más vieja, de todas las áreas.
        public IList<OperatorSessionRow> Sessions { get; set; }

        // Solo las guías con algún paso a reforzar: primero las de su área y después las otras
        // (D-269), cada grupo en el orden de Guides.
        public IList<GuideReinforcement> Reinforcement { get; set; }

        // La curva de aprendizaje de cada guía que entrenó (AC-15), con el mismo orden.
        public IList<GuideCurve> Curves { get; set; }

        // Sesiones por modo y el historial de cada guía (spec 022, brief 7): métricas derivadas.
        public OperatorActivity Activity { get; set; }
    }

    // Filtros de Pasos (spec 018 → Filtros; el período, spec 023). Los valida el servicio, como
    // OperatorFilter.
    public class StepFilter
    {
        public ReportPeriod Period { get; set; } = ReportPeriods.Default;

        // Inicio y Fin (días de la planta), solo con Personalizado.
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }

        // null = todas las guías.
        public int? GuideId { get; set; }

        // Assessment (al entrar) o Práctica libre; Guided no se ofrece.
        public SessionMode Mode { get; set; } = SessionMode.Assessment;
    }

    // El ranking de pasos de una guía, o el resumen de todas.
    public class GuideStepRanking
    {
        // null = todas las guías, con el mismo paso de cada una en un renglón.
        public int? TrainingGuideId { get; set; }
        public IList<StepStats> Steps { get; set; }
    }

    // Pasos (spec 018, AC-17 y AC-18).
    public class StepReport
    {
        // Los filtros que se aplicaron, ya validados.
        public StepFilter Applied { get; set; }

        // El período aplicado, su anterior y si se pidió uno inválido (el aviso).
        public ReportRange Range { get; set; }

        public IList<DashboardGuide> Guides { get; set; }

        // Con una guía, su ranking; con todas, un solo resumen (TrainingGuideId null). Vacío si
        // no hay intentos en el período.
        public IList<GuideStepRanking> Rankings { get; set; }

        // Los saltos solo existen en el modo que permite saltar (Práctica libre, AC-18).
        public bool ShowsSkips { get; set; }
    }

    // Filtros del Resumen (spec 018 → Filtros; el período, spec 023). Los valida el servicio.
    public class SummaryFilter
    {
        public ReportPeriod Period { get; set; } = ReportPeriods.Default;

        // Inicio y Fin (días de la planta), solo con Personalizado.
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }

        // null = todas las guías.
        public int? GuideId { get; set; }
    }

    // Resumen (spec 018, AC-9 a AC-12), el inicio del administrador.
    public class SummaryReport
    {
        // Los filtros que se aplicaron, ya validados.
        public SummaryFilter Applied { get; set; }

        // El período aplicado, su anterior y si se pidió uno inválido (el aviso).
        public ReportRange Range { get; set; }

        // Todas las guías publicadas, para el filtro.
        public IList<DashboardGuide> Guides { get; set; }

        // Una por guía mostrada (todas o la del filtro), en el orden de Guides.
        public IList<GuideSummary> GuideSummaries { get; set; }

        // Los totales de las guías mostradas: estados sumados y aprobación global (E8b, D-252).
        public SummaryTotals Totals { get; set; }

        // Sesiones del período por día, semana o mes y por modo.
        public ActivityChart Activity { get; set; }

        // Sesiones del período, de todos los modos (P-11), y su cambio en %; null con Todo.
        public int Sessions { get; set; }
        public ReportChange SessionsChange { get; set; }

        // Top 5 de pasos por % de sesiones con fallo en Assessment del período: los primeros de
        // Pasos con los mismos filtros (D-147), con su cambio.
        public IList<StepStats> TopSteps { get; set; }
    }

    // Un paso de una sesión en la descarga de sesiones, con su nombre (StepNames).
    public class SessionStepRow
    {
        public string StepName { get; set; }
        public StepRun Run { get; set; }
    }

    // Una sesión en la descarga de sesiones del Resumen (AC-22).
    public class SessionExportRow
    {
        public DashboardSession Session { get; set; }
        public string Code1 { get; set; }
        public string Name { get; set; }

        // Null si sigue en curso.
        public TimeSpan? Duration { get; set; }
        public int FailedAttempts { get; set; }

        // Null si no se muestra (DashboardQueryKit.VisibleScore).
        public decimal? Score { get; set; }

        // En orden de la guía.
        public IList<SessionStepRow> Steps { get; set; }
    }

    // Descarga de sesiones del Resumen (AC-22): las hojas Sesiones e Intentos por paso.
    public class SessionExport
    {
        public SummaryFilter Applied { get; set; }
        public ReportRange Range { get; set; }
        public IList<DashboardGuide> Guides { get; set; }

        // De la más reciente a la más vieja.
        public IList<SessionExportRow> Rows { get; set; }
    }

    // Filtros de Seguimiento: solo la guía; ignora el período (DA-3a).
    public class FollowUpFilter
    {
        // null = todas las guías.
        public int? GuideId { get; set; }
    }

    // Un operador que requiere atención en una guía, con todas las reglas que cumple.
    public class FollowUpRow
    {
        public int UserId { get; set; }
        public string Code1 { get; set; }
        public string Name { get; set; }
        public int TrainingGuideId { get; set; }
        public ReadinessState State { get; set; }

        // Inicio de su última sesión de la guía.
        public DateTime? LastActivity { get; set; }

        public IList<FollowUpReason> Reasons { get; set; }
    }

    // Seguimiento (spec 018, AC-19 y AC-20).
    public class FollowUpReport
    {
        public FollowUpFilter Applied { get; set; }
        public IList<DashboardGuide> Guides { get; set; }

        // Por guía (en el orden de Guides) y después por nombre.
        public IList<FollowUpRow> Rows { get; set; }

        // Llamar primero / Después y casos por regla (spec 022, brief 5; métricas derivadas).
        public FollowUpSummary Summary { get; set; }
    }
}
