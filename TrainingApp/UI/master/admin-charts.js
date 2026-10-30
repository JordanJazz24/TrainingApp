// Gráficas del dashboard con los tokens del sistema de diseño (Fase 11, brief 019). Se carga
// después de Chart.js en las vistas que tienen gráficas.
//
// mtCharts.create(canvas, build) dibuja la gráfica con build(tokens) y la vuelve a dibujar
// cuando cambia el tema (evento "mt-theme" de theme.js), así toma los colores nuevos. Los
// colores salen de las variables CSS (--mt-*): ninguna gráfica tiene colores sueltos.
var mtCharts = (function () {
    var charts = [];

    function token(name) {
        return getComputedStyle(document.documentElement).getPropertyValue(name).trim();
    }

    // Los colores y la tipografía del tema actual.
    function tokens() {
        return {
            text: token('--mt-text'),
            muted: token('--mt-muted'),
            border: token('--mt-border'),
            surface: token('--mt-surface'),
            surface2: token('--mt-surface-2'),
            accent: token('--mt-accent'),
            ok: token('--mt-ok'),
            bad: token('--mt-bad'),
            warnBar: token('--mt-warn-bar'),
            m1: token('--mt-m1'),
            m2: token('--mt-m2'),
            m3: token('--mt-m3'),
            font: token('--mt-font')
        };
    }

    function applyDefaults(t) {
        Chart.defaults.font.family = t.font;
        Chart.defaults.font.size = 13;
        Chart.defaults.color = t.muted;
        Chart.defaults.borderColor = t.border;
        Chart.defaults.plugins.tooltip.backgroundColor = t.text;
        Chart.defaults.plugins.tooltip.titleColor = t.surface;
        Chart.defaults.plugins.tooltip.bodyColor = t.surface;
        Chart.defaults.plugins.tooltip.padding = 10;
        Chart.defaults.plugins.tooltip.cornerRadius = 6;
    }

    // Ejes: grilla suave solo en el eje de valores, sin borde en las categorías.
    function axes(t, options) {
        options = options || {};
        var value = {
            stacked: !!options.stacked,
            beginAtZero: true,
            grid: { color: t.border },
            border: { display: false },
            ticks: {
                precision: 0,
                callback: options.percent ? function (v) { return v + '%'; } : undefined
            }
        };
        if (options.percent) value.max = 100;
        var category = { stacked: !!options.stacked, grid: { display: false }, border: { color: t.border } };
        return options.horizontal ? { x: value, y: category } : { x: category, y: value };
    }

    // Barras con extremos redondeados y una línea de superficie entre segmentos apilados.
    function bars(t, label, data, color) {
        return {
            label: label, data: data, backgroundColor: color,
            borderColor: t.surface, borderWidth: 2, borderRadius: 4, borderSkipped: false,
            maxBarThickness: 56
        };
    }

    // Rótulos sobre los puntos y las barras, para leer la gráfica sin pasar el mouse (Operador,
    // spec 022 brief 7): cada dataset los trae en mtLabels, un texto por dato (vacío: ninguno).
    function valueLabels(t) {
        return {
            id: 'mtValueLabels',
            afterDatasetsDraw: function (chart) {
                var ctx = chart.ctx;
                ctx.save();
                ctx.font = '600 12px ' + t.font;
                ctx.textAlign = 'center';
                ctx.textBaseline = 'bottom';
                ctx.lineJoin = 'round';
                chart.data.datasets.forEach(function (dataset, i) {
                    if (!dataset.mtLabels || !chart.isDatasetVisible(i)) return;
                    chart.getDatasetMeta(i).data.forEach(function (element, k) {
                        var text = dataset.mtLabels[k];
                        if (!text || element.skip || isNaN(element.y)) return;
                        var y = element.y - (dataset.type === 'line' ? 10 : 4);
                        ctx.lineWidth = 4;
                        ctx.strokeStyle = t.surface;
                        ctx.strokeText(text, element.x, y);
                        ctx.fillStyle = t.text;
                        ctx.fillText(text, element.x, y);
                    });
                });
                ctx.restore();
            }
        };
    }

    function draw(entry) {
        if (entry.chart) entry.chart.destroy();
        var t = tokens();
        applyDefaults(t);
        entry.chart = new Chart(entry.canvas, entry.build(t));
    }

    function create(canvas, build) {
        if (typeof Chart === 'undefined' || !canvas) return null;
        var entry = { canvas: canvas, build: build, chart: null };
        charts.push(entry);
        draw(entry);
        return entry;
    }

    document.addEventListener('mt-theme', function () { charts.forEach(draw); });

    return { create: create, axes: axes, bars: bars, valueLabels: valueLabels };
})();
