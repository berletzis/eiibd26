// avatar-card.js - parche definitivo: overlay disable + time-guard para evitar re-opens rápidos
(function () {
    if (typeof window === 'undefined') return;
    if (document.body.dataset.avatarCardInit === '1') return;
    document.body.dataset.avatarCardInit = '1';

    document.addEventListener('DOMContentLoaded', function () {
        const dbg = (...args) => { try { console.debug('avatar-card:', ...args); } catch { } };

        let avatarImg = document.getElementById('profileAvatar');
        let avatarContainer = avatarImg ? avatarImg.closest('.avatar-container') : null;
        const btnDummy = document.getElementById('btnUploadDummy');
        const btnRemoveOrig = document.getElementById('btnRemoveAvatar');
        const statusEl = document.getElementById('avatarUploadStatus');
        const hiddenAvatar = document.getElementById('Perfil_AvatarHidden');

        // Token helper
        function getToken() {
            const t = document.querySelector('input[name="__RequestVerificationToken"]');
            return t ? t.value : '';
        }
        function setStatus(txt, ms = 2500) {
            if (!statusEl) return;
            statusEl.textContent = txt || '';
            if (ms > 0) setTimeout(() => { if (statusEl) statusEl.textContent = ''; }, ms);
        }

        // ensure clean file input
        function createFileInput() {
            const inp = document.createElement('input');
            inp.type = 'file';
            inp.accept = 'image/*';
            inp.id = 'avatarFileInput';
            inp.style.display = 'none';
            document.body.appendChild(inp);
            return inp;
        }

        // create overlay that will intercept clicks
        function createOverlay() {
            const overlay = document.createElement('div');
            overlay.id = 'avatarOverlay';
            overlay.setAttribute('aria-hidden', 'true');
            Object.assign(overlay.style, {
                position: 'absolute',
                inset: '0px',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                background: 'rgba(0,0,0,0)',
                color: '#fff',
                cursor: 'pointer',
                zIndex: '9999',
                pointerEvents: 'auto'
            });
            const inner = document.createElement('span');
            inner.className = 'overlay-text';
            inner.textContent = 'Cambiar foto';
            inner.style.pointerEvents = 'none';
            overlay.appendChild(inner);
            return overlay;
        }

        // replace remove button with clean clone and return it
        function replaceRemoveButton(btn) {
            if (!btn || !btn.parentNode) return btn;
            try {
                const clone = btn.cloneNode(true);
                clone.id = btn.id;
                btn.parentNode.replaceChild(clone, btn);
                return clone;
            } catch (e) {
                dbg('replaceRemoveButton failed', e);
                return btn;
            }
        }

        // Get or create fileInput
        let fileInput = document.getElementById('avatarFileInput');
        if (fileInput && fileInput.parentNode) {
            try { fileInput.parentNode.removeChild(fileInput); } catch { }
        }
        fileInput = createFileInput();

        // Ensure avatarContainer
        if (!avatarContainer) {
            avatarContainer = document.querySelector('.avatar-container') || (avatarImg ? avatarImg.parentElement : null);
        }

        // Create overlay and append to container
        let overlay = null;
        if (avatarContainer) {
            // remove any existing overlay with same id
            const ex = avatarContainer.querySelector('#avatarOverlay');
            if (ex) try { ex.parentNode.removeChild(ex); } catch { }
            overlay = createOverlay();
            // ensure container is positioned
            const prevPos = window.getComputedStyle(avatarContainer).position;
            if (prevPos === 'static' || !prevPos) avatarContainer.style.position = 'relative';
            overlay.style.width = '100%';
            overlay.style.height = '100%';
            overlay.style.borderRadius = '50%';
            avatarContainer.appendChild(overlay);
        } else {
            // no container - fallback: ensure avatarImg exists and replace it with clone
            if (avatarImg) {
                try {
                    const newImg = avatarImg.cloneNode(true);
                    newImg.id = 'profileAvatar';
                    avatarImg.parentNode.replaceChild(newImg, avatarImg);
                    avatarImg = newImg;
                } catch (e) { dbg('fallback clone avatarImg', e); }
            }
            // create transparent overlay positioned over avatarImg's parent if possible
            const parent = avatarImg ? avatarImg.parentElement : document.body;
            overlay = createOverlay();
            if (parent) {
                const prevPos = window.getComputedStyle(parent).position;
                if (prevPos === 'static' || !prevPos) parent.style.position = 'relative';
                overlay.style.width = '100%';
                overlay.style.height = '100%';
                parent.appendChild(overlay);
            } else {
                document.body.appendChild(overlay);
            }
        }

        // Replace remove button and attach handler
        const btnRemove = replaceRemoveButton(btnRemoveOrig);
        if (btnRemove) {
            // remove duplicate listeners by cloning already done
            btnRemove.addEventListener('click', async function (e) {
                e && e.preventDefault && e.preventDefault();
                e && e.stopPropagation && e.stopPropagation();
                if (!confirm('¿Quitar tu foto de perfil?')) return;
                setStatus('Eliminando...');
                try {
                    const token = getToken();
                    const resp = await fetch(window.location.pathname + '?handler=RemoveAvatar', {
                        method: 'POST',
                        headers: { 'RequestVerificationToken': token },
                        credentials: 'same-origin'
                    });
                    if (resp.ok) {
                        const name = (document.getElementById('Perfil_Nombre') && document.getElementById('Perfil_Nombre').value) || 'Usuario';
                        const def = `https://ui-avatars.com/api/?name=${encodeURIComponent(name)}&size=110`;
                        const img = document.getElementById('profileAvatar');
                        if (img) img.src = def;
                        if (hiddenAvatar) hiddenAvatar.value = def;
                        setStatus('Avatar eliminado', 2000);
                    } else {
                        setStatus('Error al eliminar', 2500);
                    }
                } catch (err) {
                    console.error(err);
                    setStatus('Error al eliminar', 2500);
                } finally {
                    try { btnRemove.classList.remove('btn-loading'); } catch { }
                }
            }, { passive: false });
        }

        // State guards
        let selecting = false;
        let uploading = false;
        const SELECT_TIMEOUT = 10000;
        let selectTimeout = null;
        let lastOpenMs = 0;
        const MIN_OPEN_GAP = 400; // ms - ignore opens within this window

        function resetSelecting() {
            selecting = false;
            if (selectTimeout) { clearTimeout(selectTimeout); selectTimeout = null; }
            if (overlay) overlay.style.pointerEvents = 'auto';
            if (avatarImg) avatarImg.dataset.disabled = '0';
            dbg('resetSelecting');
        }

        // Open file dialog: disable overlay immediately and use guards
        function openFileDialog() {
            const now = Date.now();
            if (now - lastOpenMs < MIN_OPEN_GAP) {
                dbg('ignored due to lastOpenMs guard', now - lastOpenMs);
                return;
            }
            lastOpenMs = now;

            if (selecting || uploading) { dbg('open ignored selecting/uploading', selecting, uploading); return; }
            selecting = true;
            if (overlay) overlay.style.pointerEvents = 'none'; // prevent any further clicks reaching other handlers
            if (avatarImg) avatarImg.dataset.disabled = '1';
            selectTimeout = setTimeout(() => { dbg('select timeout'); resetSelecting(); }, SELECT_TIMEOUT);

            try {
                dbg('fileInput.click() invoked');
                fileInput.click();
            } catch (ex) {
                dbg('fileInput.click error', ex);
                resetSelecting();
            }
        }

        // handle file change (single handler)
        fileInput.addEventListener('change', async function (ev) {
            ev && ev.preventDefault && ev.preventDefault();
            ev && ev.stopPropagation && ev.stopPropagation();

            const f = ev.target.files && ev.target.files[0];
            if (!f) { resetSelecting(); return; }

            const allowed = ['image/jpeg', 'image/png', 'image/webp'];
            if (!allowed.includes(f.type)) { setStatus('Tipo de imagen no permitido'); resetSelecting(); return; }
            if (f.size > 5 * 1024 * 1024) { setStatus('El archivo supera 5 MB'); resetSelecting(); return; }

            uploading = true;
            const imgEl = document.getElementById('profileAvatar');
            const prev = imgEl ? imgEl.src : '';

            // preview
            try {
                const reader = new FileReader();
                reader.onload = function (e) { if (imgEl) imgEl.src = e.target.result; };
                reader.readAsDataURL(f);
            } catch (ex) { dbg('preview error', ex); }

            setStatus('Subiendo...');
            try {
                const fd = new FormData();
                fd.append('avatar', f);
                const token = getToken();
                const headers = token ? { 'RequestVerificationToken': token } : {};

                const resp = await fetch(window.location.pathname + '?handler=UploadAvatar', {
                    method: 'POST',
                    body: fd,
                    headers: headers,
                    credentials: 'same-origin'
                });

                if (!resp.ok) {
                    const txt = await resp.text();
                    dbg('upload failed', resp.status, txt);
                    if (imgEl) imgEl.src = prev;
                    setStatus('Error subiendo imagen', 3000);
                    return;
                }

                const json = await resp.json();
                if (json && json.url) {
                    const final = json.url + '?t=' + Date.now();
                    if (imgEl) imgEl.src = final;
                    if (hiddenAvatar) hiddenAvatar.value = json.url;
                    setStatus('Avatar actualizado', 2000);
                } else if (json && json.error) {
                    if (imgEl) imgEl.src = prev;
                    setStatus(json.error || 'Error subiendo', 3000);
                } else {
                    setStatus('Subida finalizada', 2000);
                }
            } catch (err) {
                console.error('avatar upload error', err);
                if (imgEl) imgEl.src = prev;
                setStatus('Error al subir', 3000);
            } finally {
                try { if (fileInput) fileInput.value = ''; } catch (e) { }
                uploading = false;
                resetSelecting();
            }
        }, { passive: false });

        // Attach overlay click to open only once
        if (overlay) {
            overlay.addEventListener('click', function (e) {
                e && e.preventDefault && e.preventDefault();
                e && e.stopPropagation && e.stopPropagation();
                openFileDialog();
            }, { passive: false });
        }

        // Also attach btnDummy to open
        if (btnDummy) {
            btnDummy.addEventListener('click', function (e) {
                e && e.preventDefault && e.preventDefault();
                e && e.stopPropagation && e.stopPropagation();
                openFileDialog();
            }, { passive: false });
        }

        dbg('avatar-card: ready, overlay strategy active');
    });
})();