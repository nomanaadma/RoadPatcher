$(document).ready(function () {

    const aidesEditor = new DataTable.Editor({
        ajax: '/api/aides',
        fields: [
            {
                label: 'Full Name',
                name: 'FullName'
            },
            {
                label: 'Aide Id',
                name: 'AideId'
            },
            {
                label: 'Status',
                name: 'Status',
                type: 'radio',
                options: [
                    {label: 'Active', value: true},
                    {label: 'Inactive', value: false}
                ]
            },
            {
                label: 'Payroll Code',
                name: 'PayrollCode'
            },
            {
                label: 'Hourly Rate',
                name: 'HourlyRate'
            },
            {
                label: 'Benefit Card',
                name: 'BenefitCard',
                type: 'radio',
                options: [
                    {label: 'Yes', value: true},
                    {label: 'No', value: false}
                ]
            },
            {
                label: 'Benefit Rate',
                name: 'BenefitRate',
            },
            {
                label: 'OT Hourly Rate',
                name: 'OtRate'
            },
            {
                label: 'Overtime',
                name: 'Overtime',
                type: 'radio',
                options: [
                    {label: 'Yes', value: true},
                    {label: 'No', value: false}
                ]
            },
            {
                label: 'SSN',
                name: 'Ssn'
            },
            {
                label: 'Address 1',
                name: 'Address1'
            },
            {
                label: 'Address 2',
                name: 'Address2'
            },
            {
                label: 'City',
                name: 'City'
            },
            {
                label: 'State',
                name: 'State'
            },
            {
                label: 'Zip',
                name: 'Zip'
            },
            {
                label: 'Date of Birth',
                name: 'DateOfBirth',
                type: 'datetime'
            },
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
            {
                label: 'Email',
                name: 'Email'
            },
            {
                label: 'Home Phone',
                name: 'HomePhone'
            },
            {
                label: 'Mobile Phone',
                name: 'MobilePhone'
            },
            {
                label: 'Date of Hire',
                name: 'DateOfHire',
                type: 'datetime'
            }
        ],
        table: '#aides'
    });
    
    const searchCriteria = [
        {
            condition: '=',
            data: 'Status',
            origData: 'Status',
            type: 'string',
            value: ['Active']
        },
    ];
    
    if(window.location.href.includes('?PayrollCodeNull')) {
        searchCriteria.push( {
            condition: 'null',
            data: 'Payroll Code',
            origData: 'PayrollCode',
        });
    }
    
    const aidesTable = $('#aides').DataTable({
        ajax: '/api/aides',
        scrollX: true,
        pageLength: 11,
        "columnDefs": [
            {
                "render": function(data) {
                    if(data === null) return null;
                    return moment(data).format('YYYY-MM-DD');
                },
                "targets": 16
            },
            {
                "render": function(data) {
                    if(data === null) return null;
                    return moment(data).format('YYYY-MM-DD');
                },
                "targets": 21
            },
            {
                "render": function(data) {
                    return moment(data).subtract(4, 'hours').format('YYYY-MM-DD - HH:mm:ss');
                },
                "targets": 22
            }
        ],
        columns: [
            {
                data: null,
                orderable: false,
                render: DataTable.render.select(),
            },
            {data: 'FullName'},
            {data: 'AideId'},
            {
                data: null,
                render: function (data, type, row) {
                    if (data.Status === true) {
                        return `<div class="tac"><span class="badge badge-sm badge-light-success fs-7 fw-bold">Active</span></div>`;
                    } else {
                        return `<div class="tac"><span class="badge badge-sm badge-light-danger fs-7 fw-bold">Inactive</span></div>`;
                    }
                }
            },
            {data: 'PayrollCode'},
            {data: 'HourlyRate', render: DataTable.render.number(null, null, 2, '$')},
            {
                data: null,
                render: function (data, type, row) {

                    let className;
                    let text;

                    if (data.BenefitCard === false) {
                        className = "red fa-circle-xmark";
                        text = "No";
                    } else {
                        className = "green fa-circle-check";
                        text = "Yes";
                    }

                    return `<span class="hidden-text">${text}</span><div class="icon-bool"><i class="fa-regular ${className}"></i></div>`;
                }
            },
            {data: 'BenefitRate', render: DataTable.render.number(null, null, 2, '$')},
            {data: 'OtRate', render: DataTable.render.number(null, null, 2, '$')},
            {
                data: null,
                render: function (data, type, row) {

                    let className;
                    let text;

                    if (data.Overtime === false) {
                        className = "red fa-circle-xmark";
                        text = "No";
                    } else {
                        className = "green fa-circle-check";
                        text = "Yes";
                    }

                    return `<span class="hidden-text">${text}</span><div class="icon-bool"><i class=" fa-regular ${className}"></i></div>`;
                }
            },
            {data: 'Ssn'},
            {data: 'Address1'},
            {data: 'Address2'},
            {data: 'City'},
            {data: 'State'},
            {data: 'Zip'},
            {data: 'DateOfBirth'},
            {
                data: null,
                render: function (data, type, row) {
                    if (data.Gender === "M" || data.Gender === "m") {
                        return `<div class="tac"><span class="badge badge-sm badge-light-primary fs-7 fw-bold">Male</span></div>`;
                    } else if (data.Gender === "F" || data.Gender === "f") {
                        return `<div class="tac"><span class="badge badge-sm badge-light-warning fs-7 fw-bold">Female</span></div>`;
                    }
                    return null;
                }
            },
            {data: 'Email'},
            {data: 'HomePhone'},
            {data: 'MobilePhone'},
            {data: 'DateOfHire'},
            {data: 'LastUpdated'},
        ],
        layout: {
            top: {
                searchBuilder: {
                    preDefined: {
                        criteria: searchCriteria,
                        logic: 'AND'
                    }
                }
            },
            topStart: {
                buttons: [
                    {extend: 'create', editor: aidesEditor, className: 'btn-dark'},
                    {extend: 'edit', editor: aidesEditor, className: 'btn-warning'},
                    {extend: 'remove', editor: aidesEditor, className: 'btn-danger'}
                ]
            }
        },
        order: [[1, 'asc']],
        select: {
            style: 'os',
            selector: 'td:first-child'
        }
    });

    aidesTable.on( 'init.dt', function () {
        $('.dt-buttons .btn-secondary').removeClass('btn-secondary');
    });

});

