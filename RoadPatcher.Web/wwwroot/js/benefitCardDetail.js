$(document).ready(function () {

    const benefitCardsEditor = new DataTable.Editor({
        ajax: '/api/benefit_card_details',
        fields: [
            {
                label: 'Payroll Code',
                name: 'PayrollCode'
            },
            {
                label: 'SSN',
                name: 'Ssn'
            },
            { label: "Last Name", name: "LastName" },
            { label: "First Name", name: "FirstName" },
            { label: "Address1", name: "Address1" },
            { label: "Address2", name: "Address2" },
            { label: "City", name: "City" },
            { label: "State", name: "State" },
            { label: "Zip", name: "Zip" },
            { label: "Date of Birth", name: "DateOfBirth" },
            {
                label: 'Gender',
                name: 'Gender',
                type: 'select',
                options: [
                    {label: '', value: null},
                    {label: 'Male', value: "M"},
                    {label: 'Female', value: "F"}
                ]
            },
            { label: "Email", name: "Email" },
            { label: "Home Phone", name: "HomePhone" },
            { label: "Mobile Phone", name: "MobilePhone" },
            { label: "Date of Hire", name: "DateOfHire" },
            { label: "Pay Period", name: "PayPeriod" },
            { label: "Hours Worked", name: "HoursWorked" },
            { label: "Benefit Rate", name: "BenefitRate" },
            { label: "Benefit Amount", name: "BenefitAmount" },
        ],
        table: '#benefitCardDetails'
    });

    const benefitCardDetailsTable = $('#benefitCardDetails');

    let filename = benefitCardDetailsTable.data('title')
        .replace('PayrollData_', '')
        .replace('.xml', '')

    filename = moment(filename, 'MM-DD-YYYY').format('YYYY-MM-DD')+'_BenefitCards';

    benefitCardDetailsTable.DataTable({
        ajax: {
            url: '/api/benefit_card_details',
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
            { data: 'Ssn' },
            { data: 'LastName' },
            { data: 'FirstName' },
            { data: 'Address1' },
            { data: 'Address2' },
            { data: 'City' },
            { data: 'State' },
            { data: 'Zip' },
            { data: 'DateOfBirth' },
            { data: 'Gender' },
            { data: 'Email' },
            { data: 'HomePhone' },
            { data: 'MobilePhone' },
            { data: 'DateOfHire' },
            { data: 'PayPeriod' },
            { data: 'HoursWorked' },
            { data: 'BenefitRate' },
            { data: 'BenefitAmount' },
        ],
        layout: {
            top: 'searchBuilder',
            topStart: {
                buttons: [
                    {extend: 'create', editor: benefitCardsEditor, className: 'btn-dark'},
                    {extend: 'edit', editor: benefitCardsEditor, className: 'btn-warning'},
                    {extend: 'remove', editor: benefitCardsEditor, className: 'btn-danger'},
                    {
                        extend: 'excelHtml5',
                        text: 'Export to Excel',
                        title: '',
                        exportOptions: {
                            columns: ':visible:not(:eq(0))'
                        },
                        filename,
                        customize: function(xlsx) {

                            const sheet = xlsx.xl.worksheets['sheet1.xml'];

                            $('row:first c').attr('s', '2');

                            $('row:not(:first) c[r]', sheet).attr('s', '51');

                        }
                    }
                ]
            }
        },
        order: [[4, 'asc']],
        select: {
            style: 'os',
            selector: 'td:first-child'
        }
    });

    benefitCardDetailsTable.on( 'init.dt', function () {
        $('.dt-buttons .btn-secondary').removeClass('btn-secondary');
        $('.buttons-excel.buttons-html5').addClass('btn-secondary');
    });

    benefitCardDetailsTable.on('click', 'tbody td:not(:first-child)', function (e) {
        benefitCardsEditor.inline(this);
    });
    
});
