var oldEmail = '';

document.addEventListener('DOMContentLoaded', function () {
    document.getElementById('frmRegister').addEventListener('submit', function (e) {
        e.preventDefault();
        e.stopPropagation();

        var emailField = document.getElementById('EmailAddress');
        validateEmail(emailField);

        var passwordField = document.getElementById('Password');
        illustrateControlValidity(passwordField, passwordField.value.length >= 14);

        var confirmPasswordField = document.getElementById('ConfirmPassword');
        illustrateControlValidity(confirmPasswordField, confirmPasswordField.value === passwordField.value);

        document.querySelectorAll('*[data-iw-validate]').forEach(function (ele) {
            if (ele.classList.contains('is-invalid')) {
                return;
            }
        });

        // Form is fine - register the user
        document.getElementById('cmdSubmit').classList.add('disabled');
        document.getElementById('cmdSubmit').innerHTML = '<span class="spinner-border spinner-border-sm" aria-hidden="true"></span><span class="visually-hidden" role="status">Registering...</span>';

        var token = document.querySelector('input[name="__RequestVerificationToken"]').value;

        fetch('/auth/register', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                email: emailField.value,
                password: passwordField.value,
                token: token
            })
        })
            .then(response => {
                if (response.status === 201) {
                    var token = response.headers.get('Token');
                    document.location = `/verify-account?token=${token}`;

                } else {
                    document.getElementById('cmdSubmit').classList.remove('disabled');
                    document.getElementById('cmdSubmit').innerHTML = 'Sign up';
                    DisplayAlert('lblStatus', AlertTypes.Error, 'An error occurred trying to create your account. Please try again. If the error persists, <a href="/contact?subject=Registration Error" class="link-danger" aria-label="Contact form">let us know</a>.')
                }
            })
            .catch(error => {
                document.getElementById('cmdSubmit').classList.remove('disabled');
                document.getElementById('cmdSubmit').innerHTML = 'Sign up';
                DisplayAlert('lblStatus', AlertTypes.Error, 'An error occurred trying to create your account. Please try again. If the error persists, <a href="/contact?subject=Registration Error" class="link-danger" aria-label="Contact form">let us know</a>.')
            });
    });

    document.getElementById('EmailAddress').addEventListener('blur', function (ele) {
        var valid = validateEmail(this);
    });
});

function validateEmail(ele) {
    if (ele.value === oldEmail) {
        return;
    }
    var email = ele.value;
    if (email.length == 0) {
        illustrateControlValidity(ele, false);
        document.getElementById('lblInvalidEmail').innerHTML = 'Please enter a valid email address.';
        return;
    }

    const regex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!regex.test(email)) {
        illustrateControlValidity(ele, false);
        document.getElementById('lblInvalidEmail').innerHTML = 'Please enter a valid email address.';
        return;
    }

    return fetch(`/auth/${email}/exists`, {
        method: 'GET',
        headers: {
            'Cache-Control': 'no-cache'
        },
        cache: 'no-cache'
    })
        .then(response => {
            console.log(response.status);
            if (response.status === 204) {
                illustrateControlValidity(ele, true)
            } else {
                illustrateControlValidity(ele, false)
                document.getElementById('lblInvalidEmail').innerHTML = 'The email you have entered is already registered. Use the profile icon top right to sign in.';
            }
        })
        .catch(error => {
            illustrateControlValidity(ele, false)
            document.getElementById('lblInvalidEmail').innerHTML = 'An error occurred checking your email address. Please try again.';
        });
    oldEmail = email;
}

function illustrateControlValidity(ele, valid) {
    if (valid) {
        ele.classList.remove('is-invalid');
        ele.classList.add('is-valid');
    } else {
        ele.classList.remove('is-valid');
        ele.classList.add('is-invalid')
    }
}