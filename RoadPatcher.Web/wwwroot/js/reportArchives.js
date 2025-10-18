$(document).ready(function () {

    // Payroll Report Table
    const reportArchiveEditor = new DataTable.Editor({
        ajax: '/api/report_archives',
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
            }
        ],
        table: '#reportArchives'
    });

    const reportArchiveTable = $('#reportArchives').DataTable({
        ajax: '/api/report_archives',
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
                data: null,
                orderable: false,
                render: (data) => {

                    const exported = data.Exported == null ? "" : `
                        <a target="_blank" class="btn btn-dark btn-sm-c" href="/PayrollExports/${data.id}">Payroll Export</a>
                        <a target="_blank" class="btn btn-success btn-sm-c" href="/BenefitCards/${data.id}">Benefit Card</a>`;
                    
                    return `
                        <a class="btn btn-info btn-sm-c" href="/ReportArchives/${data.id}">View Archive</a>
                        ${exported}
                    `;
                },
            },
        ],
        layout: {
            topStart: {
                buttons: [
                    {extend: 'remove', editor: reportArchiveEditor, className: 'btn-danger'}
                ]
            }
        },
        order: [[1, 'desc']],
        select: {
            style: 'os',
            selector: 'td:first-child'
        }
    });

    reportArchiveTable.on('init.dt', function () {
        $('.dt-buttons .btn-secondary').removeClass('btn-secondary');
    });

});