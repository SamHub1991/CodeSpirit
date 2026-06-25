(function () {
  'use strict';

  var analyzers = {};

  function getTextValue(el) {
    if (el.tagName === 'INPUT' || el.tagName === 'SELECT' || el.tagName === 'TEXTAREA') {
      return el.value;
    }
    return (el.textContent || '').trim();
  }

  function getNumericValue(el) {
    var raw = getTextValue(el);
    if (!raw) return NaN;
    var cleaned = raw.replace(/[^\d.\-+]/g, '');
    var num = parseFloat(cleaned);
    return isNaN(num) ? NaN : num;
  }

  function clearIntentClasses(el) {
    var toRemove = [];
    var list = el.classList;
    for (var i = 0; i < list.length; i++) {
      if (list[i].startsWith('intent-')) {
        toRemove.push(list[i]);
      }
    }
    toRemove.forEach(function (c) { el.classList.remove(c); });
  }

  function applyIntentTone(el, tone) {
    if (!tone || tone === 'default') return;

    clearIntentClasses(el);
    el.classList.add('intent-' + tone);

    if (el.hasAttribute('data-cs-tone')) return;

    var baseClass = el.classList[0] || '';
    if (!baseClass) return;

    var prefixDash = baseClass + '-';
    Array.from(el.classList).forEach(function (c) {
      if (c !== baseClass && c.startsWith(prefixDash)) {
        el.classList.remove(c);
      }
    });

    el.classList.add(baseClass + '-' + tone);
  }

  // ============================
  // numeric — tier by distribution
  // ============================
  analyzers.numeric = function (elements, root) {
    var pairs = [];
    elements.forEach(function (el) {
      var num = getNumericValue(el);
      if (!isNaN(num)) pairs.push({ el: el, val: num });
    });

    if (pairs.length === 0) return;

    var sorted = pairs.map(function (p) { return p.val; }).sort(function (a, b) { return a - b; });
    var len = sorted.length;
    var p30 = sorted[Math.floor(len * 0.30)];
    var p70 = sorted[Math.floor(len * 0.70)];

    if (sorted.length === 1) {
      applyIntentTone(pairs[0].el, pairs[0].val > 0 ? 'success' : pairs[0].val < 0 ? 'danger' : 'muted');
      return;
    }

    pairs.forEach(function (item) {
      var tone;
      if (item.val > p70) tone = 'success';
      else if (item.val > p30) tone = 'warning';
      else if (item.val < 0) tone = 'danger';
      else tone = 'muted';

      applyIntentTone(item.el, tone);
    });
  };

  // ============================
  // status — keyword → tone
  // ============================
  var STATUS_MAP = {
    overdue: 'danger', late: 'danger', error: 'danger', failed: 'danger', cancelled: 'danger',
    blocked: 'danger', rejected: 'danger', expired: 'danger', suspended: 'danger',
    available: 'success', active: 'success', success: 'success', ok: 'success',
    completed: 'success', approved: 'success', verified: 'success', ready: 'success',
    reserved: 'warning', pending: 'warning', waiting: 'warning', processing: 'warning',
    review: 'warning', attention: 'warning', hold: 'warning', flagged: 'warning',
    borrowed: 'info', archived: 'info', inactive: 'info', draft: 'info',
    scheduled: 'info', noted: 'info', seen: 'info'
  };

  analyzers.status = function (elements, root) {
    elements.forEach(function (el) {
      var val = getTextValue(el).toLowerCase().replace(/[^a-z]/g, '');
      var tone = STATUS_MAP[val] || 'default';
      applyIntentTone(el, tone);
    });
  };

  // ============================
  // due — days remaining → urgency
  // ============================
  analyzers.due = function (elements, root) {
    var now = Date.now();
    var DAY = 24 * 60 * 60 * 1000;

    elements.forEach(function (el) {
      var val = getTextValue(el);
      if (!val) return;

      var date = new Date(val);
      if (isNaN(date.getTime())) return;

      var daysLeft = Math.ceil((date.getTime() - now) / DAY);
      var tone;
      if (daysLeft < 0) tone = 'danger';
      else if (daysLeft <= 3) tone = 'warning';
      else if (daysLeft <= 14) tone = 'info';
      else tone = 'success';

      applyIntentTone(el, tone);
    });
  };

  // ============================
  // trend — sibling comparison
  // ============================
  analyzers.trend = function (elements, root) {
    var prev = NaN;
    elements.forEach(function (el) {
      var curr = getNumericValue(el);
      if (isNaN(curr)) return;

      var tone;
      if (!isNaN(prev)) {
        if (curr > prev) tone = 'success';
        else if (curr < prev) tone = 'danger';
        else tone = 'muted';
      } else {
        tone = 'info';
      }
      prev = curr;

      applyIntentTone(el, tone);
    });
  };

  // ============================
  // Public API
  // ============================
  function register(name, initializer) {
    if (!name || typeof initializer !== 'function') return;
    if (!/^[A-Za-z0-9_-]+$/.test(name)) {
      throw new Error('Invalid CodeSpirit intent analyzer name: ' + name);
    }
    analyzers[name] = initializer;
  }

  function analyze(root) {
    root = root || document;
    Object.keys(analyzers).forEach(function (name) {
      var selector = '[data-cs-intent~="' + name + '"]';
      var elements = Array.from(root.querySelectorAll(selector));
      if (elements.length) {
        analyzers[name](elements, root);
      }
    });
  }

  window.CodeSpirit = window.CodeSpirit || {};
  window.CodeSpirit.intent = window.CodeSpirit.intent || {};
  window.CodeSpirit.intent.analyze = analyze;
  window.CodeSpirit.intent.register = register;

  document.addEventListener('DOMContentLoaded', function () {
    analyze(document);
  });

  document.addEventListener('codespirit:updated', function (event) {
    analyze(event.target);
  });
})();
