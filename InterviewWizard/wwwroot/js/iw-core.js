document.addEventListener('DOMContentLoaded', function () {
});

var validateEmailOldEmail = '';
async function ValidateEmail(ele) {
    if (ele.value === validateEmailOldEmail) {
        return;
    }
    var email = ele.value;
    if (email.length == 0) {
        ele.classList.remove('is-valid');
        ele.classList.add('is-invalid');
        return 'Please enter a valid email address.';
    }

    const regex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!regex.test(email)) {
        ele.classList.remove('is-valid');
        ele.classList.add('is-invalid');
        return 'Please enter a valid email address.';
    }

    const result = await fetch(`/auth/${email}/exists`, {
        method: 'GET',
        headers: {
            'Cache-Control': 'no-cache'
        },
        cache: 'no-cache'
    });
    const response = await result;
    if (response.status === 204) {
        ele.classList.remove('is-invalid');
        ele.classList.add('is-valid');
        return;
    } else {
        ele.classList.remove('is-valid');
        ele.classList.add('is-invalid')
        return 'The email you have entered is already registered.';
    }
    validateEmailOldEmail = email;
}