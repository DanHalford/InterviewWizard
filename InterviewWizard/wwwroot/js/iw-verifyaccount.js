document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('input.number-box').forEach(function (el) {
        el.addEventListener('keydown', function (e) {
            if (e.key.match(/[0-9]/) == null && e.key != "Backspace" && (e.key == 'v' && e.ctrlKey == true) == false) {
                e.preventDefault();
            }
            if (this.value.length == 0 && e.key == 'Backspace') {
                const digitNumber = parseInt(this.id.substring(8));
                if (digitNumber > 1) {
                    document.getElementById(`txtDigit${digitNumber - 1}`).focus();
                }
            }
            if (e.key == 'Enter') {
                document.getElementById('btnVerify').click();
            }

        });

        el.addEventListener('keyup', function (e) {
            if (e.key.match(/[0-9]/) == null && e.key != "Backspace") {
                e.preventDefault();
            } else {
                if (this.value.length >= 1) {
                    const digitNumber = parseInt(this.id.substring(8));
                    if (digitNumber < 6) {
                        var nextBox = document.getElementById(`txtDigit${digitNumber + 1}`);
                        nextBox.focus();
                        if (nextBox.value.length > 0) {
                            nextBox.select();
                        }
                    }
                }
            }
        });

        el.addEventListener('paste', function (e) {
            e.preventDefault();
            const pastedData = e.clipboardData || window.clipboardData;
            const pastedText = pastedData.getData('Text');
            if (pastedText.match(/[0-9]{6}/) != null) {
                for (let i = 0; i < 6; i++) {
                    document.getElementById(`txtDigit${i + 1}`).value = pastedText[i];
                }
            }
            document.getElementById('txtDigit6').focus();
        });
    });
    document.getElementById('btnVerify').addEventListener('click', function () {
        var token = document.getElementById('btnVerify').getAttribute('data-iw-token');
        document.getElementById('btnVerify').classList.add('d-none');
        document.getElementById('btnWait').classList.remove('d-none');
        var code = '';
        document.querySelectorAll('input.number-box').forEach(el => code += el.value);
        if (code.length == 6 && code.match(/[0-9]{6}/)) {
            const frmVerify = document.getElementById('frmVerify');
            document.getElementById('txtToken').value = token;
            document.getElementById('txtCode').value = code;
            frmVerify.submit();
        }
    });
});