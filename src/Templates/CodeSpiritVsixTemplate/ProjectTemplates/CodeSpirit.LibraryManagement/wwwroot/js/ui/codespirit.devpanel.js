(function () {
  'use strict';

  if (typeof document === 'undefined') return;

  var PANEL_WIDTH = 360;
  var panel = null;
  var elements = [];
  var highlightEl = null;
  var activeIdx = -1;

  function init() {
    if (panel) return;

    panel = document.createElement('div');
    panel.id = 'cs-dev-panel';
    panel.innerHTML = [
      '<div class="cs-dev-header">',
        '<span>CodeSpirit Dev Panel</span>',
        '<div>',
          '<button id="cs-dev-refresh">Refresh</button>',
          '<button id="cs-dev-minimize">_</button>',
          '<button id="cs-dev-close">&#x2715;</button>',
        '</div>',
      '</div>',
      '<div class="cs-dev-body">',
        '<div class="cs-dev-section">',
          '<div class="cs-dev-section-title">Appearance Elements</div>',
          '<div id="cs-dev-list"></div>',
        '</div>',
        '<div class="cs-dev-section" id="cs-dev-editor" style="display:none">',
          '<div class="cs-dev-section-title">Edit Attribute</div>',
          '<div id="cs-dev-editor-content"></div>',
        '</div>',
        '<div class="cs-dev-section" id="cs-dev-export" style="display:none">',
          '<div class="cs-dev-section-title">Generated Code</div>',
          '<textarea id="cs-dev-export-text" readonly rows="6"></textarea>',
          '<button id="cs-dev-copy" style="margin-top:4px">Copy to Clipboard</button>',
          '<button id="cs-dev-sync" style="margin-top:4px;margin-left:4px">Sync to File</button>',
          '<div id="cs-dev-sync-status" style="margin-top:4px;font-size:11px"></div>',
        '</div>',
      '</div>',
      '<div class="cs-dev-footer">',
        '<label><input type="checkbox" id="cs-dev-hover-hl" checked> Hover Highlight</label>',
        '<span id="cs-dev-count">0 elements</span>',
      '</div>'
    ].join('');

    document.body.appendChild(panel);
    bindEvents();
    collect();
  }

  function destroy() {
    if (highlightEl) {
      highlightEl.style.outline = '';
      highlightEl = null;
    }
    if (panel) {
      panel.remove();
      panel = null;
    }
  }

  function bindEvents() {
    panel.querySelector('#cs-dev-refresh').onclick = collect;
    panel.querySelector('#cs-dev-minimize').onclick = function () {
      var body = panel.querySelector('.cs-dev-body');
      body.style.display = body.style.display === 'none' ? '' : 'none';
    };
    panel.querySelector('#cs-dev-close').onclick = destroy;
    panel.querySelector('#cs-dev-copy').onclick = copyExport;
    panel.querySelector('#cs-dev-sync').onclick = syncToFile;

    panel.querySelector('#cs-dev-hover-hl').onchange = function () {
      if (!this.checked && highlightEl) {
        highlightEl.style.outline = '';
        highlightEl = null;
      }
    };

    panel.querySelector('#cs-dev-list').addEventListener('mouseover', function (e) {
      var item = e.target.closest('.cs-dev-item');
      if (!item) return;
      var idx = parseInt(item.getAttribute('data-idx'), 10);
      var el = elements[idx];
      if (!el || !panel.querySelector('#cs-dev-hover-hl').checked) return;
      if (highlightEl) highlightEl.style.outline = '';
      el.style.outline = '2px solid #38bdf8';
      el.style.outlineOffset = '2px';
      highlightEl = el;
    });

    panel.querySelector('#cs-dev-list').addEventListener('mouseout', function (e) {
      var item = e.target.closest('.cs-dev-item');
      if (!item) return;
      if (highlightEl) {
        highlightEl.style.outline = '';
        highlightEl = null;
      }
    });

    panel.querySelector('#cs-dev-list').addEventListener('click', function (e) {
      var item = e.target.closest('.cs-dev-item');
      if (!item) return;
      var idx = parseInt(item.getAttribute('data-idx'), 10);
      editElement(idx);
    });
  }

  function collect() {
    elements = Array.from(document.querySelectorAll(
      '[data-cs-tone], [data-cs-intent], [data-cs-class], [data-cs-bind]'
    ));

    panel.querySelector('#cs-dev-count').textContent = elements.length + ' elements';
    renderList();
  }

  function renderList() {
    var list = panel.querySelector('#cs-dev-list');
    if (elements.length === 0) {
      list.innerHTML = '<div class="cs-dev-empty">No appearance elements found on this page.</div>';
      return;
    }

    var html = '';
    elements.forEach(function (el, idx) {
      var tag = el.tagName.toLowerCase();
      var id = el.id ? '#' + el.id : '';
      var cls = el.classList.length ? '.' + Array.from(el.classList).slice(0, 3).join('.') : '';
      var label = tag + id + cls;
      if (label.length > 45) label = label.substring(0, 42) + '...';

      var tone = el.getAttribute('data-cs-tone') || '';
      var intent = el.getAttribute('data-cs-intent') || '';
      var csClass = el.getAttribute('data-cs-class') || '';

      html += '<div class="cs-dev-item" data-idx="' + idx + '">';
      html += '<div class="cs-dev-item-label">' + esc(label) + '</div>';
      if (tone) html += '<span class="cs-dev-tag tone">tone: ' + esc(tone) + '</span>';
      if (intent) html += '<span class="cs-dev-tag intent">intent: ' + esc(intent) + '</span>';
      if (csClass) html += '<span class="cs-dev-tag cls">class: ' + esc(csClass) + '</span>';
      html += '</div>';
    });

    list.innerHTML = html;
  }

  function editElement(idx) {
    var el = elements[idx];
    if (!el) return;
    activeIdx = idx;

    var editorDiv = panel.querySelector('#cs-dev-editor');
    var content = panel.querySelector('#cs-dev-editor-content');
    editorDiv.style.display = '';

    var tone = el.getAttribute('data-cs-tone') || '';
    var intent = el.getAttribute('data-cs-intent') || '';
    var csClass = el.getAttribute('data-cs-class') || '';
    var tag = el.tagName.toLowerCase();
    var cls = el.classList.length ? Array.from(el.classList).join(' ') : '(none)';
    var sourceTag = getOpeningTag(el);
    if (sourceTag && !el.getAttribute('data-cs-source-tag')) {
      el.setAttribute('data-cs-source-tag', sourceTag);
    }

    content.innerHTML = [
      '<div class="cs-dev-field">',
        '<label>Tag: <strong>' + esc(tag) + '</strong></label>',
      '</div>',
      '<div class="cs-dev-field">',
        '<label>CSS Classes: <code>' + esc(cls) + '</code></label>',
      '</div>',
      '<div class="cs-dev-field">',
        '<label>Source Match: <code>' + esc(sourceTag || tag) + '</code></label>',
      '</div>',
      '<div class="cs-dev-field">',
        '<label>data-cs-tone</label>',
        '<input id="cs-dev-edit-tone" value="' + esc(tone) + '" placeholder="e.g. Tone or Status:status">',
      '</div>',
      '<div class="cs-dev-field">',
        '<label>data-cs-intent</label>',
        '<input id="cs-dev-edit-intent" value="' + esc(intent) + '" placeholder="e.g. numeric, status, due, trend">',
      '</div>',
      '<div class="cs-dev-field">',
        '<label>data-cs-class</label>',
        '<input id="cs-dev-edit-class" value="' + esc(csClass) + '" placeholder="e.g. PropName:highlight">',
      '</div>',
      '<button id="cs-dev-apply" style="margin-top:8px">Apply Preview</button>',
      '<button id="cs-dev-export-btn" style="margin-top:8px;margin-left:4px">Refresh Code</button>',
      '<div id="cs-dev-edit-status" style="margin-top:4px;font-size:11px;color:#4ade80"></div>',
    ].join('');

    document.getElementById('cs-dev-apply').onclick = function () {
      applyEdit(idx);
    };

    document.getElementById('cs-dev-export-btn').onclick = function () {
      exportElement(idx);
    };
  }

  function applyEdit(idx) {
    var el = elements[idx];
    if (!el) return;
    if (!el.getAttribute('data-cs-source-tag')) {
      el.setAttribute('data-cs-source-tag', getOpeningTag(el));
    }

    var newTone = document.getElementById('cs-dev-edit-tone').value.trim();
    var newIntent = document.getElementById('cs-dev-edit-intent').value.trim();
    var newClass = document.getElementById('cs-dev-edit-class').value.trim();

    if (newTone) {
      el.setAttribute('data-cs-tone', newTone);
      if (window.CodeSpirit && window.CodeSpirit.intent && window.CodeSpirit.intent.analyze) {
        window.CodeSpirit.intent.analyze(el);
      }
    } else {
      el.removeAttribute('data-cs-tone');
    }

    if (newIntent) {
      el.setAttribute('data-cs-intent', newIntent);
      if (window.CodeSpirit && window.CodeSpirit.intent && window.CodeSpirit.intent.analyze) {
        window.CodeSpirit.intent.analyze(el.closest('[data-cs-vm]') || document);
      }
    } else {
      el.removeAttribute('data-cs-intent');
    }

    if (newClass) {
      el.setAttribute('data-cs-class', newClass);
    } else {
      el.removeAttribute('data-cs-class');
    }

    var status = document.getElementById('cs-dev-edit-status');
    exportElement(idx);
    status.textContent = 'Applied. Sync to File writes this preview back to source.';
    setTimeout(function () { status.textContent = ''; }, 3000);
  }

  function exportElement(idx) {
    var el = elements[idx];
    if (!el) return;

    var tone = el.getAttribute('data-cs-tone') || '';
    var intent = el.getAttribute('data-cs-intent') || '';
    var csClass = el.getAttribute('data-cs-class') || '';

    var attrs = [];
    if (tone) attrs.push('data-cs-tone="' + attrEsc(tone) + '"');
    if (intent) attrs.push('data-cs-intent="' + attrEsc(intent) + '"');
    if (csClass) attrs.push('data-cs-class="' + attrEsc(csClass) + '"');

    var existingClasses = Array.from(el.classList).filter(function (c) {
      return c.indexOf('intent-') !== 0;
    }).join(' ');
    var tag = el.tagName.toLowerCase();
    var innerHtml = el.innerHTML.replace(/"/g, '&quot;');

    var snippet = '<' + tag + (existingClasses ? ' class="' + existingClasses + '"' : '') + (attrs.length ? ' ' + attrs.join(' ') : '') + '>';
    snippet += innerHtml;
    snippet += '</' + tag + '>';

    var exportDiv = panel.querySelector('#cs-dev-export');
    var textarea = panel.querySelector('#cs-dev-export-text');
    exportDiv.style.display = '';
    textarea.value = snippet;
  }

  function copyExport() {
    var textarea = panel.querySelector('#cs-dev-export-text');
    textarea.select();
    document.execCommand('copy');
    var status = panel.querySelector('#cs-dev-sync-status');
    status.textContent = 'Copied! Paste into your .aspx file.';
    status.style.color = '#4ade80';
    setTimeout(function () { status.textContent = ''; }, 3000);
  }

  function syncToFile() {
    if (activeIdx >= 0) {
      exportElement(activeIdx);
    }

    var textarea = panel.querySelector('#cs-dev-export-text');
    var snippet = textarea.value;
    if (!snippet) return;

    var status = panel.querySelector('#cs-dev-sync-status');
    status.textContent = 'Syncing...';
    status.style.color = '#fbbf24';

    var pagePath = window.location.pathname || '/';
    var pageName = pagePath === '/' ? 'Home' : pagePath.replace(/^\//, '').replace(/\/$/, '');
    if (!pageName) pageName = 'Home';

    fetch('/dev/api/sync-config', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        page: pageName,
        snippet: snippet,
        elementTag: getTagFromSnippet(snippet),
        sourceTag: activeIdx >= 0 && elements[activeIdx] ? elements[activeIdx].getAttribute('data-cs-source-tag') : ''
      })
    })
    .then(function (r) {
      return r.json().catch(function () {
        return { ok: false, error: 'Unexpected response.' };
      });
    })
    .then(function (data) {
      if (data.ok) {
        status.textContent = 'Synced! Restart server to apply.';
        status.style.color = '#4ade80';
      } else {
        status.textContent = 'Error: ' + (data.error || 'unknown');
        status.style.color = '#f87171';
      }
    })
    .catch(function (err) {
      status.textContent = 'Sync failed: ' + err.message;
      status.style.color = '#f87171';
    });
  }

  function getTagFromSnippet(snippet) {
    var match = snippet.match(/^<(\w+)/);
    return match ? match[1] : 'div';
  }

  function getOpeningTag(el) {
    var outer = el.outerHTML || '';
    var match = outer.match(/^<[^>]+>/);
    if (!match) return '';
    return match[0]
      .replace(/\sdata-cs-source-tag="[^"]*"/i, '')
      .replace(/\sdata-cs-submitting(="[^"]*")?/i, '');
  }

  function esc(s) {
    return String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
  }

  function attrEsc(s) {
    return esc(s).replace(/'/g, '&#39;');
  }

  window.__CS_DEV__ = {
    init: init,
    destroy: destroy
  };
})();
