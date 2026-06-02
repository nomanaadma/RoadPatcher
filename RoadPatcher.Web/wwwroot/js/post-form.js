toastr.options = {
    "positionClass": "toastr-bottom-right",
};

const submitButton = document.getElementById('submitBtn');
let uploadQueue = [];
function removeQueue(id) {
    const index = uploadQueue.indexOf(id);
    if (index > -1) uploadQueue.splice(index, 1);
    if (uploadQueue.length === 0) submitButton.disabled = false;
}

document.addEventListener('DOMContentLoaded', function () {

    uppyImage = uppyInstance('#uppy-image', 'Image', ['image/*'], 1, '#ImageId');

    uppyVideo = uppyInstance('#uppy-video', 'Video', ['video/*'], 1, '#VideoId');

    const completionTarget = document.querySelector('#uppy-completion');
    if (completionTarget) {
        uppyCompletion = uppyInstance('#uppy-completion', 'Completion Image', ['image/*'], 1, '#CompletionImageId');
    }
    
    uppyImages = uppyInstance('#uppy-images', 'Images', ['image/*'], 10, null);
    
    function uppyInstance(targetId, msg, allowedFileTypes, maxNumberOfFiles, el) {

        const uppy = new Uppy.Uppy({
            // Core restrictions for single image
            restrictions: {
                maxNumberOfFiles: maxNumberOfFiles,
                allowedFileTypes: allowedFileTypes
            },
            autoProceed: true // Set to true if you want upload to start immediately upon selection
        })
            .use(Uppy.Dashboard, {
                inline: true,
                target: targetId,
                proudlyDisplayPoweredByUppy: false,
                note: `${msg} only, ${maxNumberOfFiles} file max`,
                showRemoveButtonAfterComplete: true,
                doneButtonHandler: null,
            })
            .use(Uppy.XHRUpload, {
                endpoint: '/Posts/Upload', // The backend route we will create
                fieldName: 'file',             // Must match the parameter name in the Controller
                formData: true,
                // Header for Anti-Forgery Token (Optional but recommended)
                headers: {
                    'RequestVerificationToken': document.getElementsByName("__RequestVerificationToken")[0]?.value
                }
            });

        uppy.on('upload-started', (file, error, response) => {
            if (!uploadQueue.includes(file.id)) {
                uploadQueue.push(file.id);
            }
            submitButton.disabled = true;
        });

        uppy.on('upload-error', (file, error, response) => {
            uppy.info(response?.body?.error || error.message, 'error', 1000);
            removeQueue(file.id);
        });

        // Event: Successful Upload
        uppy.on('upload-success', (file, response) => {

            if (el === '#VideoId') {

                const dashboardEl = document.getElementById('uppy_'+file.id);

                if (!dashboardEl) return;

                const previewEl = dashboardEl.querySelector('.uppy-Dashboard-Item-previewInnerWrap');

                setTimeout(function() {
                    
                    if (previewEl) previewEl.remove();

                    const videoEl = document.createElement('video');
                    videoEl.src = URL.createObjectURL(file.data); // preview video
                    videoEl.controls = true;
                    videoEl.preload = "none";

                    // Append the video to the dashboard item
                    dashboardEl.querySelector('.uppy-Dashboard-Item-preview').appendChild(videoEl);
                    
                }, 100);
                
            }

            removeQueue(file.id);

            const id = response?.body?.id;

            if(id === undefined) return;

            if(el != null) {
                $(el).val(id);
            } else {
                $(`<input name="MediaFilesIds[]" class="media-image" type="hidden"  value="${id}" />`)
                    .appendTo('.mediafiles-row');
            }

        });

        uppy.on('file-removed', (file) => {

            let fileId = file.response.body.id;

            if(fileId) {
                // If the file was uploade one, get the ID from response and delete it 
                axios.delete(`/Posts/DeleteFile/${fileId}`);
            } else {
                // If the file was preloaded, get the ID from meta
                fileId = file.meta.id;
            }

            $(`.media-image[value="${fileId}"]`).remove();
            $(`[value="${fileId}"]`).val('');

        });

        return uppy;

    }

});

