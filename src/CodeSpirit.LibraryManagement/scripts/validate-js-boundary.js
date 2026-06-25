const fs = require('fs');
const vm = require('vm');
const assert = require('assert');
const path = require('path');

const templateRoot = path.resolve(__dirname, '..');

class Event {
  constructor(type, options = {}) {
    this.type = type;
    this.bubbles = Boolean(options.bubbles);
    this.detail = options.detail;
    this.defaultPrevented = false;
    this.target = null;
    this.submitter = options.submitter || null;
  }

  preventDefault() {
    this.defaultPrevented = true;
  }

  stopImmediatePropagation() {}
}

class EventTarget {
  constructor() {
    this.listeners = {};
    this.parentNode = null;
  }

  addEventListener(type, handler) {
    this.listeners[type] = this.listeners[type] || [];
    this.listeners[type].push(handler);
  }

  removeEventListener(type, handler) {
    if (!this.listeners[type]) return;
    const idx = this.listeners[type].indexOf(handler);
    if (idx >= 0) this.listeners[type].splice(idx, 1);
  }

  dispatchEvent(event) {
    event.target = event.target || this;
    (this.listeners[event.type] || []).forEach((handler) => handler.call(this, event));
    if (event.bubbles && this.parentNode) {
      this.parentNode.dispatchEvent(event);
    }
    return !event.defaultPrevented;
  }
}

function matchesSelector(element, selector) {
  var remaining = selector.trim();

  if (remaining.charAt(0) === '.') {
    var dotIdx = remaining.search(/[\[\s:]|$/);
    var className = remaining.substring(1, dotIdx);
    return element.classList.contains(className);
  }

  if (remaining.charAt(0) === '#') {
    var hashIdx = remaining.search(/[\[\s:]|$/);
    var id = remaining.substring(1, hashIdx);
    return element.getAttribute('id') === id;
  }

  var tagName = null;
  var tagMatch = remaining.match(/^([A-Za-z][A-Za-z0-9_-]*)/);
  if (tagMatch) {
    tagName = tagMatch[1].toLowerCase();
    remaining = remaining.substring(tagMatch[0].length).trim();
  }

  if (tagName && element.tagName.toLowerCase() !== tagName) {
    return false;
  }

  while (remaining.length > 0) {
    var attrEquals = remaining.match(/^\[([^=~\]]+)="([^"]*)"\]/);
    if (attrEquals) {
      if (element.getAttribute(attrEquals[1]) !== attrEquals[2]) return false;
      remaining = remaining.substring(attrEquals[0].length).trim();
      continue;
    }

    var attrToken = remaining.match(/^\[([^=~\]]+)~="([^"]*)"\]/);
    if (attrToken) {
      if (!(element.getAttribute(attrToken[1]) || '').split(/\s+/).includes(attrToken[2])) return false;
      remaining = remaining.substring(attrToken[0].length).trim();
      continue;
    }

    var attrExists = remaining.match(/^\[([^\]]+)\]/);
    if (attrExists) {
      if (element.getAttribute(attrExists[1]) == null) return false;
      remaining = remaining.substring(attrExists[0].length).trim();
      continue;
    }

    break;
  }

  if (remaining.length === 0) {
    return true;
  }

  return element.tagName.toLowerCase() === selector.toLowerCase();
}

class Element extends EventTarget {
  constructor(tagName, attributes = {}) {
    super();
    this.tagName = tagName.toUpperCase();
    this.attributes = { ...attributes };
    this.children = [];
    this.style = {};
    this.textContent = '';
    this.name = attributes.name || '';
    this.classList = new ClassListMock();
    if (['INPUT', 'BUTTON', 'SELECT', 'TEXTAREA'].includes(this.tagName)) {
      this.value = attributes.value || '';
    }
    this.disabled = Object.prototype.hasOwnProperty.call(attributes, 'disabled');
    this.closest_cache = null;
  }

  appendChild(child) {
    child.parentNode = this;
    this.children.push(child);
    return child;
  }

  createElement(tagName) {
    return new Element(tagName);
  }

  get firstElementChild() {
    return this.children[0] || null;
  }

