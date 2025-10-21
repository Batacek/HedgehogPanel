
const Routes = {
  home: "/html/components/MainContent/Home.html",
  servers: "/html/components/MainContent/Servers.html",
  services: "/html/components/MainContent/Services.html",
};

function setActiveNav(pageKey) {
  const sidebar = document.getElementById("sidebar");
  if (!sidebar) return;
  const buttons = sidebar.querySelectorAll('.nav-button[data-page]');
  buttons.forEach(btn => {
    if (btn.dataset.page === pageKey) {
      btn.classList.add('active');
      btn.setAttribute('aria-current', 'page');
    } else {
      btn.classList.remove('active');
      btn.removeAttribute('aria-current');
    }
  });
}

function resolvePageUrl(pageKeyOrPath) {
  if (Routes[pageKeyOrPath]) return Routes[pageKeyOrPath];
  if (pageKeyOrPath.includes('/') || pageKeyOrPath.endsWith('.html')) return pageKeyOrPath;
  const cap = pageKeyOrPath.charAt(0).toUpperCase() + pageKeyOrPath.slice(1).toLowerCase();
  return `/html/components/MainContent/${cap}.html`;
}

function normalizeRouteKey(input) {
  if (!input) return null;
  const val = String(input).trim();
  const lower = val.toLowerCase();
  // Match case-insensitively to known route keys
  const key = Object.keys(Routes).find(k => k.toLowerCase() === lower);
  if (key) return key;
  // If a path was provided, map it back to a key
  if (lower.includes('/') || lower.endsWith('.html')) {
    const match = Object.keys(Routes).find(k => resolvePageUrl(k) === resolvePageUrl(val));
    if (match) return match;
  }
  // Try to derive from basename (e.g., "/.../Home.html" → "home")
  const base = lower.replace(/\\/g, '/').split('/').pop().replace('.html', '');
  const key2 = Object.keys(Routes).find(k => k.toLowerCase() === base);
  return key2 || null;
}

async function loadPage(pageKeyOrPath) {
  const intendedKey = normalizeRouteKey(pageKeyOrPath);
  if (intendedKey) setActiveNav(intendedKey);
  const url = resolvePageUrl(pageKeyOrPath);
  try {
    const res = await fetch(url, { cache: 'no-cache' });
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    const html = await res.text();
    const el = document.getElementById('main-content');
    if (el) el.innerHTML = html;
    console.log('Loaded page', url);
  } catch (e) {
    console.error('Failed to load page', url, e);
    const el = document.getElementById('main-content');
    if (el) el.innerHTML = `<div class="card"><h2>Load error</h2><p>Could not load: ${url}</p></div>`;
  }
  const keyFromUrl = Object.keys(Routes).find(k => resolvePageUrl(k) === url) || null;
  if (keyFromUrl && keyFromUrl !== intendedKey) setActiveNav(keyFromUrl);
}

async function loadComponent(targetId, url) {
  try {
    const res = await fetch(url, { cache: 'no-cache' });
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    const html = await res.text();
    const el = document.getElementById(targetId);
    if (el) el.innerHTML = html;
  } catch (e) {
    console.error('Failed to load component', url, e);
  }
}

async function boot() {
  await Promise.all([
    loadComponent('sidebar', '/html/components/sidebar.html'),
    loadComponent('topbar', '/html/components/topbar.html')
  ]);
  initTopbarInteractions();
  await loadPage('home');
}

function initTopbarInteractions() {
  const topbar = document.getElementById('topbar');
  if (!topbar) return;
  const trigger = topbar.querySelector('#user-menu-trigger');
  const menu = topbar.querySelector('#user-menu');
  if (!trigger || !menu) return;
  const container = trigger.closest('.user-menu-container') || topbar;

  let isOpen = false;

  const openMenu = () => {
    if (isOpen) return;
    menu.hidden = false;
    requestAnimationFrame(() => menu.classList.add('open'));
    trigger.setAttribute('aria-expanded', 'true');
    isOpen = true;
    document.addEventListener('click', onDocumentClick, true);
    document.addEventListener('keydown', onKeyDown);
  };

  const closeMenu = () => {
    if (!isOpen) return;
    menu.classList.remove('open');
    trigger.setAttribute('aria-expanded', 'false');
    isOpen = false;
    document.removeEventListener('click', onDocumentClick, true);
    document.removeEventListener('keydown', onKeyDown);
    setTimeout(() => { if (!isOpen) menu.hidden = true; }, 130);
  };

  const toggleMenu = () => (isOpen ? closeMenu() : openMenu());

  const onDocumentClick = (e) => {
    if (!container.contains(e.target)) {
      closeMenu();
    }
  };

  const onKeyDown = (e) => {
    if (e.key === 'Escape') {
      closeMenu();
      trigger.focus();
    }
  };

  trigger.addEventListener('click', (e) => {
    e.stopPropagation();
    toggleMenu();
  });
  trigger.addEventListener('keydown', (e) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      toggleMenu();
    }
  });

  menu.addEventListener('click', (e) => {
    const item = e.target.closest('.menu-item');
    if (!item) return;
    const action = item.dataset.action;
    console.log('User menu action:', action);
    closeMenu();
  });
}

window.addEventListener('DOMContentLoaded', boot);

window.loadPage = loadPage;
