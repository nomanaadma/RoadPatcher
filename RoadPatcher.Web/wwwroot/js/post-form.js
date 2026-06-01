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

            if (!Array.isArray(data) || data.length === 0) {
                $results.html('<div class="alert alert-info mb-0">No potholes detected.</div>');
                return;
            }

            let html = '<div class="table-responsive"><table class="table table-row-bordered align-middle gy-3 gs-0 mb-0">';
            html += '<thead><tr><th>Pothole #</th><th>Area (cm²)</th></tr></thead><tbody>';

            data.forEach(function (item) {
                html += `<tr><td>${item.pothole_number}</td><td>${Number(item.area_cm2).toFixed(1)}</td></tr>`;
            });

            html += '</tbody></table></div>';
            $results.html(html);
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