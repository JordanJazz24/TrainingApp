<%@ Page Title="Elige tu área" Language="C#" MasterPageFile="~/UI/master/Site.Master"
    AutoEventWireup="true" CodeBehind="ChooseArea.aspx.cs"
    Inherits="TrainingApp.Pages.Training.ChooseArea" %>

<%-- Spec 021 (D-169, D-177, P-10): el operador sin área la elige una vez, después del cambio de
     contraseña. Después solo su trainer la cambia (Usuarios). --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="start-wrap">
        <div class="admin-page-head">
            <div>
                <h1 class="admin-page-title">Elige tu área</h1>
                <p class="admin-page-lead">Solo verás las guías de tu área. Después, solo tu trainer podrá cambiarla.</p>
            </div>
        </div>

        <section class="mt-hero start-card" aria-label="Elige tu área">
            <asp:PlaceHolder ID="PhChoose" runat="server">
                <%-- Un solo selector agrupado por producto (D-169). DropDownList no tiene optgroup:
                     el valor se lee del form y la BLL valida que el área sea elegible. --%>
                <div class="mt-field">
                    <label for="chooseAreaSelect" class="mt-field-label">Área</label>
                    <select id="chooseAreaSelect" name="area" class="form-select">
                        <option value="">-- Selecciona tu área --</option>
                        <asp:Repeater ID="RptProducts" runat="server">
                            <ItemTemplate>
                                <optgroup label="<%#: Eval("ProductName") %>">
                                    <asp:Repeater runat="server" DataSource='<%# Eval("Areas") %>'>
                                        <ItemTemplate>
                                            <option value="<%#: Eval("AreaId") %>" data-label="<%#: Eval("Label") %>"><%#: Eval("AreaName") %></option>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </optgroup>
                            </ItemTemplate>
                        </asp:Repeater>
                    </select>
                </div>

                <asp:Button ID="BtnContinue" runat="server" Text="Continuar"
                    CssClass="btn btn-primary w-100" OnClick="BtnContinue_Click"
                    OnClientClick="return chooseAreaConfirm(this);" />
            </asp:PlaceHolder>

            <asp:Label ID="LblNoAreas" runat="server" CssClass="mt-empty" Visible="false"
                Text="Aún no hay áreas con guías publicadas. Avisa a tu trainer." />
        </section>
    </div>

    <script src="<%=ResolveUrl("~/Scripts/sweetalert2.all.min.js")%>"></script>
    <script>
        // Confirmación antes de guardar (D-177): No no hace postback; Sí repite el click, que
        // lo hace. El texto sale del data-label de la opción elegida (texto, no HTML).
        function chooseAreaConfirm(button) {
            if (button.getAttribute('data-confirmed') === '1') {
                button.removeAttribute('data-confirmed');
                return true;
            }
            var option = document.getElementById('chooseAreaSelect').selectedOptions[0];
            if (!option || !option.value) {
                Swal.fire({ icon: 'info', text: 'Selecciona tu área.' });
                return false;
            }
            Swal.fire({
                icon: 'question',
                text: '¿Tu área es ' + option.getAttribute('data-label') +
                      '? Después solo tu trainer podrá cambiarla.',
                showCancelButton: true,
                confirmButtonText: 'Sí',
                cancelButtonText: 'No'
            }).then(function (r) {
                if (!r.isConfirmed) return;
                button.setAttribute('data-confirmed', '1');
                button.click();
            });
            return false;
        }
    </script>
</asp:Content>
