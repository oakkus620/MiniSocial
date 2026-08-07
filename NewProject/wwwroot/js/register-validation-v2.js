/* ==========================================================================
   KAYIT FORMU DOĞRULAMA VE DİNAMİK İŞLEMLER (register-validation.js)
   ========================================================================== */

document.addEventListener('DOMContentLoaded', function () {

    let isUsernameValid = false;
    let isEmailValid = false;

    const registerBtn = document.getElementById('registerButton');

    // 1. TEMA DEĞİŞTİRME
    const themeBtn = document.getElementById('themeToggleBtn');
    if (themeBtn) {
        const themeText = themeBtn.querySelector('.theme-text');
        const themeIcon = themeBtn.querySelector('.theme-icon');

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

    // SVG İKON ÜRETİCİ FONKSİYONLAR
    function getSuccessIcon(tooltipText) {
        return `
        <div class="status-badge success-badge" title="${tooltipText}">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#28a745" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
                <circle cx="12" cy="12" r="10"></circle>
                <polyline points="16 9 10.5 14.5 8 12"></polyline>
            </svg>
        </div>`;
    }

    function getErrorIcon(tooltipText) {
        return `
        <div class="status-badge error-badge" title="${tooltipText}">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#dc3545" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
                <circle cx="12" cy="12" r="10"></circle>
                <line x1="15" y1="9" x2="9" y2="15"></line>
                <line x1="9" y1="9" x2="15" y2="15"></line>
            </svg>
        </div>`;
    }

    // 2. DROPDOWN MEKANİZMASI
    function setupCustomDropdown(dropdownId, hiddenInputId) {
        const dropdown = document.getElementById(dropdownId);
        if (!dropdown) return;

        const header = dropdown.querySelector('.dropdown-header');
        const headerText = dropdown.querySelector('.header-text');
        const hiddenInput = document.getElementById(hiddenInputId);
        const items = dropdown.querySelectorAll('.dropdown-item');

        if (header) {
            header.addEventListener('click', function (e) {
                e.stopPropagation();
                document.querySelectorAll('.custom-dropdown').forEach(d => {
                    if (d !== dropdown) d.classList.remove('open');
                });
                dropdown.classList.toggle('open');
            });
        }

        items.forEach(item => {
            item.addEventListener('click', function (e) {
                e.stopPropagation();
                const value = this.getAttribute('data-value');
                const text = this.textContent;

                if (headerText) headerText.textContent = text;
                if (hiddenInput) hiddenInput.value = value;
                dropdown.classList.remove('open');

                checkFormValidity();
            });
        });
    }

    setupCustomDropdown('dropdownDay', 'selectedDay');
    setupCustomDropdown('dropdownMonth', 'selectedMonth');
    setupCustomDropdown('dropdownYear', 'selectedYear');

    document.addEventListener('click', function () {
        document.querySelectorAll('.custom-dropdown').forEach(d => d.classList.remove('open'));
    });

    // 3. SAYFA DÖNÜŞ ANİMASYONU
    const backBtns = document.querySelectorAll('.back-btn, .bottom-link a');
    const formContainer = document.querySelector('.form-container');

    if (formContainer && backBtns.length > 0) {
        backBtns.forEach(btn => {
            btn.addEventListener('click', function (e) {
                const targetUrl = this.getAttribute('href');
                if (targetUrl && targetUrl !== '#') {
                    e.preventDefault();
                    formContainer.classList.add('fade-out');

                    setTimeout(function () {
                        window.location.href = targetUrl;
                    }, 250);
                }
            });
        });
    }

    // 4. KULLANICI ADI ANLIK KONTROLÜ
    const usernameInput = document.getElementById('usernameInput');
    const usernameStatusIcon = document.getElementById('usernameStatusIcon');
    const usernameFeedback = document.getElementById('usernameFeedback');

    let usernameTimer;

    if (usernameInput) {
        usernameInput.addEventListener('input', function () {
            clearTimeout(usernameTimer);
            const username = this.value.trim();

            if (username.length === 0) {
                if (usernameStatusIcon) {
                    usernameStatusIcon.classList.remove('visible');
                    setTimeout(() => { usernameStatusIcon.innerHTML = ''; }, 250);
                }
                if (usernameFeedback) usernameFeedback.innerText = '';
                isUsernameValid = false;
                checkFormValidity();
                return;
            }

            if (username.length < 3) {
                if (usernameStatusIcon) {
                    usernameStatusIcon.innerHTML = getErrorIcon('Kullanıcı adı en az 3 karakter olmalıdır.');
                    usernameStatusIcon.classList.add('visible');
                }
                if (usernameFeedback) {
                    usernameFeedback.innerText = 'Kullanıcı adı en az 3 karakter olmalıdır.';
                    usernameFeedback.className = 'feedback-text error';
                }
                isUsernameValid = false;
                checkFormValidity();
                return;
            }

            usernameTimer = setTimeout(() => {
                fetch(`/Account/CheckUsername?username=${encodeURIComponent(username)}`)
                    .then(response => response.json())
                    .then(data => {
                        if (usernameInput.value.trim().length === 0) {
                            if (usernameStatusIcon) {
                                usernameStatusIcon.classList.remove('visible');
                                setTimeout(() => { usernameStatusIcon.innerHTML = ''; }, 250);
                            }
                            if (usernameFeedback) usernameFeedback.innerText = '';
                            return;
                        }
                        if (data.isAvailable) {
                            if (usernameStatusIcon) {
                                usernameStatusIcon.innerHTML = getSuccessIcon('Bu kullanıcı adı kullanılabilir.');
                                usernameStatusIcon.classList.add('visible');
                            }
                            if (usernameFeedback) {
                                usernameFeedback.innerText = 'Kullanıcı adı kullanılabilir.';
                                usernameFeedback.className = 'feedback-text success';
                            }
                            isUsernameValid = true;
                        } else {
                            if (usernameStatusIcon) {
                                usernameStatusIcon.innerHTML = getErrorIcon('Bu kullanıcı adı zaten kayıtlı!');
                                usernameStatusIcon.classList.add('visible');
                            }
                            if (usernameFeedback) {
                                usernameFeedback.innerText = 'Bu kullanıcı adı zaten alınmış!';
                                usernameFeedback.className = 'feedback-text error';
                            }
                            isUsernameValid = false;
                        }
                        checkFormValidity();
                    })
                    .catch(err => console.error("Kullanıcı adı kontrol hatası:", err));
            }, 300);
        });
    }

    // 5. EMAIL ANLIK KONTROLÜ
    const emailInput = document.getElementById('emailInput');
    const emailStatusIcon = document.getElementById('emailStatusIcon');
    const emailFeedback = document.getElementById('emailFeedback');

    let emailTimer;

    if (emailInput) {
        emailInput.addEventListener('input', function () {
            clearTimeout(emailTimer);
            const email = this.value.trim();

            if (email.length === 0) {
                if (emailStatusIcon) {
                    emailStatusIcon.classList.remove('visible');
                    setTimeout(() => { emailStatusIcon.innerHTML = ''; }, 250);
                }
                if (emailFeedback) emailFeedback.innerHTML = '';
                isEmailValid = false;
                checkFormValidity();
                return;
            }

            if (!email.includes('@') || email.length < 5) {
                if (emailStatusIcon) {
                    emailStatusIcon.innerHTML = getErrorIcon('Geçerli bir e-posta adresi girin.');
                    emailStatusIcon.classList.add('visible');
                }
                if (emailFeedback) {
                    emailFeedback.innerText = 'Geçerli bir e-posta adresi girin.';
                    emailFeedback.className = 'feedback-text error';
                }
                isEmailValid = false;
                checkFormValidity();
                return;
            }

            emailTimer = setTimeout(() => {
                fetch(`/Account/CheckEmail?email=${encodeURIComponent(email)}`)
                    .then(response => response.json())
                    .then(data => {
                        if (emailInput.value.trim().length === 0) {
                            if (emailStatusIcon) {
                                emailStatusIcon.classList.remove('visible');
                                setTimeout(() => { emailStatusIcon.innerHTML = ''; }, 250);
                            }
                            if (emailFeedback) emailFeedback.innerHTML = '';
                            return;
                        }
                        if (data.isAvailable) {
                            if (emailStatusIcon) {
                                emailStatusIcon.innerHTML = getSuccessIcon('E-posta kullanılabilir.');
                                emailStatusIcon.classList.add('visible');
                            }
                            if (emailFeedback) {
                                emailFeedback.innerText = 'E-posta kullanılabilir.';
                                emailFeedback.className = 'feedback-text success';
                            }
                            isEmailValid = true;
                        } else {
                            if (emailStatusIcon) {
                                emailStatusIcon.innerHTML = getErrorIcon('Bu e-posta adresi zaten kayıtlı!');
                                emailStatusIcon.classList.add('visible');
                            }
                            if (emailFeedback) {
                                emailFeedback.innerHTML = 'Bu e-posta zaten kayıtlı! <a href="/Account/Login" style="color:#0d6efd; font-weight:bold; text-decoration:underline;">Giriş Yap</a>';
                                emailFeedback.className = 'feedback-text error';
                            }
                            isEmailValid = false;
                        }
                        checkFormValidity();
                    })
                    .catch(err => console.error("Email kontrol hatası:", err));
            }, 300);
        });
    }

    // 6. ŞİFRE VE ŞİFRE TEKRARI ANLIK KONTROLÜ
    const passwordInput = document.getElementById('password');
    const confirmPasswordInput = document.getElementById('confirmPassword');

    const passwordStatusIcon = document.getElementById('passwordStatusIcon');
    const passwordFeedback = document.getElementById('passwordFeedback');

    const confirmPasswordStatusIcon = document.getElementById('confirmPasswordStatusIcon');
    const confirmPasswordFeedback = document.getElementById('confirmPasswordFeedback');

    function validatePasswords() {
        const passVal = passwordInput ? passwordInput.value : '';
        const confirmVal = confirmPasswordInput ? confirmPasswordInput.value : '';

        // A) İLK ŞİFRE KUTUSU
        if (passVal.length === 0) {
            if (passwordStatusIcon) {
                passwordStatusIcon.classList.remove('visible');
                setTimeout(() => { passwordStatusIcon.innerHTML = ''; }, 250);
            }
            if (passwordFeedback) passwordFeedback.innerText = '';
        } else if (passVal.length < 6) {
            if (passwordStatusIcon) {
                passwordStatusIcon.innerHTML = getErrorIcon('Şifre en az 6 karakter olmalıdır.');
                passwordStatusIcon.classList.add('visible');
            }
            if (passwordFeedback) {
                passwordFeedback.innerText = 'Şifre en az 6 karakter olmalıdır.';
                passwordFeedback.className = 'feedback-text error';
            }
        } else {
            if (passwordStatusIcon) {
                passwordStatusIcon.innerHTML = getSuccessIcon('Şifre uzunluğu yeterli.');
                passwordStatusIcon.classList.add('visible');
            }
            if (passwordFeedback) {
                passwordFeedback.innerText = 'Şifre uzunluğu yeterli.';
                passwordFeedback.className = 'feedback-text success';
            }
        }

        // B) ŞİFRE TEKRAR KUTUSU
        if (confirmVal.length === 0) {
            if (confirmPasswordStatusIcon) {
                confirmPasswordStatusIcon.classList.remove('visible');
                setTimeout(() => { confirmPasswordStatusIcon.innerHTML = ''; }, 250);
            }
            if (confirmPasswordFeedback) confirmPasswordFeedback.innerText = '';
        } else if (confirmVal.length < 6) {
            if (confirmPasswordStatusIcon) {
                confirmPasswordStatusIcon.innerHTML = getErrorIcon('Şifre en az 6 karakter olmalıdır.');
                confirmPasswordStatusIcon.classList.add('visible');
            }
            if (confirmPasswordFeedback) {
                confirmPasswordFeedback.innerText = 'Şifre en az 6 karakter olmalıdır.';
                confirmPasswordFeedback.className = 'feedback-text error';
            }
        } else if (passVal !== confirmVal) {
            if (confirmPasswordStatusIcon) {
                confirmPasswordStatusIcon.innerHTML = getErrorIcon('Şifreler birbiriyle eşleşmiyor!');
                confirmPasswordStatusIcon.classList.add('visible');
            }
            if (confirmPasswordFeedback) {
                confirmPasswordFeedback.innerText = 'Şifreler birbiriyle eşleşmiyor!';
                confirmPasswordFeedback.className = 'feedback-text error';
            }
        } else {
            if (confirmPasswordStatusIcon) {
                confirmPasswordStatusIcon.innerHTML = getSuccessIcon('Şifreler eşleşti.');
                confirmPasswordStatusIcon.classList.add('visible');
            }
            if (confirmPasswordFeedback) {
                confirmPasswordFeedback.innerText = 'Şifreler eşleşti.';
                confirmPasswordFeedback.className = 'feedback-text success';
            }
        }

        checkFormValidity();
    }

    if (passwordInput && confirmPasswordInput) {
        passwordInput.addEventListener('input', validatePasswords);
        confirmPasswordInput.addEventListener('input', validatePasswords);
    }

    // GÖZ İKONU GÖSTERME / SİLİNCE YUMUŞAKÇA GİZLEME
    const passFields = [passwordInput, confirmPasswordInput];
    passFields.forEach(input => {
        if (input) {
            ["input", "change", "keyup"].forEach(eventType => {
                input.addEventListener(eventType, function () {
                    const parent = this.closest(".form-floating");
                    const toggleBtn = parent ? parent.querySelector(".toggle-password") : null;

                    if (toggleBtn) {
                        if (this.value.length > 0) {
                            toggleBtn.classList.add("visible");
                        } else {
                            toggleBtn.classList.remove("visible");
                        }
                    }
                });
            });
        }
    });

    // AD SOYAD KONTROLÜ
    const adSoyadInput = document.getElementById('adSoyadInput');
    if (adSoyadInput) adSoyadInput.addEventListener('input', checkFormValidity);

    // 7. GENEL FORM GEÇERLİLİK KONTROLÜ
    function checkFormValidity() {
        const adSoyad = adSoyadInput ? adSoyadInput.value.trim() : '';
        const day = document.getElementById('selectedDay')?.value;
        const month = document.getElementById('selectedMonth')?.value;
        const year = document.getElementById('selectedYear')?.value;
        const pass = passwordInput ? passwordInput.value : '';
        const confirmPass = confirmPasswordInput ? confirmPasswordInput.value : '';
        const birthFeedback = document.getElementById('birthDateFeedback');

        let isValid = true;

        if (!adSoyad || adSoyad.length < 2) isValid = false;
        if (!isUsernameValid || !isEmailValid) isValid = false;

        if (!day || !month || !year) {
            isValid = false;
        } else {
            const birthDate = new Date(year, month - 1, day);
            const today = new Date();
            let age = today.getFullYear() - birthDate.getFullYear();
            const m = today.getMonth() - birthDate.getMonth();
            if (m < 0 || (m === 0 && today.getDate() < birthDate.getDate())) {
                age--;
            }

            if (age < 13) {
                isValid = false;
                if (birthFeedback) {
                    birthFeedback.innerText = 'Sosyal medya kullanım yaşı en az 13 olmalıdır.';
                    birthFeedback.className = 'feedback-text error';
                }
            } else if (birthFeedback) {
                birthFeedback.innerText = '';
            }
        }

        if (!pass || pass.length < 6) isValid = false;
        if (!confirmPass || pass !== confirmPass) isValid = false;

        if (registerBtn) {
            registerBtn.disabled = !isValid;
        }
    }

});

// 8. ŞİFRE GÖSTER / GİZLE
function togglePasswordVisibility(inputId, triggerElement) {
    const input = document.getElementById(inputId);
    if (!input) return;

    const eyeOpenSVG = `
        <svg class="eye-icon" xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"></path>
            <circle cx="12" cy="12" r="3"></circle>
        </svg>`;

    const eyeClosedSVG = `
        <svg class="eye-icon" xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"></path>
            <line x1="1" y1="1" x2="23" y2="23"></line>
        </svg>`;

    if (input.type === "password") {
        input.type = "text";
        triggerElement.innerHTML = eyeClosedSVG;
    } else {
        input.type = "password";
        triggerElement.innerHTML = eyeOpenSVG;
    }
}

// 9. OTP KONTROLÜ
document.addEventListener('DOMContentLoaded', function () {
    const otpInputs = document.querySelectorAll('.otp-input');
    const finalInput = document.getElementById('finalVerificationCode');
    const form = document.querySelector('form');

    if (otpInputs.length > 0) {
        otpInputs[0].focus();

        otpInputs.forEach((input, index) => {
            input.addEventListener('input', (e) => {
                const value = e.target.value;

                if (!/^[0-9]$/.test(value)) {
                    e.target.value = '';
                    return;
                }

                if (value && index < otpInputs.length - 1) {
                    otpInputs[index + 1].focus();
                }

                updateFinalCode();
            });

            input.addEventListener('keydown', (e) => {
                if (e.key === 'Backspace' && !input.value && index > 0) {
                    otpInputs[index - 1].focus();
                }
            });

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
            form.addEventListener('submit', function () {
                updateFinalCode();
            });
        }
    }
});

// 10. UYARI MESAJLARININ OTOMATİK KAYBOLMASI
document.addEventListener('DOMContentLoaded', function () {
    const alerts = document.querySelectorAll('.alert-custom');

    if (alerts.length > 0) {
        setTimeout(() => {
            alerts.forEach(alert => {
                alert.classList.add('fade-hide');

                setTimeout(() => {
                    alert.remove();
                }, 500);
            });
        }, 3500);
    }
});

// Build ve Cache Yenileme İmzası: v2.8-Perfect-Responsive-Sync