(function ($) {
  'use strict';

  if (!$) {
    return;
  }

  function initDatePicker(root) {
    var elements = $(root).find('[data-ui-datepicker]');
    if ($.fn.datepicker) {
      elements.datepicker();
    }

    elements.on('change', function () {
      this.dispatchEvent(new Event('input', { bubbles: true }));
    });
  }

  function initCards(root) {
    $(root).find('[data-ui-clickable-card]').each(function () {
      var card = $(this);
      card.css('cursor', 'pointer');
      card.on('click', function () {
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
