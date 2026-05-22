(function () {
    const STORAGE_KEY = 'liteqms-theme';
    const DEFAULT_THEME = 'teal';

    function getTheme() {
        return localStorage.getItem(STORAGE_KEY) || DEFAULT_THEME;
    }

    function applyTheme(theme) {
        document.documentElement.dataset.theme = theme;
    }

    function setTheme(theme) {
        localStorage.setItem(STORAGE_KEY, theme);
        applyTheme(theme);
        updateSwitcherUI(theme);
    }

    function updateSwitcherUI(theme) {
        document.querySelectorAll('.theme-btn').forEach(function (btn) {
            var isActive = btn.dataset.theme === theme;
            btn.classList.toggle('active', isActive);
            btn.setAttribute('aria-pressed', isActive);
        });
    }

    var initial = getTheme();
    applyTheme(initial);

    document.addEventListener('DOMContentLoaded', function () {
        updateSwitcherUI(initial);

        document.querySelectorAll('.theme-btn').forEach(function (btn) {
            btn.addEventListener('click', function () {
                setTheme(this.dataset.theme);
            });
        });
    });

    window.setTheme = setTheme;
})();
