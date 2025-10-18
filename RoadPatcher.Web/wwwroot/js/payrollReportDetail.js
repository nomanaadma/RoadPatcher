$(document).ready(function () {
    
    // Payroll Report Details Table
    const payrollReportDetailsEditor = new DataTable.Editor({
        ajax: '/api/payroll_report_details',
        fields: [
            {
                label: 'PayType:',
                name: 'PayType',
                type: 'radio',
                options: [
                    {label: 'Zero', value: 'Zero'},
                    {label: 'LiveIn', value: 'LiveIn'},
                    {label: 'Hours', value: 'Hours'},
                    {label: 'Night', value: 'Night'},
                ]
            }
        ],
        table: '#payrollReportDetails'
    });

    const payrollReportDetailsTable = $('#payrollReportDetails').DataTable({
        ajax: {
            url: '/api/payroll_report_details',
            data: {
                payrollReportId: payrollReportId
            }
        },
        scrollX: true,
        scrollY: 500,
        deferRender: true,
        scroller: true,
        columnDefs: [
            {
                target: 0,
                visible: false,
                searchable: false
            }
        ],
        searchDelay: 600,
        layout: {
            top: 'searchBuilder',
        },
        columns: [
            {data: 'id'},
            {
                data: 'AideNameCode',
                render: (data) => {
                    return `<span class="matchable-col">${data}</span>`;
                },
            },
            {
                data: 'WeekDate',
                render: (data) => {
                    return `<span class="matchable-col">${data}</span>`;
                },
            },
            {data: 'PatientName'},
            {
                data: 'AdmId',
                render: (data) => {
                    return `<span class="matchable-col">${data}</span>`;
                },
            },
            {
                data: 'Coordntr',
                render: (data) => {
                    return `<span class="matchable-col">${data}</span>`;
                },
            },
            {data: 'VisitDate'},
            {data: 'VisitTime'},
            {data: 'Type'},
            {data: 'ServCode'},
            {data: 'Rebill'},
            {data: 'PayHrs'},
            {data: 'PayRate', render: DataTable.render.number(null, null, 2, '$')},
            {
                data: 'PayAmt',
                render: function (data, type, row) {

                    const formattedNumber = $.fn.dataTable.render.number(null, null, 2, '$').display(data);

                    if (data < 0) {
                        return '<span class="negative">' + formattedNumber + '</span>';
                    } else if (data > 0) {
                        return formattedNumber;
                    } else {
                        return '';
                    }
                }
            },
            {data: 'OtHrs'},
            {
                data: 'OtRate',
                render: function (data, type, row) {

                    const formattedNumber = $.fn.dataTable.render.number(null, null, 2, '$').display(data);

                    if (data > 0) {
                        return formattedNumber;
                    } else {
                        return '';
                    }
                }
            },
            {
                data: 'OtAmt',
                render: function (data, type, row) {

                    const formattedNumber = $.fn.dataTable.render.number(null, null, 2, '$').display(data);

                    if (data < 0) {
                        return '<span class="negative">' + formattedNumber + '</span>';
                    } else if (data > 0) {
                        return formattedNumber;
                    } else {
                        return '';
                    }
                }
            },
            {
                data: 'TotalAmt',
                render: function (data, type, row) {

                    const formattedNumber = $.fn.dataTable.render.number(null, null, 2, '$').display(data);

                    if (data < 0) {
                        return '<span class="negative">' + formattedNumber + '</span>';
                    } else {
                        return formattedNumber;
                    }
                }
            },
            {data: 'Alert'},
            {data: 'PayType'},
            {data: 'PaidBefore'},
        ],
    });

    const tableDataset = payrollReportDetailsTable.table().node().dataset;

    // Adding Export to Excel Button
    new $.fn.dataTable.Buttons(payrollReportDetailsTable, {
        buttons: [
            {
                extend: 'excelHtml5',
                text: 'Export to Excel',
                title: '',
                exportOptions: {
                    columns: ':visible'
                },
                filename: tableDataset.title.replaceAll('/', '-')
            }
        ]
    });

    payrollReportDetailsTable.buttons().container().prependTo('#payrollReportDetails_wrapper .row.mt-2.justify-content-between:eq(0) .col-md-auto.me-auto');

    // loading overlay
    const payrollReportDetailsLoader = $('.grid-loading');
    
    // Remove All rowspans
    payrollReportDetailsTable.on('preDraw.dt', function () {

        payrollReportDetailsLoader.removeClass('d-none');

        payrollReportDetailsTable.rows().every(function () {

            const rowNode = this.node();
            if (rowNode === null) return;

            const currentRow = $(rowNode);

            currentRow.find('[rowspan]').removeAttr('rowspan').removeClass("text-shadow");

            currentRow.find('.d-none').removeClass("d-none");

        });

    });
    
    const unprocessedCell = $(".unprocessed-cells-count");
    const unprocessedCellCount = unprocessedCell.find('b');

    // adding rowspans
    payrollReportDetailsTable.on('draw.dt', function () {

        let unprocess = 0;
        
        setTimeout(function () {

            payrollReportDetailsTable.rows().every(function () {

                const rowNode = this.node();

                if (rowNode === null) return;

                let currentRow = $(rowNode);

                let rowData = this.data();
                
                if (rowData.Alert !== "" && rowData.PayType === null) {

                    unprocess++;
                    
                    unprocessedCellCount.text(unprocess);
                    
                    currentRow.find("td:nth-last-child(2)").addClass('unprocessed');
                    
                }

                if (rowData.Alert === "dupl") {
                    currentRow.find("td:nth-last-child(2)").addClass('editable');
                }

                let matcheableEls = currentRow.find('.matchable-col:visible');

                for (let element of matcheableEls) {

                    let row = currentRow;

                    const colData = element.innerText;

                    let rowspan = 1;

                    while (true) {

                        row = row.next();

                        const nextRowMatcheableEl = row.find(`.matchable-col:contains("${colData}")`);

                        if (nextRowMatcheableEl.length === 0) break;

                        nextRowMatcheableEl.parent('td').addClass('d-none');

                        rowspan++;
                    }

                    const parentMatchingTd = currentRow.find(`.matchable-col:contains("${colData}")`)
                        .parent('td')
                        .attr('rowspan', rowspan)

                    if (rowspan !== 1) {
                        parentMatchingTd.addClass('text-shadow')
                    }

                }

            });
            
            if(unprocess === 0 ) {
                unprocessedCell.remove();
            }

            payrollReportDetailsLoader.addClass('d-none');

        }, 250);

    });
    
    // allow to edit only unprocessed cells
    payrollReportDetailsTable.on('click', 'tbody td.unprocessed, tbody td.editable', function (e) {
        payrollReportDetailsEditor.bubble(this);
    });
    
    // alert legend topbar info
    $('.alert-legend').click(function() {

        Swal.fire({
            title: "Alert Legend",
            html: `
                <p>1. TotalAmt <= 0 </p>
                <p>2. Live-In Service < 13 hours </p>
                3. Hours service > 14 hours
            `,
            icon: "info"
        });
        
    });
    
    const saveBtn = $('.save_payroll_report_detail');

    payrollReportDetailsTable.on('xhr', function() {
        saveBtn.removeAttr('disabled');
    });

    saveBtn.on('click', async function() {
        
        let process = false;
        
        if(tableDataset.status === 'processed') {
            
            const result = await Swal.fire({
                icon: 'warning',
                title: "Report is Processed already. Do you want to override it?",
                showDenyButton: true,
                confirmButtonText: 'View',
                denyButtonText: 'Yes',
                cancelButtonText: 'No',
                showCancelButton: true,
                allowOutsideClick: false
            })
            
            if (result.isConfirmed) {
                window.location.href = '/ReportArchives/' + tableDataset.reportarchiveid;
            } else if (result.isDenied) {
                process = true;
            }
            
        } else {
            process = true;
        }

        if(process === false) return;
        

        const unprocessed = unprocessedCellCount.text()
        
        const selector = unprocessedCell.attr('class').replaceAll(' ', '.');
        
        if($('.'+selector).length !== 0) {

            Swal.fire({
                title: "Saving Stopped",
                html: `
                    <p><b>${unprocessed}</b> Cells are unprocessed!</p>
                    You did not finish processing PayType cells (pink)
                `,
                icon: "error"
            });

            return;

        }

        Swal.fire({
            title: "<h1>Saving...</h1>",
            html: "Please Wait",
            allowOutsideClick: false,
            didOpen: () => {
                Swal.showLoading();
            },
        });

        let aidesJson = [];
        let patientsJson = [];
        
        let archiveRequest = {
            WeekDate: tableDataset.week,
            PayrollReportId: tableDataset.id,
            ReportDetails: []
        }
        
        payrollReportDetailsTable.rows().every(function () {

            const data = this.data();
            
            const [NewName, Code] = data.AideNameCode.split(' : ');

            aidesJson.push({
                NewName,
                Code
            });

            patientsJson.push({
                AdmId: data.AdmId,
                Name: data.PatientName
            });

            archiveRequest.ReportDetails.push({
                AideCodeName: data.AideNameCode,
                AideId: Code,
                AdmId: data.AdmId,
                VstDt: data.VisitDate,
                VisitTime: data.VisitTime,
                TotalAmt: data.TotalAmt,
                PayHrs: data.PayHrs,
                OtHrs: data.OtHrs,
                Type: data.Type,
                PayType: data.PayType,
            });
    
        });

        archiveRequest.ReportDetails.sort((a, b) => {
            const aAideCodeName = a.AideCodeName.toLowerCase();
            const bAideCodeName = b.AideCodeName.toLowerCase();
            if (aAideCodeName < bAideCodeName) {
                return -1;
            }
            if (aAideCodeName > bAideCodeName) {
                return 1;
            }
            return 0;
        });

        aidesJson = aidesJson.filter((item, index, self) =>
                index === self.findIndex((t) => (
                    t.NewName === item.NewName && t.Code === item.Code
                ))
        );

        patientsJson = patientsJson.filter((item, index, self) =>
                index === self.findIndex((t) => (
                    t.AdmId === item.AdmId && t.Name === item.Name
                ))
        );

        saveData();

        async function saveData() {

            const { data: aidesResponse } = await axios.post('/api/aidenamecode', aidesJson);
            await axios.post('/api/patients_save', patientsJson);
            const { data: reportArchiveId } = await axios.post('/api/report_save', archiveRequest);

            if (aidesResponse === false) {
                Swal.fire({
                    icon: "warning",
                    title: "Warning",
                    html: `
                        <p>To create the final report for payroll all aides should have payroll codes.</p>
                        WILL YOU ENTER PAYROLL CODES NOW?
                    `,
                    allowOutsideClick: false,
                    showDenyButton: true,
                    confirmButtonText: "Yes",
                    denyButtonText: 'No',
                }).then((result) => {
                    if (result.isConfirmed) {
                        window.open('/Aides?PayrollCodeNull', '_blank');
                    }

                    reportGenerated(reportArchiveId);
                });
                
            } else {

                reportGenerated(reportArchiveId);
                
            }
        }
        
    });

    function reportGenerated(reportId) {
        Swal.fire({
            title: "Report Generated",
            text: "You will be redirected to report archive",
            icon: "success",
            allowOutsideClick: false,
        }).then( () => {
            window.location.href = '/ReportArchives/'+reportId;
        })
    }

    
    
});