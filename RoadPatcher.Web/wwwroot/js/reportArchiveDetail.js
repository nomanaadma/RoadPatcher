$(document).ready(function () {
    
    const reportArchiveDetailsTable = $('#reportArchiveDetails').DataTable({
        ajax: {
            url: '/api/report_archive_details',
            data: {
                reportArchiveId: reportArchiveId
            }
        },
        scrollX: true,
        pageLength: 13,
        lengthChange: false,
        columns: [
            {
                className: 'dt-control',
                orderable: false,
                data: null,
                defaultContent: ''
            },
            {data: 'aideName'},
            {data: 'aideId'},
            {data: 'payrollCode'},
            {data: 'hours'},
            {data: 'hoursOt'},
            {data: 'benefitHours'},
            {data: 'holidayHours'},
            {
                data: 'liveIn',
                render: function (data) {
                    return (data === 0) ? null : data;
                }
            },
        ],
        order: [[1, 'asc']],
        layout: {
            top: 'searchBuilder',
        },
    });
    
    const payroll_export_btn = $('.payroll_export');

    reportArchiveDetailsTable.on('xhr', function() {
        payroll_export_btn.removeAttr('disabled');
        setTimeout(function () {
            reportArchiveDetailsTable.columns.adjust().draw();  // Adjust columns and redraw
        }, 500);
    });

    function getReportArchiveData() {
        const data = [];

        reportArchiveDetailsTable.rows().every(function() {
            data.push(this.data())
        });

        return data;
    }
    
    
    const exported_div = $('.exported_div');

    payroll_export_btn.on('click', async function() {

        let process = true;
        
        const dataExported = exported_div.attr("data-exported");
        
        if(
            dataExported !== "" &&
            dataExported !== undefined
        ) {

            const result = await Swal.fire({
                icon: 'warning',
                title: "Report is already Exported. Do you want to override it?",
                showDenyButton: true,
                confirmButtonText: 'Yes',
                denyButtonText: 'No',
                allowOutsideClick: false
            })

            if (result.isDenied) {
                process = false;
            }
            
        }

        if(process === false) return;

        
        Swal.fire({
            title: "<h1>Generating...</h1>",
            html: "Please Wait",
            allowOutsideClick: false,
            didOpen: () => {
                Swal.showLoading();
            },
        });
        
        axios.post('/api/payroll_export_generate', {
            ReportArchiveId: $("#ReportArchiveId").val(),
            Hourly: parseFloat($("#hourly").val()),
            Benefit: parseFloat($("#benefit").val()),
            HourlyOt: parseFloat($("#hourlyot").val()),
            Period: $("#Period").val().replaceAll('/', '.'),
            DisplayArchives: getReportArchiveData()
        }).then(( { data: exportResponse } ) => {
            
            exported_div.attr('data-exported', exportResponse);
            
            Swal.fire({
                title: "Export Generated",
                html: exported_div.prop('outerHTML'),
                icon: "success",
                allowOutsideClick: false,
            })
            
        }).catch( () => {
            
            Swal.fire({
                icon: "error",
                title: "Error",
                html: `
                    <p>To Generate report all aides should have payroll codes.</p>
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
            });
            
            
        });
        
    });
    

    // Add event listener for opening and closing details
    reportArchiveDetailsTable.on('click', 'td.dt-control', function (e) {
        
        const tr = e.target.closest('tr');
        const row = reportArchiveDetailsTable.row(tr);

        if (row.child.isShown()) {
            // This row is already open - close it
            row.child.hide();
        }
        else {
            // Open this row
            row.child(format(row.data())).show();
        }
    });

    function format(d) {

        const data = d.archivePatients;
        
        let rowspan = 1;
        let prevPatient = {
            name: '',
            index: 0
        }

        let index = 0;

        for (const patient of data) {

            if(prevPatient.name === patient.patientName) {
                delete patient.patientName;
                data[prevPatient.index].rowspan = ++rowspan;
            } else {
                prevPatient.name = patient.patientName;
                prevPatient.index = index;
                rowspan = 1;
            }
            
            index++;

        }
        
        let patientsData = '';

        console.log(data)
        
        for ( const { patientName, rowspan, visitDate, hours, liveIn, night } of data) {
            
            let patientNameTd = '';

            console.log( patientName )
            
            if(patientName !== undefined) {
                patientNameTd = `<td rowspan="${rowspan}">${patientName}</td>`
            }
            
            patientsData += `
                <tr>
                    ${patientNameTd}
                    <td>${moment(visitDate).format('MM/DD/YYYY')}</td>
                    <td>${hours}</td>
                    <td>${liveIn === 0 ? '' : liveIn}</td>
                    <td>${night === 0 ? '' : night}</td>
                </tr>
            `;
        }
        
        return `<table class="padding-table display table table-bordered table-striped p-10 child-table">
             <thead>
                 <tr>
                     <th>Patient Name</th>
                     <th>Visit Date</th>
                     <th>Hours</th>
                     <th>LiveIn</th>
                     <th>Night</th>
                 </tr>
             </thead>
             <tbody>
                ${patientsData}
             </tbody>
         </table>`;
    }
    
});