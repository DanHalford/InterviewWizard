let dropZones = new Map();
var dragCounter = 0;

document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.drop-zone').forEach(ele => {

        dropZones.set(ele.id, ele.innerHTML);

        ele.addEventListener('dragenter', function (event) {
            DropZoneDragEnter(ele, event);
        });

        ele.addEventListener('dragover', function (event) {
            DropZoneDragOver(ele, event)
        });

        ele.addEventListener('drop', function (event) {
            DropZoneDrop(ele, event);
        });

        ele.addEventListener('dragleave', function (event) {
            DropZoneDragLeave(ele, event);
        });

        ele.addEventListener('click', function () {
            DropZoneClick(ele);
        });

        ele.querySelectorAll('.btn').forEach(btn => {
            btn.addEventListener('click', function (event) {
                event.stopPropagation();
            });
        });
            
    });

    const tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]')
    const tooltipList = [...tooltipTriggerList].map(tooltipTriggerEl => new bootstrap.Tooltip(tooltipTriggerEl))

    const pasteModal = document.getElementById('pasteModal');
    if (pasteModal) {
        pasteModal.addEventListener('show.bs.modal', event => {
            const link = event.relatedTarget;
            const linkType = link.getAttribute('data-paste-type');
            switch (linkType) {
                case 'resume':
                    pasteModal.querySelector('.modal-title').innerHTML = 'Paste CV contents';
                    document.getElementById('txtPasteContents').setAttribute('data-paste-type', 'resume');
                    break;
                case 'position':
                    pasteModal.querySelector('.modal-title').innerHTML = 'Paste position description / job advertisement contents';
                    document.getElementById('txtPasteContents').setAttribute('data-paste-type', 'position');
                    break;
            }
        });
    }

    document.getElementById('btnPasteCompleted').addEventListener('click', function (event) {
        const pasteBox = document.getElementById('txtPasteContents');
        const contentType = pasteBox.getAttribute('data-paste-type');
        const content = pasteBox.value;
        pasteBox.value = '';
        pasteBox.removeAttribute('data-paste-type');
        const ele = document.getElementById(`drop_zone_${contentType}`);
        ele.classList.add('drop-zone-processing');
        ele.querySelector('.status-label').classList.add('visually-hidden');
        ele.querySelector('.status-icon').querySelector('i').classList.remove('bi-download');
        ele.querySelector('.status-icon').querySelector('i').classList.add('bi-cloud-arrow-up-fill', 'drop-zone-animation');
        DismissTooltips();
        UploadContent(contentType, content);
    });

    document.getElementById('btnFetchURL').addEventListener('click', function (event) {
        const ele = document.getElementById('drop_zone_position');
        ele.classList.add('drop-zone-processing');
        ele.querySelector('.status-label').classList.add('visually-hidden');
        ele.querySelector('.status-icon').querySelector('i').classList.remove('bi-download');
        ele.querySelector('.status-icon').querySelector('i').classList.add('bi-cloud-arrow-up-fill', 'drop-zone-animation');
        DismissTooltips();
        FetchURL(document.getElementById('txtAdvertURL').value);
    });

    document.getElementById('btnReset').addEventListener('click', function (event) {
        ResetDocumentsForm();
    });

    document.getElementById('btnContinue').addEventListener('click', function (event) {
        location.href = '/preparing-questions';
    });
});

function DropZoneDragEnter(ele, event) {
    dragCounter++;
    ele.classList.add('drop-zone-drag-over');
}

function DropZoneDragOver(ele, event) {
    event.stopPropagation();
    event.preventDefault();
    event.dataTransfer.dropEffect = 'copy';
    ele.classList.add('drop-zone-drag-over');
    ele.querySelector('.status-icon').querySelector('i').classList.remove('bi-download');
    ele.querySelector('.status-icon').querySelector('i').classList.add('bi-cloud-plus-fill');
    ele.querySelector('.status-label').classList.add('visually-hidden');
}

function DropZoneDrop(ele, event) {
    event.stopPropagation();
    event.preventDefault();
    ele.classList.remove('drop-zone-drag-over');
    ele.classList.add('drop-zone-processing');
    ele.querySelector('.status-icon').querySelector('i').classList.remove('bi-cloud-plus-fill');
    ele.querySelector('.status-icon').querySelector('i').classList.add('bi-cloud-arrow-up-fill', 'drop-zone-animation');
    const files = event.dataTransfer.files;
    uploadFile(ele, files);
}

