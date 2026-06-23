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
});
