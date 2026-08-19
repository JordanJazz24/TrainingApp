var $table = $('#table');
let locale = "es-CR";

const fillTable = async () => {

    try {

        //if (await validaConsulta() === true) {
        $("#cuerpoTb").empty();
        $("#loader").removeClass("invisible");

       

        let result;

        result = await $.ajax({
            type: "POST",
            url: 'Users.aspx/All',
            contentType: "application/json; charset=utf-8",
            dataType: "json",

        })
    
        var xmlDoc = $.parseXML(result.d);
        var xml = $(xmlDoc);
        var info = xml.find("Info");

        let data = [];
    
        if (info.length > 0) {
            $(info).each(function () {

                let accion = `<button type="button" title="Reset"  class="btn btn-danger btn-md text-center" onClick="btnReset_click(${$(this).find("code1").text()})"> Reset</button>`
                // Separar nombre y apellido
                let fullName = $(this).find("fullName").text();
                let partes = fullName.split(',').map(p => p.trim());
                let apellido = partes[0] || '';
                let nombre = partes[1] || '';


                let objeto = {
                    Code1: $(this).find("code1").text(),
                    Nombre: nombre,
                    Apellido: apellido,
                    Area: $(this).find("area").text(),
                    Password:accion

                }

                data.push(objeto)
            })
        }

        $("#loader").addClass("invisible");

        $table.bootstrapTable('refresh').bootstrapTable({
            exportTypes: ['csv', 'txt', 'excel', 'pdf'],
            exportDataType: 'all',
            locale: locale

        });

        $table.bootstrapTable('load', data);

        //}

    } catch (e) {

        console.log(e);
    }
}



$(document).ready(async function () {

    if (!sessionStorage.getItem("isAuthAdmin")) {
        window.location.href = "/MESSimulator/loginAdmin.aspx"
    }
    await fillTable();

});



function btnReset_click(code1) {
    Swal.fire({
        title: '¿Estás seguro?',
        html: `¿Deseas restablecer la cuenta del usuario <strong>${code1}</strong>?`,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33',
        confirmButtonText: 'Sí, restablecer',
        cancelButtonText: 'Cancelar',
        reverseButtons: true
    }).then( (result) => {
       
        if (result.isConfirmed) {

            Swal.fire({
                title: 'Procesando...',
                text: 'Restableciendo contraseña',
                allowOutsideClick: false,
                didOpen: () => {
                    Swal.showLoading();
                }
            });

            let parametros = {
                code1
            }
             $.ajax({
                type: "POST",
                 url: 'Users.aspx/Delete',
                data: JSON.stringify(parametros),
                contentType: "application/json; charset=utf-8",
                 dataType: "json",
                 async: true,
                success: async function (response) {
                    Swal.fire({
                        icon: 'success',
                        title: '¡Cuenta restablecida!',
                        text: `La contraseña del usuario ${code1} ha sido restablecida exitosamente.`,
                        confirmButtonColor: '#3085d6'
                    });
                    await fillTable();
                },
                 error: function (error) {
                     console.log(error)
                    Swal.fire({
                        icon: 'error',
                        title: 'Error',
                        text: 'Hubo un problema al restablecer la cuenta. Inténtalo de nuevo.',
                        confirmButtonColor: '#d33'
                    });
                }
            });

        }
    });
    


}

