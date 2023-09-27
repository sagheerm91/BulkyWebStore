let dataTable;

$(document).ready(() => {
    var url = window.location.search;
    if (url.includes("inprocess")) {
        loadDataTable("inprocess");
    }
    else {
        if (url.includes("completed")) {
            loadDataTable("completed");
        }
        else {
            if (url.includes("approved")) {
                loadDataTable("approved");
            }
            else {
                if (url.includes("pending")) {
                    loadDataTable("pending");
                }
                else {
                    loadDataTable("all");
                }
            }
        } 
    }
    
});

function loadDataTable(status) {
    dataTable = $('#tblData').DataTable({
        "ajax": { url: 'order/getall?status=' + status },
        "columns": [
            { data: 'id', "width": "5%" },
            { data: 'name', "width": "15%" },
            { data: 'phoneNumber', "width": "15%" },
            { data: 'applicationUser.email', "width": "15%" },
            { data: 'orderStatus', "width": "10%" },
            { data: 'orderTotal', "width": "10%" },
            {
                data: 'id',
                "render": function (data) {
                    return `<div class="w-100 btn-group" role="group">
                    <a href="order/details/?orderId=${data}" class="btn btn-primary mx-2"> <i class="bi bi-pencil"></i></a>
                    </div>`
                },
                "width": "25%"
            }
        ]
    });
}
