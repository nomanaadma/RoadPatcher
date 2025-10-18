$(document).ready(function () {
    
    $(document).on('click', '.dt-select-checkbox', function(e) {
        e.preventDefault();
    });

    // Product Table
    const productsEditor = new DataTable.Editor({
        ajax: '/api/products',
        fields: [
            {
                label: "Name:",
                name: "Name",
            },
            {
                label: "Formula:",
                name: "Formula",
            },
            {
                label: "Company:",
                name: "Company",
            },
            {
                label: "Unit:",
                name: "Unit",
            },
            {
                label: "Stock:",
                name: "Stock",
            },
            {
                label: "Cost:",
                name: "Cost",
            },
            {
                label: "Price:",
                name: "Price",
            },
        ],
        table: '#products'
    });

    const productsTable = $('#products').DataTable({
        ajax: {
            url: '/api/products',
            type: 'POST'
        },
        serverSide: true,
        scrollX: true,
        pageLength: 8,
        "columnDefs": [
            {
                target: 1,
                visible: false,
                searchable: false
            },
            {
                "render": function(data) {
                    if(data === null) return null;
                    return moment(data).format('YYYY-MM-DD HH:mm:ss');
                },
                "targets": 9
            },
        ],
        columns: [
            {
                data: null,
                orderable: false,
                render: DataTable.render.select(),
                searchable: false,
                className: 'dt-select',
            },
            { data: "id" },
            { data: "Name" },
            { data: "Formula" },
            { data: "Company" },
            { data: "Unit" },
            { data: "Stock" },
            { data: "Cost" },
            { data: "Price" },
            { data: "LastUpdated" },
        ],
        layout: {
            top: 'searchBuilder',
            topStart: {
                buttons: [
                    {extend: 'create', editor: productsEditor, className: 'btn-dark'},
                    {extend: 'edit', editor: productsEditor, className: 'btn-warning'},
                    {extend: 'remove', editor: productsEditor, className: 'btn-danger'}
                ]
            }
        },
        order: [[1, 'asc']],
        select: {
            style: 'os',
            selector: 'td:first-child'
        }
    });

    productsTable.on( 'init.dt', function () {
        $('.dt-buttons .btn-secondary').removeClass('btn-secondary');
    });
    
});