  set innerHTML(value) {
    this.children = [];
    const elementMatch = String(value).match(/^<([A-Za-z][A-Za-z0-9-]*)([^>]*)>([\s\S]*)<\/\1>$/);
    if (!elementMatch) {
      return;
    }

    const [, tagName, rawAttributes, content] = elementMatch;
    const attributes = {};
    rawAttributes.replace(/([A-Za-z][A-Za-z0-9_-]*)="([^"]*)"/g, (_, name, attrValue) => {
      attributes[name] = attrValue;
      return '';
    });

    const element = this.appendChild(new Element(tagName, attributes));
    element.textContent = content.replace(/<[^>]+>/g, '');
    if (/^\s*<[A-Za-z][A-Za-z0-9-]*[\s>]/.test(content)) {
      element.innerHTML = content.trim();
    }
  }

  getAttribute(name) {
    return Object.prototype.hasOwnProperty.call(this.attributes, name) ? this.attributes[name] : null;
  }

  setAttribute(name, value) {
    this.attributes[name] = String(value);
    if (name === 'name') {
      this.name = String(value);
    }
    if (name === 'disabled') {
      this.disabled = true;
    }
  }

  removeAttribute(name) {
    delete this.attributes[name];
    if (name === 'name') {
      this.name = '';
    }
    if (name === 'disabled') {
      this.disabled = false;
    }
  }

  requestSubmit() {
    var event = new Event('submit', { bubbles: true });
    event.submitter = null;
    this.dispatchEvent(event);
  }

  matches(selector) {
    return matchesSelector(this, selector);
  }

  closest(selector) {
    let current = this;
    while (current) {
      if (current.matches && current.matches(selector)) {
        return current;
      }
      current = current.parentNode;
    }
    return null;
  }

  querySelectorAll(selector) {
    const selectors = selector.split(',').map((item) => item.trim());
    const results = [];

    function visit(node) {
      node.children.forEach((child) => {
        if (selectors.some((item) => child.matches(item))) {
          results.push(child);
        }
        visit(child);
      });
    }

    visit(this);
    return results;
  }

  querySelector(selector) {
    return this.querySelectorAll(selector)[0] || null;
  }

  replaceWith(next) {
    if (!this.parentNode) {
      return;
    }

    const index = this.parentNode.children.indexOf(this);
    if (index >= 0) {
      next.parentNode = this.parentNode;
      this.parentNode.children[index] = next;
      this.parentNode = null;
    }
  }
}

class ClassListMock {
  constructor() {
    this._classes = [];
  }

  add(className) {
    if (!this._classes.includes(className)) {
      this._classes.push(className);
    }
  }

  remove(className) {
    const idx = this._classes.indexOf(className);
    if (idx >= 0) this._classes.splice(idx, 1);
  }

  contains(className) {
    return this._classes.includes(className);
  }

  toggle(className) {
    if (this.contains(className)) {
      this.remove(className);
    } else {
      this.add(className);
    }
  }

  toString() {
    return this._classes.join(' ');
  }
}

class Document extends Element {
  constructor() {
    super('#document');
  }

  matches() {
    return false;
  }

  createElement(tagName) {
    return new Element(tagName);
  }
}

class FormDataStub {
  constructor(form) {
    this.fields = [];
    form.querySelectorAll('[name]').forEach((element) => {
      this.fields.push([element.name, element.value]);
    });
  }

  forEach(callback) {
    this.fields.forEach(([key, value]) => callback(value, key));
  }
}

function runScript(file, context) {
  vm.runInContext(fs.readFileSync(path.join(templateRoot, file), 'utf8'), context, { filename: file });
}

