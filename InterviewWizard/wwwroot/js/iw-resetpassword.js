document.addEventListener('DOMContentLoaded', function () {
    document.getElementById('cmdSubmit').addEventListener('click', function () {
        document.getElementById('cmdSubmit').classList.add('d-none');
        document.getElementById('cmdWait').classList.remove('d-none');
        var email = document.getElementById('EmailAddress').value;
        fetch('/auth/reset', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ Email: email })
        })
            .then(response => {
                if (response.status === 200) {
                    document.getElementById('lblStatus').innerHTML = 'If your email address is registered with us, an email has been sent to you with instructions on how to reset your password.';
                    document.getElementById('lblStatus').classList.remove('d-none');
                    document.getElementById('cmdSubmit').classList.remove('d-none');
                    document.getElementById('cmdWait').classList.add('d-none');
                } else {
                    document.getElementById('cmdSubmit').classList.remove('d-none');
                    document.getElementById('cmdWait').classList.add('d-none');
                    document.location.href = '/error?err=resetpassword';
                }
            })
    });
});