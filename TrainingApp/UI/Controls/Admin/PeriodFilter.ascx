<%@ Control Language="C#" AutoEventWireup="true"
    CodeBehind="PeriodFilter.ascx.cs"
    Inherits="TrainingApp.UI.Controls.Admin.PeriodFilter" %>
<%-- El período de Pasos y del Resumen (spec 023, P-12): los atajos, Todo y Personalizado (D-210),
     con Inicio y Fin del selector nativo (D-216) que aparecen sin postback al elegir
     Personalizado, y debajo el rango aplicado en palabras y el anterior (P-14). Va dentro de la
     barra de filtros de la página, que lo aplica con su botón Filtrar. No calcula rangos: los
     resuelve la BLL (ReportPeriods). --%>
<div class="mt-field">
    <label class="mt-field-label" for="<%= DdlPeriod.ClientID %>">Período</label>
    <asp:DropDownList ID="DdlPeriod" runat="server" CssClass="form-select">
        <asp:ListItem Value="today" Text="Hoy" />
        <asp:ListItem Value="week" Text="Esta semana" />
        <asp:ListItem Value="month" Text="Este mes" />
        <asp:ListItem Value="prevmonth" Text="Mes anterior" />
        <asp:ListItem Value="30d" Text="Últimos 30 días" />
        <asp:ListItem Value="all" Text="Todo" />
        <asp:ListItem Value="custom" Text="Personalizado…" />
    </asp:DropDownList>
</div>
<asp:Panel ID="PnlCustom" runat="server" CssClass="mt-period-custom">
    <div class="mt-field">
        <label class="mt-field-label" for="<%= TxtFrom.ClientID %>">Inicio</label>
        <asp:TextBox ID="TxtFrom" runat="server" TextMode="Date" CssClass="form-control" />
    </div>
    <div class="mt-field">
        <label class="mt-field-label" for="<%= TxtTo.ClientID %>">Fin</label>
        <asp:TextBox ID="TxtTo" runat="server" TextMode="Date" CssClass="form-control" />
    </div>
</asp:Panel>
<%-- Pasos (brief 022, 4a) lo pinta en su franja, junto al título (ShowsRange = false). --%>
<asp:PlaceHolder ID="PhRange" runat="server">
    <p class="mt-period-range" aria-live="polite"><asp:Literal ID="LitRange" runat="server" /></p>
</asp:PlaceHolder>
<script>
    // Inicio y Fin solo con Personalizado, sin postback.
    (function () {
        var select = document.getElementById('<%= DdlPeriod.ClientID %>');
        var custom = document.getElementById('<%= PnlCustom.ClientID %>');
        if (!select || !custom) return;
        function sync() { custom.hidden = select.value !== 'custom'; }
        select.addEventListener('change', sync);
        sync();
    })();
</script>
