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

class JQuerySet {
  constructor(items, previousItems = []) {
    this.items = items;
    this.previousItems = previousItems;
    this.length = items.length;
  }

  find(selector) {
    return new JQuerySet(this.items.flatMap((item) => item.querySelectorAll(selector)), this.items);
  }

  addBack(selector) {
    return new JQuerySet(this.items.concat(this.previousItems.filter((item) => item.matches && item.matches(selector))));
  }

  not(selector) {
    return new JQuerySet(this.items.filter((item) => !item.matches(selector)));
  }

  each(callback) {
    this.items.forEach((item, index) => callback.call(item, index, item));
    return this;
  }

  attr(name, value) {
    this.items.forEach((item, index) => {
      item.setAttribute(name, typeof value === 'function' ? value(index, item.getAttribute(name)) : value);
    });
    return this;
  }

  css(name, value) {
    this.items.forEach((item) => {
      item.style[name] = value;
    });
    return this;
  }

  on(type, handler) {
    this.items.forEach((item) => item.addEventListener(type, handler));
    return this;
  }

  first() {
    return new JQuerySet(this.items.slice(0, 1));
  }

  datepicker() {
    this.items.forEach((item) => {
      item.datepickerCount = (item.datepickerCount || 0) + 1;
    });
    return this;
  }
}

function createJQuery(document) {
  function $(value) {
    if (typeof value === 'function') {
      value();
      return new JQuerySet([]);
    }
    if (value instanceof JQuerySet) {
      return value;
    }
    return new JQuerySet(value ? [value] : []);
  }

  $.fn = JQuerySet.prototype;
  return $;
}

function runScript(file, context) {
  vm.runInContext(fs.readFileSync(path.join(templateRoot, file), 'utf8'), context, { filename: file });
}

async function main() {
  const document = new Document();
  const form = document.appendChild(new Element('form', { 'data-cs-vm': '', method: 'post' }));
  const city = form.appendChild(new Element('input', { name: 'City', 'data-cs-bind': 'City', value: 'Paris' }));
  const cityLabel = form.appendChild(new Element('span', { 'data-cs-bind': 'City' }));
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

  context.window.jQuery = createJQuery(document);
  context.window.$ = context.window.jQuery;
  context.jQuery = context.window.jQuery;
  context.$ = context.window.$;

  const picker = form.appendChild(new Element('input', { name: 'City', 'data-cs-bind': 'City', 'data-ui': 'datepicker', value: 'Milan' }));
  runScript('wwwroot/js/ui/jquery.behaviors.js', context);
  assert.strictEqual(typeof context.window.CodeSpirit.mount, 'function');
  assert.strictEqual(typeof context.window.CodeSpirit.refresh, 'function');
  assert.strictEqual(picker.getAttribute('data-ui-ready'), 'datepicker');
  assert.strictEqual(picker.datepickerCount, 1);

  picker.value = 'Madrid';
  picker.dispatchEvent(new Event('change', { bubbles: true }));
  assert.strictEqual(city.value, 'Madrid');
  assert.strictEqual(cityLabel.textContent, 'Madrid');

  context.window.CodeSpirit.mount(document);
  assert.strictEqual(picker.datepickerCount, 1);

  const dynamicPicker = form.appendChild(new Element('input', { name: 'City', 'data-cs-bind': 'City', 'data-ui': 'datepicker', value: 'Lisbon' }));
  context.window.CodeSpirit.refresh(form);
  assert.strictEqual(dynamicPicker.getAttribute('data-ui-ready'), 'datepicker');
  assert.strictEqual(dynamicPicker.datepickerCount, 1);

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

  console.log('JS boundary validation passed');
}

main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