function DropZoneDragLeave(ele, event) {
    event.stopPropagation();
    event.preventDefault();
    dragCounter--;
    if (dragCounter === 0) {
        ele.classList.remove('drop-zone-drag-over');
        ele.querySelector('.status-icon').querySelector('i').classList.remove('bi-cloud-plus-fill');
        ele.querySelector('.status-icon').querySelector('i').classList.add('bi-download');
        ele.querySelector('.status-label').classList.remove('visually-hidden');
    }
}

function DropZoneClick(ele) {
    ele.parentElement.querySelector('input[type="file"]').click();
}

function MarkZoneCompleted(ele) {
    ele.setAttribute('data-document-completed', 'true');
    ele.removeEventListener('dragenter', DropZoneDragEnter);
    ele.removeEventListener('dragover', DropZoneDragOver);
    ele.removeEventListener('drop', DropZoneDrop);
    ele.removeEventListener('dragleave', DropZoneDragLeave);
    ele.removeEventListener('click', DropZoneClick);
    AllowContinue();
}

function uploadFile(ele, files) {

    if (ele.type === 'file') {
        ele = ele.parentElement.querySelector('.drop-zone');
    }

    if (files.length === 0 || files.length > 1) {
        DisplayAlert(ele.parentElement.nextElementSibling.querySelector('.alert-placeholder').id, AlertTypes.Error, 'Please select a single file to upload.');
        return;
    }
    const acceptedFileTypes = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"];
    if (!acceptedFileTypes.includes(files[0].type)) {
        DisplayAlert(ele.parentElement.nextElementSibling.querySelector('.alert-placeholder').id, AlertTypes.Error, 'At present, Interview Wizard only supports Word documents. For other document types, use the <strong>paste contents</strong> option.');
        return;
    }

    const documentType = ele.getAttribute('data-document-type');
    const postTarget = `/api/uploadFile/${documentType}`;

    const formData = new FormData();
    formData.append('file', files[0]);

    const token = localStorage.getItem('iw-token');

    fetch(postTarget, {
        headers: { 'Authorization': `Bearer ${token}` },
        method: 'POST',
        body: formData,
    })
        .then(
            response => {
                switch (response.status) {
                    case 201:
                        ele.parentElement.nextElementSibling.classList.add('visually-hidden');
                        ele.querySelector('.status-label').classList.remove('visually-hidden');
                        ele.classList.remove('drop-zone-drag-over','drop-zone-processing');
                        ele.classList.add('drop-zone-success');
                        ele.querySelector('.status-icon').querySelector('i').className = 'bi bi-check-circle-fill display-6 mx-3 status-icon';
                        ele.querySelector('.status-label').innerHTML = files[0].name;
                        MarkZoneCompleted(ele);
                        gtag('event', 'file_upload', { 'document_type': documentType });
                        break;
                    case 400:
                        ResetDropZone(ele);
                        DisplayAlert(ele.parentElement.nextElementSibling.querySelector('.alert-placeholder').id, AlertTypes.Error, 'An error occurred uploading your file. Please try again.');
                        break;
                    default:
                        ResetDropZone(ele);
                        DisplayAlert(ele.parentElement.nextElementSibling.querySelector('.alert-placeholder').id, AlertTypes.Error, 'An unknown error occurred uploading your file. Please try again.');
                }
            }
        )
        .catch(error => {
            ResetDropZone(ele);
            DisplayAlert(ele.parentElement.nextElementSibling.querySelector('.alert-placeholder').id, AlertTypes.Error, 'An unknown error occurred uploading your file. Please try again.');

        });
}

function ResetDropZone(ele) {
    ele.innerHTML = dropZones.get(ele.id);
    const tooltipTriggerList = ele.querySelectorAll('[data-bs-toggle="tooltip"]')
    const tooltipList = [...tooltipTriggerList].map(tooltipTriggerEl => new bootstrap.Tooltip(tooltipTriggerEl))
}

function UploadContent(contentType, content) {
    const token = localStorage.getItem('iw-token');
    fetch(`/api/upload/${contentType}`, {
        method: 'POST',
        headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ content: content })
    })
        .then(response => {
            if (response.status === 200) {
                gtag('event', 'paste_contents', { 'document_type': contentType });
                return response.json();
            }
        })
        .then(results => {
            switch (contentType) {
                case 'resume':
                    var ele = document.getElementById('drop_zone_resume');
                    // need to handle nulls
                    const candidate = `${results.name} - ${results.title}`;
                    ele.querySelector('.status-label').innerHTML = candidate;
                    break;
                case 'position':
                    var ele = document.getElementById('drop_zone_position');
                    // need to handle nulls
                    const position = `${results.jobTitle} - ${results.advertiser}`;
                    ele.querySelector('.status-label').innerHTML = position;
                    break;
            }
            ele.parentElement.nextElementSibling.classList.add('visually-hidden');
            ele.querySelector('.status-label').classList.remove('visually-hidden');
            ele.classList.remove('drop-zone-drag-over', 'drop-zone-processing');
            ele.classList.add('drop-zone-success');
            ele.querySelector('.status-icon').querySelector('i').className = 'bi bi-check-circle-fill display-6 mx-3 status-icon';
            MarkZoneCompleted(ele);
            EnableTooltips();
        })
        .catch(error => {
            switch (contentType) {
                case 'resume':
                    var ele = document.getElementById('drop_zone_resume');
                    break;
                case 'position':
                    var ele = document.getElementById('drop_zone_position');
                    break;
            }
            ResetDropZone(ele);
            DisplayAlert(ele.parentElement.nextElementSibling.querySelector('.alert-placeholder').id, AlertTypes.Error, 'An unknown error occurred uploading your data. Please try again.');
            EnableTooltips();
        });
    
}

