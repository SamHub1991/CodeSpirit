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
  }

  preventDefault() {
    this.defaultPrevented = true;
  }
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
  const attrEquals = selector.match(/^\[([^=~\]]+)="([^"]*)"\]$/);
  if (attrEquals) {
    return element.getAttribute(attrEquals[1]) === attrEquals[2];
  }

  const attrToken = selector.match(/^\[([^=~\]]+)~="([^"]*)"\]$/);
  if (attrToken) {
    return (element.getAttribute(attrToken[1]) || '').split(/\s+/).includes(attrToken[2]);
  }

  const attrExists = selector.match(/^\[([^\]]+)\]$/);
  if (attrExists) {
    return element.getAttribute(attrExists[1]) != null;
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
    if (['INPUT', 'BUTTON', 'SELECT', 'TEXTAREA'].includes(this.tagName)) {
      this.value = attributes.value || '';
    }
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
  const form = document.appendChild(new Element('form', { 'data-cs-vm': '', method: 'post' }));
  const city = form.appendChild(new Element('input', { name: 'City', 'data-cs-bind': 'City', value: 'Paris' }));
  const cityLabel = form.appendChild(new Element('span', { 'data-cs-bind': 'City' }));
  const token = form.appendChild(new Element('input', { name: 'Token', value: '' }));
  const refresh = form.appendChild(new Element('button', { type: 'submit', 'data-cs-command': 'Refresh' }));

  let fetchPayload = null;
  const context = vm.createContext({
    window: { location: { pathname: '/weather' } },
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

  // Verify chain API is exposed
  assert.strictEqual(typeof context.window.CodeSpirit.vm, 'function');
  assert.strictEqual(typeof context.window.CodeSpirit.VmChain, 'function');

  // Verify short alias is exposed without the collision-prone CS global.
  assert.strictEqual(context.window.$cs, context.window.CodeSpirit);
  assert.strictEqual(context.window.CS, undefined);

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

  // Verify chain initialization
  var chain = context.window.CodeSpirit.vm(form);
  assert.ok(chain instanceof context.window.CodeSpirit.VmChain);
  assert.strictEqual(chain._form, form);

  // Verify .set() / .get() / .state()
  chain.set('City', 'Oslo');
  assert.strictEqual(city.value, 'Oslo');
  assert.strictEqual(cityLabel.textContent, 'Oslo');
  assert.strictEqual(chain.get('City'), 'Oslo');

  chain.set({ City: 'Stockholm', Token: 'x' });
  assert.strictEqual(city.value, 'Stockholm');
  assert.strictEqual(chain.get('City'), 'Stockholm');

  var snap = chain.state();
  assert.strictEqual(snap.City, 'Stockholm');
  assert.strictEqual(snap.Token, 'x');

  // Verify .val() getter form
  assert.strictEqual(chain.val('City'), 'Stockholm');

  // Verify chaining: set returns this
  assert.strictEqual(chain.set('City', 'Copenhagen'), chain);

  // Verify .on() / .off() / .once() return this
  assert.strictEqual(chain.on('codespirit:updated', function () {}), chain);
  assert.strictEqual(chain.once('codespirit:loaded', function () {}), chain);

  // Verify .el() / .all() scoped queries
  assert.strictEqual(chain.el('[name="City"]'), city);
  assert.ok(chain.all('[name="City"]').length >= 1);

  // Verify .invoke() returns a Promise
  var invoked = chain.invoke('Refresh');
  assert.ok(invoked && typeof invoked.then === 'function');
  assert.ok(invoked && typeof invoked.catch === 'function');

  // Reset city for existing tests after chain modifications
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

  const updated = new Promise((resolve) => form.addEventListener('codespirit:updated', resolve));
  const region = document.appendChild(new Element('section', { 'data-cs-region': 'forecast' }));
  region.textContent = 'Old Forecast';
  const regionWidget = region.appendChild(new Element('div', { 'data-ui': 'custom-widget' }));
  regionWidget.textContent = 'Old Widget';
  const submit = new Event('submit', { bubbles: true });
  submit.submitter = refresh;

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
  await updated;
  assert.strictEqual(submit.defaultPrevented, true);
  assert.strictEqual(fetchPayload.url, '/weather');
  assert.strictEqual(fetchPayload.options.body.City, 'Tokyo');
  assert.strictEqual(fetchPayload.options.body.__command, 'Refresh');
  assert.strictEqual(city.value, 'Rome');
  assert.strictEqual(cityLabel.textContent, 'Rome');
  assert.strictEqual(document.querySelector('[data-cs-region="forecast"]').textContent, 'Fresh Forecast');

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

  console.log('JS boundary validation passed');
}

main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
