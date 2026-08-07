/* ==========================================================================
   ONBOARDING (PROFIL TAMAMLAMA) - Detaing.js
   ========================================================================== */

// Seçilen ilgi alanlarının ID'lerini tutan global dizi
let selectedInterestIds = [];

document.addEventListener("DOMContentLoaded", function () {

    /* ----------------------------------------------------------------------
       1. TEMA DEĞİŞTİRME MEKANİZMASI (DARK MODE)
       ---------------------------------------------------------------------- */
    const themeToggleBtn = document.getElementById("themeToggleBtn");
    const themeIcon = themeToggleBtn ? themeToggleBtn.querySelector(".theme-icon") : null;
    const themeText = themeToggleBtn ? themeToggleBtn.querySelector(".theme-text") : null;

    // Tarayıcı hafızasından mevcut temayı çek ve uygula
    const currentTheme = localStorage.getItem("theme");

    if (currentTheme === "dark") {
        document.body.classList.add("dark-mode");
        if (themeIcon) themeIcon.textContent = "☀️";
        if (themeText) themeText.textContent = "Aydınlık Tema";
    } else {
        document.body.classList.remove("dark-mode");
        if (themeIcon) themeIcon.textContent = "🌙";
        if (themeText) themeText.textContent = "Koyu Tema";
    }

    // Tema Butonuna Tıklandığında
    if (themeToggleBtn) {
        themeToggleBtn.addEventListener("click", function () {
            document.body.classList.toggle("dark-mode");

            let theme = "light";
            if (document.body.classList.contains("dark-mode")) {
                theme = "dark";
                if (themeIcon) themeIcon.textContent = "☀️";
                if (themeText) themeText.textContent = "Aydınlık Tema";
            } else {
                if (themeIcon) themeIcon.textContent = "🌙";
                if (themeText) themeText.textContent = "Koyu Tema";
            }

            localStorage.setItem("theme", theme);
        });
    }

    /* ----------------------------------------------------------------------
       2. PROFİL FOTOĞRAFI YÜKLEME & KALDIRMA
       ---------------------------------------------------------------------- */
    const profileInput = document.getElementById("profileInput");
    const profilePreview = document.getElementById("profilePreview");
    const removePhotoBtn = document.getElementById("removePhotoBtn");
    const defaultAvatar = "https://cdn-icons-png.flaticon.com/512/149/149071.png";

    // Fotoğraf Seçilince Önizleme Yap
    if (profileInput && profilePreview) {
        profileInput.addEventListener("change", function () {
            const file = this.files[0];
            if (file) {
                const reader = new FileReader();
                reader.onload = function (e) {
                    profilePreview.src = e.target.result;
                };
                reader.readAsDataURL(file);
            }
        });
    }

    // Fotoğrafı Varsayılana Sıfırla
    if (removePhotoBtn && profileInput && profilePreview) {
        removePhotoBtn.addEventListener("click", function () {
            profileInput.value = "";
            profilePreview.src = defaultAvatar;
        });
    }

    /* ----------------------------------------------------------------------
       3. BİYOGRAFİ KARAKTER SAYACI
       ---------------------------------------------------------------------- */
    const userBio = document.getElementById("userBio");
    const currentCharCount = document.getElementById("currentCharCount");

    if (userBio && currentCharCount) {
        userBio.addEventListener("input", function () {
            currentCharCount.textContent = this.value.length;
        });
    }

    /* ----------------------------------------------------------------------
       4. SAYFADAN ÇIKIŞ / GEÇİŞ ANİMASYONU (SÜZÜLEREK GEÇİŞ)
       ---------------------------------------------------------------------- */
    const skipLink = document.querySelector(".skip-link");
    const saveBtn = document.getElementById("saveOnboardingBtn");

    // Şimdilik Atla Linkine Basılınca Yumuşak Çıkış
    if (skipLink) {
        skipLink.addEventListener("click", function (e) {
            const targetUrl = this.getAttribute("href");
            if (targetUrl) {
                e.preventDefault();
                document.body.classList.add("page-exit");
                setTimeout(function () {
                    window.location.href = targetUrl;
                }, 300); // 300ms animasyon süresi
            }
        });
    }

});

/* --------------------------------------------------------------------------
   5. İLGİ ALANI SEÇİMİ (ONCLICK EVENT)
   -------------------------------------------------------------------------- */
function toggleInterest(cardElement) {
    const interestId = cardElement.getAttribute("data-id");

    if (cardElement.classList.contains("selected")) {
        cardElement.classList.remove("selected");
        selectedInterestIds = selectedInterestIds.filter(id => id !== interestId);
    } else {
        cardElement.classList.add("selected");
        selectedInterestIds.push(interestId);
    }
}