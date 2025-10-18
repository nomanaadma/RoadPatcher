$(document).ready(function () {

    $('#PayrollExports').DataTable({
        ajax: '/api/payroll_exports',
        scrollX: true,
        pageLength: 15,
        lengthChange: false,
        scrollCollapse: true,
        scrollY: '500px',
        columnDefs: [
            {
                target: 0,
                visible: false,
                searchable: false
            }
        ],
        columns: [
            { data: 'id' },
            {
                data: 'File',
                render: (data) => {

                    data = data
                        .replace('PayrollData_', '')
                        .replace('.xml', '');

                    return moment(data, 'MM-DD-YYYY').format('YYYY-MM-DD')+'_Export.xlsx';
                    
                },
            },
            {data: 'Week'},
            {
                data: 'Exported',
                render: (data) => {
                    return data.substring(0, 10);
                },
            },
            {
                data: null,
                orderable: false,
                render: (data) => {
                    return `<a class="btn btn-info btn-sm-c" href="/PayrollExports/${data.id}">View</a>`;
                },
            },
        ],
        order: [[0, 'desc']],
    });

});