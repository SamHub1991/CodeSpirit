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

  register('tabs', function (elements) {
    elements.forEach(function (tabs) {
      markReady([tabs], 'tabs');

      function activate(key) {
        var links = Array.from(tabs.querySelectorAll('.cs-tab[href]'));
        var panels = Array.from(tabs.querySelectorAll('.cs-tab-panel'));
        links.forEach(function (link) {
          var selected = link.getAttribute('href') === '#' + key;
          link.classList.toggle('active', selected);
          link.setAttribute('aria-selected', selected ? 'true' : 'false');
        });
        panels.forEach(function (panel) {
          var selected = panel.getAttribute('id') === key;
          panel.classList.toggle('active', selected);
        });
      }

      tabs.addEventListener('click', function (event) {
        var link = event.target.closest('.cs-tab[href]');
        if (!link || !tabs.contains(link)) {
          return;
        }

        var href = link.getAttribute('href');
        if (!href || href.charAt(0) !== '#') {
          return;
        }

        var key = href.substring(1);
        if (!key) {
          return;
        }

        event.preventDefault();
        activate(key);
        if (window.history && typeof window.history.replaceState === 'function') {
          window.history.replaceState(null, '', '#' + key);
        }
      });

      if (window.location && window.location.hash) {
        var hashKey = window.location.hash.substring(1);
        if (tabs.querySelector('#' + hashKey)) {
          activate(hashKey);
        }
      }
    });
  });

  register('modal', function (elements) {
    elements.forEach(function (modal) {
      markReady([modal], 'modal');

      function close() {
        modal.setAttribute('hidden', '');
      }

      modal.addEventListener('click', function (event) {
        if (event.target === modal || event.target.closest('[data-modal-close]')) {
          close();
        }
      });
    });
  });

  document.addEventListener('click', function (event) {
    var trigger = event.target.closest('[data-modal-target]');
    if (!trigger) {
      return;
    }

    var selector = trigger.getAttribute('data-modal-target');
    var modal = selector ? document.querySelector(selector) : null;
    if (modal) {
      event.preventDefault();
      modal.removeAttribute('hidden');
    }
  });

  document.addEventListener('keydown', function (event) {
    if (event.key !== 'Escape') {
      return;
    }

    document.querySelectorAll('.cs-modal').forEach(function (modal) {
      if (!modal.hasAttribute('hidden')) {
        modal.setAttribute('hidden', '');
      }
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
