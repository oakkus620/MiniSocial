document.addEventListener("DOMContentLoaded", function () {
    const themeBtn = document.getElementById('themeToggleBtn');
    if (themeBtn) {
        const themeText = themeBtn.querySelector('.theme-text');
        const themeIcon = themeBtn.querySelector('.theme-icon');

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

    const activeCommentPostId = sessionStorage.getItem("activeCommentPostId");
    if (activeCommentPostId) {
        const box = document.getElementById('comment-box-' + activeCommentPostId);
        if (box) {
            box.style.display = 'block';
            box.style.maxHeight = '600px';
            box.style.opacity = '1';
        }
        sessionStorage.removeItem("activeCommentPostId");
    }

    const mediaInput = document.getElementById('postMediaInput');
    const previewContainer = document.getElementById('mediaPreviewContainer');
    const previewWrapper = document.getElementById('previewWrapper');
    const removeMediaBtn = document.getElementById('removeMediaBtn');
    const existingMediaUrlInput = document.getElementById('existingMediaUrl');

    if (mediaInput) {
        mediaInput.addEventListener('change', function (event) {
            const file = event.target.files[0];
            if (file) {
                previewWrapper.innerHTML = '';
                const fileURL = URL.createObjectURL(file);

                if (file.type.startsWith('video')) {
                    const video = document.createElement('video');
                    video.src = fileURL;
                    video.controls = true;
                    video.style.maxWidth = '100%';
                    video.style.maxHeight = '220px';
                    video.style.borderRadius = '6px';
                    previewWrapper.appendChild(video);
                } else {
                    const img = document.createElement('img');
                    img.src = fileURL;
                    img.style.maxWidth = '100%';
                    img.style.maxHeight = '220px';
                    img.style.objectFit = 'contain';
                    img.style.borderRadius = '6px';
                    previewWrapper.appendChild(img);
                }

                existingMediaUrlInput.value = "";
                previewContainer.style.display = 'block';
                previewContainer.style.maxHeight = '0px';
                previewContainer.style.opacity = '0';
                setTimeout(() => {
                    previewContainer.style.maxHeight = '300px';
                    previewContainer.style.opacity = '1';
                }, 10);
            }
        });

        if (removeMediaBtn) {
            removeMediaBtn.addEventListener('click', function () {
                previewContainer.style.maxHeight = '0px';
                previewContainer.style.opacity = '0';
                setTimeout(() => {
                    mediaInput.value = '';
                    previewWrapper.innerHTML = '';
                    existingMediaUrlInput.value = "";
                    previewContainer.style.display = 'none';
                }, 300);
            });
        }
    }

    document.querySelectorAll('.post-text-body').forEach(p => {
        const id = p.id.replace('post-text-', '');
        const btn = document.getElementById('read-more-' + id);
        if (p.textContent.length > 140) {
            btn.style.display = 'inline-block';
        }
    });
});

function keepCommentOpen(postId) {
    sessionStorage.setItem("activeCommentPostId", postId);
}

function toggleReadMore(id) {
    const container = document.getElementById('text-container-' + id);
    const btn = document.getElementById('read-more-' + id);

    container.classList.toggle('expanded');
    if (container.classList.contains('expanded')) {
        btn.textContent = "daha az";
    } else {
        btn.textContent = "...devamı";
    }
}

function toggleMenu(event, id) {
    event.stopPropagation();
    closeAllDropdowns();
    var menu = document.getElementById('menu-' + id);
    menu.style.display = (menu.style.display === 'block') ? 'none' : 'block';
}

// 📌 YORUM MENÜSÜNÜ KESİN VE DOĞRU KONUMDA AÇAN FONKSİYON
// Yorum Üç Nokta Menüsünü Kesin Açan Fonksiyon
function toggleCommentMenu(event, commentId) {
    event.stopPropagation();

    // Diğer tüm açık menüleri gizle
    document.querySelectorAll('[id^="comment-menu-"]').forEach(m => {
        if (m.id !== 'comment-menu-' + commentId) {
            m.style.display = 'none';
        }
    });

    var menu = document.getElementById('comment-menu-' + commentId);
    if (!menu) return;

    if (menu.style.display === 'flex') {
        menu.style.display = 'none';
    } else {
        const btnRect = event.currentTarget.getBoundingClientRect();
        menu.style.display = 'flex';

        let leftPos = btnRect.left - 90;
        let topPos = btnRect.bottom + 4;
        if (leftPos < 10) leftPos = btnRect.left;

        menu.style.top = topPos + 'px';
        menu.style.left = leftPos + 'px';
    }
}

// Gönderi Düzenleme (Blur ve Öne Çıkma Modu)
// GÖNDERİ DÜZENLEME (HATA PATLATMAYAN GÜVENLİ YÖNTEM)
function prepareEditPost(postId, content, mediaUrl) {
    const card = document.getElementById('postCreateCard');
    const backdrop = document.getElementById('editBackdrop');
    const indicator = document.getElementById('editModeIndicator');

    if (backdrop) backdrop.style.display = 'block';
    if (card) {
        card.classList.add('post-focus-edit-mode');
        card.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
    if (indicator) indicator.style.display = 'flex';

    document.getElementById('editPostId').value = postId;

    // Metni textarea'ya güvenli şekilde basıyoruz
    const textArea = document.getElementById('postContentArea');
    textArea.value = decodeURIComponent(content);

    document.getElementById('submitPostBtn').textContent = "Kaydet";

    if (mediaUrl && mediaUrl !== "") {
        const container = document.getElementById('mediaPreviewContainer');
        const wrapper = document.getElementById('previewWrapper');
        document.getElementById('existingMediaUrl').value = mediaUrl;
        wrapper.innerHTML = `<img src="${mediaUrl}" style="max-width:100%; max-height:200px; border-radius:6px;" />`;
        container.style.display = 'block';
    }
} function closeAllDropdowns() {
    document.querySelectorAll('.dropdown-menu-custom, .comment-icon-popup-panel').forEach(m => m.style.display = 'none');
}

window.onclick = function (event) {
    closeAllDropdowns();
}

function toggleCommentBox(id) {
    var box = document.getElementById('comment-box-' + id);
    if (box.style.display === 'none' || box.style.display === '') {
        box.style.display = 'block';
        box.style.maxHeight = '0px';
        box.style.opacity = '0';
        setTimeout(() => {
            box.style.maxHeight = '600px';
            box.style.opacity = '1';
        }, 10);
    } else {
        box.style.maxHeight = '0px';
        box.style.opacity = '0';
        setTimeout(() => {
            box.style.display = 'none';
        }, 300);
    }
}

// 📌 GÖNDERİ DÜZENLEME (INSTAGRAM FOCUS MODU & ARKA PLAN BLUR)
function prepareEditPost(postId, content, mediaUrl) {
    const card = document.getElementById('postCreateCard');
    const backdrop = document.getElementById('editBackdrop');
    const indicator = document.getElementById('editModeIndicator');

    document.body.classList.add('body-blur-active');
    backdrop.style.display = 'block';
    card.classList.add('post-focus-edit-mode');
    indicator.style.display = 'flex';

    card.scrollIntoView({ behavior: 'smooth', block: 'center' });

    document.getElementById('editPostId').value = postId;
    document.getElementById('postContentArea').value = content;
    document.getElementById('submitPostBtn').textContent = "Düzenlemeyi Kaydet";

    if (mediaUrl) {
        const previewContainer = document.getElementById('mediaPreviewContainer');
        const previewWrapper = document.getElementById('previewWrapper');
        document.getElementById('existingMediaUrl').value = mediaUrl;

        previewWrapper.innerHTML = '';
        if (mediaUrl.endsWith('.mp4') || mediaUrl.endsWith('.mov')) {
            const video = document.createElement('video');
            video.src = mediaUrl;
            video.controls = true;
            video.style.maxWidth = '100%';
            video.style.maxHeight = '220px';
            previewWrapper.appendChild(video);
        } else {
            const img = document.createElement('img');
            img.src = mediaUrl;
            img.style.maxWidth = '100%';
            img.style.maxHeight = '220px';
            previewWrapper.appendChild(img);
        }
        previewContainer.style.display = 'block';
        previewContainer.style.maxHeight = '300px';
        previewContainer.style.opacity = '1';
    } else {
        document.getElementById('mediaPreviewContainer').style.display = 'none';
        document.getElementById('existingMediaUrl').value = "";
    }
}

function cancelPostEdit() {
    const card = document.getElementById('postCreateCard');
    const backdrop = document.getElementById('editBackdrop');
    const indicator = document.getElementById('editModeIndicator');

    document.body.classList.remove('body-blur-active');
    backdrop.style.display = 'none';
    card.classList.remove('post-focus-edit-mode');
    indicator.style.display = 'none';

    document.getElementById('editPostId').value = "";
    document.getElementById('postContentArea').value = "";
    document.getElementById('submitPostBtn').textContent = "Gönderi Paylaş";
    document.getElementById('mediaPreviewContainer').style.display = 'none';
    document.getElementById('existingMediaUrl').value = "";
}

// 📌 YORUM İÇİN INSTAGRAM TARZI INPUT ALANI AÇMA
function prepareEditComment(commentId, currentContent) {
    const wrap = document.getElementById('comment-content-wrap-' + commentId);
    if (wrap.querySelector('.comment-edit-form-inline')) return;

    wrap.innerHTML = `
        <form action="/Home/EditComment" method="post" class="comment-edit-form-inline">
            <input type="hidden" name="commentId" value="${commentId}" />
            <input type="text" name="newContent" value="${currentContent}" class="comment-edit-input-field" autocomplete="off" autofocus required />
            <div class="comment-edit-btn-group">
                <button type="submit" class="comment-edit-save-btn">Kaydet</button>
                <button type="button" onclick="location.reload()" class="comment-edit-cancel-btn">İptal</button>
            </div>
        </form>
    `;
}

function toggleLike(postId) {
    fetch('/Home/ToggleLike?postId=' + postId, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' }
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                const icon = document.getElementById('heart-icon-' + postId);
                const countSpan = document.getElementById('like-count-' + postId);

                if (data.isLiked) {
                    icon.classList.remove('fa-regular');
                    icon.classList.add('fa-solid');
                    icon.style.color = '#ed4956';
                } else {
                    icon.classList.remove('fa-solid');
                    icon.classList.add('fa-regular');
                    icon.style.color = 'var(--text-color)';
                }

                countSpan.textContent = data.count;
                countSpan.style.display = data.count > 0 ? 'inline' : 'none';
            }
        });
}