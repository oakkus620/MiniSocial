/* ==========================================================================
   OTP DOĞRULAMA VE TEMA YÖNETİMİ (verify.js - DÜZELTİLMİŞ TAM KOD)
   ========================================================================== */

document.addEventListener('DOMContentLoaded', function () {

    // 1. TEMA DEĞİŞTİRME MEKANİZMASı
    const themeBtn = document.getElementById('themeToggleBtn');
    if (themeBtn) {
        const themeText = themeBtn.querySelector('.theme-text');
        const themeIcon = themeBtn.querySelector('.theme-icon');

        // Sayfa açıldığında localStorage kontrolü
        if (localStorage.getItem('theme') === 'dark') {
            document.body.classList.add('dark-mode');
            if (themeText) themeText.textContent = 'Açık Tema';
            if (themeIcon) themeIcon.textContent = '☀️';
        } else {
            document.body.classList.remove('dark-mode');
            if (themeText) themeText.textContent = 'Koyu Tema';
            if (themeIcon) themeIcon.textContent = '🌙';
        }

        themeBtn.addEventListener('click', function (e) {
            e.preventDefault();
            document.body.classList.toggle('dark-mode');

            if (document.body.classList.contains('dark-mode')) {
                localStorage.setItem('theme', 'dark');
                if (themeText) themeText.textContent = 'Açık Tema';
                if (themeIcon) themeIcon.textContent = '☀️';
            } else {
                localStorage.setItem('theme', 'light');
                if (themeText) themeText.textContent = 'Koyu Tema';
                if (themeIcon) themeIcon.textContent = '🌙';
            }
        });
    }

    // 2. 6 KUTULU OTP KONTROLÜ VE OTOMATİK GEÇİŞ MANTIĞI
    const otpInputs = document.querySelectorAll('.otp-input');
    const finalInput = document.getElementById('finalVerificationCode');
    const form = document.querySelector('form');

    if (otpInputs.length > 0) {
        otpInputs[0].focus();

        otpInputs.forEach((input, index) => {
            // Rakam girildiğinde bir sonraki kutuya zıplama
            input.addEventListener('input', (e) => {
                const value = e.target.value;

                // Sadece rakam kabul et
                if (!/^[0-9]$/.test(value)) {
                    e.target.value = '';
                    return;
                }

                if (value && index < otpInputs.length - 1) {
                    otpInputs[index + 1].focus();
                }

                updateFinalCode();
            });

            // Geri tuşuna (Backspace) basıldığında bir önceki kutuya dönme
            input.addEventListener('keydown', (e) => {
                if (e.key === 'Backspace') {
                    if (!input.value && index > 0) {
                        otpInputs[index - 1].focus();
                        otpInputs[index - 1].value = '';
                    } else if (input.value) {
                        input.value = '';
                    }
                    updateFinalCode();
                    e.preventDefault();
                }
            });

            // Panodan (Clipboard) 6 haneli kodu doğrudan yapıştırma desteği
            input.addEventListener('paste', (e) => {
                e.preventDefault();
                const pastedData = e.clipboardData.getData('text').trim();

                if (/^\d{6}$/.test(pastedData)) {
                    pastedData.split('').forEach((char, i) => {
                        if (otpInputs[i]) {
                            otpInputs[i].value = char;
                        }
                    });
                    otpInputs[otpInputs.length - 1].focus();
                    updateFinalCode();
                }
            });
        });

        function updateFinalCode() {
            let code = '';
            otpInputs.forEach(input => code += input.value);
            if (finalInput) finalInput.value = code;
        }

        if (form) {
            form.addEventListener('submit', function (e) {
                updateFinalCode();
                if (finalInput && finalInput.value.length < 6) {
                    e.preventDefault();
                    alert("Lütfen 6 haneli doğrulama kodunu eksiksiz girin.");
                }
            });
        }
    }
});