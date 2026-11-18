// Shared keyboard behaviour for the existing pickers and chart tooltips.
function positionChartTooltip(point) {
    const tooltip = point.querySelector('.price-tooltip, .history-point-tooltip');
    const card = point.closest('.chart-card, .history-chart-card');
    if (!tooltip || !card) return;
    tooltip.style.translate = '0 0';
    const bounds = tooltip.getBoundingClientRect(), container = card.getBoundingClientRect();
    const left = Math.max(8, container.left + 8), right = Math.min(innerWidth - 8, container.right - 8);
    const shift = Math.max(left - bounds.left, 0) - Math.max(bounds.right - right, 0);
    tooltip.style.translate = `${shift}px 0`;
}
window.smartHomeAccessibility = {
    focusSelectedOption(picker) {
        const options = [...picker.querySelectorAll('[role="option"]')];
        const selected = options.find(option => option.getAttribute('aria-selected') === 'true');
        (selected || options[0])?.focus();
    },
    focus(element) { element?.focus(); },
    openDialog(dialog) { dialog.showModal(); },
    closeDialog(dialog) { dialog.close(); }
};

document.addEventListener('keydown', event => {
    const chartPoint = event.target.closest('[data-chart-point]');
    if (chartPoint && ['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) {
        event.preventDefault();
        const points = [...chartPoint.closest('[data-chart]').querySelectorAll('[data-chart-point]')];
        const index = event.key === 'Home' ? 0 : event.key === 'End' ? points.length - 1
            : Math.max(0, Math.min(points.length - 1, points.indexOf(chartPoint) + (event.key === 'ArrowRight' ? 1 : -1)));
        points.forEach((point, i) => point.tabIndex = i === index ? 0 : -1);
        points[index].focus();
    }
    const picker = event.target.closest('[data-picker]');
    if (picker) {
        const trigger = picker.querySelector('[aria-haspopup="listbox"]');
        const options = [...picker.querySelectorAll('[role="option"]')];
        const index = options.indexOf(document.activeElement);
        if (event.key === 'Escape' && trigger.getAttribute('aria-expanded') === 'true') {
            event.preventDefault();
            event.stopPropagation();
            trigger.click();
            trigger.focus();
        } else if (['ArrowDown', 'ArrowUp', 'Home', 'End'].includes(event.key)) {
            event.preventDefault();
            if (!options.length) trigger.click();
            else {
                const next = event.key === 'Home' ? 0 : event.key === 'End' ? options.length - 1
                    : (index + (event.key === 'ArrowDown' ? 1 : -1) + options.length) % options.length;
                options[next].focus();
            }
        } else if (event.key.length === 1 && event.key !== ' ' && !event.ctrlKey && !event.metaKey && !event.altKey) {
            const ordered = [...options.slice(index + 1), ...options.slice(0, index + 1)];
            ordered.find(option => option.textContent.trim().toLocaleLowerCase().startsWith(event.key.toLocaleLowerCase()))?.focus();
        }
    }

    const radio = event.target.closest('[role="radio"]');
    if (radio && ['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown', 'Home', 'End'].includes(event.key)) {
        event.preventDefault();
        const options = [...radio.closest('[role="radiogroup"]').querySelectorAll('[role="radio"]')];
        const direction = ['ArrowLeft', 'ArrowUp'].includes(event.key) ? -1 : 1;
        const index = event.key === 'Home' ? 0 : event.key === 'End' ? options.length - 1
            : (options.indexOf(radio) + direction + options.length) % options.length;
        options[index].click();
        options[index].focus();
    }

    if (event.key === 'Escape') {
        document.querySelectorAll('[data-chart-point]').forEach(point => point.classList.add('tooltip-dismissed'));
    }
});

document.addEventListener('focusout', event => {
    const picker = event.target.closest('[data-picker]');
    if (picker && !picker.contains(event.relatedTarget)) {
        const trigger = picker.querySelector('[aria-haspopup="listbox"][aria-expanded="true"]');
        trigger?.click();
    }
});

document.addEventListener('pointerdown', event => {
    document.querySelectorAll('[data-picker]').forEach(picker => {
        if (!picker.contains(event.target)) picker.querySelector('[aria-haspopup="listbox"][aria-expanded="true"]')?.click();
    });
    const point = event.target.closest('[data-chart-point]');
    if (point) {
        point.classList.remove('tooltip-dismissed');
        point.focus();
    } else {
        document.querySelectorAll('[data-chart-point]').forEach(item => item.classList.add('tooltip-dismissed'));
    }
});

for (const type of ['focusin', 'pointerover']) {
    document.addEventListener(type, event => {
        const point = event.target.closest('[data-chart-point]');
        if (point && !point.contains(event.relatedTarget)) {
            point.classList.remove('tooltip-dismissed');
            positionChartTooltip(point);
        }
    });
}

document.addEventListener('click', event => {
    const link = event.target.closest('.form-errors a');
    if (!link) return;
    const field = document.getElementById(link.hash.slice(1));
    if (field) {
        event.preventDefault();
        field.focus();
    }
});

// Native HTML account forms keep browser validation and add a linked error summary.
document.addEventListener('invalid', event => {
    const form = event.target.closest('form[data-account-form]');
    if (!form) return;
    event.preventDefault();
    queueMicrotask(() => {
        const summary = form.querySelector('[data-native-errors]');
        const list = summary.querySelector('ul');
        list.replaceChildren();
        for (const control of form.querySelectorAll('input:invalid')) {
            const item = document.createElement('li');
            const link = document.createElement('a');
            link.href = '#' + control.id;
            const label = form.querySelector(`label[for="${control.id}"]`);
            link.textContent = `${label?.textContent || control.name}: ${control.validationMessage}`;
            item.append(link);
            list.append(item);
            control.setAttribute('aria-invalid', 'true');
            const errorId = control.id + '-native-error';
            let error = document.getElementById(errorId);
            if (!error) {
                error = document.createElement('span');
                error.id = errorId;
                error.className = 'text-danger small';
                control.after(error);
            }
            error.textContent = control.validationMessage;
            const descriptions = new Set((control.getAttribute('aria-describedby') || '').split(/\s+/).filter(Boolean));
            descriptions.add(errorId);
            control.setAttribute('aria-describedby', [...descriptions].join(' '));
        }
        summary.hidden = !list.children.length;
        if (!summary.hidden) summary.focus();
    });
}, true);

document.addEventListener('input', event => {
    if (event.target.closest('form[data-account-form]') && event.target.validity.valid) {
        event.target.removeAttribute('aria-invalid');
        const error = document.getElementById(event.target.id + '-native-error');
        if (error) error.textContent = '';
    }
});

smartHomeAccessibility.watchSession = dialog => {
    const continueButton = dialog.querySelector('[data-session-continue]');
    const dismissButton = dialog.querySelector('[data-session-dismiss]');
    const login = dialog.querySelector('[data-session-login]');
    const message = dialog.querySelector('#session-warning-message');
    const heading = dialog.querySelector('h2');
    let warnedExpiry, stopped = false, checking = false;
    const expired = () => {
        heading.textContent = 'Your session has expired';
        message.textContent = 'Sign in again before saving changes. Your current page will stay open until you choose to sign in.';
        continueButton.hidden = true;
        login.hidden = false;
        if (!dialog.open) dialog.showModal();
        login.focus();
    };
    const check = async () => {
        if (!dialog.isConnected) { dialog.stopSessionWatch(); return; }
        if (stopped || checking || document.hidden) return;
        checking = true;
        try {
            const response = await fetch('/account/session', { credentials: 'same-origin', cache: 'no-store' });
            if (stopped) return;
            if (response.status === 401) { clearInterval(dialog.sessionTimer); expired(); return; }
            if (!response.ok) return;
            const { expiresUtc } = await response.json();
            if (!expiresUtc) return;
            const remaining = new Date(expiresUtc).getTime() - Date.now();
            if (remaining <= 0) expired();
            else if (remaining <= 120000 && warnedExpiry !== expiresUtc) {
                warnedExpiry = expiresUtc;
                if (!dialog.open) dialog.showModal();
            } else if (remaining > 120000 && dialog.open) dialog.close();
        } catch { /* A temporary network failure must not discard the open form. */ }
        finally { checking = false; }
    };
    continueButton.addEventListener('click', async () => {
        continueButton.disabled = true;
        try {
            const token = dialog.querySelector('input[name="__RequestVerificationToken"]');
            const response = await fetch('/account/session/continue', {
                method: 'POST', credentials: 'same-origin',
                body: new URLSearchParams({ __RequestVerificationToken: token.value })
            });
            if (stopped) return;
            if (response.status === 401) { expired(); return; }
            if (!response.ok) throw new Error();
            dialog.close();
            warnedExpiry = null;
        } catch {
            message.textContent = 'The session could not be extended. Check your connection and try again.';
        } finally { continueButton.disabled = false; }
    });
    dismissButton.addEventListener('click', () => dialog.close());
    dialog.sessionTimer = setInterval(check, 30000);
    document.addEventListener('visibilitychange', check);
    dialog.stopSessionWatch = () => {
        stopped = true;
        clearInterval(dialog.sessionTimer);
        document.removeEventListener('visibilitychange', check);
    };
    check();
};
smartHomeAccessibility.stopSessionWatch = dialog => dialog?.stopSessionWatch?.();
