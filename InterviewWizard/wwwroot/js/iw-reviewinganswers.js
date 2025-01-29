const token = localStorage.getItem('iw-token');
fetch('/api/prepareFeedback', {
    method: 'GET',
    headers: {
        'Authorization': `Bearer ${token}`,
    }
})
    .then(response => {
        if (response.status === 200) {
            location.href = '/answerreview/1'
        } else {
            location.href = '/error?err=preparefeedback'
        }
    })
    .catch(error => {
        location.href = '/error?err=preparefeedback'
    });