$(document).ready(function () {

    function recalculateInferenceCosts() {
        const asphaltCostPerIn3 = parseFloat($('#asphaltCostPerIn3').val()) || 0;
        const servicePercent = parseFloat($('#serviceChargesPercent').val()) || 10;
        let baseCostTotal = 0;

        $('#inferenceResults .pothole-row').each(function () {
            const $row = $(this);
            const areaCm2 = parseFloat($row.find('.pothole-area-input').val()) || 0;
            const areaIn2 = areaCm2 / 6.4516;
            const depthIn = parseFloat($row.find('.pothole-depth-input').val()) || 0;
            const volumeIn3 = areaIn2 * depthIn;
            const potholeCost = volumeIn3 * asphaltCostPerIn3;

            $row.find('.pothole-volume-cell').text(volumeIn3.toFixed(2));
            $row.find('.pothole-cost-cell').text(potholeCost.toFixed(2));
            baseCostTotal += potholeCost;
        });

        const serviceChargeAmount = baseCostTotal * (servicePercent / 100);
        const grandTotal = baseCostTotal + serviceChargeAmount;

        $('#inferenceBaseCost').text(baseCostTotal.toFixed(2));
        $('#inferenceServiceChargeAmount').text(serviceChargeAmount.toFixed(2));
        $('#inferenceTotalCost').text(grandTotal.toFixed(2));

        const costValue = grandTotal.toFixed(2);
        const $costInputs = $('#Cost, input[name="Cost"]');
        if ($costInputs.length) {
            $costInputs.val(costValue);
            $costInputs.first().trigger('input');
        }
    }

    const quill = new Quill('#editor', {
        theme: 'snow'
    });
    
    const description = $('#Description'); 

    quill.root.innerHTML = description.val();
    
    quill.on('text-change', () => {
        description.val(quill.root.innerHTML);
    });

    $(document).on('input', '#confidenceThreshold', function () {
        $('.confidence-threshold-value').text(Number($(this).val()).toFixed(2));
    });

    $(document).on('input', '#asphaltCostPerIn3, #serviceChargesPercent, .pothole-depth-input, .pothole-area-input', function () {
        recalculateInferenceCosts();
    });

    $(document).on('click', '#runInferenceBtn', function () {
        const $btn = $(this);
        const imageId = $('#ImageId').val();
        const cameraHeightFromGround = $('#CameraHeightFromGround').val();
        const cameraToCenterDistance = $('#CameraToCenterDistance').val();
        const confidenceThreshold = $('#confidenceThreshold').val();
        const $results = $('#inferenceResults');

        if (!imageId) {
            toastr.error('Please upload an image first.');
            return;
        }

        if (!cameraHeightFromGround || !cameraToCenterDistance) {
            toastr.error('Please fill the required measurement fields first.');
            return;
        }

        const token = $('input[name="__RequestVerificationToken"]').first().val();

        $btn.prop('disabled', true).text('Running...');
        $results.removeClass('d-none').html('<div class="text-center text-muted">Running inference...</div>');

        $.ajax({
            url: '/Posts/RunInference',
            method: 'POST',
            headers: token ? { 'RequestVerificationToken': token } : {},
            data: {
                imageId: imageId,
                cameraHeightFromGround: cameraHeightFromGround,
                cameraToCenterDistance: cameraToCenterDistance,
                confidenceThreshold: confidenceThreshold
            }
        })
        .done(function (response) {
            const data = typeof response === 'string' ? JSON.parse(response) : response;
            const potholes = data.potholes || data;
            const processedImageUrl = data.processed_image_url;

            let html = '';

            if (processedImageUrl) {

                const cacheBustedUrl = processedImageUrl + "?t=" + new Date().getTime();

                html += `
                    <div class="mb-4">
                        <h6 class="fw-bold mb-3">Processed Image</h6>
                        <img src="${cacheBustedUrl}" alt="Processed pothole inference result" class="img-fluid rounded border" />
                    </div>
                `;
            }

            if (!Array.isArray(potholes) || potholes.length === 0) {
                html += '<div class="alert alert-info mb-0">No potholes detected.</div>';
                $results.html(html);
                return;
            }

            html += `
                <div class="severity-table table-responsive mb-4">
                   <table class="table table-sm table-bordered align-middle mb-0">
                        <thead class="table-light">
                            <tr><th colspan="5">Severity Tiers</th></tr>
                            <tr>
                                <th>Severity Tiers</th>
                                <th>Depth</th>
                                <th>Impact</th>
                                <th>Material</th>
                                <th>Strategy</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr>
                                <td>Minor</td>
                                <td>1 inch</td>
                                <td>Shallow surface damage with minimal impact on vehicles.</td>
                                <td>Cold Mix Asphalt (Ready Patch)</td>
                                <td>Surface level skim and seal. Cures by solvent evaporation.</td>
                            </tr>
                            <tr>
                                <td>Standard</td>
                                <td>2 inches</td>
                                <td>Typical pothole requiring routine maintenance.</td>
                                <td>Hot Mix Asphalt (HMA)</td>
                                <td>Single-layer structural patch. Needs mechanical compaction.</td>
                            </tr>
                            <tr>
                                <td>Moderate</td>
                                <td>3 inches</td>
                                <td>Noticeable road damage that may affect ride quality.</td>
                                <td>Hot Mix Asphalt (HMA)</td>
                                <td>Thick single-layer patch, ideal structural durability.</td>
                            </tr>
                            <tr>
                                <td>Severe</td>
                                <td>4 inches</td>
                                <td>Deep pothole posing a significant risk to vehicles.</td>
                                <td>Crushed Aggregate Base + HMA Capping</td>
                                <td>Top 2" Hot Mix Asphalt cap + Bottom 2" Sub-base stone base.</td>
                            </tr>
                            <tr>
                                <td>Critical</td>
                                <td>5+ inches</td>
                                <td>Major road hazard requiring urgent repair.</td>
                                <td>Crushed Aggregate Base + HMA Capping</td>
                                <td>Top 2" Hot Mix Asphalt cap + Bottom 3+" Sub-base stone base.</td>
                            </tr>
                        </tbody>
                    </table>
                </div>

                <div class="mb-4">
                    <label for="asphaltCostPerIn3" class="form-label fw-semibold">Asphalt Cost Per Cubic Inch</label>
                    <input id="asphaltCostPerIn3" type="number" min="0" step="0.01" class="form-control form-control-solid" placeholder="Enter asphalt cost per in³" />
                </div>
            `;

            html += '<div class="table-responsive"><table class="table table-row-bordered align-middle gy-3 gs-0 mb-0">';
            html += '<thead><tr><th>Pothole #</th><th>Area (in²)</th><th>Depth (in)</th><th>Volume (in³)</th><th>Cost</th></tr></thead><tbody>';

            potholes.forEach(function (item) {
                const areaCm2 = Number(item.area_cm2) || 0;
                const areaIn2 = areaCm2 / 6.4516;
                html += `
                    <tr class="pothole-row">
                        <td>${item.pothole_number}</td>
                        <td>
                            <input type="number" min="0" step="0.01" class="form-control form-control-sm pothole-area-input" value="${areaIn2.toFixed(2)}" />
                        </td>
                        <td>
                            <input type="number" min="0" step="0.01" class="form-control form-control-sm pothole-depth-input" placeholder="e.g. 1.5" />
                        </td>
                        <td class="pothole-volume-cell">0.00</td>
                        <td class="pothole-cost-cell">0.00</td>
                    </tr>
                `;
            });

            html += '</tbody>';
            html += '<tfoot><tr><th colspan="4" class="text-end">Cost</th><th id="inferenceBaseCost">0.00</th></tr>';
            html += '<tr><th colspan="3" class="text-end">Service Charges (%)</th><th colspan="2"><input id="serviceChargesPercent" type="number" min="0" step="0.1" value="10" class="form-control form-control-sm" /></th></tr>';
            html += '<tr><th colspan="4" class="text-end">Service Charges Amount</th><th id="inferenceServiceChargeAmount">0.00</th></tr>';
            html += '<tr><th colspan="4" class="text-end">Total Cost</th><th id="inferenceTotalCost">0.00</th></tr></tfoot>';
            html += '</table></div>';
            $results.html(html);

            recalculateInferenceCosts();
        })
        .fail(function (xhr) {
            const message = xhr.responseJSON?.error || xhr.responseText || 'Inference failed.';
            $results.html(`<div class="alert alert-danger mb-0">${message}</div>`);
        })
        .always(function () {
            $btn.prop('disabled', false).text('Run Inference');
        });
    });
    
});