// ===================================
// CodeSpirit Site Scripts
// ===================================
// data-cs-* is owned by MVVM runtime; data-ui is owned by jQuery behaviors.
// Chainable API: CodeSpirit.vm(formElement)
//   .set('key', val) / .set({k:v})  -> this
//   .val('key')                     -> value
//   .invoke('Command')              -> Promise<result>
//   .on('event', fn) / .once()      -> this
//   .refresh()                      -> this
//   .el(selector) / .all(selector)  -> DOM scoped queries

document.addEventListener('DOMContentLoaded', function () {
  console.log('[CodeSpirit] Application initialized');

  document.querySelectorAll('form[data-cs-vm]').forEach(function (form) {
    var vm = window.CodeSpirit.vm(form);

    vm.on('codespirit:updated', function (e) {
      form.classList.remove('cs-loading');
      console.log('[CodeSpirit] ViewModel updated');
    });

    vm.on('codespirit:error', function (e) {
      form.classList.remove('cs-loading');
      console.error('[CodeSpirit] ViewModel error:', e.detail);
    });

    vm.on('codespirit:changed', function (e) {
      console.debug('[CodeSpirit] Field changed:', e.detail.name, '=' + e.detail.value);
    });

    form.addEventListener('submit', function () {
      form.classList.add('cs-loading');
    });
  });
});