// src/TracLabs.Frontend/wwwroot/js/theme.js
window.traclabsTheme = {
    setThemeAttributes: function (theme, style) {
        if (theme) {
            document.documentElement.setAttribute('data-theme', theme);
            try { localStorage.setItem('traclabs_theme', theme); } catch (e) { }
        }
        document.documentElement.setAttribute('data-style', 'precision-slate');
        try { localStorage.setItem('traclabs_style', 'precision-slate'); } catch (e) { }
    },
    getThemeAttributes: function () {
        var theme = 'light';
        try {
            theme = localStorage.getItem('traclabs_theme');
            if (!theme) {
                theme = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
            }
        } catch (e) { }
        return { theme: theme, style: 'precision-slate' };
    }
};
