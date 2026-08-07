const themeBtn = document.getElementById('themeToggleBtn');
if (themeBtn) {
    const themeText = themeBtn.querySelector('.theme-text');
    const themeIcon = themeBtn.querySelector('.theme-icon');

    // Sayfa ilk yüklendiğinde kontrol
    if (localStorage.getItem('theme') === 'dark') {
        document.body.classList.add('dark-mode');
        if (themeText) themeText.textContent = 'Açık Tema';
        if (themeIcon) themeIcon.textContent = '☀️';
    }

    themeBtn.addEventListener('click', function () {
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

// Geri dön / Yönlendirme butonuna tıklanınca çıkış animasyonunu tetikle
const backBtn = document.querySelector('.back-btn');
const formContainer = document.querySelector('.form-container');

if (backBtn && formContainer) {
    backBtn.addEventListener('click', function (e) {
        e.preventDefault(); // Sayfaya anında gitmeyi durdur

        const targetUrl = this.getAttribute('href');
        formContainer.classList.add('fade-out'); // Çıkış animasyonunu başlat

        // Animasyon bitince (250ms) hedef sayfaya yönlendir
        setTimeout(function () {
            window.location.href = targetUrl;
        }, 250);
    });
}


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
        }, 3500); // 3.5 Saniye sonra kaybolur
        }
    });
