(function () {
    const debounce = (fn, ms) => {
        let timer;
        return (...args) => {
            clearTimeout(timer);
            timer = setTimeout(() => fn(...args), ms);
        };
    };

    const submitForm = form => {
        if (typeof form.requestSubmit === 'function') {
            form.requestSubmit();
            return;
        }

        form.submit();
    };

    document.querySelectorAll('form[data-auto-search]').forEach(form => {
        const submitSearch = debounce(() => {
            const pageInput = form.querySelector('input[name="page"]');
            if (pageInput) {
                pageInput.value = '1';
            }
            submitForm(form);
        }, 400);

        form.querySelectorAll('input:not([type]), input[type="text"], input[type="search"], input[type="date"], select').forEach(el => {
            el.addEventListener('input', submitSearch);
            el.addEventListener('change', submitSearch);
        });

        const clearButton = form.querySelector('[data-clear-filters]');
        if (clearButton) {
            clearButton.addEventListener('click', e => {
                e.preventDefault();
                form.querySelectorAll('input:not([type]), input[type="text"], input[type="search"], input[type="date"]').forEach(el => {
                    el.value = '';
                });
                form.querySelectorAll('select').forEach(el => {
                    el.selectedIndex = 0;
                });
                const pageInput = form.querySelector('input[name="page"]');
                if (pageInput) {
                    pageInput.value = '1';
                }
                submitForm(form);
            });
        }
    });

    document.querySelectorAll('.page-size-select').forEach(select => {
        select.addEventListener('change', () => {
            const params = new URLSearchParams(window.location.search);
            params.set('pageSize', select.value);
            params.set('page', '1');
            window.location.search = params.toString();
        });
    });
})();
