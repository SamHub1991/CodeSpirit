(function ($) {
  'use strict';

  if (!$) {
    return;
  }

  function findBehavior(root, query) {
    return $(root).find(query).addBack(query);
  }

  var behaviors = {};

  function markReady(elements, name) {
    elements.attr('data-ui-ready', function (_, value) {
      return [value, name].filter(Boolean).join(' ');
    });
  }

  function getPending(root, name) {
    return findBehavior(root, '[data-ui~="' + name + '"]').not('[data-ui-ready~="' + name + '"]');
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
    if ($.fn.datepicker) {
      elements.datepicker();
    }

    markReady(elements, 'datepicker');

    elements.on('change', function () {
      window.CodeSpirit.input(this);
    });
  });

  register('clickable-card', function (elements) {
    elements.each(function () {
      var card = $(this);
      markReady(card, 'clickable-card');
      card.css('cursor', 'pointer');
      card.on('click', function (event) {
        if ($(event.target).closest('a, button, input, select, textarea').length) {
          return;
        }

        var link = card.find('a[href]').first();
        if (link.length) {
          window.location.href = link.attr('href');
        }
      });
    });
  });

  function init(root) {
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

  $(function () {
    refresh(document);
  });

  $(document).on('codespirit:updated', function (event) {
    refresh(event.target);
  });
})(window.jQuery || window.$);
