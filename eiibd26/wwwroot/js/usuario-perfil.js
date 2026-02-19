// usuario-perfil.js - extracted from UsuarioPerfil.cshtml
// Contains autocomplete init, slug helpers, avatar helpers rely on avatar-card.js, form helpers
(function () {
    'use strict';

    // Global error hooks
    window.addEventListener('error', function (ev) {
        try { console.error('Global script error:', ev.message, 'file:', ev.filename, 'line:', ev.lineno, 'col:', ev.colno, 'error:', ev.error); } catch (e) {}
    });
    window.addEventListener('unhandledrejection', function (ev) {
        try { console.error('Unhandled promise rejection:', ev.reason); } catch (e) {}
    });

    // Safe response parser
    function safeParseResponse(resp) {
        if (!resp.ok) throw new Error('HTTP ' + resp.status);
        return resp.text().then(function (text) {
            if (!text) return {};
            try { return JSON.parse(text); } catch (e) { console.error('Invalid JSON response', e, text); return {}; }
        });
    }

    // Maps bootstrap: expose initAutocomplete and impl so Google callback can call it
    window.initAutocomplete = function () {
        if (typeof window.initAutocompleteImpl === 'function') {
            window.initAutocompleteImpl();
            return;
        }
        var tries = 0;
        var wait = setInterval(function () {
            tries++;
            if (typeof window.initAutocompleteImpl === 'function') {
                clearInterval(wait);
                window.initAutocompleteImpl();
            } else if (tries > 50) {
                clearInterval(wait);
                console.warn('initAutocompleteImpl not defined after waiting');
            }
        }, 100);
    };

    window.initAutocompleteImpl = function () {
        try {
            var input = document.getElementById('ciudad-input');
            var paisSel = document.getElementById('pais-select');
            var latEl = document.getElementById('latitud-input');
            var lngEl = document.getElementById('longitud-input');
            if (!input || !window.google || !google.maps.places) {
                console.warn('Google places not available');
                return;
            }
            var autocomplete = new google.maps.places.Autocomplete(input, { types: ['(cities)'] });

            function sanitizeCountryCode(raw) {
                if (!raw) return "";
                var token = String(raw).trim().split(/\s|-/)[0];
                var match = token.match(/[A-Za-z]{2,3}/);
                return match ? match[0].toLowerCase() : "";
            }

            if (paisSel && paisSel.value) {
                var initialCode = sanitizeCountryCode(paisSel.value);
                if (initialCode) {
                    try { autocomplete.setComponentRestrictions({ country: initialCode }); console.debug('✅ Autocomplete inicializado con país:', initialCode); } catch (e) { console.warn('Error setting initial country restriction:', e); }
                }
            }

            if (paisSel) {
                paisSel.addEventListener('change', function () {
                    var code = sanitizeCountryCode(paisSel.value);
                    try { if (code) autocomplete.setComponentRestrictions({ country: code }); else autocomplete.setComponentRestrictions({}); } catch (e) { console.warn('setComponentRestrictions failed', e); }
                    input.value = '';
                    if (latEl) latEl.value = '';
                    if (lngEl) lngEl.value = '';
                });
            }

            autocomplete.addListener('place_changed', function () {
                var place = autocomplete.getPlace();
                var ciudadErr = document.getElementById('ciudad-error');
                if (place && place.geometry && place.geometry.location) {
                    if (ciudadErr) ciudadErr.classList.add('d-none');
                    if (latEl) latEl.value = place.geometry.location.lat();
                    if (lngEl) lngEl.value = place.geometry.location.lng();
                } else {
                    if (latEl) latEl.value = '';
                    if (lngEl) lngEl.value = '';
                    if (ciudadErr) ciudadErr.classList.remove('d-none');
                }
            });

            console.debug('Places autocomplete initialized');
        } catch (ex) { console.error('initAutocompleteImpl error', ex); }
    };

    // Slug helpers and UI interactions
    (function () {
        function el(id) { return document.getElementById(id); }
        var slugInput = el('Perfil_slug');
        var slugOk = el('slug-ok');
        var slugErr = el('slug-error');
        var slugPreview = el('slug-preview');
        var slugStatusIcon = el('slug-status-icon');
        var slugStatusBadge = el('slug-status-badge');
        var suggestionsList = el('slug-suggestions-list');
        var userId = (el('Perfil_idUser') && el('Perfil_idUser').value) ? el('Perfil_idUser').value : '';

        function updatePreview(slug) {
            if (!slug) {
                if (slugPreview) { slugPreview.href = '#'; slugPreview.textContent = '—'; }
                return;
            }
            var origin = window.location.origin;
            var url = origin + '/u/' + slug;
            if (slugPreview) { slugPreview.href = url; slugPreview.textContent = url; }
        }

        function clearStatus() { if (slugStatusIcon) slugStatusIcon.style.display = 'none'; if (slugStatusBadge) { slugStatusBadge.textContent = ''; slugStatusBadge.classList.remove('error'); } }
        function setStatus(ok, text) { if (!slugStatusIcon || !slugStatusBadge) return; slugStatusIcon.style.display = 'inline-flex'; slugStatusBadge.textContent = text || (ok ? 'Disponible' : 'Ocupado'); if (ok) slugStatusBadge.classList.remove('error'); else slugStatusBadge.classList.add('error'); }
        function clearSuggestions() { if (!suggestionsList) return; suggestionsList.innerHTML = ''; suggestionsList.style.display = 'none'; }

        function buildSuggestionListFromResponse(data) {
            var list = [];
            if (Array.isArray(data.slugs)) list = data.slugs;
            else if (data.slug) { list = [data.slug]; for (var i = 2; i <= 3; i++) list.push(data.slug + '-' + i); }
            var seen = {};
            list = list.filter(function (s) { if (!s) return false; var key = s.toLowerCase(); if (seen[key]) return false; seen[key] = true; return true; });
            return list.slice(0, 3);
        }

        function fetchSuggestions(baseText) {
            if (!baseText || !suggestionsList || !slugInput) return;
            var suggestionsUrl = slugInput.dataset.suggestionsUrl || (window.location.pathname + '?handler=GenerateSlug');
            try { var tmp = new URL(suggestionsUrl, window.location.origin); if (window.location.protocol === 'https:' && tmp.protocol === 'http:') tmp.protocol = 'https:'; suggestionsUrl = tmp.href; } catch (e) {
                if (suggestionsUrl.indexOf('//') === 0) suggestionsUrl = window.location.protocol + suggestionsUrl;
                else if (/^http:/i.test(suggestionsUrl) && window.location.protocol === 'https:') suggestionsUrl = suggestionsUrl.replace(/^http:/i, 'https:');
                else if (suggestionsUrl.charAt(0) === '/') suggestionsUrl = window.location.origin + suggestionsUrl;
                else suggestionsUrl = window.location.origin + (suggestionsUrl.indexOf('?') === 0 ? window.location.pathname : '') + suggestionsUrl;
            }
            var url = suggestionsUrl + (suggestionsUrl.indexOf('?') !== -1 ? '&' : '?') + 'baseText=' + encodeURIComponent(baseText) + '&count=3';
            if (userId) url += '&userId=' + encodeURIComponent(userId);
            fetch(url, { method: 'GET', credentials: 'same-origin' }).then(function (resp) { return safeParseResponse(resp); }).then(function (data) {
                clearSuggestions(); var list = buildSuggestionListFromResponse(data); if (!list.length) return; list.forEach(function (s) {
                    var li = document.createElement('li'); var btn = document.createElement('button'); btn.type = 'button'; btn.className = 'slug-suggestion-pill'; btn.textContent = s; btn.addEventListener('click', function () { slugInput.value = s; updatePreview(s); if (slugErr) slugErr.style.display = 'none'; if (slugOk) { slugOk.style.display = 'block'; slugOk.textContent = 'Has seleccionado: ' + s; } setStatus(true, 'Disponible'); }); li.appendChild(btn); suggestionsList.appendChild(li);
                }); suggestionsList.style.display = 'block';
            }).catch(function (e) { console.error('[slug] suggestions error', e); });
        }

        if (slugInput) {
            updatePreview(slugInput.value);
            var checkUrl = slugInput.dataset.checkUrl;
            var timeout;
            slugInput.addEventListener('input', function () {
                var val = this.value.trim(); updatePreview(val); clearTimeout(timeout);
                if (!val) { if (slugOk) slugOk.style.display = 'none'; if (slugErr) slugErr.style.display = 'none'; clearStatus(); clearSuggestions(); return; }
                timeout = setTimeout(function () {
                    var url = (checkUrl || window.location.pathname + '?handler=CheckSlug') + '&slug=' + encodeURIComponent(val);
                    if (userId) url += '&userId=' + encodeURIComponent(userId);
                    fetch(url, { method: 'GET', credentials: 'same-origin' }).then(function (resp) { return safeParseResponse(resp); }).then(function (json) {
                        if (json.exists) {
                            if (slugErr) { slugErr.style.display = 'block'; slugErr.textContent = 'El slug ya está en uso.'; }
                            if (slugOk) slugOk.style.display = 'none'; setStatus(false, 'Ocupado'); var base = json.suggestion || val; fetchSuggestions(base);
                        } else {
                            if (slugErr) slugErr.style.display = 'none'; if (slugOk) { slugOk.style.display = 'block'; slugOk.textContent = 'Slug disponible'; } setStatus(true, 'Disponible'); fetchSuggestions(val);
                        }
                    }).catch(function (e) { console.error('[slug] check error', e); });
                }, 350);
            });
        }

        document.addEventListener('DOMContentLoaded', function () {
            if (!slugInput) return;
            if (slugInput.value) fetchSuggestions(slugInput.value.trim()); else {
                var nombre = el('Perfil_Nombre') ? el('Perfil_Nombre').value : '';
                var apellidos = el('Perfil_Apellidos') ? el('Perfil_Apellidos').value : '';
                var base = (nombre + ' ' + apellidos).trim(); if (base) fetchSuggestions(base);
            }
        });
    })();

    // Misc UI helpers (success fade, tel/email validators)
    (function () {
        document.addEventListener('DOMContentLoaded', function () {
            try {
                var success = document.getElementById('perfil-success-alert');
                if (success) { setTimeout(function () { success.style.transition = 'opacity 0.45s ease'; success.style.opacity = '0'; setTimeout(function () { if (success.parentNode) success.parentNode.removeChild(success); }, 500); }, 3000); }
            } catch (e) { console.error(e); }

            var tel = document.getElementById('telefono-input'); var telError = document.getElementById('telefono-error'); var email = document.getElementById('email-input'); var emailError = document.getElementById('email-error');
            if (tel) {
                tel.addEventListener('keydown', function (e) {
                    var allowedControlKeys = ['Backspace', 'Delete', 'ArrowLeft', 'ArrowRight', 'Tab', 'Home', 'End'];
                    if (allowedControlKeys.indexOf(e.key) !== -1) return;
                    if ((e.ctrlKey || e.metaKey) && ['a', 'c', 'v', 'x'].indexOf(e.key.toLowerCase()) !== -1) return;
                    if (!/^[0-9]$/.test(e.key)) e.preventDefault();
                });
                tel.addEventListener('blur', function () { this.value = this.value.replace(/\D/g, ''); if (this.value.length > 0 && this.value.length !== 10) { if (telError) telError.classList.remove('d-none'); this.classList.add('is-invalid'); } else { if (telError) telError.classList.add('d-none'); this.classList.remove('is-invalid'); } });
            }
            if (email) {
                email.addEventListener('blur', function () { var val = this.value.trim(); if (!val) { if (emailError) emailError.classList.add('d-none'); this.classList.remove('is-invalid'); return; } var re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/; if (!re.test(val)) { if (emailError) emailError.classList.remove('d-none'); this.classList.add('is-invalid'); } else { if (emailError) emailError.classList.add('d-none'); this.classList.remove('is-invalid'); } });
            }
        });
    })();

})();
