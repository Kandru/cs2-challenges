/** Preview theme picker — mirrors gui.theme / HudTheme names. */
(function () {
  const THEMES = ['gold', 'ct', 't', 'green', 'red', 'purple'];
  const CLASS_RE = /\btheme-(?:gold|ct|t|green|red|purple)\b/g;

  function currentTheme(root) {
    for (const name of THEMES) {
      if (root.classList.contains(`theme-${name}`)) {
        return name;
      }
    }
    return 'gold';
  }

  function applyTheme(root, stage, name, buttons) {
    const theme = THEMES.includes(name) ? name : 'gold';
    root.className = root.className.replace(CLASS_RE, '').replace(/\s+/g, ' ').trim();
    root.classList.add(`theme-${theme}`);
    stage.dataset.theme = theme;
    buttons.forEach((btn) => btn.classList.toggle('active', btn.dataset.theme === theme));
    try {
      localStorage.setItem('challenges-preview-theme', theme);
    } catch (_) {
      /* ignore */
    }
  }

  function mount(rootSelector) {
    const root = document.querySelector(rootSelector);
    const stage = document.querySelector('.stage');
    const host = document.getElementById('preview-themes');
    if (!root || !stage || !host) {
      return;
    }

    host.innerHTML = `
      <div class="preview-note">Theme (gui.theme)</div>
      <div class="preview-theme-row">
        ${THEMES.map((t) => `<button type="button" class="preview-theme" data-theme="${t}" title="${t}" aria-label="${t}"></button>`).join('')}
      </div>
    `;
    const buttons = [...host.querySelectorAll('.preview-theme')];
    let initial = 'gold';
    try {
      const saved = localStorage.getItem('challenges-preview-theme');
      if (saved && THEMES.includes(saved)) {
        initial = saved;
      }
    } catch (_) {
      initial = currentTheme(root);
    }
    applyTheme(root, stage, initial, buttons);
    buttons.forEach((btn) => {
      btn.addEventListener('click', () => applyTheme(root, stage, btn.dataset.theme, buttons));
    });
  }

  window.PreviewTheme = { mount, themes: THEMES };
})();
