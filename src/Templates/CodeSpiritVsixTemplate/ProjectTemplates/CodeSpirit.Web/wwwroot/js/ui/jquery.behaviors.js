(function ($) {
  'use strict';

  if (!$) {
    return;
  }

  function findBehavior(root, query) {
    return $(root).find(query).addBack(query);
  }

  function initDatePicker(root) {
    var elements = findBehavior(root, '[data-ui~="datepicker"]').not('[data-ui-ready~="datepicker"]');
    if ($.fn.datepicker) {
      elements.datepicker();
    }

    elements.attr('data-ui-ready', function (_, value) {
      return [value, 'datepicker'].filter(Boolean).join(' ');
    });

    elements.on('change', function () {
      window.CodeSpirit.input(this);
    });
  }

  function initCards(root) {
    findBehavior(root, '[data-ui~="clickable-card"]').not('[data-ui-ready~="clickable-card"]').each(function () {
      var card = $(this);
      card.attr('data-ui-ready', function (_, value) {
        return [value, 'clickable-card'].filter(Boolean).join(' ');
      });
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
  }

  function init(root) {
    initDatePicker(root);
    initCards(root);
  }

  $(function () {
    init(document);
  });

  $(document).on('codespirit:updated', function (event) {
    init(event.target);
  });
})(window.jQuery || window.$);
