/* ==========================================================================
   ANA SAYFA JS - home.js
   ========================================================================== */

document.addEventListener('DOMContentLoaded', function () {
    // Tema Değiştirme Entegrasyonu
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

    // Beğen butonu tatlı etkileşimi
    const likeButtons = document.querySelectorAll('.like-btn');
    likeButtons.forEach(btn => {
        btn.addEventListener('click', function () {
            if (this.style.color === 'rgb(220, 53, 69)') {
                this.style.color = 'var(--text-color)';
                this.textContent = '❤️ Beğen';
            } else {
                this.style.color = '#dc3545';
                this.textContent = '❤️ Beğenildi';
            }
        });
    });
});