function FetchURL(url) {
    const token = localStorage.getItem('iw-token');
    fetch(`/api/advert`, {
        method: 'POST',
        headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ url: url })
    })
        .then(response => {
            if (response.status === 201) {
                gtag('event', 'fetch_site', { 'document_type': documentType });
                return response.json();
            } else {
                var ele = document.getElementById('drop_zone_position');
                ResetDropZone(ele);
                DisplayAlert(ele.parentElement.nextElementSibling.querySelector('.alert-placeholder').id, AlertTypes.Error, 'An unknown error occurred retrieving data from the address provided. Please try again.');
                EnableTooltips();
            }
        })
        .then(results => {
            var position;
            if (results.content.jobTitle == null || results.content.advertiser == null) {
                let position = results.content.jobTitle ?? results.content.advertiser ?? '';
            } else {
                position = `${results.content.jobTitle} - ${results.content.advertiser}`;
            }
            var ele = document.getElementById('drop_zone_position');
            ele.parentElement.nextElementSibling.classList.add('visually-hidden');
            ele.querySelector('.status-label').classList.remove('visually-hidden');
            ele.classList.remove('drop-zone-drag-over', 'drop-zone-processing');
            ele.classList.add('drop-zone-success');
            ele.querySelector('.status-icon').querySelector('i').className = 'bi bi-check-circle-fill display-6 mx-3 status-icon';
            ele.querySelector('.status-label').innerHTML = position;
            MarkZoneCompleted(ele);
            EnableTooltips();
        })
        .catch(error => {
            var ele = document.getElementById('drop_zone_position');
            ResetDropZone(ele);
            DisplayAlert(ele.parentElement.nextElementSibling.querySelector('.alert-placeholder').id, AlertTypes.Error, 'An unknown error occurred retrieving data from the URL provided. Please try again.');
            EnableTooltips();
        });
}

function DismissTooltips() {
    const tooltipElements = document.querySelectorAll('[data-bs-toggle="tooltip"]');
    tooltipElements.forEach((el) => {
        const bootstrapTooltip = bootstrap.Tooltip.getInstance(el);
        if (bootstrapTooltip) {
            bootstrapTooltip.disable();
        }
    });
}

function EnableTooltips() {
    const tooltipElements = document.querySelectorAll('[data-bs-toggle="tooltip"]');
    tooltipElements.forEach((el) => {
        const bootstrapTooltip = bootstrap.Tooltip.getInstance(el);
        if (bootstrapTooltip) {
            bootstrapTooltip.enable();
        }
    });
}

function ResetDocumentsForm() {
    document.getElementById('btnReset').classList.add('disabled');
    document.getElementById('btnReset').innerHTML = "<span class=\"spinner-border spinner-border-sm\" aria-hidden=\"true\"></span><span class=\"visually-hidden\" role=\"status\"> Reset</span>";
    const token = localStorage.getItem('iw-token');
    fetch('/api/reset', {
        method: 'POST',
        headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
        }
    })
        .then(response => {
            if (response.status === 200) {
                window.location.reload();
            }
        })
        .catch(error => {
            DisplayAlert('alert-placeholder', AlertTypes.Error, 'An unknown error occurred resetting the form. Please try again.');
            document.getElementById('btnReset').classList.remove('disabled');
            document.getElementById('btnReset').innerHTML = '<i class="bi bi-arrow-counterclockwise"></i> Reset';
        });
}

function AllowContinue() {
    let allowContinue = true;
    document.querySelectorAll('.drop-zone').forEach(ele => {
        if (ele.getAttribute('data-document-completed') !== 'true') {
            allowContinue = false;
        }
    });
    if (allowContinue) {
        document.getElementById('btnContinue').classList.remove('disabled');
    }
}