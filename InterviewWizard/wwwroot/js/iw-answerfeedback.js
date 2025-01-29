document.addEventListener('DOMContentLoaded', function () {
    const tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]')
    const tooltipList = [...tooltipTriggerList].map(tooltipTriggerEl => new bootstrap.Tooltip(tooltipTriggerEl))

    document.getElementById('btnNext').addEventListener('click', function () {
        const nextPageId = document.getElementById('btnNext').getAttribute('data-next-answer');
        const nextUrl = nextPageId == 'review' ? '/epilogue' : `/answerreview/${nextPageId}`;
        location.href = nextUrl;
    });
});