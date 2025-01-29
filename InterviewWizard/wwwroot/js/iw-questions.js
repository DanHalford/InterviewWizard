const token = localStorage.getItem('iw-token');

document.addEventListener('DOMContentLoaded', function () {
    const tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]')
    const tooltipList = [...tooltipTriggerList].map(tooltipTriggerEl => new bootstrap.Tooltip(tooltipTriggerEl))

    document.getElementById('btnSaveAnswer').addEventListener('click', function () {
        document.getElementById('btnSaveAnswer').classList.add('d-none');
        document.getElementById('btnSaving').classList.remove('d-none');
        var answer = document.getElementById('txtAnswer').value;
        var questionId = this.getAttribute('data-question-number');
        fetch(`/api/questions/${questionId}/answer`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                questionId: questionId,
                answer: answer
            })
        })
            .then(response => {
                if (response.status === 200) {
                    gtag('event', 'question_answered', { 'question_number': questionId });
                    if (questionId < 3) {
                        location.href = `/questions/${parseInt(questionId) + 1}`;
                    } else {
                        location.href = '/reviewing-answers';
                    }
                } else {
                    document.getElementById('btnSaveAnswer').classList.remove('d-none');
                    document.getElementById('btnSaving').classList.add('d-none');
                    location.href = '/error?err=preparefeedback'
                }
            }
        );
    });
});