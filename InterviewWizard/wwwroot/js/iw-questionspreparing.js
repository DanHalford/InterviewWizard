const token = localStorage.getItem('iw-token');
fetch('/api/questions', {
    method: 'GET',
    headers: {
        'Authorization': `Bearer ${token}`,
    }
})
    .then(async response => {
        if (response.status === 200) {
            location.href = '/questions/1'
        } else if (response.status === 503) {
            const message = await response.text();
            location.href = `/error?err=${message.toLowerCase()}`;
        }
    })
    .catch(error => {
        location.href = '/error?err=preparequestions'
    });