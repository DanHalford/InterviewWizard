document.addEventListener('DOMContentLoaded', function () {

    document.querySelectorAll('input.form-control').forEach(function (el) {
        el.addEventListener('keydown', function (e) {
            if (e.key == 'Enter') {
                document.getElementById('cmdSubmit').click();
            }
        });
    });
    document.getElementById('cmdSubmit').addEventListener('click', function () {
        document.getElementById('cmdSubmit').classList.add('d-none');
        document.getElementById('cmdWait').classList.remove('d-none');
        var email = document.getElementById('txtEmailAddress').value;
        var password = document.getElementById('txtPassword').value;

        fetch('/auth/login', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ Email: email, Password: password })
        })
            .then(response => {
                if (response.status === 200) {
                    var token = response.headers.get('Authorization');
                    localStorage.setItem('iw-token', token);
                    location.href = '/documents';
                } else {
                    document.getElementById('cmdSubmit').classList.remove('d-none');
                    document.getElementById('cmdWait').classList.add('d-none');
                    return response.json();
                }
            }).then(data => {
                switch (data.message) {
                    case 'NOTVERIFIED':
                        document.getElementById('lblStatusAlert').innerHTML = 'Your credentials are correct but your account is not verified. Please check your email for the verification message from <strong>hello@interviewwizard.ai</strong>.';
                        break;
                    case 'INVALIDLOGIN':
                        document.getElementById('lblStatusAlert').innerHTML = 'Your email address and password were not recognised.';
                        break;
                    default:
                        document.getElementById('lblStatusAlert').innerHTML = 'An error occurred trying to sign in. Please try again. If the error persists, <a href="/contact?subject=Sign In Error" class="link-danger" aria-label="Contact form">let us know</a>.';
                        break;
                }
                document.getElementById('lblStatusAlert').classList.remove('d-none');
            })
    });
});