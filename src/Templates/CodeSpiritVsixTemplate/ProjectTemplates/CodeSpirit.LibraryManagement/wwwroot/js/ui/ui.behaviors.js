(function () {
  'use strict';

  var behaviors = {};

  function markReady(elements, name) {
    elements.forEach(function (el) {
      var current = el.getAttribute('data-ui-ready') || '';
      var values = current.split(/\s+/).filter(Boolean);
      if (values.indexOf(name) === -1) {
        values.push(name);
        el.setAttribute('data-ui-ready', values.join(' '));
      }
    });
  }

  function getPending(root, name) {
    var selector = '[data-ui~="' + name + '"]';
    return Array.from((root || document).querySelectorAll(selector)).filter(function (el) {
      var ready = el.getAttribute('data-ui-ready') || '';
      return ready.split(/\s+/).indexOf(name) === -1;
    });
  }

  function register(name, initializer) {
    if (!name || typeof initializer !== 'function') {
      return;
    }

    if (!/^[A-Za-z0-9_-]+$/.test(name)) {
      throw new Error('Invalid CodeSpirit UI behavior name: ' + name);
    }

    behaviors[name] = initializer;
  }

  register('datepicker', function (elements) {
    elements.forEach(function (el) {
      markReady([el], 'datepicker');

      if (el.tagName === 'INPUT') {
        el.setAttribute('type', 'date');
      }

      el.addEventListener('change', function () {
        if (window.CodeSpirit && typeof window.CodeSpirit.input === 'function') {
          window.CodeSpirit.input(this);
        }
      });
    });
  });

  register('clickable-card', function (elements) {
    elements.forEach(function (card) {
      markReady([card], 'clickable-card');
      card.style.cursor = 'pointer';
      card.addEventListener('click', function (event) {
        if (event.target.closest('a, button, input, select, textarea')) {
          return;
        }

        var link = card.querySelector('a[href]');
        if (link) {
          window.location.href = link.getAttribute('href');
        }
      });
    });
  });

  register('confirm-click', function (elements) {
    elements.forEach(function (el) {
      markReady([el], 'confirm-click');
      var message = el.getAttribute('data-confirm') || 'Are you sure?';
      el.addEventListener('click', function (event) {
        if (!confirm(message)) {
          event.preventDefault();
          event.stopImmediatePropagation();
        }
      });
    });
  });

  register('auto-submit', function (elements) {
    elements.forEach(function (el) {
      markReady([el], 'auto-submit');
      var timer = null;
      var delay = parseInt(el.getAttribute('data-debounce'), 10) || 300;

      el.addEventListener('input', function () {
        clearTimeout(timer);
        timer = setTimeout(function () {
          var form = el.closest('form[data-cs-vm]');
          if (form && window.CodeSpirit && typeof window.CodeSpirit.vm === 'function') {
            var chain = window.CodeSpirit.vm(form);
            chain.invoke();
            chain.destroy();
          }
        }, delay);
      });

      el.addEventListener('change', function () {
        clearTimeout(timer);
        var form = el.closest('form[data-cs-vm]');
        if (form && window.CodeSpirit && typeof window.CodeSpirit.vm === 'function') {
          var chain = window.CodeSpirit.vm(form);
          chain.invoke();
          chain.destroy();
        }
      });
    });
  });

  function init(root) {
    root = root || document;
    Object.keys(behaviors).forEach(function (name) {
      var elements = getPending(root, name);
      if (elements.length) {
        behaviors[name](elements, root);
      }
    });
  }

  function refresh(root) {
    if (window.CodeSpirit && typeof window.CodeSpirit.refresh === 'function') {
      window.CodeSpirit.refresh(root);
      return;
    }

    init(root);
  }

  window.CodeSpirit = window.CodeSpirit || {};
  window.CodeSpirit.ui = window.CodeSpirit.ui || {};
  window.CodeSpirit.ui.init = init;
  window.CodeSpirit.ui.register = register;
  window.CodeSpirit.ui.ready = markReady;

  document.addEventListener('DOMContentLoaded', function () {
    refresh(document);
  });

  document.addEventListener('codespirit:updated', function (event) {
    refresh(event.target);
  });
})();
