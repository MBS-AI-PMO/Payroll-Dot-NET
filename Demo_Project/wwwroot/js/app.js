/* Shared UI helpers for the NITB Payslip Portal: toasts, password fields, form loading states. */
(function () {
    'use strict';

    var ICONS = {
        success: 'fa-check-circle',
        info: 'fa-info-circle',
        warning: 'fa-exclamation-triangle',
        error: 'fa-times-circle'
    };
    var TITLES = { success: 'Success', info: 'Notice', warning: 'Warning', error: 'Something went wrong' };

    function container() {
        var el = document.getElementById('app-toasts');
        if (!el) {
            el = document.createElement('div');
            el.id = 'app-toasts';
            el.className = 'app-toasts';
            el.setAttribute('aria-live', 'polite');
            el.setAttribute('aria-atomic', 'false');
            document.body.appendChild(el);
        }
        return el;
    }

    function dismiss(toast) {
        if (!toast || toast.classList.contains('is-leaving')) return;
        toast.classList.add('is-leaving');
        setTimeout(function () { toast.remove(); }, 220);
    }

    window.appToast = {
        show: function (type, message, title) {
            type = ICONS[type] ? type : 'info';
            var toast = document.createElement('div');
            toast.className = 'app-toast app-toast--' + type;
            toast.setAttribute('role', type === 'error' ? 'alert' : 'status');

            var icon = document.createElement('i');
            icon.className = 'fas ' + ICONS[type] + ' app-toast__icon';
            icon.setAttribute('aria-hidden', 'true');

            var body = document.createElement('div');
            body.className = 'app-toast__body';
            var heading = document.createElement('div');
            heading.className = 'app-toast__title';
            heading.textContent = title || TITLES[type];
            var text = document.createElement('div');
            text.className = 'app-toast__message';
            text.textContent = message || '';
            body.appendChild(heading);
            body.appendChild(text);

            var close = document.createElement('button');
            close.type = 'button';
            close.className = 'app-toast__close';
            close.setAttribute('aria-label', 'Dismiss notification');
            close.innerHTML = '&times;';
            close.addEventListener('click', function () { dismiss(toast); });

            toast.appendChild(icon);
            toast.appendChild(body);
            toast.appendChild(close);
            container().appendChild(toast);

            var timeout = type === 'error' ? 7000 : 4500;
            var timer = setTimeout(function () { dismiss(toast); }, timeout);
            toast.addEventListener('mouseenter', function () { clearTimeout(timer); });
            toast.addEventListener('mouseleave', function () { timer = setTimeout(function () { dismiss(toast); }, 2500); });
        }
    };

    // Legacy hook used by existing pages – show a toast instead of a browser alert()
    window.PopAlert = function (message) {
        window.appToast.show('warning', message, 'Notice');
    };

    /* ---------- Password visibility toggles ---------- */
    document.addEventListener('click', function (e) {
        var btn = e.target.closest('[data-toggle-password]');
        if (!btn) return;
        var input = document.getElementById(btn.getAttribute('data-toggle-password'));
        if (!input) return;
        var show = input.type === 'password';
        input.type = show ? 'text' : 'password';
        btn.setAttribute('aria-pressed', show ? 'true' : 'false');
        btn.setAttribute('aria-label', show ? 'Hide password' : 'Show password');
        var icon = btn.querySelector('i');
        if (icon) icon.className = 'fas ' + (show ? 'fa-eye-slash' : 'fa-eye');
    });

    /* ---------- Auth form validation + loading state ---------- */
    function setFieldError(input, message) {
        var group = input.closest('.auth-field');
        var slot = group ? group.querySelector('.auth-field__error') : null;
        input.classList.toggle('is-invalid', !!message);
        input.setAttribute('aria-invalid', message ? 'true' : 'false');
        if (slot) slot.textContent = message || '';
    }

    function validateInput(input) {
        var value = input.value.trim();
        if (input.hasAttribute('data-required') && !value) {
            return input.getAttribute('data-required') || 'This field is required.';
        }
        if (value && input.hasAttribute('data-email')) {
            var ok = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
            if (!ok) return input.getAttribute('data-email') || 'Please enter a valid email address.';
        }
        var matchId = input.getAttribute('data-match');
        if (matchId) {
            var other = document.getElementById(matchId);
            if (other && input.value !== other.value) return input.getAttribute('data-match-message') || 'Values do not match.';
        }
        return '';
    }

    document.addEventListener('submit', function (e) {
        var form = e.target;
        if (!form.matches('[data-auth-form]')) return;

        var firstInvalid = null;
        form.querySelectorAll('input[data-required], input[data-email], input[data-match]').forEach(function (input) {
            var msg = validateInput(input);
            setFieldError(input, msg);
            if (msg && !firstInvalid) firstInvalid = input;
        });

        if (form.hasAttribute('data-password-policy')) {
            var pwd = form.querySelector('[data-password-input]');
            if (pwd && !window.appPassword.meetsPolicy(pwd)) {
                setFieldError(pwd, 'Your password does not meet all the requirements below.');
                if (!firstInvalid) firstInvalid = pwd;
            }
        }

        if (firstInvalid) {
            e.preventDefault();
            firstInvalid.focus();
            return;
        }

        var button = form.querySelector('[data-loading-text]');
        if (button) {
            if (button.disabled) { e.preventDefault(); return; }
            button.disabled = true;
            button.classList.add('is-loading');
            button.innerHTML = '<span class="btn-spinner" aria-hidden="true"></span>' + button.getAttribute('data-loading-text');
        }
    });

    document.addEventListener('input', function (e) {
        var input = e.target;
        if (!input.closest || !input.closest('[data-auth-form]')) return;
        if (input.classList.contains('is-invalid')) setFieldError(input, validateInput(input));
        if (input.hasAttribute('data-password-input')) window.appPassword.update(input);
        // Re-check the confirm field when the password changes
        var confirm = document.querySelector('[data-match="' + input.id + '"]');
        if (confirm && confirm.value) window.appPassword.updateMatch(confirm);
        if (input.hasAttribute('data-match')) window.appPassword.updateMatch(input);
    });

    /* ---------- Password strength + requirements ---------- */
    window.appPassword = {
        rules: function (input) {
            var min = parseInt(input.getAttribute('data-min-length') || '6', 10);
            var v = input.value;
            return {
                length: v.length >= min,
                upper: /[A-Z]/.test(v),
                lower: /[a-z]/.test(v),
                digit: /[0-9]/.test(v),
                special: /[^A-Za-z0-9]/.test(v)
            };
        },
        meetsPolicy: function (input) {
            var r = this.rules(input);
            var form = input.form;
            var ok = true;
            form.querySelectorAll('[data-rule]').forEach(function (li) {
                if (!r[li.getAttribute('data-rule')]) ok = false;
            });
            return ok;
        },
        update: function (input) {
            var r = this.rules(input);
            var form = input.form;
            form.querySelectorAll('[data-rule]').forEach(function (li) {
                var met = !!r[li.getAttribute('data-rule')];
                li.classList.toggle('is-met', met);
                var icon = li.querySelector('i');
                if (icon) icon.className = 'fas ' + (met ? 'fa-check-circle' : 'fa-circle');
            });

            var score = 0;
            if (input.value.length >= 8) score++;
            if (input.value.length >= 12) score++;
            if (r.upper && r.lower) score++;
            if (r.digit) score++;
            if (r.special) score++;
            var levels = [
                { label: 'Too weak', cls: 'weak' },
                { label: 'Weak', cls: 'weak' },
                { label: 'Fair', cls: 'fair' },
                { label: 'Good', cls: 'good' },
                { label: 'Strong', cls: 'strong' },
                { label: 'Very strong', cls: 'strong' }
            ];
            var meter = form.querySelector('.strength');
            if (!meter) return;
            if (!input.value) {
                meter.setAttribute('data-level', '');
                meter.querySelector('.strength__label').textContent = '';
                meter.querySelector('.strength__bar span').style.width = '0';
                return;
            }
            var level = levels[score];
            meter.setAttribute('data-level', level.cls);
            meter.querySelector('.strength__label').textContent = level.label;
            meter.querySelector('.strength__bar span').style.width = Math.max(score, 1) / 5 * 100 + '%';
        },
        updateMatch: function (confirm) {
            var other = document.getElementById(confirm.getAttribute('data-match'));
            var hint = document.getElementById(confirm.id + '-match');
            if (!hint || !other) return;
            if (!confirm.value) { hint.textContent = ''; hint.className = 'auth-field__hint'; return; }
            var ok = confirm.value === other.value;
            hint.innerHTML = ok
                ? '<i class="fas fa-check-circle" aria-hidden="true"></i> Passwords match'
                : '<i class="fas fa-times-circle" aria-hidden="true"></i> Passwords do not match';
            hint.className = 'auth-field__hint ' + (ok ? 'is-ok' : 'is-bad');
        }
    };

    /* ---------- Blazor helpers ---------- */
    window.appUi = {
        // Matches the breakpoint in MainLayout.razor.css where the sidebar becomes a slide-in panel
        isMobileLayout: function () { return window.matchMedia('(max-width: 991.98px)').matches; },
        openInNewTab: function (url) { window.open(url, '_blank', 'noopener'); },
        scrollToTop: function () { window.scrollTo({ top: 0, behavior: 'smooth' }); }
    };
})();
