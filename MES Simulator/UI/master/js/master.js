// document definition for reduce the word document 
const d = document
var seccionStorage = window.sessionStorage



d.addEventListener("DOMContentLoaded", () => {
    let $loader = d.getElementById("loader")
    $loader.classList.add("invisible")

    //Cargar el contenido de la tabla principal del dashboard
    var $workTableContainer = d.getElementById("tableWorkContainer")
    
    if (!seccionStorage.getItem("workTableContainer")) {
        $workTableContainer.innerHTML = `<tr class="OrderRow" >
                      
                      <td >CR9A053631091</td>
                      <td>DWF L Cushion PP NIR</td>
                      <td class="orderValue">3000</td>
                      <td>
                        <span class="status-indicator status-green"></span>
                        Activo
                      </td>
                      <td>-</td>
                      <td class="order_number">303467636</td>
                    </tr>`
    } else if (seccionStorage.getItem("workTableContainer") == 0) {
        $workTableContainer.textContent=""
    }

    else {
        $workTableContainer.innerHTML = seccionStorage.getItem("workTableContainer")

    }






    const sideBarList = {
        activities: d.getElementById("groupActivities"),
        reports: d.getElementById("groupReports")
    }

    //Funcion para interactuar con el sidebar 
    const $sideBar = d.getElementById("sideBar");
    $sideBar.addEventListener("click", e => {
        
        let clickedButton = null;

        if (e.target.closest(".btn_activities")) {
            clickedButton = "activities";
        } else if (e.target.closest(".btn_reports")) {
            clickedButton = "reports";
        }

       
        if (clickedButton) {
         
            Object.keys(sideBarList).forEach(key => {
                sideBarList[key].classList.add("invisible");
            });

           
            sideBarList[clickedButton].classList.remove("invisible");
        }
    });


    d.addEventListener("click", e => {
        if (!e.target.closest("#sideBar")) {
            Object.keys(sideBarList).forEach(key => {
                sideBarList[key].classList.add("invisible");
            });
        }
    });





 
})