async function main() {
  const document = new Document();
  const form = document.appendChild(new Element('form', { 'data-cs-vm': '', method: 'post', 'data-cs-vm-id': 'test-vm', name: 'TestVm' }));
  const city = form.appendChild(new Element('input', { name: 'City', 'data-cs-bind': 'City', value: 'Paris' }));
  const cityLabel = form.appendChild(new Element('span', { 'data-cs-bind': 'City' }));
  const token = form.appendChild(new Element('input', { name: 'Token', value: '' }));
  const refreshBtn = form.appendChild(new Element('button', { type: 'submit', 'data-cs-command': 'Refresh' }));
  const preDisabledBtn = form.appendChild(new Element('button', { type: 'submit', disabled: true, 'data-cs-command': 'SaveDraft' }));

  let fetchPayload = null;
  const context = vm.createContext({
    window: { location: { pathname: '/weather' }, CSS: { escape: (value) => String(value) } },
    document,
    FormData: FormDataStub,
    Event,
    CustomEvent: Event,
    fetch: async (url, options) => {
      fetchPayload = { url, options: { ...options, body: JSON.parse(options.body) } };
      return { ok: true, json: async () => ({ state: { City: 'Rome' } }) };
    }
  });
  context.window.document = document;

  runScript('wwwroot/js/codespirit.runtime.js', context);

  // ---- Exposed API ----
  assert.strictEqual(typeof context.window.CodeSpirit.vm, 'function');
  assert.strictEqual(typeof context.window.CodeSpirit.VmChain, 'function');
  assert.strictEqual(typeof context.window.CodeSpirit.applyErrors, 'function');
  assert.strictEqual(typeof context.window.CodeSpirit.clearErrors, 'function');
  assert.strictEqual(context.window.$cs, context.window.CodeSpirit);
  assert.strictEqual(context.window.CS, undefined);

  // ---- Occupied alias ----
  const occupiedAlias = { existing: true };
  const occupiedDocument = new Document();
  const occupiedContext = vm.createContext({
    window: { $cs: occupiedAlias, location: { pathname: '/weather' } },
    document: occupiedDocument,
    FormData: FormDataStub,
    Event,
    CustomEvent: Event,
    fetch: context.fetch
  });
  occupiedContext.window.document = occupiedDocument;
  runScript('wwwroot/js/codespirit.runtime.js', occupiedContext);
  assert.strictEqual(occupiedContext.window.$cs, occupiedAlias);
  assert.strictEqual(typeof occupiedContext.window.CodeSpirit.vm, 'function');

  // ---- Chain initialization ----
  var chain = context.window.CodeSpirit.vm(form);
  assert.ok(chain instanceof context.window.CodeSpirit.VmChain);
  assert.strictEqual(chain._form, form);
  assert.strictEqual(chain._destroyed, false);

  // ---- .set() / .get() / .state() ----
  chain.set('City', 'Oslo');
  assert.strictEqual(city.value, 'Oslo');
  assert.strictEqual(cityLabel.textContent, 'Oslo');
  assert.strictEqual(chain.get('City'), 'Oslo');

  chain.set({ City: 'Stockholm', Token: 'x' });
  assert.strictEqual(city.value, 'Stockholm');
  assert.strictEqual(chain.get('City'), 'Stockholm');

  const specialName = form.appendChild(new Element('input', { name: 'Field]Name', 'data-cs-bind': 'Field]Name', value: '' }));
  chain.set('Field]Name', 'escaped');
  assert.strictEqual(specialName.value, 'escaped');

  var snap = chain.state();
  assert.strictEqual(snap.City, 'Stockholm');
  assert.strictEqual(snap.Token, 'x');

  // ---- .val() getter form ----
  assert.strictEqual(chain.val('City'), 'Stockholm');

  // ---- chaining: set returns this ----
  assert.strictEqual(chain.set('City', 'Copenhagen'), chain);

  // ---- .on() / .off() / .once() return this ----
  assert.strictEqual(chain.on('codespirit:updated', function () {}), chain);
  assert.strictEqual(chain.once('codespirit:loaded', function () {}), chain);

  // ---- .el() / .all() scoped queries ----
  assert.strictEqual(chain.el('[name="City"]'), city);
  assert.ok(chain.all('[name="City"]').length >= 1);

  // ---- .invoke() returns a Promise ----
  var invoked = chain.invoke('Refresh');
  assert.ok(invoked && typeof invoked.then === 'function');

  // ---- Reset city for follow-up tests ----
  city.value = 'Paris';
  cityLabel.textContent = 'Paris';

  let changed = null;
  form.addEventListener('codespirit:changed', (event) => {
    changed = event.detail;
  });

  city.value = 'Berlin';
  city.dispatchEvent(new Event('input', { bubbles: true }));
  assert.strictEqual(cityLabel.textContent, 'Berlin');
  assert.strictEqual(changed.name, 'City');
  assert.strictEqual(changed.value, 'Berlin');

  context.window.CodeSpirit.input(city, 'City', 'Tokyo');
  assert.strictEqual(city.value, 'Tokyo');
  assert.strictEqual(cityLabel.textContent, 'Tokyo');

  // ---- Submit with loading state ----
  const updated = new Promise((resolve) => form.addEventListener('codespirit:updated', resolve));
  const region = document.appendChild(new Element('section', { 'data-cs-region': 'forecast' }));
  region.textContent = 'Old Forecast';
  const regionWidget = region.appendChild(new Element('div', { 'data-ui': 'custom-widget' }));
  regionWidget.textContent = 'Old Widget';
  const submit = new Event('submit', { bubbles: true });
  submit.submitter = refreshBtn;

  context.fetch = async (url, options) => {
    fetchPayload = { url, options: { ...options, body: JSON.parse(options.body) } };
    return {
      ok: true,
      json: async () => ({
        state: { City: 'Rome' },
        regions: { forecast: '<section data-cs-region="forecast"><div data-ui="custom-widget">Fresh Forecast</div></section>' }
      })
    };
  };
  form.dispatchEvent(submit);
  assert.ok(form.classList.contains('cs-loading'));
  assert.ok(form.getAttribute('data-cs-busy') !== null);
  assert.strictEqual(refreshBtn.disabled, true);
  assert.strictEqual(preDisabledBtn.disabled, true);
  await updated;
  assert.strictEqual(submit.defaultPrevented, true);
  assert.strictEqual(fetchPayload.url, '/weather');
  assert.strictEqual(fetchPayload.options.body.City, 'Tokyo');
  assert.strictEqual(fetchPayload.options.body.__command, 'Refresh');
  assert.strictEqual(city.value, 'Rome');
  assert.strictEqual(cityLabel.textContent, 'Rome');
  assert.strictEqual(document.querySelector('[data-cs-region="forecast"]').textContent, 'Fresh Forecast');
  assert.ok(!form.classList.contains('cs-loading'));
  assert.strictEqual(form.getAttribute('data-cs-busy'), null);
  assert.strictEqual(refreshBtn.disabled, false);
  assert.strictEqual(preDisabledBtn.disabled, true);

  // ---- .reset() ----
  chain.set('City', 'Modified');
  chain.set('Token', 'modified');
  chain.reset();
  assert.strictEqual(chain.get('City'), 'Paris');
  assert.strictEqual(chain.get('Token'), '');
  assert.ok(!form.classList.contains('cs-has-errors'));

  let resetEvent = null;
  form.addEventListener('codespirit:reset', (e) => { resetEvent = e; });
  chain.set('City', 'X');
  chain.reset(['City']);
  assert.strictEqual(chain.get('City'), 'Paris');
  assert.ok(resetEvent !== null);

  // ---- .destroy() ----
  chain.destroy();
  assert.strictEqual(chain._destroyed, true);
  try {
    chain.get('City');
    assert.fail('should throw after destroy');
  } catch (e) {
    assert.ok(e.message.indexOf('destroyed') >= 0);
  }
  try {
    chain.set('City', 'test');
    assert.fail('should throw after destroy');
  } catch (e) {
    assert.ok(e.message.indexOf('destroyed') >= 0);
  }

  // ---- New chain after destroy ----
  var chain2 = context.window.CodeSpirit.vm(form);
  assert.ok(chain2._destroyed === false);
  assert.strictEqual(chain2.get('City'), 'Paris');

  // ---- .observe() ----
  var observedFields = [];
  chain2.observe('City', function (value, field) {
    observedFields.push({ value, field });
  });
  chain2.set('City', 'London');
  assert.strictEqual(observedFields.length, 1);
  assert.strictEqual(observedFields[0].field, 'City');
  assert.strictEqual(observedFields[0].value, 'London');

  var observedMulti = [];
  chain2.observe(['City', 'Token'], function (value, field) {
    observedMulti.push({ value, field });
  });
  chain2.set({ City: 'Dublin', Token: 'abc' });
  assert.ok(observedMulti.length >= 2);

  // ---- observe all fields (empty array) ----
  var observedAll = [];
  chain2.observe([], function (value, field) {
    observedAll.push({ value, field });
  });
  chain2.set('City', 'Berlin');
  assert.ok(observedAll.length >= 1);

  // ---- applyErrors / clearErrors ----
  context.window.CodeSpirit.applyErrors(form, { City: 'City is required', Token: 'Invalid token' });
  assert.ok(city.classList.contains('cs-invalid'));
  assert.ok(token.classList.contains('cs-invalid'));
  assert.ok(form.classList.contains('cs-has-errors'));

  var validationEvent = null;
  form.addEventListener('codespirit:validation', (e) => { validationEvent = e; });

  context.window.CodeSpirit.clearErrors(form);
  assert.ok(!city.classList.contains('cs-invalid'));
  assert.ok(!token.classList.contains('cs-invalid'));
  assert.ok(!form.classList.contains('cs-has-errors'));

  // ---- applyErrors with validation event ----
  context.window.CodeSpirit.applyErrors(form, { City: 'Too short' });
  assert.ok(validationEvent !== null);
  assert.strictEqual(validationEvent.detail.errors.City, 'Too short');
  context.window.CodeSpirit.clearErrors(form);

  // ---- Submit with server-side errors ----
  chain2.set('City', 'Test');
  var errorSubmit = new Event('submit', { bubbles: true });
  errorSubmit.submitter = refreshBtn;

  var errorPromise = new Promise((resolve) => form.addEventListener('codespirit:error', resolve));
  context.fetch = async (url, options) => {
    return {
      ok: true,
      json: async () => ({
        errors: { City: 'Server validation failed' }
      })
    };
  };
  form.dispatchEvent(errorSubmit);
  var errorResult = await errorPromise;
  assert.strictEqual(errorResult.detail.City, 'Server validation failed');
  assert.ok(city.classList.contains('cs-invalid'));
  assert.ok(form.classList.contains('cs-has-errors'));
  assert.ok(!form.classList.contains('cs-loading'));
  context.window.CodeSpirit.clearErrors(form);

  // ---- Submit with HTTP error (non-ok response) ----
  var httpErrorSubmit = new Event('submit', { bubbles: true });
  httpErrorSubmit.submitter = refreshBtn;

  var httpErrorPromise = new Promise((resolve) => form.addEventListener('codespirit:error', resolve));
  context.fetch = async (url, options) => {
    return {
      ok: false,
      status: 500,
      text: async () => 'Internal Server Error'
    };
  };
  form.dispatchEvent(httpErrorSubmit);
  var httpError = await httpErrorPromise;
  assert.strictEqual(httpError.detail.status, 500);
  assert.ok(!form.classList.contains('cs-loading'));

  // ---- data-cs-visible binding ----
  const visibleDiv = form.appendChild(new Element('div', { 'data-cs-visible': 'IsActive' }));
  const hiddenDiv = form.appendChild(new Element('div', { 'data-cs-hidden': 'IsActive' }));
  const visibleExact = form.appendChild(new Element('div', { 'data-cs-visible': 'IsActive:true' }));
  const isActiveInput = form.appendChild(new Element('input', { name: 'IsActive', value: 'false' }));

  chain2.set('IsActive', 'true');
  assert.strictEqual(visibleDiv.style.display, '');
  assert.strictEqual(hiddenDiv.style.display, 'none');
  assert.strictEqual(visibleExact.style.display, '');

  chain2.set('IsActive', 'false');
  assert.strictEqual(visibleDiv.style.display, 'none');
  assert.strictEqual(hiddenDiv.style.display, '');
  assert.strictEqual(visibleExact.style.display, 'none');

  // ---- data-cs-class binding ----
  const classDiv = form.appendChild(new Element('div', { 'data-cs-class': 'IsActive:active' }));
  const classDivDefault = form.appendChild(new Element('div', { 'data-cs-class': 'IsActive' }));

  chain2.set('IsActive', 'true');
  assert.ok(classDiv.classList.contains('active'));
  assert.ok(classDivDefault.classList.contains('isactive'));

  chain2.set('IsActive', 'false');
  assert.ok(!classDiv.classList.contains('active'));
  assert.ok(!classDivDefault.classList.contains('isactive'));

  // ---- invoke with errors in result ----
  context.fetch = async (url, options) => {
    return {
      ok: true,
      json: async () => ({
        errors: { City: 'City too short' },
        state: { City: 'Test' }
      })
    };
  };

  var invokeResult = await chain2.invoke('Refresh');
  assert.strictEqual(invokeResult.errors.City, 'City too short');
  assert.ok(city.classList.contains('cs-invalid'));

  context.window.CodeSpirit.clearErrors(form);

  // ---- ui.behaviors.js integration ----
  runScript('wwwroot/js/ui/ui.behaviors.js', context);
  assert.strictEqual(typeof context.window.CodeSpirit.mount, 'function');
  assert.strictEqual(typeof context.window.CodeSpirit.refresh, 'function');

  context.window.CodeSpirit.mount(document);

  let customBehaviorCount = 0;
  context.window.CodeSpirit.ui.register('custom-widget', (elements) => {
    customBehaviorCount += elements.length;
    context.window.CodeSpirit.ui.ready(elements, 'custom-widget');
  });
  const customWidget = form.appendChild(new Element('div', { 'data-ui': 'custom-widget' }));
  const patchedWidget = document.querySelector('[data-cs-region="forecast"]').querySelector('[data-ui="custom-widget"]');
  context.window.CodeSpirit.refresh(document);
  context.window.CodeSpirit.refresh(document);
  assert.strictEqual(patchedWidget.getAttribute('data-ui-ready'), 'custom-widget');
  assert.strictEqual(customWidget.getAttribute('data-ui-ready'), 'custom-widget');
  assert.strictEqual(customBehaviorCount, 2);

  // Verify built-in datepicker behavior
  const dateInput = document.createElement('input');
  dateInput.setAttribute('data-ui', 'datepicker');
  const dateWidget = form.appendChild(dateInput);
  context.window.CodeSpirit.mount(document);
  assert.strictEqual(dateWidget.getAttribute('type'), 'date');
  assert.ok(dateWidget.getAttribute('data-ui-ready').split(/\s+/).indexOf('datepicker') >= 0);

  // Verify confirm-click behavior
  const confirmBtn = document.createElement('button');
  confirmBtn.setAttribute('data-ui', 'confirm-click');
  confirmBtn.setAttribute('data-confirm', 'Proceed?');
  form.appendChild(confirmBtn);
  context.window.CodeSpirit.mount(document);
  assert.ok(confirmBtn.getAttribute('data-ui-ready').split(/\s+/).indexOf('confirm-click') >= 0);

  // Verify auto-submit behavior
  const autoField = document.createElement('input');
  autoField.setAttribute('data-ui', 'auto-submit');
  autoField.setAttribute('data-debounce', '500');
  form.appendChild(autoField);
  context.window.CodeSpirit.mount(document);
  assert.ok(autoField.getAttribute('data-ui-ready').split(/\s+/).indexOf('auto-submit') >= 0);

  // ---- data-cs-attr binding ----
  var linkEl = form.appendChild(new Element('a', { 'data-cs-attr': 'DetailUrl:href' }));
  chain2.set('DetailUrl', '/details/42');
  assert.strictEqual(linkEl.getAttribute('href'), '/details/42');
  chain2.set('DetailUrl', '');
  assert.strictEqual(linkEl.getAttribute('href'), null);

  var imgEl = form.appendChild(new Element('img', { 'data-cs-attr': 'AvatarUrl:src' }));
  chain2.set('AvatarUrl', '/img/avatar.png');
  assert.strictEqual(imgEl.getAttribute('src'), '/img/avatar.png');

  // ---- data-cs-enabled / data-cs-disabled binding ----
  var enabledInput = form.appendChild(new Element('input', { 'data-cs-enabled': 'CanEdit', disabled: true }));
  chain2.set('CanEdit', 'true');
  assert.strictEqual(enabledInput.disabled, false);
  chain2.set('CanEdit', 'false');
  assert.strictEqual(enabledInput.disabled, true);

  var disabledBtn = form.appendChild(new Element('button', { 'data-cs-disabled': 'IsLocked' }));
  chain2.set('IsLocked', 'true');
  assert.strictEqual(disabledBtn.disabled, true);
  chain2.set('IsLocked', 'false');
  assert.strictEqual(disabledBtn.disabled, false);

  // ---- chain.submit() ----
  var submitCalled = false;
  form.requestSubmit = function () {
    submitCalled = true;
  };
  chain2.submit();
  assert.strictEqual(submitCalled, true);

  // ---- chain.validate() ----
  var chain3 = context.window.CodeSpirit.vm(form);
  chain3.set('City', '');
  var valid = chain3.validate({
    City: { required: true, message: 'City required' }
  });
  assert.strictEqual(valid, false);
  assert.ok(city.classList.contains('cs-invalid'));
  context.window.CodeSpirit.clearErrors(form);

  chain3.set('City', 'Ab');
  valid = chain3.validate({
    City: { minLength: 3, message: 'Too short' }
  });
  assert.strictEqual(valid, false);
  context.window.CodeSpirit.clearErrors(form);

  chain3.set('City', 'Paris');
  valid = chain3.validate({
    City: { required: true, minLength: 3 }
  });
  assert.strictEqual(valid, true);
  assert.ok(!city.classList.contains('cs-invalid'));

  valid = chain3.validate({
    City: { pattern: /^[A-Z]/, message: 'Must start with capital' }
  });
  assert.strictEqual(valid, true);

  // Custom validator
  valid = chain3.validate({
    City: { custom: function (val) { return val === 'Paris' || 'Must be Paris'; } }
  });
  assert.strictEqual(valid, true);

  valid = chain3.validate({
    City: { custom: function (val) { return false; } }
  });
  assert.strictEqual(valid, false);
  context.window.CodeSpirit.clearErrors(form);

  console.log('JS boundary validation passed');
}

main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
