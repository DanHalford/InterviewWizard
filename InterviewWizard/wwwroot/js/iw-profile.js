const token = localStorage.getItem('iw-token');

document.addEventListener('DOMContentLoaded', function () {
    document.getElementById('cmdSaveEmail').addEventListener('click', UpdateEmail);
    document.getElementById('cmdSavePassword').addEventListener('click', UpdatePassword);
    document.getElementById('cmdSuccessEmail').addEventListener('click', function () {
        location.reload();
    });
    document.getElementById('txtEditPassword').addEventListener('blur', function () {
        CheckPasswordLength();
    });
    document.getElementById('txtConfirmPassword').addEventListener('blur', function () {
        CheckPasswordsMatch();
    });
});

async function UpdateEmail() {
    document.getElementById('cmdSaveEmail').classList.add('d-none');
    document.getElementById('cmdWaitEmail').classList.remove('d-none');

    var response = await ValidateEmail(document.getElementById('txtEditEmail'));
    if (response !== undefined) {
        DisplayAlert('lblEmailAlert', response);
        document.getElementById('cmdSaveEmail').classList.remove('d-none');
        document.getElementById('cmdWaitEmail').classList.add('d-none');
        return;
    }
    
    fetch('/api/updateEmail', {
        method: 'POST',
        headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({
            email: document.getElementById('txtEditEmail').value,
            password: 'placeholder'
        })
    }).then(response => {
        if (response.status === 200) {
            document.getElementById('cmdWaitEmail').classList.add('d-none');
            document.getElementById('cmdSuccessEmail').classList.remove('d-none');
            setTimeout(function () { document.getElementById('cmdSuccessEmail').click() }, 3000);
        } else {
            document.getElementById('cmdSaveEmail').classList.remove('d-none');
            document.getElementById('cmdWaitEmail').classList.add('d-none');
            DisplayAlert('lblEmailAlert', 'An error occurred trying to update your email address. Please try again. If the error persists, <a href="/contact?subject=Email Update Error" class="link-danger" aria-label="Contact form">let us know</a>.');
        }
    });
}

function UpdatePassword() {
    if (CheckPasswordLength() && CheckPasswordsMatch()) {
        document.getElementById('cmdSavePassword').classList.add('d-none');
        document.getElementById('cmdWaitPassword').classList.remove('d-none');
        fetch('/api/updatePassword', {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                email: 'email@foo.bar',
                password: document.getElementById('txtEditPassword').value
            })
        }).then(response => {
            if (response.status === 200) {
                document.getElementById('cmdWaitPassword').classList.add('d-none');
                document.getElementById('cmdSuccessPassword').classList.remove('d-none');
                setTimeout(function () { document.getElementById('cmdSuccessPassword').click() }, 3000);
            } else {
                document.getElementById('cmdSavePassword').classList.remove('d-none');
                document.getElementById('cmdWaitPassword').classList.add('d-none');
                DisplayAlert('lblPasswordAlert', 'An error occurred trying to update your password. Please try again. If the error persists, <a href="/contact?subject=Password Update Error" class="link-danger" aria-label="Contact form">let us know</a>.');
            }
        });
    }
}

function CheckPasswordLength() {
    const passwordField = document.getElementById('txtEditPassword');

    if (passwordField.value.length < 14) {
        DisplayAlert('lblPasswordAlert', 'Your password must be at least 14 characters long.');
        passwordField.classList.remove('is-valid');
        passwordField.classList.add('is-invalid');
        return false;
    } else {
        passwordField.classList.add('is-valid');
        passwordField.classList.remove('is-invalid');
        return true;
    }
}

function CheckPasswordsMatch() {
    const passwordField = document.getElementById('txtEditPassword');
    const confirmPasswordField = document.getElementById('txtConfirmPassword');

    if (passwordField.value.length == 0 || confirmPasswordField.value.length == 0) {
        return false;
    }

    if (confirmPasswordField.value !== passwordField.value) {
        DisplayAlert('lblPasswordAlert', 'The passwords entered do not match.');
        passwordField.classList.remove('is-valid');
        passwordField.classList.add('is-invalid');
        confirmPasswordField.classList.remove('is-valid');
        confirmPasswordField.classList.add('is-invalid');
        return false;
    } else {
        passwordField.classList.add('is-valid');
        passwordField.classList.remove('is-invalid');
        confirmPasswordField.classList.add('is-valid');
        confirmPasswordField.classList.remove('is-invalid');
        return true;
    }
}

function DisplayAlert(elementId, message) {
    var ele = document.getElementById(elementId);
    ele.innerHTML = message;
    ele.classList.remove('d-none');
}

function HideAlert(elementId,) {
    var ele = document.getElementById(elementId);
    ele.innerHTML = '';
    ele.classList.add('d-none');
}