$(document).ready(function () {

    const PayrollExportsEditor = new DataTable.Editor({
        ajax: '/api/payroll_export_details',
        fields: [
            { label: "Payroll Code", name: "PayrollCode" },
            { label: "First Name", name: "FirstName" },
            { label: "Last Name", name: "LastName" },
            { label: "Salary Rate", name: "SalaryRate" },
            { label: "Salary Hours", name: "SalaryHours" },
            { label: "Salary Amount", name: "SalaryAmount" },
            { label: "Benefit Rate", name: "BenefitRate" },
            { label: "Benefit Hours", name: "BenefitHours" },
            { label: "Holiday Rate", name: "HolidayRate" },
            { label: "Holiday Hours", name: "HolidayHours" },
            { label: "Holiday Amount", name: "HolidayAmount" },
            { label: "Overtime Rate", name: "OvertimeRate" },
            { label: "Overtime Hours", name: "OvertimeHours" },
            { label: "Overtime Amount", name: "OvertimeAmount" },
            { label: "Vacation Rate", name: "VacationRate" },
            { label: "Vacation Hours", name: "VacationHours" },
            { label: "Vacation Amount", name: "VacationAmount" },
            { label: "Sick Pay Rate", name: "SickPayRate" },
            { label: "Sick Pay Hours", name: "SickPayHours" },
            { label: "Sick Pay Amount", name: "SickPayAmount" },
            { label: "Check", name: "Check" },
        ],
        table: '#PayrollExportDetails'
    });
    
    let PayrollExportDetailsTable = $('#PayrollExportDetails');

    let filename = PayrollExportDetailsTable.data('title')
        .replace('PayrollData_', '')
        .replace('.xml', '')

    filename = moment(filename, 'MM-DD-YYYY').format('YYYY-MM-DD')+'_Export';

    PayrollExportDetailsTable = PayrollExportDetailsTable.DataTable({
        ajax: {
            url: '/api/payroll_export_details',
            data: {
                reportArchiveId: reportArchiveId
            }
        },
        scrollX: true,
        pageLength: 15,
        lengthChange: false,
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
            { data: 'PayrollCode' },
            { data: 'FirstName' },
            { data: 'LastName' },
            { 
                data: 'SalaryRate',
                createdCell: function (td) {
                    $(td).addClass('bg-s-violet'); // Add a specific class to PayrollCode column cells
                }
            },
            { 
                data: 'SalaryHours',
                createdCell: function (td) {
                    $(td).addClass('bg-s-orange'); // Add a specific class to PayrollCode column cells
                }
            },
            { 
                data: 'SalaryAmount',
                createdCell: function (td) {
                    $(td).addClass('bg-s-yellow'); // Add a specific class to PayrollCode column cells
                }
            },
            { 
                data: 'BenefitRate',
                createdCell: function (td) {
                    $(td).addClass('bg-s-violet'); // Add a specific class to PayrollCode column cells
                }
            },
            { 
                data: 'BenefitHours',
                createdCell: function (td) {
                    $(td).addClass('bg-s-orange'); // Add a specific class to PayrollCode column cells
                }
            },
            { 
                data: 'HolidayRate',
                createdCell: function (td) {
                    $(td).addClass('bg-s-violet'); // Add a specific class to PayrollCode column cells
                }
            },
            { 
                data: 'HolidayHours',
                createdCell: function (td) {
                    $(td).addClass('bg-s-orange'); // Add a specific class to PayrollCode column cells
                }
            },
            { 
                data: 'HolidayAmount',
                createdCell: function (td) {
                    $(td).addClass('bg-s-yellow'); // Add a specific class to PayrollCode column cells
                }
            },
            { 
                data: 'OvertimeRate',
                createdCell: function (td) {
                    $(td).addClass('bg-s-violet'); // Add a specific class to PayrollCode column cells
                }
            },
            { 
                data: 'OvertimeHours',
                createdCell: function (td) {
                    $(td).addClass('bg-s-orange'); // Add a specific class to PayrollCode column cells
                }
            },
            { 
                data: 'OvertimeAmount',
                createdCell: function (td) {
                    $(td).addClass('bg-s-yellow'); // Add a specific class to PayrollCode column cells
                }
            },
            { 
                data: 'VacationRate',
                createdCell: function (td) {
                    $(td).addClass('bg-s-violet'); // Add a specific class to PayrollCode column cells
                }
            },
            { 
                data: 'VacationHours',
                createdCell: function (td) {
                    $(td).addClass('bg-s-orange'); // Add a specific class to PayrollCode column cells
                }
            },
            { 
                data: 'VacationAmount',
                createdCell: function (td) {
                    $(td).addClass('bg-s-yellow'); // Add a specific class to PayrollCode column cells
                }
            },
            { 
                data: 'SickPayRate',
                createdCell: function (td) {
                    $(td).addClass('bg-s-violet'); // Add a specific class to PayrollCode column cells
                }
            },
            { 
                data: 'SickPayHours',
                createdCell: function (td) {
                    $(td).addClass('bg-s-orange'); // Add a specific class to PayrollCode column cells
                }
            },
            { 
                data: 'SickPayAmount',
                createdCell: function (td) {
                    $(td).addClass('bg-s-yellow'); // Add a specific class to PayrollCode column cells
                }
            },
            { 
                data: 'Check',
                createdCell: function (td) {
                    $(td).addClass('bg-s-grey'); // Add a specific class to PayrollCode column cells
                }
            },
        ],
        layout: {
            top: 'searchBuilder',
            topStart: {
                buttons: [
                    {extend: 'create', editor: PayrollExportsEditor, className: 'btn-dark'},
                    {extend: 'edit', editor: PayrollExportsEditor, className: 'btn-warning'},
                    {extend: 'remove', editor: PayrollExportsEditor, className: 'btn-danger'},
                ]
            }
        },
        order: [[1, 'asc']],
        select: {
            style: 'os',
            selector: 'td:first-child'
        }
    });

    PayrollExportDetailsTable.on( 'init.dt', function () {
        $('.dt-buttons .btn-secondary').removeClass('btn-secondary');
        $('.btn-custom-excel').addClass('btn-secondary');
    });

    PayrollExportDetailsTable.on('click', 'tbody td:not(:first-child)', function (e) {
        PayrollExportsEditor.inline(this);
    });

    const exportBtn = $(`<button class="btn btn-secondary btn-custom-excel" tabindex="0" type="button"><span>Export to Excel</span></button>`);
    exportBtn.appendTo('#PayrollExportDetails_wrapper .row.mt-2.justify-content-between:eq(0) .col-md-auto.me-auto .dt-buttons');

    exportBtn.on('click', async function() {
       
        const ethis = $(this);
        
        ethis.addClass('processing').attr('disabled', 'disabled');

        const exportData = [];

        PayrollExportDetailsTable.rows().every(function () {
            exportData.push(this.data())
        });
        
        const response = await axios.post('/api/payroll_export_detail_xls', exportData, {
            responseType: 'blob'
        });

        // Create a blob from the response data and trigger a download
        const blob = new Blob([response.data], { type: 'application/vnd.ms-excel' });
        const downloadUrl = window.URL.createObjectURL(blob);
        const downloadLink = document.createElement('a');
        downloadLink.href = downloadUrl;
        downloadLink.download = filename+'.xls';
        document.body.appendChild(downloadLink);
        downloadLink.click();
        document.body.removeChild(downloadLink);

        // Reset button state
        ethis.removeClass('processing').removeAttr('disabled');
        
    });
    
});
    