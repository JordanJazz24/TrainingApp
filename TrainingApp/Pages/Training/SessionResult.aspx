<%@ Page Title="Resultado de la sesión" Language="C#" MasterPageFile="~/UI/master/Site.Master"
    AutoEventWireup="true" CodeBehind="SessionResult.aspx.cs"
    Inherits="TrainingApp.Pages.Training.SessionResult" %>

<%-- Fin de sesión (spec 025, E3; diseño "Fin de sesión v3", docs/specs/025-design/fin-de-sesion.md).
     Solo binding: qué reforzar, los puntos por paso y la acción sugerida los calcula la BLL
     (SessionResultService); aquí solo textos. Sin guía visual ni anclas data-guide: no es parte del
     POD. Las acciones llevan al Inicio con el modo y la guía elegidos; no inician la sesión. --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="res-page">
        <div class="admin-page-head">
            <div>
                <h1 class="admin-page-title">Resultado de la sesión</h1>
                <p class="admin-page-lead"><%: GuideText %></p>
            </div>
        </div>

        <div class="res-stack">
            <section class="mt-hero res" aria-label="Resultado">
                <div>
                    <asp:PlaceHolder ID="PhVerdict" runat="server" Visible="false">
                        <span class="verdict <%: VerdictClass %>"><i class="bi <%: VerdictIcon %>" aria-hidden="true"></i><%: VerdictText %></span>
                    </asp:PlaceHolder>
                    <div class="fig"><%: FigureText %></div>
                    <div class="fig-l"><%: FigureLabel %></div>
                    <asp:PlaceHolder ID="PhBar" runat="server" Visible="false">
                        <div class="bar" role="img" aria-label="<%: BarAria %>">
                            <div class="bar-f <%: VerdictClass %>" style="width: <%: BarWidth %>%"></div>
                            <span class="bar-m" style="left: <%: MarkLeft %>%"></span>
                        </div>
                        <div class="bar-c"><span class="bar-min" style="left: <%: MarkLeft %>%">Mínimo para aprobar: <%: MinText %></span></div>
                    </asp:PlaceHolder>
                </div>
                <dl class="meta">
                    <div><i class="bi bi-journal-text" aria-hidden="true"></i><span><dt>Guía</dt><dd><%: GuideTitle %></dd></span></div>
                    <div><i class="bi <%: ModeIcon %>" aria-hidden="true"></i><span><dt>Modo</dt><dd><%: ModeText %></dd></span></div>
                    <div><i class="bi bi-calendar-event" aria-hidden="true"></i><span><dt>Fecha</dt><dd><%: DateText %></dd></span></div>
                    <div><i class="bi bi-stopwatch" aria-hidden="true"></i><span><dt>Duración</dt><dd><%: DurationText %></dd></span></div>
                </dl>
            </section>

            <%-- Una sola lista "Pasos" (D-340): las filas a reforzar llevan .is-ref; sin JavaScript se
                 ve A reforzar y el selector va oculto. --%>
            <section class="mt-panel mt-panel-body" aria-labelledby="resPasos">
                <div class="ph-hd">
                    <h2 class="mt-section-title" id="resPasos">Pasos</h2>
                    <div class="seg" id="resSeg" role="group" aria-label="Qué pasos ver" hidden>
                        <button type="button" id="resBtnRef" class="on" aria-pressed="true">A reforzar (<%: RefCount %>)</button>
                        <button type="button" id="resBtnAll" aria-pressed="false">Todos (<%: AllCount %>)</button>
                    </div>
                </div>
                <div class="res-list" id="resList">
                    <asp:PlaceHolder ID="PhClean" runat="server" Visible="false">
                        <div class="clean"><i class="bi bi-check-circle" aria-hidden="true"></i>Ningún paso con fallos.</div>
                    </asp:PlaceHolder>
                    <ul class="pl">
                        <asp:Repeater ID="RptSteps" runat="server">
                            <ItemTemplate>
                                <li class="pl-i <%#: Eval("RowCss") %>">
                                    <span class="pl-n"><%#: Eval("Number") %></span>
                                    <span class="pl-t"><%#: Eval("Name") %></span>
                                    <span class="pl-c">
                                        <asp:Repeater runat="server" DataSource='<%# Eval("Chips") %>'>
                                            <ItemTemplate><span class="mot"><%#: Container.DataItem %></span></ItemTemplate>
                                        </asp:Repeater>
                                    </span>
                                    <span class="pl-o"><span class="oc <%#: Eval("OutcomeTone") %>"><i class="bi <%#: Eval("OutcomeIcon") %>" aria-hidden="true"></i><%#: Eval("OutcomeText") %></span></span>
                                    <asp:PlaceHolder runat="server" Visible='<%# (bool)Eval("HasPoints") %>'>
                                        <span class="pl-p"><%#: Eval("PointsText") %>
                                            <asp:PlaceHolder runat="server" Visible='<%# (bool)Eval("HasLost") %>'><small><%#: Eval("LostText") %></small></asp:PlaceHolder>
                                        </span>
                                    </asp:PlaceHolder>
                                </li>
                            </ItemTemplate>
                        </asp:Repeater>
                    </ul>
                </div>
            </section>

            <section class="mt-panel mt-panel-body es-next" aria-label="Qué hacer después">
                <p class="next-why"><b>Qué hacer después.</b> <%: NextWhy %></p>
                <div class="next-btns">
                    <a class="btn btn-primary" href="<%: PrimaryUrl %>"><i class="bi <%: PrimaryIcon %>" aria-hidden="true"></i><%: PrimaryLabel %></a>
                    <asp:PlaceHolder ID="PhRepeat" runat="server" Visible="false">
                        <a class="btn btn-secondary" href="<%: RepeatUrl %>"><i class="bi bi-arrow-repeat" aria-hidden="true"></i><%: RepeatLabel %></a>
                    </asp:PlaceHolder>
                    <asp:PlaceHolder ID="PhHome" runat="server" Visible="false">
                        <a class="btn btn-secondary" href="<%: HomeUrl %>"><i class="bi bi-house" aria-hidden="true"></i>Ir al Inicio</a>
                    </asp:PlaceHolder>
                </div>
            </section>
        </div>
    </div>

    <script>
        // A reforzar / Todos (D-340): solo alterna qué filas se ven; sin postback ni estado.
        (function () {
            var seg = document.getElementById('resSeg');
            var list = document.getElementById('resList');
            var btnRef = document.getElementById('resBtnRef');
            var btnAll = document.getElementById('resBtnAll');
            if (!seg || !list || !btnRef || !btnAll) return;

            function show(all) {
                list.classList.toggle('is-all', all);
                btnRef.classList.toggle('on', !all);
                btnAll.classList.toggle('on', all);
                btnRef.setAttribute('aria-pressed', all ? 'false' : 'true');
                btnAll.setAttribute('aria-pressed', all ? 'true' : 'false');
            }
            btnRef.addEventListener('click', function () { show(false); });
            btnAll.addEventListener('click', function () { show(true); });
            seg.hidden = false;
        })();
    </script>
</asp:Content>
