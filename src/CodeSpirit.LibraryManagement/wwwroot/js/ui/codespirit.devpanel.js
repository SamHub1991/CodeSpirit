(function () {
  'use strict';

  if (typeof document === 'undefined') return;

  var PANEL_WIDTH = 360;
  var panel = null;
  var elements = [];
  var highlightEl = null;
  var activeIdx = -1;
  var filterText = '';
  var originalAttrs = new WeakMap();
  var originalSnapshots = new WeakMap();
  var sourceTags = new WeakMap();
  var livePreviewTimer = null;
  var editableSelector = '[data-cs-tone], [data-cs-intent], [data-cs-class], [data-cs-bind], [data-cs-show], [data-cs-enable], [data-cs-refresh], [data-cs-confirm], [data-cs-source], [data-cs-attr], [data-cs-visible], [data-cs-hidden], [data-cs-enabled], [data-cs-disabled], [data-ui]';

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
          '<input id="cs-dev-filter" placeholder="Filter tag, class, binding, ui..." style="width:100%;margin:4px 0 8px">',
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
        '<label><input type="checkbox" id="cs-dev-inspect"> Pick Page Element</label>',
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
      document.removeEventListener('click', handlePagePick, true);
      document.removeEventListener('keydown', handleShortcuts);
      panel.remove();
      panel = null;
    }
  }

  function bindEvents() {
    panel.querySelector('#cs-dev-refresh').onclick = collect;
    panel.querySelector('#cs-dev-minimize').onclick = function () {
      var body = panel.querySelector('.cs-dev-body');
      body.style.display = body.style.display === 'none' ? '' : 'none';
      writeSetting('minimized', body.style.display === 'none' ? '1' : '0');
    };
    panel.querySelector('#cs-dev-close').onclick = destroy;
    panel.querySelector('#cs-dev-copy').onclick = copyExport;
    panel.querySelector('#cs-dev-sync').onclick = syncToFile;

    panel.querySelector('#cs-dev-filter').oninput = function () {
      filterText = this.value.trim().toLowerCase();
      renderList();
    };

    panel.querySelector('#cs-dev-hover-hl').onchange = function () {
      writeSetting('hover', this.checked ? '1' : '0');
      if (!this.checked && highlightEl) {
        highlightEl.style.outline = '';
        highlightEl = null;
      }
    };

    panel.querySelector('#cs-dev-inspect').onchange = function () {
      writeSetting('inspect', this.checked ? '1' : '0');
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
        if (activeIdx >= 0 && highlightEl === elements[activeIdx]) return;
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

    document.addEventListener('click', handlePagePick, true);
    document.addEventListener('keydown', handleShortcuts);
    restoreSettings();
  }

  function collect() {
    elements = Array.from(document.querySelectorAll(editableSelector)).filter(function (el) { return !panel.contains(el); });

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
      var haystack = (label + ' ' + Array.from(el.attributes || []).map(function (attr) { return attr.name + '=' + attr.value; }).join(' ')).toLowerCase();
      if (filterText && haystack.indexOf(filterText) === -1) return;

      var tone = el.getAttribute('data-cs-tone') || '';
      var intent = el.getAttribute('data-cs-intent') || '';
      var csClass = el.getAttribute('data-cs-class') || '';
      var ui = el.getAttribute('data-ui') || '';

      html += '<div class="cs-dev-item' + (idx === activeIdx ? ' active' : '') + '" data-idx="' + idx + '">';
      html += '<div class="cs-dev-item-label">' + esc(label) + '</div>';
      if (tone) html += '<span class="cs-dev-tag tone">tone: ' + esc(tone) + '</span>';
      if (intent) html += '<span class="cs-dev-tag intent">intent: ' + esc(intent) + '</span>';
      if (csClass) html += '<span class="cs-dev-tag cls">class: ' + esc(csClass) + '</span>';
      if (ui) html += '<span class="cs-dev-tag ui">ui: ' + esc(ui) + '</span>';
      html += '</div>';
    });

    list.innerHTML = html || '<div class="cs-dev-empty">No matching elements.</div>';
  }

  function editElement(idx) {
    var el = elements[idx];
    if (!el) return;
    activeIdx = idx;
    rememberOriginal(el);
    highlightElement(el, true);
    renderList();

    var editorDiv = panel.querySelector('#cs-dev-editor');
    var content = panel.querySelector('#cs-dev-editor-content');
    editorDiv.style.display = '';

    var tone = el.getAttribute('data-cs-tone') || '';
    var intent = el.getAttribute('data-cs-intent') || '';
    var csClass = el.getAttribute('data-cs-class') || '';
    var classAttr = el.getAttribute('class') || '';
    var styleAttr = el.getAttribute('style') || '';
    var text = el.children.length ? '' : el.textContent || '';
    var html = el.children.length ? el.innerHTML || '' : '';
    var show = el.getAttribute('data-cs-show') || '';
    var enable = el.getAttribute('data-cs-enable') || '';
    var refresh = el.getAttribute('data-cs-refresh') || '';
    var confirm = el.getAttribute('data-cs-confirm') || '';
    var source = el.getAttribute('data-cs-source') || '';
    var attr = el.getAttribute('data-cs-attr') || '';
    var visible = el.getAttribute('data-cs-visible') || '';
    var hidden = el.getAttribute('data-cs-hidden') || '';
    var enabled = el.getAttribute('data-cs-enabled') || '';
    var disabled = el.getAttribute('data-cs-disabled') || '';
    var ui = el.getAttribute('data-ui') || '';
    var tag = el.tagName.toLowerCase();
    var cls = el.classList.length ? Array.from(el.classList).join(' ') : '(none)';
    var sourceTag = getOpeningTag(el);
    if (sourceTag && !sourceTags.has(el)) {
      sourceTags.set(el, sourceTag);
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
      '<div class="cs-dev-field"><label>class</label><input id="cs-dev-edit-class-attr" value="' + esc(classAttr) + '" placeholder="CSS classes"></div>',
      '<div class="cs-dev-field"><label>style</label><input id="cs-dev-edit-style" value="' + esc(styleAttr) + '" placeholder="color:#0f172a; padding:12px"></div>',
      '<div class="cs-dev-field"><label>text</label><input id="cs-dev-edit-text" value="' + esc(text) + '" placeholder="Plain text for leaf elements"></div>',
      '<div class="cs-dev-field"><label>html</label><textarea id="cs-dev-edit-html" rows="3" placeholder="Inner HTML for container elements">' + esc(html) + '</textarea></div>',
      '<div class="cs-dev-field"><label>data-cs-show</label><input id="cs-dev-edit-show" value="' + esc(show) + '" placeholder="e.g. Count > 0"></div>',
      '<div class="cs-dev-field"><label>data-cs-enable</label><input id="cs-dev-edit-enable" value="' + esc(enable) + '" placeholder="e.g. CanEdit"></div>',
      '<div class="cs-dev-field"><label>data-cs-refresh</label><input id="cs-dev-edit-refresh" value="' + esc(refresh) + '" placeholder="Region name"></div>',
      '<div class="cs-dev-field"><label>data-cs-confirm</label><input id="cs-dev-edit-confirm" value="' + esc(confirm) + '" placeholder="Are you sure?"></div>',
      '<div class="cs-dev-field"><label>data-cs-source</label><input id="cs-dev-edit-source" value="' + esc(source) + '" placeholder="LoadOptions command"></div>',
      '<div class="cs-dev-field"><label>data-cs-attr</label><input id="cs-dev-edit-attr" value="' + esc(attr) + '" placeholder="Field:aria-label"></div>',
      '<div class="cs-dev-field"><label>data-cs-visible</label><input id="cs-dev-edit-visible" value="' + esc(visible) + '" placeholder="Field or Field:value"></div>',
      '<div class="cs-dev-field"><label>data-cs-hidden</label><input id="cs-dev-edit-hidden" value="' + esc(hidden) + '" placeholder="Field or Field:value"></div>',
      '<div class="cs-dev-field"><label>data-cs-enabled</label><input id="cs-dev-edit-enabled" value="' + esc(enabled) + '" placeholder="CanEdit"></div>',
      '<div class="cs-dev-field"><label>data-cs-disabled</label><input id="cs-dev-edit-disabled" value="' + esc(disabled) + '" placeholder="IsLocked"></div>',
      '<div class="cs-dev-field"><label>data-ui</label><input id="cs-dev-edit-ui" value="' + esc(ui) + '" placeholder="wizard tree tabs modal"></div>',
      '<button id="cs-dev-apply" style="margin-top:8px">Apply Preview</button>',
      '<button id="cs-dev-undo" style="margin-top:8px;margin-left:4px">Undo</button>',
      '<button id="cs-dev-locate" style="margin-top:8px;margin-left:4px">Locate</button>',
      '<button id="cs-dev-export-btn" style="margin-top:8px;margin-left:4px">Refresh Code</button>',
      '<pre id="cs-dev-diff" style="margin-top:8px;max-height:90px;overflow:auto;font-size:11px"></pre>',
      '<div id="cs-dev-edit-status" style="margin-top:4px;font-size:11px;color:#4ade80"></div>',
    ].join('');

    Array.from(content.querySelectorAll('input, textarea')).forEach(function (input) {
      input.addEventListener('input', function () { scheduleLivePreview(idx); });
    });

    document.getElementById('cs-dev-apply').onclick = function () {
      applyEdit(idx);
    };

    document.getElementById('cs-dev-undo').onclick = function () {
      undoEdit(idx);
    };

    document.getElementById('cs-dev-locate').onclick = function () {
      locateElement(idx);
    };

    document.getElementById('cs-dev-export-btn').onclick = function () {
      exportElement(idx);
    };
  }

  function applyEdit(idx, live) {
    var el = elements[idx];
    if (!el) return;
    rememberOriginal(el);
    if (!sourceTags.has(el)) {
      sourceTags.set(el, getOpeningTag(el));
    }

    var newTone = document.getElementById('cs-dev-edit-tone').value.trim();
    var newIntent = document.getElementById('cs-dev-edit-intent').value.trim();
    var newClass = document.getElementById('cs-dev-edit-class').value.trim();
    var newClassAttr = document.getElementById('cs-dev-edit-class-attr').value.trim();
    var newStyle = document.getElementById('cs-dev-edit-style').value.trim();
    var newText = document.getElementById('cs-dev-edit-text').value;
    var newHtml = document.getElementById('cs-dev-edit-html').value;
    var newShow = document.getElementById('cs-dev-edit-show').value.trim();
    var newEnable = document.getElementById('cs-dev-edit-enable').value.trim();
    var newRefresh = document.getElementById('cs-dev-edit-refresh').value.trim();
    var newConfirm = document.getElementById('cs-dev-edit-confirm').value.trim();
    var newSource = document.getElementById('cs-dev-edit-source').value.trim();
    var newAttr = document.getElementById('cs-dev-edit-attr').value.trim();
    var newVisible = document.getElementById('cs-dev-edit-visible').value.trim();
    var newHidden = document.getElementById('cs-dev-edit-hidden').value.trim();
    var newEnabled = document.getElementById('cs-dev-edit-enabled').value.trim();
    var newDisabled = document.getElementById('cs-dev-edit-disabled').value.trim();
    var newUi = document.getElementById('cs-dev-edit-ui').value.trim();

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

    setOptionalAttr(el, 'data-cs-show', newShow);
    setOptionalAttr(el, 'data-cs-enable', newEnable);
    setOptionalAttr(el, 'data-cs-refresh', newRefresh);
    setOptionalAttr(el, 'data-cs-confirm', newConfirm);
    setOptionalAttr(el, 'data-cs-source', newSource);
    setOptionalAttr(el, 'data-cs-attr', newAttr);
    setOptionalAttr(el, 'data-cs-visible', newVisible);
    setOptionalAttr(el, 'data-cs-hidden', newHidden);
    setOptionalAttr(el, 'data-cs-enabled', newEnabled);
    setOptionalAttr(el, 'data-cs-disabled', newDisabled);
    setOptionalAttr(el, 'data-ui', newUi);
    setOptionalAttr(el, 'class', newClassAttr);
    setOptionalAttr(el, 'style', newStyle);
    if (newHtml.trim()) {
      el.innerHTML = sanitizeHtml(newHtml);
    } else if (newText || el.children.length === 0) {
      el.textContent = newText;
    }

    applyPreviewBehaviors(el);

    var status = document.getElementById('cs-dev-edit-status');
    exportElement(idx);
    updateDiff(idx);
    renderList();
    status.textContent = live ? 'Live preview applied.' : 'Applied. Sync to File writes this preview back to source.';
    if (!live) setTimeout(function () { status.textContent = ''; }, 3000);
  }

  function exportElement(idx) {
    var el = elements[idx];
    if (!el) return;

    var tone = el.getAttribute('data-cs-tone') || '';
    var intent = el.getAttribute('data-cs-intent') || '';
    var csClass = el.getAttribute('data-cs-class') || '';
    var show = el.getAttribute('data-cs-show') || '';
    var enable = el.getAttribute('data-cs-enable') || '';
    var refresh = el.getAttribute('data-cs-refresh') || '';
    var confirm = el.getAttribute('data-cs-confirm') || '';
    var source = el.getAttribute('data-cs-source') || '';
    var attr = el.getAttribute('data-cs-attr') || '';
    var visible = el.getAttribute('data-cs-visible') || '';
    var hidden = el.getAttribute('data-cs-hidden') || '';
    var enabled = el.getAttribute('data-cs-enabled') || '';
    var disabled = el.getAttribute('data-cs-disabled') || '';
    var ui = el.getAttribute('data-ui') || '';
    var style = el.getAttribute('style') || '';

    var attrs = [];
    if (tone) attrs.push('data-cs-tone="' + attrEsc(tone) + '"');
    if (intent) attrs.push('data-cs-intent="' + attrEsc(intent) + '"');
    if (csClass) attrs.push('data-cs-class="' + attrEsc(csClass) + '"');
    if (show) attrs.push('data-cs-show="' + attrEsc(show) + '"');
    if (enable) attrs.push('data-cs-enable="' + attrEsc(enable) + '"');
    if (refresh) attrs.push('data-cs-refresh="' + attrEsc(refresh) + '"');
    if (confirm) attrs.push('data-cs-confirm="' + attrEsc(confirm) + '"');
    if (source) attrs.push('data-cs-source="' + attrEsc(source) + '"');
    if (attr) attrs.push('data-cs-attr="' + attrEsc(attr) + '"');
    if (visible) attrs.push('data-cs-visible="' + attrEsc(visible) + '"');
    if (hidden) attrs.push('data-cs-hidden="' + attrEsc(hidden) + '"');
    if (enabled) attrs.push('data-cs-enabled="' + attrEsc(enabled) + '"');
    if (disabled) attrs.push('data-cs-disabled="' + attrEsc(disabled) + '"');
    if (ui) attrs.push('data-ui="' + attrEsc(ui) + '"');
    if (style) attrs.push('style="' + attrEsc(style) + '"');

    var existingClasses = Array.from(el.classList).filter(function (c) {
      return c.indexOf('intent-') !== 0;
    }).join(' ');
    var tag = el.tagName.toLowerCase();
    var innerHtml = sanitizeHtml(el.innerHTML).replace(/"/g, '&quot;');

    var snippet = '<' + tag + (existingClasses ? ' class="' + attrEsc(existingClasses) + '"' : '') + (attrs.length ? ' ' + attrs.join(' ') : '') + '>';
    snippet += innerHtml;
    snippet += '</' + tag + '>';

    var exportDiv = panel.querySelector('#cs-dev-export');
    var textarea = panel.querySelector('#cs-dev-export-text');
    exportDiv.style.display = '';
    textarea.value = snippet;
  }

  function handlePagePick(e) {
    if (!panel || !panel.querySelector('#cs-dev-inspect').checked) return;
    if (panel.contains(e.target)) return;
    var idx = elements.indexOf(e.target.closest(editableSelector));
    if (idx < 0) return;
    e.preventDefault();
    e.stopImmediatePropagation();
    editElement(idx);
  }

  function scheduleLivePreview(idx) {
    clearTimeout(livePreviewTimer);
    livePreviewTimer = setTimeout(function () { applyEdit(idx, true); }, 120);
  }

  function handleShortcuts(e) {
    if (!panel) return;
    if (e.key === 'Escape') {
      panel.querySelector('#cs-dev-inspect').checked = false;
      writeSetting('inspect', '0');
    }
    if ((e.ctrlKey || e.metaKey) && e.shiftKey && String(e.key).toLowerCase() === 'c') {
      var inspect = panel.querySelector('#cs-dev-inspect');
      inspect.checked = !inspect.checked;
      writeSetting('inspect', inspect.checked ? '1' : '0');
    }
    if ((e.ctrlKey || e.metaKey) && String(e.key).toLowerCase() === 's' && activeIdx >= 0) {
      e.preventDefault();
      syncToFile();
    }
  }

  function setOptionalAttr(el, name, value) {
    if (value) el.setAttribute(name, value);
    else el.removeAttribute(name);
  }

  function rememberOriginal(el) {
    if (!originalAttrs.has(el)) originalAttrs.set(el, getOpeningTag(el));
    if (!originalSnapshots.has(el)) {
      originalSnapshots.set(el, {
        openingTag: getOpeningTag(el),
        text: el.textContent || '',
        html: el.innerHTML || ''
      });
    }
  }

  function undoEdit(idx) {
    var el = elements[idx];
    var original = el && originalAttrs.get(el);
    if (!el || !original) return;
    ['data-cs-tone', 'data-cs-intent', 'data-cs-class', 'data-cs-show', 'data-cs-enable', 'data-cs-refresh', 'data-cs-confirm', 'data-cs-source', 'data-cs-attr', 'data-cs-visible', 'data-cs-hidden', 'data-cs-enabled', 'data-cs-disabled', 'data-ui', 'class', 'style'].forEach(function (name) {
      var value = readAttrFromTag(original, name);
      if (value) el.setAttribute(name, value);
      else el.removeAttribute(name);
    });
    var snapshot = originalSnapshots.get(el);
    if (snapshot) {
      el.innerHTML = snapshot.html;
      if (!snapshot.html) el.textContent = snapshot.text;
    }
    applyPreviewBehaviors(el);
    editElement(idx);
  }

  function applyPreviewBehaviors(el) {
    if (!window.CodeSpirit) return;
    var root = el.closest('[data-cs-vm]') || document;
    if (window.CodeSpirit.expression && window.CodeSpirit.expression.apply) {
      window.CodeSpirit.expression.apply(root);
    }
    if (window.CodeSpirit.intent && window.CodeSpirit.intent.analyze) {
      window.CodeSpirit.intent.analyze(root);
    }
    if (window.CodeSpirit.ui && window.CodeSpirit.ui.init) {
      window.CodeSpirit.ui.init(root);
    }
  }

  function locateElement(idx) {
    var el = elements[idx];
    if (!el) return;
    if (el.scrollIntoView) el.scrollIntoView({ block: 'center', behavior: 'smooth' });
    highlightElement(el, true);
  }

  function highlightElement(el, keep) {
    if (highlightEl && highlightEl !== el) highlightEl.style.outline = '';
    el.style.outline = '2px solid #38bdf8';
    el.style.outlineOffset = '2px';
    highlightEl = el;
    if (!keep) setTimeout(function () { if (highlightEl === el) el.style.outline = ''; }, 1200);
  }

  function updateDiff(idx) {
    var diff = document.getElementById('cs-dev-diff');
    var el = elements[idx];
    if (!diff || !el) return;
    diff.textContent = 'Before: ' + (originalAttrs.get(el) || '(unknown)') + '\nAfter:  ' + getOpeningTag(el);
  }

  function readAttrFromTag(tag, name) {
    var match = tag.match(new RegExp('\\s' + name + '="([^"]*)"', 'i'));
    return match ? match[1] : '';
  }

  function restoreSettings() {
    panel.querySelector('#cs-dev-hover-hl').checked = readSetting('hover') !== '0';
    panel.querySelector('#cs-dev-inspect').checked = readSetting('inspect') === '1';
    if (readSetting('minimized') === '1') panel.querySelector('.cs-dev-body').style.display = 'none';
  }

  function readSetting(key) {
    try { return window.localStorage && window.localStorage.getItem('cs-dev-' + key); } catch (e) { return null; }
  }

  function writeSetting(key, value) {
    try { if (window.localStorage) window.localStorage.setItem('cs-dev-' + key, value); } catch (e) { }
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
        sourceTag: activeIdx >= 0 && elements[activeIdx] ? (sourceTags.get(elements[activeIdx]) || '') : ''
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

  function sanitizeHtml(html) {
    return String(html || '')
      .replace(/<\/?(script|style|iframe|object|embed)\b[^>]*>/gi, '')
      .replace(/\s+on[a-z]+\s*=\s*("[^"]*"|'[^']*'|[^\s>]+)/gi, '')
      .replace(/\s+(href|src|action|formaction)\s*=\s*(["'])\s*javascript:[\s\S]*?\2/gi, '')
      .replace(/\s+(href|src|action|formaction)\s*=\s*javascript:[^\s>]*/gi, '');
  }

  window.__CS_DEV__ = {
    init: init,
    destroy: destroy
  };
})();
