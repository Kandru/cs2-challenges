/** Preview theme picker — mirrors gui.theme / HudTheme (palette from theme-data.js). */
(function () {
  const DATA = window.HUD_THEMES || {};
  const THEMES = Object.keys(DATA);
  const CLASS_RE = /\btheme-[a-z0-9]+\b/g;

  function colors(name) {
    return DATA[name] || DATA.gold;
  }

  function currentTheme(root) {
    for (const name of THEMES) {
      if (root.classList.contains(`theme-${name}`)) {
        return name;
      }
    }
    return 'gold';
  }

  function applyAccentVars(stage, c) {
    stage.style.setProperty('--accent', c.accent);
    stage.style.setProperty('--accent-from', c.from);
    stage.style.setProperty('--accent-to', c.to);
    stage.style.setProperty('--accent-18', `${c.accent}18`);
    stage.style.setProperty('--accent-22', `${c.accent}22`);
    stage.style.setProperty('--accent-44', `${c.accent}44`);
    stage.style.setProperty('--accent-66', `${c.accent}66`);
    stage.style.setProperty('--preview-accent', c.accent);
  }

  function applyTheme(root, stage, name, buttons) {
    const theme = DATA[name] ? name : 'gold';
    const c = colors(theme);
    root.className = root.className.replace(CLASS_RE, '').replace(/\s+/g, ' ').trim();
    root.classList.add(`theme-${theme}`);
    stage.dataset.theme = theme;
    applyAccentVars(stage, c);
    buttons.forEach((btn) => {
      const active = btn.dataset.theme === theme;
      btn.classList.toggle('active', active);
      if (active) {
        btn.style.setProperty('--swatch', c.accent);
      }
    });
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
    if (!root || !stage || !host || !THEMES.length) {
      return;
    }

    host.innerHTML = `
      <div class="preview-note">Theme (gui.theme)</div>
      <div class="preview-theme-row">
        ${THEMES.map((t) => {
          const accent = colors(t).accent;
          return `<button type="button" class="preview-theme" data-theme="${t}" title="${t}" aria-label="${t}" style="--swatch:${accent};background:${accent}"></button>`;
        }).join('')}
      </div>
    `;
    const buttons = [...host.querySelectorAll('.preview-theme')];
    let initial = 'gold';
    try {
      const saved = localStorage.getItem('challenges-preview-theme');
      if (saved && DATA[saved]) {
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
