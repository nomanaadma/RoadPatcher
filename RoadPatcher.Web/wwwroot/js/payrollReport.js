$(document).ready(function () {
    
    // Payroll Report Table
    const payrollReportEditor = new DataTable.Editor({
        ajax: '/api/payroll_reports',
        fields: [
            {
                label: 'File:',
                name: 'File',
            },
            {
                label: 'Week:',
                name: 'Week'
            },
            {
                label: 'Date:',
                name: 'Date'
            },
            {
                label: 'Status:',
                name: 'Status'
            }
        ],
        table: '#payrollReport'
    });

    const payrollReportTable = $('#payrollReport').DataTable({
        ajax: '/api/payroll_reports',
        scrollX: true,
        pageLength: 15,
        scrollCollapse: true,
        scrollY: '500px',
        columnDefs: [
            {
                target: 1,
                visible: false,
                searchable: false
            }
        ],
        columns: [
            {
                data: null,
                orderable: false,
                render: DataTable.render.select(),
            },
            { data: 'id' },
            {data: 'File'},
            {data: 'Week'},
            {data: 'Date'},
            {
                data: "Status",
                render: function (data) {
                    if (data === "unprocessed") {
                        return `<div class="tac text-uppercase"><span class="badge badge-sm badge-light-warning fs-7 fw-bold">${data}</span></div>`;
                    } else {
                        return `<div class="tac text-uppercase"><span class="badge badge-sm badge-light-success fs-7 fw-bold">${data}</span></div>`;
                    }
                }
            },
            {
                data: null,
                orderable: false,
                render: (data) => {
                    return `<a class="btn btn-info btn-sm-c" href="/PayrollReports/${data.id}">View</a>`;
                },
            },
        ],
        layout: {
            topStart: {
                buttons: [
                    {extend: 'remove', editor: payrollReportEditor, className: 'btn-danger'}
                ]
            }
        },
        order: [[1, 'desc']],
        select: {
            style: 'os',
            selector: 'td:first-child'
        }
    });

    payrollReportTable.on('init.dt', function () {

        const newButton = `
            <button
                data-bs-toggle="modal" 
                data-bs-target="#payroll_report_upload" 
                class="btn buttons-create btn-dark" 
                data-backdrop='static' 
                data-keyboard='false'
                tabIndex="0" 
                aria-controls="aides" 
                type="button"> 
                    <span>New</span>
            </button>`;

        $(newButton).prependTo('.dt-buttons.btn-group');

        $('.dt-buttons .btn-secondary').removeClass('btn-secondary');

    });

    let payrollReportModel = $('#payroll_report_upload');

    if (payrollReportModel.length) {
        payrollReportModel = new bootstrap.Modal(payrollReportModel[0], {
            backdrop: 'static',
            keyboard: false
        });
    }

    const payroll_report_file = $("#payroll_report_file");
    payroll_report_file.change(function (e) {

        if (e.target.files[0]) {
            const filename = e.target.files[0].name;

            payrollReportTable.rows().every(function () {

                const data = this.data();

                if (data.File === filename) {

                    Swal.fire({
                        icon: "warning",
                        title: "File already exists.",
                        text: "Proceeding will create a duplicate record.",
                        showDenyButton: true,
                        showCancelButton: true,
                        confirmButtonText: "View",
                        denyButtonText: 'Ok',
                        allowOutsideClick: false
                    }).then((result) => {

                        if (result.isConfirmed) {
                            payroll_report_file.val("");
                            payrollReportModel.hide();
                            payrollReportTable.search(filename).draw();
                        } else if (result.isDenied) {
                            $('#PayrollReportsForm').trigger("submit");
                        } else {
                            payroll_report_file.val("");
                            payrollReportModel.hide();
                        }

                    });

                }

            });

        }

    });
});
