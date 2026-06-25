// ===================================
// CodeSpirit Site Scripts
// ===================================
// data-cs-* is owned by MVVM runtime; data-ui is owned by jQuery behaviors.
// Chainable API: CodeSpirit.vm(formElement)
//   .set('key', val) / .set({k:v})  -> this
//   .get('key')                      -> value
//   .val('key') / .val('key', val)   -> value | this
//   .state()                         -> { k: v }
//   .reset([names])                  -> this
//   .invoke('Command', opts)         -> Promise<result>
//   .observe(fields, fn)             -> this
//   .on('event', fn) / .once()       -> this
//   .destroy()                       -> void
//   .refresh()                       -> this
//   .el(selector) / .all(selector)   -> DOM scoped queries

document.addEventListener('DOMContentLoaded', function () {
  document.querySelectorAll('form[data-cs-vm]').forEach(function (form) {
    window.CodeSpirit.vm(form);
  });

  var params = new URLSearchParams(window.location.search);
  var devActive = localStorage.getItem('cs_dev_mode') === 'true';
  var isLocalDev = window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1';
  if (params.get('dev') === '1' || devActive) {
    if (isLocalDev && window.__CS_DEV__ && typeof window.__CS_DEV__.init === 'function') {
      window.__CS_DEV__.init();
    }
    if (isLocalDev && !devActive) localStorage.setItem('cs_dev_mode', 'true');
  }
  if (params.get('dev') === '0') {
    localStorage.removeItem('cs_dev_mode');
    if (window.__CS_DEV__ && typeof window.__CS_DEV__.destroy === 'function') {
      window.__CS_DEV__.destroy();
    }
  }
});

document.addEventListener('keydown', function (e) {
  if (e.ctrlKey && e.shiftKey && e.key === 'D') {
    e.preventDefault();
    if ((window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') && window.__CS_DEV__) {
      if (document.getElementById('cs-dev-panel')) {
        window.__CS_DEV__.destroy();
        localStorage.removeItem('cs_dev_mode');
      } else {
        window.__CS_DEV__.init();
        localStorage.setItem('cs_dev_mode', 'true');
      }
    }
  }
});
